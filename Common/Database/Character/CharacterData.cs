using MikuSB.Enums.Item;
using MikuSB.Proto;
using SqlSugar;

namespace MikuSB.Database.Character;

[SugarTable("character_data")]
public class CharacterData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public List<CharacterInfo> Characters { get; set; } = [];
    public uint NextCharacterGuid { get; set; } = 1;
}

public class CharacterInfo
{
    public uint Guid { get; set; }
    public ulong TemplateId { get; set; }
    public uint Level { get; set; }
    public int Exp { get; set; }
    public uint Break { get; set; }
    public int Evolue { get; set; }
    public uint ProLevel { get; set; }
    public int Trust { get; set; }
    public uint WeaponUniqueId { get; set; }
    public uint SkinId { get; set; }
    public uint WeaponSkinId { get; set; }
    public uint SupportTeamIndex { get; set; } = 1;
    public ItemFlagEnum Flag { get; set; } = ItemFlagEnum.FLAG_READED;
    public uint Expiration { get; set; }
    [SugarColumn(IsJson = true)] public List<uint> UnlockedSkin { get; set; } = [];
    [SugarColumn(IsJson = true)] public List<uint> Spines { get; set; } = [];
    [SugarColumn(IsJson = true)] public List<uint> Affixs { get; set; } = [];
    [SugarColumn(IsJson = true)] public Dictionary<uint, uint> SupportSlots { get; set; } = []; // Key = EqSlot (= support card Detail), Value = support card UniqueId
    public long Timestamp { get; set; }
    public uint Count { get; set; } = 1;

    public Item ToProto()
    {
        var proto = new Item
        {
            Id = Guid,
            Template = TemplateId,
            Count = Count,
            Flag = (uint)Flag,
            Expiration = Expiration,
            Enhance = new Enhance
            {
                Level = Level,
                Exp = ToUInt32(Exp),
                Break = Break,
                Evolue = ToUInt32(Evolue),
                ProLevel = ProLevel,
                Trust = ToUInt32(Trust)
            }
        };
        proto.Enhance.Spines.AddRange(Spines.Select(x => (ulong)x));
        proto.Enhance.Affixs.AddRange(Affixs);

        proto.Slots[(uint)ItemCardSlotTypeEnum.SLOT_WEAPON] = WeaponUniqueId;
        proto.Slots[(uint)ItemCardSlotTypeEnum.SLOT_SKIN] = SkinId;
        proto.Slots[(uint)ItemCardSlotTypeEnum.SLOT_WEAPON_SKIN] = WeaponSkinId;
        proto.Slots[(uint)ItemCardSlotTypeEnum.SLOT_SUPPORTERINDEX] = SupportTeamIndex;
        foreach (var (slot, uid) in SupportSlots)
            proto.Slots[slot] = uid;

        return proto;
    }

    private static uint ToUInt32(int value)
    {
        return value > 0 ? (uint)value : 0;
    }

}