namespace Luminfield.Core;

public enum ItemKind
{
    Tool,
    Seed,
    Sapling,
    Produce,
    Fertilizer,
    Artisan,
    Resource,
    Placeable
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

public sealed record StarlightNodeDefinition(
    string Id,
    string TitleKey,
    string DescriptionKey,
    int RequiredCount,
    IReadOnlyList<StarlightContributionOption> Options
);

public sealed record StarlightPedestalDefinition(
    string Id,
    string NameKey,
    string RegionKey,
    string RewardTitleKey,
    string RewardDescriptionKey,
    IReadOnlyList<StarlightNodeDefinition> Nodes
);

public sealed record WeatherDefinition(
    string Id,
    string NameKey,
    int AtlasIndex,
    bool AutoWatersCrops = false
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
    public const string StarbudLuminousId = "starbud_luminous";
    public const string StarbudStarlightId = "starbud_starlight";
    public const string MoonrootSeedId = "moonroot_seed";
    public const string MoonrootId = "moonroot";
    public const string MoonrootLuminousId = "moonroot_luminous";
    public const string MoonrootStarlightId = "moonroot_starlight";
    public const string CloudleafSeedId = "cloudleaf_seed";
    public const string CloudleafId = "cloudleaf";
    public const string CloudleafLuminousId = "cloudleaf_luminous";
    public const string CloudleafStarlightId = "cloudleaf_starlight";
    public const string GlowpeaSeedId = "glowpea_seed";
    public const string GlowpeaId = "glowpea";
    public const string GlowpeaLuminousId = "glowpea_luminous";
    public const string GlowpeaStarlightId = "glowpea_starlight";
    public const string EmberbellSeedId = "emberbell_seed";
    public const string EmberbellId = "emberbell";
    public const string EmberbellLuminousId = "emberbell_luminous";
    public const string EmberbellStarlightId = "emberbell_starlight";
    public const string PrismcornSeedId = "prismcorn_seed";
    public const string PrismcornId = "prismcorn";
    public const string PrismcornLuminousId = "prismcorn_luminous";
    public const string PrismcornStarlightId = "prismcorn_starlight";
    public const string DewmelonSeedId = "dewmelon_seed";
    public const string DewmelonId = "dewmelon";
    public const string DewmelonLuminousId = "dewmelon_luminous";
    public const string DewmelonStarlightId = "dewmelon_starlight";
    public const string DuskbellSeedId = "duskbell_seed";
    public const string DuskbellId = "duskbell";
    public const string DuskbellLuminousId = "duskbell_luminous";
    public const string DuskbellStarlightId = "duskbell_starlight";
    public const string DawnlaceSeedId = "dawnlace_seed";
    public const string DawnlaceId = "dawnlace";
    public const string DawnlaceLuminousId = "dawnlace_luminous";
    public const string DawnlaceStarlightId = "dawnlace_starlight";
    public const string RainwovenDawnlaceId = "rainwoven_dawnlace";
    public const string GlimmerpodSeedId = "glimmerpod_seed";
    public const string GlimmerpodId = "glimmerpod";
    public const string GlimmerpodLuminousId = "glimmerpod_luminous";
    public const string GlimmerpodStarlightId = "glimmerpod_starlight";
    public const string StarwindGlimmerpodId = "starwind_glimmerpod";
    public const string MistsongMintSeedId = "mistsong_mint_seed";
    public const string MistsongMintId = "mistsong_mint";
    public const string MistsongMintLuminousId = "mistsong_mint_luminous";
    public const string MistsongMintStarlightId = "mistsong_mint_starlight";
    public const string CometTuberSeedId = "comet_tuber_seed";
    public const string CometTuberId = "comet_tuber";
    public const string CometTuberLuminousId = "comet_tuber_luminous";
    public const string CometTuberStarlightId = "comet_tuber_starlight";
    public const string StarsoilFertilizerId = "starsoil_fertilizer";
    public const string StarbudPreserveId = "starbud_preserve";
    public const string MoonrootTonicId = "moonroot_tonic";
    public const string CloudleafTeaId = "cloudleaf_tea";
    public const string LumenwoodId = "lumenwood";
    public const string CrystalShardId = "crystal_shard";
    public const string StarwovenChestId = "starwoven_chest";
    public const string MoonstonePathId = "moonstone_path";
    public const string StarwoodFenceId = "starwood_fence";
    public const string StarlightTorchId = "starlight_torch";
    public const string DewfallSprinklerId = "dewfall_sprinkler";
    public const string MoonplumSaplingId = "moonplum_sapling";
    public const string MoonplumTreeId = "moonplum_tree";
    public const string MoonplumId = "moonplum";
    public const string StarhoneyId = "starhoney";
    public const string GlowcombHiveId = "glowcomb_hive";
    public const string StarbudPreserveRecipeId = "recipe_starbud_preserve";
    public const string MoonrootTonicRecipeId = "recipe_moonroot_tonic";
    public const string CloudleafTeaRecipeId = "recipe_cloudleaf_tea";
    public const string StarwovenChestRecipeId = "recipe_starwoven_chest";
    public const string MoonstonePathRecipeId = "recipe_moonstone_path";
    public const string StarwoodFenceRecipeId = "recipe_starwood_fence";
    public const string StarlightTorchRecipeId = "recipe_starlight_torch";
    public const string DewfallSprinklerRecipeId = "recipe_dewfall_sprinkler";
    public const string StarsoilFertilizerRecipeId =
        "recipe_starsoil_fertilizer";
    public const string GlowcombHiveRecipeId = "recipe_glowcomb_hive";
    public const string PlantStarbudCommissionId = "commission_plant_starbud";
    public const string GatherLumenwoodCommissionId = "commission_gather_lumenwood";
    public const string DeliverStarbudCommissionId = "commission_deliver_starbud";
    public const string StarlitRouteRestorationWeeklyCommissionId =
        "weekly_starlit_route_restoration";
    public const string StarlitRoutePlantStageId =
        "weekly_starlit_route_plant_starbud";
    public const string StarlitRouteGatherStageId =
        "weekly_starlit_route_gather_lumenwood";
    public const string StarlitRouteDeliverStageId =
        "weekly_starlit_route_deliver_crystal_shard";
    public const string WoodlandStarlightId = "starlight_woodland";
    public const string WoodlandHarvestNodeId = "starlight_woodland_harvest";
    public const string WoodlandMaterialsNodeId = "starlight_woodland_materials";
    public const string WoodlandCraftNodeId = "starlight_woodland_craft";
    public const string ClearWeatherId = "clear";
    public const string RainWeatherId = "rain";
    public const string StardustWindWeatherId = "stardust_wind";

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
            [CloudleafSeedId] = new(
                CloudleafSeedId,
                ItemKind.Seed,
                99,
                "item.cloudleaf_seed",
                CloudleafId,
                BuyPrice: 12
            ),
            [CloudleafId] = new(
                CloudleafId,
                ItemKind.Produce,
                99,
                "item.cloudleaf",
                SellPrice: 18
            ),
            [GlowpeaSeedId] = new(
                GlowpeaSeedId,
                ItemKind.Seed,
                99,
                "item.glowpea_seed",
                GlowpeaId,
                BuyPrice: 20
            ),
            [GlowpeaId] = new(
                GlowpeaId,
                ItemKind.Produce,
                99,
                "item.glowpea",
                SellPrice: 32
            ),
            [EmberbellSeedId] = new(
                EmberbellSeedId,
                ItemKind.Seed,
                99,
                "item.emberbell_seed",
                EmberbellId,
                BuyPrice: 28
            ),
            [EmberbellId] = new(
                EmberbellId,
                ItemKind.Produce,
                99,
                "item.emberbell",
                SellPrice: 48
            ),
            [PrismcornSeedId] = new(
                PrismcornSeedId,
                ItemKind.Seed,
                99,
                "item.prismcorn_seed",
                PrismcornId,
                BuyPrice: 36
            ),
            [PrismcornId] = new(
                PrismcornId,
                ItemKind.Produce,
                99,
                "item.prismcorn",
                SellPrice: 68
            ),
            [DewmelonSeedId] = new(
                DewmelonSeedId,
                ItemKind.Seed,
                99,
                "item.dewmelon_seed",
                DewmelonId,
                BuyPrice: 40
            ),
            [DewmelonId] = new(
                DewmelonId,
                ItemKind.Produce,
                99,
                "item.dewmelon",
                SellPrice: 76
            ),
            [DuskbellSeedId] = new(
                DuskbellSeedId,
                ItemKind.Seed,
                99,
                "item.duskbell_seed",
                DuskbellId,
                BuyPrice: 30
            ),
            [DuskbellId] = new(
                DuskbellId,
                ItemKind.Produce,
                99,
                "item.duskbell",
                SellPrice: 54
            ),
            [DawnlaceSeedId] = new(
                DawnlaceSeedId,
                ItemKind.Seed,
                99,
                "item.dawnlace_seed",
                DawnlaceId,
                BuyPrice: 26
            ),
            [DawnlaceId] = new(
                DawnlaceId,
                ItemKind.Produce,
                99,
                "item.dawnlace",
                SellPrice: 46
            ),
            [GlimmerpodSeedId] = new(
                GlimmerpodSeedId,
                ItemKind.Seed,
                99,
                "item.glimmerpod_seed",
                GlimmerpodId,
                BuyPrice: 42
            ),
            [GlimmerpodId] = new(
                GlimmerpodId,
                ItemKind.Produce,
                99,
                "item.glimmerpod",
                SellPrice: 34
            ),
            [MistsongMintSeedId] = new(
                MistsongMintSeedId,
                ItemKind.Seed,
                99,
                "item.mistsong_mint_seed",
                MistsongMintId,
                BuyPrice: 18
            ),
            [MistsongMintId] = new(
                MistsongMintId,
                ItemKind.Produce,
                99,
                "item.mistsong_mint",
                SellPrice: 30
            ),
            [CometTuberSeedId] = new(
                CometTuberSeedId,
                ItemKind.Seed,
                99,
                "item.comet_tuber_seed",
                CometTuberId,
                BuyPrice: 34
            ),
            [CometTuberId] = new(
                CometTuberId,
                ItemKind.Produce,
                99,
                "item.comet_tuber",
                SellPrice: 62
            ),
            [StarbudLuminousId] = new(
                StarbudLuminousId,
                ItemKind.Produce,
                99,
                "item.starbud_luminous",
                SellPrice: 33,
                BaseItemId: StarbudId,
                Quality: CropQuality.Luminous
            ),
            [StarbudStarlightId] = new(
                StarbudStarlightId,
                ItemKind.Produce,
                99,
                "item.starbud_starlight",
                SellPrice: 50,
                BaseItemId: StarbudId,
                Quality: CropQuality.Starlight
            ),
            [MoonrootLuminousId] = new(
                MoonrootLuminousId,
                ItemKind.Produce,
                99,
                "item.moonroot_luminous",
                SellPrice: 57,
                BaseItemId: MoonrootId,
                Quality: CropQuality.Luminous
            ),
            [MoonrootStarlightId] = new(
                MoonrootStarlightId,
                ItemKind.Produce,
                99,
                "item.moonroot_starlight",
                SellPrice: 86,
                BaseItemId: MoonrootId,
                Quality: CropQuality.Starlight
            ),
            [CloudleafLuminousId] = new(
                CloudleafLuminousId,
                ItemKind.Produce,
                99,
                "item.cloudleaf_luminous",
                SellPrice: 27,
                BaseItemId: CloudleafId,
                Quality: CropQuality.Luminous
            ),
            [CloudleafStarlightId] = new(
                CloudleafStarlightId,
                ItemKind.Produce,
                99,
                "item.cloudleaf_starlight",
                SellPrice: 41,
                BaseItemId: CloudleafId,
                Quality: CropQuality.Starlight
            ),
            [GlowpeaLuminousId] = new(
                GlowpeaLuminousId,
                ItemKind.Produce,
                99,
                "item.glowpea_luminous",
                SellPrice: 48,
                BaseItemId: GlowpeaId,
                Quality: CropQuality.Luminous
            ),
            [GlowpeaStarlightId] = new(
                GlowpeaStarlightId,
                ItemKind.Produce,
                99,
                "item.glowpea_starlight",
                SellPrice: 72,
                BaseItemId: GlowpeaId,
                Quality: CropQuality.Starlight
            ),
            [EmberbellLuminousId] = new(
                EmberbellLuminousId,
                ItemKind.Produce,
                99,
                "item.emberbell_luminous",
                SellPrice: 72,
                BaseItemId: EmberbellId,
                Quality: CropQuality.Luminous
            ),
            [EmberbellStarlightId] = new(
                EmberbellStarlightId,
                ItemKind.Produce,
                99,
                "item.emberbell_starlight",
                SellPrice: 108,
                BaseItemId: EmberbellId,
                Quality: CropQuality.Starlight
            ),
            [PrismcornLuminousId] = new(
                PrismcornLuminousId,
                ItemKind.Produce,
                99,
                "item.prismcorn_luminous",
                SellPrice: 102,
                BaseItemId: PrismcornId,
                Quality: CropQuality.Luminous
            ),
            [PrismcornStarlightId] = new(
                PrismcornStarlightId,
                ItemKind.Produce,
                99,
                "item.prismcorn_starlight",
                SellPrice: 153,
                BaseItemId: PrismcornId,
                Quality: CropQuality.Starlight
            ),
            [DewmelonLuminousId] = new(
                DewmelonLuminousId,
                ItemKind.Produce,
                99,
                "item.dewmelon_luminous",
                SellPrice: 114,
                BaseItemId: DewmelonId,
                Quality: CropQuality.Luminous
            ),
            [DewmelonStarlightId] = new(
                DewmelonStarlightId,
                ItemKind.Produce,
                99,
                "item.dewmelon_starlight",
                SellPrice: 171,
                BaseItemId: DewmelonId,
                Quality: CropQuality.Starlight
            ),
            [DuskbellLuminousId] = new(
                DuskbellLuminousId,
                ItemKind.Produce,
                99,
                "item.duskbell_luminous",
                SellPrice: 81,
                BaseItemId: DuskbellId,
                Quality: CropQuality.Luminous
            ),
            [DuskbellStarlightId] = new(
                DuskbellStarlightId,
                ItemKind.Produce,
                99,
                "item.duskbell_starlight",
                SellPrice: 122,
                BaseItemId: DuskbellId,
                Quality: CropQuality.Starlight
            ),
            [DawnlaceLuminousId] = new(
                DawnlaceLuminousId,
                ItemKind.Produce,
                99,
                "item.dawnlace_luminous",
                SellPrice: 69,
                BaseItemId: DawnlaceId,
                Quality: CropQuality.Luminous
            ),
            [DawnlaceStarlightId] = new(
                DawnlaceStarlightId,
                ItemKind.Produce,
                99,
                "item.dawnlace_starlight",
                SellPrice: 104,
                BaseItemId: DawnlaceId,
                Quality: CropQuality.Starlight
            ),
            [RainwovenDawnlaceId] = new(
                RainwovenDawnlaceId,
                ItemKind.Produce,
                99,
                "item.rainwoven_dawnlace",
                SellPrice: 92,
                BaseItemId: DawnlaceId
            ),
            [GlimmerpodLuminousId] = new(
                GlimmerpodLuminousId,
                ItemKind.Produce,
                99,
                "item.glimmerpod_luminous",
                SellPrice: 51,
                BaseItemId: GlimmerpodId,
                Quality: CropQuality.Luminous
            ),
            [GlimmerpodStarlightId] = new(
                GlimmerpodStarlightId,
                ItemKind.Produce,
                99,
                "item.glimmerpod_starlight",
                SellPrice: 77,
                BaseItemId: GlimmerpodId,
                Quality: CropQuality.Starlight
            ),
            [StarwindGlimmerpodId] = new(
                StarwindGlimmerpodId,
                ItemKind.Produce,
                99,
                "item.starwind_glimmerpod",
                SellPrice: 88,
                BaseItemId: GlimmerpodId
            ),
            [MistsongMintLuminousId] = new(
                MistsongMintLuminousId,
                ItemKind.Produce,
                99,
                "item.mistsong_mint_luminous",
                SellPrice: 45,
                BaseItemId: MistsongMintId,
                Quality: CropQuality.Luminous
            ),
            [MistsongMintStarlightId] = new(
                MistsongMintStarlightId,
                ItemKind.Produce,
                99,
                "item.mistsong_mint_starlight",
                SellPrice: 68,
                BaseItemId: MistsongMintId,
                Quality: CropQuality.Starlight
            ),
            [CometTuberLuminousId] = new(
                CometTuberLuminousId,
                ItemKind.Produce,
                99,
                "item.comet_tuber_luminous",
                SellPrice: 93,
                BaseItemId: CometTuberId,
                Quality: CropQuality.Luminous
            ),
            [CometTuberStarlightId] = new(
                CometTuberStarlightId,
                ItemKind.Produce,
                99,
                "item.comet_tuber_starlight",
                SellPrice: 140,
                BaseItemId: CometTuberId,
                Quality: CropQuality.Starlight
            ),
            [StarsoilFertilizerId] = new(
                StarsoilFertilizerId,
                ItemKind.Fertilizer,
                99,
                "item.starsoil_fertilizer"
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
            [CloudleafTeaId] = new(
                CloudleafTeaId,
                ItemKind.Artisan,
                99,
                "item.cloudleaf_tea",
                SellPrice: 62
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
            ),
            [StarwovenChestId] = new(
                StarwovenChestId,
                ItemKind.Placeable,
                99,
                "item.starwoven_chest"
            ),
            [MoonstonePathId] = new(
                MoonstonePathId,
                ItemKind.Placeable,
                99,
                "item.moonstone_path"
            ),
            [StarwoodFenceId] = new(
                StarwoodFenceId,
                ItemKind.Placeable,
                99,
                "item.starwood_fence"
            ),
            [StarlightTorchId] = new(
                StarlightTorchId,
                ItemKind.Placeable,
                99,
                "item.starlight_torch"
            ),
            [DewfallSprinklerId] = new(
                DewfallSprinklerId,
                ItemKind.Placeable,
                99,
                "item.dewfall_sprinkler"
            ),
            [MoonplumSaplingId] = new(
                MoonplumSaplingId,
                ItemKind.Sapling,
                99,
                "item.moonplum_sapling",
                BuyPrice: 120,
                FruitTreeId: MoonplumTreeId
            ),
            [MoonplumId] = new(
                MoonplumId,
                ItemKind.Produce,
                99,
                "item.moonplum",
                SellPrice: 92
            ),
            [StarhoneyId] = new(
                StarhoneyId,
                ItemKind.Artisan,
                99,
                "item.starhoney",
                SellPrice: 118
            ),
            [GlowcombHiveId] = new(
                GlowcombHiveId,
                ItemKind.Placeable,
                99,
                "item.glowcomb_hive"
            )
        };

    public static readonly IReadOnlyList<string> SellableItemIds =
    [
        StarbudId,
        StarbudLuminousId,
        StarbudStarlightId,
        MoonrootId,
        MoonrootLuminousId,
        MoonrootStarlightId,
        CloudleafId,
        CloudleafLuminousId,
        CloudleafStarlightId,
        GlowpeaId,
        GlowpeaLuminousId,
        GlowpeaStarlightId,
        EmberbellId,
        EmberbellLuminousId,
        EmberbellStarlightId,
        PrismcornId,
        PrismcornLuminousId,
        PrismcornStarlightId,
        DewmelonId,
        DewmelonLuminousId,
        DewmelonStarlightId,
        DuskbellId,
        DuskbellLuminousId,
        DuskbellStarlightId,
        DawnlaceId,
        DawnlaceLuminousId,
        DawnlaceStarlightId,
        RainwovenDawnlaceId,
        GlimmerpodId,
        GlimmerpodLuminousId,
        GlimmerpodStarlightId,
        StarwindGlimmerpodId,
        MistsongMintId,
        MistsongMintLuminousId,
        MistsongMintStarlightId,
        CometTuberId,
        CometTuberLuminousId,
        CometTuberStarlightId,
        StarbudPreserveId,
        MoonrootTonicId,
        CloudleafTeaId,
        MoonplumId,
        StarhoneyId,
        LumenwoodId,
        CrystalShardId
    ];

    public static readonly IReadOnlyList<string> SeedItemIds =
    [
        StarbudSeedId,
        MoonrootSeedId,
        CloudleafSeedId,
        GlowpeaSeedId,
        EmberbellSeedId,
        PrismcornSeedId,
        DewmelonSeedId,
        DuskbellSeedId,
        DawnlaceSeedId,
        GlimmerpodSeedId,
        MistsongMintSeedId,
        CometTuberSeedId
    ];

    public static readonly IReadOnlyList<string> CropIds =
    [
        StarbudId,
        MoonrootId,
        CloudleafId,
        GlowpeaId,
        EmberbellId,
        PrismcornId,
        DewmelonId,
        DuskbellId,
        DawnlaceId,
        GlimmerpodId,
        MistsongMintId,
        CometTuberId
    ];

    public static readonly IReadOnlyList<string> GleamriseCropIds =
    [
        DawnlaceId,
        GlimmerpodId,
        MistsongMintId,
        CometTuberId
    ];

    public static readonly IReadOnlyList<string> GleamriseSeedItemIds =
    [
        DawnlaceSeedId,
        GlimmerpodSeedId,
        MistsongMintSeedId,
        CometTuberSeedId
    ];

    public static readonly IReadOnlyList<string> SaplingItemIds =
    [
        MoonplumSaplingId
    ];

    public static readonly IReadOnlyList<string> QualityProduceItemIds =
    [
        StarbudLuminousId,
        StarbudStarlightId,
        MoonrootLuminousId,
        MoonrootStarlightId,
        CloudleafLuminousId,
        CloudleafStarlightId,
        GlowpeaLuminousId,
        GlowpeaStarlightId,
        EmberbellLuminousId,
        EmberbellStarlightId,
        PrismcornLuminousId,
        PrismcornStarlightId,
        DewmelonLuminousId,
        DewmelonStarlightId,
        DuskbellLuminousId,
        DuskbellStarlightId,
        DawnlaceLuminousId,
        DawnlaceStarlightId,
        GlimmerpodLuminousId,
        GlimmerpodStarlightId,
        MistsongMintLuminousId,
        MistsongMintStarlightId,
        CometTuberLuminousId,
        CometTuberStarlightId
    ];

    public static readonly IReadOnlyList<string> ResonanceProduceItemIds =
    [
        RainwovenDawnlaceId,
        StarwindGlimmerpodId
    ];

    public static readonly IReadOnlyList<string> StorableItemIds =
    [
        StarbudSeedId,
        StarbudId,
        StarbudLuminousId,
        StarbudStarlightId,
        MoonrootSeedId,
        MoonrootId,
        MoonrootLuminousId,
        MoonrootStarlightId,
        CloudleafSeedId,
        CloudleafId,
        CloudleafLuminousId,
        CloudleafStarlightId,
        GlowpeaSeedId,
        GlowpeaId,
        GlowpeaLuminousId,
        GlowpeaStarlightId,
        EmberbellSeedId,
        EmberbellId,
        EmberbellLuminousId,
        EmberbellStarlightId,
        PrismcornSeedId,
        PrismcornId,
        PrismcornLuminousId,
        PrismcornStarlightId,
        DewmelonSeedId,
        DewmelonId,
        DewmelonLuminousId,
        DewmelonStarlightId,
        DuskbellSeedId,
        DuskbellId,
        DuskbellLuminousId,
        DuskbellStarlightId,
        DawnlaceSeedId,
        DawnlaceId,
        DawnlaceLuminousId,
        DawnlaceStarlightId,
        RainwovenDawnlaceId,
        GlimmerpodSeedId,
        GlimmerpodId,
        GlimmerpodLuminousId,
        GlimmerpodStarlightId,
        StarwindGlimmerpodId,
        MistsongMintSeedId,
        MistsongMintId,
        MistsongMintLuminousId,
        MistsongMintStarlightId,
        CometTuberSeedId,
        CometTuberId,
        CometTuberLuminousId,
        CometTuberStarlightId,
        StarsoilFertilizerId,
        StarbudPreserveId,
        MoonrootTonicId,
        CloudleafTeaId,
        MoonplumSaplingId,
        MoonplumId,
        StarhoneyId,
        LumenwoodId,
        CrystalShardId,
        StarwovenChestId,
        MoonstonePathId,
        StarwoodFenceId,
        StarlightTorchId,
        DewfallSprinklerId,
        GlowcombHiveId
    ];

    public static readonly IReadOnlyDictionary<string, FarmObjectDefinition>
        FarmObjects =
            new Dictionary<string, FarmObjectDefinition>(StringComparer.Ordinal)
            {
                [MoonstonePathId] = new(
                    MoonstonePathId,
                    FarmObjectKind.Path,
                    FarmObjectSurface.Ground,
                    BlocksMovement: false
                ),
                [StarwoodFenceId] = new(
                    StarwoodFenceId,
                    FarmObjectKind.Fence,
                    FarmObjectSurface.Ground,
                    BlocksMovement: true
                ),
                [StarlightTorchId] = new(
                    StarlightTorchId,
                    FarmObjectKind.Torch,
                    FarmObjectSurface.Ground,
                    BlocksMovement: true
                ),
                [DewfallSprinklerId] = new(
                    DewfallSprinklerId,
                    FarmObjectKind.Sprinkler,
                    FarmObjectSurface.PlantingBed,
                    BlocksMovement: true
                ),
                [GlowcombHiveId] = new(
                    GlowcombHiveId,
                    FarmObjectKind.Beehive,
                    FarmObjectSurface.Ground,
                    BlocksMovement: true
                )
            };

    public static readonly IReadOnlyDictionary<string, WeatherDefinition> WeatherDefinitions =
        new Dictionary<string, WeatherDefinition>(StringComparer.Ordinal)
        {
            [ClearWeatherId] = new(
                ClearWeatherId,
                "weather.clear",
                0
            ),
            [RainWeatherId] = new(
                RainWeatherId,
                "weather.rain",
                1,
                AutoWatersCrops: true
            ),
            [StardustWindWeatherId] = new(
                StardustWindWeatherId,
                "weather.stardust_wind",
                2
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
            ),
            [CloudleafId] = new(
                CloudleafId,
                CloudleafSeedId,
                CloudleafId,
                "crop.cloudleaf",
                [0, 1, 2],
                0
            ),
            [GlowpeaId] = new(
                GlowpeaId,
                GlowpeaSeedId,
                GlowpeaId,
                "crop.glowpea",
                [0, 1, 2, 3],
                0
            ),
            [EmberbellId] = new(
                EmberbellId,
                EmberbellSeedId,
                EmberbellId,
                "crop.emberbell",
                [0, 1, 2, 3, 4],
                0
            ),
            [PrismcornId] = new(
                PrismcornId,
                PrismcornSeedId,
                PrismcornId,
                "crop.prismcorn",
                [0, 1, 3, 5],
                0
            ),
            [DewmelonId] = new(
                DewmelonId,
                DewmelonSeedId,
                DewmelonId,
                "crop.dewmelon",
                [0, 2, 4, 5],
                0
            ),
            [DuskbellId] = new(
                DuskbellId,
                DuskbellSeedId,
                DuskbellId,
                "crop.duskbell",
                [0, 1, 3, 4],
                0
            ),
            [DawnlaceId] = new(
                DawnlaceId,
                DawnlaceSeedId,
                DawnlaceId,
                "crop.dawnlace",
                [0, 1, 2, 4],
                0,
                SeasonIds: [CalendarSystem.GleamriseSeasonId],
                Resonances:
                [
                    new CropResonanceDefinition(
                        RainwovenDawnlaceId,
                        RainWeatherId,
                        3,
                        0
                    )
                ]
            ),
            [GlimmerpodId] = new(
                GlimmerpodId,
                GlimmerpodSeedId,
                GlimmerpodId,
                "crop.glimmerpod",
                [0, 1, 3, 5],
                0,
                SeasonIds: [CalendarSystem.GleamriseSeasonId],
                RegrowthNights: 2,
                Resonances:
                [
                    new CropResonanceDefinition(
                        StarwindGlimmerpodId,
                        StardustWindWeatherId,
                        3,
                        1
                    )
                ]
            ),
            [MistsongMintId] = new(
                MistsongMintId,
                MistsongMintSeedId,
                MistsongMintId,
                "crop.mistsong_mint",
                [0, 1, 2, 3],
                0,
                SeasonIds: [CalendarSystem.GleamriseSeasonId]
            ),
            [CometTuberId] = new(
                CometTuberId,
                CometTuberSeedId,
                CometTuberId,
                "crop.comet_tuber",
                [0, 1, 2, 4],
                0,
                SeasonIds: [CalendarSystem.GleamriseSeasonId]
            )
        };

    public static readonly IReadOnlyList<string> FruitTreeIds =
    [
        MoonplumTreeId
    ];

    public static readonly IReadOnlyDictionary<string, FruitTreeDefinition>
        FruitTrees =
            new Dictionary<string, FruitTreeDefinition>(StringComparer.Ordinal)
            {
                [MoonplumTreeId] = new(
                    MoonplumTreeId,
                    MoonplumSaplingId,
                    MoonplumId,
                    "fruit_tree.moonplum",
                    MatureAfterNights: 3,
                    RegrowthNights: 2
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
            ),
            [CloudleafTeaRecipeId] = new(
                CloudleafTeaRecipeId,
                CloudleafId,
                3,
                CloudleafTeaId,
                1,
                2,
                "recipe.cloudleaf_tea"
            )
        };

    public static readonly IReadOnlyDictionary<string, CraftingRecipe> CraftingRecipes =
        new Dictionary<string, CraftingRecipe>(StringComparer.Ordinal)
        {
            [StarsoilFertilizerRecipeId] = new(
                StarsoilFertilizerRecipeId,
                StarsoilFertilizerId,
                2,
                [
                    new CraftingIngredient(LumenwoodId, 1),
                    new CraftingIngredient(CrystalShardId, 1)
                ],
                "recipe.starsoil_fertilizer"
            ),
            [StarwovenChestRecipeId] = new(
                StarwovenChestRecipeId,
                StarwovenChestId,
                1,
                [
                    new CraftingIngredient(LumenwoodId, 6),
                    new CraftingIngredient(CrystalShardId, 2)
                ],
                "recipe.starwoven_chest"
            ),
            [MoonstonePathRecipeId] = new(
                MoonstonePathRecipeId,
                MoonstonePathId,
                4,
                [
                    new CraftingIngredient(CrystalShardId, 1)
                ],
                "recipe.moonstone_path"
            ),
            [StarwoodFenceRecipeId] = new(
                StarwoodFenceRecipeId,
                StarwoodFenceId,
                4,
                [
                    new CraftingIngredient(LumenwoodId, 2)
                ],
                "recipe.starwood_fence"
            ),
            [StarlightTorchRecipeId] = new(
                StarlightTorchRecipeId,
                StarlightTorchId,
                2,
                [
                    new CraftingIngredient(LumenwoodId, 1),
                    new CraftingIngredient(CrystalShardId, 1)
                ],
                "recipe.starlight_torch"
            ),
            [DewfallSprinklerRecipeId] = new(
                DewfallSprinklerRecipeId,
                DewfallSprinklerId,
                1,
                [
                    new CraftingIngredient(LumenwoodId, 4),
                    new CraftingIngredient(CrystalShardId, 3)
                ],
                "recipe.dewfall_sprinkler"
            ),
            [GlowcombHiveRecipeId] = new(
                GlowcombHiveRecipeId,
                GlowcombHiveId,
                1,
                [
                    new CraftingIngredient(LumenwoodId, 8),
                    new CraftingIngredient(CrystalShardId, 2),
                    new CraftingIngredient(MoonplumId, 1)
                ],
                "recipe.glowcomb_hive"
            )
        };

    public static readonly IReadOnlyList<DailyCommissionDefinition> DailyCommissionRotation =
    [
        new(
            PlantStarbudCommissionId,
            DailyCommissionKind.Plant,
            StarbudId,
            2,
            40,
            "commission.plant_starbud.title",
            "commission.plant_starbud.description"
        ),
        new(
            GatherLumenwoodCommissionId,
            DailyCommissionKind.Gather,
            LumenwoodId,
            3,
            55,
            "commission.gather_lumenwood.title",
            "commission.gather_lumenwood.description"
        ),
        new(
            DeliverStarbudCommissionId,
            DailyCommissionKind.Deliver,
            StarbudId,
            2,
            70,
            "commission.deliver_starbud.title",
            "commission.deliver_starbud.description"
        )
    ];

    public static readonly IReadOnlyDictionary<string, DailyCommissionDefinition>
        DailyCommissions = DailyCommissionRotation.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal
        );

    public static readonly WeeklyCommissionDefinition WeeklyCommission = new(
        StarlitRouteRestorationWeeklyCommissionId,
        "weekly_commission.starlit_route.title",
        [
            new WeeklyCommissionStageDefinition(
                StarlitRoutePlantStageId,
                WeeklyCommissionStageKind.Plant,
                StarbudId,
                3,
                "weekly_commission.stage.plant.description"
            ),
            new WeeklyCommissionStageDefinition(
                StarlitRouteGatherStageId,
                WeeklyCommissionStageKind.Gather,
                LumenwoodId,
                4,
                "weekly_commission.stage.gather.description"
            ),
            new WeeklyCommissionStageDefinition(
                StarlitRouteDeliverStageId,
                WeeklyCommissionStageKind.Deliver,
                CrystalShardId,
                3,
                "weekly_commission.stage.deliver.description"
            )
        ],
        120,
        MoonstonePathId,
        4
    );

    public static readonly StarlightPedestalDefinition WoodlandStarlight =
        new(
            WoodlandStarlightId,
            "starlight.woodland.name",
            "starlight.woodland.region",
            "starlight.woodland.reward_title",
            "starlight.woodland.reward_description",
            [
                new StarlightNodeDefinition(
                    WoodlandHarvestNodeId,
                    "starlight.node.harvest.title",
                    "starlight.node.harvest.description",
                    3,
                    [
                        new StarlightContributionOption(StarbudId, 1),
                        new StarlightContributionOption(MoonrootId, 1),
                        new StarlightContributionOption(CloudleafId, 1),
                        new StarlightContributionOption(GlowpeaId, 1),
                        new StarlightContributionOption(EmberbellId, 1),
                        new StarlightContributionOption(PrismcornId, 1),
                        new StarlightContributionOption(DewmelonId, 1),
                        new StarlightContributionOption(DuskbellId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    WoodlandMaterialsNodeId,
                    "starlight.node.materials.title",
                    "starlight.node.materials.description",
                    8,
                    [
                        new StarlightContributionOption(LumenwoodId, 6),
                        new StarlightContributionOption(CrystalShardId, 2)
                    ]
                ),
                new StarlightNodeDefinition(
                    WoodlandCraftNodeId,
                    "starlight.node.craft.title",
                    "starlight.node.craft.description",
                    3,
                    [
                        new StarlightContributionOption(StarbudPreserveId, 1),
                        new StarlightContributionOption(MoonrootTonicId, 1),
                        new StarlightContributionOption(StarwovenChestId, 1)
                    ]
                )
            ]
        );

    public static readonly IReadOnlyDictionary<string, StarlightPedestalDefinition>
        StarlightPedestals =
            new Dictionary<string, StarlightPedestalDefinition>(
                StringComparer.Ordinal
            )
            {
                [WoodlandStarlight.Id] = WoodlandStarlight
            };

    public static readonly IReadOnlyDictionary<string, StarlightNodeDefinition>
        StarlightNodes = WoodlandStarlight.Nodes.ToDictionary(
            node => node.Id,
            StringComparer.Ordinal
        );

    public static IReadOnlyList<string> SeedItemIdsForDay(int day) =>
        SeedItemIds.Where(itemId => IsSeedAvailableOnDay(itemId, day))
            .ToArray();

    public static IReadOnlyList<string> FarmShopItemIdsForDay(int day) =>
        SeedItemIdsForDay(day)
            .Concat(SaplingItemIds.Where(itemId =>
                IsSaplingAvailableOnDay(itemId, day)
            ))
            .ToArray();

    public static bool IsSeedAvailableOnDay(string itemId, int day)
    {
        if (!Items.TryGetValue(itemId, out var item) ||
            item.Kind != ItemKind.Seed ||
            string.IsNullOrWhiteSpace(item.CropId) ||
            !Crops.TryGetValue(item.CropId, out var crop))
        {
            return false;
        }

        return crop.IsAvailableOnDay(day);
    }

    public static bool IsSaplingAvailableOnDay(string itemId, int day)
    {
        if (!Items.TryGetValue(itemId, out var item) ||
            item.Kind != ItemKind.Sapling ||
            string.IsNullOrWhiteSpace(item.FruitTreeId) ||
            !FruitTrees.TryGetValue(item.FruitTreeId, out var tree))
        {
            return false;
        }

        return tree.IsAvailableOnDay(day);
    }

    public static string BaseItemId(string itemId)
    {
        if (!Items.TryGetValue(itemId, out var item))
        {
            return itemId;
        }

        return string.IsNullOrWhiteSpace(item.BaseItemId)
            ? item.Id
            : item.BaseItemId;
    }

    public static CropQuality ItemQuality(string itemId) =>
        Items.TryGetValue(itemId, out var item)
            ? item.Quality
            : CropQuality.Regular;

    public static string ProduceItemId(
        string baseItemId,
        CropQuality quality
    )
    {
        if (quality == CropQuality.Regular)
        {
            return baseItemId;
        }

        return Items.Values.First(item =>
            item.BaseItemId == baseItemId &&
            item.Quality == quality
        ).Id;
    }

    public static IReadOnlyList<string> ItemFamilyIds(string itemId)
    {
        var baseItemId = BaseItemId(itemId);
        if (!Crops.ContainsKey(baseItemId))
        {
            return [itemId];
        }

        var family = new List<string>
        {
            baseItemId,
            ProduceItemId(baseItemId, CropQuality.Luminous),
            ProduceItemId(baseItemId, CropQuality.Starlight)
        };
        if (Crops[baseItemId].Resonances is { Count: > 0 } resonances)
        {
            family.AddRange(resonances.Select(resonance => resonance.ItemId));
        }

        return family;
    }

    public static ItemDefinition Item(string id) =>
        Items.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown item id '{id}'.");

    public static CropDefinition Crop(string id) =>
        Crops.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown crop id '{id}'.");

    public static FruitTreeDefinition FruitTree(string id) =>
        FruitTrees.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown fruit tree id '{id}'.");

    public static ProcessorRecipe ProcessorRecipe(string id) =>
        ProcessorRecipes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown processor recipe id '{id}'.");

    public static CraftingRecipe CraftingRecipe(string id) =>
        CraftingRecipes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown crafting recipe id '{id}'.");

    public static FarmObjectDefinition FarmObject(string itemId) =>
        FarmObjects.TryGetValue(itemId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown farm object item id '{itemId}'.");

    public static DailyCommissionDefinition DailyCommission(string id) =>
        DailyCommissions.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown daily commission id '{id}'.");

    public static StarlightPedestalDefinition StarlightPedestal(string id) =>
        StarlightPedestals.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown starlight pedestal id '{id}'.");

    public static StarlightNodeDefinition StarlightNode(string id) =>
        StarlightNodes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown starlight node id '{id}'.");

    public static WeatherDefinition Weather(string id) =>
        WeatherDefinitions.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown weather id '{id}'.");
}

public static class ProcessorCatalog
{
    public const string MoonwellInfuserId = "machine_moonwell_infuser";
    public const string PrismPreserveVatId = "machine_prism_preserve_vat";
    public const string StarweaveDryingLoomId = "machine_starweave_drying_loom";
    public const string MainMachineId = MoonwellInfuserId;

    public static readonly IReadOnlyDictionary<string, ProcessorMachineDefinition>
        Machines = new Dictionary<string, ProcessorMachineDefinition>(
            StringComparer.Ordinal
        )
        {
            [MoonwellInfuserId] = new(
                MoonwellInfuserId,
                new GridPosition(36, 14),
                "processor.machine.moonwell",
                [
                    DataCatalog.MoonrootTonicRecipeId,
                    DataCatalog.StarbudPreserveRecipeId
                ]
            ),
            [PrismPreserveVatId] = new(
                PrismPreserveVatId,
                new GridPosition(35, 12),
                "processor.machine.prism_vat",
                [DataCatalog.StarbudPreserveRecipeId]
            ),
            [StarweaveDryingLoomId] = new(
                StarweaveDryingLoomId,
                new GridPosition(37, 12),
                "processor.machine.drying_loom",
                [DataCatalog.CloudleafTeaRecipeId]
            )
        };

    public static ProcessorMachineDefinition Machine(string id) =>
        Machines.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Unknown processor machine id '{id}'."
            );

    public static bool SupportsRecipe(string machineId, string recipeId) =>
        Machines.TryGetValue(machineId, out var machine) &&
        machine.RecipeIds.Contains(recipeId, StringComparer.Ordinal);

    public static string? MachineIdAt(GridPosition position) =>
        Machines.Values.FirstOrDefault(machine => machine.Position == position)?.Id;
}
