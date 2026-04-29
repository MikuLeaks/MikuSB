using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlWeaponSkin_Change")]
public class GirlWeaponSkin_Change : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<GirlWeaponSkinParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "GirlWeaponSkin_Change", "{\"err\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var cardData = player.CharacterManager.GetCharacterByGUID(req.CardId);
        if (cardData == null) return;
        var skinData = player.InventoryManager.GetNormalItem(req.SkinId);
        if (skinData == null)
        {
            await CallGSRouter.SendScript(connection, "GirlWeaponSkin_Change", "{\"err\":\"error.BadParam\"}");
            return;
        }

        cardData.WeaponSkinId = req.SkinId;
        var sync = new NtfSyncPlayer
        {
            Items = { cardData.ToProto() }
        };

        await CallGSRouter.SendScript(connection, "GirlWeaponSkin_Change", "null", sync);
    }
}

internal sealed class GirlWeaponSkinParam
{
    [JsonPropertyName("nCardId")]
    public uint CardId { get; set; }

    [JsonPropertyName("nSkinId")]
    public uint SkinId { get; set; }
}
