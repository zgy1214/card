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
    "challenge_state": {
      "submitted_seat_indexes": [],
      "own_target_seat_index": -2
    },
    "showdown_state": {}
  }
}
```

阶段：

- `play_select`：玩家选择并确认出牌。
- `challenge_select`：展示公开声明，玩家选择是否质疑。
- `showdown`：按被质疑玩家公示暗牌和奖惩。
- `final_result`：打开结算界面，从 `players[]` 展示玩家名、当前福气/炸弹及累计成功溜走的 3。评分规则待确认，排名、分数、称号暂显示“—”。

关键字段：

- `players[].public_play.face_up_card.card_name` 和 `local_player_private.hand_cards[].card_name` 是英文资源 key，例如 `spade_3`、`heart_7`，不是玩家可见展示文案。
- 牌面数字展示和声明应使用 `rank`，不要从 `card_name` 里解析或展示资源名。
- `challenge_state.own_target_seat_index`：当前玩家已提交的质疑目标，`-1` 表示不质疑，`-2` 表示尚未提交。
- `showdown_state.events[].fortune_deltas`：气运变化数组，元素形如 `{ "seat_index": 0, "delta": -1 }`；正数表示霉运转好运，负数表示好运转霉运。

### 公示与福气展示

- `players[].lucky_count` / `unlucky_count`：该玩家本局当前福气 / 炸弹数量，公开给所有接收方。每局初始为 `4/4`，转换保持总数 `8`，跨回合保留；服务端为唯一判定来源。
- `players[].escaped_three_count`：本局累计成功溜走的 3 张数。某玩家本轮未被任何人质疑时，其本轮打出的全部 3（含明牌）计入；被质疑则不计入。跨回合保留，结算表“小3”列直接显示该数值。
- `showdown_state` 仅在 `showdown` 阶段有内容。`events` 按被质疑者的座位升序排列，同一目标的多个质疑人合并成一个事件；事件中 `target_seat_index` 才是昵称旁标记“抓”的玩家。
- 每个事件保留 `revealed_cards`（只有该目标的暗牌）、`challenger_seat_indexes`、`success` 和实际 `fortune_deltas`，新增 `fortune_before` / `fortune_after` 数组，元素为 `{ "seat_index": 0, "lucky_count": 4, "unlucky_count": 4 }`。每组快照含四名玩家，表示该组转换前后的数量，相邻事件快照连续。
- 时间轴由 `started_at`（服务端 Unix 秒）和 `overview_seconds`、`focus_seconds`、`reveal_seconds`、`reward_seconds` 指定。第一版依次为 `2.0` 秒总览，每组 `1.2` 秒聚焦、`1.6` 秒揭牌、`1.8` 秒属性转换。`phase_end_time = started_at + overview_seconds + 事件数 × 每组时长`。
- 客户端用 `server_time` 加接收后的本地流逝时间定位当前步骤；重连和重复状态推送不重播、不累计属性。总览显示首组 `fortune_before`，聚焦/揭牌显示当前组 `fortune_before`，转换显示当前组 `fortune_after` 并高亮转换的图标。`players[]` 是已结算的最终数量，不能用于提前覆盖公示中的中间值。
- 第三轮在上述完整公示时间后额外停留 `3.0` 秒，因此该轮 `phase_end_time` 在上述公式基础上加 `3.0` 秒，`started_at` 仍为实际公示起点。随后服务端进入 `final_result`，`match_status` 设为 `finished`，不执行旧随机抽福气或旧评分算法。客户端收到该阶段后关闭对局界面并打开结算界面，重复报文不重复打开。
- 全员不质疑时 `events` 为空，只展示总览；前两轮结束总览即进入下一轮，第三轮总览后同样额外停留 `3.0` 秒再进入结算。不公开未被质疑者的暗牌。质疑提交报文与交互不变。

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

## 当前版本协议边界

客户端与服务端须配套更新，不再兼容旧公示时间或缺失属性字段。缺失必需数据视为协议错误，不猜测时间、不保留旧属性代替新报文。

已移除抽福气接口、旧评分/称号数据、未使用的对子统计与私有气运池副本。结算仅展示玩家名、最终福气/炸弹及成功溜走的 3；排名、分数和称号暂显示“—”。
