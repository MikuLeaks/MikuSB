# MikuSB

Languages: English | [中文](docs/user/README_zh.md) | [日本語](docs/user/README_jp.md)

<strong>MikuSB</strong> is a server emulator of a certain dungeon anime game.
`SdkServer`, `GameServer`, and an optional local HTTP/HTTPS proxy are started from a single `net10.0` application.

[Discord](https://discord.gg/aMwCu9JyUR)

## Documentation

- [Linux guide](docs/user/platform/README_linux_en.md)
- [Usage guide](docs/user/usage/USAGE_en.md)
- [Command guide](docs/user/commands/COMMAND_GUIDE_en.md)
- [Command target notes](docs/user/commands/COMMAND_TARGET_en.md)

## Overview

- `SdkServer`
  - Serves HTTP APIs and dispatch responses
  - Returns server lists, version queries, and fallback responses
- `GameServer`
  - Accepts TCP-based game connections
  - Handles `ReqCallGS` and some normal packets
- `Proxy`
  - Listens on `127.0.0.1:8888` when enabled
  - Redirects some Snowbreak-related domains to the local `SdkServer`
- `Common` / `Proto` / `TcpSharp`
  - Shared data, protobuf definitions, and networking infrastructure

## Project Layout

- [MikuSB](MikuSB): entry point
- [SdkServer](SdkServer): HTTP server and dispatch
- [GameServer](GameServer): main game server
- [Proxy](Proxy): local proxy
- [Common](Common): config, database, and shared utilities
- [Proto](Proto): protobuf definitions

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## Running

1. Restore dependencies and build.

```powershell
dotnet build
```
2. Set `GamePath` in `Config/Config.json` to the path of your game executable.
3. Start the server.

```powershell
dotnet run --project .\MikuSB
```

4. Create an account in the server console.
5. Run the `game` command in the server console (arguments are passed through to the game process).
6. Start the game and log in, or launch directly with `MikuSB.exe -game [-path game_path] [-arg param1] [-arg param2]`.

For publish commands and generated data details, see the [usage guide](docs/user/usage/USAGE_en.md).

## Feature List

- [x] Login and basic account entry
- [x] Player data loading
- [x] Inventory loading
- [x] Character loading
- [x] Skin loading
- [x] Weapon loading
- [x] Lobby display character switching
- [x] Character skin switching
- [x] Character skin form switching
- [x] Weapon replacement
- [x] Weapon upgrade
- [x] Player rename
- [x] Basic saving of currently supported lobby state
- [x] Main chapter stage entry and related flow
- [x] Daily stage entry and related flow
- [x] Basic player setting synchronization
- [x] Basic profile synchronization
- [x] Activity-related requests
- [x] Achievement-related requests
- [x] Lineup-related requests
- [x] Preview-related requests
- [x] Some shop-related requests
- [ ] Full combat flow
- [ ] Mission / quest progression
- [ ] Gacha / recruitment systems
- [ ] Complete shop behavior
- [ ] Multiplayer systems
- [ ] Base / dorm systems
- [ ] Full client API coverage

## Contributors

- [Naruse](https://github.com/DevilProMT)
- [Kei-Luna](https://github.com/Kei-Luna)

## Notes on use

This software is intended for research and testing purposes in a local environment.
It is not intended for unauthorized access to, interference with, or commercial use of official services.

## Legal Disclaimer

MikuSB was developed for educational and research purposes.

- All trademarks, copyrights, and other intellectual property related to the original game and its associated franchise belong to their respective owners.
- This repository does not include any copyrighted game assets, binaries, or master data.
- Use this software at your own risk. The authors assume no responsibility for any damages or legal consequences resulting from its use.

If you are a rights holder and have any concerns regarding this software, please contact `devilpromt` or `kei_luna` on Discord.
