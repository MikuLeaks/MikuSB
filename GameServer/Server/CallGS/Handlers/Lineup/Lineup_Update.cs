using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Lineup;

[CallGSApi("Lineup_Update")]
public class Lineup_Update : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<LineupUpdateParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "UpdateLineup", "{}");
            return;
        }

        var formation = await connection.Player!.LineupManager.UpdateLineup(req.Index,req.Member1,req.Member2,req.Member3);
        if (formation == null)
        {
            await CallGSRouter.SendScript(connection, "UpdateLineup", "{}");
            return;
        }

        var rsp = new NtfSyncLineup 
        { 
            Lineup = formation.ToProto()
        };
        await connection.SendPacket(CmdIds.NtfSyncLineup, rsp);
        await CallGSRouter.SendScript(connection, "UpdateLineup", "{}");
    }
}

internal sealed class LineupUpdateParam
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("member1")] public uint Member1 { get; set; }
    [JsonPropertyName("member2")] public uint Member2 { get; set; }
    [JsonPropertyName("member3")] public uint Member3 { get; set; }
}