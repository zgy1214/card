import logging
from typing import Any

from .game_session import GameRuleError
from .protocol import ProtocolError, make_error, make_message, parse_client_message
from .server_context import ServerContext


class ClientConnection:
    def __init__(self, websocket: Any, logger: logging.Logger, server_context: ServerContext) -> None:
        self._websocket = websocket
        self._logger = logger
        self._server_context = server_context
        self.player_id: str | None = None
        self.name: str | None = None

    async def run(self) -> None:
        try:
            async for raw_message in self._websocket:
                await self._handle_raw_message(raw_message)
        finally:
            await self._server_context.unregister_connection(self)

    def bind_player(self, player_id: str, name: str) -> None:
        self.player_id = player_id
        self.name = name

    async def send_message(self, message_type: str, payload: dict[str, Any]) -> None:
        await self._websocket.send(make_message(message_type, payload))

    async def send_error(self, scope: str, code: str, message: str) -> None:
        await self._websocket.send(make_error(scope, code, message))

    async def close(self) -> None:
        await self._websocket.close()

    async def _handle_raw_message(self, raw_message: str) -> None:
        try:
            message_type, payload = parse_client_message(raw_message)
            if message_type == "session/hello":
                await self._handle_session_hello(payload)
                return

            self._ensure_authenticated()
            await self._route_authenticated_message(message_type, payload)
        except ProtocolError as error:
            await self.send_error("protocol", error.code, error.message)
        except GameRuleError as error:
            await self.send_error(self._scope_from_message_type(raw_message), error.code, error.message)
        except Exception:
            self._logger.exception("Unexpected error while handling a client message.")
            await self.send_error("server", "invalid_operation", "Unexpected server error.")

    async def _handle_session_hello(self, payload: dict[str, Any]) -> None:
        player_id = self._require_string(payload, "player_id")
        name = self._require_string(payload, "name")
        character_id = payload.get("character_id", "mengshen")
        if not isinstance(character_id, str):
            character_id = "mengshen"

        await self._server_context.register_player(self, player_id, name, character_id)

    async def _route_authenticated_message(self, message_type: str, payload: dict[str, Any]) -> None:
        assert self.player_id is not None

        if message_type == "room/list":
            await self._server_context.list_rooms(self.player_id)
            return

        if message_type == "profile/set_character":
            await self._server_context.set_player_character(
                self.player_id,
                self._require_string(payload, "character_id"),
            )
            return

        if message_type == "room/create":
            await self._server_context.create_room(
                self.player_id,
                self._require_string(payload, "name"),
            )
            return

        if message_type == "room/join":
            await self._server_context.join_room(
                self.player_id,
                self._require_string(payload, "room_id"),
            )
            return

        if message_type == "room/leave":
            await self._server_context.leave_room(
                self.player_id,
                self._require_string(payload, "room_id"),
            )
            return

        if message_type == "room/ready":
            await self._server_context.set_room_ready(
                self.player_id,
                self._require_string(payload, "room_id"),
                self._require_bool(payload, "is_ready"),
            )
            return

        if message_type == "room/add_ai":
            await self._server_context.add_room_ai(
                self.player_id,
                self._require_string(payload, "room_id"),
            )
            return

        if message_type == "room/chat":
            await self._server_context.send_room_chat(
                self.player_id,
                self._require_string(payload, "room_id"),
                self._require_string(payload, "message"),
            )
            return

        if message_type == "room/start_game":
            await self._server_context.start_room_game(
                self.player_id,
                self._require_string(payload, "room_id"),
            )
            return

        if message_type == "matchmaking/start":
            await self._server_context.start_matchmaking(self.player_id)
            return

        if message_type == "matchmaking/cancel":
            await self._server_context.cancel_matchmaking(self.player_id)
            return

        if message_type == "game/submit_play":
            await self._server_context.submit_play(
                self.player_id,
                self._require_string(payload, "match_id"),
                self._require_string_list(payload, "card_ids"),
                self._require_string(payload, "face_up_card_id"),
            )
            return

        if message_type == "game/submit_challenge":
            await self._server_context.submit_challenge(
                self.player_id,
                self._require_string(payload, "match_id"),
                self._require_int(payload, "target_seat_index"),
            )
            return

        if message_type == "game/submit_fortune_draw":
            await self._server_context.submit_fortune_draw(
                self.player_id,
                self._require_string(payload, "match_id"),
                self._require_int(payload, "draw_count"),
            )
            return

        if message_type == "game/chat":
            await self._server_context.send_game_chat(
                self.player_id,
                self._require_string(payload, "match_id"),
                self._require_string(payload, "message"),
            )
            return

        if message_type == "game/leave":
            await self._server_context.leave_game(
                self.player_id,
                self._require_string(payload, "match_id"),
            )
            return

        raise ProtocolError("invalid_operation", f"Unsupported message type '{message_type}'.")

    def _ensure_authenticated(self) -> None:
        if self.player_id is None:
            raise ProtocolError("invalid_operation", "session/hello is required before other messages.")

    def _require_string(self, payload: dict[str, Any], field_name: str) -> str:
        value = payload.get(field_name)
        if not isinstance(value, str) or not value:
            raise ProtocolError("invalid_operation", f"'{field_name}' must be a non-empty string.")

        return value

    def _require_bool(self, payload: dict[str, Any], field_name: str) -> bool:
        value = payload.get(field_name)
        if not isinstance(value, bool):
            raise ProtocolError("invalid_operation", f"'{field_name}' must be a boolean.")

        return value

    def _require_int(self, payload: dict[str, Any], field_name: str) -> int:
        value = payload.get(field_name)
        if not isinstance(value, int):
            raise ProtocolError("invalid_operation", f"'{field_name}' must be an integer.")

        return value

    def _require_string_list(self, payload: dict[str, Any], field_name: str) -> list[str]:
        value = payload.get(field_name)
        if not isinstance(value, list) or not value:
            raise ProtocolError("invalid_operation", f"'{field_name}' must be a non-empty string array.")

        result: list[str] = []
        for item in value:
            if not isinstance(item, str) or not item:
                raise ProtocolError("invalid_operation", f"'{field_name}' must be a non-empty string array.")
            result.append(item)

        return result

    def _scope_from_message_type(self, raw_message: str) -> str:
        try:
            message_type, _ = parse_client_message(raw_message)
        except ProtocolError:
            return "protocol"

        return message_type.split("/", 1)[0] if "/" in message_type else "protocol"
