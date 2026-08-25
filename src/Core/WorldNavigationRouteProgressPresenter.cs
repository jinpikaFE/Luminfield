namespace Luminfield.Core;

public enum WorldNavigationRouteDirection
{
    None,
    North,
    South,
    West,
    East
}

public enum WorldNavigationRouteProgressTargetKind
{
    Guide,
    Endpoint
}

public sealed record WorldNavigationRouteProgressTarget(
    string Id,
    WorldNavigationRouteProgressTargetKind Kind,
    GridPosition Position,
    int PathIndex,
    WorldBiome Region,
    WorldNavigationGuideKind? GuideKind
);

public sealed record WorldNavigationRouteProgress(
    string RouteId,
    bool RouteExists,
    WorldBiome? DestinationRegion,
    GridPosition PlayerCell,
    int NearestPathIndex,
    GridPosition? NearestPathCell,
    int DistanceFromRoute,
    WorldNavigationRouteProgressTarget? NextTarget,
    WorldNavigationRouteDirection MainDirection,
    WorldNavigationRouteDirection RecoveryDirection,
    int RemainingSteps,
    bool IsArrived
);

public static class WorldNavigationRouteProgressPresenter
{
    public const string ReverseRouteIdSuffix = "_reverse";

    private static readonly IReadOnlyDictionary<string, RouteProgressContract>
        Routes = WorldNavigationRouteAuditor
            .AuditAll()
            .SelectMany(CreateRouteContracts)
            .ToDictionary(route => route.RouteId, StringComparer.Ordinal);

    public static string ReverseRouteId(string contractId) =>
        $"{contractId}{ReverseRouteIdSuffix}";

    public static WorldNavigationRouteProgress Create(
        string routeId,
        GridPosition playerCell
    )
    {
        if (!Routes.TryGetValue(routeId, out var route))
        {
            return MissingRoute(routeId, playerCell);
        }

        var nearestIndex = NearestPathIndex(route.Path, playerCell);
        var nearestCell = route.Path[nearestIndex];
        var remainingSteps = route.PathLength - nearestIndex;
        var isArrived = playerCell == route.End;
        var nextTarget = NextTarget(route, nearestIndex);

        return new WorldNavigationRouteProgress(
            route.RouteId,
            RouteExists: true,
            route.ToRegion,
            playerCell,
            nearestIndex,
            nearestCell,
            ManhattanDistance(playerCell, nearestCell),
            nextTarget,
            MainDirection(route.Path, nearestIndex),
            DirectionToward(playerCell, nearestCell),
            remainingSteps,
            isArrived
        );
    }

    public static WorldNavigationRouteProgress CreatePath(
        string routeId,
        WorldBiome destinationRegion,
        IReadOnlyList<GridPosition> path,
        GridPosition playerCell
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count == 0)
        {
            return MissingRoute(routeId, playerCell);
        }

        var route = new RouteProgressContract(
            routeId,
            destinationRegion,
            path[^1],
            path,
            []
        );
        var nearestIndex = NearestPathIndex(route.Path, playerCell);
        var nearestCell = route.Path[nearestIndex];
        var remainingSteps = route.PathLength - nearestIndex;
        var isArrived = playerCell == route.End;
        var nextTarget = NextTarget(route, nearestIndex);

        return new WorldNavigationRouteProgress(
            route.RouteId,
            RouteExists: true,
            route.ToRegion,
            playerCell,
            nearestIndex,
            nearestCell,
            ManhattanDistance(playerCell, nearestCell),
            nextTarget,
            MainDirection(route.Path, nearestIndex),
            DirectionToward(playerCell, nearestCell),
            remainingSteps,
            isArrived
        );
    }

    private static IEnumerable<RouteProgressContract> CreateRouteContracts(
        WorldNavigationRouteAudit audit
    )
    {
        yield return new RouteProgressContract(
            audit.ContractId,
            audit.ToRegion,
            audit.End,
            audit.Path,
            audit.VisibleGuides
        );

        yield return new RouteProgressContract(
            ReverseRouteId(audit.ContractId),
            audit.FromRegion,
            audit.Start,
            audit.Path.Reverse().ToArray(),
            ReverseGuides(audit)
        );
    }

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

    private static WorldNavigationRouteProgress MissingRoute(
        string routeId,
        GridPosition playerCell
    ) => new(
        routeId,
        RouteExists: false,
        DestinationRegion: null,
        playerCell,
        NearestPathIndex: -1,
        NearestPathCell: null,
        DistanceFromRoute: -1,
        NextTarget: null,
        WorldNavigationRouteDirection.None,
        WorldNavigationRouteDirection.None,
        RemainingSteps: 0,
        IsArrived: false
    );

    private static int NearestPathIndex(
        IReadOnlyList<GridPosition> path,
        GridPosition playerCell
    )
    {
        var nearestIndex = 0;
        var nearestDistance = int.MaxValue;

        for (var index = 0; index < path.Count; index++)
        {
            var distance = ManhattanDistance(playerCell, path[index]);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestIndex = index;
        }

        return nearestIndex;
    }

    private static WorldNavigationRouteProgressTarget NextTarget(
        RouteProgressContract route,
        int nearestIndex
    )
    {
        var guide = route.VisibleGuides
            .FirstOrDefault(value => value.PathIndex > nearestIndex);
        if (guide is not null)
        {
            return new WorldNavigationRouteProgressTarget(
                guide.GuideId,
                WorldNavigationRouteProgressTargetKind.Guide,
                guide.Position,
                guide.PathIndex,
                guide.Region,
                guide.Kind
            );
        }

        return new WorldNavigationRouteProgressTarget(
            $"{route.RouteId}:end",
            WorldNavigationRouteProgressTargetKind.Endpoint,
            route.End,
            route.Path.Count - 1,
            route.ToRegion,
            GuideKind: null
        );
    }

    private static WorldNavigationRouteDirection MainDirection(
        IReadOnlyList<GridPosition> path,
        int nearestIndex
    )
    {
        if (nearestIndex < 0 || nearestIndex >= path.Count - 1)
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

    private static WorldNavigationRouteDirection DirectionToward(
        GridPosition from,
        GridPosition to
    )
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        if (Math.Abs(deltaX) >= Math.Abs(deltaY) && deltaX != 0)
        {
            return deltaX > 0
                ? WorldNavigationRouteDirection.East
                : WorldNavigationRouteDirection.West;
        }

        if (deltaY != 0)
        {
            return deltaY > 0
                ? WorldNavigationRouteDirection.South
                : WorldNavigationRouteDirection.North;
        }

        return WorldNavigationRouteDirection.None;
    }

    private static int ManhattanDistance(
        GridPosition left,
        GridPosition right
    ) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private sealed record RouteProgressContract(
        string RouteId,
        WorldBiome ToRegion,
        GridPosition End,
        IReadOnlyList<GridPosition> Path,
        IReadOnlyList<WorldNavigationRouteGuideAudit> VisibleGuides
    )
    {
        public int PathLength => Math.Max(0, Path.Count - 1);
    }
}
