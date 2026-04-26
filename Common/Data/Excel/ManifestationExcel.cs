using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

[ResourceEntity("manifestation.json")]
public class ManifestationExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    public string I18n { get; set; } = "";
    [JsonProperty("profile")]public List<int> Profile { get; set; } = [];

    public override uint GetId()
    {
        return (Genre << 24) | (Detail << 16) | (Particular << 8) | Level;
    }

    public override void Loaded()
    {
        GameData.ManifestationData.Add(GetId(), this);
    }
}
