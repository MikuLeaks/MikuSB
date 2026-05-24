using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_EnterLevel")]
public class VirCaptureLevel_EnterLevel : ICallGSHandler
{
    private const uint GroupId = 128;
    private const uint MapDataStart = 10000;
    private const uint MaxMapCount = 3;
    private const uint MaxMapDataLen = 3000;
    private const uint OffMapId = 1;
    private const uint OffDayNight = 7;
    private const uint OffMapLevel = 8;
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureEnterLevelParam>(param);
        if (req == null || req.LevelId == 0 || req.TeamId <= 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var now = DateTime.Now;
        var act = ResolveCurrent(GameData.VirCaptureTimeData.Values, now);
        if (act == null || !act.CaptureRegionId.Contains((uint)req.LevelId))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_EnterLevel", "{\"sErr\":\"ui.TxtNotOpen\"}");
            return;
        }

        if (!GameData.VirCaptureCaptureRegionData.TryGetValue((uint)req.LevelId, out var region))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var regionStart = ParseConfigTime(region.StartTime);
        var regionEnd = ParseConfigTime(region.EndTime);
        if (!regionStart.HasValue || !regionEnd.HasValue || now < regionStart.Value || now >= regionEnd.Value)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_EnterLevel", "{\"sErr\":\"ui.TxtNotOpen\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        EnsureMapState(player, (uint)req.LevelId, sync);

        var rsp = $"{{\"nSeed\":{Random.Next(1, 1_000_000_000)}}}";
        await CallGSRouter.SendScript(connection, "VirCaptureLevel_EnterLevel", rsp, sync);
    }

    private static void EnsureMapState(PlayerInstance player, uint levelId, NtfSyncPlayer sync)
    {
        var slotStart = FindOrAllocateMapSlot(player, levelId);
        if (slotStart == 0)
            return;

        EnsureMapAttr(player, slotStart + OffMapId, levelId, sync);
        EnsureMapAttr(player, slotStart + OffDayNight, 1, sync);
        EnsureMapAttr(player, slotStart + OffMapLevel, 1, sync);
    }

    private static uint FindOrAllocateMapSlot(PlayerInstance player, uint levelId)
    {
        uint? emptySlot = null;
        for (uint i = 0; i < MaxMapCount; i++)
        {
            var slotStart = MapDataStart + (i * MaxMapDataLen);
            var mapIdAttr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == slotStart + OffMapId);
            if (mapIdAttr?.Val == levelId)
                return slotStart;

            if (emptySlot == null && (mapIdAttr == null || mapIdAttr.Val == 0))
                emptySlot = slotStart;
        }

        return emptySlot ?? 0;
    }

    private static void EnsureMapAttr(PlayerInstance player, uint sid, uint minValue, NtfSyncPlayer sync)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr == null)
        {
            attr = new PlayerAttr
            {
                Gid = GroupId,
                Sid = sid,
                Val = minValue
            };
            player.Data.Attrs.Add(attr);
            SyncAttr(player, sync, sid, minValue);
            return;
        }

        if (attr.Val < minValue)
        {
            attr.Val = minValue;
            SyncAttr(player, sync, sid, attr.Val);
        }
    }

    private static void SyncAttr(PlayerInstance player, NtfSyncPlayer sync, uint sid, uint value)
    {
        sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }

    private static VirCaptureTimeExcel? ResolveCurrent(IEnumerable<VirCaptureTimeExcel> configs, DateTime now)
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
        if (latestStarted != null && latestStarted.End > latestStarted.Start)
            return latestStarted.Config;

        return null;
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

internal sealed class VirCaptureEnterLevelParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nTeamID")]
    public int TeamId { get; set; }
}
