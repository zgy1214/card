from dataclasses import dataclass
import random
import uuid

from .models import Card, MATCH_STATUS_FINISHED, MATCH_STATUS_PLAYING, PLAYER_TYPE_AI, PlayerState, RoomPlayer


class GameRuleError(Exception):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


@dataclass(frozen=True)
class PlayResult:
    seat_index: int
    card: Card


class GameSession:
    initial_hand_count = 5
    turn_duration_seconds = 10
    bot_action_delay_min_seconds = 3.0
    bot_action_delay_max_seconds = 5.0

    def __init__(self, room_players: list[RoomPlayer], rng: random.Random | None = None) -> None:
        if not room_players:
            raise ValueError("A game session requires at least one player.")

        self._rng = rng or random.Random()
        self.match_id = f"match_{uuid.uuid4().hex[:8]}"
        self.match_status = MATCH_STATUS_PLAYING
        self.turn = 0
        self.current_seat_index = 0
        self.current_played_card: Card | None = None
        self.winner_seat_index: int | None = None
        self.players = [
            PlayerState(
                seat_index=room_player.seat_index,
                player_id=room_player.player_id,
                name=room_player.name,
                player_type=room_player.player_type,
            )
            for room_player in sorted(room_players, key=lambda item: item.seat_index)
        ]

    @property
    def is_playing(self) -> bool:
        return self.match_status == MATCH_STATUS_PLAYING

    @property
    def human_player_ids(self) -> list[str]:
        return [player.player_id for player in self.players if player.is_human]

    def start(self) -> None:
        deck = self._build_shuffled_deck()
        for player in self.players:
            player.hand_cards.clear()

        for _ in range(self.initial_hand_count):
            for player in self.players:
                player.hand_cards.append(deck.pop())

        self.match_status = MATCH_STATUS_PLAYING
        self.turn = 1
        self.current_seat_index = self.players[0].seat_index
        self.current_played_card = None
        self.winner_seat_index = None

    def current_turn_delay_seconds(self) -> float:
        current_player = self.current_player
        if current_player is None or current_player.is_human:
            return float(self.turn_duration_seconds)

        return self._rng.uniform(
            self.bot_action_delay_min_seconds,
            self.bot_action_delay_max_seconds,
        )

    @property
    def current_player(self) -> PlayerState | None:
        return self.get_player_by_seat(self.current_seat_index)

    def get_player_by_id(self, player_id: str) -> PlayerState | None:
        return next((player for player in self.players if player.player_id == player_id), None)

    def get_player_by_seat(self, seat_index: int) -> PlayerState | None:
        return next((player for player in self.players if player.seat_index == seat_index), None)

    def play_card(self, match_id: str, player_id: str, card_id: str) -> PlayResult:
        self._validate_match_is_active()
        self._validate_match_id(match_id)
        current_player = self.current_player
        if current_player is None or current_player.player_id != player_id:
            raise GameRuleError("not_your_turn", "Current turn does not belong to this player.")

        return self._play_card(current_player.seat_index, card_id)

    def auto_play_current_turn(self) -> PlayResult:
        self._validate_match_is_active()
        current_player = self.current_player
        if current_player is None or current_player.hand_count == 0:
            raise GameRuleError("invalid_operation", "Current player has no cards to auto-play.")

        random_index = self._rng.randrange(current_player.hand_count)
        random_card = current_player.hand_cards[random_index]
        return self._play_card(current_player.seat_index, random_card.card_id)

    def replace_player_with_ai(self, player_id: str) -> tuple[int, str] | None:
        player = self.get_player_by_id(player_id)
        if player is None or not player.is_human:
            return None

        ai_player_id = f"ai_replace_{player.seat_index}_{uuid.uuid4().hex[:4]}"
        player.replace_with_ai(ai_player_id, f"AI {player.seat_index + 1}")
        return player.seat_index, ai_player_id

    def to_match_start_payload(self) -> dict[str, object]:
        return {
            "match_id": self.match_id,
        }

    def to_match_state_payload(self, receiver_player_id: str | None) -> dict[str, object]:
        played_card_payload = self.current_played_card.to_payload() if self.current_played_card else {}
        return {
            "match_id": self.match_id,
            "match_status": self.match_status,
            "turn": self.turn,
            "current_seat_index": self.current_seat_index,
            "current_played_card": played_card_payload,
            "players": [
                player.to_payload(include_hand_cards=player.player_id == receiver_player_id)
                for player in self.players
            ],
        }

    def to_turn_start_payload(self) -> dict[str, object]:
        return {
            "match_id": self.match_id,
            "turn": self.turn,
            "current_seat_index": self.current_seat_index,
            "remaining_seconds": self.turn_duration_seconds,
        }

    def to_card_played_payload(self, play_result: PlayResult) -> dict[str, object]:
        return {
            "match_id": self.match_id,
            "seat_index": play_result.seat_index,
            "card": play_result.card.to_payload(),
        }

    def to_player_replaced_by_ai_payload(
        self,
        seat_index: int,
        player_id: str,
        ai_player_id: str,
    ) -> dict[str, object]:
        return {
            "match_id": self.match_id,
            "seat_index": seat_index,
            "player_id": player_id,
            "ai_player_id": ai_player_id,
        }

    def to_match_end_payload(self) -> dict[str, object]:
        if self.winner_seat_index is None:
            raise GameRuleError("invalid_operation", "Winner is not available for a finished match.")

        return {
            "match_id": self.match_id,
            "match_status": MATCH_STATUS_FINISHED,
            "winner_seat_index": self.winner_seat_index,
        }

    def _play_card(self, seat_index: int, card_id: str) -> PlayResult:
        player = self.get_player_by_seat(seat_index)
        if player is None:
            raise GameRuleError("invalid_operation", "Seat does not exist.")

        card = self._remove_card_from_hand(player, card_id)
        self.current_played_card = card
        play_result = PlayResult(seat_index=seat_index, card=card)

        if player.hand_count == 0:
            self.match_status = MATCH_STATUS_FINISHED
            self.winner_seat_index = seat_index
            return play_result

        self.current_seat_index = self._find_next_active_seat(seat_index)
        self.turn += 1
        return play_result

    def _remove_card_from_hand(self, player: PlayerState, card_id: str) -> Card:
        for index, card in enumerate(player.hand_cards):
            if card.card_id != card_id:
                continue

            return player.hand_cards.pop(index)

        raise GameRuleError("card_not_in_hand", f"Card '{card_id}' was not found in hand.")

    def _find_next_active_seat(self, previous_seat_index: int) -> int:
        sorted_players = sorted(self.players, key=lambda player: player.seat_index)
        seat_indexes = [player.seat_index for player in sorted_players]
        previous_index = seat_indexes.index(previous_seat_index)

        for offset in range(1, len(sorted_players) + 1):
            next_player = sorted_players[(previous_index + offset) % len(sorted_players)]
            if next_player.hand_count > 0:
                return next_player.seat_index

        raise GameRuleError("invalid_operation", "Cannot find a valid next player.")

    def _validate_match_is_active(self) -> None:
        if self.match_status == MATCH_STATUS_FINISHED:
            raise GameRuleError("game_finished", "The current match has already finished.")

    def _validate_match_id(self, match_id: str) -> None:
        if not match_id:
            raise GameRuleError("invalid_operation", "Match id is required.")

        if match_id != self.match_id:
            raise GameRuleError("game_not_found", "Match id does not match the current active match.")

    def _build_shuffled_deck(self) -> list[Card]:
        deck: list[Card] = []
        suits = ("S", "H", "D", "C")
        ranks = ("A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K")

        for suit in suits:
            for rank in ranks:
                deck.append(Card(card_id=f"{suit}_{rank}", card_name=f"{rank}{suit}"))

        self._rng.shuffle(deck)
        return deck
