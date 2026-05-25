using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("dlc/vircapture/levellist.json")]
public class VirCaptureLevelListExcel : ExcelResource
{
    [JsonProperty("Level")] public uint Level { get; set; }
    [JsonProperty("Exp")] public uint Exp { get; set; }
    [JsonProperty("Num")] public uint Num { get; set; }
    [JsonProperty("MaxCost")] public uint MaxCost { get; set; }
    [JsonProperty("Rewards")] public List<List<uint>> Rewards { get; set; } = [];
    [JsonProperty("ExpUp")] public double ExpUp { get; set; }

    public override uint GetId() => Level;

    public override void Loaded()
    {
        GameData.VirCaptureLevelListData[Level] = this;
    }
}
