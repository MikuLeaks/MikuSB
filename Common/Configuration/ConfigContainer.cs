using MikuSB.Proto;

namespace MikuSB.Configuration;

public class ConfigContainer
{
    public HttpServerConfig HttpServer { get; set; } = new();
    public GameServerConfig GameServer { get; set; } = new();
    public PathConfig Path { get; set; } = new();
    public ServerOption ServerOption { get; set; } = new();
    public ProxyOptions Proxy { get; set; } = new();
}

public class HttpServerConfig
{
    public string BindAddress { get; set; } = "127.0.0.1";
    public string PublicAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 21500;
    public bool EnableLog { get; set; } = false;

    public string GetDisplayAddress()
    {
        return "http" + "://" + PublicAddress + ":" + Port;
    }

    public string GetBindDisplayAddress()
    {
        return "http" + "://" + BindAddress + ":" + Port;
    }
}

public class GameServerConfig
{
    public string BindAddress { get; set; } = "127.0.0.1";
    public string PublicAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 21000;
    public int KcpAliveMs { get; set; } = 45000;
    public string DatabaseName { get; set; } = "Miku.db";
    public string GameServerId { get; set; } = "MikuSB";
    public string GameServerName { get; set; } = "MikuSB";
    public string GetDisplayAddress()
    {
        return PublicAddress + ":" + Port;
    }
}

public class PathConfig
{
    public string ResourcePath { get; set; } = "Resources";
    public string ConfigPath { get; set; } = "Config";
    public string DatabasePath { get; set; } = "Config/Database";
    public string HandbookPath { get; set; } = "Config/Handbook";
    public string LogPath { get; set; } = "Config/Logs";
    public string DataPath { get; set; } = "Config/Data";
}

public class ServerOption
{
    public string Language { get; set; } = "EN";
    public string FallbackLanguage { get; set; } = "EN";
    public string[] DefaultPermissions { get; set; } = ["Admin"];
    public ServerProfile ServerProfile { get; set; } = new();
    public bool EnableGmMenu { get; set; } = false;
    public bool AutoCreateUser { get; set; } = true;
    public bool SavePersonalDebugFile { get; set; } = false;
    public bool AutoSendResponseWhenNoHandler { get; set; } = true;
#if DEBUG
    public bool EnableDebug { get; set; } = true;
#else
    public bool EnableDebug { get; set; } = false;
#endif
    public bool DebugMessage { get; set; } = true;
    public bool DebugDetailMessage { get; set; } = true;
    public bool DebugNoHandlerPacket { get; set; } = true;
}

public class ServerProfile
{
    public string Name { get; set; } = "Miku-chan";
    public string Signature { get; set; } = "SnowBreak Private Server";
    public int Uid { get; set; } = 80;
    public int Level { get; set; } = 100;
    public Sex Gender { get; set; } = Sex.Female;
}

public class ProxyOptions
{
    public bool Enabled { get; set; } = true;
    public string BindAddress { get; set; } =  "127.0.0.1";
    public int Port { get; set; } = 8888;
    public string ServerHttpAddress { get; set; } =  "127.0.0.1";
    public int ServerHttpPort { get; set; } = 21500;
    public bool InstallRootCertificate { get; set; } = true;
    public bool ManageSystemProxy { get; set; } = true;
    public bool RestoreSystemProxyOnStop { get; set; } = true;
    public string ProxyOverride { get; set; } = "localhost;127.*;10.*;192.168.*;<local>";
}
