using MikuSB.GameServer.Game.Player;
using MikuSB.TcpSharp;
using MikuSB.Proto;
using MikuSB.Util;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Server.Packet.Send.Login;

public class PacketRspLogin : BasePacket
{
    private static readonly Logger Logger = new("RspLogin");

    public PacketRspLogin(PlayerInstance player, bool includeSupportCards = true) : base(CmdIds.RspLogin)
    {
        var characterCount = player.CharacterManager.CharacterData.Characters.Count;
        var itemCount = player.InventoryManager.InventoryData.Items.Count;
        var skinCount = player.InventoryManager.InventoryData.Skins.Count;
        var weaponCount = player.InventoryManager.InventoryData.Weapons.Count;
        var supportCardCount = player.InventoryManager.InventoryData.SupportCards.Count;
        var attrCount = player.Data.Attrs.Count;
        var strAttrCount = player.Data.StrAttrs.Count;
        var showItemCount = player.Data.ShowItems.Count;

        var proto = new RspLogin
        {
            Timestamp = (uint)Extensions.GetUnixSec(),
            WorldChannel = 1,
            AreaId = 1,
            Data = player.ToPlayerProto(includeSupportCards),
            NeedRename = false
        };

        var bytes = Google.Protobuf.MessageExtensions.ToByteArray(proto);
        Logger.Info(
            "RspLogin content: " +
            $"characters={characterCount}, " +
            $"items={itemCount}, " +
            $"skins={skinCount}, " +
            $"weapons={weaponCount}, " +
            $"supportCards={supportCardCount}, " +
            $"supportCardsInRspLogin={(includeSupportCards ? supportCardCount : 0)}, " +
            $"attrs={attrCount}, " +
            $"strAttrs={strAttrCount}, " +
            $"showItems={showItemCount}, " +
            $"protoItems={proto.Data.Items.Count}, " +
            $"protoAttrs={proto.Data.Attrs.Count}, " +
            $"protoStrAttrs={proto.Data.StrAttrs.Count}");
        Logger.Info($"RspLogin proto size: {bytes.Length} bytes");

        SetData(bytes);
    }
}
