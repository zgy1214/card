public sealed class StateLobby : GameState
{
    public override void OnEnter()
    {
        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.RoomEntered += OnRoomEntered;
            onlineGameController.MatchStarted += OnMatchStarted;
        }

        GameEventSystem.Trigger(
            EventRoute.OpenView,
            new OpenViewArg(nameof(Lobby), UIManager.PageLayerName));
    }

    public override void OnExit()
    {
        OnlineGameController onlineGameController = GameApp.Current?.OnlineGameController;
        if (onlineGameController != null)
        {
            onlineGameController.RoomEntered -= OnRoomEntered;
            onlineGameController.MatchStarted -= OnMatchStarted;
        }

        GameEventSystem.Trigger(
            EventRoute.CloseView,
            new CloseViewArg(nameof(Lobby)));
    }

    private void OnMatchStarted()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("game"));
    }

    private void OnRoomEntered()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("room"));
    }
}
