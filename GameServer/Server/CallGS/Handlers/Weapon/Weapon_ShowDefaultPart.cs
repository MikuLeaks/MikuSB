using MikuSB.Enums.Item;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("Weapon_ShowDefaultPart")]
public class Weapon_ShowDefaultPart : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<WeaponShowDefaultPartParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "Weapon_ShowDefaultPart", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var weaponData = player.InventoryManager.GetWeaponItem(req.Id);
        if (weaponData == null)
        {
            await CallGSRouter.SendScript(connection, "Weapon_ShowDefaultPart", "{}");
            return;
        }

        if (req.Flag == 1) weaponData.Flag = ItemFlagEnum.FLAG_WEAPON_DEFAULT;
        else weaponData.Flag = ItemFlagEnum.FLAG_READED;

        var sync = new NtfSyncPlayer
        {
            Items = { weaponData.ToProto() }
        };
        await CallGSRouter.SendScript(connection, "Weapon_ShowDefaultPart", "null", sync);
    }
}

internal sealed class WeaponShowDefaultPartParam
{
    [JsonPropertyName("nFlag")] public int Flag { get; set; }
    public uint Id { get; set; }
}