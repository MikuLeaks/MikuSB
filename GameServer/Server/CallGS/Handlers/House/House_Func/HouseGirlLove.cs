using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("SetPlayerRingInfo")]
public class SetPlayerRingInfo : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var ringId = HouseJson.NumField(root, "RingId");
        var ringPos = HouseJson.NumField(root, "RingPos");
        var ringOffset = HouseJson.NumField(root, "RingOffset");
        var sync = new NtfSyncPlayer();
        if (ringPos is >= 1 and <= 10 && ringOffset is >= 0 and <= 2)
        {
            var sid = HouseAttr.PlayerRingInfoSidBase + (uint)ringPos;
            var prev = HouseAttr.Read(connection.Player!, sid);
            var shift = ringOffset * 10;
            var mask = ~(0x3ffu << shift);
            var next = (prev & mask) | (((uint)ringId & 0x3ffu) << shift);
            await HouseAttr.SetAsync(connection, sid, next, sync, deleteIfZero: true, sendImmediate: true);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("ReadGirlLoveStory")]
public class ReadGirlLoveStory : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var girlId = HouseJson.NumField(root, "GirlId");
        var index = HouseJson.NumField(root, "Index");
        var sync = new NtfSyncPlayer();
        if (girlId > 0 && index is >= 0 and <= 30)
        {
            var sid = (uint)(girlId * 50 + 2);
            var prev = HouseAttr.Read(connection.Player!, sid);
            var next = prev | (1u << index);
            await HouseAttr.SetAsync(connection, sid, next, sync, sendImmediate: true);
        }

        var rsp = new JsonObject
        {
            ["GirlId"] = girlId,
            ["Index"] = index,
            ["tbReward"] = new JsonArray(),
            ["FuncName"] = "ReadGirlLoveStorySuccess"
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp), sync);
    }
}
