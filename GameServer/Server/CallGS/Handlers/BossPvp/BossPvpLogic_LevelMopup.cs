using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_LevelMopup")]
public class BossPvpLogic_LevelMopup : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = BossPvpService.HandleMopup(connection.Player!, param);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_LevelMopup", System.Text.Json.JsonSerializer.Serialize(response), sync);
    }
}
