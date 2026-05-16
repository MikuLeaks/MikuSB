# Command targets and `giveall` parameters (EN)

Languages: English | [中文](COMMAND_TARGET_zh.md) | [日本語](COMMAND_TARGET_jp.md)

This document explains why running `giveall` in the console can show “player not found”, and what ID you should provide.

## 1. Target resolution rules

The command system resolves targets as follows:

- If no target is provided, the default target is the **command sender**.
- When running in the console, the sender is `Console` with UID `0`.
- Target syntax is: `@<uid>` (for example, `@1001`).
- The current implementation only parses numeric UIDs after `@`; it does not accept usernames.

## 2. Why “player not found” happens

`giveall` checks whether the target is online (`CheckOnlineTarget()`):

- The target must be an **online connected player**.
- If the target is the console (`uid=0`) or offline, it will report “player not found”.

## 3. `giveall` parameter meaning (important)

Using `weapon` as an example:

```text
/giveall weapon <detail/-1> p<particular> l<level> @<uid>
```

- `<detail/-1>`: item detail, `-1` means all.
- `p<particular>`: item particular parameter (for example `p1`).
- `l<level>`: level parameter.
- `@<uid>`: target player UID.

Notes:

- Strings like `miku` are not treated as target usernames.
- `p1` is an item parameter, not a player ID.

## 4. UID or username?

Conclusion:

- Use **UID** as the target parameter (`@<uid>`).
- Do not use usernames.

Database mapping:

- Account table: `Account` (`[SugarTable("Account")]`)
- Player ID field: `Uid` (primary key)
- Username field: `Username`

## 5. Correct example

```text
/giveall weapon -1 p1 l90 @1001
```

Meaning: give all weapons to the online player with UID `1001` (particular=1, level=90).
