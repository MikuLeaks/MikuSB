using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Guide;

[CallGSApi("GuideLogic_WriteGuideLog")]
public class GuideLogic_WriteGuideLog : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        // Client writes guide progress log. Return empty success JSON to client.
        // param: {nGuideId, ...}
        await CallGSRouter.SendScript(connection, "GuideLogic_WriteGuideLog", "{}");
    }
}
