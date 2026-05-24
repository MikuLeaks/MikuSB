using MikuSB.Data;
using MikuSB.Database;
using MikuSB.Enums.Item;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCapture_ChangeFormation")]
public class VirCapture_ChangeFormation : ICallGSHandler
{
    private const uint StrGroupId = 57;
    private const uint FormationSid = 1;
    private const uint VirCaptureGroupId = 128;
    private const uint CurLevelSid = 3;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureChangeFormationParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var formation = ReadFormation(player);
        var addId = (uint)Math.Max(0, req.Id);
        var unloadId = (uint)Math.Max(0, req.UnloadId);

        var unloadIndex = unloadId == 0 ? -1 : formation.FindIndex(x => x == unloadId);
        if (unloadId > 0 && unloadIndex < 0)
        {
            await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        if (addId > 0)
        {
            if (formation.Contains(addId))
            {
                await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", "{\"sErr\":\"error.BadParam\"}");
                return;
            }

            var addItem = player.InventoryManager.GetNormalItem(addId);
            if (addItem == null || addItem.ItemType != ItemTypeEnum.TYPE_MONSTER_CARD)
            {
                await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", "{\"sErr\":\"error.BadParam\"}");
                return;
            }
        }

        if (unloadIndex >= 0)
            formation.RemoveAt(unloadIndex);

        if (addId > 0)
        {
            if (unloadIndex >= 0 && unloadIndex <= formation.Count)
                formation.Insert(unloadIndex, addId);
            else
                formation.Add(addId);
        }

        if (!ValidateFormation(player, formation))
        {
            await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var json = JsonSerializer.Serialize(formation);
        player.SetStrAttr(StrGroupId, FormationSid, json);

        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer();
        sync.CustomStr[player.ToShiftedAttrKey(StrGroupId, FormationSid)] = json;

        var response = new JsonObject
        {
            ["nId"] = req.Id,
            ["nUnloadId"] = req.UnloadId,
            ["bAdd"] = addId > 0
        };

        await CallGSRouter.SendScript(connection, "VirCapture_ChangeFormation", response.ToJsonString(), sync);
    }

    private static List<uint> ReadFormation(MikuSB.GameServer.Game.Player.PlayerInstance player)
    {
        var raw = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == StrGroupId && x.Sid == FormationSid)?.Val;
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<uint>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool ValidateFormation(MikuSB.GameServer.Game.Player.PlayerInstance player, List<uint> formation)
    {
        var curLevel = player.Data.Attrs.FirstOrDefault(x => x.Gid == VirCaptureGroupId && x.Sid == CurLevelSid)?.Val ?? 1;
        if (!GameData.VirCaptureLevelListData.TryGetValue(curLevel, out var levelCfg))
            return formation.Count == 0;

        if (formation.Count > levelCfg.Num)
            return false;

        uint totalCost = 0;
        foreach (var itemId in formation)
        {
            var item = player.InventoryManager.GetNormalItem(itemId);
            if (item == null || item.ItemType != ItemTypeEnum.TYPE_MONSTER_CARD)
                return false;

            if (!GameData.MonsterCardData.TryGetValue(item.TemplateId, out var monsterCfg))
                return false;

            totalCost += monsterCfg.CostValue;
        }

        return totalCost <= levelCfg.MaxCost;
    }
}

internal sealed class VirCaptureChangeFormationParam
{
    [JsonPropertyName("nId")]
    public int Id { get; set; }

    [JsonPropertyName("nUnloadId")]
    public int UnloadId { get; set; }
}
