using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GameSession
{
    private readonly List<PlayerRuntime> _gamePlayerRuntimes = new List<PlayerRuntime>();

    public bool IsRunning { get; private set; }
    public string MatchId { get; private set; }
    public string MatchStatus { get; private set; }
    public string LocalPlayerId { get; private set; }
    public int LocalSeatIndex { get; private set; } = -1;
    public IReadOnlyList<PlayerRuntime> GamePlayerRuntimes => _gamePlayerRuntimes;
    public TurnManager GameTurnManager { get; private set; }
    public CardDefinition CurrentPlayedCardDefinition { get; private set; }
    public int CurrentPlayedSeatIndex { get; private set; } = -1;
    public int WinnerSeatIndex { get; private set; } = -1;
    public bool IsFinished => string.Equals(MatchStatus, "finished", StringComparison.Ordinal);

    public event Action GameStarted;
    public event Action MatchStateUpdated;
    public event Action<PlayerRuntime, CardDefinition> HandCardPlayed;
    public event Action MatchEnded;

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

        GameTurnManager = new TurnManager(_gamePlayerRuntimes);
        GameTurnManager.Initialize();
        ResetMatchState();
    }

    public void Shutdown()
    {
        ResetMatchState();
        GameTurnManager = null;
        _gamePlayerRuntimes.Clear();
    }

    public void Tick(float deltaTime)
    {
        GameTurnManager?.Tick(deltaTime);
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
        WinnerSeatIndex = -1;
        IsRunning = true;
        CurrentPlayedCardDefinition = null;
        CurrentPlayedSeatIndex = -1;
        GameTurnManager?.Initialize();
        GameStarted?.Invoke();
    }

    public void ApplyMatchState(NetworkMatchStatePayload networkMatchStatePayload)
    {
        if (networkMatchStatePayload == null)
        {
            throw new ArgumentNullException(nameof(networkMatchStatePayload));
        }

        if (string.IsNullOrEmpty(MatchId))
        {
            MatchId = networkMatchStatePayload.match_id;
        }

        MatchStatus = networkMatchStatePayload.match_status;
        if (networkMatchStatePayload.current_played_card != null
            && networkMatchStatePayload.current_played_card.HasValue())
        {
            CurrentPlayedCardDefinition = networkMatchStatePayload.current_played_card.ToCardDefinition();
        }
        else
        {
            CurrentPlayedCardDefinition = null;
            CurrentPlayedSeatIndex = -1;
        }

        if (networkMatchStatePayload.players == null)
        {
            throw new InvalidOperationException("Match state players payload cannot be null.");
        }

        SynchronizePlayers(networkMatchStatePayload.players);
        foreach (NetworkPlayerStatePayload networkPlayerStatePayload in networkMatchStatePayload.players)
        {
            if (networkPlayerStatePayload == null)
            {
                continue;
            }

            PlayerRuntime gamePlayerRuntime = GetPlayerRuntime(networkPlayerStatePayload.seat_index);
            if (gamePlayerRuntime == null)
            {
                throw new InvalidOperationException($"Cannot find player runtime for seat index: {networkPlayerStatePayload.seat_index}");
            }

            List<CardDefinition> handCardDefinitions = new List<CardDefinition>();
            if (networkPlayerStatePayload.hand_cards != null)
            {
                foreach (NetworkCardPayload networkCardPayload in networkPlayerStatePayload.hand_cards)
                {
                    if (networkCardPayload == null || !networkCardPayload.HasValue())
                    {
                        continue;
                    }

                    handCardDefinitions.Add(networkCardPayload.ToCardDefinition());
                }
            }

            gamePlayerRuntime.SetHandState(handCardDefinitions, networkPlayerStatePayload.hand_count);
        }

        EnsureTurnManager();
        GameTurnManager?.Synchronize(networkMatchStatePayload.turn, networkMatchStatePayload.current_seat_index);
        IsRunning = !IsFinished;
        MatchStateUpdated?.Invoke();
    }

    public void ApplyTurnStart(NetworkTurnStartPayload networkTurnStartPayload)
    {
        if (networkTurnStartPayload == null)
        {
            throw new ArgumentNullException(nameof(networkTurnStartPayload));
        }

        MatchStatus = "playing";
        IsRunning = true;
        EnsureTurnManager();
        GameTurnManager?.ApplyTurnStart(
            networkTurnStartPayload.turn,
            networkTurnStartPayload.current_seat_index,
            networkTurnStartPayload.remaining_seconds);
    }

    public void ApplyCardPlayed(NetworkCardPlayedPayload networkCardPlayedPayload)
    {
        if (networkCardPlayedPayload == null)
        {
            throw new ArgumentNullException(nameof(networkCardPlayedPayload));
        }

        if (networkCardPlayedPayload.card == null || !networkCardPlayedPayload.card.HasValue())
        {
            throw new InvalidOperationException("Played card payload is missing card data.");
        }

        PlayerRuntime gamePlayerRuntime = GetPlayerRuntime(networkCardPlayedPayload.seat_index);
        if (gamePlayerRuntime == null)
        {
            throw new InvalidOperationException($"Cannot find player runtime for seat index: {networkCardPlayedPayload.seat_index}");
        }

        CardDefinition playedCardDefinition = networkCardPlayedPayload.card.ToCardDefinition();
        CurrentPlayedCardDefinition = playedCardDefinition;
        CurrentPlayedSeatIndex = networkCardPlayedPayload.seat_index;
        HandCardPlayed?.Invoke(gamePlayerRuntime, playedCardDefinition);
    }

    public void ApplyMatchEnd(NetworkMatchEndPayload networkMatchEndPayload)
    {
        if (networkMatchEndPayload == null)
        {
            throw new ArgumentNullException(nameof(networkMatchEndPayload));
        }

        MatchStatus = networkMatchEndPayload.match_status;
        WinnerSeatIndex = networkMatchEndPayload.winner_seat_index;
        IsRunning = false;
        GameTurnManager?.Finish();
        MatchEnded?.Invoke();
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

    private void SynchronizePlayers(NetworkPlayerStatePayload[] playerPayloads)
    {
        _gamePlayerRuntimes.Clear();
        foreach (NetworkPlayerStatePayload playerPayload in playerPayloads)
        {
            if (playerPayload == null || playerPayload.seat_index < 0)
            {
                continue;
            }

            bool isLocalPlayer = string.Equals(playerPayload.player_id, LocalPlayerId, StringComparison.Ordinal);
            PlayerRuntime playerRuntime = new PlayerRuntime(
                string.IsNullOrEmpty(playerPayload.player_id) ? $"seat_{playerPayload.seat_index}" : playerPayload.player_id,
                playerPayload.seat_index,
                isLocalPlayer,
                1);
            playerRuntime.SetNetworkIdentity(
                playerRuntime.PlayerId,
                playerPayload.name,
                playerPayload.player_type,
                isLocalPlayer);
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

    private void EnsureTurnManager()
    {
        if (GameTurnManager != null || _gamePlayerRuntimes.Count == 0)
        {
            return;
        }

        GameTurnManager = new TurnManager(_gamePlayerRuntimes);
        GameTurnManager.Initialize();
    }

    private void ResetMatchState()
    {
        MatchId = null;
        MatchStatus = null;
        CurrentPlayedCardDefinition = null;
        CurrentPlayedSeatIndex = -1;
        WinnerSeatIndex = -1;
        IsRunning = false;
        LocalSeatIndex = -1;
        foreach (PlayerRuntime gamePlayerRuntime in _gamePlayerRuntimes)
        {
            gamePlayerRuntime.ClearHandCards();
        }

        GameTurnManager?.Initialize();
    }
}
