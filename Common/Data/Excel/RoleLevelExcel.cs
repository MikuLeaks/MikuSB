namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/role/level.json")]
public class RoleLevelExcel : ExcelResource
{
    public uint ID { get; set; }

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.RoleLevelData[ID] = this;
    }
}
