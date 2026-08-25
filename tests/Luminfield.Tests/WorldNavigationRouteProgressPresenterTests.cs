using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRouteProgressPresenterTests
{
    private static readonly GridPosition[] Directions =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    [Fact]
    public void PresenterReportsStartProgressForEveryRoute()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var progress = WorldNavigationRouteProgressPresenter.Create(
                audit.ContractId,
                audit.Start
            );
            var expectedTarget = audit.VisibleGuides
                .First(guide => guide.PathIndex > 0);

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(audit.ContractId, progress.RouteId);
            Assert.Equal(audit.ToRegion, progress.DestinationRegion);
            Assert.Equal(0, progress.NearestPathIndex);
            Assert.Equal(audit.Start, progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(audit.PathLength, progress.RemainingSteps);
            Assert.Equal(
                ExpectedDirection(audit.Path, 0),
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertGuideTarget(expectedTarget, progress.NextTarget);
        }
    }

    [Fact]
    public void PresenterReportsReverseStartProgressForEveryRoute()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var routeId = WorldNavigationRouteProgressPresenter.ReverseRouteId(
                audit.ContractId
            );
            var reversePath = ReversePath(audit);
            var reverseGuides = ReverseGuides(audit);
            var progress = WorldNavigationRouteProgressPresenter.Create(
                routeId,
                audit.End
            );
            var expectedTarget = reverseGuides
                .First(guide => guide.PathIndex > 0);

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(routeId, progress.RouteId);
            Assert.Equal(audit.FromRegion, progress.DestinationRegion);
            Assert.Equal(0, progress.NearestPathIndex);
            Assert.Equal(audit.End, progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(audit.PathLength, progress.RemainingSteps);
            Assert.Equal(
                ExpectedDirection(reversePath, 0),
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertGuideTarget(expectedTarget, progress.NextTarget);
        }
    }

    [Fact]
    public void PresenterReportsMidRouteProgressForEveryRoute()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var middleIndex = audit.Path.Count / 2;
            var progress = WorldNavigationRouteProgressPresenter.Create(
                audit.ContractId,
                audit.Path[middleIndex]
            );

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(middleIndex, progress.NearestPathIndex);
            Assert.Equal(audit.ToRegion, progress.DestinationRegion);
            Assert.Equal(audit.Path[middleIndex], progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(
                audit.PathLength - middleIndex,
                progress.RemainingSteps
            );
            Assert.Equal(
                ExpectedDirection(audit.Path, middleIndex),
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertNextTarget(audit, middleIndex, progress.NextTarget);
        }
    }

    [Fact]
    public void PresenterReportsReverseMidRouteProgressForEveryRoute()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var routeId = WorldNavigationRouteProgressPresenter.ReverseRouteId(
                audit.ContractId
            );
            var reversePath = ReversePath(audit);
            var reverseGuides = ReverseGuides(audit);
            var middleIndex = reversePath.Count / 2;
            var progress = WorldNavigationRouteProgressPresenter.Create(
                routeId,
                reversePath[middleIndex]
            );

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(routeId, progress.RouteId);
            Assert.Equal(middleIndex, progress.NearestPathIndex);
            Assert.Equal(audit.FromRegion, progress.DestinationRegion);
            Assert.Equal(reversePath[middleIndex], progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(
                audit.PathLength - middleIndex,
                progress.RemainingSteps
            );
            Assert.Equal(
                ExpectedDirection(reversePath, middleIndex),
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertNextTarget(
                routeId,
                audit.FromRegion,
                audit.Start,
                reversePath,
                reverseGuides,
                middleIndex,
                progress.NextTarget
            );
        }
    }

    [Fact]
    public void PresenterSnapsOffRouteCellsToTheNearestLegalPathIndex()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var probe = FindOffRouteProbe(audit);
            var progress = WorldNavigationRouteProgressPresenter.Create(
                audit.ContractId,
                probe.PlayerCell
            );

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(probe.ExpectedPathIndex, progress.NearestPathIndex);
            Assert.Equal(audit.ToRegion, progress.DestinationRegion);
            Assert.Equal(audit.Path[probe.ExpectedPathIndex], progress.NearestPathCell);
            Assert.Equal(1, progress.DistanceFromRoute);
            Assert.Equal(
                audit.PathLength - probe.ExpectedPathIndex,
                progress.RemainingSteps
            );
            Assert.Equal(
                ExpectedDirection(audit.Path, probe.ExpectedPathIndex),
                progress.MainDirection
            );
            Assert.Equal(
                DirectionToward(
                    probe.PlayerCell,
                    audit.Path[probe.ExpectedPathIndex]
                ),
                progress.RecoveryDirection
            );
            AssertNextTarget(
                audit,
                probe.ExpectedPathIndex,
                progress.NextTarget
            );
        }
    }

    [Fact]
    public void PresenterSnapsReverseOffRouteCellsToNearestLegalPathIndex()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var routeId = WorldNavigationRouteProgressPresenter.ReverseRouteId(
                audit.ContractId
            );
            var reversePath = ReversePath(audit);
            var reverseGuides = ReverseGuides(audit);
            var probe = FindOffRouteProbe(reversePath, routeId);
            var progress = WorldNavigationRouteProgressPresenter.Create(
                routeId,
                probe.PlayerCell
            );

            Assert.True(progress.RouteExists);
            Assert.False(progress.IsArrived);
            Assert.Equal(routeId, progress.RouteId);
            Assert.Equal(probe.ExpectedPathIndex, progress.NearestPathIndex);
            Assert.Equal(audit.FromRegion, progress.DestinationRegion);
            Assert.Equal(reversePath[probe.ExpectedPathIndex], progress.NearestPathCell);
            Assert.Equal(1, progress.DistanceFromRoute);
            Assert.Equal(
                audit.PathLength - probe.ExpectedPathIndex,
                progress.RemainingSteps
            );
            Assert.Equal(
                ExpectedDirection(reversePath, probe.ExpectedPathIndex),
                progress.MainDirection
            );
            Assert.Equal(
                DirectionToward(
                    probe.PlayerCell,
                    reversePath[probe.ExpectedPathIndex]
                ),
                progress.RecoveryDirection
            );
            AssertNextTarget(
                routeId,
                audit.FromRegion,
                audit.Start,
                reversePath,
                reverseGuides,
                probe.ExpectedPathIndex,
                progress.NextTarget
            );
        }
    }

    [Fact]
    public void PresenterReportsArrivalAtEveryRouteEndpoint()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var progress = WorldNavigationRouteProgressPresenter.Create(
                audit.ContractId,
                audit.End
            );

            Assert.True(progress.RouteExists);
            Assert.True(progress.IsArrived);
            Assert.Equal(audit.ToRegion, progress.DestinationRegion);
            Assert.Equal(audit.Path.Count - 1, progress.NearestPathIndex);
            Assert.Equal(audit.End, progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(0, progress.RemainingSteps);
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertEndpointTarget(audit, progress.NextTarget);
        }
    }

    [Fact]
    public void PresenterReportsReverseArrivalAtEveryRouteEndpoint()
    {
        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var routeId = WorldNavigationRouteProgressPresenter.ReverseRouteId(
                audit.ContractId
            );
            var reversePath = ReversePath(audit);
            var progress = WorldNavigationRouteProgressPresenter.Create(
                routeId,
                audit.Start
            );

            Assert.True(progress.RouteExists);
            Assert.True(progress.IsArrived);
            Assert.Equal(routeId, progress.RouteId);
            Assert.Equal(audit.FromRegion, progress.DestinationRegion);
            Assert.Equal(reversePath.Count - 1, progress.NearestPathIndex);
            Assert.Equal(audit.Start, progress.NearestPathCell);
            Assert.Equal(0, progress.DistanceFromRoute);
            Assert.Equal(0, progress.RemainingSteps);
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.MainDirection
            );
            Assert.Equal(
                WorldNavigationRouteDirection.None,
                progress.RecoveryDirection
            );
            AssertEndpointTarget(
                routeId,
                audit.FromRegion,
                audit.Start,
                reversePath,
                progress.NextTarget
            );
        }
    }

    [Fact]
    public void NearestEndpointDoesNotCountAsArrivalWhilePlayerIsOffRoute()
    {
        var auditedRouteCount = 0;

        foreach (var audit in WorldNavigationRouteAuditor.AuditAll())
        {
            var probe = TryFindEndpointProbe(audit.Path);
            if (probe is null)
            {
                continue;
            }

            auditedRouteCount++;
            var progress = WorldNavigationRouteProgressPresenter.Create(
                audit.ContractId,
                probe.Value
            );

            Assert.False(progress.IsArrived);
            Assert.Equal(audit.Path.Count - 1, progress.NearestPathIndex);
            Assert.Equal(audit.End, progress.NearestPathCell);
            Assert.Equal(1, progress.DistanceFromRoute);
            Assert.Equal(
                DirectionToward(probe.Value, audit.End),
                progress.RecoveryDirection
            );
        }

        Assert.True(auditedRouteCount > 0);
    }

    [Fact]
    public void PresenterReturnsEmptyProgressForUnknownRoutes()
    {
        var playerCell = new GridPosition(128, 80);
        var progress = WorldNavigationRouteProgressPresenter.Create(
            "route_missing",
            playerCell
        );

        Assert.False(progress.RouteExists);
        Assert.False(progress.IsArrived);
        Assert.Equal("route_missing", progress.RouteId);
        Assert.Null(progress.DestinationRegion);
        Assert.Equal(playerCell, progress.PlayerCell);
        Assert.Equal(-1, progress.NearestPathIndex);
        Assert.Null(progress.NearestPathCell);
        Assert.Equal(-1, progress.DistanceFromRoute);
        Assert.Null(progress.NextTarget);
        Assert.Equal(0, progress.RemainingSteps);
        Assert.Equal(
            WorldNavigationRouteDirection.None,
            progress.MainDirection
        );
        Assert.Equal(
            WorldNavigationRouteDirection.None,
            progress.RecoveryDirection
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

    private static GridPosition? TryFindEndpointProbe(
        IReadOnlyList<GridPosition> path
    )
    {
        var routeCells = path.ToHashSet();
        var endpointIndex = path.Count - 1;
        var endpoint = path[endpointIndex];
        foreach (var direction in Directions)
        {
            var probe = new GridPosition(
                endpoint.X + direction.X,
                endpoint.Y + direction.Y
            );
            if (!WorldDefinition.IsInBounds(probe) ||
                WorldDefinition.IsBlocked(probe) ||
                routeCells.Contains(probe))
            {
                continue;
            }

            if (NearestPathIndexes(path, probe).SequenceEqual([endpointIndex]))
            {
                return probe;
            }
        }

        return null;
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

    private static WorldNavigationRouteDirection DirectionToward(
        GridPosition from,
        GridPosition to
    )
    {
        if (to.X > from.X)
        {
            return WorldNavigationRouteDirection.East;
        }

        if (to.X < from.X)
        {
            return WorldNavigationRouteDirection.West;
        }

        if (to.Y > from.Y)
        {
            return WorldNavigationRouteDirection.South;
        }

        if (to.Y < from.Y)
        {
            return WorldNavigationRouteDirection.North;
        }

        return WorldNavigationRouteDirection.None;
    }

    private static void AssertNextTarget(
        WorldNavigationRouteAudit audit,
        int nearestIndex,
        WorldNavigationRouteProgressTarget? actual
    ) => AssertNextTarget(
        audit.ContractId,
        audit.ToRegion,
        audit.End,
        audit.Path,
        audit.VisibleGuides,
        nearestIndex,
        actual
    );

    private static void AssertNextTarget(
        string routeId,
        WorldBiome destinationRegion,
        GridPosition end,
        IReadOnlyList<GridPosition> path,
        IReadOnlyList<WorldNavigationRouteGuideAudit> visibleGuides,
        int nearestIndex,
        WorldNavigationRouteProgressTarget? actual
    )
    {
        var guide = visibleGuides
            .FirstOrDefault(value => value.PathIndex > nearestIndex);
        if (guide is not null)
        {
            AssertGuideTarget(guide, actual);
            return;
        }

        AssertEndpointTarget(routeId, destinationRegion, end, path, actual);
    }

    private static void AssertGuideTarget(
        WorldNavigationRouteGuideAudit expected,
        WorldNavigationRouteProgressTarget? actual
    )
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.GuideId, actual.Id);
        Assert.Equal(WorldNavigationRouteProgressTargetKind.Guide, actual.Kind);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.PathIndex, actual.PathIndex);
        Assert.Equal(expected.Region, actual.Region);
        Assert.Equal(expected.Kind, actual.GuideKind);
    }

    private static void AssertEndpointTarget(
        WorldNavigationRouteAudit audit,
        WorldNavigationRouteProgressTarget? actual
    ) => AssertEndpointTarget(
        audit.ContractId,
        audit.ToRegion,
        audit.End,
        audit.Path,
        actual
    );

    private static void AssertEndpointTarget(
        string routeId,
        WorldBiome destinationRegion,
        GridPosition end,
        IReadOnlyList<GridPosition> path,
        WorldNavigationRouteProgressTarget? actual
    )
    {
        Assert.NotNull(actual);
        Assert.Equal($"{routeId}:end", actual.Id);
        Assert.Equal(
            WorldNavigationRouteProgressTargetKind.Endpoint,
            actual.Kind
        );
        Assert.Equal(end, actual.Position);
        Assert.Equal(path.Count - 1, actual.PathIndex);
        Assert.Equal(destinationRegion, actual.Region);
        Assert.Null(actual.GuideKind);
    }

    private static IReadOnlyList<GridPosition> ReversePath(
        WorldNavigationRouteAudit audit
    ) => audit.Path.Reverse().ToArray();

    private static IReadOnlyList<WorldNavigationRouteGuideAudit> ReverseGuides(
        WorldNavigationRouteAudit audit
    )
    {
        var lastPathIndex = audit.Path.Count - 1;
        return audit.VisibleGuides
            .OrderByDescending(guide => guide.PathIndex)
            .Select(guide => guide with
            {
                PathIndex = lastPathIndex - guide.PathIndex
            })
            .ToArray();
    }

    private static WorldNavigationRouteDirection ExpectedDirection(
        IReadOnlyList<GridPosition> path,
        int nearestIndex
    )
    {
        if (nearestIndex >= path.Count - 1)
        {
            return WorldNavigationRouteDirection.None;
        }

        var current = path[nearestIndex];
        var next = path[nearestIndex + 1];
        if (next.X > current.X)
        {
            return WorldNavigationRouteDirection.East;
        }

        if (next.X < current.X)
        {
            return WorldNavigationRouteDirection.West;
        }

        if (next.Y > current.Y)
        {
            return WorldNavigationRouteDirection.South;
        }

        if (next.Y < current.Y)
        {
            return WorldNavigationRouteDirection.North;
        }

        return WorldNavigationRouteDirection.None;
    }

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private readonly record struct OffRouteProbe(
        GridPosition PlayerCell,
        int ExpectedPathIndex
    );
}
