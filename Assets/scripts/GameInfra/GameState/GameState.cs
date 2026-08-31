public class GameState
{
    protected EventSystem GameEventSystem { get; private set; }

    public virtual void OnEnter()
    {
    }

    public virtual void OnExit()
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    internal void BindEventSystem(EventSystem gameEventSystem)
    {
        if (gameEventSystem == null)
        {
            throw new System.ArgumentNullException(nameof(gameEventSystem));
        }

        GameEventSystem = gameEventSystem;
    }
}
