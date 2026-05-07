using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

internal static class HouseRequestScript
{
    internal static readonly Dictionary<string, string> FuncNameMap = new()
    {
        ["ChangeGirlBeachSuitId"] = "ChangeGirlBeachSuitIdSuccess",
        ["ChangeNpcSuitByAreaId"] = "ChangeNpcSuitByAreaIdRsp",
        ["SetGroupFurIndex"] = "SetGroupFurSuccess",
        ["RouletteEnd"] = "RouletteEndRsp",
        ["UnLockWaterGunGameItem"] = "UnLockWaterGunGameItemRsp",
        ["ChangeNpcSuit"] = "ChangeNpcSuitSuccess",
        ["GiveGiftToArea"] = "GiveGiftToAreaSuccess",
        ["BuyRefreshMapTimes"] = "BuyRefreshMapTimesSuccess",
        ["CompleteHousePuzzleTask"] = "CompleteHousePuzzleTaskSuccess",
        ["CompleteStarWishTask"] = "CompleteStarWishTaskSuccess",
        ["SetPlayerRingInfo"] = "SetPlayerRingInfoSuccess",
        ["ReadGirlLoveStory"] = "ReadGirlLoveStorySuccess",
        ["GirlRoomChange"] = "GirlRoomChangeSuccess",
        ["GirlRegister"] = "GirlRegisterSuccess"
    };

    internal static string Synthesize(JsonObject request)
    {
        var funcName = request["FuncName"]?.GetValue<string>();
        var response = CreateSuccessObject();
        foreach (var (key, value) in request)
        {
            if (key is "FuncName" or "tblog")
                continue;

            response[key] = value?.DeepClone();
        }

        if (!string.IsNullOrEmpty(funcName))
            response["FuncName"] = FuncNameMap.TryGetValue(funcName, out var renamed) ? renamed : funcName;

        return response.ToJsonString();
    }

    internal static string Success(JsonObject? extra = null) => CreateSuccessObject(extra).ToJsonString();

    internal static JsonObject CreateSuccessObject(JsonObject? extra = null)
    {
        var response = new JsonObject
        {
            ["bSuccess"] = true,
            ["nResult"] = 0
        };

        if (extra != null)
        {
            foreach (var (key, value) in extra)
                response[key] = value?.DeepClone();
        }

        return response;
    }
}

internal static class HouseJson
{
    internal static JsonObject? ParseObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    internal static int NumField(JsonObject? obj, string name) => ToInt(obj?[name]);

    internal static int ToInt(JsonNode? node)
    {
        if (node == null) return 0;
        try
        {
            if (node is JsonValue value)
            {
                if (value.TryGetValue<int>(out var i)) return i;
                if (value.TryGetValue<uint>(out var u)) return unchecked((int)u);
                if (value.TryGetValue<long>(out var l)) return unchecked((int)l);
                if (value.TryGetValue<string>(out var s) && int.TryParse(s, out var parsed)) return parsed;
            }
        }
        catch
        {
        }
        return 0;
    }
}

internal static class HouseAttr
{
    internal const uint Gid = 101;
    internal const uint BedroomStartSid = 2550;
    internal const uint BedroomRegisteredNoRoom = 100;
    internal const uint PlayerRingInfoSidBase = 3174;

    internal static uint Read(PlayerInstance player, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == Gid && x.Sid == sid);
        return attr?.Val ?? 0;
    }

    internal static async Task SetAsync(
        Connection connection,
        uint sid,
        uint value,
        NtfSyncPlayer sync,
        bool deleteIfZero = false,
        bool sendImmediate = false)
    {
        var player = connection.Player!;
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == Gid && x.Sid == sid);
        if (value == 0 && deleteIfZero)
        {
            if (attr != null)
                player.Data.Attrs.Remove(attr);
        }
        else
        {
            if (attr == null)
            {
                attr = new PlayerAttr { Gid = Gid, Sid = sid };
                player.Data.Attrs.Add(attr);
            }
            attr.Val = value;
        }

        sync.Custom[player.ToPackedAttrKey(Gid, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(Gid, sid)] = value;
        if (sendImmediate)
            await player.SendPacket(CmdIds.NtfSetAttr, new NtfSetAttr { Gid = Gid, Sid = sid, Val = value });
    }

    internal static uint BedroomSlotSid(int roomId) => BedroomStartSid + (uint)roomId;

    internal static uint GirlRoomNumSid(int girlId) => (uint)(girlId * 50 + 1);

    internal static async Task MoveGirlIntoRoomAsync(Connection connection, int girlId, int roomId, NtfSyncPlayer sync)
    {
        var player = connection.Player!;
        var oldRoom = (int)Read(player, GirlRoomNumSid(girlId));
        var targetSlotSid = BedroomSlotSid(roomId);
        var oldSlotSid = oldRoom is >= 1 and < 100 ? BedroomSlotSid(oldRoom) : 0;
        var occupant = Read(player, targetSlotSid);

        await SetAsync(connection, targetSlotSid, (uint)girlId, sync);
        await SetAsync(connection, GirlRoomNumSid(girlId), (uint)roomId, sync);

        if (oldRoom is >= 1 and < 100 && oldRoom != roomId)
            await SetAsync(connection, oldSlotSid, 0, sync);

        if (occupant > 0 && occupant != (uint)girlId)
        {
            if (oldRoom is >= 1 and < 100 && oldRoom != roomId)
            {
                await SetAsync(connection, oldSlotSid, occupant, sync);
                await SetAsync(connection, GirlRoomNumSid((int)occupant), (uint)oldRoom, sync);
            }
            else
            {
                await SetAsync(connection, GirlRoomNumSid((int)occupant), BedroomRegisteredNoRoom, sync);
            }
        }
    }

    internal static uint PackArcadePropUse(int type, int id, ushort count) =>
        (((uint)count & 0xffffu) << 16) | (((uint)id & 0xffu) << 8) | ((uint)type & 0xffu);
}
