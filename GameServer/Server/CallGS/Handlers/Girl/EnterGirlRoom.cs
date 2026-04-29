using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("EnterGirlRoom")]
public class EnterGirlRoom : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<EnterGirlRoomParam>(param);
        var response = new JsonObject
        {
            ["nCardId"] = 0,
            ["nSkinId"] = 0,
            ["bOpen"] = false
        };
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "EnterGirlRoom", response.ToJsonString());
            return;
        }

        response["nCardId"] = req.CardId;
        response["nSkinId"] = req.SkinId;
        response["bOpen"] = true;
        await CallGSRouter.SendScript(connection, "EnterGirlRoom", response.ToJsonString());
    }
}

internal sealed class EnterGirlRoomParam
{
    [JsonPropertyName("nSkinId")]
    public int SkinId { get; set; }

    [JsonPropertyName("nCardId")]
    public uint CardId { get; set; }
}