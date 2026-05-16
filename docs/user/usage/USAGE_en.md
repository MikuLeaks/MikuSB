# MikuSB Usage Guide (From Zero)

Languages: English | [中文](USAGE_zh.md) | [日本語](USAGE_jp.md)

> This document focuses on: full command flow, database fields and their sources, and how data is generated.

## 1. Run from scratch (development)

### 1.1 Requirements

- Install [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Install Git

### 1.2 Get the source

```bash
git clone https://github.com/AliceJump/MikuSB.git
cd MikuSB
```

### 1.3 Build

```bash
dotnet build
```

### 1.4 Start the server

```bash
dotnet run --project ./MikuSB
```

After startup, the following services run together:

- `SdkServer` (HTTP)
- `GameServer` (TCP)
- Local proxy (enabled by default, listens on `127.0.0.1:8888`)

### 1.5 What is created on first start

- `Config/Config.json` (generated if missing)
- `Config/Database/Miku.db` (SQLite database file)
- Database tables (Code First auto-create)
- `proxy-certs/*` (proxy root CA and derived certificates)
- `Config/Handbook/*` (command handbook text generated from TextMap)

## 2. Publish commands

### 2.1 Linux single-file publish

```bash
dotnet publish ./MikuSB/MikuSB.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true --property:PublishDir=../publish
```

### 2.2 Windows publish (multi-file, same as CI)

```powershell
dotnet publish .\MikuSB\MikuSB.csproj -c Release -p:PublishProfile=MikuSB-Win64-MultiFile -o .\artifacts\publish\MikuSB
```

## 3. How resources and database are generated

### 3.1 Resource files

- On startup, the server checks key files under `Resources/` (for example `item/templates/card.json`, `item/templates/weapon.json`).
- If missing, it downloads the resource archive and extracts it into `Resources/`.
- The resource data is then loaded into in-memory `GameData.*` dictionaries/lists for later initialization of characters, weapons, and items.

### 3.2 Database and table creation

- Database type: SQLite (SqlSugar)
- Database path: `Config/Database/Miku.db` (can be changed in `Config/Config.json`)
- Table creation: scans all types that inherit `BaseDatabaseDataHelper` and runs Code First table creation

### 3.3 How data is written

- Player heartbeats enqueue UIDs for saving; by default it flushes every 5 minutes
- On process exit, a final flush is triggered

## 4. Database tables and fields (with sources)

> Primary key is unified as `Uid` (player UID).

### 4.1 `Account`

Fields:

- `Uid`: account UID. If missing on first login it is created, starting from 1.
- `Username`: username. Default for auto-created account is `"MIKU"`.
- `Password`: password hash (SHA256); empty password stores an empty string.
- `BanType`: ban type enum.
- `Phone`: phone field (default `"123456"`).
- `Permissions` (JSON): permission list from `Config.json -> ServerOption.DefaultPermissions`.
- `ComboToken` / `DispatchToken`: session tokens written when generated.

Source:

- Auto-created: first login creates UID=1 if missing.
- Also manageable via logic/commands.

### 4.2 `Player`

Fields:

- `Uid`: player UID (same as account).
- `Name`: display name, defaulted from account name (fallback to `Miku` when blank).
- `Signature`: signature (default `MikuPS`).
- `Level` / `Exp` / `Vigor` / `Gender`: base player attributes.
- `RegisterTime`: registration time (Unix seconds).
- `LastActiveTime`: last active time (refreshed on player manager init).
- `Attrs` (JSON): numeric attributes (tutorial values, currency, unlocks, etc.).
- `StrAttrs` (JSON): string attributes.
- `ShowItems` (JSON): profile display items.

Source:

- If account exists but player data does not, `PlayerGameData` is created.
- `Attrs` are filled/raised by tutorial and stage data during serialization.

### 4.3 `inventory_data`

Fields:

- `Uid`: player UID.
- `NextUniqueUid`: inventory unique item ID allocator (starts at `100000`).
- `Items` (JSON): item dictionary (supplies, AR, manifestation, etc.).
- `Weapons` (JSON): weapon dictionary.
- `Skins` (JSON): skin dictionary.
- `SupportCards` (JSON): support card dictionary.
- `SkinTypesBySkinId` (JSON): skin form mapping (`nSkinId -> nType`).

Source:

- On first player creation, initial skins/characters/supplies are granted from resource tables.
- Business requests (upgrade, replace, skin change, etc.) continually modify this table.

### 4.4 `character_data`

Fields:

- `Uid`: player UID.
- `Characters` (JSON): character list.
  - Key subfields: `Guid`, `TemplateId`, `Level`, `Break`, `Evolue`, `ProLevel`, `Trust`, `WeaponUniqueId`, `SkinId`, `WeaponSkinId`, `SupportSlots`, `UnlockedSkin`, `Spines`, `Affixs`, etc.
- `NextCharacterGuid`: character GUID counter.

Source:

- First-time player initialization creates characters from resource templates.
- Character creation automatically assigns default weapon/skin bindings.

### 4.5 `lineup_data`

Fields:

- `Uid`: player UID.
- `LineupInfo` (JSON): lineup dictionary keyed by lineup slot.
  - Subfields: `Index`, `Name`, `Member1`, `Member2`, `Member3`.

Source:

- New player initialization randomly picks 3 characters for the default lineup.
- Later lineup updates keep writing to this table.

## 5. Quick checks

### 5.1 Inspect database files and tables

```bash
ls -lah ./Config/Database
sqlite3 ./Config/Database/Miku.db ".tables"
sqlite3 ./Config/Database/Miku.db ".schema Account"
sqlite3 ./Config/Database/Miku.db ".schema Player"
```

### 5.2 Check database path in config

```bash
cat ./Config/Config.json
```

## 6. Notes

- This project stores many fields as JSON columns; read the corresponding C# data structures when needed.
- To reset local progress, stop the server, then back up or delete `Config/Database/Miku.db` and restart.
