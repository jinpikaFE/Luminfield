using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRouteGuidanceHudTests
{
    [Fact]
    public void MultiSegmentTextKeepsFinalDestinationAndCurrentLegVisible()
    {
        var selection = new WorldNavigationRouteSelection();
        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            WorldBiome.WhisperingWoods
        ));

        var firstRoute = WorldNavigationRouteSelection.Routes.Single(route =>
            route.RouteId == selection.SelectedRouteId
        );
        var first = WorldNavigationRouteGuidanceHud.CreateText(
            selection.Project(firstRoute.Start),
            Locale()
        );

        Assert.Contains("To Whispering Woods · leg 1/2", first);
        Assert.Contains("Via Lumen Village", first);

        Assert.True(selection.TryAdvanceAt(firstRoute.End));
        var second = WorldNavigationRouteGuidanceHud.CreateText(
            selection.Project(firstRoute.End),
            Locale()
        );

        Assert.Contains("To Whispering Woods · leg 2/2", second);
        Assert.Contains("Via Whispering Woods", second);
    }

    [Fact]
    public void FinalArrivalUsesJourneyDestination()
    {
        var selection = new WorldNavigationRouteSelection();
        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            WorldBiome.CrystalVale
        ));
        var firstRoute = Route(selection.SelectedRouteId);
        Assert.True(selection.TryAdvanceAt(firstRoute.End));
        var lastRoute = Route(selection.SelectedRouteId);

        var text = WorldNavigationRouteGuidanceHud.CreateText(
            selection.Project(lastRoute.End),
            Locale()
        );

        Assert.Equal("Arrived at Crystal Vale", text);
    }

    [Fact]
    public void JourneyTargetNameOverridesRegionAndHandsOffToFinalApproach()
    {
        var selection = new WorldNavigationRouteSelection();
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:woods_lantern",
            WorldBiome.WhisperingWoods,
            WorldDefinition.WoodlandStarlightCell,
            WorldNavigationDestinationKind.Landmark,
            "world.landmark.woods_lantern"
        );
        Assert.True(selection.SelectDestination(WorldBiome.Home, target));
        var firstRoute = Route(selection.SelectedRouteId);
        Assert.True(selection.TryAdvanceAt(firstRoute.End));
        var lastRoute = Route(selection.SelectedRouteId);

        var regional = WorldNavigationRouteGuidanceHud.CreateText(
            selection.Project(firstRoute.End),
            Locale()
        );
        Assert.True(selection.TryAdvanceAt(lastRoute.End));
        var finalApproachProjection = selection.Project(lastRoute.End);
        var finalApproach = WorldNavigationRouteGuidanceHud.CreateText(
            finalApproachProjection,
            Locale()
        );

        Assert.Contains("To Woodland Watch Starlight · leg 2/3", regional);
        Assert.StartsWith(
            "To Woodland Watch Starlight · final approach",
            finalApproach
        );
        Assert.True(finalApproachProjection.IsFinalTargetSegment);
    }

    [Fact]
    public void AdjacentToTargetCountsAsArrivedAtTarget()
    {
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );
        var projection = new WorldNavigationRouteSelectionProjection(
            WorldNavigationRouteSelection.Routes,
            "route_home_to_lumen_village",
            new WorldNavigationRouteProgress(
                "route_home_to_lumen_village",
                RouteExists: true,
                WorldBiome.Home,
                new GridPosition(
                    FarmLayout.StarlightMailboxCell.X,
                    FarmLayout.StarlightMailboxCell.Y + 1
                ),
                NearestPathIndex: 0,
                new GridPosition(0, 0),
                DistanceFromRoute: 0,
                NextTarget: null,
                WorldNavigationRouteDirection.None,
                WorldNavigationRouteDirection.None,
                RemainingSteps: 0,
                IsArrived: false
            )
        )
        {
            JourneyDestinationRegion = WorldBiome.Home,
            JourneyTarget = target,
            CurrentSegmentNumber = 1,
            SegmentCount = 1
        };

        var text = WorldNavigationRouteGuidanceHud.CreateText(
            projection,
            Locale()
        );

        Assert.Equal("Arrived at Starlight mail", text);
    }

    [Fact]
    public void WorldDoorDoesNotCountAsArrivedBeforeLocationHandoff()
    {
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:archive_liora",
            WorldBiome.LumenVillage,
            VillageCatalog.MoonlitArchiveDoorCell,
            WorldNavigationDestinationKind.Character,
            "village.npc.liora.name",
            PlayerLocationIds.MoonlitArchive,
            new GridPosition(20, 12)
        );
        var projection = new WorldNavigationRouteSelectionProjection(
            WorldNavigationRouteSelection.Routes,
            "target:test:archive_liora",
            new WorldNavigationRouteProgress(
                "target:test:archive_liora",
                RouteExists: true,
                WorldBiome.LumenVillage,
                new GridPosition(
                    VillageCatalog.MoonlitArchiveDoorCell.X,
                    VillageCatalog.MoonlitArchiveDoorCell.Y + 1
                ),
                NearestPathIndex: 0,
                new GridPosition(
                    VillageCatalog.MoonlitArchiveDoorCell.X,
                    VillageCatalog.MoonlitArchiveDoorCell.Y + 1
                ),
                DistanceFromRoute: 0,
                NextTarget: null,
                WorldNavigationRouteDirection.None,
                WorldNavigationRouteDirection.None,
                RemainingSteps: 0,
                IsArrived: true
            )
        )
        {
            JourneyDestinationRegion = WorldBiome.LumenVillage,
            JourneyTarget = target,
            CurrentSegmentNumber = 1,
            SegmentCount = 1,
            IsFinalTargetSegment = true,
            PlayerLocationId = PlayerLocationIds.World,
            ActiveTargetCell = VillageCatalog.MoonlitArchiveDoorCell,
            RequiresLocationHandoff = true,
            IsLocationTargetSegment = false
        };

        var text = WorldNavigationRouteGuidanceHud.CreateText(
            projection,
            Locale()
        );

        Assert.Equal(
            "At the entrance · go inside to continue to Liora",
            text
        );
    }

    [Fact]
    public void LocationTargetCountsAsArrivedAfterHandoff()
    {
        var targetCell = new GridPosition(20, 12);
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:archive_liora",
            WorldBiome.LumenVillage,
            VillageCatalog.MoonlitArchiveDoorCell,
            WorldNavigationDestinationKind.Character,
            "village.npc.liora.name",
            PlayerLocationIds.MoonlitArchive,
            targetCell
        );
        var projection = new WorldNavigationRouteSelectionProjection(
            WorldNavigationRouteSelection.Routes,
            "target:test:archive_liora",
            new WorldNavigationRouteProgress(
                "target:test:archive_liora",
                RouteExists: true,
                WorldBiome.LumenVillage,
                new GridPosition(targetCell.X, targetCell.Y + 1),
                NearestPathIndex: 0,
                new GridPosition(targetCell.X, targetCell.Y + 1),
                DistanceFromRoute: 0,
                NextTarget: null,
                WorldNavigationRouteDirection.None,
                WorldNavigationRouteDirection.None,
                RemainingSteps: 0,
                IsArrived: true
            )
        )
        {
            JourneyDestinationRegion = WorldBiome.LumenVillage,
            JourneyTarget = target,
            CurrentSegmentNumber = 1,
            SegmentCount = 1,
            IsFinalTargetSegment = true,
            PlayerLocationId = PlayerLocationIds.MoonlitArchive,
            ActiveTargetCell = targetCell,
            RequiresLocationHandoff = true,
            IsLocationTargetSegment = true
        };

        var text = WorldNavigationRouteGuidanceHud.CreateText(
            projection,
            Locale()
        );

        Assert.Equal("Arrived at Liora", text);
    }

    [Fact]
    public void MultiSegmentOffRouteTextKeepsLegContext()
    {
        var projection = new WorldNavigationRouteSelectionProjection(
            WorldNavigationRouteSelection.Routes,
            "route_home_to_lumen_village",
            new WorldNavigationRouteProgress(
                "route_home_to_lumen_village",
                RouteExists: true,
                WorldBiome.LumenVillage,
                new GridPosition(3, 4),
                NearestPathIndex: 2,
                new GridPosition(3, 3),
                DistanceFromRoute: 1,
                NextTarget: null,
                WorldNavigationRouteDirection.East,
                WorldNavigationRouteDirection.North,
                RemainingSteps: 12,
                IsArrived: false
            )
        )
        {
            JourneyDestinationRegion = WorldBiome.StarfallRuins,
            CurrentSegmentNumber = 1,
            SegmentCount = 2
        };

        var text = WorldNavigationRouteGuidanceHud.CreateText(
            projection,
            Locale()
        );

        Assert.Equal(
            "To Starfall Ruins · leg 1/2\n1 tiles off route · head north",
            text
        );
    }

    [Fact]
    public void RequiredKeysCoverSingleAndMultiSegmentStates()
    {
        Assert.Equal(
            8,
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys.Count
        );
        Assert.Contains(
            "route_guidance.hud.journey_progress",
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys
        );
        Assert.Contains(
            "route_guidance.hud.journey_off_route",
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys
        );
        Assert.Contains(
            "route_guidance.hud.target_progress",
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys
        );
        Assert.Contains(
            "route_guidance.hud.target_off_route",
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys
        );
        Assert.Contains(
            "route_guidance.hud.enter_location",
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys
        );
    }

    private static WorldNavigationRouteOption Route(string? routeId) =>
        WorldNavigationRouteSelection.Routes.Single(route =>
            route.RouteId == routeId
        );

    private static LocaleService Locale()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["route_guidance.hud.progress"] =
                "To {0} · {1} · about {2} tiles",
            ["route_guidance.hud.off_route"] =
                "{0} tiles off route · head {1}",
            ["route_guidance.hud.arrived"] = "Arrived at {0}",
            ["route_guidance.hud.journey_progress"] =
                "To {0} · leg {1}/{2}\nVia {3} · {4} · about {5} tiles",
            ["route_guidance.hud.journey_off_route"] =
                "To {0} · leg {1}/{2}\n{3} tiles off route · head {4}",
            ["route_guidance.hud.target_progress"] =
                "To {0} · final approach\nHead {1} · about {2} tiles",
            ["route_guidance.hud.target_off_route"] =
                "To {0} · final approach\n{1} tiles off route · head {2}",
            ["route_guidance.hud.enter_location"] =
                "At the entrance · go inside to continue to {0}",
            ["route_guidance.direction.none"] = "destination",
            ["route_guidance.direction.north"] = "north",
            ["route_guidance.direction.south"] = "south",
            ["route_guidance.direction.west"] = "west",
            ["route_guidance.direction.east"] = "east",
            ["route_guidance.region.home"] = "Home",
            ["route_guidance.region.village"] = "Lumen Village",
            ["route_guidance.region.woods"] = "Whispering Woods",
            ["route_guidance.region.meadow"] = "Starfall Meadow",
            ["route_guidance.region.crystal"] = "Crystal Vale",
            ["route_guidance.region.wetlands"] = "Moonwater Wetlands",
            ["route_guidance.region.ruins"] = "Starfall Ruins",
            ["world.landmark.woods_lantern"] = "Woodland Watch Starlight",
            ["morning.mail.title"] = "Starlight mail",
            ["village.npc.liora.name"] = "Liora"
        };
        var locale = new LocaleService();
        locale.LoadJson(
            LocaleService.English,
            JsonSerializer.Serialize(values)
        );
        locale.SetLocale(LocaleService.English);
        return locale;
    }
}
