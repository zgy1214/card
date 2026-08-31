public sealed class ConfigManager
{
    public LaunchConfig GameLaunchConfig { get; private set; }

    public void Initialize()
    {
        GameLaunchConfig = new LaunchConfig();
    }

    public void Shutdown()
    {
        GameLaunchConfig = null;
    }
}
