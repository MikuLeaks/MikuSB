using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;

namespace MikuSB.GameServer.Game.Inventory;

public class InventoryManager(PlayerInstance player) : BasePlayerManager(player)
{
    public InventoryData InventoryData { get; } = DatabaseHelper.GetInstanceOrCreateNew<InventoryData>(player.Uid);

    public async ValueTask<GameWeaponInfo?> AddWeaponItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1)
    {
        if (genre != ItemTypeEnum.TYPE_WEAPON) return null;
        var weaponData = GameData.WeaponData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (weaponData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,level);
        var weaponInfo = new GameWeaponInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            Level = level,
            Break = weaponData.InitBreak,
            Flag = ItemFlagEnum.FLAG_READED,
            ItemCount = 1
        };
        InventoryData.Weapons[weaponInfo.UniqueId] = weaponInfo;

        return weaponInfo;
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

    public async ValueTask<GameSkinInfo?> AddSkinItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1)
    {
        if (genre != ItemTypeEnum.TYPE_CARD_SKIN) return null;
        var skinData = GameData.CardSkinData.Values.FirstOrDefault(x => x.Genre == (int)genre && x.Detail == detail && x.Particular == particular && x.Level == level);
        if (skinData == null) return null;

        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,level);
        var skinInfo = new GameSkinInfo
        {
            TemplateId = templateId,
            UniqueId = InventoryData.NextUniqueUid++,
            Level = level,
            Flag = ItemFlagEnum.FLAG_READED,
            ItemCount = 1
        };
        InventoryData.Skins[skinInfo.UniqueId] = skinInfo;

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
        var templateId = GameResourceTemplateId.FromGdpl((uint)genre,detail,particular,level);
        return InventoryData.Skins.Values.FirstOrDefault(x => x.TemplateId == templateId);
    }

    public async ValueTask<BaseGameItemInfo?> AddArItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1)
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
            Flag = ItemFlagEnum.FLAG_READED,
            ItemCount = 1
        };
        InventoryData.Items[arInfo.UniqueId] = arInfo;
        return arInfo;
    }

    public async ValueTask<BaseGameItemInfo?> AddManifestationItem(ItemTypeEnum genre, uint detail, uint particular, uint level = 1)
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
            Flag = ItemFlagEnum.FLAG_READED,
            ItemCount = 1
        };
        InventoryData.Items[manifestInfo.UniqueId] = manifestInfo;
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
}