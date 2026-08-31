using System;
using System.Collections.Generic;

public sealed class LobbyModel
{
    private readonly List<RoomSummarySnapshot> _rooms = new List<RoomSummarySnapshot>();

    public IReadOnlyList<RoomSummarySnapshot> Rooms => _rooms;
    public MatchmakingSnapshot MatchmakingState { get; private set; } = MatchmakingSnapshot.Idle(0, 3);
    public bool IsSessionReady { get; private set; }

    public event Action RoomListUpdated;
    public event Action MatchmakingStateUpdated;
    public event Action SessionStateUpdated;

    public void SetSessionReady(bool isReady)
    {
        if (IsSessionReady == isReady)
        {
            return;
        }

        IsSessionReady = isReady;
        SessionStateUpdated?.Invoke();
    }

    public void ApplyRoomList(NetworkRoomSummaryPayload[] roomPayloads)
    {
        _rooms.Clear();
        if (roomPayloads != null)
        {
            foreach (NetworkRoomSummaryPayload roomPayload in roomPayloads)
            {
                if (roomPayload == null)
                {
                    continue;
                }

                _rooms.Add(new RoomSummarySnapshot(
                    roomPayload.room_id,
                    roomPayload.name,
                    roomPayload.owner_name,
                    roomPayload.status,
                    roomPayload.player_count,
                    roomPayload.max_players));
            }
        }

        RoomListUpdated?.Invoke();
    }

    public void ApplyMatchmakingState(NetworkMatchmakingStatePayload payload)
    {
        if (payload == null)
        {
            return;
        }

        MatchmakingState = new MatchmakingSnapshot(
            payload.status,
            payload.current_count,
            payload.required_count);
        MatchmakingStateUpdated?.Invoke();
    }

    public void ResetMatchmaking()
    {
        MatchmakingState = MatchmakingSnapshot.Idle(0, MatchmakingState.RequiredCount);
        MatchmakingStateUpdated?.Invoke();
    }
}

public sealed class RoomSummarySnapshot
{
    public string RoomId { get; }
    public string Name { get; }
    public string OwnerName { get; }
    public string Status { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }

    public RoomSummarySnapshot(
        string roomId,
        string name,
        string ownerName,
        string status,
        int playerCount,
        int maxPlayers)
    {
        RoomId = roomId;
        Name = name;
        OwnerName = ownerName;
        Status = status;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
    }
}

public sealed class MatchmakingSnapshot
{
    public string Status { get; }
    public int CurrentCount { get; }
    public int RequiredCount { get; }
    public bool IsQueued => string.Equals(Status, "queued", StringComparison.Ordinal)
        || string.Equals(Status, "matchmaking", StringComparison.Ordinal);

    public MatchmakingSnapshot(string status, int currentCount, int requiredCount)
    {
        Status = string.IsNullOrEmpty(status) ? "idle" : status;
        CurrentCount = Math.Max(0, currentCount);
        RequiredCount = Math.Max(1, requiredCount);
    }

    public static MatchmakingSnapshot Idle(int currentCount, int requiredCount)
    {
        return new MatchmakingSnapshot("idle", currentCount, requiredCount);
    }
}
