using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Inventory;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Game.BossPvp;

internal static class BossPvpService
{
    private const uint GroupId = 51;
    private const uint ActivitySubId = 0;
    private const uint ChallengeNumSid = 1;
    private const uint DiffStartId = 10;
    private const uint LevelStartSid = 100;
    private const uint LevelStride = 10;
    private const uint BossLineup1 = 15;
    private const uint BossLineup2 = 16;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async ValueTask<(object Response, NtfSyncPlayer Sync)> HandleGetOpenIdAsync(PlayerInstance player)
    {
        await EnsureBossLineupsAsync(player);

        var sync = new NtfSyncPlayer();
        var season = GetOpenSeason();
        var seasonId = season?.ID ?? 1u;

        SetStr(player, ActivitySubId, seasonId.ToString(CultureInfo.InvariantCulture), sync);
        SetStr(player, ChallengeNumSid, GetDailyChallengeNum().ToString(CultureInfo.InvariantCulture), sync);

        if (season != null)
        {
            for (var index = 0; index < season.BossIds.Count; index++)
            {
                var bossLevelId = season.BossIds[index];
                EnsureStr(player, DiffStartId + (uint)(index + 1), "0", sync);
                EnsureStr(player, GetBossSid(bossLevelId, 1), EmptySnapshotJson(), sync);
                EnsureStr(player, GetBossSid(bossLevelId, 2), EmptySnapshotJson(), sync);
                EnsureStr(player, GetBossSid(bossLevelId, 3), EmptySnapshotJson(), sync);
                EnsureStr(player, GetBossSid(bossLevelId, 4), "0", sync);
                EnsureStr(player, GetBossSid(bossLevelId, 5), "0", sync);
                EnsureStr(player, GetBossSid(bossLevelId, 6), "0", sync);
                EnsureStr(player, GetBossSid(bossLevelId, 7), "0", sync);
                EnsureStr(player, GetBossSid(bossLevelId, 8), "0", sync);
            }
        }

        var response = new
        {
            nID = seasonId,
            tbTimeCfg = new[]
            {
                new
                {
                    nStartTime = -1,
                    nEndTime = -1
                }
            }
        };

        return (response, sync);
    }

    public static object HandleEnterLevel(string? param)
    {
        var req = Deserialize<EnterLevelParam>(param);
        return new
        {
            nSeed = Random.Shared.Next(1, int.MaxValue),
            nID = req?.NId ?? 0
        };
    }

    public static (object Response, NtfSyncPlayer Sync) HandleRecord(PlayerInstance player, string? param)
    {
        var req = Deserialize<RecordParam>(param);
        if (req == null)
        {
            return (new { bRecord = false }, new NtfSyncPlayer());
        }

        var sync = new NtfSyncPlayer();
        if (req.BRecord)
        {
            var historyScore = ReadInt(player, GetBossSid(req.NId, 4));
            var currentScore = ComputeIntegral(req.NId, req.NDiff, req.ResidueTime);
            if (currentScore >= historyScore)
            {
                WriteBestRun(player, req.NId, req.NTeamId, req.NTime, currentScore, sync);
            }
        }

        return (new { bRecord = req.BRecord }, sync);
    }

    public static (JsonNode Response, NtfSyncPlayer Sync) HandleSettlement(PlayerInstance player, JsonNode? param)
    {
        var req = param?.Deserialize<SettlementParam>(JsonOptions);
        var sync = new NtfSyncPlayer();
        if (req == null)
        {
            return (new JsonObject(), sync);
        }

        var totalSid = GetBossSid(req.NId, 7);
        var successSid = GetBossSid(req.NId, 6);
        var diffSid = GetBossSid(req.NId, 8);

        SetStr(player, totalSid, (ReadInt(player, totalSid) + 1).ToString(CultureInfo.InvariantCulture), sync);
        SetStr(player, successSid, (ReadInt(player, successSid) + 1).ToString(CultureInfo.InvariantCulture), sync);

        var clearedDiff = Math.Max(ReadInt(player, diffSid), req.NDiff);
        SetStr(player, diffSid, clearedDiff.ToString(CultureInfo.InvariantCulture), sync);

        var positionSid = TryGetPositionDiffSid(req.NId);
        if (positionSid != null)
        {
            var newPositionDiff = Math.Max(ReadInt(player, positionSid.Value), req.NDiff);
            SetStr(player, positionSid.Value, newPositionDiff.ToString(CultureInfo.InvariantCulture), sync);
        }

        var score = ComputeIntegral(req.NId, req.NDiff, req.ResidueTime);
        if (score > ReadInt(player, GetBossSid(req.NId, 4)))
        {
            WriteBestRun(player, req.NId, req.NTeamId, req.NTime, score, sync);
        }

        return (new JsonObject(), sync);
    }

    public static (JsonNode Response, NtfSyncPlayer Sync) HandleFail(PlayerInstance player, JsonNode? param)
    {
        var req = param?.Deserialize<FailParam>(JsonOptions);
        var sync = new NtfSyncPlayer();
        if (req == null)
        {
            return (new JsonObject(), sync);
        }

        var totalSid = GetBossSid(req.NId, 7);
        SetStr(player, totalSid, (ReadInt(player, totalSid) + 1).ToString(CultureInfo.InvariantCulture), sync);

        return (new JsonObject(), sync);
    }

    public static (object Response, NtfSyncPlayer Sync) HandleMopup(PlayerInstance player, string? param)
    {
        var req = Deserialize<MopupParam>(param);
        var sync = new NtfSyncPlayer();
        if (req == null)
        {
            return (new { }, sync);
        }

        var totalSid = GetBossSid(req.NId, 7);
        var successSid = GetBossSid(req.NId, 6);
        var diffSid = GetBossSid(req.NId, 8);

        SetStr(player, totalSid, (ReadInt(player, totalSid) + 1).ToString(CultureInfo.InvariantCulture), sync);
        SetStr(player, successSid, (ReadInt(player, successSid) + 1).ToString(CultureInfo.InvariantCulture), sync);

        var clearedDiff = Math.Max(ReadInt(player, diffSid), req.NDiff);
        SetStr(player, diffSid, clearedDiff.ToString(CultureInfo.InvariantCulture), sync);

        var positionSid = TryGetPositionDiffSid(req.NId);
        if (positionSid != null)
        {
            var newPositionDiff = Math.Max(ReadInt(player, positionSid.Value), req.NDiff + 1);
            SetStr(player, positionSid.Value, newPositionDiff.ToString(CultureInfo.InvariantCulture), sync);
        }

        var score = ComputeIntegral(req.NId, req.NDiff, 0);
        if (score > ReadInt(player, GetBossSid(req.NId, 4)))
        {
            WriteBestRun(player, req.NId, 0, 0, score, sync);
        }

        return (new { }, sync);
    }

    public static object HandleGetReward(string? param)
    {
        _ = Deserialize<RewardParam>(param);
        return new { tbAward = Array.Empty<object>() };
    }

    private static async ValueTask EnsureBossLineupsAsync(PlayerInstance player)
    {
        var lineups = player.LineupManager.LineupData.LineupInfo;
        var baseLineup = lineups.GetValueOrDefault(1) ?? lineups.Values.FirstOrDefault();
        if (baseLineup == null)
        {
            return;
        }

        if (!lineups.ContainsKey((int)BossLineup1))
        {
            await player.LineupManager.UpdateLineup((int)BossLineup1, baseLineup.Member1, baseLineup.Member2, baseLineup.Member3, true);
        }

        if (!lineups.ContainsKey((int)BossLineup2))
        {
            await player.LineupManager.UpdateLineup((int)BossLineup2, baseLineup.Member1, baseLineup.Member2, baseLineup.Member3, true);
        }
    }

    private static void WriteBestRun(PlayerInstance player, uint bossLevelId, uint lineupId, double finishTime, int score, NtfSyncPlayer sync)
    {
        var snapshots = CaptureLineupSnapshots(player, lineupId);
        SetStr(player, GetBossSid(bossLevelId, 1), System.Text.Json.JsonSerializer.Serialize(snapshots[0], JsonOptions), sync);
        SetStr(player, GetBossSid(bossLevelId, 2), System.Text.Json.JsonSerializer.Serialize(snapshots[1], JsonOptions), sync);
        SetStr(player, GetBossSid(bossLevelId, 3), System.Text.Json.JsonSerializer.Serialize(snapshots[2], JsonOptions), sync);
        SetStr(player, GetBossSid(bossLevelId, 4), score.ToString(CultureInfo.InvariantCulture), sync);
        SetStr(player, GetBossSid(bossLevelId, 5), Math.Max(0, (int)Math.Floor(finishTime)).ToString(CultureInfo.InvariantCulture), sync);
    }

    private static BossPvpRoleSnapshot[] CaptureLineupSnapshots(PlayerInstance player, uint lineupId)
    {
        var lineups = player.LineupManager.LineupData.LineupInfo;
        var lineup = lineups.GetValueOrDefault((int)lineupId)
            ?? lineups.GetValueOrDefault((int)BossLineup1)
            ?? lineups.GetValueOrDefault(1)
            ?? lineups.Values.FirstOrDefault();

        if (lineup == null)
        {
            return [new(), new(), new()];
        }

        return
        [
            CaptureRoleSnapshot(player, lineup.Member1),
            CaptureRoleSnapshot(player, lineup.Member2),
            CaptureRoleSnapshot(player, lineup.Member3)
        ];
    }

    private static BossPvpRoleSnapshot CaptureRoleSnapshot(PlayerInstance player, uint characterGuid)
    {
        if (characterGuid == 0)
        {
            return new BossPvpRoleSnapshot();
        }

        var character = player.CharacterManager.GetCharacterByGUID(characterGuid);
        if (character == null)
        {
            return new BossPvpRoleSnapshot();
        }

        var snapshot = new BossPvpRoleSnapshot
        {
            Role = character.Guid,
            Weapon = character.WeaponUniqueId
        };

        var weapon = player.InventoryManager.GetWeaponItem(character.WeaponUniqueId);
        if (weapon != null)
        {
            snapshot.Wgdpl = BuildWeaponGdpl(weapon);
            snapshot.Wslot = weapon.PartSlots;
        }

        var supports = character.SupportSlots
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .Where(x => x != 0)
            .Take(3)
            .ToArray();

        if (supports.Length > 0)
        {
            snapshot.S1 = supports[0];
            snapshot.Sgdpl1 = BuildSupportGdpl(player.InventoryManager.GetSupportCardItem(supports[0]));
        }

        if (supports.Length > 1)
        {
            snapshot.S2 = supports[1];
            snapshot.Sgdpl2 = BuildSupportGdpl(player.InventoryManager.GetSupportCardItem(supports[1]));
        }

        if (supports.Length > 2)
        {
            snapshot.S3 = supports[2];
            snapshot.Sgdpl3 = BuildSupportGdpl(player.InventoryManager.GetSupportCardItem(supports[2]));
        }

        return snapshot;
    }

    private static List<uint> BuildWeaponGdpl(GameWeaponInfo weapon)
    {
        var gdpl = DecodeGdpl(weapon.TemplateId);
        gdpl.Add(weapon.Level);
        gdpl.Add(weapon.Evolue);
        return gdpl;
    }

    private static List<uint> BuildSupportGdpl(GameSupportCardInfo? support)
    {
        if (support == null)
        {
            return [];
        }

        var gdpl = DecodeGdpl(support.TemplateId);
        gdpl.Add(support.Level);
        gdpl.Add(0);
        return gdpl;
    }

    private static List<uint> DecodeGdpl(ulong templateId)
    {
        return
        [
            (uint)(templateId & 0xFFFF),
            (uint)((templateId >> 16) & 0xFFFF),
            (uint)((templateId >> 32) & 0xFFFF),
            (uint)((templateId >> 48) & 0xFFFF)
        ];
    }

    private static int ComputeIntegral(uint bossLevelId, int diff, int residueTime)
    {
        if (!GameData.BossPvpBossData.TryGetValue(bossLevelId, out var boss) || diff <= 0 || diff > boss.BossLevel.Count)
        {
            return 0;
        }

        var info = boss.BossLevel[diff - 1];
        if (info.Count == 0)
        {
            return 0;
        }

        var multiplier = info.Count > 2 ? info[2] : 0;
        var baseScore = info.Count > 3 ? info[3] : 0;
        var residueScore = info.Count > 4 ? info[4] : 0;
        var total = (baseScore + residueScore * Math.Max(0, residueTime)) * multiplier;
        return (int)Math.Floor(total + 0.5);
    }

    private static uint? TryGetPositionDiffSid(uint bossLevelId)
    {
        var season = GetOpenSeason();
        if (season == null)
        {
            return null;
        }

        var index = season.BossIds.FindIndex(x => x == bossLevelId);
        return index >= 0 ? DiffStartId + (uint)(index + 1) : null;
    }

    private static BossPvpBossChallengeExcel? GetOpenSeason()
    {
        var now = DateTimeOffset.Now;
        var current = GameData.BossPvpBossChallengeData.Values
            .OrderBy(x => x.ID)
            .FirstOrDefault(x =>
            {
                var startAt = ParseBossTime(x.StartTime);
                var endAt = ParseBossTime(x.EndTime);
                return startAt != null && endAt != null && now >= startAt && now <= endAt;
            });

        return current ?? GameData.BossPvpBossChallengeData.Values.OrderBy(x => x.ID).FirstOrDefault();
    }

    private static uint GetDailyChallengeNum()
    {
        var now = DateTime.Now;
        if (now.Hour < 4)
        {
            now = now.AddHours(-4);
        }

        var week = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
        return GameData.BossPvpNumData.TryGetValue((uint)week, out var count) ? count.Num : 8;
    }

    private static int ReadInt(PlayerInstance player, uint sid)
    {
        var attr = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid)?.Val;
        return int.TryParse(attr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static void EnsureStr(PlayerInstance player, uint sid, string value, NtfSyncPlayer sync)
    {
        var attr = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid);
        if (attr != null)
        {
            return;
        }

        SetStr(player, sid, value, sync);
    }

    private static void SetStr(PlayerInstance player, uint sid, string value, NtfSyncPlayer sync)
    {
        player.SetStrAttr(GroupId, sid, value);
        sync.CustomStr[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }

    private static uint GetBossSid(uint bossLevelId, uint offset) => (LevelStride * bossLevelId) + LevelStartSid + offset;

    private static string EmptySnapshotJson() => System.Text.Json.JsonSerializer.Serialize(new BossPvpRoleSnapshot(), JsonOptions);

    private static T? Deserialize<T>(string? param)
    {
        if (string.IsNullOrWhiteSpace(param))
        {
            return default;
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(param, JsonOptions);
    }

    private static DateTimeOffset? ParseBossTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim().Trim('[', ']');
        if (!DateTime.TryParseExact(raw, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var localTime))
        {
            return null;
        }

        return new DateTimeOffset(localTime);
    }

    private sealed class EnterLevelParam
    {
        [JsonPropertyName("nID")] public uint NId { get; set; }
    }

    private sealed class RecordParam
    {
        [JsonPropertyName("nID")] public uint NId { get; set; }
        [JsonPropertyName("nDiff")] public int NDiff { get; set; }
        [JsonPropertyName("nTime")] public double NTime { get; set; }
        [JsonPropertyName("ResidueTime")] public int ResidueTime { get; set; }
        [JsonPropertyName("bRecord")] public bool BRecord { get; set; }
        [JsonPropertyName("nTeamID")] public uint NTeamId { get; set; }
    }

    private sealed class SettlementParam
    {
        [JsonPropertyName("nID")] public uint NId { get; set; }
        [JsonPropertyName("nDiff")] public int NDiff { get; set; }
        [JsonPropertyName("nTime")] public double NTime { get; set; }
        [JsonPropertyName("ResidueTime")] public int ResidueTime { get; set; }
        [JsonPropertyName("nTeamID")] public uint NTeamId { get; set; }
    }

    private sealed class FailParam
    {
        [JsonPropertyName("nID")] public uint NId { get; set; }
    }

    private sealed class MopupParam
    {
        [JsonPropertyName("nID")] public uint NId { get; set; }
        [JsonPropertyName("nDiff")] public int NDiff { get; set; }
    }

    private sealed class RewardParam
    {
        [JsonPropertyName("tbTaskID")] public List<uint> TaskIds { get; set; } = [];
    }

    private sealed class BossPvpRoleSnapshot
    {
        [JsonPropertyName("role")] public uint Role { get; set; }
        [JsonPropertyName("weapon")] public uint Weapon { get; set; }
        [JsonPropertyName("s1")] public uint S1 { get; set; }
        [JsonPropertyName("s2")] public uint S2 { get; set; }
        [JsonPropertyName("s3")] public uint S3 { get; set; }
        [JsonPropertyName("wgdpl")] public List<uint> Wgdpl { get; set; } = [];
        [JsonPropertyName("wslot")] public Dictionary<uint, ulong> Wslot { get; set; } = [];
        [JsonPropertyName("sgdpl1")] public List<uint> Sgdpl1 { get; set; } = [];
        [JsonPropertyName("sgdpl2")] public List<uint> Sgdpl2 { get; set; } = [];
        [JsonPropertyName("sgdpl3")] public List<uint> Sgdpl3 { get; set; } = [];
    }
}
