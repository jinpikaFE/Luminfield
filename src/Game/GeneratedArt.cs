using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class GeneratedArt
{
    private static readonly Texture2D Characters =
        GD.Load<Texture2D>("res://assets/generated/characters/player/character_directions_chroma.png");

    private static readonly Texture2D PlayerWalkCycle =
        GD.Load<Texture2D>("res://assets/generated/characters/player/player_walk_cycle_chroma.png");

    private static readonly Texture2D EconomyAssets =
        GD.Load<Texture2D>("res://assets/generated/features/core-loop/economy_assets_chroma.png");

    private static readonly Texture2D ProcessorMachines =
        GD.Load<Texture2D>("res://assets/generated/features/processors/processor_machines.png");

    private static readonly Texture2D MoonpearlEggPress =
        GD.Load<Texture2D>("res://assets/generated/features/processors/moonpearl_egg_press.png");

    private static readonly Texture2D PhaseAAssets =
        GD.Load<Texture2D>("res://assets/generated/features/core-loop/phase_a_systems.png");

    private static readonly Texture2D LongnightSnowWeather =
        GD.Load<Texture2D>(
            "res://assets/generated/world/weather/longnight_snow_weather.png"
        );

    private static readonly Rect2[] LongnightSnowflakeRegions =
    [
        new Rect2(172, 263, 72, 76),
        new Rect2(485, 236, 114, 129),
        new Rect2(793, 171, 230, 262),
        new Rect2(1154, 116, 303, 352)
    ];

    private static readonly Rect2[] LongnightSnowGustRegions =
    [
        new Rect2(81, 641, 265, 152),
        new Rect2(428, 651, 271, 155),
        new Rect2(781, 659, 280, 154)
    ];

    private static readonly Rect2 LongnightSnowIconRegion =
        new(1172, 610, 266, 262);

    private static readonly Texture2D CropExpansion =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/crop_expansion.png");

    private static readonly Texture2D GleamriseCrops =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/gleamrise_crops.png");

    private static readonly Texture2D GleamriseResonance =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/gleamrise_resonance.png");

    private static readonly Texture2D RainveilCrops =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/rainveil_crops.png");

    private static readonly Texture2D StarharvestCrops =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/starharvest_crops.png");

    private static readonly Texture2D StarwovenChest =
        GD.Load<Texture2D>("res://assets/generated/features/storage/starwoven_chest.png");

    private static readonly Texture2D FarmPlaceables =
        GD.Load<Texture2D>("res://assets/generated/farming/placeables/farm_placeables.png");

    private static readonly Texture2D CropQualityFertilizer =
        GD.Load<Texture2D>(
            "res://assets/generated/farming/crops/crop_quality_fertilizer.png"
        );

    private static readonly Texture2D VillageLandmarks =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/village_landmarks.png"
        );

    private static readonly Texture2D TwilightEmporiumExterior =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/twilight_emporium_exterior.png"
        );

    private static readonly Texture2D StarlightPostExterior =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/starlight_post_exterior.png"
        );

    private static readonly Texture2D StarfallWatchExterior =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/starfall_watch_exterior.png"
        );

    private static readonly Texture2D VillageNpcs =
        GD.Load<Texture2D>(
            "res://assets/generated/characters/npcs/village_npcs.png"
        );

    private static readonly Texture2D VillageNpcsExpansion =
        GD.Load<Texture2D>(
            "res://assets/generated/characters/npcs/village_npcs_expansion.png"
        );

    private static readonly Texture2D VillageNpcsWave2 =
        GD.Load<Texture2D>(
            "res://assets/generated/characters/npcs/village_npcs_wave_2.png"
        );

    private static readonly Texture2D VillageNpcsWave3 =
        GD.Load<Texture2D>(
            "res://assets/generated/characters/npcs/village_npcs_wave_3.png"
        );

    private static readonly Texture2D RelationshipGifts =
        GD.Load<Texture2D>(
            "res://assets/generated/features/relationships/relationship_gifts.png"
        );

    private static readonly Texture2D DailyCommissionBoard =
        GD.Load<Texture2D>("res://assets/generated/features/commissions/daily_commission_board.png");

    private static readonly Texture2D WoodlandStarlightPedestal =
        GD.Load<Texture2D>(
            "res://assets/generated/features/starlights/woodland_starlight_pedestal.png"
        );

    private static readonly Texture2D StarlightMailbox =
        GD.Load<Texture2D>(
            "res://assets/generated/features/mail/starlight_mailbox.png"
        );

    private static readonly Texture2D OrchardHives =
        GD.Load<Texture2D>("res://assets/generated/farming/orchard/orchard_hives.png");

    private static readonly IReadOnlyDictionary<string, int> CropExpansionRows =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [DataCatalog.CloudleafId] = 0,
            [DataCatalog.GlowpeaId] = 1,
            [DataCatalog.EmberbellId] = 2,
            [DataCatalog.PrismcornId] = 3,
            [DataCatalog.DewmelonId] = 4,
            [DataCatalog.DuskbellId] = 5
        };

    private static readonly IReadOnlyDictionary<string, int> GleamriseCropRows =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [DataCatalog.DawnlaceId] = 0,
            [DataCatalog.GlimmerpodId] = 1,
            [DataCatalog.MistsongMintId] = 2,
            [DataCatalog.CometTuberId] = 3
        };

    private static readonly IReadOnlyDictionary<string, int>
        GleamriseResonanceColumns =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [DataCatalog.RainwovenDawnlaceId] = 0,
                [DataCatalog.StarwindGlimmerpodId] = 1
            };

    private static readonly IReadOnlyDictionary<string, int> RainveilCropRows =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [DataCatalog.RipplecapId] = 0,
            [DataCatalog.TideglassTaroId] = 1,
            [DataCatalog.LanternReedId] = 2,
            [DataCatalog.RainveilLotusId] = 3
        };

    private static readonly IReadOnlyDictionary<string, int>
        StarharvestCropRows =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [DataCatalog.AuricShootId] = 0,
                [DataCatalog.SunvaultGourdId] = 1,
                [DataCatalog.CrownstarSaffronId] = 2,
                [DataCatalog.AmberthreadClusterId] = 3
            };

    private static readonly Rect2 StarbudPreserveRegion = new(185, 125, 275, 330);
    private static readonly Rect2 MoonrootTonicRegion = new(805, 75, 220, 420);
    private static readonly Rect2 MarketStallRegion = new(55, 630, 515, 565);
    private static readonly Rect2 MoonwellInfuserRegion = new(665, 615, 505, 535);
    private static readonly Rect2[] PhaseAIconRegions =
    [
        new Rect2(70, 125, 300, 350),
        new Rect2(430, 155, 310, 290),
        new Rect2(815, 150, 320, 310),
        new Rect2(1180, 130, 320, 350),
        new Rect2(770, 555, 350, 320),
        new Rect2(1145, 555, 360, 330)
    ];
    private static readonly Rect2 ClosedShippingBinRegion =
        new(55, 535, 315, 350);
    private static readonly Rect2 OpenShippingBinRegion =
        new(425, 530, 315, 365);
    private static readonly Rect2 StarwovenChestItemRegion =
        new(160, 181, 333, 333);
    private static readonly Rect2 StarwovenChestClosedRegion =
        new(652, 113, 427, 427);
    private static readonly Rect2 StarwovenChestOpenRegion =
        new(96, 653, 477, 477);
    private static readonly Rect2 StarwovenChestCraftRegion =
        new(680, 680, 444, 444);
    private static readonly IReadOnlyDictionary<string, Rect2> FarmObjectRegions =
        new Dictionary<string, Rect2>(StringComparer.Ordinal)
        {
            [DataCatalog.MoonstonePathId] = new(90, 146, 340, 323),
            [DataCatalog.StarwoodFenceId] = new(507, 192, 244, 277),
            [DataCatalog.StarlightTorchId] = new(933, 109, 90, 363),
            [DataCatalog.DewfallSprinklerId] = new(1185, 265, 222, 197)
        };
    private static readonly IReadOnlyDictionary<string, Rect2> FarmObjectIconRegions =
        new Dictionary<string, Rect2>(StringComparer.Ordinal)
        {
            [DataCatalog.MoonstonePathId] = new(106, 614, 280, 281),
            [DataCatalog.StarwoodFenceId] = new(458, 625, 270, 252),
            [DataCatalog.StarlightTorchId] = new(837, 617, 226, 278),
            [DataCatalog.DewfallSprinklerId] = new(1141, 627, 322, 267)
        };
    private static readonly Rect2 CommissionBoardClosedRegion =
        new(83, 139, 483, 488);
    private static readonly Rect2 CommissionBoardActiveRegion =
        new(671, 125, 503, 502);
    private static readonly Rect2 CommissionParchmentRegion =
        new(157, 627, 370, 494);
    private static readonly Rect2 CommissionRewardRegion =
        new(723, 627, 413, 491);
    private static readonly Rect2 WoodlandStarlightDormantRegion =
        new(90, 65, 520, 520);
    private static readonly Rect2 WoodlandStarlightActiveRegion =
        new(650, 65, 520, 520);
    private static readonly Rect2 StarlightNodeSealRegion =
        new(110, 655, 480, 480);
    private static readonly Rect2 WoodlandRenewalRegion =
        new(700, 635, 370, 540);
    private static readonly Rect2 StarlightMailboxClosedRegion =
        new(0, 0, 627, 627);
    private static readonly Rect2 StarlightMailboxUnreadRegion =
        new(627, 0, 627, 627);
    private static readonly Rect2 StarlightEnvelopeRegion =
        new(0, 627, 627, 627);
    private static readonly Rect2 RelationshipReplyRegion =
        new(627, 627, 627, 627);
    private static readonly Rect2 MoonplumSaplingMapRegion =
        new(56, 102, 328, 410);
    private static readonly Rect2 MoonplumTreeMapRegion =
        new(384, 25, 384, 487);
    private static readonly Rect2 GlowcombHiveMapRegion =
        new(768, 169, 384, 343);
    private static readonly Rect2 GlowcombHiveReadyMapRegion =
        new(1152, 97, 334, 415);
    private static readonly Rect2 MoonplumSaplingIconRegion =
        new(57, 512, 327, 438);
    private static readonly Rect2 MoonplumIconRegion =
        new(385, 512, 361, 421);
    private static readonly Rect2 StarhoneyIconRegion =
        new(806, 640, 304, 321);
    private static readonly Rect2 GlowcombHiveIconRegion =
        new(1152, 512, 319, 446);
    private static readonly Rect2 StarsoilFertilizerItemRegion =
        new(154, 126, 281, 324);
    private static readonly Rect2 FertilizedSoilRegion =
        new(597, 288, 278, 151);
    private static readonly Rect2 StarsoilFertilizerCraftRegion =
        new(1036, 94, 367, 362);
    private static readonly Rect2 RegularQualityRegion =
        new(181, 631, 184, 256);
    private static readonly Rect2 LuminousQualityRegion =
        new(594, 607, 262, 305);
    private static readonly Rect2 StarlightQualityRegion =
        new(1049, 563, 336, 362);
    private static readonly Rect2[] VillageLandmarkRegions =
    [
        new Rect2(52, 41, 366, 412),
        new Rect2(418, 109, 384, 343),
        new Rect2(849, 37, 405, 413),
        new Rect2(1254, 105, 372, 357),
        new Rect2(39, 516, 379, 338),
        new Rect2(418, 538, 418, 311),
        new Rect2(836, 584, 368, 262),
        new Rect2(1277, 604, 325, 245)
    ];
    private static readonly Rect2 TwilightEmporiumExteriorRegion =
        new(178, 94, 881, 975);
    private static readonly Rect2 StarlightPostExteriorRegion =
        new(158, 79, 952, 1033);
    private static readonly Rect2 StarfallWatchExteriorRegion =
        new(194, 49, 866, 1119);
    private static readonly Rect2[][] VillageNpcRegions =
    [
        [
            new Rect2(154, 10, 156, 292),
            new Rect2(565, 10, 156, 292),
            new Rect2(977, 9, 129, 295),
            new Rect2(1361, 10, 127, 295)
        ],
        [
            new Rect2(161, 318, 153, 309),
            new Rect2(560, 318, 150, 309),
            new Rect2(980, 319, 137, 308),
            new Rect2(1348, 320, 136, 307)
        ],
        [
            new Rect2(160, 627, 147, 290),
            new Rect2(565, 627, 144, 290),
            new Rect2(977, 627, 125, 293),
            new Rect2(1362, 627, 125, 293)
        ]
    ];
    private static readonly Rect2[][] VillageNpcExpansionRegions =
    [
        [
            new Rect2(79, 41, 142, 257),
            new Rect2(348, 41, 141, 257),
            new Rect2(646, 41, 112, 257),
            new Rect2(918, 42, 114, 256)
        ],
        [
            new Rect2(62, 325, 161, 259),
            new Rect2(347, 325, 154, 259),
            new Rect2(643, 326, 121, 260),
            new Rect2(909, 327, 119, 259)
        ],
        [
            new Rect2(79, 606, 126, 249),
            new Rect2(358, 606, 128, 249),
            new Rect2(645, 610, 111, 247),
            new Rect2(918, 610, 112, 247)
        ],
        [
            new Rect2(82, 877, 146, 239),
            new Rect2(340, 878, 145, 238),
            new Rect2(648, 882, 115, 235),
            new Rect2(913, 882, 114, 235)
        ],
        [
            new Rect2(77, 1133, 136, 235),
            new Rect2(352, 1133, 133, 235),
            new Rect2(646, 1133, 109, 237),
            new Rect2(920, 1133, 109, 237)
        ]
    ];
    private static readonly Rect2[][] VillageNpcWave2Regions =
    [
        [
            new Rect2(63, 20, 129, 218),
            new Rect2(321, 20, 125, 218),
            new Rect2(594, 20, 92, 218),
            new Rect2(850, 20, 92, 218)
        ],
        [
            new Rect2(68, 276, 120, 218),
            new Rect2(325, 276, 117, 218),
            new Rect2(592, 276, 96, 218),
            new Rect2(849, 276, 94, 218)
        ],
        [
            new Rect2(73, 532, 109, 218),
            new Rect2(329, 532, 109, 218),
            new Rect2(594, 532, 92, 218),
            new Rect2(850, 532, 92, 218)
        ],
        [
            new Rect2(62, 788, 132, 218),
            new Rect2(318, 788, 131, 218),
            new Rect2(591, 788, 97, 218),
            new Rect2(848, 788, 96, 218)
        ]
    ];
    private static readonly Rect2[][] VillageNpcWave3Regions =
    [
        [
            new Rect2(73, 20, 109, 218),
            new Rect2(330, 20, 108, 218),
            new Rect2(596, 20, 88, 218),
            new Rect2(852, 20, 88, 218)
        ],
        [
            new Rect2(60, 276, 135, 218),
            new Rect2(318, 276, 132, 218),
            new Rect2(587, 276, 106, 218),
            new Rect2(844, 276, 103, 218)
        ],
        [
            new Rect2(63, 532, 130, 218),
            new Rect2(323, 532, 122, 218),
            new Rect2(595, 532, 89, 218),
            new Rect2(852, 532, 88, 218)
        ],
        [
            new Rect2(73, 788, 109, 218),
            new Rect2(330, 788, 108, 218),
            new Rect2(596, 788, 88, 218),
            new Rect2(852, 788, 88, 218)
        ]
    ];

    private static readonly Rect2[] PlayerFrames =
    [
        new Rect2(105, 38, 245, 410),
        new Rect2(482, 46, 250, 407),
        new Rect2(840, 52, 220, 405),
        new Rect2(1200, 50, 225, 405),
    ];

    private static readonly float[] PlayerFrameBottomInsets = [15, 20, 21, 19];

    private static readonly Rect2[][] PlayerWalkFrames =
    [
        [
            new Rect2(108, 55, 240, 410),
            new Rect2(490, 65, 235, 400),
            new Rect2(845, 72, 230, 395),
            new Rect2(1208, 76, 220, 395),
        ],
        [
            new Rect2(108, 530, 235, 395),
            new Rect2(490, 542, 235, 385),
            new Rect2(845, 552, 225, 380),
            new Rect2(1208, 554, 220, 385),
        ],
    ];

    private static readonly float[][] PlayerWalkFrameBottomInsets =
    [
        [25, 23, 31, 33],
        [2, 4, 14, 18],
    ];

    private static readonly Rect2[] MiraFrames =
    [
        new Rect2(112, 520, 230, 450),
        new Rect2(480, 520, 250, 450),
        new Rect2(842, 520, 215, 450),
        new Rect2(1205, 520, 215, 450),
    ];

    public static Sprite2D CreatePlayerSprite()
    {
        var sprite = CreateCharacterSprite(PlayerFrames, 48);
        SetPlayerFrame(sprite, Vector2I.Down, false, 0);
        sprite.Position = new Vector2(0, 8);
        return sprite;
    }

    public static Sprite2D CreateMiraSprite()
    {
        var sprite = CreateCharacterSprite(MiraFrames, 52);
        sprite.Position = new Vector2(0, -18);
        return sprite;
    }

    public static Sprite2D CreateMarketStallSprite() =>
        CreateEconomySprite(MarketStallRegion, 78);

    public static Sprite2D CreateMoonwellInfuserSprite() =>
        CreateEconomySprite(MoonwellInfuserRegion, 70);

    public static Sprite2D CreateProcessorMachineSprite(
        string machineId,
        ProcessorMachineState? state = null
    )
    {
        var source = machineId switch
        {
            ProcessorCatalog.MoonwellInfuserId =>
                new Rect2(20, 90, 344, 390),
            ProcessorCatalog.PrismPreserveVatId =>
                new Rect2(404, 50, 344, 430),
            ProcessorCatalog.StarweaveDryingLoomId =>
                new Rect2(788, 50, 344, 430),
            ProcessorCatalog.MoonpearlEggPressId =>
                MoonpearlEggPressRegion(state),
            _ => throw new KeyNotFoundException(
                $"Unknown processor machine id '{machineId}'."
            )
        };
        var targetHeight = machineId == ProcessorCatalog.MoonwellInfuserId
            ? 62f
            : 56f;
        var texture = machineId == ProcessorCatalog.MoonpearlEggPressId
            ? MoonpearlEggPress
            : ProcessorMachines;
        return CreateProcessorMachineSprite(texture, source, targetHeight);
    }

    public static void SetProcessorMachineState(
        Sprite2D sprite,
        string machineId,
        ProcessorMachineState state
    )
    {
        if (machineId != ProcessorCatalog.MoonpearlEggPressId)
        {
            return;
        }

        var source = MoonpearlEggPressRegion(state);
        sprite.Texture = MoonpearlEggPress;
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (56f / source.Size.Y);
    }

    public static Sprite2D CreateShippingBinSprite(bool open)
    {
        var sprite = new Sprite2D
        {
            Texture = PhaseAAssets,
            RegionEnabled = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        SetShippingBinState(sprite, open);
        return sprite;
    }

    public static void SetShippingBinState(Sprite2D sprite, bool open)
    {
        var source = open ? OpenShippingBinRegion : ClosedShippingBinRegion;
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (56f / source.Size.Y);
    }

    public static Texture2D CreateWeatherIcon(string weatherId)
    {
        if (weatherId == DataCatalog.LongnightSnowWeatherId)
        {
            return new AtlasTexture
            {
                Atlas = LongnightSnowWeather,
                Region = LongnightSnowIconRegion,
                FilterClip = true
            };
        }

        return CreatePhaseAIcon(DataCatalog.Weather(weatherId).AtlasIndex);
    }

    internal static Texture2D LongnightSnowTexture =>
        LongnightSnowWeather;

    internal static Rect2 LongnightSnowflakeRegion(int variant) =>
        LongnightSnowflakeRegions[
            Math.Clamp(variant, 0, LongnightSnowflakeRegions.Length - 1)
        ];

    internal static Rect2 LongnightSnowGustRegion(int frame) =>
        LongnightSnowGustRegions[
            Math.Clamp(frame, 0, LongnightSnowGustRegions.Length - 1)
        ];

    public static Texture2D CreateForecastIcon() =>
        CreatePhaseAIcon(3);

    public static Texture2D CreateShippingBinIcon(bool open) => new AtlasTexture
    {
        Atlas = PhaseAAssets,
        Region = open ? OpenShippingBinRegion : ClosedShippingBinRegion,
        FilterClip = true
    };

    public static Texture2D CreateCalendarIcon() =>
        CreatePhaseAIcon(4);

    public static Texture2D CreateEarningsIcon() =>
        CreatePhaseAIcon(5);

    public static Sprite2D CreateStarwovenChestSprite(bool open)
    {
        var sprite = new Sprite2D
        {
            Texture = StarwovenChest,
            RegionEnabled = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        SetStarwovenChestState(sprite, open);
        return sprite;
    }

    public static void SetStarwovenChestState(Sprite2D sprite, bool open)
    {
        var source = open ? StarwovenChestOpenRegion : StarwovenChestClosedRegion;
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (42f / source.Size.Y);
    }

    public static Texture2D CreateStarwovenChestItemIcon() => new AtlasTexture
    {
        Atlas = StarwovenChest,
        Region = StarwovenChestItemRegion,
        FilterClip = true
    };

    public static Texture2D CreateCraftingIcon() => new AtlasTexture
    {
        Atlas = StarwovenChest,
        Region = StarwovenChestCraftRegion,
        FilterClip = true
    };

    public static Sprite2D CreateFarmObjectSprite(string itemId)
    {
        if (itemId == DataCatalog.GlowcombHiveId)
        {
            return CreateBeehiveSprite(false);
        }

        var definition = DataCatalog.FarmObject(itemId);
        var source = FarmObjectRegions[itemId];
        var sprite = new Sprite2D
        {
            Texture = FarmPlaceables,
            RegionEnabled = true,
            RegionRect = source,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        if (definition.Kind == FarmObjectKind.Path)
        {
            var largestSide = Math.Max(source.Size.X, source.Size.Y);
            sprite.Scale = Vector2.One * (16f / largestSide);
            return sprite;
        }

        var targetHeight = definition.Kind switch
        {
            FarmObjectKind.Fence => 24f,
            FarmObjectKind.Torch => 29f,
            FarmObjectKind.Sprinkler => 18f,
            _ => 16f
        };
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (targetHeight / source.Size.Y);
        return sprite;
    }

    public static Texture2D CreateFarmObjectItemIcon(string itemId)
    {
        if (TryOrchardItemIcon(itemId, out var texture, out var region))
        {
            return new AtlasTexture
            {
                Atlas = texture,
                Region = region,
                FilterClip = true
            };
        }

        return new AtlasTexture
        {
            Atlas = FarmPlaceables,
            Region = FarmObjectIconRegions[itemId],
            FilterClip = true
        };
    }

    public static bool TryFarmObjectItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        if (TryOrchardItemIcon(itemId, out texture, out region))
        {
            return true;
        }

        texture = FarmPlaceables;
        return FarmObjectIconRegions.TryGetValue(itemId, out region);
    }

    public static Sprite2D CreateFruitTreeSprite(FruitTreeState tree)
    {
        var definition = DataCatalog.FruitTree(tree.TreeId);
        var source = definition.Id == DataCatalog.MoonplumTreeId &&
            tree.IsMature
                ? MoonplumTreeMapRegion
                : MoonplumSaplingMapRegion;
        var height = tree.IsMature ? 72f : 42f;
        return CreateOrchardSprite(source, height);
    }

    public static Sprite2D CreateBeehiveSprite(bool ready)
    {
        var source = ready
            ? GlowcombHiveReadyMapRegion
            : GlowcombHiveMapRegion;
        return CreateOrchardSprite(source, ready ? 50f : 45f);
    }

    public static bool TryOrchardItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = OrchardHives;
        region = itemId switch
        {
            DataCatalog.MoonplumSaplingId => MoonplumSaplingIconRegion,
            DataCatalog.MoonplumId => MoonplumIconRegion,
            DataCatalog.StarhoneyId => StarhoneyIconRegion,
            DataCatalog.GlowcombHiveId => GlowcombHiveIconRegion,
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    public static Texture2D CreateStarsoilFertilizerCraftIcon() =>
        new AtlasTexture
        {
            Atlas = CropQualityFertilizer,
            Region = StarsoilFertilizerCraftRegion,
            FilterClip = true
        };

    public static bool TryCropQualityItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = CropQualityFertilizer;
        region = itemId == DataCatalog.StarsoilFertilizerId
            ? StarsoilFertilizerItemRegion
            : default;
        return region.Size != Vector2.Zero;
    }

    public static Texture2D CropQualityFertilizerTexture =>
        CropQualityFertilizer;

    public static Rect2 FertilizedSoilTextureRegion =>
        FertilizedSoilRegion;

    public static Rect2 QualityBadgeRegion(CropQuality quality)
    {
        return quality switch
        {
            CropQuality.Luminous => LuminousQualityRegion,
            CropQuality.Starlight => StarlightQualityRegion,
            _ => RegularQualityRegion
        };
    }

    public static Texture2D VillageLandmarkTexture => VillageLandmarks;

    public static Texture2D TwilightEmporiumExteriorTexture =>
        TwilightEmporiumExterior;

    public static Rect2 TwilightEmporiumExteriorTextureRegion =>
        TwilightEmporiumExteriorRegion;

    public static Texture2D StarlightPostExteriorTexture =>
        StarlightPostExterior;

    public static Rect2 StarlightPostExteriorTextureRegion =>
        StarlightPostExteriorRegion;

    public static Texture2D StarfallWatchExteriorTexture =>
        StarfallWatchExterior;

    public static Rect2 StarfallWatchExteriorTextureRegion =>
        StarfallWatchExteriorRegion;

    public static Rect2 VillageLandmarkRegion(int atlasIndex) =>
        VillageLandmarkRegions[Math.Clamp(
            atlasIndex,
            0,
            VillageLandmarkRegions.Length - 1
        )];

    public static Texture2D VillageNpcTexture(string atlasId) => atlasId switch
    {
        NpcArtCatalog.BaseAtlasId => VillageNpcs,
        NpcArtCatalog.ExpansionAtlasId => VillageNpcsExpansion,
        NpcArtCatalog.Wave2AtlasId => VillageNpcsWave2,
        NpcArtCatalog.Wave3AtlasId => VillageNpcsWave3,
        _ => throw new ArgumentException(
            $"Unknown village NPC atlas: {atlasId}.",
            nameof(atlasId)
        )
    };

    public static Rect2 VillageNpcRegion(
        string atlasId,
        int row,
        NpcFacing facing
    )
    {
        var column = facing switch
        {
            NpcFacing.Down => 0,
            NpcFacing.Up => 1,
            NpcFacing.Left => 2,
            NpcFacing.Right => 3,
            _ => 0
        };
        var regions = atlasId switch
        {
            NpcArtCatalog.BaseAtlasId => VillageNpcRegions,
            NpcArtCatalog.ExpansionAtlasId => VillageNpcExpansionRegions,
            NpcArtCatalog.Wave2AtlasId => VillageNpcWave2Regions,
            NpcArtCatalog.Wave3AtlasId => VillageNpcWave3Regions,
            _ => throw new ArgumentException(
                $"Unknown village NPC atlas: {atlasId}.",
                nameof(atlasId)
            )
        };
        if (row < 0 || row >= regions.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row),
                row,
                $"Village NPC row is outside atlas {atlasId}."
            );
        }

        return regions[row][column];
    }

    public static Texture2D RelationshipIcon(
        RelationshipTier tier
    )
    {
        var column = tier switch
        {
            RelationshipTier.TrustedFriend => 1,
            RelationshipTier.KindredLight => 2,
            _ => 0
        };
        return CreateRelationshipAtlas(column, 0);
    }

    public static Texture2D GiftReactionIcon(GiftReaction reaction)
    {
        var column = reaction switch
        {
            GiftReaction.Loved => 0,
            GiftReaction.Disliked => 2,
            _ => 1
        };
        return CreateRelationshipAtlas(column, 1);
    }

    public static Sprite2D CreateCommissionBoardSprite(bool active)
    {
        var sprite = new Sprite2D
        {
            Texture = DailyCommissionBoard,
            RegionEnabled = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        SetCommissionBoardState(sprite, active);
        return sprite;
    }

    public static void SetCommissionBoardState(Sprite2D sprite, bool active)
    {
        var source = active
            ? CommissionBoardActiveRegion
            : CommissionBoardClosedRegion;
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (56f / source.Size.Y);
    }

    public static Texture2D CreateCommissionParchmentIcon() => new AtlasTexture
    {
        Atlas = DailyCommissionBoard,
        Region = CommissionParchmentRegion,
        FilterClip = true
    };

    public static Texture2D CreateCommissionRewardIcon() => new AtlasTexture
    {
        Atlas = DailyCommissionBoard,
        Region = CommissionRewardRegion,
        FilterClip = true
    };

    public static Sprite2D CreateStarlightMailboxSprite(bool unread)
    {
        var sprite = new Sprite2D
        {
            Texture = StarlightMailbox,
            RegionEnabled = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        SetStarlightMailboxState(sprite, unread);
        return sprite;
    }

    public static void SetStarlightMailboxState(
        Sprite2D sprite,
        bool unread
    )
    {
        var source = unread
            ? StarlightMailboxUnreadRegion
            : StarlightMailboxClosedRegion;
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (62f / source.Size.Y);
    }

    public static Texture2D CreateStarlightEnvelopeIcon() => new AtlasTexture
    {
        Atlas = StarlightMailbox,
        Region = StarlightEnvelopeRegion,
        FilterClip = true
    };

    public static Texture2D CreateRelationshipReplyIcon() => new AtlasTexture
    {
        Atlas = StarlightMailbox,
        Region = RelationshipReplyRegion,
        FilterClip = true
    };

    public static Sprite2D CreateWoodlandStarlightSprite(bool active)
    {
        var sprite = new Sprite2D
        {
            Texture = WoodlandStarlightPedestal,
            RegionEnabled = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        SetWoodlandStarlightState(sprite, active);
        return sprite;
    }

    public static void SetWoodlandStarlightState(
        Sprite2D sprite,
        bool active
    )
    {
        var source = WoodlandStarlightRegion(active);
        sprite.RegionRect = source;
        sprite.Offset = new Vector2(0, -source.Size.Y / 2f);
        sprite.Scale = Vector2.One * (78f / source.Size.Y);
    }

    public static Texture2D CreateStarlightNodeSealIcon() => new AtlasTexture
    {
        Atlas = WoodlandStarlightPedestal,
        Region = StarlightNodeSealRegion,
        FilterClip = true
    };

    public static Texture2D CreateWoodlandRenewalIcon() => new AtlasTexture
    {
        Atlas = WoodlandStarlightPedestal,
        Region = WoodlandRenewalRegion,
        FilterClip = true
    };

    public static Texture2D WoodlandStarlightTexture =>
        WoodlandStarlightPedestal;

    public static Rect2 WoodlandStarlightRegion(bool active)
    {
        if (active)
        {
            return WoodlandStarlightActiveRegion;
        }

        return WoodlandStarlightDormantRegion;
    }

    public static bool TryStarwovenChestItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = StarwovenChest;
        region = itemId == DataCatalog.StarwovenChestId
            ? StarwovenChestItemRegion
            : default;
        return region.Size != Vector2.Zero;
    }

    public static Texture2D CropExpansionTexture => CropExpansion;

    public static bool TryCropExpansionRow(string cropId, out int row) =>
        CropExpansionRows.TryGetValue(cropId, out row);

    public static bool TryCropExpansionItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = CropExpansion;
        region = default;
        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        var cropId = item.Kind switch
        {
            ItemKind.Seed => item.CropId,
            ItemKind.Produce => DataCatalog.BaseItemId(item.Id),
            _ => null
        };
        if (cropId is null || !TryCropExpansionRow(cropId, out var row))
        {
            return false;
        }

        region = CropExpansionRegion(row, item.Kind == ItemKind.Seed ? 0 : 1);
        return true;
    }

    public static Rect2 CropExpansionRegion(int row, int column)
    {
        const int columns = 6;
        const int rows = 6;
        const float width = 1536;
        const float height = 1024;
        var cellWidth = width / columns;
        var top = MathF.Floor(row * height / rows);
        var bottom = MathF.Floor((row + 1) * height / rows);
        return new Rect2(column * cellWidth, top, cellWidth, bottom - top);
    }

    public static Texture2D GleamriseCropsTexture => GleamriseCrops;

    public static bool TryGleamriseCropRow(string cropId, out int row) =>
        GleamriseCropRows.TryGetValue(cropId, out row);

    public static bool TryGleamriseItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = GleamriseResonance;
        region = default;
        if (GleamriseResonanceColumns.TryGetValue(itemId, out var column))
        {
            const float cellSize = 887;
            region = new Rect2(column * cellSize, 0, cellSize, cellSize);
            return true;
        }

        texture = GleamriseCrops;
        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        var cropId = item.Kind switch
        {
            ItemKind.Seed => item.CropId,
            ItemKind.Produce => DataCatalog.BaseItemId(item.Id),
            _ => null
        };
        if (cropId is null || !TryGleamriseCropRow(cropId, out var row))
        {
            return false;
        }

        region = GleamriseCropRegion(
            row,
            item.Kind == ItemKind.Seed ? 0 : 1
        );
        return true;
    }

    public static Rect2 GleamriseCropRegion(int row, int column) =>
        new(column * 256, row * 256, 256, 256);

    public static Texture2D RainveilCropsTexture => RainveilCrops;

    public static bool TryRainveilCropRow(string cropId, out int row) =>
        RainveilCropRows.TryGetValue(cropId, out row);

    public static bool TryRainveilItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = RainveilCrops;
        region = default;
        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        var cropId = item.Kind switch
        {
            ItemKind.Seed => item.CropId,
            ItemKind.Produce => DataCatalog.BaseItemId(item.Id),
            _ => null
        };
        if (cropId is null || !TryRainveilCropRow(cropId, out var row))
        {
            return false;
        }

        region = RainveilCropRegion(
            row,
            item.Kind == ItemKind.Seed ? 0 : 1
        );
        return true;
    }

    public static Rect2 RainveilCropRegion(int row, int column) =>
        new(column * 256, row * 256, 256, 256);

    public static Texture2D StarharvestCropsTexture => StarharvestCrops;

    public static bool TryStarharvestCropRow(string cropId, out int row) =>
        StarharvestCropRows.TryGetValue(cropId, out row);

    public static bool TryStarharvestItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = StarharvestCrops;
        region = default;
        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        var cropId = item.Kind switch
        {
            ItemKind.Seed => item.CropId,
            ItemKind.Produce => DataCatalog.BaseItemId(item.Id),
            _ => null
        };
        if (cropId is null || !TryStarharvestCropRow(cropId, out var row))
        {
            return false;
        }

        region = StarharvestCropRegion(
            row,
            item.Kind == ItemKind.Seed ? 0 : 1
        );
        return true;
    }

    public static Rect2 StarharvestCropRegion(int row, int column) =>
        new(column * 256, row * 256, 256, 256);

    public static (Texture2D Texture, Rect2 Region) EconomyItemIcon(string itemId) =>
        itemId switch
        {
            DataCatalog.StarbudPreserveId =>
                (ProcessorMachines, new Rect2(45, 600, 294, 330)),
            DataCatalog.MoonrootTonicId =>
                (ProcessorMachines, new Rect2(450, 590, 252, 340)),
            DataCatalog.CloudleafTeaId =>
                (ProcessorMachines, new Rect2(825, 670, 270, 270)),
            DataCatalog.StarfeatherCreamId =>
                (MoonpearlEggPress, new Rect2(0, 627, 627, 627)),
            _ => (null!, default)
        };

    public static bool TryProcessorMachineItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        region = itemId switch
        {
            DataCatalog.StarbudPreserveId =>
                new Rect2(45, 600, 294, 330),
            DataCatalog.MoonrootTonicId =>
                new Rect2(450, 590, 252, 340),
            DataCatalog.CloudleafTeaId =>
                new Rect2(825, 670, 270, 270),
            DataCatalog.StarfeatherCreamId =>
                new Rect2(0, 627, 627, 627),
            _ => default
        };
        texture = itemId == DataCatalog.StarfeatherCreamId
            ? MoonpearlEggPress
            : ProcessorMachines;
        return region.Size != Vector2.Zero;
    }

    private static Rect2 MoonpearlEggPressRegion(
        ProcessorMachineState? state
    )
    {
        if (state?.IsReady == true)
        {
            return new Rect2(627, 627, 627, 627);
        }

        return state?.IsIdle == false
            ? new Rect2(627, 0, 627, 627)
            : new Rect2(0, 0, 627, 627);
    }

    public static void SetPlayerFrame(
        Sprite2D sprite,
        Vector2I facing,
        bool isWalking,
        int walkFrame
    )
    {
        var directionIndex = DirectionIndex(facing);
        var frameIndex = Math.Clamp(walkFrame, 0, PlayerWalkFrames.Length - 1);
        var source = isWalking
            ? PlayerWalkFrames[frameIndex][directionIndex]
            : PlayerFrames[directionIndex];
        var bottomInset = isWalking
            ? PlayerWalkFrameBottomInsets[frameIndex][directionIndex]
            : PlayerFrameBottomInsets[directionIndex];
        sprite.Texture = isWalking ? PlayerWalkCycle : Characters;
        sprite.RegionRect = source;
        // Pivot every generated frame at the visible boot sole instead of the region center.
        // The generated atlas has different transparent padding per direction and stride.
        sprite.Offset = new Vector2(0, bottomInset - source.Size.Y / 2f);
        var scale = 48f / source.Size.Y;
        sprite.Scale = new Vector2(scale, scale);
    }

    public static ShaderMaterial CreateChromaKeyMaterial()
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;

                void fragment() {
                    vec4 pixel = texture(TEXTURE, UV);
                    float other = max(pixel.g, pixel.b);
                    bool chroma = pixel.r > 0.45
                        && pixel.g < 0.32
                        && pixel.b < 0.32
                        && pixel.r > other * 2.0;
                    if (chroma) {
                        pixel.a = 0.0;
                    }
                    COLOR = pixel;
                }
                """
        };
        return new ShaderMaterial { Shader = shader };
    }

    private static Sprite2D CreateCharacterSprite(Rect2[] frames, float targetHeight)
    {
        var source = frames[0];
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = Characters,
            RegionEnabled = true,
            RegionRect = source,
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Material = CreateChromaKeyMaterial()
        };
    }

    private static Sprite2D CreateEconomySprite(Rect2 source, float targetHeight)
    {
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = EconomyAssets,
            RegionEnabled = true,
            RegionRect = source,
            Offset = new Vector2(0, -source.Size.Y / 2f),
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Material = CreateChromaKeyMaterial()
        };
    }

    private static Sprite2D CreateProcessorMachineSprite(
        Texture2D texture,
        Rect2 source,
        float targetHeight
    )
    {
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = texture,
            RegionEnabled = true,
            RegionRect = source,
            Offset = new Vector2(0, -source.Size.Y / 2f),
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
    }

    private static Sprite2D CreateOrchardSprite(
        Rect2 source,
        float targetHeight
    )
    {
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = OrchardHives,
            RegionEnabled = true,
            RegionRect = source,
            Offset = new Vector2(0, -source.Size.Y / 2f),
            Scale = Vector2.One * scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
    }

    private static AtlasTexture CreatePhaseAIcon(int index) => new()
    {
        Atlas = PhaseAAssets,
        Region = PhaseAIconRegions[index],
        FilterClip = true
    };

    private static AtlasTexture CreateRelationshipAtlas(
        int column,
        int row
    ) => new()
    {
        Atlas = RelationshipGifts,
        Region = new Rect2(column * 512, row * 512, 512, 512),
        FilterClip = true
    };

    private static int DirectionIndex(Vector2I facing)
    {
        if (facing == Vector2I.Up)
        {
            return 1;
        }

        // The generated side-profile cells describe the visible side: cell 2 faces left,
        // while cell 3 faces right.
        if (facing == Vector2I.Right)
        {
            return 3;
        }

        return facing == Vector2I.Left ? 2 : 0;
    }
}

internal sealed partial class CottageBackdrop : Node2D
{
    private static readonly Texture2D BaseBackground =
        GD.Load<Texture2D>("res://assets/generated/locations/cottage/cottage_twilight_interior.png");
    private static readonly Texture2D UpgradedBackground =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/cottage/cottage_first_upgrade_interior.png"
        );
    private static readonly Texture2D SecondUpgradeBackground =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/cottage/cottage_second_upgrade_interior.png"
        );
    private readonly GameSession _session;

    public CottageBackdrop(GameSession session)
    {
        _session = session;
        ZIndex = -100;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        session.Construction.Changed += QueueRedraw;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            _session.CottageUpgradeLevel switch
            {
                2 => SecondUpgradeBackground,
                1 => UpgradedBackground,
                _ => BaseBackground
            },
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }

    public override void _ExitTree()
    {
        _session.Construction.Changed -= QueueRedraw;
    }
}

internal sealed partial class ArchiveBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/moonlit_archive_interior.png"
        );

    public ArchiveBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class WorkshopBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/moonstone_workshop_interior.png"
        );

    public WorkshopBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class TeaHouseBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/starweaver_tea_house_interior.png"
        );

    public TeaHouseBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class TwilightEmporiumBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/twilight_emporium_interior.png"
        );

    public TwilightEmporiumBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class StarlightPostBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/starlight_post_interior.png"
        );

    public StarlightPostBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class StarfallWatchBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/locations/village/starfall_watch_interior.png"
        );

    public StarfallWatchBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class GeneratedOrchardLayer : Node2D
{
    private readonly GameSession _session;

    public GeneratedOrchardLayer(GameSession session)
    {
        _session = session;
        ZIndex = 6;
        YSortEnabled = true;
        session.Orchard.Changed += OnOrchardChanged;
        session.FarmObjects.Changed += OnFarmObjectChanged;
        Rebuild();
    }

    public override void _ExitTree()
    {
        _session.Orchard.Changed -= OnOrchardChanged;
        _session.FarmObjects.Changed -= OnFarmObjectChanged;
    }

    private void OnOrchardChanged(GridPosition position)
    {
        _ = position;
        Rebuild();
    }

    private void OnFarmObjectChanged(GridPosition position)
    {
        _ = position;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        foreach (var pair in _session.Orchard.FruitTrees)
        {
            var sprite = GeneratedArt.CreateFruitTreeSprite(pair.Value);
            sprite.Name = $"FruitTree_{pair.Value.TreeId}_{pair.Key.X}_{pair.Key.Y}";
            sprite.Position = CellCenter(pair.Key) + new Vector2(0, 8);
            sprite.ZIndex = pair.Key.Y;
            sprite.AddChild(new ActorShadow
            {
                Position = new Vector2(0, 1),
                ZIndex = -2
            });
            AddChild(sprite);
        }

        foreach (var pair in _session.FarmObjects.Objects)
        {
            if (pair.Value != DataCatalog.GlowcombHiveId)
            {
                continue;
            }

            var hive = _session.Orchard.BeehiveAt(pair.Key);
            var sprite = GeneratedArt.CreateBeehiveSprite(
                hive?.HasHoney == true
            );
            sprite.Name = $"GlowcombHive_{pair.Key.X}_{pair.Key.Y}";
            sprite.Position = CellCenter(pair.Key) + new Vector2(0, 8);
            sprite.ZIndex = pair.Key.Y;
            sprite.AddChild(new ActorShadow
            {
                Position = new Vector2(0, 1),
                ZIndex = -2
            });
            if (hive?.HasHoney == true)
            {
                sprite.AddChild(new FarmObjectGlow(FarmObjectKind.Beehive)
                {
                    Position = new Vector2(0, -18),
                    ZIndex = -1
                });
            }

            AddChild(sprite);
        }
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class GeneratedCropLayer : Node2D
{
    private static readonly Texture2D Crops =
        GD.Load<Texture2D>("res://assets/generated/farming/crops/crop_stages_chroma.png");

    private static readonly Rect2[] StarbudFrames =
    [
        new Rect2(92, 315, 190, 150),
        new Rect2(405, 280, 235, 185),
        new Rect2(775, 130, 265, 345),
        new Rect2(1140, 82, 310, 395),
    ];

    private static readonly Rect2[] MoonrootFrames =
    [
        new Rect2(100, 728, 185, 160),
        new Rect2(400, 700, 255, 195),
        new Rect2(735, 632, 330, 275),
        new Rect2(1090, 530, 380, 385),
    ];

    private readonly FarmSystem _farm;

    public GeneratedCropLayer(FarmSystem farm)
    {
        _farm = farm;
        ZIndex = 1;
        farm.TileChanged += OnTileChanged;
        Rebuild();
    }

    public override void _ExitTree()
    {
        _farm.TileChanged -= OnTileChanged;
    }

    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        foreach (var tile in _farm.Tiles.Values)
        {
            if (tile.FertilizerId == DataCatalog.StarsoilFertilizerId)
            {
                var fertilizerSource =
                    GeneratedArt.FertilizedSoilTextureRegion;
                var fertilizerScale = 15f / fertilizerSource.Size.X;
                AddChild(new Sprite2D
                {
                    Texture = GeneratedArt.CropQualityFertilizerTexture,
                    RegionEnabled = true,
                    RegionRect = fertilizerSource,
                    Scale = Vector2.One * fertilizerScale,
                    Position = new Vector2(
                        tile.X * 16 + 8,
                        tile.Y * 16 + 11
                    ),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                });
            }

            if (string.IsNullOrWhiteSpace(tile.CropId))
            {
                continue;
            }

            var definition = DataCatalog.Crop(tile.CropId);
            var frameIndex = definition.GetVisualStageIndex(tile.WateredNights);
            Texture2D texture;
            Rect2 source;
            Material? material;
            float height;
            if (GeneratedArt.TryStarharvestCropRow(
                    tile.CropId,
                    out var starharvestRow
                ))
            {
                texture = GeneratedArt.StarharvestCropsTexture;
                source = GeneratedArt.StarharvestCropRegion(
                    starharvestRow,
                    frameIndex + 2
                );
                material = null;
                height = 34f;
            }
            else if (GeneratedArt.TryRainveilCropRow(
                    tile.CropId,
                    out var rainveilRow
                ))
            {
                texture = GeneratedArt.RainveilCropsTexture;
                source = GeneratedArt.RainveilCropRegion(
                    rainveilRow,
                    frameIndex + 2
                );
                material = null;
                height = 34f;
            }
            else if (GeneratedArt.TryGleamriseCropRow(
                    tile.CropId,
                    out var gleamriseRow
                ))
            {
                texture = GeneratedArt.GleamriseCropsTexture;
                source = GeneratedArt.GleamriseCropRegion(
                    gleamriseRow,
                    frameIndex + 2
                );
                material = null;
                height = 34f;
            }
            else if (GeneratedArt.TryCropExpansionRow(
                         tile.CropId,
                         out var expandedRow
                     ))
            {
                texture = GeneratedArt.CropExpansionTexture;
                source = GeneratedArt.CropExpansionRegion(expandedRow, frameIndex + 2);
                material = null;
                height = 34f;
            }
            else
            {
                texture = Crops;
                var frames = tile.CropId == DataCatalog.StarbudId
                    ? StarbudFrames
                    : MoonrootFrames;
                source = frames[Math.Clamp(frameIndex, 0, frames.Length - 1)];
                material = GeneratedArt.CreateChromaKeyMaterial();
                height = frameIndex switch
                {
                    0 => 10f,
                    1 => 16f,
                    2 => 23f,
                    _ => 29f
                };
            }

            var baseline = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 15);
            var scale = height / source.Size.Y;
            AddChild(new Sprite2D
            {
                Texture = texture,
                RegionEnabled = true,
                RegionRect = source,
                Scale = new Vector2(scale, scale),
                Position = baseline - new Vector2(0, height / 2),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Material = material
            });
        }
    }

    private void OnTileChanged(GridPosition position)
    {
        _ = position;
        Rebuild();
    }
}

internal sealed partial class FarmSoilStateLayer : Node2D
{
    private readonly FarmSystem _farm;

    public FarmSoilStateLayer(FarmSystem farm)
    {
        _farm = farm;
        ZIndex = -1;
        farm.TileChanged += OnTileChanged;
    }

    public override void _Draw()
    {
        foreach (var tile in _farm.Tiles.Values)
        {
            if (!tile.Tilled)
            {
                continue;
            }

            var origin = new Vector2(tile.X * 16, tile.Y * 16);
            var soil = tile.Watered
                ? new Color("#18394ed9")
                : new Color("#2b202bd9");
            var ridge = tile.Watered
                ? new Color("#4f8293d0")
                : new Color("#6f4e52c9");
            DrawColoredPolygon(
                [
                    origin + new Vector2(1, 6),
                    origin + new Vector2(4, 2),
                    origin + new Vector2(12, 2),
                    origin + new Vector2(15, 6),
                    origin + new Vector2(14, 12),
                    origin + new Vector2(10, 14),
                    origin + new Vector2(4, 13),
                    origin + new Vector2(1, 10),
                ],
                soil
            );
            DrawLine(origin + new Vector2(3, 6), origin + new Vector2(13, 5), ridge, 1);
            DrawLine(origin + new Vector2(3, 10), origin + new Vector2(12, 9), ridge, 1);

            if (tile.Watered)
            {
                DrawCircle(origin + new Vector2(5, 4), 1, new Color("#8ee6becf"));
                DrawCircle(origin + new Vector2(12, 11), 0.8f, new Color("#4bc5bdc8"));
            }
        }
    }

    public override void _ExitTree()
    {
        _farm.TileChanged -= OnTileChanged;
    }

    private void OnTileChanged(GridPosition position)
    {
        _ = position;
        QueueRedraw();
    }
}
