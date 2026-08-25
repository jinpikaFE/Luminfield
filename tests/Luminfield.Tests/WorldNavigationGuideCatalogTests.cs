using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationGuideCatalogTests
{
    private static readonly GridPosition[] Directions =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    [Fact]
    public void GuidesUseStableIdsAndExistingPropAtlasCells()
    {
        var guides = WorldNavigationGuideCatalog.Guides;

        Assert.True(guides.Count >= 55);
        Assert.Equal(
            guides.Count,
            guides.Select(guide => guide.Id).Distinct().Count()
        );

        foreach (var guide in guides)
        {
            Assert.StartsWith("nav_", guide.Id);
            Assert.InRange(
                guide.AtlasIndex,
                0,
                WorldSeasonVisualCatalog.PropAtlasEntryCount - 1
            );
            Assert.True(WorldDefinition.IsInBounds(guide.Position), guide.Id);
            Assert.Equal(WorldDefinition.GetBiome(guide.Position), guide.Region);
            Assert.True(WorldDefinition.IsPath(guide.Position), guide.Id);
            Assert.False(WorldDefinition.IsWater(guide.Position), guide.Id);
            Assert.False(WorldDefinition.IsBlocked(guide.Position), guide.Id);
            Assert.Equal(
                WorldResourceKind.None,
                WorldDefinition.ResourceAt(guide.Position)
            );
            Assert.True(
                WorldDefinition.PropAtlasIndex(guide.Position) < 0,
                guide.Id
            );
            Assert.Null(WorldDefinition.LandmarkAt(guide.Position));
        }
    }

    [Fact]
    public void GuidesCoverRoadsRegionThresholdsAndScenicApproaches()
    {
        foreach (var region in Enum.GetValues<WorldBiome>())
        {
            Assert.Contains(
                WorldNavigationGuideCatalog.Guides,
                guide => guide.Region == region
            );
        }

        foreach (var kind in Enum.GetValues<WorldNavigationGuideKind>())
        {
            Assert.Contains(
                WorldNavigationGuideCatalog.Guides,
                guide => guide.Kind == kind
            );
        }

        foreach (var scenic in WorldDefinition.ScenicLandmarks)
        {
            Assert.Contains(
                WorldNavigationGuideCatalog.Guides,
                guide =>
                    guide.Kind == WorldNavigationGuideKind.LandmarkApproach &&
                    ManhattanDistance(guide.Position, scenic.Position) <= 30
            );
        }
    }

    [Fact]
    public void RouteContractsCoverVillageConnectionsToAllOuterRegions()
    {
        var routes = WorldNavigationGuideCatalog.RouteContracts;
        var routeIds = routes
            .Select(route => route.Id)
            .ToHashSet(StringComparer.Ordinal);
        var outerRegions = routes
            .Select(route =>
                route.FromRegion == WorldBiome.LumenVillage
                    ? route.ToRegion
                    : route.FromRegion)
            .ToHashSet();
        var expectedRouteIds = new HashSet<string>(
            StringComparer.Ordinal
        )
        {
            "route_home_to_lumen_village",
            "route_lumen_village_to_whispering_woods",
            "route_lumen_village_to_starfall_meadow",
            "route_lumen_village_to_crystal_vale",
            "route_lumen_village_to_moonwater_wetlands",
            "route_lumen_village_to_starfall_ruins"
        };

        Assert.Equal(6, routes.Count);
        Assert.True(expectedRouteIds.SetEquals(routeIds));
        var expectedOuterRegions = Enum.GetValues<WorldBiome>()
            .Where(region => region != WorldBiome.LumenVillage)
            .ToHashSet();
        Assert.True(expectedOuterRegions.SetEquals(outerRegions));
        Assert.Contains(
            routes,
            route =>
                route.FromRegion == WorldBiome.Home &&
                route.ToRegion == WorldBiome.LumenVillage
        );
    }

    [Fact]
    public void RouteContractsUseKnownGuidesAndWalkableEndpoints()
    {
        var guides = WorldNavigationGuideCatalog.Guides
            .ToDictionary(guide => guide.Id, StringComparer.Ordinal);

        foreach (var route in WorldNavigationGuideCatalog.RouteContracts)
        {
            Assert.StartsWith("route_", route.Id);
            Assert.NotEmpty(route.GuideIds);
            Assert.Equal(route.GuideIds.Count, route.GuideIds.Distinct().Count());
            Assert.Equal(route.FromRegion, WorldDefinition.GetBiome(route.Start));
            Assert.Equal(route.ToRegion, WorldDefinition.GetBiome(route.End));
            Assert.True(IsWalkable(route.Start), route.Id);
            Assert.True(IsWalkable(route.End), route.Id);

            foreach (var guideId in route.GuideIds)
            {
                Assert.True(guides.TryGetValue(guideId, out var guide), guideId);
                Assert.True(WorldDefinition.IsPath(guide.Position), guideId);
                Assert.True(IsWalkable(guide.Position), guideId);
            }
        }
    }

    [Fact]
    public void RouteContractsKeepUnguidedWalkableGapsControlled()
    {
        var guides = WorldNavigationGuideCatalog.Guides
            .ToDictionary(guide => guide.Id, StringComparer.Ordinal);

        foreach (var route in WorldNavigationGuideCatalog.RouteContracts)
        {
            var routePositions = new[]
                {
                    route.Start
                }
                .Concat(route.GuideIds.Select(id => guides[id].Position))
                .Concat([route.End])
                .ToArray();

            Assert.Equal(
                WorldNavigationGuideCatalog.MaximumUnguidedRouteDistance,
                route.MaximumUnguidedDistance
            );

            foreach (var pair in routePositions.Zip(routePositions.Skip(1)))
            {
                var distance = ShortestWalkableDistance(
                    pair.First,
                    pair.Second,
                    route.MaximumUnguidedDistance
                );
                Assert.InRange(
                    distance,
                    0,
                    route.MaximumUnguidedDistance
                );
            }
        }
    }

    [Fact]
    public void RouteSegmentsStayWithinCameraDiscoveryBudget()
    {
        Assert.Equal(640, WorldNavigationGuideCatalog.InternalViewportWidthPixels);
        Assert.Equal(360, WorldNavigationGuideCatalog.InternalViewportHeightPixels);
        Assert.Equal(16, WorldNavigationGuideCatalog.TileSizePixels);
        Assert.Equal(40, WorldNavigationGuideCatalog.CameraVisibleColumns);
        Assert.Equal(22, WorldNavigationGuideCatalog.CameraVisibleRows);
        Assert.Equal(18, WorldNavigationGuideCatalog.MaximumCameraDiscoveryDistance);
        Assert.Equal(
            WorldNavigationGuideCatalog.MaximumCameraDiscoveryDistance,
            WorldNavigationGuideCatalog.MaximumUnguidedRouteDistance
        );
        Assert.True(
            WorldNavigationGuideCatalog.MaximumCameraDiscoveryDistance <=
            WorldNavigationGuideCatalog.CameraVisibleColumns / 2
        );
        Assert.Equal(
            WorldNavigationGuideCatalog.MaximumCameraDiscoveryDistance,
            WorldNavigationGuideCatalog.CameraVisibleRows -
            WorldNavigationGuideCatalog.HudAndTurnMarginTiles
        );

        var guides = WorldNavigationGuideCatalog.Guides
            .ToDictionary(guide => guide.Id, StringComparer.Ordinal);

        foreach (var route in WorldNavigationGuideCatalog.RouteContracts)
        {
            var routePositions = new[]
                {
                    route.Start
                }
                .Concat(route.GuideIds.Select(id => guides[id].Position))
                .Concat([route.End])
                .ToArray();

            foreach (var pair in routePositions.Zip(routePositions.Skip(1)))
            {
                var distance = ManhattanDistance(pair.First, pair.Second);
                Assert.InRange(
                    distance,
                    0,
                    WorldNavigationGuideCatalog.MaximumCameraDiscoveryDistance
                );
            }
        }
    }

    [Fact]
    public void FarmGateCanReachEveryNavigationGuide()
    {
        var reachable = ReachableFrom(new GridPosition(19, 30));

        foreach (var guide in WorldNavigationGuideCatalog.Guides)
        {
            Assert.True(reachable.Contains(guide.Position), guide.Id);
        }
    }

    [Fact]
    public void ChunkLookupMatchesGuidePositions()
    {
        for (var y = 0; y < WorldDefinition.ChunkRows; y++)
        {
            for (var x = 0; x < WorldDefinition.ChunkColumns; x++)
            {
                var chunk = new ChunkPosition(x, y);
                var expected = WorldNavigationGuideCatalog.Guides
                    .Where(guide =>
                        WorldDefinition.GetChunk(guide.Position) == chunk
                    )
                    .Select(guide => guide.Id);
                var actual = WorldNavigationGuideCatalog.ForChunk(chunk)
                    .Select(guide => guide.Id);

                Assert.Equal(expected, actual);
            }
        }
    }

    private static HashSet<GridPosition> ReachableFrom(GridPosition start)
    {
        var visited = new HashSet<GridPosition> { start };
        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            foreach (var direction in Directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (!WorldDefinition.IsInBounds(next) ||
                    visited.Contains(next) ||
                    WorldDefinition.IsBlocked(next))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited;
    }

    private static int ShortestWalkableDistance(
        GridPosition start,
        GridPosition end,
        int limit
    )
    {
        var visited = new HashSet<GridPosition> { start };
        var queue = new Queue<(GridPosition Position, int Distance)>();
        queue.Enqueue((start, 0));

        while (queue.TryDequeue(out var current))
        {
            if (current.Position == end)
            {
                return current.Distance;
            }

            if (current.Distance >= limit)
            {
                continue;
            }

            foreach (var direction in Directions)
            {
                var next = new GridPosition(
                    current.Position.X + direction.X,
                    current.Position.Y + direction.Y
                );
                if (!WorldDefinition.IsInBounds(next) ||
                    visited.Contains(next) ||
                    !IsWalkable(next))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue((next, current.Distance + 1));
            }
        }

        return int.MaxValue;
    }

    private static bool IsWalkable(GridPosition cell) =>
        WorldDefinition.IsInBounds(cell) &&
        !WorldDefinition.IsBlocked(cell);

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
}
