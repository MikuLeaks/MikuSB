using Newtonsoft.Json;

namespace MikuSB.Data.Excel;

// spine.json: SpineId → Node{i}Req (nodecondition ID per master node index)
[ResourceEntity("spine.json")]
public class SpineExcel : ExcelResource
{
    [JsonProperty("ID")] public uint Id { get; set; }

    [JsonProperty("Node1Req")] public uint Node1Req { get; set; }
    [JsonProperty("Node2Req")] public uint Node2Req { get; set; }
    [JsonProperty("Node3Req")] public uint Node3Req { get; set; }
    [JsonProperty("Node4Req")] public uint Node4Req { get; set; }
    [JsonProperty("Node5Req")] public uint Node5Req { get; set; }
    [JsonProperty("Node6Req")] public uint Node6Req { get; set; }

    public uint GetNodeReq(int mastIdx) => mastIdx switch
    {
        1 => Node1Req,
        2 => Node2Req,
        3 => Node3Req,
        4 => Node4Req,
        5 => Node5Req,
        6 => Node6Req,
        _ => 0
    };

    public override uint GetId() => Id;

    public override void Loaded()
    {
        GameData.SpineData[Id] = this;
    }
}
