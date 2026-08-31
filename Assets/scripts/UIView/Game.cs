using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Game : UIScript
{
    private const string CardPrefabPath = "prefabs/template/card";

    private Button _closeButton;
    private Transform _gameCardAreaTransform;
    private Transform _gamePlayer0CardTransform;
    private TMP_Text _gamePlayer1CardCountText;
    private TMP_Text _gamePlayer2CardCountText;
    private TMP_Text _gamePlayer0NameText;
    private TMP_Text _gamePlayer1NameText;
    private TMP_Text _gamePlayer2NameText;
    private TMP_Text _gamePlayer0TimerText;
    private TMP_Text _gamePlayer1TimerText;
    private TMP_Text _gamePlayer2TimerText;
    private GameSession _gameSession;

    public override string GetPath()
    {
        return "prefabs/view/Game";
    }

    public override void OnOpen()
    {
        GameViewArg gameGameViewArg = this.GameViewArg as GameViewArg;
        if (gameGameViewArg == null)
        {
            throw new System.ArgumentException("Game view requires GameViewArg.");
        }

        _gameSession = gameGameViewArg.GameSession;
        BindNodes();
        SubscribeGameEvents();
        RenderHandState();
        RenderPlayedCardState();
        RefreshTurnVisualState();
    }

    public override void OnClose()
    {
        UnsubscribeGameEvents();

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _closeButton = null;
        }

        _gameCardAreaTransform = null;
        _gamePlayer0CardTransform = null;
        _gamePlayer1CardCountText = null;
        _gamePlayer2CardCountText = null;
        _gamePlayer0NameText = null;
        _gamePlayer1NameText = null;
        _gamePlayer2NameText = null;
        _gamePlayer0TimerText = null;
        _gamePlayer1TimerText = null;
        _gamePlayer2TimerText = null;
        _gameSession = null;
    }

    private void BindNodes()
    {
        _gamePlayer0CardTransform = FindRequiredTransform("root/panel_local/card_player0");
        _gameCardAreaTransform = FindRequiredTransform("root/card_area");
        _gamePlayer1CardCountText = FindRequiredText("root/card_player1/card_num/txt_card_num");
        _gamePlayer2CardCountText = FindRequiredText("root/card_player2/card_num/txt_card_num");
        _gamePlayer0NameText = FindRequiredText("root/panel_local/panel_local_header/txt_player0_name");
        _gamePlayer1NameText = FindRequiredText("root/card_player1/txt_player1_name");
        _gamePlayer2NameText = FindRequiredText("root/card_player2/txt_player2_name");
        _gamePlayer0TimerText = FindRequiredText("root/panel_local/panel_local_header/txt_timer0");
        _gamePlayer1TimerText = FindRequiredText("root/card_player1/txt_timer1");
        _gamePlayer2TimerText = FindRequiredText("root/card_player2/txt_timer2");

        Transform closeButtonTransform = FindRequiredTransform("root/panel_top_bar/btn_close");
        _closeButton = closeButtonTransform.GetComponent<Button>();
        if (_closeButton == null)
        {
            throw new System.InvalidOperationException("Game view cannot find Button on btn_close.");
        }

        _closeButton.onClick.AddListener(OnClickCloseButton);
    }

    private void SubscribeGameEvents()
    {
        if (_gameSession == null)
        {
            return;
        }

        _gameSession.GameStarted += OnGameStarted;
        _gameSession.MatchStateUpdated += OnMatchStateUpdated;
        _gameSession.HandCardPlayed += OnHandCardPlayed;

        if (_gameSession.GameTurnManager == null)
        {
            return;
        }

        _gameSession.GameTurnManager.TurnStarted += OnTurnStarted;
        _gameSession.GameTurnManager.TurnTicked += OnTurnTicked;
        _gameSession.GameTurnManager.TurnEnded += OnTurnEnded;
    }

    private void UnsubscribeGameEvents()
    {
        if (_gameSession == null)
        {
            return;
        }

        _gameSession.GameStarted -= OnGameStarted;
        _gameSession.MatchStateUpdated -= OnMatchStateUpdated;
        _gameSession.HandCardPlayed -= OnHandCardPlayed;

        if (_gameSession.GameTurnManager == null)
        {
            return;
        }

        _gameSession.GameTurnManager.TurnStarted -= OnTurnStarted;
        _gameSession.GameTurnManager.TurnTicked -= OnTurnTicked;
        _gameSession.GameTurnManager.TurnEnded -= OnTurnEnded;
    }

    private void OnGameStarted()
    {
        RenderHandState();
        RenderPlayedCardState();
        RefreshTurnVisualState();
    }

    private void OnMatchStateUpdated()
    {
        RenderHandState();
        RenderPlayedCardState();
        RefreshTurnVisualState();
    }

    private void OnHandCardPlayed(PlayerRuntime gamePlayerRuntime, CardDefinition playedCardDefinition)
    {
        RenderPlayedCardState();
    }

    private void OnTurnStarted()
    {
        RefreshTurnVisualState();
    }

    private void OnTurnTicked()
    {
        RenderTurnTimer();
    }

    private void OnTurnEnded()
    {
        HideAllTimers();
        UpdateLocalHandInteraction();
    }

    private void RenderHandState()
    {
        if (_gameSession == null)
        {
            return;
        }

        PlayerRuntime displayPlayer0Runtime = _gameSession.GetDisplayPlayerRuntime(0);
        PlayerRuntime displayPlayer1Runtime = _gameSession.GetDisplayPlayerRuntime(1);
        PlayerRuntime displayPlayer2Runtime = _gameSession.GetDisplayPlayerRuntime(2);
        if (displayPlayer0Runtime == null)
        {
            ClearOpenedChildren(_gamePlayer0CardTransform);
            RenderPlayerLabels(null, null, null);
            return;
        }

        ClearOpenedChildren(_gamePlayer0CardTransform);
        RenderPlayerLabels(displayPlayer0Runtime, displayPlayer1Runtime, displayPlayer2Runtime);
        _gamePlayer1CardCountText.text = displayPlayer1Runtime == null ? "0" : displayPlayer1Runtime.HandCardCount.ToString();
        _gamePlayer2CardCountText.text = displayPlayer2Runtime == null ? "0" : displayPlayer2Runtime.HandCardCount.ToString();

        for (int index = 0; index < displayPlayer0Runtime.GameHandCardDefinitions.Count; index += 1)
        {
            CardDefinition gameCardDefinition = displayPlayer0Runtime.GameHandCardDefinitions[index];
            RenderPlayer0HandCard(gameCardDefinition, index);
        }
    }

    private void RenderPlayerLabels(
        PlayerRuntime displayPlayer0Runtime,
        PlayerRuntime displayPlayer1Runtime,
        PlayerRuntime displayPlayer2Runtime)
    {
        _gamePlayer0NameText.text = BuildPlayerLabel(displayPlayer0Runtime, "你");
        _gamePlayer1NameText.text = BuildPlayerLabel(displayPlayer1Runtime, "玩家 2");
        _gamePlayer2NameText.text = BuildPlayerLabel(displayPlayer2Runtime, "玩家 3");
    }

    private string BuildPlayerLabel(PlayerRuntime playerRuntime, string fallback)
    {
        if (playerRuntime == null)
        {
            return fallback;
        }

        string name = string.IsNullOrEmpty(playerRuntime.Name) ? fallback : playerRuntime.Name;
        return playerRuntime.IsLocalPlayer ? $"{name}（你）" : name;
    }

    private void RenderPlayedCardState()
    {
        ClearOpenedChildren(_gameCardAreaTransform);

        if (_gameSession == null || _gameSession.CurrentPlayedCardDefinition == null)
        {
            return;
        }

        GameObject cardGameObject = OpenGameObject(
            _gameCardAreaTransform,
            CardPrefabPath,
            "played_card");
        BindCardGameObject(cardGameObject, _gameSession.CurrentPlayedCardDefinition, null);
    }

    private void RenderPlayer0HandCard(CardDefinition gameCardDefinition, int index)
    {
        if (gameCardDefinition == null)
        {
            throw new System.ArgumentNullException(nameof(gameCardDefinition));
        }

        GameObject cardGameObject = OpenGameObject(
            _gamePlayer0CardTransform,
            CardPrefabPath,
            $"player0_card_{index}");
        BindCardGameObject(cardGameObject, gameCardDefinition, OnClickPlayer0HandCard);
    }

    private void BindCardGameObject(
        GameObject cardGameObject,
        CardDefinition gameCardDefinition,
        System.Action<string> onClickCardAction)
    {
        if (cardGameObject == null)
        {
            throw new System.ArgumentNullException(nameof(cardGameObject));
        }

        if (gameCardDefinition == null)
        {
            throw new System.ArgumentNullException(nameof(gameCardDefinition));
        }

        Transform cardNameTransform = cardGameObject.transform.Find("card_name");
        if (cardNameTransform == null)
        {
            throw new System.InvalidOperationException("Card prefab cannot find card_name node.");
        }

        TMP_Text cardNameText = cardNameTransform.GetComponent<TMP_Text>();
        if (cardNameText == null)
        {
            throw new System.InvalidOperationException("Card prefab cannot find TMP_Text on card_name.");
        }

        cardNameText.text = gameCardDefinition.CardName;

        Button cardButton = FindCardButton(cardGameObject.transform);
        cardButton.onClick.RemoveAllListeners();
        bool hasClickAction = onClickCardAction != null;
        cardButton.interactable = CanLocalPlayerInteractWithCards() && hasClickAction;

        if (!hasClickAction)
        {
            return;
        }

        string cardId = gameCardDefinition.CardId;
        cardButton.onClick.AddListener(() => onClickCardAction(cardId));
    }

    private bool CanLocalPlayerInteractWithCards()
    {
        if (_gameSession?.GameTurnManager == null)
        {
            return false;
        }

        PlayerRuntime currentPlayerRuntime = _gameSession.GameTurnManager.CurrentPlayerRuntime;
        return _gameSession.GameTurnManager.IsTurnActive
            && currentPlayerRuntime != null
            && currentPlayerRuntime.IsLocalPlayer;
    }

    private void RefreshTurnVisualState()
    {
        UpdateLocalHandInteraction();
        RenderTurnTimer();
    }

    private void UpdateLocalHandInteraction()
    {
        if (_gamePlayer0CardTransform == null)
        {
            return;
        }

        bool canInteract = CanLocalPlayerInteractWithCards();
        for (int index = 0; index < _gamePlayer0CardTransform.childCount; index += 1)
        {
            Transform cardTransform = _gamePlayer0CardTransform.GetChild(index);
            Button cardButton = FindCardButton(cardTransform);
            cardButton.interactable = canInteract;
        }
    }

    private void RenderTurnTimer()
    {
        HideAllTimers();

        if (_gameSession?.GameTurnManager == null || !_gameSession.GameTurnManager.IsTurnActive)
        {
            return;
        }

        PlayerRuntime currentPlayerRuntime = _gameSession.GameTurnManager.CurrentPlayerRuntime;
        if (currentPlayerRuntime == null)
        {
            return;
        }

        TMP_Text currentTimerText = GetTimerText(_gameSession.GetDisplayIndexForSeat(currentPlayerRuntime.SeatIndex));
        if (currentTimerText == null)
        {
            return;
        }

        currentTimerText.gameObject.SetActive(true);
        currentTimerText.text = _gameSession.GameTurnManager.CurrentTurnRemainingSeconds.ToString();
    }

    private void HideAllTimers()
    {
        SetTimerVisible(_gamePlayer0TimerText, false);
        SetTimerVisible(_gamePlayer1TimerText, false);
        SetTimerVisible(_gamePlayer2TimerText, false);
    }

    private TMP_Text GetTimerText(int displayIndex)
    {
        switch (displayIndex)
        {
            case 0:
                return _gamePlayer0TimerText;
            case 1:
                return _gamePlayer1TimerText;
            case 2:
                return _gamePlayer2TimerText;
            default:
                return null;
        }
    }

    private void SetTimerVisible(TMP_Text timerText, bool isVisible)
    {
        if (timerText == null)
        {
            return;
        }

        timerText.gameObject.SetActive(isVisible);
    }

    private Button FindCardButton(Transform cardTransform)
    {
        if (cardTransform == null)
        {
            throw new System.ArgumentNullException(nameof(cardTransform));
        }

        Transform buttonTransform = cardTransform.Find("btn");
        if (buttonTransform == null)
        {
            throw new System.InvalidOperationException("Card prefab cannot find btn node.");
        }

        Button cardButton = buttonTransform.GetComponent<Button>();
        if (cardButton == null)
        {
            throw new System.InvalidOperationException("Card prefab cannot find Button on btn.");
        }

        return cardButton;
    }

    private void OnClickPlayer0HandCard(string cardId)
    {
        GameApp.Current?.OnlineGameController?.PlayCard(cardId);
    }

    private void ClearOpenedChildren(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            return;
        }

        List<GameObject> childGameObjects = new List<GameObject>();
        for (int index = 0; index < parentTransform.childCount; index += 1)
        {
            childGameObjects.Add(parentTransform.GetChild(index).gameObject);
        }

        foreach (GameObject childGameObject in childGameObjects)
        {
            UnityEngine.Object.Destroy(childGameObject);
        }
    }

    private TMP_Text FindRequiredText(string path)
    {
        Transform targetTransform = FindRequiredTransform(path);
        TMP_Text gameText = targetTransform.GetComponent<TMP_Text>();
        if (gameText == null)
        {
            throw new System.InvalidOperationException($"Game view cannot find TMP_Text at path: {path}");
        }

        return gameText;
    }

    private Transform FindRequiredTransform(string path)
    {
        Transform targetTransform = ViewTransform.Find(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Game view cannot find node: {path}");
        }

        return targetTransform;
    }

    private void OnClickCloseButton()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        if (controller != null)
        {
            controller.LeaveGame();
            return;
        }

        GameEventSystem.Trigger(
            EventRoute.SwitchState,
            new SwitchStateArg("lobby"));
    }
}
