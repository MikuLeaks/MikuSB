using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

[CallGSApi("Gacha_Launch")]
public class Gacha_Launch : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var gachaList = LoadGachaList();
        
        var response = JsonSerializer.Serialize(new
        {
            tbGachaList = gachaList,
            GachaList = gachaList
        });

        await CallGSRouter.SendScript(connection, "Gacha_Launch", response);
    }

    private static JsonElement[] LoadGachaList()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "gacha", "gacha.json");
        if (!File.Exists(path))
            return [];

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .EnumerateArray()
            .Select(row => row.Clone())
            .ToArray();
    }
}
