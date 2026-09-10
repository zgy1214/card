using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Loading : UIScript
{
    private const string FailureDialogPrefabPath = "prefabs/template/connection_failure_dialog";

    private Slider _progressSlider;
    private GameObject _failureDialog;
    private Button _quitButton;
    private TMP_Text _failureMessageText;
    private float _displayedProgress;
    private float _elapsedSeconds;
    private float _minimumDisplaySeconds;

    public override string GetPath()
    {
        return "prefabs/view/Loading";
    }

    public override void OnOpen()
    {
        _progressSlider = FindRequiredTransform("root/group_progress").GetComponent<Slider>();
        if (_progressSlider == null)
        {
            throw new System.InvalidOperationException("Loading view cannot find Slider on root/group_progress.");
        }

        _progressSlider.minValue = 0f;
        _progressSlider.maxValue = 1f;
        _progressSlider.wholeNumbers = false;
        _progressSlider.SetValueWithoutNotify(0f);
        _elapsedSeconds = 0f;
        _minimumDisplaySeconds = Random.Range(1.2f, 1.6f);
        _displayedProgress = 0f;
    }

    public override void OnTick(float deltaTime)
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller == null)
        {
            return;
        }

        _elapsedSeconds += deltaTime;

        // Keep the bar moving during fast connections while reserving the final step for success.
        float timeProgress = Mathf.Clamp01(_elapsedSeconds / _minimumDisplaySeconds) * 0.92f;
        float targetProgress = Mathf.Max(timeProgress, Mathf.Clamp01(controller.StartupProgress));
        bool canFinish = controller.StartupSucceeded && _elapsedSeconds >= _minimumDisplaySeconds;
        if (!canFinish)
        {
            targetProgress = Mathf.Min(targetProgress, 0.92f);
        }

        _displayedProgress = Mathf.MoveTowards(
            _displayedProgress,
            Mathf.Max(_displayedProgress, targetProgress),
            deltaTime * 1.2f);
        _progressSlider.SetValueWithoutNotify(_displayedProgress);

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
        _progressSlider = null;
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
