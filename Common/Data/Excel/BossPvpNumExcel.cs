namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/bosspvp/num.json")]
public class BossPvpNumExcel : ExcelResource
{
    public uint Week { get; set; }
    public uint Num { get; set; }

    public override uint GetId() => Week;

    public override void Loaded()
    {
        GameData.BossPvpNumData[Week] = this;
    }
}
