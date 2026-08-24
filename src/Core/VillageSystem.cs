namespace Luminfield.Core;

public enum NpcFacing
{
    Down,
    Up,
    Left,
    Right
}

public enum GiftReaction
{
    Loved,
    Liked,
    Neutral,
    Disliked
}

public enum RelationshipTier
{
    NewAcquaintance,
    TrustedFriend,
    KindredLight
}

public readonly record struct GridArea(
    int MinX,
    int MinY,
    int MaxX,
    int MaxY
)
{
    public bool Contains(GridPosition cell) =>
        cell.X >= MinX &&
        cell.X <= MaxX &&
        cell.Y >= MinY &&
        cell.Y <= MaxY;
}

public sealed record VillageLandmarkDefinition(
    string Id,
    GridPosition Anchor,
    int AtlasIndex,
    string NameKey,
    IReadOnlyList<GridArea> CollisionAreas
);

public sealed record NpcScheduleEntry(
    int StartMinute,
    int EndMinute,
    string LocationId,
    GridPosition Position,
    NpcFacing Facing,
    string DialogueKey,
    IReadOnlyList<int> WeekdayIndices,
    IReadOnlyList<string>? WeatherIds = null,
    IReadOnlyList<string>? SeasonIds = null,
    int Priority = 0,
    IReadOnlyList<string>? FestivalIds = null
)
{
    public bool Matches(int day, int minuteOfDay) => Matches(
        day,
        minuteOfDay,
        WeatherSystem.WeatherForDay(day)
    );

    public bool Matches(
        int day,
        int minuteOfDay,
        string weatherId
    )
    {
        if (minuteOfDay < StartMinute || minuteOfDay >= EndMinute)
        {
            return false;
        }

        if (WeekdayIndices.Count > 0 &&
            !WeekdayIndices.Contains(CalendarSystem.WeekdayIndex(day)))
        {
            return false;
        }

        if (WeatherIds is { Count: > 0 } &&
            !WeatherIds.Contains(weatherId, StringComparer.Ordinal))
        {
            return false;
        }

        if (SeasonIds is { Count: > 0 } &&
            !SeasonIds.Contains(
                CalendarSystem.SeasonId(day),
                StringComparer.Ordinal
            ))
        {
            return false;
        }

        return FestivalIds is not { Count: > 0 } ||
            FestivalIds.Any(festivalId =>
                FestivalCatalog.OccursOnDay(festivalId, day)
            );
    }
}

public sealed record VillageNpcDefinition(
    string Id,
    string NameKey,
    string RoleKey,
    string IntroductionKey,
    int ScheduleOrder,
    IReadOnlyList<string> LovedGiftIds,
    IReadOnlyList<ItemKind> LikedGiftKinds,
    IReadOnlyList<ItemKind> DislikedGiftKinds,
    IReadOnlyList<NpcScheduleEntry> Schedule
);

public sealed record VillageNpcState(
    VillageNpcDefinition Definition,
    string LocationId,
    GridPosition Position,
    NpcFacing Facing,
    string DialogueKey
);

public sealed record VillageConversation(
    string NpcId,
    string NameKey,
    string RoleKey,
    string DialogueKey,
    bool FirstMeeting,
    GiftReaction? GiftReaction,
    int RelationshipPoints,
    RelationshipTier RelationshipTier,
    CharacterEventDialogue? CharacterEvent = null
);

public sealed record VillageInteractionCheck(
    VillageNpcState? Npc,
    bool IsAvailable,
    bool IsGift,
    GiftReaction? GiftReaction,
    string FailureKey
);

public static class VillageCatalog
{
    public const int BaseSchedulePriority = 0;
    public const int SeasonSchedulePriority = 10;
    public const int WeatherSchedulePriority = 20;
    public const int RestdaySchedulePriority = 100;
    public const int FestivalSchedulePriority = 200;
    public const string LioraId = "liora";
    public const string TaviId = "tavi";
    public const string NemiId = "nemi";
    public const string SelaId = "sela";
    public const string ElowenId = "elowen";
    public const string VessaId = "vessa";
    public const string OrinId = "orin";
    public const string KaelId = "kael";
    public const string HaldenId = "halden";
    public const string MaveaId = "mavea";
    public const string SivrenId = "sivren";
    public const string DorrikId = "dorrik";
    public const string YvaraId = "yvara";
    public const string BrialId = "brial";
    public const string PavriId = "pavri";
    public const string RovenId = "roven";
    public const string VillageGateLandmarkId = "lumen_village_gate";
    public const string MoonlitArchiveLandmarkId = "moonlit_archive";
    public const string MoonstoneWorkshopLandmarkId =
        "moonstone_workshop";
    public const string StarweaverTeaHouseLandmarkId =
        "starweaver_tea_house";
    public const string TwilightEmporiumLandmarkId =
        "twilight_emporium";
    public const string StarlightPostLandmarkId = "starlight_post";
    public const string StarfallWatchLandmarkId = "starfall_watch";

    public static readonly GridArea VillageBounds = new(64, 32, 191, 127);
    public static readonly GridPosition VillageCenterCell = new(128, 80);
    public static readonly GridPosition VillageGateCell = new(128, 127);
    public static readonly GridPosition MoonlitArchiveDoorCell = new(100, 59);
    public static readonly GridPosition MoonlitArchiveExitCell = new(20, 18);
    public static readonly GridPosition MoonlitArchiveDeskCell = new(20, 9);
    public static readonly GridArea MoonlitArchiveDeskArea =
        new(16, 8, 23, 11);
    public static readonly GridPosition MoonstoneWorkshopDoorCell =
        new(98, 98);
    public static readonly GridPosition MoonstoneWorkshopExitCell =
        new(20, 19);
    public static readonly GridPosition MoonRuneWorkbenchCell =
        new(20, 9);
    public static readonly GridPosition StarweaverTeaHouseDoorCell =
        new(158, 62);
    public static readonly GridPosition StarweaverTeaHouseExitCell =
        new(20, 19);
    public static readonly GridPosition StarwovenTeaCounterCell =
        new(20, 9);
    public static readonly GridPosition TwilightEmporiumDoorCell =
        new(166, 98);
    public static readonly GridPosition TwilightEmporiumExitCell =
        new(20, 19);
    public static readonly GridPosition TravelManifestCell =
        new(20, 8);
    public static readonly GridPosition StarlightPostDoorCell =
        new(76, 59);
    public static readonly GridPosition StarlightPostExitCell =
        new(20, 19);
    public static readonly GridPosition RouteSortingCounterCell =
        new(20, 8);
    public static readonly GridPosition StarfallWatchDoorCell =
        new(76, 98);
    public static readonly GridPosition StarfallWatchExitCell =
        new(20, 19);
    public static readonly GridPosition SealRouteTableCell =
        new(20, 8);
    public const int MoonlitArchiveOpenMinute = 8 * 60;
    public const int MoonlitArchiveCloseMinute = 20 * 60;
    public const int MoonstoneWorkshopOpenMinute = 8 * 60;
    public const int MoonstoneWorkshopCloseMinute = 19 * 60;
    public const int StarweaverTeaHouseOpenMinute = 9 * 60;
    public const int StarweaverTeaHouseCloseMinute = 21 * 60;
    public const int TwilightEmporiumOpenMinute = 10 * 60;
    public const int TwilightEmporiumCloseMinute = 18 * 60;
    public const int StarlightPostOpenMinute = 7 * 60;
    public const int StarlightPostCloseMinute = 19 * 60;
    public const int StarfallWatchOpenMinute = 6 * 60;
    public const int StarfallWatchCloseMinute = 20 * 60;

    public static bool IsMoonlitArchiveDeskCell(GridPosition cell) =>
        MoonlitArchiveDeskArea.Contains(cell);

    public static bool IsAdjacentToMoonlitArchiveDesk(GridPosition cell)
    {
        var nearestX = Math.Clamp(
            cell.X,
            MoonlitArchiveDeskArea.MinX,
            MoonlitArchiveDeskArea.MaxX
        );
        var nearestY = Math.Clamp(
            cell.Y,
            MoonlitArchiveDeskArea.MinY,
            MoonlitArchiveDeskArea.MaxY
        );
        return Math.Abs(cell.X - nearestX) +
            Math.Abs(cell.Y - nearestY) <= 1;
    }

    public static readonly IReadOnlyList<VillageLandmarkDefinition> Landmarks =
    [
        new(
            MoonlitArchiveLandmarkId,
            MoonlitArchiveDoorCell,
            0,
            "village.landmark.archive",
            [new GridArea(94, 45, 106, 57)]
        ),
        new(
            StarweaverTeaHouseLandmarkId,
            StarweaverTeaHouseDoorCell,
            1,
            "village.landmark.tea_house",
            [new GridArea(151, 48, 165, 60)]
        ),
        new(
            MoonstoneWorkshopLandmarkId,
            MoonstoneWorkshopDoorCell,
            2,
            "village.landmark.workshop",
            [new GridArea(91, 84, 104, 96)]
        ),
        new(
            "starlight_well",
            new GridPosition(112, 72),
            3,
            "village.landmark.well",
            [new GridArea(108, 64, 116, 72)]
        ),
        new(
            VillageGateLandmarkId,
            VillageGateCell,
            4,
            "world.landmark.village_gate",
            [
                new GridArea(121, 120, 123, 127),
                new GridArea(133, 120, 135, 127)
            ]
        ),
        new(
            "village_sign",
            new GridPosition(116, 118),
            5,
            "village.landmark.sign",
            [new GridArea(116, 118, 116, 118)]
        ),
        new(
            "lantern_bench",
            new GridPosition(146, 74),
            6,
            "village.landmark.bench",
            [new GridArea(143, 74, 149, 74)]
        ),
        new(
            "glowflower_cart",
            new GridPosition(174, 82),
            7,
            "village.landmark.flower_cart",
            [new GridArea(173, 80, 175, 82)]
        ),
        new(
            TwilightEmporiumLandmarkId,
            TwilightEmporiumDoorCell,
            8,
            "village.landmark.twilight_emporium",
            [new GridArea(162, 86, 170, 96)]
        ),
        new(
            StarlightPostLandmarkId,
            StarlightPostDoorCell,
            9,
            "village.landmark.starlight_post",
            [new GridArea(69, 45, 82, 57)]
        ),
        new(
            StarfallWatchLandmarkId,
            StarfallWatchDoorCell,
            10,
            "village.landmark.starfall_watch",
            [new GridArea(69, 84, 82, 96)]
        )
    ];

    public static readonly IReadOnlyDictionary<string, VillageNpcDefinition>
        Npcs = new Dictionary<string, VillageNpcDefinition>(
            StringComparer.Ordinal
        )
        {
            [LioraId] = new(
                LioraId,
                "village.npc.liora.name",
                "village.npc.liora.role",
                "village.npc.liora.intro",
                0,
                [
                    DataCatalog.MoonrootId,
                    DataCatalog.MoonrootTonicId,
                    DataCatalog.CrystalShardId
                ],
                [ItemKind.Produce, ItemKind.Artisan],
                [ItemKind.Fertilizer, ItemKind.Placeable],
                [
                    GleamriseFestivalSlot(LioraId, NpcFacing.Down),
                    LongnightFestivalSlot(LioraId, NpcFacing.Down),
                    FireflyFestivalSlot(LioraId, NpcFacing.Down),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[LioraId],
                        NpcFacing.Down,
                        LioraId
                    ),
                    SeasonSlot(
                        13,
                        17,
                        PlayerLocationIds.World,
                        new GridPosition(103, 64),
                        NpcFacing.Up,
                        "village.npc.liora.season_longnight",
                        CalendarSystem.LongnightSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(104, 64),
                        NpcFacing.Left,
                        "village.npc.liora.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(83, 52),
                        NpcFacing.Down,
                        "village.npc.liora.morning"
                    ),
                    ArchiveSlot(
                        9,
                        13,
                        new GridPosition(12, 9),
                        NpcFacing.Left,
                        "village.npc.liora.archive"
                    ),
                    Slot(
                        13,
                        17,
                        new GridPosition(91, 64),
                        NpcFacing.Right,
                        "village.npc.liora.plaza"
                    ),
                    Slot(
                        17,
                        23,
                        new GridPosition(107, 54),
                        NpcFacing.Down,
                        "village.npc.liora.evening"
                    )
                ]
            ),
            [TaviId] = new(
                TaviId,
                "village.npc.tavi.name",
                "village.npc.tavi.role",
                "village.npc.tavi.intro",
                1,
                [
                    DataCatalog.LumenwoodId,
                    DataCatalog.CrystalShardId,
                    DataCatalog.MoonstonePathId
                ],
                [ItemKind.Resource, ItemKind.Artisan],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(TaviId, NpcFacing.Right),
                    LongnightFestivalSlot(TaviId, NpcFacing.Left),
                    FireflyFestivalSlot(TaviId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[TaviId],
                        NpcFacing.Right,
                        TaviId
                    ),
                    WeatherSlot(
                        13,
                        16,
                        PlayerLocationIds.MoonstoneWorkshop,
                        new GridPosition(27, 12),
                        NpcFacing.Left,
                        "village.npc.tavi.weather_stardust",
                        DataCatalog.StardustWindWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(91, 72),
                        NpcFacing.Right,
                        "village.npc.tavi.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(83, 78),
                        NpcFacing.Down,
                        "village.npc.tavi.morning"
                    ),
                    WorkshopSlot(
                        9,
                        13,
                        new GridPosition(13, 10),
                        NpcFacing.Right,
                        "village.npc.tavi.workshop"
                    ),
                    Slot(
                        13,
                        16,
                        new GridPosition(91, 70),
                        NpcFacing.Right,
                        "village.npc.tavi.plaza"
                    ),
                    Slot(
                        16,
                        23,
                        new GridPosition(111, 54),
                        NpcFacing.Down,
                        "village.npc.tavi.evening"
                    )
                ]
            ),
            [NemiId] = new(
                NemiId,
                "village.npc.nemi.name",
                "village.npc.nemi.role",
                "village.npc.nemi.intro",
                2,
                [
                    DataCatalog.StarbudId,
                    DataCatalog.CloudleafId,
                    DataCatalog.StarbudPreserveId
                ],
                [ItemKind.Produce, ItemKind.Seed],
                [ItemKind.Resource, ItemKind.Placeable],
                [
                    GleamriseFestivalSlot(NemiId, NpcFacing.Left),
                    LongnightFestivalSlot(NemiId, NpcFacing.Right),
                    FireflyFestivalSlot(NemiId, NpcFacing.Right),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[NemiId],
                        NpcFacing.Left,
                        NemiId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.StarweaverTeaHouse,
                        new GridPosition(13, 13),
                        NpcFacing.Right,
                        "village.npc.nemi.weather_rain",
                        DataCatalog.RainWeatherId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(110, 56),
                        NpcFacing.Left,
                        "village.npc.nemi.season_gleamrise",
                        CalendarSystem.GleamriseSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(112, 76),
                        NpcFacing.Left,
                        "village.npc.nemi.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(97, 88),
                        NpcFacing.Up,
                        "village.npc.nemi.morning"
                    ),
                    PostSlot(
                        9,
                        13,
                        new GridPosition(13, 12),
                        NpcFacing.Right,
                        "village.npc.nemi.starlight_post"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(112, 70),
                        NpcFacing.Down,
                        "village.npc.nemi.route"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(108, 54),
                        NpcFacing.Down,
                        "village.npc.nemi.evening"
                    )
                ]
            ),
            [SelaId] = new(
                SelaId,
                "village.npc.sela.name",
                "village.npc.sela.role",
                "village.npc.sela.intro",
                3,
                [
                    DataCatalog.CrystalShardId,
                    DataCatalog.MoonstonePathId,
                    DataCatalog.StarlightTorchId
                ],
                [ItemKind.Resource, ItemKind.Placeable],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(SelaId, NpcFacing.Right),
                    LongnightFestivalSlot(SelaId, NpcFacing.Left),
                    FireflyFestivalSlot(SelaId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[SelaId],
                        NpcFacing.Right,
                        SelaId
                    ),
                    WeatherSlot(
                        13,
                        17,
                        PlayerLocationIds.MoonstoneWorkshop,
                        new GridPosition(27, 12),
                        NpcFacing.Left,
                        "village.npc.sela.weather_rain",
                        DataCatalog.RainWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(107, 72),
                        NpcFacing.Left,
                        "village.npc.sela.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(87, 76),
                        NpcFacing.Down,
                        "village.npc.sela.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(88, 76),
                        NpcFacing.Left,
                        "village.npc.sela.workshop"
                    ),
                    Slot(
                        13,
                        17,
                        new GridPosition(100, 72),
                        NpcFacing.Up,
                        "village.npc.sela.plaza"
                    ),
                    Slot(
                        17,
                        23,
                        new GridPosition(114, 54),
                        NpcFacing.Left,
                        "village.npc.sela.evening"
                    )
                ]
            ),
            [ElowenId] = new(
                ElowenId,
                "village.npc.elowen.name",
                "village.npc.elowen.role",
                "village.npc.elowen.intro",
                4,
                [
                    DataCatalog.DewmelonId,
                    DataCatalog.CloudleafId,
                    DataCatalog.MoonrootTonicId
                ],
                [ItemKind.Produce, ItemKind.Artisan],
                [ItemKind.Placeable, ItemKind.Resource],
                [
                    GleamriseFestivalSlot(ElowenId, NpcFacing.Left),
                    LongnightFestivalSlot(ElowenId, NpcFacing.Right),
                    FireflyFestivalSlot(ElowenId, NpcFacing.Right),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[ElowenId],
                        NpcFacing.Left,
                        ElowenId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(103, 56),
                        NpcFacing.Down,
                        "village.npc.elowen.weather_stardust",
                        DataCatalog.StardustWindWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(101, 54),
                        NpcFacing.Down,
                        "village.npc.elowen.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(99, 84),
                        NpcFacing.Up,
                        "village.npc.elowen.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(108, 56),
                        NpcFacing.Right,
                        "village.npc.elowen.well"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(101, 76),
                        NpcFacing.Left,
                        "village.npc.elowen.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(92, 56),
                        NpcFacing.Right,
                        "village.npc.elowen.evening"
                    )
                ]
            ),
            [VessaId] = new(
                VessaId,
                "village.npc.vessa.name",
                "village.npc.vessa.role",
                "village.npc.vessa.intro",
                5,
                [
                    DataCatalog.CloudleafId,
                    DataCatalog.MoonrootId,
                    DataCatalog.MoonrootTonicId
                ],
                [ItemKind.Produce, ItemKind.Seed],
                [ItemKind.Placeable, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(VessaId, NpcFacing.Up),
                    LongnightFestivalSlot(VessaId, NpcFacing.Left),
                    FireflyFestivalSlot(VessaId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[VessaId],
                        NpcFacing.Up,
                        VessaId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(89, 54),
                        NpcFacing.Right,
                        "village.npc.vessa.season_rainveil",
                        CalendarSystem.RainveilSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(85, 56),
                        NpcFacing.Right,
                        "village.npc.vessa.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(89, 56),
                        NpcFacing.Left,
                        "village.npc.vessa.morning"
                    ),
                    TeaHouseSlot(
                        9,
                        13,
                        new GridPosition(13, 10),
                        NpcFacing.Right,
                        "village.npc.vessa.tea_house"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(105, 76),
                        NpcFacing.Left,
                        "village.npc.vessa.route"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(115, 56),
                        NpcFacing.Left,
                        "village.npc.vessa.evening"
                    )
                ]
            ),
            [OrinId] = new(
                OrinId,
                "village.npc.orin.name",
                "village.npc.orin.role",
                "village.npc.orin.intro",
                6,
                [
                    DataCatalog.StarbudPreserveId,
                    DataCatalog.PrismcornId,
                    DataCatalog.GlowpeaId
                ],
                [ItemKind.Artisan, ItemKind.Produce],
                [ItemKind.Fertilizer, ItemKind.Resource],
                [
                    GleamriseFestivalSlot(OrinId, NpcFacing.Left),
                    LongnightFestivalSlot(OrinId, NpcFacing.Right),
                    FireflyFestivalSlot(OrinId, NpcFacing.Right),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[OrinId],
                        NpcFacing.Left,
                        OrinId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.TwilightEmporium,
                        new GridPosition(14, 12),
                        NpcFacing.Right,
                        "village.npc.orin.weather_rain",
                        DataCatalog.RainWeatherId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(114, 76),
                        NpcFacing.Left,
                        "village.npc.orin.season_starharvest",
                        CalendarSystem.StarharvestSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(103, 76),
                        NpcFacing.Left,
                        "village.npc.orin.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(105, 56),
                        NpcFacing.Right,
                        "village.npc.orin.morning"
                    ),
                    Slot(
                        9,
                        10,
                        new GridPosition(114, 56),
                        NpcFacing.Left,
                        "village.npc.orin.market"
                    ),
                    EmporiumSlot(
                        10,
                        13,
                        new GridPosition(14, 10),
                        NpcFacing.Right,
                        "village.npc.orin.emporium"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(104, 74),
                        NpcFacing.Right,
                        "village.npc.orin.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(92, 74),
                        NpcFacing.Right,
                        "village.npc.orin.evening"
                    )
                ]
            ),
            [KaelId] = new(
                KaelId,
                "village.npc.kael.name",
                "village.npc.kael.role",
                "village.npc.kael.intro",
                7,
                [
                    DataCatalog.CrystalShardId,
                    DataCatalog.StarlightTorchId,
                    DataCatalog.EmberbellId
                ],
                [ItemKind.Resource, ItemKind.Placeable],
                [ItemKind.Seed, ItemKind.Artisan],
                [
                    GleamriseFestivalSlot(KaelId, NpcFacing.Up),
                    LongnightFestivalSlot(KaelId, NpcFacing.Left),
                    FireflyFestivalSlot(KaelId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[KaelId],
                        NpcFacing.Up,
                        KaelId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(92, 60),
                        NpcFacing.Right,
                        "village.npc.kael.weather_stardust",
                        DataCatalog.StardustWindWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(96, 54),
                        NpcFacing.Down,
                        "village.npc.kael.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(95, 84),
                        NpcFacing.Up,
                        "village.npc.kael.morning"
                    ),
                    WatchSlot(
                        9,
                        13,
                        new GridPosition(13, 12),
                        NpcFacing.Right,
                        "village.npc.kael.starfall_watch"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(99, 76),
                        NpcFacing.Down,
                        "village.npc.kael.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(100, 54),
                        NpcFacing.Left,
                        "village.npc.kael.evening"
                    )
                ]
            ),
            [HaldenId] = new(
                HaldenId,
                "village.npc.halden.name",
                "village.npc.halden.role",
                "village.npc.halden.intro",
                8,
                [
                    DataCatalog.StarfeatherEggId,
                    DataCatalog.MoonfleeceId,
                    DataCatalog.DewhornMilkId
                ],
                [ItemKind.AnimalFeed, ItemKind.AnimalProduct],
                [ItemKind.Resource, ItemKind.Placeable],
                [
                    GleamriseFestivalSlot(HaldenId, NpcFacing.Right),
                    LongnightFestivalSlot(HaldenId, NpcFacing.Right),
                    FireflyFestivalSlot(HaldenId, NpcFacing.Right),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[HaldenId],
                        NpcFacing.Right,
                        HaldenId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.StarweaverTeaHouse,
                        new GridPosition(27, 13),
                        NpcFacing.Left,
                        "village.npc.halden.weather_longnight_snow",
                        DataCatalog.LongnightSnowWeatherId
                    ),
                    Slot(
                        6,
                        18,
                        new GridPosition(107, 66),
                        NpcFacing.Down,
                        "village.npc.halden.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(100, 84),
                        NpcFacing.Up,
                        "village.npc.halden.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(114, 70),
                        NpcFacing.Left,
                        "village.npc.halden.stocktake"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(103, 70),
                        NpcFacing.Left,
                        "village.npc.halden.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(100, 86),
                        NpcFacing.Up,
                        "village.npc.halden.evening"
                    )
                ]
            ),
            [MaveaId] = new(
                MaveaId,
                "village.npc.mavea.name",
                "village.npc.mavea.role",
                "village.npc.mavea.intro",
                9,
                [
                    DataCatalog.MoonmistStewId,
                    DataCatalog.SunvaultHashId,
                    DataCatalog.StarhoneyCustardId,
                    DataCatalog.LanternrootBrothId
                ],
                [ItemKind.Artisan, ItemKind.AnimalProduct],
                [ItemKind.Resource, ItemKind.Placeable],
                [
                    GleamriseFestivalSlot(MaveaId, NpcFacing.Left),
                    LongnightFestivalSlot(MaveaId, NpcFacing.Left),
                    FireflyFestivalSlot(MaveaId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[MaveaId],
                        NpcFacing.Left,
                        MaveaId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.StarweaverTeaHouse,
                        new GridPosition(25, 13),
                        NpcFacing.Right,
                        "village.npc.mavea.season_longnight",
                        CalendarSystem.LongnightSeasonId
                    ),
                    Slot(
                        10,
                        18,
                        new GridPosition(101, 72),
                        NpcFacing.Up,
                        "village.npc.mavea.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(115, 70),
                        NpcFacing.Down,
                        "village.npc.mavea.morning"
                    ),
                    TeaHouseSlot(
                        9,
                        13,
                        new GridPosition(13, 10),
                        NpcFacing.Right,
                        "village.npc.mavea.tea_house"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(110, 56),
                        NpcFacing.Left,
                        "village.npc.mavea.plaza"
                    ),
                    TeaHouseSlot(
                        18,
                        21,
                        new GridPosition(27, 10),
                        NpcFacing.Left,
                        "village.npc.mavea.evening"
                    ),
                    Slot(
                        21,
                        23,
                        new GridPosition(114, 56),
                        NpcFacing.Left,
                        "village.npc.mavea.close"
                    )
                ]
            ),
            [SivrenId] = new(
                SivrenId,
                "village.npc.sivren.name",
                "village.npc.sivren.role",
                "village.npc.sivren.intro",
                10,
                [
                    DataCatalog.CloudleafTeaId,
                    DataCatalog.CrownstarSaffronId,
                    DataCatalog.CrystalShardId
                ],
                [ItemKind.Artisan, ItemKind.Produce],
                [ItemKind.AnimalFeed, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(SivrenId, NpcFacing.Down),
                    LongnightFestivalSlot(SivrenId, NpcFacing.Down),
                    FireflyFestivalSlot(SivrenId, NpcFacing.Down),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[SivrenId],
                        NpcFacing.Down,
                        SivrenId
                    ),
                    WeatherSlot(
                        13,
                        17,
                        PlayerLocationIds.MoonlitArchive,
                        new GridPosition(27, 12),
                        NpcFacing.Left,
                        "village.npc.sivren.weather_rain",
                        DataCatalog.RainWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(91, 66),
                        NpcFacing.Right,
                        "village.npc.sivren.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(83, 54),
                        NpcFacing.Down,
                        "village.npc.sivren.morning"
                    ),
                    ArchiveSlot(
                        9,
                        17,
                        new GridPosition(27, 9),
                        NpcFacing.Left,
                        "village.npc.sivren.archive"
                    ),
                    Slot(
                        17,
                        23,
                        new GridPosition(89, 54),
                        NpcFacing.Right,
                        "village.npc.sivren.evening"
                    )
                ]
            ),
            [DorrikId] = new(
                DorrikId,
                "village.npc.dorrik.name",
                "village.npc.dorrik.role",
                "village.npc.dorrik.intro",
                11,
                [
                    DataCatalog.MoonstonePathId,
                    DataCatalog.StarwoodFenceId,
                    DataCatalog.DewfallSprinklerId
                ],
                [ItemKind.Placeable, ItemKind.Resource],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(DorrikId, NpcFacing.Up),
                    LongnightFestivalSlot(DorrikId, NpcFacing.Up),
                    FireflyFestivalSlot(DorrikId, NpcFacing.Up),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[DorrikId],
                        NpcFacing.Up,
                        DorrikId
                    ),
                    WeatherSlot(
                        13,
                        17,
                        PlayerLocationIds.MoonstoneWorkshop,
                        new GridPosition(27, 14),
                        NpcFacing.Left,
                        "village.npc.dorrik.weather_stardust",
                        DataCatalog.StardustWindWeatherId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(100, 70),
                        NpcFacing.Down,
                        "village.npc.dorrik.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(85, 78),
                        NpcFacing.Down,
                        "village.npc.dorrik.morning"
                    ),
                    WorkshopSlot(
                        9,
                        13,
                        new GridPosition(27, 10),
                        NpcFacing.Left,
                        "village.npc.dorrik.workshop"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(99, 78),
                        NpcFacing.Up,
                        "village.npc.dorrik.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(85, 76),
                        NpcFacing.Down,
                        "village.npc.dorrik.evening"
                    )
                ]
            ),
            [YvaraId] = new(
                YvaraId,
                "village.npc.yvara.name",
                "village.npc.yvara.role",
                "village.npc.yvara.intro",
                12,
                [
                    DataCatalog.StarsoilFertilizerId,
                    DataCatalog.MoonplumSaplingId,
                    DataCatalog.RainveilLotusSeedId
                ],
                [ItemKind.Seed, ItemKind.Sapling],
                [ItemKind.AnimalProduct, ItemKind.Placeable],
                [
                    GleamriseFestivalSlot(YvaraId, NpcFacing.Right),
                    LongnightFestivalSlot(YvaraId, NpcFacing.Right),
                    FireflyFestivalSlot(YvaraId, NpcFacing.Right),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[YvaraId],
                        NpcFacing.Right,
                        YvaraId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.MoonlitArchive,
                        new GridPosition(26, 12),
                        NpcFacing.Left,
                        "village.npc.yvara.season_longnight",
                        CalendarSystem.LongnightSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(110, 76),
                        NpcFacing.Left,
                        "village.npc.yvara.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        10,
                        new GridPosition(114, 90),
                        NpcFacing.Left,
                        "village.npc.yvara.morning"
                    ),
                    EmporiumSlot(
                        10,
                        13,
                        new GridPosition(26, 10),
                        NpcFacing.Left,
                        "village.npc.yvara.emporium"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(108, 76),
                        NpcFacing.Left,
                        "village.npc.yvara.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(111, 56),
                        NpcFacing.Left,
                        "village.npc.yvara.evening"
                    )
                ]
            ),
            [BrialId] = new(
                BrialId,
                "village.npc.brial.name",
                "village.npc.brial.role",
                "village.npc.brial.intro",
                13,
                [
                    DataCatalog.MoonplumId,
                    DataCatalog.StarhoneyId,
                    DataCatalog.DawnlaceId
                ],
                [ItemKind.Produce, ItemKind.Artisan],
                [ItemKind.Resource, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(BrialId, NpcFacing.Left),
                    LongnightFestivalSlot(BrialId, NpcFacing.Left),
                    FireflyFestivalSlot(BrialId, NpcFacing.Left),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[BrialId],
                        NpcFacing.Left,
                        BrialId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.StarweaverTeaHouse,
                        new GridPosition(25, 13),
                        NpcFacing.Right,
                        "village.npc.brial.weather_rain",
                        DataCatalog.RainWeatherId
                    ),
                    SeasonSlot(
                        13,
                        18,
                        PlayerLocationIds.World,
                        new GridPosition(112, 76),
                        NpcFacing.Left,
                        "village.npc.brial.season_gleamrise",
                        CalendarSystem.GleamriseSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(112, 70),
                        NpcFacing.Left,
                        "village.npc.brial.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(112, 76),
                        NpcFacing.Left,
                        "village.npc.brial.morning"
                    ),
                    TeaHouseSlot(
                        9,
                        13,
                        new GridPosition(27, 13),
                        NpcFacing.Left,
                        "village.npc.brial.tea_house"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(110, 70),
                        NpcFacing.Left,
                        "village.npc.brial.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(108, 70),
                        NpcFacing.Right,
                        "village.npc.brial.evening"
                    )
                ]
            ),
            [PavriId] = new(
                PavriId,
                "village.npc.pavri.name",
                "village.npc.pavri.role",
                "village.npc.pavri.intro",
                14,
                [
                    DataCatalog.MoonfleeceId,
                    DataCatalog.CloudleafTeaId,
                    DataCatalog.StarhoneyCustardId
                ],
                [ItemKind.AnimalProduct, ItemKind.Artisan],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    GleamriseFestivalSlot(PavriId, NpcFacing.Up),
                    LongnightFestivalSlot(PavriId, NpcFacing.Up),
                    FireflyFestivalSlot(PavriId, NpcFacing.Up),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[PavriId],
                        NpcFacing.Up,
                        PavriId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.MoonstoneWorkshop,
                        new GridPosition(13, 14),
                        NpcFacing.Right,
                        "village.npc.pavri.weather_longnight_snow",
                        DataCatalog.LongnightSnowWeatherId
                    ),
                    TeaHouseSlot(
                        9,
                        18,
                        new GridPosition(25, 13),
                        NpcFacing.Right,
                        "village.npc.pavri.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(84, 76),
                        NpcFacing.Right,
                        "village.npc.pavri.morning"
                    ),
                    WorkshopSlot(
                        9,
                        13,
                        new GridPosition(13, 12),
                        NpcFacing.Right,
                        "village.npc.pavri.workshop"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(89, 76),
                        NpcFacing.Right,
                        "village.npc.pavri.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(89, 74),
                        NpcFacing.Left,
                        "village.npc.pavri.evening"
                    )
                ]
            ),
            [RovenId] = new(
                RovenId,
                "village.npc.roven.name",
                "village.npc.roven.role",
                "village.npc.roven.intro",
                15,
                [
                    DataCatalog.MoonstonePathId,
                    DataCatalog.StarlightTorchId,
                    DataCatalog.LanternrootBrothId
                ],
                [ItemKind.Placeable, ItemKind.CookedDish],
                [ItemKind.Fertilizer, ItemKind.AnimalFeed],
                [
                    GleamriseFestivalSlot(RovenId, NpcFacing.Up),
                    LongnightFestivalSlot(RovenId, NpcFacing.Up),
                    FireflyFestivalSlot(RovenId, NpcFacing.Up),
                    FestivalSlot(
                        StarharvestMarketLayout.NpcAnchors[RovenId],
                        NpcFacing.Up,
                        RovenId
                    ),
                    WeatherSlot(
                        13,
                        18,
                        PlayerLocationIds.StarfallWatch,
                        new GridPosition(27, 12),
                        NpcFacing.Left,
                        "village.npc.roven.weather_stardust",
                        DataCatalog.StardustWindWeatherId
                    ),
                    SeasonSlot(
                        18,
                        23,
                        PlayerLocationIds.World,
                        new GridPosition(77, 76),
                        NpcFacing.Up,
                        "village.npc.roven.season_longnight",
                        CalendarSystem.LongnightSeasonId
                    ),
                    Slot(
                        9,
                        18,
                        new GridPosition(107, 76),
                        NpcFacing.Left,
                        "village.npc.roven.restday",
                        CalendarSystem.LanternrestWeekdayIndex
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(77, 52),
                        NpcFacing.Right,
                        "village.npc.roven.morning"
                    ),
                    PostSlot(
                        9,
                        13,
                        new GridPosition(27, 12),
                        NpcFacing.Left,
                        "village.npc.roven.starlight_post"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(93, 76),
                        NpcFacing.Right,
                        "village.npc.roven.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(77, 76),
                        NpcFacing.Up,
                        "village.npc.roven.evening"
                    )
                ]
            )
        };

    public static bool IsVillageCell(GridPosition cell) =>
        VillageBounds.Contains(cell);

    public static bool IsMoonlitArchiveDoor(GridPosition cell) =>
        cell == MoonlitArchiveDoorCell;

    public static bool IsMoonlitArchiveOpen(int minuteOfDay) =>
        minuteOfDay >= MoonlitArchiveOpenMinute &&
        minuteOfDay < MoonlitArchiveCloseMinute;

    public static bool IsMoonstoneWorkshopDoor(GridPosition cell) =>
        cell == MoonstoneWorkshopDoorCell;

    public static bool IsMoonstoneWorkshopOpen(int minuteOfDay) =>
        minuteOfDay >= MoonstoneWorkshopOpenMinute &&
        minuteOfDay < MoonstoneWorkshopCloseMinute;

    public static bool IsStarweaverTeaHouseDoor(GridPosition cell) =>
        cell == StarweaverTeaHouseDoorCell;

    public static bool IsStarweaverTeaHouseOpen(int minuteOfDay) =>
        minuteOfDay >= StarweaverTeaHouseOpenMinute &&
        minuteOfDay < StarweaverTeaHouseCloseMinute;

    public static bool IsTwilightEmporiumDoor(GridPosition cell) =>
        cell == TwilightEmporiumDoorCell;

    public static TwilightEmporiumAccessCheck TwilightEmporiumAccess(
        int day,
        int minuteOfDay
    ) => TwilightEmporiumSystem.CheckAccess(day, minuteOfDay);

    public static bool IsTwilightEmporiumOpen(
        int day,
        int minuteOfDay
    ) => TwilightEmporiumAccess(day, minuteOfDay).IsOpen;

    public static bool IsStarlightPostDoor(GridPosition cell) =>
        cell == StarlightPostDoorCell;

    public static bool IsStarlightPostOpen(int minuteOfDay) =>
        minuteOfDay >= StarlightPostOpenMinute &&
        minuteOfDay < StarlightPostCloseMinute;

    public static bool IsStarfallWatchDoor(GridPosition cell) =>
        cell == StarfallWatchDoorCell;

    public static bool IsStarfallWatchOpen(int minuteOfDay) =>
        minuteOfDay >= StarfallWatchOpenMinute &&
        minuteOfDay < StarfallWatchCloseMinute;

    public static bool IsVillagePath(GridPosition cell)
    {
        if (!IsVillageCell(cell))
        {
            return false;
        }

        var eastWestSpine = cell.Y is >= 78 and <= 82;
        var northSouthSpine = cell.X is >= 126 and <= 130;
        var northLane = cell.Y is >= 58 and <= 64 &&
            cell.X is >= 72 and <= 168;
        var southLane = cell.Y is >= 96 and <= 101 &&
            cell.X is >= 72 and <= 172;
        var southMarketLane = cell.Y is >= 116 and <= 121 &&
            cell.X is >= 108 and <= 186;
        var westRing = cell.X is >= 72 and <= 77 &&
            cell.Y is >= 58 and <= 101;
        var eastRing = cell.X is >= 162 and <= 167 &&
            cell.Y is >= 58 and <= 121;
        var centralPlaza = cell.X is >= 108 and <= 150 &&
            cell.Y is >= 64 and <= 92;
        var eastPromenade = cell.X is >= 146 and <= 176 &&
            cell.Y is >= 68 and <= 104;
        var cityGardenLane = cell.X is >= 90 and <= 176 &&
            cell.Y is >= 106 and <= 112;
        var archiveApproach = cell.X is >= 98 and <= 102 &&
            cell.Y is >= 56 and <= 64;
        var workshopApproach = cell.X is >= 96 and <= 100 &&
            cell.Y is >= 94 and <= 102;
        var expansionLane = cell.Y is >= 114 and <= 120 &&
            cell.X is >= 108 and <= 186;
        return eastWestSpine || northSouthSpine || northLane ||
            southLane || southMarketLane || westRing || eastRing ||
            centralPlaza || eastPromenade || cityGardenLane ||
            archiveApproach || workshopApproach || expansionLane;
    }

    public static bool IsBlocked(GridPosition cell) =>
        Landmarks
            .SelectMany(landmark => landmark.CollisionAreas)
            .Any(area => area.Contains(cell));

    public static VillageNpcState? CurrentNpc(
        string npcId,
        int day,
        int minuteOfDay
    ) => NpcScheduleSystem.ResolveCatalogNpc(
        npcId,
        day,
        minuteOfDay,
        WeatherSystem.WeatherForDay(day)
    );

    public static VillageNpcState? CurrentNpc(
        string npcId,
        int day,
        int minuteOfDay,
        string weatherId
    ) => NpcScheduleSystem.ResolveCatalogNpc(
        npcId,
        day,
        minuteOfDay,
        weatherId
    );

    private static NpcScheduleEntry Slot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.World,
        RelocateWorldScheduleCell(position),
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry ArchiveSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.MoonlitArchive,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry WorkshopSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.MoonstoneWorkshop,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry TeaHouseSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.StarweaverTeaHouse,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry EmporiumSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.TwilightEmporium,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry PostSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.StarlightPost,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry WatchSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.StarfallWatch,
        position,
        facing,
        dialogueKey,
        weekdayIndices,
        Priority: weekdayIndices.Contains(
            CalendarSystem.LanternrestWeekdayIndex
        )
            ? RestdaySchedulePriority
            : BaseSchedulePriority
    );

    private static NpcScheduleEntry WeatherSlot(
        int startHour,
        int endHour,
        string locationId,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        string weatherId
    ) => new(
        startHour * 60,
        endHour * 60,
        locationId,
        locationId == PlayerLocationIds.World
            ? RelocateWorldScheduleCell(position)
            : position,
        facing,
        dialogueKey,
        [],
        [weatherId],
        [],
        WeatherSchedulePriority
    );

    private static GridPosition RelocateWorldScheduleCell(
        GridPosition legacyCell
    )
    {
        var x = VillageCenterCell.X +
            (legacyCell.X - 96) * 2;
        var y = VillageCenterCell.Y +
            (int)MathF.Round((legacyCell.Y - 64) * 1.5f);
        var relocated = new GridPosition(x, y);
        if (CityExpansionLayout.FacilityGatewayReservedArea.Contains(
                relocated
            ))
        {
            relocated = new GridPosition(relocated.X, relocated.Y - 16);
        }
        if (IsScheduleCellAvailable(relocated))
        {
            return relocated;
        }

        for (var distance = 1; distance <= 16; distance++)
        {
            for (var offsetY = -distance;
                 offsetY <= distance;
                 offsetY++)
            {
                var offsetX = distance - Math.Abs(offsetY);
                var left = new GridPosition(
                    relocated.X - offsetX,
                    relocated.Y + offsetY
                );
                if (IsScheduleCellAvailable(left))
                {
                    return left;
                }

                if (offsetX == 0)
                {
                    continue;
                }

                var right = new GridPosition(
                    relocated.X + offsetX,
                    relocated.Y + offsetY
                );
                if (IsScheduleCellAvailable(right))
                {
                    return right;
                }
            }
        }

        return VillageCenterCell;
    }

    private static bool IsScheduleCellAvailable(GridPosition cell) =>
        IsVillageCell(cell) &&
        IsVillagePath(cell) &&
        !IsBlocked(cell) &&
        !CityExpansionLayout.IsBlocked(cell) &&
        !CityExpansionLayout.FacilityGatewayReservedArea.Contains(cell) &&
        cell != MoonlitArchiveDoorCell &&
        cell != MoonstoneWorkshopDoorCell &&
        cell != StarweaverTeaHouseDoorCell &&
        cell != TwilightEmporiumDoorCell &&
        cell != StarlightPostDoorCell &&
        cell != StarfallWatchDoorCell &&
        cell != VillageGateCell;

    private static NpcScheduleEntry SeasonSlot(
        int startHour,
        int endHour,
        string locationId,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        string seasonId
    ) => new(
        startHour * 60,
        endHour * 60,
        locationId,
        locationId == PlayerLocationIds.World
            ? RelocateWorldScheduleCell(position)
            : position,
        facing,
        dialogueKey,
        [],
        [],
        [seasonId],
        SeasonSchedulePriority
    );

    private static NpcScheduleEntry FestivalSlot(
        GridPosition position,
        NpcFacing facing,
        string npcId
    ) => new(
        // Stage festival projections at the daily simulation origin so all
        // villagers are already at their unique anchors before the 10:00
        // player gate opens. Starting later would funnel all actors through
        // one arrival cell and leave them queued at the entrance.
        GameClock.StartMinute,
        18 * 60,
        PlayerLocationIds.StarharvestMarket,
        position,
        facing,
        $"festival.starharvest.dialogue.{npcId}",
        [],
        [],
        [],
        FestivalSchedulePriority,
        [FestivalCatalog.StarharvestMarketFestivalId]
    );

    private static NpcScheduleEntry GleamriseFestivalSlot(
        string npcId,
        NpcFacing facing
    ) => new(
        GameClock.StartMinute,
        FestivalCatalog.GleamrisePlanting.CloseMinute,
        PlayerLocationIds.GleamrisePlantingFestival,
        GleamrisePlantingFestivalLayout.NpcAnchors[npcId],
        facing,
        $"festival.gleamrise.dialogue.{npcId}",
        [],
        [],
        [],
        FestivalSchedulePriority,
        [FestivalCatalog.GleamrisePlantingFestivalId]
    );

    private static NpcScheduleEntry LongnightFestivalSlot(
        string npcId,
        NpcFacing facing
    ) => new(
        GameClock.StartMinute,
        FestivalCatalog.LongnightLanternFeast.CloseMinute,
        PlayerLocationIds.LongnightLanternFeast,
        LongnightLanternFeastLayout.NpcAnchors[npcId],
        facing,
        $"festival.longnight.dialogue.{npcId}",
        [],
        [],
        [],
        FestivalSchedulePriority,
        [FestivalCatalog.LongnightLanternFeastFestivalId]
    );

    private static NpcScheduleEntry FireflyFestivalSlot(
        string npcId,
        NpcFacing facing
    ) => new(
        GameClock.StartMinute,
        FestivalCatalog.FireflyTide.CloseMinute,
        PlayerLocationIds.FireflyTide,
        FireflyTideLayout.NpcAnchors[npcId],
        facing,
        $"festival.firefly.dialogue.{npcId}",
        [],
        [],
        [],
        FestivalSchedulePriority,
        [FestivalCatalog.FireflyTideFestivalId]
    );
}

public sealed class VillageSystem
{
    private readonly WeatherSystem? _weather;
    private readonly NpcScheduleSystem _scheduleSystem = new();
    private readonly HashSet<string> _metNpcIds =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, VillageRelationshipSave>
        _relationships = new(StringComparer.Ordinal);

    public IReadOnlySet<string> MetNpcIds => _metNpcIds;
    public const int MaximumRelationshipPoints = 100;

    public event Action? Changed;

    public VillageSystem(WeatherSystem? weather = null)
    {
        _weather = weather;
    }

    public void Reset()
    {
        _scheduleSystem.ResetRuntime();
        _metNpcIds.Clear();
        _relationships.Clear();
        Changed?.Invoke();
    }

    public void Restore(VillageSave? save)
    {
        _scheduleSystem.ResetRuntime();
        var normalized = NormalizeSave(save);
        _metNpcIds.Clear();
        _relationships.Clear();
        foreach (var npcId in normalized.MetNpcIds)
        {
            _metNpcIds.Add(npcId);
        }

        foreach (var relationship in normalized.Relationships)
        {
            _relationships[relationship.NpcId] = Clone(relationship);
        }

        Changed?.Invoke();
    }

    public IReadOnlyList<VillageNpcState> CurrentNpcs(
        int day,
        int minuteOfDay,
        string locationId
    ) => CurrentNpcs(
        day,
        minuteOfDay,
        locationId,
        CurrentWeatherId(day)
    );

    public IReadOnlyList<VillageNpcState> CurrentNpcs(
        int day,
        int minuteOfDay,
        string locationId,
        string weatherId
    ) => CurrentNpcs(
        day,
        minuteOfDay,
        locationId,
        weatherId,
        null
    );

    public IReadOnlyList<VillageNpcState> CurrentNpcs(
        int day,
        int minuteOfDay,
        string locationId,
        GridPosition? playerPosition
    ) => CurrentNpcs(
        day,
        minuteOfDay,
        locationId,
        CurrentWeatherId(day),
        playerPosition
    );

    private IReadOnlyList<VillageNpcState> CurrentNpcs(
        int day,
        int minuteOfDay,
        string locationId,
        string weatherId,
        GridPosition? playerPosition
    ) => _scheduleSystem.ResolveAll(
        day,
        minuteOfDay,
        weatherId,
        playerPosition is null ? null : locationId,
        playerPosition
        )
        .Where(state => state.LocationId == locationId)
        .ToList();

    public IReadOnlyList<VillageNpcState> AllCurrentNpcs(
        int day,
        int minuteOfDay
    ) => AllCurrentNpcs(day, minuteOfDay, CurrentWeatherId(day));

    public IReadOnlyList<VillageNpcState> AllCurrentNpcs(
        int day,
        int minuteOfDay,
        string weatherId
    ) => _scheduleSystem.ResolveAll(day, minuteOfDay, weatherId);

    public IReadOnlyList<VillageNpcState> AllCurrentNpcs(
        int day,
        int minuteOfDay,
        string playerLocationId,
        GridPosition playerPosition
    ) => _scheduleSystem.ResolveAll(
        day,
        minuteOfDay,
        CurrentWeatherId(day),
        playerLocationId,
        playerPosition
    );

    public VillageNpcState? NpcAt(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId
    ) => NpcAt(
        position,
        day,
        minuteOfDay,
        locationId,
        CurrentWeatherId(day)
    );

    public VillageNpcState? NpcAt(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string weatherId
    ) => CurrentNpcs(day, minuteOfDay, locationId, weatherId)
        .FirstOrDefault(state => state.Position == position);

    public VillageNpcState? NpcAt(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        GridPosition? playerPosition
    ) => CurrentNpcs(
        day,
        minuteOfDay,
        locationId,
        playerPosition
    )
        .FirstOrDefault(state => state.Position == position);

    public VillageInteractionCheck CheckInteraction(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId
    ) => CheckInteraction(
        position,
        day,
        minuteOfDay,
        locationId,
        selectedItemId,
        CurrentWeatherId(day)
    );

    public VillageInteractionCheck CheckInteraction(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        string weatherId
    ) => CheckInteraction(
        position,
        day,
        minuteOfDay,
        locationId,
        selectedItemId,
        weatherId,
        null
    );

    public VillageInteractionCheck CheckInteraction(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        GridPosition? playerPosition
    ) => CheckInteraction(
        position,
        day,
        minuteOfDay,
        locationId,
        selectedItemId,
        CurrentWeatherId(day),
        playerPosition
    );

    private VillageInteractionCheck CheckInteraction(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        string weatherId,
        GridPosition? playerPosition
    )
    {
        if (playerPosition is { } player &&
            Math.Abs(player.X - position.X) +
                Math.Abs(player.Y - position.Y) != 1)
        {
            return new VillageInteractionCheck(
                null,
                false,
                false,
                null,
                "notice.nothing_to_interact"
            );
        }

        var state = CurrentNpcs(
                day,
                minuteOfDay,
                locationId,
                weatherId,
                playerPosition
            )
            .FirstOrDefault(value => value.Position == position);
        if (state is null)
        {
            return new VillageInteractionCheck(
                null,
                false,
                false,
                null,
                "notice.nothing_to_interact"
            );
        }

        if (selectedItemId == DataCatalog.HandId)
        {
            return new VillageInteractionCheck(
                state,
                true,
                false,
                null,
                string.Empty
            );
        }

        if (!DataCatalog.Items.TryGetValue(
                selectedItemId,
                out var selectedItem
            ) ||
            selectedItem.Kind == ItemKind.Tool)
        {
            return new VillageInteractionCheck(
                state,
                false,
                false,
                null,
                "notice.needs_hand"
            );
        }

        var relationship = Relationship(state.Definition.Id);
        if (relationship.LastGiftDay == day)
        {
            return new VillageInteractionCheck(
                state,
                false,
                true,
                null,
                "village.gift.already_today"
            );
        }

        return new VillageInteractionCheck(
            state,
            true,
            true,
            GiftReactionFor(state.Definition, selectedItemId),
            string.Empty
        );
    }

    private string CurrentWeatherId(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_weather is not null && _weather.Day == normalizedDay)
        {
            return _weather.CurrentId;
        }

        return WeatherSystem.WeatherForDay(normalizedDay);
    }

    public VillageConversation? Interact(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        Inventory inventory,
        out ActionResult result,
        GridPosition? playerPosition = null
    )
    {
        var check = CheckInteraction(
            position,
            day,
            minuteOfDay,
            locationId,
            selectedItemId,
            playerPosition
        );
        if (!check.IsAvailable || check.Npc is null)
        {
            result = ActionResult.Fail(check.FailureKey);
            return null;
        }

        var state = check.Npc;
        var firstMeeting = !_metNpcIds.Contains(state.Definition.Id);
        var relationship = Relationship(state.Definition.Id);
        var dialogueKey = state.DialogueKey;
        if (check.IsGift)
        {
            if (!inventory.Remove(selectedItemId, 1))
            {
                result = ActionResult.Fail("village.gift.missing_item");
                return null;
            }

            var reaction = check.GiftReaction ?? GiftReaction.Neutral;
            relationship.Points = Math.Clamp(
                relationship.Points + PointsFor(reaction),
                0,
                MaximumRelationshipPoints
            );
            relationship.LastGiftDay = day;
            dialogueKey = GiftDialogueKey(state.Definition.Id, reaction);
            result = ActionResult.Success(messageKey: "village.gift.given");
        }
        else
        {
            if (relationship.LastTalkDay != day)
            {
                relationship.Points = Math.Clamp(
                    relationship.Points + 2,
                    0,
                    MaximumRelationshipPoints
                );
                relationship.LastTalkDay = day;
            }

            if (firstMeeting)
            {
                dialogueKey = state.Definition.IntroductionKey;
            }
            result = ActionResult.Success(messageKey: "village.talked");
        }

        _metNpcIds.Add(state.Definition.Id);
        _relationships[state.Definition.Id] = relationship;
        Changed?.Invoke();
        return new VillageConversation(
            state.Definition.Id,
            state.Definition.NameKey,
            state.Definition.RoleKey,
            dialogueKey,
            firstMeeting,
            check.GiftReaction,
            relationship.Points,
            TierFor(relationship.Points)
        );
    }

    public VillageRelationshipSave Relationship(string npcId)
    {
        if (_relationships.TryGetValue(npcId, out var relationship))
        {
            return Clone(relationship);
        }

        return new VillageRelationshipSave { NpcId = npcId };
    }

    public VillageSave Capture() => new()
    {
        MetNpcIds = _metNpcIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList(),
        Relationships = _relationships.Values
            .Where(value => VillageCatalog.Npcs.ContainsKey(value.NpcId))
            .OrderBy(value => value.NpcId, StringComparer.Ordinal)
            .Select(Clone)
            .ToList()
    };

    public static VillageSave NormalizeSave(VillageSave? save)
    {
        var metNpcIds = (save?.MetNpcIds ?? [])
            .Where(VillageCatalog.Npcs.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var relationships = new Dictionary<string, VillageRelationshipSave>(
            StringComparer.Ordinal
        );
        foreach (var saved in save?.Relationships ?? [])
        {
            if (!VillageCatalog.Npcs.ContainsKey(saved.NpcId))
            {
                continue;
            }

            relationships[saved.NpcId] = new VillageRelationshipSave
            {
                NpcId = saved.NpcId,
                Points = Math.Clamp(
                    saved.Points,
                    0,
                    MaximumRelationshipPoints
                ),
                LastTalkDay = Math.Max(0, saved.LastTalkDay),
                LastGiftDay = Math.Max(0, saved.LastGiftDay)
            };
        }

        foreach (var npcId in metNpcIds)
        {
            relationships.TryAdd(
                npcId,
                new VillageRelationshipSave { NpcId = npcId }
            );
        }

        return new VillageSave
        {
            MetNpcIds = metNpcIds,
            Relationships = relationships.Values
                .OrderBy(value => value.NpcId, StringComparer.Ordinal)
                .Select(Clone)
                .ToList()
        };
    }

    public static RelationshipTier TierFor(int points)
    {
        if (points >= 60)
        {
            return RelationshipTier.KindredLight;
        }

        return points >= 25
            ? RelationshipTier.TrustedFriend
            : RelationshipTier.NewAcquaintance;
    }

    private static GiftReaction GiftReactionFor(
        VillageNpcDefinition definition,
        string itemId
    )
    {
        var baseItemId = DataCatalog.BaseItemId(itemId);
        if (definition.LovedGiftIds.Contains(
                baseItemId,
                StringComparer.Ordinal
            ) ||
            definition.LovedGiftIds.Contains(
                itemId,
                StringComparer.Ordinal
            ))
        {
            return GiftReaction.Loved;
        }

        var kind = DataCatalog.Item(itemId).Kind;
        if (definition.DislikedGiftKinds.Contains(kind))
        {
            return GiftReaction.Disliked;
        }

        return definition.LikedGiftKinds.Contains(kind)
            ? GiftReaction.Liked
            : GiftReaction.Neutral;
    }

    private static int PointsFor(GiftReaction reaction) => reaction switch
    {
        GiftReaction.Loved => 12,
        GiftReaction.Liked => 7,
        GiftReaction.Neutral => 3,
        GiftReaction.Disliked => 0,
        _ => 0
    };

    private static string GiftDialogueKey(
        string npcId,
        GiftReaction reaction
    ) => $"village.npc.{npcId}.gift.{reaction.ToString().ToLowerInvariant()}";

    private static VillageRelationshipSave Clone(
        VillageRelationshipSave relationship
    ) => new()
    {
        NpcId = relationship.NpcId,
        Points = relationship.Points,
        LastTalkDay = relationship.LastTalkDay,
        LastGiftDay = relationship.LastGiftDay
    };
}
