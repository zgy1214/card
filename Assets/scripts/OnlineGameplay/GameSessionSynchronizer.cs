using System;

public sealed class GameSessionSynchronizer
{
    private readonly GameSession _gameSession;

    public GameSessionSynchronizer(GameSession gameSession)
    {
        _gameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
    }

    public void ApplyMatchStart(NetworkMatchStartPayload networkMatchStartPayload)
    {
        if (networkMatchStartPayload == null)
        {
            throw new ArgumentNullException(nameof(networkMatchStartPayload));
        }

        _gameSession.ApplyMatchStart(networkMatchStartPayload.match_id);
    }

    public void ApplyMatchState(NetworkMatchStatePayload networkMatchStatePayload)
    {
        _gameSession.ApplyMatchState(networkMatchStatePayload);
    }

    public void ApplyTurnStart(NetworkTurnStartPayload networkTurnStartPayload)
    {
        _gameSession.ApplyTurnStart(networkTurnStartPayload);
    }

    public void ApplyCardPlayed(NetworkCardPlayedPayload networkCardPlayedPayload)
    {
        _gameSession.ApplyCardPlayed(networkCardPlayedPayload);
    }

    public void ApplyMatchEnd(NetworkMatchEndPayload networkMatchEndPayload)
    {
        _gameSession.ApplyMatchEnd(networkMatchEndPayload);
    }
}
