public sealed class GameViewArg : Arg
{
    public GameSession GameSession { get; }

    public GameViewArg(GameSession gameSession)
    {
        if (gameSession == null)
        {
            throw new System.ArgumentNullException(nameof(gameSession));
        }

        GameSession = gameSession;
    }
}
