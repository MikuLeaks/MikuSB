using MikuSB.Proto;
using System.Text.Json;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("Weapon_ReplacePart")]
public class Weapon_ReplacePart : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<WeaponPartReplaceParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "Weapon_ReplacePart", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var weaponData = player.InventoryManager.GetWeaponItem(req.Id);
        if (weaponData == null)
        {
            await CallGSRouter.SendScript(connection, "Weapon_ReplacePart", "{}");
            return;
        }

        uint partId = 0;
        if (req.PartId != -1)
        {
            var partData = player.InventoryManager.GetNormalItem((uint)req.PartId);
            if (partData != null) partId = partData.UniqueId;
        }

        weaponData.PartSlots[req.Type] = partId;
        var sync = new NtfSyncPlayer
        {
            Items = { weaponData.ToProto() }
        };
        await CallGSRouter.SendScript(connection, "Weapon_ReplacePart", "null", sync);
    }
}

internal sealed class WeaponPartReplaceParam
{
    public int PartId { get; set; }
    public uint Type { get; set; }
    public uint Id { get; set; }
}