using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlSpine_UnlockNodeOneKey")]
public class GirlSpine_UnlockNodeOneKey : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<OneKeyUnlockParam>(param);
        if (req == null || req.CardId == 0 || req.MastIdx <= 0 || req.SubIdxList == null || req.SubIdxList.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var card = player.CharacterManager.GetCharacterByGUID((uint)req.CardId);
        if (card == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        // Look up costs from config: CardExcel → SpineId → SpineExcel → NodeConditionId → NodeConditionExcel
        var cardTemplateId = card.TemplateId;
        var cardDetail = (uint)((cardTemplateId >> 16) & 0xFFFF);
        var cardParticular = (uint)((cardTemplateId >> 32) & 0xFFFF);

        var cardExcel = GameData.CardData.Values.FirstOrDefault(
            x => x.Detail == cardDetail && x.Particular == cardParticular);

        var requestedMaterials = new Dictionary<ulong, uint>();

        if (cardExcel != null && GameData.SpineData.TryGetValue(cardExcel.SpineId, out var spineExcel))
        {
            var nodeCondId = spineExcel.GetNodeReq(req.MastIdx);
            if (nodeCondId != 0 && GameData.NodeConditionData.TryGetValue(nodeCondId, out var nodeCond))
            {
                int spineListIdx = req.MastIdx - 1;
                while (card.Spines.Count <= spineListIdx) card.Spines.Add(0);
                var currentMask = card.Spines[spineListIdx];

                foreach (var subIdx in req.SubIdxList)
                {
                    if (subIdx <= 0) continue;
                    uint bit = 1u << (subIdx - 1);
                    if ((currentMask & bit) != 0) continue; // already unlocked, skip cost

                    foreach (var row in nodeCond.GetNodeCost(subIdx))
                    {
                        if (row.Count < 5) continue;
                        var tid = GameResourceTemplateId.FromGdpl(
                            (uint)row[0], (uint)row[1], (uint)row[2], (uint)row[3]);
                        requestedMaterials[tid] = requestedMaterials.GetValueOrDefault(tid) + (uint)row[4];
                    }
                }
            }
        }

        // Validate materials
        foreach (var (tid, count) in requestedMaterials)
        {
            var item = player.InventoryManager.InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == tid);
            if (item == null || item.ItemCount < count)
            {
                await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"tip.not_material\"}");
                return;
            }
        }

        // Consume materials
        var syncItems = new List<Item>();
        foreach (var (tid, count) in requestedMaterials)
        {
            var item = player.InventoryManager.InventoryData.Items.Values.First(x => x.TemplateId == tid);
            item.ItemCount -= count;
            var proto = item.ToProto();
            if (item.ItemCount == 0)
            {
                player.InventoryManager.InventoryData.Items.Remove(item.UniqueId);
                proto.Count = 0;
            }
            syncItems.Add(proto);
        }

        // Unlock all specified sub-nodes
        int mastSpineIdx = req.MastIdx - 1;
        while (card.Spines.Count <= mastSpineIdx) card.Spines.Add(0);
        foreach (var subIdx in req.SubIdxList)
        {
            if (subIdx <= 0) continue;
            card.Spines[mastSpineIdx] |= 1u << (subIdx - 1);
        }
        syncItems.Add(card.ToProto());

        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);

        var sync = new NtfSyncPlayer();
        sync.Items.AddRange(syncItems);

        // No s2c handler exists for GirlSpine_UnlockNodeOneKey — reuse GirlSpine_ChildUnLock
        // which calls UI.CloseConnection() and triggers OnNerveNodeUp to refresh the UI.
        var lastSubIdx = req.SubIdxList.Count > 0 ? req.SubIdxList[^1] : 9;
        var rsp = $"{{\"tb\":{{\"D\":{cardDetail},\"pId\":{req.CardId},\"MastIdx\":{req.MastIdx},\"SubIdx\":{lastSubIdx}}}}}";
        await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", rsp, sync);
    }
}

internal sealed class OneKeyUnlockParam
{
    [JsonPropertyName("pId")]
    public int CardId { get; set; }

    [JsonPropertyName("nIdx")]
    public int MastIdx { get; set; }

    [JsonPropertyName("tbOneKey")]
    public List<int> SubIdxList { get; set; } = [];
}
