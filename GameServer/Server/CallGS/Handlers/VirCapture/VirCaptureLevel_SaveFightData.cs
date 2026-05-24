using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_SaveFightData")]
public class VirCaptureLevel_SaveFightData : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureSaveFightDataParam>(param);
        if (req == null || req.LevelId == 0 || req.RegionId == 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveFightData", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        VirCaptureStateHelper.SetPointState(player, (uint)req.LevelId, (uint)req.RegionId, 2u, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);

        var response = new JsonObject
        {
            ["nLevelID"] = req.LevelId,
            ["nRegionId"] = req.RegionId,
            ["tbRewards"] = new JsonArray()
        };

        await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveFightData", response.ToJsonString(), sync);
    }
}

internal sealed class VirCaptureSaveFightDataParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nRegionId")]
    public int RegionId { get; set; }
}
