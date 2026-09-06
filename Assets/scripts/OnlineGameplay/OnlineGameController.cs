using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class OnlineGameController
{
    private readonly LaunchConfig _gameLaunchConfig;
    private readonly WebSocketTransport _webSocketTransport = new WebSocketTransport();
    private readonly GameSessionSynchronizer _gameSessionSynchronizer;

    private bool _helloSent;

    public LocalPlayerProfile LocalPlayerProfile { get; } = new LocalPlayerProfile();
    public LobbyModel LobbyModel { get; } = new LobbyModel();
    public RoomModel RoomModel { get; } = new RoomModel();
    public GameSession GameSession { get; } = new GameSession();
    public bool IsConnected => _webSocketTransport.IsConnected;

    public event Action RoomEntered;
    public event Action RoomLeft;
    public event Action MatchStarted;
    public event Action MatchEnded;
    public event Action<NetworkRoomChatPayload> RoomChatReceived;
    public event Action<NetworkGameChatPayload> GameChatReceived;
    public event Action<string> ErrorOccurred;

    public OnlineGameController(LaunchConfig gameLaunchConfig)
    {
        _gameLaunchConfig = gameLaunchConfig ?? throw new ArgumentNullException(nameof(gameLaunchConfig));
        GameSession.Initialize(new[]
        {
            new PlayerRuntime("player0", 0, false),
            new PlayerRuntime("player1", 1, false),
            new PlayerRuntime("player2", 2, false),
            new PlayerRuntime("player3", 3, false)
        });
        _gameSessionSynchronizer = new GameSessionSynchronizer(GameSession);
    }

    public void Initialize()
    {
        LocalPlayerProfile.LoadOrCreate();
        GameSession.SetLocalPlayerId(LocalPlayerProfile.PlayerId);
        FireAndForget(EnsureSessionAsync(), "Failed to connect to the game server during initialization.");
    }

    public void Tick(float deltaTime)
    {
        LocalPlayerProfile.Tick(deltaTime);
        ProcessIncomingMessages();
        GameSession.Tick(deltaTime);
    }

    public void Shutdown()
    {
        LocalPlayerProfile.SaveIfDirty(force: true);
        _webSocketTransport.Disconnect();
        GameSession.Shutdown();
        RoomModel.Clear();
        LobbyModel.SetSessionReady(false);
    }

    public void RequestRoomList()
    {
        FireAndForget(SendTextAsync(NetworkProtocolMessages.SerializeRoomListRequest()), "Failed to request room list.");
    }

    public void CreateRoom(string roomName)
    {
        string normalizedRoomName = string.IsNullOrEmpty(roomName) ? string.Empty : roomName.Trim();
        if (string.IsNullOrEmpty(normalizedRoomName))
        {
            normalizedRoomName = $"{LocalPlayerProfile.Name} 的房间";
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomCreateRequest(normalizedRoomName)),
            "Failed to create room.");
    }

    public void SetCharacter(string characterId)
    {
        LocalPlayerProfile.SetCharacterId(characterId);
        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeProfileSetCharacterRequest(LocalPlayerProfile.CharacterId)),
            "Failed to update character.");
    }

    public void JoinRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            PublishError("请输入房间号。");
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomJoinRequest(roomId.Trim())),
            "Failed to join room.");
    }

    public void LeaveRoom()
    {
        if (!RoomModel.HasRoom)
        {
            return;
        }

        string roomId = RoomModel.RoomId;
        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomLeaveRequest(roomId)),
            "Failed to leave room.");
        RoomModel.Clear();
        RoomLeft?.Invoke();
        RequestRoomList();
    }

    public void SetReady(bool isReady)
    {
        if (!RoomModel.HasRoom)
        {
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomReadyRequest(RoomModel.RoomId, isReady)),
            "Failed to set room ready.");
    }

    public void AddAI()
    {
        if (!RoomModel.HasRoom)
        {
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomAddAiRequest(RoomModel.RoomId)),
            "Failed to add AI.");
    }

    public void StartRoomGame()
    {
        if (!RoomModel.HasRoom)
        {
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomStartGameRequest(RoomModel.RoomId)),
            "Failed to start room game.");
    }

    public void SendRoomChat(string message)
    {
        if (!RoomModel.HasRoom)
        {
            return;
        }

        string trimmedMessage = string.IsNullOrEmpty(message) ? string.Empty : message.Trim();
        if (string.IsNullOrEmpty(trimmedMessage))
        {
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeRoomChatRequest(RoomModel.RoomId, trimmedMessage)),
            "Failed to send room chat.");
    }

    public void StartMatchmaking()
    {
        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeMatchmakingStartRequest()),
            "Failed to start matchmaking.");
    }

    public void CancelMatchmaking()
    {
        // 取消必须等服务器确认。UI 遮罩会保留到 matchmaking/state idle 或 matchmaking/found。
        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeMatchmakingCancelRequest()),
            "Failed to cancel matchmaking.");
    }

    public void SubmitPlay(string[] cardIds, string faceUpCardId)
    {
        if (cardIds == null || cardIds.Length == 0)
        {
            PublishError("请选择至少一张手牌。");
            return;
        }

        if (string.IsNullOrEmpty(faceUpCardId))
        {
            PublishError("请选择一张明牌。");
            return;
        }

        if (string.IsNullOrEmpty(GameSession.MatchId))
        {
            Debug.LogWarning("Cannot send game/submit_play because there is no active match id.");
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeSubmitPlayRequest(GameSession.MatchId, cardIds, faceUpCardId)),
            "Failed to send game/submit_play.");
    }

    public void SubmitChallenge(int targetSeatIndex)
    {
        if (string.IsNullOrEmpty(GameSession.MatchId))
        {
            Debug.LogWarning("Cannot send game/submit_challenge because there is no active match id.");
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeSubmitChallengeRequest(GameSession.MatchId, targetSeatIndex)),
            "Failed to send game/submit_challenge.");
    }

    public void SubmitFortuneDraw(int drawCount)
    {
        if (string.IsNullOrEmpty(GameSession.MatchId))
        {
            Debug.LogWarning("Cannot send game/submit_fortune_draw because there is no active match id.");
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeSubmitFortuneDrawRequest(GameSession.MatchId, drawCount)),
            "Failed to send game/submit_fortune_draw.");
    }

    public void SendGameChat(string message)
    {
        if (string.IsNullOrEmpty(GameSession.MatchId))
        {
            return;
        }

        string trimmedMessage = string.IsNullOrEmpty(message) ? string.Empty : message.Trim();
        if (string.IsNullOrEmpty(trimmedMessage))
        {
            return;
        }

        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeGameChatRequest(GameSession.MatchId, trimmedMessage)),
            "Failed to send game/chat.");
    }

    public void LeaveGame()
    {
        if (string.IsNullOrEmpty(GameSession.MatchId))
        {
            MatchEnded?.Invoke();
            return;
        }

        string matchId = GameSession.MatchId;
        FireAndForget(
            SendTextAsync(NetworkProtocolMessages.SerializeGameLeaveRequest(matchId)),
            "Failed to leave game.");
        MatchEnded?.Invoke();
        RequestRoomList();
    }

    private async Task SendTextAsync(string rawMessage)
    {
        await EnsureSessionAsync();
        if (!IsConnected)
        {
            PublishError("未连接服务器，请稍后重试。");
            return;
        }

        await _webSocketTransport.SendTextAsync(rawMessage);
    }

    private async Task EnsureSessionAsync()
    {
        await EnsureConnectedAsync();
        if (!IsConnected || _helloSent)
        {
            return;
        }

        _helloSent = true;
        await _webSocketTransport.SendTextAsync(
            NetworkProtocolMessages.SerializeSessionHello(
                LocalPlayerProfile.PlayerId,
                LocalPlayerProfile.Name,
                LocalPlayerProfile.CharacterId));
    }

    private async Task EnsureConnectedAsync()
    {
        if (IsConnected)
        {
            return;
        }

        while (_webSocketTransport.IsConnecting)
        {
            await Task.Delay(50);
        }

        if (IsConnected)
        {
            return;
        }

        try
        {
            _helloSent = false;
            await _webSocketTransport.ConnectAsync(_gameLaunchConfig.ServerUri);
            Debug.Log($"Connected to server: {_gameLaunchConfig.ServerUri}");
        }
        catch (Exception exception)
        {
            LobbyModel.SetSessionReady(false);
            Debug.LogError($"Failed to connect to server {_gameLaunchConfig.ServerUri}: {exception}");
        }
    }

    private void ProcessIncomingMessages()
    {
        while (_webSocketTransport.TryDequeueReceivedMessage(out string rawMessage))
        {
            HandleServerMessage(rawMessage);
        }
    }

    private void HandleServerMessage(string rawMessage)
    {
        if (!NetworkProtocolMessages.TryGetMessageType(rawMessage, out string messageType))
        {
            Debug.LogWarning($"Cannot parse server message type: {rawMessage}");
            return;
        }

        switch (messageType)
        {
            case NetworkProtocolMessages.SessionHelloAck:
                HandleSessionHelloAck(rawMessage);
                return;
            case NetworkProtocolMessages.RoomListResult:
                HandleRoomListResult(rawMessage);
                return;
            case NetworkProtocolMessages.RoomState:
                HandleRoomState(rawMessage);
                return;
            case NetworkProtocolMessages.RoomChatMessage:
                HandleRoomChatMessage(rawMessage);
                return;
            case NetworkProtocolMessages.MatchmakingState:
                HandleMatchmakingState(rawMessage);
                return;
            case NetworkProtocolMessages.MatchmakingFound:
                HandleMatchmakingFound(rawMessage);
                return;
            case NetworkProtocolMessages.GameMatchStart:
                HandleMatchStart(rawMessage);
                return;
            case NetworkProtocolMessages.GameState:
                HandleGameState(rawMessage);
                return;
            case NetworkProtocolMessages.GameChat:
                HandleGameChat(rawMessage);
                return;
            case NetworkProtocolMessages.GamePlayerReplacedByAi:
                HandlePlayerReplacedByAi(rawMessage);
                return;
            case NetworkProtocolMessages.Error:
                HandleError(rawMessage);
                return;
            default:
                Debug.LogWarning($"Unsupported server message type: {messageType}");
                return;
        }
    }

    private void HandleSessionHelloAck(string rawMessage)
    {
        NetworkSessionHelloAckMessage message = JsonUtility.FromJson<NetworkSessionHelloAckMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid session/hello_ack payload.");
            return;
        }

        LobbyModel.SetSessionReady(true);
        LocalPlayerProfile.SetCharacterId(message.payload.character_id);
        RequestRoomList();
    }

    private void HandleRoomListResult(string rawMessage)
    {
        NetworkRoomListResultMessage message = JsonUtility.FromJson<NetworkRoomListResultMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid room/list_result payload.");
            return;
        }

        LobbyModel.ApplyRoomList(message.payload.rooms);
    }

    private void HandleRoomState(string rawMessage)
    {
        NetworkRoomStateMessage message = JsonUtility.FromJson<NetworkRoomStateMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid room/state payload.");
            return;
        }

        bool wasInRoom = RoomModel.HasRoom;
        RoomModel.ApplyState(message.payload);
        if (!wasInRoom)
        {
            RoomEntered?.Invoke();
        }
    }

    private void HandleRoomChatMessage(string rawMessage)
    {
        NetworkRoomChatMessage message = JsonUtility.FromJson<NetworkRoomChatMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid room/chat_message payload.");
            return;
        }

        RoomChatReceived?.Invoke(message.payload);
    }

    private void HandleMatchmakingState(string rawMessage)
    {
        NetworkMatchmakingStateMessage message = JsonUtility.FromJson<NetworkMatchmakingStateMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid matchmaking/state payload.");
            return;
        }

        LobbyModel.ApplyMatchmakingState(message.payload);
    }

    private void HandleMatchmakingFound(string rawMessage)
    {
        NetworkMatchmakingFoundMessage message = JsonUtility.FromJson<NetworkMatchmakingFoundMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid matchmaking/found payload.");
            return;
        }

        LobbyModel.ResetMatchmaking();
    }

    private void HandleMatchStart(string rawMessage)
    {
        NetworkMatchStartMessage message = JsonUtility.FromJson<NetworkMatchStartMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid game/match_start payload.");
            return;
        }

        _gameSessionSynchronizer.ApplyMatchStart(message.payload);
        MatchStarted?.Invoke();
        RoomModel.Clear();
    }

    private void HandleGameState(string rawMessage)
    {
        NetworkGameStateMessage message = JsonUtility.FromJson<NetworkGameStateMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid game/state payload.");
            return;
        }

        _gameSessionSynchronizer.ApplyGameState(message.payload);
    }

    private void HandleGameChat(string rawMessage)
    {
        NetworkGameChatMessage message = JsonUtility.FromJson<NetworkGameChatMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid game/chat payload.");
            return;
        }

        GameChatReceived?.Invoke(message.payload);
    }

    private void HandlePlayerReplacedByAi(string rawMessage)
    {
        NetworkPlayerReplacedByAiMessage message = JsonUtility.FromJson<NetworkPlayerReplacedByAiMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid game/player_replaced_by_ai payload.");
            return;
        }

        Debug.Log($"Player at seat {message.payload.seat_index} was replaced by AI.");
    }

    private void HandleError(string rawMessage)
    {
        NetworkErrorMessage message = JsonUtility.FromJson<NetworkErrorMessage>(rawMessage);
        if (message?.payload == null)
        {
            Debug.LogWarning("Received invalid error payload.");
            return;
        }

        string text = string.IsNullOrEmpty(message.payload.message)
            ? message.payload.code
            : message.payload.message;
        Debug.LogWarning($"Server error [{message.payload.scope}/{message.payload.code}]: {text}");
        PublishError(text);
    }

    private void PublishError(string message)
    {
        ErrorOccurred?.Invoke(message);
    }

    private void FireAndForget(Task task, string errorPrefix)
    {
        task.ContinueWith(
            continuationTask =>
            {
                if (continuationTask.IsFaulted && continuationTask.Exception != null)
                {
                    Debug.LogError($"{errorPrefix} {continuationTask.Exception}");
                }
            },
            TaskScheduler.Default);
    }
}
