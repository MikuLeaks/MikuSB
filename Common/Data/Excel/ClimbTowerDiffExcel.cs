using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/climbtower/climb_tower_diff.json")]
public class ClimbTowerDiffExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("Level1")] public int Level1 { get; set; }
    [JsonProperty("Level2")] public int Level2 { get; set; }

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.ClimbTowerDiffData[ID] = this;
    }
}
