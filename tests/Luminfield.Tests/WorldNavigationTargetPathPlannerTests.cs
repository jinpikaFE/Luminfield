using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationTargetPathPlannerTests
{
    [Fact]
    public void HomesteadPathEndsBesideMailboxAndAvoidsStaticObstacles()
    {
        var start = new GridPosition(
            FarmLayout.CottageDoorCell.X,
            FarmLayout.CottageDoorCell.Y + 1
        );
        var destination = WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );

        Assert.True(WorldNavigationTargetPathPlanner.TryCreate(
            start,
            destination,
            out var path
        ));
        Assert.NotNull(path);
        Assert.Equal(start, path.Start);
        Assert.Equal(start, path.Path[0]);
        Assert.Equal(path.ArrivalCell, path.Path[^1]);
        Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
            destination,
            path.ArrivalCell
        ));
        Assert.DoesNotContain(FarmLayout.StarlightMailboxCell, path.Path);
        Assert.All(path.Path, cell =>
            Assert.True(WorldNavigationTargetPathPlanner.IsWalkable(cell))
        );
        AssertWalksOneCellAtATime(path.Path);
    }

    [Fact]
    public void RuntimeWalkabilityCanRouteAroundTemporaryBlockers()
    {
        var fixture = FindDetourFixture();

        Assert.True(WorldNavigationTargetPathPlanner.TryCreate(
            fixture.Start,
            fixture.Destination,
            cell => WorldNavigationTargetPathPlanner.IsWalkable(cell) &&
                cell != fixture.BlockedArrivalCell,
            out var rerouted
        ));
        Assert.NotNull(rerouted);
        Assert.NotEqual(fixture.BlockedArrivalCell, rerouted.ArrivalCell);
        Assert.DoesNotContain(fixture.BlockedArrivalCell, rerouted.Path);
        Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
            fixture.Destination,
            rerouted.ArrivalCell
        ));
        AssertWalksOneCellAtATime(rerouted.Path);
    }

    [Fact]
    public void StableLandmarksAndFestivalEntrancesHaveReachableApproaches()
    {
        var destinations = WorldDefinition.Landmarks
            .Select(landmark => WorldNavigationDestination.AdjacentTarget(
                $"landmark:{landmark.Id}",
                WorldDefinition.GetBiome(landmark.Position),
                landmark.Position,
                WorldNavigationDestinationKind.Landmark,
                landmark.NameKey
            ))
            .Concat(FestivalSpatialCatalog.All.Select(festival =>
                WorldNavigationDestination.AdjacentTarget(
                    $"festival:{festival.FestivalId}",
                    WorldDefinition.GetBiome(festival.WorldEntryCell),
                    festival.WorldEntryCell,
                    WorldNavigationDestinationKind.FestivalEntrance,
                    FestivalCatalog.Festivals[festival.FestivalId].NameKey,
                    festival.LocationId
                )
            ))
            .ToArray();

        foreach (var destination in destinations)
        {
            var start = RepresentativeStart(destination.Region);

            Assert.True(
                WorldNavigationTargetPathPlanner.TryCreate(
                    start,
                    destination,
                    out var path
                ),
                $"No approach path for {destination.Id}."
            );
            Assert.NotNull(path);
            Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
                destination,
                path.ArrivalCell
            ));
            AssertWalksOneCellAtATime(path.Path);
        }
    }

    [Fact]
    public void RegionOnlyAndBlockedStartsAreRejectedWithoutInventingAPath()
    {
        Assert.False(WorldNavigationTargetPathPlanner.TryCreate(
            new GridPosition(16, 12),
            WorldNavigationDestination.RegionOnly(WorldBiome.Home),
            out _
        ));
        Assert.False(WorldNavigationTargetPathPlanner.TryCreate(
            FarmLayout.CottageDoorCell,
            WorldNavigationDestination.AdjacentTarget(
                "test:mailbox",
                WorldBiome.Home,
                FarmLayout.StarlightMailboxCell,
                WorldNavigationDestinationKind.Mailbox,
                "morning.mail.title"
            ),
            out _
        ));
    }

    [Fact]
    public void IndoorTargetPathContinuesFromArrivalToAnAdjacentNpcCell()
    {
        var locationId = PlayerLocationIds.MoonlitArchive;
        var indoorTarget = new GridPosition(19, 15);
        var destination = WorldNavigationDestination.AdjacentTarget(
            "test:archive_character",
            WorldBiome.LumenVillage,
            VillageCatalog.MoonlitArchiveDoorCell,
            WorldNavigationDestinationKind.Character,
            "npc.liora",
            locationId,
            indoorTarget
        );
        var start = Assert.IsType<GridPosition>(
            NpcNavigationMap.SafeArrivalCell(
                PlayerLocationIds.World,
                locationId
            )
        );
        Assert.True(destination.TryGetTargetCell(
            locationId,
            out var resolvedTarget
        ));
        Assert.Equal(indoorTarget, resolvedTarget);
        Assert.True(NpcNavigationMap.IsWalkableGeometry(locationId, start));
        Assert.True(NpcNavigationMap.IsWalkableGeometry(
            locationId,
            new GridPosition(indoorTarget.X, indoorTarget.Y + 1)
        ));

        Assert.True(WorldNavigationTargetPathPlanner.TryCreate(
            start,
            destination,
            locationId,
            out var path
        ));
        Assert.NotNull(path);
        Assert.Equal(locationId, path.LocationId);
        Assert.Equal(indoorTarget, path.TargetCell);
        Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
            destination,
            locationId,
            path.ArrivalCell
        ));
        Assert.DoesNotContain(indoorTarget, path.Path);
        Assert.All(path.Path, cell => Assert.True(
            NpcNavigationMap.IsWalkableGeometry(locationId, cell)
        ));
        AssertWalksOneCellAtATime(path.Path);
    }

    private static GridPosition RepresentativeStart(WorldBiome region)
    {
        if (region == WorldBiome.Home)
        {
            return new GridPosition(
                FarmLayout.CottageDoorCell.X,
                FarmLayout.CottageDoorCell.Y + 1
            );
        }

        if (region == WorldBiome.LumenVillage)
        {
            return VillageCatalog.VillageCenterCell;
        }

        return WorldNavigationRouteSelection.Routes
            .Single(route =>
                route.FromRegion == WorldBiome.LumenVillage &&
                route.ToRegion == region
            )
            .End;
    }

    private static DetourFixture FindDetourFixture()
    {
        foreach (var destination in StableApproachDestinations())
        {
            var start = RepresentativeStart(destination.Region);
            if (!WorldNavigationTargetPathPlanner.TryCreate(
                    start,
                    destination,
                    out var initialPath
                ) ||
                initialPath is null)
            {
                continue;
            }

            var blockedArrivalCell = initialPath.ArrivalCell;
            if (WorldNavigationTargetPathPlanner.TryCreate(
                    start,
                    destination,
                    cell => WorldNavigationTargetPathPlanner.IsWalkable(cell) &&
                        cell != blockedArrivalCell,
                    out var reroutedPath
                ) &&
                reroutedPath is not null &&
                reroutedPath.ArrivalCell != blockedArrivalCell)
            {
                return new DetourFixture(
                    start,
                    destination,
                    blockedArrivalCell
                );
            }
        }

        throw new InvalidOperationException(
            "No stable target with an alternate arrival cell was found."
        );
    }

    private static IEnumerable<WorldNavigationDestination>
        StableApproachDestinations()
    {
        yield return WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );
        yield return WorldNavigationDestination.AdjacentTarget(
            "test:commission_board",
            WorldBiome.Home,
            FarmLayout.CommissionBoardCell,
            WorldNavigationDestinationKind.CommissionBoard,
            "commission.board.title"
        );

        foreach (var landmark in WorldDefinition.Landmarks)
        {
            yield return WorldNavigationDestination.AdjacentTarget(
                $"landmark:{landmark.Id}",
                WorldDefinition.GetBiome(landmark.Position),
                landmark.Position,
                WorldNavigationDestinationKind.Landmark,
                landmark.NameKey
            );
        }

        foreach (var festival in FestivalSpatialCatalog.All)
        {
            yield return WorldNavigationDestination.AdjacentTarget(
                $"festival:{festival.FestivalId}",
                WorldDefinition.GetBiome(festival.WorldEntryCell),
                festival.WorldEntryCell,
                WorldNavigationDestinationKind.FestivalEntrance,
                FestivalCatalog.Festivals[festival.FestivalId].NameKey,
                festival.LocationId
            );
        }
    }

    private static void AssertWalksOneCellAtATime(
        IReadOnlyList<GridPosition> path
    )
    {
        for (var index = 1; index < path.Count; index++)
        {
            var distance = Math.Abs(path[index].X - path[index - 1].X) +
                Math.Abs(path[index].Y - path[index - 1].Y);
            Assert.Equal(1, distance);
        }
    }

    private sealed record DetourFixture(
        GridPosition Start,
        WorldNavigationDestination Destination,
        GridPosition BlockedArrivalCell
    );
}
