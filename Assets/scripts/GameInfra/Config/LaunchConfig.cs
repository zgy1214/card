public sealed class LaunchConfig
{
    public const string RemoteServerUri = "ws://47.93.85.51:8765";
#if UNITY_EDITOR
    public const string LocalServerUri = "ws://127.0.0.1:8765";
    public const string EditorRemoteServerPreference = "CardGame.UseRemoteServer";
#endif

    public string InitialState { get; } = "start";
    public string ServerUri { get; }

    public LaunchConfig()
    {
#if UNITY_EDITOR
        ServerUri = UnityEditor.EditorPrefs.GetBool(EditorRemoteServerPreference, false)
            ? RemoteServerUri : LocalServerUri;
#else
        ServerUri = RemoteServerUri;
#endif
    }
}
