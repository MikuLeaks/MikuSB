using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using MikuSB.Util;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("TowerEventChapter_LevelSettlement")]
public class TowerEventChapter_LevelSettlement : ICallGSHandler
{
    private const uint LevelStateGroupId = 21;
    private const uint LaunchPassGroupId = 22;
    private const uint PassedFlagMask = (1u << 8) | 0b111u;
    private static readonly Logger Logger = new("TowerEvent");

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = HandleSettlement(connection.Player!, JsonNode.Parse(param));
        await CallGSRouter.SendScript(connection, "TowerEventChapter_LevelSettlement", response.ToJsonString(), sync);
    }

    public static (JsonNode Response, NtfSyncPlayer Sync) HandleSettlement(PlayerInstance player, JsonNode? tbParam)
    {
        var req = tbParam?.Deserialize<TowerEventSettlementParam>();
        if (req == null || req.LevelId == 0 || req.ChapterId == 0)
        {
            Logger.Error($"Invalid tower event settlement payload: {tbParam?.ToJsonString() ?? "null"}");
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());
        }

        var sync = new NtfSyncPlayer();

        var levelStateAttr = GetOrCreateAttr(player.Data, LevelStateGroupId, (uint)req.LevelId);
        levelStateAttr.Val |= PassedFlagMask;
        SyncAttr(sync, player, levelStateAttr);

        var passAttr = GetOrCreateAttr(player.Data, LaunchPassGroupId, (uint)req.LevelId);
        passAttr.Val = Math.Max(1u, passAttr.Val + 1);
        SyncAttr(sync, player, passAttr);

        Logger.Info(
            $"TowerEvent settlement saved. uid={player.Uid} chapterId={req.ChapterId} levelId={req.LevelId} " +
            $"levelStateVal={levelStateAttr.Val} passVal={passAttr.Val}");

        DatabaseHelper.SaveDatabaseType(player.Data);
        return (new JsonObject(), sync);
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

    private static void SyncAttr(NtfSyncPlayer sync, PlayerInstance player, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class TowerEventSettlementParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nChapterID")]
    public int ChapterId { get; set; }
}
