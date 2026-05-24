using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/captureregion.json")]
public class VirCaptureCaptureRegionExcel : ExcelResource
{
    [JsonProperty("Id")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("MapId")] public uint MapId { get; set; }
    [JsonProperty("LevelRegionName")] public string LevelRegionName { get; set; } = "";

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.VirCaptureCaptureRegionData[Id] = this;
    }
}
