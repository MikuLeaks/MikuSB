using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/tower.json")]
public class VirCaptureTowerExcel : ExcelResource
{
    [JsonProperty("ID")] public uint Id { get; set; }
    [JsonProperty("Condition")] public JToken? ConditionRaw { get; set; }
    [JsonProperty("MapID")] public uint MapId { get; set; }
    [JsonProperty("TrialCard")] public List<uint> TrialCard { get; set; } = [];
    [JsonProperty("TaskPath")] public string TaskPath { get; set; } = "";

    [JsonIgnore]
    public Dictionary<int, uint> Condition { get; } = [];

    public override uint GetId() => Id;

    public override void Loaded()
    {
        Condition.Clear();
        if (ConditionRaw is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                if (!int.TryParse(property.Name, out var key))
                    continue;

                uint value = 0;
                if (property.Value.Type == JTokenType.Integer)
                    value = property.Value.Value<uint>();
                else if (property.Value.Type == JTokenType.String &&
                         uint.TryParse(property.Value.Value<string>(), out var parsed))
                    value = parsed;

                if (value > 0)
                    Condition[key] = value;
            }
        }

        GameData.VirCaptureTowerData[Id] = this;
    }
}
