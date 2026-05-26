using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("battlepass/timelist.json")]
public class BattlePassTimeExcel : ExcelResource
{
    [JsonProperty("ID")] public uint Id { get; set; }
    [JsonProperty("StartTime")] public string StartTime { get; set; } = "";
    [JsonProperty("EndTime")] public string EndTime { get; set; } = "";
    [JsonProperty("BuyStartTime")] public string BuyStartTime { get; set; } = "";
    [JsonProperty("BuyEndTime")] public string BuyEndTime { get; set; } = "";
    [JsonProperty("Condition")] public string Condition { get; set; } = "";
    [JsonProperty("ExpStep")] public uint ExpStep { get; set; }
    [JsonProperty("MaxExPerWeek")] public uint MaxExPerWeek { get; set; }

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.BattlePassTimeData[Id] = this;
    }
}
