using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.DreamCard;

[CallGSApi("DreamCard_LevelSettlement")]
public class DreamCard_LevelSettlement : ICallGSHandler
{
    private const uint LevelGroupId = 152;
    private const uint LevelSubNum = 10;
    private const int OrdinaryType = 1;
    private const int ChallengeType = 2;
    private const int EndlessType = 3;

    private static readonly Lazy<DreamCardSettlementIndex?> SettlementIndex = new(LoadIndex);

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = HandleSettlement(connection.Player!, JsonNode.Parse(param));
        await CallGSRouter.SendScript(connection, "DreamCard_LevelSettlement", response.ToJsonString(), sync);
    }

    public static (JsonObject Response, NtfSyncPlayer Sync) HandleSettlement(PlayerInstance player, JsonNode? tbParam)
    {
        var req = tbParam?.Deserialize<DreamCardLevelSettlementParam>();
        if (req == null || req.LevelId <= 0 || req.Diff <= 0 || req.Type is < OrdinaryType or > EndlessType)
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());

        var sync = new NtfSyncPlayer();
        var response = new JsonObject
        {
            ["nID"] = req.LevelId,
            ["nDiff"] = req.Diff,
            ["nType"] = req.Type
        };

        switch (req.Type)
        {
            case OrdinaryType:
                HandleOrdinary(player, sync, response, req);
                break;
            case ChallengeType:
                HandleChallenge(player, sync, response, req);
                break;
            case EndlessType:
                HandleEndless(response, req);
                break;
        }

        DatabaseHelper.SaveDatabaseType(player.Data);
        return (response, sync);
    }

    private static void HandleOrdinary(PlayerInstance player, NtfSyncPlayer sync, JsonObject response, DreamCardLevelSettlementParam req)
    {
        var baseSid = (uint)(LevelSubNum * req.LevelId);

        var passAttr = GetOrCreateAttr(player.Data, LevelGroupId, baseSid + 1);
        passAttr.Val += 1;
        SyncAttr(sync, player, passAttr);

        var diffAttr = GetOrCreateAttr(player.Data, LevelGroupId, baseSid + 2);
        diffAttr.Val = Math.Max(diffAttr.Val, (uint)req.Diff);
        SyncAttr(sync, player, diffAttr);

        var starAttr = GetOrCreateAttr(player.Data, LevelGroupId, baseSid + 3);
        starAttr.Val = MergeDifficultyBits(starAttr.Val, req.Diff, req.StarValue);
        SyncAttr(sync, player, starAttr);

        if (TryGetOrdinaryRewardId((uint)req.LevelId, (uint)req.Diff, out var rewardId) && rewardId > 0)
            response["nRewardID"] = rewardId;
    }

    private static void HandleChallenge(PlayerInstance player, NtfSyncPlayer sync, JsonObject response, DreamCardLevelSettlementParam req)
    {
        var baseSid = (uint)(LevelSubNum * req.LevelId);
        var scoreSid = baseSid + (uint)req.Diff + 4;

        var currentScore = (uint)Math.Max(0, req.Score);
        var scoreAttr = GetOrCreateAttr(player.Data, LevelGroupId, scoreSid);
        var newRecord = currentScore > scoreAttr.Val;
        scoreAttr.Val = Math.Max(scoreAttr.Val, currentScore);
        SyncAttr(sync, player, scoreAttr);

        var challengePeriodId = ResolveCurrentChallengePeriodId(DateTime.Now);
        if (challengePeriodId > 0)
        {
            var periodAttr = GetOrCreateAttr(player.Data, LevelGroupId, 0);
            periodAttr.Val = challengePeriodId;
            SyncAttr(sync, player, periodAttr);
        }

        response["NewRecord"] = newRecord;
    }

    private static void HandleEndless(JsonObject response, DreamCardLevelSettlementParam req)
    {
        response["NewRecord"] = false;
    }

    private static uint MergeDifficultyBits(uint currentValue, int diff, int starMask)
    {
        var bitStart = Math.Max(0, diff - 1) * 3;
        var result = currentValue;
        for (var i = 0; i < 3; i++)
        {
            if (((starMask >> i) & 1) == 0)
                continue;

            result |= 1u << (bitStart + i);
        }

        return result;
    }

    private static bool TryGetOrdinaryRewardId(uint levelId, uint diff, out uint rewardId)
    {
        rewardId = 0;
        var index = SettlementIndex.Value;
        if (index == null)
            return false;

        return index.TryGetOrdinaryRewardId(levelId, diff, out rewardId);
    }

    private static uint ResolveCurrentChallengePeriodId(DateTime now)
    {
        var index = SettlementIndex.Value;
        return index?.ResolveCurrentChallengePeriodId(now) ?? 0;
    }

    private static DreamCardSettlementIndex? LoadIndex()
    {
        try
        {
            var root = Path.Combine(MikuSB.Util.ConfigManager.Config.Path.ResourcePath, "dlc", "DreamCard");
            var ordinaryLevels = LoadJson<List<DreamCardOrdinarySettlementEntry>>(Path.Combine(root, "levellist.json")) ?? [];
            var challengeTimes = LoadJson<List<DreamCardChallengeTimeEntry>>(Path.Combine(root, "chall_time.json")) ?? [];
            return new DreamCardSettlementIndex(ordinaryLevels, challengeTimes);
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

    private static PlayerAttr GetOrCreateAttr(PlayerGameData data, uint gid, uint sid)
    {
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = gid,
            Sid = sid
        };
        data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncAttr(NtfSyncPlayer sync, PlayerInstance player, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class DreamCardLevelSettlementParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nDiff")]
    public int Diff { get; set; }

    [JsonPropertyName("nType")]
    public int Type { get; set; }

    [JsonPropertyName("nStarValue")]
    public int StarValue { get; set; }

    [JsonPropertyName("nScore")]
    public int Score { get; set; }
}

internal sealed class DreamCardSettlementIndex
{
    private readonly Dictionary<(uint LevelId, uint Diff), uint> ordinaryRewardIds;
    private readonly List<DreamCardChallengeTimeEntry> challengeTimes;

    public DreamCardSettlementIndex(
        IEnumerable<DreamCardOrdinarySettlementEntry> ordinaryLevels,
        IEnumerable<DreamCardChallengeTimeEntry> challengeTimes)
    {
        ordinaryRewardIds = ordinaryLevels
            .Where(x => x.LevelListId > 0 && x.HardStage > 0)
            .GroupBy(x => (x.LevelListId, x.HardStage))
            .ToDictionary(x => x.Key, x => x.First().RewardId);

        this.challengeTimes = challengeTimes.ToList();
    }

    public bool TryGetOrdinaryRewardId(uint levelId, uint diff, out uint rewardId)
    {
        return ordinaryRewardIds.TryGetValue((levelId, diff), out rewardId);
    }

    public uint ResolveCurrentChallengePeriodId(DateTime now)
    {
        foreach (var entry in challengeTimes.OrderBy(x => x.ChallTimeId))
        {
            var start = ParseConfigTime(entry.StartTime);
            var end = ParseConfigTime(entry.EndTime);
            if (!start.HasValue || !end.HasValue)
                continue;

            if (start.Value <= now && now < end.Value)
                return entry.ChallTimeId;
        }

        return 0;
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

internal sealed class DreamCardOrdinarySettlementEntry
{
    [JsonPropertyName("LevelListID")]
    public uint LevelListId { get; set; }

    [JsonPropertyName("HardStage")]
    public uint HardStage { get; set; }

    [JsonPropertyName("RewardID")]
    public uint RewardId { get; set; }
}

internal sealed class DreamCardChallengeTimeEntry
{
    [JsonPropertyName("ChallTimeID")]
    public uint ChallTimeId { get; set; }

    [JsonPropertyName("StartTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("EndTime")]
    public string EndTime { get; set; } = "";
}
