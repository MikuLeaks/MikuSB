# MikuSB コマンド使用ガイド（ゼロから）

Languages: [English](COMMAND_GUIDE_en.md) | [中文](COMMAND_GUIDE_zh.md) | 日本語

> この文書では、特に `giveall` を中心にコマンドの使い方を説明します。

## 1. サーバーを起動する

先にサーバーを起動してください。セットアップと起動コマンドは[使用ガイド](../usage/USAGE_jp.md)を参照してください。

起動後、コンソールで `help` を入力するとコマンドヘルプを確認できます。

---

## 2. コマンド入力場所

コマンドは次の 2 か所で入力できます。

1. **サーバーコンソール**: `/` なしで直接入力します。
   - 例: `help`
2. **ゲーム内チャット**: コマンドの前に `/` を付けます。
   - 例: `/help`

---

## 3. 基本構文

```text
<main> <sub> <args> @<targetUID>
```

- `@<targetUID>` は任意で、対象プレイヤーを指定します。
- `@` を省略すると、コマンド送信者が対象になります。
- 対象解決の詳細は[コマンド対象説明](COMMAND_TARGET_jp.md)を参照してください。

補足:

- オプションは `p1 l90 g9 s9` のように書きます（先頭に `-` は付けません）。
- `detail` / `guid` は `-1` で全対象を表します。

---

## 4. まず `help` を確認する

```text
help
help giveall
help girl
help debug
```

---

## 5. よく使うコマンド

- `help [command]`（別名: `h`）
- `game <args...>`
- `account create <email> <uid>`
- `account list`
- `debug [on|off|simple|detail|file]`（別名: `dbg`）
- `girl add <detail/-1> p<particular> l<level> s<star>`（別名: `g`）
- `girl level <guid/-1> <level>`
- `girl neuronic <guid/-1> <level>`
- `girl break <guid/-1> <level>`
- `giveall <type> <detail/-1> [options]`（別名: `ga`）

---

## 6. `giveall` の使い方

`giveall` の別名は `ga` です。
利用できるサブコマンド:

- `weapon`
- `card`
- `weaponskin`
- `profile`
- `skinpart`
- `weaponpart`
- `call`
- `skin`
- `furniture`

### 6.1 パラメーター規則

現在の実装では、オプションは次のように書きます。

- `p1`
- `l90`
- `g9`

`-p1` / `-l90` / `-g9` のようには書かないでください。別の引数として解析されます。

### 6.2 例

```text
# 自分に全武器を付与、particular=1、レベル90
giveall weapon -1 p1 l90

# UID=1 のプレイヤーに全武器を付与
giveall weapon -1 p1 l90 @1

# 自分に全サポートカードを付与
giveall card -1 p1 l80

# 自分に全武器スキンを付与
giveall weaponskin -1 p1

# 自分に全キャラクタースキンを付与（genre=9 は例）
giveall skin -1 g9 p1 l1
```

補足:

- `detail=-1` は「全部」を意味します。
- `detail>=0` は特定の項目を意味します。

---

## 7. `girl` コマンド

```text
girl add -1 p1 l1 s9
girl level -1 80
girl neuronic -1 6
girl break -1 45
```

---

## 8. debug 切り替え

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

- `help` でコマンドが存在するか確認します。
- ゲーム内チャットでは `/` を付けます。
- サーバーコンソールでは `/` を付けません。

### Q2: “Player not found”

[コマンド対象説明](COMMAND_TARGET_jp.md)を参照してください。

### Q3: コマンドは実行されたが結果がおかしい

- `help <command>` で必要な引数を確認します。
- `giveall` のオプションは `p1 l90 g9` の形式で入力します。
