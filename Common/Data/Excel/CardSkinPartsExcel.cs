using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("card_skin_parts.json")]
public class CardSkinPartsExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public uint Icon { get; set; }
    public int AppearID { get; set; }
    [JsonProperty("profile")] public List<List<int>> CardSkinID { get; set; } = [];
    public string I18n { get; set; } = "";
    [JsonIgnore] public ulong TemplateId { get; set; }
    public override uint GetId()
    {
        return (uint)I18n.GetHashCode();
    }

    public override void Loaded()
    {
        TemplateId = GameResourceTemplateId.FromGdpl(Genre, Detail, Particular, Level);
        GameData.CardSkinPartsData.Add(Icon, this);
    }
}