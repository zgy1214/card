import json
from typing import Any


class ProtocolError(Exception):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


def parse_client_message(raw_message: str) -> tuple[str, dict[str, Any]]:
    try:
        message = json.loads(raw_message)
    except json.JSONDecodeError as error:
        raise ProtocolError("invalid_operation", f"Invalid JSON message: {error.msg}.") from error

    if not isinstance(message, dict):
        raise ProtocolError("invalid_operation", "Client message must be a JSON object.")

    message_type = message.get("type")
    payload = message.get("payload")
    if not isinstance(message_type, str) or not message_type:
        raise ProtocolError("invalid_operation", "Client message type is required.")

    if not isinstance(payload, dict):
        raise ProtocolError("invalid_operation", "Client message payload must be a JSON object.")

    return message_type, payload


def make_message(message_type: str, payload: dict[str, Any] | None = None) -> str:
    return json.dumps(
        {
            "type": message_type,
            "payload": payload if payload is not None else {},
        },
        separators=(",", ":"),
    )


def make_error(scope: str, code: str, message: str) -> str:
    return make_message(
        "error",
        {
            "scope": scope,
            "code": code,
            "message": message,
        },
    )
