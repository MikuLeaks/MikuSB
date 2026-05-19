using MikuSB.GameServer.Game.BossPvp;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BossPvp;

[CallGSApi("BossPvpLogic_GetOpenID")]
public class BossPvpLogic_GetOpenID : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = await BossPvpService.HandleGetOpenIdAsync(connection.Player!);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_GetOpenID", System.Text.Json.JsonSerializer.Serialize(response), sync);
    }
}
