namespace Luminfield.Core;

public enum WorldNavigationGuideKind
{
    RoadEdge,
    RegionThreshold,
    LandmarkApproach
}

public sealed record WorldNavigationGuide(
    string Id,
    WorldBiome Region,
    GridPosition Position,
    int AtlasIndex,
    WorldNavigationGuideKind Kind
);

public sealed record WorldNavigationRouteContract(
    string Id,
    WorldBiome FromRegion,
    WorldBiome ToRegion,
    GridPosition Start,
    GridPosition End,
    IReadOnlyList<string> GuideIds,
    int MaximumUnguidedDistance
);

public static class WorldNavigationGuideCatalog
{
    public const int InternalViewportWidthPixels = 640;
    public const int InternalViewportHeightPixels = 360;
    public const int TileSizePixels = 16;
    public const int CameraVisibleColumns =
        InternalViewportWidthPixels / TileSizePixels;
    public const int CameraVisibleRows =
        InternalViewportHeightPixels / TileSizePixels;
    public const int HudAndTurnMarginTiles = 4;
    public const int MaximumCameraDiscoveryDistance =
        CameraVisibleRows - HudAndTurnMarginTiles;
    public const int MaximumUnguidedRouteDistance =
        MaximumCameraDiscoveryDistance;

    public static readonly IReadOnlyList<WorldNavigationGuide> Guides =
    [
        Guide(
            "home_farm_gate_lantern",
            WorldBiome.Home,
            new GridPosition(19, 48),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "home_beginner_arch_west_bloom",
            WorldBiome.Home,
            new GridPosition(48, 48),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "home_beginner_link_mid_bloom",
            WorldBiome.Home,
            new GridPosition(34, 48),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "home_beginner_arch_east_bloom",
            WorldBiome.Home,
            new GridPosition(60, 48),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "home_east_trail_bloom",
            WorldBiome.Home,
            new GridPosition(62, 51),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_beginner_link_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(64, 51),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_northwest_link_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(72, 58),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_west_ring_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(72, 80),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_west_link_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(72, 68),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_west_mid_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(84, 80),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_west_spine_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(112, 80),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_woods_gate_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(60, 80),
            5,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "woods_branch_threshold_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 80),
            5,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "woods_north_loop_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 96),
            5,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "woods_grove_lantern",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 112),
            8,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "woods_mid_loop_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 126),
            5,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "woods_south_loop_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 142),
            5,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "woods_south_mid_mushroom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 155),
            5,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "woods_deep_loop_bloom",
            WorldBiome.WhisperingWoods,
            new GridPosition(42, 168),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "meadow_west_bloom",
            WorldBiome.StarfallMeadow,
            new GridPosition(72, 24),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "meadow_circle_bloom",
            WorldBiome.StarfallMeadow,
            new GridPosition(136, 24),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "meadow_east_bloom",
            WorldBiome.StarfallMeadow,
            new GridPosition(184, 24),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "meadow_crossroad_lantern",
            WorldBiome.StarfallMeadow,
            new GridPosition(128, 25),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "meadow_south_gate_bloom",
            WorldBiome.StarfallMeadow,
            new GridPosition(128, 31),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_west_gate_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(96, 80),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "village_crossroad_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(128, 80),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "village_north_spine_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(128, 64),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_upper_spine_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(128, 48),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_east_spine_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(160, 80),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_east_mid_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(144, 80),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_civic_garden_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(132, 56),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "village_facilities_gateway_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(128, 116),
            8,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "village_east_gate_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(176, 80),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "village_east_outskirts_bloom",
            WorldBiome.LumenVillage,
            new GridPosition(186, 80),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "village_south_spine_lantern",
            WorldBiome.LumenVillage,
            new GridPosition(128, 98),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "southern_spine_north_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(128, 130),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "southern_spine_crossroads_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(128, 142),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "crystal_north_branch_shrub",
            WorldBiome.CrystalVale,
            new GridPosition(84, 132),
            4,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "crystal_grotto_turn_lantern",
            WorldBiome.CrystalVale,
            new GridPosition(80, 142),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "crystal_well_approach_lantern",
            WorldBiome.CrystalVale,
            new GridPosition(84, 154),
            8,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "crystal_east_road_bloom",
            WorldBiome.CrystalVale,
            new GridPosition(116, 142),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "crystal_mid_road_shrub",
            WorldBiome.CrystalVale,
            new GridPosition(94, 142),
            4,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "crystal_south_road_bloom",
            WorldBiome.CrystalVale,
            new GridPosition(108, 142),
            13,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "wetland_west_causeway_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(196, 80),
            14,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "wetland_islet_causeway_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(214, 80),
            14,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "wetland_road_cross_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(226, 80),
            14,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "wetland_north_road_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(226, 68),
            14,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "wetland_islet_approach_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(224, 56),
            14,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "wetland_monolith_lantern",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(226, 48),
            8,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "wetland_south_turn_reeds",
            WorldBiome.MoonwaterWetlands,
            new GridPosition(226, 92),
            14,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "ruins_west_gate_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(144, 158),
            8,
            WorldNavigationGuideKind.RegionThreshold
        ),
        Guide(
            "ruins_starlight_bloom",
            WorldBiome.StarfallRuins,
            new GridPosition(160, 160),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "ruins_colonnade_bloom",
            WorldBiome.StarfallRuins,
            new GridPosition(184, 160),
            13,
            WorldNavigationGuideKind.LandmarkApproach
        ),
        Guide(
            "ruins_north_branch_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(128, 154),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "ruins_west_approach_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(136, 158),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "ruins_eastern_run_lantern",
            WorldBiome.StarfallRuins,
            new GridPosition(224, 158),
            8,
            WorldNavigationGuideKind.RoadEdge
        ),
        Guide(
            "ruins_southern_road_bloom",
            WorldBiome.StarfallRuins,
            new GridPosition(202, 142),
            13,
            WorldNavigationGuideKind.RoadEdge
        )
    ];

    public static readonly IReadOnlyList<WorldNavigationRouteContract>
        RouteContracts =
        [
            Route(
                "home_to_lumen_village",
                WorldBiome.Home,
                WorldBiome.LumenVillage,
                new GridPosition(19, 30),
                VillageCatalog.VillageCenterCell,
                [
                    "nav_home_farm_gate_lantern",
                    "nav_home_beginner_link_mid_bloom",
                    "nav_home_beginner_arch_west_bloom",
                    "nav_home_beginner_arch_east_bloom",
                    "nav_home_east_trail_bloom",
                    "nav_village_beginner_link_bloom",
                    "nav_village_northwest_link_bloom",
                    "nav_village_west_link_lantern",
                    "nav_village_west_ring_lantern",
                    "nav_village_west_mid_lantern",
                    "nav_village_west_gate_lantern",
                    "nav_village_west_spine_lantern",
                    "nav_village_crossroad_lantern"
                ]
            ),
            Route(
                "lumen_village_to_whispering_woods",
                WorldBiome.LumenVillage,
                WorldBiome.WhisperingWoods,
                VillageCatalog.VillageCenterCell,
                new GridPosition(42, 168),
                [
                    "nav_village_crossroad_lantern",
                    "nav_village_west_spine_lantern",
                    "nav_village_west_gate_lantern",
                    "nav_village_west_mid_lantern",
                    "nav_village_west_ring_lantern",
                    "nav_village_woods_gate_mushroom",
                    "nav_woods_branch_threshold_mushroom",
                    "nav_woods_north_loop_mushroom",
                    "nav_woods_grove_lantern",
                    "nav_woods_mid_loop_mushroom",
                    "nav_woods_south_loop_mushroom",
                    "nav_woods_south_mid_mushroom",
                    "nav_woods_deep_loop_bloom"
                ]
            ),
            Route(
                "lumen_village_to_starfall_meadow",
                WorldBiome.LumenVillage,
                WorldBiome.StarfallMeadow,
                VillageCatalog.VillageCenterCell,
                new GridPosition(136, 24),
                [
                    "nav_village_crossroad_lantern",
                    "nav_village_north_spine_lantern",
                    "nav_village_upper_spine_lantern",
                    "nav_meadow_south_gate_bloom",
                    "nav_meadow_crossroad_lantern",
                    "nav_meadow_circle_bloom"
                ]
            ),
            Route(
                "lumen_village_to_crystal_vale",
                WorldBiome.LumenVillage,
                WorldBiome.CrystalVale,
                VillageCatalog.VillageCenterCell,
                new GridPosition(84, 154),
                [
                    "nav_village_crossroad_lantern",
                    "nav_village_south_spine_lantern",
                    "nav_village_facilities_gateway_lantern",
                    "nav_southern_spine_north_lantern",
                    "nav_southern_spine_crossroads_lantern",
                    "nav_crystal_east_road_bloom",
                    "nav_crystal_south_road_bloom",
                    "nav_crystal_mid_road_shrub",
                    "nav_crystal_grotto_turn_lantern",
                    "nav_crystal_well_approach_lantern"
                ]
            ),
            Route(
                "lumen_village_to_moonwater_wetlands",
                WorldBiome.LumenVillage,
                WorldBiome.MoonwaterWetlands,
                VillageCatalog.VillageCenterCell,
                new GridPosition(226, 48),
                [
                    "nav_village_crossroad_lantern",
                    "nav_village_east_mid_bloom",
                    "nav_village_east_spine_bloom",
                    "nav_village_east_gate_lantern",
                    "nav_village_east_outskirts_bloom",
                    "nav_wetland_west_causeway_reeds",
                    "nav_wetland_islet_causeway_reeds",
                    "nav_wetland_road_cross_reeds",
                    "nav_wetland_north_road_reeds",
                    "nav_wetland_islet_approach_reeds",
                    "nav_wetland_monolith_lantern"
                ]
            ),
            Route(
                "lumen_village_to_starfall_ruins",
                WorldBiome.LumenVillage,
                WorldBiome.StarfallRuins,
                VillageCatalog.VillageCenterCell,
                StarfallRuinsTrialLayout.WorldReturnCell,
                [
                    "nav_village_crossroad_lantern",
                    "nav_village_south_spine_lantern",
                    "nav_village_facilities_gateway_lantern",
                    "nav_southern_spine_north_lantern",
                    "nav_southern_spine_crossroads_lantern",
                    "nav_ruins_north_branch_lantern",
                    "nav_ruins_west_approach_lantern",
                    "nav_ruins_west_gate_lantern",
                    "nav_ruins_starlight_bloom"
                ]
            )
        ];

    public static IEnumerable<WorldNavigationGuide> ForChunk(
        ChunkPosition chunk
    ) => Guides.Where(guide =>
        WorldDefinition.GetChunk(guide.Position) == chunk
    );

    private static WorldNavigationGuide Guide(
        string suffix,
        WorldBiome region,
        GridPosition position,
        int atlasIndex,
        WorldNavigationGuideKind kind
    ) => new(
        $"nav_{suffix}",
        region,
        position,
        atlasIndex,
        kind
    );

    private static WorldNavigationRouteContract Route(
        string id,
        WorldBiome fromRegion,
        WorldBiome toRegion,
        GridPosition start,
        GridPosition end,
        IReadOnlyList<string> guideIds
    ) => new(
        $"route_{id}",
        fromRegion,
        toRegion,
        start,
        end,
        guideIds,
        MaximumUnguidedRouteDistance
    );
}
