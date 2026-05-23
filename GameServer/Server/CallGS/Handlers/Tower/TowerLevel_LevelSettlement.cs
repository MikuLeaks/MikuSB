using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using MikuSB.Util;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("TowerLevel_LevelSettlement")]
public class TowerLevel_LevelSettlement : ICallGSHandler
{
    private static readonly Logger Logger = new("Tower");
    private const uint TowerGroupId = 3;
    private const uint LaunchPassGroupId = 22;
    private const uint BasicProgressSid = 2;
    private const uint AdvancedProgressSid = 3;
    private const uint LevelStateSidBase = 10000;
    private const int FinalArea = 3;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = HandleSettlement(connection.Player!, JsonNode.Parse(param));
        await CallGSRouter.SendScript(connection, "TowerLevel_LevelSettlement", response.ToJsonString(), sync);
    }

    public static (JsonNode Response, NtfSyncPlayer Sync) HandleSettlement(PlayerInstance player, JsonNode? tbParam)
    {
        var req = tbParam?.Deserialize<TowerLevelSettlementParam>();
        if (req == null || req.TowerId == 0 || req.LevelId == 0)
        {
            Logger.Error($"Invalid tower settlement payload: {tbParam?.ToJsonString() ?? "null"}");
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());
        }

        var cycle = ResolveCurrentCycle(GameData.ClimbTowerTimeData.Values, DateTime.Now);
        if (cycle == null)
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());

        var towerType = ResolveTowerType(cycle, (uint)req.TowerId);
        if (towerType == 0)
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());

        var sync = new NtfSyncPlayer();
        var levelStateSid = LevelStateSidBase + (uint)req.TowerId;
        var levelState = GetOrCreateAttr(player.Data, TowerGroupId, levelStateSid);
        levelState.Val = MergeAreaStars(levelState.Val, FinalArea, req.StarMask);
        SyncAttr(sync, player, levelState);

        var progressSid = towerType == 1 ? BasicProgressSid : AdvancedProgressSid;
        var progressAttr = GetOrCreateAttr(player.Data, TowerGroupId, progressSid);
        progressAttr.Val = 0;
        SyncAttr(sync, player, progressAttr);

        var passAttr = GetOrCreateAttr(player.Data, LaunchPassGroupId, (uint)req.LevelId);
        passAttr.Val = Math.Max(1u, passAttr.Val + 1);
        SyncAttr(sync, player, passAttr);

        Logger.Info(
            $"Tower settlement saved. uid={player.Uid} towerId={req.TowerId} levelId={req.LevelId} starMask={req.StarMask} " +
            $"towerStateSid={levelStateSid} towerStateVal={levelState.Val} progressSid={progressSid} passVal={passAttr.Val}");

        DatabaseHelper.SaveDatabaseType(player.Data);
        return (new JsonObject(), sync);
    }

    private static uint MergeAreaStars(uint currentValue, int area, int starMask)
    {
        var areaIndex = Math.Clamp(area, 1, 3) - 1;
        var result = currentValue;
        for (var i = 0; i < 3; i++)
        {
            if (((starMask >> i) & 1) == 0)
                continue;

            var bitIndex = areaIndex * 3 + i;
            result |= 1u << bitIndex;
        }

        return result;
    }

    private static int ResolveTowerType(ClimbTowerTimeExcel cycle, uint levelId)
    {
        if (ContainsLevel(cycle.GetLevelGroups(1), levelId))
            return 1;

        if (ContainsLevel(cycle.GetLevelGroups(2), levelId))
            return 2;

        return 0;
    }

    private static bool ContainsLevel(IEnumerable<IReadOnlyList<uint>> groups, uint levelId)
    {
        return groups.Any(group => group.Any(id => id == levelId));
    }

    private static ClimbTowerTimeExcel? ResolveCurrentCycle(IEnumerable<ClimbTowerTimeExcel> configs, DateTime now)
    {
        var parsed = configs
            .Select(x => new
            {
                Config = x,
                Start = ParseConfigTime(x.StartTime),
                End = ParseConfigTime(x.EndTime)
            })
            .Where(x => x.Start.HasValue && x.End.HasValue)
            .OrderBy(x => x.Start)
            .ToList();

        var current = parsed.FirstOrDefault(x => x.Start <= now && now < x.End);
        if (current != null)
            return current.Config;

        var latestStarted = parsed.LastOrDefault(x => x.Start <= now);
        if (latestStarted != null)
            return latestStarted.Config;

        return parsed.FirstOrDefault()?.Config;
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
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var value)
            ? value
            : null;
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

    private static void SyncAttr(MikuSB.Proto.NtfSyncPlayer sync, PlayerInstance player, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class TowerLevelSettlementParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nTowerID")]
    public int TowerId { get; set; }

    [JsonPropertyName("nStar")]
    public int StarMask { get; set; }
}
