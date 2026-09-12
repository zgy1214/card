using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Game : UIScript
{
    private const string CardPrefabPath = "prefabs/template/card";

    private readonly TMP_Text[] _playerNameTexts = new TMP_Text[4];
    private readonly Image[] _playerCharacterImages = new Image[4];
    private readonly List<string> _selectedCardIds = new List<string>();

    private Transform _handAreaTransform;
    private HorizontalLayoutGroup _handLayout;
    private float _lastHandLayoutWidth = -1f;
    private Transform _previewAreaTransform;
    private HorizontalLayoutGroup _previewLayout;
    private float _lastPreviewLayoutWidth = -1f;
    private Transform _challengeTargetsTransform;
    private TMP_Text _timerText;
    private ChatPanel _chatPanel;
    private Button _catchButton;
    private Button _letgoButton;
    private Button _confirmButton;
    private CharacterArtLibrary _characterArtLibrary;
    private CardArtLibrary _cardArtLibrary;
    private GameSession _gameSession;
    private string _faceUpCardId;
    private string _lastPhase;

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
        BindChallengeNodes();
        BindShowdownNodes();
        SubscribeGameEvents();
        SubscribeControllerEvents();
        ResetLocalPhaseStateIfNeeded();
        RenderAll();
    }

    public override void OnClose()
    {
        UnsubscribeGameEvents();
        UnsubscribeControllerEvents();
        CloseChallenge();
        CloseShowdown();
        _chatPanel?.Close();
        _chatPanel = null;
        _characterArtLibrary = null;
        _cardArtLibrary = null;
        _handAreaTransform = null;
        _handLayout = null;
        _lastHandLayoutWidth = -1f;
        _previewAreaTransform = null;
        _previewLayout = null;
        _lastPreviewLayoutWidth = -1f;
        _challengeTargetsTransform = null;
        _timerText = null;
        _catchButton = null;
        _letgoButton = null;
        _confirmButton = null;
        _gameSession = null;
        _selectedCardIds.Clear();
        _faceUpCardId = null;
        _lastPhase = null;
    }

    private void BindNodes()
    {
        _handAreaTransform = FindRequiredTransform("root/panel_local_hand/card_player0");
        _handLayout = _handAreaTransform.GetComponent<HorizontalLayoutGroup>();
        _previewAreaTransform = FindRequiredTransform("root/table/preview_cards");
        _previewLayout = _previewAreaTransform.GetComponent<HorizontalLayoutGroup>();
        _challengeTargetsTransform = FindRequiredTransform("root/panel_challenge_targets");
        _timerText = FindRequiredText("root/table/timer/txt_timer");
        _catchButton = FindRequiredButton("root/action_panel1/btn_catch");
        _letgoButton = FindRequiredButton("root/action_panel1/btn_letgo");
        _confirmButton = FindRequiredButton("root/action_panel2/btn_confirm");
        _chatPanel = FindRequiredTransform("root/group_chat").GetComponent<ChatPanel>();
        _chatPanel.Initialize(message => GameApp.Current?.OnlineGameController?.SendGameChat(message));
        BindSeatTexts(0, "root/seat_local");
        BindSeatTexts(1, "root/seat_left");
        BindSeatTexts(2, "root/seat_top");
        BindSeatTexts(3, "root/seat_right");

    }

    private void BindSeatTexts(int displayIndex, string rootPath)
    {
        _playerCharacterImages[displayIndex] = FindRequiredTransform($"{rootPath}/panel_portrait/img_character").GetComponent<Image>();
        _playerNameTexts[displayIndex] = FindRequiredText($"{rootPath}/txt_name");
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
            controller.ErrorOccurred += OnChallengeError;
        }
    }

    private void UnsubscribeControllerEvents()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.GameChatReceived -= OnGameChatReceived;
            controller.ErrorOccurred -= OnChallengeError;
        }
    }

    private void OnGameChatReceived(NetworkGameChatPayload payload)
    {
        if (payload == null)
        {
            return;
        }

        string senderName = string.IsNullOrEmpty(payload.name) ? "玩家" : payload.name;
        _chatPanel.AddMessage(senderName, payload.message);
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
        _selectedCardIds.Clear();
        _faceUpCardId = null;
    }

    private void OnTurnTicked()
    {
        RenderTimer();
        if (_gameSession?.Phase == "challenge_select") RenderChallengeControls();
    }

    private void RenderAll()
    {
        RenderPhasePanel();
        RenderPlayerSeats();
        RenderHandCards();
        RenderChallenge();
        RenderShowdown(true);
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
        if (characterImage.sprite != null)
        {
            characterImage.SetNativeSize();
            RectTransform frame = (RectTransform)rectTransform.parent;
            float fit = Mathf.Min(frame.rect.width / rectTransform.sizeDelta.x,
                frame.rect.height / rectTransform.sizeDelta.y);
            scale *= fit;
        }
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
            RefreshCardLayout(_handLayout, ref _lastHandLayoutWidth);
            return;
        }

        bool canSelectCards = CanLocalSelectCards();
        for (int index = 0; index < localPlayer.GameHandCardDefinitions.Count; index += 1)
        {
            CardDefinition cardDefinition = localPlayer.GameHandCardDefinitions[index];
            GameObject cardObject = OpenGameObject(_handAreaTransform, CardPrefabPath, $"hand_card_{index}");
            BindCardObject(cardObject, cardDefinition, canSelectCards ? OnClickHandCard : null, true);
            ApplyHandCardSelectionVisual(cardObject, canSelectCards && _selectedCardIds.Contains(cardDefinition.CardId));
        }
        RefreshCardLayout(_handLayout, ref _lastHandLayoutWidth);
    }

    public override void OnTick(float deltaTime)
    {
        RenderShowdown();
        if (_handAreaTransform != null
            && !Mathf.Approximately(((RectTransform)_handAreaTransform).rect.width, _lastHandLayoutWidth))
        {
            RefreshCardLayout(_handLayout, ref _lastHandLayoutWidth);
        }
        if (_previewAreaTransform != null && _previewAreaTransform.gameObject.activeSelf
            && !Mathf.Approximately(((RectTransform)_previewAreaTransform).rect.width, _lastPreviewLayoutWidth))
        {
            RefreshCardLayout(_previewLayout, ref _lastPreviewLayoutWidth);
        }
    }

    private void RefreshCardLayout(HorizontalLayoutGroup layout, ref float lastWidth)
    {
        if (layout == null) return;

        RectTransform container = (RectTransform)layout.transform;
        lastWidth = container.rect.width;
        float availableWidth = Mathf.Max(0f, container.rect.width - layout.padding.horizontal);
        float totalCardWidth = 0f;
        int count = 0;
        foreach (RectTransform card in container)
        {
            // Destroy is deferred until the end of the frame; ignore old, disabled cards.
            if (!card.gameObject.activeSelf) continue;
            totalCardWidth += card.rect.width * card.localScale.x;
            count++;
        }

        layout.spacing = count <= 1 ? 0f : Mathf.Min(0f, (availableWidth - totalCardWidth) / (count - 1));
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    private void RenderPhasePanel()
    {
        RenderTimer();
        bool isPlaySelect = string.Equals(_gameSession?.Phase, "play_select", StringComparison.Ordinal);
        bool isChallengeSelect = string.Equals(_gameSession?.Phase, "challenge_select", StringComparison.Ordinal);
        bool isShowdown = _gameSession?.Phase == "showdown";
        if (_previewLayout != null) _previewLayout.enabled = isPlaySelect;
        SetNodeActive(_previewAreaTransform, isPlaySelect || isShowdown);
        SetNodeActive(_challengeTargetsTransform, isChallengeSelect);
        SetNodeActive(_confirmButton, isPlaySelect && !IsLocalPlaySubmitted());

        SetNodeActive(_catchButton, isChallengeSelect);
        SetNodeActive(_letgoButton, isChallengeSelect);

        if (isPlaySelect)
        {
            RenderPlayPanel();
        }
        else
        {
            if (!isShowdown) ClearChildren(_previewAreaTransform);
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
            // Keep the hit area for choosing the face-up card, but never tint preview cards.
            Button previewButton = FindRequiredChild(cardObject.transform, "btn").GetComponent<Button>();
            previewButton.transition = Selectable.Transition.None;
            previewButton.targetGraphic.color = Color.clear;
        }
        RefreshCardLayout(_previewLayout, ref _lastPreviewLayoutWidth);

        string error = submitted ? string.Empty : GetPlaySelectionError(previewCards);
        _confirmButton.interactable = !submitted && string.IsNullOrEmpty(error);
    }

    private void OnClickHandCard(string cardId)
    {
        if (!CanLocalSelectCards() || string.IsNullOrEmpty(cardId))
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
        if (!CanLocalSelectCards() || !_selectedCardIds.Contains(cardId))
        {
            return;
        }

        _faceUpCardId = cardId;
        RenderPlayPanel();
    }

    private void OnClickConfirmPlayButton()
    {
        if (!CanLocalSelectCards()) return;

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
            throw new InvalidOperationException($"Card art is missing for '{cardDefinition.CardName}'.");
        }

        faceImage.sprite = sprite;
        faceImage.color = Color.white;
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

    private void ApplyHandCardSelectionVisual(GameObject cardObject, bool selected)
    {
        Image buttonImage = FindRequiredChild(cardObject.transform, "btn").GetComponent<Image>();
        buttonImage.color = selected ? new Color32(255, 248, 220, 96) : Color.clear;

        // Both rows leave root positioning to the layout group. Only selected hand
        // cards lift their contents; preview cards remain aligned, including the face-up card.
        if (!selected) return;

        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Called once for each freshly instantiated card, preserving prefab offsets.
        foreach (RectTransform content in rectTransform)
            content.anchoredPosition += Vector2.up * 14f;
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
            child.SetActive(false);
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
