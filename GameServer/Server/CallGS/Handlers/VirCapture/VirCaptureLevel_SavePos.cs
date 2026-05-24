using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_SavePos")]
public class VirCaptureLevel_SavePos : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureSavePosParam>(param);
        if (req == null || req.LevelId == 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SavePos", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        VirCaptureStateHelper.SetSignedMapOffset(player, (uint)req.LevelId, VirCaptureStateHelper.OffPosX, req.PosX, sync);
        VirCaptureStateHelper.SetSignedMapOffset(player, (uint)req.LevelId, VirCaptureStateHelper.OffPosY, req.PosY, sync);
        VirCaptureStateHelper.SetSignedMapOffset(player, (uint)req.LevelId, VirCaptureStateHelper.OffPosZ, req.PosZ, sync);
        VirCaptureStateHelper.SetSignedMapOffset(player, (uint)req.LevelId, VirCaptureStateHelper.OffToward, req.Toward, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);
        await CallGSRouter.SendScript(connection, "VirCaptureLevel_SavePos", "{}", sync);
    }
}

internal sealed class VirCaptureSavePosParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nPosX")]
    public int PosX { get; set; }

    [JsonPropertyName("nPosY")]
    public int PosY { get; set; }

    [JsonPropertyName("nPosZ")]
    public int PosZ { get; set; }

    [JsonPropertyName("nToward")]
    public int Toward { get; set; }
}
