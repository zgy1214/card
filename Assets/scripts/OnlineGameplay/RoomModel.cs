using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RoomModel
{
    private readonly List<RoomPlayerSnapshot> _players = new List<RoomPlayerSnapshot>();

    public string RoomId { get; private set; }
    public string Name { get; private set; }
    public string Status { get; private set; }
    public string OwnerPlayerId { get; private set; }
    public int MaxPlayers { get; private set; } = 3;
    public IReadOnlyList<RoomPlayerSnapshot> Players => _players;
    public bool HasRoom => !string.IsNullOrEmpty(RoomId);

    public event Action RoomStateUpdated;
    public event Action RoomCleared;

    public void ApplyState(NetworkRoomStatePayload payload)
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        RoomId = payload.room_id;
        Name = payload.name;
        Status = payload.status;
        OwnerPlayerId = payload.owner_player_id;
        MaxPlayers = Math.Max(1, payload.max_players);

        _players.Clear();
        if (payload.players != null)
        {
            foreach (NetworkRoomPlayerPayload playerPayload in payload.players)
            {
                if (playerPayload == null)
                {
                    continue;
                }

                _players.Add(new RoomPlayerSnapshot(
                    playerPayload.seat_index,
                    playerPayload.player_id,
                    playerPayload.name,
                    playerPayload.player_type,
                    playerPayload.is_owner,
                    playerPayload.is_ready));
            }
        }

        _players.Sort((left, right) => left.SeatIndex.CompareTo(right.SeatIndex));
        RoomStateUpdated?.Invoke();
    }

    public void Clear()
    {
        if (!HasRoom && _players.Count == 0)
        {
            return;
        }

        RoomId = null;
        Name = null;
        Status = null;
        OwnerPlayerId = null;
        MaxPlayers = 3;
        _players.Clear();
        RoomCleared?.Invoke();
    }

    public RoomPlayerSnapshot GetPlayerBySeat(int seatIndex)
    {
        return _players.FirstOrDefault(player => player.SeatIndex == seatIndex);
    }

    public RoomPlayerSnapshot GetLocalPlayer(string localPlayerId)
    {
        if (string.IsNullOrEmpty(localPlayerId))
        {
            return null;
        }

        return _players.FirstOrDefault(player => string.Equals(player.PlayerId, localPlayerId, StringComparison.Ordinal));
    }

    public bool IsLocalOwner(string localPlayerId)
    {
        return string.Equals(OwnerPlayerId, localPlayerId, StringComparison.Ordinal);
    }

    public bool CanLocalReady(string localPlayerId)
    {
        RoomPlayerSnapshot localPlayer = GetLocalPlayer(localPlayerId);
        return localPlayer != null && localPlayer.IsHuman && !localPlayer.IsOwner;
    }

    public bool CanLocalStartGame(string localPlayerId)
    {
        if (!IsLocalOwner(localPlayerId) || !IsFull())
        {
            return false;
        }

        foreach (RoomPlayerSnapshot player in _players)
        {
            if (player.IsAI || player.IsOwner)
            {
                continue;
            }

            if (!player.IsReady)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsFull()
    {
        return _players.Count >= MaxPlayers;
    }
}

public sealed class RoomPlayerSnapshot
{
    public int SeatIndex { get; }
    public string PlayerId { get; }
    public string Name { get; }
    public string PlayerType { get; }
    public bool IsOwner { get; }
    public bool IsReady { get; }
    public bool IsHuman => string.Equals(PlayerType, "human", StringComparison.Ordinal);
    public bool IsAI => string.Equals(PlayerType, "ai", StringComparison.Ordinal);

    public RoomPlayerSnapshot(
        int seatIndex,
        string playerId,
        string name,
        string playerType,
        bool isOwner,
        bool isReady)
    {
        SeatIndex = seatIndex;
        PlayerId = playerId;
        Name = name;
        PlayerType = playerType;
        IsOwner = isOwner;
        IsReady = isReady;
    }
}
