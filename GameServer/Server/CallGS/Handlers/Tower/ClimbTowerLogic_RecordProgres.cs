using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("ClimbTowerLogic_RecordProgres")]
public class ClimbTowerLogic_RecordProgres : ICallGSHandler
{
    private const uint TowerGroupId = 3;
    private const uint BasicProgressSid = 2;
    private const uint AdvancedProgressSid = 3;
    private const uint LevelStateSidBase = 10000;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<ClimbTowerRecordProgressParam>(param);
        if (req == null || req.LevelId == 0 || req.Area <= 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_RecordProgres", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var cycle = ResolveCurrentCycle(GameData.ClimbTowerTimeData.Values, DateTime.Now);
        if (cycle == null)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_RecordProgres", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var towerType = ResolveTowerType(cycle, (uint)req.LevelId);
        if (towerType == 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_RecordProgres", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var sync = new NtfSyncPlayer();

        var levelStateSid = LevelStateSidBase + (uint)req.LevelId;
        var levelState = GetOrCreateAttr(player.Data, TowerGroupId, levelStateSid);
        levelState.Val = MergeAreaStars(levelState.Val, req.Area, req.StarMask);
        SyncAttr(sync, player, levelState);

        var progressSid = towerType == 1 ? BasicProgressSid : AdvancedProgressSid;
        var progressAttr = GetOrCreateAttr(player.Data, TowerGroupId, progressSid);
        progressAttr.Val = req.Area >= 3 ? 0u : PackProgress((uint)req.LevelId, (uint)(req.Area + 1));
        SyncAttr(sync, player, progressAttr);

        if (req.RoleHP.Count > 0 || req.TeamEnergy.HasValue)
        {
            SaveRoleState(player, sync, towerType, req.RoleHP, req.TeamEnergy.GetValueOrDefault());
        }

        DatabaseHelper.SaveDatabaseType(player.Data);
        await CallGSRouter.SendScript(connection, "ClimbTowerLogic_RecordProgres", "{}", sync);
    }

    private static void SaveRoleState(
        PlayerInstance player,
        NtfSyncPlayer sync,
        int towerType,
        List<List<int>> roleHp,
        int teamEnergy)
    {
        var slotStart = towerType == 2 ? 4u : 1u;

        for (var slot = slotStart; slot < slotStart + 3; slot++)
        {
            var templateAttr = GetOrCreateAttr(player.Data, TowerGroupId, slot * 10);
            var hpAttr = GetOrCreateAttr(player.Data, TowerGroupId, slot * 10 + 1);
            templateAttr.Val = 0;
            hpAttr.Val = 0;
            SyncAttr(sync, player, templateAttr);
            SyncAttr(sync, player, hpAttr);
        }

        for (var i = 0; i < Math.Min(roleHp.Count, 3); i++)
        {
            var row = roleHp[i];
            if (row == null || row.Count < 2)
                continue;

            var slot = slotStart + (uint)i;
            var templateAttr = GetOrCreateAttr(player.Data, TowerGroupId, slot * 10);
            var hpAttr = GetOrCreateAttr(player.Data, TowerGroupId, slot * 10 + 1);
            templateAttr.Val = (uint)Math.Max(0, row[0]);
            hpAttr.Val = (uint)Math.Max(0, row[1]);
            SyncAttr(sync, player, templateAttr);
            SyncAttr(sync, player, hpAttr);
        }

        var energyAttr = GetOrCreateAttr(player.Data, TowerGroupId, slotStart * 10 + 2);
        energyAttr.Val = (uint)Math.Max(0, teamEnergy);
        SyncAttr(sync, player, energyAttr);
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

    private static uint PackProgress(uint levelId, uint area) => (area << 24) | (levelId & 0x00FF_FFFF);

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

    private static void SyncAttr(NtfSyncPlayer sync, PlayerInstance player, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class ClimbTowerRecordProgressParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nArea")]
    public int Area { get; set; }

    [JsonPropertyName("nStar")]
    public int StarMask { get; set; }

    [JsonPropertyName("tbRoleHP")]
    public List<List<int>> RoleHP { get; set; } = [];

    [JsonPropertyName("nTeamEnergy")]
    public int? TeamEnergy { get; set; }
}
