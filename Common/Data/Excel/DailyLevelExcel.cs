namespace MikuSB.Data.Excel;

[ResourceEntity("daily_level.json")]
public class DailyLevelExcel : ExcelResource
{
    public uint ID { get; set; }

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.DailyLevelData.Add(ID, this);
    }
}
