namespace Luminfield.Core;

public sealed record WorldNavigationJourneyPlan(
    WorldBiome Origin,
    WorldBiome Destination,
    IReadOnlyList<WorldNavigationRouteOption> Segments
)
{
    public int SegmentCount => Segments.Count;

    public bool IsSameRegion => SegmentCount == 0;
}

public static class WorldNavigationJourneyPlanner
{
    private static readonly IReadOnlyList<WorldNavigationRouteOption> Routes =
        WorldNavigationRouteSelection.Routes;

    private static readonly IReadOnlyDictionary<WorldBiome, IReadOnlyList<
        WorldNavigationRouteOption
    >> RoutesByOrigin = Routes
        .GroupBy(route => route.FromRegion)
        .ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<WorldNavigationRouteOption>)
                group.ToArray()
        );

    public static bool TryCreate(
        WorldBiome origin,
        WorldBiome destination,
        out WorldNavigationJourneyPlan? plan
    )
    {
        plan = null;

        if (!IsKnownRegion(origin) || !IsKnownRegion(destination))
        {
            return false;
        }

        if (origin == destination)
        {
            plan = new WorldNavigationJourneyPlan(
                origin,
                destination,
                EmptySegments()
            );
            return true;
        }

        return TryFindRoute(origin, destination, out plan);
    }

    private static bool TryFindRoute(
        WorldBiome origin,
        WorldBiome destination,
        out WorldNavigationJourneyPlan? plan
    )
    {
        var visited = new HashSet<WorldBiome>
        {
            origin
        };
        var queue = new Queue<JourneySearchNode>();
        queue.Enqueue(
            new JourneySearchNode(
                origin,
                EmptySegments()
            )
        );

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!RoutesByOrigin.TryGetValue(current.Region, out var routes))
            {
                continue;
            }

            foreach (var route in routes)
            {
                if (!visited.Add(route.ToRegion))
                {
                    continue;
                }

                var segments = Append(current.Segments, route);
                if (route.ToRegion == destination)
                {
                    plan = new WorldNavigationJourneyPlan(
                        origin,
                        destination,
                        segments
                    );
                    return true;
                }

                queue.Enqueue(new JourneySearchNode(route.ToRegion, segments));
            }
        }

        plan = null;
        return false;
    }

    private static IReadOnlyList<WorldNavigationRouteOption> Append(
        IReadOnlyList<WorldNavigationRouteOption> segments,
        WorldNavigationRouteOption route
    )
    {
        var result = new WorldNavigationRouteOption[segments.Count + 1];
        for (var index = 0; index < segments.Count; index++)
        {
            result[index] = segments[index];
        }

        result[^1] = route;
        return Array.AsReadOnly(result);
    }

    private static IReadOnlyList<WorldNavigationRouteOption> EmptySegments() =>
        Array.AsReadOnly(Array.Empty<WorldNavigationRouteOption>());

    private static bool IsKnownRegion(WorldBiome region) => region switch
    {
        WorldBiome.Home => true,
        WorldBiome.WhisperingWoods => true,
        WorldBiome.StarfallMeadow => true,
        WorldBiome.LumenVillage => true,
        WorldBiome.CrystalVale => true,
        WorldBiome.MoonwaterWetlands => true,
        WorldBiome.StarfallRuins => true,
        _ => false
    };

    private sealed record JourneySearchNode(
        WorldBiome Region,
        IReadOnlyList<WorldNavigationRouteOption> Segments
    );
}
