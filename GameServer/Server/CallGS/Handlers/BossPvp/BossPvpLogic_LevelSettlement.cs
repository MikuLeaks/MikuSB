using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_LevelSettlement")]
public class BossPvpLogic_LevelSettlement : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(param);
        var (response, sync) = BossPvpService.HandleSettlement(connection.Player!, node);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_LevelSettlement", response.ToJsonString(), sync);
    }
}
