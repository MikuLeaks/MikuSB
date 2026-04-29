using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("break.json")]
public class BreakExcel : ExcelResource
{
    [JsonProperty("ID")] public int Id { get; set; }

    [JsonProperty("Items1")] public List<List<int>> Items1 { get; set; } = [];
    [JsonProperty("Items2")] public List<List<int>> Items2 { get; set; } = [];
    [JsonProperty("Items3")] public List<List<int>> Items3 { get; set; } = [];
    [JsonProperty("Items4")] public List<List<int>> Items4 { get; set; } = [];
    [JsonProperty("Items5")] public List<List<int>> Items5 { get; set; } = [];
    [JsonProperty("Items6")] public List<List<int>> Items6 { get; set; } = [];

    public List<List<int>> GetItems(uint breakLevel) => breakLevel switch
    {
        1 => Items1,
        2 => Items2,
        3 => Items3,
        4 => Items4,
        5 => Items5,
        6 => Items6,
        _ => []
    };

    public override uint GetId() => (uint)Id;

    public override void Loaded()
    {
        GameData.BreakData[Id] = this;
    }
}
