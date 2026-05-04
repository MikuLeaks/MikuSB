namespace MikuSB.Internationalization.Message;

#region Root

public class LanguageCHT
{
    public GameTextCHT Game { get; } = new();
    public ServerTextCHT Server { get; } = new();
    public WordTextCHT Word { get; } = new(); // a placeholder for the actual word text
}

#endregion

#region Layer 1

/// <summary>
///     path: Game
/// </summary>
public class GameTextCHT
{
    public CommandTextCHT Command { get; } = new();
}

/// <summary>
///     path: Server
/// </summary>
public class ServerTextCHT
{
    public WebTextCHT Web { get; } = new();
    public ServerInfoTextCHT ServerInfo { get; } = new();
}

/// <summary>
///     path: Word
/// </summary>
public class WordTextCHT
{
    public string WeaponPart => "武器部件";
    public string CallItem => "召喚道具";
    public string SkinPart => "外觀部件";
    public string Profile => "個人資料";
    public string WeaponSkin => "武器外觀";
    public string SupportCard => "支援卡";
    public string Weapon => "武器";
    public string Rank => "星魂";
    public string Avatar => "角色";
    public string Material => "材料";
    public string Pet => "寵物";
    public string Relic => "遺器";
    public string Equipment => "光錐";
    public string Talent => "行跡";
    public string Banner => "卡池";
    public string Activity => "活動";
    public string CdKey => "兌換碼";
    public string VideoKey => "過場動畫金鑰";
    public string Buff => "祝福";
    public string Miracle => "奇物";
    public string Unlock => "奢侈品";
    public string TrainParty => "派對車廂";

    // server info
    public string Config => "配置文件";
    public string Language => "語言";
    public string Log => "日誌";
    public string GameData => "遊戲數據";
    public string Cache => "資源緩存";
    public string CustomData => "自定義數據";
    public string Database => "數據庫";
    public string Command => "命令";
    public string SSL => "SSL";
    public string Ec2b => "Ec2b";
    public string SdkServer => "Web服務器";
    public string Handler => "包處理器";
    public string Dispatch => "全局分發";
    public string Game => "遊戲";
    public string Handbook => "手冊";
    public string NotFound => "未找到";
    public string Error => "錯誤";
    public string FloorInfo => "區域文件";
    public string FloorGroupInfo => "區域組文件";
    public string FloorMissingResult => "傳送與世界生成";
    public string FloorGroupMissingResult => "傳送、怪物戰鬥與世界生成";
    public string Mission => "任務";
    public string MissionInfo => "任務文件";
    public string SubMission => "子任務";
    public string SubMissionInfo => "子任務文件";
    public string MazeSkill => "角色秘技";
    public string MazeSkillInfo => "角色秘技文件";
    public string Dialogue => "模擬宇宙事件";
    public string DialogueInfo => "模擬宇宙事件文件";
    public string Performance => "劇情操作";
    public string PerformanceInfo => "劇情操作文件";
    public string RogueChestMap => "模擬宇宙地圖";
    public string RogueChestMapInfo => "模擬宇宙地圖文件";
    public string ChessRogueRoom => "模擬宇宙DLC";
    public string ChessRogueRoomInfo => "模擬宇宙DLC文件";
    public string SummonUnit => "秘技生成";
    public string SummonUnitInfo => "秘技生成文件";
    public string RogueTournRoom => "差分宇宙";
    public string RogueTournRoomInfo => "差分宇宙房間文件";
    public string TypesOfRogue => "類型的模擬宇宙";
    public string RogueMagicRoom => "不可知域";
    public string RogueMagicRoomInfo => "不可知域房間文件";
    public string RogueDiceSurface => "骰面效果";
    public string RogueDiceSurfaceInfo => "骰面效果文件";
    public string AdventureModifier => "AdventureModifier";
    public string AdventureModifierInfo => "AdventureModifier文件";
    public string RogueMapGen => "RogueMapGen文件";
    public string RogueMiracleGroup => "RogueMiracleGroup文件";
    public string RogueMiracleEffectGen => "RogueMiracleEffectGen文件";

    public string DatabaseAccount => "數據庫賬號";
    public string Tutorial => "教程";
}

#endregion

#region Layer 2

#region GameText

/// <summary>
///     path: Game.Command
/// </summary>
public class CommandTextCHT
{
    public NoticeTextCHT Notice { get; } = new();
    public HelpTextCHT Help { get; } = new();
    public GirlTextCHT Girl { get; } = new();
    public GiveAllTextCHT GiveAll { get; } = new();
    public DebugTextCHT Debug { get; } = new();
    public GmMenuTextCHT GmMenu { get; } = new();
}

#endregion

#region ServerText

/// <summary>
///     path: Server.Web
/// </summary>
public class WebTextCHT
{
    public string Maintain => "服務器正在維修, 請稍後嘗試。";
}

/// <summary>
///     path: Server.ServerInfo
/// </summary>
public class ServerInfoTextCHT
{
    public string Shutdown => "關閉中…";
    public string CancelKeyPressed => "已按下取消鍵 (Ctrl + C), 服務器即將關閉…";
    public string StartingServer => "正在啟動 MikuSB";
    public string CurrentVersion => "當前服務端支援的版本: {0}";
    public string InvalidVersion => "目前為不受支援的遊戲版本 {0}\n請更新遊戲到 {1}";
    public string LoadingItem => "正在加載 {0}…";
    public string GeneratingItem => "正在生成 {0}…";
    public string WaitingItem => "正在等待進程 {0} 完成…";
    public string RegisterItem => "註冊了 {0} 個 {1}。";
    public string FailedToLoadItem => "加載 {0} 失敗。";
    public string NewClientSecretKey => "客戶端密鑰不存在, 正在生成新的客戶端密鑰。";
    public string FailedToInitializeItem => "初始化 {0} 失敗。";
    public string FailedToReadItem => "讀取 {0} 失敗, 文件{1}";
    public string GeneratedItem => "已生成 {0}。";
    public string LoadedItem => "已加載 {0}。";
    public string LoadedItems => "已加載 {0} 個 {1}。";
    public string ServerRunning => "{0} 服務器正在監聽 {1}";
    public string ServerStarted => "啟動完成!用時 {0}s, 擊敗了99%的用戶, 輸入 『help』 來獲取命令幫助"; // 玩梗, 考慮英語版本將其本土化
    public string MissionEnabled => "任務系統已啟用, 此功能仍在開發中, 且可能不會按預期工作, 如果遇見任何bug, 請匯報給開發者。";
    public string KeyStoreError => "SSL證書不存在, 已關閉SSL功能。";
    public string CacheLoadSkip => "已跳過緩存加載。";

    public string ConfigMissing => "{0} 缺失, 請檢查你的資源文件夾: {1}, {2} 可能不能使用。";
    public string UnloadedItems => "卸載了所有 {0}。";
    public string SaveDatabase => "已保存數據庫, 用時 {0}s";
    public string WaitForAllDone => "現在還不可以進入遊戲, 請等待所有項目加載完成後再試";

    public string UnhandledException => "發生未經處理的異常: {0}";
}

#endregion

#endregion

#region Layer 3

#region CommandText

/// <summary>
///     path: Game.Command.Notice
/// </summary>
public class NoticeTextCHT
{
    public string PlayerNotFound => "未找到玩家!";
    public string InvalidArguments => "無效的參數!";
    public string NoPermission => "你沒有權限這麽做!";
    public string CommandNotFound => "未找到命令! 輸入 '/help' 來獲取幫助";
    public string TargetOffline => "目標 {0}({1}) 離線了!清除當前目標";
    public string TargetFound => "找到目標 {0}({1}), 下一次命令將默認對其執行";
    public string TargetNotFound => "未找到目標 {0}!";
    public string InternalError => "在處理命令時發生了內部錯誤: {0}!";
}

/// <summary>
///     path: Game.Command.Help
/// </summary>
public class HelpTextCHT
{
    public string Desc => "顯示幫助信息";
    public string Usage =>
        "用法: /help\n" +
        "用法: /help [命令]";
    public string Commands => "命令: ";
    public string CommandPermission => "所需權限: ";
    public string CommandAlias => "命令別名: ";
}

/// <summary>
///     path: Game.Command.Girl
/// </summary>
public class GirlTextCHT
{
    public string Desc => "新增角色到玩家\n" +
                          "detail 和 particular 可在 Resources/ExcelOutput/card.json 中查看\n\n" +
                          "注意：-1 表示所有角色\n";

    public string Usage =>
        "用法: /girl add <detail/-1> -p<particular> -l<level> -s<star>\n" +
        "用法: /girl level <guid/-1> <level>\n" +
        "用法: /girl neuronic <guid/-1> <level>\n" +
        "用法: /girl manifestation <guid/-1> <level>";

    public string NotFound => "角色不存在！";
    public string Added => "已為玩家新增 {0} 個角色！";
    public string UpdateLevel => "已將 {1} 個角色等級設為 {0}！";
    public string UpdateNeuronicLevel => "已將 {1} 個角色的神經元等級設置為 {0}！";
    public string UpdateManifestationLevel => "已将 {1} 个角色的天啟/武格等级设置为 {0}！";
}

/// <summary>
///     path: Game.Command.GiveAll
/// </summary>
public class GiveAllTextCHT
{
    public string Desc => "給予玩家所有物品\n" +
                          "注意：-1 表示全部";
    public string Usage => "用法：/giveall weapon <detail/-1> -p<特定> -l<等級>\n" +
                           "用法：/giveall weaponskin <detail/-1> -p<特定>\n" +
                           "用法：/giveall card <detail/-1> -p<特定> -l<等級>" +
                           "用法：/giveall profile <detail/-1> -g<類型> -p<特定> -l<等級>" +
                           "用法：/giveall skinpart <detail/-1> -g<類型> -p<特定> -l<等級>" +
                           "用法：/giveall weaponpart <detail/-1> -g<類型> -p<特定> -l<等級>" +
                           "用法：/giveall call <detail/-1> -g<類型> -p<特定> -l<等級>";
    public string NotFound => "未找到 {0}！";
    public string GiveAllItems => "已向玩家添加 {0} 個 {1}！";
}

/// <summary>
///     path: Game.Command.Debug
/// </summary>
public class DebugTextCHT
{
    public string Desc => "切換調試封包輸出";
    public string Usage => "用法: /debug [on|off|simple|detail|file]";
    public string Enabled => "已啟用調試封包輸出。";
    public string Disabled => "已停用調試封包輸出。";
    public string SimpleEnabled => "已啟用簡易調試封包輸出。";
    public string DetailEnabled => "已啟用詳細調試封包輸出。";
    public string FileEnabled => "個人調試檔案輸出已啟用。";
    public string FileDisabled => "個人調試檔案輸出已停用。";
}

/// <summary>
///     path: Game.Command.GmMenu
/// </summary>
public class GmMenuTextCHT
{
    public string Desc => "切換GM選單";
    public string Usage => "用法: /gm [on|off]";
    public string Enabled => "已啟用GM選單。";
    public string Disabled => "已停用GM選單。";
}

#endregion

#endregion