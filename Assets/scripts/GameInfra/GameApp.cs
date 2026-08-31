using UnityEngine;

public sealed class GameApp
{
    public static GameApp Current { get; private set; }

    public bool IsInitialized { get; private set; }
    public ConfigManager GameConfigManager { get; private set; }
    public OnlineGameController OnlineGameController { get; private set; }
    public TimerManager GameTimerManager { get; private set; }
    public UIManager GameUIManager { get; private set; }
    public StateManager GameStateManager { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        Current = this;
        GameConfigManager = new ConfigManager();
        GameConfigManager.Initialize();

        OnlineGameController = new OnlineGameController(GameConfigManager.GameLaunchConfig);
        OnlineGameController.Initialize();

        GameUIManager = new UIManager();
        GameUIManager.Initialize();

        GameStateManager = new StateManager(GameConfigManager.GameLaunchConfig);
        GameTimerManager = new TimerManager(GameStateManager.GameEventSystem);
        GameUIManager.BindEventSystem(GameStateManager.GameEventSystem);
        GameStateManager.Initialize();
        IsInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsInitialized)
        {
            return;
        }

        OnlineGameController?.Tick(deltaTime);
        GameStateManager?.Tick(deltaTime);
        GameTimerManager?.Tick(deltaTime);
    }

    public void Shutdown()
    {
        if (!IsInitialized)
        {
            return;
        }

        OnlineGameController?.Shutdown();
        OnlineGameController = null;

        GameUIManager?.Shutdown();
        GameUIManager = null;

        GameStateManager?.Shutdown();
        GameStateManager = null;

        GameTimerManager?.Shutdown();
        GameTimerManager = null;

        GameConfigManager?.Shutdown();
        GameConfigManager = null;

        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }

        IsInitialized = false;
    }
}
