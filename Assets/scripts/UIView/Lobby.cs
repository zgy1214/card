using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Lobby : UIScript
{
    private const string RoomItemPrefabPath = "prefabs/template/room_item";
    private const string BlockingOverlayPrefabPath = "prefabs/template/blocking_overlay";
    private const int CharacterOptionCount = 2;

    private readonly List<GameObject> _roomItemGameObjects = new List<GameObject>();
    private readonly List<CharacterOptionBinding> _characterOptionBindings = new List<CharacterOptionBinding>();

    private TMP_Text _playerNameText;
    private TMP_Text _playerIdText;
    private TMP_Text _connectionStatusText;
    private TMP_Text _matchmakingStatusText;
    private TMP_Text _emptyHintText;
    private TMP_InputField _roomIdInput;
    private TMP_InputField _roomNameInput;
    private Transform _roomListContentTransform;
    private Button _refreshButton;
    private Button _matchmakingButton;
    private Button _joinRoomButton;
    private Button _createRoomButton;
    private Button _profileButton;
    private Image _selectedCharacterImage;
    private TMP_Text _selectedCharacterText;
    private CharacterArtLibrary _characterArtLibrary;
    private GameObject _blockingOverlayObject;
    private TMP_Text _overlayTitleText;
    private TMP_Text _overlayMessageText;
    private Button _overlayCancelButton;

    public override string GetPath()
    {
        return "prefabs/view/Lobby";
    }

    public override void OnOpen()
    {
        BindNodes();
        SubscribeEvents();
        RenderProfile();
        RenderConnectionState();
        RenderRoomList();
        RenderMatchmakingState();
        GameApp.Current?.OnlineGameController?.RequestRoomList();
    }

    public override void OnClose()
    {
        UnsubscribeEvents();
        ClearRoomItems();
        HideBlockingOverlay();
        RemoveButtonListeners();
        ClearReferences();
    }

    private void BindNodes()
    {
        _characterArtLibrary = ViewGameObject.GetComponent<CharacterArtLibrary>();
        _playerNameText = FindRequiredText("root/panel_actions/panel_player_info/txt_player_name");
        _playerIdText = FindRequiredText("root/panel_actions/panel_player_info/txt_player_id");
        _connectionStatusText = FindRequiredText("root/panel_actions/panel_player_info/txt_connection_status");
        _matchmakingStatusText = FindRequiredText("root/panel_actions/panel_matchmaking/txt_matchmaking_status");
        _emptyHintText = FindRequiredText("root/panel_room_list/txt_empty_hint");
        _selectedCharacterImage = FindRequiredImage("root/panel_profile/panel_character_showcase/img_character");
        _selectedCharacterText = FindRequiredText("root/panel_profile/panel_character_showcase/txt_character_name");
        _roomIdInput = FindRequiredInput("root/panel_actions/panel_join_room/panel_join_row/input_room_id");
        _roomNameInput = FindRequiredInput("root/panel_actions/panel_create_room/input_room_name");
        _roomListContentTransform = FindRequiredTransform("root/panel_room_list/scroll_room_list/viewport/content");
        _refreshButton = FindRequiredButton("root/panel_room_list/panel_room_list_header/btn_refresh");
        _matchmakingButton = FindRequiredButton("root/panel_actions/panel_matchmaking/btn_matchmaking");
        _joinRoomButton = FindRequiredButton("root/panel_actions/panel_join_room/panel_join_row/btn_join_room");
        _createRoomButton = FindRequiredButton("root/panel_actions/panel_create_room/btn_create_room");
        _profileButton = FindRequiredButton("root/panel_actions/panel_player_info/btn_profile");

        _refreshButton.onClick.AddListener(OnClickRefresh);
        _matchmakingButton.onClick.AddListener(OnClickMatchmaking);
        _joinRoomButton.onClick.AddListener(OnClickJoinRoom);
        _createRoomButton.onClick.AddListener(OnClickCreateRoom);
        _profileButton.onClick.AddListener(OnClickProfile);

        for (int index = 0; index < CharacterOptionCount; index += 1)
        {
            string characterId = $"character_{index + 1}";
            Transform optionTransform = FindRequiredTransform($"root/panel_profile/panel_character_picker/btn_character{index}");
            Button button = optionTransform.GetComponent<Button>();
            Image portraitImage = optionTransform.Find("img_portrait")?.GetComponent<Image>();
            TMP_Text labelText = optionTransform.Find("txt_label")?.GetComponent<TMP_Text>();
            if (button == null || portraitImage == null || labelText == null)
            {
                throw new System.InvalidOperationException($"Lobby character option is incomplete: {optionTransform.name}");
            }

            _characterOptionBindings.Add(new CharacterOptionBinding(characterId, button, portraitImage, labelText));
            button.onClick.AddListener(() => OnClickCharacter(characterId));
        }
    }

    private void SubscribeEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller == null)
        {
            return;
        }

        controller.LobbyModel.RoomListUpdated += RenderRoomList;
        controller.LobbyModel.MatchmakingStateUpdated += RenderMatchmakingState;
        controller.LobbyModel.SessionStateUpdated += RenderConnectionState;
        controller.ErrorOccurred += OnErrorOccurred;
    }

    private void UnsubscribeEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller == null)
        {
            return;
        }

        controller.LobbyModel.RoomListUpdated -= RenderRoomList;
        controller.LobbyModel.MatchmakingStateUpdated -= RenderMatchmakingState;
        controller.LobbyModel.SessionStateUpdated -= RenderConnectionState;
        controller.ErrorOccurred -= OnErrorOccurred;
    }

    private void RenderProfile()
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (profile == null)
        {
            return;
        }

        _playerNameText.text = profile.Name;
        _playerIdText.text = $"ID: {ShortenPlayerId(profile.PlayerId)}";
        RenderCharacterSelection(profile.CharacterId);
    }

    private void RenderCharacterSelection(string characterId)
    {
        string selectedCharacterId = string.IsNullOrEmpty(characterId) ? "character_1" : characterId;
        if (_selectedCharacterImage != null)
        {
            _selectedCharacterImage.sprite = _characterArtLibrary?.GetSprite(selectedCharacterId);
            _selectedCharacterImage.preserveAspect = true;
        }

        if (_selectedCharacterText != null)
        {
            _selectedCharacterText.text = GetCharacterDisplayName(selectedCharacterId);
        }

        foreach (CharacterOptionBinding binding in _characterOptionBindings)
        {
            bool isSelected = string.Equals(binding.CharacterId, selectedCharacterId, System.StringComparison.Ordinal);
            binding.PortraitImage.sprite = _characterArtLibrary?.GetSprite(binding.CharacterId);
            binding.PortraitImage.preserveAspect = true;
            binding.LabelText.text = isSelected ? "已选择" : GetCharacterDisplayName(binding.CharacterId);
            binding.Button.targetGraphic.color = isSelected
                ? new Color32(250, 210, 104, 255)
                : new Color32(128, 38, 32, 235);
        }
    }

    private void RenderConnectionState()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        bool isReady = controller != null && controller.LobbyModel.IsSessionReady;
        _connectionStatusText.text = isReady ? "已连接" : "连接中...";
    }

    private void RenderRoomList()
    {
        LobbyModel lobbyModel = GameApp.Current?.OnlineGameController?.LobbyModel;
        if (lobbyModel == null)
        {
            return;
        }

        ClearRoomItems();
        _emptyHintText.gameObject.SetActive(lobbyModel.Rooms.Count == 0);
        foreach (RoomSummarySnapshot room in lobbyModel.Rooms)
        {
            CreateRoomItem(room);
        }
    }

    private void RenderMatchmakingState()
    {
        LobbyModel lobbyModel = GameApp.Current?.OnlineGameController?.LobbyModel;
        if (lobbyModel == null)
        {
            return;
        }

        MatchmakingSnapshot matchmakingState = lobbyModel.MatchmakingState;
        bool isQueued = matchmakingState.IsQueued;
        _matchmakingButton.interactable = !isQueued;
        _matchmakingStatusText.text = isQueued
            ? $"匹配中 {matchmakingState.CurrentCount}/{matchmakingState.RequiredCount}"
            : "空闲";

        if (isQueued)
        {
            ShowBlockingOverlay(matchmakingState);
        }
        else
        {
            HideBlockingOverlay();
        }
    }

    private void CreateRoomItem(RoomSummarySnapshot room)
    {
        GameObject itemObject = OpenGameObject(_roomListContentTransform, RoomItemPrefabPath, $"room_item_{room.RoomId}");
        _roomItemGameObjects.Add(itemObject);

        SetText(itemObject.transform, "txt_room_name", room.Name);
        SetText(itemObject.transform, "txt_room_id", room.RoomId);
        SetText(itemObject.transform, "txt_owner_name", room.OwnerName);
        SetText(itemObject.transform, "txt_player_count", $"{room.PlayerCount}/{room.MaxPlayers}");

        Button joinButton = FindRequiredButton(itemObject.transform, "btn_join");
        bool canJoin = string.Equals(room.Status, "waiting", System.StringComparison.Ordinal)
            && room.PlayerCount < room.MaxPlayers;
        joinButton.interactable = canJoin;
        joinButton.onClick.AddListener(() => GameApp.Current?.OnlineGameController?.JoinRoom(room.RoomId));
    }

    private void ShowBlockingOverlay(MatchmakingSnapshot matchmakingState)
    {
        if (_blockingOverlayObject == null)
        {
            _blockingOverlayObject = OpenGameObject(ViewTransform, BlockingOverlayPrefabPath, "blocking_overlay");
            _overlayTitleText = FindRequiredText(_blockingOverlayObject.transform, "panel_message/txt_title");
            _overlayMessageText = FindRequiredText(_blockingOverlayObject.transform, "panel_message/txt_message");
            _overlayCancelButton = FindRequiredButton(_blockingOverlayObject.transform, "panel_message/btn_cancel");
            _overlayCancelButton.onClick.AddListener(OnClickCancelMatchmaking);
        }

        _blockingOverlayObject.SetActive(true);
        _overlayTitleText.text = "匹配中";
        _overlayMessageText.text = $"等待玩家 {matchmakingState.CurrentCount}/{matchmakingState.RequiredCount}";
    }

    private void HideBlockingOverlay()
    {
        if (_blockingOverlayObject == null)
        {
            return;
        }

        if (_overlayCancelButton != null)
        {
            _overlayCancelButton.onClick.RemoveListener(OnClickCancelMatchmaking);
        }

        UnityEngine.Object.Destroy(_blockingOverlayObject);
        _blockingOverlayObject = null;
        _overlayTitleText = null;
        _overlayMessageText = null;
        _overlayCancelButton = null;
    }

    private void OnClickRefresh()
    {
        GameApp.Current?.OnlineGameController?.RequestRoomList();
    }

    private void OnClickMatchmaking()
    {
        GameApp.Current?.OnlineGameController?.StartMatchmaking();
    }

    private void OnClickCancelMatchmaking()
    {
        if (_overlayTitleText != null)
        {
            _overlayTitleText.text = "取消中";
        }

        if (_overlayMessageText != null)
        {
            _overlayMessageText.text = "正在等待服务器确认...";
        }

        if (_overlayCancelButton != null)
        {
            _overlayCancelButton.interactable = false;
        }

        GameApp.Current?.OnlineGameController?.CancelMatchmaking();
    }

    private void OnClickJoinRoom()
    {
        GameApp.Current?.OnlineGameController?.JoinRoom(_roomIdInput.text);
    }

    private void OnClickCreateRoom()
    {
        GameApp.Current?.OnlineGameController?.CreateRoom(_roomNameInput.text);
    }

    private void OnClickProfile()
    {
        Debug.Log("Profile button is a placeholder in this version.");
    }

    private void OnClickCharacter(string characterId)
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller == null)
        {
            return;
        }

        controller.SetCharacter(characterId);
        RenderCharacterSelection(controller.LocalPlayerProfile.CharacterId);
    }

    private void OnErrorOccurred(string message)
    {
        _matchmakingStatusText.text = message;
        if (_overlayCancelButton != null)
        {
            _overlayCancelButton.interactable = true;
        }
    }

    private void ClearRoomItems()
    {
        foreach (GameObject roomItemGameObject in _roomItemGameObjects)
        {
            if (roomItemGameObject != null)
            {
                UnityEngine.Object.Destroy(roomItemGameObject);
            }
        }

        _roomItemGameObjects.Clear();
    }

    private void RemoveButtonListeners()
    {
        if (_refreshButton != null)
        {
            _refreshButton.onClick.RemoveListener(OnClickRefresh);
        }

        if (_matchmakingButton != null)
        {
            _matchmakingButton.onClick.RemoveListener(OnClickMatchmaking);
        }

        if (_joinRoomButton != null)
        {
            _joinRoomButton.onClick.RemoveListener(OnClickJoinRoom);
        }

        if (_createRoomButton != null)
        {
            _createRoomButton.onClick.RemoveListener(OnClickCreateRoom);
        }

        if (_profileButton != null)
        {
            _profileButton.onClick.RemoveListener(OnClickProfile);
        }

        foreach (CharacterOptionBinding binding in _characterOptionBindings)
        {
            if (binding.Button != null)
            {
                binding.Button.onClick.RemoveAllListeners();
            }
        }
    }

    private void ClearReferences()
    {
        _playerNameText = null;
        _playerIdText = null;
        _connectionStatusText = null;
        _matchmakingStatusText = null;
        _emptyHintText = null;
        _roomIdInput = null;
        _roomNameInput = null;
        _roomListContentTransform = null;
        _refreshButton = null;
        _matchmakingButton = null;
        _joinRoomButton = null;
        _createRoomButton = null;
        _profileButton = null;
        _selectedCharacterImage = null;
        _selectedCharacterText = null;
        _characterArtLibrary = null;
        _characterOptionBindings.Clear();
    }

    private void SetText(Transform rootTransform, string path, string text)
    {
        FindRequiredText(rootTransform, path).text = string.IsNullOrEmpty(text) ? "-" : text;
    }

    private string ShortenPlayerId(string playerId)
    {
        if (string.IsNullOrEmpty(playerId) || playerId.Length <= 8)
        {
            return playerId;
        }

        return playerId.Substring(0, 8);
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
            throw new System.InvalidOperationException($"Lobby view cannot find TMP_Text at path: {path}");
        }

        return text;
    }

    private TMP_InputField FindRequiredInput(string path)
    {
        Transform targetTransform = FindRequiredTransform(ViewTransform, path);
        TMP_InputField input = targetTransform.GetComponent<TMP_InputField>();
        if (input == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find TMP_InputField at path: {path}");
        }

        return input;
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
            throw new System.InvalidOperationException($"Lobby view cannot find Button at path: {path}");
        }

        return button;
    }

    private Image FindRequiredImage(string path)
    {
        Transform targetTransform = FindRequiredTransform(ViewTransform, path);
        Image image = targetTransform.GetComponent<Image>();
        if (image == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find Image at path: {path}");
        }

        return image;
    }

    private Transform FindRequiredTransform(string path)
    {
        return FindRequiredTransform(ViewTransform, path);
    }

    private Transform FindRequiredTransform(Transform rootTransform, string path)
    {
        Transform targetTransform = rootTransform == null ? null : rootTransform.Find(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find node: {path}");
        }

        return targetTransform;
    }

    private string GetCharacterDisplayName(string characterId)
    {
        switch (characterId)
        {
            case "character_1":
                return "福童";
            case "character_2":
                return "沪上阿姨";
            default:
                return "角色";
        }
    }

    private sealed class CharacterOptionBinding
    {
        public string CharacterId { get; }
        public Button Button { get; }
        public Image PortraitImage { get; }
        public TMP_Text LabelText { get; }

        public CharacterOptionBinding(string characterId, Button button, Image portraitImage, TMP_Text labelText)
        {
            CharacterId = characterId;
            Button = button;
            PortraitImage = portraitImage;
            LabelText = labelText;
        }
    }
}
