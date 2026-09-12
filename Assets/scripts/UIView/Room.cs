using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class Room : UIScript
{
    private const int SeatCount = 4;

    private readonly List<SeatBinding> _seatBindings = new List<SeatBinding>();

    private ChatPanel _chatPanel;
    private TMP_Text _roomNameText;
    private TMP_Text _roomIdText;
    private Button _leaveButton;
    private Button _actionButton;
    private Image _actionButtonImage;
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
        _roomNameText = FindRequiredText("root/panel_room_info/txt_room_name");
        _roomIdText = FindRequiredText("root/panel_room_info/id_badge/txt_room_ID");
        _leaveButton = FindRequiredButton("root/btn_leave");
        _actionButton = FindRequiredButton("root/btn_action");
        _actionButtonImage = _actionButton.GetComponent<Image>();
        _chatPanel = FindRequiredTransform("root/group_chat").GetComponent<ChatPanel>();
        _chatPanel.Initialize(message => GameApp.Current?.OnlineGameController?.SendRoomChat(message));

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

        _roomNameText.text = SingleLine(roomModel.Name);
        if (_roomIdText != null)
        {
            _roomIdText.text = SingleLine(roomModel.RoomId);
        }

        _actionButton.gameObject.SetActive(true);
        if (isLocalOwner)
        {
            _actionButton.interactable = roomModel.CanLocalStartGame(localPlayerId);
            SetActionButtonSprite(_characterArtLibrary?.RoomActionStartSprite);
        }
        else
        {
            _actionButton.interactable = roomModel.CanLocalReady(localPlayerId);
            bool isReady = localPlayer != null && localPlayer.IsReady;
            SetActionButtonSprite(
                isReady
                    ? _characterArtLibrary?.RoomActionUnreadySprite
                    : _characterArtLibrary?.RoomActionReadySprite);
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
        seatBinding.CharacterImage.color = seatBinding.CharacterImage.sprite != null ? Color.white : Color.clear;
        if (seatBinding.CharacterImage.sprite != null) seatBinding.CharacterImage.SetNativeSize();
        ApplyCharacterPresentation(
            seatBinding.CharacterImage,
            _characterArtLibrary?.GetRoomOffset(characterId) ?? Vector2.zero,
            _characterArtLibrary?.GetRoomScale(characterId) ?? 1f);
        Sprite shadow = _characterArtLibrary?.GetRoomShadowSprite(characterId);
        seatBinding.ShadowImage.sprite = shadow;
        seatBinding.ShadowImage.gameObject.SetActive(shadow != null && seatBinding.CharacterImage.sprite != null);
        if (shadow != null)
        {
            seatBinding.ShadowImage.SetNativeSize();
            ApplyCharacterPresentation(seatBinding.ShadowImage,
                (_characterArtLibrary?.GetRoomOffset(characterId) ?? Vector2.zero)
                + (_characterArtLibrary?.GetRoomShadowOffset(characterId) ?? Vector2.zero),
                _characterArtLibrary?.GetRoomScale(characterId) ?? 1f);
        }

        RectTransform portrait = seatBinding.CharacterImage.rectTransform;
        seatBinding.NameText.rectTransform.anchoredPosition = new Vector2(
            portrait.anchoredPosition.x,
            portrait.anchoredPosition.y + portrait.rect.height * portrait.localScale.y + 22f);
        seatBinding.NameText.text = SingleLine(GetDisplayName(player));
    }

    private static string SingleLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

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
            _actionButtonImage.SetNativeSize();
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

    private void OnRoomChatReceived(NetworkRoomChatPayload payload)
    {
        if (payload == null || string.IsNullOrEmpty(payload.message))
        {
            return;
        }

        _chatPanel.AddMessage(payload.name, payload.message);
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

        foreach (SeatBinding seatBinding in _seatBindings)
        {
            seatBinding.PlusButton.onClick.RemoveListener(seatBinding.PlusClickAction);
        }

        _chatPanel?.Close();
    }

    private void ClearReferences()
    {
        _seatBindings.Clear();
        _chatPanel = null;
        _roomNameText = null;
        _roomIdText = null;
        _leaveButton = null;
        _actionButton = null;
        _actionButtonImage = null;
        _characterArtLibrary = null;
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
