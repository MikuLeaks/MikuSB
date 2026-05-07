using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("UnLockWaterGunGameItem")]
public class UnLockWaterGunGameItem : IHouseFuncHandler
{
    private const uint HouseHotSpringInfoStart = 15000;
    private const uint WaterGunItemCollectBegin = 17;

    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var sync = new NtfSyncPlayer();
        if (root["tbItem"] is JsonArray items)
        {
            var touched = new Dictionary<uint, uint>();
            foreach (var node in items)
            {
                var itemId = HouseJson.ToInt(node);
                if (itemId is < 0 or > 60) continue;
                var offset = itemId / 30;
                var bit = itemId % 30;
                var sid = HouseHotSpringInfoStart + WaterGunItemCollectBegin + (uint)offset;
                var cur = touched.TryGetValue(sid, out var existing)
                    ? existing
                    : HouseAttr.Read(connection.Player!, sid);
                touched[sid] = cur | (1u << bit);
            }

            foreach (var (sid, value) in touched)
                await HouseAttr.SetAsync(connection, sid, value, sync);
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("RouletteEnd")]
public class RouletteEnd : IHouseFuncHandler
{
    private const uint HouseHotSpringInfoStart = 15000;
    private const uint MaxRoundsInWaterGun = 16;
    private const uint HasCompleteGameGuide = 19;
    private const uint HotSpringGirlInfoStart = 15100;
    private const uint HotSpringGirlAttrCount = 10;
    private const uint HotSpringGirlHasCompleteStory = 1;
    private const int RoulettePlayModePlot = 1;
    private const int RoulettePlayModeEndless = 2;
    private const int RouletteEndReasonSuccess = 0;
    private const int RouletteTutorialGirlId = 5;

    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var modeType = HouseJson.NumField(root, "ModeType");
        var girlId = HouseJson.NumField(root, "GirlId");
        var isSuccess = HouseJson.NumField(root, "IsSuccess");
        var rounds = HouseJson.NumField(root, "Rounds");
        var sync = new NtfSyncPlayer();

        if (isSuccess == RouletteEndReasonSuccess)
        {
            if (modeType == RoulettePlayModePlot && girlId > 0)
            {
                await HouseAttr.SetAsync(
                    connection,
                    HotSpringGirlInfoStart + (uint)(girlId * (int)HotSpringGirlAttrCount) + HotSpringGirlHasCompleteStory,
                    1,
                    sync);
                if (girlId == RouletteTutorialGirlId)
                    await HouseAttr.SetAsync(connection, HouseHotSpringInfoStart + HasCompleteGameGuide, 1, sync);
            }
            else if (modeType == RoulettePlayModeEndless && rounds > 0)
            {
                var sid = HouseHotSpringInfoStart + MaxRoundsInWaterGun;
                var prev = HouseAttr.Read(connection.Player!, sid);
                if ((uint)rounds > prev)
                    await HouseAttr.SetAsync(connection, sid, (uint)rounds, sync);
            }
        }

        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Synthesize(root), sync);
    }
}
