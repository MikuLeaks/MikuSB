using MikuSB.Enums.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_SetProfileFace")]
public class PlayerSetting_SetProfileFace : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<SetProfileFaceParam>(param);
        if (req == null)
            return;

        if (req.HeadItemId > 0)
        {
            var item = player.InventoryManager.GetNormalItem(req.HeadItemId);
            if (item == null)
            {
                await CallGSRouter.SendScript(connection, "PlayerSetting_SetProfileFace", "{\"err\":\"error.BadParam\"}");
                return;
            }
            player.SetShowItem((int)ProfileShowItemTypeEnum.SHOWITEM_FACE, item.UniqueId);
        }
        if (req.FrameItemId > 0)
        {
            var item = player.InventoryManager.GetNormalItem(req.FrameItemId);
            if (item == null)
            {
                await CallGSRouter.SendScript(connection, "PlayerSetting_SetProfileFace", "{\"err\":\"error.BadParam\"}");
                return;
            }
            player.SetShowItem((int)ProfileShowItemTypeEnum.SHOWITEM_FRAME, item.UniqueId);
        }
        
        var sync = new NtfSyncPlayer();
        sync.ShowItems.AddRange(player.Data.ShowItems);
        await CallGSRouter.SendScript(connection, "PlayerSetting_SetProfileFace", "null", sync);
    }
}

internal sealed class SetProfileFaceParam
{
    [JsonPropertyName("nHeadItemID")] public uint HeadItemId { get; set; }
    [JsonPropertyName("nFrameItemID")] public uint FrameItemId { get; set; }
}
