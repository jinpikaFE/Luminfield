using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRouteSelectionTests
{
    private static readonly GridPosition[] Directions =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    [Fact]
    public void StartsWithoutAnActiveRouteSelection()
    {
        var selection = new WorldNavigationRouteSelection();
        var projection = selection.Project(new GridPosition(128, 80));

        Assert.False(projection.HasSelection);
        Assert.Null(selection.SelectedRouteId);
        Assert.Null(selection.JourneyDestinationRegion);
        Assert.Null(projection.SelectedRouteId);
        Assert.Null(projection.Progress);
        Assert.Null(projection.JourneyDestinationRegion);
        Assert.Null(projection.JourneyTarget);
        Assert.Equal(0, projection.CurrentSegmentNumber);
        Assert.Equal(0, projection.SegmentCount);
        Assert.False(projection.IsMultiSegmentJourney);
        Assert.Equal(
            WorldNavigationGuideCatalog.RouteContracts.Count * 2,
            projection.Routes.Count
        );
        Assert.Equal(
            WorldNavigationGuideCatalog.RouteContracts.Select(route => route.Id),
            projection.Routes
                .Where(route => !route.IsReversed)
                .Select(route => route.RouteId)
        );
    }

    [Fact]
    public void RoutesAreListedInForwardThenReverseContractOrder()
    {
        var routes = WorldNavigationRouteSelection.Routes;
        var contracts = WorldNavigationGuideCatalog.RouteContracts;

        Assert.Equal(contracts.Count * 2, routes.Count);
        Assert.Equal(
            contracts.Select(route => route.Id),
            routes.Take(contracts.Count).Select(route => route.RouteId)
        );
        Assert.Equal(
            contracts.Select(route =>
                WorldNavigationRouteProgressPresenter.ReverseRouteId(route.Id)
            ),
            routes.Skip(contracts.Count).Select(route => route.RouteId)
        );
    }

    [Fact]
    public void RoutesExposeStableForwardAndReverseMetrics()
    {
        var routes = WorldNavigationRouteSelection.Routes;
        var audits = WorldNavigationRouteAuditor.AuditAll()
            .ToDictionary(audit => audit.ContractId, StringComparer.Ordinal);

        foreach (var route in routes)
        {
            var audit = audits[route.ContractId];

            if (route.IsReversed)
            {
                Assert.Equal(
                    WorldNavigationRouteProgressPresenter.ReverseRouteId(
                        audit.ContractId
                    ),
                    route.RouteId
                );
                Assert.Equal(audit.ToRegion, route.FromRegion);
                Assert.Equal(audit.FromRegion, route.ToRegion);
                Assert.Equal(audit.End, route.Start);
                Assert.Equal(audit.Start, route.End);
            }
            else
            {
                Assert.Equal(audit.ContractId, route.RouteId);
                Assert.Equal(audit.FromRegion, route.FromRegion);
                Assert.Equal(audit.ToRegion, route.ToRegion);
                Assert.Equal(audit.Start, route.Start);
                Assert.Equal(audit.End, route.End);
            }

            Assert.Equal(audit.ContractId, route.ContractId);
            Assert.Equal(audit.VisibleGuides.Count, route.GuideCount);
            Assert.Equal(audit.PathLength, route.PathLength);
        }
    }

    [Fact]
    public void AvailableFromReturnsStableRouteOptionsForEachRegion()
    {
        var homeRoute = Assert.Single(
            WorldNavigationRouteSelection.AvailableFrom(WorldBiome.Home)
        );

        Assert.False(homeRoute.IsReversed);
        Assert.Equal(
            6,
            WorldNavigationRouteSelection
                .AvailableFrom(WorldBiome.LumenVillage)
                .Count
        );

        AssertSinglePeripheralRoute(WorldBiome.WhisperingWoods);
        AssertSinglePeripheralRoute(WorldBiome.StarfallMeadow);
        AssertSinglePeripheralRoute(WorldBiome.CrystalVale);
        AssertSinglePeripheralRoute(WorldBiome.MoonwaterWetlands);
        AssertSinglePeripheralRoute(WorldBiome.StarfallRuins);
    }

    [Fact]
    public void CanExplicitlySelectEachForwardAndReverseRouteAndRefreshProgress()
    {
        var selection = new WorldNavigationRouteSelection();

        foreach (var route in WorldNavigationRouteSelection.Routes)
        {
            Assert.True(selection.Select(route.RouteId));

            var projection = selection.Project(route.Start);

            Assert.True(projection.HasSelection);
            Assert.Equal(route.RouteId, selection.SelectedRouteId);
            Assert.Equal(route.RouteId, projection.SelectedRouteId);
            Assert.NotNull(projection.Progress);
            Assert.True(projection.Progress.RouteExists);
            Assert.Equal(route.RouteId, projection.Progress.RouteId);
            Assert.Equal(route.ToRegion, projection.Progress.DestinationRegion);
            Assert.Equal(route.ToRegion, projection.JourneyDestinationRegion);
            Assert.Null(projection.JourneyTarget);
            Assert.Equal(1, projection.CurrentSegmentNumber);
            Assert.Equal(1, projection.SegmentCount);
            Assert.False(projection.IsMultiSegmentJourney);
            Assert.Equal(0, projection.Progress.NearestPathIndex);
            Assert.False(projection.Progress.IsArrived);
        }
    }

    [Fact]
    public void DestinationSelectionCreatesAndAdvancesAStableMultiSegmentJourney()
    {
        var selection = new WorldNavigationRouteSelection();
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:woods_lantern",
            WorldBiome.WhisperingWoods,
            WorldDefinition.WoodlandStarlightCell,
            WorldNavigationDestinationKind.Landmark,
            "world.landmark.woods_lantern"
        );

        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            target
        ));

        var first = selection.Project(new GridPosition(19, 30));
        Assert.Equal(
            "route_home_to_lumen_village",
            first.SelectedRouteId
        );
        Assert.Equal(
            WorldBiome.WhisperingWoods,
            first.JourneyDestinationRegion
        );
        Assert.Equal(target, first.JourneyTarget);
        Assert.Equal(1, first.CurrentSegmentNumber);
        Assert.Equal(3, first.SegmentCount);
        Assert.True(first.IsMultiSegmentJourney);

        var villageCenter = WorldNavigationRouteSelection.Routes
            .Single(route =>
                route.RouteId == "route_home_to_lumen_village"
            )
            .End;
        Assert.True(selection.TryAdvanceAt(villageCenter));

        var second = selection.Project(villageCenter);
        Assert.Equal(
            "route_lumen_village_to_whispering_woods",
            second.SelectedRouteId
        );
        Assert.Equal(
            WorldBiome.WhisperingWoods,
            second.JourneyDestinationRegion
        );
        Assert.Equal(target, second.JourneyTarget);
        Assert.Equal(2, second.CurrentSegmentNumber);
        Assert.Equal(3, second.SegmentCount);
        Assert.False(selection.TryAdvanceAt(villageCenter));

        var woodsEnd = WorldNavigationRouteSelection.Routes
            .Single(route =>
                route.RouteId == second.SelectedRouteId
            )
            .End;
        var regionalArrival = selection.Project(woodsEnd);
        Assert.True(regionalArrival.Progress?.IsArrived);
        Assert.True(selection.TryAdvanceAt(woodsEnd));

        var finalApproach = selection.Project(woodsEnd);
        Assert.Equal("target:test:woods_lantern", selection.SelectedRouteId);
        Assert.Equal(3, finalApproach.CurrentSegmentNumber);
        Assert.Equal(3, finalApproach.SegmentCount);
        Assert.True(finalApproach.IsFinalTargetSegment);
        Assert.True(finalApproach.Progress?.RouteExists);
        Assert.False(finalApproach.Progress?.IsArrived);

        var arrivalCell = Assert.IsType<GridPosition>(
            finalApproach.Progress?.NextTarget?.Position
        );
        var arrived = selection.Project(arrivalCell);
        Assert.True(arrived.Progress?.IsArrived);
        Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
            target,
            arrivalCell
        ));
        Assert.False(selection.TryAdvanceAt(arrivalCell));
    }

    [Fact]
    public void SameRegionTargetStartsWithFinalApproachInsteadOfClearing()
    {
        var selection = new WorldNavigationRouteSelection();
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );
        var start = new GridPosition(
            FarmLayout.CottageDoorCell.X,
            FarmLayout.CottageDoorCell.Y + 1
        );

        Assert.True(selection.SelectDestination(WorldBiome.Home, target));

        var projection = selection.Project(start);
        Assert.True(projection.HasSelection);
        Assert.Equal("target:test:mailbox", projection.SelectedRouteId);
        Assert.Equal(target, projection.JourneyTarget);
        Assert.Equal(1, projection.CurrentSegmentNumber);
        Assert.Equal(1, projection.SegmentCount);
        Assert.True(projection.IsFinalTargetSegment);
        Assert.True(projection.Progress?.RouteExists);
    }

    [Fact]
    public void IndoorCharacterTargetHandsTheFinalApproachThroughTheDoor()
    {
        var selection = new WorldNavigationRouteSelection();
        var locationId = PlayerLocationIds.MoonlitArchive;
        var indoorTarget = new GridPosition(19, 15);
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:archive_character",
            WorldBiome.LumenVillage,
            VillageCatalog.MoonlitArchiveDoorCell,
            WorldNavigationDestinationKind.Character,
            "npc.liora",
            locationId,
            indoorTarget
        );

        Assert.True(selection.SelectDestination(
            WorldBiome.LumenVillage,
            target
        ));
        var world = selection.Project(VillageCatalog.VillageCenterCell);
        Assert.True(world.RequiresLocationHandoff);
        Assert.False(world.IsLocationTargetSegment);
        Assert.Equal(target.TargetCell, world.ActiveTargetCell);
        Assert.True(world.Progress?.RouteExists);
        Assert.False(selection.CanGuideAtLocation(
            PlayerLocationIds.MoonstoneWorkshop
        ));

        Assert.True(selection.TryHandoffToLocation(locationId));
        var indoorStart = Assert.IsType<GridPosition>(
            NpcNavigationMap.SafeArrivalCell(
                PlayerLocationIds.World,
                locationId
            )
        );
        var indoor = selection.Project(
            locationId,
            indoorStart,
            cell => NpcNavigationMap.IsWalkableGeometry(locationId, cell)
        );

        Assert.True(indoor.IsFinalTargetSegment);
        Assert.True(indoor.IsLocationTargetSegment);
        Assert.Equal(indoorTarget, indoor.ActiveTargetCell);
        Assert.True(indoor.Progress?.RouteExists);
        Assert.False(indoor.Progress?.IsArrived);

        var arrivalCell = Assert.IsType<GridPosition>(
            indoor.Progress?.NextTarget?.Position
        );
        var arrived = selection.Project(
            locationId,
            arrivalCell,
            cell => NpcNavigationMap.IsWalkableGeometry(locationId, cell)
        );
        Assert.True(arrived.Progress?.IsArrived);
        Assert.True(WorldNavigationTargetPathPlanner.IsArrivalCell(
            target,
            locationId,
            arrivalCell
        ));
    }

    [Fact]
    public void FinalApproachReplansFromThePlayerWhenTheyLeaveTheCachedPath()
    {
        var selection = new WorldNavigationRouteSelection();
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );
        var start = new GridPosition(
            FarmLayout.CottageDoorCell.X,
            FarmLayout.CottageDoorCell.Y + 1
        );

        Assert.True(selection.SelectDestination(WorldBiome.Home, target));
        Assert.True(selection.Project(start).Progress?.RouteExists);

        var probe = FindFinalApproachReplanProbe(start, target);
        var projection = selection.Project(probe.PlayerCell);

        Assert.True(projection.IsFinalTargetSegment);
        Assert.NotNull(projection.Progress);
        Assert.True(projection.Progress.RouteExists);
        Assert.Equal(0, projection.Progress.DistanceFromRoute);
        Assert.Equal(probe.PlayerCell, projection.Progress.NearestPathCell);
        Assert.Equal(
            probe.ExpectedRemainingSteps,
            projection.Progress.RemainingSteps
        );
    }

    [Fact]
    public void FinalApproachReplansWhenCachedArrivalBecomesBlocked()
    {
        var selection = new WorldNavigationRouteSelection();
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:mailbox",
            WorldBiome.Home,
            FarmLayout.StarlightMailboxCell,
            WorldNavigationDestinationKind.Mailbox,
            "morning.mail.title"
        );
        var start = new GridPosition(
            FarmLayout.CottageDoorCell.X,
            FarmLayout.CottageDoorCell.Y + 1
        );

        Assert.True(selection.SelectDestination(WorldBiome.Home, target));
        var initial = selection.Project(start);
        var blockedArrival = Assert.IsType<GridPosition>(
            initial.Progress?.NextTarget?.Position
        );
        Assert.True(WorldNavigationTargetPathPlanner.TryCreate(
            start,
            target,
            cell => WorldNavigationTargetPathPlanner.IsWalkable(cell) &&
                cell != blockedArrival,
            out var reroutedPath
        ));
        Assert.NotNull(reroutedPath);
        Assert.NotEqual(blockedArrival, reroutedPath.ArrivalCell);

        var updated = selection.Project(
            start,
            cell => WorldNavigationTargetPathPlanner.IsWalkable(cell) &&
                cell != blockedArrival
        );

        Assert.True(updated.IsFinalTargetSegment);
        Assert.NotNull(updated.Progress);
        Assert.True(updated.Progress.RouteExists);
        Assert.Equal(reroutedPath.ArrivalCell, updated.Progress.NextTarget?.Position);
        Assert.Equal(reroutedPath.PathLength, updated.Progress.RemainingSteps);
    }

    [Fact]
    public void LocationTargetHandoffCreatesAnInteriorFinalApproach()
    {
        var selection = new WorldNavigationRouteSelection();
        var targetCell = FindInteriorTargetCell(PlayerLocationIds.MoonlitArchive);
        var target = WorldNavigationDestination.AdjacentTarget(
            "test:archive_liora",
            WorldBiome.LumenVillage,
            VillageCatalog.MoonlitArchiveDoorCell,
            WorldNavigationDestinationKind.Character,
            "village.npc.liora.name",
            PlayerLocationIds.MoonlitArchive,
            targetCell
        );
        var start = Assert.IsType<GridPosition>(
            NpcNavigationMap.SafeArrivalCell(
                PlayerLocationIds.World,
                PlayerLocationIds.MoonlitArchive
            )
        );

        Assert.True(selection.SelectDestination(WorldBiome.Home, target));
        Assert.True(selection.CanGuideAtLocation(
            PlayerLocationIds.MoonlitArchive
        ));
        Assert.True(selection.TryHandoffToLocation(
            PlayerLocationIds.MoonlitArchive
        ));

        var projection = selection.Project(
            PlayerLocationIds.MoonlitArchive,
            start,
            cell => NpcNavigationMap.IsWalkableGeometry(
                PlayerLocationIds.MoonlitArchive,
                cell
            )
        );

        Assert.True(projection.IsFinalTargetSegment);
        Assert.True(projection.RequiresLocationHandoff);
        Assert.True(projection.IsLocationTargetSegment);
        Assert.Equal(PlayerLocationIds.MoonlitArchive, projection.PlayerLocationId);
        Assert.Equal(targetCell, projection.ActiveTargetCell);
        Assert.NotNull(projection.Progress);
        Assert.True(projection.Progress.RouteExists);
        Assert.Equal("target:test:archive_liora", projection.SelectedRouteId);
        Assert.Equal(start, projection.Progress.NearestPathCell);
    }

    [Fact]
    public void SameRegionDestinationClearsAnExistingJourney()
    {
        var selection = new WorldNavigationRouteSelection();
        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            WorldBiome.CrystalVale
        ));

        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            WorldBiome.Home
        ));

        var projection = selection.Project(new GridPosition(19, 30));
        Assert.False(projection.HasSelection);
        Assert.Null(projection.JourneyDestinationRegion);
        Assert.Null(projection.JourneyTarget);
        Assert.Equal(0, projection.SegmentCount);
    }

    [Fact]
    public void InvalidDestinationKeepsTheExistingJourneyUntouched()
    {
        var selection = new WorldNavigationRouteSelection();
        Assert.True(selection.SelectDestination(
            WorldBiome.Home,
            WorldBiome.StarfallMeadow
        ));
        var before = selection.Project(new GridPosition(19, 30));

        Assert.False(selection.SelectDestination(
            WorldBiome.Home,
            (WorldBiome)999
        ));

        var after = selection.Project(new GridPosition(19, 30));
        Assert.Equal(before.SelectedRouteId, after.SelectedRouteId);
        Assert.Equal(
            before.JourneyDestinationRegion,
            after.JourneyDestinationRegion
        );
        Assert.Equal(before.SegmentCount, after.SegmentCount);
    }

    [Fact]
    public void ClearRemovesSelectionAndProgress()
    {
        var selection = new WorldNavigationRouteSelection();
        var contract = WorldNavigationGuideCatalog.RouteContracts[0];

        Assert.True(selection.Select(contract.Id));
        Assert.True(selection.Project(contract.Start).HasSelection);

        selection.Clear();
        var projection = selection.Project(contract.Start);

        Assert.False(projection.HasSelection);
        Assert.Null(selection.SelectedRouteId);
        Assert.Null(selection.JourneyDestinationRegion);
        Assert.Null(projection.SelectedRouteId);
        Assert.Null(projection.Progress);
        Assert.Equal(0, projection.SegmentCount);
    }

    [Fact]
    public void UnknownRouteIdsAreRejectedWithoutChangingSelection()
    {
        var selection = new WorldNavigationRouteSelection();
        var knownRoute = WorldNavigationGuideCatalog.RouteContracts[0];

        Assert.True(selection.Select(knownRoute.Id));
        Assert.False(selection.Select("route_missing"));

        var projection = selection.Project(knownRoute.Start);

        Assert.True(projection.HasSelection);
        Assert.Equal(knownRoute.Id, selection.SelectedRouteId);
        Assert.Equal(knownRoute.Id, projection.SelectedRouteId);
        Assert.Equal(knownRoute.Id, projection.Progress?.RouteId);
    }

    [Fact]
    public void RefreshReportsArrivalAndOffRouteProgressForTheSelectedRoute()
    {
        var selection = new WorldNavigationRouteSelection();
        var audit = WorldNavigationRouteAuditor
            .AuditAll()
            .Single(value =>
                value.ContractId == "route_lumen_village_to_starfall_meadow"
            );

        Assert.True(selection.Select(audit.ContractId));

        var arrived = selection.Project(audit.End).Progress;
        Assert.NotNull(arrived);
        Assert.True(arrived.IsArrived);
        Assert.Equal(0, arrived.RemainingSteps);
        Assert.Equal(audit.Path.Count - 1, arrived.NearestPathIndex);

        var probe = FindOffRouteProbe(audit);

        var offRoute = selection.Project(probe.PlayerCell).Progress;
        Assert.NotNull(offRoute);
        Assert.False(offRoute.IsArrived);
        Assert.Equal(probe.ExpectedPathIndex, offRoute.NearestPathIndex);
        Assert.Equal(1, offRoute.DistanceFromRoute);
        Assert.Equal(
            audit.PathLength - probe.ExpectedPathIndex,
            offRoute.RemainingSteps
        );
    }

    [Fact]
    public void RefreshReportsReverseArrivalAndOffRouteProgress()
    {
        var selection = new WorldNavigationRouteSelection();
        var audit = WorldNavigationRouteAuditor
            .AuditAll()
            .Single(value =>
                value.ContractId == "route_lumen_village_to_starfall_meadow"
            );
        var routeId = WorldNavigationRouteProgressPresenter.ReverseRouteId(
            audit.ContractId
        );
        var reversePath = audit.Path.Reverse().ToArray();

        Assert.True(selection.Select(routeId));

        var arrived = selection.Project(audit.Start).Progress;
        Assert.NotNull(arrived);
        Assert.True(arrived.IsArrived);
        Assert.Equal(routeId, arrived.RouteId);
        Assert.Equal(audit.FromRegion, arrived.DestinationRegion);
        Assert.Equal(0, arrived.RemainingSteps);
        Assert.Equal(reversePath.Length - 1, arrived.NearestPathIndex);

        var probe = FindOffRouteProbe(reversePath, routeId);

        var offRoute = selection.Project(probe.PlayerCell).Progress;
        Assert.NotNull(offRoute);
        Assert.False(offRoute.IsArrived);
        Assert.Equal(routeId, offRoute.RouteId);
        Assert.Equal(probe.ExpectedPathIndex, offRoute.NearestPathIndex);
        Assert.Equal(1, offRoute.DistanceFromRoute);
        Assert.Equal(
            audit.PathLength - probe.ExpectedPathIndex,
            offRoute.RemainingSteps
        );
    }

    private static OffRouteProbe FindOffRouteProbe(
        WorldNavigationRouteAudit audit
    ) => FindOffRouteProbe(audit.Path, audit.ContractId);

    private static OffRouteProbe FindOffRouteProbe(
        IReadOnlyList<GridPosition> path,
        string routeId
    )
    {
        var routeCells = path.ToHashSet();
        for (var index = 1; index < path.Count - 1; index++)
        {
            var routeCell = path[index];
            foreach (var direction in Directions)
            {
                var probe = new GridPosition(
                    routeCell.X + direction.X,
                    routeCell.Y + direction.Y
                );
                if (!WorldDefinition.IsInBounds(probe) ||
                    WorldDefinition.IsBlocked(probe) ||
                    routeCells.Contains(probe))
                {
                    continue;
                }

                if (NearestPathIndexes(path, probe).SequenceEqual([index]))
                {
                    return new OffRouteProbe(probe, index);
                }
            }
        }

        throw new InvalidOperationException(
            $"No off-route probe found for {routeId}."
        );
    }

    private static void AssertSinglePeripheralRoute(WorldBiome region)
    {
        var route = Assert.Single(
            WorldNavigationRouteSelection.AvailableFrom(region)
        );

        Assert.True(route.IsReversed);
        Assert.Equal(WorldBiome.LumenVillage, route.ToRegion);
    }

    private static FinalApproachReplanProbe FindFinalApproachReplanProbe(
        GridPosition start,
        WorldNavigationDestination target
    )
    {
        Assert.True(WorldNavigationTargetPathPlanner.TryCreate(
            start,
            target,
            out var initialPath
        ));
        Assert.NotNull(initialPath);

        var routeCells = initialPath.Path.ToHashSet();
        foreach (var routeCell in initialPath.Path)
        {
            foreach (var direction in Directions)
            {
                var probe = new GridPosition(
                    routeCell.X + direction.X,
                    routeCell.Y + direction.Y
                );
                if (routeCells.Contains(probe) ||
                    !WorldNavigationTargetPathPlanner.IsWalkable(probe))
                {
                    continue;
                }

                if (!WorldNavigationTargetPathPlanner.TryCreate(
                        probe,
                        target,
                        out var refreshedPath
                    ) ||
                    refreshedPath is null ||
                    refreshedPath.PathLength == 0)
                {
                    continue;
                }

                return new FinalApproachReplanProbe(
                    probe,
                    refreshedPath.PathLength
                );
            }
        }

        throw new InvalidOperationException(
            $"No final approach replan probe found for {target.Id}."
        );
    }

    private static GridPosition FindInteriorTargetCell(string locationId)
    {
        for (var y = 4; y <= 19; y++)
        {
            for (var x = 3; x <= 36; x++)
            {
                var target = new GridPosition(x, y);
                if (!NpcNavigationMap.IsWalkableGeometry(locationId, target))
                {
                    continue;
                }

                if (Directions.Any(direction =>
                    NpcNavigationMap.IsWalkableGeometry(
                        locationId,
                        new GridPosition(
                            target.X + direction.X,
                            target.Y + direction.Y
                        )
                    )))
                {
                    return target;
                }
            }
        }

        throw new InvalidOperationException(
            $"No interior target probe found for {locationId}."
        );
    }

    private static IReadOnlyList<int> NearestPathIndexes(
        IReadOnlyList<GridPosition> path,
        GridPosition playerCell
    )
    {
        var nearestDistance = path.Min(cell =>
            ManhattanDistance(playerCell, cell)
        );
        return path
            .Select((cell, index) => new
            {
                Cell = cell,
                Index = index
            })
            .Where(value =>
                ManhattanDistance(playerCell, value.Cell) == nearestDistance
            )
            .Select(value => value.Index)
            .ToArray();
    }

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private readonly record struct OffRouteProbe(
        GridPosition PlayerCell,
        int ExpectedPathIndex
    );

    private readonly record struct FinalApproachReplanProbe(
        GridPosition PlayerCell,
        int ExpectedRemainingSteps
    );
}
