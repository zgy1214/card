using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed partial class Game
{
    private readonly FortuneBar[] _fortuneBars = new FortuneBar[4];
    private readonly Transform[] _showdownSeatCards = new Transform[4];
    private readonly Transform[] _caughtMarkers = new Transform[4];
    private readonly Canvas[] _showdownForeground = new Canvas[4];
    private Transform _showdownPanel;
    private Transform _showdownOverview;
    private Transform _showdownFocus;
    private Transform _showdownCards;
    private TMP_Text _showdownName;
    private Transform _showdownMask;
    private readonly Transform[] _showdownChallengers = new Transform[3];
    private NetworkShowdownStatePayload _renderedShowdown;
    private int _showdownStep = int.MinValue;
    private bool _showdownActive;

    private void BindShowdownNodes()
    {
        _showdownPanel = FindRequiredTransform("root/panel_showdown");
        _showdownOverview = _showdownPanel.Find("overview");
        _showdownFocus = _showdownPanel.Find("focus");
        _showdownCards = _previewAreaTransform;
        _showdownMask = _showdownPanel.Find("mask");
        string[] foregroundPaths = { "root/seat_local", "root/panel_local_hand", "root/table/preview_cards", "root/panel_showdown/focus" };
        for (int i = 0; i < foregroundPaths.Length; i++)
            _showdownForeground[i] = FindRequiredTransform(foregroundPaths[i]).GetComponent<Canvas>();
        _showdownName = _showdownFocus.Find("txt_target").GetComponent<TMP_Text>();

        string[] sides = { "local", "left", "top", "right" };
        for (int i = 0; i < 4; i++)
        {

            _fortuneBars[i] = FindRequiredTransform(i == 0 ? "root/panel_local_hand/fortune_bar"
                : "root/seat_" + sides[i] + "/fortune_bar").GetComponent<FortuneBar>();
            _showdownSeatCards[i] = _showdownOverview.Find(sides[i] + "/cards");
            _caughtMarkers[i] = _showdownOverview.Find(sides[i] + "/caught");
        }
        for (int i = 0; i < 3; i++) _showdownChallengers[i] = _showdownFocus.Find("challengers/player_" + i);
    }

    private void CloseShowdown()
    {
        Array.Clear(_fortuneBars, 0, _fortuneBars.Length);
        Array.Clear(_showdownSeatCards, 0, _showdownSeatCards.Length);
        Array.Clear(_caughtMarkers, 0, _caughtMarkers.Length);
        Array.Clear(_showdownChallengers, 0, _showdownChallengers.Length);
        _showdownPanel = _showdownOverview = _showdownFocus = _showdownCards = null;
        _showdownName = null;
        _showdownMask = null;
        Array.Clear(_showdownForeground, 0, _showdownForeground.Length);
        _renderedShowdown = null;
        _showdownStep = int.MinValue;
        _showdownActive = false;
    }

    private void RenderShowdown(bool force = false)
    {
        bool active = _gameSession?.Phase == "showdown";
        SetNodeActive(_showdownPanel, active);
        SetNodeActive(_timerText?.transform.parent, !active);
        if (!active)
        {
            if (force || _showdownActive) SetShowdownFocusLayers(false);
            if (force || _showdownActive) RenderFortuneCounts(null, null);
            if (_showdownActive)
            {
                foreach (Transform cards in _showdownSeatCards) ClearChildren(cards);
                _renderedShowdown = null;
                _showdownStep = int.MinValue;
            }
            _showdownActive = false;
            return;
        }
        NetworkShowdownStatePayload state = _gameSession.ShowdownState;
        NetworkShowdownEventPayload[] events = state.events;
        double now = _gameSession.ServerTime + Time.realtimeSinceStartupAsDouble - _gameSession.StateReceivedRealtime;
        float overview = state.overview_seconds;
        float focus = state.focus_seconds;
        float reveal = state.reveal_seconds;
        float duration = focus + reveal + state.reward_seconds;
        _showdownActive = true;
        double start = state.started_at;
        double elapsed = Math.Max(0, now - start);
        bool isOverview = elapsed < overview || events.Length == 0;
        // Hold the final event during the last-round pause without indexing past the array.
        int index = isOverview ? -1 : (int)Math.Min(events.Length - 1d, Math.Max(0, Math.Floor((elapsed - overview) / duration)));
        double eventTime = isOverview ? 0 : elapsed - overview - index * duration;
        int stage = eventTime < focus ? 0 : eventTime < focus + reveal ? 1 : 2;
        int step = isOverview ? -1 : index * 3 + stage;
        SetNodeActive(_challengeDim, false);
        _challengeTableImage.color = _tableNormalColor;
        SetShowdownFocusLayers(!isOverview);
        SetNodeActive(_previewAreaTransform, !isOverview);
        if (!force && _renderedShowdown == state && _showdownStep == step) return;
        _renderedShowdown = state;
        _showdownStep = step;
        SetNodeActive(_showdownOverview, isOverview);
        SetNodeActive(_showdownFocus, !isOverview);

        if (isOverview)
        {
            RenderFortuneCounts(events.Length > 0 ? events[0].fortune_before : null, null);
            for (int i = 0; i < 4; i++)
            {
                int seat = _gameSession.GetDisplayPlayerRuntime(i)?.SeatIndex ?? -1;
                bool caught = Array.Exists(events, e => e.target_seat_index == seat);
                SetNodeActive(_caughtMarkers[i], caught);
                RenderPublicCards(_showdownSeatCards[i], FindStatePlayer(seat)?.public_play, null, false, 132f, true);
            }
            return;
        }

        NetworkShowdownEventPayload current = events[index];
        NetworkFortuneCountsPayload[] counts = stage == 2 ? current.fortune_after : current.fortune_before;
        NetworkFortuneCountsPayload[] before = stage == 2 ? current.fortune_before : null;
        RenderFortuneCounts(counts, before);
        _showdownName.text = GetDisplayPlayerName(_gameSession.GetPlayerRuntime(current.target_seat_index), "玩家");
        RenderPublicCards(_showdownCards, FindStatePlayer(current.target_seat_index)?.public_play,
            current.revealed_cards, stage >= 1, 300f, false);

        int[] challengers = current.challenger_seat_indexes ?? Array.Empty<int>();
        for (int i = 0; i < _showdownChallengers.Length; i++)
        {
            Transform row = _showdownChallengers[i];
            SetNodeActive(row, i < challengers.Length);
            if (i >= challengers.Length) continue;
            PlayerRuntime player = _gameSession.GetPlayerRuntime(challengers[i]);
            row.Find("txt_name").GetComponent<TMP_Text>().text = GetDisplayPlayerName(player, "玩家");
            Image portrait = row.Find("portrait").GetComponent<Image>();
            portrait.sprite = _characterArtLibrary.GetSprite(GetDisplayCharacterId(player));
            portrait.preserveAspect = true;
            portrait.enabled = portrait.sprite != null;
        }
    }


    private void SetShowdownFocusLayers(bool focused)
    {
        SetNodeActive(_showdownMask, focused);
        SetNodeActive(_showdownFocus, focused);
        int order = ViewGameObject.GetComponent<Canvas>().sortingOrder + 10;
        if (focused)
        {
            Canvas maskCanvas = _showdownMask.GetComponent<Canvas>();
            maskCanvas.overrideSorting = true;
            maskCanvas.sortingOrder = order;
        }
        foreach (Canvas canvas in _showdownForeground)
        {
            if (canvas == null) continue;
            canvas.enabled = true;
            canvas.overrideSorting = focused;
            if (focused) canvas.sortingOrder = order + 1;
        }
    }

    private void RenderFortuneCounts(NetworkFortuneCountsPayload[] counts, NetworkFortuneCountsPayload[] before)
    {
        for (int i = 0; i < 4; i++) ShowFortuneForSeat(_fortuneBars[i],
            _gameSession?.GetDisplayPlayerRuntime(i)?.SeatIndex ?? -1, counts, before);
    }

    private void ShowFortuneForSeat(FortuneBar bar, int seat, NetworkFortuneCountsPayload[] counts, NetworkFortuneCountsPayload[] before)
    {
        if (bar == null) return;
        NetworkFortuneCountsPayload current = counts == null ? null : Array.Find(counts, c => c != null && c.seat_index == seat);
        NetworkFortuneCountsPayload previous = before == null ? null : Array.Find(before, c => c != null && c.seat_index == seat);
        NetworkGameStatePlayerPayload player = FindStatePlayer(seat);
        if (player == null) return; // No player snapshot yet between match_start and game/state.
        if (counts != null && current == null) throw new InvalidOperationException("Showdown fortune snapshot is missing a seat.");
        bar.ShowCounts(current == null ? player.lucky_count : current.lucky_count,
            current == null ? player.unlucky_count : current.unlucky_count, previous?.unlucky_count ?? -1);
    }

    private void RenderPublicCards(Transform area, NetworkPublicPlayPayload play, NetworkCardPayload[] hidden,
        bool revealed, float height, bool compact)
    {
        ClearChildren(area);
        if (play?.HasValue() != true) return;
        int hiddenCount = Mathf.Clamp(play.hidden_count, 0, 47);
        int count = hiddenCount + 1;
        float scale = height / 378f;
        float width = 225f * scale;
        float available = ((RectTransform)area).rect.width;
        float step = count <= 1 ? 0 : Mathf.Min(compact ? width * .35f : width, (available - width) / (count - 1));
        float start = -(width + step * (count - 1)) * .5f + width * .5f;
        // Public face stays in front when overlap is needed. Hidden cards keep their server order.
        for (int i = count - 1; i >= 0; i--)
        {
            NetworkCardPayload payload = i == 0 ? play.face_up_card : revealed && hidden != null && i - 1 < hidden.Length ? hidden[i - 1] : null;
            GameObject card = OpenGameObject(area, CardPrefabPath, "public_card_" + i);
            RectTransform rect = (RectTransform)card.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(225, 378);
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(start + i * step, 0);
            Image face = card.transform.Find("img_card_face").GetComponent<Image>();
            face.sprite = payload == null ? _cardArtLibrary.BackSprite : _cardArtLibrary.GetFaceSprite(payload.card_name);
            face.color = Color.white;
            face.preserveAspect = true;
            if (revealed && payload?.rank == 3)
            {
                Outline highlight = face.gameObject.AddComponent<Outline>();
                highlight.effectColor = new Color(1f, .76f, .15f, 1f);
                highlight.effectDistance = new Vector2(5, 5);
            }
            foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            card.transform.Find("btn").gameObject.SetActive(false);
        }
    }
}
