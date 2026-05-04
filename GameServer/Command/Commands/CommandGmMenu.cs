using MikuSB.Configuration;
using MikuSB.Enums.Player;
using MikuSB.Util;
using MikuSB.Internationalization;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Command.Commands;

[CommandInfo("gm", "Game.Command.GmMenu.Desc", "Game.Command.GmMenu.Usage", ["gm"], [PermEnum.Admin, PermEnum.Support])]
public class CommandGmMenu : ICommands
{
    private static readonly Logger Logger = new("CommandManager");

    [CommandDefault]
    public async ValueTask ToggleGmMenu(CommandArg arg)
    {
        var option = arg.Args.FirstOrDefault()?.ToLowerInvariant() ?? "on";
        var serverOption = ConfigManager.Config.ServerOption;
        var player = arg.Target?.Player;
        var message = option switch
        {
            "on" => await EnableGmMenu(serverOption, player),
            "off" => await DisableGmMenu(serverOption, player),
            _ => I18NManager.Translate("Game.Command.GmMenu.Usage")
        };

        Logger.Info(message);
        await arg.SendMsg(message);
    }

    private static async ValueTask<string> EnableGmMenu(ServerOption serverOption, Game.Player.PlayerInstance? player)
    {
        serverOption.EnableGmMenu = true;
        if (player != null) await player.SendScript("gm.notifylogin", new JsonObject
        {
            ["IsDebug"] = ConfigManager.Config.ServerOption.EnableGmMenu
        }.ToJsonString());
        return I18NManager.Translate("Game.Command.GmMenu.Enabled");
    }

    private static async ValueTask<string> DisableGmMenu(ServerOption serverOption, Game.Player.PlayerInstance? player)
    {
        serverOption.EnableGmMenu = false;
        if (player != null) await player.SendScript("gm.notifylogin", new JsonObject
        {
            ["IsDebug"] = ConfigManager.Config.ServerOption.EnableGmMenu
        }.ToJsonString());
        return I18NManager.Translate("Game.Command.GmMenu.Disabled");
    }
}
