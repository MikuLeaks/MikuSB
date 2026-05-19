using MikuSB.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("gacha/gacha.json")]
public class GachaExcel : ExcelResource
{
    public uint ID { get; set; }
    public List<string>? Pool { get; set; }
    public uint Probability { get; set; }
    public uint ProbabilityTen { get; set; }
    public JToken? ProtectNum { get; set; }
    public JToken? UpNum { get; set; }
    public uint? ProtectTag { get; set; }
    public uint? ProtectType { get; set; }
    public JToken? ProtectCount { get; set; }
    public uint? UpSelect { get; set; }

    public override uint GetId() => ID;
    public override void Loaded() => GameData.GachaData[ID] = this;

    public override void AfterAllDone()
    {
        foreach (var poolName in Pool ?? [])
        {
            if (GameData.GachaPoolData.ContainsKey(poolName)) continue;
            var path = ConfigManager.Config.Path.ResourcePath + "/gacha/pool/" + poolName + ".json";
            if (!File.Exists(path)) continue;
            var json = File.ReadAllText(path);
            var items = JsonConvert.DeserializeObject<List<GachaPoolItem>>(json) ?? [];
            GameData.GachaPoolData[poolName] = items;
        }
    }
}

public class GachaPoolItem
{
    public int ID { get; set; }
    public int Rarity { get; set; }
    public List<uint> GDPL { get; set; } = [];
    public int Weight { get; set; }
    public int? UPTag { get; set; }
    public int? UPSelectTag { get; set; }
}
