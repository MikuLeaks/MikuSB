using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BattlePass;

[CallGSApi("BattlePassLogic_ClientRefresh")]
public class BattlePassLogic_ClientRefresh : ICallGSHandler
{
    private const uint GroupId = 25;
    private const uint CurIdSid = 1;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var now = DateTime.Now;
        var battlePass = ResolveCurrent(GameData.BattlePassTimeData.Values, now);
        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        if (battlePass == null)
        {
            SetAttr(player, CurIdSid, 0, sync);
            await CallGSRouter.SendScript(connection, "BattlePassLogic_ClientRefresh", "{}", sync);
            return;
        }

        SetAttr(player, CurIdSid, battlePass.Id, sync);

        var response = new JsonObject
        {
            ["nId"] = battlePass.Id,
            ["nStartTime"] = ToUnixSeconds(ParseConfigTime(battlePass.StartTime)),
            ["nEndTime"] = ToUnixSeconds(ParseConfigTime(battlePass.EndTime))
        };

        await CallGSRouter.SendScript(connection, "BattlePassLogic_ClientRefresh", response.ToJsonString(), sync);
    }

    private static BattlePassTimeExcel? ResolveCurrent(IEnumerable<BattlePassTimeExcel> configs, DateTime now)
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
