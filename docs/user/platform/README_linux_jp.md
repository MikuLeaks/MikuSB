# MikuSB on Linux

Languages: [English](README_linux_en.md) | [中文](README_linux_zh.md) | 日本語

## 設定

### Steam 起動オプション

`HTTP_PROXY="http://127.0.0.1:8888" HTTPS_PROXY="http://127.0.0.1:8888" ALL_PROXY="http://127.0.0.1:8888" %command%`

### ローカルサーバーを起動する

```bash
./MikuSB
```

ビルドと公開コマンドは[使用ガイド](../usage/USAGE_jp.md)を参照してください。

### ルート CA バンドル

ルート CA 証明書のパスは `proxy-certs/MikuSB.Proxy.Root.pem` です。

### Proton/Wine ルート CA

Proton PFX（Wine prefix）フォルダーを再作成した場合でも、この手順が再度必要にならないことがあります。

`Proton Hotfix` は Steam の `Force the use of a specific Steam Play compatibility tool` で選択した Proton バージョンです。

```bash
APPID=<THE-APP-ID-OF-THE-GAME>
STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/$APPID/pfx
STEAM_WINE_PATH="$HOME/.steam/steam/steamapps/common/Proton Hotfix/files/bin/wine"
WINEPREFIX=$STEAM_COMPAT_DATA_PATH $STEAM_WINE_PATH certutil -addstore -f Root proxy-certs/MikuSB.Proxy.Root.pem
```

### ゲームを起動する

## TODO

- [ ] CA 証明書を作成し Proton/Wine に導入するツールまたはスクリプト
- [ ] メインプログラムで自動処理する
