# MikuSB Command Guide (From Zero)

Languages: English | [中文](COMMAND_GUIDE_zh.md) | [日本語](COMMAND_GUIDE_jp.md)

> This guide explains how to use commands, especially `giveall`.

## 1. Start the server

Start the server first. Setup and run commands are covered in the [usage guide](../usage/USAGE_en.md).

After startup, the console will show that you can type `help` for command help.

---

## 2. Where to enter commands

You can enter commands in two places:

1. **Server console**: enter commands directly (no `/`)
   - Example: `help`
2. **In-game chat**: prefix commands with `/`
   - Example: `/help`

---

## 3. Basic command syntax

Command structure:

```text
<main> <sub> <args> @<targetUID>
```

- `@<targetUID>` is optional and specifies the target player.
- Without `@`, the command applies to the sender by default.
- Target resolution details are covered in [command target notes](COMMAND_TARGET_en.md).

Notes:

- Options are passed as `p1 l90 g9 s9` (no leading `-`).
- `detail`/`guid` can be `-1` to apply to all.

---

## 4. Learn `help` first

```text
help
help giveall
help girl
help debug
```

---

## 5. Common commands (quick list)

- `help [command]` (alias: `h`)
- `game <args...>`
- `account create <email> <uid>`
- `account list`
- `debug [on|off|simple|detail|file]` (alias: `dbg`)
- `girl add <detail/-1> p<particular> l<level> s<star>` (alias: `g`)
- `girl level <guid/-1> <level>`
- `girl neuronic <guid/-1> <level>`
- `girl break <guid/-1> <level>`
- `giveall <type> <detail/-1> [options]` (alias: `ga`)

---

## 6. How to use `giveall` (important)

The main command `giveall` is also aliased as `ga`.
Available subcommands:

- `weapon`
- `card`
- `weaponskin`
- `profile`
- `skinpart`
- `weaponpart`
- `call`
- `skin`
- `furniture`

### 6.1 Parameter rules (important)

In the current implementation, option parameters should be written as:

- `p1`
- `l90`
- `g9`

Do **not** write `-p1` / `-l90` / `-g9` or they will be parsed as other parameters.

### 6.2 Common examples

```text
# Give yourself all weapons, particular=1, level 90
giveall weapon -1 p1 l90

# Give all weapons to UID=1
giveall weapon -1 p1 l90 @1

# Give yourself all support cards
giveall card -1 p1 l80

# Give yourself all weapon skins
giveall weaponskin -1 p1

# Give yourself all character skins (genre=9 is just an example)
giveall skin -1 g9 p1 l1
```

Notes:

- `detail=-1` means “all”
- `detail>=0` means a specific item

---

## 7. `girl` commands

```text
girl add -1 p1 l1 s9
girl level -1 80
girl neuronic -1 6
girl break -1 45
```

---

## 8. Debug toggles

```text
debug on
debug off
debug simple
debug detail
debug file
```

---

## 9. FAQ

### Q1: “Command not found”

- Use `help` to confirm the command exists
- Remember `/` in chat
- Do not use `/` in the server console

### Q2: “Player not found”

See [command target notes](COMMAND_TARGET_en.md).

### Q3: Command ran but results look wrong

- Check `help <command>` for the required parameters
- For `giveall`, use the `p1 l90 g9` style
