namespace MikuSB.Data.Excel;

[ResourceEntity("challenge/bosspvp/boss.json")]
public class BossPvpBossExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint LevelID { get; set; }
    public uint BossID { get; set; }
    public List<List<int>> BossLevel { get; set; } = [];

    public override uint GetId() => ID;

    public override void Loaded()
    {
        GameData.BossPvpBossData[ID] = this;
    }
}
