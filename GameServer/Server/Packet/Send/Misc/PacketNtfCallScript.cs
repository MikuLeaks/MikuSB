using MikuSB.Database.Character;
using MikuSB.Proto;
using MikuSB.TcpSharp;

namespace MikuSB.GameServer.Server.Packet.Send.Misc;

public class PacketNtfCallScript : BasePacket
{
    public PacketNtfCallScript(List<CharacterInfo> Characters) : base(CmdIds.NtfScript)
    {
        var proto = new NtfCallScript 
        { 
            Api = "",
            Arg = "{}",
            ExtraSync = new NtfSyncPlayer
            {
                Items = { Characters.Select(x => x.ToProto()) }
            } 
        };

        SetData(proto);
    }
}
