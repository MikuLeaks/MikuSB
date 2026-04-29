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

[CallGSApi("GirlSkin_ChangeSkinType")]
public class GirlSkin_ChangeSkinType : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<ChangeSkinTypeParam>(param);
        var skinType = ClampClientSkinType(req?.Type ?? 0);
        var response = new JsonObject
        {
            ["nType"] = skinType,
            ["nSkinId"] = req?.SkinId
        };
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString());
            return;
        }

        var player = connection.Player!;
        var skinData = GetOrCreateSkinItem(player, req.SkinId);
        if (skinData != null)
            skinData.SkinType = skinType;

        player.InventoryManager.InventoryData.SkinTypesBySkinId ??= [];
        player.InventoryManager.InventoryData.SkinTypesBySkinId[req.SkinId] = skinType;
        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);

        if (skinData == null)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString());
            return;
        }

        var sync = new NtfSyncPlayer
        {
            Items = { skinData.ToProto() }
        };

        await CallGSRouter.SendScript(connection, "GirlSkin_ChangeSkinType", response.ToJsonString(), sync);
    }

    internal static uint ClampClientSkinType(uint skinType)
    {
        return Math.Min(skinType, 1);
    }

    internal static GameSkinInfo? GetOrCreateSkinItem(PlayerInstance player, uint skinId)
    {
        var inventoryData = player.InventoryManager.InventoryData;
        if (inventoryData.Skins.TryGetValue(skinId, out var skinInfo))
            return skinInfo;

        if (!GameData.CardSkinData.TryGetValue(skinId, out var skinData))
            return null;

        var templateId = GameResourceTemplateId.FromGdpl(skinData.Genre, skinData.Detail, skinData.Particular, skinData.Level);
        skinInfo = player.InventoryManager.GetSkinItemByTemplateId(templateId);
        if (skinInfo != null)
        {
            inventoryData.Skins.Remove(skinInfo.UniqueId);
            skinInfo.UniqueId = skinId;
        }
        else
        {
            skinInfo = new GameSkinInfo
            {
                UniqueId = skinId,
                TemplateId = templateId,
                ItemType = ItemTypeEnum.TYPE_CARD_SKIN,
                ItemCount = 1
            };
        }

        inventoryData.Skins[skinId] = skinInfo;
        return skinInfo;
    }
}

internal sealed class ChangeSkinTypeParam
{
    [JsonPropertyName("nType")]
    public uint Type { get; set; }

    [JsonPropertyName("nSkinId")]
    public uint SkinId { get; set; }
}
