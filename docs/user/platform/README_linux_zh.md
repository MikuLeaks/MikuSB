# MikuSB 在 Linux 上使用

Languages: [English](README_linux_en.md) | 中文 | [日本語](README_linux_jp.md)

## 配置

### Steam 启动项

`HTTP_PROXY="http://127.0.0.1:8888" HTTPS_PROXY="http://127.0.0.1:8888" ALL_PROXY="http://127.0.0.1:8888" %command%`

### 启动本地服务器

```bash
./MikuSB
```

构建与发布命令见[使用指导](../usage/USAGE_zh.md)。

### 根证书包

根证书路径：`proxy-certs/MikuSB.Proxy.Root.pem`

### Proton/Wine 根证书

注意：即使删除 Proton PFX（Wine 前缀）目录，不重新执行这一步也可能不报证书问题。

`Proton Hotfix` 是 Steam 中选择的 `Force the use of a specific Steam Play compatibility tool` 对应版本。

```bash
APPID=<THE-APP-ID-OF-THE-GAME>
STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/$APPID/pfx
STEAM_WINE_PATH="$HOME/.steam/steam/steamapps/common/Proton Hotfix/files/bin/wine"
WINEPREFIX=$STEAM_COMPAT_DATA_PATH $STEAM_WINE_PATH certutil -addstore -f Root proxy-certs/MikuSB.Proxy.Root.pem
```

### 启动游戏

## TODO

- [ ] 提供自动生成并安装 CA 证书的工具/脚本
- [ ] 在主程序中自动完成上述步骤
