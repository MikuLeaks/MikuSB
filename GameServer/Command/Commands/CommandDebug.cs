using MikuSB.Configuration;
using MikuSB.Enums.Player;
using MikuSB.Util;
using MikuSB.Internationalization;

namespace MikuSB.GameServer.Command.Commands;

[CommandInfo("debug", "Game.Command.Debug.Desc", "Game.Command.Debug.Usage", ["dbg"], [PermEnum.Admin, PermEnum.Support])]
public class CommandDebug : ICommands
{
    private static readonly Logger Logger = new("CommandManager");

    [CommandDefault]
    public async ValueTask ToggleDebug(CommandArg arg)
    {
        var option = arg.Args.FirstOrDefault()?.ToLowerInvariant() ?? "on";
        var serverOption = ConfigManager.Config.ServerOption;
        var message = option switch
        {
            "on" => EnableDebug(serverOption),
            "off" => DisableDebug(serverOption),
            "simple" => EnableSimpleDebug(serverOption),
            "detail" => EnableDetailDebug(serverOption),
            "file" => ToggleDebugFile(serverOption),
            _ => I18NManager.Translate("Game.Command.Debug.Usage")
        };

        Logger.Info(message);
        await arg.SendMsg(message);
    }

    private static string EnableDebug(ServerOption serverOption)
    {
        serverOption.EnableDebug = true;
        serverOption.DebugMessage = true;
        serverOption.DebugDetailMessage = true;
        return I18NManager.Translate("Game.Command.Debug.Enabled");
    }

    private static string DisableDebug(ServerOption serverOption)
    {
        serverOption.EnableDebug = false;
        return I18NManager.Translate("Game.Command.Debug.Disabled");
    }

    private static string EnableSimpleDebug(ServerOption serverOption)
    {
        serverOption.EnableDebug = true;
        serverOption.DebugMessage = true;
        serverOption.DebugDetailMessage = false;
        return I18NManager.Translate("Game.Command.Debug.SimpleEnabled");
    }

    private static string EnableDetailDebug(ServerOption serverOption)
    {
        serverOption.EnableDebug = true;
        serverOption.DebugMessage = true;
        serverOption.DebugDetailMessage = true;
        return I18NManager.Translate("Game.Command.Debug.DetailEnabled");
    }

    private static string ToggleDebugFile(ServerOption serverOption)
    {
        serverOption.SavePersonalDebugFile = !serverOption.SavePersonalDebugFile;
        return serverOption.SavePersonalDebugFile
            ? I18NManager.Translate("Game.Command.Debug.FileEnabled")
            : I18NManager.Translate("Game.Command.Debug.FileDisabled");
    }
}
