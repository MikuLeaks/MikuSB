using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("ClimbTowerLogic_SetLevelDiff")]
public class ClimbTowerLogic_SetLevelDiff : ICallGSHandler
{
    private const uint TowerGroupId = 3;
    private const uint DiffSid = 4;
    private const uint HisDiffSid = 5;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<ClimbTowerSetLevelDiffParam>(param);
        if (req == null || req.Diff <= 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_SetLevelDiff", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.ClimbTowerDiffData.ContainsKey((uint)req.Diff))
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_SetLevelDiff", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var hisDiff = GetAttrValue(player.Data, TowerGroupId, HisDiffSid);
        if (req.Diff > hisDiff + 1)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_SetLevelDiff", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var diffAttr = GetOrCreateAttr(player.Data, TowerGroupId, DiffSid);
        diffAttr.Val = (uint)req.Diff;

        var sync = new NtfSyncPlayer();
        sync.Custom[player.ToPackedAttrKey(diffAttr.Gid, diffAttr.Sid)] = diffAttr.Val;
        sync.Custom[player.ToShiftedAttrKey(diffAttr.Gid, diffAttr.Sid)] = diffAttr.Val;

        DatabaseHelper.SaveDatabaseType(player.Data);
        await CallGSRouter.SendScript(connection, "ClimbTowerLogic_SetLevelDiff", "{}", sync);
    }

    private static uint GetAttrValue(PlayerGameData data, uint gid, uint sid)
    {
        return data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid)?.Val ?? 0;
    }

    private static PlayerAttr GetOrCreateAttr(PlayerGameData data, uint gid, uint sid)
    {
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = gid,
            Sid = sid
        };
        data.Attrs.Add(attr);
        return attr;
    }
}

internal sealed class ClimbTowerSetLevelDiffParam
{
    [JsonPropertyName("nDiff")]
    public int Diff { get; set; }
}
