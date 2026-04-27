using Microsoft.AspNetCore.Components;
using MikuSB.Data;
using MikuSB.Database;
using MikuSB.GameServer.Command;
using MikuSB.GameServer.Server;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.Packet;
using MikuSB.Internationalization;
using MikuSB.MikuSB.Tool;
using MikuSB.Proto;
using MikuSB.SdkServer;
using MikuSB.TcpSharp;
using MikuSB.Util;
using MikuSB.Util.Security;
using System.Reflection;

namespace MikuSB.MikuSB.Program;

public class LoaderManager : MikuSB
{
    public static void InitConfig()
    {
        // Initialize log
        var counter = 0;
        FileInfo file;
        while (true)
        {
            file = new FileInfo(ConfigManager.Config.Path.LogPath + $"/{DateTime.Now:yyyy-MM-dd}-{++counter}.log");
            if (file is not { Exists: false, Directory: not null }) continue;
            file.Directory.Create();
            break;
        }
        Logger.SetLogFile(file);

        // Init all directories
        try
        {
            ConfigManager.InitDirectories();
        }
        catch (Exception e)
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Config")), e);
            Console.ReadLine();
            return;
        }

        // Starting the server
        Logger.Info(I18NManager.Translate("Server.ServerInfo.StartingServer"));
        Logger.Info($"Build version: {BuildVersion.Current}");

        // Load the config
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Config")));
        try
        {
            ConfigManager.LoadConfig();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Config")), e);
            Console.ReadLine();
            return;
        }

        // Load the language
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Language")));
        try
        {
            I18NManager.LoadLanguage();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Language")), e);
            Console.ReadLine();
            return;
        }
    }

    public static void InitDatabase()
    {
        // Initialize the database
        try
        {
            _ = Task.Run(DatabaseHelper.Initialize); // do not wait

            while (!DatabaseHelper.LoadAccount) Thread.Sleep(100);

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem",
                I18NManager.Translate("Word.DatabaseAccount")));
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Database")), e);
            Console.ReadLine();
            return;
        }
    }

    public static async Task InitSdkServer()
    {
        SdkServer.SdkServer.Start([]);
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerRunning", I18NManager.Translate("Word.Dispatch"),
            ConfigManager.Config.HttpServer.GetDisplayAddress()));

        //KcpListener.BaseConnection = typeof(Connection);
        //KcpListener.StartListener();
        SocketListener.BaseConnection = typeof(Connection);
        SocketListener.StartListener();

        await Task.CompletedTask;
    }

    public static void InitPacket()
    {
        // get opcode from CmdIds
        var opcodes = typeof(CmdIds).GetFields().Where(x => x.FieldType == typeof(int)).ToList();
        foreach (var opcode in opcodes)
        {
            var name = opcode.Name;
            var value = (int)opcode.GetValue(null)!;
            SocketConnection.LogMap.TryAdd(value, name);
        }

        HandlerManager.Init();
        CallGSRouter.Init();
    }

    public static async Task InitResource()
    {
        // Init custom files
        Logger.Info(I18NManager.Translate("Server.ServerInfo.GeneratingItem", I18NManager.Translate("Word.CustomData")));
        try
        {
            await AssemblyGenerater.LoadCustomData(Assembly.GetExecutingAssembly());
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.CustomData")), e);
            Console.ReadLine();
            return;
        }

        // Load the game data
        try
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.GameData")));
            ResourceManager.LoadGameData();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.GameData")), e);
            Console.ReadLine();
            return;
        }
    }

    public static void InitCommand()
    {
        // Register the command handlers
        try
        {
            CommandManager.RegisterCommands();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToInitializeItem",
                    I18NManager.Translate("Word.Command")), e);
            Console.ReadLine();
            return;
        }
        IConsole.OnConsoleExcuteCommand += CommandExecutor.ConsoleExcuteCommand;
        CommandExecutor.OnRunCommand += (sender, e) => { CommandManager.HandleCommand(e, sender); };

        IConsole.ListenConsole();
    }
}