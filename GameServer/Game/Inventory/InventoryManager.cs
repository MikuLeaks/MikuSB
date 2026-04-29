using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server.Packet.Send.Misc;

namespace MikuSB.GameServer.Game.Inventory;

public class InventoryManager(PlayerInstance player) : BasePlayerManager(player)
{
    public InventoryData InventoryData { get; } = DatabaseHelper.GetInstanceOrCreateNew<InventoryData>(player.Uid);

    public async ValueTask<GameWeaponInfo?> AddWeaponItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, uint weaponLevel = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_WEAPON) return null;
        var weaponData = GameData.WeaponData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (weaponData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,level);
        var weaponInfo = new GameWeaponInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            Level = weaponLevel,
            Break = GetWeaponBreak(weaponLevel),
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Weapons[weaponInfo.UniqueId] = weaponInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([weaponInfo]));

        return weaponInfo;
    }

    private static uint GetWeaponBreak(uint level)
    {
        if (level <= 20) return 1;
        if (level <= 40) return 2;
        if (level <= 60) return 3;
        if (level <= 70) return 4;
        if (level <= 80) return 5;
        if (level <= 90) return 6;
        return 7;
    }

    public GameWeaponInfo? GetWeaponItem(uint uniqueId)
    {
        return InventoryData.Weapons.GetValueOrDefault(uniqueId);
    }

    public GameWeaponInfo? GetWeaponItemByTemplateId(ulong templateId)
    {
        return InventoryData.Weapons.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public GameWeaponInfo? GetWeaponItemGDPL(ItemTypeEnum genre, uint detail, uint particular, uint level)
    {
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular, level);
        return InventoryData.Weapons.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public async ValueTask<GameSkinInfo?> AddSkinItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_CARD_SKIN) return null;
        var skinData = GameData.CardSkinData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (skinData == null) return null;
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var skinInfo = new GameSkinInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Skins[skinInfo.UniqueId] = skinInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([skinInfo]));

        return skinInfo;
    }

    public GameSkinInfo? GetSkinItem(uint uniqueId)
    {
        return InventoryData.Skins.GetValueOrDefault(uniqueId);
    }

    public GameSkinInfo? GetSkinItemByTemplateId(ulong templateId)
    {
        return InventoryData.Skins.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public GameSkinInfo? GetSkinItemGDPL(ItemTypeEnum genre, uint detail, uint particular, uint level)
    {
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        return InventoryData.Skins.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public async ValueTask<BaseGameItemInfo?> AddArItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_AR) return null;
        var arData = GameData.ArItemData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (arData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var arInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[arInfo.UniqueId] = arInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([arInfo]));

        return arInfo;
    }

    public async ValueTask<GameSupportCardInfo?> AddSupportCardItem(uint detail, uint particular, uint level = 1, uint cardLevel = 1, bool sendPacket = true)
    {
        const ItemTypeEnum genre = ItemTypeEnum.TYPE_SUPPORT;
        var spCard = GameData.SupportCardData.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (spCard == null) return null;
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        cardLevel = Math.Clamp(cardLevel, 1, spCard.MaxLevel);
        var info = new GameSupportCardInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1,
            Level = cardLevel,
        };
        InventoryData.SupportCards[info.UniqueId] = info;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([info]));

        return info;
    }

    public GameSupportCardInfo? GetSupportCardItem(uint uniqueId)
    {
        return InventoryData.SupportCards.GetValueOrDefault(uniqueId);
    }

    public GameSupportCardInfo? GetSupportCardByTemplateId(ulong templateId)
    {
        return InventoryData.SupportCards.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public GameSupportCardInfo? GetSupportCardItemGDPL(ItemTypeEnum genre, uint detail, uint particular, uint level)
    {
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        return InventoryData.SupportCards.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public async ValueTask<BaseGameItemInfo?> AddManifestationItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_MANIFESTATION) return null;
        var manifestData = GameData.ManifestationData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (manifestData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var manifestInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[manifestInfo.UniqueId] = manifestInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([manifestInfo]));

        return manifestInfo;
    }

    public BaseGameItemInfo? GetNormalItem(uint uniqueId)
    {
        return InventoryData.Items.GetValueOrDefault(uniqueId);
    }

    public BaseGameItemInfo? GetNormalItemByTemplateId(ulong templateId)
    {
        return InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public BaseGameItemInfo? GetNormalItemGDPL(ItemTypeEnum genre, uint detail, uint particular, uint level)
    {
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        return InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    private static uint GetSuppliesMaxCount(SuppliesExcel suppliesData) =>
        suppliesData.Genre == 5 && suppliesData.Detail == 4 ? 999999u : 99999u;

    public async ValueTask<BaseGameItemInfo?> AddSuppliesItem(SuppliesExcel suppliesData, uint count, bool sendPacket = true)
    {
        var templateId = GameResourceTemplateId.FromGdpl(suppliesData.Genre, suppliesData.Detail, suppliesData.Particular, suppliesData.Level);

        uint maxCount = GetSuppliesMaxCount(suppliesData);
        uint giveCount = Math.Min(count, maxCount);

        var existing = InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
        if (existing != null)
        {
            existing.ItemCount = Math.Min(existing.ItemCount + giveCount, maxCount);
            return existing;
        }

        var itemInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = ItemTypeEnum.TYPE_SUPPLIES,
            ItemCount = giveCount
        };
        InventoryData.Items[itemInfo.UniqueId] = itemInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([itemInfo]));

        return itemInfo;
    }

    public async ValueTask<BaseGameItemInfo?> AddWeaponSkinItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_WEAPON_SKIN) return null;
        var skinData = GameData.WeaponSkinData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (skinData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var skinInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[skinInfo.UniqueId] = skinInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([skinInfo]));

        return skinInfo;
    }

    public async ValueTask<BaseGameItemInfo?> AddProfileItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre < ItemTypeEnum.TYPE_PROFILE || genre > ItemTypeEnum.TYPE_ANALYST) return null;
        var profileData = GameData.ProfileData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (profileData == null) return null;
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var profileInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[profileInfo.UniqueId] = profileInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([profileInfo]));

        return profileInfo;
    }

    public async ValueTask<BaseGameItemInfo?> AddSkinPartItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_CARD_SKIN_PART) return null;
        var skinPartData = GameData.CardSkinPartsData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (skinPartData == null) return null;
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var skinPartInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[skinPartInfo.UniqueId] = skinPartInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([skinPartInfo]));

        return skinPartInfo;
    }

    public async ValueTask<BaseGameItemInfo?> AddCallItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1, bool sendPacket = true)
    {
        if (genre != ItemTypeEnum.TYPE_CALL) return null;
        var callData = GameData.CallItemData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (callData == null) return null;
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre, detail, particular, level);
        if (InventoryData.Items.Values.Any(x => x.TemplateId == templateId)) return null;
        var callInfo = new BaseGameItemInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            ItemType = genre,
            ItemCount = 1
        };
        InventoryData.Items[callInfo.UniqueId] = callInfo;

        if (sendPacket) await Player.SendPacket(new PacketNtfCallScript([callInfo]));

        return callInfo;
    }
}