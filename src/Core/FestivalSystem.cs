namespace Luminfield.Core;

public sealed record FestivalDefinition(
    string Id,
    string LocationId,
    string NameKey,
    string SeasonId,
    int SeasonDay,
    int OpenMinute,
    int CloseMinute
);

public sealed record FestivalAwardDefinition(
    string Id,
    string NameKey,
    int MinimumScore,
    int ScripReward
);

public sealed record FestivalOfferDefinition(
    string Id,
    string ItemId,
    int Count,
    int ScripCost
);

public sealed record FestivalGiftExchangeDefinition(
    string Id,
    string GiftItemId,
    string RewardItemId,
    int RewardCount
);

public sealed record FestivalLongnightPreview(
    bool CanComplete,
    string FailureKey,
    IReadOnlyList<string> DishItemIds,
    FestivalGiftExchangeDefinition? Exchange,
    int Score,
    string AwardId,
    int LanternKnotReward
);

public sealed record FestivalLongnightResult(
    bool Succeeded,
    string MessageKey,
    FestivalYearResultSave? Result = null,
    int LanternKnotReward = 0
);

public sealed record FestivalSubmissionPreview(
    bool CanSubmit,
    string FailureKey,
    IReadOnlyList<string> ItemIds,
    int Score,
    string AwardId,
    int AuctionCoins,
    int ScripReward
);

public sealed record FestivalSubmissionResult(
    bool Succeeded,
    string MessageKey,
    FestivalYearResultSave? Result = null,
    int ScripReward = 0
);

public sealed record FestivalPurchaseCheck(
    bool CanPurchase,
    string FailureKey,
    FestivalOfferDefinition? Offer
);

public sealed record FestivalReplayRuleDefinition(
    string Id,
    string NameKey,
    string DescriptionKey
);

public sealed record FestivalRewardChoiceDefinition(
    string Id,
    string FestivalId,
    string ItemId,
    int Count
);

public sealed record FestivalPlantingStartCheck(
    bool CanStart,
    string FailureKey,
    IReadOnlyList<string> SelectedSeedItemIds
);

public sealed record FestivalPlantingCheck(
    bool CanPlant,
    string FailureKey,
    string PlotId,
    string SeedItemId
);

public sealed record FestivalPlantingResolution(
    bool Succeeded,
    bool Completed,
    string MessageKey,
    FestivalYearResultSave? Result = null,
    int CurrencyReward = 0
);

public static class FestivalCatalog
{
    public const string ClassicRuleId = "festival_rule_classic";
    public const string SeasonalFocusRuleId =
        "festival_rule_seasonal_focus";
    public const string CraftFocusRuleId = "festival_rule_craft_focus";
    public const string StarharvestMarketFestivalId =
        "festival_starharvest_market";
    public const string StarharvestShowcaseActivityId =
        "festival_starharvest_showcase";
    public const string StarharvestShopId = "festival_starharvest_shop";
    public const string StarharvestScripId = "festival_starharvest_scrip";
    public const string BronzeLeafAwardId =
        "festival_starharvest_bronze_leaf";
    public const string SilverSheafAwardId =
        "festival_starharvest_silver_sheaf";
    public const string GoldenCrownAwardId =
        "festival_starharvest_golden_crown";

    public const string GleamrisePlantingFestivalId =
        "festival_gleamrise_planting";
    public const string GleamriseSharedBloomfieldActivityId =
        "festival_gleamrise_shared_bloomfield";
    public const string GleamriseSeedExchangeId =
        "festival_gleamrise_seed_exchange";
    public const string GleamriseBloomTokenId =
        "festival_gleamrise_bloom_token";
    public const string GleamriseSproutKnotAwardId =
        "festival_gleamrise_sprout_knot";
    public const string GleamriseBloomWreathAwardId =
        "festival_gleamrise_bloom_wreath";
    public const string GleamriseStarfieldCrownAwardId =
        "festival_gleamrise_starfield_crown";

    public const string LongnightLanternFeastFestivalId =
        "festival_longnight_lantern_feast";
    public const string LongnightSharedTableId =
        "festival_longnight_shared_table";
    public const string LongnightGiftExchangeId =
        "festival_longnight_gift_exchange";
    public const string LongnightLanternStallId =
        "festival_longnight_lantern_stall";
    public const string LongnightStarlightRiteId =
        "festival_longnight_starlight_rite";
    public const string LongnightLanternKnotId =
        "festival_longnight_lantern_knot";
    public const string LongnightHearthsidePlaceAwardId =
        "festival_longnight_hearthside_place";
    public const string LongnightSharedGlowWreathAwardId =
        "festival_longnight_shared_glow_wreath";
    public const string LongnightStarwardHostAwardId =
        "festival_longnight_starward_host";

    public const string FireflyTideFestivalId =
        "festival_rainveil_firefly_tide";
    public const string FireflyLanternLaunchId =
        "festival_firefly_tide_lantern_launch";
    public const string FireflyFishBasinId =
        "festival_firefly_tide_fish_basin";
    public const string FireflyGlowshopId =
        "festival_firefly_tide_glowshop";
    public const string FireflyTideAltarId =
        "festival_firefly_tide_lantern_altar";
    public const string FireflyGlowmarkId =
        "festival_firefly_tide_glowmark";
    public const string FireflyReedLanternAwardId =
        "festival_firefly_tide_reed_lantern";
    public const string FireflyTideWreathAwardId =
        "festival_firefly_tide_tide_wreath";
    public const string FireflyMoonwakeBeaconAwardId =
        "festival_firefly_tide_moonwake_beacon";

    public const string FireflyLotusSeedOfferId =
        "firefly_offer_rainveil_lotus_seeds";
    public const string FireflyTorchOfferId =
        "firefly_offer_starlight_torches";
    public const string FireflyPathOfferId =
        "firefly_offer_moonstone_paths";
    public const string FireflyHiveOfferId =
        "firefly_offer_glowcomb_hive";

    public const string LongnightStarbudPreserveExchangeId =
        "festival_longnight_exchange_starbud_preserve";
    public const string LongnightCloudleafTeaExchangeId =
        "festival_longnight_exchange_cloudleaf_tea";
    public const string LongnightMoonrootTonicExchangeId =
        "festival_longnight_exchange_moonroot_tonic";
    public const string LongnightStarhoneyExchangeId =
        "festival_longnight_exchange_starhoney";

    public const string LongnightTorchBundleOfferId =
        "longnight_offer_starlight_torch_bundle";
    public const string LongnightStarsoilBundleOfferId =
        "longnight_offer_starsoil_bundle";
    public const string LongnightFodderBundleOfferId =
        "longnight_offer_meadow_fodder_bundle";
    public const string LongnightMoonplumSaplingOfferId =
        "longnight_offer_moonplum_sapling";

    public const string GleamriseDawnlaceOfferId =
        "gleamrise_offer_dawnlace_seed_bundle";
    public const string GleamriseGlimmerpodOfferId =
        "gleamrise_offer_glimmerpod_seed_bundle";
    public const string GleamriseMistsongMintOfferId =
        "gleamrise_offer_mistsong_mint_seed_bundle";
    public const string GleamriseCometTuberOfferId =
        "gleamrise_offer_comet_tuber_seed_bundle";

    public const int GleamriseChallengeLatestStartMinute = 14 * 60;
    public const int GleamriseChallengeDurationMinutes = 180;

    public const string AuricSeedBundleOfferId =
        "starharvest_offer_auric_seed_bundle";
    public const string StarsoilBundleOfferId =
        "starharvest_offer_starsoil_bundle";
    public const string TorchBundleOfferId =
        "starharvest_offer_torch_bundle";
    public const string FenceBundleOfferId =
        "starharvest_offer_fence_bundle";

    public const string StarharvestSeedRewardId =
        "festival_reward_starharvest_seeds";
    public const string StarharvestSoilRewardId =
        "festival_reward_starharvest_soil";
    public const string GleamriseDawnRewardId =
        "festival_reward_gleamrise_dawn";
    public const string GleamriseGlimmerRewardId =
        "festival_reward_gleamrise_glimmer";
    public const string LongnightTorchRewardId =
        "festival_reward_longnight_torches";
    public const string LongnightFodderRewardId =
        "festival_reward_longnight_fodder";
    public const string FireflyLotusRewardId =
        "festival_reward_firefly_lotus";
    public const string FireflyPathRewardId =
        "festival_reward_firefly_paths";

    public static readonly FestivalDefinition StarharvestMarket = new(
        StarharvestMarketFestivalId,
        PlayerLocationIds.StarharvestMarket,
        "festival.starharvest.name",
        CalendarSystem.StarharvestSeasonId,
        11,
        10 * 60,
        18 * 60
    );

    public static readonly FestivalDefinition GleamrisePlanting = new(
        GleamrisePlantingFestivalId,
        PlayerLocationIds.GleamrisePlantingFestival,
        "festival.gleamrise.name",
        CalendarSystem.GleamriseSeasonId,
        4,
        9 * 60,
        17 * 60
    );

    public static readonly FestivalDefinition LongnightLanternFeast = new(
        LongnightLanternFeastFestivalId,
        PlayerLocationIds.LongnightLanternFeast,
        "festival.longnight.name",
        CalendarSystem.LongnightSeasonId,
        13,
        17 * 60,
        22 * 60
    );

    public static readonly FestivalDefinition FireflyTide = new(
        FireflyTideFestivalId,
        PlayerLocationIds.FireflyTide,
        "festival.firefly.name",
        CalendarSystem.RainveilSeasonId,
        12,
        18 * 60,
        23 * 60
    );

    public static readonly IReadOnlyDictionary<string, FestivalDefinition>
        Festivals = new Dictionary<string, FestivalDefinition>(
            StringComparer.Ordinal
        )
        {
            [StarharvestMarket.Id] = StarharvestMarket,
            [GleamrisePlanting.Id] = GleamrisePlanting,
            [LongnightLanternFeast.Id] = LongnightLanternFeast,
            [FireflyTide.Id] = FireflyTide
        };

    public static readonly IReadOnlyDictionary<string,
        FestivalReplayRuleDefinition> ReplayRules =
            new Dictionary<string, FestivalReplayRuleDefinition>(
                StringComparer.Ordinal
            )
            {
                [ClassicRuleId] = new(
                    ClassicRuleId,
                    "festival.replay.rule.classic.name",
                    "festival.replay.rule.classic.description"
                ),
                [SeasonalFocusRuleId] = new(
                    SeasonalFocusRuleId,
                    "festival.replay.rule.seasonal.name",
                    "festival.replay.rule.seasonal.description"
                ),
                [CraftFocusRuleId] = new(
                    CraftFocusRuleId,
                    "festival.replay.rule.craft.name",
                    "festival.replay.rule.craft.description"
                )
            };

    public static readonly IReadOnlyDictionary<string,
        FestivalRewardChoiceDefinition> RewardChoices =
            new Dictionary<string, FestivalRewardChoiceDefinition>(
                StringComparer.Ordinal
            )
            {
                [StarharvestSeedRewardId] = new(
                    StarharvestSeedRewardId,
                    StarharvestMarketFestivalId,
                    DataCatalog.AuricShootSeedId,
                    4
                ),
                [StarharvestSoilRewardId] = new(
                    StarharvestSoilRewardId,
                    StarharvestMarketFestivalId,
                    DataCatalog.StarsoilFertilizerId,
                    3
                ),
                [GleamriseDawnRewardId] = new(
                    GleamriseDawnRewardId,
                    GleamrisePlantingFestivalId,
                    DataCatalog.DawnlaceSeedId,
                    4
                ),
                [GleamriseGlimmerRewardId] = new(
                    GleamriseGlimmerRewardId,
                    GleamrisePlantingFestivalId,
                    DataCatalog.GlimmerpodSeedId,
                    4
                ),
                [LongnightTorchRewardId] = new(
                    LongnightTorchRewardId,
                    LongnightLanternFeastFestivalId,
                    DataCatalog.StarlightTorchId,
                    3
                ),
                [LongnightFodderRewardId] = new(
                    LongnightFodderRewardId,
                    LongnightLanternFeastFestivalId,
                    DataCatalog.MeadowFodderId,
                    8
                ),
                [FireflyLotusRewardId] = new(
                    FireflyLotusRewardId,
                    FireflyTideFestivalId,
                    DataCatalog.RainveilLotusSeedId,
                    4
                ),
                [FireflyPathRewardId] = new(
                    FireflyPathRewardId,
                    FireflyTideFestivalId,
                    DataCatalog.MoonstonePathId,
                    6
                )
            };

    public static FestivalReplayRuleDefinition ReplayRuleFor(
        string festivalId,
        int year
    )
    {
        if (!Festivals.ContainsKey(festivalId) || year <= 1)
        {
            return ReplayRules[ClassicRuleId];
        }

        var ruleId = year % 2 == 0
            ? SeasonalFocusRuleId
            : CraftFocusRuleId;
        return ReplayRules[ruleId];
    }

    public static IReadOnlyList<FestivalRewardChoiceDefinition>
        RewardChoicesFor(string festivalId) => RewardChoices.Values
            .Where(choice => choice.FestivalId == festivalId)
            .OrderBy(choice => choice.Id, StringComparer.Ordinal)
            .ToArray();

    public static string RelationshipDialogueKey(
        string locationId,
        int year,
        int relationshipPoints,
        string fallbackKey
    )
    {
        var festival = FestivalAtLocation(locationId);
        if (festival is null || year <= 1)
        {
            return fallbackKey;
        }

        var prefix = festival.Id switch
        {
            GleamrisePlantingFestivalId => "festival.gleamrise.dialogue",
            LongnightLanternFeastFestivalId => "festival.longnight.dialogue",
            FireflyTideFestivalId => "festival.firefly.dialogue",
            _ => "festival.starharvest.dialogue"
        };
        if (relationshipPoints >= VillageSystem.KindredLightThreshold)
        {
            return $"{prefix}.relationship.kindred";
        }
        if (relationshipPoints >= VillageSystem.TrustedFriendThreshold)
        {
            return $"{prefix}.relationship.trusted";
        }

        return $"{prefix}.returning";
    }

    public static int ReplayScoreBonus(
        string festivalId,
        int year,
        IReadOnlyList<string> itemIds
    )
    {
        var rule = ReplayRuleFor(festivalId, year);
        if (rule.Id == ClassicRuleId || itemIds.Count == 0)
        {
            return 0;
        }

        if (rule.Id == SeasonalFocusRuleId)
        {
            return SeasonalFocusBonus(festivalId, itemIds);
        }

        return CraftFocusBonus(festivalId, itemIds);
    }

    private static int SeasonalFocusBonus(
        string festivalId,
        IReadOnlyList<string> itemIds
    ) => festivalId switch
    {
        StarharvestMarketFestivalId => itemIds
            .Select(DataCatalog.BaseItemId)
            .All(DataCatalog.StarharvestCropIds.Contains) ? 4 : 0,
        GleamrisePlantingFestivalId => itemIds
            .Distinct(StringComparer.Ordinal)
            .Count() >= 3 ? 4 : 0,
        LongnightLanternFeastFestivalId => itemIds.Count(itemId =>
            LongnightDishScores.GetValueOrDefault(itemId) >= 9) * 2,
        FireflyTideFestivalId => itemIds.Count(FireflyWeatherFishIds.Contains),
        _ => 0
    };

    private static int CraftFocusBonus(
        string festivalId,
        IReadOnlyList<string> itemIds
    ) => festivalId switch
    {
        StarharvestMarketFestivalId => itemIds.Any(ArtisanItemIds.Contains)
            ? 4
            : 0,
        GleamrisePlantingFestivalId => itemIds
            .Distinct(StringComparer.Ordinal)
            .Count() == 3 ? 4 : 0,
        LongnightLanternFeastFestivalId => itemIds
            .Distinct(StringComparer.Ordinal)
            .Count() == 2 ? 3 : 0,
        FireflyTideFestivalId => itemIds.Count(FireflySeasonalFishIds.Contains),
        _ => 0
    };

    public static readonly IReadOnlyList<FestivalAwardDefinition> Awards =
    [
        new(
            BronzeLeafAwardId,
            "festival.starharvest.award.bronze",
            0,
            4
        ),
        new(
            SilverSheafAwardId,
            "festival.starharvest.award.silver",
            20,
            7
        ),
        new(
            GoldenCrownAwardId,
            "festival.starharvest.award.gold",
            26,
            10
        )
    ];

    public static readonly IReadOnlyList<FestivalAwardDefinition>
        GleamriseAwards =
    [
        new(
            GleamriseSproutKnotAwardId,
            "festival.gleamrise.award.sprout",
            0,
            4
        ),
        new(
            GleamriseBloomWreathAwardId,
            "festival.gleamrise.award.bloom",
            20,
            7
        ),
        new(
            GleamriseStarfieldCrownAwardId,
            "festival.gleamrise.award.crown",
            25,
            10
        )
    ];

    public static readonly IReadOnlyList<FestivalAwardDefinition>
        LongnightAwards =
    [
        new(
            LongnightHearthsidePlaceAwardId,
            "festival.longnight.award.hearthside",
            0,
            4
        ),
        new(
            LongnightSharedGlowWreathAwardId,
            "festival.longnight.award.shared_glow",
            17,
            7
        ),
        new(
            LongnightStarwardHostAwardId,
            "festival.longnight.award.starward",
            19,
            10
        )
    ];

    public static readonly IReadOnlyList<FestivalAwardDefinition>
        FireflyAwards =
    [
        new(
            FireflyReedLanternAwardId,
            "festival.firefly.award.reed_lantern",
            0,
            4
        ),
        new(
            FireflyTideWreathAwardId,
            "festival.firefly.award.tide_wreath",
            13,
            7
        ),
        new(
            FireflyMoonwakeBeaconAwardId,
            "festival.firefly.award.moonwake_beacon",
            20,
            10
        )
    ];

    public static readonly IReadOnlyDictionary<string, FestivalOfferDefinition>
        StarharvestOffers =
            new Dictionary<string, FestivalOfferDefinition>(
                StringComparer.Ordinal
            )
            {
                [AuricSeedBundleOfferId] = new(
                    AuricSeedBundleOfferId,
                    DataCatalog.AuricShootSeedId,
                    3,
                    2
                ),
                [StarsoilBundleOfferId] = new(
                    StarsoilBundleOfferId,
                    DataCatalog.StarsoilFertilizerId,
                    2,
                    2
                ),
                [TorchBundleOfferId] = new(
                    TorchBundleOfferId,
                    DataCatalog.StarlightTorchId,
                    2,
                    3
                ),
                [FenceBundleOfferId] = new(
                    FenceBundleOfferId,
                    DataCatalog.StarwoodFenceId,
                    4,
                    3
                )
            };

    public static readonly IReadOnlyDictionary<string, FestivalOfferDefinition>
        GleamriseOffers =
            new Dictionary<string, FestivalOfferDefinition>(
                StringComparer.Ordinal
            )
            {
                [GleamriseDawnlaceOfferId] = new(
                    GleamriseDawnlaceOfferId,
                    DataCatalog.DawnlaceSeedId,
                    2,
                    2
                ),
                [GleamriseGlimmerpodOfferId] = new(
                    GleamriseGlimmerpodOfferId,
                    DataCatalog.GlimmerpodSeedId,
                    2,
                    3
                ),
                [GleamriseMistsongMintOfferId] = new(
                    GleamriseMistsongMintOfferId,
                    DataCatalog.MistsongMintSeedId,
                    2,
                    2
                ),
                [GleamriseCometTuberOfferId] = new(
                    GleamriseCometTuberOfferId,
                    DataCatalog.CometTuberSeedId,
                    2,
                    3
                )
            };

    public static readonly IReadOnlyDictionary<string, FestivalOfferDefinition>
        LongnightOffers =
            new Dictionary<string, FestivalOfferDefinition>(
                StringComparer.Ordinal
            )
            {
                [LongnightTorchBundleOfferId] = new(
                    LongnightTorchBundleOfferId,
                    DataCatalog.StarlightTorchId,
                    2,
                    2
                ),
                [LongnightStarsoilBundleOfferId] = new(
                    LongnightStarsoilBundleOfferId,
                    DataCatalog.StarsoilFertilizerId,
                    2,
                    2
                ),
                [LongnightFodderBundleOfferId] = new(
                    LongnightFodderBundleOfferId,
                    DataCatalog.MeadowFodderId,
                    6,
                    3
                ),
                [LongnightMoonplumSaplingOfferId] = new(
                    LongnightMoonplumSaplingOfferId,
                    DataCatalog.MoonplumSaplingId,
                    1,
                    6
                )
            };

    public static readonly IReadOnlyDictionary<string, FestivalOfferDefinition>
        FireflyOffers =
            new Dictionary<string, FestivalOfferDefinition>(
                StringComparer.Ordinal
            )
            {
                [FireflyLotusSeedOfferId] = new(
                    FireflyLotusSeedOfferId,
                    DataCatalog.RainveilLotusSeedId,
                    3,
                    2
                ),
                [FireflyTorchOfferId] = new(
                    FireflyTorchOfferId,
                    DataCatalog.StarlightTorchId,
                    2,
                    2
                ),
                [FireflyPathOfferId] = new(
                    FireflyPathOfferId,
                    DataCatalog.MoonstonePathId,
                    4,
                    3
                ),
                [FireflyHiveOfferId] = new(
                    FireflyHiveOfferId,
                    DataCatalog.GlowcombHiveId,
                    1,
                    6
                )
            };

    public static readonly IReadOnlyDictionary<string,
        FestivalGiftExchangeDefinition> LongnightGiftExchanges =
            new Dictionary<string, FestivalGiftExchangeDefinition>(
                StringComparer.Ordinal
            )
            {
                [LongnightStarbudPreserveExchangeId] = new(
                    LongnightStarbudPreserveExchangeId,
                    DataCatalog.StarbudPreserveId,
                    DataCatalog.MeadowFodderId,
                    6
                ),
                [LongnightCloudleafTeaExchangeId] = new(
                    LongnightCloudleafTeaExchangeId,
                    DataCatalog.CloudleafTeaId,
                    DataCatalog.StarsoilFertilizerId,
                    2
                ),
                [LongnightMoonrootTonicExchangeId] = new(
                    LongnightMoonrootTonicExchangeId,
                    DataCatalog.MoonrootTonicId,
                    DataCatalog.StarlightTorchId,
                    2
                ),
                [LongnightStarhoneyExchangeId] = new(
                    LongnightStarhoneyExchangeId,
                    DataCatalog.StarhoneyId,
                    DataCatalog.MoonplumSaplingId,
                    1
                )
            };

    public static readonly IReadOnlyDictionary<string, int>
        LongnightDishScores = new Dictionary<string, int>(
            StringComparer.Ordinal
        )
        {
            [DataCatalog.MoonmistStewId] = 9,
            [DataCatalog.SunvaultHashId] = 7,
            [DataCatalog.StarhoneyCustardId] = 10,
            [DataCatalog.LanternrootBrothId] = 9
        };

    public static readonly IReadOnlyList<string> FireflyTideFishIds =
    [
        DataCatalog.MoonwaterMinnowId,
        DataCatalog.MarshveilKilliId,
        DataCatalog.SilverreedMudfishId,
        DataCatalog.MooncapGobyId,
        DataCatalog.RainveilLampreyId,
        DataCatalog.StardustRayId,
        DataCatalog.StarharvestOrbfinId,
        DataCatalog.LongnightWispfishId
    ];

    public static readonly IReadOnlySet<string> FireflyWeatherFishIds =
        new HashSet<string>(
            [DataCatalog.RainveilLampreyId, DataCatalog.StardustRayId],
            StringComparer.Ordinal
        );

    public static readonly IReadOnlySet<string> FireflySeasonalFishIds =
        new HashSet<string>(
            [
                DataCatalog.StarharvestOrbfinId,
                DataCatalog.LongnightWispfishId
            ],
            StringComparer.Ordinal
        );

    public static readonly IReadOnlyList<string> GleamriseChallengeSeedIds =
    [
        DataCatalog.DawnlaceSeedId,
        DataCatalog.GlimmerpodSeedId,
        DataCatalog.MistsongMintSeedId,
        DataCatalog.CometTuberSeedId
    ];

    public static readonly IReadOnlySet<string> ArtisanItemIds =
        new HashSet<string>(
            [
                DataCatalog.StarbudPreserveId,
                DataCatalog.MoonrootTonicId,
                DataCatalog.CloudleafTeaId
            ],
            StringComparer.Ordinal
        );

    public static bool OccursOnDay(string festivalId, int day) =>
        Festivals.TryGetValue(festivalId, out var festival) &&
        CalendarSystem.SeasonId(day) == festival.SeasonId &&
        CalendarSystem.SeasonDay(day) == festival.SeasonDay;

    public static bool IsOpen(
        string festivalId,
        int day,
        int minuteOfDay
    ) => Festivals.TryGetValue(festivalId, out var festival) &&
        OccursOnDay(festivalId, day) &&
        minuteOfDay >= festival.OpenMinute &&
        minuteOfDay < festival.CloseMinute;

    public static bool IsTomorrow(string festivalId, int day) =>
        OccursOnDay(festivalId, Math.Max(1, day) + 1);

    public static FestivalDefinition? FestivalOnDay(int day) =>
        Festivals.Values.FirstOrDefault(festival =>
            OccursOnDay(festival.Id, day)
        );

    public static FestivalDefinition? FestivalAtLocation(
        string locationId
    ) => Festivals.Values.FirstOrDefault(festival =>
        festival.LocationId == locationId
    );

    public static bool IsEligibleExhibitItem(string itemId)
    {
        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        if (ArtisanItemIds.Contains(itemId))
        {
            return true;
        }

        return item.Kind == ItemKind.Produce &&
            DataCatalog.Crops.ContainsKey(DataCatalog.BaseItemId(itemId));
    }

    public static int Score(IReadOnlyList<string> itemIds)
    {
        var score = itemIds.Sum(itemId =>
        {
            var item = DataCatalog.Item(itemId);
            var valueScore = item.SellPrice switch
            {
                >= 180 => 9,
                >= 120 => 7,
                >= 60 => 5,
                _ => 3
            };
            var baseItemId = DataCatalog.BaseItemId(itemId);
            var seasonalBonus = DataCatalog.StarharvestCropIds.Contains(
                baseItemId,
                StringComparer.Ordinal
            )
                ? 1
                : 0;
            var artisanBonus = ArtisanItemIds.Contains(itemId) ? 2 : 0;
            return valueScore + seasonalBonus + artisanBonus;
        });
        var distinctFamilies = itemIds
            .Select(DataCatalog.BaseItemId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return score + (distinctFamilies == 3 ? 3 : 0);
    }

    public static FestivalAwardDefinition AwardForScore(int score) =>
        AwardForScore(StarharvestMarketFestivalId, score);

    public static FestivalAwardDefinition AwardForScore(
        string festivalId,
        int score
    ) => AwardsFor(festivalId)
            .Where(award => score >= award.MinimumScore)
            .OrderByDescending(award => award.MinimumScore)
            .First();

    public static IReadOnlyList<FestivalAwardDefinition> AwardsFor(
        string festivalId
    ) => festivalId switch
    {
        GleamrisePlantingFestivalId => GleamriseAwards,
        LongnightLanternFeastFestivalId => LongnightAwards,
        FireflyTideFestivalId => FireflyAwards,
        _ => Awards
    };

    public static int ResultItemLimit(string festivalId) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId =>
                GleamrisePlantingFestivalLayout.PlotIds.Count,
            LongnightLanternFeastFestivalId => 2,
            FireflyTideFestivalId => 3,
            _ => 3
        };

    public static string ClosedKey(string festivalId) => festivalId switch
    {
        GleamrisePlantingFestivalId => "festival.gleamrise.closed",
        LongnightLanternFeastFestivalId => "festival.longnight.closed",
        FireflyTideFestivalId => "festival.firefly.closed",
        _ => "festival.starharvest.closed"
    };

    public static string EnterNoticeKey(string festivalId) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId =>
                "notice.enter_gleamrise_festival",
            LongnightLanternFeastFestivalId =>
                "notice.enter_longnight_feast",
            FireflyTideFestivalId => "notice.enter_firefly_tide",
            _ => "notice.enter_starharvest_market"
        };

    public static string LeaveNoticeKey(string festivalId) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId =>
                "notice.leave_gleamrise_festival",
            LongnightLanternFeastFestivalId =>
                "notice.leave_longnight_feast",
            FireflyTideFestivalId => "notice.leave_firefly_tide",
            _ => "notice.leave_starharvest_market"
        };

    public static string EnterActionKey(string festivalId) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId =>
                "target.action.enter_gleamrise_festival",
            LongnightLanternFeastFestivalId =>
                "target.action.enter_longnight_feast",
            FireflyTideFestivalId =>
                "target.action.enter_firefly_tide",
            _ => "target.action.enter_starharvest_market"
        };

    public static string ExitActionKey(string festivalId) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId =>
                "target.action.exit_gleamrise_festival",
            LongnightLanternFeastFestivalId =>
                "target.action.exit_longnight_feast",
            FireflyTideFestivalId =>
                "target.action.exit_firefly_tide",
            _ => "target.action.exit_starharvest_market"
        };

    public static string HudKey(string festivalId, bool tomorrow) =>
        festivalId switch
        {
            GleamrisePlantingFestivalId => tomorrow
                ? "festival.gleamrise.hud.tomorrow"
                : "festival.gleamrise.hud.today",
            LongnightLanternFeastFestivalId => tomorrow
                ? "festival.longnight.hud.tomorrow"
                : "festival.longnight.hud.today",
            FireflyTideFestivalId => tomorrow
                ? "festival.firefly.hud.tomorrow"
                : "festival.firefly.hud.today",
            _ => tomorrow
                ? "festival.starharvest.hud.tomorrow"
                : "festival.starharvest.hud.today"
        };

    public static int GleamrisePlantingScore(
        IReadOnlyList<FestivalPlotPlantingSave> plantings,
        int elapsedMinutes
    )
    {
        var valid = plantings
            .Where(planting =>
                GleamrisePlantingFestivalLayout.PlotCellsById.ContainsKey(
                    planting.PlotId
                ) && GleamriseChallengeSeedIds.Contains(
                    planting.SeedItemId,
                    StringComparer.Ordinal
                ))
            .GroupBy(planting => planting.PlotId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var familyScore = valid
            .Select(planting => planting.SeedItemId)
            .Distinct(StringComparer.Ordinal)
            .Count() * 2;
        var byPlot = valid.ToDictionary(
            planting => planting.PlotId,
            planting => planting.SeedItemId,
            StringComparer.Ordinal
        );
        var harmonyRows = Enumerable.Range(0, 3).Count(row =>
        {
            var rowSeeds = GleamrisePlantingFestivalLayout.PlotIds
                .Skip(row * 4)
                .Take(4)
                .Where(byPlot.ContainsKey)
                .Select(plotId => byPlot[plotId])
                .ToArray();
            return rowSeeds.Length == 4 &&
                rowSeeds.Distinct(StringComparer.Ordinal).Count() == 3;
        });
        var complete = valid.Length ==
            GleamrisePlantingFestivalLayout.PlotIds.Count;
        var timeScore = !complete
            ? 0
            : elapsedMinutes <= 120
                ? 6
                : elapsedMinutes <= 150
                    ? 4
                    : elapsedMinutes <= 180
                        ? 2
                        : 0;
        return valid.Length + familyScore + harmonyRows * 2 + timeScore;
    }

    public static int AuctionCoins(IReadOnlyList<string> itemIds)
    {
        var sellTotal = itemIds.Sum(itemId => DataCatalog.Item(itemId).SellPrice);
        return sellTotal + sellTotal / 4;
    }

    public static bool IsEligibleFireflyFish(string itemId) =>
        FireflyTideFishIds.Contains(itemId, StringComparer.Ordinal);

    public static int FireflyTideScore(IReadOnlyList<string> itemIds) =>
        itemIds.Sum(itemId =>
        {
            var valueScore = DataCatalog.Item(itemId).SellPrice switch
            {
                >= 100 => 7,
                >= 70 => 5,
                >= 45 => 3,
                _ => 2
            };
            var weatherBonus = FireflyWeatherFishIds.Contains(itemId)
                ? 2
                : 0;
            var seasonalBonus = FireflySeasonalFishIds.Contains(itemId)
                ? 2
                : 0;
            var nightBonus = itemId == DataCatalog.MooncapGobyId ? 1 : 0;
            return valueScore + weatherBonus + seasonalBonus + nightBonus;
        });
}

public sealed class FestivalSystem
{
    private readonly Dictionary<(string FestivalId, int Year),
        FestivalYearResultSave> _results = [];
    private readonly Dictionary<(string FestivalId, int Year),
        FestivalPlantingAttemptSave> _plantingAttempts = [];
    private readonly Dictionary<string, int> _currencyBalances =
        new(StringComparer.Ordinal);

    public int Scrip { get; private set; }
    public IReadOnlyList<FestivalYearResultSave> Results => _results.Values
        .OrderBy(result => result.Year)
        .ThenBy(result => result.FestivalId, StringComparer.Ordinal)
        .Select(Clone)
        .ToArray();
    public int BloomTokens => CurrencyBalance(
        FestivalCatalog.GleamriseBloomTokenId
    );
    public int LanternKnots => CurrencyBalance(
        FestivalCatalog.LongnightLanternKnotId
    );
    public int Glowmarks => CurrencyBalance(
        FestivalCatalog.FireflyGlowmarkId
    );

    public event Action? Changed;

    public void Reset()
    {
        Scrip = 0;
        _results.Clear();
        _plantingAttempts.Clear();
        _currencyBalances.Clear();
        Changed?.Invoke();
    }

    public void Restore(FestivalSave? save)
    {
        var normalized = NormalizeSave(save);
        Scrip = normalized.Scrip;
        _results.Clear();
        foreach (var result in normalized.Results)
        {
            _results[(result.FestivalId, result.Year)] = Clone(result);
        }
        _plantingAttempts.Clear();
        foreach (var attempt in normalized.PlantingAttempts)
        {
            _plantingAttempts[(attempt.FestivalId, attempt.Year)] =
                Clone(attempt);
        }
        _currencyBalances.Clear();
        foreach (var currency in normalized.CurrencyBalances)
        {
            _currencyBalances[currency.CurrencyId] = currency.Balance;
        }

        Changed?.Invoke();
    }

    public bool HasParticipated(string festivalId, int year) =>
        _results.ContainsKey((festivalId, Math.Max(1, year)));

    public FestivalYearResultSave? ResultFor(string festivalId, int year) =>
        _results.TryGetValue((festivalId, Math.Max(1, year)), out var result)
            ? Clone(result)
            : null;

    public FestivalPlantingAttemptSave? PlantingAttemptFor(
        string festivalId,
        int year
    ) => _plantingAttempts.TryGetValue(
        (festivalId, Math.Max(1, year)),
        out var attempt
    )
        ? Clone(attempt)
        : null;

    public int CurrencyBalance(string currencyId) =>
        _currencyBalances.GetValueOrDefault(currencyId);

    public ActionResult CheckRewardChoice(
        string festivalId,
        int year,
        string choiceId,
        Inventory inventory
    )
    {
        if (!_results.TryGetValue(
                (festivalId, Math.Max(1, year)),
                out var result
            ))
        {
            return ActionResult.Fail("festival.replay.reward.no_result");
        }
        if (result.RewardClaimed)
        {
            return ActionResult.Fail("festival.replay.reward.already_claimed");
        }
        if (!FestivalCatalog.RewardChoices.TryGetValue(
                choiceId,
                out var choice
            ) || choice.FestivalId != festivalId)
        {
            return ActionResult.Fail("festival.replay.reward.unknown");
        }
        if (!inventory.CanAdd(choice.ItemId, choice.Count))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        return ActionResult.Grant(
            choice.ItemId,
            choice.Count,
            0,
            "festival.replay.reward.ready"
        );
    }

    public ActionResult ClaimRewardChoice(
        string festivalId,
        int year,
        string choiceId,
        Inventory inventory
    )
    {
        var check = CheckRewardChoice(
            festivalId,
            year,
            choiceId,
            inventory
        );
        if (!check.Succeeded ||
            !FestivalCatalog.RewardChoices.TryGetValue(
                choiceId,
                out var choice
            ))
        {
            return check;
        }
        if (!inventory.Add(choice.ItemId, choice.Count))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var key = (festivalId, Math.Max(1, year));
        var result = _results[key];
        result.RewardChoiceId = choice.Id;
        result.RewardClaimed = true;
        Changed?.Invoke();
        return ActionResult.Grant(
            choice.ItemId,
            choice.Count,
            0,
            "festival.replay.reward.claimed"
        );
    }

    public FestivalSubmissionPreview CheckSubmission(
        string festivalId,
        int year,
        IReadOnlyList<string>? itemIds,
        Inventory inventory
    )
    {
        var items = itemIds?.Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray() ?? [];
        if (!FestivalCatalog.Festivals.ContainsKey(festivalId))
        {
            return Invalid("festival.submission.unknown", items);
        }

        if (HasParticipated(festivalId, year))
        {
            return Invalid("festival.submission.already_participated", items);
        }

        if (items.Length != 3)
        {
            return Invalid("festival.submission.select_three", items);
        }

        if (items.Any(itemId =>
                !FestivalCatalog.IsEligibleExhibitItem(itemId)))
        {
            return Invalid("festival.submission.ineligible", items);
        }

        if (items.Select(DataCatalog.BaseItemId)
                .Distinct(StringComparer.Ordinal)
                .Count() != 3)
        {
            return Invalid("festival.submission.distinct_families", items);
        }

        var required = items
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(group.Key, group.Count()))
            .ToArray();
        if (required.Any(item => inventory.Count(item.ItemId) < item.Count))
        {
            return Invalid("festival.submission.items_changed", items);
        }

        var score = FestivalCatalog.Score(items) +
            FestivalCatalog.ReplayScoreBonus(festivalId, year, items);
        var award = FestivalCatalog.AwardForScore(score);
        return new FestivalSubmissionPreview(
            true,
            string.Empty,
            items,
            score,
            award.Id,
            FestivalCatalog.AuctionCoins(items),
            award.ScripReward
        );
    }

    public FestivalSubmissionResult Submit(
        string festivalId,
        int year,
        IReadOnlyList<string> itemIds,
        Inventory inventory
    )
    {
        var preview = CheckSubmission(festivalId, year, itemIds, inventory);
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(false, preview.FailureKey);
        }

        var removals = preview.ItemIds
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(group.Key, group.Count()))
            .ToArray();
        if (!inventory.TryRemoveMany(removals))
        {
            return new FestivalSubmissionResult(
                false,
                "festival.submission.items_changed"
            );
        }

        var result = new FestivalYearResultSave
        {
            FestivalId = festivalId,
            Year = Math.Max(1, year),
            ItemIds = preview.ItemIds.ToList(),
            Score = preview.Score,
            AwardId = preview.AwardId,
            AuctionCoins = preview.AuctionCoins,
            RuleVariantId = FestivalCatalog.ReplayRuleFor(
                festivalId,
                year
            ).Id
        };
        _results[(result.FestivalId, result.Year)] = result;
        Scrip += preview.ScripReward;
        Changed?.Invoke();
        return new FestivalSubmissionResult(
            true,
            "festival.submission.completed",
            Clone(result),
            preview.ScripReward
        );
    }

    public FestivalLongnightPreview CheckLongnightContribution(
        int year,
        IReadOnlyList<string>? dishItemIds,
        string exchangeId,
        Inventory inventory
    )
    {
        var normalizedYear = Math.Max(1, year);
        var dishes = dishItemIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray() ?? [];
        if (HasParticipated(
                FestivalCatalog.LongnightLanternFeastFestivalId,
                normalizedYear
            ))
        {
            return InvalidLongnight(
                "festival.longnight.activity.already_done",
                dishes
            );
        }

        if (dishes.Length != 2)
        {
            return InvalidLongnight(
                "festival.longnight.activity.select_two",
                dishes
            );
        }

        if (dishes.Distinct(StringComparer.Ordinal).Count() != 2)
        {
            return InvalidLongnight(
                "festival.longnight.activity.distinct_dishes",
                dishes
            );
        }

        if (dishes.Any(itemId =>
                !FestivalCatalog.LongnightDishScores.ContainsKey(itemId)))
        {
            return InvalidLongnight(
                "festival.longnight.activity.ineligible",
                dishes
            );
        }

        if (!FestivalCatalog.LongnightGiftExchanges.TryGetValue(
                exchangeId,
                out var exchange
            ))
        {
            return InvalidLongnight(
                "festival.longnight.activity.select_gift",
                dishes
            );
        }

        var removals = dishes
            .Append(exchange.GiftItemId)
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(
                group.Key,
                group.Count()
            ))
            .ToArray();
        if (removals.Any(removal =>
                inventory.Count(removal.ItemId) < removal.Count))
        {
            return InvalidLongnight(
                "festival.longnight.activity.items_changed",
                dishes,
                exchange
            );
        }

        if (!inventory.CanExchange(
                removals,
                exchange.RewardItemId,
                exchange.RewardCount
            ))
        {
            return InvalidLongnight(
                "festival.longnight.activity.backpack_full",
                dishes,
                exchange
            );
        }

        var score = dishes.Sum(itemId =>
            FestivalCatalog.LongnightDishScores[itemId]);
        score += FestivalCatalog.ReplayScoreBonus(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            normalizedYear,
            dishes
        );
        var award = FestivalCatalog.AwardForScore(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            score
        );
        return new FestivalLongnightPreview(
            true,
            string.Empty,
            dishes,
            exchange,
            score,
            award.Id,
            award.ScripReward
        );
    }

    public FestivalLongnightResult SubmitLongnightContribution(
        int year,
        IReadOnlyList<string> dishItemIds,
        string exchangeId,
        Inventory inventory
    )
    {
        var preview = CheckLongnightContribution(
            year,
            dishItemIds,
            exchangeId,
            inventory
        );
        if (!preview.CanComplete || preview.Exchange is null)
        {
            return new FestivalLongnightResult(
                false,
                preview.FailureKey
            );
        }

        var removals = preview.DishItemIds
            .Append(preview.Exchange.GiftItemId)
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(
                group.Key,
                group.Count()
            ))
            .ToArray();
        if (!inventory.TryExchange(
                removals,
                preview.Exchange.RewardItemId,
                preview.Exchange.RewardCount
            ))
        {
            return new FestivalLongnightResult(
                false,
                "festival.longnight.activity.items_changed"
            );
        }

        var result = new FestivalYearResultSave
        {
            FestivalId = FestivalCatalog.LongnightLanternFeastFestivalId,
            Year = Math.Max(1, year),
            ItemIds = preview.DishItemIds.ToList(),
            Score = preview.Score,
            AwardId = preview.AwardId,
            AuctionCoins = 0,
            GiftItemId = preview.Exchange.GiftItemId,
            GiftRewardItemId = preview.Exchange.RewardItemId,
            RitualId = FestivalCatalog.LongnightStarlightRiteId,
            RuleVariantId = FestivalCatalog.ReplayRuleFor(
                FestivalCatalog.LongnightLanternFeastFestivalId,
                year
            ).Id
        };
        _results[(result.FestivalId, result.Year)] = result;
        _currencyBalances[FestivalCatalog.LongnightLanternKnotId] =
            (int)Math.Min(
                int.MaxValue,
                (long)LanternKnots + preview.LanternKnotReward
            );
        Changed?.Invoke();
        return new FestivalLongnightResult(
            true,
            "festival.longnight.activity.completed",
            Clone(result),
            preview.LanternKnotReward
        );
    }

    public FestivalPurchaseCheck CheckLongnightPurchase(
        string offerId,
        Inventory inventory
    )
    {
        if (!FestivalCatalog.LongnightOffers.TryGetValue(
                offerId,
                out var offer
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.longnight.stall.unknown_offer",
                null
            );
        }

        if (LanternKnots < offer.ScripCost)
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.longnight.stall.insufficient_knots",
                offer
            );
        }

        return inventory.CanAdd(offer.ItemId, offer.Count)
            ? new FestivalPurchaseCheck(true, string.Empty, offer)
            : new FestivalPurchaseCheck(
                false,
                "festival.longnight.stall.backpack_full",
                offer
            );
    }

    public ActionResult PurchaseLongnightOffer(
        string offerId,
        Inventory inventory
    )
    {
        var check = CheckLongnightPurchase(offerId, inventory);
        if (!check.CanPurchase || check.Offer is null)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        if (!inventory.Add(check.Offer.ItemId, check.Offer.Count))
        {
            return ActionResult.Fail(
                "festival.longnight.stall.backpack_full"
            );
        }

        _currencyBalances[FestivalCatalog.LongnightLanternKnotId] =
            LanternKnots - check.Offer.ScripCost;
        Changed?.Invoke();
        return ActionResult.Grant(
            check.Offer.ItemId,
            check.Offer.Count,
            0,
            "festival.longnight.stall.completed"
        );
    }

    public FestivalSubmissionPreview CheckFireflyTideContribution(
        int year,
        IReadOnlyList<string>? fishItemIds,
        Inventory inventory
    )
    {
        var fish = fishItemIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray() ?? [];
        if (HasParticipated(
                FestivalCatalog.FireflyTideFestivalId,
                Math.Max(1, year)
            ))
        {
            return Invalid("festival.firefly.activity.already_done", fish);
        }

        if (fish.Length != 3)
        {
            return Invalid("festival.firefly.activity.select_three", fish);
        }

        if (fish.Distinct(StringComparer.Ordinal).Count() != 3)
        {
            return Invalid(
                "festival.firefly.activity.distinct_fish",
                fish
            );
        }

        if (fish.Any(itemId =>
                !FestivalCatalog.IsEligibleFireflyFish(itemId)))
        {
            return Invalid("festival.firefly.activity.ineligible", fish);
        }

        var removals = fish
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(
                group.Key,
                group.Count()
            ))
            .ToArray();
        if (removals.Any(removal =>
                inventory.Count(removal.ItemId) < removal.Count))
        {
            return Invalid(
                "festival.firefly.activity.items_changed",
                fish
            );
        }

        var score = FestivalCatalog.FireflyTideScore(fish) +
            FestivalCatalog.ReplayScoreBonus(
                FestivalCatalog.FireflyTideFestivalId,
                year,
                fish
            );
        var award = FestivalCatalog.AwardForScore(
            FestivalCatalog.FireflyTideFestivalId,
            score
        );
        return new FestivalSubmissionPreview(
            true,
            string.Empty,
            fish,
            score,
            award.Id,
            0,
            award.ScripReward
        );
    }

    public FestivalSubmissionResult SubmitFireflyTideContribution(
        int year,
        IReadOnlyList<string> fishItemIds,
        Inventory inventory
    )
    {
        var preview = CheckFireflyTideContribution(
            year,
            fishItemIds,
            inventory
        );
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(
                false,
                preview.FailureKey
            );
        }

        var removals = preview.ItemIds
            .GroupBy(itemId => itemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(
                group.Key,
                group.Count()
            ))
            .ToArray();
        if (!inventory.TryRemoveMany(removals))
        {
            return new FestivalSubmissionResult(
                false,
                "festival.firefly.activity.items_changed"
            );
        }

        var result = new FestivalYearResultSave
        {
            FestivalId = FestivalCatalog.FireflyTideFestivalId,
            Year = Math.Max(1, year),
            ItemIds = preview.ItemIds.ToList(),
            Score = preview.Score,
            AwardId = preview.AwardId,
            AuctionCoins = 0,
            RuleVariantId = FestivalCatalog.ReplayRuleFor(
                FestivalCatalog.FireflyTideFestivalId,
                year
            ).Id
        };
        _results[(result.FestivalId, result.Year)] = result;
        _currencyBalances[FestivalCatalog.FireflyGlowmarkId] =
            (int)Math.Min(
                int.MaxValue,
                (long)Glowmarks + preview.ScripReward
            );
        Changed?.Invoke();
        return new FestivalSubmissionResult(
            true,
            "festival.firefly.activity.completed",
            Clone(result),
            preview.ScripReward
        );
    }

    public FestivalPurchaseCheck CheckFireflyPurchase(
        string offerId,
        Inventory inventory
    )
    {
        if (!FestivalCatalog.FireflyOffers.TryGetValue(
                offerId,
                out var offer
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.firefly.shop.unknown_offer",
                null
            );
        }

        if (Glowmarks < offer.ScripCost)
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.firefly.shop.insufficient_glowmarks",
                offer
            );
        }

        return inventory.CanAdd(offer.ItemId, offer.Count)
            ? new FestivalPurchaseCheck(true, string.Empty, offer)
            : new FestivalPurchaseCheck(
                false,
                "festival.firefly.shop.backpack_full",
                offer
            );
    }

    public ActionResult PurchaseFireflyOffer(
        string offerId,
        Inventory inventory
    )
    {
        var check = CheckFireflyPurchase(offerId, inventory);
        if (!check.CanPurchase || check.Offer is null)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        if (!inventory.Add(check.Offer.ItemId, check.Offer.Count))
        {
            return ActionResult.Fail(
                "festival.firefly.shop.backpack_full"
            );
        }

        _currencyBalances[FestivalCatalog.FireflyGlowmarkId] =
            Glowmarks - check.Offer.ScripCost;
        Changed?.Invoke();
        return ActionResult.Grant(
            check.Offer.ItemId,
            check.Offer.Count,
            0,
            "festival.firefly.shop.completed"
        );
    }

    public FestivalPlantingStartCheck CheckStartPlantingChallenge(
        int year,
        int minuteOfDay,
        IReadOnlyList<string>? selectedSeedItemIds
    )
    {
        var normalizedYear = Math.Max(1, year);
        var selected = selectedSeedItemIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray() ?? [];
        if (HasParticipated(
                FestivalCatalog.GleamrisePlantingFestivalId,
                normalizedYear
            ))
        {
            return new FestivalPlantingStartCheck(
                false,
                "festival.sowing.already_completed",
                selected
            );
        }

        var existing = PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            normalizedYear
        );
        if (existing is { Plantings.Count: > 0 })
        {
            return new FestivalPlantingStartCheck(
                false,
                "festival.sowing.already_started",
                selected
            );
        }

        if (minuteOfDay > FestivalCatalog.GleamriseChallengeLatestStartMinute)
        {
            return new FestivalPlantingStartCheck(
                false,
                "festival.sowing.challenge_closed",
                selected
            );
        }

        if (selected.Length != 3 ||
            selected.Distinct(StringComparer.Ordinal).Count() != 3)
        {
            return new FestivalPlantingStartCheck(
                false,
                "festival.sowing.select_three",
                selected
            );
        }

        if (selected.Any(seedId =>
                !FestivalCatalog.GleamriseChallengeSeedIds.Contains(
                    seedId,
                    StringComparer.Ordinal
                )))
        {
            return new FestivalPlantingStartCheck(
                false,
                "festival.sowing.unknown_seed",
                selected
            );
        }

        return new FestivalPlantingStartCheck(true, string.Empty, selected);
    }

    public ActionResult StartPlantingChallenge(
        int year,
        int minuteOfDay,
        IReadOnlyList<string> selectedSeedItemIds
    )
    {
        var check = CheckStartPlantingChallenge(
            year,
            minuteOfDay,
            selectedSeedItemIds
        );
        if (!check.CanStart)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        var normalizedYear = Math.Max(1, year);
        var attempt = new FestivalPlantingAttemptSave
        {
            FestivalId = FestivalCatalog.GleamrisePlantingFestivalId,
            Year = normalizedYear,
            StartedMinute = minuteOfDay,
            SelectedSeedItemIds = check.SelectedSeedItemIds.ToList(),
            ActiveSeedItemId = check.SelectedSeedItemIds[0]
        };
        _plantingAttempts[(attempt.FestivalId, attempt.Year)] = attempt;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "festival.sowing.challenge_started"
        );
    }

    public ActionResult CheckSelectPlantingSeed(int year, string seedItemId)
    {
        var attempt = PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        if (attempt is null)
        {
            return ActionResult.Fail("festival.sowing.start_challenge");
        }

        return attempt.SelectedSeedItemIds.Contains(
            seedItemId,
            StringComparer.Ordinal
        )
            ? ActionResult.Success(
                messageKey: "festival.sowing.seed_selected"
            )
            : ActionResult.Fail("festival.sowing.unknown_seed");
    }

    public ActionResult SelectPlantingSeed(int year, string seedItemId)
    {
        var check = CheckSelectPlantingSeed(year, seedItemId);
        if (!check.Succeeded)
        {
            return check;
        }

        var key = (
            FestivalCatalog.GleamrisePlantingFestivalId,
            Math.Max(1, year)
        );
        _plantingAttempts[key].ActiveSeedItemId = seedItemId;
        Changed?.Invoke();
        return check;
    }

    public FestivalPlantingCheck CheckPlantingPlot(
        int year,
        int minuteOfDay,
        string plotId
    )
    {
        var attempt = PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        if (attempt is null)
        {
            return InvalidPlanting(
                "festival.sowing.start_challenge",
                plotId
            );
        }

        if (!GleamrisePlantingFestivalLayout.PlotCellsById.ContainsKey(
                plotId
            ))
        {
            return InvalidPlanting("festival.sowing.unknown_plot", plotId);
        }

        var deadline = Math.Min(
            attempt.StartedMinute +
                FestivalCatalog.GleamriseChallengeDurationMinutes,
            FestivalCatalog.GleamrisePlanting.CloseMinute
        );
        if (minuteOfDay >= deadline)
        {
            return InvalidPlanting(
                "festival.sowing.challenge_closed",
                plotId,
                attempt.ActiveSeedItemId
            );
        }

        if (attempt.Plantings.Any(planting =>
                planting.PlotId == plotId))
        {
            return InvalidPlanting(
                "festival.sowing.plot_already_planted",
                plotId,
                attempt.ActiveSeedItemId
            );
        }

        if (!attempt.SelectedSeedItemIds.Contains(
                attempt.ActiveSeedItemId,
                StringComparer.Ordinal
            ))
        {
            return InvalidPlanting(
                "festival.sowing.select_seed",
                plotId
            );
        }

        if (attempt.Plantings.Count(planting =>
                planting.SeedItemId == attempt.ActiveSeedItemId) >= 4)
        {
            return InvalidPlanting(
                "festival.sowing.seed_depleted",
                plotId,
                attempt.ActiveSeedItemId
            );
        }

        return new FestivalPlantingCheck(
            true,
            string.Empty,
            plotId,
            attempt.ActiveSeedItemId
        );
    }

    public FestivalPlantingResolution PlantPlot(
        int year,
        int minuteOfDay,
        string plotId
    )
    {
        var check = CheckPlantingPlot(year, minuteOfDay, plotId);
        if (!check.CanPlant)
        {
            return new FestivalPlantingResolution(
                false,
                false,
                check.FailureKey
            );
        }

        var key = (
            FestivalCatalog.GleamrisePlantingFestivalId,
            Math.Max(1, year)
        );
        var attempt = _plantingAttempts[key];
        attempt.Plantings.Add(new FestivalPlotPlantingSave
        {
            PlotId = plotId,
            SeedItemId = check.SeedItemId
        });
        if (attempt.Plantings.Count <
            GleamrisePlantingFestivalLayout.PlotIds.Count)
        {
            Changed?.Invoke();
            return new FestivalPlantingResolution(
                true,
                false,
                "festival.sowing.plot_planted"
            );
        }

        return CompletePlantingAttempt(attempt, minuteOfDay);
    }

    public FestivalPlantingResolution ResolvePlantingAttempt(
        int year,
        int minuteOfDay,
        bool force = false
    )
    {
        var key = (
            FestivalCatalog.GleamrisePlantingFestivalId,
            Math.Max(1, year)
        );
        if (!_plantingAttempts.TryGetValue(key, out var attempt))
        {
            return new FestivalPlantingResolution(
                false,
                false,
                "festival.sowing.no_active_challenge"
            );
        }

        var deadline = Math.Min(
            attempt.StartedMinute +
                FestivalCatalog.GleamriseChallengeDurationMinutes,
            FestivalCatalog.GleamrisePlanting.CloseMinute
        );
        if (!force && minuteOfDay < deadline)
        {
            return new FestivalPlantingResolution(
                false,
                false,
                "festival.sowing.challenge_active"
            );
        }

        if (attempt.Plantings.Count == 0)
        {
            _plantingAttempts.Remove(key);
            Changed?.Invoke();
            return new FestivalPlantingResolution(
                true,
                false,
                "festival.sowing.no_result"
            );
        }

        return CompletePlantingAttempt(attempt, minuteOfDay);
    }

    public int PlantingMinutesRemaining(int year, int minuteOfDay)
    {
        var attempt = PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        if (attempt is null)
        {
            return 0;
        }

        var deadline = Math.Min(
            attempt.StartedMinute +
                FestivalCatalog.GleamriseChallengeDurationMinutes,
            FestivalCatalog.GleamrisePlanting.CloseMinute
        );
        return Math.Max(0, deadline - minuteOfDay);
    }

    public int CurrentPlantingScore(int year, int minuteOfDay)
    {
        var attempt = PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        if (attempt is null)
        {
            return ResultFor(
                FestivalCatalog.GleamrisePlantingFestivalId,
                year
            )?.Score ?? 0;
        }

        return FestivalCatalog.GleamrisePlantingScore(
            attempt.Plantings,
            Math.Max(0, minuteOfDay - attempt.StartedMinute)
        );
    }

    public FestivalPurchaseCheck CheckPurchase(
        string offerId,
        Inventory inventory
    )
    {
        if (!FestivalCatalog.StarharvestOffers.TryGetValue(
                offerId,
                out var offer
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.shop.unknown_offer",
                null
            );
        }

        if (Scrip < offer.ScripCost)
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.shop.not_enough_scrip",
                offer
            );
        }

        if (!inventory.CanAdd(offer.ItemId, offer.Count))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.shop.backpack_full",
                offer
            );
        }

        return new FestivalPurchaseCheck(true, string.Empty, offer);
    }

    public ActionResult Purchase(string offerId, Inventory inventory)
    {
        var check = CheckPurchase(offerId, inventory);
        if (!check.CanPurchase || check.Offer is null)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        if (!inventory.Add(check.Offer.ItemId, check.Offer.Count))
        {
            return ActionResult.Fail("festival.shop.backpack_full");
        }

        Scrip -= check.Offer.ScripCost;
        Changed?.Invoke();
        return ActionResult.Grant(
            check.Offer.ItemId,
            check.Offer.Count,
            0,
            "festival.shop.purchased"
        );
    }

    public FestivalPurchaseCheck CheckGleamrisePurchase(
        string offerId,
        Inventory inventory
    )
    {
        if (!FestivalCatalog.GleamriseOffers.TryGetValue(
                offerId,
                out var offer
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.gleamrise.exchange.unknown_offer",
                null
            );
        }

        if (BloomTokens < offer.ScripCost)
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.gleamrise.exchange.not_enough_tokens",
                offer
            );
        }

        if (!inventory.CanAdd(offer.ItemId, offer.Count))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.gleamrise.exchange.backpack_full",
                offer
            );
        }

        return new FestivalPurchaseCheck(true, string.Empty, offer);
    }

    public ActionResult PurchaseGleamriseSeeds(
        string offerId,
        Inventory inventory
    )
    {
        var check = CheckGleamrisePurchase(offerId, inventory);
        if (!check.CanPurchase || check.Offer is null)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        if (!inventory.Add(check.Offer.ItemId, check.Offer.Count))
        {
            return ActionResult.Fail(
                "festival.gleamrise.exchange.backpack_full"
            );
        }

        _currencyBalances[FestivalCatalog.GleamriseBloomTokenId] =
            BloomTokens - check.Offer.ScripCost;
        Changed?.Invoke();
        return ActionResult.Grant(
            check.Offer.ItemId,
            check.Offer.Count,
            0,
            "festival.gleamrise.exchange.completed"
        );
    }

    public FestivalSave Capture() => new()
    {
        Scrip = Scrip,
        Results = Results.Select(Clone).ToList(),
        PlantingAttempts = _plantingAttempts.Values
            .OrderBy(attempt => attempt.Year)
            .ThenBy(attempt => attempt.FestivalId, StringComparer.Ordinal)
            .Select(Clone)
            .ToList(),
        CurrencyBalances = _currencyBalances
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new FestivalCurrencySave
            {
                CurrencyId = entry.Key,
                Balance = entry.Value
            })
            .ToList()
    };

    public static FestivalSave NormalizeSave(FestivalSave? save)
    {
        var results = new Dictionary<(string FestivalId, int Year),
            FestivalYearResultSave>();
        foreach (var saved in save?.Results ?? [])
        {
            if (!FestivalCatalog.Festivals.ContainsKey(saved.FestivalId) ||
                saved.Year < 1)
            {
                continue;
            }

            var isGleamrise = saved.FestivalId ==
                FestivalCatalog.GleamrisePlantingFestivalId;
            var isLongnight = saved.FestivalId ==
                FestivalCatalog.LongnightLanternFeastFestivalId;
            var isFirefly = saved.FestivalId ==
                FestivalCatalog.FireflyTideFestivalId;
            var plantings = isGleamrise
                ? NormalizePlantings(saved.Plantings, null)
                : [];
            var itemIds = isGleamrise
                ? plantings.Select(entry => entry.SeedItemId).ToList()
                : (saved.ItemIds ?? [])
                    .Where(DataCatalog.Items.ContainsKey)
                    .ToList();
            FestivalGiftExchangeDefinition? giftExchange = null;
            if (isLongnight)
            {
                itemIds = itemIds
                    .Where(FestivalCatalog.LongnightDishScores.ContainsKey)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .Take(2)
                    .ToList();
                giftExchange = FestivalCatalog.LongnightGiftExchanges.Values
                    .FirstOrDefault(exchange =>
                        exchange.GiftItemId == saved.GiftItemId &&
                        exchange.RewardItemId == saved.GiftRewardItemId);
                if (itemIds.Count != 2 || giftExchange is null ||
                    saved.RitualId !=
                        FestivalCatalog.LongnightStarlightRiteId)
                {
                    continue;
                }
            }
            else if (isFirefly)
            {
                itemIds = itemIds
                    .Where(FestivalCatalog.IsEligibleFireflyFish)
                    .Distinct(StringComparer.Ordinal)
                    .Take(3)
                    .ToList();
                if (itemIds.Count != 3)
                {
                    continue;
                }
            }
            else if (!isGleamrise)
            {
                itemIds = itemIds
                    .Take(FestivalCatalog.ResultItemLimit(saved.FestivalId))
                    .ToList();
            }

            var ruleVariantId = FestivalCatalog.ReplayRules.ContainsKey(
                saved.RuleVariantId
            )
                ? saved.RuleVariantId
                : FestivalCatalog.ClassicRuleId;
            var score = Math.Max(0, saved.Score);
            if (isGleamrise)
            {
                score = Math.Clamp(saved.Score, 0, 34);
            }
            else if (isLongnight)
            {
                score = itemIds.Sum(id =>
                    FestivalCatalog.LongnightDishScores[id]);
                if (ruleVariantId != FestivalCatalog.ClassicRuleId)
                {
                    score += FestivalCatalog.ReplayScoreBonus(
                        saved.FestivalId,
                        saved.Year,
                        itemIds
                    );
                }
            }
            else if (isFirefly)
            {
                score = FestivalCatalog.FireflyTideScore(itemIds);
                if (ruleVariantId != FestivalCatalog.ClassicRuleId)
                {
                    score += FestivalCatalog.ReplayScoreBonus(
                        saved.FestivalId,
                        saved.Year,
                        itemIds
                    );
                }
            }
            var rewardChoiceId = string.Empty;
            if (FestivalCatalog.RewardChoices.TryGetValue(
                    saved.RewardChoiceId,
                    out var savedChoice
                ) && savedChoice.FestivalId == saved.FestivalId)
            {
                rewardChoiceId = savedChoice.Id;
            }
            var validAwards = FestivalCatalog.AwardsFor(saved.FestivalId);
            var awardId = !isLongnight && validAwards.Any(award =>
                award.Id == saved.AwardId)
                    ? saved.AwardId
                    : FestivalCatalog.AwardForScore(
                        saved.FestivalId,
                        score
                    ).Id;
            var normalized = new FestivalYearResultSave
            {
                FestivalId = saved.FestivalId,
                Year = saved.Year,
                ItemIds = itemIds,
                Score = score,
                AwardId = awardId,
                AuctionCoins = isGleamrise || isLongnight || isFirefly
                    ? 0
                    : Math.Max(0, saved.AuctionCoins),
                Plantings = plantings,
                GiftItemId = giftExchange?.GiftItemId ?? string.Empty,
                GiftRewardItemId = giftExchange?.RewardItemId ?? string.Empty,
                RitualId = isLongnight
                    ? FestivalCatalog.LongnightStarlightRiteId
                    : string.Empty,
                RuleVariantId = ruleVariantId,
                RewardChoiceId = rewardChoiceId,
                RewardClaimed = saved.RewardClaimed &&
                    !string.IsNullOrEmpty(rewardChoiceId)
            };
            var key = (normalized.FestivalId, normalized.Year);
            if (!results.TryGetValue(key, out var existing) ||
                Compare(normalized, existing) > 0)
            {
                results[key] = normalized;
            }
        }

        var attempts = new Dictionary<(string FestivalId, int Year),
            FestivalPlantingAttemptSave>();
        foreach (var saved in save?.PlantingAttempts ?? [])
        {
            if (saved.FestivalId !=
                    FestivalCatalog.GleamrisePlantingFestivalId ||
                saved.Year < 1 ||
                results.ContainsKey((saved.FestivalId, saved.Year)))
            {
                continue;
            }

            var selected = (saved.SelectedSeedItemIds ?? [])
                .Where(seedId =>
                    FestivalCatalog.GleamriseChallengeSeedIds.Contains(
                        seedId,
                        StringComparer.Ordinal
                    ))
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .ToList();
            if (selected.Count != 3)
            {
                continue;
            }

            var normalized = new FestivalPlantingAttemptSave
            {
                FestivalId = saved.FestivalId,
                Year = saved.Year,
                StartedMinute = Math.Clamp(
                    saved.StartedMinute,
                    FestivalCatalog.GleamrisePlanting.OpenMinute,
                    FestivalCatalog.GleamriseChallengeLatestStartMinute
                ),
                SelectedSeedItemIds = selected,
                ActiveSeedItemId = selected.Contains(
                    saved.ActiveSeedItemId,
                    StringComparer.Ordinal
                )
                    ? saved.ActiveSeedItemId
                    : selected[0],
                Plantings = NormalizePlantings(saved.Plantings, selected)
            };
            var key = (normalized.FestivalId, normalized.Year);
            if (!attempts.TryGetValue(key, out var existing) ||
                CompareAttempts(normalized, existing) > 0)
            {
                attempts[key] = normalized;
            }
        }

        var currencies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var saved in save?.CurrencyBalances ?? [])
        {
            if (saved.CurrencyId != FestivalCatalog.GleamriseBloomTokenId &&
                saved.CurrencyId != FestivalCatalog.LongnightLanternKnotId &&
                saved.CurrencyId != FestivalCatalog.FireflyGlowmarkId)
            {
                continue;
            }

            currencies[saved.CurrencyId] = Math.Max(
                currencies.GetValueOrDefault(saved.CurrencyId),
                Math.Max(0, saved.Balance)
            );
        }

        return new FestivalSave
        {
            Scrip = Math.Max(0, save?.Scrip ?? 0),
            Results = results.Values
                .OrderBy(result => result.Year)
                .ThenBy(result => result.FestivalId, StringComparer.Ordinal)
                .Select(Clone)
                .ToList(),
            PlantingAttempts = attempts.Values
                .OrderBy(attempt => attempt.Year)
                .ThenBy(attempt => attempt.FestivalId, StringComparer.Ordinal)
                .Select(Clone)
                .ToList(),
            CurrencyBalances = currencies
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new FestivalCurrencySave
                {
                    CurrencyId = entry.Key,
                    Balance = entry.Value
                })
                .ToList()
        };
    }

    private static FestivalSubmissionPreview Invalid(
        string failureKey,
        IReadOnlyList<string> itemIds
    ) => new(false, failureKey, itemIds, 0, string.Empty, 0, 0);

    private static FestivalLongnightPreview InvalidLongnight(
        string failureKey,
        IReadOnlyList<string> dishItemIds,
        FestivalGiftExchangeDefinition? exchange = null
    ) => new(
        false,
        failureKey,
        dishItemIds,
        exchange,
        0,
        string.Empty,
        0
    );

    private static FestivalPlantingCheck InvalidPlanting(
        string failureKey,
        string plotId,
        string seedItemId = ""
    ) => new(false, failureKey, plotId, seedItemId);

    private FestivalPlantingResolution CompletePlantingAttempt(
        FestivalPlantingAttemptSave attempt,
        int minuteOfDay
    )
    {
        var elapsed = Math.Max(0, minuteOfDay - attempt.StartedMinute);
        var orderedPlantings = NormalizePlantings(
            attempt.Plantings,
            attempt.SelectedSeedItemIds
        );
        var score = FestivalCatalog.GleamrisePlantingScore(
            orderedPlantings,
            elapsed
        );
        score += FestivalCatalog.ReplayScoreBonus(
            FestivalCatalog.GleamrisePlantingFestivalId,
            attempt.Year,
            orderedPlantings
                .Select(planting => planting.SeedItemId)
                .ToArray()
        );
        var award = FestivalCatalog.AwardForScore(
            FestivalCatalog.GleamrisePlantingFestivalId,
            score
        );
        var result = new FestivalYearResultSave
        {
            FestivalId = FestivalCatalog.GleamrisePlantingFestivalId,
            Year = attempt.Year,
            ItemIds = orderedPlantings
                .Select(entry => entry.SeedItemId)
                .ToList(),
            Score = score,
            AwardId = award.Id,
            AuctionCoins = 0,
            Plantings = orderedPlantings,
            RuleVariantId = FestivalCatalog.ReplayRuleFor(
                FestivalCatalog.GleamrisePlantingFestivalId,
                attempt.Year
            ).Id
        };
        var key = (result.FestivalId, result.Year);
        _results[key] = result;
        _plantingAttempts.Remove(key);
        _currencyBalances[FestivalCatalog.GleamriseBloomTokenId] =
            BloomTokens + award.ScripReward;
        Changed?.Invoke();
        return new FestivalPlantingResolution(
            true,
            true,
            "festival.sowing.challenge_completed",
            Clone(result),
            award.ScripReward
        );
    }

    private static List<FestivalPlotPlantingSave> NormalizePlantings(
        IEnumerable<FestivalPlotPlantingSave>? values,
        IReadOnlyList<string>? selectedSeedItemIds
    )
    {
        var allowedSeeds = selectedSeedItemIds is { Count: > 0 }
            ? selectedSeedItemIds
            : FestivalCatalog.GleamriseChallengeSeedIds;
        var byPlot = new Dictionary<string, FestivalPlotPlantingSave>(
            StringComparer.Ordinal
        );
        var seedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var value in values ?? [])
        {
            if (!GleamrisePlantingFestivalLayout.PlotCellsById.ContainsKey(
                    value.PlotId
                ) || byPlot.ContainsKey(value.PlotId) ||
                !allowedSeeds.Contains(
                    value.SeedItemId,
                    StringComparer.Ordinal
                ) || seedCounts.GetValueOrDefault(value.SeedItemId) >= 4)
            {
                continue;
            }

            byPlot[value.PlotId] = new FestivalPlotPlantingSave
            {
                PlotId = value.PlotId,
                SeedItemId = value.SeedItemId
            };
            seedCounts[value.SeedItemId] =
                seedCounts.GetValueOrDefault(value.SeedItemId) + 1;
        }

        return GleamrisePlantingFestivalLayout.PlotIds
            .Where(byPlot.ContainsKey)
            .Select(plotId => byPlot[plotId])
            .ToList();
    }

    private static int CompareAttempts(
        FestivalPlantingAttemptSave left,
        FestivalPlantingAttemptSave right
    )
    {
        var planted = left.Plantings.Count.CompareTo(right.Plantings.Count);
        if (planted != 0)
        {
            return planted;
        }

        var started = right.StartedMinute.CompareTo(left.StartedMinute);
        if (started != 0)
        {
            return started;
        }

        return string.CompareOrdinal(
            string.Join('\u001f', left.SelectedSeedItemIds),
            string.Join('\u001f', right.SelectedSeedItemIds)
        );
    }

    private static int Compare(
        FestivalYearResultSave left,
        FestivalYearResultSave right
    )
    {
        var score = left.Score.CompareTo(right.Score);
        if (score != 0)
        {
            return score;
        }

        var coins = left.AuctionCoins.CompareTo(right.AuctionCoins);
        if (coins != 0)
        {
            return coins;
        }

        var ritual = string.CompareOrdinal(left.RitualId, right.RitualId);
        if (ritual != 0)
        {
            return ritual;
        }

        var gift = string.CompareOrdinal(left.GiftItemId, right.GiftItemId);
        if (gift != 0)
        {
            return gift;
        }

        return string.CompareOrdinal(
            string.Join('\u001f', left.ItemIds),
            string.Join('\u001f', right.ItemIds)
        );
    }

    private static FestivalYearResultSave Clone(FestivalYearResultSave value) =>
        new()
        {
            FestivalId = value.FestivalId,
            Year = value.Year,
            ItemIds = value.ItemIds.ToList(),
            Score = value.Score,
            AwardId = value.AwardId,
            AuctionCoins = value.AuctionCoins,
            Plantings = value.Plantings.Select(Clone).ToList(),
            GiftItemId = value.GiftItemId,
            GiftRewardItemId = value.GiftRewardItemId,
            RitualId = value.RitualId,
            RuleVariantId = value.RuleVariantId,
            RewardChoiceId = value.RewardChoiceId,
            RewardClaimed = value.RewardClaimed
        };

    private static FestivalPlantingAttemptSave Clone(
        FestivalPlantingAttemptSave value
    ) => new()
    {
        FestivalId = value.FestivalId,
        Year = value.Year,
        StartedMinute = value.StartedMinute,
        SelectedSeedItemIds = value.SelectedSeedItemIds.ToList(),
        ActiveSeedItemId = value.ActiveSeedItemId,
        Plantings = value.Plantings.Select(Clone).ToList()
    };

    private static FestivalPlotPlantingSave Clone(
        FestivalPlotPlantingSave value
    ) => new()
    {
        PlotId = value.PlotId,
        SeedItemId = value.SeedItemId
    };
}
