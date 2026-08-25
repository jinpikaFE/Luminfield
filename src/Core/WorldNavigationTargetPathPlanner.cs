namespace Luminfield.Core;

public static class WorldNavigationTargetPathPlanner
{
    private static readonly GridPosition[] Directions =
    [
        new(0, 1),
        new(1, 0),
        new(-1, 0),
        new(0, -1)
    ];

    public static bool TryCreate(
        GridPosition start,
        WorldNavigationDestination destination,
        out WorldNavigationTargetPath? path
    ) => TryCreate(
        start,
        destination,
        PlayerLocationIds.World,
        IsWalkable,
        out path
    );

    public static bool TryCreate(
        GridPosition start,
        WorldNavigationDestination destination,
        Func<GridPosition, bool> canOccupy,
        out WorldNavigationTargetPath? path
    ) => TryCreate(
        start,
        destination,
        PlayerLocationIds.World,
        canOccupy,
        out path
    );

    public static bool TryCreate(
        GridPosition start,
        WorldNavigationDestination destination,
        string locationId,
        out WorldNavigationTargetPath? path
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        Func<GridPosition, bool> canOccupy =
            locationId == PlayerLocationIds.World
                ? IsWalkable
                : cell => NpcNavigationMap.IsWalkableGeometry(
                    locationId,
                    cell
                );
        return TryCreate(
            start,
            destination,
            locationId,
            canOccupy,
            out path
        );
    }

    public static bool TryCreate(
        GridPosition start,
        WorldNavigationDestination destination,
        string locationId,
        Func<GridPosition, bool> canOccupy,
        out WorldNavigationTargetPath? path
    )
    {
        path = null;
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        ArgumentNullException.ThrowIfNull(canOccupy);

        if (!destination.TryGetTargetCell(locationId, out var targetCell))
        {
            return false;
        }

        if (!canOccupy(start))
        {
            return false;
        }

        var arrivalCells = ArrivalCells(targetCell, canOccupy).ToArray();
        if (arrivalCells.Length == 0)
        {
            return false;
        }

        var route = FindPath(start, arrivalCells, canOccupy);
        if (route.Count == 0)
        {
            return false;
        }

        path = new WorldNavigationTargetPath(
            destination,
            locationId,
            targetCell,
            start,
            route[^1],
            route
        );
        return true;
    }

    public static bool IsArrivalCell(
        WorldNavigationDestination destination,
        GridPosition playerCell
    ) => IsArrivalCell(
        destination,
        PlayerLocationIds.World,
        playerCell
    );

    public static bool IsArrivalCell(
        WorldNavigationDestination destination,
        string locationId,
        GridPosition playerCell
    )
    {
        if (!destination.TryGetTargetCell(locationId, out var targetCell))
        {
            return false;
        }

        return IsArrivalCell(targetCell, playerCell);
    }

    public static bool IsArrivalCell(
        GridPosition targetCell,
        GridPosition playerCell
    )
    {
        return Math.Abs(playerCell.X - targetCell.X) +
            Math.Abs(playerCell.Y - targetCell.Y) == 1;
    }

    private static IEnumerable<GridPosition> ArrivalCells(
        GridPosition targetCell,
        Func<GridPosition, bool> isWalkable
    )
    {
        foreach (var direction in Directions)
        {
            var candidate = new GridPosition(
                targetCell.X + direction.X,
                targetCell.Y + direction.Y
            );
            if (isWalkable(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IReadOnlyList<GridPosition> FindPath(
        GridPosition start,
        IReadOnlyCollection<GridPosition> arrivalCells,
        Func<GridPosition, bool> isWalkable
    )
    {
        if (!isWalkable(start))
        {
            return [];
        }

        var arrivalSet = arrivalCells.ToHashSet();
        var visited = new HashSet<GridPosition> { start };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            if (arrivalSet.Contains(current))
            {
                return ReconstructPath(previous, start, current);
            }

            foreach (var direction in Directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (!visited.Add(next) ||
                    !WorldDefinition.IsInBounds(next) ||
                    !isWalkable(next))
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return [];
    }

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

    public static bool IsWalkable(GridPosition cell)
    {
        if (!WorldDefinition.IsInBounds(cell) ||
            WorldDefinition.IsBlocked(cell) ||
            FarmLayout.IsStaticBlocked(cell))
        {
            return false;
        }

        return true;
    }
}
