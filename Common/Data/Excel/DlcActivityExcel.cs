using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/dlc_activities.json")]
public class DlcActivityExcel : ExcelResource
{
    [JsonProperty("ID")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("EnterStartTime")] public string EnterStartTime { get; set; } = "";
    [JsonProperty("CloseEndTime")] public string CloseEndTime { get; set; } = "";
    [JsonProperty("Condition")] public string Condition { get; set; } = "";

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.DlcActivityData[Id] = this;
    }
}
