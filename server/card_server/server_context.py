import asyncio
import logging
import random
from typing import Any

from .game_session import GameRuleError, GameSession
from .models import (
    PLAYER_STATUS_GAME,
    PLAYER_STATUS_IDLE,
    PLAYER_STATUS_MATCHMAKING,
    PLAYER_STATUS_ROOM,
    PLAYER_TYPE_AI,
    PlayerRecord,
    Room,
    RoomPlayer,
    ROOM_STATUS_PLAYING,
    ROOM_STATUS_WAITING,
)

CHARACTER_IDS = ("mengshen", "xiaomu", "ayi", "naibao")


class ServerContext:
    required_matchmaking_players = 4
    default_room_max_players = 4

    def __init__(self, logger: logging.Logger) -> None:
        self._logger = logger
        self._lock = asyncio.Lock()
        self._connections: dict[str, Any] = {}
        self._players: dict[str, PlayerRecord] = {}
        self._rooms: dict[str, Room] = {}
        self._matches: dict[str, GameSession] = {}
        self._matchmaking_queue: list[str] = []
        self._phase_tasks: dict[str, asyncio.Task[None]] = {}
        self._ai_counter = 0
        self._rng = random.Random()

    async def register_player(self, connection: Any, player_id: str, name: str, character_id: str) -> None:
        async with self._lock:
            if player_id in self._connections:
                await connection.send_error("session", "duplicate_player", "Player is already online.")
                await connection.close()
                return

            self._connections[player_id] = connection
            self._players[player_id] = PlayerRecord(
                player_id=player_id,
                name=name,
                character_id=self._normalize_character_id(character_id),
            )
            connection.bind_player(player_id, name)
            await connection.send_message(
                "session/hello_ack",
                {
                    "player_id": player_id,
                    "name": name,
                    "character_id": self._players[player_id].character_id,
                },
            )

    async def set_player_character(self, player_id: str, character_id: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            player.character_id = self._normalize_character_id(character_id)

            if player.status != PLAYER_STATUS_ROOM or player.room_id is None:
                return

            room = self._rooms.get(player.room_id)
            if room is None:
                return

            room_player = room.get_player(player_id)
            if room_player is not None:
                room_player.character_id = player.character_id
                await self._broadcast_room_state_locked(room)

    async def unregister_connection(self, connection: Any) -> None:
        player_id = connection.player_id
        if player_id is None:
            return

        async with self._lock:
            if self._connections.get(player_id) is not connection:
                return

            player = self._players.get(player_id)
            if player is None:
                return

            # Disconnect has the same gameplay meaning as leaving the current activity.
            if player.status == PLAYER_STATUS_MATCHMAKING:
                self._remove_from_matchmaking_queue(player_id)
                player.status = PLAYER_STATUS_IDLE
                await self._broadcast_matchmaking_state_locked()
            elif player.status == PLAYER_STATUS_ROOM and player.room_id is not None:
                await self._remove_player_from_room_locked(player)
            elif player.status == PLAYER_STATUS_GAME and player.match_id is not None:
                await self._replace_game_player_with_ai_locked(player)

            self._connections.pop(player_id, None)
            self._players.pop(player_id, None)

    async def list_rooms(self, player_id: str) -> None:
        async with self._lock:
            connection = self._require_connection(player_id)
            rooms = [
                room.to_summary_payload()
                for room in self._rooms.values()
                if room.status == ROOM_STATUS_WAITING
            ]
            await connection.send_message("room/list_result", {"rooms": rooms})

    async def create_room(self, player_id: str, room_name: str) -> None:
        async with self._lock:
            player = self._require_idle_player(player_id)
            if not room_name:
                raise GameRuleError("invalid_operation", "Room name is required.")

            room_id = self._generate_room_id()
            room = Room(
                room_id=room_id,
                name=room_name,
                owner_player_id=player_id,
                max_players=self.default_room_max_players,
            )
            room.players.append(player.to_room_player(seat_index=0, is_owner=True))
            self._rooms[room_id] = room
            player.status = PLAYER_STATUS_ROOM
            player.room_id = room_id
            await self._broadcast_room_state_locked(room)

    async def join_room(self, player_id: str, room_id: str) -> None:
        async with self._lock:
            player = self._require_idle_player(player_id)
            room = self._require_waiting_room(room_id)
            if room.is_full:
                raise GameRuleError("room_full", "Room is full.")

            room.players.append(player.to_room_player(room.next_free_seat_index(), is_owner=False))
            player.status = PLAYER_STATUS_ROOM
            player.room_id = room.room_id
            room.clear_human_ready()
            await self._broadcast_room_state_locked(room)

    async def leave_room(self, player_id: str, room_id: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            if player.room_id != room_id:
                raise GameRuleError("room_not_found", "Player is not in this room.")

            await self._remove_player_from_room_locked(player)

    async def set_room_ready(self, player_id: str, room_id: str, is_ready: bool) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            room = self._require_waiting_room(room_id)
            if player.room_id != room.room_id:
                raise GameRuleError("room_not_found", "Player is not in this room.")

            room_player = room.get_player(player_id)
            if room_player is None:
                raise GameRuleError("room_not_found", "Player is not in this room.")

            if room_player.is_owner:
                raise GameRuleError("invalid_operation", "Room owner does not use ready.")

            room_player.is_ready = is_ready
            await self._broadcast_room_state_locked(room)

    async def add_room_ai(self, player_id: str, room_id: str) -> None:
        async with self._lock:
            room = self._require_owned_waiting_room(player_id, room_id)
            if room.is_full:
                raise GameRuleError("room_full", "Room is full.")

            self._ai_counter += 1
            seat_index = room.next_free_seat_index()
            room.players.append(
                RoomPlayer(
                    seat_index=seat_index,
                    player_id=f"ai_{self._ai_counter:03d}",
                    name=f"AI {seat_index + 1}",
                    character_id=self._rng.choice(CHARACTER_IDS),
                    player_type=PLAYER_TYPE_AI,
                    is_ready=True,
                )
            )
            room.clear_human_ready()
            await self._broadcast_room_state_locked(room)

    async def start_room_game(self, player_id: str, room_id: str) -> None:
        async with self._lock:
            room = self._require_owned_waiting_room(player_id, room_id)
            if not room.can_start():
                raise GameRuleError("not_ready", "Room is not ready to start.")

            room.status = ROOM_STATUS_PLAYING
            await self._start_game_from_room_players_locked(room.players)
            self._rooms.pop(room.room_id, None)

    async def start_matchmaking(self, player_id: str) -> None:
        async with self._lock:
            player = self._require_idle_player(player_id)
            if player_id not in self._matchmaking_queue:
                self._matchmaking_queue.append(player_id)

            player.status = PLAYER_STATUS_MATCHMAKING
            await self._broadcast_matchmaking_state_locked()
            if len(self._matchmaking_queue) >= self.required_matchmaking_players:
                matched_player_ids = self._matchmaking_queue[: self.required_matchmaking_players]
                del self._matchmaking_queue[: self.required_matchmaking_players]
                await self._broadcast_matchmaking_state_locked()
                await self._start_matchmaking_game_locked(matched_player_ids)

    async def cancel_matchmaking(self, player_id: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            connection = self._require_connection(player_id)
            if player.status == PLAYER_STATUS_GAME:
                return

            if player.status == PLAYER_STATUS_IDLE:
                await connection.send_message(
                    "matchmaking/state",
                    {
                        "status": PLAYER_STATUS_IDLE,
                        "current_count": 0,
                        "required_count": self.required_matchmaking_players,
                    },
                )
                return

            if player.status != PLAYER_STATUS_MATCHMAKING:
                raise GameRuleError("invalid_state", "Player is not in matchmaking.")

            self._remove_from_matchmaking_queue(player_id)
            player.status = PLAYER_STATUS_IDLE
            await connection.send_message(
                "matchmaking/state",
                {
                    "status": PLAYER_STATUS_IDLE,
                    "current_count": 0,
                    "required_count": self.required_matchmaking_players,
                },
            )
            await self._broadcast_matchmaking_state_locked()

    async def submit_play(self, player_id: str, match_id: str, card_ids: list[str], face_up_card_id: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            if player.match_id != match_id:
                raise GameRuleError("game_not_found", "Player is not in this match.")

            session = self._require_match(match_id)
            previous_phase = session.phase
            session.submit_play(match_id, player_id, card_ids, face_up_card_id)
            await self._after_game_action_locked(session, previous_phase)

    async def submit_challenge(self, player_id: str, match_id: str, target_seat_index: int) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            if player.match_id != match_id:
                raise GameRuleError("game_not_found", "Player is not in this match.")

            session = self._require_match(match_id)
            previous_phase = session.phase
            session.submit_challenge(match_id, player_id, target_seat_index)
            await self._after_game_action_locked(session, previous_phase)


    async def leave_game(self, player_id: str, match_id: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            if player.match_id != match_id:
                raise GameRuleError("game_not_found", "Player is not in this match.")

            session = self._require_match(match_id)
            if not session.is_playing:
                player.status = PLAYER_STATUS_IDLE
                player.match_id = None
                if not self._connected_human_ids_for_match(session):
                    await self._destroy_match_locked(session.match_id)
                return

            await self._replace_game_player_with_ai_locked(player)

    async def send_game_chat(self, player_id: str, match_id: str, message: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            if player.match_id != match_id:
                raise GameRuleError("game_not_found", "Player is not in this match.")

            session = self._require_match(match_id)
            trimmed_message = message.strip()
            if not trimmed_message:
                raise GameRuleError("invalid_operation", "Chat message is empty.")

            if len(trimmed_message) > 80:
                trimmed_message = trimmed_message[:80]

            await self._broadcast_to_match_locked(
                session,
                "game/chat",
                {
                    "match_id": match_id,
                    "player_id": player.player_id,
                    "name": player.name,
                    "character_id": player.character_id,
                    "message": trimmed_message,
                },
            )

    async def send_room_chat(self, player_id: str, room_id: str, message: str) -> None:
        async with self._lock:
            player = self._require_player(player_id)
            room = self._require_waiting_room(room_id)
            if player.room_id != room.room_id:
                raise GameRuleError("room_not_found", "Player is not in this room.")

            trimmed_message = message.strip()
            if not trimmed_message:
                raise GameRuleError("invalid_operation", "Chat message is empty.")

            if len(trimmed_message) > 80:
                trimmed_message = trimmed_message[:80]

            await self._broadcast_room_chat_locked(
                room,
                {
                    "room_id": room.room_id,
                    "player_id": player.player_id,
                    "name": player.name,
                    "character_id": player.character_id,
                    "message": trimmed_message,
                },
            )

    async def _start_matchmaking_game_locked(self, player_ids: list[str]) -> None:
        room_players: list[RoomPlayer] = []
        for seat_index, player_id in enumerate(player_ids):
            player = self._require_player(player_id)
            room_players.append(player.to_room_player(seat_index=seat_index, is_owner=seat_index == 0))

        session = self._create_game_from_room_players_locked(room_players)
        for player_id in player_ids:
            connection = self._connections.get(player_id)
            if connection is not None:
                await connection.send_message("matchmaking/found", {"match_id": session.match_id})

        await self._broadcast_game_start_locked(session)
        self._schedule_phase_task_locked(session)

    async def _start_game_from_room_players_locked(self, room_players: list[RoomPlayer]) -> GameSession:
        session = self._create_game_from_room_players_locked(room_players)
        await self._broadcast_game_start_locked(session)
        self._schedule_phase_task_locked(session)
        return session

    def _create_game_from_room_players_locked(self, room_players: list[RoomPlayer]) -> GameSession:
        session = GameSession(room_players)
        session.start()
        self._matches[session.match_id] = session

        for room_player in room_players:
            if not room_player.is_human:
                continue

            player = self._require_player(room_player.player_id)
            player.status = PLAYER_STATUS_GAME
            player.room_id = None
            player.match_id = session.match_id

        return session

    async def _after_game_action_locked(self, session: GameSession, previous_phase: str) -> None:
        if session.phase != previous_phase:
            await self._cancel_phase_task_locked(session.match_id)

        await self._broadcast_match_state_locked(session)
        if session.is_playing:
            self._schedule_phase_task_locked(session)

    async def _replace_game_player_with_ai_locked(self, player: PlayerRecord) -> None:
        if player.match_id is None:
            return

        session = self._matches.get(player.match_id)
        if session is None:
            player.status = PLAYER_STATUS_IDLE
            player.match_id = None
            return

        previous_phase = session.phase
        replacement = session.replace_player_with_ai(player.player_id)
        old_player_id = player.player_id
        player.status = PLAYER_STATUS_IDLE
        player.match_id = None

        if replacement is None:
            return

        if session.phase != previous_phase:
            await self._cancel_phase_task_locked(session.match_id)

        seat_index, ai_player_id = replacement
        await self._broadcast_to_match_locked(
            session,
            "game/player_replaced_by_ai",
            session.to_player_replaced_by_ai_payload(seat_index, old_player_id, ai_player_id),
        )
        await self._broadcast_match_state_locked(session)
        self._schedule_phase_task_locked(session)

        # Once no connected humans remain, there is no client left to observe this demo match.
        if not self._connected_human_ids_for_match(session):
            await self._destroy_match_locked(session.match_id)

    async def _run_phase_timeout(
        self,
        expected_match_id: str,
        expected_phase: str,
        expected_round: int,
        delay_seconds: float,
    ) -> None:
        try:
            await asyncio.sleep(delay_seconds)
            async with self._lock:
                session = self._matches.get(expected_match_id)
                if session is None or not session.is_playing:
                    return

                if session.phase != expected_phase or session.round_index != expected_round:
                    return

                previous_phase = session.phase
                session.handle_phase_timeout(expected_phase, expected_round)
                await self._after_game_action_locked(session, previous_phase)
        except asyncio.CancelledError:
            return
        except Exception:
            self._logger.exception("Unexpected error while resolving a phase timeout.")

    def _schedule_phase_task_locked(self, session: GameSession) -> None:
        if not session.is_playing:
            return

        if session.match_id in self._phase_tasks:
            return

        delay_seconds = session.current_phase_delay_seconds()
        if delay_seconds <= 0:
            return

        self._phase_tasks[session.match_id] = asyncio.create_task(
            self._run_phase_timeout(
                session.match_id,
                session.phase,
                session.round_index,
                delay_seconds,
            )
        )

    async def _cancel_phase_task_locked(self, match_id: str) -> None:
        task = self._phase_tasks.pop(match_id, None)
        if task is None:
            return

        if task is asyncio.current_task():
            return

        task.cancel()
        try:
            await task
        except asyncio.CancelledError:
            pass

    async def _destroy_match_locked(self, match_id: str) -> None:
        await self._cancel_phase_task_locked(match_id)
        self._matches.pop(match_id, None)

    async def _remove_player_from_room_locked(self, player: PlayerRecord) -> None:
        room = self._rooms.get(player.room_id or "")
        player.status = PLAYER_STATUS_IDLE
        player.room_id = None

        if room is None:
            return

        room.players = [room_player for room_player in room.players if room_player.player_id != player.player_id]
        if not room.players or not room.transfer_owner_if_needed():
            self._rooms.pop(room.room_id, None)
            return

        room.clear_human_ready()
        await self._broadcast_room_state_locked(room)

    async def _broadcast_game_start_locked(self, session: GameSession) -> None:
        await self._broadcast_to_match_locked(session, "game/match_start", session.to_match_start_payload())
        await self._broadcast_match_state_locked(session)

    async def _broadcast_match_state_locked(self, session: GameSession) -> None:
        for player_id in self._connected_human_ids_for_match(session):
            connection = self._connections.get(player_id)
            if connection is not None:
                await connection.send_message("game/state", session.to_game_state_payload(player_id))

    async def _broadcast_to_match_locked(
        self,
        session: GameSession,
        message_type: str,
        payload: dict[str, object],
    ) -> None:
        for player_id in self._connected_human_ids_for_match(session):
            connection = self._connections.get(player_id)
            if connection is not None:
                await connection.send_message(message_type, payload)

    async def _broadcast_room_state_locked(self, room: Room) -> None:
        payload = room.to_state_payload()
        for room_player in room.players:
            if not room_player.is_human:
                continue

            connection = self._connections.get(room_player.player_id)
            if connection is not None:
                await connection.send_message("room/state", payload)

    async def _broadcast_room_chat_locked(self, room: Room, payload: dict[str, object]) -> None:
        for room_player in room.players:
            if not room_player.is_human:
                continue

            connection = self._connections.get(room_player.player_id)
            if connection is not None:
                await connection.send_message("room/chat_message", payload)

    async def _broadcast_matchmaking_state_locked(self) -> None:
        payload = {
            "status": PLAYER_STATUS_MATCHMAKING,
            "current_count": len(self._matchmaking_queue),
            "required_count": self.required_matchmaking_players,
        }
        for player_id in self._matchmaking_queue:
            connection = self._connections.get(player_id)
            if connection is not None:
                await connection.send_message("matchmaking/state", payload)

    def _connected_human_ids_for_match(self, session: GameSession) -> list[str]:
        player_ids: list[str] = []
        for player in session.players:
            player_record = self._players.get(player.player_id)
            if (
                player.is_human
                and player.player_id in self._connections
                and player_record is not None
                and player_record.match_id == session.match_id
            ):
                player_ids.append(player.player_id)

        return player_ids

    def _remove_from_matchmaking_queue(self, player_id: str) -> None:
        self._matchmaking_queue = [
            queued_player_id
            for queued_player_id in self._matchmaking_queue
            if queued_player_id != player_id
        ]

    def _require_player(self, player_id: str) -> PlayerRecord:
        player = self._players.get(player_id)
        if player is None:
            raise GameRuleError("invalid_state", "Player is not connected.")

        return player

    def _require_idle_player(self, player_id: str) -> PlayerRecord:
        player = self._require_player(player_id)
        if player.status != PLAYER_STATUS_IDLE:
            raise GameRuleError("invalid_state", "Player is busy.")

        return player

    def _require_connection(self, player_id: str) -> Any:
        connection = self._connections.get(player_id)
        if connection is None:
            raise GameRuleError("invalid_state", "Player connection is not available.")

        return connection

    def _require_waiting_room(self, room_id: str) -> Room:
        room = self._rooms.get(room_id)
        if room is None or room.status != ROOM_STATUS_WAITING:
            raise GameRuleError("room_not_found", "Room not found.")

        return room

    def _require_owned_waiting_room(self, player_id: str, room_id: str) -> Room:
        room = self._require_waiting_room(room_id)
        if room.owner_player_id != player_id:
            raise GameRuleError("not_room_owner", "Only the room owner can do this.")

        return room

    def _require_match(self, match_id: str) -> GameSession:
        session = self._matches.get(match_id)
        if session is None:
            raise GameRuleError("game_not_found", "Match not found.")

        return session

    def _generate_room_id(self) -> str:
        for _ in range(100):
            room_id = f"{self._rng.randint(1000, 9999)}"
            if room_id not in self._rooms:
                return room_id

        raise GameRuleError("invalid_operation", "Cannot allocate a room id.")

    def _normalize_character_id(self, character_id: str) -> str:
        if character_id in CHARACTER_IDS:
            return character_id

        return "mengshen"
