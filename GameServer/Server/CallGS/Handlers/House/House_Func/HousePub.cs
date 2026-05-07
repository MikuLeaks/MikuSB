using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

// PubGameEnter — returns nSeed for client-side game initialization.
[HouseFunc("PubGameEnter")]
public class PubGameEnter : IHouseFuncHandler
{
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject
        {
            ["FuncName"] = "PubGameEnter",
            ["nSeed"] = Random.Next(1, 1_000_000_000),
            ["nModeType"] = 1,
            ["bIsGuide"] = false,
            ["bHasTry"] = false
        };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}

[HouseFunc("PubGameMulExit")]
public class PubGameMulExit : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameMulExit" };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}

// PubGameSettlement — nAddExp=0 on private server.
[HouseFunc("PubGameSettlement")]
public class PubGameSettlement : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameSettlement", ["nAddExp"] = 0 };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}

[HouseFunc("PubGameGetReward")]
public class PubGameGetReward : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameGetReward" };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}

[HouseFunc("PubGameGetAchReward")]
public class PubGameGetAchReward : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameGetAchReward" };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}

[HouseFunc("PubGameAchievementFinish")]
public class PubGameAchievementFinish : IHouseFuncHandler
{
    public async Task Handle(Connection connection, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameAchievementFinish" };
        await CallGSRouter.SendScript(connection, "House_Request", rsp.ToJsonString());
    }
}
