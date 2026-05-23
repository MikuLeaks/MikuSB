using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("ClimbTowerLogic_CheckCycleLevel")]
public class ClimbTowerLogic_CheckCycleLevel : ICallGSHandler
{
    private const uint TowerGroupId = 3;
    private const uint TimeSubId = 1;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var current = ResolveCurrentCycle(GameData.ClimbTowerTimeData.Values, DateTime.Now);
        if (current == null)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_CheckCycleLevel", "{}");
            return;
        }

        var currentTimeId = GetAttr(player.Data, TowerGroupId, TimeSubId);
        var sync = new NtfSyncPlayer();
        if (currentTimeId != current.ID)
        {
            ResetTowerAttrs(player, sync);
            SetAttr(player.Data, TowerGroupId, TimeSubId, current.ID, sync, player);
            DatabaseHelper.SaveDatabaseType(player.Data);
        }

        await CallGSRouter.SendScript(connection, "ClimbTowerLogic_CheckCycleLevel", $$"""{"timeID":{{current.ID}}}""", sync);
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

    private static uint GetAttr(PlayerGameData data, uint gid, uint sid)
    {
        return data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid)?.Val ?? 0;
    }

    private static void ResetTowerAttrs(PlayerInstance player, NtfSyncPlayer sync)
    {
        var towerAttrs = player.Data.Attrs
            .Where(x => x.Gid == TowerGroupId)
            .ToList();

        foreach (var attr in towerAttrs)
        {
            sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = 0;
            sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = 0;
        }

        player.Data.Attrs.RemoveAll(x => x.Gid == TowerGroupId);
    }

    private static void SetAttr(PlayerGameData data, uint gid, uint sid, uint value, NtfSyncPlayer sync, PlayerInstance player)
    {
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr == null)
        {
            attr = new PlayerAttr
            {
                Gid = gid,
                Sid = sid
            };
            data.Attrs.Add(attr);
        }

        attr.Val = value;
        sync.Custom[player.ToPackedAttrKey(gid, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(gid, sid)] = value;
    }
}
