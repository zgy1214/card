public sealed class StateGame : GameState
{
    public override void OnEnter()
    {
        GameSession gameSession = GameApp.Current?.OnlineGameController?.GameSession;
        if (gameSession == null)
        {
            throw new System.InvalidOperationException("Online game session is not available.");
        }

        GameEventSystem.Trigger(
            EventRoute.OpenView,
            new OpenViewArg(
                nameof(Game),
                UIManager.PageLayerName,
                viewArg: new GameViewArg(gameSession)));

        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.MatchEnded += OnMatchEnded;
        }
    }

    public override void OnExit()
    {
        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.MatchEnded -= OnMatchEnded;
        }

        GameEventSystem.Trigger(
            EventRoute.CloseView,
            new CloseViewArg(nameof(Game)));
    }

    private void OnMatchEnded()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("lobby"));
    }
}
