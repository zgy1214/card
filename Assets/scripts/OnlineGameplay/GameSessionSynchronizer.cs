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

    public void ApplyGameState(NetworkGameStatePayload networkGameStatePayload)
    {
        _gameSession.ApplyGameState(networkGameStatePayload);
    }

}
