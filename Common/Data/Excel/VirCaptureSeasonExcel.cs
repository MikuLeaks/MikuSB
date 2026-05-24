using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/season.json")]
public class VirCaptureSeasonExcel : ExcelResource
{
    [JsonProperty("Id")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.VirCaptureSeasonData[Id] = this;
    }
}
