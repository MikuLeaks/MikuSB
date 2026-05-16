# MikuSB

Languages: [English](../../README.md) | 中文 | [日本語](README_jp.md)

<strong>MikuSB</strong> 是某款地牢题材动漫游戏的服务器模拟器。
它会从一个 `net10.0` 应用中启动 `SdkServer`、`GameServer`，以及可选的本地 HTTP/HTTPS 代理。

[Discord](https://discord.gg/aMwCu9JyUR)

## 文档

- [使用指导](usage/USAGE_zh.md)
- [命令使用指南](commands/COMMAND_GUIDE_zh.md)
- [命令目标说明](commands/COMMAND_TARGET_zh.md)
- [Linux 使用说明](platform/README_linux_zh.md)

## 概览

- `SdkServer`
  - 提供 HTTP API 并分发响应
  - 返回服务器列表、版本查询和各类兜底响应
- `GameServer`
  - 接受基于 TCP 的游戏连接
  - 处理 `ReqCallGS` 与部分普通协议包
- `Proxy`
  - 启用时监听 `127.0.0.1:8888`
  - 将部分 Snowbreak 相关域名重定向到本地 `SdkServer`
- `Common` / `Proto` / `TcpSharp`
  - 共享数据、protobuf 定义与网络通信基础设施

## 项目结构

- [MikuSB](../../MikuSB): 入口程序
- [SdkServer](../../SdkServer): HTTP 服务与分发
- [GameServer](../../GameServer): 主游戏服务器
- [Proxy](../../Proxy): 本地代理
- [Common](../../Common): 配置、数据库与公共工具
- [Proto](../../Proto): protobuf 定义

## 环境要求

- [.NET SDK 10.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

## 运行

1. 还原依赖并构建。

```powershell
dotnet build
```

2. 在 `Config/Config.json` 中将 `GamePath` 设置为游戏可执行文件路径。
3. 启动服务。

```powershell
dotnet run --project .\MikuSB
```

4. 在服务端控制台创建账号。
5. 在服务端控制台执行 `game` 命令。
6. 启动游戏并登录。

发布命令与生成数据说明见[使用指导](usage/USAGE_zh.md)。

## 功能列表

- [x] 登录与基础账号进入
- [x] 玩家数据加载
- [x] 背包数据加载
- [x] 角色数据加载
- [x] 皮肤数据加载
- [x] 武器数据加载
- [x] 大厅展示角色切换
- [x] 角色皮肤切换
- [x] 角色皮肤形态切换
- [x] 武器替换
- [x] 武器强化
- [x] 玩家改名
- [x] 当前已支持大厅状态的基础保存
- [x] 主线章节关卡进入及相关流程
- [x] 日常关卡进入及相关流程
- [x] 基础玩家设置同步
- [x] 基础个人资料同步
- [x] 活动相关请求
- [x] 成就相关请求
- [x] 编队相关请求
- [x] 预览相关请求
- [x] 部分商店相关请求
- [ ] 完整战斗流程
- [ ] 任务 / 委托进度
- [ ] 抽卡 / 招募系统
- [ ] 完整商店行为
- [ ] 多人系统
- [ ] 基地 / 宿舍系统
- [ ] 客户端 API 全覆盖

## 贡献者

- [Naruse](https://github.com/DevilProMT)
- [Kei-Luna](https://github.com/Kei-Luna)

## 使用说明

本软件仅用于本地环境下的研究与测试。
不用于对官方服务进行未授权访问、干扰或商业用途。

## 法律免责声明

MikuSB 仅为教育与研究目的开发。

- 与原游戏及其相关系列有关的所有商标、版权及其他知识产权均归其各自所有者所有。
- 本仓库不包含任何受版权保护的游戏资源、二进制文件或主数据。
- 使用本软件需自行承担风险。作者不对因使用本软件导致的任何损失或法律后果负责。

若您是权利持有方并对本软件有任何顾虑，请在 Discord 联系 `devilpromt` 或 `kei_luna`。
