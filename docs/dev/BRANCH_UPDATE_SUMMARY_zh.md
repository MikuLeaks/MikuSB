# 本分支基于主线（origin/main）更新总结

> 分支：`copilot/analyze-login-rejection`
> 对比基线：`origin/main`
> 统计：13 个提交，5 个文件变更（+163 / -141）

## 一、提交列表（按时间从旧到新）

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

## 二、文件级更新概览

### 1) `Common/Database/Account/AccountData.cs`
- 新增 `GetFirstAccount()`：
  - 从账号表中按 `Uid` 升序选择首个账号；
  - 作为自动登录回退账号解析的统一入口。

### 2) `MikuSB/Program/LoaderManager.cs`
- 在数据库初始化阶段新增“新库首启数据初始化”逻辑：
  - 通过数据库文件存在性判断是否为首次初始化；
  - 首次初始化时调用 `InitializeStartupData()`；
  - 若库中无账号，则自动创建默认账号 `MIKU`（`Uid=1`）；
  - 初始密码改为随机生成（会话密钥形式）。

### 3) `SdkServer/Handlers/RouteController.cs`
- 收敛 SDK 登录相关分支逻辑到“首账号自动登录”路径：
  - `/seasun/login`
  - `/seasun/loginByToken`
- 移除原有较复杂的 token/email/uid 多源解析代码路径；
- 在找不到账号时统一返回登录失败响应。

### 4) `GameServer/Server/Packet/Recv/Login/HandlerReqLogin.cs`
- 登录包处理增加自动回退：
  - token/dispatch/combo 解析失败后，回退到 `GetFirstAccount()`；
  - 若无可用账号则拒绝登录；
  - 有可用账号则继续登录流程。

### 5) `GameServer/Game/Player/PlayerInstance.cs`
- 新增并复用完整初始化方法 `InitializeAllDatabaseData()`：
  - 覆盖武器、支援卡、皮肤、资料、挂件、家具、AR、显现、角色、补给等初始化；
  - 统一用于“新玩家创建”与“空数据登录回填”。
- 新增 `ShouldBackfillAllDatabaseData()`：
  - 当角色与关键库存均为空时触发全量回填。
- 默认阵容初始化增加防护：
  - 仅在随机选出**恰好 3 名角色**时执行阵容写入。
- 角色引导等级调整：
  - 统一常量 `BootstrapLevel = 80`。

## 三、本分支相对主线的核心变化主题

1. **登录容错增强**：token 无法匹配时可回退首账号，减少首次接入/异常数据导致的登录拒绝。
2. **新库可开箱运行**：首次启动自动生成默认账号，降低初始化门槛。
3. **玩家数据自愈能力提升**：对空档案进行登录时全量回填，避免关键数据缺失。
4. **初始化规则统一化**：引导等级配置集中化，行为更稳定可控。

## 四、变更文件清单

- `Common/Database/Account/AccountData.cs`
- `GameServer/Game/Player/PlayerInstance.cs`
- `GameServer/Server/Packet/Recv/Login/HandlerReqLogin.cs`
- `MikuSB/Program/LoaderManager.cs`
- `SdkServer/Handlers/RouteController.cs`
