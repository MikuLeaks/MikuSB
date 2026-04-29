namespace MikuSB.Data.Excel;

[ResourceEntity("profile.json")]
public class ProfileExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public string I18n { get; set; } = "";
    public string LuaType { get; set; } = "";

    public override uint GetId()
    {
        return (uint)I18n.GetHashCode();
    }

    public override void Loaded()
    {
        GameData.ProfileData.Add(GetId(), this);
    }
}
