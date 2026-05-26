using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.DLC;

[CallGSApi("DLCLogic_CheckOpenAct")]
public class DLCLogic_CheckOpenAct : ICallGSHandler
{
    private const uint GroupId = 15;
    private const uint ActIdSid = 1;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var now = DateTime.Now;
        var act = ResolveCurrent(GameData.DlcActivityData.Values, now);
        if (act == null)
        {
            await CallGSRouter.SendScript(connection, "DLCLogic_CheckOpenAct", "{\"bOpen\":false}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        SetAttr(player, ActIdSid, act.Id, sync);

        var response = new JsonObject
        {
            ["bOpen"] = true,
            ["nId"] = act.Id,
            ["nStartTime"] = ToUnixSeconds(ParseConfigTime(act.EnterStartTime)),
            ["nEndTime"] = ToUnixSeconds(ParseConfigTime(act.CloseEndTime))
        };

        await CallGSRouter.SendScript(connection, "DLCLogic_CheckOpenAct", response.ToJsonString(), sync);
    }

    private static DlcActivityExcel? ResolveCurrent(IEnumerable<DlcActivityExcel> configs, DateTime now)
    {
        var parsed = configs
            .Select(x => new
            {
                Config = x,
                Start = ParseConfigTime(x.EnterStartTime),
                End = ParseConfigTime(x.CloseEndTime)
            })
            .Where(x => x.Start.HasValue && x.End.HasValue)
            .OrderBy(x => x.Start)
            .ToList();

        var current = parsed.FirstOrDefault(x => x.Start <= now && now < x.End);
        if (current != null)
            return current.Config;

        var latestStarted = parsed.LastOrDefault(x => x.Start <= now && x.End > x.Start);
        return latestStarted?.Config;
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

    private static void SetAttr(PlayerInstance player, uint sid, uint value, NtfSyncPlayer sync)
    {
        var attr = GetOrCreateAttr(player, sid);
        if (attr.Val != value)
        {
            attr.Val = value;
            sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = value;
            sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = value;
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
}
