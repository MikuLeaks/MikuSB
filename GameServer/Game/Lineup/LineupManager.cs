using MikuSB.Database;
using MikuSB.GameServer.Game.Player;
using MikuSB.Database.Lineup;

namespace MikuSB.GameServer.Game.Lineup;

public class LineupManager(PlayerInstance player) : BasePlayerManager(player)
{
    public LineupData LineupData { get; } = DatabaseHelper.GetInstanceOrCreateNew<LineupData>(player.Uid);

    public async ValueTask<LineupDataInfo?> UpdateLineup(int lineupId, uint member1, uint member2, uint member3)
    {
        if (!LineupData.LineupInfo.TryGetValue(lineupId, out var formation))
        {
            formation = new LineupDataInfo
            {
                Index = (uint)lineupId,
                Name = lineupId.ToString()
            };

            LineupData.LineupInfo[lineupId] = formation;
        }

        formation.Member1 = member1;
        formation.Member2 = member2;
        formation.Member3 = member3;
        return formation;
    }
}