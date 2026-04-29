using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("call_item.json")]
public class CallItemExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public string I18n { get; set; } = "";

    public override uint GetId()
    {
        return (uint)I18n.GetHashCode();
    }

    public override void Loaded()
    {
        GameData.CallItemData.Add(GetId(), this);
    }
}
