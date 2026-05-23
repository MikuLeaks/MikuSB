namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("ExtendFightLog")]
public class ExtendFightLog : ICallGSHandler
{
    public Task Handle(Connection connection, string param, ushort seqNo)
    {
        return Task.CompletedTask;
    }
}
