namespace Luminfield.Core;

public enum ItemKind
{
    Tool,
    Seed,
    Produce
}

public sealed record ItemDefinition(
    string Id,
    ItemKind Kind,
    int MaxStack,
    string NameKey,
    string? CropId = null
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
    public const string HoeId = "hoe";
    public const string WateringCanId = "watering_can";
    public const string StarbudSeedId = "starbud_seed";
    public const string StarbudId = "starbud";
    public const string MoonrootSeedId = "moonroot_seed";
    public const string MoonrootId = "moonroot";

    public static readonly IReadOnlyDictionary<string, ItemDefinition> Items =
        new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
        {
            [HoeId] = new(HoeId, ItemKind.Tool, 1, "item.hoe"),
            [WateringCanId] = new(WateringCanId, ItemKind.Tool, 1, "item.watering_can"),
            [StarbudSeedId] = new(StarbudSeedId, ItemKind.Seed, 99, "item.starbud_seed", StarbudId),
            [StarbudId] = new(StarbudId, ItemKind.Produce, 99, "item.starbud"),
            [MoonrootSeedId] = new(MoonrootSeedId, ItemKind.Seed, 99, "item.moonroot_seed", MoonrootId),
            [MoonrootId] = new(MoonrootId, ItemKind.Produce, 99, "item.moonroot")
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

    public static ItemDefinition Item(string id) =>
        Items.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown item id '{id}'.");

    public static CropDefinition Crop(string id) =>
        Crops.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown crop id '{id}'.");
}
