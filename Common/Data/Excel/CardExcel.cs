using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("card.json")]
public class CardExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public uint Icon { get; set; }
    public uint InitBreak { get; set; }
    public int BreakMatID { get; set; }
    public int LevelLimitID { get; set; }
    public int GrowupID { get; set; }
    public int AppearID { get; set; }
    public List<uint> DefaultWeaponGPDL { get; set; } = [];
    [JsonProperty("profile")] public List<List<int>> Profile { get; set; } = [];
    public List<List<int>> Pieces { get; set; } = [];
    public List<int> Attribute { get; set; } = [];
    [JsonProperty("SpineID")] public uint SpineId { get; set; }
    public override uint GetId()
    {
        return Icon;
    }

    public override void Loaded()
    {
        GameData.CardData.Add(Icon, this);
    }
}