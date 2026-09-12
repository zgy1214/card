from __future__ import annotations

from dataclasses import dataclass
import random
import time
import uuid

from .models import Card, MATCH_STATUS_FINISHED, MATCH_STATUS_PLAYING, PLAYER_TYPE_AI, PlayerState, RoomPlayer

CARD_SUITS = ("spade", "heart", "club", "diamond")
PHASE_PLAY_SELECT = "play_select"
PHASE_CHALLENGE_SELECT = "challenge_select"
PHASE_SHOWDOWN = "showdown"
PHASE_FINAL_RESULT = "final_result"

class GameRuleError(Exception):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message

@dataclass(frozen=True)
class PlaySubmission:
    seat_index: int
    cards: list[Card]
    face_up_card_id: str

    @property
    def face_up_card(self) -> Card:
        return next(card for card in self.cards if card.card_id == self.face_up_card_id)

    @property
    def hidden_cards(self) -> list[Card]:
        return [card for card in self.cards if card.card_id != self.face_up_card_id]

    @property
    def declaration(self) -> str:
        return f"{len(self.cards)}张{self.face_up_card.rank}"

@dataclass
class ShowdownEvent:
    target_seat_index: int
    challenger_seat_indexes: list[int]
    success: bool
    revealed_cards: list[Card]
    fortune_deltas: dict[int, int]
    fortune_before: list[dict[str, int]]
    fortune_after: list[dict[str, int]]

class GameSession:
    initial_hand_count = 6
    guaranteed_initial_three_count = 2
    round_draw_count = 2
    max_round = 3
    play_select_duration_seconds = 20
    challenge_select_duration_seconds = 20
    showdown_overview_seconds = 2.0
    showdown_focus_seconds = 1.2
    showdown_reveal_seconds = 1.6
    showdown_reward_seconds = 1.8
    settlement_delay_seconds = 3.0
    initial_bad_fortune_count = 4
    fortune_pool_size = 8
    ai_challenge_probability = 0.55
    ai_multi_card_play_probability = 0.78
    ai_hidden_three_play_probability = 0.58

    def __init__(self, room_players: list[RoomPlayer], rng: random.Random | None = None) -> None:
        if len(room_players) != 4:
            raise ValueError("Lx3 requires exactly four players.")

        self._rng = rng or random.Random()
        self.match_id = f"match_{uuid.uuid4().hex[:8]}"
        self.match_status = MATCH_STATUS_PLAYING
        self.phase = PHASE_PLAY_SELECT
        self.round_index = 1
        self.phase_end_time = 0.0
        self.players = [
            PlayerState(
                seat_index=room_player.seat_index,
                player_id=room_player.player_id,
                name=room_player.name,
                player_type=room_player.player_type,
                character_id=room_player.character_id,
            )
            for room_player in sorted(room_players, key=lambda item: item.seat_index)
        ]
        self.play_submissions: dict[int, PlaySubmission] = {}
        self.challenge_submissions: dict[int, int] = {}
        self.showdown_events: list[ShowdownEvent] = []
        self.successful_escaped_threes: dict[int, int] = {player.seat_index: 0 for player in self.players}
        self.fortune_pools: dict[int, list[str]] = {}
        self._deck: list[Card] = []

    @property
    def is_playing(self) -> bool:
        return self.match_status == MATCH_STATUS_PLAYING

    @property
    def human_player_ids(self) -> list[str]:
        return [player.player_id for player in self.players if player.is_human]

    def start(self) -> None:
        self._deck = self._build_shuffled_deck()
        self._deal_initial_hands()
        self.fortune_pools = {
            player.seat_index: self._initial_fortune_pool()
            for player in self.players
        }
        self.round_index = 1
        self._draw_for_round()
        self._enter_phase(PHASE_PLAY_SELECT)

    def submit_play(self, match_id: str, player_id: str, card_ids: list[str], face_up_card_id: str) -> None:
        self._validate_match_is_active()
        self._validate_match_id(match_id)
        if self.phase != PHASE_PLAY_SELECT:
            raise GameRuleError("invalid_phase", "Play can only be submitted during play_select.")

        player = self._require_player_by_id(player_id)
        if player.seat_index in self.play_submissions:
            raise GameRuleError("already_submitted", "This player has already submitted play.")

        cards = self._remove_cards_from_hand(player, card_ids)
        if face_up_card_id not in {card.card_id for card in cards}:
            player.hand_cards.extend(cards)
            player.sort_hand_cards()
            raise GameRuleError("invalid_operation", "Face-up card must be one of the submitted cards.")

        if not self._is_legal_play(cards):
            player.hand_cards.extend(cards)
            player.sort_hand_cards()
            raise GameRuleError("invalid_play", "Cards must share the same rank or include rank 3.")

        self.play_submissions[player.seat_index] = PlaySubmission(
            seat_index=player.seat_index,
            cards=cards,
            face_up_card_id=face_up_card_id,
        )
        if self._all_players_submitted(self.play_submissions):
            self._enter_challenge_phase()

    def submit_challenge(self, match_id: str, player_id: str, target_seat_index: int) -> None:
        self._validate_match_is_active()
        self._validate_match_id(match_id)
        if self.phase != PHASE_CHALLENGE_SELECT:
            raise GameRuleError("invalid_phase", "Challenge can only be submitted during challenge_select.")

        player = self._require_player_by_id(player_id)
        if player.seat_index in self.challenge_submissions:
            raise GameRuleError("already_submitted", "This player has already submitted challenge.")

        if target_seat_index != -1:
            if target_seat_index == player.seat_index:
                raise GameRuleError("invalid_operation", "Players cannot challenge themselves.")
            self._require_player_by_seat(target_seat_index)

        self.challenge_submissions[player.seat_index] = target_seat_index
        if self._all_players_submitted(self.challenge_submissions):
            self._resolve_challenges()

    def handle_phase_timeout(self, expected_phase: str, expected_round: int) -> None:
        if self.phase != expected_phase or self.round_index != expected_round or not self.is_playing:
            return

        if self.phase == PHASE_PLAY_SELECT:
            self._enter_challenge_phase()
            return

        if self.phase == PHASE_CHALLENGE_SELECT:
            self._resolve_challenges()
            return

        if self.phase == PHASE_SHOWDOWN:
            self._advance_after_showdown()
            return

    def replace_player_with_ai(self, player_id: str) -> tuple[int, str] | None:
        player = self.get_player_by_id(player_id)
        if player is None or not player.is_human:
            return None

        ai_player_id = f"ai_replace_{player.seat_index}_{uuid.uuid4().hex[:4]}"
        player.replace_with_ai(ai_player_id, f"AI {player.seat_index + 1}")
        if self.phase == PHASE_PLAY_SELECT and player.seat_index not in self.play_submissions:
            self._auto_submit_play(player)
        if self.phase == PHASE_CHALLENGE_SELECT and player.seat_index not in self.challenge_submissions:
            self.challenge_submissions[player.seat_index] = self._choose_ai_challenge_target(player.seat_index)
        self._advance_if_current_phase_ready()
        return player.seat_index, ai_player_id

    def get_player_by_id(self, player_id: str) -> PlayerState | None:
        return next((player for player in self.players if player.player_id == player_id), None)

    def get_player_by_seat(self, seat_index: int) -> PlayerState | None:
        return next((player for player in self.players if player.seat_index == seat_index), None)

    def current_phase_delay_seconds(self) -> float:
        if self.phase == PHASE_PLAY_SELECT:
            return float(self.play_select_duration_seconds)
        if self.phase == PHASE_CHALLENGE_SELECT:
            return float(self.challenge_select_duration_seconds)
        if self.phase == PHASE_SHOWDOWN:
            return self.showdown_overview_seconds + len(self.showdown_events) * (
                self.showdown_focus_seconds + self.showdown_reveal_seconds + self.showdown_reward_seconds
            ) + (self.settlement_delay_seconds if self.round_index >= self.max_round else 0.0)
        return 0.0

    def to_match_start_payload(self) -> dict[str, object]:
        return {"match_id": self.match_id}

    def to_game_state_payload(self, receiver_player_id: str | None) -> dict[str, object]:
        receiver = self.get_player_by_id(receiver_player_id or "")
        return {
            "match_id": self.match_id,
            "match_status": self.match_status,
            "phase": self.phase,
            "round_index": self.round_index,
            "server_time": time.time(),
            "phase_end_time": self.phase_end_time,
            "players": [self._player_public_payload(player) for player in self.players],
            "local_player_private": self._private_payload(receiver) if receiver else {},
            "challenge_state": self._challenge_state_payload(receiver),
            "showdown_state": self._showdown_state_payload(),
        }

    def to_player_replaced_by_ai_payload(self, seat_index: int, player_id: str, ai_player_id: str) -> dict[str, object]:
        return {
            "match_id": self.match_id,
            "seat_index": seat_index,
            "player_id": player_id,
            "ai_player_id": ai_player_id,
        }

    def _enter_challenge_phase(self) -> None:
        self._auto_submit_missing_plays()
        self.challenge_submissions.clear()
        self._enter_phase(PHASE_CHALLENGE_SELECT)

    def _advance_if_current_phase_ready(self) -> None:
        if self.phase == PHASE_PLAY_SELECT and self._all_players_submitted(self.play_submissions):
            self._enter_challenge_phase()
            return

        if self.phase == PHASE_CHALLENGE_SELECT and self._all_players_submitted(self.challenge_submissions):
            self._resolve_challenges()
            return

    def _resolve_challenges(self) -> None:
        self._auto_submit_missing_challenges()
        self.showdown_events = []
        challenge_by_target: dict[int, list[int]] = {}
        for challenger_seat_index, target_seat_index in self.challenge_submissions.items():
            if target_seat_index == -1:
                continue
            challenge_by_target.setdefault(target_seat_index, []).append(challenger_seat_index)

        for target_seat_index in sorted(challenge_by_target):
            challengers = sorted(challenge_by_target[target_seat_index])
            submission = self.play_submissions[target_seat_index]
            success = any(card.rank == 3 for card in submission.cards)
            fortune_before = self._fortune_counts_payload()
            deltas = self._apply_challenge_reward(target_seat_index, challengers, success)
            self.showdown_events.append(
                ShowdownEvent(
                    target_seat_index=target_seat_index,
                    challenger_seat_indexes=challengers,
                    success=success,
                    revealed_cards=submission.hidden_cards,
                    fortune_deltas=deltas,
                    fortune_before=fortune_before,
                    fortune_after=self._fortune_counts_payload(),
                )
            )

        challenged_targets = set(challenge_by_target)
        for seat_index, submission in self.play_submissions.items():
            if seat_index in challenged_targets:
                continue
            escaped_threes = sum(1 for card in submission.cards if card.rank == 3)
            self.successful_escaped_threes[seat_index] += escaped_threes

        self._enter_phase(PHASE_SHOWDOWN)

    def _advance_after_showdown(self) -> None:
        if self.round_index < self.max_round:
            self.round_index += 1
            self.play_submissions.clear()
            self.challenge_submissions.clear()
            self.showdown_events.clear()
            self._draw_for_round()
            self._enter_phase(PHASE_PLAY_SELECT)
            return

        self.match_status = MATCH_STATUS_FINISHED
        self._enter_phase(PHASE_FINAL_RESULT)

    def _enter_phase(self, phase: str) -> None:
        self.phase = phase
        delay_seconds = self.current_phase_delay_seconds()
        self.phase_end_time = time.time() + delay_seconds if delay_seconds > 0 else 0.0

    def _deal_initial_hands(self) -> None:
        for player in self.players:
            player.hand_cards.clear()

        threes = [card for card in self._deck if card.rank == 3]
        self._rng.shuffle(threes)
        for _ in range(self.guaranteed_initial_three_count):
            for player in self.players:
                card = threes.pop()
                self._deck.remove(card)
                player.hand_cards.append(card)

        for _ in range(self.initial_hand_count - self.guaranteed_initial_three_count):
            for player in self.players:
                player.hand_cards.append(self._deck.pop())

        for player in self.players:
            player.sort_hand_cards()

    def _draw_for_round(self) -> None:
        for player in self.players:
            for _ in range(self.round_draw_count):
                if self._deck:
                    player.hand_cards.append(self._deck.pop())
            player.sort_hand_cards()

    def _auto_submit_missing_plays(self) -> None:
        for player in self.players:
            if player.seat_index not in self.play_submissions:
                self._auto_submit_play(player)

    def _auto_submit_play(self, player: PlayerState) -> None:
        if not player.hand_cards:
            raise GameRuleError("invalid_operation", "Player has no cards to auto-play.")

        cards = self._choose_ai_play_cards(player)
        for card in cards:
            player.hand_cards.remove(card)
        player.sort_hand_cards()

        face_up_card = self._choose_ai_face_up_card(cards)
        self.play_submissions[player.seat_index] = PlaySubmission(
            seat_index=player.seat_index,
            cards=cards,
            face_up_card_id=face_up_card.card_id,
        )

    def _auto_submit_missing_challenges(self) -> None:
        for player in self.players:
            if player.seat_index not in self.challenge_submissions:
                self.challenge_submissions[player.seat_index] = (
                    self._choose_ai_challenge_target(player.seat_index)
                    if player.player_type == PLAYER_TYPE_AI
                    else -1
                )

    def _choose_ai_challenge_target(self, seat_index: int) -> int:
        target_scores: list[tuple[float, int]] = []
        for player in self.players:
            if player.seat_index == seat_index:
                continue

            submission = self.play_submissions.get(player.seat_index)
            if submission is None:
                continue

            suspicion = self._score_ai_challenge_suspicion(submission)
            target_scores.append((suspicion, player.seat_index))

        if not target_scores:
            return -1

        target_scores.sort(reverse=True)
        best_suspicion, best_target = target_scores[0]
        challenge_probability = min(0.95, self.ai_challenge_probability + best_suspicion)
        if self._rng.random() > challenge_probability:
            return -1

        if len(target_scores) > 1 and self._rng.random() < 0.22:
            return self._rng.choice(target_scores[: min(2, len(target_scores))])[1]

        return best_target

    def _choose_ai_play_cards(self, player: PlayerState) -> list[Card]:
        same_rank_groups = self._group_cards_by_rank(player.hand_cards)
        non_three_groups = [cards for rank, cards in same_rank_groups.items() if rank != 3]
        non_three_groups.sort(key=lambda cards: (len(cards), cards[0].rank), reverse=True)

        selected: list[Card] = []
        if non_three_groups and (len(non_three_groups[0]) > 1 or self._rng.random() < self.ai_multi_card_play_probability):
            selected.extend(non_three_groups[0][: self._choose_ai_same_rank_count(len(non_three_groups[0]))])
        else:
            selected.append(player.hand_cards[0])

        threes = same_rank_groups.get(3, [])
        can_hide_three = threes and any(card.rank != 3 for card in selected)
        if can_hide_three and self._rng.random() < self.ai_hidden_three_play_probability:
            selected.append(threes[0])
            if len(threes) > 1 and len(selected) < 4 and self._rng.random() < 0.25:
                selected.append(threes[1])

        if len(selected) == 1 and threes and selected[0].rank == 3 and len(threes) > 1:
            selected = threes[: self._choose_ai_same_rank_count(len(threes))]

        return selected

    def _choose_ai_same_rank_count(self, available_count: int) -> int:
        if available_count <= 1:
            return 1

        max_count = min(available_count, 4)
        if max_count >= 3 and self._rng.random() < 0.55:
            return self._rng.randint(3, max_count)

        return self._rng.randint(2, max_count)

    def _choose_ai_face_up_card(self, cards: list[Card]) -> Card:
        non_three_cards = [card for card in cards if card.rank != 3]
        if non_three_cards:
            return self._rng.choice(non_three_cards)

        return self._rng.choice(cards)

    def _group_cards_by_rank(self, cards: list[Card]) -> dict[int, list[Card]]:
        groups: dict[int, list[Card]] = {}
        for card in cards:
            groups.setdefault(card.rank, []).append(card)

        for grouped_cards in groups.values():
            grouped_cards.sort(key=lambda card: card.card_id)

        return groups

    def _score_ai_challenge_suspicion(self, submission: PlaySubmission) -> float:
        suspicion = 0.0
        if submission.face_up_card.rank == 3:
            suspicion += 0.4

        hidden_count = len(submission.hidden_cards)
        suspicion += hidden_count * 0.12
        if len(submission.cards) >= 3:
            suspicion += 0.16
        if len(submission.cards) >= 4:
            suspicion += 0.12

        return suspicion

    def _apply_challenge_reward(self, target_seat_index: int, challengers: list[int], success: bool) -> dict[int, int]:
        deltas: dict[int, int] = {}
        challenger_count = len(challengers)
        if success:
            target_penalty = 2 if challenger_count == 3 else 1
            deltas[target_seat_index] = self._apply_penalty(target_seat_index, target_penalty)
            if challenger_count < 3:
                for challenger in challengers:
                    deltas[challenger] = self._apply_reward(challenger, 1)
            return deltas

        target_reward = 2 if challenger_count == 3 else 1
        deltas[target_seat_index] = self._apply_reward(target_seat_index, target_reward)
        for challenger in challengers:
            deltas[challenger] = self._apply_penalty(challenger, 1)
        return deltas

    def _apply_reward(self, seat_index: int, count: int) -> int:
        changed = 0
        pool = self.fortune_pools[seat_index]
        for _ in range(count):
            try:
                bad_index = pool.index("bad")
            except ValueError:
                break
            pool[bad_index] = "good"
            changed += 1
        return changed

    def _apply_penalty(self, seat_index: int, count: int) -> int:
        changed = 0
        pool = self.fortune_pools[seat_index]
        for _ in range(count):
            try:
                good_index = pool.index("good")
            except ValueError:
                break
            pool[good_index] = "bad"
            changed -= 1
        return changed

    def _remove_cards_from_hand(self, player: PlayerState, card_ids: list[str]) -> list[Card]:
        if not card_ids:
            raise GameRuleError("invalid_operation", "At least one card is required.")

        if len(set(card_ids)) != len(card_ids):
            raise GameRuleError("invalid_operation", "Duplicate card ids are not allowed.")

        removed: list[Card] = []
        for card_id in card_ids:
            for index, card in enumerate(player.hand_cards):
                if card.card_id == card_id:
                    removed.append(player.hand_cards.pop(index))
                    break
            else:
                player.hand_cards.extend(removed)
                player.sort_hand_cards()
                raise GameRuleError("card_not_in_hand", f"Card '{card_id}' was not found in hand.")

        return removed

    def _is_legal_play(self, cards: list[Card]) -> bool:
        return any(card.rank == 3 for card in cards) or len({card.rank for card in cards}) == 1

    def _all_players_submitted(self, submissions: dict[int, object]) -> bool:
        return all(player.seat_index in submissions for player in self.players)

    def _player_public_payload(self, player: PlayerState) -> dict[str, object]:
        submission = self.play_submissions.get(player.seat_index)
        pool = self.fortune_pools[player.seat_index]
        return {
            "seat_index": player.seat_index,
            "player_id": player.player_id,
            "name": player.name,
            "player_type": player.player_type,
            "character_id": player.character_id,
            "hand_count": player.hand_count,
            "lucky_count": pool.count("good"),
            "unlucky_count": pool.count("bad"),
            "escaped_three_count": self.successful_escaped_threes[player.seat_index],
            "play_submitted": submission is not None,
            "challenge_submitted": player.seat_index in self.challenge_submissions,
            "public_play": self._public_play_payload(submission),
        }

    def _public_play_payload(self, submission: PlaySubmission | None) -> dict[str, object]:
        if submission is None or self.phase == PHASE_PLAY_SELECT:
            return {}

        return {
            "seat_index": submission.seat_index,
            "card_count": len(submission.cards),
            "hidden_count": len(submission.hidden_cards),
            "face_up_card": submission.face_up_card.to_payload(),
            "declaration": submission.declaration,
        }

    def _private_payload(self, player: PlayerState | None) -> dict[str, object]:
        if player is None:
            return {}

        return {
            "seat_index": player.seat_index,
            "hand_cards": [card.to_payload() for card in player.hand_cards],
            "submitted_play": self._submitted_play_private_payload(self.play_submissions.get(player.seat_index)),
        }

    def _submitted_play_private_payload(self, submission: PlaySubmission | None) -> dict[str, object]:
        if submission is None:
            return {}

        return {
            "card_ids": [card.card_id for card in submission.cards],
            "face_up_card_id": submission.face_up_card_id,
            "cards": [card.to_payload() for card in submission.cards],
        }

    def _challenge_state_payload(self, receiver: PlayerState | None) -> dict[str, object]:
        own_target = self.challenge_submissions.get(receiver.seat_index, None) if receiver else None
        return {
            "submitted_seat_indexes": sorted(self.challenge_submissions),
            "own_target_seat_index": own_target if own_target is not None else -2,
        }

    def _showdown_state_payload(self) -> dict[str, object]:
        if self.phase != PHASE_SHOWDOWN:
            return {}
        return {
            "started_at": self.phase_end_time - self.current_phase_delay_seconds(),
            "overview_seconds": self.showdown_overview_seconds,
            "focus_seconds": self.showdown_focus_seconds,
            "reveal_seconds": self.showdown_reveal_seconds,
            "reward_seconds": self.showdown_reward_seconds,
            "events": [
                {
                    "target_seat_index": event.target_seat_index,
                    "challenger_seat_indexes": event.challenger_seat_indexes,
                    "success": event.success,
                    "revealed_cards": [card.to_payload() for card in event.revealed_cards],
                    "fortune_deltas": [
                        {
                            "seat_index": seat,
                            "delta": delta,
                        }
                        for seat, delta in sorted(event.fortune_deltas.items())
                    ],
                    "fortune_before": event.fortune_before,
                    "fortune_after": event.fortune_after,
                }
                for event in self.showdown_events
            ],
        }

    def _fortune_counts_payload(self) -> list[dict[str, int]]:
        return [
            {"seat_index": player.seat_index,
             "lucky_count": self.fortune_pools[player.seat_index].count("good"),
             "unlucky_count": self.fortune_pools[player.seat_index].count("bad")}
            for player in self.players
        ]

    def _initial_fortune_pool(self) -> list[str]:
        bad_count = self.initial_bad_fortune_count
        good_count = self.fortune_pool_size - bad_count
        return ["bad"] * bad_count + ["good"] * good_count

    def _build_shuffled_deck(self) -> list[Card]:
        deck: list[Card] = []
        for rank in range(2, 8):
            for copy_index in range(8):
                suit = CARD_SUITS[copy_index % len(CARD_SUITS)]
                deck.append(Card(card_id=f"card_{rank}_{copy_index}", card_name=f"{suit}_{rank}", rank=rank))
        self._rng.shuffle(deck)
        return deck

    def _require_player_by_id(self, player_id: str) -> PlayerState:
        player = self.get_player_by_id(player_id)
        if player is None:
            raise GameRuleError("invalid_operation", "Player is not in this match.")
        return player

    def _require_player_by_seat(self, seat_index: int) -> PlayerState:
        player = self.get_player_by_seat(seat_index)
        if player is None:
            raise GameRuleError("invalid_operation", "Seat does not exist.")
        return player

    def _validate_match_is_active(self) -> None:
        if self.phase == PHASE_FINAL_RESULT:
            raise GameRuleError("game_finished", "The current match has already finished.")

    def _validate_match_id(self, match_id: str) -> None:
        if not match_id:
            raise GameRuleError("invalid_operation", "Match id is required.")
        if match_id != self.match_id:
            raise GameRuleError("game_not_found", "Match id does not match the current active match.")
