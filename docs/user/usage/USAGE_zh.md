# MikuSB 使用指导（从零开始）

Languages: [English](USAGE_en.md) | 中文 | [日本語](USAGE_jp.md)

> 本文档聚焦：完整命令流程、数据库字段含义与来源、数据如何生成。

## 1. 从零开始运行（开发模式）

### 1.1 环境准备

- 安装 [.NET SDK 10.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
- 安装 Git

### 1.2 获取源码

```bash
git clone https://github.com/AliceJump/MikuSB.git
cd MikuSB
```

### 1.3 构建

```bash
dotnet build
```

### 1.4 启动服务

```bash
dotnet run --project ./MikuSB
```

启动后会同时拉起：

- `SdkServer`（HTTP）
- `GameServer`（TCP）
- 本地代理（默认启用，监听 `127.0.0.1:8888`）

### 1.5 首次启动会自动生成的内容

- `Config/Config.json`（若不存在则生成默认配置）
- `Config/Database/Miku.db`（SQLite 数据库文件）
- 数据库表结构（Code First 自动建表）
- `proxy-certs/*`（代理根证书与派生证书）
- `Config/Handbook/*`（命令手册文本，按 TextMap 生成）

## 2. 发布命令

### 2.1 Linux 发布单文件

```bash
dotnet publish ./MikuSB/MikuSB.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true --property:PublishDir=../publish
```

### 2.2 Windows 发布（多文件，和 CI 一致）

```powershell
dotnet publish .\MikuSB\MikuSB.csproj -c Release -p:PublishProfile=MikuSB-Win64-MultiFile -o .\artifacts\publish\MikuSB
```

## 3. 资源与数据库“如何生成”

### 3.1 资源文件来源

- 服务启动时会检查 `Resources/` 下的关键文件（如 `item/templates/card.json`、`item/templates/weapon.json`）。
- 若缺失，会自动下载资源压缩包并解压到 `Resources/`。
- 资源数据随后被加载为内存中的 `GameData.*` 字典/列表，用于后续角色、武器、道具初始化。

### 3.2 数据库与表“如何生成”

- 数据库类型：SQLite（SqlSugar）
- 数据库路径：`Config/Database/Miku.db`（可通过 `Config/Config.json` 修改目录与文件名）
- 建表方式：扫描所有继承 `BaseDatabaseDataHelper` 的类型并执行 Code First 自动建表

### 3.3 数据“如何写入”

- 玩家心跳会将 UID 加入待保存列表，默认每 5 分钟批量落盘
- 进程退出时会触发最终一次保存（final flush）

## 4. 数据库表与字段说明（含来源）

> 主键统一为 `Uid`（玩家 UID）。

### 4.1 `Account`

字段：

- `Uid`：账号 UID。首次登录时若不存在账号会创建，默认从 1 开始递增。
- `Username`：用户名。首次自动创建账号时默认 `"MIKU"`。
- `Password`：密码哈希（SHA256）；空密码会存空字符串。
- `BanType`：封禁类型枚举。
- `Phone`：手机号字段（默认 `"123456"`）。
- `Permissions`（JSON）：权限列表，来源于 `Config.json -> ServerOption.DefaultPermissions`。
- `ComboToken` / `DispatchToken`：会话 token，调用对应生成方法时写入。

来源：

- 自动创建：首次处理登录包时，若 UID=1 不存在则创建。
- 也可通过逻辑/命令触发账号管理。

### 4.2 `Player`

字段：

- `Uid`：玩家 UID（与账号一致）。
- `Name`：显示名，默认取账号名并标准化（空白时回退 `Miku`）。
- `Signature`：签名（默认 `MikuPS`）。
- `Level` / `Exp` / `Vigor` / `Gender`：玩家基础属性。
- `RegisterTime`：注册时间（Unix 秒，创建对象时写入）。
- `LastActiveTime`：最近活跃时间（初始化玩家管理器时刷新）。
- `Attrs`（JSON）：数值属性（大量引导/货币/关卡解锁等引导值）。
- `StrAttrs`（JSON）：字符串属性。
- `ShowItems`（JSON）：个人展示道具列表。

来源：

- 当账号已存在但无玩家数据时，创建 `PlayerGameData`。
- `Attrs` 会在玩家序列化流程中由引导与关卡数据补齐/抬高。

### 4.3 `inventory_data`

字段：

- `Uid`：玩家 UID。
- `NextUniqueUid`：背包唯一物品 ID 分配器（默认从 `100000` 开始）。
- `Items`（JSON）：普通道具字典（包含补给、AR、彰痕等）。
- `Weapons`（JSON）：武器字典。
- `Skins`（JSON）：皮肤字典。
- `SupportCards`（JSON）：支援卡字典。
- `SkinTypesBySkinId`（JSON）：皮肤形态映射（`nSkinId -> nType`）。

来源：

- 首次创建玩家后，系统会根据资源表批量发放初始皮肤、角色、补给等。
- 各种业务请求（强化、替换、皮肤切换等）持续修改该表。

### 4.4 `character_data`

字段：

- `Uid`：玩家 UID。
- `Characters`（JSON）：角色列表。
  - 关键子字段：`Guid`、`TemplateId`、`Level`、`Break`、`Evolue`、`ProLevel`、`Trust`、`WeaponUniqueId`、`SkinId`、`WeaponSkinId`、`SupportSlots`、`UnlockedSkin`、`Spines`、`Affixs` 等。
- `NextCharacterGuid`：角色 GUID 递增计数器。

来源：

- 首次玩家初始化会按资源中的角色模板批量创建角色。
- 创建角色时会自动补默认武器与皮肤关联。

### 4.5 `lineup_data`

字段：

- `Uid`：玩家 UID。
- `LineupInfo`（JSON）：编队字典，键为编队位。
  - 子字段：`Index`、`Name`、`Member1`、`Member2`、`Member3`。

来源：

- 新玩家初始化后，会随机选 3 名角色写入默认编队。
- 后续编队更新请求持续写入。

## 5. 快速排查与校验

### 5.1 查看数据库文件与表

```bash
ls -lah ./Config/Database
sqlite3 ./Config/Database/Miku.db ".tables"
sqlite3 ./Config/Database/Miku.db ".schema Account"
sqlite3 ./Config/Database/Miku.db ".schema Player"
```

### 5.2 查看配置中的数据库路径

```bash
cat ./Config/Config.json
```

## 6. 备注

- 本项目大量字段为 JSON 列（对象序列化存储），阅读时建议结合对应 C# 数据结构一起看。
- 若要重置本地进度，先停止服务，再备份或删除 `Config/Database/Miku.db` 后重新启动。
