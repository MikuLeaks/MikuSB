using MikuSB.Data;
using MikuSB.Data.Excel;
using System.Globalization;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.DreamCard;

[CallGSApi("DreamCard_CheckOpen")]
public class DreamCard_CheckOpen : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var now = DateTime.Now;
        var ids = GameData.DreamCardActivityData.Values
            .Where(x => IsOpen(x, now))
            .OrderBy(x => x.ID)
            .Select(x => JsonValue.Create(x.ID))
            .ToArray();

        var response = new JsonObject
        {
            ["tbID"] = new JsonArray(ids)
        };

        await CallGSRouter.SendScript(connection, "DreamCard_CheckOpen", response.ToJsonString());
    }

    private static bool IsOpen(DreamCardActivityExcel config, DateTime now)
    {
        var start = ParseConfigTime(config.StartTime);
        if (!start.HasValue || start > now)
            return false;

        var end = ParseConfigTime(config.EndTime);
        if (end.HasValue && now >= end.Value)
            return false;

        return string.IsNullOrWhiteSpace(config.Condition);
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
