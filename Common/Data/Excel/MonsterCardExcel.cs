using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("item/templates/monster_card.json")]
public class MonsterCardExcel : ExcelResource
{
    [JsonProperty("Genre")] public uint Genre { get; set; }
    [JsonProperty("Detail")] public uint Detail { get; set; }
    [JsonProperty("Particular")] public uint Particular { get; set; }
    [JsonProperty("Level")] public uint Level { get; set; }
    [JsonProperty("Color")] public uint Color { get; set; }
    [JsonProperty("RikiId")] public uint RikiId { get; set; }
    [JsonProperty("CostValue")] public uint CostValue { get; set; }
    [JsonProperty("Exp")] public uint Exp { get; set; }

    [JsonIgnore]
    public ulong TemplateId => GameResourceTemplateId.FromGdpl(Genre, Detail, Particular, Level);

    public override uint GetId() => Particular;

    public override void Loaded()
    {
        GameData.MonsterCardData[TemplateId] = this;
    }
}
