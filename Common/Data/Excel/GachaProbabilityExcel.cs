namespace MikuSB.Data.Excel;

[ResourceEntity("gacha/probability.json")]
public class GachaProbabilityExcel : ExcelResource
{
    public uint ID { get; set; }
    public int Rarity1 { get; set; }
    public int Rarity2 { get; set; }
    public int Rarity3 { get; set; }
    public int Rarity4 { get; set; }
    public int Rarity5 { get; set; }
    public int Rarity6 { get; set; }

    public int[] Weights => [Rarity1, Rarity2, Rarity3, Rarity4, Rarity5, Rarity6];

    public override uint GetId() => ID;
    public override void Loaded() => GameData.GachaProbabilityData[ID] = this;
}
