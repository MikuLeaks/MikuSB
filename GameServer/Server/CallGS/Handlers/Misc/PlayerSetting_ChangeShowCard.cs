using MikuSB.Enums.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_ChangeShowCard")]
public class PlayerSetting_ChangeShowCard : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<ChangeShowCardParam>(param);
        if (req == null)
            return;

        var card = player.CharacterManager.GetCharacterByGUID(req.Id);
        if (card == null)
        {
            await CallGSRouter.SendScript(connection, "PlayerSetting_ChangeShowCard", "{}");
            return;
        }
        player.SetShowItem((int)ProfileShowItemTypeEnum.SHOWITEM_GIRL, card.Guid);
        var sync = new NtfSyncPlayer();
        sync.ShowItems.AddRange(player.Data.ShowItems);
        await CallGSRouter.SendScript(connection, "PlayerSetting_ChangeShowCard", "{}", sync);
    }
}

internal sealed class ChangeShowCardParam
{
    [JsonPropertyName("nID")]
    public uint Id { get; set; }
}
