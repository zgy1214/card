import asyncio
import logging
from typing import Any

import websockets

from .config import ServerConfig
from .connection import ClientConnection
from .server_context import ServerContext


class CardGameServer:
    def __init__(self, config: ServerConfig) -> None:
        self._config = config
        self._logger = logging.getLogger("card_server")
        self._server_context = ServerContext(self._logger)

    async def handle_connection(self, websocket: Any, *_args: Any) -> None:
        connection = ClientConnection(websocket, self._logger, self._server_context)
        await connection.run()

    async def serve_forever(self) -> None:
        async with websockets.serve(
            self.handle_connection,
            self._config.host,
            self._config.port,
        ):
            self._logger.info(
                "Server listening on ws://%s:%s",
                self._config.host,
                self._config.port,
            )
            await asyncio.Future()


def run() -> int:
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    )
    config = ServerConfig.from_env()
    server = CardGameServer(config)
    asyncio.run(server.serve_forever())
    return 0


if __name__ == "__main__":
    raise SystemExit(run())
