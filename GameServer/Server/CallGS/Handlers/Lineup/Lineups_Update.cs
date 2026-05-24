using MikuSB.Database;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Lineup;

[CallGSApi("Lineups_Update")]
public class Lineups_Update : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<List<LineupUpdateBatchParam>>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "UpdateLineup", "{}");
            return;
        }

        foreach (var lineup in req)
        {
            if (lineup == null)
                continue;

            await connection.Player!.LineupManager.UpdateLineup(
                lineup.Index,
                lineup.Member1,
                lineup.Member2,
                lineup.Member3);
        }

        DatabaseHelper.SaveDatabaseType(connection.Player!.LineupManager.LineupData);
        await CallGSRouter.SendScript(connection, "UpdateLineup", "{}");
    }
}

internal sealed class LineupUpdateBatchParam
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("member1")] public uint Member1 { get; set; }
    [JsonPropertyName("member2")] public uint Member2 { get; set; }
    [JsonPropertyName("member3")] public uint Member3 { get; set; }
}
