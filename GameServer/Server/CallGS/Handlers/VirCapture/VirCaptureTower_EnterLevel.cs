using MikuSB.Data;
using MikuSB.Database.Player;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureTower_EnterLevel")]
public class VirCaptureTower_EnterLevel : ICallGSHandler
{
    private const uint LaunchPassGroupId = 22;
    private const uint VirCaptureGroupId = 128;
    private const uint VirCaptureLevelSid = 3;
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureTowerEnterLevelParam>(param);
        if (req == null || req.LevelId <= 0 || req.TeamId <= 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureTower_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (!GameData.VirCaptureTowerData.TryGetValue((uint)req.LevelId, out var levelCfg))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureTower_EnterLevel", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        if (!CheckConditions(player.Data, levelCfg.Condition))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureTower_EnterLevel", "{\"sErr\":\"tip.LevelLocked\"}");
            return;
        }

        await CallGSRouter.SendScript(connection, "VirCaptureTower_EnterLevel", $"{{\"nSeed\":{Random.Next(1, 1_000_000_000)}}}");
    }

    private static bool CheckConditions(PlayerGameData data, IReadOnlyDictionary<int, uint> conditions)
    {
        foreach (var (key, value) in conditions)
        {
            switch (key)
            {
                case 1:
                    if (data.Level < value)
                        return false;
                    break;
                case 2:
                {
                    var pass = data.Attrs.FirstOrDefault(x => x.Gid == LaunchPassGroupId && x.Sid == value)?.Val ?? 0;
                    if (pass == 0)
                        return false;
                    break;
                }
                case 20:
                {
                    var virLevel = data.Attrs.FirstOrDefault(x => x.Gid == VirCaptureGroupId && x.Sid == VirCaptureLevelSid)?.Val ?? 0;
                    if (virLevel < value)
                        return false;
                    break;
                }
            }
        }

        return true;
    }
}

internal sealed class VirCaptureTowerEnterLevelParam
{
    [JsonPropertyName("nID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nTeamID")]
    public int TeamId { get; set; }
}
