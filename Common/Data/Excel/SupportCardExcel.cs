namespace MikuSB.Data.Excel;

[ResourceEntity("support_card.json")]
public class SupportCardExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public uint Icon { get; set; }

    public override uint GetId() => Icon;

    public override void Loaded()
    {
        GameData.SupportCardData.Add(this);
    }
}
