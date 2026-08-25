using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class RouteGuidanceOverlayTests
{
    [Fact]
    public void RouteOptionIdsFollowSelectionOptionOrder()
    {
        Assert.Equal(
            WorldNavigationRouteSelection.Routes.Select(option => option.RouteId),
            RouteGuidanceOverlay.RouteOptionIds
        );
        Assert.Equal(
            RouteGuidanceOverlay.RouteOptionIds,
            RouteGuidanceOverlay.RouteButtonIds
        );
    }

    [Fact]
    public void RouteItemsUseNeutralRegionKeysAndLocalizedEndpoints()
    {
        var locale = CreateLocale();
        var options = CreateBidirectionalOptions();
        var items = RouteGuidanceOverlay.CreateRouteItems(options, locale);

        Assert.Equal(12, items.Count);
        foreach (var item in items)
        {
            Assert.StartsWith("route_guidance.region.", item.FromRegionKey);
            Assert.StartsWith("route_guidance.region.", item.ToRegionKey);
            Assert.DoesNotContain("world.region.", item.FromRegionKey);
            Assert.DoesNotContain("world.region.", item.ToRegionKey);
            Assert.Equal(
                $"{item.FromRegionName} → {item.ToRegionName}",
                item.ButtonText
            );
        }

        Assert.Equal("Home → Lumen Village", items[0].ButtonText);
        Assert.Equal(
            "Lumen Village → Home",
            items[1].ButtonText
        );
    }

    [Fact]
    public void CurrentRegionFiltersOnlyOutgoingOptions()
    {
        var options = CreateBidirectionalOptions();

        Assert.Equal(
            6,
            RouteGuidanceOverlay
                .OptionsFromRegion(options, WorldBiome.LumenVillage)
                .Count
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(options, WorldBiome.Home)
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(
                options,
                WorldBiome.WhisperingWoods
            )
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(
                options,
                WorldBiome.StarfallMeadow
            )
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(
                options,
                WorldBiome.CrystalVale
            )
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(
                options,
                WorldBiome.MoonwaterWetlands
            )
        );
        Assert.Single(
            RouteGuidanceOverlay.OptionsFromRegion(
                options,
                WorldBiome.StarfallRuins
            )
        );
    }

    [Fact]
    public void SelectedRouteItemCanDescribeActiveRouteOutsideVisibleButtons()
    {
        var locale = CreateLocale();
        var options = CreateBidirectionalOptions();
        var visibleItems = RouteGuidanceOverlay.CreateRouteItems(
            RouteGuidanceOverlay.OptionsFromRegion(options, WorldBiome.Home),
            locale
        );
        var active = options.Single(option =>
            option.RouteId == WorldNavigationRouteProgressPresenter
                .ReverseRouteId("route_home_to_lumen_village")
        );

        var selected = RouteGuidanceOverlay.SelectedRouteItem(
            active.RouteId,
            active,
            visibleItems,
            locale
        );

        Assert.NotNull(selected);
        Assert.Equal(active.RouteId, selected.RouteId);
        Assert.Equal("Lumen Village", selected.FromRegionName);
        Assert.Equal("Home", selected.ToRegionName);
        Assert.Equal("Lumen Village → Home", selected.ButtonText);
        Assert.DoesNotContain(
            visibleItems,
            item => item.RouteId == active.RouteId
        );
    }

    [Fact]
    public void RequiredLocalizationKeysCoverEveryRouteEndpoint()
    {
        foreach (var biome in Enum.GetValues<WorldBiome>())
        {
            Assert.Contains(
                RouteGuidanceOverlay.RegionNameKey(biome),
                RouteGuidanceOverlay.RequiredLocalizationKeys
            );
        }

        foreach (var option in WorldNavigationRouteSelection.Routes)
        {
            Assert.Contains(
                RouteGuidanceOverlay.RegionNameKey(option.FromRegion),
                RouteGuidanceOverlay.RequiredLocalizationKeys
            );
            Assert.Contains(
                RouteGuidanceOverlay.RegionNameKey(option.ToRegion),
                RouteGuidanceOverlay.RequiredLocalizationKeys
            );
        }

        Assert.Contains(
            RouteGuidanceOverlay.MenuTitleKey,
            RouteGuidanceOverlay.RequiredLocalizationKeys
        );
        Assert.Contains(
            RouteGuidanceOverlay.RouteButtonTextKey,
            RouteGuidanceOverlay.RequiredLocalizationKeys
        );
        Assert.All(
            WorldNavigationRouteGuidanceHud.RequiredLocalizationKeys,
            key => Assert.Contains(
                key,
                RouteGuidanceOverlay.RequiredLocalizationKeys
            )
        );
        Assert.Contains(
            "route_guidance.journey_started",
            RouteGuidanceOverlay.RequiredLocalizationKeys
        );
        Assert.Contains(
            "route_guidance.already_in_region",
            RouteGuidanceOverlay.RequiredLocalizationKeys
        );
        Assert.DoesNotContain(
            RouteGuidanceOverlay.RequiredLocalizationKeys,
            key => key.StartsWith("world.region.", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void StartupFlagIsExact()
    {
        Assert.Equal("--open-route-guidance", RouteGuidanceStartup.OpenFlag);
        Assert.True(RouteGuidanceStartup.ShouldOpen(["--open-route-guidance"]));
        Assert.True(
            RouteGuidanceStartup.ShouldOpen(
                ["--debug-ui", "--open-route-guidance"]
            )
        );
        Assert.False(RouteGuidanceStartup.ShouldOpen([]));
        Assert.False(
            RouteGuidanceStartup.ShouldOpen(["--open-route-guidance-extra"])
        );
    }

    [Fact]
    public void StartupSelectionAcceptsOnlyKnownExactRouteOptionIds()
    {
        foreach (var option in WorldNavigationRouteSelection.Routes)
        {
            Assert.Equal(
                option.RouteId,
                RouteGuidanceStartup.SelectedRouteId(
                    [$"--select-route-guidance={option.RouteId}"]
                )
            );
        }

        var knownRouteId = WorldNavigationRouteSelection.Routes[0].RouteId;
        Assert.Null(RouteGuidanceStartup.SelectedRouteId([]));
        Assert.Null(RouteGuidanceStartup.SelectedRouteId(
            ["--select-route-guidance=route_missing"]
        ));
        Assert.Null(RouteGuidanceStartup.SelectedRouteId(
            [$"--select-route-guidance={knownRouteId}-extra"]
        ));
    }

    [Fact]
    public void StartupDestinationAcceptsOnlyKnownExactBiomeNames()
    {
        foreach (var biome in Enum.GetValues<WorldBiome>())
        {
            Assert.Equal(
                biome,
                RouteGuidanceStartup.SelectedDestination(
                    [$"--select-route-destination={biome}"]
                )
            );
        }

        Assert.Null(RouteGuidanceStartup.SelectedDestination([]));
        Assert.Null(RouteGuidanceStartup.SelectedDestination(
            ["--select-route-destination=whisperingwoods"]
        ));
        Assert.Null(RouteGuidanceStartup.SelectedDestination(
            ["--select-route-destination=999"]
        ));
        Assert.Null(RouteGuidanceStartup.SelectedDestination(
            ["--select-route-destination=Home-extra"]
        ));
    }

    [Fact]
    public void OverlayExposesRouteEventContract()
    {
        var overlay = typeof(RouteGuidanceOverlay);

        Assert.Equal(
            typeof(Action<string>),
            overlay.GetEvent(nameof(RouteGuidanceOverlay.RouteSelected))
                ?.EventHandlerType
        );
        Assert.Equal(
            typeof(Action),
            overlay.GetEvent(nameof(RouteGuidanceOverlay.RouteCleared))
                ?.EventHandlerType
        );
        Assert.Equal(
            typeof(Action),
            overlay.GetEvent(nameof(RouteGuidanceOverlay.CloseRequested))
                ?.EventHandlerType
        );
    }

    private static LocaleService CreateLocale()
    {
        var locale = new LocaleService();
        locale.LoadJson(
            LocaleService.English,
            """
            {
              "menu.route_guidance": "Route guidance",
              "route_guidance.subtitle": "Choose a route to make the road glow.",
              "route_guidance.route_button": "{0} → {1}",
              "route_guidance.current_route": "Current route: {0} → {1}",
              "route_guidance.no_current_route": "No active route",
              "route_guidance.clear": "Clear route",
              "route_guidance.close": "Close",
              "route_guidance.region.home": "Home",
              "route_guidance.region.village": "Lumen Village",
              "route_guidance.region.woods": "Whispering Woods",
              "route_guidance.region.meadow": "Starfall Meadow",
              "route_guidance.region.crystal": "Crystal Vale",
              "route_guidance.region.wetlands": "Moonwater Wetlands",
              "route_guidance.region.ruins": "Starfall Ruins"
            }
            """
        );
        locale.SetLocale(LocaleService.English);
        return locale;
    }

    private static IReadOnlyList<WorldNavigationRouteOption>
        CreateBidirectionalOptions()
    {
        return
        [
            ForwardOption(
                "route_home_to_lumen_village",
                WorldBiome.Home,
                WorldBiome.LumenVillage
            ),
            ReverseOption(
                "route_home_to_lumen_village",
                WorldBiome.LumenVillage,
                WorldBiome.Home
            ),
            ForwardOption(
                "route_lumen_village_to_whispering_woods",
                WorldBiome.LumenVillage,
                WorldBiome.WhisperingWoods
            ),
            ReverseOption(
                "route_lumen_village_to_whispering_woods",
                WorldBiome.WhisperingWoods,
                WorldBiome.LumenVillage
            ),
            ForwardOption(
                "route_lumen_village_to_starfall_meadow",
                WorldBiome.LumenVillage,
                WorldBiome.StarfallMeadow
            ),
            ReverseOption(
                "route_lumen_village_to_starfall_meadow",
                WorldBiome.StarfallMeadow,
                WorldBiome.LumenVillage
            ),
            ForwardOption(
                "route_lumen_village_to_crystal_vale",
                WorldBiome.LumenVillage,
                WorldBiome.CrystalVale
            ),
            ReverseOption(
                "route_lumen_village_to_crystal_vale",
                WorldBiome.CrystalVale,
                WorldBiome.LumenVillage
            ),
            ForwardOption(
                "route_lumen_village_to_moonwater_wetlands",
                WorldBiome.LumenVillage,
                WorldBiome.MoonwaterWetlands
            ),
            ReverseOption(
                "route_lumen_village_to_moonwater_wetlands",
                WorldBiome.MoonwaterWetlands,
                WorldBiome.LumenVillage
            ),
            ForwardOption(
                "route_lumen_village_to_starfall_ruins",
                WorldBiome.LumenVillage,
                WorldBiome.StarfallRuins
            ),
            ReverseOption(
                "route_lumen_village_to_starfall_ruins",
                WorldBiome.StarfallRuins,
                WorldBiome.LumenVillage
            )
        ];
    }

    private static WorldNavigationRouteOption ForwardOption(
        string routeId,
        WorldBiome fromRegion,
        WorldBiome toRegion
    ) => Option(
        routeId,
        routeId,
        fromRegion,
        toRegion,
        isReversed: false
    );

    private static WorldNavigationRouteOption ReverseOption(
        string contractId,
        WorldBiome fromRegion,
        WorldBiome toRegion
    ) => Option(
        WorldNavigationRouteProgressPresenter.ReverseRouteId(contractId),
        contractId,
        fromRegion,
        toRegion,
        isReversed: true
    );

    private static WorldNavigationRouteOption Option(
        string routeId,
        string contractId,
        WorldBiome fromRegion,
        WorldBiome toRegion,
        bool isReversed
    ) => new(
        routeId,
        contractId,
        fromRegion,
        toRegion,
        new GridPosition(0, 0),
        new GridPosition(1, 1),
        1,
        2,
        isReversed
    );
}
