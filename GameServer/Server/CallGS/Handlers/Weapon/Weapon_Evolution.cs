using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Weapon;

// s2c: function(sErr) — send "null" on success
// Id      = target weapon UniqueId
// nItemId = material item UniqueId (weapon or supply item to consume)
[CallGSApi("Weapon_Evolution")]
public class Weapon_Evolution : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<WeaponEvolutionParam>(param);
        if (req == null || req.WeaponId == 0 || req.MaterialId == 0)
        {
            await CallGSRouter.SendScript(connection, "Weapon_Evolution", "\"error.BadParam\"");
            return;
        }

        var weapon = player.InventoryManager.InventoryData.Weapons.GetValueOrDefault((uint)req.WeaponId);
        if (weapon == null)
        {
            await CallGSRouter.SendScript(connection, "Weapon_Evolution", "\"error.BadParam\"");
            return;
        }

        var syncItems = new List<Item>();

        // Material can be a weapon or a regular item
        if (player.InventoryManager.InventoryData.Weapons.TryGetValue((uint)req.MaterialId, out var matWeapon))
        {
            player.InventoryManager.InventoryData.Weapons.Remove((uint)req.MaterialId);
            var removed = matWeapon.ToProto();
            removed.Count = 0;
            syncItems.Add(removed);
        }
        else if (player.InventoryManager.InventoryData.Items.TryGetValue((uint)req.MaterialId, out var matItem))
        {
            matItem.ItemCount--;
            var proto = matItem.ToProto();
            if (matItem.ItemCount == 0)
            {
                player.InventoryManager.InventoryData.Items.Remove(matItem.UniqueId);
                proto.Count = 0;
            }
            syncItems.Add(proto);
        }
        else
        {
            await CallGSRouter.SendScript(connection, "Weapon_Evolution", "\"tip.not_material\"");
            return;
        }

        weapon.Evolue++;
        syncItems.Add(weapon.ToProto());

        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);

        var sync = new NtfSyncPlayer();
        sync.Items.AddRange(syncItems);

        await CallGSRouter.SendScript(connection, "Weapon_Evolution", "null", sync);
    }
}

internal sealed class WeaponEvolutionParam
{
    [JsonPropertyName("Id")]
    public int WeaponId { get; set; }

    [JsonPropertyName("nItemId")]
    public int MaterialId { get; set; }
}
