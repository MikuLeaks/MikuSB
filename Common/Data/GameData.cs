using MikuSB.Data.Excel;

namespace MikuSB.Data;

public static class GameData
{
    public static Dictionary<uint, CardExcel> CardData { get; private set; } = [];
    public static Dictionary<uint, WeaponExcel> WeaponData { get; private set; } = [];
    public static Dictionary<uint, CardSkinExcel> CardSkinData { get; private set; } = [];
    public static Dictionary<uint, SuppliesExcel> SuppliesData { get; private set; } = [];
    public static List<SuppliesExcel> AllSuppliesData { get; private set; } = [];
    public static Dictionary<int, UpgradeExpExcel> UpgradeExpData { get; private set; } = [];
    public static Dictionary<int, BreakLevelLimitExcel> BreakLevelLimitData { get; private set; } = [];
    public static Dictionary<int, RecycleExcel> RecycleData { get; private set; } = [];
    public static Dictionary<uint, ChapterLevelExcel> ChapterLevelData { get; private set; } = [];
    public static Dictionary<uint, ArItemExcel> ArItemData { get; private set; } = [];
    public static Dictionary<uint, ManifestationExcel> ManifestationData { get; private set; } = [];
    public static Dictionary<uint, Rogue3DDifficultExcel> Rogue3DDifficultData { get; private set; } = [];
    public static Dictionary<uint, SpineExcel> SpineData { get; private set; } = [];
    public static Dictionary<uint, NodeConditionExcel> NodeConditionData { get; private set; } = [];
    public static List<SupportCardExcel> SupportCardData { get; private set; } = [];
}

public static class GameResourceTemplateId
{
    public static ulong FromGdpl(uint genre, uint detail, uint particular, uint level) =>
        ((ulong)level << 48) | ((ulong)particular << 32) | ((ulong)detail << 16) | genre;

    public static ulong FromGdpl(IReadOnlyList<uint> gdpl) =>
        gdpl.Count >= 4 ? FromGdpl(gdpl[0], gdpl[1], gdpl[2], gdpl[3]) : 0;
}