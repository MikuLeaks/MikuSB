using MikuSB.Enums.Item;
using MikuSB.Proto;
using SqlSugar;

namespace MikuSB.Database.Inventory;

[SugarTable("inventory_data")]
public class InventoryData : BaseDatabaseDataHelper
{
    public uint NextUniqueUid { get; set; } = 100000;

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, BaseGameItemInfo> Items { get; set; } = [];  // Key: UniqueId

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, GameWeaponInfo> Weapons { get; set; } = [];  // Key: UniqueId

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, GameSkinInfo> Skins { get; set; } = [];  // Key: UniqueId

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, GameSupportCardInfo> SupportCards { get; set; } = [];  // Key: UniqueId

    [SugarColumn(IsJson = true)]
    public Dictionary<uint, uint> SkinTypesBySkinId { get; set; } = [];  // Key: nSkinId, Value: client nType
}

public class BaseGameItemInfo
{
    public uint UniqueId { get; set; }
    public ulong TemplateId { get; set; }
    public uint ItemCount { get; set; }
    public ItemTypeEnum ItemType { get; set; }
    public ItemFlagEnum Flag { get; set; } = ItemFlagEnum.FLAG_READED;
    public uint Level { get; set; }
    public uint Exp { get; set; }

    public virtual Item ToProto()
    {
        var proto = new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag
        };
        if (Level > 0 || Exp > 0)
            proto.Enhance = new Enhance { Level = Level, Exp = Exp };
        return proto;
    }
}

public abstract class GrowableItemInfo : BaseGameItemInfo
{
    public bool IsLocked { get; set; }
    public new uint Level { get; set; }
    public new uint Exp { get; set; }
    public uint Break { get; set; }
    public uint Evolue { get; set; }
    public uint EquipAvatarId { get; set; }
}

public class GameWeaponInfo : GrowableItemInfo
{
    [SugarColumn(IsJson = true)] public Dictionary<uint, ulong> PartSlots { get; set; } = [];
    public override Item ToProto()
    {
        var proto = new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag,
            Enhance = new Enhance
            {
                Level = Level,
                Exp = Exp,
                Break = Break,
                Evolue = Evolue
            }
        };
        foreach (var (slot, uid) in PartSlots) proto.Slots[slot] = uid;
        return proto;
    }
}
public class GameSkinInfo : BaseGameItemInfo
{
    [SugarColumn(IsJson = true)] public Dictionary<uint, ulong> PartSlots { get; set; } = [];
    public uint SkinType { get; set; }
    public override Item ToProto()
    {
        var proto = new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag,
        };
        proto.Slots[(uint)ItemSkinSlotTypeEnum.SLOT_CARD_SKIL_TYPE] = Math.Min(SkinType, 1);
        foreach (var (slot, uid) in PartSlots) proto.Slots[slot] = uid;
        return proto;
    }
}


public class GameSupportCardInfo : BaseGameItemInfo
{
    public uint AffixId { get; set; }
    public override Item ToProto()
    {
        var proto = new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag,
            Enhance = new Enhance
            {
                Level = Level,
                Exp = Exp
            }
        };
        proto.Slots[(uint)ItemSupportCardSlotTypeEnum.SLOT_AFFIXINDEX] = AffixId;
        return proto;
    }
}
