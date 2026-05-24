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

namespace MikuSB.GameServer.Server.CallGS.Handlers.Tower;

[CallGSApi("ClimbTowerLogic_GetReward")]
public class ClimbTowerLogic_GetReward : ICallGSHandler
{
    private const uint TowerGroupId = 3;
    private const uint RewardStateSidBase = 100;
    private const uint TowerLevelStateSidBase = 10000;
    private const uint LaunchPassGroupId = 22;
    private const uint AdvancedDiffSid = 4;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<ClimbTowerGetRewardParam>(param);
        if (req == null || req.Layer <= 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var cycle = ResolveCurrentCycle(GameData.ClimbTowerTimeData.Values, DateTime.Now);
        if (cycle == null)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!TryResolveLayer(cycle, req.Layer, player.Data, out var towerIds, out var diff))
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.ClimbTowerAwardData.TryGetValue((uint)req.Layer, out var diffMap) ||
            !diffMap.TryGetValue(diff, out var rewardCfg))
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var groups = ResolveRequestedGroups(req.Group);
        if (groups.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var claimableGroups = groups
            .Where(group => CanClaimGroup(player.Data, rewardCfg, towerIds, req.Layer, group))
            .Distinct()
            .ToList();

        if (claimableGroups.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var sync = new NtfSyncPlayer();
        var rewardStateAttr = GetOrCreateAttr(player.Data, TowerGroupId, RewardStateSidBase + (uint)req.Layer);
        var responseRewards = new JsonArray();

        foreach (var group in claimableGroups)
        {
            rewardStateAttr.Val |= 1u << GetFlagBitOffset(group);

            foreach (var reward in rewardCfg.GetRewards(group))
            {
                if (reward.Count < 5)
                    continue;

                await GrantRewardAsync(player, sync, reward);
                responseRewards.Add(new JsonArray(
                    (int)reward[0],
                    (int)reward[1],
                    (int)reward[2],
                    (int)reward[3],
                    (int)reward[4]));
            }
        }

        SyncAttr(sync, player, rewardStateAttr);
        DatabaseHelper.SaveDatabaseType(player.Data);
        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);

        var rsp = new JsonObject
        {
            ["tbRewards"] = responseRewards
        };

        await CallGSRouter.SendScript(connection, "ClimbTowerLogic_GetReward", rsp.ToJsonString(), sync);
    }

    private static async Task GrantRewardAsync(PlayerInstance player, NtfSyncPlayer sync, IReadOnlyList<uint> reward)
    {
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
                if (GameData.SuppliesData.TryGetValue(templateId, out var supplies))
                {
                    var item = await player.InventoryManager.AddSuppliesItem(supplies, count, sendPacket: false);
                    if (item != null)
                        sync.Items.Add(item.ToProto());
                }
                break;
            }
            case ItemTypeEnum.TYPE_USEABLE:
            {
                var item = AddOtherItem(player.InventoryManager.InventoryData, reward[0], detail, particular, level, count);
                if (item != null)
                    sync.Items.Add(item.ToProto());
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

    private static bool CanClaimGroup(
        PlayerGameData data,
        ClimbTowerAwardExcel rewardCfg,
        IReadOnlyList<uint> towerIds,
        int layer,
        int group)
    {
        if (group is < 0 or > 3 || IsRewardClaimed(data, layer, group))
            return false;

        if (group == 0)
            return IsLayerPass(data, towerIds);

        var requiredStar = rewardCfg.GetStarCount(group);
        return requiredStar > 0 && GetLayerStar(data, towerIds) >= requiredStar;
    }

    private static bool IsLayerPass(PlayerGameData data, IReadOnlyList<uint> towerIds)
    {
        foreach (var towerId in towerIds)
        {
            if (!GameData.ClimbTowerLevelOrderData.TryGetValue(towerId, out var orderCfg))
                return false;

            var passAttr = data.Attrs.FirstOrDefault(x => x.Gid == LaunchPassGroupId && x.Sid == orderCfg.LevelID);
            if (passAttr == null || passAttr.Val == 0)
                return false;
        }

        return true;
    }

    private static int GetLayerStar(PlayerGameData data, IReadOnlyList<uint> towerIds)
    {
        var total = 0;
        foreach (var towerId in towerIds)
        {
            var attr = data.Attrs.FirstOrDefault(x => x.Gid == TowerGroupId && x.Sid == TowerLevelStateSidBase + towerId);
            var value = attr?.Val ?? 0;
            for (var i = 0; i < 9; i++)
            {
                if (((value >> i) & 1u) != 0)
                    total++;
            }
        }

        return total;
    }

    private static bool IsRewardClaimed(PlayerGameData data, int layer, int group)
    {
        var attr = data.Attrs.FirstOrDefault(x => x.Gid == TowerGroupId && x.Sid == RewardStateSidBase + (uint)layer);
        if (attr == null)
            return false;

        var offset = GetFlagBitOffset(group);
        return ((attr.Val >> offset) & 0xFu) > 0;
    }

    private static int GetFlagBitOffset(int group) => group switch
    {
        0 => 0,
        1 => 4,
        2 => 8,
        3 => 12,
        _ => 0
    };

    private static List<int> ResolveRequestedGroups(int? group)
    {
        if (!group.HasValue)
            return [0, 1, 2, 3];

        return group.Value is >= 0 and <= 3 ? [group.Value] : [];
    }

    private static bool TryResolveLayer(
        ClimbTowerTimeExcel cycle,
        int layer,
        PlayerGameData data,
        out IReadOnlyList<uint> towerIds,
        out int diff)
    {
        var basicGroups = cycle.GetLevelGroups(1);
        if (layer <= basicGroups.Count)
        {
            towerIds = basicGroups[layer - 1];
            diff = 1;
            return towerIds.Count > 0;
        }

        var advancedIndex = layer - basicGroups.Count;
        var advancedGroups = cycle.GetLevelGroups(2);
        if (advancedIndex <= 0 || advancedIndex > advancedGroups.Count)
        {
            towerIds = [];
            diff = 0;
            return false;
        }

        var diffAttr = data.Attrs.FirstOrDefault(x => x.Gid == TowerGroupId && x.Sid == AdvancedDiffSid);
        diff = (int)(diffAttr?.Val ?? 0);
        towerIds = advancedGroups[advancedIndex - 1];
        return diff > 0 && towerIds.Count > 0;
    }

    private static ClimbTowerTimeExcel? ResolveCurrentCycle(IEnumerable<ClimbTowerTimeExcel> configs, DateTime now)
    {
        var parsed = configs
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
            return current.Config;

        var latestStarted = parsed.LastOrDefault(x => x.Start <= now);
        if (latestStarted != null)
            return latestStarted.Config;

        return parsed.FirstOrDefault()?.Config;
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

internal sealed class ClimbTowerGetRewardParam
{
    [JsonPropertyName("nType")]
    public int? Type { get; set; }

    [JsonPropertyName("nLayer")]
    public int Layer { get; set; }

    [JsonPropertyName("nGroup")]
    public int? Group { get; set; }
}
