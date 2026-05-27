using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("Adjust_Record")]
public class Adjust_Record : ICallGSHandler
{
    private const uint GroupId = 107;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<AdjustRecordParam>(param);
        if (req == null || req.Type == 0)
        {
            await CallGSRouter.SendScript(connection, "Adjust_Record", "null");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        var attr = GetOrCreateAttr(player, req.Type);

        if (attr.Val == 0)
        {
            attr.Val = 1;
            sync.Custom[player.ToPackedAttrKey(GroupId, req.Type)] = 1;
            sync.Custom[player.ToShiftedAttrKey(GroupId, req.Type)] = 1;
            DatabaseHelper.SaveDatabaseType(player.Data);
        }

        await CallGSRouter.SendScript(connection, "Adjust_Record", "null", sync);
    }

    private static PlayerAttr GetOrCreateAttr(PlayerInstance player, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = GroupId,
            Sid = sid
        };
        player.Data.Attrs.Add(attr);
        return attr;
    }
}

internal sealed class AdjustRecordParam
{
    [JsonPropertyName("nType")]
    public uint Type { get; set; }
}
