namespace Luminfield.Core;

public enum WorldNavigationDestinationKind
{
    Region,
    Mailbox,
    CommissionBoard,
    FestivalEntrance,
    Character,
    Landmark
}

public enum WorldNavigationArrivalMode
{
    RegionOnly,
    AdjacentToTarget
}

public sealed record WorldNavigationDestination(
    string Id,
    WorldBiome Region,
    string LocationId,
    GridPosition? TargetCell,
    WorldNavigationDestinationKind Kind,
    string NameKey,
    WorldNavigationArrivalMode ArrivalMode,
    GridPosition? LocationTargetCell = null
)
{
    public bool HasTargetCell => TargetCell is not null &&
        ArrivalMode != WorldNavigationArrivalMode.RegionOnly;

    public bool HasLocationTargetCell =>
        LocationId != PlayerLocationIds.World &&
        LocationTargetCell is not null &&
        ArrivalMode != WorldNavigationArrivalMode.RegionOnly;

    public bool TryGetTargetCell(
        string locationId,
        out GridPosition targetCell
    )
    {
        if (locationId == PlayerLocationIds.World &&
            HasTargetCell &&
            TargetCell is GridPosition worldTarget)
        {
            targetCell = worldTarget;
            return true;
        }

        if (locationId == LocationId &&
            HasLocationTargetCell &&
            LocationTargetCell is GridPosition locationTarget)
        {
            targetCell = locationTarget;
            return true;
        }

        targetCell = default;
        return false;
    }

    public static WorldNavigationDestination RegionOnly(
        WorldBiome region
    ) => new(
        $"region:{region}",
        region,
        PlayerLocationIds.World,
        TargetCell: null,
        WorldNavigationDestinationKind.Region,
        WorldDefinition.RegionNameKey(region),
        WorldNavigationArrivalMode.RegionOnly,
        LocationTargetCell: null
    );

    public static WorldNavigationDestination AdjacentTarget(
        string id,
        WorldBiome region,
        GridPosition targetCell,
        WorldNavigationDestinationKind kind,
        string nameKey,
        string locationId = PlayerLocationIds.World,
        GridPosition? locationTargetCell = null
    ) => new(
        id,
        region,
        locationId,
        targetCell,
        kind,
        nameKey,
        WorldNavigationArrivalMode.AdjacentToTarget,
        locationTargetCell
    );
}

public sealed record WorldNavigationTargetPath(
    WorldNavigationDestination Destination,
    string LocationId,
    GridPosition TargetCell,
    GridPosition Start,
    GridPosition ArrivalCell,
    IReadOnlyList<GridPosition> Path
)
{
    public string RouteId => RouteIdFor(Destination.Id);

    public int PathLength => Math.Max(0, Path.Count - 1);

    public static string RouteIdFor(string destinationId) =>
        $"target:{destinationId}";
}
