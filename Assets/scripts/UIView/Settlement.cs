using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Settlement : UIScript
{
    private GameSession _session;
    private Button _backButton;

    public override string GetPath()
    {
        return "prefabs/view/Settlement";
    }

    public override void OnOpen()
    {
        _session = (GameViewArg as GameViewArg)?.GameSession;
        if (_session == null) throw new InvalidOperationException("Settlement requires a game session.");
        _backButton = FindViewTransform("root/content/btn_back_lobby").GetComponent<Button>();
        _backButton.onClick.AddListener(ReturnToLobby);
        _session.MatchStateUpdated += Render;
        Render();
    }

    public override void OnClose()
    {
        if (_session != null) _session.MatchStateUpdated -= Render;
        if (_backButton != null) _backButton.onClick.RemoveListener(ReturnToLobby);
        _session = null;
        _backButton = null;
    }

    private void ReturnToLobby()
    {
        GameApp.Current?.OnlineGameController?.LeaveGame();
    }

    private void Render()
    {
        // Seat order is presentation order until ranking rules are agreed.
        var players = (_session.GameStatePlayers ?? Array.Empty<NetworkGameStatePlayerPayload>())
            .Where(player => player != null).OrderBy(player => player.seat_index).ToArray();
        Transform rows = FindViewTransform("root/content/image_frame/rows");
        for (int i = 0; i < rows.childCount; i++)
        {
            Transform row = rows.GetChild(i);
            row.gameObject.SetActive(i < players.Length);
            if (i >= players.Length) continue;
            var player = players[i];
            Set(row, "player_name", player.name);
            Set(row, "unlucky", player.unlucky_count.ToString());
            Set(row, "lucky", player.lucky_count.ToString());
            Set(row, "small_three", player.escaped_three_count.ToString());
            Set(row, "rank", "—");
            Set(row, "score", "—");
            Set(row, "title", "—");
        }
    }

    private static void Set(Transform row, string field, string value)
    {
        row.Find("txt_" + field).GetComponent<TMP_Text>().text = string.IsNullOrEmpty(value) ? "—" : value;
    }
}
