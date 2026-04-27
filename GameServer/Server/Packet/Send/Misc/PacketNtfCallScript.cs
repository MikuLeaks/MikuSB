using MikuSB.Database.Character;
using MikuSB.Database.Inventory;
using MikuSB.GameServer.Game.Inventory;
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
        proto.ExtraSync = extraSync;
        SetData(proto);
    }
}
