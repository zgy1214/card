public sealed class StateManager
{
    private readonly StateGame _gameStateGame;
    private readonly StateLobby _gameStateLobby;
    private readonly StateRoom _gameStateRoom;
    private readonly GameState _initialGameState;

    public GameState CurrentState { get; private set; }
    public EventSystem GameEventSystem { get; private set; }

    public StateManager(LaunchConfig gameLaunchConfig)
    {
        GameEventSystem = new EventSystem();
        GameEventSystem.Register(EventRoute.SwitchState, OnSwitchState);
        _gameStateLobby = new StateLobby();
        _gameStateRoom = new StateRoom();
        _gameStateGame = new StateGame();
        _gameStateLobby.BindEventSystem(GameEventSystem);
        _gameStateRoom.BindEventSystem(GameEventSystem);
        _gameStateGame.BindEventSystem(GameEventSystem);
        _initialGameState = GetInitialState(gameLaunchConfig);
    }

    public void Initialize()
    {
        if (CurrentState != null)
        {
            return;
        }

        SetState(_initialGameState);
    }

    public void SetState(GameState nextState)
    {
        if (ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        CurrentState?.OnExit();
        CurrentState = nextState;
        CurrentState?.OnEnter();
    }

    public void Reset()
    {
        SetState(null);
    }

    public void Tick(float deltaTime)
    {
        CurrentState?.Tick(deltaTime);
    }

    public void Shutdown()
    {
        Reset();
        GameEventSystem?.Unregister(EventRoute.SwitchState, OnSwitchState);
        GameEventSystem?.Clear();
        GameEventSystem = null;
    }

    private GameState GetInitialState(LaunchConfig gameLaunchConfig)
    {
        if (gameLaunchConfig == null)
        {
            throw new System.ArgumentNullException(nameof(gameLaunchConfig));
        }

        switch (gameLaunchConfig.InitialState)
        {
            case "lobby":
                return _gameStateLobby;
            case "game":
                return _gameStateGame;
            case "room":
                return _gameStateRoom;
            default:
                throw new System.ArgumentOutOfRangeException(
                    nameof(gameLaunchConfig.InitialState),
                    gameLaunchConfig.InitialState,
                    "Unsupported initial state.");
        }
    }

    private void OnSwitchState(Arg arg)
    {
        SwitchStateArg switchStateArg = arg as SwitchStateArg;
        if (switchStateArg == null)
        {
            throw new System.ArgumentException("SwitchState event requires SwitchStateArg.", nameof(arg));
        }

        switch (switchStateArg.StateName)
        {
            case "lobby":
                SetState(_gameStateLobby);
                return;
            case "game":
                SetState(_gameStateGame);
                return;
            case "room":
                SetState(_gameStateRoom);
                return;
            default:
                throw new System.ArgumentOutOfRangeException(
                    nameof(switchStateArg.StateName),
                    switchStateArg.StateName,
                    "Unsupported state name.");
        }
    }
}
