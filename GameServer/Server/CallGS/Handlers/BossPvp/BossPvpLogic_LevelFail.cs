using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_LevelFail")]
public class BossPvpLogic_LevelFail : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(param);
        var (response, sync) = BossPvpService.HandleFail(connection.Player!, node);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_LevelFail", response.ToJsonString(), sync);
    }
}
