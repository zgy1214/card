using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Room : UIScript
{
    private readonly List<SeatBinding> _seatBindings = new List<SeatBinding>();

    private TMP_Text _roomNameText;
    private TMP_Text _roomIdText;
    private TMP_Text _ownerNameText;
    private TMP_Text _roomStatusText;
    private Button _leaveButton;
    private Button _readyButton;
    private Button _addAiButton;
    private Button _startGameButton;

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
        _roomNameText = FindRequiredText("root/panel_header/txt_room_name");
        _roomIdText = FindRequiredText("root/panel_header/txt_room_id");
        _ownerNameText = FindRequiredText("root/panel_header/txt_owner_name");
        _roomStatusText = FindRequiredText("root/txt_room_status");
        _leaveButton = FindRequiredButton("root/panel_header/btn_leave");
        _readyButton = FindRequiredButton("root/panel_controls/btn_ready");
        _addAiButton = FindRequiredButton("root/panel_controls/btn_add_ai");
        _startGameButton = FindRequiredButton("root/panel_controls/btn_start_game");

        for (int seatIndex = 0; seatIndex < 3; seatIndex += 1)
        {
            Transform slotTransform = FindRequiredTransform($"root/panel_players/slot_player{seatIndex}");
            _seatBindings.Add(new SeatBinding(
                seatIndex,
                FindRequiredText(slotTransform, "txt_seat"),
                FindRequiredText(slotTransform, "txt_name"),
                FindRequiredText(slotTransform, "txt_player_type"),
                FindRequiredText(slotTransform, "txt_ready"),
                FindRequiredText(slotTransform, "txt_owner"),
                FindRequiredButton(slotTransform, "btn_remove_ai")));
        }

        _leaveButton.onClick.AddListener(OnClickLeave);
        _readyButton.onClick.AddListener(OnClickReady);
        _addAiButton.onClick.AddListener(OnClickAddAI);
        _startGameButton.onClick.AddListener(OnClickStartGame);
    }

    private void SubscribeEvents()
    {
        RoomModel roomModel = GameApp.Current?.OnlineGameController?.RoomModel;
        if (roomModel == null)
        {
            return;
        }

        roomModel.RoomStateUpdated += RenderRoomState;
    }

    private void UnsubscribeEvents()
    {
        RoomModel roomModel = GameApp.Current?.OnlineGameController?.RoomModel;
        if (roomModel == null)
        {
            return;
        }

        roomModel.RoomStateUpdated -= RenderRoomState;
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
        _roomIdText.text = $"房间号 {roomModel.RoomId}";
        _ownerNameText.text = $"房主 {FindOwnerName(roomModel)}";
        _roomStatusText.text = BuildRoomStatusText(roomModel, localPlayer, isLocalOwner);

        _readyButton.gameObject.SetActive(!isLocalOwner);
        _readyButton.interactable = roomModel.CanLocalReady(localPlayerId);
        SetButtonLabel(_readyButton, localPlayer != null && localPlayer.IsReady ? "取消准备" : "准备");

        _addAiButton.gameObject.SetActive(isLocalOwner);
        _addAiButton.interactable = isLocalOwner && !roomModel.IsFull();
        _startGameButton.gameObject.SetActive(isLocalOwner);
        _startGameButton.interactable = roomModel.CanLocalStartGame(localPlayerId);

        foreach (SeatBinding seatBinding in _seatBindings)
        {
            RenderSeat(roomModel, seatBinding, isLocalOwner);
        }
    }

    private void RenderSeat(RoomModel roomModel, SeatBinding seatBinding, bool isLocalOwner)
    {
        RoomPlayerSnapshot player = roomModel.GetPlayerBySeat(seatBinding.SeatIndex);
        seatBinding.SeatText.text = $"座位 {seatBinding.SeatIndex + 1}";
        if (player == null)
        {
            seatBinding.NameText.text = "空位";
            seatBinding.PlayerTypeText.text = "-";
            seatBinding.ReadyText.text = "-";
            seatBinding.OwnerText.text = "";
            seatBinding.RemoveAiButton.gameObject.SetActive(false);
            seatBinding.RemoveAiButton.onClick.RemoveAllListeners();
            return;
        }

        seatBinding.NameText.text = player.Name;
        seatBinding.PlayerTypeText.text = player.IsAI ? "AI" : "玩家";
        seatBinding.ReadyText.text = player.IsOwner ? "房主" : player.IsReady ? "已准备" : "未准备";
        seatBinding.OwnerText.text = player.IsOwner ? "Owner" : "";
        seatBinding.RemoveAiButton.gameObject.SetActive(player.IsAI && isLocalOwner);
        seatBinding.RemoveAiButton.interactable = player.IsAI && isLocalOwner;
        seatBinding.RemoveAiButton.onClick.RemoveAllListeners();
        if (player.IsAI && isLocalOwner)
        {
            int seatIndex = player.SeatIndex;
            seatBinding.RemoveAiButton.onClick.AddListener(() => GameApp.Current?.OnlineGameController?.RemoveAI(seatIndex));
        }
    }

    private string BuildRoomStatusText(RoomModel roomModel, RoomPlayerSnapshot localPlayer, bool isLocalOwner)
    {
        if (!roomModel.IsFull())
        {
            return $"等待玩家加入 {roomModel.Players.Count}/{roomModel.MaxPlayers}";
        }

        if (isLocalOwner)
        {
            return "人数已满，可以在全部真人准备后开始";
        }

        return localPlayer != null && localPlayer.IsReady ? "已准备，等待房主开始" : "人数已满，请准备";
    }

    private string FindOwnerName(RoomModel roomModel)
    {
        foreach (RoomPlayerSnapshot player in roomModel.Players)
        {
            if (player.IsOwner)
            {
                return player.Name;
            }
        }

        return "-";
    }

    private void OnClickLeave()
    {
        GameApp.Current?.OnlineGameController?.LeaveRoom();
    }

    private void OnClickReady()
    {
        OnlineGameController controller = GameApp.Current?.OnlineGameController;
        RoomPlayerSnapshot localPlayer = controller?.RoomModel.GetLocalPlayer(controller.LocalPlayerProfile.PlayerId);
        if (localPlayer == null)
        {
            return;
        }

        controller.SetReady(!localPlayer.IsReady);
    }

    private void OnClickAddAI()
    {
        GameApp.Current?.OnlineGameController?.AddAI();
    }

    private void OnClickStartGame()
    {
        GameApp.Current?.OnlineGameController?.StartRoomGame();
    }

    private void RemoveButtonListeners()
    {
        if (_leaveButton != null)
        {
            _leaveButton.onClick.RemoveListener(OnClickLeave);
        }

        if (_readyButton != null)
        {
            _readyButton.onClick.RemoveListener(OnClickReady);
        }

        if (_addAiButton != null)
        {
            _addAiButton.onClick.RemoveListener(OnClickAddAI);
        }

        if (_startGameButton != null)
        {
            _startGameButton.onClick.RemoveListener(OnClickStartGame);
        }

        foreach (SeatBinding seatBinding in _seatBindings)
        {
            seatBinding.RemoveAiButton.onClick.RemoveAllListeners();
        }
    }

    private void ClearReferences()
    {
        _seatBindings.Clear();
        _roomNameText = null;
        _roomIdText = null;
        _ownerNameText = null;
        _roomStatusText = null;
        _leaveButton = null;
        _readyButton = null;
        _addAiButton = null;
        _startGameButton = null;
    }

    private void SetButtonLabel(Button button, string label)
    {
        TMP_Text labelText = button.transform.Find("txt_label")?.GetComponent<TMP_Text>();
        if (labelText != null)
        {
            labelText.text = label;
        }
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

    private Transform FindRequiredTransform(string path)
    {
        return FindRequiredTransform(ViewTransform, path);
    }

    private Transform FindRequiredTransform(Transform rootTransform, string path)
    {
        Transform targetTransform = rootTransform == null ? null : rootTransform.Find(path);
        if (targetTransform == null)
        {
            throw new System.InvalidOperationException($"Room view cannot find node: {path}");
        }

        return targetTransform;
    }

    private sealed class SeatBinding
    {
        public int SeatIndex { get; }
        public TMP_Text SeatText { get; }
        public TMP_Text NameText { get; }
        public TMP_Text PlayerTypeText { get; }
        public TMP_Text ReadyText { get; }
        public TMP_Text OwnerText { get; }
        public Button RemoveAiButton { get; }

        public SeatBinding(
            int seatIndex,
            TMP_Text seatText,
            TMP_Text nameText,
            TMP_Text playerTypeText,
            TMP_Text readyText,
            TMP_Text ownerText,
            Button removeAiButton)
        {
            SeatIndex = seatIndex;
            SeatText = seatText;
            NameText = nameText;
            PlayerTypeText = playerTypeText;
            ReadyText = readyText;
            OwnerText = ownerText;
            RemoveAiButton = removeAiButton;
        }
    }
}
