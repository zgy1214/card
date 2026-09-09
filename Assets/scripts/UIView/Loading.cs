using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Loading : UIScript
{
    private const string FailureDialogPrefabPath = "prefabs/template/connection_failure_dialog";

    private RectTransform _progressFill;
    private RectTransform _progressIndicator;
    private GameObject _failureDialog;
    private Button _quitButton;
    private TMP_Text _failureMessageText;
    private float _displayedProgress;

    public override string GetPath()
    {
        return "prefabs/view/Loading";
    }

    public override void OnOpen()
    {
        _progressFill = FindRequiredRectTransform("root/group_progress/image_progress_fill");
        _progressIndicator = FindRequiredRectTransform("root/group_progress/image_progress_indicator");
        _displayedProgress = 0f;
        ApplyProgress(0f);
    }

    public override void OnTick(float deltaTime)
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller == null)
        {
            return;
        }

        float targetProgress = Mathf.Clamp01(controller.StartupProgress);
        _displayedProgress = Mathf.MoveTowards(
            _displayedProgress,
            Mathf.Max(_displayedProgress, targetProgress),
            Mathf.Max(0.35f, deltaTime * 0.9f));
        ApplyProgress(_displayedProgress);

        if (controller.StartupFailed)
        {
            ShowFailureDialog(controller.StartupFailureMessage);
        }
    }

    public override void OnClose()
    {
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnClickQuitGame);
        }

        _failureDialog = null;
        _quitButton = null;
        _failureMessageText = null;
        _progressFill = null;
        _progressIndicator = null;
    }

    private void ApplyProgress(float progress)
    {
        if (_progressFill != null)
        {
            Vector2 anchorMax = _progressFill.anchorMax;
            anchorMax.x = progress;
            _progressFill.anchorMax = anchorMax;
        }

        if (_progressIndicator != null)
        {
            Vector2 anchorMin = _progressIndicator.anchorMin;
            Vector2 anchorMax = _progressIndicator.anchorMax;
            anchorMin.x = progress;
            anchorMax.x = progress;
            _progressIndicator.anchorMin = anchorMin;
            _progressIndicator.anchorMax = anchorMax;
        }
    }

    private void ShowFailureDialog(string message)
    {
        if (_failureDialog == null)
        {
            _failureDialog = OpenGameObject(ViewTransform, FailureDialogPrefabPath, "connection_failure_dialog");
            _quitButton = FindRequiredButton(_failureDialog.transform, "panel_dialog/btn_quit_game");
            _failureMessageText = FindRequiredText(_failureDialog.transform, "panel_dialog/txt_message");
            _quitButton.onClick.AddListener(OnClickQuitGame);
        }

        if (_failureMessageText != null && !string.IsNullOrEmpty(message))
        {
            _failureMessageText.text = message;
        }
    }

    private void OnClickQuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested from startup connection failure dialog.");
#else
        Application.Quit();
#endif
    }

    private RectTransform FindRequiredRectTransform(string path)
    {
        Transform targetTransform = FindRequiredTransform(path);
        return targetTransform as RectTransform;
    }

    private Button FindRequiredButton(Transform rootTransform, string path)
    {
        Transform targetTransform = rootTransform.Find(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Loading view cannot find node: {path}");
        }

        Button button = targetTransform.GetComponent<Button>();
        if (button == null)
        {
            throw new System.InvalidOperationException($"Loading view cannot find Button at path: {path}");
        }

        return button;
    }

    private TMP_Text FindRequiredText(Transform rootTransform, string path)
    {
        Transform targetTransform = rootTransform.Find(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Loading view cannot find node: {path}");
        }

        TMP_Text text = targetTransform.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new System.InvalidOperationException($"Loading view cannot find TMP_Text at path: {path}");
        }

        return text;
    }

    private Transform FindRequiredTransform(string path)
    {
        Transform targetTransform = FindViewTransform(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Loading view cannot find node: {path}");
        }

        return targetTransform;
    }
}
