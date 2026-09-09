using UnityEngine;
using UnityEngine.UI;

public sealed class Start : UIScript
{
    private Button _startButton;

    public override string GetPath()
    {
        return "prefabs/view/Start";
    }

    public override void OnOpen()
    {
        Transform buttonTransform = FindRequiredTransform("root/group_foreground/btn_start_game");
        _startButton = buttonTransform.GetComponent<Button>();
        if (_startButton == null)
        {
            throw new System.InvalidOperationException("Start view cannot find Button on btn_start_game.");
        }

        _startButton.onClick.AddListener(OnClickStartGame);
    }

    public override void OnClose()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnClickStartGame);
            _startButton = null;
        }
    }

    private void OnClickStartGame()
    {
        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("loading"));
    }

    private Transform FindRequiredTransform(string path)
    {
        Transform targetTransform = FindViewTransform(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Start view cannot find node: {path}");
        }

        return targetTransform;
    }
}
