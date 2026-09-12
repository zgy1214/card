using UnityEditor;
using UnityEngine;

public static class ServerEnvironmentMenu
{
    private const string LocalMenu = "游戏工具/服务器/本地服务器";
    private const string RemoteMenu = "游戏工具/服务器/远程服务器";

    [MenuItem(LocalMenu, false, 100)]
    private static void UseLocalServer() => SelectServer(false);

    [MenuItem(RemoteMenu, false, 101)]
    private static void UseRemoteServer() => SelectServer(true);

    [MenuItem(LocalMenu, true)]
    private static bool ValidateLocalServer() => ValidateSelection(LocalMenu, false);

    [MenuItem(RemoteMenu, true)]
    private static bool ValidateRemoteServer() => ValidateSelection(RemoteMenu, true);

    private static bool ValidateSelection(string menu, bool remote)
    {
        Menu.SetChecked(menu, EditorPrefs.GetBool(LaunchConfig.EditorRemoteServerPreference, false) == remote);
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void SelectServer(bool remote)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorPrefs.SetBool(LaunchConfig.EditorRemoteServerPreference, remote);
        Debug.Log($"编辑器服务器：{new LaunchConfig().ServerUri}。正式包固定连接：{LaunchConfig.RemoteServerUri}");
    }
}
