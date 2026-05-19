using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_GetReward")]
public class BossPvpLogic_GetReward : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var response = BossPvpService.HandleGetReward(param);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_GetReward", System.Text.Json.JsonSerializer.Serialize(response));
    }
}
