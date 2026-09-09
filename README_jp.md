# MikuSB

<strong>MikuSB</strong>は、あるダンジョンアニメゲームのサーバーエミュレーターです。  
`SdkServer`、`GameServer`、任意のローカル HTTP/HTTPS プロキシを 1 つの `net9.0` アプリとして起動します。  

[Discord](https://discord.gg/6MvWY774hX)

English documentation is available in [README.md](README.md).

## 詐欺に関する警告

MikuSB は完全無料のオープンソースです。  
このサーバーを誰かから有料で販売された場合、それは詐欺です。  
すぐに返金を申請し、購入記録や証拠とあわせて Discord で私たちに通報してください。  

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

- [MikuSB](MikuSB): エントリーポイント
- [SdkServer](SdkServer): HTTP サーバーとディスパッチ
- [GameServer](GameServer): ゲームサーバー本体
- [Proxy](Proxy): ローカルプロキシ
- [Common](Common): 設定、DB、共通処理
- [Proto](Proto): protobuf 定義

## 要件

- [.NET SDK 10.0](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0)

## 起動方法

1. 依存を復元してビルドします。
```powershell
dotnet build
```
2. Config.json の`GamePath`にあなたのゲームの実行ファイルのパスを書き込みます  
3. サーバーを起動し`game`コマンドを入力します  
4. サーバーコンソールでアカウントを作成する  
5. 楽しむ

## 機能一覧

* [x] ログインシステム
* [x] インベントリ
* [x] 戦闘
* [x] キャラクター
* [x] GMメニュー
* [x] 武器
* [x] 後方支援
* [x] アポカリプス
* [x] アウクトゥス
* [x] 神格神経
* [x] キャラスキン
* [x] 武器スキン
* [x] ガチャ
* [x] メインストーリーチャプター
* [x] 社員寮
* [x] ミニゲーム
* [x] 地下清掃
* [x] ニューロンシュミレーション
* [x] 戦術評価
* [x] エピソードストーリー
* [x] ルーチン作戦
* [x] 逆説迷宮
* [x] ロビー画面の編集
* [✓] 惑星開拓
* [✓] 夢の絵本
* [ ] ショップ
* [ ] マルチプレイシステム
* [ ] 名誉紛争


## 貢献者  
- [Naruse](https://github.com/DevilProMT)
- [Kei-Luna](https://github.com/Kei-Luna)

## ライセンス
このリポジトリは [MIT License](LICENSE) の下で公開されています。

## 利用上の注意
本ソフトウェアはローカル環境での研究・検証用途を想定しています。  
公式サービスへの不正な接続、妨害、または商用利用を意図したものではありません。

## 法的免責事項
MikuSBは教育および研究目的で開発されました｡  
- 元のゲーム及び関連フランチャイズに関するすべての商標､著作権知的財産権はそれぞれの所有者に帰属します｡  
- このリポジトリには､著作権で保護されたゲームアセット､バイナリ､マスターデータは一切含まれていません｡  
- 自己責任でご利用下さい｡ 著者は､本ソフトウェアによって生じるいかなる損害または法的結果についても一切責任を負いません｡  

本ソフトウェアに関して懸念事項をお持ちの権利保有者は`devilpromt`または`kei_luna`にDiscordでご連絡下さい｡