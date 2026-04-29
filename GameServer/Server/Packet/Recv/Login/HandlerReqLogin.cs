using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Account;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.Girl;
using MikuSB.GameServer.Server.Packet.Send.Friend;
using MikuSB.GameServer.Server.Packet.Send.Login;
using MikuSB.GameServer.Server.Packet.Send.Misc;
using MikuSB.Proto;
using MikuSB.TcpSharp;
using MikuSB.Util;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.Packet.Recv.Login;

[Opcode(CmdIds.ReqLogin)]
public class HandlerReqLogin : Handler
{
    public override async Task OnHandle(Connection connection, byte[] data, ushort seqNo)
    {
        var req = ReqLogin.Parser.ParseFrom(data);
        var account = AccountData.GetAccountByUid(1);
        if (account == null)
        {
            AccountData.CreateAccount("MIKU", 0, "");
            account = AccountData.GetAccountByUid(1);
            if (account == null)
            {
                await connection.SendPacket(CmdIds.NtfLogout);
                return;
            }
        }
        if (!ResourceManager.IsLoaded)
            // resource manager not loaded, return
            return;
        var prev = Listener.GetActiveConnection(account.Uid);
        if (prev != null)
        {
            await connection.SendPacket(CmdIds.NtfLogout);
            prev.Stop();
        }

        connection.State = SessionStateEnum.WAITING_FOR_LOGIN;
        var pd = DatabaseHelper.GetInstance<PlayerGameData>(account.Uid);
        connection.Player = pd == null ? new PlayerInstance(account.Uid) : new PlayerInstance(pd);
        if (connection.Player.Data.EnsureDisplayName())
            DatabaseHelper.UpdateInstance(connection.Player.Data);

        connection.DebugFile = Path.Combine(ConfigManager.Config.Path.LogPath, "Debug/", $"{account.Uid}/",
            $"Debug-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.log");
        await connection.Player.OnEnterGame();
        connection.Player.Connection = connection;
        await connection.SendPacket(new PacketRspLogin(connection.Player!));
        await SendDebugLoginState(connection);

        await connection.Player.OnHeartBeat();
        await connection.SendPacket(new PacketNtfUpdateFriend(connection.Player!));
        ApplySavedGirlSkinTypes(connection.Player!);
        await connection.SendPacket(new PacketNtfCallScript(connection.Player!.InventoryManager.InventoryData));
        await SendGirlSkinTypeOnLogin(connection);
    }

    private static void ApplySavedGirlSkinTypes(PlayerInstance player)
    {
        var inventoryData = player.InventoryManager.InventoryData;
        inventoryData.SkinTypesBySkinId ??= [];
        var changed = false;

        foreach (var (skinId, skinType) in inventoryData.SkinTypesBySkinId.ToArray())
        {
            var clamped = GirlSkin_ChangeSkinType.ClampClientSkinType(skinType);
            if (clamped != skinType)
            {
                inventoryData.SkinTypesBySkinId[skinId] = clamped;
                changed = true;
            }

            var skinData = GirlSkin_ChangeSkinType.GetOrCreateSkinItem(player, skinId);
            if (skinData != null && skinData.SkinType != clamped)
            {
                skinData.SkinType = clamped;
                changed = true;
            }
        }

        if (changed)
            DatabaseHelper.SaveDatabaseType(inventoryData);
    }

    private static async Task SendGirlSkinTypeOnLogin(Connection connection)
    {
        var player = connection.Player;
        if (player == null)
            return;

        var inventoryData = player.InventoryManager.InventoryData;
        inventoryData.SkinTypesBySkinId ??= [];
        foreach (var (skinId, skinType) in inventoryData.SkinTypesBySkinId)
        {
            var clamped = GirlSkin_ChangeSkinType.ClampClientSkinType(skinType);
            var skinData = GirlSkin_ChangeSkinType.GetOrCreateSkinItem(player, skinId);
            var response = new JsonObject
            {
                ["nType"] = clamped,
                ["nSkinId"] = skinId
            };

            if (skinData == null)
            {
                await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString());
                continue;
            }

            await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString(), new NtfSyncPlayer
            {
                Items = { skinData.ToProto() }
            });
        }
    }

    private static async Task SendDebugLoginState(Connection connection)
    {
        var response = new JsonObject
        {
            ["IsDebug"] = ConfigManager.Config.ServerOption.EnableGmMenu
        };

        await CallGSRouter.SendScript(connection, "gm.notifylogin", response.ToJsonString());
    }
}
