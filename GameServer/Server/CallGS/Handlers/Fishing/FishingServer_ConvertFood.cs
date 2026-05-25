using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Database.Player;
using MikuSB.Enums.Item;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Fishing;

[CallGSApi("FishingServer_ConvertFood")]
public class FishingServer_ConvertFood : ICallGSHandler
{
    private const uint FishingGroupId = 32;
    private const uint CashGroupId = 1;
    private const uint FoodBaseSid = 30000;
    private const uint FoodAvaTimeSubType = 1;
    private const uint ExploreAvaTimeSubType = 2;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<FishingConvertFoodParam>(param);
        if (req == null || req.FoodId <= 0 || req.Num <= 0)
        {
            await CallGSRouter.SendScript(connection, "FishingServer_ConvertFood", "{\"sError\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.FishingFoodData.TryGetValue((uint)req.FoodId, out var food))
        {
            await CallGSRouter.SendScript(connection, "FishingServer_ConvertFood", "{\"sError\":\"error.BadParam\"}");
            return;
        }

        var count = Math.Max(1u, req.Num);
        var sync = new NtfSyncPlayer();

        if (!HasEnoughMaterials(player.InventoryManager.InventoryData, food.NeedItem, count) ||
            !HasEnoughCash(player.Data, food.BaitNum, count))
        {
            await CallGSRouter.SendScript(connection, "FishingServer_ConvertFood", "{\"sError\":\"tip.girlcard_cmd_err\"}");
            return;
        }

        ConsumeMaterials(player.InventoryManager.InventoryData, food.NeedItem, count, sync.Items);
        ConsumeCash(player, food.BaitNum, count, sync);

        var response = new JsonObject
        {
            ["nFoodID"] = req.FoodId
        };

        switch (food.FoodType)
        {
            case 1:
                ApplyFoodDuration(player, food, FoodAvaTimeSubType, count, sync);
                break;
            case 2:
            {
                var rewards = await CreateItemsAsync(player, sync, food.CreateItems, count);
                response["tbBait"] = rewards;
                break;
            }
            case 3:
                ApplyFoodDuration(player, food, ExploreAvaTimeSubType, count, sync);
                break;
            default:
                await CallGSRouter.SendScript(connection, "FishingServer_ConvertFood", "{\"sError\":\"error.BadParam\"}");
                return;
        }

        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.Data);

        await CallGSRouter.SendScript(connection, "FishingServer_ConvertFood", response.ToJsonString(), sync);
    }

    private static bool HasEnoughMaterials(InventoryData inventory, IEnumerable<List<uint>> costs, uint multiplier)
    {
        foreach (var cost in costs)
        {
            if (cost.Count < 5)
                return false;

            var templateId = GameResourceTemplateId.FromGdpl(cost[0], cost[1], cost[2], cost[3]);
            var item = inventory.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
            var needCount = checked(cost[4] * multiplier);
            if (item == null || item.ItemCount < needCount)
                return false;
        }

        return true;
    }

    private static void ConsumeMaterials(InventoryData inventory, IEnumerable<List<uint>> costs, uint multiplier, ICollection<Item> syncItems)
    {
        foreach (var cost in costs)
        {
            var templateId = GameResourceTemplateId.FromGdpl(cost[0], cost[1], cost[2], cost[3]);
            var item = inventory.Items.Values.First(x => x.TemplateId == templateId);
            var needCount = checked(cost[4] * multiplier);
            item.ItemCount -= needCount;

            if (item.ItemCount == 0)
            {
                inventory.Items.Remove(item.UniqueId);
                var proto = item.ToProto();
                proto.Count = 0;
                syncItems.Add(proto);
            }
            else
            {
                syncItems.Add(item.ToProto());
            }
        }
    }

    private static bool HasEnoughCash(PlayerGameData data, IReadOnlyList<uint> baitNum, uint multiplier)
    {
        if (baitNum.Count < 2)
            return true;

        var moneyType = baitNum[0];
        var need = checked(baitNum[1] * multiplier);
        var sid = moneyType * 2 + 1;
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == CashGroupId && x.Sid == sid);
        return (attr?.Val ?? 0) >= need;
    }

    private static void ConsumeCash(MikuSB.GameServer.Game.Player.PlayerInstance player, IReadOnlyList<uint> baitNum, uint multiplier, NtfSyncPlayer sync)
    {
        if (baitNum.Count < 2)
            return;

        var moneyType = baitNum[0];
        var sid = moneyType * 2 + 1;
        var need = checked(baitNum[1] * multiplier);
        var attr = GetOrCreateAttr(player.Data, CashGroupId, sid);
        attr.Val -= need;
        SyncAttr(player, sync, attr);
    }

    private static void ApplyFoodDuration(MikuSB.GameServer.Game.Player.PlayerInstance player, FishingFoodExcel food, uint subType, uint count, NtfSyncPlayer sync)
    {
        var sid = FoodBaseSid + food.Id * 10 + subType;
        var attr = GetOrCreateAttr(player.Data, FishingGroupId, sid);
        var now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = Math.Max(attr.Val, now);
        attr.Val = checked(startTime + food.EffectTime * count);
        SyncAttr(player, sync, attr);
    }

    private static async Task<JsonArray> CreateItemsAsync(MikuSB.GameServer.Game.Player.PlayerInstance player, NtfSyncPlayer sync, IReadOnlyList<uint> createItem, uint multiplier)
    {
        var rewards = new JsonArray();
        if (createItem.Count < 5)
            return rewards;

        var itemType = (ItemTypeEnum)createItem[0];
        var detail = createItem[1];
        var particular = createItem[2];
        var level = createItem[3];
        var totalCount = checked(createItem[4] * multiplier);

        switch (itemType)
        {
            case ItemTypeEnum.TYPE_SUPPLIES:
            {
                var templateId = (uint)GameResourceTemplateId.FromGdpl(createItem[0], detail, particular, level);
                if (GameData.SuppliesData.TryGetValue(templateId, out var supplies))
                {
                    var item = await player.InventoryManager.AddSuppliesItem(supplies, totalCount, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            }
            case ItemTypeEnum.TYPE_USEABLE:
            {
                var item = AddOtherItem(player.InventoryManager.InventoryData, detail, particular, level, totalCount);
                if (item != null)
                    sync.Items.Add(item.ToProto());
                break;
            }
        }

        rewards.Add(new JsonArray((int)createItem[0], (int)detail, (int)particular, (int)level, (int)totalCount));
        return rewards;
    }

    private static BaseGameItemInfo? AddOtherItem(InventoryData inventory, uint detail, uint particular, uint level, uint count)
    {
        var templateId = (uint)GameResourceTemplateId.FromGdpl((uint)ItemTypeEnum.TYPE_USEABLE, detail, particular, level);
        if (!GameData.OtherItemData.TryGetValue(templateId, out var otherItem))
            return null;

        var maxCount = otherItem.GMnum > 0 ? otherItem.GMnum : 99999u;
        var existing = inventory.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
        if (existing != null)
        {
            existing.ItemCount = Math.Min(existing.ItemCount + count, maxCount);
            return existing;
        }

        var item = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = inventory.NextUniqueUid++,
            ItemType = ItemTypeEnum.TYPE_USEABLE,
            ItemCount = Math.Min(count, maxCount)
        };
        inventory.Items[item.UniqueId] = item;
        return item;
    }

    private static PlayerAttr GetOrCreateAttr(PlayerGameData data, uint gid, uint sid)
    {
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new PlayerAttr { Gid = gid, Sid = sid, Val = 0 };
        data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncAttr(MikuSB.GameServer.Game.Player.PlayerInstance player, NtfSyncPlayer sync, PlayerAttr attr)
    {
        sync.Custom[player.ToPackedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(attr.Gid, attr.Sid)] = attr.Val;
    }
}

internal sealed class FishingConvertFoodParam
{
    [JsonPropertyName("nFoodID")]
    public int FoodId { get; set; }

    [JsonPropertyName("nNum")]
    public uint Num { get; set; }
}
