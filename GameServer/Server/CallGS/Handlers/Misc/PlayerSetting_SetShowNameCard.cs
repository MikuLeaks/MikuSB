using MikuSB.Enums.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_SetShowNameCard")]
public class PlayerSetting_SetShowNameCard : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<SetShowNameCardParam>(param);
        if (req == null)
            return;

        var item = player.InventoryManager.GetNormalItem(req.Id);
        if (item == null)
        {
            await CallGSRouter.SendScript(connection, "PlayerSetting_SetShowNameCard", "{\"err\":\"error.BadParam\"}");
            return;
        }

        player.SetShowItem((int)ProfileShowItemTypeEnum.SHOWITEM_NAMECARD, item.UniqueId);
        var sync = new NtfSyncPlayer();
        sync.ShowItems.AddRange(player.Data.ShowItems);
        await CallGSRouter.SendScript(connection, "PlayerSetting_SetShowNameCard", "null", sync);
    }
}

internal sealed class SetShowNameCardParam
{
    [JsonPropertyName("nID")]
    public uint Id { get; set; }
}
