using Azure;
using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlSkinParts_Update")]
public class GirlSkinParts_Update : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<GirlSkinPartsUpdateParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSkinParts_Update", "{\"sErr\":\"error.BadParam\"}");
            return;
        }
        var player = connection.Player!;
        var data = new List<GameSkinInfo>();
        foreach(var partId in req.PartsId)
        {
            var partData = player.InventoryManager.GetNormalItem(partId);
            if (partData == null) continue;

            var partExcel = GameData.CardSkinPartsData.Values.FirstOrDefault(x => x.TemplateId == partData.TemplateId);
            if (partExcel == null) continue;

            var skinData = player.InventoryManager.GetSkinItem(req.SkinId);
            if (skinData == null) continue;

            skinData.PartSlots[partExcel.Detail] = partData.UniqueId;
            data.Add(skinData);
        }

        var sync = new NtfSyncPlayer 
        {
            Items = { data.Select(x => x.ToProto()) }
        };
        await CallGSRouter.SendScript(connection, "GirlSkinParts_Update", "{}", sync);
    }
}

internal sealed class GirlSkinPartsUpdateParam
{
    [JsonPropertyName("tbPartsID")]
    public List<uint> PartsId { get; set; } = [];

    [JsonPropertyName("nSkinId")]
    public uint SkinId { get; set; }
}
