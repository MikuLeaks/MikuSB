using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("ChangeNpcSuit")]
public class ChangeNpcSuit : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var npcId = HouseJson.NumField(root, "NpcId");
        var suitId = HouseJson.NumField(root, "SuitId");
        var sync = new NtfSyncPlayer();
        await HouseAttr.SetAsync(connection, (uint)(npcId * 50 + 7), (uint)Math.Max(0, suitId), sync, sendImmediate: true);

        var rsp = new JsonObject
        {
            ["NpcId"] = npcId,
            ["SuitId"] = suitId,
            ["npcSuit"] = new JsonObject
            {
                ["NpcId"] = npcId,
                ["SuitId"] = suitId
            },
            ["FuncName"] = "ChangeNpcSuitSuccess"
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp), sync);
    }
}

[HouseFunc("ChangeNpcSuitByAreaId")]
public class ChangeNpcSuitByAreaId : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var npcId = HouseJson.NumField(root, "NpcId");
        var areaId = HouseJson.NumField(root, "AreaId");
        var suitId = HouseJson.NumField(root, "SuitId");
        var sync = new NtfSyncPlayer();

        if (npcId > 0 && areaId > 0)
        {
            uint[] slotSids = Enumerable.Range(24, 6).Select(i => (uint)(npcId * 50 + i)).ToArray();
            uint? chosenSid = null;
            foreach (var sid in slotSids)
            {
                if ((HouseAttr.Read(connection.Player!, sid) & 0xffffu) == (uint)areaId)
                {
                    chosenSid = sid;
                    break;
                }
            }

            if (chosenSid == null)
            {
                foreach (var sid in slotSids)
                {
                    if (HouseAttr.Read(connection.Player!, sid) == 0)
                    {
                        chosenSid = sid;
                        break;
                    }
                }
            }

            chosenSid ??= slotSids[0];
            var packed = (((uint)suitId & 0xffffu) << 16) | ((uint)areaId & 0xffffu);
            await HouseAttr.SetAsync(connection, chosenSid.Value, packed, sync, sendImmediate: true);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("ChangeGirlBeachSuitId")]
public class ChangeGirlBeachSuitId : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var npcId = HouseJson.NumField(root, "NpcId");
        var suitId = HouseJson.NumField(root, "SuitId");
        var sync = new NtfSyncPlayer();
        if (npcId > 0)
            await HouseAttr.SetAsync(connection, (uint)(npcId * 50 + 8), (uint)Math.Max(0, suitId), sync, sendImmediate: true);

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}
