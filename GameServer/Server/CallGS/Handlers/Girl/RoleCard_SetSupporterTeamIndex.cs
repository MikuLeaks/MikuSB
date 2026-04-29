using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("RoleCard_SetSupporterTeamIndex")]
public class RoleCard_SetSupporterTeamIndex : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<SetSupporterTeamIndexParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "RoleCard_SetSupporterTeamIndex", "{\"err\":\"error.BadParam\"}");
            return;
        }
        var player = connection.Player!;
        var cardData = player.CharacterManager.GetCharacterByGUID(req.CardId);
        if (cardData == null) return;

        cardData.SupportTeamIndex = req.Index;
        var sync = new NtfSyncPlayer
        {
            Items = { cardData.ToProto() }
        };
        await CallGSRouter.SendScript(connection, "RoleCard_SetSupporterTeamIndex", "null", sync);
    }
}

internal sealed class SetSupporterTeamIndexParam
{
    [JsonPropertyName("Id")]
    public uint CardId { get; set; }
    public uint Index { get; set; }
}
