using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

// Spine node encoding: (MastIdx << 8) | SubIdx stored as uint in CharacterInfo.Spines
// GetStrAttribute(Gid=30, Sid=Detail) stores JSON: { "<Particular>": { "ns": <MastIdx>, "tbn": [0,0], "tbr": [] } }
[CallGSApi("GirlSpine_ChildUnLock")]
public class GirlSpine_ChildUnLock : ICallGSHandler
{
    private const uint SpineStrAttrGid = 30;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var req = JsonSerializer.Deserialize<ChildUnLockParam>(param);
        if (req == null || req.CardId == 0 || req.Info == null || req.Materials == null)
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

        var mastIdx = req.Info.Indx;
        var subIdx = req.Info.InSubIdx;
        if (mastIdx <= 0 || subIdx <= 0)
        {
            await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        // Spines[MastIdx-1] is a bitmask; bit (SubIdx-1) = 1 means that sub-node is unlocked.
        // GetSpine(MastIdx, SubIdx) checks (Spines[MastIdx-1] & (1 << (SubIdx-1))) != 0
        int spineListIdx = mastIdx - 1;
        uint spineBit = 1u << (subIdx - 1);

        while (card.Spines.Count <= spineListIdx)
            card.Spines.Add(0);

        if ((card.Spines[spineListIdx] & spineBit) != 0)
        {
            await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"tip.girlcard_alread_break\"}");
            return;
        }

        // Consume materials
        var requestedMaterials = new Dictionary<ulong, uint>();
        foreach (var row in req.Materials)
        {
            if (row == null || row.Count < 5) continue;
            var genre = (uint)Math.Max(0, row[0]);
            var detail = (uint)Math.Max(0, row[1]);
            var particular = (uint)Math.Max(0, row[2]);
            var level = (uint)Math.Max(0, row[3]);
            var count = (uint)Math.Max(0, row[4]);
            if (genre == 0 || detail == 0 || particular == 0 || level == 0 || count == 0) continue;
            var tid = GameResourceTemplateId.FromGdpl(genre, detail, particular, level);
            requestedMaterials[tid] = requestedMaterials.GetValueOrDefault(tid) + count;
        }

        var syncItems = new List<Item>();
        foreach (var (tid, count) in requestedMaterials)
        {
            var item = player.InventoryManager.InventoryData.Items.Values.FirstOrDefault(x => x.TemplateId == tid);
            if (item == null || item.ItemCount < count)
            {
                await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", "{\"sErr\":\"tip.not_material\"}");
                return;
            }
        }

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

        // Unlock the spine node by setting the corresponding bit
        card.Spines[spineListIdx] |= spineBit;
        syncItems.Add(card.ToProto());

        // Extract Detail and Particular from TemplateId for StrAttr
        var cardDetail = (uint)((card.TemplateId >> 16) & 0xFFFF);
        var cardParticular = (uint)((card.TemplateId >> 32) & 0xFFFF);

        // Build and persist StrAttr JSON: { "<particular>": { "ns": mastIdx, "tbn": [0,0], "tbr": [] } }
        UpdateSpineStrAttr(player.Data, cardDetail, cardParticular, mastIdx);

        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);
        DatabaseHelper.SaveDatabaseType(player.Data);

        // Send NtfSetStrAttr so client's GetStrAttribute(30, Detail) returns fresh data
        var strAttrData = GetSpineStrAttrJson(player.Data, cardDetail);
        var ntfStrAttr = new NtfSetStrAttr { Gid = SpineStrAttrGid, Sid = cardDetail, Val = strAttrData };
        await connection.Player!.SendPacket(CmdIds.NtfSetStrAttr, ntfStrAttr);

        var sync = new NtfSyncPlayer();
        sync.Items.AddRange(syncItems);

        var rsp = $"{{\"tb\":{{\"D\":{cardDetail},\"pId\":{req.CardId},\"MastIdx\":{mastIdx},\"SubIdx\":{subIdx}}}}}";
        await CallGSRouter.SendScript(connection, "GirlSpine_ChildUnLock", rsp, sync);
    }

    private static void UpdateSpineStrAttr(PlayerGameData data, uint detail, uint particular, int mastIdx)
    {
        var existing = data.StrAttrs.FirstOrDefault(x => x.Gid == SpineStrAttrGid && x.Sid == detail);
        if (existing == null)
        {
            existing = new PlayerStrAttr { Gid = SpineStrAttrGid, Sid = detail, Val = "{}" };
            data.StrAttrs.Add(existing);
        }

        var root = JsonSerializer.Deserialize<Dictionary<string, SpineStrData>>(existing.Val)
                   ?? new Dictionary<string, SpineStrData>();

        var key = particular.ToString();
        if (!root.TryGetValue(key, out var entry))
            entry = new SpineStrData();

        entry.Ns = mastIdx;
        root[key] = entry;

        existing.Val = JsonSerializer.Serialize(root);
    }

    private static string GetSpineStrAttrJson(PlayerGameData data, uint detail)
    {
        var existing = data.StrAttrs.FirstOrDefault(x => x.Gid == SpineStrAttrGid && x.Sid == detail);
        return existing?.Val ?? "{}";
    }
}

internal sealed class ChildUnLockParam
{
    [JsonPropertyName("pId")]
    public int CardId { get; set; }

    [JsonPropertyName("tbInfo")]
    public NodeInfo? Info { get; set; }

    [JsonPropertyName("tbMat")]
    public List<List<int>> Materials { get; set; } = [];
}

internal sealed class NodeInfo
{
    [JsonPropertyName("Indx")]
    public int Indx { get; set; }

    [JsonPropertyName("InSubIdx")]
    public int InSubIdx { get; set; }
}

internal sealed class SpineStrData
{
    [JsonPropertyName("ns")]
    public int Ns { get; set; } = 0;

    [JsonPropertyName("tbn")]
    public List<int> Tbn { get; set; } = [0, 0];

    [JsonPropertyName("tbr")]
    public List<int> Tbr { get; set; } = [];
}
