using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("weapon_parts.json")]
public class WeaponPartsExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public uint Icon { get; set; }
    public int AppearID { get; set; }
    public string I18n { get; set; } = "";
    public override uint GetId()
    {
        return (uint)I18n.GetHashCode();
    }

    public override void Loaded()
    {
        GameData.WeaponPartsData.Add(GetId(), this);
    }
}