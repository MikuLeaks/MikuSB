# Branch update summary (based on origin/main)

> Branch: `copilot/analyze-login-rejection`
> Baseline: `origin/main`
> Stats: 13 commits, 5 files changed (+163 / -141)

## 1. Commit list (oldest to newest)

1. `038e236` feat: force login to MIKU account
2. `8ea8b75` fix: handle forced account fallback exceptions
3. `13c7ed8` refactor: share forced account resolution for login
4. `89c8e81` feat: auto login first account from database
5. `1d217f0` refactor: select first account for auto-login fallback
6. `60b091d` feat: initialize default account at startup for new database
7. `f502007` fix: avoid logging startup account identifiers
8. `fbb31c8` fix: use random password for startup-initialized account
9. `2ca4f0a` feat: auto grant level 90 weapons on new player initialization
10. `61a231f` feat: initialize all giveall items for new players
11. `80fbf48` fix: backfill full player initialization on empty login data
12. `25c76c2` fix: require exactly three characters before default lineup init
13. `8582e3c` fix: set bootstrap equipment and character progression to level 80

## 2. File-level overview

### 1) `Common/Database/Account/AccountData.cs`
- Added `GetFirstAccount()`:
  - Selects the first account by ascending `Uid`.
  - Serves as the shared fallback for auto-login.

### 2) `MikuSB/Program/LoaderManager.cs`
- Added startup initialization for fresh databases:
  - Detects first-run by checking the database file.
  - Calls `InitializeStartupData()` on first run.
  - Creates a default account `MIKU` (`Uid=1`) when no account exists.
  - Initial password is a random session key.

### 3) `SdkServer/Handlers/RouteController.cs`
- SDK login logic is consolidated to the “first account auto-login” path:
  - `/seasun/login`
  - `/seasun/loginByToken`
- Removed the earlier token/email/uid multi-source resolution path.
- Returns a unified login-failed response when no account is found.

### 4) `GameServer/Server/Packet/Recv/Login/HandlerReqLogin.cs`
- Added auto fallback on login packet handling:
  - If token/dispatch/combo resolution fails, fallback to `GetFirstAccount()`.
  - Reject login if no account exists.
  - Continue login flow if an account is available.

### 5) `GameServer/Game/Player/PlayerInstance.cs`
- Added and reused `InitializeAllDatabaseData()`:
  - Covers weapons, support cards, skins, profiles, accessories, furniture, AR, manifestation, characters, and supplies.
  - Used for both new player creation and empty-login backfill.
- Added `ShouldBackfillAllDatabaseData()`:
  - Triggers full backfill when characters and key inventory are empty.
- Default lineup init is guarded:
  - Only writes lineup when exactly three characters are selected.
- Bootstrap level is unified:
  - Constant `BootstrapLevel = 80`.

## 3. Core themes vs main

1. **Login resilience**: fallback to the first account when tokens do not match.
2. **Fresh DB usability**: auto-create a default account on first start.
3. **Player data self-healing**: backfill full data when a login has empty records.
4. **Unified bootstrap rules**: consolidated initial level configuration.

## 4. Changed files

- `Common/Database/Account/AccountData.cs`
- `GameServer/Game/Player/PlayerInstance.cs`
- `GameServer/Server/Packet/Recv/Login/HandlerReqLogin.cs`
- `MikuSB/Program/LoaderManager.cs`
- `SdkServer/Handlers/RouteController.cs`
