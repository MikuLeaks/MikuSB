# MikuSB

Snowbreak: Containment Zone private server reimplementation written in C#.  
`SdkServer`, `GameServer`, and an optional local HTTP/HTTPS proxy are started from a single `net9.0` application.  

[Discord](https://discord.gg/aMwCu9JyUR)

日本語のドキュメントは [README_jp.md](README_jp.md) にあります。

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

- .NET SDK 9.0

## Running

1. Restore dependencies and build.

```powershell
dotnet build
```

2. Enjoy.

## Feature List

* [x] Login and basic account entry
* [x] Player data loading
* [x] Inventory loading
* [x] Character loading
* [x] Skin loading
* [x] Weapon loading
* [x] Lobby display character switching
* [x] Character skin switching
* [x] Character skin form switching
* [x] Weapon replacement
* [x] Weapon upgrade
* [x] Player rename
* [x] Basic saving of currently supported lobby state
* [✓] Main chapter stage entry and related flow
* [✓] Daily stage entry and related flow
* [✓] Basic player setting synchronization
* [✓] Basic profile synchronization
* [✓] Activity-related requests
* [✓] Achievement-related requests
* [✓] Lineup-related requests
* [✓] Preview-related requests
* [✓] Some shop-related requests
* [ ] Full combat flow
* [ ] Mission / quest progression
* [ ] Gacha / recruitment systems
* [ ] Complete shop behavior
* [ ] Multiplayer systems
* [ ] Base / dorm systems
* [ ] Full client API coverage

## Contributors
- [Naruse](https://github.com/DevilProMT)
- [Kei-Luna](https://github.com/Kei-Luna)

## Notes on use
This software is intended for research and testing purposes in a local environment.  
It is not intended for unauthorized access to, interference with, or commercial use of official services.

## Legal Disclaimer
MikuSB was developed for educational and research purposes.  
- This project is not affiliated with or endorsed by SEASUN GAMES PTE. LTD.  
- All trademarks, copyrights, and other intellectual property related to the original game and its associated franchise belong to their respective owners.  
- This repository does not include any copyrighted game assets, binaries, or master data.  
- Use this software at your own risk. The authors assume no responsibility for any damages or legal consequences resulting from its use.  

If you are a rights holder and have any concerns regarding this software, please contact `devilpromt` or `kei_luna` on Discord.