namespace Luminfield.Core;

public static class CollectionCategoryIds
{
    public const string Crops = "codex_crops";
    public const string Cooking = "codex_cooking";
    public const string Artisan = "codex_artisan";
    public const string Forage = "codex_forage";
    public const string Fish = "codex_fish";
    public const string Minerals = "codex_minerals";
    public const string Artifacts = "codex_artifacts";
    public const string Enemies = "codex_enemies";
}

public static class CollectionRewardIds
{
    public const string MoonlitAlmanac =
        "codex_reward_moonlit_almanac";
    public const string MoonhearthRecipeJournal =
        "codex_reward_moonhearth_recipe_journal";
    public const string StarlitAppraisalLedger =
        "codex_reward_starlit_appraisal_ledger";
    public const string StarpathForagersGuide =
        "codex_reward_starpath_foragers_guide";
}

public enum CompendiumEntryKind
{
    Crop,
    CookedDish,
    ArtisanGood,
    Forage,
    Fish,
    Mineral,
    Artifact,
    Enemy
}

public sealed record CompendiumCategoryDefinition(
    string Id,
    string NameKey,
    string TitleKey,
    string UndiscoveredDescriptionKey,
    string DiscoveryNoticeKey,
    IReadOnlyList<string> EntryIds
);

public sealed record CompendiumEntryDefinition(
    string Id,
    string CategoryId,
    CompendiumEntryKind Kind,
    string NameKey,
    string ItemId,
    string CropId = "",
    string SeedItemId = ""
)
{
    public string HarvestItemId => ItemId;
}

public sealed record CompendiumRewardDefinition(
    string Id,
    string CategoryId,
    string NameKey,
    string DescriptionKey,
    string ClaimedMessageKey,
    IReadOnlyList<string> RequiredEntryIds
);

public static class CompendiumCatalog
{
    public const int MoonhearthRecipeJournalEnergyBonus = 5;

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        CropEntries = DataCatalog.CropIds
            .Select(cropId =>
            {
                var crop = DataCatalog.Crop(cropId);
                return new CompendiumEntryDefinition(
                    crop.Id,
                    CollectionCategoryIds.Crops,
                    CompendiumEntryKind.Crop,
                    crop.NameKey,
                    crop.HarvestItemId,
                    crop.Id,
                    crop.SeedItemId
                );
            })
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        CookingEntries = DataCatalog.CookedDishItemIds
            .Select(itemId => new CompendiumEntryDefinition(
                itemId,
                CollectionCategoryIds.Cooking,
                CompendiumEntryKind.CookedDish,
                DataCatalog.Item(itemId).NameKey,
                itemId
            ))
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        ArtisanEntries =
        new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.StarhoneyId
        }
        .Select(itemId => new CompendiumEntryDefinition(
            itemId,
            CollectionCategoryIds.Artisan,
            CompendiumEntryKind.ArtisanGood,
            DataCatalog.Item(itemId).NameKey,
            itemId
        ))
        .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        ForageEntries = ForageCatalog.Definitions
            .Select(definition => new CompendiumEntryDefinition(
                definition.ItemId,
                CollectionCategoryIds.Forage,
                CompendiumEntryKind.Forage,
                DataCatalog.Item(definition.ItemId).NameKey,
                definition.ItemId
            ))
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        FishEntries = DataCatalog.FishItemIds
            .Select(itemId => new CompendiumEntryDefinition(
                itemId,
                CollectionCategoryIds.Fish,
                CompendiumEntryKind.Fish,
                DataCatalog.Fishes[itemId].NameKey,
                itemId
            ))
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        MineralEntries = MiningCatalog.Minerals
            .Select(mineral => new CompendiumEntryDefinition(
                mineral.ItemId,
                CollectionCategoryIds.Minerals,
                CompendiumEntryKind.Mineral,
                DataCatalog.Item(mineral.ItemId).NameKey,
                mineral.ItemId
            ))
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        ArtifactEntries = StarfallRuinsTrialCatalog.Artifacts
            .Select(artifact => new CompendiumEntryDefinition(
                artifact.ItemId,
                CollectionCategoryIds.Artifacts,
                CompendiumEntryKind.Artifact,
                DataCatalog.Item(artifact.ItemId).NameKey,
                artifact.ItemId
            ))
            .ToArray();

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        EnemyEntries = StarfallRuinsTrialCatalog.Enemies
            .Select(enemy => new CompendiumEntryDefinition(
                enemy.Id,
                CollectionCategoryIds.Enemies,
                CompendiumEntryKind.Enemy,
                $"enemy.{enemy.Id["enemy_".Length..]}.name",
                string.Empty
            ))
            .ToArray();

    public static readonly IReadOnlyList<string> CategoryIds =
    [
        CollectionCategoryIds.Crops,
        CollectionCategoryIds.Cooking,
        CollectionCategoryIds.Artisan,
        CollectionCategoryIds.Forage,
        CollectionCategoryIds.Fish,
        CollectionCategoryIds.Minerals,
        CollectionCategoryIds.Artifacts,
        CollectionCategoryIds.Enemies
    ];

    public static readonly IReadOnlyList<CompendiumEntryDefinition>
        EntriesInOrder = CropEntries
            .Concat(CookingEntries)
            .Concat(ArtisanEntries)
            .Concat(ForageEntries)
            .Concat(FishEntries)
            .Concat(MineralEntries)
            .Concat(ArtifactEntries)
            .Concat(EnemyEntries)
            .ToArray();

    public static readonly IReadOnlyDictionary<string, CompendiumEntryDefinition>
        Entries = EntriesInOrder.ToDictionary(
            entry => entry.Id,
            StringComparer.Ordinal
        );

    public static readonly IReadOnlyDictionary<string, CompendiumCategoryDefinition>
        Categories = new Dictionary<string, CompendiumCategoryDefinition>(
            StringComparer.Ordinal
        )
        {
            [CollectionCategoryIds.Crops] = new(
                CollectionCategoryIds.Crops,
                "collection.category.crops",
                "collection.crop_codex.title",
                "collection.entry.crop.undiscovered.description",
                "collection.discovery.notice",
                CropEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Cooking] = new(
                CollectionCategoryIds.Cooking,
                "collection.category.cooking",
                "collection.cooking_codex.title",
                "collection.entry.cooking.undiscovered.description",
                "collection.discovery.cooking.notice",
                CookingEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Artisan] = new(
                CollectionCategoryIds.Artisan,
                "collection.category.artisan",
                "collection.artisan_codex.title",
                "collection.entry.artisan.undiscovered.description",
                "collection.discovery.artisan.notice",
                ArtisanEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Forage] = new(
                CollectionCategoryIds.Forage,
                "collection.category.forage",
                "collection.forage_codex.title",
                "collection.entry.forage.undiscovered.description",
                "collection.discovery.forage.notice",
                ForageEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Fish] = new(
                CollectionCategoryIds.Fish,
                "collection.category.fish",
                "collection.fish_codex.title",
                "collection.entry.fish.undiscovered.description",
                "collection.discovery.fish.notice",
                FishEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Minerals] = new(
                CollectionCategoryIds.Minerals,
                "collection.category.minerals",
                "collection.mineral_codex.title",
                "collection.entry.mineral.undiscovered.description",
                "collection.discovery.mineral.notice",
                MineralEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Artifacts] = new(
                CollectionCategoryIds.Artifacts,
                "collection.category.artifacts",
                "collection.artifact_codex.title",
                "collection.entry.artifact.undiscovered.description",
                "collection.discovery.artifact.notice",
                ArtifactEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionCategoryIds.Enemies] = new(
                CollectionCategoryIds.Enemies,
                "collection.category.enemies",
                "collection.enemy_codex.title",
                "collection.entry.enemy.undiscovered.description",
                "collection.discovery.enemy.notice",
                EnemyEntries.Select(entry => entry.Id).ToArray()
            )
        };

    public static readonly IReadOnlyDictionary<string, CompendiumRewardDefinition>
        Rewards = new Dictionary<string, CompendiumRewardDefinition>(
            StringComparer.Ordinal
        )
        {
            [CollectionRewardIds.MoonlitAlmanac] = new(
                CollectionRewardIds.MoonlitAlmanac,
                CollectionCategoryIds.Crops,
                "collection.reward.moonlit_almanac.name",
                "collection.reward.moonlit_almanac.description",
                "collection.reward.moonlit_almanac.claimed",
                CropEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionRewardIds.MoonhearthRecipeJournal] = new(
                CollectionRewardIds.MoonhearthRecipeJournal,
                CollectionCategoryIds.Cooking,
                "collection.reward.moonhearth_recipe_journal.name",
                "collection.reward.moonhearth_recipe_journal.description",
                "collection.reward.moonhearth_recipe_journal.claimed",
                CookingEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionRewardIds.StarlitAppraisalLedger] = new(
                CollectionRewardIds.StarlitAppraisalLedger,
                CollectionCategoryIds.Artisan,
                "collection.reward.starlit_appraisal_ledger.name",
                "collection.reward.starlit_appraisal_ledger.description",
                "collection.reward.starlit_appraisal_ledger.claimed",
                ArtisanEntries.Select(entry => entry.Id).ToArray()
            ),
            [CollectionRewardIds.StarpathForagersGuide] = new(
                CollectionRewardIds.StarpathForagersGuide,
                CollectionCategoryIds.Forage,
                "collection.reward.starpath_foragers_guide.name",
                "collection.reward.starpath_foragers_guide.description",
                "collection.reward.starpath_foragers_guide.claimed",
                ForageEntries.Select(entry => entry.Id).ToArray()
            )
        };

    private static readonly IReadOnlyDictionary<string, CompendiumEntryDefinition>
        ObtainedItemEntries = BuildObtainedItemEntries();

    public static CompendiumCategoryDefinition Category(string categoryId) =>
        Categories.TryGetValue(categoryId, out var category)
            ? category
            : throw new KeyNotFoundException(
                $"Unknown compendium category '{categoryId}'."
            );

    public static CompendiumEntryDefinition Entry(string entryId) =>
        Entries.TryGetValue(entryId, out var entry)
            ? entry
            : throw new KeyNotFoundException(
                $"Unknown compendium entry '{entryId}'."
            );

    public static IReadOnlyList<CompendiumEntryDefinition> EntriesForCategory(
        string categoryId
    ) => Category(categoryId).EntryIds
        .Select(Entry)
        .ToArray();

    public static CompendiumRewardDefinition? RewardForCategory(
        string categoryId
    ) => Rewards.Values.SingleOrDefault(reward =>
        reward.CategoryId == categoryId
    );

    public static bool TryResolveObtainedItem(
        string? itemId,
        out CompendiumEntryDefinition entry
    )
    {
        entry = null!;
        return !string.IsNullOrWhiteSpace(itemId) &&
            ObtainedItemEntries.TryGetValue(itemId, out entry!);
    }

    private static IReadOnlyDictionary<string, CompendiumEntryDefinition>
        BuildObtainedItemEntries()
    {
        var entries = new Dictionary<string, CompendiumEntryDefinition>(
            StringComparer.Ordinal
        );
        foreach (var entry in CropEntries)
        {
            foreach (var itemId in DataCatalog.ItemFamilyIds(entry.ItemId))
            {
                entries[itemId] = entry;
            }
        }

        foreach (var entry in CookingEntries)
        {
            entries[entry.ItemId] = entry;
        }

        foreach (var entry in ArtisanEntries)
        {
            entries[entry.ItemId] = entry;
        }

        foreach (var entry in ForageEntries)
        {
            entries[entry.ItemId] = entry;
        }

        foreach (var entry in FishEntries)
        {
            entries[entry.ItemId] = entry;
        }

        foreach (var entry in MineralEntries)
        {
            entries[entry.ItemId] = entry;
        }

        foreach (var entry in ArtifactEntries)
        {
            entries[entry.ItemId] = entry;
        }

        return entries;
    }
}
