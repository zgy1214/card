public sealed class StateRoom : GameState
{
    public override void OnEnter()
    {
        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.RoomLeft += OnRoomLeft;
            onlineGameController.MatchStarted += OnMatchStarted;
        }

        GameEventSystem.Trigger(
            EventRoute.OpenView,
            new OpenViewArg(nameof(Room), UIManager.PageLayerName));
    }

    public override void OnExit()
    {
        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.RoomLeft -= OnRoomLeft;
            onlineGameController.MatchStarted -= OnMatchStarted;
        }

        GameEventSystem.Trigger(
            EventRoute.CloseView,
            new CloseViewArg(nameof(Room)));
    }

    private void OnRoomLeft()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("lobby"));
    }

    private void OnMatchStarted()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("game"));
    }
}
