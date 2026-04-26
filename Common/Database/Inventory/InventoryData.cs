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
}

public class BaseGameItemInfo
{
    public uint UniqueId { get; set; }
    public ulong TemplateId { get; set; }
    public uint ItemCount { get; set; }
    public ItemFlagEnum Flag { get; set; }

    public virtual Item ToProto()
    {
        return new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag
        };
    }
}

public abstract class GrowableItemInfo : BaseGameItemInfo
{
    public bool IsLocked { get; set; }
    public uint Level { get; set; }
    public uint Exp { get; set; }
    public uint Break { get; set; }
    public uint EquipAvatarId { get; set; }
}

public class GameWeaponInfo : GrowableItemInfo
{
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
                Break = Break
            }
        };
        return proto;
    }
}public class GameSkinInfo : BaseGameItemInfo
{
    public uint Level { get; set; }
    public override Item ToProto()
    {
        var proto = new Item
        {
            Id = UniqueId,
            Template = TemplateId,
            Count = ItemCount,
            Flag = (uint)Flag,
        };
        return proto;
    }
}