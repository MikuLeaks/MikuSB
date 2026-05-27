using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Database.Player;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Shop;

[CallGSApi("IBLogic_BuyGoods")]
public class IBLogic_BuyGoods : ICallGSHandler
{
    private const uint BuyGroupId = 26;
    private const uint RedGroupId = 113;
    private const uint CashGroupId = 1;
    private const uint BattlePassGroupId = 25;
    private const uint BattlePassCurIdSid = 1;
    private const uint BattlePassStatusSid = 2;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<IbBuyGoodsParam>(param);
        var player = connection.Player!;
        if (req?.Type == 3 && req.GoodsId > 0 && req.Count > 0)
        {
            await HandleBattlePassPurchase(connection, player, req);
            return;
        }

        if (req == null ||
            req.GoodsId == 0 ||
            req.Count == 0 ||
            !GameData.IbGoodsData.TryGetValue(req.GoodsId, out var goods))
        {
            await CallGSRouter.SendScript(connection, "IBLogic_BuyGoods", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (goods.LimitTimes > 0)
        {
            var buyAttr = GetOrCreateAttr(player, BuyGroupId, req.GoodsId);
            if (buyAttr.Val >= goods.LimitTimes)
            {
                await CallGSRouter.SendScript(connection, "IBLogic_BuyGoods", "{\"sErr\":\"tip.Mall_Limit_Buy\"}");
                return;
            }
        }

        var rewardItems = BuildRewardItems(goods, req);
        if (rewardItems.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "IBLogic_BuyGoods", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var sync = new NtfSyncPlayer();
        foreach (var reward in rewardItems)
            await GrantRewardAsync(player, sync, reward);

        var buyCountAttr = GetOrCreateAttr(player, BuyGroupId, req.GoodsId);
        buyCountAttr.Val += req.Count;
        SyncAttr(player, sync, buyCountAttr);

        var redAttr = GetOrCreateAttr(player, RedGroupId, req.GoodsId);
        if (redAttr.Val == 0)
        {
            redAttr.Val = 1;
            SyncAttr(player, sync, redAttr);
        }

        DatabaseHelper.SaveDatabaseType(player.Data);
        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);

        var responseGoods = new JsonArray();
        foreach (var reward in rewardItems)
        {
            var row = new JsonArray();
            foreach (var value in reward)
                row.Add((int)value);
            responseGoods.Add(row);
        }

        var rsp = new JsonObject
        {
            ["nGoodsId"] = (int)req.GoodsId,
            ["tbGoods"] = responseGoods
        };

        var productId = goods.GetProductId();
        if (!string.IsNullOrWhiteSpace(productId))
            rsp["sProductId"] = productId;

        var cost = req.Index == 2 ? goods.Cost2 : goods.Cost;
        if (cost.Count >= 2)
            rsp["nTotalPrice"] = (int)cost[1];

        await CallGSRouter.SendScript(connection, "IBLogic_BuyGoods", rsp.ToJsonString(), sync);
    }

    private static async Task HandleBattlePassPurchase(Connection connection, PlayerInstance player, IbBuyGoodsParam req)
    {
        var sync = new NtfSyncPlayer();
        var battlePassId = ResolveCurrentBattlePassId();
        if (battlePassId > 0)
        {
            var curIdAttr = GetOrCreateAttr(player, BattlePassGroupId, BattlePassCurIdSid);
            curIdAttr.Val = battlePassId;
            SyncAttr(player, sync, curIdAttr);
        }

        var statusAttr = GetOrCreateAttr(player, BattlePassGroupId, BattlePassStatusSid);
        if (statusAttr.Val < 2)
        {
            statusAttr.Val = 2;
            SyncAttr(player, sync, statusAttr);
        }

        var buyCountAttr = GetOrCreateAttr(player, BuyGroupId, req.GoodsId);
        buyCountAttr.Val += req.Count;
        SyncAttr(player, sync, buyCountAttr);

        var redAttr = GetOrCreateAttr(player, RedGroupId, req.GoodsId);
        if (redAttr.Val == 0)
        {
            redAttr.Val = 1;
            SyncAttr(player, sync, redAttr);
        }

        DatabaseHelper.SaveDatabaseType(player.Data);

        var rsp = new JsonObject
        {
            ["nGoodsId"] = (int)req.GoodsId,
            ["tbGoods"] = new JsonArray()
        };

        await CallGSRouter.SendScript(connection, "IBLogic_BuyGoods", rsp.ToJsonString(), sync);
    }

    private static List<List<uint>> BuildRewardItems(IbGoodsExcel goods, IbBuyGoodsParam req)
    {
        var rewards = new List<List<uint>>();

        if (goods.Item.Count >= 4)
            rewards.Add(WithCount(goods.Item, req.Count));

        if (req.SelectItem1?.Count >= 4)
            rewards.Add(WithCount(req.SelectItem1, req.Count));

        if (req.SelectItem2?.Count >= 4)
            rewards.Add(WithCount(req.SelectItem2, req.Count));

        return rewards;
    }

    private static List<uint> WithCount(IReadOnlyList<uint> item, uint buyCount)
    {
        var reward = item.Take(5).ToList();
        while (reward.Count < 5)
            reward.Add(1);

        reward[4] = Math.Max(1u, reward[4]) * Math.Max(1u, buyCount);
        return reward;
    }

    private static async Task GrantRewardAsync(PlayerInstance player, NtfSyncPlayer sync, IReadOnlyList<uint> reward)
    {
        if (reward.Count < 5)
            return;

        var itemType = (ItemTypeEnum)reward[0];
        var detail = reward[1];
        var particular = reward[2];
        var level = reward[3];
        var count = Math.Max(1u, reward[4]);

        switch (itemType)
        {
            case ItemTypeEnum.TYPE_CARD:
                for (var i = 0u; i < count; i++)
                {
                    var character = await player.CharacterManager.AddCharacter(itemType, detail, particular, level, sendPacket: false);
                    if (character != null)
                        sync.Items.Add(character.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_WEAPON:
                for (var i = 0u; i < count; i++)
                {
                    var weapon = await player.InventoryManager.AddWeaponItem(itemType, detail, particular, level, sendPacket: false);
                    if (weapon != null)
                        sync.Items.Add(weapon.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_SUPPORT:
                for (var i = 0u; i < count; i++)
                {
                    var support = await player.InventoryManager.AddSupportCardItem(detail, particular, level, sendPacket: false);
                    if (support != null)
                        sync.Items.Add(support.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_SUPPLIES:
            {
                var templateId = (uint)GameResourceTemplateId.FromGdpl(reward[0], detail, particular, level);
                if (!GameData.SuppliesData.TryGetValue(templateId, out var supplies))
                    break;

                var item = await player.InventoryManager.AddSuppliesItem(supplies, count, sendPacket: false);
                if (item != null)
                    sync.Items.Add(item.ToProto());
                break;
            }
            case ItemTypeEnum.TYPE_USEABLE:
            {
                if (!TryGrantCashBox(player, sync, detail, particular, level, count))
                {
                    var item = AddOtherItem(player.InventoryManager.InventoryData, reward[0], detail, particular, level, count);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            }
            case ItemTypeEnum.TYPE_WEAPON_PART:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddWeaponPartItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_CARD_SKIN:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddSkinItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_HOUSE:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddHouseFurnitureItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_PROFILE:
            case ItemTypeEnum.TYPE_FRAME:
            case ItemTypeEnum.TYPE_BADGE:
            case ItemTypeEnum.TYPE_COVER:
            case ItemTypeEnum.TYPE_NAMECARD:
            case ItemTypeEnum.TYPE_EXPRESSION:
            case ItemTypeEnum.TYPE_BUBBLE:
            case ItemTypeEnum.TYPE_ANALYST:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddProfileItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_WEAPON_SKIN:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddWeaponSkinItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_MANIFESTATION:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddManifestationItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_CARD_SKIN_PART:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddSkinPartItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_AR:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddArItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            case ItemTypeEnum.TYPE_CALL:
                for (var i = 0u; i < count; i++)
                {
                    var item = await player.InventoryManager.AddCallItem(itemType, detail, particular, level, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
        }
    }

    private static BaseGameItemInfo? AddOtherItem(InventoryData inventory, uint genre, uint detail, uint particular, uint level, uint count)
    {
        var templateId = (uint)GameResourceTemplateId.FromGdpl(genre, detail, particular, level);
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

    private static bool TryGrantCashBox(PlayerInstance player, NtfSyncPlayer sync, uint detail, uint particular, uint level, uint count)
    {
        var templateId = (uint)GameResourceTemplateId.FromGdpl((uint)ItemTypeEnum.TYPE_USEABLE, detail, particular, level);
        if (!GameData.OtherItemData.TryGetValue(templateId, out var otherItem))
            return false;

        uint moneyType = otherItem.LuaType switch
        {
            "money_box" => 1,
            "gold_box" => 2,
            "silver_box" => 3,
            "vigor_box" => 4,
            _ => 0
        };

        if (moneyType == 0 || otherItem.Param1 == 0)
            return false;

        var amount = checked(otherItem.Param1 * count);
        var sid = moneyType * 2 + 1;
        var attr = GetOrCreateAttr(player, CashGroupId, sid);
        attr.Val += amount;
        SyncAttr(player, sync, attr);
        if (moneyType == 1)
        {
            foreach (var (key, value) in player.BuildMoneySync())
                sync.Money[key] = value;
        }
        return true;
    }

    private static uint ResolveCurrentBattlePassId()
    {
        var now = DateTime.Now;
        var parsed = GameData.BattlePassTimeData.Values
            .Select(x => new
            {
                Config = x,
                Start = ParseConfigTime(x.StartTime),
                End = ParseConfigTime(x.EndTime)
            })
            .Where(x => x.Start.HasValue && x.End.HasValue)
            .OrderBy(x => x.Start)
            .ToList();

        var current = parsed.FirstOrDefault(x => x.Start <= now && now < x.End);
        if (current != null)
            return current.Config.Id;

        var latestStarted = parsed.LastOrDefault(x => x.Start <= now && x.End > x.Start);
        return latestStarted?.Config.Id ?? 0;
    }

    private static DateTime? ParseConfigTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim().Trim('[', ']');
        if (normalized.Length != 12)
            return null;

        return DateTime.TryParseExact(
            normalized,
            "yyyyMMddHHmm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var value)
            ? value
            : null;
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

internal sealed class IbBuyGoodsParam
{
    [JsonPropertyName("nType")]
    public int Type { get; set; }

    [JsonPropertyName("nGoodsId")]
    public uint GoodsId { get; set; }

    [JsonPropertyName("nCount")]
    public uint Count { get; set; }

    [JsonPropertyName("nIndex")]
    public int Index { get; set; }

    [JsonPropertyName("tbSelectItem1")]
    public List<uint>? SelectItem1 { get; set; }

    [JsonPropertyName("tbSelectItem2")]
    public List<uint>? SelectItem2 { get; set; }
}
