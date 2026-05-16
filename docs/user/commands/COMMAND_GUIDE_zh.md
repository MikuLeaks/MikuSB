# MikuSB 命令使用指南（从零开始）

Languages: [English](COMMAND_GUIDE_en.md) | 中文 | [日本語](COMMAND_GUIDE_jp.md)

> 这份文档专门讲「怎么用命令」，尤其是 `giveall`。

## 1. 先把服务跑起来

请先启动服务。安装与运行命令见[使用指导](../usage/USAGE_zh.md)。

启动成功后，控制台会提示可输入 `help` 获取命令帮助。

---

## 2. 命令在哪里输入

你可以在两个地方输入命令：

1. **服务端控制台**：直接输入命令（不带 `/`）
   - 例如：`help`
2. **游戏内聊天**：命令前带 `/`
   - 例如：`/help`

---

## 3. 命令基础语法

命令结构：

```text
<主命令> <子命令> <参数> @<目标UID>
```

- `@<目标UID>` 可选，用于指定目标玩家。
- 不写 `@` 时，默认对命令发送者生效。
- 目标解析细节见[命令目标说明](COMMAND_TARGET_zh.md)。

说明：

- 选项参数写成 `p1 l90 g9 s9`（不带 `-`）。
- `detail` / `guid` 允许使用 `-1` 表示全部。

---

## 4. 先学会 help

```text
help
help giveall
help girl
help debug
```

---

## 5. 常用命令速览

- `help [command]`（别名：`h`）
- `game <args...>`
- `account create <email> <uid>`
- `account list`
- `debug [on|off|simple|detail|file]`（别名：`dbg`）
- `girl add <detail/-1> p<particular> l<level> s<star>`（别名：`g`）
- `girl level <guid/-1> <level>`
- `girl neuronic <guid/-1> <level>`
- `girl break <guid/-1> <level>`
- `giveall <类型> <detail/-1> [选项]`（别名：`ga`）

---

## 6. giveall 怎么用（重点）

`giveall` 主命令别名是 `ga`。
可用子命令：

- `weapon`
- `card`
- `weaponskin`
- `profile`
- `skinpart`
- `weaponpart`
- `call`
- `skin`
- `furniture`

### 6.1 参数规则（很关键）

在当前实现里，选项参数建议写成：

- `p1`
- `l90`
- `g9`

即 **不要写成 `-p1` / `-l90` / `-g9`**，否则会被当成其他参数处理。

### 6.2 常见示例

```text
# 给自己所有武器，particular=1，等级90
giveall weapon -1 p1 l90

# 给 UID=1 的玩家所有武器
giveall weapon -1 p1 l90 @1

# 给自己所有支援卡
giveall card -1 p1 l80

# 给自己所有武器皮肤
giveall weaponskin -1 p1

# 给自己所有角色皮肤（genre=9 仅示例）
giveall skin -1 g9 p1 l1
```

说明：

- `detail=-1` 代表“全部”
- `detail>=0` 代表给某个具体条目

---

## 7. girl 命令

```text
girl add -1 p1 l1 s9
girl level -1 80
girl neuronic -1 6
girl break -1 45
```

---

## 8. debug 开关

```text
debug on
debug off
debug simple
debug detail
debug file
```

---

## 9. 常见问题

### Q1：提示“未找到命令”

- 用 `help` 看命令是否存在
- 游戏聊天里记得加 `/`
- 控制台里不要加 `/`

### Q2：提示“未找到玩家”

见[命令目标说明](COMMAND_TARGET_zh.md)。

### Q3：命令执行了但结果不对

- 优先用 `help <命令>` 对照参数格式
- `giveall` 选项参数按 `p1 l90 g9` 这种写法输入
