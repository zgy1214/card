public sealed class StateLoading : GameState
{
    private const float MinimumDisplaySeconds = 1.5f;
    private const float CompletionDisplayBufferSeconds = 0.2f;

    private float _elapsedSeconds;
    private bool _hasRequestedLobby;

    public override void OnEnter()
    {
        _elapsedSeconds = 0f;
        _hasRequestedLobby = false;
        GameEventSystem.Trigger(
            EventRoute.OpenView,
            new OpenViewArg(nameof(Loading), UIManager.PageLayerName));
        GameApp.Current?.OnlineGameController?.StartStartupConnection();
    }

    public override void Tick(float deltaTime)
    {
        _elapsedSeconds += deltaTime;
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (_hasRequestedLobby || controller == null || controller.StartupFailed)
        {
            return;
        }

        if (controller.StartupSucceeded
            && _elapsedSeconds >= MinimumDisplaySeconds + CompletionDisplayBufferSeconds)
        {
            _hasRequestedLobby = true;
            GameEventSystem.Trigger(
                EventRoute.SwitchState,
                new SwitchStateArg("lobby"));
        }
    }

    public override void OnExit()
    {
        GameEventSystem.Trigger(
            EventRoute.CloseView,
            new CloseViewArg(nameof(Loading)));
    }
}
