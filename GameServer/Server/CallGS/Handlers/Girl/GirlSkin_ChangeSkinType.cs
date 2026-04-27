using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlSkin_ChangeSkinType")]
public class GirlSkin_ChangeSkinType : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<ChangeSkinTypeParam>(param);
        var response = new JsonObject
        {
            ["nType"] = req?.Type ?? 1,
            ["nSkinId"] = req?.SkinId
        };
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString());
            return;
        }
        
        var player = connection.Player!;
        var skinData = player.InventoryManager.GetSkinItem(req.SkinId);
        if (skinData == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString());
            return;
        }

        skinData.SkinType = req.Type;
        var sync = new NtfSyncPlayer
        {
            Items = { skinData.ToProto() }
        };

        await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString(), sync);
    }
}

internal sealed class ChangeSkinTypeParam
{
    [JsonPropertyName("nType")]
    public uint Type { get; set; }

    [JsonPropertyName("nSkinId")]
    public uint SkinId { get; set; }
}
