using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using MikuSB.Util;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureTower_LevelSettlement")]
public class VirCaptureTower_LevelSettlement : ICallGSHandler
{
    private const uint LaunchLevelStateGroupId = 21;
    private const uint LaunchPassGroupId = 22;
    private const uint PassedFlagBit = 1u << 8;
    private static readonly Logger Logger = new("VirCaptureTower");

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var (response, sync) = HandleSettlement(connection.Player!, JsonNode.Parse(param));
        await CallGSRouter.SendScript(connection, "VirCaptureTower_LevelSettlement", response.ToJsonString(), sync);
    }

    public static (JsonNode Response, NtfSyncPlayer Sync) HandleSettlement(PlayerInstance player, JsonNode? tbParam)
    {
        var req = tbParam?.Deserialize<VirCaptureTowerSettlementParam>();
        if (req == null || req.LevelId == 0)
        {
            Logger.Error($"Invalid vircapture tower settlement payload: {tbParam?.ToJsonString() ?? "null"}");
            return (new JsonObject { ["sErr"] = "error.BadParam" }, new NtfSyncPlayer());
        }

        var sync = new NtfSyncPlayer();

        var levelStateAttr = GetOrCreateAttr(player.Data, LaunchLevelStateGroupId, (uint)req.LevelId);
        levelStateAttr.Val |= MergeStarMask(req.StarMask) | PassedFlagBit;
        SyncAttr(sync, player, levelStateAttr);

        var passAttr = GetOrCreateAttr(player.Data, LaunchPassGroupId, (uint)req.LevelId);
        passAttr.Val = Math.Max(1u, passAttr.Val + 1);
        SyncAttr(sync, player, passAttr);

        Logger.Info(
            $"VirCaptureTower settlement saved. uid={player.Uid} levelId={req.LevelId} starMask={req.StarMask} " +
            $"levelStateVal={levelStateAttr.Val} passVal={passAttr.Val}");

        DatabaseHelper.SaveDatabaseType(player.Data);
        return (new JsonObject(), sync);
    }

    private static uint MergeStarMask(int starMask)
    {
        uint result = 0;
        for (var i = 0; i < 3; i++)
        {
            if (((starMask >> i) & 1) != 0)
                result |= 1u << i;
        }

        return result;
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

internal sealed class VirCaptureTowerSettlementParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nStar")]
    public int StarMask { get; set; }
}
