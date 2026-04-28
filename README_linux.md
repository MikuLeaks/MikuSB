# MikuSB on Linux


## Config

### setup steam launch options as following

`HTTP_PROXY="http://127.0.0.1:8888" HTTPS_PROXY="http://127.0.0.1:8888" ALL_PROXY="http://127.0.0.1:8888" %command%`

### start local server and keep it running

```
./MikuSB
```

### find root CA cert, and create ca bundle

root CA cert, should in the path: `proxy-certs/MikuSB.Proxy.Root.pem`


### setup root CA for proton/wine

not sure, even I remove Proton PFX (Wine prefix) folder, without redo this step, still no cert issue.

`Proton Hotfix` is the proton version which selected in steam `Force the use of a specific Steam Play compatibility tool`

```bash
APPID=<THE-APP-ID-OF-THE-GAME>
STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/$APPID/pfx
STEAM_WINE_PATH="$HOME/.steam/steam/steamapps/common/Proton Hotfix/files/bin/wine"
WINEPREFIX=$STEAM_COMPAT_DATA_PATH $STEAM_WINE_PATH certutil -addstore -f Root proxy-certs/MikuSB.Proxy.Root.pem
```

### start the game and enjoy


## development

1. Restore dependencies and build.

```bash
dotnet build
```

2. run it

```bash
dotnet run --project ./MikuSB
```

## release build

```bash
LANG=C time dotnet build -c Release
```

## TODO:

* [ ] tool/script for CA cert create and install to proton/wine
* [ ] automatic done in main program
