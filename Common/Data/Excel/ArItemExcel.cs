namespace MikuSB.Data.Excel;

[ResourceEntity("ar_item.json")]
public class ArItemExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public string I18n { get; set; } = "";

    public override uint GetId()
    {
        return (Genre << 24) | (Detail << 16) | (Particular << 8) | Level;
    }

    public override void Loaded()
    {
        GameData.ArItemData.Add(GetId(), this);
    }
}
