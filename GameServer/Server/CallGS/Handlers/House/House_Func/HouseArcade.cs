using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("ArcadeGameEnterMainUI")]
public class ArcadeGameEnterMainUI : IHouseFuncHandler
{
    private static readonly int[] ArcadeUnlockedGirlList = [1, 5, 13, 23, 10];

    public async Task Handle(Connection connection, string param)
    {
        var unlockGirls = new JsonArray();
        foreach (var girlId in ArcadeUnlockedGirlList)
            unlockGirls.Add(girlId);

        var rsp = new JsonObject
        {
            ["tbUnlockGirlList"] = unlockGirls,
            ["FuncName"] = "ArcadeGameEnterMainUI"
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp));
    }
}

[HouseFunc("ArcadeGameEnter")]
public class ArcadeGameEnter : IHouseFuncHandler
{
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject
        {
            ["FuncName"] = "ArcadeGameEnter",
            ["nSeed"] = Random.Next(1, 1_000_000_000)
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp));
    }
}

[HouseFunc("ArcadeGameSettlement")]
public class ArcadeGameSettlement : IHouseFuncHandler
{
    private const uint HouseArcadeInfoStart = 18000;
    private const uint ArcadeAttrExpOffset = 2;
    private const uint ArcadeAttrEndlessScoreOffset = 4;
    private const uint ArcadeAttrGirlEndlessModeStateOffset = 5;
    private const uint ArcadeAttrGirlNormalModeStateOffset = 10;
    private const uint ArcadeAttrGirlNormalModeStateEndOffset = 35;
    private const uint ArcadeAttrConditionValStartOffset = 36;
    private const uint ArcadeAttrConditionValEndOffset = 55;
    private const uint ArcadeAttrPropUseStartOffset = 56;
    private const uint ArcadeAttrPropUseEndOffset = 250;
    private const int ArcadeGameModeNormal = 1;
    private const int ArcadeGameModeEndless = 2;

    public async Task Handle(Connection connection, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return;

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        var modeType = HouseJson.NumField(root, "nModeType");
        var finishRound = Math.Max(0, HouseJson.NumField(root, "nFinishRound"));
        var maxScore = Math.Max(0, HouseJson.NumField(root, "nMaxScore"));

        if (root["tbConditionVal"] is JsonArray conditions)
        {
            foreach (var conditionNode in conditions)
            {
                if (conditionNode is not JsonArray condition || condition.Count < 2) continue;
                var id = HouseJson.ToInt(condition[0]);
                var value = Math.Max(0, HouseJson.ToInt(condition[1]));
                if (id < 0) continue;
                var sid = HouseArcadeInfoStart + ArcadeAttrConditionValStartOffset + (uint)id;
                if (sid > HouseArcadeInfoStart + ArcadeAttrConditionValEndOffset) continue;
                var prev = HouseAttr.Read(player, sid);
                if ((uint)value > prev)
                    await HouseAttr.SetAsync(connection, sid, (uint)value, sync);
            }
        }

        if (root["tbPropUse"] is JsonArray propUses)
        {
            var slotStart = HouseArcadeInfoStart + ArcadeAttrPropUseStartOffset;
            var slotEnd = HouseArcadeInfoStart + ArcadeAttrPropUseEndOffset;
            var slotState = new Dictionary<uint, ArcadePropUseSlot>();
            var emptySlots = new Queue<uint>();
            for (uint sid = slotStart; sid <= slotEnd; sid++)
            {
                var value = HouseAttr.Read(player, sid);
                if (value == 0)
                {
                    emptySlots.Enqueue(sid);
                    continue;
                }

                slotState[sid] = new ArcadePropUseSlot
                {
                    Type = (byte)(value & 0xff),
                    Id = (byte)((value >> 8) & 0xff),
                    Count = (ushort)((value >> 16) & 0xffff)
                };
            }

            foreach (var propUseNode in propUses)
            {
                if (propUseNode is not JsonArray propUse || propUse.Count < 3) continue;
                var type = HouseJson.ToInt(propUse[0]) & 0xff;
                var id = HouseJson.ToInt(propUse[1]) & 0xff;
                var count = HouseJson.ToInt(propUse[2]);
                if (count <= 0) continue;

                uint? foundSid = null;
                foreach (var (sid, slot) in slotState)
                {
                    if (slot.Type == type && slot.Id == id)
                    {
                        foundSid = sid;
                        break;
                    }
                }

                if (foundSid != null)
                {
                    var slot = slotState[foundSid.Value];
                    slot.Count = (ushort)Math.Min(0xffff, slot.Count + count);
                    slotState[foundSid.Value] = slot;
                    await HouseAttr.SetAsync(connection, foundSid.Value, HouseAttr.PackArcadePropUse(slot.Type, slot.Id, slot.Count), sync);
                }
                else if (emptySlots.Count > 0)
                {
                    var sid = emptySlots.Dequeue();
                    var packedCount = (ushort)Math.Min(0xffff, count);
                    slotState[sid] = new ArcadePropUseSlot { Type = type, Id = id, Count = packedCount };
                    await HouseAttr.SetAsync(connection, sid, HouseAttr.PackArcadePropUse(type, id, packedCount), sync);
                }
            }
        }

        var girlList = root["tbGirlDataList"] as JsonArray;
        var nAddExp = 0;
        if (modeType == ArcadeGameModeNormal)
        {
            if (girlList != null)
            {
                foreach (var girlNode in girlList)
                {
                    var girlId = HouseJson.ToInt(girlNode);
                    if (girlId <= 0) continue;
                    var sid = HouseArcadeInfoStart + ArcadeAttrGirlNormalModeStateOffset + (uint)girlId;
                    if (sid > HouseArcadeInfoStart + ArcadeAttrGirlNormalModeStateEndOffset) continue;
                    var prev = HouseAttr.Read(player, sid);
                    await HouseAttr.SetAsync(connection, sid, prev + (uint)finishRound, sync);
                }
            }
            nAddExp = finishRound * 10;
        }
        else if (modeType == ArcadeGameModeEndless)
        {
            var scoreSid = HouseArcadeInfoStart + ArcadeAttrEndlessScoreOffset;
            var prevScore = HouseAttr.Read(player, scoreSid);
            if ((uint)maxScore > prevScore)
                await HouseAttr.SetAsync(connection, scoreSid, (uint)maxScore, sync);

            var bitsSid = HouseArcadeInfoStart + ArcadeAttrGirlEndlessModeStateOffset;
            var bits = HouseAttr.Read(player, bitsSid);
            var originalBits = bits;
            if (girlList != null)
            {
                foreach (var girlNode in girlList)
                {
                    var girlId = HouseJson.ToInt(girlNode);
                    if (girlId is > 0 and < 31)
                        bits |= 1u << girlId;
                }
            }
            if (bits != originalBits)
                await HouseAttr.SetAsync(connection, bitsSid, bits, sync);

            nAddExp = maxScore / 10;
        }

        if (nAddExp > 0)
        {
            var expSid = HouseArcadeInfoStart + ArcadeAttrExpOffset;
            var prevExp = HouseAttr.Read(player, expSid);
            await HouseAttr.SetAsync(connection, expSid, prevExp + (uint)nAddExp, sync);
        }

        var rsp = new JsonObject
        {
            ["nAddExp"] = nAddExp,
            ["FuncName"] = "ArcadeGameSettlement"
        };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp), sync);
    }

    private struct ArcadePropUseSlot
    {
        public int Type { get; set; }
        public int Id { get; set; }
        public ushort Count { get; set; }
    }
}

[HouseFunc("ArcadeGameLogSettlement")]
public class ArcadeGameLogSettlement : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "ArcadeGameLogSettlement" };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp));
    }
}

[HouseFunc("ArcadeGameGetLevelReward")]
public class ArcadeGameGetLevelReward : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "ArcadeGameGetLevelReward" };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp));
    }
}

[HouseFunc("ArcadeGameGetAchReward")]
public class ArcadeGameGetAchReward : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "ArcadeGameGetAchReward" };
        await CallGSRouter.SendScript(connection, "House_Request", HouseRequestScript.Success(rsp));
    }
}
