using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChatPanel : MonoBehaviour
{
    private const int MessageLimit = 3;
    private readonly List<TMP_Text> _messages = new List<TMP_Text>();
    private TMP_InputField _input;
    private Button _sendButton;
    private RectTransform _messageArea;
    private Action<string> _send;
    private CanvasGroup _placeholderGroup;

    public void Initialize(Action<string> send)
    {
        Close();
        _messageArea = (RectTransform)transform.Find("messages");
        _input = transform.Find("group_chat_input/input_chat").GetComponent<TMP_InputField>();
        _sendButton = transform.Find("group_chat_input/btn_send_chat").GetComponent<Button>();
        if (_input.placeholder != null)
        {
            _placeholderGroup = _input.placeholder.GetComponent<CanvasGroup>();
            if (_placeholderGroup == null)
                _placeholderGroup = _input.placeholder.gameObject.AddComponent<CanvasGroup>();
        }
        _send = send;
        _sendButton.onClick.AddListener(Submit);
        _input.onSubmit.AddListener(OnSubmit);
        _input.onSelect.AddListener(OnFocus);
        _input.onDeselect.AddListener(OnBlur);
        SetPlaceholderVisible(!_input.isFocused);
    }

    public void AddMessage(string sender, string message)
    {
        if (_messageArea == null || string.IsNullOrWhiteSpace(message)) return;
        var line = new GameObject("chat_message", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        line.transform.SetParent(_messageArea, false);
        TMP_Text text = line.GetComponent<TMP_Text>();
        text.font = _input.textComponent.font;
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.richText = false;
        text.raycastTarget = false;
        text.text = $"{sender}: {message}";
        _messages.Add(text);
        if (_messages.Count > MessageLimit)
        {
            TMP_Text oldest = _messages[0];
            _messages.RemoveAt(0);
            oldest.gameObject.SetActive(false);
            Destroy(oldest.gameObject);
        }
        RefreshMessages();
    }

    private void OnRectTransformDimensionsChange() => RefreshMessages();

    private void RefreshMessages()
    {
        if (_messageArea == null) return;
        float maxHeight = Mathf.Max(36f, (_messageArea.rect.height - 16f) / MessageLimit);
        for (int i = 0; i < _messages.Count; i++)
        {
            TMP_Text text = _messages[i];
            int age = _messages.Count - 1 - i;
            text.color = new Color(1f, 1f, 1f, age == 0 ? 1f : age == 1 ? 0.65f : 0.35f);
            text.GetComponent<LayoutElement>().preferredHeight = Mathf.Clamp(
                text.GetPreferredValues(text.text, _messageArea.rect.width, Mathf.Infinity).y, 36f, maxHeight);
        }
        LayoutRebuilder.MarkLayoutForRebuild(_messageArea);
    }

    private void OnSubmit(string _) => Submit();

    private void OnFocus(string _) => SetPlaceholderVisible(false);
    private void OnBlur(string _) => SetPlaceholderVisible(true);

    private void SetPlaceholderVisible(bool visible)
    {
        // TMP refreshes Graphic.enabled when text changes; a separate alpha gate
        // keeps its empty-field refresh from revealing the focused placeholder.
        if (_placeholderGroup != null) _placeholderGroup.alpha = visible ? 1f : 0f;
    }

    private void Submit()
    {
        string message = _input.text.Trim();
        if (string.IsNullOrEmpty(message)) return;
        _send?.Invoke(message);
        _input.text = string.Empty;
        _input.ActivateInputField();
    }

    public void Close()
    {
        if (_sendButton != null) _sendButton.onClick.RemoveListener(Submit);
        if (_input != null)
        {
            _input.onSubmit.RemoveListener(OnSubmit);
            _input.onSelect.RemoveListener(OnFocus);
            _input.onDeselect.RemoveListener(OnBlur);
            SetPlaceholderVisible(true);
        }
        _send = null;
        foreach (TMP_Text text in _messages)
        {
            if (text == null) continue;
            text.gameObject.SetActive(false);
            Destroy(text.gameObject);
        }
        _messages.Clear();
    }
}
