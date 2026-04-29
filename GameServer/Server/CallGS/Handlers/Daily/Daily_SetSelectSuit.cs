using System.Text.Json;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Daily;

[CallGSApi("Daily_SetSelectSuit")]
public class Daily_SetSelectSuit : ICallGSHandler
{

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<GirlWeaponSkinParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "Daily_SetSelectSuit", "{}");
            return;
        }
        var rsp = $"{{\"SuitId\":{req.Suit}}}";
        await CallGSRouter.SendScript(connection, "Daily_SetSelectSuit", rsp);
    }
}

internal sealed class GirlWeaponSkinParam
{
    public uint Type { get; set; }
    public uint Suit { get; set; }
}
