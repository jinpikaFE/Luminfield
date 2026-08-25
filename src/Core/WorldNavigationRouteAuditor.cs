namespace Luminfield.Core;

public sealed record WorldNavigationRouteGuideAudit(
    string GuideId,
    WorldNavigationGuideKind Kind,
    WorldBiome Region,
    GridPosition Position,
    int PathIndex
);

public sealed record WorldNavigationRouteSegmentAudit(
    string FromId,
    string ToId,
    GridPosition Start,
    GridPosition End,
    int StartIndex,
    int EndIndex,
    int StepCount
);

public sealed record WorldNavigationRouteAudit(
    string ContractId,
    WorldBiome FromRegion,
    WorldBiome ToRegion,
    GridPosition Start,
    GridPosition End,
    IReadOnlyList<GridPosition> Path,
    IReadOnlyList<GridPosition> TurnPoints,
    IReadOnlyList<WorldNavigationRouteGuideAudit> VisibleGuides,
    IReadOnlyList<WorldNavigationRouteSegmentAudit> Segments
)
{
    public int PathLength => Math.Max(0, Path.Count - 1);

    public int MaximumUnguidedDistance =>
        Segments.Count == 0 ? 0 : Segments.Max(segment => segment.StepCount);
}

public static class WorldNavigationRouteAuditor
{
    private static readonly GridPosition[] Directions =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    public static IReadOnlyList<WorldNavigationRouteAudit> AuditAll() =>
        WorldNavigationGuideCatalog.RouteContracts
            .Select(Audit)
            .ToArray();

    public static WorldNavigationRouteAudit Audit(
        WorldNavigationRouteContract contract
    )
    {
        var guides = WorldNavigationGuideCatalog.Guides
            .ToDictionary(guide => guide.Id, StringComparer.Ordinal);
        var stops = new List<RouteStop>
        {
            new($"{contract.Id}:start", contract.Start, null)
        };
        stops.AddRange(contract.GuideIds.Select(guideId =>
        {
            if (!guides.TryGetValue(guideId, out var guide))
            {
                throw new InvalidOperationException(
                    $"Unknown navigation guide '{guideId}'."
                );
            }

            return new RouteStop(guideId, guide.Position, guide);
        }));
        stops.Add(new RouteStop($"{contract.Id}:end", contract.End, null));

        var path = new List<GridPosition>();
        var stopIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var segments = new List<WorldNavigationRouteSegmentAudit>();

        for (var index = 0; index < stops.Count - 1; index++)
        {
            var from = stops[index];
            var to = stops[index + 1];
            var segmentPath = FindPath(from.Position, to.Position);
            if (segmentPath.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Route segment '{contract.Id}' cannot walk from " +
                    $"{from.Id} to {to.Id}."
                );
            }

            if (path.Count == 0)
            {
                path.AddRange(segmentPath);
                stopIndexes[from.Id] = 0;
                stopIndexes[to.Id] = segmentPath.Count - 1;
            }
            else
            {
                var startIndex = path.Count - 1;
                path.AddRange(segmentPath.Skip(1));
                stopIndexes[from.Id] = startIndex;
                stopIndexes[to.Id] = path.Count - 1;
            }

            segments.Add(new WorldNavigationRouteSegmentAudit(
                from.Id,
                to.Id,
                from.Position,
                to.Position,
                stopIndexes[from.Id],
                stopIndexes[to.Id],
                segmentPath.Count - 1
            ));
        }

        var visibleGuides = stops
            .Where(stop => stop.Guide is not null)
            .Select(stop => new WorldNavigationRouteGuideAudit(
                stop.Id,
                stop.Guide!.Kind,
                stop.Guide.Region,
                stop.Position,
                stopIndexes[stop.Id]
            ))
            .ToArray();

        return new WorldNavigationRouteAudit(
            contract.Id,
            contract.FromRegion,
            contract.ToRegion,
            contract.Start,
            contract.End,
            path,
            FindTurnPoints(path),
            visibleGuides,
            segments
        );
    }

    private static IReadOnlyList<GridPosition> FindPath(
        GridPosition start,
        GridPosition end
    )
    {
        if (!IsRouteWalkable(start) || !IsRouteWalkable(end))
        {
            return [];
        }

        var visited = new HashSet<GridPosition> { start };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            if (current == end)
            {
                return ReconstructPath(previous, start, end);
            }

            foreach (var direction in Directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (!visited.Add(next) || !IsRouteWalkable(next))
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return [];
    }

    private static bool IsRouteWalkable(GridPosition cell) =>
        WorldDefinition.IsInBounds(cell) &&
        !WorldDefinition.IsBlocked(cell);

    private static IReadOnlyList<GridPosition> ReconstructPath(
        IReadOnlyDictionary<GridPosition, GridPosition> previous,
        GridPosition start,
        GridPosition end
    )
    {
        var path = new List<GridPosition> { end };
        var current = end;
        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static IReadOnlyList<GridPosition> FindTurnPoints(
        IReadOnlyList<GridPosition> path
    )
    {
        if (path.Count < 3)
        {
            return [];
        }

        var turns = new List<GridPosition>();
        for (var index = 1; index < path.Count - 1; index++)
        {
            var previous = Delta(path[index - 1], path[index]);
            var next = Delta(path[index], path[index + 1]);
            if (previous != next)
            {
                turns.Add(path[index]);
            }
        }

        return turns;
    }

    private static GridPosition Delta(GridPosition from, GridPosition to) =>
        new(to.X - from.X, to.Y - from.Y);

    private sealed record RouteStop(
        string Id,
        GridPosition Position,
        WorldNavigationGuide? Guide
    );
}
