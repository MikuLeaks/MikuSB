using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/DreamCard/activity.json")]
public class DreamCardActivityExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("Condition")] public string Condition { get; set; } = "";
    [JsonProperty("LevelListID")] public List<uint> LevelListID { get; set; } = [];

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.DreamCardActivityData[ID] = this;
    }
}
