using MikuSB.Database.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Rogue3D;

// Selects the Rogue3D season talent and persists it as player attribute (GroupId=124, TalentId=1007).
// param: {"nTalentId": int}
// Response: {} on success, {"sErr": "key"} on failure
[CallGSApi("Rogue3D_SelectSeasonTalent")]
public class Rogue3D_SelectSeasonTalent : ICallGSHandler
{
    private const uint GroupId = 124;
    private const uint SeasonTalentIdSid = 1007;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<SelectSeasonTalentParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "Rogue3D_SelectSeasonTalent", "{}");
            return;
        }

        var player = connection.Player!;
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == SeasonTalentIdSid);
        if (attr == null)
        {
            attr = new PlayerAttr { Gid = GroupId, Sid = SeasonTalentIdSid };
            player.Data.Attrs.Add(attr);
        }
        attr.Val = req.TalentId;

        var sync = new NtfSyncPlayer();
        sync.Custom[player.ToPackedAttrKey(GroupId, SeasonTalentIdSid)] = attr.Val;
        sync.Custom[player.ToShiftedAttrKey(GroupId, SeasonTalentIdSid)] = attr.Val;

        await CallGSRouter.SendScript(connection, "Rogue3D_SelectSeasonTalent", "{}", sync);
    }
}

internal sealed class SelectSeasonTalentParam
{
    [JsonPropertyName("nTalentId")]
    public uint TalentId { get; set; }
}
