using System;
using UnityEngine;

[Serializable]
public sealed class NetworkMessageTypeEnvelope
{
    public string type;
}

[Serializable]
public sealed class NetworkEmptyPayload
{
}

[Serializable]
public sealed class NetworkSessionHelloMessage
{
    public string type = NetworkProtocolMessages.SessionHello;
    public NetworkSessionHelloPayload payload;
}

[Serializable]
public sealed class NetworkSessionHelloPayload
{
    public string player_id;
    public string name;
    public string client_version;
}

[Serializable]
public sealed class NetworkSessionHelloAckMessage
{
    public string type;
    public NetworkSessionHelloAckPayload payload;
}

[Serializable]
public sealed class NetworkSessionHelloAckPayload
{
    public string player_id;
    public string name;
}

[Serializable]
public sealed class NetworkRoomListRequestMessage
{
    public string type = NetworkProtocolMessages.RoomList;
    public NetworkEmptyPayload payload = new NetworkEmptyPayload();
}

[Serializable]
public sealed class NetworkRoomListResultMessage
{
    public string type;
    public NetworkRoomListResultPayload payload;
}

[Serializable]
public sealed class NetworkRoomListResultPayload
{
    public NetworkRoomSummaryPayload[] rooms;
}

[Serializable]
public sealed class NetworkRoomSummaryPayload
{
    public string room_id;
    public string name;
    public string owner_name;
    public string status;
    public int player_count;
    public int max_players;
}

[Serializable]
public sealed class NetworkRoomCreateRequestMessage
{
    public string type = NetworkProtocolMessages.RoomCreate;
    public NetworkRoomCreateRequestPayload payload;
}

[Serializable]
public sealed class NetworkRoomCreateRequestPayload
{
    public string name;
}

[Serializable]
public sealed class NetworkRoomJoinRequestMessage
{
    public string type = NetworkProtocolMessages.RoomJoin;
    public NetworkRoomIdPayload payload;
}

[Serializable]
public sealed class NetworkRoomLeaveRequestMessage
{
    public string type = NetworkProtocolMessages.RoomLeave;
    public NetworkRoomIdPayload payload;
}

[Serializable]
public sealed class NetworkRoomIdPayload
{
    public string room_id;
}

[Serializable]
public sealed class NetworkRoomReadyRequestMessage
{
    public string type = NetworkProtocolMessages.RoomReady;
    public NetworkRoomReadyRequestPayload payload;
}

[Serializable]
public sealed class NetworkRoomReadyRequestPayload
{
    public string room_id;
    public bool is_ready;
}

[Serializable]
public sealed class NetworkRoomAddAiRequestMessage
{
    public string type = NetworkProtocolMessages.RoomAddAi;
    public NetworkRoomIdPayload payload;
}

[Serializable]
public sealed class NetworkRoomRemoveAiRequestMessage
{
    public string type = NetworkProtocolMessages.RoomRemoveAi;
    public NetworkRoomRemoveAiRequestPayload payload;
}

[Serializable]
public sealed class NetworkRoomRemoveAiRequestPayload
{
    public string room_id;
    public int seat_index;
}

[Serializable]
public sealed class NetworkRoomStartGameRequestMessage
{
    public string type = NetworkProtocolMessages.RoomStartGame;
    public NetworkRoomIdPayload payload;
}

[Serializable]
public sealed class NetworkRoomStateMessage
{
    public string type;
    public NetworkRoomStatePayload payload;
}

[Serializable]
public sealed class NetworkRoomStatePayload
{
    public string room_id;
    public string name;
    public string status;
    public string owner_player_id;
    public int max_players;
    public NetworkRoomPlayerPayload[] players;
}

[Serializable]
public sealed class NetworkRoomPlayerPayload
{
    public int seat_index;
    public string player_id;
    public string name;
    public string player_type;
    public bool is_owner;
    public bool is_ready;
}

[Serializable]
public sealed class NetworkMatchmakingStartRequestMessage
{
    public string type = NetworkProtocolMessages.MatchmakingStart;
    public NetworkEmptyPayload payload = new NetworkEmptyPayload();
}

[Serializable]
public sealed class NetworkMatchmakingCancelRequestMessage
{
    public string type = NetworkProtocolMessages.MatchmakingCancel;
    public NetworkEmptyPayload payload = new NetworkEmptyPayload();
}

[Serializable]
public sealed class NetworkMatchmakingStateMessage
{
    public string type;
    public NetworkMatchmakingStatePayload payload;
}

[Serializable]
public sealed class NetworkMatchmakingStatePayload
{
    public string status;
    public int current_count;
    public int required_count;
}

[Serializable]
public sealed class NetworkMatchmakingFoundMessage
{
    public string type;
    public NetworkMatchmakingFoundPayload payload;
}

[Serializable]
public sealed class NetworkMatchmakingFoundPayload
{
    public string match_id;
}

[Serializable]
public sealed class NetworkPlayCardRequestMessage
{
    public string type = NetworkProtocolMessages.GamePlayCard;
    public NetworkPlayCardRequestPayload payload;
}

[Serializable]
public sealed class NetworkPlayCardRequestPayload
{
    public string match_id;
    public string card_id;
}

[Serializable]
public sealed class NetworkGameLeaveRequestMessage
{
    public string type = NetworkProtocolMessages.GameLeave;
    public NetworkGameLeaveRequestPayload payload;
}

[Serializable]
public sealed class NetworkGameLeaveRequestPayload
{
    public string match_id;
}

[Serializable]
public sealed class NetworkMatchStartMessage
{
    public string type;
    public NetworkMatchStartPayload payload;
}

[Serializable]
public sealed class NetworkMatchStartPayload
{
    public string match_id;
}

[Serializable]
public sealed class NetworkMatchStateMessage
{
    public string type;
    public NetworkMatchStatePayload payload;
}

[Serializable]
public sealed class NetworkMatchStatePayload
{
    public string match_id;
    public string match_status;
    public int turn;
    public int current_seat_index;
    public NetworkCardPayload current_played_card;
    public NetworkPlayerStatePayload[] players;
}

[Serializable]
public sealed class NetworkPlayerStatePayload
{
    public int seat_index;
    public string player_id;
    public string name;
    public string player_type;
    public int hand_count;
    public NetworkCardPayload[] hand_cards;
}

[Serializable]
public sealed class NetworkTurnStartMessage
{
    public string type;
    public NetworkTurnStartPayload payload;
}

[Serializable]
public sealed class NetworkTurnStartPayload
{
    public string match_id;
    public int turn;
    public int current_seat_index;
    public int remaining_seconds;
}

[Serializable]
public sealed class NetworkCardPlayedMessage
{
    public string type;
    public NetworkCardPlayedPayload payload;
}

[Serializable]
public sealed class NetworkCardPlayedPayload
{
    public string match_id;
    public int seat_index;
    public NetworkCardPayload card;
}

[Serializable]
public sealed class NetworkPlayerReplacedByAiMessage
{
    public string type;
    public NetworkPlayerReplacedByAiPayload payload;
}

[Serializable]
public sealed class NetworkPlayerReplacedByAiPayload
{
    public string match_id;
    public int seat_index;
    public string player_id;
    public string ai_player_id;
}

[Serializable]
public sealed class NetworkMatchEndMessage
{
    public string type;
    public NetworkMatchEndPayload payload;
}

[Serializable]
public sealed class NetworkMatchEndPayload
{
    public string match_id;
    public string match_status;
    public int winner_seat_index;
}

[Serializable]
public sealed class NetworkErrorMessage
{
    public string type;
    public NetworkErrorPayload payload;
}

[Serializable]
public sealed class NetworkErrorPayload
{
    public string scope;
    public string code;
    public string message;
}

[Serializable]
public sealed class NetworkCardPayload
{
    public string card_id;
    public string card_name;

    public bool HasValue()
    {
        return !string.IsNullOrEmpty(card_id) && !string.IsNullOrEmpty(card_name);
    }

    public CardDefinition ToCardDefinition()
    {
        if (!HasValue())
        {
            throw new InvalidOperationException("Card payload is incomplete.");
        }

        return new CardDefinition(card_id, card_name, 0);
    }
}

public static class NetworkProtocolMessages
{
    public const string SessionHello = "session/hello";
    public const string SessionHelloAck = "session/hello_ack";
    public const string RoomList = "room/list";
    public const string RoomListResult = "room/list_result";
    public const string RoomCreate = "room/create";
    public const string RoomJoin = "room/join";
    public const string RoomLeave = "room/leave";
    public const string RoomReady = "room/ready";
    public const string RoomAddAi = "room/add_ai";
    public const string RoomRemoveAi = "room/remove_ai";
    public const string RoomStartGame = "room/start_game";
    public const string RoomState = "room/state";
    public const string MatchmakingStart = "matchmaking/start";
    public const string MatchmakingCancel = "matchmaking/cancel";
    public const string MatchmakingState = "matchmaking/state";
    public const string MatchmakingFound = "matchmaking/found";
    public const string GamePlayCard = "game/play_card";
    public const string GameLeave = "game/leave";
    public const string GameMatchStart = "game/match_start";
    public const string GameMatchState = "game/match_state";
    public const string GameTurnStart = "game/turn_start";
    public const string GameCardPlayed = "game/card_played";
    public const string GamePlayerReplacedByAi = "game/player_replaced_by_ai";
    public const string GameMatchEnd = "game/match_end";
    public const string Error = "error";

    private const string ClientVersion = "0.1.0";

    public static string SerializeSessionHello(string playerId, string name)
    {
        return JsonUtility.ToJson(new NetworkSessionHelloMessage
        {
            payload = new NetworkSessionHelloPayload
            {
                player_id = playerId,
                name = name,
                client_version = ClientVersion
            }
        });
    }

    public static string SerializeRoomListRequest()
    {
        return JsonUtility.ToJson(new NetworkRoomListRequestMessage());
    }

    public static string SerializeRoomCreateRequest(string roomName)
    {
        return JsonUtility.ToJson(new NetworkRoomCreateRequestMessage
        {
            payload = new NetworkRoomCreateRequestPayload
            {
                name = roomName
            }
        });
    }

    public static string SerializeRoomJoinRequest(string roomId)
    {
        return JsonUtility.ToJson(new NetworkRoomJoinRequestMessage
        {
            payload = new NetworkRoomIdPayload
            {
                room_id = roomId
            }
        });
    }

    public static string SerializeRoomLeaveRequest(string roomId)
    {
        return JsonUtility.ToJson(new NetworkRoomLeaveRequestMessage
        {
            payload = new NetworkRoomIdPayload
            {
                room_id = roomId
            }
        });
    }

    public static string SerializeRoomReadyRequest(string roomId, bool isReady)
    {
        return JsonUtility.ToJson(new NetworkRoomReadyRequestMessage
        {
            payload = new NetworkRoomReadyRequestPayload
            {
                room_id = roomId,
                is_ready = isReady
            }
        });
    }

    public static string SerializeRoomAddAiRequest(string roomId)
    {
        return JsonUtility.ToJson(new NetworkRoomAddAiRequestMessage
        {
            payload = new NetworkRoomIdPayload
            {
                room_id = roomId
            }
        });
    }

    public static string SerializeRoomRemoveAiRequest(string roomId, int seatIndex)
    {
        return JsonUtility.ToJson(new NetworkRoomRemoveAiRequestMessage
        {
            payload = new NetworkRoomRemoveAiRequestPayload
            {
                room_id = roomId,
                seat_index = seatIndex
            }
        });
    }

    public static string SerializeRoomStartGameRequest(string roomId)
    {
        return JsonUtility.ToJson(new NetworkRoomStartGameRequestMessage
        {
            payload = new NetworkRoomIdPayload
            {
                room_id = roomId
            }
        });
    }

    public static string SerializeMatchmakingStartRequest()
    {
        return JsonUtility.ToJson(new NetworkMatchmakingStartRequestMessage());
    }

    public static string SerializeMatchmakingCancelRequest()
    {
        return JsonUtility.ToJson(new NetworkMatchmakingCancelRequestMessage());
    }

    public static string SerializePlayCardRequest(string matchId, string cardId)
    {
        return JsonUtility.ToJson(new NetworkPlayCardRequestMessage
        {
            payload = new NetworkPlayCardRequestPayload
            {
                match_id = matchId,
                card_id = cardId
            }
        });
    }

    public static string SerializeGameLeaveRequest(string matchId)
    {
        return JsonUtility.ToJson(new NetworkGameLeaveRequestMessage
        {
            payload = new NetworkGameLeaveRequestPayload
            {
                match_id = matchId
            }
        });
    }

    public static bool TryGetMessageType(string rawMessage, out string messageType)
    {
        messageType = null;
        if (string.IsNullOrEmpty(rawMessage))
        {
            return false;
        }

        NetworkMessageTypeEnvelope envelope = JsonUtility.FromJson<NetworkMessageTypeEnvelope>(rawMessage);
        if (envelope == null || string.IsNullOrEmpty(envelope.type))
        {
            return false;
        }

        messageType = envelope.type;
        return true;
    }
}
