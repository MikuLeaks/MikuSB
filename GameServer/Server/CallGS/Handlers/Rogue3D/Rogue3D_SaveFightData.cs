namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Saves in-progress fight data (checkpoint). Client s2c handler is empty — just acknowledge.
// param: {"tbData": {...}, "chaosWave": int}
[CallGSApi("Rogue3D_SaveFightData")]
public class Rogue3D_SaveFightData : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        await CallGSRouter.SendScript(connection, "Rogue3D_SaveFightData", "{}");
    }
}
