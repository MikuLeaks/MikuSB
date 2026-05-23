using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlCard_UpBySpecialBreak")]
public class GirlCard_UpBySpecialBreak : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<GirlCardUpBySpecialBreakParam>(param);
        if (req == null || req.CardId == 0)
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var card = player.CharacterManager.GetCharacterByGUID((uint)req.CardId);
        if (card == null)
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var cardTemplate = GameData.CardData.Values.FirstOrDefault(x =>
            GameResourceTemplateId.FromGdpl(x.Genre, x.Detail, x.Particular, x.Level) == card.TemplateId);
        if (cardTemplate == null)
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (cardTemplate.BreakMatID <= 10000 ||
            !GameData.SpecialBreakData.TryGetValue(cardTemplate.BreakMatID, out var specialBreakExcel))
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var nextBreak = card.Break + 1;
        if (!specialBreakExcel.HasBreakLevel(nextBreak))
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"tip.already_max_break\"}");
            return;
        }

        var requestedMaterials = new Dictionary<ulong, uint>();
        foreach (var row in specialBreakExcel.GetItems(nextBreak))
        {
            if (row.Count < 5)
                continue;

            var templateId = GameResourceTemplateId.FromGdpl(
                (uint)Math.Max(0, row[0]),
                (uint)Math.Max(0, row[1]),
                (uint)Math.Max(0, row[2]),
                (uint)Math.Max(0, row[3]));
            var count = (uint)Math.Max(0, row[4]);
            if (templateId == 0 || count == 0)
                continue;

            requestedMaterials[templateId] = requestedMaterials.GetValueOrDefault(templateId) + count;
        }

        if (requestedMaterials.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"tip.not_material_for_break\"}");
            return;
        }

        foreach (var (templateId, count) in requestedMaterials)
        {
            var item = player.InventoryManager.InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == templateId);
            if (item == null || item.ItemCount < count)
            {
                await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{\"sErr\":\"tip.not_material_for_break\"}");
                return;
            }
        }

        var syncItems = new List<Item>();
        foreach (var (templateId, count) in requestedMaterials)
        {
            var item = player.InventoryManager.InventoryData.Items.Values.First(x => x.TemplateId == templateId);
            item.ItemCount -= count;

            if (item.ItemCount == 0)
            {
                player.InventoryManager.InventoryData.Items.Remove(item.UniqueId);
                syncItems.Add(BuildRemovedProto(item));
            }
            else
            {
                syncItems.Add(item.ToProto());
            }
        }

        card.Break = nextBreak;
        syncItems.Add(card.ToProto());

        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);

        var sync = new NtfSyncPlayer();
        sync.Items.AddRange(syncItems);

        await CallGSRouter.SendScript(connection, "GirlCard_UpBySpecialBreak", "{}", sync);
    }

    private static Item BuildRemovedProto(BaseGameItemInfo item)
    {
        var proto = item.ToProto();
        proto.Count = 0;
        return proto;
    }
}

internal sealed class GirlCardUpBySpecialBreakParam
{
    [JsonPropertyName("nCardId")]
    public int CardId { get; set; }
}
