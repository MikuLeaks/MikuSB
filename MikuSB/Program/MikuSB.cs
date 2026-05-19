using MikuSB.Data;
using MikuSB.Database;
using MikuSB.MikuSB.Tool;
using MikuSB.GameServer.Command;
using MikuSB.GameServer.Server;
using MikuSB.Internationalization;
using MikuSB.MikuSB.Update;
using MikuSB.TcpSharp;
using MikuSB.Util;
using System.Globalization;

namespace MikuSB.MikuSB.Program;

public class MikuSB
{
    public static readonly Logger Logger = new("MikuSB");
    public static readonly DatabaseHelper DatabaseHelper = new();
    public static readonly Listener Listener = new();
    public static readonly CommandManager CommandManager = new();

    // for exit signal
    private static readonly CancellationTokenSource _cts = new();
    private static int _exitCode = 0;

    public static async Task Main()
    {
        var time = DateTime.Now;
        IConsole.InitConsole();
        LoaderManager.InitConfig();
        ShowAntiScamWarning();
        if (await UpdateService.TryStartSelfUpdateAsync())
            return;

        RegisterExitEvent();
        await LoaderManager.InitSdkServer();
        LoaderManager.InitPacket();

        LoaderManager.InitDatabase();
        if (!DatabaseHelper.LoadAllData)
        {
            var t = Task.Run(() =>
            {
                while (!DatabaseHelper.LoadAllData) // wait for all data to be loaded
                    Thread.Sleep(100);
            });

            await t.WaitAsync(new CancellationToken());

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem", I18NManager.Translate("Word.Database")));
        }

        Logger.Warn(I18NManager.Translate("Server.ServerInfo.WaitForAllDone"));

        await LoaderManager.InitResource();
        ResourceManager.IsLoaded = true;

        HandbookGenerator.GenerateAll();
        var consoleTask = Task.Run(() => LoaderManager.InitCommand(_cts.Token), _cts.Token);

        var elapsed = DateTime.Now - time;
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerStarted",
            Math.Round(elapsed.TotalSeconds, 2).ToString(CultureInfo.InvariantCulture)));

        await consoleTask;

        await ProcessExit(Volatile.Read(ref _exitCode));
    }

    private static void ShowAntiScamWarning()
    {
        Logger.Warn("============================================================");
        Logger.Warn("MikuSB is completely free and open source.");
        Logger.Warn("If you paid anyone for this server, you were scammed.");
        Logger.Warn("Request a refund immediately and report the seller to us.");
        Logger.Warn("Discord: https://discord.gg/aMwCu9JyUR");
        Logger.Warn("============================================================");
    }

    #region Exit

    private static void RegisterExitEvent()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            RequestShutdown(0);
        };
        AppDomain.CurrentDomain.UnhandledException += (obj, arg) =>
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.UnhandledException", obj.GetType().Name),
                (Exception)arg.ExceptionObject);
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            RequestShutdown(1);
        };

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.CancelKeyPressed"));
            eventArgs.Cancel = true;
            RequestShutdown(0);
        };
    }

    private static void RequestShutdown(int exitCode)
    {
        Interlocked.Exchange(ref _exitCode, exitCode);
        _cts.Cancel();
    }

    private static async Task ProcessExit(int exitCode)
    {
        SocketListener.Connections.Values.ToList().ForEach(x => x.Stop(true));

        DatabaseHelper.Stop();          // notify stop
        await DatabaseHelper.WaitAsync(); // wait AutoSave thread exit

        DatabaseHelper.SaveDatabase(); // final flush

        Environment.Exit(exitCode);
    }

    # endregion
}
