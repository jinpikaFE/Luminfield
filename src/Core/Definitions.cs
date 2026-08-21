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

public static class DataCatalog
{
    public const string LegacyHoeId = "hoe";
    public const string HandId = "hand";
    public const string ShovelId = "shovel";
    public const string MacheteId = "machete";
    public const string WateringCanId = "watering_can";
    public const string BucketId = "water_bucket";
    public const string FishingRodId = "fishing_rod";
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
    public const string RipplecapSeedId = "ripplecap_seed";
    public const string RipplecapId = "ripplecap";
    public const string RipplecapLuminousId = "ripplecap_luminous";
    public const string RipplecapStarlightId = "ripplecap_starlight";
    public const string TideglassTaroSeedId = "tideglass_taro_seed";
    public const string TideglassTaroId = "tideglass_taro";
    public const string TideglassTaroLuminousId = "tideglass_taro_luminous";
    public const string TideglassTaroStarlightId = "tideglass_taro_starlight";
    public const string LanternReedSeedId = "lantern_reed_seed";
    public const string LanternReedId = "lantern_reed";
    public const string LanternReedLuminousId = "lantern_reed_luminous";
    public const string LanternReedStarlightId = "lantern_reed_starlight";
    public const string RainveilLotusSeedId = "rainveil_lotus_seed";
    public const string RainveilLotusId = "rainveil_lotus";
    public const string RainveilLotusLuminousId = "rainveil_lotus_luminous";
    public const string RainveilLotusStarlightId = "rainveil_lotus_starlight";
    public const string AuricShootSeedId = "auric_shoot_seed";
    public const string AuricShootId = "auric_shoot";
    public const string AuricShootLuminousId = "auric_shoot_luminous";
    public const string AuricShootStarlightId = "auric_shoot_starlight";
    public const string SunvaultGourdSeedId = "sunvault_gourd_seed";
    public const string SunvaultGourdId = "sunvault_gourd";
    public const string SunvaultGourdLuminousId = "sunvault_gourd_luminous";
    public const string SunvaultGourdStarlightId = "sunvault_gourd_starlight";
    public const string CrownstarSaffronSeedId = "crownstar_saffron_seed";
    public const string CrownstarSaffronId = "crownstar_saffron";
    public const string CrownstarSaffronLuminousId =
        "crownstar_saffron_luminous";
    public const string CrownstarSaffronStarlightId =
        "crownstar_saffron_starlight";
    public const string AmberthreadClusterSeedId = "amberthread_cluster_seed";
    public const string AmberthreadClusterId = "amberthread_cluster";
    public const string AmberthreadClusterLuminousId =
        "amberthread_cluster_luminous";
    public const string AmberthreadClusterStarlightId =
        "amberthread_cluster_starlight";
    public const string StarsoilFertilizerId = "starsoil_fertilizer";
    public const string StarbudPreserveId = "starbud_preserve";
    public const string MoonrootTonicId = "moonroot_tonic";
    public const string CloudleafTeaId = "cloudleaf_tea";
    public const string LumenwoodId = "lumenwood";
    public const string CrystalShardId = "crystal_shard";
    public const string LumenSlateOreId = "lumen_slate_ore";
    public const string MoonveinOreId = "moonvein_ore";
    public const string PrismheartOreId = "prismheart_ore";
    public const string StarironOreId = "stariron_ore";
    public const string WhisperbloomId = "whisperbloom";
    public const string DewglassCloverId = "dewglass_clover";
    public const string RainbellMossId = "rainbell_moss";
    public const string MistcoilFernId = "mistcoil_fern";
    public const string GloamgoldBerryId = "gloamgold_berry";
    public const string SunwispPodId = "sunwisp_pod";
    public const string NightlampLichenId = "nightlamp_lichen";
    public const string FrostwickRootId = "frostwick_root";
    public const string PondglowMinnowId = "pondglow_minnow";
    public const string ReedwhisperBreamId = "reedwhisper_bream";
    public const string LanternscaleCarpId = "lanternscale_carp";
    public const string SunveilGudgeonId = "sunveil_gudgeon";
    public const string RainpetalLoachId = "rainpetal_loach";
    public const string DuskglassEelId = "duskglass_eel";
    public const string StarharvestKoiId = "starharvest_koi";
    public const string LongnightKoiId = "longnight_koi";
    public const string CrystalfinDaceId = "crystalfin_dace";
    public const string QuartzscaleTroutId = "quartzscale_trout";
    public const string ShardbackPerchId = "shardback_perch";
    public const string StarlitCharId = "starlit_char";
    public const string MistglassSmeltId = "mistglass_smelt";
    public const string StardustPikeId = "stardust_pike";
    public const string StarharvestChubId = "starharvest_chub";
    public const string LongnightGlowlingId = "longnight_glowling";
    public const string MoonwaterMinnowId = "moonwater_minnow";
    public const string MarshveilKilliId = "marshveil_killi";
    public const string SilverreedMudfishId = "silverreed_mudfish";
    public const string MooncapGobyId = "mooncap_goby";
    public const string RainveilLampreyId = "rainveil_lamprey";
    public const string StardustRayId = "stardust_ray";
    public const string StarharvestOrbfinId = "starharvest_orbfin";
    public const string LongnightWispfishId = "longnight_wispfish";
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
    public const string MeadowFodderId = "meadow_fodder";
    public const string StarfeatherEggId = "starfeather_egg";
    public const string StarfeatherEggLuminousId =
        "starfeather_egg_luminous";
    public const string StarfeatherEggStarlightId =
        "starfeather_egg_starlight";
    public const string MoonfleeceId = "moonfleece";
    public const string MoonfleeceLuminousId = "moonfleece_luminous";
    public const string MoonfleeceStarlightId = "moonfleece_starlight";
    public const string DewhornMilkId = "dewhorn_milk";
    public const string DewhornMilkLuminousId = "dewhorn_milk_luminous";
    public const string DewhornMilkStarlightId =
        "dewhorn_milk_starlight";
    public const string MoonmistStewId = "moonmist_stew";
    public const string SunvaultHashId = "sunvault_hash";
    public const string StarhoneyCustardId = "starhoney_custard";
    public const string LanternrootBrothId = "lanternroot_broth";
    public const string MoonmistStewRecipeId = "recipe_moonmist_stew";
    public const string SunvaultHashRecipeId = "recipe_sunvault_hash";
    public const string StarhoneyCustardRecipeId =
        "recipe_starhoney_custard";
    public const string LanternrootBrothRecipeId =
        "recipe_lanternroot_broth";
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
    public const string HomesteadStarlightId = "starlight_homestead";
    public const string HomesteadHarvestNodeId =
        "starlight_homestead_harvest";
    public const string HomesteadArtisanNodeId =
        "starlight_homestead_artisan";
    public const string HomesteadBuildingNodeId =
        "starlight_homestead_building";
    public const string MeadowStarlightId = "starlight_meadow";
    public const string MeadowBloomsNodeId = "starlight_meadow_blooms";
    public const string MeadowBountyNodeId = "starlight_meadow_bounty";
    public const string MeadowCelebrationNodeId =
        "starlight_meadow_celebration";
    public const string MoonwaterStarlightId = "starlight_moonwater";
    public const string MoonwaterLocalFishNodeId =
        "starlight_moonwater_local_fish";
    public const string MoonwaterWeatherFishNodeId =
        "starlight_moonwater_weather_fish";
    public const string MoonwaterSeasonalFishNodeId =
        "starlight_moonwater_seasonal_fish";
    public const string CrystalValeStarlightId =
        "starlight_crystal_vale";
    public const string CrystalValeMineralChorusNodeId =
        "starlight_crystal_vale_mineral_chorus";
    public const string CrystalValeTemperedShovelNodeId =
        "starlight_crystal_vale_tempered_shovel";
    public const string CrystalValeDepthAnchorNodeId =
        "starlight_crystal_vale_depth_anchor";
    public const string CrystalRuinsPassageRewardId =
        "starlight_reward_crystal_ruins_passage";
    public const string MoonsteelShortbladeId = "moonsteel_shortblade";
    public const string DawnpathCompassId = "artifact_dawnpath_compass";
    public const string TideglassTabletId = "artifact_tideglass_tablet";
    public const string HushedGleambellId = "artifact_hushed_gleambell";
    public const string StarweaveSpindleId = "artifact_starweave_spindle";
    public const string StarfallRuinsStarlightId =
        "starlight_starfall_ruins";
    public const string StarfallMemoryArchiveNodeId =
        "starlight_starfall_ruins_memory_archive";
    public const string StarfallNightwatchTrialNodeId =
        "starlight_starfall_ruins_nightwatch_trial";
    public const string StarfallTrustedPathsNodeId =
        "starlight_starfall_ruins_trusted_paths";
    public const string StarfallFiveLightsNodeId =
        "starlight_starfall_ruins_five_lights";
    public const string KaelTrustedRelationshipMilestoneId =
        "relationship_kael_trusted_60";
    public const string LioraTrustedRelationshipMilestoneId =
        "relationship_liora_trusted_60";
    public const string StarfallSixfoldConvergenceRewardId =
        "starlight_reward_starfall_sixfold_convergence";
    public const string ClearWeatherId = "clear";
    public const string RainWeatherId = "rain";
    public const string StardustWindWeatherId = "stardust_wind";
    public const string LongnightSnowWeatherId = "longnight_snow";

    public static readonly IReadOnlyDictionary<string, ItemDefinition> Items =
        new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
        {
            [HandId] = new(HandId, ItemKind.Tool, 1, "item.hand"),
            [ShovelId] = new(ShovelId, ItemKind.Tool, 1, "item.shovel"),
            [MacheteId] = new(MacheteId, ItemKind.Tool, 1, "item.machete"),
            [WateringCanId] = new(WateringCanId, ItemKind.Tool, 1, "item.watering_can"),
            [BucketId] = new(BucketId, ItemKind.Tool, 1, "item.water_bucket"),
            [FishingRodId] = new(
                FishingRodId,
                ItemKind.Tool,
                1,
                "item.fishing_rod"
            ),
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
            [RipplecapSeedId] = new(
                RipplecapSeedId,
                ItemKind.Seed,
                99,
                "item.ripplecap_seed",
                RipplecapId,
                BuyPrice: 16
            ),
            [RipplecapId] = new(
                RipplecapId,
                ItemKind.Produce,
                99,
                "item.ripplecap",
                SellPrice: 30
            ),
            [TideglassTaroSeedId] = new(
                TideglassTaroSeedId,
                ItemKind.Seed,
                99,
                "item.tideglass_taro_seed",
                TideglassTaroId,
                BuyPrice: 38
            ),
            [TideglassTaroId] = new(
                TideglassTaroId,
                ItemKind.Produce,
                99,
                "item.tideglass_taro",
                SellPrice: 72
            ),
            [LanternReedSeedId] = new(
                LanternReedSeedId,
                ItemKind.Seed,
                99,
                "item.lantern_reed_seed",
                LanternReedId,
                BuyPrice: 46
            ),
            [LanternReedId] = new(
                LanternReedId,
                ItemKind.Produce,
                99,
                "item.lantern_reed",
                SellPrice: 40
            ),
            [RainveilLotusSeedId] = new(
                RainveilLotusSeedId,
                ItemKind.Seed,
                99,
                "item.rainveil_lotus_seed",
                RainveilLotusId,
                BuyPrice: 52
            ),
            [RainveilLotusId] = new(
                RainveilLotusId,
                ItemKind.Produce,
                99,
                "item.rainveil_lotus",
                SellPrice: 105
            ),
            [AuricShootSeedId] = new(
                AuricShootSeedId,
                ItemKind.Seed,
                99,
                "item.auric_shoot_seed",
                AuricShootId,
                BuyPrice: 30
            ),
            [AuricShootId] = new(
                AuricShootId,
                ItemKind.Produce,
                99,
                "item.auric_shoot",
                SellPrice: 52
            ),
            [SunvaultGourdSeedId] = new(
                SunvaultGourdSeedId,
                ItemKind.Seed,
                99,
                "item.sunvault_gourd_seed",
                SunvaultGourdId,
                BuyPrice: 46
            ),
            [SunvaultGourdId] = new(
                SunvaultGourdId,
                ItemKind.Produce,
                99,
                "item.sunvault_gourd",
                SellPrice: 86
            ),
            [CrownstarSaffronSeedId] = new(
                CrownstarSaffronSeedId,
                ItemKind.Seed,
                99,
                "item.crownstar_saffron_seed",
                CrownstarSaffronId,
                BuyPrice: 78
            ),
            [CrownstarSaffronId] = new(
                CrownstarSaffronId,
                ItemKind.Produce,
                99,
                "item.crownstar_saffron",
                SellPrice: 154
            ),
            [AmberthreadClusterSeedId] = new(
                AmberthreadClusterSeedId,
                ItemKind.Seed,
                99,
                "item.amberthread_cluster_seed",
                AmberthreadClusterId,
                BuyPrice: 64
            ),
            [AmberthreadClusterId] = new(
                AmberthreadClusterId,
                ItemKind.Produce,
                99,
                "item.amberthread_cluster",
                SellPrice: 52
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
            [RipplecapLuminousId] = new(
                RipplecapLuminousId,
                ItemKind.Produce,
                99,
                "item.ripplecap_luminous",
                SellPrice: 45,
                BaseItemId: RipplecapId,
                Quality: CropQuality.Luminous
            ),
            [RipplecapStarlightId] = new(
                RipplecapStarlightId,
                ItemKind.Produce,
                99,
                "item.ripplecap_starlight",
                SellPrice: 68,
                BaseItemId: RipplecapId,
                Quality: CropQuality.Starlight
            ),
            [TideglassTaroLuminousId] = new(
                TideglassTaroLuminousId,
                ItemKind.Produce,
                99,
                "item.tideglass_taro_luminous",
                SellPrice: 108,
                BaseItemId: TideglassTaroId,
                Quality: CropQuality.Luminous
            ),
            [TideglassTaroStarlightId] = new(
                TideglassTaroStarlightId,
                ItemKind.Produce,
                99,
                "item.tideglass_taro_starlight",
                SellPrice: 162,
                BaseItemId: TideglassTaroId,
                Quality: CropQuality.Starlight
            ),
            [LanternReedLuminousId] = new(
                LanternReedLuminousId,
                ItemKind.Produce,
                99,
                "item.lantern_reed_luminous",
                SellPrice: 60,
                BaseItemId: LanternReedId,
                Quality: CropQuality.Luminous
            ),
            [LanternReedStarlightId] = new(
                LanternReedStarlightId,
                ItemKind.Produce,
                99,
                "item.lantern_reed_starlight",
                SellPrice: 90,
                BaseItemId: LanternReedId,
                Quality: CropQuality.Starlight
            ),
            [RainveilLotusLuminousId] = new(
                RainveilLotusLuminousId,
                ItemKind.Produce,
                99,
                "item.rainveil_lotus_luminous",
                SellPrice: 158,
                BaseItemId: RainveilLotusId,
                Quality: CropQuality.Luminous
            ),
            [RainveilLotusStarlightId] = new(
                RainveilLotusStarlightId,
                ItemKind.Produce,
                99,
                "item.rainveil_lotus_starlight",
                SellPrice: 237,
                BaseItemId: RainveilLotusId,
                Quality: CropQuality.Starlight
            ),
            [AuricShootLuminousId] = new(
                AuricShootLuminousId,
                ItemKind.Produce,
                99,
                "item.auric_shoot_luminous",
                SellPrice: 78,
                BaseItemId: AuricShootId,
                Quality: CropQuality.Luminous
            ),
            [AuricShootStarlightId] = new(
                AuricShootStarlightId,
                ItemKind.Produce,
                99,
                "item.auric_shoot_starlight",
                SellPrice: 117,
                BaseItemId: AuricShootId,
                Quality: CropQuality.Starlight
            ),
            [SunvaultGourdLuminousId] = new(
                SunvaultGourdLuminousId,
                ItemKind.Produce,
                99,
                "item.sunvault_gourd_luminous",
                SellPrice: 129,
                BaseItemId: SunvaultGourdId,
                Quality: CropQuality.Luminous
            ),
            [SunvaultGourdStarlightId] = new(
                SunvaultGourdStarlightId,
                ItemKind.Produce,
                99,
                "item.sunvault_gourd_starlight",
                SellPrice: 194,
                BaseItemId: SunvaultGourdId,
                Quality: CropQuality.Starlight
            ),
            [CrownstarSaffronLuminousId] = new(
                CrownstarSaffronLuminousId,
                ItemKind.Produce,
                99,
                "item.crownstar_saffron_luminous",
                SellPrice: 231,
                BaseItemId: CrownstarSaffronId,
                Quality: CropQuality.Luminous
            ),
            [CrownstarSaffronStarlightId] = new(
                CrownstarSaffronStarlightId,
                ItemKind.Produce,
                99,
                "item.crownstar_saffron_starlight",
                SellPrice: 347,
                BaseItemId: CrownstarSaffronId,
                Quality: CropQuality.Starlight
            ),
            [AmberthreadClusterLuminousId] = new(
                AmberthreadClusterLuminousId,
                ItemKind.Produce,
                99,
                "item.amberthread_cluster_luminous",
                SellPrice: 78,
                BaseItemId: AmberthreadClusterId,
                Quality: CropQuality.Luminous
            ),
            [AmberthreadClusterStarlightId] = new(
                AmberthreadClusterStarlightId,
                ItemKind.Produce,
                99,
                "item.amberthread_cluster_starlight",
                SellPrice: 117,
                BaseItemId: AmberthreadClusterId,
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
            [LumenSlateOreId] = new(
                LumenSlateOreId,
                ItemKind.Resource,
                99,
                "item.lumen_slate_ore",
                SellPrice: 32
            ),
            [MoonveinOreId] = new(
                MoonveinOreId,
                ItemKind.Resource,
                99,
                "item.moonvein_ore",
                SellPrice: 48
            ),
            [PrismheartOreId] = new(
                PrismheartOreId,
                ItemKind.Resource,
                99,
                "item.prismheart_ore",
                SellPrice: 72
            ),
            [StarironOreId] = new(
                StarironOreId,
                ItemKind.Resource,
                99,
                "item.stariron_ore",
                SellPrice: 96
            ),
            [WhisperbloomId] = new(
                WhisperbloomId,
                ItemKind.Resource,
                99,
                "item.whisperbloom",
                SellPrice: 18
            ),
            [DewglassCloverId] = new(
                DewglassCloverId,
                ItemKind.Resource,
                99,
                "item.dewglass_clover",
                SellPrice: 24
            ),
            [RainbellMossId] = new(
                RainbellMossId,
                ItemKind.Resource,
                99,
                "item.rainbell_moss",
                SellPrice: 22
            ),
            [MistcoilFernId] = new(
                MistcoilFernId,
                ItemKind.Resource,
                99,
                "item.mistcoil_fern",
                SellPrice: 30
            ),
            [GloamgoldBerryId] = new(
                GloamgoldBerryId,
                ItemKind.Resource,
                99,
                "item.gloamgold_berry",
                SellPrice: 28
            ),
            [SunwispPodId] = new(
                SunwispPodId,
                ItemKind.Resource,
                99,
                "item.sunwisp_pod",
                SellPrice: 38
            ),
            [NightlampLichenId] = new(
                NightlampLichenId,
                ItemKind.Resource,
                99,
                "item.nightlamp_lichen",
                SellPrice: 34
            ),
            [FrostwickRootId] = new(
                FrostwickRootId,
                ItemKind.Resource,
                99,
                "item.frostwick_root",
                SellPrice: 44
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
            [MeadowFodderId] = new(
                MeadowFodderId,
                ItemKind.AnimalFeed,
                99,
                "item.meadow_fodder",
                BuyPrice: 8
            ),
            [StarfeatherEggId] = new(
                StarfeatherEggId,
                ItemKind.AnimalProduct,
                99,
                "item.starfeather_egg",
                SellPrice: 48
            ),
            [StarfeatherEggLuminousId] = new(
                StarfeatherEggLuminousId,
                ItemKind.AnimalProduct,
                99,
                "item.starfeather_egg_luminous",
                SellPrice: 72,
                BaseItemId: StarfeatherEggId,
                Quality: CropQuality.Luminous
            ),
            [StarfeatherEggStarlightId] = new(
                StarfeatherEggStarlightId,
                ItemKind.AnimalProduct,
                99,
                "item.starfeather_egg_starlight",
                SellPrice: 108,
                BaseItemId: StarfeatherEggId,
                Quality: CropQuality.Starlight
            ),
            [MoonfleeceId] = new(
                MoonfleeceId,
                ItemKind.AnimalProduct,
                99,
                "item.moonfleece",
                SellPrice: 84
            ),
            [MoonfleeceLuminousId] = new(
                MoonfleeceLuminousId,
                ItemKind.AnimalProduct,
                99,
                "item.moonfleece_luminous",
                SellPrice: 126,
                BaseItemId: MoonfleeceId,
                Quality: CropQuality.Luminous
            ),
            [MoonfleeceStarlightId] = new(
                MoonfleeceStarlightId,
                ItemKind.AnimalProduct,
                99,
                "item.moonfleece_starlight",
                SellPrice: 189,
                BaseItemId: MoonfleeceId,
                Quality: CropQuality.Starlight
            ),
            [DewhornMilkId] = new(
                DewhornMilkId,
                ItemKind.AnimalProduct,
                99,
                "item.dewhorn_milk",
                SellPrice: 96
            ),
            [DewhornMilkLuminousId] = new(
                DewhornMilkLuminousId,
                ItemKind.AnimalProduct,
                99,
                "item.dewhorn_milk_luminous",
                SellPrice: 144,
                BaseItemId: DewhornMilkId,
                Quality: CropQuality.Luminous
            ),
            [DewhornMilkStarlightId] = new(
                DewhornMilkStarlightId,
                ItemKind.AnimalProduct,
                99,
                "item.dewhorn_milk_starlight",
                SellPrice: 216,
                BaseItemId: DewhornMilkId,
                Quality: CropQuality.Starlight
            ),
            [MoonmistStewId] = new(
                MoonmistStewId,
                ItemKind.CookedDish,
                99,
                "item.moonmist_stew",
                SellPrice: 156
            ),
            [SunvaultHashId] = new(
                SunvaultHashId,
                ItemKind.CookedDish,
                99,
                "item.sunvault_hash",
                SellPrice: 148
            ),
            [StarhoneyCustardId] = new(
                StarhoneyCustardId,
                ItemKind.CookedDish,
                99,
                "item.starhoney_custard",
                SellPrice: 258
            ),
            [LanternrootBrothId] = new(
                LanternrootBrothId,
                ItemKind.CookedDish,
                99,
                "item.lanternroot_broth",
                SellPrice: 150
            ),
            [PondglowMinnowId] = new(
                PondglowMinnowId,
                ItemKind.Fish,
                99,
                "item.pondglow_minnow",
                SellPrice: 34
            ),
            [ReedwhisperBreamId] = new(
                ReedwhisperBreamId,
                ItemKind.Fish,
                99,
                "item.reedwhisper_bream",
                SellPrice: 42
            ),
            [LanternscaleCarpId] = new(
                LanternscaleCarpId,
                ItemKind.Fish,
                99,
                "item.lanternscale_carp",
                SellPrice: 54
            ),
            [SunveilGudgeonId] = new(
                SunveilGudgeonId,
                ItemKind.Fish,
                99,
                "item.sunveil_gudgeon",
                SellPrice: 46
            ),
            [RainpetalLoachId] = new(
                RainpetalLoachId,
                ItemKind.Fish,
                99,
                "item.rainpetal_loach",
                SellPrice: 66
            ),
            [DuskglassEelId] = new(
                DuskglassEelId,
                ItemKind.Fish,
                99,
                "item.duskglass_eel",
                SellPrice: 82
            ),
            [StarharvestKoiId] = new(
                StarharvestKoiId,
                ItemKind.Fish,
                99,
                "item.starharvest_koi",
                SellPrice: 92
            ),
            [LongnightKoiId] = new(
                LongnightKoiId,
                ItemKind.Fish,
                99,
                "item.longnight_koi",
                SellPrice: 104
            ),
            [CrystalfinDaceId] = new(
                CrystalfinDaceId,
                ItemKind.Fish,
                99,
                "item.crystalfin_dace",
                SellPrice: 48
            ),
            [QuartzscaleTroutId] = new(
                QuartzscaleTroutId,
                ItemKind.Fish,
                99,
                "item.quartzscale_trout",
                SellPrice: 58
            ),
            [ShardbackPerchId] = new(
                ShardbackPerchId,
                ItemKind.Fish,
                99,
                "item.shardback_perch",
                SellPrice: 64
            ),
            [StarlitCharId] = new(
                StarlitCharId,
                ItemKind.Fish,
                99,
                "item.starlit_char",
                SellPrice: 74
            ),
            [MistglassSmeltId] = new(
                MistglassSmeltId,
                ItemKind.Fish,
                99,
                "item.mistglass_smelt",
                SellPrice: 76
            ),
            [StardustPikeId] = new(
                StardustPikeId,
                ItemKind.Fish,
                99,
                "item.stardust_pike",
                SellPrice: 96
            ),
            [StarharvestChubId] = new(
                StarharvestChubId,
                ItemKind.Fish,
                99,
                "item.starharvest_chub",
                SellPrice: 90
            ),
            [LongnightGlowlingId] = new(
                LongnightGlowlingId,
                ItemKind.Fish,
                99,
                "item.longnight_glowling",
                SellPrice: 108
            ),
            [MoonwaterMinnowId] = new(
                MoonwaterMinnowId,
                ItemKind.Fish,
                99,
                "item.moonwater_minnow",
                SellPrice: 56
            ),
            [MarshveilKilliId] = new(
                MarshveilKilliId,
                ItemKind.Fish,
                99,
                "item.marshveil_killi",
                SellPrice: 62
            ),
            [SilverreedMudfishId] = new(
                SilverreedMudfishId,
                ItemKind.Fish,
                99,
                "item.silverreed_mudfish",
                SellPrice: 68
            ),
            [MooncapGobyId] = new(
                MooncapGobyId,
                ItemKind.Fish,
                99,
                "item.mooncap_goby",
                SellPrice: 72
            ),
            [RainveilLampreyId] = new(
                RainveilLampreyId,
                ItemKind.Fish,
                99,
                "item.rainveil_lamprey",
                SellPrice: 88
            ),
            [StardustRayId] = new(
                StardustRayId,
                ItemKind.Fish,
                99,
                "item.stardust_ray",
                SellPrice: 112
            ),
            [StarharvestOrbfinId] = new(
                StarharvestOrbfinId,
                ItemKind.Fish,
                99,
                "item.starharvest_orbfin",
                SellPrice: 98
            ),
            [LongnightWispfishId] = new(
                LongnightWispfishId,
                ItemKind.Fish,
                99,
                "item.longnight_wispfish",
                SellPrice: 118
            ),
            [GlowcombHiveId] = new(
                GlowcombHiveId,
                ItemKind.Placeable,
                99,
                "item.glowcomb_hive"
            ),
            [MoonsteelShortbladeId] = new(
                MoonsteelShortbladeId,
                ItemKind.Weapon,
                1,
                "item.moonsteel_shortblade"
            ),
            [DawnpathCompassId] = new(
                DawnpathCompassId,
                ItemKind.Artifact,
                1,
                "item.artifact_dawnpath_compass"
            ),
            [TideglassTabletId] = new(
                TideglassTabletId,
                ItemKind.Artifact,
                1,
                "item.artifact_tideglass_tablet"
            ),
            [HushedGleambellId] = new(
                HushedGleambellId,
                ItemKind.Artifact,
                1,
                "item.artifact_hushed_gleambell"
            ),
            [StarweaveSpindleId] = new(
                StarweaveSpindleId,
                ItemKind.Artifact,
                1,
                "item.artifact_starweave_spindle"
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
        RipplecapId,
        RipplecapLuminousId,
        RipplecapStarlightId,
        TideglassTaroId,
        TideglassTaroLuminousId,
        TideglassTaroStarlightId,
        LanternReedId,
        LanternReedLuminousId,
        LanternReedStarlightId,
        RainveilLotusId,
        RainveilLotusLuminousId,
        RainveilLotusStarlightId,
        AuricShootId,
        AuricShootLuminousId,
        AuricShootStarlightId,
        SunvaultGourdId,
        SunvaultGourdLuminousId,
        SunvaultGourdStarlightId,
        CrownstarSaffronId,
        CrownstarSaffronLuminousId,
        CrownstarSaffronStarlightId,
        AmberthreadClusterId,
        AmberthreadClusterLuminousId,
        AmberthreadClusterStarlightId,
        StarbudPreserveId,
        MoonrootTonicId,
        CloudleafTeaId,
        MoonplumId,
        StarhoneyId,
        StarfeatherEggId,
        StarfeatherEggLuminousId,
        StarfeatherEggStarlightId,
        MoonfleeceId,
        MoonfleeceLuminousId,
        MoonfleeceStarlightId,
        DewhornMilkId,
        DewhornMilkLuminousId,
        DewhornMilkStarlightId,
        MoonmistStewId,
        SunvaultHashId,
        StarhoneyCustardId,
        LanternrootBrothId,
        LumenwoodId,
        CrystalShardId,
        WhisperbloomId,
        DewglassCloverId,
        RainbellMossId,
        MistcoilFernId,
        GloamgoldBerryId,
        SunwispPodId,
        NightlampLichenId,
        FrostwickRootId,
        PondglowMinnowId,
        ReedwhisperBreamId,
        LanternscaleCarpId,
        SunveilGudgeonId,
        RainpetalLoachId,
        DuskglassEelId,
        StarharvestKoiId,
        LongnightKoiId,
        CrystalfinDaceId,
        QuartzscaleTroutId,
        ShardbackPerchId,
        StarlitCharId,
        MistglassSmeltId,
        StardustPikeId,
        StarharvestChubId,
        LongnightGlowlingId,
        MoonwaterMinnowId,
        MarshveilKilliId,
        SilverreedMudfishId,
        MooncapGobyId,
        RainveilLampreyId,
        StardustRayId,
        StarharvestOrbfinId,
        LongnightWispfishId
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
        CometTuberSeedId,
        RipplecapSeedId,
        TideglassTaroSeedId,
        LanternReedSeedId,
        RainveilLotusSeedId,
        AuricShootSeedId,
        SunvaultGourdSeedId,
        CrownstarSaffronSeedId,
        AmberthreadClusterSeedId
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
        CometTuberId,
        RipplecapId,
        TideglassTaroId,
        LanternReedId,
        RainveilLotusId,
        AuricShootId,
        SunvaultGourdId,
        CrownstarSaffronId,
        AmberthreadClusterId
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

    public static readonly IReadOnlyList<string> RainveilCropIds =
    [
        RipplecapId,
        TideglassTaroId,
        LanternReedId,
        RainveilLotusId
    ];

    public static readonly IReadOnlyList<string> RainveilSeedItemIds =
    [
        RipplecapSeedId,
        TideglassTaroSeedId,
        LanternReedSeedId,
        RainveilLotusSeedId
    ];

    public static readonly IReadOnlyList<string> StarharvestCropIds =
    [
        AuricShootId,
        SunvaultGourdId,
        CrownstarSaffronId,
        AmberthreadClusterId
    ];

    public static readonly IReadOnlyList<string> StarharvestSeedItemIds =
    [
        AuricShootSeedId,
        SunvaultGourdSeedId,
        CrownstarSaffronSeedId,
        AmberthreadClusterSeedId
    ];

    public static readonly IReadOnlyList<string> LongnightGreenhouseSeedItemIds =
    [
        CloudleafSeedId,
        StarbudSeedId,
        MoonrootSeedId,
        GlowpeaSeedId,
        EmberbellSeedId,
        DuskbellSeedId,
        PrismcornSeedId,
        DewmelonSeedId
    ];

    public static readonly IReadOnlyList<string> SaplingItemIds =
    [
        MoonplumSaplingId
    ];

    public static readonly IReadOnlyList<string> FishItemIds =
    [
        PondglowMinnowId,
        ReedwhisperBreamId,
        LanternscaleCarpId,
        SunveilGudgeonId,
        RainpetalLoachId,
        DuskglassEelId,
        StarharvestKoiId,
        LongnightKoiId,
        CrystalfinDaceId,
        QuartzscaleTroutId,
        ShardbackPerchId,
        StarlitCharId,
        MistglassSmeltId,
        StardustPikeId,
        StarharvestChubId,
        LongnightGlowlingId,
        MoonwaterMinnowId,
        MarshveilKilliId,
        SilverreedMudfishId,
        MooncapGobyId,
        RainveilLampreyId,
        StardustRayId,
        StarharvestOrbfinId,
        LongnightWispfishId
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
        CometTuberStarlightId,
        RipplecapLuminousId,
        RipplecapStarlightId,
        TideglassTaroLuminousId,
        TideglassTaroStarlightId,
        LanternReedLuminousId,
        LanternReedStarlightId,
        RainveilLotusLuminousId,
        RainveilLotusStarlightId,
        AuricShootLuminousId,
        AuricShootStarlightId,
        SunvaultGourdLuminousId,
        SunvaultGourdStarlightId,
        CrownstarSaffronLuminousId,
        CrownstarSaffronStarlightId,
        AmberthreadClusterLuminousId,
        AmberthreadClusterStarlightId
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
        RipplecapSeedId,
        RipplecapId,
        RipplecapLuminousId,
        RipplecapStarlightId,
        TideglassTaroSeedId,
        TideglassTaroId,
        TideglassTaroLuminousId,
        TideglassTaroStarlightId,
        LanternReedSeedId,
        LanternReedId,
        LanternReedLuminousId,
        LanternReedStarlightId,
        RainveilLotusSeedId,
        RainveilLotusId,
        RainveilLotusLuminousId,
        RainveilLotusStarlightId,
        AuricShootSeedId,
        AuricShootId,
        AuricShootLuminousId,
        AuricShootStarlightId,
        SunvaultGourdSeedId,
        SunvaultGourdId,
        SunvaultGourdLuminousId,
        SunvaultGourdStarlightId,
        CrownstarSaffronSeedId,
        CrownstarSaffronId,
        CrownstarSaffronLuminousId,
        CrownstarSaffronStarlightId,
        AmberthreadClusterSeedId,
        AmberthreadClusterId,
        AmberthreadClusterLuminousId,
        AmberthreadClusterStarlightId,
        StarsoilFertilizerId,
        StarbudPreserveId,
        MoonrootTonicId,
        CloudleafTeaId,
        MoonplumSaplingId,
        MoonplumId,
        StarhoneyId,
        MeadowFodderId,
        StarfeatherEggId,
        StarfeatherEggLuminousId,
        StarfeatherEggStarlightId,
        MoonfleeceId,
        MoonfleeceLuminousId,
        MoonfleeceStarlightId,
        DewhornMilkId,
        DewhornMilkLuminousId,
        DewhornMilkStarlightId,
        MoonmistStewId,
        SunvaultHashId,
        StarhoneyCustardId,
        LanternrootBrothId,
        LumenwoodId,
        CrystalShardId,
        WhisperbloomId,
        DewglassCloverId,
        RainbellMossId,
        MistcoilFernId,
        GloamgoldBerryId,
        SunwispPodId,
        NightlampLichenId,
        FrostwickRootId,
        PondglowMinnowId,
        ReedwhisperBreamId,
        LanternscaleCarpId,
        SunveilGudgeonId,
        RainpetalLoachId,
        DuskglassEelId,
        StarharvestKoiId,
        LongnightKoiId,
        CrystalfinDaceId,
        QuartzscaleTroutId,
        ShardbackPerchId,
        StarlitCharId,
        MistglassSmeltId,
        StardustPikeId,
        StarharvestChubId,
        LongnightGlowlingId,
        MoonwaterMinnowId,
        MarshveilKilliId,
        SilverreedMudfishId,
        MooncapGobyId,
        RainveilLampreyId,
        StardustRayId,
        StarharvestOrbfinId,
        LongnightWispfishId,
        StarwovenChestId,
        MoonstonePathId,
        StarwoodFenceId,
        StarlightTorchId,
        DewfallSprinklerId,
        GlowcombHiveId,
        MoonsteelShortbladeId,
        DawnpathCompassId,
        TideglassTabletId,
        HushedGleambellId,
        StarweaveSpindleId
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
            ),
            [LongnightSnowWeatherId] = new(
                LongnightSnowWeatherId,
                "weather.longnight_snow",
                -1,
                EffectKey: "weather.longnight_snow.effect",
                OutdoorMovementMultiplier: 0.85f
            )
        };

    public static readonly IReadOnlyDictionary<string, FishDefinition> Fishes =
        new Dictionary<string, FishDefinition>(StringComparer.Ordinal)
        {
            [PondglowMinnowId] = new(
                PondglowMinnowId,
                PondglowMinnowId,
                FishingWaterKind.HomesteadPond,
                "fish.pondglow_minnow"
            ),
            [ReedwhisperBreamId] = new(
                ReedwhisperBreamId,
                ReedwhisperBreamId,
                FishingWaterKind.HomesteadPond,
                "fish.reedwhisper_bream",
                StartMinute: 10 * 60,
                EndMinute: 15 * 60
            ),
            [LanternscaleCarpId] = new(
                LanternscaleCarpId,
                LanternscaleCarpId,
                FishingWaterKind.HomesteadPond,
                "fish.lanternscale_carp",
                StartMinute: 15 * 60,
                EndMinute: 20 * 60
            ),
            [SunveilGudgeonId] = new(
                SunveilGudgeonId,
                SunveilGudgeonId,
                FishingWaterKind.HomesteadPond,
                "fish.sunveil_gudgeon",
                [CalendarSystem.GleamriseSeasonId],
                StartMinute: 12 * 60,
                EndMinute: 18 * 60
            ),
            [RainpetalLoachId] = new(
                RainpetalLoachId,
                RainpetalLoachId,
                FishingWaterKind.HomesteadPond,
                "fish.rainpetal_loach",
                WeatherId: RainWeatherId
            ),
            [DuskglassEelId] = new(
                DuskglassEelId,
                DuskglassEelId,
                FishingWaterKind.HomesteadPond,
                "fish.duskglass_eel",
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
            ),
            [StarharvestKoiId] = new(
                StarharvestKoiId,
                StarharvestKoiId,
                FishingWaterKind.HomesteadPond,
                "fish.starharvest_koi",
                [CalendarSystem.StarharvestSeasonId],
                StartMinute: 8 * 60,
                EndMinute: 18 * 60
            ),
            [LongnightKoiId] = new(
                LongnightKoiId,
                LongnightKoiId,
                FishingWaterKind.HomesteadPond,
                "fish.longnight_koi",
                [CalendarSystem.LongnightSeasonId],
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
            ),
            [CrystalfinDaceId] = new(
                CrystalfinDaceId,
                CrystalfinDaceId,
                FishingWaterKind.CrystalStream,
                "fish.crystalfin_dace"
            ),
            [QuartzscaleTroutId] = new(
                QuartzscaleTroutId,
                QuartzscaleTroutId,
                FishingWaterKind.CrystalStream,
                "fish.quartzscale_trout",
                StartMinute: 10 * 60,
                EndMinute: 15 * 60
            ),
            [ShardbackPerchId] = new(
                ShardbackPerchId,
                ShardbackPerchId,
                FishingWaterKind.CrystalStream,
                "fish.shardback_perch",
                StartMinute: 14 * 60,
                EndMinute: 19 * 60
            ),
            [StarlitCharId] = new(
                StarlitCharId,
                StarlitCharId,
                FishingWaterKind.CrystalStream,
                "fish.starlit_char",
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
            ),
            [MistglassSmeltId] = new(
                MistglassSmeltId,
                MistglassSmeltId,
                FishingWaterKind.CrystalStream,
                "fish.mistglass_smelt",
                WeatherId: RainWeatherId
            ),
            [StardustPikeId] = new(
                StardustPikeId,
                StardustPikeId,
                FishingWaterKind.CrystalStream,
                "fish.stardust_pike",
                StartMinute: 12 * 60,
                EndMinute: GameClock.EndMinute,
                WeatherId: StardustWindWeatherId
            ),
            [StarharvestChubId] = new(
                StarharvestChubId,
                StarharvestChubId,
                FishingWaterKind.CrystalStream,
                "fish.starharvest_chub",
                [CalendarSystem.StarharvestSeasonId],
                StartMinute: GameClock.StartMinute,
                EndMinute: 14 * 60
            ),
            [LongnightGlowlingId] = new(
                LongnightGlowlingId,
                LongnightGlowlingId,
                FishingWaterKind.CrystalStream,
                "fish.longnight_glowling",
                [CalendarSystem.LongnightSeasonId],
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
            ),
            [MoonwaterMinnowId] = new(
                MoonwaterMinnowId,
                MoonwaterMinnowId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.moonwater_minnow"
            ),
            [MarshveilKilliId] = new(
                MarshveilKilliId,
                MarshveilKilliId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.marshveil_killi",
                StartMinute: 10 * 60,
                EndMinute: 16 * 60
            ),
            [SilverreedMudfishId] = new(
                SilverreedMudfishId,
                SilverreedMudfishId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.silverreed_mudfish",
                StartMinute: 14 * 60,
                EndMinute: 20 * 60
            ),
            [MooncapGobyId] = new(
                MooncapGobyId,
                MooncapGobyId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.mooncap_goby",
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
            ),
            [RainveilLampreyId] = new(
                RainveilLampreyId,
                RainveilLampreyId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.rainveil_lamprey",
                [CalendarSystem.RainveilSeasonId],
                WeatherId: RainWeatherId
            ),
            [StardustRayId] = new(
                StardustRayId,
                StardustRayId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.stardust_ray",
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute,
                WeatherId: StardustWindWeatherId
            ),
            [StarharvestOrbfinId] = new(
                StarharvestOrbfinId,
                StarharvestOrbfinId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.starharvest_orbfin",
                [CalendarSystem.StarharvestSeasonId],
                StartMinute: 10 * 60,
                EndMinute: 18 * 60
            ),
            [LongnightWispfishId] = new(
                LongnightWispfishId,
                LongnightWispfishId,
                FishingWaterKind.MoonwaterWetlands,
                "fish.longnight_wispfish",
                [CalendarSystem.LongnightSeasonId],
                StartMinute: 18 * 60,
                EndMinute: GameClock.EndMinute
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
            ),
            [RipplecapId] = new(
                RipplecapId,
                RipplecapSeedId,
                RipplecapId,
                "crop.ripplecap",
                [0, 1, 2],
                0,
                SeasonIds: [CalendarSystem.RainveilSeasonId]
            ),
            [TideglassTaroId] = new(
                TideglassTaroId,
                TideglassTaroSeedId,
                TideglassTaroId,
                "crop.tideglass_taro",
                [0, 1, 3, 4],
                0,
                SeasonIds: [CalendarSystem.RainveilSeasonId]
            ),
            [LanternReedId] = new(
                LanternReedId,
                LanternReedSeedId,
                LanternReedId,
                "crop.lantern_reed",
                [0, 1, 2, 4],
                0,
                SeasonIds: [CalendarSystem.RainveilSeasonId],
                RegrowthNights: 2
            ),
            [RainveilLotusId] = new(
                RainveilLotusId,
                RainveilLotusSeedId,
                RainveilLotusId,
                "crop.rainveil_lotus",
                [0, 1, 3, 5],
                0,
                SeasonIds: [CalendarSystem.RainveilSeasonId]
            ),
            [AuricShootId] = new(
                AuricShootId,
                AuricShootSeedId,
                AuricShootId,
                "crop.auric_shoot",
                [0, 1, 2, 3],
                0,
                SeasonIds: [CalendarSystem.StarharvestSeasonId]
            ),
            [SunvaultGourdId] = new(
                SunvaultGourdId,
                SunvaultGourdSeedId,
                SunvaultGourdId,
                "crop.sunvault_gourd",
                [0, 1, 3, 4],
                0,
                SeasonIds: [CalendarSystem.StarharvestSeasonId]
            ),
            [CrownstarSaffronId] = new(
                CrownstarSaffronId,
                CrownstarSaffronSeedId,
                CrownstarSaffronId,
                "crop.crownstar_saffron",
                [0, 2, 4, 6],
                0,
                SeasonIds: [CalendarSystem.StarharvestSeasonId]
            ),
            [AmberthreadClusterId] = new(
                AmberthreadClusterId,
                AmberthreadClusterSeedId,
                AmberthreadClusterId,
                "crop.amberthread_cluster",
                [0, 1, 3, 5],
                0,
                SeasonIds: [CalendarSystem.StarharvestSeasonId],
                RegrowthNights: 3
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

    public static readonly IReadOnlyList<string> CookedDishItemIds =
    [
        MoonmistStewId,
        SunvaultHashId,
        StarhoneyCustardId,
        LanternrootBrothId
    ];

    public static readonly IReadOnlyDictionary<string, CookingRecipeDefinition>
        CookingRecipes =
            new Dictionary<string, CookingRecipeDefinition>(
                StringComparer.Ordinal
            )
            {
                [MoonmistStewRecipeId] = new(
                    MoonmistStewRecipeId,
                    MoonmistStewId,
                    1,
                    [
                        new CraftingIngredient(RipplecapId, 1),
                        new CraftingIngredient(MistsongMintId, 1),
                        new CraftingIngredient(DewhornMilkId, 1)
                    ],
                    "recipe.moonmist_stew"
                ),
                [SunvaultHashRecipeId] = new(
                    SunvaultHashRecipeId,
                    SunvaultHashId,
                    1,
                    [
                        new CraftingIngredient(SunvaultGourdId, 1),
                        new CraftingIngredient(CometTuberId, 1)
                    ],
                    "recipe.sunvault_hash"
                ),
                [StarhoneyCustardRecipeId] = new(
                    StarhoneyCustardRecipeId,
                    StarhoneyCustardId,
                    1,
                    [
                        new CraftingIngredient(StarhoneyId, 1),
                        new CraftingIngredient(StarfeatherEggId, 1),
                        new CraftingIngredient(MoonplumId, 1)
                    ],
                    "recipe.starhoney_custard"
                ),
                [LanternrootBrothRecipeId] = new(
                    LanternrootBrothRecipeId,
                    LanternrootBrothId,
                    1,
                    [
                        new CraftingIngredient(LanternReedId, 1),
                        new CraftingIngredient(MoonrootId, 1),
                        new CraftingIngredient(TideglassTaroId, 1)
                    ],
                    "recipe.lanternroot_broth"
                )
            };

    public static readonly IReadOnlyDictionary<string, CookedDishDefinition>
        CookedDishes =
            new Dictionary<string, CookedDishDefinition>(
                StringComparer.Ordinal
            )
            {
                [MoonmistStewId] = new(MoonmistStewId, 60),
                [SunvaultHashId] = new(SunvaultHashId, 45),
                [StarhoneyCustardId] = new(StarhoneyCustardId, 70),
                [LanternrootBrothId] = new(LanternrootBrothId, 55)
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

    public static readonly StarlightPedestalDefinition HomesteadStarlight =
        new(
            HomesteadStarlightId,
            "starlight.homestead.name",
            "starlight.homestead.region",
            "starlight.homestead.reward_title",
            "starlight.homestead.reward_description",
            [
                new StarlightNodeDefinition(
                    HomesteadHarvestNodeId,
                    "starlight.node.homestead_harvest.title",
                    "starlight.node.homestead_harvest.description",
                    4,
                    CropIds.Select(itemId =>
                        new StarlightContributionOption(itemId, 1)
                    ).ToArray()
                ),
                new StarlightNodeDefinition(
                    HomesteadArtisanNodeId,
                    "starlight.node.homestead_artisan.title",
                    "starlight.node.homestead_artisan.description",
                    3,
                    [
                        new StarlightContributionOption(StarbudPreserveId, 1),
                        new StarlightContributionOption(MoonrootTonicId, 1),
                        new StarlightContributionOption(CloudleafTeaId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    HomesteadBuildingNodeId,
                    "starlight.node.homestead_building.title",
                    "starlight.node.homestead_building.description",
                    3,
                    [
                        new StarlightContributionOption(MoonstonePathId, 1),
                        new StarlightContributionOption(StarwoodFenceId, 1),
                        new StarlightContributionOption(StarlightTorchId, 1),
                        new StarlightContributionOption(DewfallSprinklerId, 1)
                    ]
                )
            ]
        );

    public static readonly StarlightPedestalDefinition MeadowStarlight =
        new(
            MeadowStarlightId,
            "starlight.meadow.name",
            "starlight.meadow.region",
            "starlight.meadow.reward_title",
            "starlight.meadow.reward_description",
            [
                new StarlightNodeDefinition(
                    MeadowBloomsNodeId,
                    "starlight.node.meadow_blooms.title",
                    "starlight.node.meadow_blooms.description",
                    3,
                    [
                        new StarlightContributionOption(DawnlaceId, 1),
                        new StarlightContributionOption(EmberbellId, 1),
                        new StarlightContributionOption(DuskbellId, 1),
                        new StarlightContributionOption(RainveilLotusId, 1),
                        new StarlightContributionOption(
                            CrownstarSaffronId,
                            1
                        )
                    ]
                ),
                new StarlightNodeDefinition(
                    MeadowBountyNodeId,
                    "starlight.node.meadow_bounty.title",
                    "starlight.node.meadow_bounty.description",
                    4,
                    [
                        new StarlightContributionOption(StarhoneyId, 1),
                        new StarlightContributionOption(
                            StarfeatherEggId,
                            1
                        ),
                        new StarlightContributionOption(MoonfleeceId, 1),
                        new StarlightContributionOption(DewhornMilkId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    MeadowCelebrationNodeId,
                    "starlight.node.meadow_celebration.title",
                    "starlight.node.meadow_celebration.description",
                    1,
                    [],
                    StarlightNodeSourceKind.FestivalResults,
                    [
                        FestivalCatalog.GleamrisePlantingFestivalId,
                        FestivalCatalog.StarharvestMarketFestivalId
                    ]
                )
            ],
            "starlight.meadow.activated"
        );

    public static readonly StarlightPedestalDefinition MoonwaterStarlight =
        new(
            MoonwaterStarlightId,
            "starlight.moonwater.name",
            "starlight.moonwater.region",
            "starlight.moonwater.reward_title",
            "starlight.moonwater.reward_description",
            [
                new StarlightNodeDefinition(
                    MoonwaterLocalFishNodeId,
                    "starlight.node.moonwater_local.title",
                    "starlight.node.moonwater_local.description",
                    3,
                    [
                        new StarlightContributionOption(MoonwaterMinnowId, 1),
                        new StarlightContributionOption(MarshveilKilliId, 1),
                        new StarlightContributionOption(
                            SilverreedMudfishId,
                            1
                        ),
                        new StarlightContributionOption(MooncapGobyId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    MoonwaterWeatherFishNodeId,
                    "starlight.node.moonwater_weather.title",
                    "starlight.node.moonwater_weather.description",
                    2,
                    [
                        new StarlightContributionOption(RainveilLampreyId, 1),
                        new StarlightContributionOption(StardustRayId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    MoonwaterSeasonalFishNodeId,
                    "starlight.node.moonwater_seasonal.title",
                    "starlight.node.moonwater_seasonal.description",
                    2,
                    [
                        new StarlightContributionOption(
                            StarharvestOrbfinId,
                            1
                        ),
                        new StarlightContributionOption(
                            LongnightWispfishId,
                            1
                        )
                    ]
                )
            ],
            "starlight.moonwater.activated"
        );

    public static readonly StarlightPedestalDefinition CrystalValeStarlight =
        new(
            CrystalValeStarlightId,
            "starlight.crystal_vale.name",
            "starlight.crystal_vale.region",
            "starlight.crystal_vale.reward_title",
            "starlight.crystal_vale.reward_description",
            [
                new StarlightNodeDefinition(
                    CrystalValeMineralChorusNodeId,
                    "starlight.node.crystal_vale_mineral_chorus.title",
                    "starlight.node.crystal_vale_mineral_chorus.description",
                    4,
                    [
                        new StarlightContributionOption(LumenSlateOreId, 1),
                        new StarlightContributionOption(MoonveinOreId, 1),
                        new StarlightContributionOption(PrismheartOreId, 1),
                        new StarlightContributionOption(StarironOreId, 1)
                    ]
                ),
                new StarlightNodeDefinition(
                    CrystalValeTemperedShovelNodeId,
                    "starlight.node.crystal_vale_tempered_shovel.title",
                    "starlight.node.crystal_vale_tempered_shovel.description",
                    1,
                    [],
                    StarlightNodeSourceKind.Milestones,
                    [ToolProgressionCatalog.ShovelBronzeStarUpgradeId]
                ),
                new StarlightNodeDefinition(
                    CrystalValeDepthAnchorNodeId,
                    "starlight.node.crystal_vale_depth_anchor.title",
                    "starlight.node.crystal_vale_depth_anchor.description",
                    1,
                    [],
                    StarlightNodeSourceKind.Milestones,
                    [MiningCatalog.CrystalGrottoFifthRoomAnchorId]
                )
            ],
            "starlight.crystal_vale.activated",
            CrystalRuinsPassageRewardId
        );

    public static readonly StarlightPedestalDefinition StarfallRuinsStarlight =
        new(
            StarfallRuinsStarlightId,
            "starlight.starfall_ruins.name",
            "starlight.starfall_ruins.region",
            "starlight.starfall_ruins.reward_title",
            "starlight.starfall_ruins.reward_description",
            [
                new StarlightNodeDefinition(
                    StarfallMemoryArchiveNodeId,
                    "starlight.node.starfall_memory_archive.title",
                    "starlight.node.starfall_memory_archive.description",
                    3,
                    [],
                    StarlightNodeSourceKind.Milestones,
                    [
                        DawnpathCompassId,
                        TideglassTabletId,
                        HushedGleambellId,
                        StarweaveSpindleId
                    ]
                ),
                new StarlightNodeDefinition(
                    StarfallNightwatchTrialNodeId,
                    "starlight.node.starfall_nightwatch_trial.title",
                    "starlight.node.starfall_nightwatch_trial.description",
                    3,
                    [],
                    StarlightNodeSourceKind.Milestones,
                    [
                        StarfallRuinsTrialCatalog.ShardlingEnemyId,
                        StarfallRuinsTrialCatalog.PrismWispEnemyId,
                        StarfallRuinsTrialCatalog.HollowSentinelEnemyId
                    ]
                ),
                new StarlightNodeDefinition(
                    StarfallTrustedPathsNodeId,
                    "starlight.node.starfall_trusted_paths.title",
                    "starlight.node.starfall_trusted_paths.description",
                    2,
                    [],
                    StarlightNodeSourceKind.Milestones,
                    [
                        KaelTrustedRelationshipMilestoneId,
                        LioraTrustedRelationshipMilestoneId
                    ]
                ),
                new StarlightNodeDefinition(
                    StarfallFiveLightsNodeId,
                    "starlight.node.starfall_five_lights.title",
                    "starlight.node.starfall_five_lights.description",
                    5,
                    [],
                    StarlightNodeSourceKind.PedestalRewards,
                    [
                        WoodlandStarlightId,
                        HomesteadStarlightId,
                        MeadowStarlightId,
                        MoonwaterStarlightId,
                        CrystalValeStarlightId
                    ]
                )
            ],
            "starlight.starfall_ruins.activated",
            StarfallSixfoldConvergenceRewardId,
            RequiresManualActivation: true
        );

    public static readonly IReadOnlyDictionary<string, StarlightPedestalDefinition>
        StarlightPedestals =
            new Dictionary<string, StarlightPedestalDefinition>(
                StringComparer.Ordinal
            )
            {
                [WoodlandStarlight.Id] = WoodlandStarlight,
                [HomesteadStarlight.Id] = HomesteadStarlight,
                [MeadowStarlight.Id] = MeadowStarlight,
                [MoonwaterStarlight.Id] = MoonwaterStarlight,
                [CrystalValeStarlight.Id] = CrystalValeStarlight,
                [StarfallRuinsStarlight.Id] = StarfallRuinsStarlight
            };

    public static readonly IReadOnlyDictionary<string, StarlightNodeDefinition>
        StarlightNodes = StarlightPedestals.Values
            .SelectMany(pedestal => pedestal.Nodes)
            .ToDictionary(node => node.Id, StringComparer.Ordinal);

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
        var qualityVariants = Items.Values
            .Where(item => item.BaseItemId == baseItemId)
            .OrderBy(item => item.Quality)
            .Select(item => item.Id)
            .ToArray();
        if (!Crops.ContainsKey(baseItemId) && qualityVariants.Length == 0)
        {
            return [itemId];
        }

        var family = new List<string> { baseItemId };
        family.AddRange(qualityVariants);
        if (Crops.TryGetValue(baseItemId, out var crop) &&
            crop.Resonances is { Count: > 0 } resonances)
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
