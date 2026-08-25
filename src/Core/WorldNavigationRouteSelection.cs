namespace Luminfield.Core;

public sealed record WorldNavigationRouteOption(
    string RouteId,
    string ContractId,
    WorldBiome FromRegion,
    WorldBiome ToRegion,
    GridPosition Start,
    GridPosition End,
    int GuideCount,
    int PathLength,
    bool IsReversed
);

public sealed record WorldNavigationRouteSelectionProjection(
    IReadOnlyList<WorldNavigationRouteOption> Routes,
    string? SelectedRouteId,
    WorldNavigationRouteProgress? Progress
)
{
    public bool HasSelection => SelectedRouteId is not null;

    public WorldBiome? JourneyDestinationRegion { get; init; }

    public WorldNavigationDestination? JourneyTarget { get; init; }

    public int CurrentSegmentNumber { get; init; }

    public int SegmentCount { get; init; }

    public bool IsFinalTargetSegment { get; init; }

    public string PlayerLocationId { get; init; } =
        PlayerLocationIds.World;

    public GridPosition? ActiveTargetCell { get; init; }

    public bool RequiresLocationHandoff { get; init; }

    public bool IsLocationTargetSegment { get; init; }

    public bool IsMultiSegmentJourney => SegmentCount > 1;
}

public sealed class WorldNavigationRouteSelection
{
    private static readonly IReadOnlyList<WorldNavigationRouteOption>
        StableRoutes = BuildStableRoutes();

    private string? _selectedRouteId;
    private IReadOnlyList<WorldNavigationRouteOption> _journeySegments =
        Array.Empty<WorldNavigationRouteOption>();
    private int _activeJourneySegmentIndex;
    private WorldBiome? _journeyDestinationRegion;
    private WorldNavigationDestination? _journeyTarget;
    private WorldNavigationTargetPath? _targetPath;
    private bool _hasTargetSegment;

    public string? SelectedRouteId => _selectedRouteId;

    public WorldBiome? JourneyDestinationRegion =>
        _journeyDestinationRegion;

    public WorldNavigationDestination? JourneyTarget => _journeyTarget;

    public int GuidanceSegmentCount => SegmentCount();

    public static IReadOnlyList<WorldNavigationRouteOption> Routes =>
        StableRoutes;

    public static IReadOnlyList<WorldNavigationRouteOption> AvailableFrom(
        WorldBiome fromRegion
    ) => StableRoutes
        .Where(route => route.FromRegion == fromRegion)
        .ToArray();

    public WorldNavigationRouteSelectionProjection Project(
        GridPosition playerCell
    ) => Project(
        PlayerLocationIds.World,
        playerCell,
        WorldNavigationTargetPathPlanner.IsWalkable
    );

    public WorldNavigationRouteSelectionProjection Project(
        GridPosition playerCell,
        Func<GridPosition, bool> canOccupy
    ) => Project(PlayerLocationIds.World, playerCell, canOccupy);

    public WorldNavigationRouteSelectionProjection Project(
        string playerLocationId,
        GridPosition playerCell,
        Func<GridPosition, bool> canOccupy
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerLocationId);
        ArgumentNullException.ThrowIfNull(canOccupy);

        var progress = CreateProgress(
            playerLocationId,
            playerCell,
            canOccupy
        );
        var targetSegmentActive = IsTargetSegmentActive();
        var locationTargetActive = targetSegmentActive &&
            _journeyTarget is not null &&
            playerLocationId == _journeyTarget.LocationId &&
            _journeyTarget.HasLocationTargetCell;
        GridPosition? activeTargetCell = null;
        if (_journeyTarget is { } journeyTarget &&
            journeyTarget.TryGetTargetCell(
                playerLocationId,
                out var projectedTargetCell
            ))
        {
            activeTargetCell = projectedTargetCell;
        }

        return new WorldNavigationRouteSelectionProjection(
            StableRoutes,
            _selectedRouteId,
            progress
        )
        {
            JourneyDestinationRegion = _journeyDestinationRegion,
            JourneyTarget = _journeyTarget,
            CurrentSegmentNumber = _selectedRouteId is null
                ? 0
                : CurrentSegmentNumber(targetSegmentActive),
            SegmentCount = SegmentCount(),
            IsFinalTargetSegment = targetSegmentActive,
            PlayerLocationId = playerLocationId,
            ActiveTargetCell = activeTargetCell,
            RequiresLocationHandoff =
                _journeyTarget?.HasLocationTargetCell == true,
            IsLocationTargetSegment = locationTargetActive
        };
    }

    public bool CanGuideAtLocation(string locationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        if (locationId == PlayerLocationIds.World)
        {
            return _selectedRouteId is not null;
        }

        return _journeyTarget is
        {
            HasLocationTargetCell: true
        } target && target.LocationId == locationId;
    }

    public bool TryHandoffToLocation(string locationId)
    {
        if (!CanGuideAtLocation(locationId) ||
            locationId == PlayerLocationIds.World ||
            _journeyTarget is null)
        {
            return false;
        }

        _activeJourneySegmentIndex = _journeySegments.Count;
        if (_selectedRouteId != TargetRouteId() ||
            _targetPath?.LocationId != locationId)
        {
            _selectedRouteId = TargetRouteId();
            _targetPath = null;
        }
        return true;
    }

    public bool Select(string routeId)
    {
        var route = StableRoutes.FirstOrDefault(option =>
            option.RouteId == routeId
        );
        if (route is null)
        {
            return false;
        }

        SetJourney(
            [route],
            route.ToRegion,
            journeyTarget: null,
            hasTargetSegment: false
        );
        return true;
    }

    public bool SelectDestination(
        WorldBiome origin,
        WorldBiome destination
    )
    {
        if (!WorldNavigationJourneyPlanner.TryCreate(
                origin,
                destination,
                out var plan
            ) ||
            plan is null)
        {
            return false;
        }

        if (plan.IsSameRegion)
        {
            Clear();
            return true;
        }

        SetJourney(
            plan.Segments,
            plan.Destination,
            journeyTarget: null,
            hasTargetSegment: false
        );
        return true;
    }

    public bool SelectDestination(
        WorldBiome origin,
        WorldNavigationDestination target
    )
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!WorldNavigationJourneyPlanner.TryCreate(
                origin,
                target.Region,
                out var plan
            ) ||
            plan is null)
        {
            return false;
        }

        if (!target.HasTargetCell)
        {
            return SelectDestination(origin, target.Region);
        }

        SetJourney(
            plan.Segments,
            target.Region,
            target,
            hasTargetSegment: true
        );
        return true;
    }

    public bool TryAdvanceAt(GridPosition playerCell)
    {
        if (_selectedRouteId is null || IsTargetSegmentActive())
        {
            return false;
        }

        var progress = WorldNavigationRouteProgressPresenter.Create(
            _selectedRouteId,
            playerCell
        );
        if (!progress.IsArrived)
        {
            return false;
        }

        if (_activeJourneySegmentIndex < _journeySegments.Count - 1)
        {
            _activeJourneySegmentIndex++;
            _selectedRouteId = _journeySegments[
                _activeJourneySegmentIndex
            ].RouteId;
            return true;
        }

        if (!_hasTargetSegment || _journeyTarget is null)
        {
            return false;
        }

        _targetPath = null;
        _selectedRouteId = WorldNavigationTargetPath.RouteIdFor(
            _journeyTarget.Id
        );
        return true;
    }

    public void Clear()
    {
        _selectedRouteId = null;
        _journeySegments = Array.Empty<WorldNavigationRouteOption>();
        _activeJourneySegmentIndex = 0;
        _journeyDestinationRegion = null;
        _journeyTarget = null;
        _targetPath = null;
        _hasTargetSegment = false;
    }

    private void SetJourney(
        IReadOnlyList<WorldNavigationRouteOption> segments,
        WorldBiome destinationRegion,
        WorldNavigationDestination? journeyTarget,
        bool hasTargetSegment
    )
    {
        _journeySegments = segments;
        _activeJourneySegmentIndex = 0;
        _journeyDestinationRegion = destinationRegion;
        _journeyTarget = journeyTarget;
        _targetPath = null;
        _hasTargetSegment = hasTargetSegment;
        _selectedRouteId = segments.Count > 0
            ? segments[0].RouteId
            : TargetRouteId();
    }

    private WorldNavigationRouteProgress? CreateProgress(
        string playerLocationId,
        GridPosition playerCell,
        Func<GridPosition, bool> canOccupy
    )
    {
        if (_selectedRouteId is null)
        {
            return null;
        }

        if (IsTargetSegmentActive())
        {
            if (!EnsureTargetPath(
                    playerLocationId,
                    playerCell,
                    canOccupy
                ) ||
                _targetPath is null)
            {
                return WorldNavigationRouteProgressPresenter.CreatePath(
                    _selectedRouteId,
                    _journeyDestinationRegion ??
                        WorldDefinition.GetBiome(playerCell),
                    [],
                    playerCell
                );
            }

            return WorldNavigationRouteProgressPresenter.CreatePath(
                _targetPath.RouteId,
                _targetPath.Destination.Region,
                _targetPath.Path,
                playerCell
            );
        }

        return WorldNavigationRouteProgressPresenter.Create(
            _selectedRouteId,
            playerCell
        );
    }

    private bool EnsureTargetPath(
        string playerLocationId,
        GridPosition playerCell,
        Func<GridPosition, bool> canOccupy
    )
    {
        if (_journeyTarget is null)
        {
            return false;
        }

        var cachedPathUsable = _targetPath is not null &&
            _targetPath.LocationId == playerLocationId &&
            IsTargetPathUsable(_targetPath, playerCell, canOccupy);
        if (cachedPathUsable &&
            _targetPath is not null &&
            _targetPath.Path.Contains(playerCell))
        {
            return true;
        }

        if (!cachedPathUsable)
        {
            _targetPath = null;
        }

        if (WorldNavigationTargetPathPlanner.TryCreate(
                playerCell,
                _journeyTarget,
                playerLocationId,
                canOccupy,
                out var refreshedPath
            ) &&
            refreshedPath is not null)
        {
            _targetPath = refreshedPath;
            return true;
        }

        return _targetPath is not null;
    }

    private static bool IsTargetPathUsable(
        WorldNavigationTargetPath targetPath,
        GridPosition playerCell,
        Func<GridPosition, bool> canOccupy
    )
    {
        foreach (var cell in targetPath.Path)
        {
            if (cell == playerCell)
            {
                continue;
            }

            if (!canOccupy(cell))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTargetSegmentActive() =>
        _hasTargetSegment &&
        _selectedRouteId == TargetRouteId();

    private string? TargetRouteId() => _journeyTarget is null
        ? null
        : WorldNavigationTargetPath.RouteIdFor(_journeyTarget.Id);

    private int CurrentSegmentNumber(bool targetSegmentActive)
    {
        if (targetSegmentActive)
        {
            return _journeySegments.Count + 1;
        }

        return _activeJourneySegmentIndex + 1;
    }

    private int SegmentCount() =>
        _journeySegments.Count + (_hasTargetSegment ? 1 : 0);

    private static IReadOnlyList<WorldNavigationRouteOption> BuildStableRoutes()
    {
        var audits = WorldNavigationRouteAuditor.AuditAll();
        return audits
            .Select(CreateForwardOption)
            .Concat(audits.Select(CreateReverseOption))
            .ToArray();
    }

    private static WorldNavigationRouteOption CreateForwardOption(
        WorldNavigationRouteAudit audit
    ) => new(
        audit.ContractId,
        audit.ContractId,
        audit.FromRegion,
        audit.ToRegion,
        audit.Start,
        audit.End,
        audit.VisibleGuides.Count,
        audit.PathLength,
        IsReversed: false
    );

    private static WorldNavigationRouteOption CreateReverseOption(
        WorldNavigationRouteAudit audit
    ) => new(
        WorldNavigationRouteProgressPresenter.ReverseRouteId(audit.ContractId),
        audit.ContractId,
        audit.ToRegion,
        audit.FromRegion,
        audit.End,
        audit.Start,
        audit.VisibleGuides.Count,
        audit.PathLength,
        IsReversed: true
    );
}
