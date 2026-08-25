using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed record RouteGuidanceRouteItem(
    string RouteId,
    string FromRegionKey,
    string ToRegionKey,
    string FromRegionName,
    string ToRegionName,
    string ButtonText
);

public sealed partial class RouteGuidanceOverlay : FullScreenUi
{
    public const string MenuTitleKey = "menu.route_guidance";
    public const string SubtitleKey = "route_guidance.subtitle";
    public const string RouteButtonTextKey = "route_guidance.route_button";
    public const string CurrentRouteKey = "route_guidance.current_route";
    public const string NoCurrentRouteKey = "route_guidance.no_current_route";
    public const string ClearKey = "route_guidance.clear";
    public const string CloseKey = "route_guidance.close";

    public static IReadOnlyList<string> RouteOptionIds { get; } =
        WorldNavigationRouteSelection.Routes
            .Select(option => option.RouteId)
            .ToArray();

    public static IReadOnlyList<string> RouteButtonIds => RouteOptionIds;

    public static IReadOnlyList<string> RequiredLocalizationKeys { get; } =
    [
        MenuTitleKey,
        SubtitleKey,
        RouteButtonTextKey,
        CurrentRouteKey,
        NoCurrentRouteKey,
        ClearKey,
        CloseKey,
        "route_guidance.hud.progress",
        "route_guidance.hud.off_route",
        "route_guidance.hud.arrived",
        "route_guidance.hud.journey_progress",
        "route_guidance.hud.journey_off_route",
        "route_guidance.hud.target_progress",
        "route_guidance.hud.target_off_route",
        "route_guidance.hud.enter_location",
        "route_guidance.journey_started",
        "route_guidance.already_in_region",
        "route_guidance.region.home",
        "route_guidance.region.village",
        "route_guidance.region.woods",
        "route_guidance.region.meadow",
        "route_guidance.region.crystal",
        "route_guidance.region.wetlands",
        "route_guidance.region.ruins"
    ];

    private readonly LocaleService _locale;
    private readonly IReadOnlyList<WorldNavigationRouteOption> _options;
    private readonly Dictionary<string, Button> _routeButtons =
        new(StringComparer.Ordinal);
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label _selection;
    private readonly Button _clear;
    private readonly Button _close;
    private string? _selectedRouteId;
    private WorldNavigationRouteOption? _selectedRouteOption;

    public RouteGuidanceOverlay(
        Theme theme,
        LocaleService locale
    ) : this(theme, OptionsFromRegion(WorldBiome.LumenVillage), locale)
    {
    }

    public RouteGuidanceOverlay(
        Theme theme,
        IEnumerable<WorldNavigationRouteOption> options,
        LocaleService locale
    ) : base(theme)
    {
        _locale = locale;
        _options = options.ToArray();
        var routeRowCount = Math.Max(1, (_options.Count + 1) / 2);
        var singleRouteLayout = _options.Count == 1;

        AddChild(Dim(new Color(0.007f, 0.013f, 0.056f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(
                560,
                230 + ((routeRowCount - 1) * 48)
            )
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0c1836fc"),
                ThemeFactory.Mint,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Gold);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _subtitle = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _subtitle.CustomMinimumSize = new Vector2(522, 28);
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_subtitle);

        var routeGrid = new GridContainer
        {
            Columns = singleRouteLayout ? 1 : 2,
            CustomMinimumSize = new Vector2(
                522,
                (routeRowCount * 36) + ((routeRowCount - 1) * 6)
            ),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        routeGrid.AddThemeConstantOverride("h_separation", 7);
        routeGrid.AddThemeConstantOverride("v_separation", 6);
        column.AddChild(routeGrid);

        foreach (var option in _options)
        {
            var routeId = option.RouteId;
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(
                singleRouteLayout ? 522 : 256,
                36
            );
            button.ToggleMode = true;
            button.Pressed += () => SelectRoute(routeId);
            _routeButtons[routeId] = button;
            routeGrid.AddChild(button);
        }

        var statusPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(522, 34)
        };
        statusPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101f3ff2"),
                ThemeFactory.PanelEdge,
                1,
                6,
                6
            )
        );
        _selection = ThemeFactory.Label(size: 10, color: ThemeFactory.Ink);
        _selection.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusPanel.AddChild(_selection);
        column.AddChild(statusPanel);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 9);
        _clear = ThemeFactory.Button("");
        _clear.CustomMinimumSize = new Vector2(150, 26);
        _clear.Pressed += ClearRoute;
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 26);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_clear);
        actions.AddChild(_close);
        column.AddChild(actions);

        locale.LocaleChanged += RefreshText;
        RefreshText();
    }

    public event Action<string>? RouteSelected;
    public event Action? RouteCleared;
    public event Action? CloseRequested;

    public string? SelectedRouteId => _selectedRouteId;

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= RefreshText;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputSetup.UiCancel))
        {
            return;
        }

        CloseRequested?.Invoke();
        GetViewport().SetInputAsHandled();
    }

    public void SetSelectedRoute(string? routeId)
    {
        if (routeId is null)
        {
            _selectedRouteId = null;
            _selectedRouteOption = null;
            RefreshSelectionText(CreateRouteItems(_options, _locale));
            RefreshButtonStates();
            return;
        }

        _selectedRouteOption = ResolveRouteOption(routeId);
        _selectedRouteId = _selectedRouteOption?.RouteId;
        RefreshSelectionText(CreateRouteItems(_options, _locale));
        RefreshButtonStates();
    }

    public void SetSelectedRoute(WorldNavigationRouteOption? currentOption)
    {
        _selectedRouteOption = currentOption;
        _selectedRouteId = currentOption?.RouteId;
        RefreshSelectionText(CreateRouteItems(_options, _locale));
        RefreshButtonStates();
    }

    public void RefreshText()
    {
        var items = CreateRouteItems(_options, _locale);

        _title.Text = _locale.Tr(MenuTitleKey);
        _subtitle.Text = _locale.Tr(SubtitleKey);
        _clear.Text = _locale.Tr(ClearKey);
        _close.Text = _locale.Tr(CloseKey);

        foreach (var item in items)
        {
            if (_routeButtons.TryGetValue(item.RouteId, out var button))
            {
                button.Text = item.ButtonText;
            }
        }

        RefreshSelectionText(items);
        RefreshButtonStates();
    }

    public static IReadOnlyList<RouteGuidanceRouteItem> CreateRouteItems(
        LocaleService locale
    ) => CreateRouteItems(WorldNavigationRouteSelection.Routes, locale);

    public static IReadOnlyList<RouteGuidanceRouteItem> CreateRouteItems(
        IEnumerable<WorldNavigationRouteOption> options,
        LocaleService locale
    ) => options
        .Select(option => CreateRouteItem(option, locale))
        .ToArray();

    public static RouteGuidanceRouteItem CreateRouteItem(
        WorldNavigationRouteOption option,
        LocaleService locale
    )
    {
        var fromKey = RegionNameKey(option.FromRegion);
        var toKey = RegionNameKey(option.ToRegion);
        var fromName = locale.Tr(fromKey);
        var toName = locale.Tr(toKey);
        return new RouteGuidanceRouteItem(
            option.RouteId,
            fromKey,
            toKey,
            fromName,
            toName,
            locale.Tr(RouteButtonTextKey, fromName, toName)
        );
    }

    public static IReadOnlyList<WorldNavigationRouteOption> OptionsFromRegion(
        WorldBiome region
    ) => OptionsFromRegion(WorldNavigationRouteSelection.Routes, region);

    public static IReadOnlyList<WorldNavigationRouteOption> OptionsFromRegion(
        IEnumerable<WorldNavigationRouteOption> options,
        WorldBiome region
    ) => options
        .Where(option => option.FromRegion == region)
        .ToArray();

    public static RouteGuidanceRouteItem? SelectedRouteItem(
        string? routeId,
        WorldNavigationRouteOption? currentOption,
        IEnumerable<RouteGuidanceRouteItem> visibleItems,
        LocaleService locale
    )
    {
        if (routeId is null)
        {
            return null;
        }

        var visibleItem = visibleItems.FirstOrDefault(item =>
            item.RouteId == routeId
        );
        if (visibleItem is not null)
        {
            return visibleItem;
        }

        if (currentOption?.RouteId == routeId)
        {
            return CreateRouteItem(currentOption, locale);
        }

        var globalOption = WorldNavigationRouteSelection.Routes
            .FirstOrDefault(option => option.RouteId == routeId);
        return globalOption is null
            ? null
            : CreateRouteItem(globalOption, locale);
    }

    public static string RegionNameKey(WorldBiome biome) => biome switch
    {
        WorldBiome.Home => "route_guidance.region.home",
        WorldBiome.LumenVillage => "route_guidance.region.village",
        WorldBiome.WhisperingWoods => "route_guidance.region.woods",
        WorldBiome.StarfallMeadow => "route_guidance.region.meadow",
        WorldBiome.CrystalVale => "route_guidance.region.crystal",
        WorldBiome.MoonwaterWetlands => "route_guidance.region.wetlands",
        WorldBiome.StarfallRuins => "route_guidance.region.ruins",
        _ => throw new ArgumentOutOfRangeException(nameof(biome), biome, null)
    };

    private void SelectRoute(string routeId)
    {
        _selectedRouteOption = _options.FirstOrDefault(option =>
            option.RouteId == routeId
        );
        _selectedRouteId = _selectedRouteOption?.RouteId;
        RefreshSelectionText(CreateRouteItems(_options, _locale));
        RefreshButtonStates();
        if (_selectedRouteId is not null)
        {
            RouteSelected?.Invoke(_selectedRouteId);
        }
    }

    private void ClearRoute()
    {
        if (_selectedRouteId is null)
        {
            return;
        }

        _selectedRouteId = null;
        _selectedRouteOption = null;
        RefreshSelectionText(CreateRouteItems(_options, _locale));
        RefreshButtonStates();
        RouteCleared?.Invoke();
    }

    private void RefreshSelectionText(
        IReadOnlyList<RouteGuidanceRouteItem> items
    )
    {
        var selected = SelectedRouteItem(
            _selectedRouteId,
            _selectedRouteOption,
            items,
            _locale
        );
        _clear.Disabled = _selectedRouteId is null;
        _selection.Text = selected is null
            ? _locale.Tr(NoCurrentRouteKey)
            : _locale.Tr(
                CurrentRouteKey,
                selected.FromRegionName,
                selected.ToRegionName
            );
    }

    private void RefreshButtonStates()
    {
        foreach (var (routeId, button) in _routeButtons)
        {
            button.ButtonPressed = routeId == _selectedRouteId;
        }
    }

    private WorldNavigationRouteOption? ResolveRouteOption(string routeId)
    {
        var visibleOption = _options.FirstOrDefault(option =>
            option.RouteId == routeId
        );
        if (visibleOption is not null)
        {
            return visibleOption;
        }

        return WorldNavigationRouteSelection.Routes
            .FirstOrDefault(option => option.RouteId == routeId);
    }
}

public static class RouteGuidanceStartup
{
    public const string OpenFlag = "--open-route-guidance";
    public const string SelectionArgumentPrefix =
        "--select-route-guidance=";
    public const string DestinationArgumentPrefix =
        "--select-route-destination=";

    public static bool ShouldOpen(IEnumerable<string> arguments) =>
        arguments.Contains(OpenFlag, StringComparer.Ordinal);

    public static string? SelectedRouteId(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            if (!argument.StartsWith(
                    SelectionArgumentPrefix,
                    StringComparison.Ordinal
                ))
            {
                continue;
            }

            var routeId = argument[SelectionArgumentPrefix.Length..];
            if (RouteGuidanceOverlay.RouteOptionIds.Contains(
                    routeId,
                    StringComparer.Ordinal
                ))
            {
                return routeId;
            }
        }

        return null;
    }

    public static WorldBiome? SelectedDestination(
        IEnumerable<string> arguments
    )
    {
        foreach (var argument in arguments)
        {
            if (!argument.StartsWith(
                    DestinationArgumentPrefix,
                    StringComparison.Ordinal
                ))
            {
                continue;
            }

            var value = argument[DestinationArgumentPrefix.Length..];
            if (Enum.TryParse<WorldBiome>(
                    value,
                    ignoreCase: false,
                    out var destination
                ) &&
                Enum.IsDefined(destination))
            {
                return destination;
            }
        }

        return null;
    }
}
