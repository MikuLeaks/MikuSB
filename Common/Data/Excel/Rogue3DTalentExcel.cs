using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("server_03_talent.json")]
public class Rogue3DTalentExcel : ExcelResource
{
    [JsonProperty("TalentID")] public uint TalentId { get; set; }
    [JsonProperty("UnlockCondition")] private object? UnlockConditionRaw { get; set; }
    [JsonIgnore] public uint UnlockCondition { get; private set; }

    public override uint GetId() => TalentId;

    public override void Loaded()
    {
        UnlockCondition = ParseUnlockCondition(UnlockConditionRaw);
        GameData.Rogue3DTalentData[TalentId] = this;
    }

    private static uint ParseUnlockCondition(object? raw)
    {
        return raw switch
        {
            null => 0,
            long value when value > 0 => (uint)value,
            int value when value > 0 => (uint)value,
            string text when uint.TryParse(text, out var value) => value,
            _ => 0
        };
    }
}
