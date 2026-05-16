# コマンド対象と `giveall` パラメーター説明（日本語）

Languages: [English](COMMAND_TARGET_en.md) | [中文](COMMAND_TARGET_zh.md) | 日本語

この文書では、コンソールで `giveall` を実行したときに “player not found” が出る理由と、どの ID を指定するべきかを説明します。

## 1. 対象解決ルール

コマンドシステムは次のように対象を解決します。

- 対象を指定しない場合、既定の対象は**コマンド送信者**です。
- コンソールで実行した場合、送信者は `Console` で UID は `0` です。
- 対象指定の構文は `@<uid>` です（例: `@1001`）。
- 現在の実装では `@` の後ろの数値 UID のみを解析し、ユーザー名は対象として扱いません。

## 2. “player not found” が出る理由

`giveall` は実行前に対象がオンラインか確認します。

- 対象は**オンライン接続中のプレイヤー**である必要があります。
- 対象がコンソール（`uid=0`）またはオフラインの場合、“player not found” が表示されます。

## 3. `giveall` パラメーターの意味

`weapon` を例にします。

```text
/giveall weapon <detail/-1> p<particular> l<level> @<uid>
```

- `<detail/-1>`: アイテム detail。`-1` は全部を意味します。
- `p<particular>`: アイテム particular パラメーター（例: `p1`）。
- `l<level>`: レベルパラメーター。
- `@<uid>`: 対象プレイヤー UID。

注意:

- `miku` のような文字列は対象ユーザー名として扱われません。
- `p1` はアイテムパラメーターであり、プレイヤー ID ではありません。

## 4. UID かユーザー名か

結論:

- 対象パラメーターには **UID** を指定してください（`@<uid>`）。
- ユーザー名は指定しません。

データベース上の対応:

- アカウントテーブル: `Account`（`[SugarTable("Account")]`）
- プレイヤー ID フィールド: `Uid`（主キー）
- ユーザー名フィールド: `Username`

## 5. 正しい例

```text
/giveall weapon -1 p1 l90 @1001
```

意味: UID `1001` のオンラインプレイヤーに全武器を付与します（particular=1、level=90）。
