using MikuSB.Database.Character;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Game.Support;
using MikuSB.Proto;
using MikuSB.TcpSharp;

namespace MikuSB.GameServer.Server.Packet.Send.Misc;

public class PacketNtfCallScript : BasePacket
{
    public PacketNtfCallScript(List<CharacterInfo> characters) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { characters.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }

    public PacketNtfCallScript(List<GameWeaponInfo> weapons) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { weapons.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }

    public PacketNtfCallScript(List<BaseGameItemInfo> items) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { items.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }

    public PacketNtfCallScript(List<GameSupportCardInfo> cards) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { cards.Select(ToSupportCardProto) }
            }
        };

        SetData(proto);
    }

    public PacketNtfCallScript(List<GameSkinInfo> skins) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { skins.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }

    public PacketNtfCallScript(InventoryData inventory) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}"
        };

        var extraSync = new NtfSyncPlayer();
        foreach (var item in inventory.Items.Values) extraSync.Items.Add(item.ToProto());
        foreach (var skin in inventory.Skins.Values) extraSync.Items.Add(skin.ToProto());
        foreach (var weapon in inventory.Weapons.Values) extraSync.Items.Add(weapon.ToProto());
        foreach (var supportCard in inventory.SupportCards.Values) extraSync.Items.Add(ToSupportCardProto(supportCard));
        proto.ExtraSync = extraSync;
        SetData(proto);
    }

    public PacketNtfCallScript(PlayerInstance Player) : base(CmdIds.NtfScript)
    {
        Player.BuildPlayerAttr(true);
        var proto = new NtfCallScript
        {
            Api = "",
            Arg = "{}"
        };
        var sync = new NtfSyncPlayer();
        foreach (var x in Player.Data.Attrs)
        {
            uint gid = x.Gid;
            uint sid = x.Sid;
            uint val = x.Val;

            if (gid == 0)
            {
                sync.Custom[sid] = val;
                continue;
            }

            sync.Custom[Player.ToPackedAttrKey(gid, sid)] = val;
            sync.Custom[Player.ToShiftedAttrKey(gid, sid)] = val;
        }
        foreach (var (key, value) in Player.BuildMoneySync())
            sync.Money[key] = value;
        proto.ExtraSync = sync;

        SetData(proto);
    }

    private static Item ToSupportCardProto(GameSupportCardInfo card)
    {
        SupportAffixStateService.NormalizePendingState(card);
        var proto = card.ToProto();
        proto.Slots[(uint)ItemSupportCardSlotTypeEnum.SLOT_AFFIXINDEX] = SupportAffixStateService.GetVisibleInitialAffixIndex(card);
        return proto;
    }
}
