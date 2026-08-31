# 联机协议草案

## 1. 目标

当前版本实现一个轻量联机 demo：

- 全部在线通信使用 WebSocket。
- 服务器是权威状态来源。
- 客户端维护 `player_id` 和 `name`，服务器维护在线连接、玩家状态、房间、匹配队列和对局。
- 支持大厅刷新房间列表、创建房间、通过房间号加入、房间 ready、房间内添加 AI。
- 支持 FIFO 快速匹配，凑满真人后立即开局。
- 支持游戏中玩家退出后由 AI 接管。

当前不处理：

- 账号登录
- 断线重连
- 心跳业务协议
- 复杂匹配评分
- 多服务器扩展

## 2. 基础格式

所有消息都是 JSON：

```json
{
  "type": "module/action",
  "payload": {}
}
```

约定：

- `type` 使用 `session/*`、`room/*`、`matchmaking/*`、`game/*` 分组。
- `payload` 必须存在，没有内容时使用 `{}`。
- 字段命名使用小写加下划线，例如 `player_id`、`room_id`。
- 客户端只发送操作意图，关键状态必须等待服务器确认。
- 服务端在关键状态变化后下发全量状态，例如 `room/state`、`game/match_state`。

## 3. 状态约定

### 3.1 玩家主状态

同一玩家同一时间只能处于一种主状态：

- `idle`：空闲，可以匹配、建房、加入房间。
- `matchmaking`：匹配中，不能建房或加入房间。
- `room`：房间中，不能开始匹配。
- `game`：游戏中，不能进入大厅房间流程。

客户端和服务器都需要校验状态。服务器校验为准。

### 3.2 标识符

- `player_id`：客户端生成 UUID，写入本地存档。
- `name`：客户端玩家名；首次无存档时生成类似 `player_A7K2`。
- `room_id`：服务器生成 4 位数字字符串，只保证当前活跃房间内唯一。
- `match_id`：服务器生成，对局唯一标识。
- `seat_index`：座位编号，从 `0` 开始；当前默认 `max_players = 3`，后续可扩展。
- `player_type`：`human` 或 `ai`。

### 3.3 房间规则

- 房间有 `name`、`room_id`、房主、玩家列表、ready 状态。
- 房主不能踢真人玩家。
- 房主可以添加或移除 AI。
- AI 占座位，不能成为房主。
- 房主离开时自动转让给其他真人；没有真人时房间销毁。
- 玩家进出、AI 增删、房主变化时清空真人 ready 状态。
- 房主不需要 ready。
- 开局条件：座位满、非房主真人 ready、房主发送 `room/start_game`。
- 开局后房间从大厅列表隐藏。

### 3.4 匹配规则

- 当前只有一个 FIFO 队列。
- 匹配只接受真人，不补 AI。
- 队列人数变化时向队列内玩家推送 `matchmaking/state`。
- 凑满 `required_count` 后立即创建对局。
- 客户端取消匹配必须等待服务器确认。
- 如果取消请求到达前已匹配成功，取消作废，客户端收到 `matchmaking/found` 后进入游戏。

### 3.5 游戏规则

当前仍使用简单轮流出牌 demo：

- 使用一副 52 张牌。
- 每个玩家开局 5 张手牌。
- 每回合只能出 1 张牌。
- 回合时长固定为 10 秒。
- 超时未出牌时由服务器随机出牌。
- 谁先出完手牌谁获胜。
- 游戏中玩家主动退出或断线后，该座位由 AI 接管，对局继续。
- 如果所有真人都离开，服务器可以直接销毁对局。

## 4. 通用错误

### `error`

用途：通知本次操作失败。客户端第一版只需要 toast 提示，不自行推导状态。

```json
{
  "type": "error",
  "payload": {
    "scope": "room",
    "code": "room_not_found",
    "message": "Room not found."
  }
}
```

字段：

- `scope`：错误所属模块，例如 `session`、`room`、`matchmaking`、`game`。
- `code`：错误码。
- `message`：可展示或可记录的错误说明。

常用错误码：

- `invalid_operation`
- `duplicate_player`
- `invalid_state`
- `room_not_found`
- `room_full`
- `not_room_owner`
- `not_ready`
- `game_not_found`
- `not_your_turn`
- `card_not_in_hand`
- `game_finished`

## 5. Session

### `session/hello`

客户端连接后发送。服务器若发现相同 `player_id` 已在线，拒绝新连接。

```json
{
  "type": "session/hello",
  "payload": {
    "player_id": "8a4f1f29-2d5a-46ef-a665-293fd67b68cc",
    "name": "player_A7K2",
    "client_version": "0.1.0"
  }
}
```

### `session/hello_ack`

```json
{
  "type": "session/hello_ack",
  "payload": {
    "player_id": "8a4f1f29-2d5a-46ef-a665-293fd67b68cc",
    "name": "player_A7K2"
  }
}
```

## 6. 大厅和房间

### `room/list`

客户端进入大厅或点击刷新时发送。

```json
{
  "type": "room/list",
  "payload": {}
}
```

### `room/list_result`

只返回可加入或可展示的房间摘要。

```json
{
  "type": "room/list_result",
  "payload": {
    "rooms": [
      {
        "room_id": "1234",
        "name": "player_A7K2 的房间",
        "owner_name": "player_A7K2",
        "status": "waiting",
        "player_count": 2,
        "max_players": 3
      }
    ]
  }
}
```

### `room/create`

```json
{
  "type": "room/create",
  "payload": {
    "name": "我的房间"
  }
}
```

成功后服务器向房间内玩家发送 `room/state`。

### `room/join`

```json
{
  "type": "room/join",
  "payload": {
    "room_id": "1234"
  }
}
```

成功后服务器向房间内玩家广播 `room/state`。

### `room/leave`

```json
{
  "type": "room/leave",
  "payload": {
    "room_id": "1234"
  }
}
```

成功后离开者回到大厅，其余房间玩家收到新的 `room/state`。如果房间无人则销毁。

### `room/ready`

```json
{
  "type": "room/ready",
  "payload": {
    "room_id": "1234",
    "is_ready": true
  }
}
```

房主不需要 ready。服务器广播新的 `room/state`。

### `room/add_ai`

仅房主可用。

```json
{
  "type": "room/add_ai",
  "payload": {
    "room_id": "1234"
  }
}
```

### `room/remove_ai`

仅房主可用。

```json
{
  "type": "room/remove_ai",
  "payload": {
    "room_id": "1234",
    "seat_index": 2
  }
}
```

### `room/start_game`

仅房主可用。满足开局条件后，服务器创建对局并发送游戏内消息。

```json
{
  "type": "room/start_game",
  "payload": {
    "room_id": "1234"
  }
}
```

成功后的发送顺序：

1. `game/match_start`
2. `game/match_state`
3. `game/turn_start`

### `room/state`

房间完整状态。

```json
{
  "type": "room/state",
  "payload": {
    "room_id": "1234",
    "name": "我的房间",
    "status": "waiting",
    "owner_player_id": "8a4f1f29-2d5a-46ef-a665-293fd67b68cc",
    "max_players": 3,
    "players": [
      {
        "seat_index": 0,
        "player_id": "8a4f1f29-2d5a-46ef-a665-293fd67b68cc",
        "name": "player_A7K2",
        "player_type": "human",
        "is_owner": true,
        "is_ready": false
      },
      {
        "seat_index": 1,
        "player_id": "ai_001",
        "name": "AI 1",
        "player_type": "ai",
        "is_owner": false,
        "is_ready": true
      }
    ]
  }
}
```

## 7. 匹配

### `matchmaking/start`

```json
{
  "type": "matchmaking/start",
  "payload": {}
}
```

成功进入队列后，服务器发送 `matchmaking/state`。

### `matchmaking/cancel`

```json
{
  "type": "matchmaking/cancel",
  "payload": {}
}
```

取消结果以服务器消息为准：

- 收到 `matchmaking/state` 且 `status = idle`：取消成功。
- 收到 `matchmaking/found`：取消失败，已经匹配成功。

### `matchmaking/state`

```json
{
  "type": "matchmaking/state",
  "payload": {
    "status": "queued",
    "current_count": 2,
    "required_count": 3
  }
}
```

字段：

- `status`：`idle` 或 `queued`。
- `current_count`：当前队列人数。
- `required_count`：开局所需人数。

### `matchmaking/found`

匹配成功，客户端进入游戏流程。

```json
{
  "type": "matchmaking/found",
  "payload": {
    "match_id": "match_abcd"
  }
}
```

成功后的发送顺序：

1. `matchmaking/found`
2. `game/match_start`
3. `game/match_state`
4. `game/turn_start`

## 8. 游戏内消息

### `game/play_card`

客户端尝试出牌。

```json
{
  "type": "game/play_card",
  "payload": {
    "match_id": "match_abcd",
    "card_id": "S_A"
  }
}
```

服务端校验：

- 对局存在且未结束。
- 当前玩家属于该对局。
- 当前轮到该玩家。
- `card_id` 在该玩家手牌中。

### `game/leave`

玩家主动退出游戏。该玩家回大厅，游戏内座位由 AI 接管。

```json
{
  "type": "game/leave",
  "payload": {
    "match_id": "match_abcd"
  }
}
```

### `game/match_start`

```json
{
  "type": "game/match_start",
  "payload": {
    "match_id": "match_abcd"
  }
}
```

### `game/match_state`

对局完整状态。每个客户端只收到自己的完整手牌，其他玩家只收到手牌数量。

```json
{
  "type": "game/match_state",
  "payload": {
    "match_id": "match_abcd",
    "match_status": "playing",
    "turn": 1,
    "current_seat_index": 0,
    "current_played_card": {},
    "players": [
      {
        "seat_index": 0,
        "player_id": "8a4f1f29-2d5a-46ef-a665-293fd67b68cc",
        "name": "player_A7K2",
        "player_type": "human",
        "hand_count": 5,
        "hand_cards": [
          {
            "card_id": "S_A",
            "card_name": "AS"
          }
        ]
      },
      {
        "seat_index": 1,
        "player_id": "f230b5e1-d82a-4536-92e0-2e1d14033e56",
        "name": "player_Z9Q1",
        "player_type": "human",
        "hand_count": 5,
        "hand_cards": []
      },
      {
        "seat_index": 2,
        "player_id": "ai_001",
        "name": "AI 1",
        "player_type": "ai",
        "hand_count": 5,
        "hand_cards": []
      }
    ]
  }
}
```

字段：

- `match_status`：`playing` 或 `finished`。
- `current_played_card`：没有已出牌时使用 `{}`。
- `players[].hand_cards`：只对接收方自己的座位下发完整手牌。

### `game/turn_start`

```json
{
  "type": "game/turn_start",
  "payload": {
    "match_id": "match_abcd",
    "turn": 2,
    "current_seat_index": 1,
    "remaining_seconds": 10
  }
}
```

客户端收到后开始本地倒计时表现。真正超时判定仍由服务器负责。

### `game/card_played`

```json
{
  "type": "game/card_played",
  "payload": {
    "match_id": "match_abcd",
    "seat_index": 1,
    "card": {
      "card_id": "D_K",
      "card_name": "KD"
    }
  }
}
```

成功出牌后的发送顺序：

1. `game/card_played`
2. `game/match_state`
3. `game/turn_start` 或 `game/match_end`

### `game/player_replaced_by_ai`

玩家退出或断线后，该座位由 AI 接管。

```json
{
  "type": "game/player_replaced_by_ai",
  "payload": {
    "match_id": "match_abcd",
    "seat_index": 1,
    "player_id": "f230b5e1-d82a-4536-92e0-2e1d14033e56",
    "ai_player_id": "ai_replace_1"
  }
}
```

随后服务器广播新的 `game/match_state`。

### `game/match_end`

```json
{
  "type": "game/match_end",
  "payload": {
    "match_id": "match_abcd",
    "match_status": "finished",
    "winner_seat_index": 0
  }
}
```

客户端收到后进入结算或返回大厅。当前版本默认最终回到大厅。

## 9. 主要流程

### 9.1 连接

1. 客户端连接 WebSocket。
2. 客户端发送 `session/hello`。
3. 服务器返回 `session/hello_ack`。
4. 客户端进入大厅。

### 9.2 刷新大厅

1. 客户端发送 `room/list`。
2. 服务器返回 `room/list_result`。

### 9.3 创建房间并开始游戏

1. 房主发送 `room/create`。
2. 服务器返回 `room/state`。
3. 其他玩家通过 `room/join` 加入。
4. 非房主真人发送 `room/ready`。
5. 房主可发送 `room/add_ai` 或 `room/remove_ai`。
6. 满足开局条件后房主发送 `room/start_game`。
7. 服务器发送 `game/match_start -> game/match_state -> game/turn_start`。

### 9.4 快速匹配

1. 客户端发送 `matchmaking/start`。
2. 服务器发送 `matchmaking/state queued`。
3. 队列人数变化时服务器推送新的 `matchmaking/state`。
4. 客户端取消时发送 `matchmaking/cancel` 并等待服务器确认。
5. 凑满人后服务器发送 `matchmaking/found -> game/match_start -> game/match_state -> game/turn_start`。

### 9.5 游戏内出牌

1. 当前回合玩家发送 `game/play_card`。
2. 服务器校验并执行。
3. 服务器发送 `game/card_played`。
4. 服务器发送最新 `game/match_state`。
5. 未结束则发送下一次 `game/turn_start`，已结束则发送 `game/match_end`。

### 9.6 游戏中退出

1. 玩家发送 `game/leave` 或连接断开。
2. 该玩家客户端回大厅。
3. 对局内该座位由 AI 接管。
4. 其他玩家收到 `game/player_replaced_by_ai` 和新的 `game/match_state`。
5. 如果所有真人都离开，服务器可以销毁对局。
