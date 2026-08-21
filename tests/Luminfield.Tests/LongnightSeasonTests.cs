using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class LongnightSeasonTests
{
    [Theory]
    [InlineData(42, true, "")]
    [InlineData(43, false, "notice.longnight_outdoor_planting")]
    [InlineData(56, false, "notice.longnight_outdoor_planting")]
    [InlineData(57, true, "")]
    [InlineData(99, false, "notice.longnight_outdoor_planting")]
    public void OutdoorPlantingFollowsLongnightBoundaries(
        int day,
        bool expectedSuccess,
        string expectedMessageKey
    )
    {
        var check = new FarmSystem().CheckCropPlanting(
            DataCatalog.CloudleafId,
            day
        );

        Assert.Equal(expectedSuccess, check.Succeeded);
        Assert.Equal(expectedMessageKey, check.MessageKey);
    }

    [Theory]
    [InlineData(43)]
    [InlineData(56)]
    [InlineData(99)]
    public void GreenhousePlantingRemainsAvailableDuringLongnight(int day)
    {
        var check = new FarmSystem(CultivationZoneCatalog.Greenhouse)
            .CheckCropPlanting(DataCatalog.RipplecapId, day);

        Assert.True(check.Succeeded);
    }

    [Fact]
    public void PreviewAndActionShareFrostboundRuleWithoutMutation()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(43, 8 * 60);
        var target = new GridPosition(22, 16);
        Assert.True(session.Farm.TryTill(target, session.Energy).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.CloudleafSeedId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.CloudleafSeedId
        ));
        var before = JsonSerializer.Serialize(session.Capture());

        var preview = session.PreviewSelectedTarget(target);
        var result = session.UseSelected(target);

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.Equal(
            "target.blocked.longnight_outdoor_planting",
            preview.LabelKey
        );
        Assert.False(result.Succeeded);
        Assert.Equal(
            "notice.longnight_outdoor_planting",
            result.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void CropPlantedBeforeLongnightSurvivesAndKeepsGrowing()
    {
        var farm = new FarmSystem();
        var target = new GridPosition(22, 16);
        Assert.True(farm.TryTill(target, 100).Succeeded);
        Assert.True(farm.TryPlant(
            target,
            DataCatalog.AuricShootId,
            42
        ).Succeeded);
        Assert.True(farm.TryWater(target, 100).Succeeded);

        Assert.Equal(1, farm.EndDay());

        var tile = farm.Tiles[target];
        Assert.Equal(DataCatalog.AuricShootId, tile.CropId);
        Assert.Equal(1, tile.WateredNights);
        Assert.True(farm.TryWater(target, 100).Succeeded);
    }

    [Fact]
    public void EmporiumUsesTwoIntentionalLongnightGreenhouseRotations()
    {
        Assert.Equal(
            new[]
            {
                DataCatalog.CloudleafSeedId,
                DataCatalog.StarbudSeedId,
                DataCatalog.MoonrootSeedId,
                DataCatalog.GlowpeaSeedId
            },
            TwilightEmporiumSystem.StockForDay(43)
        );
        Assert.Equal(
            new[]
            {
                DataCatalog.EmberbellSeedId,
                DataCatalog.DuskbellSeedId,
                DataCatalog.PrismcornSeedId,
                DataCatalog.DewmelonSeedId
            },
            TwilightEmporiumSystem.StockForDay(50)
        );
        Assert.Equal(
            TwilightEmporiumSystem.StockForDay(43),
            TwilightEmporiumSystem.StockForDay(99)
        );
        Assert.Equal(
            DataCatalog.LongnightGreenhouseSeedItemIds,
            TwilightEmporiumSystem.StockForDay(43)
                .Concat(TwilightEmporiumSystem.StockForDay(50))
                .ToArray()
        );
    }
}
