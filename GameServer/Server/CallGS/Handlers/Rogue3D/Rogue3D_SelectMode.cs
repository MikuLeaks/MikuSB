namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Selects the Rogue3D game mode (nModeID: 1=infinity, 2=normal, 3=season).
// param: {"nModeID": int}
// Response: {} on success, {"sErr": "key"} on failure
[CallGSApi("Rogue3D_SelectMode")]
public class Rogue3D_SelectMode : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var sync = Rogue3DStateHelper.EnsureUnlockState(connection.Player!);
        await CallGSRouter.SendScript(connection, "Rogue3D_SelectMode", "{}", sync);
    }
}
