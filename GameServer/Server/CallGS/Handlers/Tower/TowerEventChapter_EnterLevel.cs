using MikuSB.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("TowerEventChapter_EnterLevel")]
public class TowerEventChapter_EnterLevel : ICallGSHandler
{
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<TowerEventEnterLevelParam>(param);
        if (req == null || req.LevelId == 0 || req.TeamId <= 0)
        {
            await CallGSRouter.SendScript(connection, "TowerEventChapter_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.TowerEventLevelData.ContainsKey((uint)req.LevelId))
        {
            await CallGSRouter.SendScript(connection, "TowerEventChapter_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var rsp = $"{{\"nSeed\":{Random.Next(1, 1_000_000_000)}}}";
        await CallGSRouter.SendScript(connection, "TowerEventChapter_EnterLevel", rsp);
    }
}

internal sealed class TowerEventEnterLevelParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nTeamID")]
    public int TeamId { get; set; }
}
