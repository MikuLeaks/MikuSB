using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/fishing/food.json")]
public class FishingFoodExcel : ExcelResource
{
    [JsonProperty("ID")] public uint Id { get; set; }
    [JsonProperty("FoodType")] public JToken? FoodTypeRaw { get; set; }
    [JsonProperty("NeedItem")] public JToken? NeedItemRaw { get; set; }
    [JsonProperty("CreateItems")] public JToken? CreateItemsRaw { get; set; }
    [JsonProperty("EffectTime")] public JToken? EffectTimeRaw { get; set; }
    [JsonProperty("FishingLevel")] public JToken? FishingLevelRaw { get; set; }
    [JsonProperty("SeasonId")] public JToken? SeasonIdRaw { get; set; }
    [JsonProperty("BaitNum")] public JToken? BaitNumRaw { get; set; }
    [JsonProperty("FoodArea")] public JToken? FoodAreaRaw { get; set; }

    [JsonIgnore] public uint FoodType => ReadUInt(FoodTypeRaw);
    [JsonIgnore] public uint EffectTime => ReadUInt(EffectTimeRaw);
    [JsonIgnore] public uint FishingLevel => ReadUInt(FishingLevelRaw);
    [JsonIgnore] public uint SeasonId => ReadUInt(SeasonIdRaw);
    [JsonIgnore] public List<List<uint>> NeedItem => ReadNestedUIntList(NeedItemRaw);
    [JsonIgnore] public List<uint> CreateItems => ReadUIntList(CreateItemsRaw);
    [JsonIgnore] public List<uint> BaitNum => ReadUIntList(BaitNumRaw);
    [JsonIgnore] public List<uint> FoodArea => ReadUIntList(FoodAreaRaw);

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.FishingFoodData[Id] = this;
    }

    private static int ReadInt(JToken? token)
    {
        if (token == null)
            return 0;

        return token.Type switch
        {
            JTokenType.Integer => token.Value<int>(),
            JTokenType.Float => (int)token.Value<decimal>(),
            JTokenType.String when int.TryParse(token.Value<string>(), out var value) => value,
            _ => 0
        };
    }

    private static uint ReadUInt(JToken? token)
    {
        var value = ReadInt(token);
        return value > 0 ? (uint)value : 0;
    }

    private static List<uint> ReadUIntList(JToken? token)
    {
        if (token is not JArray array)
            return [];

        var result = new List<uint>(array.Count);
        foreach (var item in array)
        {
            var value = ReadUInt(item);
            if (value > 0)
                result.Add(value);
        }

        return result;
    }

    private static List<List<uint>> ReadNestedUIntList(JToken? token)
    {
        if (token is not JArray array)
            return [];

        var result = new List<List<uint>>(array.Count);
        foreach (var row in array.OfType<JArray>())
        {
            var values = new List<uint>(row.Count);
            foreach (var item in row)
            {
                values.Add(ReadUInt(item));
            }
            result.Add(values);
        }

        return result;
    }
}
