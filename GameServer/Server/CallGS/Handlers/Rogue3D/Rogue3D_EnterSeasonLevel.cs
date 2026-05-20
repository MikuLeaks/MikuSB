using MikuSB.Data;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Enters the Rogue3D season level. Returns a random seed used by the client for map generation.
// Persists SeasonGameplayId (sid=1006) and SeasonEnterFlag (sid=1008) as player attributes (GroupId=124).
// param: {"nDiffId", "nTeamID", "tbTeam", "tbBuffList", "tbLog"}
// Response: {"nSeed": int} on success, {"sErr": "key"} on failure
[CallGSApi("Rogue3D_EnterSeasonLevel")]
public class Rogue3D_EnterSeasonLevel : ICallGSHandler
{
    private const uint GroupId = 124;
    private const uint SeasonGameplayIdSid = 1006;
    private const uint SeasonEnterFlagSid = 1008;
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<EnterSeasonLevelParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "Rogue3D_EnterSeasonLevel", "{\"nSeed\":0}");
            return;
        }

        if (!GameData.Rogue3DDifficultData.TryGetValue(req.DiffId, out var cfg) || cfg.GameplayGroup.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "Rogue3D_EnterSeasonLevel", "{\"sErr\":\"rogue3.massage_gameProcessError\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        SetAttr(player, SeasonGameplayIdSid, cfg.GameplayGroup[0], sync);
        SetAttr(player, SeasonEnterFlagSid, 1, sync);

        var seed = Random.Next(1, 1_000_000_000);
        await CallGSRouter.SendScript(connection, "Rogue3D_EnterSeasonLevel", $"{{\"nSeed\":{seed}}}", sync);
    }

    private static void SetAttr(PlayerInstance player, uint sid, uint val, NtfSyncPlayer sync)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr == null)
        {
            attr = new PlayerAttr { Gid = GroupId, Sid = sid };
            player.Data.Attrs.Add(attr);
        }

        if (attr.Val == val)
        {
            return;
        }

        attr.Val = val;
        sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = val;
        sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = val;
    }
}

internal sealed class EnterSeasonLevelParam
{
    [JsonPropertyName("nDiffId")]
    public uint DiffId { get; set; }
}
