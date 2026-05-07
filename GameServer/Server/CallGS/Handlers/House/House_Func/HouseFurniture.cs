using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("SetGroupFurIndex")]
public class SetGroupFurIndex : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var areaId = HouseJson.NumField(root, "AreaId");
        var groupId = HouseJson.NumField(root, "GroupId");
        var index = HouseJson.NumField(root, "Index");
        var sync = new NtfSyncPlayer();
        if (areaId > 0 && groupId is >= 1 and <= 10)
        {
            var sid = (uint)(areaId * 50 + 20);
            var prev = HouseAttr.Read(connection.Player!, sid);
            var shift = (groupId - 1) * 3;
            var mask = ~(0b111u << shift);
            var next = (prev & mask) | (((uint)index & 0b111u) << shift);
            await HouseAttr.SetAsync(connection, sid, next, sync, sendImmediate: true);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}
