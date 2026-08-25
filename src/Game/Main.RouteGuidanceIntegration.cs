using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private readonly WorldNavigationRouteSelection _routeGuidanceSelection =
        new();
    private RouteGuidanceOverlay? _routeGuidanceOverlay;
    private WorldNavigationRouteGuidanceHud? _routeGuidanceHud;
    private bool _routeGuidanceMovementSubscribed;

    private void EnsureRouteGuidanceHud()
    {
        if (_routeGuidanceHud is null)
        {
            _routeGuidanceHud = new WorldNavigationRouteGuidanceHud(
                _theme,
                _locale
            );
            _uiLayer.AddChild(_routeGuidanceHud);
        }

        if (!_routeGuidanceMovementSubscribed)
        {
            _session.PlayerMoved += RefreshRouteGuidanceProjection;
            _routeGuidanceMovementSubscribed = true;
        }

        RefreshRouteGuidanceProjection();
    }

    private void OpenRouteGuidance()
    {
        if (_routeGuidanceOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        var origin = RouteGuidanceOriginPresenter.Resolve(
            _session.PlayerLocationId,
            _session.PlayerCell
        );
        var availableRoutes = WorldNavigationRouteSelection.AvailableFrom(
            origin
        );
        var selectedRoute = WorldNavigationRouteSelection.Routes
            .FirstOrDefault(route =>
                route.RouteId == _routeGuidanceSelection.SelectedRouteId
            );

        _routeGuidanceOverlay = new RouteGuidanceOverlay(
            _theme,
            availableRoutes,
            _locale
        );
        _routeGuidanceOverlay.SetSelectedRoute(selectedRoute);
        _routeGuidanceOverlay.RouteSelected += OnRouteGuidanceSelected;
        _routeGuidanceOverlay.RouteCleared += OnRouteGuidanceCleared;
        _routeGuidanceOverlay.CloseRequested += CloseRouteGuidance;
        _uiLayer.AddChild(_routeGuidanceOverlay);
    }

    private void CloseRouteGuidance()
    {
        if (_routeGuidanceOverlay is null)
        {
            return;
        }

        _routeGuidanceOverlay.RouteSelected -= OnRouteGuidanceSelected;
        _routeGuidanceOverlay.RouteCleared -= OnRouteGuidanceCleared;
        _routeGuidanceOverlay.CloseRequested -= CloseRouteGuidance;
        FreeUi(_routeGuidanceOverlay);
        _routeGuidanceOverlay = null;
        if (RestorePauseAfterChild())
        {
            return;
        }

        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OnRouteGuidanceSelected(string routeId)
    {
        if (!_routeGuidanceSelection.Select(routeId))
        {
            return;
        }

        RefreshRouteGuidanceProjection();
    }

    private void OnRouteGuidanceCleared()
    {
        _routeGuidanceSelection.Clear();
        RefreshRouteGuidanceProjection();
    }

    private void StartRouteGuidanceJourney(WorldBiome destination)
    {
        var origin = RouteGuidanceOriginPresenter.Resolve(
            _session.PlayerLocationId,
            _session.PlayerCell
        );
        if (!_routeGuidanceSelection.SelectDestination(origin, destination))
        {
            return;
        }

        RefreshRouteGuidanceProjection();
        var destinationName = _locale.Tr(
            RouteGuidanceOverlay.RegionNameKey(destination)
        );
        if (origin == destination)
        {
            _hud?.ShowNoticeFormatted(
                "route_guidance.already_in_region",
                2.2,
                destinationName
            );
            return;
        }

        var segmentCount = _routeGuidanceSelection.GuidanceSegmentCount;
        _hud?.ShowNoticeFormatted(
            "route_guidance.journey_started",
            2.2,
            destinationName,
            segmentCount
        );
    }

    private void StartRouteGuidanceJourney(
        WorldNavigationDestination destination
    )
    {
        ArgumentNullException.ThrowIfNull(destination);

        var origin = RouteGuidanceOriginPresenter.Resolve(
            _session.PlayerLocationId,
            _session.PlayerCell
        );
        if (!_routeGuidanceSelection.SelectDestination(origin, destination))
        {
            return;
        }

        RefreshRouteGuidanceProjection();
        var destinationName = _locale.Tr(destination.NameKey);
        var segmentCount = _routeGuidanceSelection.GuidanceSegmentCount;
        _hud?.ShowNoticeFormatted(
            "route_guidance.journey_started",
            2.2,
            destinationName,
            segmentCount
        );
    }

    private void RefreshRouteGuidanceProjection()
    {
        var playerLocationId = _session.PlayerLocationId;
        var inWorld = playerLocationId == PlayerLocationIds.World;
        if (!inWorld &&
            !_routeGuidanceSelection.TryHandoffToLocation(playerLocationId))
        {
            _hud?.SetNavigationProgress(null);
            _routeGuidanceHud?.SetProjection(null);
            _routeGuidanceHud?.SetGuidanceVisible(false);
            return;
        }

        if (inWorld)
        {
            _routeGuidanceSelection.TryAdvanceAt(_session.PlayerCell);
        }

        var projection = _routeGuidanceSelection.Project(
            playerLocationId,
            _session.PlayerCell,
            CanOccupyRouteGuidanceCell
        );
        var progress = projection.Progress;
        if (inWorld)
        {
            _hud?.SetNavigationProgress(
                progress,
                projection.JourneyTarget
            );
        }
        else
        {
            _hud?.SetNavigationProgress(null);
        }
        _routeGuidanceHud?.SetProjection(projection);
        _routeGuidanceHud?.SetGuidanceVisible(true);
    }

    private bool CanOccupyRouteGuidanceCell(GridPosition cell)
    {
        if (cell == _session.PlayerCell)
        {
            return true;
        }

        return _session.CanOccupyNavigationCell(
            _session.PlayerLocationId,
            cell
        );
    }

    private void ResetRouteGuidance()
    {
        if (_routeGuidanceOverlay is not null)
        {
            _routeGuidanceOverlay.RouteSelected -= OnRouteGuidanceSelected;
            _routeGuidanceOverlay.RouteCleared -= OnRouteGuidanceCleared;
            _routeGuidanceOverlay.CloseRequested -= CloseRouteGuidance;
            FreeUi(_routeGuidanceOverlay);
            _routeGuidanceOverlay = null;
        }

        _routeGuidanceSelection.Clear();
        _hud?.SetNavigationProgress(null);
        FreeUi(_routeGuidanceHud);
        _routeGuidanceHud = null;
    }
}
