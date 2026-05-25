using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/timelist.json")]
public class VirCaptureTimeExcel : ExcelResource
{
    [JsonProperty("Id")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("CaptureRegionId")] public List<uint> CaptureRegionId { get; set; } = [];
    [JsonProperty("MaxExp")] public uint MaxExp { get; set; }

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.VirCaptureTimeData[Id] = this;
    }
}
