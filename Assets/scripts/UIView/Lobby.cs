using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Lobby : UIScript
{
    private const string BlockingOverlayPrefabPath = "prefabs/template/blocking_overlay";
    private static readonly string[] CharacterIds = { "character_1", "character_2" };

    private CharacterArtLibrary _characterArtLibrary;
    private Image _characterImage;
    private TMP_Text _playerNameText;
    private TMP_InputField _playerNameInput;
    private Button _editNameButton;
    private Button _previousCharacterButton;
    private Button _nextCharacterButton;
    private Button _joinRoomButton;
    private Button _createRoomButton;
    private Button _matchmakingButton;
    private Button _rulesButton;
    private Button _helpButton;
    private Button _settingsButton;
    private GameObject _blockingOverlayObject;
    private TMP_Text _overlayTitleText;
    private TMP_Text _overlayMessageText;
    private Button _overlayCancelButton;
    private int _characterIndex;

    public override string GetPath() => "prefabs/view/Lobby";

    public override void OnOpen()
    {
        BindNodes();
        SubscribeEvents();
        RenderProfile();
        RenderMatchmakingState();
    }

    public override void OnClose()
    {
        UnsubscribeEvents();
        HideBlockingOverlay();
        RemoveButtonListeners();
        ClearReferences();
    }

    private void BindNodes()
    {
        _characterArtLibrary = ViewGameObject.GetComponent<CharacterArtLibrary>();
        _characterImage = FindRequiredImage("root/group_character_stage/image_character");
        _playerNameText = FindRequiredText("root/group_character_stage/group_player_identity/txt_player_name");
        _playerNameInput = FindRequiredInput("root/group_character_stage/group_player_identity/input_player_name");
        _editNameButton = FindRequiredButton("root/group_character_stage/group_player_identity/btn_edit_name");
        _previousCharacterButton = FindRequiredButton("root/group_character_stage/btn_character_previous");
        _nextCharacterButton = FindRequiredButton("root/group_character_stage/btn_character_next");
        _joinRoomButton = FindRequiredButton("root/group_main_actions/btn_join_room");
        _createRoomButton = FindRequiredButton("root/group_main_actions/btn_create_room");
        _matchmakingButton = FindRequiredButton("root/group_main_actions/btn_matchmaking");
        _rulesButton = FindRequiredButton("root/group_main_actions/btn_rules");
        _helpButton = FindRequiredButton("root/group_top_actions/btn_help");
        _settingsButton = FindRequiredButton("root/group_top_actions/btn_settings");

        _editNameButton.onClick.AddListener(OnClickEditName);
        _playerNameInput.onEndEdit.AddListener(OnNameEditEnded);
        _previousCharacterButton.onClick.AddListener(OnClickPreviousCharacter);
        _nextCharacterButton.onClick.AddListener(OnClickNextCharacter);
        _joinRoomButton.onClick.AddListener(OnClickJoinRoom);
        _createRoomButton.onClick.AddListener(OnClickCreateRoom);
        _matchmakingButton.onClick.AddListener(OnClickMatchmaking);
        _rulesButton.onClick.AddListener(OnClickRules);
        _helpButton.onClick.AddListener(OnClickHelp);
        _settingsButton.onClick.AddListener(OnClickSettings);
    }

    private void SubscribeEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.LobbyModel.MatchmakingStateUpdated += RenderMatchmakingState;
        }
    }

    private void UnsubscribeEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.LobbyModel.MatchmakingStateUpdated -= RenderMatchmakingState;
        }
    }

    private void RenderProfile()
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (profile == null)
        {
            return;
        }

        _playerNameText.text = profile.Name;
        _playerNameInput.text = profile.Name;
        _characterIndex = GetCharacterIndex(profile.CharacterId);
        RenderCharacter();
    }

    private void RenderCharacter()
    {
        _characterImage.sprite = _characterArtLibrary?.GetSprite(CharacterIds[_characterIndex]);
        _characterImage.preserveAspect = true;
    }

    private void RenderMatchmakingState()
    {
        LobbyModel lobbyModel = GameApp.Current?.OnlineGameController?.LobbyModel;
        if (lobbyModel == null)
        {
            return;
        }

        bool isQueued = lobbyModel.MatchmakingState.IsQueued;
        _matchmakingButton.interactable = !isQueued;
        if (isQueued)
        {
            ShowBlockingOverlay(lobbyModel.MatchmakingState);
        }
        else
        {
            HideBlockingOverlay();
        }
    }

    private void OnClickEditName()
    {
        _playerNameInput.text = _playerNameText.text;
        _playerNameText.gameObject.SetActive(false);
        _editNameButton.gameObject.SetActive(false);
        _playerNameInput.gameObject.SetActive(true);
        _playerNameInput.Select();
        _playerNameInput.ActivateInputField();
    }

    private void OnNameEditEnded(string value)
    {
        string normalizedName = string.IsNullOrWhiteSpace(value) ? _playerNameText.text : value.Trim();
        if (normalizedName.Length > 16)
        {
            normalizedName = normalizedName.Substring(0, 16);
        }

        GameApp.Current?.OnlineGameController?.SetName(normalizedName);

        _playerNameText.text = normalizedName;
        _playerNameInput.text = normalizedName;
        _playerNameInput.gameObject.SetActive(false);
        _playerNameText.gameObject.SetActive(true);
        _editNameButton.gameObject.SetActive(true);
    }

    private void OnClickPreviousCharacter()
    {
        _characterIndex = (_characterIndex + CharacterIds.Length - 1) % CharacterIds.Length;
        ApplyCharacterSelection();
    }

    private void OnClickNextCharacter()
    {
        _characterIndex = (_characterIndex + 1) % CharacterIds.Length;
        ApplyCharacterSelection();
    }

    private void ApplyCharacterSelection()
    {
        GameApp.Current?.OnlineGameController?.SetCharacter(CharacterIds[_characterIndex]);
        RenderCharacter();
    }

    private void OnClickMatchmaking()
    {
        GameApp.Current?.OnlineGameController?.StartMatchmaking();
    }

    private void OnClickJoinRoom() => Debug.Log("TODO: open join room dialog.");
    private void OnClickCreateRoom()
    {
        GameApp.Current?.OnlineGameController?.CreateRoom(string.Empty);
    }
    private void OnClickRules() => Debug.Log("TODO: open rules dialog.");
    private void OnClickHelp() => Debug.Log("TODO: open help dialog.");
    private void OnClickSettings() => Debug.Log("TODO: open settings dialog.");

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

        _overlayCancelButton?.onClick.RemoveListener(OnClickCancelMatchmaking);
        Object.Destroy(_blockingOverlayObject);
        _blockingOverlayObject = null;
        _overlayTitleText = null;
        _overlayMessageText = null;
        _overlayCancelButton = null;
    }

    private void OnClickCancelMatchmaking()
    {
        GameApp.Current?.OnlineGameController?.CancelMatchmaking();
    }

    private void RemoveButtonListeners()
    {
        _editNameButton?.onClick.RemoveListener(OnClickEditName);
        _playerNameInput?.onEndEdit.RemoveListener(OnNameEditEnded);
        _previousCharacterButton?.onClick.RemoveListener(OnClickPreviousCharacter);
        _nextCharacterButton?.onClick.RemoveListener(OnClickNextCharacter);
        _joinRoomButton?.onClick.RemoveListener(OnClickJoinRoom);
        _createRoomButton?.onClick.RemoveListener(OnClickCreateRoom);
        _matchmakingButton?.onClick.RemoveListener(OnClickMatchmaking);
        _rulesButton?.onClick.RemoveListener(OnClickRules);
        _helpButton?.onClick.RemoveListener(OnClickHelp);
        _settingsButton?.onClick.RemoveListener(OnClickSettings);
    }

    private void ClearReferences()
    {
        _characterArtLibrary = null;
        _characterImage = null;
        _playerNameText = null;
        _playerNameInput = null;
        _editNameButton = null;
        _previousCharacterButton = null;
        _nextCharacterButton = null;
        _joinRoomButton = null;
        _createRoomButton = null;
        _matchmakingButton = null;
        _rulesButton = null;
        _helpButton = null;
        _settingsButton = null;
    }

    private int GetCharacterIndex(string characterId)
    {
        for (int index = 0; index < CharacterIds.Length; index += 1)
        {
            if (string.Equals(CharacterIds[index], characterId, System.StringComparison.Ordinal))
            {
                return index;
            }
        }

        return 0;
    }

    private TMP_Text FindRequiredText(string path) => FindRequiredText(ViewTransform, path);

    private TMP_Text FindRequiredText(Transform rootTransform, string path)
    {
        Transform target = FindRequiredTransform(rootTransform, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find TMP_Text at path: {path}");
        }

        return text;
    }

    private TMP_InputField FindRequiredInput(string path)
    {
        Transform target = FindRequiredTransform(ViewTransform, path);
        TMP_InputField input = target.GetComponent<TMP_InputField>();
        if (input == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find TMP_InputField at path: {path}");
        }

        return input;
    }

    private Button FindRequiredButton(string path) => FindRequiredButton(ViewTransform, path);

    private Button FindRequiredButton(Transform rootTransform, string path)
    {
        Transform target = FindRequiredTransform(rootTransform, path);
        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find Button at path: {path}");
        }

        return button;
    }

    private Image FindRequiredImage(string path)
    {
        Transform target = FindRequiredTransform(ViewTransform, path);
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find Image at path: {path}");
        }

        return image;
    }

    private Transform FindRequiredTransform(Transform rootTransform, string path)
    {
        Transform target = rootTransform == null ? null : rootTransform.Find(path);
        if (target == null)
        {
            throw new System.InvalidOperationException($"Lobby view cannot find node: {path}");
        }

        return target;
    }
}
