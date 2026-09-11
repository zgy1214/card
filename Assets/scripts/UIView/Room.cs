using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class Room : UIScript
{
    private const int SeatCount = 4;
    private const int MaxChatLineCount = 30;

    private readonly List<SeatBinding> _seatBindings = new List<SeatBinding>();
    private readonly List<GameObject> _chatLineObjects = new List<GameObject>();

    private TMP_Text _roomNameText;
    private TMP_Text _roomIdText;
    private TMP_Text _roomStatusText;
    private Button _leaveButton;
    private Button _actionButton;
    private Image _actionButtonImage;
    private TMP_InputField _chatInput;
    private Button _sendChatButton;
    private ScrollRect _chatScrollRect;
    private Transform _chatContentTransform;
    private TMP_FontAsset _chatFontAsset;
    private CharacterArtLibrary _characterArtLibrary;

    public override string GetPath()
    {
        return "prefabs/view/Room";
    }

    public override void OnOpen()
    {
        BindNodes();
        SubscribeEvents();
        RenderRoomState();
    }

    public override void OnClose()
    {
        UnsubscribeEvents();
        RemoveButtonListeners();
        ClearReferences();
    }

    private void BindNodes()
    {
        _characterArtLibrary = ViewGameObject.GetComponent<CharacterArtLibrary>();
        _roomNameText = FindRequiredText("root/txt_room_name");
        _roomIdText = FindOptionalText("root/txt_room_ID");
        _roomStatusText = FindOptionalText("root/txt_room_status");
        _leaveButton = FindRequiredButton("root/btn_leave");
        _actionButton = FindRequiredButton("root/btn_action");
        _actionButtonImage = _actionButton.GetComponent<Image>();
        _chatFontAsset = FindOptionalText("root/group_chat/group_chat_input/input_chat/text_area/txt_input")?.font;
        _chatScrollRect = FindRequiredScrollRect("root/group_chat/scroll_chat");
        _chatInput = FindRequiredInput("root/group_chat/group_chat_input/input_chat");
        _sendChatButton = FindRequiredButton("root/group_chat/group_chat_input/btn_send_chat");
        _chatContentTransform = FindRequiredTransform("root/group_chat/scroll_chat/viewport/content");

        for (int seatIndex = 0; seatIndex < SeatCount; seatIndex += 1)
        {
            Transform slotTransform = FindRequiredTransform($"root/slot_player{seatIndex}");
            Image plusImage = FindRequiredImage(slotTransform, "img_plus");
            Button plusButton = plusImage.GetComponent<Button>() ?? plusImage.gameObject.AddComponent<Button>();
            plusButton.targetGraphic = plusImage;
            int capturedSeatIndex = seatIndex;
            UnityAction plusClickAction = () => OnClickAddAI(capturedSeatIndex);
            plusButton.onClick.AddListener(plusClickAction);

            _seatBindings.Add(new SeatBinding(
                seatIndex,
                FindRequiredImage(slotTransform, "img_character"),
                FindRequiredImage(slotTransform, "img_shadow"),
                FindRequiredText(slotTransform, "txt_name"),
                plusImage.gameObject,
                plusButton,
                plusClickAction));
        }

        _leaveButton.onClick.AddListener(OnClickLeave);
        _actionButton.onClick.AddListener(OnClickAction);
        _sendChatButton.onClick.AddListener(OnClickSendChat);
        _chatInput.onSubmit.AddListener(OnSubmitChat);
    }

    private void SubscribeEvents()
    {
        RoomModel roomModel = GameApp.Current?.OnlineGameController?.RoomModel;
        if (roomModel == null)
        {
            return;
        }

        roomModel.RoomStateUpdated += RenderRoomState;
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.RoomChatReceived += OnRoomChatReceived;
        }
    }

    private void UnsubscribeEvents()
    {
        RoomModel roomModel = GameApp.Current?.OnlineGameController?.RoomModel;
        if (roomModel != null)
        {
            roomModel.RoomStateUpdated -= RenderRoomState;
        }

        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.RoomChatReceived -= OnRoomChatReceived;
        }
    }

    private void RenderRoomState()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        RoomModel roomModel = controller?.RoomModel;
        if (roomModel == null || !roomModel.HasRoom)
        {
            return;
        }

        string localPlayerId = controller.LocalPlayerProfile.PlayerId;
        bool isLocalOwner = roomModel.IsLocalOwner(localPlayerId);
        RoomPlayerSnapshot localPlayer = roomModel.GetLocalPlayer(localPlayerId);

        _roomNameText.text = roomModel.Name;
        if (_roomIdText != null)
        {
            _roomIdText.text = $"房间号 {roomModel.RoomId}";
        }
        if (_roomStatusText != null)
        {
            _roomStatusText.text = BuildRoomStatusText(roomModel, localPlayer, isLocalOwner);
        }

        _actionButton.gameObject.SetActive(true);
        if (isLocalOwner)
        {
            _actionButton.interactable = roomModel.CanLocalStartGame(localPlayerId);
            SetActionButtonSprite(_characterArtLibrary?.RoomActionStartSprite);
            SetButtonLabel(_actionButton, "开始游戏");
        }
        else
        {
            _actionButton.interactable = roomModel.CanLocalReady(localPlayerId);
            bool isReady = localPlayer != null && localPlayer.IsReady;
            SetActionButtonSprite(
                isReady
                    ? _characterArtLibrary?.RoomActionUnreadySprite
                    : _characterArtLibrary?.RoomActionReadySprite);
            SetButtonLabel(_actionButton, isReady ? "取消准备" : "准备");
        }

        int nextEmptySeatIndex = FindNextEmptySeatIndex(roomModel);
        foreach (SeatBinding seatBinding in _seatBindings)
        {
            RenderSeat(roomModel, seatBinding, roomModel.CanLocalAddAI(localPlayerId), nextEmptySeatIndex);
        }
    }

    private int FindNextEmptySeatIndex(RoomModel roomModel)
    {
        for (int seatIndex = 0; seatIndex < SeatCount; seatIndex += 1)
        {
            if (roomModel.GetPlayerBySeat(seatIndex) == null)
            {
                return seatIndex;
            }
        }

        return -1;
    }

    private void RenderSeat(
        RoomModel roomModel,
        SeatBinding seatBinding,
        bool canLocalAddAI,
        int nextEmptySeatIndex)
    {
        RoomPlayerSnapshot player = roomModel.GetPlayerBySeat(seatBinding.SeatIndex);
        if (player == null)
        {
            SetSeatTextVisible(seatBinding, false);
            bool isNextEmptySeat = seatBinding.SeatIndex == nextEmptySeatIndex;
            seatBinding.PlusObject.SetActive(isNextEmptySeat);
            seatBinding.PlusButton.interactable = canLocalAddAI && isNextEmptySeat;
            seatBinding.ShadowImage.gameObject.SetActive(false);

            seatBinding.CharacterImage.sprite = null;
            seatBinding.CharacterImage.color = new Color(1f, 1f, 1f, 0f);
            ApplyCharacterPresentation(seatBinding.CharacterImage, Vector2.zero, 1f);
            return;
        }

        SetSeatTextVisible(seatBinding, true);
        seatBinding.PlusObject.SetActive(false);
        seatBinding.PlusButton.interactable = false;
        seatBinding.ShadowImage.gameObject.SetActive(true);

        string characterId = GetDisplayCharacterId(player);
        seatBinding.CharacterImage.sprite = _characterArtLibrary?.GetSprite(characterId);
        seatBinding.CharacterImage.preserveAspect = true;
        seatBinding.CharacterImage.color = Color.white;
        ApplyCharacterPresentation(
            seatBinding.CharacterImage,
            _characterArtLibrary?.GetRoomOffset(characterId) ?? Vector2.zero,
            _characterArtLibrary?.GetRoomScale(characterId) ?? 1f);
        seatBinding.NameText.text = GetDisplayName(player);
    }

    private void ApplyCharacterPresentation(Image characterImage, Vector2 offset, float scale)
    {
        RectTransform rectTransform = characterImage.rectTransform;
        rectTransform.anchoredPosition = offset;
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetActionButtonSprite(Sprite sprite)
    {
        if (_actionButtonImage != null && sprite != null)
        {
            _actionButtonImage.sprite = sprite;
        }
    }

    private string GetDisplayCharacterId(RoomPlayerSnapshot player)
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (player != null
            && profile != null
            && string.Equals(player.PlayerId, profile.PlayerId, StringComparison.Ordinal))
        {
            return profile.CharacterId;
        }

        return player?.CharacterId;
    }

    private string GetDisplayName(RoomPlayerSnapshot player)
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (player != null
            && profile != null
            && string.Equals(player.PlayerId, profile.PlayerId, StringComparison.Ordinal))
        {
            return profile.Name;
        }

        return player?.Name;
    }

    private string BuildRoomStatusText(RoomModel roomModel, RoomPlayerSnapshot localPlayer, bool isLocalOwner)
    {
        if (!roomModel.IsFull())
        {
            return string.Empty;
        }

        if (isLocalOwner)
        {
            return "人数已满，可以在全部真人准备后开始";
        }

        return localPlayer != null && localPlayer.IsReady ? "已准备，等待房主开始" : "人数已满，请准备";
    }

    private void SetSeatTextVisible(SeatBinding seatBinding, bool isVisible)
    {
        seatBinding.NameText.gameObject.SetActive(isVisible);
    }

    private void OnClickAddAI(int seatIndex)
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        RoomModel roomModel = controller?.RoomModel;
        if (controller == null || roomModel == null || !roomModel.HasRoom)
        {
            return;
        }

        string localPlayerId = controller.LocalPlayerProfile.PlayerId;
        if (!roomModel.CanLocalAddAI(localPlayerId)
            || FindNextEmptySeatIndex(roomModel) != seatIndex)
        {
            return;
        }

        controller.AddAI();
    }

    private void OnClickLeave()
    {
        GameApp.Current?.OnlineGameController?.LeaveRoom();
    }

    private void OnClickAction()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        RoomModel roomModel = controller?.RoomModel;
        if (controller == null || roomModel == null || !roomModel.HasRoom)
        {
            return;
        }

        string localPlayerId = controller.LocalPlayerProfile.PlayerId;
        if (roomModel.IsLocalOwner(localPlayerId))
        {
            if (roomModel.CanLocalStartGame(localPlayerId))
            {
                controller.StartRoomGame();
            }

            return;
        }

        RoomPlayerSnapshot localPlayer = roomModel.GetLocalPlayer(localPlayerId);
        if (localPlayer == null || !roomModel.CanLocalReady(localPlayerId))
        {
            return;
        }

        controller.SetReady(!localPlayer.IsReady);
    }

    private void OnClickSendChat()
    {
        SendChatFromInput();
    }

    private void OnSubmitChat(string _)
    {
        SendChatFromInput();
    }

    private void SendChatFromInput()
    {
        if (_chatInput == null)
        {
            return;
        }

        string message = string.IsNullOrEmpty(_chatInput.text) ? string.Empty : _chatInput.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            _chatInput.text = string.Empty;
            return;
        }

        GameApp.Current?.OnlineGameController?.SendRoomChat(message);
        _chatInput.text = string.Empty;
        _chatInput.ActivateInputField();
    }

    private void OnRoomChatReceived(NetworkRoomChatPayload payload)
    {
        if (payload == null || string.IsNullOrEmpty(payload.message))
        {
            return;
        }

        AddChatLine($"{payload.name}: {payload.message}");
    }

    private void AddChatLine(string message)
    {
        if (_chatContentTransform == null)
        {
            return;
        }

        GameObject chatLineObject = new GameObject("chat_line", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        chatLineObject.transform.SetParent(_chatContentTransform, false);

        TMP_Text text = chatLineObject.GetComponent<TMP_Text>();
        text.text = message;
        text.fontSize = 20;
        text.color = new Color32(255, 238, 202, 255);
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.Normal;
        if (_chatFontAsset != null)
        {
            text.font = _chatFontAsset;
        }

        ContentSizeFitter fitter = chatLineObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        LayoutElement layoutElement = chatLineObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 30;
        layoutElement.flexibleWidth = 1;
        _chatLineObjects.Add(chatLineObject);

        while (_chatLineObjects.Count > MaxChatLineCount)
        {
            GameObject firstLine = _chatLineObjects[0];
            _chatLineObjects.RemoveAt(0);
            if (firstLine != null)
            {
                UnityEngine.Object.Destroy(firstLine);
            }
        }

        Canvas.ForceUpdateCanvases();
        if (_chatScrollRect != null)
        {
            _chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void RemoveButtonListeners()
    {
        if (_leaveButton != null)
        {
            _leaveButton.onClick.RemoveListener(OnClickLeave);
        }

        if (_actionButton != null)
        {
            _actionButton.onClick.RemoveListener(OnClickAction);
        }

        if (_sendChatButton != null)
        {
            _sendChatButton.onClick.RemoveListener(OnClickSendChat);
        }

        if (_chatInput != null)
        {
            _chatInput.onSubmit.RemoveListener(OnSubmitChat);
        }

        foreach (SeatBinding seatBinding in _seatBindings)
        {
            seatBinding.PlusButton.onClick.RemoveListener(seatBinding.PlusClickAction);
        }

        ClearChatLines();
    }

    private void ClearReferences()
    {
        _seatBindings.Clear();
        _roomNameText = null;
        _roomIdText = null;
        _roomStatusText = null;
        _leaveButton = null;
        _actionButton = null;
        _actionButtonImage = null;
        _chatInput = null;
        _sendChatButton = null;
        _chatScrollRect = null;
        _chatContentTransform = null;
        _chatFontAsset = null;
        _characterArtLibrary = null;
    }

    private void ClearChatLines()
    {
        foreach (GameObject chatLineObject in _chatLineObjects)
        {
            if (chatLineObject != null)
            {
                UnityEngine.Object.Destroy(chatLineObject);
            }
        }

        _chatLineObjects.Clear();
    }

    private void SetButtonLabel(Button button, string label)
    {
        TMP_Text labelText = button.transform.Find("txt_label")?.GetComponent<TMP_Text>();
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    private TMP_Text FindOptionalText(string path)
    {
        return FindOptionalText(ViewTransform, path);
    }

    private TMP_Text FindOptionalText(Transform rootTransform, string path)
    {
        Transform targetTransform = FindOptionalTransform(rootTransform, path);
        return targetTransform == null ? null : targetTransform.GetComponent<TMP_Text>();
    }

    private TMP_Text FindRequiredText(string path)
    {
        return FindRequiredText(ViewTransform, path);
    }

    private TMP_Text FindRequiredText(Transform rootTransform, string path)
    {
        Transform targetTransform = FindRequiredTransform(rootTransform, path);
        TMP_Text text = targetTransform.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find TMP_Text at path: {path}");
        }

        return text;
    }

    private Button FindRequiredButton(string path)
    {
        return FindRequiredButton(ViewTransform, path);
    }

    private Button FindRequiredButton(Transform rootTransform, string path)
    {
        Transform targetTransform = FindRequiredTransform(rootTransform, path);
        Button button = targetTransform.GetComponent<Button>();
        if (button == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find Button at path: {path}");
        }

        return button;
    }

    private ScrollRect FindRequiredScrollRect(string path)
    {
        Transform targetTransform = FindRequiredTransform(ViewTransform, path);
        ScrollRect scrollRect = targetTransform.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find ScrollRect at path: {path}");
        }

        return scrollRect;
    }

    private TMP_InputField FindRequiredInput(string path)
    {
        Transform targetTransform = FindRequiredTransform(ViewTransform, path);
        TMP_InputField input = targetTransform.GetComponent<TMP_InputField>();
        if (input == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find TMP_InputField at path: {path}");
        }

        return input;
    }

    private Image FindRequiredImage(Transform rootTransform, string path)
    {
        Transform targetTransform = FindRequiredTransform(rootTransform, path);
        Image image = targetTransform.GetComponent<Image>();
        if (image == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find Image at path: {path}");
        }

        return image;
    }

    private Transform FindRequiredTransform(string path)
    {
        return FindRequiredTransform(ViewTransform, path);
    }

    private Transform FindRequiredTransform(Transform rootTransform, string path)
    {
        Transform targetTransform = FindOptionalTransform(rootTransform, path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find node: {path}");
        }

        return targetTransform;
    }

    private Transform FindOptionalTransform(Transform rootTransform, string path)
    {
        return rootTransform == null ? null : rootTransform.Find(path);
    }

    private sealed class SeatBinding
    {
        public int SeatIndex { get; }
        public Image CharacterImage { get; }
        public Image ShadowImage { get; }
        public TMP_Text NameText { get; }
        public GameObject PlusObject { get; }
        public Button PlusButton { get; }
        public UnityAction PlusClickAction { get; }

        public SeatBinding(
            int seatIndex,
            Image characterImage,
            Image shadowImage,
            TMP_Text nameText,
            GameObject plusObject,
            Button plusButton,
            UnityAction plusClickAction)
        {
            SeatIndex = seatIndex;
            CharacterImage = characterImage;
            ShadowImage = shadowImage;
            NameText = nameText;
            PlusObject = plusObject;
            PlusButton = plusButton;
            PlusClickAction = plusClickAction;
        }
    }
}
