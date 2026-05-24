using MikuSB.Data.Excel;
using MikuSB.Util;
using Newtonsoft.Json.Linq;

namespace MikuSB.GameServer.Server.CallGS.Handlers.VirCapture;

internal static class VirCaptureCaptureRewardResolver
{
    private static readonly Lock CacheLock = new();
    private static readonly Dictionary<string, Dictionary<uint, VirCaptureLevelRegionReward>> RegionCache = [];
    private static readonly Dictionary<string, Dictionary<uint, List<uint>>> BossCache = [];

    public static List<uint>? ResolveGdpl(VirCaptureCaptureRegionExcel captureRegion, uint regionId)
    {
        if (string.IsNullOrWhiteSpace(captureRegion.LevelRegionName))
            return null;

        var regionMap = GetOrLoadRegionMap(captureRegion.LevelRegionName);
        if (!regionMap.TryGetValue(regionId, out var regionReward))
            return null;

        if (regionReward.PalType == 2)
            return GetOrLoadBossMap(captureRegion.LevelRegionName).GetValueOrDefault(regionId);

        return regionReward.Rewards1;
    }

    private static Dictionary<uint, VirCaptureLevelRegionReward> GetOrLoadRegionMap(string mapName)
    {
        lock (CacheLock)
        {
            if (RegionCache.TryGetValue(mapName, out var cached))
                return cached;

            var loaded = new Dictionary<uint, VirCaptureLevelRegionReward>();
            var path = Path.Combine(
                ConfigManager.Config.Path.ResourcePath,
                "dlc",
                "vircapture",
                mapName,
                "region_info.json");

            if (File.Exists(path))
            {
                var array = JArray.Parse(File.ReadAllText(path));
                foreach (var token in array)
                {
                    var id = ReadUInt(token["Id"]);
                    if (id == 0)
                        continue;

                    loaded[id] = new VirCaptureLevelRegionReward
                    {
                        PalType = ReadInt(token["PalType"]),
                        Rewards1 = token["Rewards1"]?.ToObject<List<uint>>() ?? []
                    };
                }
            }

            RegionCache[mapName] = loaded;
            return loaded;
        }
    }

    private static Dictionary<uint, List<uint>> GetOrLoadBossMap(string mapName)
    {
        lock (CacheLock)
        {
            if (BossCache.TryGetValue(mapName, out var cached))
                return cached;

            var loaded = new Dictionary<uint, List<uint>>();
            var path = Path.Combine(
                ConfigManager.Config.Path.ResourcePath,
                "dlc",
                "vircapture",
                mapName,
                "boss.json");

            if (File.Exists(path))
            {
                var array = JArray.Parse(File.ReadAllText(path));
                foreach (var token in array)
                {
                    var regionId = ReadUInt(token["RegionId"]);
                    var boss = token["Boss"]?.ToObject<List<uint>>();
                    if (regionId == 0 || boss == null || boss.Count < 4)
                        continue;

                    loaded.TryAdd(regionId, boss);
                }
            }

            BossCache[mapName] = loaded;
            return loaded;
        }
    }

    private sealed class VirCaptureLevelRegionReward
    {
        public int PalType { get; init; }
        public List<uint> Rewards1 { get; init; } = [];
    }

    private static uint ReadUInt(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return 0;

        return token.Type switch
        {
            JTokenType.Integer => token.Value<uint>(),
            JTokenType.Float => Math.Max(0u, (uint)token.Value<double>()),
            JTokenType.String when uint.TryParse(token.Value<string>(), out var value) => value,
            JTokenType.String => 0,
            _ => 0
        };
    }

    private static int ReadInt(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return 0;

        return token.Type switch
        {
            JTokenType.Integer => token.Value<int>(),
            JTokenType.Float => (int)token.Value<double>(),
            JTokenType.String when int.TryParse(token.Value<string>(), out var value) => value,
            JTokenType.String => 0,
            _ => 0
        };
    }
}
