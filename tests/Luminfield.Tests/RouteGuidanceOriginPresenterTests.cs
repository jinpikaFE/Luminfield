using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class RouteGuidanceOriginPresenterTests
{
    [Fact]
    public void WorldLocationUsesTheRealPlayerBiome()
    {
        foreach (var route in WorldNavigationGuideCatalog.RouteContracts)
        {
            Assert.Equal(
                route.FromRegion,
                RouteGuidanceOriginPresenter.Resolve(
                    PlayerLocationIds.World,
                    route.Start
                )
            );
            Assert.Equal(
                route.ToRegion,
                RouteGuidanceOriginPresenter.Resolve(
                    PlayerLocationIds.World,
                    route.End
                )
            );
        }
    }

    [Theory]
    [InlineData(PlayerLocationIds.Cottage)]
    [InlineData(PlayerLocationIds.Greenhouse)]
    [InlineData(PlayerLocationIds.StarfeatherCoop)]
    [InlineData(PlayerLocationIds.MoonfleeceBarn)]
    public void HomesteadInteriorsResolveToHome(string locationId)
    {
        Assert.Equal(
            WorldBiome.Home,
            RouteGuidanceOriginPresenter.Resolve(
                locationId,
                new GridPosition(128, 80)
            )
        );
    }

    [Theory]
    [InlineData(PlayerLocationIds.MoonlitArchive)]
    [InlineData(PlayerLocationIds.MoonstoneWorkshop)]
    [InlineData(PlayerLocationIds.StarweaverTeaHouse)]
    [InlineData(PlayerLocationIds.TwilightEmporium)]
    [InlineData(PlayerLocationIds.StarlightPost)]
    [InlineData(PlayerLocationIds.StarfallWatch)]
    public void VillageInteriorsResolveToVillage(string locationId)
    {
        Assert.Equal(
            WorldBiome.LumenVillage,
            RouteGuidanceOriginPresenter.Resolve(
                locationId,
                new GridPosition(4, 4)
            )
        );
    }

    [Fact]
    public void FestivalInteriorsUseTheirWorldReturnBiome()
    {
        foreach (var festival in FestivalSpatialCatalog.All)
        {
            Assert.Equal(
                WorldDefinition.GetBiome(festival.WorldReturnCell),
                RouteGuidanceOriginPresenter.Resolve(
                    festival.LocationId,
                    festival.SafeArrivalCell
                )
            );
        }
    }

    [Theory]
    [InlineData(
        PlayerLocationIds.CrystalGrottoSurvey,
        WorldBiome.CrystalVale
    )]
    [InlineData(
        PlayerLocationIds.StarfallRuinsTrial,
        WorldBiome.StarfallRuins
    )]
    public void ExplorationInteriorsResolveToTheirOuterRegion(
        string locationId,
        WorldBiome expected
    )
    {
        Assert.Equal(
            expected,
            RouteGuidanceOriginPresenter.Resolve(
                locationId,
                new GridPosition(4, 4)
            )
        );
    }
}
