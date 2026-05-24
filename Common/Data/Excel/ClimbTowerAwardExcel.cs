using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/climbtower/climb_tower_award.json")]
public class ClimbTowerAwardExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("Diff")] public JToken? DiffRaw { get; set; }
    [JsonProperty("FirstAward")] public List<List<uint>> FirstAward { get; set; } = [];
    [JsonProperty("StarCount1")] public int StarCount1 { get; set; }
    [JsonProperty("StarAward1")] public List<List<uint>> StarAward1 { get; set; } = [];
    [JsonProperty("StarCount2")] public int StarCount2 { get; set; }
    [JsonProperty("StarAward2")] public List<List<uint>> StarAward2 { get; set; } = [];
    [JsonProperty("StarCount3")] public int StarCount3 { get; set; }
    [JsonProperty("StarAward3")] public List<List<uint>> StarAward3 { get; set; } = [];

    [JsonIgnore]
    public int Diff => DiffRaw?.Type switch
    {
        JTokenType.Integer => Math.Max(1, DiffRaw.Value<int>()),
        JTokenType.String when int.TryParse(DiffRaw.Value<string>(), out var value) => Math.Max(1, value),
        _ => 1
    };

    public override uint GetId() => (ID * 10u) + (uint)Diff;

    public override void Loaded()
    {
        if (!GameData.ClimbTowerAwardData.TryGetValue(ID, out var diffMap))
        {
            diffMap = [];
            GameData.ClimbTowerAwardData[ID] = diffMap;
        }

        diffMap[Diff] = this;
    }

    public int GetStarCount(int group) => group switch
    {
        1 => StarCount1,
        2 => StarCount2,
        3 => StarCount3,
        _ => 0
    };

    public IReadOnlyList<IReadOnlyList<uint>> GetRewards(int group) => group switch
    {
        0 => FirstAward,
        1 => StarAward1,
        2 => StarAward2,
        3 => StarAward3,
        _ => []
    };
}
