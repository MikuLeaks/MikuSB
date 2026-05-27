using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Shop;

[CallGSApi("IBLogic_GoodsRedDot")]
public class IBLogic_GoodsRedDot : ICallGSHandler
{
    private const uint RedGroupId = 113;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<IbGoodsRedDotParam>(param);
        if (req?.GoodsIds == null || req.GoodsIds.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "IBLogic_GoodsRedDot", "null");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        var changed = false;

        foreach (var goodsId in req.GoodsIds.Where(x => x > 0).Distinct())
        {
            var attr = GetOrCreateAttr(player, RedGroupId, goodsId);
            if (attr.Val > 0)
                continue;

            attr.Val = 1;
            SyncAttr(player, sync, attr);
            changed = true;
        }

        if (changed)
            DatabaseHelper.SaveDatabaseType(player.Data);

        await CallGSRouter.SendScript(connection, "IBLogic_GoodsRedDot", "null", sync);
    }

    private static PlayerAttr GetOrCreateAttr(PlayerInstance player, uint gid, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr
        {
            Gid = gid,
            Sid = sid
        };
        player.Data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncAttr(PlayerInstance player, NtfSyncPlayer sync, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class IbGoodsRedDotParam
{
    [JsonPropertyName("tbList")]
    public List<uint> GoodsIds { get; set; } = [];
}
