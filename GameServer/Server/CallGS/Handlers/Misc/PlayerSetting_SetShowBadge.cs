using MikuSB.Enums.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_SetShowBadge")]
public class PlayerSetting_SetShowBadge : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<SetShowBadgeParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "PlayerSetting_SetShowBadge", "{\"err\":\"error.BadParam\"}");
            return;
        }

        var slots = new[]
        {
            ProfileShowItemTypeEnum.SHOWITEM_BADGE1,
            ProfileShowItemTypeEnum.SHOWITEM_BADGE2,
            ProfileShowItemTypeEnum.SHOWITEM_BADGE3
        };
        for (int i = 0; i < slots.Length; i++)
        {
            var uniqueId = i < req.Badges.Count ? req.Badges[i] : 0;
            player.SetShowItem((int)slots[i], uniqueId);
        }

        var sync = new NtfSyncPlayer();
        sync.ShowItems.AddRange(player.Data.ShowItems);
        await CallGSRouter.SendScript(connection, "PlayerSetting_SetShowBadge", "null", sync);
    }
}

internal sealed class SetShowBadgeParam
{
    [JsonPropertyName("tbBadge")]
    public List<uint> Badges { get; set; } = [];
}
