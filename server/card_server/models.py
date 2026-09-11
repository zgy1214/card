from dataclasses import dataclass, field


MATCH_STATUS_PLAYING = "playing"
MATCH_STATUS_FINISHED = "finished"
PLAYER_STATUS_IDLE = "idle"
PLAYER_STATUS_MATCHMAKING = "matchmaking"
PLAYER_STATUS_ROOM = "room"
PLAYER_STATUS_GAME = "game"
PLAYER_TYPE_HUMAN = "human"
PLAYER_TYPE_AI = "ai"
ROOM_STATUS_WAITING = "waiting"
ROOM_STATUS_PLAYING = "playing"


@dataclass(frozen=True)
class Card:
    card_id: str
    card_name: str
    rank: int = 0

    def to_payload(self) -> dict[str, object]:
        return {
            "card_id": self.card_id,
            "card_name": self.card_name,
            "rank": self.rank,
        }


@dataclass
class PlayerState:
    seat_index: int
    player_id: str
    name: str
    player_type: str
    character_id: str = "mengshen"
    hand_cards: list[Card] = field(default_factory=list)

    @property
    def hand_count(self) -> int:
        return len(self.hand_cards)

    def to_payload(self, include_hand_cards: bool) -> dict[str, object]:
        return {
            "seat_index": self.seat_index,
            "player_id": self.player_id,
            "name": self.name,
            "player_type": self.player_type,
            "character_id": self.character_id,
            "hand_count": self.hand_count,
            "hand_cards": [card.to_payload() for card in self.hand_cards] if include_hand_cards else [],
        }

    @property
    def is_human(self) -> bool:
        return self.player_type == PLAYER_TYPE_HUMAN

    def replace_with_ai(self, ai_player_id: str, ai_name: str) -> None:
        self.player_id = ai_player_id
        self.name = ai_name
        self.player_type = PLAYER_TYPE_AI

    def sort_hand_cards(self) -> None:
        self.hand_cards.sort(key=lambda card: (card.rank, card.card_id))


@dataclass
class PlayerRecord:
    player_id: str
    name: str
    character_id: str = "mengshen"
    status: str = PLAYER_STATUS_IDLE
    room_id: str | None = None
    match_id: str | None = None

    def to_room_player(self, seat_index: int, is_owner: bool) -> "RoomPlayer":
        return RoomPlayer(
            seat_index=seat_index,
            player_id=self.player_id,
            name=self.name,
            character_id=self.character_id,
            player_type=PLAYER_TYPE_HUMAN,
            is_owner=is_owner,
            is_ready=False,
        )


@dataclass
class RoomPlayer:
    seat_index: int
    player_id: str
    name: str
    character_id: str
    player_type: str
    is_owner: bool = False
    is_ready: bool = False

    @property
    def is_human(self) -> bool:
        return self.player_type == PLAYER_TYPE_HUMAN

    @property
    def is_ai(self) -> bool:
        return self.player_type == PLAYER_TYPE_AI

    def to_payload(self) -> dict[str, object]:
        return {
            "seat_index": self.seat_index,
            "player_id": self.player_id,
            "name": self.name,
            "character_id": self.character_id,
            "player_type": self.player_type,
            "is_owner": self.is_owner,
            "is_ready": self.is_ready,
        }


@dataclass
class Room:
    room_id: str
    name: str
    owner_player_id: str
    max_players: int = 4
    status: str = ROOM_STATUS_WAITING
    players: list[RoomPlayer] = field(default_factory=list)

    @property
    def player_count(self) -> int:
        return len(self.players)

    @property
    def is_full(self) -> bool:
        return self.player_count >= self.max_players

    def get_player(self, player_id: str) -> RoomPlayer | None:
        return next((player for player in self.players if player.player_id == player_id), None)

    def get_player_by_seat(self, seat_index: int) -> RoomPlayer | None:
        return next((player for player in self.players if player.seat_index == seat_index), None)

    def next_free_seat_index(self) -> int:
        occupied_seats = {player.seat_index for player in self.players}
        for seat_index in range(self.max_players):
            if seat_index not in occupied_seats:
                return seat_index

        raise ValueError("Room is full.")

    def clear_human_ready(self) -> None:
        for player in self.players:
            if player.is_human:
                player.is_ready = False

    def refresh_owner_flags(self) -> None:
        for player in self.players:
            player.is_owner = player.player_id == self.owner_player_id

    def transfer_owner_if_needed(self) -> bool:
        if self.get_player(self.owner_player_id) is not None:
            self.refresh_owner_flags()
            return True

        next_owner = next((player for player in self.players if player.is_human), None)
        if next_owner is None:
            return False

        self.owner_player_id = next_owner.player_id
        self.refresh_owner_flags()
        return True

    def can_start(self) -> bool:
        if self.status != ROOM_STATUS_WAITING or not self.is_full:
            return False

        for player in self.players:
            if player.is_ai or player.is_owner:
                continue

            if not player.is_ready:
                return False

        return True

    def to_summary_payload(self) -> dict[str, object]:
        owner = self.get_player(self.owner_player_id)
        return {
            "room_id": self.room_id,
            "name": self.name,
            "owner_name": owner.name if owner else "",
            "status": self.status,
            "player_count": self.player_count,
            "max_players": self.max_players,
        }

    def to_state_payload(self) -> dict[str, object]:
        return {
            "room_id": self.room_id,
            "name": self.name,
            "status": self.status,
            "owner_player_id": self.owner_player_id,
            "max_players": self.max_players,
            "players": [player.to_payload() for player in sorted(self.players, key=lambda item: item.seat_index)],
        }
