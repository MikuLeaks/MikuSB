using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.DreamCard;

[CallGSApi("DreamCard_UpdateData")]
public class DreamCard_UpdateData : ICallGSHandler
{
    private const uint DataGroupId = 62;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        var dirty = false;

        try
        {
            var entries = JsonSerializer.Deserialize<List<DreamCardUpdateDataEntry>>(param) ?? [];
            foreach (var entry in entries)
            {
                if (entry.Id <= 0)
                    continue;

                var value = NormalizeJson(entry.Data);
                player.SetStrAttr(DataGroupId, (uint)entry.Id, value);
                sync.CustomStr[player.ToShiftedAttrKey(DataGroupId, (uint)entry.Id)] = value;
                dirty = true;
            }
        }
        catch
        {
            // Ignore malformed payloads so the client-side save queue can continue.
        }

        if (dirty)
            DatabaseHelper.SaveDatabaseType(player.Data);

        await CallGSRouter.SendScript(connection, "DreamCard_UpdateData", "{}", sync);
    }

    private static string NormalizeJson(JsonElement data)
    {
        return data.ValueKind == JsonValueKind.Undefined
            ? "null"
            : data.GetRawText();
    }
}

internal sealed class DreamCardUpdateDataEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}
