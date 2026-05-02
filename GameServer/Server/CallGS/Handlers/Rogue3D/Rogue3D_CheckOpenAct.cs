namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Returns which Rogue3D acts (server_04_timelist) are currently open.
// param: [] (empty)
// Response: {"listActId":[...]}
[CallGSApi("Rogue3D_CheckOpenAct")]
public class Rogue3D_CheckOpenAct : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var sync = Rogue3DStateHelper.EnsureUnlockState(connection.Player!);
        await CallGSRouter.SendScript(connection, "Rogue3D_CheckOpenAct", "{\"bOpen\":true}", sync);
    }
}
