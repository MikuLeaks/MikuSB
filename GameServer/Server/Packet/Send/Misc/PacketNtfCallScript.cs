using MikuSB.Database.Character;
using MikuSB.Database.Inventory;
using MikuSB.GameServer.Game.Player;
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
                Items = { cards.Select(x => x.ToProto()) }
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
        foreach (var supportCard in inventory.SupportCards.Values) extraSync.Items.Add(supportCard.ToProto());
        proto.ExtraSync = extraSync;
        SetData(proto);
    }

    public PacketNtfCallScript(PlayerInstance Player) : base(CmdIds.NtfScript)
    {
        Player.BuildPlayerAttr();
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
        proto.ExtraSync = sync;

        SetData(proto);
    }
}
