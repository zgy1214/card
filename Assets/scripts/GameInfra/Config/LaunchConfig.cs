public sealed class LaunchConfig
{
    public string InitialState { get; } = "lobby";
    public string ServerUri { get; } = "ws://127.0.0.1:8765";
}
