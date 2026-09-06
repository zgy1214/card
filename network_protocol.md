# 联机协议草案

## 1. 目标

当前版本实现《溜小3》的 4 人联机闭环：

- 全部在线通信使用 WebSocket。
- 服务端是权威状态来源，客户端只发送操作意图。
- 房间固定 `max_players = 4`，支持真人和 AI。
- 游戏中断线或主动离开时，未结算对局由 AI 接管该座位。
- 结算页是一个可停留阶段，不再收到结算后自动回大厅。

当前不处理：

- 账号登录
- 断线重连
- 房主自定义参数
- 复杂 AI 策略
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

- `type` 使用 `session/*`、`profile/*`、`room/*`、`matchmaking/*`、`game/*` 分组。
- `payload` 必须存在，没有内容时使用 `{}`。
- 字段命名使用小写加下划线，例如 `player_id`、`room_id`。
- 关键状态变化后，服务端下发完整快照，不做增量同步。

## 3. Gameplay 报文

服务端推送：

```text
game/match_start
game/state
game/chat
game/player_replaced_by_ai
error
```

客户端请求：

```text
game/submit_play
game/submit_challenge
game/submit_fortune_draw
game/chat
game/leave
```

不使用 `game/cancel_play`。选牌、取消选中、切换明牌都属于客户端本地交互；只有点“确认出牌”才发送 `game/submit_play`。

## 4. game/state

`game/state` 是对局主状态。服务端在开局、阶段切换、玩家提交、结算发生时推送。

```json
{
  "type": "game/state",
  "payload": {
    "match_id": "match_xxx",
    "match_status": "playing",
    "phase": "play_select",
    "round_index": 1,
    "server_time": 1710000000.0,
    "phase_end_time": 1710000020.0,
    "players": [],
    "local_player_private": {},
    "round_public": {
      "pair_count": 0,
      "pair_contains_three_count": 0,
      "escaped_three_history": []
    },
    "challenge_state": {
      "submitted_seat_indexes": [],
      "own_target_seat_index": -2
    },
    "showdown_state": {
      "events": []
    },
    "fortune_state": {},
    "final_result": {}
  }
}
```

阶段：

- `play_select`：玩家选择并确认出牌。
- `challenge_select`：展示公开声明，玩家选择是否质疑。
- `showdown`：按被质疑玩家公示暗牌和奖惩。
- `fortune_draw`：玩家从自己的气运池中抽取。
- `final_result`：展示最终排名。

关键字段：

- `players[].public_play.face_up_card.card_name` 和 `local_player_private.hand_cards[].card_name` 是英文资源 key，例如 `spade_3`、`heart_7`，不是玩家可见展示文案。
- 牌面数字展示和声明应使用 `rank`，不要从 `card_name` 里解析或展示资源名。
- `round_public.pair_count`：本轮已提交出牌中形成的对子数量。
- `round_public.pair_contains_three_count`：本轮对子信息中涉及的数字 3 数量。
- `round_public.escaped_three_history`：已完成轮次的成功溜 3 总数数组，例如 `[1, 0]` 表示第 1 轮 1 张、第 2 轮 0 张。
- `challenge_state.own_target_seat_index`：当前玩家已提交的质疑目标，`-1` 表示不质疑，`-2` 表示尚未提交。
- `showdown_state.events[].fortune_deltas`：气运变化数组，元素形如 `{ "seat_index": 0, "delta": -1 }`；正数表示霉运转好运，负数表示好运转霉运。

倒计时：

- 服务端只在 `game/state` 中给 `server_time` 和 `phase_end_time`。
- 客户端自己显示倒计时，不要求服务端每秒推送。

## 5. 请求示例

### game/submit_play

```json
{
  "type": "game/submit_play",
  "payload": {
    "match_id": "match_xxx",
    "card_ids": ["card_3_0", "card_5_2"],
    "face_up_card_id": "card_5_2"
  }
}
```

规则：

- 至少 1 张牌。
- 明牌必须来自 `card_ids`。
- 出牌合法条件：全部同数字，或者包含数字 3。
- 声明由服务端/客户端根据“总张数 + 明牌数字”生成，不允许手动填写。

### game/submit_challenge

```json
{
  "type": "game/submit_challenge",
  "payload": {
    "match_id": "match_xxx",
    "target_seat_index": 2
  }
}
```

`target_seat_index = -1` 表示不质疑。

### game/submit_fortune_draw

```json
{
  "type": "game/submit_fortune_draw",
  "payload": {
    "match_id": "match_xxx",
    "draw_count": 3
  }
}
```

P0 默认抽取范围是 `2-5`。

### game/chat

```json
{
  "type": "game/chat",
  "payload": {
    "match_id": "match_xxx",
    "message": "我觉得你有3"
  }
}
```

服务端广播时复用房间聊天结构，额外带 `match_id`。

## 6. 隐藏信息边界

- 自己的完整手牌只出现在自己的 `local_player_private.hand_cards`。
- 自己的气运池和抽取结果只出现在自己的 `local_player_private`。
- 其他玩家只看到公开信息：昵称、座位、手牌数、提交状态、公开明牌和声明。
- 客户端不能收到“不该显示但先隐藏”的敏感信息。
