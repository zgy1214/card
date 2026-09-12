using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Game
{
    private sealed class ChallengeTarget
    {
        public Button Button;
        public RectTransform Cards;
        public GameObject Highlight;
        public GameObject Check;
        public TMP_Text Name;
        public int Seat = -1;
        public string CardsKey;
    }

    private readonly ChallengeTarget[] _challengeTargets = new ChallengeTarget[3];
    private Transform _challengeDim;
    private Image _challengeTableImage;
    private Color _tableNormalColor;
    private RectTransform _challengeTimer;
    private Vector2 _timerNormalPosition;
    private string _challengeMatch;
    private int _challengeRound = -1;
    private int _challengeTargetSeat = -2;
    private bool _challengePending;
    private bool _challengeActive;
    private int _challengeRequestVersion;

    private void BindChallengeNodes()
    {
        _challengeDim = FindRequiredTransform("root/panel_challenge_dim");
        _challengeTableImage = FindRequiredTransform("root/table/img_table").GetComponent<Image>();
        _tableNormalColor = _challengeTableImage.color;
        _challengeTimer = (RectTransform)FindRequiredTransform("root/table/timer");
        _timerNormalPosition = _challengeTimer.anchoredPosition;
        string[] directions = { "left", "top", "right" };
        for (int i = 0; i < directions.Length; i++)
        {
            string path = "root/panel_challenge_targets/challenge_" + directions[i];
            ChallengeTarget target = new ChallengeTarget
            {
                Button = FindRequiredButton(path),
                Cards = (RectTransform)FindRequiredTransform(path + "/preview_cards"),
                Highlight = FindRequiredTransform(path + "/img_selected").gameObject,
                Check = FindRequiredTransform(path + "/target_header/img_check").gameObject,
                Name = FindRequiredText(path + "/target_header/txt_name")
            };
            _challengeTargets[i] = target;
            target.Button.onClick.AddListener(() => SelectChallengeTarget(target));
        }
        _catchButton.onClick.AddListener(OnCatch);
        _letgoButton.onClick.AddListener(OnLetGo);
    }

    private void CloseChallenge()
    {
        _challengeRequestVersion++;
        UnbindButton(_catchButton, OnCatch);
        UnbindButton(_letgoButton, OnLetGo);
        foreach (ChallengeTarget target in _challengeTargets)
            if (target != null) target.Button.onClick.RemoveAllListeners();
        Array.Clear(_challengeTargets, 0, _challengeTargets.Length);
        _challengeDim = null;
        _challengeTableImage = null;
        _challengeTimer = null;
        _challengeActive = false;
        _challengePending = false;
        _challengeRound = -1;
        _challengeMatch = null;
    }

    private void RenderChallenge()
    {
        bool active = _gameSession?.Phase == "challenge_select";
        SetNodeActive(_challengeDim, active);
        _challengeTableImage.color = active
            ? new Color(_tableNormalColor.r * 0.65f, _tableNormalColor.g * 0.65f, _tableNormalColor.b * 0.65f, _tableNormalColor.a)
            : _tableNormalColor;
        _challengeTimer.anchoredPosition = active
            ? new Vector2(_timerNormalPosition.x, ((RectTransform)FindRequiredTransform("root")).rect.height * 0.5f - 15f)
            : _timerNormalPosition;
        for (int i = 1; i < 4; i++)
        {
            SetNodeActive(_playerNameTexts[i], !active);
            SetNodeActive(_playerNameTexts[i].transform.parent.Find("tag_name"), !active);
        }

        if (!active)
        {
            if (_challengeActive)
            {
                _challengeRequestVersion++;
                foreach (ChallengeTarget target in _challengeTargets)
                {
                    ClearChildren(target.Cards);
                    target.CardsKey = null;
                    target.Highlight.SetActive(false);
                    target.Check.SetActive(false);
                }
            }
            _challengeActive = false;
            _challengePending = false;
            return;
        }

        if (!_challengeActive || _challengeRound != _gameSession.RoundIndex || _challengeMatch != _gameSession.MatchId)
        {
            _challengeRequestVersion++;
            _challengeTargetSeat = -2;
            _challengePending = false;
            _challengeRound = _gameSession.RoundIndex;
            _challengeMatch = _gameSession.MatchId;
            foreach (ChallengeTarget target in _challengeTargets) target.CardsKey = null;
        }
        _challengeActive = true;

        for (int i = 0; i < _challengeTargets.Length; i++)
        {
            ChallengeTarget target = _challengeTargets[i];
            PlayerRuntime player = _gameSession.GetDisplayPlayerRuntime(i + 1);
            target.Seat = player?.SeatIndex ?? -1;
            NetworkGameStatePlayerPayload state = FindStatePlayer(target.Seat);
            target.Name.text = GetDisplayPlayerName(player, "玩家");
            RenderChallengeCards(target, state?.public_play);
        }
        RenderChallengeControls();
    }

    private bool ChallengeSubmitted()
    {
        PlayerRuntime local = _gameSession?.GetDisplayPlayerRuntime(0);
        return local != null && FindStatePlayer(local.SeatIndex)?.challenge_submitted == true;
    }

    private bool ChallengeTimeExpired()
    {
        return _gameSession.PhaseEndTime > 0 && _gameSession.ServerTime
            + Time.realtimeSinceStartupAsDouble - _gameSession.StateReceivedRealtime >= _gameSession.PhaseEndTime;
    }

    private bool CanChooseChallenge()
    {
        return _challengeActive && _gameSession?.Phase == "challenge_select"
            && !_challengePending && !ChallengeSubmitted() && !ChallengeTimeExpired();
    }

    private void RenderChallengeControls()
    {
        bool submitted = ChallengeSubmitted();
        if (submitted)
        {
            _challengePending = false;
            _challengeTargetSeat = _gameSession.ChallengeState?.own_target_seat_index ?? -2;
        }
        bool canChoose = CanChooseChallenge();
        bool hasTarget = false;
        foreach (ChallengeTarget target in _challengeTargets)
        {
            bool hasPlay = FindStatePlayer(target.Seat)?.public_play?.HasValue() == true;
            bool selected = hasPlay && target.Seat == _challengeTargetSeat;
            target.Button.interactable = canChoose && hasPlay;
            target.Highlight.SetActive(selected);
            target.Check.SetActive(selected);
            foreach (Transform card in target.Cards)
            {
                Image face = card.Find("img_card_face")?.GetComponent<Image>();
                if (face != null) face.color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
            }
            hasTarget |= selected;
        }
        _catchButton.interactable = canChoose && hasTarget;
        _letgoButton.interactable = canChoose;

    }

    private void SelectChallengeTarget(ChallengeTarget target)
    {
        if (!CanChooseChallenge() || !target.Button.interactable) return;
        _challengeTargetSeat = target.Seat;
        RenderChallengeControls();
    }

    private void OnCatch()
    {
        if (_challengeTargetSeat >= 0 && _catchButton.interactable) SendChallenge(_challengeTargetSeat);
    }

    private void OnLetGo() => SendChallenge(-1);

    private async void SendChallenge(int seat)
    {
        if (!CanChooseChallenge()) return;
        _challengePending = true;
        int requestVersion = ++_challengeRequestVersion;
        RenderChallengeControls();
        try
        {
            OnlineGameController controller = GameApp.Current?.OnlineGameController;
            if (controller == null) throw new InvalidOperationException("服务器尚未连接。");
            await controller.SubmitChallengeAsync(seat);
            // Only game/state acknowledges the action; sending bytes is not confirmation.
        }
        catch (Exception)
        {
            if (requestVersion == _challengeRequestVersion && _challengeActive)
                OnChallengeError("提交失败，请检查连接后重试。");
        }
    }

    private void OnChallengeError(string message)
    {
        if (!_challengeActive || !_challengePending || ChallengeSubmitted()) return;
        _challengePending = false;
        Debug.LogWarning($"Challenge submission failed: {message}");
        RenderChallengeControls();
    }

    private void RenderChallengeCards(ChallengeTarget target, NetworkPublicPlayPayload play)
    {
        string key = play != null && play.HasValue()
            ? $"{target.Seat}:{play.face_up_card.card_id}:{play.hidden_count}" : string.Empty;
        if (target.CardsKey == key) return;
        target.CardsKey = key;
        ClearChildren(target.Cards);
        if (string.IsNullOrEmpty(key)) return;

        const float cardHeight = 341f;
        const float cardScale = cardHeight / 378f;
        const float cardWidth = 225f * cardScale;
        int hiddenCount = Mathf.Clamp(play.hidden_count, 0, 47);
        float spacing = hiddenCount == 0 ? 0 : Mathf.Min(100f, (target.Cards.rect.width - cardWidth) / hiddenCount);
        float startX = -(cardWidth + hiddenCount * spacing) * 0.5f;
        // Build back-to-front so the public face is always drawn above the backs.
        for (int index = hiddenCount; index >= 0; index--)
        {
            GameObject card = OpenGameObject(target.Cards, CardPrefabPath, "public_card_" + index);
            RectTransform rect = (RectTransform)card.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(225f, 378f);
            rect.localScale = Vector3.one * cardScale;
            rect.anchoredPosition = new Vector2(startX + index * spacing, -cardHeight * 0.5f);
            Image face = FindRequiredChild(card.transform, "img_card_face").GetComponent<Image>();
            face.sprite = index == 0 ? _cardArtLibrary.GetFaceSprite(play.face_up_card.card_name) : _cardArtLibrary.BackSprite;
            face.color = Color.white;
            foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            FindRequiredChild(card.transform, "btn").gameObject.SetActive(false);
        }
    }
}
