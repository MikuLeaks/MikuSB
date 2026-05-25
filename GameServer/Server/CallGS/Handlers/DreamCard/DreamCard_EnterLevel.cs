using MikuSB.Data;
using MikuSB.Util;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.DreamCard;

[CallGSApi("DreamCard_EnterLevel")]
public class DreamCard_EnterLevel : ICallGSHandler
{
    private static readonly Random Random = new();
    private static readonly Lazy<DreamCardLevelIndex?> LevelIndex = new(LoadLevelIndex);

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<DreamCardEnterLevelParam>(param);
        if (req == null || req.LevelId <= 0 || req.Diff <= 0 || req.Type is < 1 or > 3)
        {
            await CallGSRouter.SendScript(connection, "DreamCard_EnterLevel", "null");
            return;
        }

        var now = DateTime.Now;
        if (!IsAllowed(req, now))
        {
            await CallGSRouter.SendScript(connection, "DreamCard_EnterLevel", "null");
            return;
        }

        var response = new JsonObject
        {
            ["nSeed"] = Random.Next(1, 1_000_000_000),
            ["nID"] = req.LevelId,
            ["nDiff"] = req.Diff,
            ["nType"] = req.Type
        };

        await CallGSRouter.SendScript(connection, "DreamCard_EnterLevel", response.ToJsonString());
    }

    private static bool IsAllowed(DreamCardEnterLevelParam req, DateTime now)
    {
        var index = LevelIndex.Value;
        if (index == null)
            return true;

        return req.Type switch
        {
            1 => index.OpenOrdinaryLevelIds(now).Contains((uint)req.LevelId),
            2 => index.IsChallengeOpen((uint)req.LevelId, now),
            3 => index.IsEndlessOpen((uint)req.LevelId, now),
            _ => false
        };
    }

    private static DreamCardLevelIndex? LoadLevelIndex()
    {
        try
        {
            var resourceRoot = ConfigManager.Config.Path.ResourcePath;
            var dreamCardRoot = Path.Combine(resourceRoot, "dlc", "DreamCard");

            var ordinaryLevels = LoadJson<List<DreamCardOrdinaryLevelEntry>>(Path.Combine(dreamCardRoot, "levellist.json")) ?? [];
            var challengeLevels = LoadJson<List<DreamCardChallengeLevelEntry>>(Path.Combine(dreamCardRoot, "challenge.json")) ?? [];
            var endlessLevels = LoadJson<List<DreamCardEndlessLevelEntry>>(Path.Combine(dreamCardRoot, "endless.json")) ?? [];

            return new DreamCardLevelIndex(ordinaryLevels, challengeLevels, endlessLevels);
        }
        catch
        {
            return null;
        }
    }

    private static T? LoadJson<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
    }
}

internal sealed class DreamCardEnterLevelParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nDiff")]
    public int Diff { get; set; }

    [JsonPropertyName("nType")]
    public int Type { get; set; }

    [JsonPropertyName("nRoleId")]
    public int RoleId { get; set; }
}

internal sealed class DreamCardLevelIndex
{
    private readonly HashSet<uint> ordinaryLevelIds;
    private readonly Dictionary<uint, DreamCardChallengeLevelEntry> challengeLevels;
    private readonly Dictionary<uint, DreamCardEndlessLevelEntry> endlessLevels;

    public DreamCardLevelIndex(
        IEnumerable<DreamCardOrdinaryLevelEntry> ordinaryLevels,
        IEnumerable<DreamCardChallengeLevelEntry> challengeLevels,
        IEnumerable<DreamCardEndlessLevelEntry> endlessLevels)
    {
        ordinaryLevelIds = ordinaryLevels
            .Where(x => x.LevelListId > 0)
            .Select(x => x.LevelListId)
            .ToHashSet();

        this.challengeLevels = challengeLevels
            .Where(x => x.ChallengeId > 0)
            .GroupBy(x => x.ChallengeId)
            .ToDictionary(x => x.Key, x => x.First());

        this.endlessLevels = endlessLevels
            .Where(x => x.EndlessId > 0)
            .GroupBy(x => x.EndlessId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    public HashSet<uint> OpenOrdinaryLevelIds(DateTime now)
    {
        var ids = new HashSet<uint>();
        foreach (var activity in GameData.DreamCardActivityData.Values)
        {
            if (!IsActivityOpen(activity, now))
                continue;

            foreach (var id in activity.LevelListID)
            {
                if (ordinaryLevelIds.Contains(id))
                    ids.Add(id);
            }
        }

        return ids;
    }

    public bool IsChallengeOpen(uint id, DateTime now)
    {
        return challengeLevels.TryGetValue(id, out var entry) && IsWithin(entry.StartTime, entry.EndTime, now);
    }

    public bool IsEndlessOpen(uint id, DateTime now)
    {
        return endlessLevels.TryGetValue(id, out var entry) && IsWithin(entry.StartTime, entry.EndTime, now);
    }

    private static bool IsActivityOpen(Data.Excel.DreamCardActivityExcel config, DateTime now)
    {
        var start = ParseConfigTime(config.StartTime);
        if (!start.HasValue || start > now)
            return false;

        var end = ParseConfigTime(config.EndTime);
        if (end.HasValue && now >= end.Value)
            return false;

        return string.IsNullOrWhiteSpace(config.Condition);
    }

    private static bool IsWithin(string? startRaw, string? endRaw, DateTime now)
    {
        var start = ParseConfigTime(startRaw);
        if (!start.HasValue || now < start.Value)
            return false;

        var end = ParseConfigTime(endRaw);
        return !end.HasValue || now < end.Value;
    }

    private static DateTime? ParseConfigTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim().Trim('[', ']');
        if (normalized.Length != 12)
            return null;

        return DateTime.TryParseExact(
            normalized,
            "yyyyMMddHHmm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var value)
            ? value
            : null;
    }
}

internal sealed class DreamCardOrdinaryLevelEntry
{
    [JsonPropertyName("LevelListID")]
    public uint LevelListId { get; set; }
}

internal sealed class DreamCardChallengeLevelEntry
{
    [JsonPropertyName("ChallengeId")]
    public uint ChallengeId { get; set; }

    [JsonPropertyName("StartTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("EndTime")]
    public string EndTime { get; set; } = "";
}

internal sealed class DreamCardEndlessLevelEntry
{
    [JsonPropertyName("EndlessID")]
    public uint EndlessId { get; set; }

    [JsonPropertyName("StartTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("EndTime")]
    public string EndTime { get; set; } = "";
}
