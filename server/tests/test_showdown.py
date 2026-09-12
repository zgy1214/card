import random
import unittest

from card_server.game_session import GameSession, PlaySubmission, PHASE_SHOWDOWN, PHASE_CHALLENGE_SELECT
from card_server.models import Card, RoomPlayer


def make_session(challenges, caught_seats=()):
    players = [RoomPlayer(i, f"p{i}", f"玩家{i}", ("mengshen", "naibao", "xiaomu", "ayi")[i], "human") for i in range(4)]
    session = GameSession(players, random.Random(1))
    session.start()
    for seat in range(4):
        rank = 3 if seat in caught_seats else 5
        cards = [Card(f"face{seat}", "spade_5", 5), Card(f"hidden{seat}", f"club_{rank}", rank), Card(f"hiddenb{seat}", "heart_5", 5)]
        session.play_submissions[seat] = PlaySubmission(seat, cards, cards[0].card_id)
    session.challenge_submissions = dict(challenges)
    session.phase = PHASE_CHALLENGE_SELECT
    return session


class ShowdownTests(unittest.TestCase):
    def test_escaped_threes_include_face_up_and_hidden_but_exclude_challenged(self):
        session = make_session({0: 2, 1: -1, 2: -1, 3: -1}, (1, 2))
        cards = [Card("face3", "spade_3", 3), Card("hidden3", "club_3", 3)]
        session.play_submissions[3] = PlaySubmission(3, cards, "face3")
        session._resolve_challenges()
        self.assertEqual([p["escaped_three_count"] for p in session.to_game_state_payload("p0")["players"]], [0, 1, 0, 2])
        session.handle_phase_timeout(PHASE_SHOWDOWN, 1)
        self.assertEqual([p["escaped_three_count"] for p in session.to_game_state_payload("p0")["players"]], [0, 1, 0, 2])

    def test_initial_counts_are_public_for_all_receivers(self):
        session = make_session({})
        for receiver in ("p0", "p2", None):
            state = session.to_game_state_payload(receiver)
            self.assertEqual([(p["lucky_count"], p["unlucky_count"]) for p in state["players"]], [(4, 4)] * 4)
            self.assertEqual(state["showdown_state"], {})

    def test_groups_are_ordered_and_snapshot_chain_preserves_conversion(self):
        session = make_session({0: 2, 1: 2, 2: 0, 3: -1}, (2,))
        session._resolve_challenges()
        state = session.to_game_state_payload("p0")
        events = state["showdown_state"]["events"]
        self.assertEqual([e["target_seat_index"] for e in events], [0, 2])
        self.assertEqual(events[1]["challenger_seat_indexes"], [0, 1])
        self.assertEqual(events[0]["fortune_before"], [{"seat_index": i, "lucky_count": 4, "unlucky_count": 4} for i in range(4)])
        self.assertEqual(events[0]["fortune_after"], events[1]["fortune_before"])
        for e in events:
            for before, after in zip(e["fortune_before"], e["fortune_after"]):
                self.assertEqual(after["lucky_count"] + after["unlucky_count"], 8)
                delta = next((d["delta"] for d in e["fortune_deltas"] if d["seat_index"] == after["seat_index"]), 0)
                self.assertEqual(after["lucky_count"] - before["lucky_count"], delta)
        self.assertEqual(events[-1]["fortune_after"], session._fortune_counts_payload())
        self.assertAlmostEqual(session.current_phase_delay_seconds(), 11.2)
        self.assertAlmostEqual(state["phase_end_time"] - state["showdown_state"]["started_at"], 11.2, places=5)

    def test_last_round_opens_settlement_after_showdown_and_pause(self):
        for challenges in ({i: -1 for i in range(4)}, {0: 2, 1: -1, 2: -1, 3: -1}):
            with self.subTest(challenges=challenges):
                session = make_session(challenges, (2,))
                session.round_index = session.max_round
                session._resolve_challenges()
                playback = 2 + len(session.showdown_events) * 4.6
                self.assertAlmostEqual(session.current_phase_delay_seconds(), playback + 3)
                payload = session.to_game_state_payload("p0")
                self.assertAlmostEqual(payload["phase_end_time"] - payload["showdown_state"]["started_at"], playback + 3, places=5)
                counts = session._fortune_counts_payload()
                session.handle_phase_timeout(PHASE_SHOWDOWN, session.max_round)
                self.assertEqual(session.phase, "final_result")
                self.assertEqual(session.match_status, "finished")
                self.assertNotIn("fortune_state", session.to_game_state_payload("p0"))
                self.assertNotIn("final_result", session.to_game_state_payload("p0"))
                self.assertEqual(session._fortune_counts_payload(), counts)
                session.handle_phase_timeout(PHASE_SHOWDOWN, session.max_round)
                self.assertEqual(session.phase, "final_result")

    def test_three_challengers_retain_special_rules(self):
        for success in (True, False):
            with self.subTest(success=success):
                session = make_session({0: -1, 1: 0, 2: 0, 3: 0}, (0,) if success else ())
                session._resolve_challenges()
                self.assertEqual(len(session.showdown_events), 1)
                self.assertEqual(session.showdown_events[0].fortune_deltas, {0: -2} if success else {0: 2, 1: -1, 2: -1, 3: -1})

    def test_saturated_pool_reports_actual_zero_conversion(self):
        session = make_session({0: -1, 1: 0, 2: -1, 3: -1}, (0,))
        session.fortune_pools[0] = ["bad"] * 8
        session.fortune_pools[1] = ["good"] * 8
        session._resolve_challenges()
        event = session.showdown_events[0]
        self.assertEqual(event.fortune_deltas, {0: 0, 1: 0})
        self.assertEqual(event.fortune_before, event.fortune_after)

    def test_no_challenge_has_only_overview_and_no_hidden_reveal(self):
        session = make_session({i: -1 for i in range(4)}, (1,))
        session._resolve_challenges()
        self.assertEqual(session.showdown_events, [])
        self.assertEqual(session.current_phase_delay_seconds(), 2)
        payload = session.to_game_state_payload("p0")
        self.assertNotIn("hidden1", str(payload))
        session.handle_phase_timeout(PHASE_SHOWDOWN, 1)
        self.assertEqual((session.phase, session.round_index), ("play_select", 2))
        self.assertEqual(session._showdown_state_payload(), {})

    def test_only_challenged_hidden_cards_are_public(self):
        session = make_session({0: 2, 1: -1, 2: -1, 3: -1}, (2,))
        session._resolve_challenges()
        payload = session.to_game_state_payload(None)
        self.assertIn("hidden2", str(payload))
        self.assertNotIn("hidden1", str(payload))
        self.assertNotIn("hidden3", str(payload))

    def test_repeated_state_and_stale_timeout_do_not_change_results(self):
        session = make_session({0: 2, 1: -1, 2: 0, 3: -1}, (2,))
        session._resolve_challenges()
        first = session._showdown_state_payload()
        session.handle_phase_timeout(PHASE_CHALLENGE_SELECT, 1)
        self.assertEqual(session._showdown_state_payload(), first)
        session.handle_phase_timeout(PHASE_SHOWDOWN, 1)
        saved = session._fortune_counts_payload()
        session.handle_phase_timeout(PHASE_SHOWDOWN, 1)
        self.assertEqual(session._fortune_counts_payload(), saved)
        self.assertEqual(session.round_index, 2)


if __name__ == "__main__":
    unittest.main()
