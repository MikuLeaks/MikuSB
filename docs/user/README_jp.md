# MikuSB

Languages: [English](../../README.md) | [中文](README_zh.md) | 日本語

<strong>MikuSB</strong>は、あるダンジョンアニメゲームのサーバーエミュレーターです。
`SdkServer`、`GameServer`、任意のローカル HTTP/HTTPS プロキシを 1 つの `net10.0` アプリとして起動します。

[Discord](https://discord.gg/aMwCu9JyUR)

## ドキュメント

- [Linux ガイド](platform/README_linux_jp.md)
- [使用ガイド](usage/USAGE_jp.md)
- [コマンドガイド](commands/COMMAND_GUIDE_jp.md)
- [コマンド対象説明](commands/COMMAND_TARGET_jp.md)

## 概要

- `SdkServer`
  - HTTP API とディスパッチを返します
  - サーバー一覧、バージョン照会、各種フォールバックレスポンスを返します
- `GameServer`
  - TCP ベースのゲーム接続を受けます
  - `ReqCallGS` と一部の通常パケットを処理します
- `Proxy`
  - 有効時のみ `127.0.0.1:8888` で待ち受けます
  - 一部の Snowbreak 関連ドメインをローカル `SdkServer` へリダイレクトします
- `Common` / `Proto` / `TcpSharp`
  - 共通データ、protobuf 定義、通信基盤です

## プロジェクト構成

- [MikuSB](../../MikuSB): エントリーポイント
- [SdkServer](../../SdkServer): HTTP サーバーとディスパッチ
- [GameServer](../../GameServer): ゲームサーバー本体
- [Proxy](../../Proxy): ローカルプロキシ
- [Common](../../Common): 設定、DB、共通処理
- [Proto](../../Proto): protobuf 定義

## 要件

- [.NET SDK 10.0](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0)

## 起動方法

1. 依存関係を復元してビルドします。

```powershell
dotnet build
```

2. `Config/Config.json` の `GamePath` にゲーム実行ファイルのパスを設定します。
3. サーバーを起動します。

```powershell
dotnet run --project .\MikuSB
```

4. サーバーコンソールでアカウントを作成します。
5. サーバーコンソールで `game` コマンドを実行します。
6. ゲームを起動してログインします。

公開コマンドと生成データの詳細は[使用ガイド](usage/USAGE_jp.md)を参照してください。

## 機能一覧

- [x] ログインと基本的なアカウント入場
- [x] プレイヤーデータの読み込み
- [x] 所持品の読み込み
- [x] キャラクターの読み込み
- [x] スキンの読み込み
- [x] 武器の読み込み
- [x] ロビー表示キャラクターの変更
- [x] キャラクタースキンの変更
- [x] キャラクタースキン形態の変更
- [x] 武器の付け替え
- [x] 武器の強化
- [x] プレイヤー名の変更
- [x] 現在対応済みロビー状態の基本保存
- [x] メイン章のステージ入場と関連フロー
- [x] デイリーのステージ入場と関連フロー
- [x] 基本的なプレイヤー設定同期
- [x] 基本的なプロフィール同期
- [x] イベント関連リクエスト
- [x] 実績関連リクエスト
- [x] 編成関連リクエスト
- [x] プレビュー関連リクエスト
- [x] 一部のショップ関連リクエスト
- [ ] 完全な戦闘フロー
- [ ] ミッション / クエスト進行
- [ ] ガチャ / 募集システム
- [ ] 完全なショップ挙動
- [ ] マルチプレイシステム
- [ ] 基地 / 宿舎システム
- [ ] クライアント API 全体の対応

## 貢献者

- [Naruse](https://github.com/DevilProMT)
- [Kei-Luna](https://github.com/Kei-Luna)

## 利用上の注意

本ソフトウェアはローカル環境での研究・検証用途を想定しています。
公式サービスへの不正な接続、妨害、または商用利用を意図したものではありません。

## 法的免責事項

MikuSBは教育および研究目的で開発されました｡

- 元のゲーム及び関連フランチャイズに関するすべての商標､著作権知的財産権はそれぞれの所有者に帰属します｡
- このリポジトリには､著作権で保護されたゲームアセット､バイナリ､マスターデータは一切含まれていません｡
- 自己責任でご利用下さい｡ 著者は､本ソフトウェアによって生じるいかなる損害または法的結果についても一切責任を負いません｡

本ソフトウェアに関して懸念事項をお持ちの権利保有者は`devilpromt`または`kei_luna`にDiscordでご連絡下さい｡
