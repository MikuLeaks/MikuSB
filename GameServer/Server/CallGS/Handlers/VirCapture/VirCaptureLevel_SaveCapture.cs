using MikuSB.Database;
using MikuSB.Data;
using MikuSB.Enums.Item;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_SaveCapture")]
public class VirCaptureLevel_SaveCapture : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureSaveCaptureParam>(param);
        if (req == null || req.LevelId == 0 || req.RegionId == 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        VirCaptureStateHelper.SetPointState(player, (uint)req.LevelId, (uint)req.RegionId, 2u, sync);

        if (!GameData.VirCaptureCaptureRegionData.TryGetValue((uint)req.LevelId, out var captureRegion))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var rewardGdpl = VirCaptureCaptureRewardResolver.ResolveGdpl(captureRegion, (uint)req.RegionId);
        if (rewardGdpl == null || rewardGdpl.Count < 4 || rewardGdpl[0] != (uint)ItemTypeEnum.TYPE_MONSTER_CARD)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}", sync);
            return;
        }

        var grantedItem = await player.InventoryManager.AddMonsterCardItem(
            rewardGdpl[1],
            rewardGdpl[2],
            rewardGdpl[3],
            sendPacket: false);
        if (grantedItem == null)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}", sync);
            return;
        }

        sync.Items.Add(grantedItem.ToProto());
        SyncVirCaptureCounters(player, grantedItem.TemplateId, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);
        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);

        var response = new JsonObject
        {
            ["nLevelID"] = req.LevelId,
            ["nRegionId"] = req.RegionId,
            ["nAddItemId"] = grantedItem.UniqueId,
            ["tbGDPL"] = new JsonArray(rewardGdpl.Select(x => JsonValue.Create((int)x)).ToArray())
        };

        await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", response.ToJsonString(), sync);
    }

    private static void SyncVirCaptureCounters(MikuSB.GameServer.Game.Player.PlayerInstance player, ulong templateId, NtfSyncPlayer sync)
    {
        var bagCount = (uint)player.InventoryManager.InventoryData.Items.Values.Count(x => x.ItemType == ItemTypeEnum.TYPE_MONSTER_CARD);
        VirCaptureStateHelper.SetUnsignedAttr(player, 5, bagCount, sync);

        if (!GameData.MonsterCardData.TryGetValue(templateId, out var monsterCard) || monsterCard.RikiId == 0)
            return;

        var rikiAttr = player.Data.Attrs.FirstOrDefault(x => x.Gid == 135 && x.Sid == monsterCard.RikiId);
        if (rikiAttr == null)
        {
            rikiAttr = new Database.Player.PlayerAttr
            {
                Gid = 135,
                Sid = monsterCard.RikiId,
                Val = 0
            };
            player.Data.Attrs.Add(rikiAttr);
        }

        rikiAttr.Val += 1;
        sync.Custom[player.ToPackedAttrKey(135, monsterCard.RikiId)] = rikiAttr.Val;
        sync.Custom[player.ToShiftedAttrKey(135, monsterCard.RikiId)] = rikiAttr.Val;
    }
}

internal sealed class VirCaptureSaveCaptureParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nRegionId")]
    public int RegionId { get; set; }
}
