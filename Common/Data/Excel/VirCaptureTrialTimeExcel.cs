using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/trial_timelist.json")]
public class VirCaptureTrialTimeExcel : ExcelResource
{
    [JsonProperty("Id")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("AwardTime")] public string AwardTime { get; set; } = "";

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.VirCaptureTrialTimeData[Id] = this;
    }
}
