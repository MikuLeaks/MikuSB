using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("item/templates/support_card.json")]
public class SupportCardExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public uint Icon { get; set; }
    public uint ProvideExp { get; set; }
    public uint Color { get; set; }
    [JsonProperty("LevelLimitID")] public int LevelLimitId { get; set; }
    [JsonProperty("AffixPool")] public List<int> AffixPool { get; set; } = [];
    [JsonProperty("AffixCost")] public JToken? AffixCostRaw { get; set; }
    [JsonProperty("InitialAffixCost")] public JToken? InitialAffixCostRaw { get; set; }
    [JsonProperty("FixedAffixCost")] public JToken? FixedAffixCostRaw { get; set; }

    public uint MaxLevel => LevelLimitId switch
    {
        1007 => 10,
        1008 => 13,
        1009 => 16,
        _ => 10
    };

    public int InitialAffixCount => Color >= 5 ? 2 : 1;

    public int TotalAffixCount => Color >= 5 ? 3 : 2;

    [JsonIgnore]
    public IReadOnlyList<uint> AffixCost => ParseFlatCost(AffixCostRaw);

    [JsonIgnore]
    public IReadOnlyList<IReadOnlyList<uint>> InitialAffixCost => ParseNestedCost(InitialAffixCostRaw);

    [JsonIgnore]
    public IReadOnlyList<uint> FixedAffixCost => ParseFlatCost(FixedAffixCostRaw);
    public ulong TemplateId => GameResourceTemplateId.FromGdpl(Genre, Detail, Particular, Level);

    public override uint GetId() => Icon;

    public override void Loaded()
    {
        GameData.SupportCardData.Add(this);
    }

    private static IReadOnlyList<uint> ParseFlatCost(JToken? token)
    {
        if (token is not JArray array)
            return [];

        return array.Select(x => x.Value<uint>()).ToArray();
    }

    private static IReadOnlyList<IReadOnlyList<uint>> ParseNestedCost(JToken? token)
    {
        if (token is not JArray outer)
            return [];

        var result = new List<IReadOnlyList<uint>>();
        foreach (var entry in outer.OfType<JArray>())
            result.Add(entry.Select(x => x.Value<uint>()).ToArray());
        return result;
    }
}
