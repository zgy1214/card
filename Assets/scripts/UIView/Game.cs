using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Game : UIScript
{
    private const string CardPrefabPath = "prefabs/template/card";

    private readonly TMP_Text[] _playerNameTexts = new TMP_Text[4];
    private readonly TMP_Text[] _playerStatusTexts = new TMP_Text[4];
    private readonly Image[] _playerCharacterImages = new Image[4];
    private readonly List<string> _selectedCardIds = new List<string>();

    private Transform _handAreaTransform;
    private Transform _previewAreaTransform;
    private Transform _challengeTargetsTransform;
    private TMP_Text _timerText;
    private TMP_Text _chatPreviewText;
    private TMP_InputField _chatInput;
    private Button _chatSendButton;
    private Button _catchButton;
    private Button _letgoButton;
    private Button _confirmButton;
    private CharacterArtLibrary _characterArtLibrary;
    private CardArtLibrary _cardArtLibrary;
    private GameSession _gameSession;
    private string _faceUpCardId;
    private string _lastPhase;
    private readonly List<string> _gameChatMessages = new List<string>();

    public override string GetPath()
    {
        return "prefabs/view/Game";
    }

    public override void OnOpen()
    {
        GameViewArg gameViewArg = GameViewArg as GameViewArg;
        if (gameViewArg == null)
        {
            throw new ArgumentException("Game view requires GameViewArg.");
        }

        _gameSession = gameViewArg.GameSession;
        _characterArtLibrary = ViewGameObject.GetComponent<CharacterArtLibrary>();
        _cardArtLibrary = ViewGameObject.GetComponent<CardArtLibrary>();
        BindNodes();
        SubscribeGameEvents();
        SubscribeControllerEvents();
        RenderAll();
    }

    public override void OnClose()
    {
        UnsubscribeGameEvents();
        UnsubscribeControllerEvents();
        UnbindButton(_chatSendButton, OnClickSendChatButton);
        _chatSendButton = null;
        _chatInput = null;
        _characterArtLibrary = null;
        _cardArtLibrary = null;
        _handAreaTransform = null;
        _previewAreaTransform = null;
        _challengeTargetsTransform = null;
        _timerText = null;
        _catchButton = null;
        _letgoButton = null;
        _confirmButton = null;
        _gameSession = null;
        _selectedCardIds.Clear();
        _gameChatMessages.Clear();
        _faceUpCardId = null;
        _lastPhase = null;
    }

    private void BindNodes()
    {
        _handAreaTransform = FindRequiredTransform("root/panel_local_hand/card_player0");
        _previewAreaTransform = FindRequiredTransform("root/table/preview_cards");
        _challengeTargetsTransform = FindRequiredTransform("root/panel_challenge_targets");
        _timerText = FindRequiredText("root/table/timer/txt_timer");
        _catchButton = FindRequiredButton("root/action_panel1/btn_catch");
        _letgoButton = FindRequiredButton("root/action_panel1/btn_letgo");
        _confirmButton = FindRequiredButton("root/action_panel2/btn_confirm");
        _chatPreviewText = FindRequiredText("root/panel_chat/txt_chat_preview");
        _chatInput = FindRequiredTransform("root/panel_chat/panel_chat_input/input_chat").GetComponent<TMP_InputField>();
        _chatSendButton = FindRequiredButton("root/panel_chat/panel_chat_input/btn_send_chat");
        BindSeatTexts(0, "root/seat_local");
        BindSeatTexts(1, "root/seat_left");
        BindSeatTexts(2, "root/seat_top");
        BindSeatTexts(3, "root/seat_right");

        _chatSendButton.onClick.AddListener(OnClickSendChatButton);
    }

    private void BindSeatTexts(int displayIndex, string rootPath)
    {
        _playerCharacterImages[displayIndex] = FindRequiredTransform($"{rootPath}/panel_portrait/img_character").GetComponent<Image>();
        _playerNameTexts[displayIndex] = FindRequiredText($"{rootPath}/txt_name");
        _playerStatusTexts[displayIndex] = displayIndex == 0
            ? null
            : FindRequiredText($"{rootPath}/txt_status");
    }

    private void SubscribeGameEvents()
    {
        if (_gameSession == null)
        {
            return;
        }

        _gameSession.GameStarted += OnGameStateChanged;
        _gameSession.MatchStateUpdated += OnGameStateChanged;
        _gameSession.PhaseTimerTicked += OnTurnTicked;
    }

    private void UnsubscribeGameEvents()
    {
        if (_gameSession == null)
        {
            return;
        }

        _gameSession.GameStarted -= OnGameStateChanged;
        _gameSession.MatchStateUpdated -= OnGameStateChanged;
        _gameSession.PhaseTimerTicked -= OnTurnTicked;
    }

    private void SubscribeControllerEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.GameChatReceived += OnGameChatReceived;
        }
    }

    private void UnsubscribeControllerEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.GameChatReceived -= OnGameChatReceived;
        }
    }

    private void OnGameChatReceived(NetworkGameChatPayload payload)
    {
        if (payload == null)
        {
            return;
        }

        string senderName = string.IsNullOrEmpty(payload.name) ? "玩家" : payload.name;
        _gameChatMessages.Add($"{senderName}: {payload.message}");
        while (_gameChatMessages.Count > 5)
        {
            _gameChatMessages.RemoveAt(0);
        }

        RenderChatPreview();
    }

    private void OnGameStateChanged()
    {
        ResetLocalPhaseStateIfNeeded();
        PruneInvalidSelection();
        RenderAll();
    }

    private void ResetLocalPhaseStateIfNeeded()
    {
        string phase = _gameSession?.Phase;
        if (string.Equals(_lastPhase, phase, StringComparison.Ordinal))
        {
            return;
        }

        _lastPhase = phase;
        if (phase == "play_select")
        {
            _selectedCardIds.Clear();
            _faceUpCardId = null;
        }

    }

    private void OnTurnTicked()
    {
        RenderTimer();
    }

    private void RenderAll()
    {
        RenderPhasePanel();
        RenderPlayerSeats();
        RenderHandCards();
        RenderChatPreview();
    }

    private void RenderTimer()
    {
        if (_timerText == null)
        {
            return;
        }

        if (_gameSession != null && _gameSession.PhaseEndTime > 0)
        {
            double now = _gameSession.ServerTime > 0
                ? _gameSession.ServerTime + (Time.realtimeSinceStartupAsDouble - _gameSession.StateReceivedRealtime)
                : Time.realtimeSinceStartup;
            int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt((float)(_gameSession.PhaseEndTime - now)));
            _timerText.text = $"{remainingSeconds}s";
            return;
        }

        _timerText.text = "--";
    }

    private void RenderPlayerSeats()
    {
        for (int displayIndex = 0; displayIndex < 4; displayIndex += 1)
        {
            PlayerRuntime playerRuntime = _gameSession?.GetDisplayPlayerRuntime(displayIndex);
            string fallbackName = displayIndex == 0 ? "你" : $"玩家 {displayIndex + 1}";
            if (_playerNameTexts[displayIndex] != null)
            {
                _playerNameTexts[displayIndex].text = BuildPlayerName(playerRuntime, fallbackName);
            }

            if (_playerStatusTexts[displayIndex] != null)
            {
                _playerStatusTexts[displayIndex].text = BuildPlayerStatus(playerRuntime);
            }

            RenderSeatCharacter(displayIndex, playerRuntime);
        }
    }

    private void RenderSeatCharacter(int displayIndex, PlayerRuntime playerRuntime)
    {
        Image characterImage = _playerCharacterImages[displayIndex];
        if (characterImage == null)
        {
            return;
        }

        if (playerRuntime == null)
        {
            characterImage.sprite = null;
            characterImage.color = new Color32(255, 236, 188, 36);
            ApplyCharacterImageLayout(characterImage, Vector2.zero, 1f);
            return;
        }

        string characterId = GetDisplayCharacterId(playerRuntime);
        characterImage.sprite = _characterArtLibrary?.GetSprite(characterId);
        characterImage.preserveAspect = true;
        characterImage.color = characterImage.sprite == null
            ? new Color32(255, 236, 188, 36)
            : Color.white;
        ApplyCharacterImageLayout(
            characterImage,
            _characterArtLibrary?.GetGameOffset(characterId) ?? Vector2.zero,
            _characterArtLibrary?.GetGameScale(characterId) ?? 1f);
    }

    private string GetDisplayCharacterId(PlayerRuntime playerRuntime)
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (playerRuntime != null
            && profile != null
            && string.Equals(playerRuntime.PlayerId, profile.PlayerId, StringComparison.Ordinal))
        {
            return profile.CharacterId;
        }

        return playerRuntime?.CharacterId;
    }

    private void ApplyCharacterImageLayout(Image characterImage, Vector2 offset, float scale)
    {
        RectTransform rectTransform = characterImage.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = offset;
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private string BuildPlayerName(PlayerRuntime playerRuntime, string fallbackName)
    {
        if (playerRuntime == null)
        {
            return fallbackName;
        }

        string name = GetDisplayPlayerName(playerRuntime, fallbackName);
        return playerRuntime.IsLocalPlayer ? $"{name}（你）" : name;
    }

    private string GetDisplayPlayerName(PlayerRuntime playerRuntime, string fallbackName)
    {
        LocalPlayerProfile profile = GameApp.Current?.OnlineGameController?.LocalPlayerProfile;
        if (playerRuntime != null
            && profile != null
            && string.Equals(playerRuntime.PlayerId, profile.PlayerId, StringComparison.Ordinal))
        {
            return profile.Name;
        }

        return string.IsNullOrEmpty(playerRuntime?.Name) ? fallbackName : playerRuntime.Name;
    }

    private string BuildPlayerStatus(PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
        {
            return "等待同步";
        }

        NetworkGameStatePlayerPayload statePlayer = FindStatePlayer(playerRuntime.SeatIndex);
        if (statePlayer != null)
        {
            switch (_gameSession?.Phase)
            {
                case "play_select":
                    return statePlayer.play_submitted ? "已出牌" : "选择中";
                case "challenge_select":
                    return statePlayer.challenge_submitted ? "已确认" : "选择中";
                case "fortune_draw":
                    return statePlayer.fortune_draw_submitted ? "已抽取" : "抽取中";
                case "showdown":
                    return "公示中";
                case "final_result":
                    return "已结算";
            }
        }

        return playerRuntime.IsLocalPlayer ? "本地玩家" : "等待中";
    }

    private NetworkGameStatePlayerPayload FindStatePlayer(int seatIndex)
    {
        if (_gameSession?.GameStatePlayers == null)
        {
            return null;
        }

        foreach (NetworkGameStatePlayerPayload player in _gameSession.GameStatePlayers)
        {
            if (player != null && player.seat_index == seatIndex)
            {
                return player;
            }
        }

        return null;
    }

    private bool CanLocalSelectCards()
    {
        return string.Equals(_gameSession?.Phase, "play_select", StringComparison.Ordinal)
            && !IsLocalPlaySubmitted();
    }

    private bool IsLocalPlaySubmitted()
    {
        PlayerRuntime localPlayer = _gameSession?.GetDisplayPlayerRuntime(0);
        if (localPlayer == null)
        {
            return false;
        }

        NetworkGameStatePlayerPayload statePlayer = FindStatePlayer(localPlayer.SeatIndex);
        return statePlayer != null && statePlayer.play_submitted;
    }

    private void RenderHandCards()
    {
        ClearChildren(_handAreaTransform);
        PlayerRuntime localPlayer = _gameSession?.GetDisplayPlayerRuntime(0);
        if (localPlayer == null)
        {
            return;
        }

        for (int index = 0; index < localPlayer.GameHandCardDefinitions.Count; index += 1)
        {
            CardDefinition cardDefinition = localPlayer.GameHandCardDefinitions[index];
            GameObject cardObject = OpenGameObject(_handAreaTransform, CardPrefabPath, $"hand_card_{index}");
            BindCardObject(cardObject, cardDefinition, CanLocalSelectCards() ? OnClickHandCard : null, true);
            ApplyCardSelectionVisual(cardObject, _selectedCardIds.Contains(cardDefinition.CardId), false);
        }
    }

    private void RenderPhasePanel()
    {
        RenderTimer();
        bool isPlaySelect = string.Equals(_gameSession?.Phase, "play_select", StringComparison.Ordinal);
        bool isChallengeSelect = string.Equals(_gameSession?.Phase, "challenge_select", StringComparison.Ordinal);
        SetNodeActive(_previewAreaTransform, isPlaySelect);
        SetNodeActive(_challengeTargetsTransform, isChallengeSelect);
        SetNodeActive(_confirmButton, isPlaySelect);

        // These controls belong to the other action state. Keep their existing
        // meaning and layout; only hide them while selecting cards.
        SetNodeActive(_catchButton, isChallengeSelect);
        SetNodeActive(_letgoButton, isChallengeSelect);

        if (isPlaySelect)
        {
            RenderPlayPanel();
        }
        else
        {
            ClearChildren(_previewAreaTransform);
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.interactable = false;
            }
        }
    }

    private void RenderPlayPanel()
    {
        if (_previewAreaTransform == null || _confirmButton == null)
        {
            return;
        }

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(OnClickConfirmPlayButton);

        bool submitted = IsLocalPlaySubmitted();
        string faceUpCardId = _faceUpCardId;
        List<CardDefinition> previewCards = submitted ? GetSubmittedPlayCards(out faceUpCardId) : GetSelectedCards();

        ClearChildren(_previewAreaTransform);
        foreach (CardDefinition cardDefinition in previewCards)
        {
            GameObject cardObject = OpenGameObject(
                _previewAreaTransform,
                CardPrefabPath,
                $"preview_{cardDefinition.CardId}");
            bool faceUp = string.Equals(cardDefinition.CardId, faceUpCardId, StringComparison.Ordinal);
            BindCardObject(cardObject, cardDefinition, submitted ? null : OnClickPreviewCard, faceUp);
            ApplyCardSelectionVisual(cardObject, true, faceUp);
        }

        string error = submitted ? string.Empty : GetPlaySelectionError(previewCards);
        _confirmButton.interactable = !submitted && string.IsNullOrEmpty(error);
    }

    private void RenderChatPreview()
    {
        if (_chatPreviewText != null)
        {
            _chatPreviewText.text = _gameChatMessages.Count == 0 ? string.Empty : string.Join("\n", _gameChatMessages);
        }
    }

    private void OnClickHandCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return;
        }

        if (_selectedCardIds.Contains(cardId))
        {
            _selectedCardIds.Remove(cardId);
            if (string.Equals(_faceUpCardId, cardId, StringComparison.Ordinal))
            {
                _faceUpCardId = _selectedCardIds.Count > 0 ? _selectedCardIds[0] : null;
            }
        }
        else
        {
            _selectedCardIds.Add(cardId);
            if (string.IsNullOrEmpty(_faceUpCardId))
            {
                _faceUpCardId = cardId;
            }
        }

        RenderHandCards();
        RenderPlayPanel();
    }

    private void OnClickPreviewCard(string cardId)
    {
        if (!_selectedCardIds.Contains(cardId))
        {
            return;
        }

        _faceUpCardId = cardId;
        RenderPlayPanel();
    }

    private void OnClickConfirmPlayButton()
    {
        List<CardDefinition> selectedCards = GetSelectedCards();
        string error = GetPlaySelectionError(selectedCards);
        if (!string.IsNullOrEmpty(error))
        {
            RenderPlayPanel();
            return;
        }

        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.SubmitPlay(_selectedCardIds.ToArray(), _faceUpCardId);
        }
    }

    private void OnClickSendChatButton()
    {
        string message = _chatInput == null ? string.Empty : _chatInput.text;
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.SendGameChat(message);
        }

        if (_chatInput != null)
        {
            _chatInput.text = string.Empty;
        }
    }

    private List<CardDefinition> GetSelectedCards()
    {
        PlayerRuntime localPlayer = _gameSession?.GetDisplayPlayerRuntime(0);
        List<CardDefinition> selectedCards = new List<CardDefinition>();
        if (localPlayer == null)
        {
            return selectedCards;
        }

        foreach (string selectedCardId in _selectedCardIds)
        {
            foreach (CardDefinition cardDefinition in localPlayer.GameHandCardDefinitions)
            {
                if (string.Equals(cardDefinition.CardId, selectedCardId, StringComparison.Ordinal))
                {
                    selectedCards.Add(cardDefinition);
                    break;
                }
            }
        }

        return selectedCards;
    }

    private List<CardDefinition> GetSubmittedPlayCards(out string faceUpCardId)
    {
        faceUpCardId = _gameSession?.LocalPlayerPrivate?.submitted_play?.face_up_card_id;
        List<CardDefinition> submittedCards = new List<CardDefinition>();
        NetworkCardPayload[] cards = _gameSession?.LocalPlayerPrivate?.submitted_play?.cards;
        if (cards == null)
        {
            return submittedCards;
        }

        foreach (NetworkCardPayload cardPayload in cards)
        {
            if (cardPayload != null && cardPayload.HasValue())
            {
                submittedCards.Add(cardPayload.ToCardDefinition());
            }
        }

        return submittedCards;
    }

    private string GetPlaySelectionError(IReadOnlyList<CardDefinition> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            return "请选择至少一张手牌";
        }

        if (string.IsNullOrEmpty(_faceUpCardId))
        {
            return "请选择一张明牌";
        }

        if (!ContainsCard(selectedCards, _faceUpCardId))
        {
            return "明牌必须来自已选手牌";
        }

        if (!IsLegalPlay(selectedCards))
        {
            return "所选牌必须同数字，或至少包含一张 3";
        }

        return string.Empty;
    }

    private bool IsLegalPlay(IReadOnlyList<CardDefinition> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            return false;
        }

        int firstRank = selectedCards[0].Rank;
        bool sameRank = true;
        foreach (CardDefinition cardDefinition in selectedCards)
        {
            if (cardDefinition.Rank == 3)
            {
                return true;
            }

            if (cardDefinition.Rank != firstRank)
            {
                sameRank = false;
            }
        }

        return sameRank;
    }

    private bool ContainsCard(IReadOnlyList<CardDefinition> cards, string cardId)
    {
        foreach (CardDefinition cardDefinition in cards)
        {
            if (string.Equals(cardDefinition.CardId, cardId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void PruneInvalidSelection()
    {
        PlayerRuntime localPlayer = _gameSession?.GetDisplayPlayerRuntime(0);
        if (localPlayer == null)
        {
            _selectedCardIds.Clear();
            _faceUpCardId = null;
            return;
        }

        for (int index = _selectedCardIds.Count - 1; index >= 0; index -= 1)
        {
            if (!ContainsCard(localPlayer.GameHandCardDefinitions, _selectedCardIds[index]))
            {
                _selectedCardIds.RemoveAt(index);
            }
        }

        if (!string.IsNullOrEmpty(_faceUpCardId) && !_selectedCardIds.Contains(_faceUpCardId))
        {
            _faceUpCardId = _selectedCardIds.Count > 0 ? _selectedCardIds[0] : null;
        }
    }

    private void BindCardObject(
        GameObject cardObject,
        CardDefinition cardDefinition,
        Action<string> clickAction,
        bool revealFace)
    {
        if (cardObject == null)
        {
            throw new ArgumentNullException(nameof(cardObject));
        }

        if (cardDefinition == null)
        {
            throw new ArgumentNullException(nameof(cardDefinition));
        }

        Image faceImage = FindRequiredChild(cardObject.transform, "img_card_face").GetComponent<Image>();
        Sprite sprite = revealFace
            ? _cardArtLibrary?.GetFaceSprite(cardDefinition.CardName)
            : _cardArtLibrary?.BackSprite;
        if (sprite == null)
        {
            Debug.LogError($"Card art is missing for '{cardDefinition.CardName}'. Rebuild prefabs and verify CardArtLibrary entries.");
        }

        faceImage.sprite = sprite;
        faceImage.color = sprite == null ? new Color32(248, 244, 230, 255) : Color.white;
        faceImage.preserveAspect = true;
        Button cardButton = FindRequiredChild(cardObject.transform, "btn").GetComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.interactable = clickAction != null;
        if (clickAction != null)
        {
            string cardId = cardDefinition.CardId;
            cardButton.onClick.AddListener(() => clickAction(cardId));
        }
    }

    private void ApplyCardSelectionVisual(GameObject cardObject, bool selected, bool faceUp)
    {
        Image buttonImage = FindRequiredChild(cardObject.transform, "btn").GetComponent<Image>();
        buttonImage.color = faceUp
            ? new Color32(255, 232, 150, 96)
            : selected ? new Color32(255, 248, 220, 96) : new Color32(255, 255, 255, 0);

        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = selected && !faceUp
                ? new Vector2(rectTransform.anchoredPosition.x, 14f)
                : new Vector2(rectTransform.anchoredPosition.x, 0f);
        }
    }

    private void ClearChildren(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            return;
        }

        List<GameObject> children = new List<GameObject>();
        for (int index = 0; index < parentTransform.childCount; index += 1)
        {
            children.Add(parentTransform.GetChild(index).gameObject);
        }

        foreach (GameObject child in children)
        {
            UnityEngine.Object.Destroy(child);
        }
    }

    private void SetNodeActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private TMP_Text FindRequiredText(string path)
    {
        TMP_Text text = FindRequiredTransform(path).GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException($"Game view cannot find TMP_Text at path: {path}");
        }

        return text;
    }

    private Button FindRequiredButton(string path)
    {
        Button button = FindRequiredTransform(path).GetComponent<Button>();
        if (button == null)
        {
            throw new InvalidOperationException($"Game view cannot find Button at path: {path}");
        }

        return button;
    }

    private Transform FindRequiredTransform(string path)
    {
        Transform target = ViewTransform.Find(path);
        if (target == null)
        {
            throw new InvalidOperationException($"Game view cannot find node: {path}");
        }

        return target;
    }

    private Transform FindRequiredChild(Transform parent, string path)
    {
        Transform target = parent.Find(path);
        if (target == null)
        {
            throw new InvalidOperationException($"Game panel cannot find node: {path}");
        }

        return target;
    }
}
