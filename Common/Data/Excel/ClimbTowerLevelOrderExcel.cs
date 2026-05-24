using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/climbtower/climb_tower_levelorder.json")]
public class ClimbTowerLevelOrderExcel : ExcelResource
{
    [JsonProperty("ID")] public uint ID { get; set; }
    [JsonProperty("LevelID")] public uint LevelID { get; set; }

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.ClimbTowerLevelOrderData[ID] = this;
    }
}
