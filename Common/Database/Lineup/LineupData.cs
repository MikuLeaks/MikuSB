using SqlSugar;

namespace MikuSB.Database.Lineup;

[SugarTable("lineup_data")]
public class LineupData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public Dictionary<int, LineupDataInfo> LineupInfo { get; set; } = [];
}

public class LineupDataInfo
{
    public uint Index { get; set; }
    public string Name { get; set; } = "";
    public uint Member1 { get; set; }
    public uint Member2 { get; set; }
    public uint Member3 { get; set; }

    public Proto.Lineup ToProto()
    {
        var proto = new Proto.Lineup
        {
            Index = Index,
            Name = Name,
            Member1 = Member1,
            Member2 = Member2,
            Member3 = Member3,
        };
        return proto;
    }
}