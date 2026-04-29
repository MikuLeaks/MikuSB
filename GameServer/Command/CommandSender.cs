using MikuSB.Enums.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using MikuSB.Util;
using MikuSB.Util.Extensions;

namespace MikuSB.GameServer.Command;

public interface ICommandSender
{
    public ValueTask SendMsg(string msg);

    public int GetSender();
}

public class ConsoleCommandSender(Logger logger) : ICommandSender
{
    public async ValueTask SendMsg(string msg)
    {
        logger.Info(msg);
        await Task.CompletedTask;
    }

    public int GetSender()
    {
        return (int)ServerEnum.Console;
    }
}

public class PlayerCommandSender(PlayerInstance player) : ICommandSender
{
    public PlayerInstance Player = player;

    public async ValueTask SendMsg(string msg)
    {
        var data = new ChatMsg
        {
            Type = ChatType.Friend,
            Sender = (uint)ConfigManager.Config.ServerOption.ServerProfile.Uid,
            Recver = (uint)Player.Uid,
            Text = ChatMessageHelper.NormalizeForClient(msg),
            Profile = Player.ToServerFriendProto(),
            TimeStamp = ChatMessageHelper.BuildClientTimestamp()
        };
        await Player.SendPacket(CmdIds.NtfFriendChat, data);
    }

    public int GetSender()
    {
        return Player.Uid;
    }
}