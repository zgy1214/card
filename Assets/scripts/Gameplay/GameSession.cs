using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GameSession
{
    private readonly List<PlayerRuntime> _gamePlayerRuntimes = new List<PlayerRuntime>();
    private float _phaseTimerElapsedTime;

    public bool IsRunning { get; private set; }
    public string MatchId { get; private set; }
    public string MatchStatus { get; private set; }
    public string LocalPlayerId { get; private set; }
    public int LocalSeatIndex { get; private set; } = -1;
    public IReadOnlyList<PlayerRuntime> GamePlayerRuntimes => _gamePlayerRuntimes;
    public bool IsFinished => string.Equals(MatchStatus, "finished", StringComparison.Ordinal);
    public string Phase { get; private set; }
    public int RoundIndex { get; private set; }
    public double ServerTime { get; private set; }
    public double PhaseEndTime { get; private set; }
    public double StateReceivedRealtime { get; private set; }
    public NetworkGameStatePlayerPayload[] GameStatePlayers { get; private set; }
    public NetworkLocalPlayerPrivatePayload LocalPlayerPrivate { get; private set; }
    public NetworkRoundPublicPayload RoundPublic { get; private set; }
    public NetworkChallengeStatePayload ChallengeState { get; private set; }
    public NetworkShowdownStatePayload ShowdownState { get; private set; }
    public NetworkFortuneStatePayload FortuneState { get; private set; }
    public NetworkFinalResultPayload FinalResult { get; private set; }

    public event Action GameStarted;
    public event Action MatchStateUpdated;
    public event Action PhaseTimerTicked;

    public void Initialize(IEnumerable<PlayerRuntime> gamePlayerRuntimes)
    {
        if (gamePlayerRuntimes == null)
        {
            throw new ArgumentNullException(nameof(gamePlayerRuntimes));
        }

        _gamePlayerRuntimes.Clear();
        foreach (PlayerRuntime gamePlayerRuntime in gamePlayerRuntimes)
        {
            if (gamePlayerRuntime == null)
            {
                throw new ArgumentException("Player runtime cannot be null.", nameof(gamePlayerRuntimes));
            }

            _gamePlayerRuntimes.Add(gamePlayerRuntime);
        }

        if (_gamePlayerRuntimes.Count == 0)
        {
            throw new ArgumentException("At least one player runtime is required.", nameof(gamePlayerRuntimes));
        }

        ResetMatchState();
    }

    public void Shutdown()
    {
        ResetMatchState();
        _gamePlayerRuntimes.Clear();
    }

    public void Tick(float deltaTime)
    {
        TickPhaseTimer(deltaTime);
    }

    private void TickPhaseTimer(float deltaTime)
    {
        if (PhaseEndTime <= 0)
        {
            return;
        }

        if (deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time cannot be negative.");
        }

        _phaseTimerElapsedTime += deltaTime;
        while (_phaseTimerElapsedTime >= 1f)
        {
            _phaseTimerElapsedTime -= 1f;
            PhaseTimerTicked?.Invoke();
        }
    }

    public void SetLocalPlayerId(string localPlayerId)
    {
        LocalPlayerId = localPlayerId;
        RefreshLocalSeatIndex();
    }

    public PlayerRuntime GetPlayerRuntime(int seatIndex)
    {
        return _gamePlayerRuntimes.FirstOrDefault(gamePlayerRuntime => gamePlayerRuntime.SeatIndex == seatIndex);
    }

    public void ApplyMatchStart(string matchId)
    {
        if (string.IsNullOrEmpty(matchId))
        {
            throw new ArgumentException("Match id cannot be null or empty.", nameof(matchId));
        }

        foreach (PlayerRuntime gamePlayerRuntime in _gamePlayerRuntimes)
        {
            gamePlayerRuntime.ClearHandCards();
        }

        MatchId = matchId;
        MatchStatus = "playing";
        IsRunning = true;
        GameStarted?.Invoke();
    }

    public void ApplyGameState(NetworkGameStatePayload networkGameStatePayload)
    {
        if (networkGameStatePayload == null)
        {
            throw new ArgumentNullException(nameof(networkGameStatePayload));
        }

        MatchId = networkGameStatePayload.match_id;
        MatchStatus = networkGameStatePayload.match_status;
        Phase = networkGameStatePayload.phase;
        RoundIndex = networkGameStatePayload.round_index;
        ServerTime = networkGameStatePayload.server_time;
        PhaseEndTime = networkGameStatePayload.phase_end_time;
        StateReceivedRealtime = UnityEngine.Time.realtimeSinceStartupAsDouble;
        _phaseTimerElapsedTime = 0f;
        GameStatePlayers = networkGameStatePayload.players ?? Array.Empty<NetworkGameStatePlayerPayload>();
        LocalPlayerPrivate = networkGameStatePayload.local_player_private;
        RoundPublic = networkGameStatePayload.round_public;
        ChallengeState = networkGameStatePayload.challenge_state;
        ShowdownState = networkGameStatePayload.showdown_state;
        FortuneState = networkGameStatePayload.fortune_state;
        FinalResult = networkGameStatePayload.final_result;

        SynchronizeGameStatePlayers(GameStatePlayers, LocalPlayerPrivate);
        IsRunning = !IsFinished;
        MatchStateUpdated?.Invoke();
    }

    public PlayerRuntime GetDisplayPlayerRuntime(int displayIndex)
    {
        if (displayIndex < 0 || _gamePlayerRuntimes.Count == 0)
        {
            return null;
        }

        int localPlayerIndex = _gamePlayerRuntimes.FindIndex(player => player.IsLocalPlayer);
        if (localPlayerIndex < 0)
        {
            localPlayerIndex = 0;
        }

        int playerIndex = (localPlayerIndex + displayIndex) % _gamePlayerRuntimes.Count;
        return _gamePlayerRuntimes[playerIndex];
    }

    public int GetDisplayIndexForSeat(int seatIndex)
    {
        if (_gamePlayerRuntimes.Count == 0)
        {
            return -1;
        }

        int localPlayerIndex = _gamePlayerRuntimes.FindIndex(player => player.IsLocalPlayer);
        if (localPlayerIndex < 0)
        {
            localPlayerIndex = 0;
        }

        int seatPlayerIndex = _gamePlayerRuntimes.FindIndex(player => player.SeatIndex == seatIndex);
        if (seatPlayerIndex < 0)
        {
            return -1;
        }

        return (seatPlayerIndex - localPlayerIndex + _gamePlayerRuntimes.Count) % _gamePlayerRuntimes.Count;
    }

    private void SynchronizeGameStatePlayers(
        NetworkGameStatePlayerPayload[] playerPayloads,
        NetworkLocalPlayerPrivatePayload localPlayerPrivatePayload)
    {
        _gamePlayerRuntimes.Clear();
        foreach (NetworkGameStatePlayerPayload playerPayload in playerPayloads)
        {
            if (playerPayload == null || playerPayload.seat_index < 0)
            {
                continue;
            }

            bool isLocalPlayer = string.Equals(playerPayload.player_id, LocalPlayerId, StringComparison.Ordinal);
            PlayerRuntime playerRuntime = new PlayerRuntime(
                string.IsNullOrEmpty(playerPayload.player_id) ? $"seat_{playerPayload.seat_index}" : playerPayload.player_id,
                playerPayload.seat_index,
                isLocalPlayer);
            playerRuntime.SetNetworkIdentity(
                playerRuntime.PlayerId,
                playerPayload.name,
                playerPayload.player_type,
                isLocalPlayer,
                playerPayload.character_id);

            List<CardDefinition> handCards = new List<CardDefinition>();
            if (isLocalPlayer && localPlayerPrivatePayload?.hand_cards != null)
            {
                foreach (NetworkCardPayload cardPayload in localPlayerPrivatePayload.hand_cards)
                {
                    if (cardPayload != null && cardPayload.HasValue())
                    {
                        handCards.Add(cardPayload.ToCardDefinition());
                    }
                }
            }

            playerRuntime.SetHandState(handCards, playerPayload.hand_count);
            _gamePlayerRuntimes.Add(playerRuntime);
        }

        _gamePlayerRuntimes.Sort((left, right) => left.SeatIndex.CompareTo(right.SeatIndex));
        RefreshLocalSeatIndex();
    }

    private void RefreshLocalSeatIndex()
    {
        PlayerRuntime localPlayerRuntime = _gamePlayerRuntimes.FirstOrDefault(player => player.IsLocalPlayer);
        LocalSeatIndex = localPlayerRuntime == null ? -1 : localPlayerRuntime.SeatIndex;
    }

    private void ResetMatchState()
    {
        MatchId = null;
        MatchStatus = null;
        Phase = null;
        RoundIndex = 0;
        ServerTime = 0;
        PhaseEndTime = 0;
        StateReceivedRealtime = 0;
        _phaseTimerElapsedTime = 0f;
        GameStatePlayers = Array.Empty<NetworkGameStatePlayerPayload>();
        LocalPlayerPrivate = null;
        RoundPublic = null;
        ChallengeState = null;
        ShowdownState = null;
        FortuneState = null;
        FinalResult = null;
        IsRunning = false;
        LocalSeatIndex = -1;
        foreach (PlayerRuntime gamePlayerRuntime in _gamePlayerRuntimes)
        {
            gamePlayerRuntime.ClearHandCards();
        }
    }
}
