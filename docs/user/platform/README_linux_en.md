# MikuSB on Linux

Languages: English | [中文](README_linux_zh.md) | [日本語](README_linux_jp.md)

## Config

### Steam Launch Options

`HTTP_PROXY="http://127.0.0.1:8888" HTTPS_PROXY="http://127.0.0.1:8888" ALL_PROXY="http://127.0.0.1:8888" %command%`

### Start Local Server

```bash
./MikuSB
```

For build and publish commands, see the [usage guide](../usage/USAGE_en.md).

### Root CA Bundle

The root CA cert should be in `proxy-certs/MikuSB.Proxy.Root.pem`.

### Proton/Wine Root CA

This step may not be required again if the Proton PFX (Wine prefix) folder is recreated.

`Proton Hotfix` is the Proton version selected in Steam `Force the use of a specific Steam Play compatibility tool`.

```bash
APPID=<THE-APP-ID-OF-THE-GAME>
STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/$APPID/pfx
STEAM_WINE_PATH="$HOME/.steam/steam/steamapps/common/Proton Hotfix/files/bin/wine"
WINEPREFIX=$STEAM_COMPAT_DATA_PATH $STEAM_WINE_PATH certutil -addstore -f Root proxy-certs/MikuSB.Proxy.Root.pem
```

### Start The Game

## TODO

- [ ] Tool/script for CA cert creation and Proton/Wine installation
- [ ] Automatically handle the setup in the main program
