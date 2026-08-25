namespace Luminfield.Core;

public enum ItemKind
{
    Tool,
    Seed,
    Sapling,
    Produce,
    Fish,
    Fertilizer,
    Artisan,
    AnimalFeed,
    AnimalProduct,
    CookedDish,
    Resource,
    Placeable,
    Weapon,
    Artifact
}

public enum CropQuality
{
    Regular,
    Luminous,
    Starlight
}

public sealed record ItemDefinition(
    string Id,
    ItemKind Kind,
    int MaxStack,
    string NameKey,
    string? CropId = null,
    int BuyPrice = 0,
    int SellPrice = 0,
    string? BaseItemId = null,
    CropQuality Quality = CropQuality.Regular,
    string? FruitTreeId = null
);

public sealed record ProcessorRecipe(
    string Id,
    string InputItemId,
    int InputCount,
    string OutputItemId,
    int OutputCount,
    int Nights,
    string NameKey
);

public sealed record ProcessorMachineDefinition(
    string Id,
    GridPosition Position,
    string NameKey,
    IReadOnlyList<string> RecipeIds
);

public sealed record CraftingIngredient(string ItemId, int Count);

public sealed record CraftingRecipe(
    string Id,
    string OutputItemId,
    int OutputCount,
    IReadOnlyList<CraftingIngredient> Ingredients,
    string NameKey
);

public sealed record CookingRecipeDefinition(
    string Id,
    string OutputItemId,
    int OutputCount,
    IReadOnlyList<CraftingIngredient> Ingredients,
    string NameKey
);

public sealed record CookedDishDefinition(
    string ItemId,
    int EnergyRestore
);

public enum FarmObjectKind
{
    Path,
    Fence,
    Torch,
    Sprinkler,
    Beehive
}

public enum FarmObjectSurface
{
    Ground,
    PlantingBed
}

public sealed record FarmObjectDefinition(
    string ItemId,
    FarmObjectKind Kind,
    FarmObjectSurface Surface,
    bool BlocksMovement
);

public enum DailyCommissionKind
{
    Plant,
    Gather,
    Deliver
}

public sealed record DailyCommissionDefinition(
    string Id,
    DailyCommissionKind Kind,
    string TargetId,
    int RequiredCount,
    int RewardCoins,
    string TitleKey,
    string DescriptionKey
);

public enum WeeklyCommissionStageKind
{
    Plant,
    Gather,
    Deliver
}

public sealed record WeeklyCommissionStageDefinition(
    string Id,
    WeeklyCommissionStageKind Kind,
    string TargetId,
    int RequiredCount,
    string DescriptionKey
);

public sealed record WeeklyCommissionDefinition(
    string Id,
    string TitleKey,
    IReadOnlyList<WeeklyCommissionStageDefinition> Stages,
    int RewardCoins,
    string RewardItemId,
    int RewardItemCount
);

public sealed record StarlightContributionOption(
    string ItemId,
    int MaximumCount
);

public enum StarlightNodeSourceKind
{
    Inventory,
    FestivalResults,
    Milestones,
    PedestalRewards
}

public sealed record StarlightNodeDefinition(
    string Id,
    string TitleKey,
    string DescriptionKey,
    int RequiredCount,
    IReadOnlyList<StarlightContributionOption> Options,
    StarlightNodeSourceKind SourceKind = StarlightNodeSourceKind.Inventory,
    IReadOnlyList<string>? SourceIds = null
);

public sealed record StarlightPedestalDefinition(
    string Id,
    string NameKey,
    string RegionKey,
    string RewardTitleKey,
    string RewardDescriptionKey,
    IReadOnlyList<StarlightNodeDefinition> Nodes,
    string ActivationMessageKey = "starlight.activated",
    string RewardId = "",
    bool RequiresManualActivation = false
);

public sealed record WeatherDefinition(
    string Id,
    string NameKey,
    int AtlasIndex,
    bool AutoWatersCrops = false,
    string? EffectKey = null,
    float OutdoorMovementMultiplier = 1f
);

public sealed record CropResonanceDefinition(
    string ItemId,
    string WeatherId,
    int RollModulo,
    int RollResidue
);

public sealed record CropDefinition(
    string Id,
    string SeedItemId,
    string HarvestItemId,
    string NameKey,
    int[] StageDayThresholds,
    int AtlasStartIndex,
    IReadOnlyList<string>? SeasonIds = null,
    int RegrowthNights = 0,
    IReadOnlyList<CropResonanceDefinition>? Resonances = null
)
{
    public int MatureAfterWateredNights => StageDayThresholds[^1];

    public int GetStageIndex(int wateredNights)
    {
        var stage = 0;
        for (var index = 0; index < StageDayThresholds.Length; index++)
        {
            if (wateredNights >= StageDayThresholds[index])
            {
                stage = index;
            }
        }

        return stage;
    }

    public bool IsMature(int wateredNights) => wateredNights >= MatureAfterWateredNights;

    public bool IsAvailableOnDay(int day) =>
        SeasonIds is not { Count: > 0 } ||
        SeasonIds.Contains(
            CalendarSystem.SeasonId(day),
            StringComparer.Ordinal
        );

    public bool AllowsResonanceItem(string? itemId) =>
        !string.IsNullOrWhiteSpace(itemId) &&
        Resonances is { Count: > 0 } &&
        Resonances.Any(resonance => resonance.ItemId == itemId);

    public int RegrowthWateredNights => Math.Max(
        0,
        MatureAfterWateredNights - RegrowthNights
    );

    public int GetVisualStageIndex(int wateredNights)
    {
        if (MatureAfterWateredNights <= 0)
        {
            return 3;
        }

        return Math.Clamp(
            wateredNights * 3 / MatureAfterWateredNights,
            0,
            3
        );
    }
}

public sealed record FruitTreeDefinition(
    string Id,
    string SaplingItemId,
    string HarvestItemId,
    string NameKey,
    int MatureAfterNights,
    int RegrowthNights,
    IReadOnlyList<string>? SeasonIds = null
)
{
    public bool IsAvailableOnDay(int day) =>
        SeasonIds is not { Count: > 0 } ||
        SeasonIds.Contains(
            CalendarSystem.SeasonId(day),
            StringComparer.Ordinal
        );
}

public enum FishingWaterKind
{
    HomesteadPond,
    CrystalStream,
    MoonwaterWetlands
}

public sealed record FishDefinition(
    string Id,
    string ItemId,
    FishingWaterKind WaterKind,
    string NameKey,
    IReadOnlyList<string>? SeasonIds = null,
    int StartMinute = GameClock.StartMinute,
    int EndMinute = GameClock.EndMinute,
    string? WeatherId = null
)
{
    public bool IsAvailable(int day, int minuteOfDay, string weatherId) =>
        minuteOfDay >= StartMinute &&
        minuteOfDay <= EndMinute &&
        (SeasonIds is not { Count: > 0 } ||
            SeasonIds.Contains(
                CalendarSystem.SeasonId(day),
                StringComparer.Ordinal
            )) &&
        (WeatherId is null || WeatherId == weatherId);

    public int AvailabilitySpecificity
    {
        get
        {
            var score = 0;
            if (SeasonIds is { Count: > 0 })
            {
                score += 1;
            }

            if (WeatherId is not null)
            {
                score += 1;
            }

            if (StartMinute != GameClock.StartMinute ||
                EndMinute != GameClock.EndMinute)
            {
                score += 1;
            }

            return score;
        }
    }
}
