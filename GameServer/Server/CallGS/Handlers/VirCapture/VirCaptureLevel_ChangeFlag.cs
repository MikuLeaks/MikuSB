using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_ChangeFlag")]
public class VirCaptureLevel_ChangeFlag : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureChangeFlagParam>(param);
        if (req == null || req.LevelId == 0 || req.RegionId == 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_ChangeFlag", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        VirCaptureStateHelper.SetPointState(player, (uint)req.LevelId, (uint)req.RegionId, req.Clean ? 0u : 1u, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);
        var rsp = $"{{\"nLevelID\":{req.LevelId},\"nRegionId\":{req.RegionId},\"bClean\":{req.Clean.ToString().ToLowerInvariant()}}}";
        await CallGSRouter.SendScript(connection, "VirCaptureLevel_ChangeFlag", rsp, sync);
    }
}

internal sealed class VirCaptureChangeFlagParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nRegionId")]
    public int RegionId { get; set; }

    [JsonPropertyName("bClean")]
    public bool Clean { get; set; }
}
