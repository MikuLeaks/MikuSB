namespace MikuSB.GameServer.Server.CallGS.Handlers.Chapter;

// Success response shape expected by Lua:
// { nSeed = random_number }
[CallGSApi("Chapter_EnterLevel")]
public class Chapter_EnterLevel : ICallGSHandler
{
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var rsp = $"{{\"nSeed\":{Random.Next(1, 1000000000)}}}";
        await CallGSRouter.SendScript(connection, "Chapter_EnterLevel", rsp);
    }
}
