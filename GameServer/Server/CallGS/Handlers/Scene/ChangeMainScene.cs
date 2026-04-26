namespace MikuSB.GameServer.Server.CallGS.Handlers.Scene;

// Response:{sErr:true or false}
[CallGSApi("ChangeMainScene")]
public class ChangeMainScene : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        string rsp = $"{{\"sErr\":false}}";
        // TODO change scene with ExtraSync
        await CallGSRouter.SendScript(connection, "ChangeMainScene", rsp);
    }
}