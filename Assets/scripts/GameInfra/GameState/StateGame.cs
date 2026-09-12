public sealed class StateGame : GameState
{
    private bool _settlementOpened;

    public override void OnEnter()
    {
        _settlementOpened = false;
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
        GameEventSystem.Trigger(EventRoute.CloseView, new CloseViewArg(nameof(Settlement)));
        _settlementOpened = false;
    }

    public override void Tick(float deltaTime)
    {
        GameSession session = GameApp.Current?.OnlineGameController?.GameSession;
        if (_settlementOpened || session == null || !session.IsFinished || session.Phase != "final_result") return;

        GameEventSystem.Trigger(EventRoute.OpenView,
            new OpenViewArg(nameof(Settlement), UIManager.PageLayerName, viewArg: new GameViewArg(session)));
        GameEventSystem.Trigger(EventRoute.CloseView, new CloseViewArg(nameof(Game)));
        _settlementOpened = true;
    }

    private void OnMatchEnded()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("lobby"));
    }
}
