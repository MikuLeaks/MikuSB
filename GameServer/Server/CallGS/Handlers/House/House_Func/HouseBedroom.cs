using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("GirlRegister")]
public class GirlRegister : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var girlId = HouseJson.NumField(root, "GirlId");
        var sync = new NtfSyncPlayer();
        if (girlId > 0)
            await HouseAttr.SetAsync(connection, HouseAttr.GirlRoomNumSid(girlId), HouseAttr.BedroomRegisteredNoRoom, sync);

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("SetBedroomGirlId")]
public class SetBedroomGirlId : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var bedroomId = HouseJson.NumField(root, "BedroomId");
        var girlId = HouseJson.NumField(root, "GirlId");
        var sync = new NtfSyncPlayer();
        if (bedroomId > 0 && girlId > 0)
            await HouseAttr.MoveGirlIntoRoomAsync(connection, girlId, bedroomId, sync);

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("GirlRoomChange")]
public class GirlRoomChange : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var girlId = HouseJson.NumField(root, "GirlId");
        var roomId = HouseJson.NumField(root, "RoomId");
        var oldRoomId = girlId > 0 ? (int)HouseAttr.Read(connection.Player!, HouseAttr.GirlRoomNumSid(girlId)) : 0;
        var sync = new NtfSyncPlayer();
        if (girlId > 0 && roomId > 0)
            await HouseAttr.MoveGirlIntoRoomAsync(connection, girlId, roomId, sync);

        var rsp = new JsonObject
        {
            ["FuncName"] = "GirlRoomChangeSuccess",
            ["GirlId"] = girlId,
            ["OldRoomId"] = oldRoomId,
            ["NewRoomId"] = roomId
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp), sync);
    }
}

[HouseFunc("GirlLeaveRoom")]
public class GirlLeaveRoom : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var bedroomId = HouseJson.NumField(root, "BedroomId");
        var girlId = HouseJson.NumField(root, "GirlId");
        var sync = new NtfSyncPlayer();
        if (bedroomId > 0 && girlId > 0)
        {
            await HouseAttr.SetAsync(connection, HouseAttr.BedroomSlotSid(bedroomId), 0, sync);
            await HouseAttr.SetAsync(connection, HouseAttr.GirlRoomNumSid(girlId), HouseAttr.BedroomRegisteredNoRoom, sync);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("ExchangeRoomGirl")]
public class ExchangeRoomGirl : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var roomId1 = HouseJson.NumField(root, "RoomId1");
        var roomId2 = HouseJson.NumField(root, "RoomId2");
        var sync = new NtfSyncPlayer();
        if (roomId1 > 0 && roomId2 > 0 && roomId1 != roomId2)
        {
            var slot1 = HouseAttr.BedroomSlotSid(roomId1);
            var slot2 = HouseAttr.BedroomSlotSid(roomId2);
            var girl1 = HouseAttr.Read(connection.Player!, slot1);
            var girl2 = HouseAttr.Read(connection.Player!, slot2);
            await HouseAttr.SetAsync(connection, slot1, girl2, sync);
            await HouseAttr.SetAsync(connection, slot2, girl1, sync);
            if (girl1 > 0) await HouseAttr.SetAsync(connection, HouseAttr.GirlRoomNumSid((int)girl1), (uint)roomId2, sync);
            if (girl2 > 0) await HouseAttr.SetAsync(connection, HouseAttr.GirlRoomNumSid((int)girl2), (uint)roomId1, sync);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}
