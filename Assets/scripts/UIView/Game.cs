using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Game : UIScript
{
    private const string CardPrefabPath = "prefabs/template/card";
    private const string PlayPanelPath = "prefabs/template/game_play_panel";
    private const string ChallengePanelPath = "prefabs/template/game_challenge_panel";
    private const string ShowdownPanelPath = "prefabs/template/game_showdown_panel";
    private const string FortunePanelPath = "prefabs/template/game_fortune_panel";
    private const string ResultPanelPath = "prefabs/template/game_result_panel";

    private readonly TMP_Text[] _playerNameTexts = new TMP_Text[4];
    private readonly TMP_Text[] _playerCardCountTexts = new TMP_Text[4];
    private readonly TMP_Text[] _playerStatusTexts = new TMP_Text[4];
    private readonly Image[] _playerCharacterImages = new Image[4];
    private readonly List<string> _selectedCardIds = new List<string>();

    private Button _menuButton;
    private Transform _handAreaTransform;
    private Transform _phaseHostTransform;
    private TMP_Text _phaseTitleText;
    private TMP_Text _phaseHintText;
    private TMP_Text _timerText;
    private TMP_Text _chatPreviewText;
    private TMP_InputField _chatInput;
    private Button _chatSendButton;
    private CharacterArtLibrary _characterArtLibrary;
    private CardArtLibrary _cardArtLibrary;
    private GameObject _activePhasePanel;
    private string _activePhasePanelPath;
    private GameSession _gameSession;
    private string _faceUpCardId;
    private string _lastPhase;
    private int _selectedChallengeSeatIndex = -2;
    private int _fortuneDrawCount = 2;
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
        UnbindButton(_menuButton, OnClickMenuButton);
        UnbindButton(_chatSendButton, OnClickSendChatButton);
        _menuButton = null;
        _chatSendButton = null;
        _chatInput = null;
        _characterArtLibrary = null;
        _cardArtLibrary = null;
        _handAreaTransform = null;
        _phaseHostTransform = null;
        _activePhasePanel = null;
        _activePhasePanelPath = null;
        _gameSession = null;
        _selectedCardIds.Clear();
        _gameChatMessages.Clear();
        _faceUpCardId = null;
        _lastPhase = null;
        _selectedChallengeSeatIndex = -2;
        _fortuneDrawCount = 2;
    }

    private void BindNodes()
    {
        _phaseTitleText = FindRequiredText("root/panel_top_bar/txt_phase_title");
        _phaseHintText = FindRequiredText("root/panel_top_bar/txt_phase_hint");
        _timerText = null;
        _handAreaTransform = FindRequiredTransform("root/panel_local_hand/card_player0");
        _phaseHostTransform = FindRequiredTransform("root/phase_host");
        _chatPreviewText = FindRequiredText("root/panel_chat/txt_chat_preview");
        _chatInput = FindRequiredTransform("root/panel_chat/panel_chat_input/input_chat").GetComponent<TMP_InputField>();
        _chatSendButton = FindRequiredButton("root/panel_chat/panel_chat_input/btn_send_chat");
        _menuButton = FindRequiredButton("root/panel_top_bar/btn_menu");

        BindSeatTexts(0, "root/seat_local");
        BindSeatTexts(1, "root/seat_left");
        BindSeatTexts(2, "root/seat_top");
        BindSeatTexts(3, "root/seat_right");

        _menuButton.onClick.AddListener(OnClickMenuButton);
        _chatSendButton.onClick.AddListener(OnClickSendChatButton);
    }

    private void BindSeatTexts(int displayIndex, string rootPath)
    {
        _playerCharacterImages[displayIndex] = FindRequiredTransform($"{rootPath}/panel_portrait/img_character").GetComponent<Image>();
        _playerNameTexts[displayIndex] = FindRequiredText($"{rootPath}/txt_name");
        _playerCardCountTexts[displayIndex] = FindRequiredText($"{rootPath}/txt_card_count");
        _playerStatusTexts[displayIndex] = FindRequiredText($"{rootPath}/txt_status");
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

        if (phase == "challenge_select")
        {
            _selectedChallengeSeatIndex = -2;
        }

        if (phase == "fortune_draw")
        {
            _fortuneDrawCount = _gameSession?.FortuneState == null ? 2 : _gameSession.FortuneState.min_draw_count;
        }
    }

    private void OnTurnTicked()
    {
        RenderTimer();
    }

    private void RenderAll()
    {
        RenderPhasePanel();
        RenderPhaseHeader();
        RenderPlayerSeats();
        RenderHandCards();
        RenderChatPreview();
    }

    private void RenderPhaseHeader()
    {
        if (_gameSession == null || string.IsNullOrEmpty(_gameSession.MatchId))
        {
            _phaseTitleText.text = "对局准备中";
            _phaseHintText.text = "等待服务器同步牌局状态";
            RenderTimer();
            return;
        }

        _phaseTitleText.text = BuildPhaseTitle();
        _phaseHintText.text = BuildPhaseHint();
        RenderTimer();
    }

    private void RenderTimer()
    {
        if (_timerText == null)
        {
            _timerText = FindPhaseTimerText();
            if (_timerText == null)
            {
                return;
            }
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

    private string BuildPhaseTitle()
    {
        switch (_gameSession?.Phase)
        {
            case "play_select":
                return $"第 {_gameSession.RoundIndex} 轮 出牌";
            case "challenge_select":
                return $"第 {_gameSession.RoundIndex} 轮 质疑";
            case "showdown":
                return $"第 {_gameSession.RoundIndex} 轮 公示";
            case "fortune_draw":
                return "气运抽取";
            case "final_result":
                return "最终结算";
            default:
                return _gameSession?.IsFinished == true ? "最终结算" : "对局进行中";
        }
    }

    private string BuildPhaseHint()
    {
        switch (_gameSession?.Phase)
        {
            case "play_select":
                return "选择手牌，指定明牌后确认出牌";
            case "challenge_select":
                return "查看公开声明，选择是否质疑";
            case "showdown":
                return "被质疑玩家依次翻开暗牌并结算气运";
            case "fortune_draw":
                return "从自己的气运池中选择抽取数量";
            case "final_result":
                return "按有效霉运、最终福运和溜走的3排名";
            default:
                return "等待服务器同步牌局状态";
        }
    }

    private void RenderPlayerSeats()
    {
        for (int displayIndex = 0; displayIndex < 4; displayIndex += 1)
        {
            PlayerRuntime playerRuntime = _gameSession?.GetDisplayPlayerRuntime(displayIndex);
            string fallbackName = displayIndex == 0 ? "你" : $"玩家 {displayIndex + 1}";
            _playerNameTexts[displayIndex].text = BuildPlayerName(playerRuntime, fallbackName);
            _playerCardCountTexts[displayIndex].text = playerRuntime == null
                ? "0"
                : playerRuntime.HandCardCount.ToString();
            _playerStatusTexts[displayIndex].text = BuildPlayerStatus(playerRuntime);
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

        characterImage.sprite = _characterArtLibrary?.GetSprite(playerRuntime.CharacterId);
        characterImage.preserveAspect = true;
        characterImage.color = characterImage.sprite == null
            ? new Color32(255, 236, 188, 36)
            : Color.white;
        ApplyCharacterImageLayout(
            characterImage,
            _characterArtLibrary?.GetGameOffset(playerRuntime.CharacterId) ?? Vector2.zero,
            _characterArtLibrary?.GetGameScale(playerRuntime.CharacterId) ?? 1f);
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

        string name = string.IsNullOrEmpty(playerRuntime.Name) ? fallbackName : playerRuntime.Name;
        return playerRuntime.IsLocalPlayer ? $"{name}（你）" : name;
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
            BindCardObject(cardObject, cardDefinition, CanLocalSelectCards() ? OnClickHandCard : null, true, false);
            ApplyCardLayoutSize(cardObject, 70f, 104f);
            ApplyCardSelectionVisual(cardObject, _selectedCardIds.Contains(cardDefinition.CardId), false);
        }
    }

    private void RenderPhasePanel()
    {
        string panelPath = GetPhasePanelPath();
        if (string.IsNullOrEmpty(panelPath))
        {
            ClearActivePhasePanel();
            return;
        }

        if (_activePhasePanel == null || !string.Equals(_activePhasePanelPath, panelPath, StringComparison.Ordinal))
        {
            ClearActivePhasePanel();
            _activePhasePanel = OpenGameObject(_phaseHostTransform, panelPath, System.IO.Path.GetFileNameWithoutExtension(panelPath));
            _activePhasePanelPath = panelPath;
        }

        _timerText = FindPhaseTimerText();
        RenderTimer();

        switch (_gameSession?.Phase)
        {
            case "challenge_select":
                RenderChallengePanel();
                return;
            case "showdown":
                RenderShowdownPanel();
                return;
            case "fortune_draw":
                RenderFortunePanel();
                return;
            case "final_result":
                RenderResultPanel();
                return;
            default:
                RenderPlayPanel();
                return;
        }
    }

    private string GetPhasePanelPath()
    {
        switch (_gameSession?.Phase)
        {
            case "challenge_select":
                return ChallengePanelPath;
            case "showdown":
                return ShowdownPanelPath;
            case "fortune_draw":
                return FortunePanelPath;
            case "final_result":
                return ResultPanelPath;
            case "play_select":
            case null:
            case "":
                return PlayPanelPath;
            default:
                return PlayPanelPath;
        }
    }

    private void ClearActivePhasePanel()
    {
        if (_activePhasePanel != null)
        {
            UnityEngine.Object.Destroy(_activePhasePanel);
            _activePhasePanel = null;
        }

        _activePhasePanelPath = null;
        _timerText = null;
    }

    private TMP_Text FindPhaseTimerText()
    {
        if (_activePhasePanel == null)
        {
            return null;
        }

        TMP_Text[] texts = _activePhasePanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && string.Equals(text.name, "txt_timer", StringComparison.Ordinal))
            {
                return text;
            }
        }

        return null;
    }

    private void RenderPlayPanel()
    {
        if (_activePhasePanel == null)
        {
            return;
        }

        Transform previewArea = FindRequiredChild(_activePhasePanel.transform, "panel_card_preview/preview_cards");
        TMP_Text declarationText = FindRequiredChild(_activePhasePanel.transform, "panel_card_preview/txt_declaration").GetComponent<TMP_Text>();
        TMP_Text errorText = FindRequiredChild(_activePhasePanel.transform, "panel_actions/txt_error").GetComponent<TMP_Text>();
        Button confirmButton = FindRequiredChild(_activePhasePanel.transform, "panel_actions/btn_confirm_play").GetComponent<Button>();
        Button clearButton = FindRequiredChild(_activePhasePanel.transform, "panel_actions/btn_clear_selection").GetComponent<Button>();

        confirmButton.onClick.RemoveAllListeners();
        clearButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnClickConfirmPlayButton);
        clearButton.onClick.AddListener(OnClickClearSelectionButton);

        bool submitted = IsLocalPlaySubmitted();
        string faceUpCardId = _faceUpCardId;
        List<CardDefinition> previewCards = submitted ? GetSubmittedPlayCards(out faceUpCardId) : GetSelectedCards();

        ClearChildren(previewArea);
        foreach (CardDefinition cardDefinition in previewCards)
        {
            GameObject cardObject = OpenGameObject(previewArea, CardPrefabPath, $"preview_{cardDefinition.CardId}");
            bool faceUp = string.Equals(cardDefinition.CardId, faceUpCardId, StringComparison.Ordinal);
            BindCardObject(cardObject, cardDefinition, submitted ? null : OnClickPreviewCard, faceUp, faceUp);
            ApplyCardLayoutSize(cardObject, 60f, 88f);
            ApplyCardSelectionVisual(cardObject, true, faceUp);
        }

        declarationText.text = BuildDeclarationText(previewCards, faceUpCardId);
        string error = submitted ? string.Empty : GetPlaySelectionError(previewCards);
        errorText.text = submitted ? "已确认出牌，等待其他玩家" : error;
        confirmButton.interactable = !submitted && string.IsNullOrEmpty(error);
        clearButton.interactable = !submitted && previewCards.Count > 0;
    }

    private void RenderChallengePanel()
    {
        if (_activePhasePanel == null)
        {
            return;
        }

        Button noneButton = FindRequiredChild(_activePhasePanel.transform, "panel_challenge/panel_no_challenge/btn_no_challenge").GetComponent<Button>();
        Button confirmButton = FindRequiredChild(_activePhasePanel.transform, "panel_challenge/panel_no_challenge/btn_confirm_challenge").GetComponent<Button>();
        TMP_Text statusText = FindRequiredChild(_activePhasePanel.transform, "panel_challenge/txt_status").GetComponent<TMP_Text>();

        int ownTarget = _gameSession?.ChallengeState == null ? -2 : _gameSession.ChallengeState.own_target_seat_index;
        if (ownTarget != -2)
        {
            _selectedChallengeSeatIndex = ownTarget;
        }

        RenderChallengeInfoRow("row_left", 1, ownTarget);
        RenderChallengeInfoRow("row_top", 2, ownTarget);
        RenderChallengeInfoRow("row_right", 3, ownTarget);
        BindChallengeChoiceButton(noneButton, -1, ownTarget, true);
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnClickConfirmChallengeButton);
        confirmButton.interactable = _selectedChallengeSeatIndex != -2 && ownTarget == -2;
        statusText.text = ownTarget == -2
            ? BuildChallengeStatusText()
            : "已确认，等待其他玩家";
    }

    private void RenderChallengeInfoRow(string rowName, int displayIndex, int ownTarget)
    {
        Transform row = FindRequiredChild(_activePhasePanel.transform, $"panel_challenge/{rowName}");
        TMP_Text playerNameText = FindRequiredChild(row, "txt_player_name").GetComponent<TMP_Text>();
        TMP_Text declarationText = FindRequiredChild(row, "txt_declaration").GetComponent<TMP_Text>();
        Transform previewCards = FindRequiredChild(row, "preview_cards");
        Button challengeButton = FindRequiredChild(row, "btn_challenge").GetComponent<Button>();

        int seatIndex = GetSeatIndexForDisplay(displayIndex);
        NetworkGameStatePlayerPayload player = FindStatePlayer(seatIndex);
        NetworkPublicPlayPayload publicPlay = player?.public_play;
        playerNameText.text = GetPlayerNameBySeat(seatIndex);
        ClearChildren(previewCards);

        bool hasPublicPlay = publicPlay != null && publicPlay.HasValue();
        if (!hasPublicPlay)
        {
            declarationText.text = "声明牌：等待公开";
            BindChallengeChoiceButton(challengeButton, seatIndex, ownTarget, false);
            return;
        }

        declarationText.text = $"声明牌：{publicPlay.declaration}";
        GameObject faceUpCard = OpenGameObject(previewCards, CardPrefabPath, $"challenge_face_{seatIndex}");
        BindCardObject(faceUpCard, publicPlay.face_up_card.ToCardDefinition(), null, true, true);
        ApplyCardLayoutSize(faceUpCard, 42f, 62f);

        for (int index = 0; index < publicPlay.hidden_count; index += 1)
        {
            CardDefinition hiddenCard = new CardDefinition(
                $"challenge_hidden_{seatIndex}_{index}",
                publicPlay.face_up_card.card_name,
                publicPlay.face_up_card.rank);
            GameObject hiddenCardObject = OpenGameObject(previewCards, CardPrefabPath, $"challenge_hidden_{seatIndex}_{index}");
            BindCardObject(hiddenCardObject, hiddenCard, null, false, false);
            ApplyCardLayoutSize(hiddenCardObject, 42f, 62f);
        }

        BindChallengeChoiceButton(challengeButton, seatIndex, ownTarget, seatIndex >= 0);
    }

    private void RenderShowdownPanel()
    {
        if (_activePhasePanel == null)
        {
            return;
        }

        Image targetCharacterImage = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_target_card/panel_target_portrait/img_target_character").GetComponent<Image>();
        TMP_Text targetNameText = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_target_card/txt_target_name").GetComponent<TMP_Text>();
        TMP_Text challengersText = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_target_card/txt_challengers").GetComponent<TMP_Text>();
        Transform cardsTransform = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_reveal_stage/panel_revealed_cards");
        TMP_Text resultText = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_reveal_stage/panel_result_stamp/txt_result").GetComponent<TMP_Text>();
        TMP_Text eventRowsText = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_event_list/txt_event_rows").GetComponent<TMP_Text>();
        eventRowsText.lineSpacing = 14f;
        ClearChildren(cardsTransform);

        NetworkShowdownEventPayload firstEvent = FirstShowdownEvent();
        if (firstEvent == null)
        {
            targetNameText.text = "无人被质疑";
            challengersText.text = "暗牌全部保持隐藏";
            resultText.text = "安全过关";
            resultText.color = AccentTextColor(true);
            eventRowsText.text = "本轮无人质疑\n短暂停留后进入下一阶段";
            targetCharacterImage.sprite = null;
            targetCharacterImage.color = new Color32(255, 236, 188, 36);
            CreateRuntimeText(cardsTransform, "没有暗牌需要翻开", 20, TextAlignmentOptions.Center);
            RenderShowdownDeltaSlots(null);
            return;
        }

        NetworkShowdownEventPayload[] events = _gameSession?.ShowdownState?.events;
        PlayerRuntime targetPlayer = _gameSession?.GetPlayerRuntime(firstEvent.target_seat_index);
        targetCharacterImage.sprite = _characterArtLibrary?.GetSprite(targetPlayer?.CharacterId);
        targetCharacterImage.preserveAspect = true;
        targetCharacterImage.color = targetCharacterImage.sprite == null
            ? new Color32(255, 236, 188, 36)
            : Color.white;
        ApplyCharacterImageLayout(targetCharacterImage, Vector2.zero, 1f);
        targetNameText.text = GetPlayerNameBySeat(firstEvent.target_seat_index);
        challengersText.text = $"质疑者：{BuildChallengerNames(firstEvent.challenger_seat_indexes)}";
        resultText.text = firstEvent.success ? "质疑成功" : "质疑失败";
        resultText.color = AccentTextColor(firstEvent.success);
        eventRowsText.text = BuildShowdownEventRows(events);
        RenderShowdownDeltaSlots(AggregateFortuneDeltas(events));

        if (firstEvent.revealed_cards == null || firstEvent.revealed_cards.Length == 0)
        {
            CreateRuntimeText(cardsTransform, "明牌已公开，无暗牌", 20, TextAlignmentOptions.Center);
            return;
        }

        for (int index = 0; index < firstEvent.revealed_cards.Length; index += 1)
        {
            NetworkCardPayload cardPayload = firstEvent.revealed_cards[index];
            if (cardPayload == null || !cardPayload.HasValue())
            {
                continue;
            }

            GameObject cardObject = OpenGameObject(cardsTransform, CardPrefabPath, $"revealed_{index}");
            BindCardObject(cardObject, cardPayload.ToCardDefinition(), null, true, false);
            ApplyCardLayoutSize(cardObject, 82f, 122f);
        }
    }

    private void RenderShowdownDeltaSlots(Dictionary<int, int> deltasBySeat)
    {
        Transform grid = FindRequiredChild(_activePhasePanel.transform, "panel_showdown/panel_delta_grid");
        for (int displayIndex = 0; displayIndex < 4; displayIndex += 1)
        {
            Transform slot = FindRequiredChild(grid, $"delta_slot_{displayIndex}");
            TMP_Text playerText = FindRequiredChild(slot, "txt_player").GetComponent<TMP_Text>();
            TMP_Text deltaText = FindRequiredChild(slot, "txt_delta").GetComponent<TMP_Text>();
            int seatIndex = GetSeatIndexForDisplay(displayIndex);
            int delta = 0;
            if (deltasBySeat != null && deltasBySeat.TryGetValue(seatIndex, out int changedDelta))
            {
                delta = changedDelta;
            }

            playerText.text = GetPlayerNameBySeat(seatIndex);
            deltaText.text = FormatFortuneDelta(delta);
            deltaText.color = AccentTextColor(delta >= 0);
        }
    }

    private void RenderFortunePanel()
    {
        if (_activePhasePanel == null)
        {
            return;
        }

        TMP_Text poolText = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/panel_fortune_pool/txt_pool_hint").GetComponent<TMP_Text>();
        TMP_Text drawCountText = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/panel_draw_controls/txt_draw_count").GetComponent<TMP_Text>();
        TMP_Text resultText = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/txt_result").GetComponent<TMP_Text>();
        Button minusButton = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/panel_draw_controls/btn_minus").GetComponent<Button>();
        Button plusButton = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/panel_draw_controls/btn_plus").GetComponent<Button>();
        Button submitButton = FindRequiredChild(_activePhasePanel.transform, "panel_fortune/btn_submit_draw").GetComponent<Button>();

        int minDraw = _gameSession?.FortuneState == null ? 2 : _gameSession.FortuneState.min_draw_count;
        int maxDraw = _gameSession?.FortuneState == null ? 5 : _gameSession.FortuneState.max_draw_count;
        _fortuneDrawCount = Mathf.Clamp(_fortuneDrawCount, minDraw, maxDraw);
        bool submitted = _gameSession?.FortuneState != null && _gameSession.FortuneState.own_submitted;

        minusButton.onClick.RemoveAllListeners();
        plusButton.onClick.RemoveAllListeners();
        submitButton.onClick.RemoveAllListeners();
        minusButton.onClick.AddListener(() => ChangeFortuneDrawCount(-1));
        plusButton.onClick.AddListener(() => ChangeFortuneDrawCount(1));
        submitButton.onClick.AddListener(OnClickSubmitFortuneDrawButton);
        minusButton.interactable = !submitted && _fortuneDrawCount > minDraw;
        plusButton.interactable = !submitted && _fortuneDrawCount < maxDraw;
        submitButton.interactable = !submitted;

        drawCountText.text = _fortuneDrawCount.ToString();
        poolText.text = BuildFortunePoolText();
        resultText.text = BuildFortuneResultText(submitted);
    }

    private void RenderResultPanel()
    {
        if (_activePhasePanel == null)
        {
            return;
        }

        TMP_Text rowsText = FindRequiredChild(_activePhasePanel.transform, "panel_result/panel_ranking/txt_rows").GetComponent<TMP_Text>();
        Button viewLogButton = FindRequiredChild(_activePhasePanel.transform, "panel_result/panel_actions/btn_view_log").GetComponent<Button>();
        Button rematchButton = FindRequiredChild(_activePhasePanel.transform, "panel_result/panel_actions/btn_rematch").GetComponent<Button>();
        Button backLobbyButton = FindRequiredChild(_activePhasePanel.transform, "panel_result/panel_actions/btn_back_lobby").GetComponent<Button>();
        rowsText.text = BuildResultRowsText();

        viewLogButton.onClick.RemoveAllListeners();
        rematchButton.onClick.RemoveAllListeners();
        backLobbyButton.onClick.RemoveAllListeners();
        viewLogButton.onClick.AddListener(() => SetPhaseHint("本局记录面板后续接详细流水"));
        rematchButton.onClick.AddListener(() => SetPhaseHint("再来一局后续回到原房间准备"));
        backLobbyButton.onClick.AddListener(OnClickBackLobbyButton);
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

    private void OnClickClearSelectionButton()
    {
        _selectedCardIds.Clear();
        _faceUpCardId = null;
        RenderHandCards();
        RenderPlayPanel();
    }

    private void BindChallengeChoiceButton(Button button, int targetSeatIndex, int submittedTargetSeatIndex, bool enabled)
    {
        button.onClick.RemoveAllListeners();
        bool submitted = submittedTargetSeatIndex != -2;
        button.interactable = enabled && !submitted;

        if (button.interactable)
        {
            button.onClick.AddListener(() =>
            {
                _selectedChallengeSeatIndex = targetSeatIndex;
                RenderChallengePanel();
            });
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            bool selected = _selectedChallengeSeatIndex == targetSeatIndex;
            image.color = selected
                ? new Color32(244, 190, 86, 255)
                : targetSeatIndex == -1
                    ? new Color32(84, 92, 104, 255)
                    : new Color32(169, 72, 72, 255);
        }
    }

    private void OnClickConfirmChallengeButton()
    {
        if (_selectedChallengeSeatIndex == -2)
        {
            return;
        }

        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.SubmitChallenge(_selectedChallengeSeatIndex);
        }
    }

    private void ChangeFortuneDrawCount(int delta)
    {
        int minDraw = _gameSession?.FortuneState == null ? 2 : _gameSession.FortuneState.min_draw_count;
        int maxDraw = _gameSession?.FortuneState == null ? 5 : _gameSession.FortuneState.max_draw_count;
        _fortuneDrawCount = Mathf.Clamp(_fortuneDrawCount + delta, minDraw, maxDraw);
        RenderFortunePanel();
    }

    private void OnClickSubmitFortuneDrawButton()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.SubmitFortuneDraw(_fortuneDrawCount);
        }
    }

    private void OnClickBackLobbyButton()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.LeaveGame();
            return;
        }

        GameEventSystem.Trigger(EventRoute.SwitchState, new SwitchStateArg("lobby"));
    }

    private void OnClickMenuButton()
    {
        if (_phaseHintText != null)
        {
            _phaseHintText.text = "菜单面板待接入：规则说明、声音设置、返回确认";
        }
    }

    private void SetPhaseHint(string hint)
    {
        if (_phaseHintText != null)
        {
            _phaseHintText.text = hint;
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

    private int GetSeatIndexForDisplay(int displayIndex)
    {
        PlayerRuntime playerRuntime = _gameSession?.GetDisplayPlayerRuntime(displayIndex);
        return playerRuntime == null ? -1 : playerRuntime.SeatIndex;
    }

    private string GetPlayerNameBySeat(int seatIndex)
    {
        PlayerRuntime playerRuntime = _gameSession?.GetPlayerRuntime(seatIndex);
        if (playerRuntime == null)
        {
            return $"座位{seatIndex + 1}";
        }

        return string.IsNullOrEmpty(playerRuntime.Name) ? $"座位{seatIndex + 1}" : playerRuntime.Name;
    }

    private string BuildChallengeStatusText()
    {
        if (_selectedChallengeSeatIndex == -2)
        {
            return "请选择一个质疑结果";
        }

        if (_selectedChallengeSeatIndex == -1)
        {
            return "当前选择：不质疑";
        }

        return $"当前选择：质疑 {GetPlayerNameBySeat(_selectedChallengeSeatIndex)}";
    }

    private string BuildShowdownEventRows(NetworkShowdownEventPayload[] events)
    {
        if (events == null || events.Length == 0)
        {
            return "本轮无人质疑";
        }

        List<string> lines = new List<string>();
        for (int index = 0; index < events.Length; index += 1)
        {
            NetworkShowdownEventPayload showdownEvent = events[index];
            if (showdownEvent == null)
            {
                continue;
            }

            string result = showdownEvent.success ? "成功" : "失败";
            lines.Add($"{index + 1}. {GetPlayerNameBySeat(showdownEvent.target_seat_index)}  {result}");
        }

        return string.Join("\n", lines);
    }

    private string BuildChallengerNames(int[] challengerSeatIndexes)
    {
        if (challengerSeatIndexes == null || challengerSeatIndexes.Length == 0)
        {
            return "-";
        }

        List<string> names = new List<string>();
        foreach (int seatIndex in challengerSeatIndexes)
        {
            names.Add(GetPlayerNameBySeat(seatIndex));
        }

        return string.Join("、", names);
    }

    private Dictionary<int, int> AggregateFortuneDeltas(NetworkShowdownEventPayload[] events)
    {
        Dictionary<int, int> deltasBySeat = new Dictionary<int, int>();
        if (events == null)
        {
            return deltasBySeat;
        }

        foreach (NetworkShowdownEventPayload showdownEvent in events)
        {
            if (showdownEvent?.fortune_deltas == null)
            {
                continue;
            }

            foreach (NetworkFortuneDeltaPayload fortuneDelta in showdownEvent.fortune_deltas)
            {
                if (fortuneDelta == null)
                {
                    continue;
                }

                if (!deltasBySeat.ContainsKey(fortuneDelta.seat_index))
                {
                    deltasBySeat[fortuneDelta.seat_index] = 0;
                }

                deltasBySeat[fortuneDelta.seat_index] += fortuneDelta.delta;
            }
        }

        return deltasBySeat;
    }

    private string FormatFortuneDelta(int delta)
    {
        if (delta == 0)
        {
            return "气运 0";
        }

        string sign = delta > 0 ? "+" : string.Empty;
        return $"气运 {sign}{delta}";
    }

    private Color AccentTextColor(bool positive)
    {
        return positive
            ? new Color32(232, 171, 84, 255)
            : new Color32(255, 112, 92, 255);
    }

    private NetworkShowdownEventPayload FirstShowdownEvent()
    {
        NetworkShowdownEventPayload[] events = _gameSession?.ShowdownState?.events;
        if (events == null || events.Length == 0)
        {
            return null;
        }

        return events[0];
    }

    private string BuildFortunePoolText()
    {
        string[] pool = _gameSession?.LocalPlayerPrivate?.fortune_pool;
        if (pool == null || pool.Length == 0)
        {
            return "气运池等待同步";
        }

        int good = 0;
        int bad = 0;
        foreach (string token in pool)
        {
            if (string.Equals(token, "good", StringComparison.Ordinal))
            {
                good += 1;
            }
            else if (string.Equals(token, "bad", StringComparison.Ordinal))
            {
                bad += 1;
            }
        }

        return $"好运 {good}    霉运 {bad}\n{BuildTokenLine(pool)}";
    }

    private string BuildFortuneResultText(bool submitted)
    {
        NetworkFortuneDrawPayload draw = _gameSession?.LocalPlayerPrivate?.fortune_draw;
        if (!submitted || draw == null || draw.result_tokens == null || draw.result_tokens.Length == 0)
        {
            return "选择数量后开始抽取";
        }

        int good = 0;
        int bad = 0;
        foreach (string token in draw.result_tokens)
        {
            if (string.Equals(token, "good", StringComparison.Ordinal))
            {
                good += 1;
            }
            else if (string.Equals(token, "bad", StringComparison.Ordinal))
            {
                bad += 1;
            }
        }

        return $"抽取结果：好运 {good}，霉运 {bad}";
    }

    private string BuildTokenLine(string[] tokens)
    {
        List<string> labels = new List<string>();
        foreach (string token in tokens)
        {
            labels.Add(string.Equals(token, "good", StringComparison.Ordinal) ? "福" : "霉");
        }

        return string.Join("  ", labels);
    }

    private string BuildResultRowsText()
    {
        NetworkFinalResultRowPayload[] rows = _gameSession?.FinalResult?.results;
        if (rows == null || rows.Length == 0)
        {
            return "等待结算数据";
        }

        List<string> lines = new List<string>();
        foreach (NetworkFinalResultRowPayload row in rows)
        {
            string you = string.Equals(row.player_id, _gameSession.LocalPlayerId, StringComparison.Ordinal) ? "（你）" : "";
            lines.Add($"{row.rank}. {row.name}{you}    霉运 {row.effective_bad_fortune}    福运 {row.final_fortune}    溜3 {row.escaped_three_count}    {row.title}");
        }

        return string.Join("\n", lines);
    }

    private GameObject CreateRuntimeText(Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("txt_runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = new Color32(238, 242, 246, 255);
        tmp.alignment = alignment;
        return textObject;
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
            return "所选牌必须同数字，或至少包含一张3";
        }

        return string.Empty;
    }

    private string BuildDeclarationText(IReadOnlyList<CardDefinition> selectedCards)
    {
        return BuildDeclarationText(selectedCards, _faceUpCardId);
    }

    private string BuildDeclarationText(IReadOnlyList<CardDefinition> selectedCards, string faceUpCardId)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            return "声明：等待选择";
        }

        CardDefinition faceUpCard = null;
        foreach (CardDefinition selectedCard in selectedCards)
        {
            if (string.Equals(selectedCard.CardId, faceUpCardId, StringComparison.Ordinal))
            {
                faceUpCard = selectedCard;
                break;
            }
        }

        string rank = faceUpCard == null ? "?" : faceUpCard.Rank.ToString();
        return $"声明：{selectedCards.Count}张{rank}";
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
        bool revealFace,
        bool showFaceUpMarker)
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
        TMP_Text cardNameText = FindRequiredChild(cardObject.transform, "card_name").GetComponent<TMP_Text>();
        Transform markerTransform = FindRequiredChild(cardObject.transform, "txt_face_up_marker");
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
        cardNameText.text = cardDefinition.Rank.ToString();
        cardNameText.gameObject.SetActive(false);
        markerTransform.gameObject.SetActive(showFaceUpMarker);

        Button cardButton = FindRequiredChild(cardObject.transform, "btn").GetComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.interactable = clickAction != null;
        if (clickAction != null)
        {
            string cardId = cardDefinition.CardId;
            cardButton.onClick.AddListener(() => clickAction(cardId));
        }
    }

    private void ApplyCardLayoutSize(GameObject cardObject, float width, float height)
    {
        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }
    }

    private void ApplyCardSelectionVisual(GameObject cardObject, bool selected, bool faceUp)
    {
        Image image = cardObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = faceUp
                ? new Color32(255, 232, 150, 255)
                : selected ? new Color32(255, 248, 220, 255) : new Color32(238, 232, 212, 255);
        }

        RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += selected && !faceUp ? new Vector2(0f, 14f) : Vector2.zero;
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
