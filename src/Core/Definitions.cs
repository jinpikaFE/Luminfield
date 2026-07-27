namespace Luminfield.Core;

public enum ItemKind
{
    Tool,
    Seed,
    Produce,
    Artisan,
    Resource
}

public sealed record ItemDefinition(
    string Id,
    ItemKind Kind,
    int MaxStack,
    string NameKey,
    string? CropId = null,
    int BuyPrice = 0,
    int SellPrice = 0
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

public sealed record CropDefinition(
    string Id,
    string SeedItemId,
    string HarvestItemId,
    string NameKey,
    int[] StageDayThresholds,
    int AtlasStartIndex
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
}

public static class DataCatalog
{
    public const string LegacyHoeId = "hoe";
    public const string HandId = "hand";
    public const string ShovelId = "shovel";
    public const string MacheteId = "machete";
    public const string WateringCanId = "watering_can";
    public const string BucketId = "water_bucket";
    public const string StarbudSeedId = "starbud_seed";
    public const string StarbudId = "starbud";
    public const string MoonrootSeedId = "moonroot_seed";
    public const string MoonrootId = "moonroot";
    public const string StarbudPreserveId = "starbud_preserve";
    public const string MoonrootTonicId = "moonroot_tonic";
    public const string LumenwoodId = "lumenwood";
    public const string CrystalShardId = "crystal_shard";
    public const string StarbudPreserveRecipeId = "recipe_starbud_preserve";
    public const string MoonrootTonicRecipeId = "recipe_moonroot_tonic";

    public static readonly IReadOnlyDictionary<string, ItemDefinition> Items =
        new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
        {
            [HandId] = new(HandId, ItemKind.Tool, 1, "item.hand"),
            [ShovelId] = new(ShovelId, ItemKind.Tool, 1, "item.shovel"),
            [MacheteId] = new(MacheteId, ItemKind.Tool, 1, "item.machete"),
            [WateringCanId] = new(WateringCanId, ItemKind.Tool, 1, "item.watering_can"),
            [BucketId] = new(BucketId, ItemKind.Tool, 1, "item.water_bucket"),
            [StarbudSeedId] = new(
                StarbudSeedId,
                ItemKind.Seed,
                99,
                "item.starbud_seed",
                StarbudId,
                BuyPrice: 15
            ),
            [StarbudId] = new(
                StarbudId,
                ItemKind.Produce,
                99,
                "item.starbud",
                SellPrice: 22
            ),
            [MoonrootSeedId] = new(
                MoonrootSeedId,
                ItemKind.Seed,
                99,
                "item.moonroot_seed",
                MoonrootId,
                BuyPrice: 24
            ),
            [MoonrootId] = new(
                MoonrootId,
                ItemKind.Produce,
                99,
                "item.moonroot",
                SellPrice: 38
            ),
            [StarbudPreserveId] = new(
                StarbudPreserveId,
                ItemKind.Artisan,
                99,
                "item.starbud_preserve",
                SellPrice: 55
            ),
            [MoonrootTonicId] = new(
                MoonrootTonicId,
                ItemKind.Artisan,
                99,
                "item.moonroot_tonic",
                SellPrice: 90
            ),
            [LumenwoodId] = new(
                LumenwoodId,
                ItemKind.Resource,
                99,
                "item.lumenwood",
                SellPrice: 12
            ),
            [CrystalShardId] = new(
                CrystalShardId,
                ItemKind.Resource,
                99,
                "item.crystal_shard",
                SellPrice: 28
            )
        };

    public static readonly IReadOnlyDictionary<string, CropDefinition> Crops =
        new Dictionary<string, CropDefinition>(StringComparer.Ordinal)
        {
            [StarbudId] = new(
                StarbudId,
                StarbudSeedId,
                StarbudId,
                "crop.starbud",
                [0, 1, 2],
                0
            ),
            [MoonrootId] = new(
                MoonrootId,
                MoonrootSeedId,
                MoonrootId,
                "crop.moonroot",
                [0, 1, 2, 3],
                4
            )
        };

    public static readonly IReadOnlyDictionary<string, ProcessorRecipe> ProcessorRecipes =
        new Dictionary<string, ProcessorRecipe>(StringComparer.Ordinal)
        {
            [StarbudPreserveRecipeId] = new(
                StarbudPreserveRecipeId,
                StarbudId,
                2,
                StarbudPreserveId,
                1,
                1,
                "recipe.starbud_preserve"
            ),
            [MoonrootTonicRecipeId] = new(
                MoonrootTonicRecipeId,
                MoonrootId,
                2,
                MoonrootTonicId,
                1,
                1,
                "recipe.moonroot_tonic"
            )
        };

    public static ItemDefinition Item(string id) =>
        Items.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown item id '{id}'.");

    public static CropDefinition Crop(string id) =>
        Crops.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown crop id '{id}'.");

    public static ProcessorRecipe ProcessorRecipe(string id) =>
        ProcessorRecipes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown processor recipe id '{id}'.");
}
