namespace Luminfield.Core;

public readonly record struct GridPosition(int X, int Y);

public static class PlayerLocationIds
{
    public const string World = "world";
    public const string Cottage = "cottage";
    public const string MoonlitArchive = "moonlit_archive";
    public const string MoonstoneWorkshop = "moonstone_workshop";
    public const string StarweaverTeaHouse = "starweaver_tea_house";
    public const string TwilightEmporium = "twilight_emporium";
    public const string StarlightPost = "starlight_post";
    public const string StarfallWatch = "starfall_watch";
    public const string Greenhouse = "greenhouse";
    public const string StarfeatherCoop = "starfeather_coop";
    public const string MoonfleeceBarn = "moonfleece_barn";
    public const string StarharvestMarket = "starharvest_market";
    public const string GleamrisePlantingFestival =
        "gleamrise_planting_festival";
    public const string LongnightLanternFeast =
        "longnight_lantern_feast";
    public const string FireflyTide = "firefly_tide";
    public const string CrystalGrottoSurvey = "crystal_grotto_survey";
    public const string StarfallRuinsTrial = "starfall_ruins_trial";

    public static bool IsValid(string? locationId) =>
        locationId is World or Cottage or MoonlitArchive or
            MoonstoneWorkshop or StarweaverTeaHouse or TwilightEmporium or
            StarlightPost or StarfallWatch or Greenhouse or
            StarfeatherCoop or MoonfleeceBarn or StarharvestMarket or
            GleamrisePlantingFestival or LongnightLanternFeast or
            FireflyTide or CrystalGrottoSurvey or StarfallRuinsTrial;

    public static string Normalize(
        string? locationId,
        bool legacyInsideCottage = false
    )
    {
        if (IsValid(locationId))
        {
            return locationId!;
        }

        return legacyInsideCottage ? Cottage : World;
    }
}

public sealed class InventorySlot
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(ItemId) || Count <= 0;

    public InventorySlot Clone() => new()
    {
        ItemId = ItemId,
        Count = Count
    };
}

public sealed class FarmTileState
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Tilled { get; set; }
    public bool Watered { get; set; }
    public string? FertilizerId { get; set; }
    public string? CropId { get; set; }
    public int WateredNights { get; set; }
    public int QualityRoll { get; set; } = -1;
    public int PlantedDay { get; set; }
    public string? ResonanceItemId { get; set; }

    public GridPosition Position => new(X, Y);

    public FarmTileState Clone() => new()
    {
        X = X,
        Y = Y,
        Tilled = Tilled,
        Watered = Watered,
        FertilizerId = FertilizerId,
        CropId = CropId,
        WateredNights = WateredNights,
        QualityRoll = QualityRoll,
        PlantedDay = PlantedDay,
        ResonanceItemId = ResonanceItemId
    };
}

public enum QuestStage
{
    TalkToMira,
    Till,
    Plant,
    Water,
    Grow,
    Harvest,
    ReturnToMira,
    Complete
}

public sealed class QuestSave
{
    public QuestStage Stage { get; set; } = QuestStage.TalkToMira;
    public int Tilled { get; set; }
    public int Planted { get; set; }
    public int Watered { get; set; }
    public int GrownNights { get; set; }
    public int Harvested { get; set; }
}

public sealed class PlayerSave
{
    public float X { get; set; } = GameSession.NewGamePlayerX;
    public float Y { get; set; } = GameSession.NewGamePlayerY;
    public int Energy { get; set; } = GameSession.MaxEnergy;
    public int WateringCanWater { get; set; } = GameSession.MaxWateringCanWater;
    public int SelectedSlot { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public bool InsideCottage { get; set; }
}

public sealed class ProcessorSave
{
    public string RecipeId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
    public List<ProcessorMachineSave> Machines { get; set; } = [];
}

public sealed class ProcessorMachineSave
{
    public string MachineId { get; set; } = string.Empty;
    public string RecipeId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
}

public sealed class ExplorationSave
{
    public List<string> DiscoveredChunks { get; set; } = [];
}

public sealed class ResourceSave
{
    public List<string> RemovedNodes { get; set; } = [];
    public List<ResourceDepletionSave> DepletedNodes { get; set; } = [];
}

public sealed class ResourceDepletionSave
{
    public string NodeId { get; set; } = string.Empty;
    public int RemovedDay { get; set; } = 1;
}

public sealed class MiningSave
{
    public List<string> DepletedVeinIds { get; set; } = [];
    public int DeepestRoomReached { get; set; }
    public int ExpeditionSeed { get; set; }
    public bool ExpeditionActive { get; set; }
    public int ExpeditionRoom { get; set; }
    public int ExpeditionEnemyHealth { get; set; }
    public float ExpeditionRetaliationProgress { get; set; }
    public int DeepestExpeditionRoom { get; set; }
    public int StableAnchorRoom { get; set; }
    public List<int> ClearedExpeditionRooms { get; set; } = [];
    public List<int> ExcavatedExpeditionRooms { get; set; } = [];
    public List<string> ClaimedExpeditionWeaponIds { get; set; } = [];
    public AdventureSkillSave CrystalMiningSkill { get; set; } = new();
    public AdventureSkillSave NightwatchSkill { get; set; } = new();
}

public sealed class AdventureSkillSave
{
    public int Experience { get; set; }
    public int Level { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
}

public sealed class StarGateSave
{
    public bool Activated { get; set; }
    public string LastDestinationId { get; set; } = string.Empty;
    public int TravelCount { get; set; }
}

public sealed class ToolProgressionSave
{
    public List<ToolProgressionEntrySave> Tools { get; set; } = [];
}

public sealed class ToolProgressionEntrySave
{
    public string ToolId { get; set; } = string.Empty;
    public string TierId { get; set; } = string.Empty;
    public string ActiveUpgradeId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
}

public sealed class ForageSave
{
    public int ResolvedDay { get; set; } = 1;
    public List<ForageSpawnSave> Spawns { get; set; } = [];
}

public sealed class GatheringSkillSave
{
    public int Experience { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
}

public sealed class StellarResonanceSave
{
    public bool MainStoryCompleted { get; set; }
    public int CompletionDay { get; set; }
    public int Experience { get; set; }
}

public sealed class ForageSpawnSave
{
    public string SlotId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public bool Collected { get; set; }
}

public sealed class FishingSave
{
    public List<string> CaughtFishIds { get; set; } = [];
    public List<string> ClaimedRewardIds { get; set; } = [];
    public List<string> DonatedFishIds { get; set; } = [];
    public string RodTierId { get; set; } = string.Empty;
    public List<string> OwnedBobberIds { get; set; } = [];
    public string EquippedBaitId { get; set; } = string.Empty;
    public string EquippedBobberId { get; set; } = string.Empty;
    public int Experience { get; set; }
    public int Level { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
    public List<CrabPotSave> CrabPots { get; set; } = [];
}

public sealed class CrabPotSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public string BaitItemId { get; set; } = string.Empty;
    public string CatchItemId { get; set; } = string.Empty;
}

public sealed class WeatherSave
{
    public int Day { get; set; }
    public string CurrentId { get; set; } = string.Empty;
    public string ForecastId { get; set; } = string.Empty;
}

public sealed class ShippingEntrySave
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
    public int UnitPrice { get; set; }
}

public sealed class ShippingSettlementSave
{
    public int Day { get; set; }
    public List<ShippingEntrySave> Entries { get; set; } = [];
}

public sealed class ShippingSave
{
    public List<ShippingEntrySave> Pending { get; set; } = [];
    public ShippingSettlementSave LastSettlement { get; set; } = new();
}

public sealed class PlacedChestSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public List<InventorySlot> Items { get; set; } = [];
}

public sealed class StorageSave
{
    public List<PlacedChestSave> Chests { get; set; } = [];
}

public sealed class PlacedFarmObjectSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public string ItemId { get; set; } = string.Empty;
}

public sealed class FarmObjectSave
{
    public List<PlacedFarmObjectSave> Objects { get; set; } = [];
}

public sealed class FruitTreeSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public string TreeId { get; set; } = string.Empty;
    public int AgeNights { get; set; }
    public bool FruitReady { get; set; }
    public int RegrowthProgress { get; set; }
}

public sealed class BeehiveSave
{
    public int X { get; set; }
    public int Y { get; set; }
    public int PendingHoney { get; set; }
    public int ProgressNights { get; set; }
}

public sealed class OrchardSave
{
    public List<FruitTreeSave> FruitTrees { get; set; } = [];
    public List<BeehiveSave> Beehives { get; set; } = [];
}

public sealed class DailyCommissionSave
{
    public int Day { get; set; } = 1;
    public string DefinitionId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public int Progress { get; set; }
    public bool Claimed { get; set; }
}

public sealed class WeeklyCommissionSave
{
    public int Week { get; set; } = 1;
    public string DefinitionId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string StageId { get; set; } = string.Empty;
    public int Progress { get; set; }
    public bool Claimed { get; set; }
}

public sealed class StarlightContributionSave
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class StarlightNodeSave
{
    public string NodeId { get; set; } = string.Empty;
    public List<StarlightContributionSave> Contributions { get; set; } = [];
}

public sealed class StarlightSave
{
    public string PedestalId { get; set; } = string.Empty;
    public bool Discovered { get; set; }
    public bool RewardUnlocked { get; set; }
    public List<StarlightNodeSave> Nodes { get; set; } = [];
    public List<StarlightPedestalSave> Pedestals { get; set; } = [];
}

public sealed class StarlightPedestalSave
{
    public string PedestalId { get; set; } = string.Empty;
    public bool Discovered { get; set; }
    public bool RewardUnlocked { get; set; }
    public List<StarlightNodeSave> Nodes { get; set; } = [];
}

public sealed class StarlightStoryEntrySave
{
    public string BeatId { get; set; } = string.Empty;
    public int CompletedDay { get; set; } = 1;
}

public sealed class StarlightStorySave
{
    public List<StarlightStoryEntrySave> Entries { get; set; } = [];
}

public sealed class VillageSave
{
    public List<string> MetNpcIds { get; set; } = [];
    public List<VillageRelationshipSave> Relationships { get; set; } = [];
}

public sealed class VillageRelationshipSave
{
    public string NpcId { get; set; } = string.Empty;
    public int Points { get; set; }
    public int LastTalkDay { get; set; }
    public int LastGiftDay { get; set; }
}

public sealed class MailEntrySave
{
    public string MailId { get; set; } = string.Empty;
    public int DeliveredDay { get; set; } = 1;
    public bool IsRead { get; set; }
    public bool AttachmentClaimed { get; set; }
}

public sealed class MailSave
{
    public List<MailEntrySave> Entries { get; set; } = [];
}

public sealed class CharacterEventEntrySave
{
    public string EventId { get; set; } = string.Empty;
    public int CompletedDay { get; set; } = 1;
}

public sealed class CharacterEventSave
{
    public List<CharacterEventEntrySave> Entries { get; set; } = [];
}

public sealed class GroupCharacterEventEntrySave
{
    public string EventId { get; set; } = string.Empty;
    public int CompletedDay { get; set; } = 1;
}

public sealed class GroupCharacterEventSave
{
    public List<GroupCharacterEventEntrySave> Entries { get; set; } = [];
}

public sealed class ConstructionSave
{
    public string ProjectId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
    public bool Completed { get; set; }
    public List<ConstructionProjectSave> Projects { get; set; } = [];
}

public sealed class ConstructionProjectSave
{
    public string ProjectId { get; set; } = string.Empty;
    public int RemainingNights { get; set; }
    public bool Completed { get; set; }
}

public sealed class GreenhouseSave
{
    public List<FarmTileState> Tiles { get; set; } = [];
}

public sealed class KitchenSave
{
    public List<InventorySlot> PantryItems { get; set; } = [];
}

public sealed class AnimalSave
{
    public List<AnimalEntrySave> Animals { get; set; } = [];
    public List<AnimalBuildingAutomationSave> Automation { get; set; } = [];
}

public sealed class AnimalBuildingAutomationSave
{
    public string BuildingId { get; set; } = string.Empty;
    public int StoredFeed { get; set; }
    public List<ShippingEntrySave> StoredProducts { get; set; } = [];
    public int LastResolvedDay { get; set; }
    public int LastAutoFedCount { get; set; }
    public int LastAutoCollectedCount { get; set; }
    public string LastFeedStatusId { get; set; } = string.Empty;
    public string LastCollectionStatusId { get; set; } = string.Empty;
}

public sealed class AnimalEntrySave
{
    public string InstanceId { get; set; } = string.Empty;
    public string SpeciesId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public int AgeNights { get; set; }
    public int Mood { get; set; } = AnimalSystem.InitialMood;
    public int LastFedDay { get; set; }
    public int LastPettedDay { get; set; }
    public int ProductionProgress { get; set; }
    public string PendingProductItemId { get; set; } = string.Empty;
}

public sealed class FarmingSkillSave
{
    public int Experience { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
}

public sealed class GleamriseGoalEntrySave
{
    public string GoalId { get; set; } = string.Empty;
    public int ClaimedDay { get; set; }
}

public sealed class GleamriseGoalCounterSave
{
    public string CounterId { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class GleamriseSeasonSave
{
    public int Year { get; set; } = 1;
    public string SeasonId { get; set; } = CalendarSystem.GleamriseSeasonId;
    public List<GleamriseGoalEntrySave> Goals { get; set; } = [];
    public List<GleamriseGoalCounterSave> Counters { get; set; } = [];
}

public sealed class FestivalYearResultSave
{
    public string FestivalId { get; set; } = string.Empty;
    public int Year { get; set; } = 1;
    public List<string> ItemIds { get; set; } = [];
    public int Score { get; set; }
    public string AwardId { get; set; } = string.Empty;
    public int AuctionCoins { get; set; }
    public List<FestivalPlotPlantingSave> Plantings { get; set; } = [];
    public string GiftItemId { get; set; } = string.Empty;
    public string GiftRewardItemId { get; set; } = string.Empty;
    public string RitualId { get; set; } = string.Empty;
}

public sealed class FestivalPlotPlantingSave
{
    public string PlotId { get; set; } = string.Empty;
    public string SeedItemId { get; set; } = string.Empty;
}

public sealed class FestivalPlantingAttemptSave
{
    public string FestivalId { get; set; } = string.Empty;
    public int Year { get; set; } = 1;
    public int StartedMinute { get; set; }
    public List<string> SelectedSeedItemIds { get; set; } = [];
    public string ActiveSeedItemId { get; set; } = string.Empty;
    public List<FestivalPlotPlantingSave> Plantings { get; set; } = [];
}

public sealed class FestivalCurrencySave
{
    public string CurrencyId { get; set; } = string.Empty;
    public int Balance { get; set; }
}

public sealed class FestivalSave
{
    public int Scrip { get; set; }
    public List<FestivalYearResultSave> Results { get; set; } = [];
    public List<FestivalPlantingAttemptSave> PlantingAttempts { get; set; } = [];
    public List<FestivalCurrencySave> CurrencyBalances { get; set; } = [];
}

public sealed class CollectionSave
{
    public bool Initialized { get; set; }
    public List<string> InitializedCategoryIds { get; set; } = [];
    public List<string> DiscoveredEntryIds { get; set; } = [];
    public List<string> DonatedEntryIds { get; set; } = [];
    public List<string> ClaimedRewardIds { get; set; } = [];
}

public sealed class CombatSave
{
    public int CurrentHealth { get; set; } = CombatSystem.MaxHealth;
}

public sealed class StarfallRuinsTrialSave
{
    public bool WeaponClaimed { get; set; }
    public List<string> ClearedRoomIds { get; set; } = [];
    public List<string> RecoveredArtifactIds { get; set; } = [];
}

public sealed class ExperienceGuidanceSave
{
    public int LastMorningBriefingDay { get; set; }
}

public sealed class TeaHouseSave
{
    public int Day { get; set; } = 1;
    public string PurchasedOfferId { get; set; } = string.Empty;
    public string ActiveEffectId { get; set; } = string.Empty;
    public int EffectExpiresMinuteOfDay { get; set; }
    public bool GatheringHosted { get; set; }
    public List<string> GatheringGuestNpcIds { get; set; } = [];
}

public sealed class PostDeliverySave
{
    public int Day { get; set; } = 1;
    public string ActiveRouteId { get; set; } = string.Empty;
    public List<string> CompletedRouteIds { get; set; } = [];
}

public sealed class StarfallWatchSave
{
    public int Day { get; set; } = 1;
    public string ActivePatrolId { get; set; } = string.Empty;
    public bool PatrolTargetReached { get; set; }
    public List<string> CompletedPatrolIds { get; set; } = [];
    public string ActiveBountyId { get; set; } = string.Empty;
    public int ActiveBountyProgress { get; set; }
    public string FailedBountyId { get; set; } = string.Empty;
    public List<string> CompletedBountyIds { get; set; } = [];
    public string PreparationId { get; set; } = string.Empty;
    public bool PreparationConsumed { get; set; }
}

public sealed class GameSaveV1
{
    public int SchemaVersion { get; set; } = SaveService.CurrentSchemaVersion;
    public int Day { get; set; } = 1;
    public int MinuteOfDay { get; set; } = GameClock.StartMinute;
    public string Locale { get; set; } = LocaleService.SimplifiedChinese;
    public PlayerSave Player { get; set; } = new();
    public List<InventorySlot> Inventory { get; set; } = [];
    public List<FarmTileState> FarmTiles { get; set; } = [];
    public GreenhouseSave Greenhouse { get; set; } = new();
    public KitchenSave Kitchen { get; set; } = new();
    public AnimalSave Animals { get; set; } = new();
    public QuestSave Quest { get; set; } = new();
    public int Coins { get; set; } = GameSession.NewGameCoins;
    public ProcessorSave Processor { get; set; } = new();
    public ExplorationSave Exploration { get; set; } = new();
    public ResourceSave Resources { get; set; } = new();
    public MiningSave Mining { get; set; } = new();
    public ToolProgressionSave ToolProgression { get; set; } = new();
    public CombatSave Combat { get; set; } = new();
    public StarfallRuinsTrialSave StarfallRuinsTrial { get; set; } = new();
    public ForageSave Forage { get; set; } = new();
    public GatheringSkillSave GatheringSkill { get; set; } = new();
    public FishingSave Fishing { get; set; } = new();
    public WeatherSave Weather { get; set; } = new();
    public ShippingSave Shipping { get; set; } = new();
    public StorageSave Storage { get; set; } = new();
    public FarmObjectSave FarmObjects { get; set; } = new();
    public OrchardSave Orchard { get; set; } = new();
    public DailyCommissionSave Commission { get; set; } = new();
    public WeeklyCommissionSave WeeklyCommission { get; set; } = new();
    public StarlightSave Starlight { get; set; } = new();
    public StarlightStorySave StarlightStory { get; set; } = new();
    public VillageSave Village { get; set; } = new();
    public MailSave Mail { get; set; } = new();
    public CharacterEventSave CharacterEvents { get; set; } = new();
    public GroupCharacterEventSave GroupCharacterEvents { get; set; } = new();
    public ConstructionSave Construction { get; set; } = new();
    public FarmingSkillSave FarmingSkill { get; set; } = new();
    public GleamriseSeasonSave GleamriseSeason { get; set; } = new();
    public FestivalSave Festival { get; set; } = new();
    public CollectionSave Collection { get; set; } = new();
    public ExperienceGuidanceSave ExperienceGuidance { get; set; } = new();
    public TeaHouseSave TeaHouse { get; set; } = new();
    public PostDeliverySave PostDelivery { get; set; } = new();
    public StarfallWatchSave StarfallWatch { get; set; } = new();
    public StarGateSave StarGate { get; set; } = new();
    public StellarResonanceSave StellarResonance { get; set; } = new();
}

public sealed record ActionResult(
    bool Succeeded,
    int EnergyCost = 0,
    string MessageKey = "",
    string? GrantedItemId = null,
    int GrantedItemCount = 0
)
{
    public static ActionResult Fail(string messageKey) => new(false, 0, messageKey);
    public static ActionResult Success(int energyCost = 0, string messageKey = "") =>
        new(true, energyCost, messageKey);

    public static ActionResult Grant(
        string itemId,
        int count,
        int energyCost,
        string messageKey
    ) => new(true, energyCost, messageKey, itemId, count);
}

public enum TargetPreviewState
{
    Neutral,
    Available,
    NeedsTool,
    Blocked
}

public enum TargetPreviewKind
{
    None,
    Ground,
    Soil,
    Crop,
    Tree,
    Crystal,
    MineralVein,
    CrystalGrottoPortal,
    CrystalGrottoExit,
    ToolUpgradeBench,
    MineDepthAnchor,
    GrottoSeal,
    Forage,
    Water,
    CrabPot,
    Landmark,
    StarlightPedestal,
    Mailbox,
    Character,
    Door,
    Station,
    ArchiveResearchDesk,
    HomesteadWorkshop,
    GreenhousePortal,
    GreenhouseExit,
    Cistern,
    CommissionBoard,
    StorageChest,
    Path,
    Fence,
    Torch,
    Sprinkler,
    FruitTree,
    Beehive,
    Bed,
    KitchenReserve,
    KitchenStation,
    IngredientPantry,
    FestivalPortal,
    FestivalExit,
    FestivalExhibit,
    FestivalBidBoard,
    FestivalShop,
    FestivalPlantingPlot,
    FestivalSeedRack,
    FestivalSeedExchange,
    FestivalFeastTable,
    FestivalGiftExchange,
    FestivalRitual,
    FestivalLanternLaunch,
    FestivalFishBasin,
    FestivalTideAltar,
    AnimalBuildingPortal,
    AnimalBuildingExit,
    AnimalFeedTrough,
    AnimalNest,
    Animal,
    MoonfleeceBarnPortal,
    MoonfleeceBarnExit,
    AnimalProductStation,
    MoonfleeceSheep,
    DewhornMilkingStation,
    Dewhorn,
    AnimalAutomationStation,
    StarfallRuinsPortal,
    StarfallRuinsExit,
    RuinsWeaponRack,
    RuinsEnemy,
    RuinsArtifact,
    RuinsSeal,
    StarGate
}

public sealed record TargetPreview(
    GridPosition Target,
    TargetPreviewState State,
    TargetPreviewKind Kind,
    string LabelKey = ""
)
{
    public bool IsAvailable => State == TargetPreviewState.Available;

    public static TargetPreview Neutral(GridPosition target) =>
        new(target, TargetPreviewState.Neutral, TargetPreviewKind.None);

    public static TargetPreview Available(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.Available, kind, labelKey);

    public static TargetPreview NeedsTool(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.NeedsTool, kind, labelKey);

    public static TargetPreview Blocked(
        GridPosition target,
        TargetPreviewKind kind,
        string labelKey
    ) => new(target, TargetPreviewState.Blocked, kind, labelKey);
}

public sealed record InteractionContext(GameSession Session, GridPosition Target);

public interface IInteractable
{
    void Interact(InteractionContext context);
}

public interface IToolAction
{
    ActionResult TryApply(GameSession session, GridPosition target);
}
