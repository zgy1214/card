# Python Server

## Overview

This server implements the current prototype protocol from [network_protocol.md](../network_protocol.md).

Current scope:

- one WebSocket connection per online player
- client-provided `player_id` and `name`
- in-memory player, room, matchmaking, and match state
- refresh-based room list
- 4-digit room ids
- FIFO matchmaking for 3 human players
- room-created matches with optional AI seats
- authoritative turn flow
- server-side timeout and bot auto-play
- game leave/disconnect replacement by AI

## Structure

- `main.py`: startup entrypoint
- `card_server/app.py`: WebSocket server bootstrap
- `card_server/connection.py`: per-connection protocol routing and lifecycle
- `card_server/server_context.py`: shared online state, rooms, matchmaking, and match orchestration
- `card_server/game_session.py`: match rules and state transitions
- `card_server/protocol.py`: message parsing and formatting
- `card_server/models.py`: shared data models

## Run

Install dependencies:

```powershell
pip install -r server/requirements.txt
```

Start the server:

```powershell
python server/main.py
```

Optional environment variables:

- `CARD_SERVER_HOST`
- `CARD_SERVER_PORT`
