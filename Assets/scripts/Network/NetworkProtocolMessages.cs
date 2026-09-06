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
    public string character_id;
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
    public string character_id;
}

[Serializable]
public sealed class NetworkProfileSetCharacterRequestMessage
{
    public string type = NetworkProtocolMessages.ProfileSetCharacter;
    public NetworkProfileSetCharacterRequestPayload payload;
}

[Serializable]
public sealed class NetworkProfileSetCharacterRequestPayload
{
    public string character_id;
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
    public string character_id;
    public string player_type;
    public bool is_owner;
    public bool is_ready;
}

[Serializable]
public sealed class NetworkRoomChatRequestMessage
{
    public string type = NetworkProtocolMessages.RoomChat;
    public NetworkRoomChatRequestPayload payload;
}

[Serializable]
public sealed class NetworkRoomChatRequestPayload
{
    public string room_id;
    public string message;
}

[Serializable]
public sealed class NetworkRoomChatMessage
{
    public string type;
    public NetworkRoomChatPayload payload;
}

[Serializable]
public sealed class NetworkRoomChatPayload
{
    public string room_id;
    public string player_id;
    public string name;
    public string character_id;
    public string message;
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
public sealed class NetworkSubmitPlayRequestMessage
{
    public string type = NetworkProtocolMessages.GameSubmitPlay;
    public NetworkSubmitPlayRequestPayload payload;
}

[Serializable]
public sealed class NetworkSubmitPlayRequestPayload
{
    public string match_id;
    public string[] card_ids;
    public string face_up_card_id;
}

[Serializable]
public sealed class NetworkSubmitChallengeRequestMessage
{
    public string type = NetworkProtocolMessages.GameSubmitChallenge;
    public NetworkSubmitChallengeRequestPayload payload;
}

[Serializable]
public sealed class NetworkSubmitChallengeRequestPayload
{
    public string match_id;
    public int target_seat_index;
}

[Serializable]
public sealed class NetworkSubmitFortuneDrawRequestMessage
{
    public string type = NetworkProtocolMessages.GameSubmitFortuneDraw;
    public NetworkSubmitFortuneDrawRequestPayload payload;
}

[Serializable]
public sealed class NetworkSubmitFortuneDrawRequestPayload
{
    public string match_id;
    public int draw_count;
}

[Serializable]
public sealed class NetworkGameChatRequestMessage
{
    public string type = NetworkProtocolMessages.GameChat;
    public NetworkGameChatRequestPayload payload;
}

[Serializable]
public sealed class NetworkGameChatRequestPayload
{
    public string match_id;
    public string message;
}

[Serializable]
public sealed class NetworkGameChatMessage
{
    public string type;
    public NetworkGameChatPayload payload;
}

[Serializable]
public sealed class NetworkGameChatPayload
{
    public string match_id;
    public string player_id;
    public string name;
    public string character_id;
    public string message;
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
public sealed class NetworkGameStateMessage
{
    public string type;
    public NetworkGameStatePayload payload;
}

[Serializable]
public sealed class NetworkGameStatePayload
{
    public string match_id;
    public string match_status;
    public string phase;
    public int round_index;
    public double server_time;
    public double phase_end_time;
    public NetworkGameStatePlayerPayload[] players;
    public NetworkLocalPlayerPrivatePayload local_player_private;
    public NetworkRoundPublicPayload round_public;
    public NetworkChallengeStatePayload challenge_state;
    public NetworkShowdownStatePayload showdown_state;
    public NetworkFortuneStatePayload fortune_state;
    public NetworkFinalResultPayload final_result;
}

[Serializable]
public sealed class NetworkGameStatePlayerPayload
{
    public int seat_index;
    public string player_id;
    public string name;
    public string player_type;
    public string character_id;
    public int hand_count;
    public bool play_submitted;
    public bool challenge_submitted;
    public bool fortune_draw_submitted;
    public NetworkPublicPlayPayload public_play;
}

[Serializable]
public sealed class NetworkPublicPlayPayload
{
    public int seat_index;
    public int card_count;
    public int hidden_count;
    public NetworkCardPayload face_up_card;
    public string declaration;

    public bool HasValue()
    {
        return !string.IsNullOrEmpty(declaration) && face_up_card != null && face_up_card.HasValue();
    }
}

[Serializable]
public sealed class NetworkLocalPlayerPrivatePayload
{
    public int seat_index;
    public NetworkCardPayload[] hand_cards;
    public string[] fortune_pool;
    public NetworkFortuneDrawPayload fortune_draw;
    public NetworkSubmittedPlayPayload submitted_play;
}

[Serializable]
public sealed class NetworkSubmittedPlayPayload
{
    public string[] card_ids;
    public string face_up_card_id;
    public NetworkCardPayload[] cards;
}

[Serializable]
public sealed class NetworkRoundPublicPayload
{
    public int pair_count;
    public int pair_contains_three_count;
    public int[] escaped_three_history;
}

[Serializable]
public sealed class NetworkChallengeStatePayload
{
    public int[] submitted_seat_indexes;
    public int own_target_seat_index;
}

[Serializable]
public sealed class NetworkShowdownStatePayload
{
    public NetworkShowdownEventPayload[] events;
}

[Serializable]
public sealed class NetworkShowdownEventPayload
{
    public int target_seat_index;
    public int[] challenger_seat_indexes;
    public bool success;
    public NetworkCardPayload[] revealed_cards;
    public NetworkFortuneDeltaPayload[] fortune_deltas;
    public string message;
}

[Serializable]
public sealed class NetworkFortuneDeltaPayload
{
    public int seat_index;
    public int delta;
}

[Serializable]
public sealed class NetworkFortuneStatePayload
{
    public int min_draw_count;
    public int max_draw_count;
    public int[] submitted_seat_indexes;
    public bool own_submitted;
}

[Serializable]
public sealed class NetworkFortuneDrawPayload
{
    public int draw_count;
    public string[] result_tokens;
}

[Serializable]
public sealed class NetworkFinalResultPayload
{
    public NetworkFinalResultRowPayload[] results;
}

[Serializable]
public sealed class NetworkFinalResultRowPayload
{
    public int rank;
    public int seat_index;
    public string player_id;
    public string name;
    public int effective_bad_fortune;
    public int final_fortune;
    public int escaped_three_count;
    public string title;
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
    public int rank;

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

        return new CardDefinition(card_id, card_name, rank);
    }
}

public static class NetworkProtocolMessages
{
    public const string SessionHello = "session/hello";
    public const string SessionHelloAck = "session/hello_ack";
    public const string ProfileSetCharacter = "profile/set_character";
    public const string RoomList = "room/list";
    public const string RoomListResult = "room/list_result";
    public const string RoomCreate = "room/create";
    public const string RoomJoin = "room/join";
    public const string RoomLeave = "room/leave";
    public const string RoomReady = "room/ready";
    public const string RoomAddAi = "room/add_ai";
    public const string RoomStartGame = "room/start_game";
    public const string RoomState = "room/state";
    public const string RoomChat = "room/chat";
    public const string RoomChatMessage = "room/chat_message";
    public const string MatchmakingStart = "matchmaking/start";
    public const string MatchmakingCancel = "matchmaking/cancel";
    public const string MatchmakingState = "matchmaking/state";
    public const string MatchmakingFound = "matchmaking/found";
    public const string GameSubmitPlay = "game/submit_play";
    public const string GameSubmitChallenge = "game/submit_challenge";
    public const string GameSubmitFortuneDraw = "game/submit_fortune_draw";
    public const string GameChat = "game/chat";
    public const string GameState = "game/state";
    public const string GameLeave = "game/leave";
    public const string GameMatchStart = "game/match_start";
    public const string GamePlayerReplacedByAi = "game/player_replaced_by_ai";
    public const string Error = "error";

    private const string ClientVersion = "0.1.0";

    public static string SerializeSessionHello(string playerId, string name, string characterId)
    {
        return JsonUtility.ToJson(new NetworkSessionHelloMessage
        {
            payload = new NetworkSessionHelloPayload
            {
                player_id = playerId,
                name = name,
                client_version = ClientVersion,
                character_id = characterId
            }
        });
    }

    public static string SerializeProfileSetCharacterRequest(string characterId)
    {
        return JsonUtility.ToJson(new NetworkProfileSetCharacterRequestMessage
        {
            payload = new NetworkProfileSetCharacterRequestPayload
            {
                character_id = characterId
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

    public static string SerializeRoomChatRequest(string roomId, string message)
    {
        return JsonUtility.ToJson(new NetworkRoomChatRequestMessage
        {
            payload = new NetworkRoomChatRequestPayload
            {
                room_id = roomId,
                message = message
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

    public static string SerializeSubmitPlayRequest(string matchId, string[] cardIds, string faceUpCardId)
    {
        return JsonUtility.ToJson(new NetworkSubmitPlayRequestMessage
        {
            payload = new NetworkSubmitPlayRequestPayload
            {
                match_id = matchId,
                card_ids = cardIds,
                face_up_card_id = faceUpCardId
            }
        });
    }

    public static string SerializeSubmitChallengeRequest(string matchId, int targetSeatIndex)
    {
        return JsonUtility.ToJson(new NetworkSubmitChallengeRequestMessage
        {
            payload = new NetworkSubmitChallengeRequestPayload
            {
                match_id = matchId,
                target_seat_index = targetSeatIndex
            }
        });
    }

    public static string SerializeSubmitFortuneDrawRequest(string matchId, int drawCount)
    {
        return JsonUtility.ToJson(new NetworkSubmitFortuneDrawRequestMessage
        {
            payload = new NetworkSubmitFortuneDrawRequestPayload
            {
                match_id = matchId,
                draw_count = drawCount
            }
        });
    }

    public static string SerializeGameChatRequest(string matchId, string message)
    {
        return JsonUtility.ToJson(new NetworkGameChatRequestMessage
        {
            payload = new NetworkGameChatRequestPayload
            {
                match_id = matchId,
                message = message
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
