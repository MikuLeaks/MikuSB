# 命令目标与 `giveall` 参数说明（中文）

Languages: [English](COMMAND_TARGET_en.md) | 中文 | [日本語](COMMAND_TARGET_jp.md)

本文档说明为什么在控制台执行 `giveall` 时会出现“未找到玩家”，以及“ID 应该填什么”。

## 1. 目标解析规则

命令系统会按如下方式解析目标：

- 不写目标时，默认目标是**命令发送者本人**。
- 在控制台执行命令时，发送者是 `Console`，其 UID 为 `0`。
- 指定目标的语法是：`@<uid>`（例如 `@1001`）。
- 当前实现只解析 `@` 后面的**数字 UID**，不支持直接写用户名作为目标。

## 2. 为什么会“未找到玩家”

`giveall` 在执行前会检查目标是否在线（`CheckOnlineTarget()`）：

- 目标必须是**在线连接中的玩家**。
- 目标是控制台（`uid=0`）或离线玩家时，会提示“未找到玩家”。

## 3. `giveall` 参数含义（重点）

以 `weapon` 为例：

```text
/giveall weapon <detail/-1> p<particular> l<level> @<uid>
```

- `<detail/-1>`：物品 detail，`-1` 表示全部。
- `p<particular>`：物品 particular 参数（例如 `p1`）。
- `l<level>`：等级参数。
- `@<uid>`：目标玩家 UID。

注意：

- `miku` 这种字符串不会被当成目标玩家名。
- `p1` 是物品参数，不是玩家 ID。

## 4. 到底该填 UID 还是用户名？

结论：

- 目标参数请填 **UID**（`@<uid>`）。
- 不是用户名。

数据库对应关系：

- 账号表：`Account`（`[SugarTable("Account")]`）
- 玩家 ID 字段：`Uid`（主键）
- 用户名字段：`Username`

## 5. 正确示例

```text
/giveall weapon -1 p1 l90 @1001
```

表示：给 UID 为 `1001` 的在线玩家发放全部武器（particular=1，level=90）。
