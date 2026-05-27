using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("purchase/ibgoods.json")]
public class IbGoodsExcel : ExcelResource
{
    [JsonProperty("GoodsId")] private JToken? GoodsIdRaw { get; set; }
    [JsonProperty("Type")] private JToken? TypeRaw { get; set; }
    [JsonProperty("PreId")] private JToken? PreIdRaw { get; set; }
    [JsonProperty("LimitTimes")] private JToken? LimitTimesRaw { get; set; }
    [JsonProperty("Item")] private JToken? ItemRaw { get; set; }
    [JsonProperty("Cost")] private JToken? CostRaw { get; set; }
    [JsonProperty("Cost2")] private JToken? Cost2Raw { get; set; }
    [JsonProperty("PcId")] public string PcId { get; set; } = "";
    [JsonProperty("IosId")] public string IosId { get; set; } = "";
    [JsonProperty("AndroidId")] public string AndroidId { get; set; } = "";

    public override uint GetId() => GoodsId;

    public override void Loaded()
    {
        GameData.IbGoodsData[GoodsId] = this;
    }

    [JsonIgnore]
    public uint GoodsId => ReadUInt(GoodsIdRaw);

    [JsonIgnore]
    public int Type => (int)ReadUInt(TypeRaw);

    [JsonIgnore]
    public uint PreId => ReadUInt(PreIdRaw);

    [JsonIgnore]
    public uint LimitTimes => ReadUInt(LimitTimesRaw);

    [JsonIgnore]
    public List<uint> Item => ReadUIntList(ItemRaw);

    [JsonIgnore]
    public List<uint> Cost => ReadUIntList(CostRaw);

    [JsonIgnore]
    public List<uint> Cost2 => ReadUIntList(Cost2Raw);

    public string GetProductId() =>
        !string.IsNullOrWhiteSpace(PcId) ? PcId :
        !string.IsNullOrWhiteSpace(AndroidId) ? AndroidId :
        IosId;

    private static uint ReadUInt(JToken? token)
    {
        if (token == null || token.Type is JTokenType.Null or JTokenType.Undefined)
            return 0;

        if (token.Type == JTokenType.Integer)
            return token.Value<uint>();

        if (token.Type == JTokenType.String && uint.TryParse(token.Value<string>(), out var value))
            return value;

        return 0;
    }

    private static List<uint> ReadUIntList(JToken? token)
    {
        if (token is not JArray array)
            return [];

        return array
            .Select(entry => entry.Type == JTokenType.Integer ? entry.Value<uint>() : 0u)
            .ToList();
    }
}
