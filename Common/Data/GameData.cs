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
    public static Dictionary<uint, RoleLevelExcel> RoleLevelData { get; private set; } = [];
    public static Dictionary<uint, ArItemExcel> ArItemData { get; private set; } = [];
    public static Dictionary<uint, ManifestationExcel> ManifestationData { get; private set; } = [];
    public static Dictionary<uint, Rogue3DDifficultExcel> Rogue3DDifficultData { get; private set; } = [];
    public static Dictionary<uint, Rogue3DSeasonExcel> Rogue3DSeasonData { get; private set; } = [];
    public static Dictionary<uint, Rogue3DTalentExcel> Rogue3DTalentData { get; private set; } = [];
    public static Dictionary<uint, Rogue3DDailyBuffExcel> Rogue3DDailyBuffData { get; private set; } = [];
    public static Dictionary<int, BreakExcel> BreakData { get; private set; } = [];
    public static Dictionary<int, SpecialBreakExcel> SpecialBreakData { get; private set; } = [];
    public static Dictionary<uint, SpineExcel> SpineData { get; private set; } = [];
    public static Dictionary<uint, NodeConditionExcel> NodeConditionData { get; private set; } = [];
    public static List<SupportCardExcel> SupportCardData { get; private set; } = [];
    public static Dictionary<int, SupportAffixExcel> SupportAffixData { get; private set; } = [];
    public static Dictionary<int, SupportAffixPoolExcel> SupportAffixPoolData { get; private set; } = [];
    public static Dictionary<int, SupportFixedExcel> SupportFixedData { get; private set; } = [];
    public static Dictionary<uint, WeaponSkinExcel> WeaponSkinData { get; private set; } = [];
    public static Dictionary<uint, DailyLevelExcel> DailyLevelData { get; private set; } = [];
    public static Dictionary<uint, BossPvpBossChallengeExcel> BossPvpBossChallengeData { get; private set; } = [];
    public static Dictionary<uint, BossPvpBossExcel> BossPvpBossData { get; private set; } = [];
    public static Dictionary<uint, BossPvpNumExcel> BossPvpNumData { get; private set; } = [];
    public static Dictionary<uint, ClimbTowerTimeExcel> ClimbTowerTimeData { get; private set; } = [];
    public static Dictionary<uint, ClimbTowerDiffExcel> ClimbTowerDiffData { get; private set; } = [];
    public static Dictionary<uint, Dictionary<int, ClimbTowerAwardExcel>> ClimbTowerAwardData { get; private set; } = [];
    public static Dictionary<uint, ClimbTowerLevelOrderExcel> ClimbTowerLevelOrderData { get; private set; } = [];
    public static Dictionary<uint, TowerLevelExcel> TowerLevelData { get; private set; } = [];
    public static Dictionary<uint, TowerEventLevelExcel> TowerEventLevelData { get; private set; } = [];
    public static Dictionary<uint, OtherItemExcel> OtherItemData { get; private set; } = [];
    public static Dictionary<uint, ProfileExcel> ProfileData { get; private set; } = [];
    public static Dictionary<uint, CardSkinPartsExcel> CardSkinPartsData { get; private set; } = [];
    public static Dictionary<uint, CallItemExcel> CallItemData { get; private set; } = [];
    public static Dictionary<uint, WeaponPartsExcel> WeaponPartsData { get; private set; } = [];
    public static Dictionary<uint, GuideExcel> GuideData { get; private set; } = [];
    public static Dictionary<uint, DormGiftExcel> DormGiftData { get; private set; } = [];
    public static Dictionary<uint, HouseFurniturePosExcel> HouseFurniturePosData { get; private set; } = [];
    public static Dictionary<uint, GachaExcel> GachaData { get; private set; } = [];
    public static Dictionary<uint, GachaProbabilityExcel> GachaProbabilityData { get; private set; } = [];
    public static Dictionary<string, List<GachaPoolItem>> GachaPoolData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureTimeExcel> VirCaptureTimeData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureSeasonExcel> VirCaptureSeasonData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureTrialTimeExcel> VirCaptureTrialTimeData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureCaptureRegionExcel> VirCaptureCaptureRegionData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureLevelListExcel> VirCaptureLevelListData { get; private set; } = [];
    public static Dictionary<ulong, MonsterCardExcel> MonsterCardData { get; private set; } = [];
    public static Dictionary<uint, FishingFoodExcel> FishingFoodData { get; private set; } = [];
    public static Dictionary<uint, VirCaptureTowerExcel> VirCaptureTowerData { get; private set; } = [];
    public static Dictionary<uint, DreamCardActivityExcel> DreamCardActivityData { get; private set; } = [];
    public static Dictionary<uint, DlcActivityExcel> DlcActivityData { get; private set; } = [];
    public static Dictionary<uint, BattlePassTimeExcel> BattlePassTimeData { get; private set; } = [];
    public static Dictionary<uint, IbGoodsExcel> IbGoodsData { get; private set; } = [];
}

public static class GameResourceTemplateId
{
    public static ulong FromGdpl(uint genre, uint detail, uint particular, uint level) =>
        ((ulong)level << 48) | ((ulong)particular << 32) | ((ulong)detail << 16) | genre;

    public static ulong FromGdpl(IReadOnlyList<uint> gdpl) =>
        gdpl.Count >= 4 ? FromGdpl(gdpl[0], gdpl[1], gdpl[2], gdpl[3]) : 0;
}