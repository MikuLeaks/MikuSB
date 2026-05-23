namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("ExtendFightDynamicLog")]
public class ExtendFightDynamicLog : ICallGSHandler
{
    public Task Handle(Connection connection, string param, ushort seqNo)
    {
        return Task.CompletedTask;
    }
}
