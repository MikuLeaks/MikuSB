using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Shop;

[CallGSApi("ShopLogic_GetGoodsList")]
public class ShopLogic_GetGoodsList : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = string.IsNullOrEmpty(param)
            ? new ShopLogicGetGoodsListParam { ShopId = 0 }
            : JsonSerializer.Deserialize<ShopLogicGetGoodsListParam>(param);
        var shopId = req?.ShopId ?? 0;
        var goodsList = LoadShopGoods(shopId);

        var response = JsonSerializer.Serialize(new
        {
            nShopId = shopId,
            tbGoodsList = goodsList,
            GoodsList = goodsList
        });

        await CallGSRouter.SendScript(connection, "ShopLogic_GetGoodsList", response);
    }

    private static JsonElement[] LoadShopGoods(int shopId)
    {
        if (shopId <= 0)
            return [];

        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "shop", "goods.json");
        if (!File.Exists(path))
            return [];

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .EnumerateArray()
            .Where(row => row.TryGetProperty("ShopId", out var shopIdProp) && GetIntValue(shopIdProp) == shopId)
            .Select(row => row.Clone())
            .ToArray();
    }

    private static int GetIntValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String => int.TryParse(element.GetString(), out var value) ? value : 0,
            _ => 0
        };
    }
}

internal sealed class ShopLogicGetGoodsListParam
{
    [JsonPropertyName("nShopId")]
    public int ShopId { get; set; }
}
