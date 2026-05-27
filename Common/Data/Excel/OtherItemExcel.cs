using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("item/templates/others.json")]
public class OtherItemExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public string LuaType { get; set; } = "";
    [JsonProperty("UseMode")] public JToken? UseModeRaw { get; set; }
    [JsonProperty("Param1")] public JToken? Param1Raw { get; set; }
    [JsonProperty("GMnum")] public JToken? GMnumRaw { get; set; }

    [JsonIgnore]
    public uint UseMode => ReadUInt(UseModeRaw);

    [JsonIgnore]
    public uint Param1 => ReadUInt(Param1Raw);

    [JsonIgnore]
    public uint GMnum => ReadUInt(GMnumRaw);

    public override uint GetId() => (uint)GameResourceTemplateId.FromGdpl(Genre, Detail, Particular, Level);

    public override void Loaded()
    {
        GameData.OtherItemData[GetId()] = this;
    }

    private static uint ReadUInt(JToken? token)
    {
        if (token == null)
            return 0;

        return token.Type switch
        {
            JTokenType.Integer => token.Value<uint>(),
            JTokenType.Float => (uint)Math.Max(0, token.Value<decimal>()),
            JTokenType.String when uint.TryParse(token.Value<string>(), out var value) => value,
            _ => 0
        };
    }
}
