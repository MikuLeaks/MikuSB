using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCapture_CheckOpenAct")]
public class VirCapture_CheckOpenAct : ICallGSHandler
{
    private const uint GroupId = 128;
    private const uint ActIdSid = 1;
    private const uint CurLevelSid = 3;
    private const uint TrialActIdSid = 6;
    private const uint SeasonActIdSid = 9;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var now = DateTime.Now;
        var act = ResolveCurrent(GameData.VirCaptureTimeData.Values, now);
        if (act == null)
        {
            await CallGSRouter.SendScript(connection, "VirCapture_CheckOpenAct", "{\"bOpen\":false}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        SetAttr(player, ActIdSid, act.Id, sync);
        EnsureMinAttr(player, CurLevelSid, 1, sync);

        var response = new JsonObject
        {
            ["bOpen"] = true,
            ["nId"] = act.Id,
            ["nStartTime"] = ToUnixSeconds(ParseConfigTime(act.StartTime)),
            ["nEndTime"] = ToUnixSeconds(ParseConfigTime(act.EndTime))
        };

        var season = ResolveCurrent(GameData.VirCaptureSeasonData.Values, now);
        if (season != null)
        {
            SetAttr(player, SeasonActIdSid, season.Id, sync);
            response["tbSeason"] = new JsonObject
            {
                ["nId"] = season.Id,
                ["nStartTime"] = ToUnixSeconds(ParseConfigTime(season.StartTime)),
                ["nEndTime"] = ToUnixSeconds(ParseConfigTime(season.EndTime))
            };
        }
        else
        {
            SetAttr(player, SeasonActIdSid, 0, sync);
        }

        var trial = ResolveCurrent(GameData.VirCaptureTrialTimeData.Values, now);
        SetAttr(player, TrialActIdSid, trial?.Id ?? 0, sync);

        await CallGSRouter.SendScript(connection, "VirCapture_CheckOpenAct", response.ToJsonString(), sync);
    }

    private static T? ResolveCurrent<T>(IEnumerable<T> configs, DateTime now) where T : class
    {
        var parsed = configs
            .Select(x => new
            {
                Config = x,
                Start = ParseConfigTime(GetTimeValue(x, true)),
                End = ParseConfigTime(GetTimeValue(x, false))
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

    private static string? GetTimeValue<T>(T value, bool start) where T : class
    {
        return value switch
        {
            VirCaptureTimeExcel time => start ? time.StartTime : time.EndTime,
            VirCaptureSeasonExcel season => start ? season.StartTime : season.EndTime,
            _ => null
        };
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

    private static long ToUnixSeconds(DateTime? value)
    {
        return value.HasValue ? new DateTimeOffset(value.Value).ToUnixTimeSeconds() : 0L;
    }

    private static void EnsureMinAttr(PlayerInstance player, uint sid, uint minValue, NtfSyncPlayer sync)
    {
        var attr = GetOrCreateAttr(player, sid);
        if (attr.Val < minValue)
        {
            attr.Val = minValue;
            SyncAttr(player, sync, sid, attr.Val);
        }
    }

    private static void SetAttr(PlayerInstance player, uint sid, uint value, NtfSyncPlayer sync)
    {
        var attr = GetOrCreateAttr(player, sid);
        if (attr.Val != value)
        {
            attr.Val = value;
            SyncAttr(player, sync, sid, value);
        }
    }

    private static PlayerAttr GetOrCreateAttr(PlayerInstance player, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = GroupId,
            Sid = sid
        };
        player.Data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncAttr(PlayerInstance player, NtfSyncPlayer sync, uint sid, uint value)
    {
        sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }
}
