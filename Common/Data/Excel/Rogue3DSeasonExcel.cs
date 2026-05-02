using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("server_10_season.json")]
public class Rogue3DSeasonExcel : ExcelResource
{
    [JsonProperty("SeasonID")] public uint SeasonId { get; set; }
    [JsonProperty("Type")] public int Type { get; set; }
    [JsonProperty("OpenTime")] public string OpenTime { get; set; } = "";
    [JsonProperty("CloseTime")] public string CloseTime { get; set; } = "";

    public override uint GetId() => SeasonId;

    public override void Loaded()
    {
        GameData.Rogue3DSeasonData[SeasonId] = this;
    }
}
