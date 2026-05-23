using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("item/cardbreak/breaknew.json")]
public class SpecialBreakExcel : ExcelResource
{
    [JsonProperty("ID")] public int Id { get; set; }

    [JsonProperty("1Items1")] public List<List<int>> Items1 { get; set; } = [];
    [JsonProperty("2Items1")] public List<List<int>> Items2 { get; set; } = [];
    [JsonProperty("3Items1")] public List<List<int>> Items3 { get; set; } = [];
    [JsonProperty("4Items1")] public List<List<int>> Items4 { get; set; } = [];

    public List<List<int>> GetItems(uint breakLevel) => breakLevel switch
    {
        1 => Items1,
        2 => Items2,
        3 => Items3,
        4 => Items4,
        _ => []
    };

    public bool HasBreakLevel(uint breakLevel) => breakLevel switch
    {
        1 => Items1.Count > 0,
        2 => Items2.Count > 0,
        3 => Items3.Count > 0,
        4 => Items4.Count > 0,
        _ => false
    };

    public override uint GetId() => (uint)Id;

    public override void Loaded()
    {
        GameData.SpecialBreakData[Id] = this;
    }
}
