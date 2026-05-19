using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_Record")]
public class BossPvpLogic_Record : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = BossPvpService.HandleRecord(connection.Player!, param);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_Record", System.Text.Json.JsonSerializer.Serialize(response), sync);
    }
}
