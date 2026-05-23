using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/climbtower/level.json")]
public class TowerLevelExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("MapID")] public uint MapID { get; set; }
    [JsonProperty("FightID")] public uint FightID { get; set; }
    [JsonProperty("TaskPath")] public string TaskPath { get; set; } = "";
    [JsonProperty("ConsumeVigor")] public List<int> ConsumeVigor { get; set; } = [];

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.TowerLevelData[ID] = this;
    }
}
