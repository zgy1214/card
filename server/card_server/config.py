from dataclasses import dataclass
import os


@dataclass(frozen=True)
class ServerConfig:
    host: str = "127.0.0.1"
    port: int = 8765

    @classmethod
    def from_env(cls) -> "ServerConfig":
        host = os.environ.get("CARD_SERVER_HOST", cls.host)
        port_text = os.environ.get("CARD_SERVER_PORT", str(cls.port))
        port = int(port_text)
        return cls(host=host, port=port)
