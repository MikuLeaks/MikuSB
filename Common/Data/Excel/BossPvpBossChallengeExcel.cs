using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/bosspvp/boss_challenge.json")]
public class BossPvpBossChallengeExcel : ExcelResource
{
    public uint ID { get; set; }
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public List<uint> tbTaskID { get; set; } = [];

    [JsonExtensionData] public IDictionary<string, JToken> ExtraData { get; set; } = new Dictionary<string, JToken>();

    [JsonIgnore] public List<uint> BossIds { get; private set; } = [];

    public override uint GetId() => ID;

    public override void Loaded()
    {
        BossIds = ExtraData
            .Where(x => x.Key.StartsWith("Boss", StringComparison.Ordinal) && int.TryParse(x.Key[4..], out _))
            .OrderBy(x => int.Parse(x.Key[4..], CultureInfo.InvariantCulture))
            .Select(x => x.Value.Type == JTokenType.Integer ? x.Value.Value<uint>() : 0u)
            .Where(x => x > 0)
            .ToList();

        GameData.BossPvpBossChallengeData[ID] = this;
    }
}
