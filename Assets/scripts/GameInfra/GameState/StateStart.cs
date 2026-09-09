public sealed class StateStart : GameState
{
    public override void OnEnter()
    {
        GameEventSystem.Trigger(
            EventRoute.OpenView,
            new OpenViewArg(nameof(Start), UIManager.PageLayerName));
    }

    public override void OnExit()
    {
        GameEventSystem.Trigger(
            EventRoute.CloseView,
            new CloseViewArg(nameof(Start)));
    }
}
