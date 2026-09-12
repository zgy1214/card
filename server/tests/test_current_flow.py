import unittest

from card_server.connection import ClientConnection
from card_server.game_session import GameRuleError
from card_server.protocol import ProtocolError, parse_client_message
from test_showdown import make_session


class CurrentFlowTests(unittest.TestCase):
    def test_three_round_timeout_flow_has_no_legacy_payloads(self):
        session = make_session({})
        session.start()
        for round_index in range(1, 4):
            for phase in ("play_select", "challenge_select", "showdown"):
                self.assertEqual((session.phase, session.round_index), (phase, round_index))
                state = session.to_game_state_payload("p0")
                self.assertFalse({"round_public", "fortune_state", "final_result"} & state.keys())
                self.assertNotIn("fortune_pool", state["local_player_private"])
                for player in state["players"]:
                    self.assertEqual(player["lucky_count"] + player["unlucky_count"], 8)
                session.handle_phase_timeout(phase, round_index)
        self.assertEqual(session.phase, "final_result")
        self.assertFalse(session.is_playing)
        with self.assertRaises(GameRuleError):
            session.submit_challenge(session.match_id, "p0", -1)

    def test_disconnect_during_challenge_replaces_player_and_resolves(self):
        session = make_session({0: -1, 1: -1, 2: -1})
        replacement = session.replace_player_with_ai("p3")
        self.assertEqual(replacement[0], 3)
        self.assertEqual(session.phase, "showdown")

    def test_repeated_challenge_does_not_replace_choice(self):
        session = make_session({0: -1})
        with self.assertRaises(GameRuleError):
            session.submit_challenge(session.match_id, "p0", 1)
        self.assertEqual(session.challenge_submissions[0], -1)

    def test_message_payload_is_required(self):
        with self.assertRaises(ProtocolError):
            parse_client_message('{"type":"room/list"}')
        self.assertEqual(parse_client_message('{"type":"room/list","payload":{}}'), ("room/list", {}))

    def test_boolean_is_not_a_seat_number(self):
        with self.assertRaises(ProtocolError):
            ClientConnection._require_int(None, {"seat": True}, "seat")


if __name__ == "__main__":
    unittest.main()
