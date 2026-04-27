namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Called when a room is cleared. Client s2c handler is empty — just acknowledge.
// param: {"nNodeId": int, "tbKill": [...], "tbMonster": [...]}
[CallGSApi("Rogue3D_RoomSettlement")]
public class Rogue3D_RoomSettlement : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        await CallGSRouter.SendScript(connection, "Rogue3D_RoomSettlement", "{}");
    }
}
