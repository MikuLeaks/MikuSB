# MikuSB 使用ガイド（ゼロから）

Languages: [English](USAGE_en.md) | [中文](USAGE_zh.md) | 日本語

> この文書では、起動手順、公開コマンド、データベース項目、データ生成の流れを扱います。

## 1. ゼロから起動する（開発環境）

### 1.1 要件

- [.NET SDK 10.0](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0) をインストールする
- Git をインストールする

### 1.2 ソースコードを取得する

```bash
git clone https://github.com/AliceJump/MikuSB.git
cd MikuSB
```

### 1.3 ビルド

```bash
dotnet build
```

### 1.4 サーバーを起動する

```bash
dotnet run --project ./MikuSB
```

起動後、次のサービスが同時に動作します。

- `SdkServer`（HTTP）
- `GameServer`（TCP）
- ローカルプロキシ（既定で有効、`127.0.0.1:8888` で待ち受け）

### 1.5 初回起動時に作成されるもの

- `Config/Config.json`（存在しない場合に生成）
- `Config/Database/Miku.db`（SQLite データベース）
- データベーステーブル（Code First で自動作成）
- `proxy-certs/*`（プロキシ用ルート CA と派生証明書）
- `Config/Handbook/*`（TextMap から生成されるコマンドハンドブック）

## 2. 公開コマンド

### 2.1 Linux 単一ファイル公開

```bash
dotnet publish ./MikuSB/MikuSB.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true --property:PublishDir=../publish
```

### 2.2 Windows 公開（複数ファイル、CI と同等）

```powershell
dotnet publish .\MikuSB\MikuSB.csproj -c Release -p:PublishProfile=MikuSB-Win64-MultiFile -o .\artifacts\publish\MikuSB
```

## 3. リソースとデータベースの生成

### 3.1 リソースファイル

- 起動時に `Resources/` 配下の主要ファイルを確認します。
- 不足している場合はリソースアーカイブをダウンロードして展開します。
- 読み込まれたデータは、キャラクター、武器、アイテムの初期化に使われます。

### 3.2 データベースとテーブル作成

- データベース種類: SQLite（SqlSugar）
- データベースパス: `Config/Database/Miku.db`（`Config/Config.json` で変更可能）
- テーブル作成: `BaseDatabaseDataHelper` を継承する型を走査し、Code First で作成します。

### 3.3 データの保存

- プレイヤーのハートビートで UID が保存キューに入ります。
- 既定では 5 分ごとに保存されます。
- プロセス終了時に最後の保存が実行されます。

## 4. 主なデータベーステーブル

### 4.1 `Account`

- `Uid`: アカウント UID
- `Username`: ユーザー名
- `Password`: パスワードハッシュ
- `Permissions`: 権限リスト
- `ComboToken` / `DispatchToken`: セッショントークン

### 4.2 `Player`

- `Uid`: プレイヤー UID
- `Name`: 表示名
- `Level` / `Exp` / `Vigor` / `Gender`: 基本属性
- `Attrs` / `StrAttrs`: 属性データ
- `ShowItems`: プロフィール表示項目

### 4.3 `inventory_data`

- `Items`: アイテム辞書
- `Weapons`: 武器辞書
- `Skins`: スキン辞書
- `SupportCards`: サポートカード辞書

### 4.4 `character_data`

- `Characters`: キャラクター一覧
- `NextCharacterGuid`: キャラクター GUID カウンター

### 4.5 `lineup_data`

- `LineupInfo`: 編成情報

## 5. 確認コマンド

```bash
ls -lah ./Config/Database
sqlite3 ./Config/Database/Miku.db ".tables"
sqlite3 ./Config/Database/Miku.db ".schema Account"
sqlite3 ./Config/Database/Miku.db ".schema Player"
```

## 6. 注意

- 多くの項目は JSON カラムとして保存されます。
- ローカル進行状況をリセットする場合は、サーバーを停止してから `Config/Database/Miku.db` をバックアップまたは削除してください。
