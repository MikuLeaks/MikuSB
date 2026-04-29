namespace MikuSB.Internationalization.Message;

#region Root

public class LanguageEN
{
    public GameTextEN Game { get; } = new();
    public ServerTextEN Server { get; } = new();
    public WordTextEN Word { get; } = new(); // a placeholder for the actual word text
}

#endregion

#region Layer 1

/// <summary>
///     path: Game
/// </summary>
public class GameTextEN
{
    public CommandTextEN Command { get; } = new();
}

/// <summary>
///     path: Server
/// </summary>
public class ServerTextEN
{
    public WebTextEN Web { get; } = new();
    public ServerInfoTextEN ServerInfo { get; } = new();
}

/// <summary>
///     path: Word
/// </summary>
public class WordTextEN
{
    public string CallItem => "Call Item";
    public string SkinPart => "Skin Part";
    public string Profile => "Profile";
    public string WeaponSkin => "Weapon Skin";
    public string Valk => "Valkyrie";
    public string Material => "Material";
    public string SupportCard => "Support Card";
    public string Weapon => "Weapon";
    public string Banner => "Gacha";
    public string Activity => "Activity";
    public string Elf => "Elf";
    public string Dress => "Outfit";
    public string Bracket => "Bracket";
    public string Disturbance => "Disturbance";
    public string Site => "Site";

    // server info
    public string Config => "Config File";
    public string Language => "Language";
    public string Log => "Log";
    public string GameData => "Game Data";
    public string Cache => "Resource Cache";
    public string CustomData => "Custom Data";
    public string Database => "Database";
    public string Command => "Command";
    public string SdkServer => "Web Server";
    public string Handler => "Packet Handler";
    public string Dispatch => "Global Dispatch";
    public string Game => "Game";
    public string Handbook => "Handbook";
    public string NotFound => "Not Found";
    public string Error => "Error";
    public string DatabaseAccount => "Database Account";
    public string Tutorial => "Tutorial";
}

#endregion

#region Layer 2

#region GameText

/// <summary>
///     path: Game.Command
/// </summary>
public class CommandTextEN
{
    public NoticeTextEN Notice { get; } = new();
    public HelpTextEN Help { get; } = new();
    public GirlTextEN Girl { get; } = new();
    public GiveAllTextEN GiveAll { get; } = new();
    public DebugTextEN Debug { get; } = new();
    public GmMenuTextEN GmMenu { get; } = new();
}

#endregion

#region ServerTextEN

/// <summary>
///     path: Server.Web
/// </summary>
public class WebTextEN
{
    public string Maintain => "The server is undergoing maintenance, please try again later.";
}

/// <summary>
///     path: Server.ServerInfo
/// </summary>
public class ServerInfoTextEN
{
    public string Shutdown => "Shutting down...";
    public string CancelKeyPressed => "Cancel key pressed (Ctrl + C), server shutting down...";
    public string StartingServer => "Starting MikuSB";
    public string CurrentVersion => "Server supported versions: {0}";
    public string InvalidVersion => "Unsupported game version {0}\nPlease update game to {1}";
    public string LoadingItem => "Loading {0}...";
    public string GeneratingItem => "Building {0}...";
    public string WaitingItem => "Waiting for process {0} to complete...";
    public string RegisterItem => "Registered {0} {1}(s).";
    public string FailedToLoadItem => "Failed to load {0}.";
    public string NewClientSecretKey => "Client Secret Key does not exist and a new Client Secret Key is being generated.";
    public string FailedToInitializeItem => "Failed to initialize {0}.";
    public string FailedToReadItem => "Failed to read {0}, file {1}";
    public string GeneratedItem => "Generated {0}.";
    public string LoadedItem => "Loaded {0}.";
    public string LoadedItems => "Loaded {0} {1}(s).";
    public string ServerRunning => "{0} server listening on {1}";

    public string ServerStarted =>
        "Startup complete! Took {0}s, better than 99% of users. Type 'help' for command help"; // This is a meme, consider localpermissiong in English

    public string MissionEnabled =>
        "Mission system enabled. This feature is still in development and may not work as expected. Please report any bugs to the developers.";
    public string KeyStoreError => "The SSL certificate does not exist, SSL functionality has been disabled.";
    public string CacheLoadSkip => "Skipped cache loading.";

    public string ConfigMissing => "{0} is missing. Please check your resource folder: {1}, {2} may not be available.";
    public string UnloadedItems => "Unloaded all {0}.";
    public string SaveDatabase => "Database saved in {0}s";

    public string WaitForAllDone =>
        "You cannot enter the game yet. Please wait for all items to load before trying again";

    public string UnhandledException => "An unhandled exception occurred: {0}";
}

#endregion

#endregion

#region Layer 3

#region CommandText

/// <summary>
///     path: Game.Command.Notice
/// </summary>
public class NoticeTextEN
{
    public string PlayerNotFound => "Player not found!";
    public string InvalidArguments => "Invalid arguments!";
    public string NoPermission => "You don't have permission!";
    public string CommandNotFound => "Command not found! Type '/help' for assistance";
    public string TargetOffline => "Target {0}({1}) is offline! Clearing current target";
    public string TargetFound => "Target {0}({1}) found. Next command will default to this target";
    public string TargetNotFound => "Target {0} not found!";
    public string InternalError => "Internal error occurred while processing command!";
}

/// <summary>
///     path: Game.Command.Help
/// </summary>
public class HelpTextEN
{
    public string Desc => "Show help information";
    public string Usage =>
        "Usage: /help\n" +
        "Usage: /help [cmd]";
    public string Commands => "Commands: ";
    public string CommandUsage => "Usage: ";
    public string CommandPermission => "Level Permission For Access: ";
    public string CommandAlias => "Command Alias：";
}

/// <summary>
///     path: Game.Command.Girl
/// </summary>
public class GirlTextEN
{
    public string Desc => "Add characters to player\n" +
                          "detail and particular can be found in Resources/ExcelOutput/card.json\n\n" +
                          "Note: -1 means all characters\n";

    public string Usage =>
        "Usage: /girl add <detail/-1> -p<particular> -l<level> -s<star>\n" +
        "Usage: /girl level <guid/-1> <level>\n" +
        "Usage: /girl neuronic <guid/-1> <level>";

    public string NotFound => "Character not found!";
    public string Added => "Granted {0} character(s) to player!";
    public string UpdateLevel => "Set {1} character(s) to level {0}!";
    public string UpdateNeuronicLevel => "Set {1} character(s) Neuronic to level {0}!";
}

/// <summary>
///     path: Game.Command.GiveAll
/// </summary>
public class GiveAllTextEN
{
    public string Desc => "Give all items to player\n"+
                          "Note: -1 means all";
    public string Usage => "Usage: /giveall weapon <detail/-1> -p<particular> -l<level>\n" +
                           "Usage: /giveall weaponskin <detail/-1> -p<particular>\n" +
                           "Usage: /giveall card <detail/-1> -p<particular> -l<level>" +
                           "Usage: /giveall profile <detail/-1> -g<genre> -p<particular> -l<level>" +
                           "Usage: /giveall skinpart <detail/-1> -g<genre> -p<particular> -l<level>" +
                           "Usage: /giveall call <detail/-1> -g<genre> -p<particular> -l<level>";
    public string NotFound => "{0} not found!";
    public string GiveAllItems => "Added {0} {1} to player!";
}

/// <summary>
///     path: Game.Command.Debug
/// </summary>
public class DebugTextEN
{
    public string Desc => "Toggle debug packet output";
    public string Usage => "Usage: /debug [on|off|simple|detail|file]";
    public string Enabled => "Debug packet output enabled.";
    public string Disabled => "Debug packet output disabled.";
    public string SimpleEnabled => "Simple debug packet output enabled.";
    public string DetailEnabled => "Detailed debug packet output enabled.";
    public string FileEnabled => "Personal debug file output enabled.";
    public string FileDisabled => "Personal debug file output disabled.";
}

/// <summary>
///     path: Game.Command.GmMenu
/// </summary>
public class GmMenuTextEN
{
    public string Desc => "Toggle GM menu";
    public string Usage => "Usage: /gm [on|off]";
    public string Enabled => "GM menu enabled.";
    public string Disabled => "GM menu disabled.";
}

#endregion

#endregion