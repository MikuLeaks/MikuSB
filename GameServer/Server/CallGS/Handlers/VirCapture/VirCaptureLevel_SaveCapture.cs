using MikuSB.Database;
using MikuSB.Data;
using MikuSB.Enums.Item;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

[CallGSApi("VirCaptureLevel_SaveCapture")]
public class VirCaptureLevel_SaveCapture : ICallGSHandler
{
    private const uint VirCaptureGroupId = 128;
    private const uint CurExpSid = 2;
    private const uint CurLevelSid = 3;
    private const uint BagNumSid = 5;
    private const uint DailyExpSid = 8;
    private const uint ColorMaxStartSid = 11;
    private const uint RikiGroupId = 135;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<VirCaptureSaveCaptureParam>(param);
        if (req == null || req.LevelId == 0 || req.RegionId == 0)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();
        VirCaptureStateHelper.SetPointState(player, (uint)req.LevelId, (uint)req.RegionId, 2u, sync);

        if (!GameData.VirCaptureCaptureRegionData.TryGetValue((uint)req.LevelId, out var captureRegion))
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var rewardGdpl = VirCaptureCaptureRewardResolver.ResolveGdpl(captureRegion, (uint)req.RegionId);
        if (rewardGdpl == null || rewardGdpl.Count < 4 || rewardGdpl[0] != (uint)ItemTypeEnum.TYPE_MONSTER_CARD)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}", sync);
            return;
        }

        var grantedItem = await player.InventoryManager.AddMonsterCardItem(
            rewardGdpl[1],
            rewardGdpl[2],
            rewardGdpl[3],
            sendPacket: false);
        if (grantedItem == null)
        {
            await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", "{\"sErr\":\"error.BadParam\"}", sync);
            return;
        }

        sync.Items.Add(grantedItem.ToProto());
        SyncVirCaptureCounters(player, grantedItem.TemplateId, sync);
        ApplyCaptureExp(player, grantedItem.TemplateId, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);
        DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);

        var response = new JsonObject
        {
            ["nLevelID"] = req.LevelId,
            ["nRegionId"] = req.RegionId,
            ["nAddItemId"] = grantedItem.UniqueId,
            ["tbGDPL"] = new JsonArray(rewardGdpl.Select(x => JsonValue.Create((int)x)).ToArray())
        };

        await CallGSRouter.SendScript(connection, "VirCaptureLevel_SaveCapture", response.ToJsonString(), sync);
    }

    private static void SyncVirCaptureCounters(MikuSB.GameServer.Game.Player.PlayerInstance player, ulong templateId, NtfSyncPlayer sync)
    {
        var bagCount = (uint)player.InventoryManager.InventoryData.Items.Values.Count(x => x.ItemType == ItemTypeEnum.TYPE_MONSTER_CARD);
        VirCaptureStateHelper.SetUnsignedAttr(player, BagNumSid, bagCount, sync);

        if (!GameData.MonsterCardData.TryGetValue(templateId, out var monsterCard) || monsterCard.RikiId == 0)
            return;

        var colorSid = ColorMaxStartSid + Math.Max(0u, monsterCard.Color - 1u);
        var colorAttr = player.Data.Attrs.FirstOrDefault(x => x.Gid == VirCaptureGroupId && x.Sid == colorSid);
        var nextColorValue = (colorAttr?.Val ?? 0) + 1;
        VirCaptureStateHelper.SetUnsignedAttr(player, colorSid, nextColorValue, sync);

        var rikiAttr = player.Data.Attrs.FirstOrDefault(x => x.Gid == RikiGroupId && x.Sid == monsterCard.RikiId);
        if (rikiAttr == null)
        {
            rikiAttr = new Database.Player.PlayerAttr
            {
                Gid = RikiGroupId,
                Sid = monsterCard.RikiId,
                Val = 0
            };
            player.Data.Attrs.Add(rikiAttr);
        }

        rikiAttr.Val += 1;
        sync.Custom[player.ToPackedAttrKey(RikiGroupId, monsterCard.RikiId)] = rikiAttr.Val;
        sync.Custom[player.ToShiftedAttrKey(RikiGroupId, monsterCard.RikiId)] = rikiAttr.Val;
    }

    private static void ApplyCaptureExp(MikuSB.GameServer.Game.Player.PlayerInstance player, ulong templateId, NtfSyncPlayer sync)
    {
        if (!GameData.MonsterCardData.TryGetValue(templateId, out var monsterCard) || monsterCard.Exp == 0)
            return;

        var curLevelAttr = GetOrCreateVirCaptureAttr(player, CurLevelSid);
        var curExpAttr = GetOrCreateVirCaptureAttr(player, CurExpSid);
        var dailyExpAttr = GetOrCreateVirCaptureAttr(player, DailyExpSid);

        var maxLevel = GameData.VirCaptureLevelListData.Count == 0 ? 1u : GameData.VirCaptureLevelListData.Keys.Max();
        var curLevel = Math.Max(1u, curLevelAttr.Val);
        if (curLevel >= maxLevel)
            return;

        var baseExp = monsterCard.Exp;
        if (GameData.VirCaptureLevelListData.TryGetValue(curLevel, out var currentLevelCfg) && currentLevelCfg.ExpUp > 1d)
            baseExp = (uint)Math.Floor(baseExp * currentLevelCfg.ExpUp);

        var maxDailyExp = ResolveCurrentAct(player)?.MaxExp ?? 0u;
        if (maxDailyExp > 0 && dailyExpAttr.Val >= maxDailyExp)
            return;

        var gainExp = baseExp;
        if (maxDailyExp > 0)
            gainExp = Math.Min(gainExp, maxDailyExp - dailyExpAttr.Val);

        if (gainExp == 0)
            return;

        dailyExpAttr.Val += gainExp;
        SyncVirCaptureAttr(player, DailyExpSid, dailyExpAttr.Val, sync);

        var pendingExp = curExpAttr.Val + gainExp;
        while (GameData.VirCaptureLevelListData.TryGetValue(curLevel, out var levelCfg) && curLevel < maxLevel)
        {
            if (pendingExp < levelCfg.Exp)
                break;

            pendingExp -= levelCfg.Exp;
            curLevel++;
        }

        curLevelAttr.Val = curLevel;
        curExpAttr.Val = curLevel >= maxLevel
            ? GameData.VirCaptureLevelListData.GetValueOrDefault(maxLevel)?.Exp ?? pendingExp
            : pendingExp;

        SyncVirCaptureAttr(player, CurLevelSid, curLevelAttr.Val, sync);
        SyncVirCaptureAttr(player, CurExpSid, curExpAttr.Val, sync);
    }

    private static Database.Player.PlayerAttr GetOrCreateVirCaptureAttr(MikuSB.GameServer.Game.Player.PlayerInstance player, uint sid)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == VirCaptureGroupId && x.Sid == sid);
        if (attr != null)
            return attr;

        attr = new Database.Player.PlayerAttr
        {
            Gid = VirCaptureGroupId,
            Sid = sid,
            Val = 0
        };
        player.Data.Attrs.Add(attr);
        return attr;
    }

    private static void SyncVirCaptureAttr(MikuSB.GameServer.Game.Player.PlayerInstance player, uint sid, uint value, NtfSyncPlayer sync)
    {
        sync.Custom[player.ToPackedAttrKey(VirCaptureGroupId, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(VirCaptureGroupId, sid)] = value;
    }

    private static MikuSB.Data.Excel.VirCaptureTimeExcel? ResolveCurrentAct(MikuSB.GameServer.Game.Player.PlayerInstance player)
    {
        var actId = player.Data.Attrs.FirstOrDefault(x => x.Gid == VirCaptureGroupId && x.Sid == 1)?.Val ?? 0;
        if (actId > 0 && GameData.VirCaptureTimeData.TryGetValue(actId, out var act))
            return act;

        var now = DateTime.Now;
        return GameData.VirCaptureTimeData.Values
            .Select(x => new { Config = x, Start = ParseConfigTime(x.StartTime), End = ParseConfigTime(x.EndTime) })
            .Where(x => x.Start.HasValue && x.End.HasValue && x.Start <= now && now < x.End)
            .OrderBy(x => x.Start)
            .Select(x => x.Config)
            .FirstOrDefault();
    }

    private static DateTime? ParseConfigTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim().Trim('[', ']');
        if (normalized.Length != 12)
            return null;

        return DateTime.TryParseExact(
            normalized,
            "yyyyMMddHHmm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var value)
            ? value
            : null;
    }
}

internal sealed class VirCaptureSaveCaptureParam
{
    [JsonPropertyName("nLevelID")]
    public int LevelId { get; set; }

    [JsonPropertyName("nRegionId")]
    public int RegionId { get; set; }
}
