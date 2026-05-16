using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.BattlePass;

[CallGSApi("BattlePassLogic_ClientRefresh")]
public class BattlePassLogic_ClientRefresh : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = string.IsNullOrEmpty(param) 
            ? new BattlePassLogicClientRefreshParam { ClickNum = 0 }
            : JsonSerializer.Deserialize<BattlePassLogicClientRefreshParam>(param);
        var clickNum = req?.ClickNum ?? 0;
        var battlePassList = LoadBattlePassGoods();
        var playerLevel = connection.Player?.Data.Level ?? 0;

        var response = JsonSerializer.Serialize(new
        {
            clicknum = clickNum,
            nBattlePassLevel = playerLevel,
            nBattlePassExp = 0,
            tbBattlePassList = battlePassList,
            BattlePassList = battlePassList
        });

        await CallGSRouter.SendScript(connection, "BattlePassLogic_ClientRefresh", response);
    }

    private static JsonElement[] LoadBattlePassGoods()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "purchase", "ibgoods.json");
        if (!File.Exists(path))
            return [];

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .EnumerateArray()
            .Where(row => IsBattlePassGoods(row))
            .Select(row => row.Clone())
            .ToArray();
    }

    private static bool IsBattlePassGoods(JsonElement row)
    {
        if (row.TryGetProperty("GoodsTag", out var goodsTag) && GetStringValue(goodsTag)?.Contains("battlepass", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (row.TryGetProperty("IosId", out var iosId) && GetStringValue(iosId)?.Contains("battlepass", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (row.TryGetProperty("AndroidId", out var androidId) && GetStringValue(androidId)?.Contains("battlepass", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
    }
}

internal sealed class BattlePassLogicClientRefreshParam
{
    [JsonPropertyName("clicknum")]
    public int ClickNum { get; set; }
}
