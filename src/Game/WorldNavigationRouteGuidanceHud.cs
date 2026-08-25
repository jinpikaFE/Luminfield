using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class WorldNavigationRouteGuidanceHud : PanelContainer
{
    public static IReadOnlyList<string> RequiredLocalizationKeys { get; } =
    [
        "route_guidance.hud.progress",
        "route_guidance.hud.off_route",
        "route_guidance.hud.arrived",
        "route_guidance.hud.journey_progress",
        "route_guidance.hud.journey_off_route",
        "route_guidance.hud.target_progress",
        "route_guidance.hud.target_off_route",
        "route_guidance.hud.enter_location"
    ];

    private readonly LocaleService _locale;
    private readonly Label _label;
    private WorldNavigationRouteSelectionProjection? _projection;
    private bool _guidanceVisible = true;

    public WorldNavigationRouteGuidanceHud(
        Theme theme,
        LocaleService locale
    )
    {
        Theme = theme;
        _locale = locale;
        Position = new Vector2(500, 163);
        Size = new Vector2(132, 44);
        ZIndex = 55;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101a31ed"),
                ThemeFactory.Gold,
                1,
                6,
                5
            )
        );
        _label = ThemeFactory.Label(size: 8, color: ThemeFactory.Gold);
        _label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        AddChild(_label);
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public void SetProjection(
        WorldNavigationRouteSelectionProjection? projection
    )
    {
        _projection = projection;
        Refresh();
    }

    public void SetGuidanceVisible(bool visible)
    {
        _guidanceVisible = visible;
        Refresh();
    }

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        var progress = _projection?.Progress;
        Visible = _guidanceVisible &&
            progress is { RouteExists: true };
        if (!Visible || _projection is null || progress is null)
        {
            return;
        }

        _label.Text = CreateText(_projection, _locale);
    }

    public static string CreateText(
        WorldNavigationRouteSelectionProjection projection,
        LocaleService locale
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(locale);

        var progress = projection.Progress;
        if (progress is null || !progress.RouteExists)
        {
            return string.Empty;
        }

        var targetRegion = progress.DestinationRegion ??
            progress.NextTarget?.Region ??
            WorldDefinition.GetBiome(progress.PlayerCell);
        var region = locale.Tr(
            RouteGuidanceOverlay.RegionNameKey(targetRegion)
        );
        var journeyDestination = projection.JourneyDestinationRegion ??
            targetRegion;
        var journeyRegion = locale.Tr(
            RouteGuidanceOverlay.RegionNameKey(journeyDestination)
        );
        var journeyName = DestinationName(projection, locale, journeyRegion);
        if (IsAtJourneyTarget(projection, progress.PlayerCell))
        {
            return locale.Tr(
                "route_guidance.hud.arrived",
                journeyName
            );
        }

        if (progress.IsArrived)
        {
            if (projection.RequiresLocationHandoff &&
                !projection.IsLocationTargetSegment)
            {
                return locale.Tr(
                    "route_guidance.hud.enter_location",
                    journeyName
                );
            }

            return locale.Tr(
                "route_guidance.hud.arrived",
                journeyName
            );
        }

        var direction = locale.Tr(
            RouteGuidanceText.DirectionKey(progress.MainDirection)
        );
        if (progress.DistanceFromRoute > 0)
        {
            direction = locale.Tr(
                RouteGuidanceText.DirectionKey(
                    progress.RecoveryDirection
                )
            );
            if (projection.IsFinalTargetSegment)
            {
                return locale.Tr(
                    "route_guidance.hud.target_off_route",
                    journeyName,
                    progress.DistanceFromRoute,
                    direction
                );
            }

            if (projection.IsMultiSegmentJourney)
            {
                return locale.Tr(
                    "route_guidance.hud.journey_off_route",
                    journeyName,
                    projection.CurrentSegmentNumber,
                    projection.SegmentCount,
                    progress.DistanceFromRoute,
                    direction
                );
            }

            return locale.Tr(
                "route_guidance.hud.off_route",
                progress.DistanceFromRoute,
                direction
            );
        }

        if (projection.IsFinalTargetSegment)
        {
            return locale.Tr(
                "route_guidance.hud.target_progress",
                journeyName,
                direction,
                progress.RemainingSteps
            );
        }

        if (projection.IsMultiSegmentJourney)
        {
            return locale.Tr(
                "route_guidance.hud.journey_progress",
                journeyName,
                projection.CurrentSegmentNumber,
                projection.SegmentCount,
                region,
                direction,
                progress.RemainingSteps
            );
        }

        return locale.Tr(
            "route_guidance.hud.progress",
            journeyName,
            direction,
            progress.RemainingSteps
        );
    }

    private static string DestinationName(
        WorldNavigationRouteSelectionProjection projection,
        LocaleService locale,
        string fallback
    )
    {
        if (projection.JourneyTarget is not { } target ||
            string.IsNullOrWhiteSpace(target.NameKey))
        {
            return fallback;
        }

        return locale.Tr(target.NameKey);
    }

    private static bool IsAtJourneyTarget(
        WorldNavigationRouteSelectionProjection projection,
        GridPosition playerCell
    )
    {
        if (projection.RequiresLocationHandoff &&
            !projection.IsLocationTargetSegment)
        {
            return false;
        }

        var targetCell = projection.ActiveTargetCell;
        if (targetCell is null &&
            projection.JourneyTarget is { } journeyTarget &&
            journeyTarget.TryGetTargetCell(
                projection.PlayerLocationId,
                out var resolvedTargetCell
            ))
        {
            targetCell = resolvedTargetCell;
        }

        if (targetCell is not GridPosition resolvedCell)
        {
            return false;
        }

        return WorldNavigationTargetPathPlanner.IsArrivalCell(
            resolvedCell,
            playerCell
        );
    }

}

public static class RouteGuidanceText
{
    public static string DirectionKey(
        WorldNavigationRouteDirection direction
    ) => direction switch
    {
        WorldNavigationRouteDirection.North =>
            "route_guidance.direction.north",
        WorldNavigationRouteDirection.South =>
            "route_guidance.direction.south",
        WorldNavigationRouteDirection.West =>
            "route_guidance.direction.west",
        WorldNavigationRouteDirection.East =>
            "route_guidance.direction.east",
        _ => "route_guidance.direction.none"
    };
}
