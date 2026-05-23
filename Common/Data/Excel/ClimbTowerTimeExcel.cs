using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/climbtower/climb_tower_time.json")]
public class ClimbTowerTimeExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("Level1")] public List<List<uint>> Level1 { get; set; } = [];
    [JsonProperty("Level2")] public JToken? Level2Raw { get; set; }

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.ClimbTowerTimeData[ID] = this;
    }

    public IReadOnlyList<IReadOnlyList<uint>> GetLevelGroups(int type)
    {
        if (type == 1)
            return Level1;

        if (Level2Raw == null)
            return [];

        if (Level2Raw.Type == JTokenType.Array)
        {
            return Level2Raw
                .Children()
                .OfType<JArray>()
                .Select(x => (IReadOnlyList<uint>)x.Values<uint>().ToList())
                .ToList();
        }

        if (Level2Raw.Type == JTokenType.Object)
        {
            return Level2Raw
                .Children<JProperty>()
                .Select(x => new
                {
                    Key = uint.TryParse(x.Name, CultureInfo.InvariantCulture, out var key) ? key : 0u,
                    Value = x.Value.Type == JTokenType.Integer ? x.Value.Value<uint>() : 0u
                })
                .Where(x => x.Key > 0 && x.Value > 0)
                .OrderBy(x => x.Key)
                .Select(x => (IReadOnlyList<uint>)new List<uint> { x.Key, x.Value })
                .ToList();
        }

        return [];
    }
}
