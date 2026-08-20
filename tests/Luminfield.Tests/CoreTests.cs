using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class GameClockTests
{
    [Fact]
    public void AdvancesInTenMinuteTicksAndStopsAtNight()
    {
        var clock = new GameClock();

        Assert.True(clock.AdvanceRealTime(GameClock.SecondsPerTick));
        Assert.Equal(6 * 60 + 10, clock.MinuteOfDay);

        clock.AdvanceRealTime(24 * 60 * 60);
        Assert.Equal(GameClock.EndMinute, clock.MinuteOfDay);
        Assert.True(clock.EndOfDayReached);

        clock.StartNextDay();
        Assert.Equal(2, clock.Day);
        Assert.Equal(GameClock.StartMinute, clock.MinuteOfDay);
    }
}

public sealed class CalendarAndWeatherTests
{
    [Fact]
    public void CalendarWrapsAcrossSevenNamedWeekdays()
    {
        Assert.Equal(1, CalendarSystem.WeekNumber(1));
        Assert.Equal("calendar.weekday.1", CalendarSystem.WeekdayKey(1));
        Assert.Equal("calendar.weekday.7", CalendarSystem.WeekdayKey(7));
        Assert.Equal(2, CalendarSystem.WeekNumber(8));
        Assert.Equal("calendar.weekday.1", CalendarSystem.WeekdayKey(8));
    }

    [Theory]
    [InlineData(1, 1, CalendarSystem.GleamriseSeasonId, 1)]
    [InlineData(14, 1, CalendarSystem.GleamriseSeasonId, 14)]
    [InlineData(15, 1, CalendarSystem.RainveilSeasonId, 1)]
    [InlineData(28, 1, CalendarSystem.RainveilSeasonId, 14)]
    [InlineData(29, 1, CalendarSystem.StarharvestSeasonId, 1)]
    [InlineData(42, 1, CalendarSystem.StarharvestSeasonId, 14)]
    [InlineData(43, 1, CalendarSystem.LongnightSeasonId, 1)]
    [InlineData(56, 1, CalendarSystem.LongnightSeasonId, 14)]
    [InlineData(57, 2, CalendarSystem.GleamriseSeasonId, 1)]
    public void CalendarDerivesFourteenDaySeasonsAndYears(
        int day,
        int year,
        string seasonId,
        int seasonDay
    )
    {
        Assert.Equal(year, CalendarSystem.YearNumber(day));
        Assert.Equal(seasonId, CalendarSystem.SeasonId(day));
        Assert.Equal(seasonDay, CalendarSystem.SeasonDay(day));
        Assert.Equal(
            $"calendar.season.{seasonId}",
            CalendarSystem.SeasonNameKey(day)
        );
    }

    [Fact]
    public void FirstWeekContainsClearRainAndStardustWindWithForecast()
    {
        var firstWeek = Enumerable.Range(1, CalendarSystem.DaysPerWeek)
            .Select(WeatherSystem.WeatherForDay)
            .ToArray();

        Assert.Contains(DataCatalog.ClearWeatherId, firstWeek);
        Assert.Contains(DataCatalog.RainWeatherId, firstWeek);
        Assert.Contains(DataCatalog.StardustWindWeatherId, firstWeek);

        var weather = new WeatherSystem();
        weather.Reset(1);
        Assert.Equal(WeatherSystem.WeatherForDay(1), weather.CurrentId);
        Assert.Equal(WeatherSystem.WeatherForDay(2), weather.ForecastId);
        weather.AdvanceToDay(2);
        Assert.Equal(WeatherSystem.WeatherForDay(2), weather.CurrentId);
        Assert.Equal(WeatherSystem.WeatherForDay(3), weather.ForecastId);
    }

    [Fact]
    public void RainWatersNewSoilWithoutEnergyOrWaterUse()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 2;
        save.Weather = new WeatherSave
        {
            Day = 2,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        session.Restore(save);
        var position = new GridPosition(12, 16);

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.True(session.Farm.Tiles[position].Watered);
        Assert.Equal(GameSession.MaxWateringCanWater, session.WateringCanWater);
        Assert.Equal(GameSession.MaxEnergy - 2, session.Energy);

        session.Inventory.Add(DataCatalog.StarbudSeedId, 1);
        session.Inventory.Select(6);
        Assert.True(session.UseSelected(position).Succeeded);
        session.EndDay();
        Assert.Equal(1, session.Farm.Tiles[position].WateredNights);
    }
}

public sealed class FarmSystemTests
{
    [Fact]
    public void OnlyTheSixVisiblePlantingBedsAreTillable()
    {
        var farm = new FarmSystem();

        Assert.True(farm.IsTillable(new GridPosition(12, 16)));
        Assert.True(farm.IsTillable(new GridPosition(20, 20)));
        Assert.True(farm.IsTillable(new GridPosition(31, 20)));
        Assert.False(farm.IsTillable(new GridPosition(18, 16)));
        Assert.False(farm.IsTillable(new GridPosition(12, 18)));
        Assert.False(farm.IsTillable(new GridPosition(34, 20)));
    }

    [Fact]
    public void InvalidToolActionDoesNotMutateOrChargeEnergy()
    {
        var farm = new FarmSystem();
        var blocked = new GridPosition(5, 5);

        var blockedResult = farm.TryTill(blocked, 100);
        var noEnergyResult = farm.TryTill(new GridPosition(12, 16), 0);

        Assert.False(blockedResult.Succeeded);
        Assert.Equal(0, blockedResult.EnergyCost);
        Assert.False(noEnergyResult.Succeeded);
        Assert.Equal(0, noEnergyResult.EnergyCost);
        Assert.Empty(farm.Tiles);
    }

    [Fact]
    public void OnlyWateredCropsGrowAndWaterResets()
    {
        var farm = new FarmSystem();
        var wet = new GridPosition(12, 16);
        var dry = new GridPosition(13, 16);
        Assert.True(farm.TryTill(wet, 100).Succeeded);
        Assert.True(farm.TryTill(dry, 100).Succeeded);
        Assert.True(farm.TryPlant(wet, DataCatalog.StarbudId).Succeeded);
        Assert.True(farm.TryPlant(dry, DataCatalog.StarbudId).Succeeded);
        Assert.True(farm.TryWater(wet, 100).Succeeded);

        farm.EndDay();

        Assert.Equal(1, farm.Tiles[wet].WateredNights);
        Assert.Equal(0, farm.Tiles[dry].WateredNights);
        Assert.False(farm.Tiles[wet].Watered);
    }

    [Fact]
    public void StarbudAndMoonrootUseDifferentDataDrivenGrowthDurations()
    {
        var starbud = DataCatalog.Crop(DataCatalog.StarbudId);
        var moonroot = DataCatalog.Crop(DataCatalog.MoonrootId);

        Assert.True(starbud.IsMature(2));
        Assert.False(moonroot.IsMature(2));
        Assert.True(moonroot.IsMature(3));
    }

    [Fact]
    public void AllTwelveCatalogCropsPlantGrowHarvestAndRemainSellable()
    {
        Assert.Equal(12, DataCatalog.CropIds.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(12, DataCatalog.SeedItemIds.Distinct(StringComparer.Ordinal).Count());

        foreach (var cropId in DataCatalog.CropIds)
        {
            var crop = DataCatalog.Crop(cropId);
            var seed = DataCatalog.Item(crop.SeedItemId);
            var harvest = DataCatalog.Item(crop.HarvestItemId);
            Assert.Equal(ItemKind.Seed, seed.Kind);
            Assert.Equal(cropId, seed.CropId);
            Assert.Equal(ItemKind.Produce, harvest.Kind);
            Assert.Contains(crop.HarvestItemId, DataCatalog.SellableItemIds);
            Assert.True(seed.BuyPrice > 0);
            Assert.True(harvest.SellPrice > 0);

            var session = new GameSession();
            session.NewGame();
            var position = new GridPosition(12, 16);
            Assert.True(session.Farm.TryTill(position, session.Energy).Succeeded);
            Assert.True(session.Inventory.Add(crop.SeedItemId, 1));
            session.Inventory.Select(6);
            Assert.True(session.PreviewSelectedTarget(position).IsAvailable);
            Assert.True(session.UseSelected(position).Succeeded);
            Assert.Equal(cropId, session.Farm.Tiles[position].CropId);

            for (var night = 0; night < crop.MatureAfterWateredNights; night++)
            {
                Assert.True(session.Farm.TryWater(position, session.Energy).Succeeded);
                session.Farm.EndDay();
            }

            session.Inventory.Select(0);
            Assert.True(session.UseSelected(position).Succeeded);
            Assert.Equal(1, session.Inventory.Count(crop.HarvestItemId));
        }
    }

    [Fact]
    public void GleamriseSeedsSharePreviewPlantingAndPurchaseSeasonRules()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(15, 8 * 60);
        var position = new GridPosition(12, 16);
        Assert.True(session.Farm.TryTill(position, 100).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.DawnlaceSeedId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.DawnlaceSeedId
        ));
        var beforeEnergy = session.Energy;
        var beforeCoins = session.Coins;

        var preview = session.PreviewSelectedTarget(position);
        var planted = session.UseSelected(position);
        var bought = session.BuyItem(DataCatalog.DawnlaceSeedId);

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.Equal(
            "target.blocked.seed_out_of_season",
            preview.LabelKey
        );
        Assert.False(planted.Succeeded);
        Assert.Equal("notice.seed_out_of_season", planted.MessageKey);
        Assert.False(bought.Succeeded);
        Assert.Equal("shop.seed_out_of_season", bought.MessageKey);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.DawnlaceSeedId));
        Assert.Equal(beforeEnergy, session.Energy);
        Assert.Equal(beforeCoins, session.Coins);
        Assert.Null(session.Farm.Tiles[position].CropId);
        Assert.All(DataCatalog.CropIds.Take(8), cropId =>
            Assert.True(DataCatalog.Crop(cropId).IsAvailableOnDay(15))
        );
        Assert.All(DataCatalog.GleamriseCropIds, cropId =>
            Assert.False(DataCatalog.Crop(cropId).IsAvailableOnDay(15))
        );
    }

    [Fact]
    public void GlimmerpodReallyRegrowsWithoutConsumingAnotherSeed()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        var crop = DataCatalog.Crop(DataCatalog.GlimmerpodId);
        Assert.Equal(2, crop.RegrowthNights);
        Assert.True(session.Farm.TryTill(position, 100).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.GlimmerpodSeedId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.GlimmerpodSeedId
        ));
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.GlimmerpodSeedId));

        GrowToMaturity(
            session.Farm,
            position,
            crop,
            DataCatalog.ClearWeatherId
        );
        session.Inventory.Select(0);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(DataCatalog.GlimmerpodId, session.Farm.Tiles[position].CropId);
        Assert.Equal(
            crop.RegrowthWateredNights,
            session.Farm.Tiles[position].WateredNights
        );

        for (var night = 0; night < crop.RegrowthNights; night++)
        {
            Assert.True(session.Farm.TryWater(position, 100).Succeeded);
            session.Farm.EndDay();
        }

        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.GlimmerpodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.GlimmerpodSeedId));
        Assert.Equal(DataCatalog.GlimmerpodId, session.Farm.Tiles[position].CropId);
    }

    [Fact]
    public void ResonanceHarvestsUseWeatherPlantingDayAndCoordinatesDeterministically()
    {
        var scenarios = new[]
        {
            (
                DataCatalog.DawnlaceId,
                DataCatalog.RainWeatherId,
                DataCatalog.RainwovenDawnlaceId
            ),
            (
                DataCatalog.GlimmerpodId,
                DataCatalog.StardustWindWeatherId,
                DataCatalog.StarwindGlimmerpodId
            )
        };

        foreach (var (cropId, weatherId, expectedItemId) in scenarios)
        {
            var position = FindResonantPosition(cropId, weatherId, expectedItemId);
            var first = MatureFarm(position, cropId, weatherId, plantedDay: 1);
            var restoredEquivalent = MatureFarm(
                position,
                cropId,
                weatherId,
                plantedDay: 1
            );
            var clear = MatureFarm(
                position,
                cropId,
                DataCatalog.ClearWeatherId,
                plantedDay: 1
            );

            Assert.Equal(expectedItemId, first.HarvestItemIdAt(position));
            Assert.Equal(
                first.Tiles[position].ResonanceItemId,
                restoredEquivalent.Tiles[position].ResonanceItemId
            );
            Assert.Null(clear.Tiles[position].ResonanceItemId);
            Assert.Equal(1, first.Tiles[position].PlantedDay);
        }
    }

    [Fact]
    public void FullBackpackDoesNotHarvestOrClearResonanceState()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        var crop = DataCatalog.Crop(DataCatalog.DawnlaceId);
        session.Farm.Restore(
        [
            new FarmTileState
            {
                X = position.X,
                Y = position.Y,
                Tilled = true,
                CropId = crop.Id,
                WateredNights = crop.MatureAfterWateredNights,
                PlantedDay = 1,
                ResonanceItemId = DataCatalog.RainwovenDawnlaceId
            }
        ]);
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));

        var preview = session.PreviewSelectedTarget(position);
        var harvested = session.UseSelected(position);

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.Equal("target.blocked.backpack_full", preview.LabelKey);
        Assert.False(harvested.Succeeded);
        Assert.Equal(crop.Id, session.Farm.Tiles[position].CropId);
        Assert.Equal(
            crop.MatureAfterWateredNights,
            session.Farm.Tiles[position].WateredNights
        );
        Assert.Equal(
            DataCatalog.RainwovenDawnlaceId,
            session.Farm.Tiles[position].ResonanceItemId
        );
    }

    [Fact]
    public void ResonanceProduceSupportsImmediateSaleAndShipping()
    {
        var session = new GameSession();
        session.NewGame();
        var beforeCoins = session.Coins;
        Assert.True(session.Inventory.Add(DataCatalog.RainwovenDawnlaceId, 2));

        Assert.True(session.SellItem(
            DataCatalog.RainwovenDawnlaceId
        ).Succeeded);
        Assert.True(session.QueueForShipping(
            DataCatalog.RainwovenDawnlaceId
        ).Succeeded);
        var settlement = session.EndDay();

        Assert.Equal(1, settlement.TotalItems);
        Assert.Equal(
            beforeCoins + DataCatalog.Item(
                DataCatalog.RainwovenDawnlaceId
            ).SellPrice * 2,
            session.Coins
        );
    }

    private static GridPosition FindResonantPosition(
        string cropId,
        string weatherId,
        string expectedItemId
    )
    {
        for (var y = 15; y <= 21; y++)
        {
            for (var x = 11; x <= 32; x++)
            {
                var position = new GridPosition(x, y);
                if (!FarmSystem.IsPlantingBed(position))
                {
                    continue;
                }

                var farm = MatureFarm(
                    position,
                    cropId,
                    weatherId,
                    plantedDay: 1
                );
                if (farm.HarvestItemIdAt(position) == expectedItemId)
                {
                    return position;
                }
            }
        }

        throw new InvalidOperationException(
            $"No deterministic resonance position found for {cropId}."
        );
    }

    private static FarmSystem MatureFarm(
        GridPosition position,
        string cropId,
        string weatherId,
        int plantedDay
    )
    {
        var farm = new FarmSystem();
        Assert.True(farm.TryTill(position, 100).Succeeded);
        Assert.True(farm.TryPlant(position, cropId, plantedDay).Succeeded);
        GrowToMaturity(
            farm,
            position,
            DataCatalog.Crop(cropId),
            weatherId
        );
        return farm;
    }

    private static void GrowToMaturity(
        FarmSystem farm,
        GridPosition position,
        CropDefinition crop,
        string weatherId
    )
    {
        for (var night = 0; night < crop.MatureAfterWateredNights; night++)
        {
            Assert.True(farm.TryWater(position, 100).Succeeded);
            farm.EndDay(weatherId);
        }
    }
}

public sealed class CropQualityAndFertilizerTests
{
    [Fact]
    public void FertilizerPreviewAndActionShareTheSameEmptyTilledSoilRules()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        Assert.True(session.Inventory.Add(
            DataCatalog.StarsoilFertilizerId,
            1
        ));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        ));

        var outsideBed = new GridPosition(50, 50);
        var outsidePreview = session.PreviewSelectedTarget(outsideBed);
        Assert.Equal(TargetPreviewState.Blocked, outsidePreview.State);
        Assert.Equal(
            "target.blocked.fertilizer_needs_tilled",
            outsidePreview.LabelKey
        );
        Assert.False(session.UseSelected(outsideBed).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );

        var untilled = session.PreviewSelectedTarget(position);
        Assert.Equal(TargetPreviewState.Blocked, untilled.State);
        Assert.Equal(
            "target.blocked.fertilizer_needs_tilled",
            untilled.LabelKey
        );
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(position).Succeeded);
        var energyAfterTilling = session.Energy;
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        ));

        var ready = session.PreviewSelectedTarget(position);
        Assert.True(ready.IsAvailable);
        Assert.Equal(TargetPreviewKind.Soil, ready.Kind);
        Assert.Equal("target.action.fertilize", ready.LabelKey);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(energyAfterTilling, session.Energy);
        Assert.Equal(
            DataCatalog.StarsoilFertilizerId,
            session.Farm.Tiles[position].FertilizerId
        );
        Assert.Equal(
            0,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );

        Assert.True(session.Inventory.Add(
            DataCatalog.StarsoilFertilizerId,
            1
        ));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        ));
        var alreadyApplied = session.PreviewSelectedTarget(position);
        Assert.Equal(TargetPreviewState.Blocked, alreadyApplied.State);
        Assert.Equal("target.status.fertilized", alreadyApplied.LabelKey);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );

        Assert.True(session.Inventory.Add(DataCatalog.StarbudSeedId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudSeedId
        ));
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        ));
        var planted = session.PreviewSelectedTarget(position);
        Assert.Equal(TargetPreviewState.Blocked, planted.State);
        Assert.Equal(
            "target.blocked.fertilizer_before_planting",
            planted.LabelKey
        );
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );

        var crop = DataCatalog.Crop(DataCatalog.StarbudId);
        session.Farm.Tiles[position].WateredNights =
            crop.MatureAfterWateredNights;
        var mature = session.PreviewSelectedTarget(position);
        Assert.Equal(TargetPreviewState.Blocked, mature.State);
        Assert.Equal(
            "target.blocked.fertilizer_before_planting",
            mature.LabelKey
        );
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );
    }

    [Fact]
    public void FertilizedCropsYieldStableLuminousOrStarlightQualityOnce()
    {
        var farm = new FarmSystem();
        GridPosition? luminous = null;
        GridPosition? starlight = null;

        for (var y = 15; y <= 21; y++)
        {
            for (var x = 11; x <= 32; x++)
            {
                var position = new GridPosition(x, y);
                if (!farm.IsTillable(position))
                {
                    continue;
                }

                Assert.True(farm.TryTill(position, 100).Succeeded);
                Assert.True(farm.TryFertilize(
                    position,
                    DataCatalog.StarsoilFertilizerId
                ).Succeeded);
                Assert.True(farm.TryPlant(
                    position,
                    DataCatalog.StarbudId,
                    plantedDay: 3
                ).Succeeded);
                var quality = farm.HarvestQualityAt(position);
                if (quality == CropQuality.Luminous && luminous is null)
                {
                    luminous = position;
                }
                if (quality == CropQuality.Starlight && starlight is null)
                {
                    starlight = position;
                }
                if (luminous is not null && starlight is not null)
                {
                    break;
                }
            }

            if (luminous is not null && starlight is not null)
            {
                break;
            }
        }

        Assert.NotNull(luminous);
        Assert.NotNull(starlight);
        var targets = new[] { luminous!.Value, starlight!.Value };
        for (var night = 0; night < 2; night++)
        {
            foreach (var target in targets)
            {
                Assert.True(farm.TryWater(target, 100).Succeeded);
            }
            farm.EndDay();
        }

        var luminousHarvest = farm.TryHarvest(luminous.Value);
        var starlightHarvest = farm.TryHarvest(starlight.Value);
        Assert.Equal(
            DataCatalog.StarbudLuminousId,
            luminousHarvest.GrantedItemId
        );
        Assert.Equal(
            DataCatalog.StarbudStarlightId,
            starlightHarvest.GrantedItemId
        );
        Assert.Null(farm.Tiles[luminous.Value].FertilizerId);
        Assert.Null(farm.Tiles[starlight.Value].FertilizerId);

        Assert.True(farm.TryPlant(
            luminous.Value,
            DataCatalog.StarbudId,
            plantedDay: 4
        ).Succeeded);
        for (var night = 0; night < 2; night++)
        {
            Assert.True(farm.TryWater(luminous.Value, 100).Succeeded);
            farm.EndDay();
        }

        Assert.Equal(
            DataCatalog.StarbudId,
            farm.TryHarvest(luminous.Value).GrantedItemId
        );
    }

    [Fact]
    public void EveryCropHasStableIncreasingQualityVariants()
    {
        Assert.Equal(24, DataCatalog.QualityProduceItemIds.Count);
        Assert.Equal(
            24,
            DataCatalog.QualityProduceItemIds
                .Distinct(StringComparer.Ordinal)
                .Count()
        );

        foreach (var cropId in DataCatalog.CropIds)
        {
            var regular = DataCatalog.Item(cropId);
            var luminousId = DataCatalog.ProduceItemId(
                cropId,
                CropQuality.Luminous
            );
            var starlightId = DataCatalog.ProduceItemId(
                cropId,
                CropQuality.Starlight
            );
            var luminous = DataCatalog.Item(luminousId);
            var starlight = DataCatalog.Item(starlightId);

            Assert.Equal(cropId, luminous.BaseItemId);
            Assert.Equal(cropId, starlight.BaseItemId);
            Assert.Equal(CropQuality.Luminous, luminous.Quality);
            Assert.Equal(CropQuality.Starlight, starlight.Quality);
            Assert.True(luminous.SellPrice > regular.SellPrice);
            Assert.True(starlight.SellPrice > luminous.SellPrice);
            Assert.Contains(luminousId, DataCatalog.SellableItemIds);
            Assert.Contains(starlightId, DataCatalog.StorableItemIds);
        }
    }
}

public sealed class WorldDefinitionTests
{
    [Fact]
    public void LargeWorldIsSixByFourChunksAndSixteenTimesTheFarmArea()
    {
        Assert.Equal(6, WorldDefinition.ChunkColumns);
        Assert.Equal(4, WorldDefinition.ChunkRows);
        Assert.Equal(
            FarmSystem.MapWidth * FarmSystem.MapHeight * 16,
            WorldDefinition.Width * WorldDefinition.Height
        );
    }

    [Fact]
    public void SouthernFarmGateConnectsToEveryExplorationLandmark()
    {
        var start = new GridPosition(19, 30);
        var visited = new HashSet<GridPosition> { start };
        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);
        var directions = new[]
        {
            new GridPosition(1, 0),
            new GridPosition(-1, 0),
            new GridPosition(0, 1),
            new GridPosition(0, -1)
        };

        while (queue.TryDequeue(out var current))
        {
            foreach (var direction in directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (!visited.Contains(next) && !WorldDefinition.IsBlocked(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        foreach (var landmark in WorldDefinition.Landmarks)
        {
            Assert.Contains(
                directions.Select(direction => new GridPosition(
                    landmark.Position.X + direction.X,
                    landmark.Position.Y + direction.Y
                )),
                visited.Contains
            );
        }
    }

    [Fact]
    public void ExplorationDiscoveryUsesStableChunkIds()
    {
        var exploration = new ExplorationSystem();
        exploration.Reset();

        Assert.True(exploration.Discover(new GridPosition(70, 70)));
        Assert.False(exploration.Discover(new GridPosition(70, 70)));
        Assert.True(exploration.IsDiscovered(new ChunkPosition(2, 2)));

        var restored = new ExplorationSystem();
        restored.Restore(exploration.Capture());
        Assert.True(restored.IsDiscovered(new ChunkPosition(0, 0)));
        Assert.True(restored.IsDiscovered(new ChunkPosition(2, 2)));
    }

    [Fact]
    public void StreamingNeighborhoodNeverExceedsNineValidChunks()
    {
        var center = WorldDefinition.StreamingNeighborhood(new ChunkPosition(3, 2));
        var corner = WorldDefinition.StreamingNeighborhood(new ChunkPosition(0, 0));

        Assert.Equal(9, center.Count);
        Assert.Equal(4, corner.Count);
        Assert.All(center, chunk => Assert.True(WorldDefinition.IsValidChunk(chunk)));
        Assert.All(corner, chunk => Assert.True(WorldDefinition.IsValidChunk(chunk)));
    }
}

public sealed class InventoryTests
{
    [Fact]
    public void StacksItemsAndRejectsAnOverflowingFullBackpack()
    {
        var inventory = new Inventory();
        inventory.Reset();
        var availableStacks = Inventory.SlotCount - Inventory.StartingToolCount;

        Assert.True(inventory.Add(DataCatalog.StarbudSeedId, 99 * availableStacks));
        Assert.Equal(
            99 * availableStacks,
            inventory.Count(DataCatalog.StarbudSeedId)
        );
        Assert.False(inventory.Add(DataCatalog.StarbudSeedId, 1));
        Assert.Equal(
            99 * availableStacks,
            inventory.Count(DataCatalog.StarbudSeedId)
        );
    }

    [Fact]
    public void SelectionWrapsAcrossEightSlots()
    {
        var inventory = new Inventory();
        inventory.Reset();
        inventory.Select(0);
        inventory.SelectRelative(-1);
        Assert.Equal(7, inventory.SelectedIndex);
        inventory.SelectRelative(1);
        Assert.Equal(0, inventory.SelectedIndex);
    }

    [Fact]
    public void StartingToolsHaveStableHotbarOrderAndBackpackCapacity()
    {
        var inventory = new Inventory();
        inventory.Reset();

        Assert.Equal(24, inventory.Slots.Count);
        Assert.Equal(DataCatalog.HandId, inventory.Slots[0].ItemId);
        Assert.Equal(DataCatalog.ShovelId, inventory.Slots[1].ItemId);
        Assert.Equal(DataCatalog.MacheteId, inventory.Slots[2].ItemId);
        Assert.Equal(DataCatalog.WateringCanId, inventory.Slots[3].ItemId);
        Assert.Equal(DataCatalog.BucketId, inventory.Slots[4].ItemId);
        Assert.Equal(DataCatalog.FishingRodId, inventory.Slots[5].ItemId);
        Assert.All(
            inventory.Slots.Take(Inventory.StartingToolCount),
            slot => Assert.Equal(1, slot.Count)
        );
    }

    [Fact]
    public void AddManySequentiallySimulatesDuplicateItemsAndNeverPartiallyCommits()
    {
        var inventory = new Inventory();
        inventory.Reset();
        Assert.True(inventory.Add(DataCatalog.StarbudPreserveId, 98));
        Assert.True(inventory.Add(
            DataCatalog.DuskbellSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount - 1)
        ));
        var changes = 0;
        inventory.Changed += () => changes++;

        var failed = inventory.TryAddMany(
        [
            new CraftingIngredient(DataCatalog.StarbudPreserveId, 1),
            new CraftingIngredient(DataCatalog.StarbudPreserveId, 1)
        ]);

        Assert.False(failed);
        Assert.Equal(0, changes);
        Assert.Equal(98, inventory.Count(DataCatalog.StarbudPreserveId));

        Assert.True(inventory.Remove(DataCatalog.DuskbellSeedId, 99));
        changes = 0;
        var succeeded = inventory.TryAddMany(
        [
            new CraftingIngredient(DataCatalog.StarbudPreserveId, 1),
            new CraftingIngredient(DataCatalog.StarbudPreserveId, 99)
        ]);

        Assert.True(succeeded);
        Assert.Equal(1, changes);
        Assert.Equal(198, inventory.Count(DataCatalog.StarbudPreserveId));
    }
}

public sealed class CraftingAndStorageTests
{
    [Fact]
    public void StarsoilRecipeCraftsTwoConsumableFertilizersAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 1));

        var crafted = session.CraftItem(
            DataCatalog.StarsoilFertilizerRecipeId
        );

        Assert.True(crafted.Succeeded);
        Assert.Equal(
            2,
            session.Inventory.Count(DataCatalog.StarsoilFertilizerId)
        );
        Assert.Equal(
            DataCatalog.StarsoilFertilizerId,
            session.Inventory.Selected.ItemId
        );
        Assert.Equal(
            ItemKind.Fertilizer,
            DataCatalog.Item(DataCatalog.StarsoilFertilizerId).Kind
        );
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
    }

    [Fact]
    public void CraftingIsAtomicAndPromotesTheChestToTheHotbar()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.LumenwoodId, 5);
        session.Inventory.Add(DataCatalog.CrystalShardId, 2);

        var missing = session.CraftItem(DataCatalog.StarwovenChestRecipeId);

        Assert.False(missing.Succeeded);
        Assert.Equal(5, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(2, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarwovenChestId));

        session.Inventory.Add(DataCatalog.LumenwoodId, 1);
        var crafted = session.CraftItem(DataCatalog.StarwovenChestRecipeId);

        Assert.True(crafted.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarwovenChestId));
        Assert.Equal(
            DataCatalog.StarwovenChestId,
            session.Inventory.Selected.ItemId
        );
        Assert.InRange(
            session.Inventory.SelectedIndex,
            Inventory.StartingToolCount,
            Inventory.HotbarSlotCount - 1
        );
    }

    [Fact]
    public void PlacementPreviewAndActionUseTheSameFarmRules()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarwovenChestId, 3);
        session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        var valid = new GridPosition(25, 13);
        var plantingBed = new GridPosition(12, 16);
        var outside = new GridPosition(60, 60);

        var validPreview = session.PreviewSelectedTarget(valid);
        Assert.True(validPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.StorageChest, validPreview.Kind);
        Assert.Equal("target.action.place_chest", validPreview.LabelKey);

        var blockedPreview = session.PreviewSelectedTarget(plantingBed);
        Assert.Equal(TargetPreviewState.Blocked, blockedPreview.State);
        Assert.Equal("target.blocked.place_clear", blockedPreview.LabelKey);
        Assert.False(session.UseSelected(plantingBed).Succeeded);

        var outsidePreview = session.PreviewSelectedTarget(outside);
        Assert.Equal(TargetPreviewState.Blocked, outsidePreview.State);
        Assert.Equal("target.blocked.place_home", outsidePreview.LabelKey);
        Assert.False(session.UseSelected(outside).Succeeded);
        Assert.Equal(3, session.Inventory.Count(DataCatalog.StarwovenChestId));

        Assert.True(session.UseSelected(valid).Succeeded);
        Assert.True(session.Storage.HasChest(valid));
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarwovenChestId));

        session.Inventory.Select(0);
        var openPreview = session.PreviewSelectedTarget(valid);
        Assert.True(openPreview.IsAvailable);
        Assert.Equal("target.action.open_storage", openPreview.LabelKey);
    }

    [Fact]
    public void ChestTransfersDoNotLoseItemsWhenEitherSideIsFull()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(25, 13);
        session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        Assert.True(session.UseSelected(position).Succeeded);
        session.Inventory.Add(DataCatalog.StarbudSeedId, 2);

        Assert.True(session.StoreInChest(
            position,
            DataCatalog.StarbudSeedId
        ).Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudSeedId));
        Assert.Equal(
            1,
            session.Storage.ChestAt(position)!.Count(DataCatalog.StarbudSeedId)
        );
        Assert.True(session.TakeFromChest(
            position,
            DataCatalog.StarbudSeedId
        ).Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarbudSeedId));

        var chest = session.Storage.ChestAt(position)!;
        foreach (var itemId in DataCatalog.StorableItemIds.Take(
                     StorageChestState.SlotCount
                 ))
        {
            Assert.True(chest.Add(itemId, DataCatalog.Item(itemId).MaxStack));
        }
        var overflowItem = DataCatalog.StorableItemIds[StorageChestState.SlotCount];
        session.Inventory.Add(overflowItem, 1);

        var failed = session.StoreInChest(position, overflowItem);

        Assert.False(failed.Succeeded);
        Assert.Equal(1, session.Inventory.Count(overflowItem));
        Assert.Equal(0, chest.Count(overflowItem));
    }

    [Theory]
    [InlineData(
        DataCatalog.MoonstonePathRecipeId,
        DataCatalog.MoonstonePathId,
        0,
        1,
        4
    )]
    [InlineData(
        DataCatalog.StarwoodFenceRecipeId,
        DataCatalog.StarwoodFenceId,
        2,
        0,
        4
    )]
    [InlineData(
        DataCatalog.StarlightTorchRecipeId,
        DataCatalog.StarlightTorchId,
        1,
        1,
        2
    )]
    [InlineData(
        DataCatalog.DewfallSprinklerRecipeId,
        DataCatalog.DewfallSprinklerId,
        4,
        3,
        1
    )]
    public void FarmFacilitiesCraftAtomicallyAndUseStableItemIds(
        string recipeId,
        string outputItemId,
        int wood,
        int crystal,
        int outputCount
    )
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, wood));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, crystal));

        var result = session.CraftItem(recipeId);

        Assert.True(result.Succeeded);
        Assert.Equal(outputCount, session.Inventory.Count(outputItemId));
        Assert.Equal(outputItemId, session.Inventory.Selected.ItemId);
        Assert.Equal(ItemKind.Placeable, DataCatalog.Item(outputItemId).Kind);
    }

    [Fact]
    public void FarmFacilityPreviewAndActionShareSurfaceAndOccupancyRules()
    {
        var session = new GameSession();
        session.NewGame();
        var ground = new GridPosition(25, 13);
        var plantingBed = new GridPosition(15, 16);
        Assert.True(session.Inventory.Add(DataCatalog.MoonstonePathId, 2));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.MoonstonePathId));

        var pathPreview = session.PreviewSelectedTarget(ground);
        var wrongPathPreview = session.PreviewSelectedTarget(plantingBed);

        Assert.True(pathPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Path, pathPreview.Kind);
        Assert.Equal(TargetPreviewState.Blocked, wrongPathPreview.State);
        Assert.False(session.UseSelected(plantingBed).Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.MoonstonePathId));
        Assert.True(session.UseSelected(ground).Succeeded);
        Assert.True(session.FarmObjects.HasObject(ground));
        Assert.False(session.FarmObjects.BlocksMovement(ground));

        Assert.True(session.Inventory.Add(DataCatalog.DewfallSprinklerId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.DewfallSprinklerId));
        var sprinklerPreview = session.PreviewSelectedTarget(plantingBed);
        var wrongSprinklerPreview = session.PreviewSelectedTarget(
            new GridPosition(26, 13)
        );

        Assert.True(sprinklerPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Sprinkler, sprinklerPreview.Kind);
        Assert.Equal(TargetPreviewState.Blocked, wrongSprinklerPreview.State);
        Assert.True(session.UseSelected(plantingBed).Succeeded);
        Assert.True(session.FarmObjects.BlocksMovement(plantingBed));

        Assert.True(session.Inventory.Add(DataCatalog.StarwovenChestId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId));
        Assert.False(session.UseSelected(ground).Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarwovenChestId));
    }

    [Fact]
    public void DewfallSprinklerWatersFourAdjacentTilesBeforeNightGrowth()
    {
        var session = new GameSession();
        session.NewGame();
        var sprinkler = new GridPosition(15, 16);
        GridPosition[] wateredTargets =
        [
            new(15, 15),
            new(16, 16),
            new(15, 17),
            new(14, 16)
        ];
        var outsideRange = new GridPosition(12, 16);
        foreach (var target in wateredTargets.Append(outsideRange))
        {
            Assert.True(session.Farm.TryTill(target, 100).Succeeded);
            Assert.True(session.Farm.TryPlant(
                target,
                DataCatalog.StarbudId
            ).Succeeded);
        }

        Assert.True(session.Inventory.Add(DataCatalog.DewfallSprinklerId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.DewfallSprinklerId));
        Assert.True(session.UseSelected(sprinkler).Succeeded);

        session.EndDay();

        Assert.All(wateredTargets, target =>
        {
            var tile = session.Farm.Tiles[target];
            Assert.Equal(1, tile.WateredNights);
        });
        Assert.Equal(0, session.Farm.Tiles[outsideRange].WateredNights);
    }
}

public sealed class OrchardSystemTests
{
    [Fact]
    public void MoonplumTreesUsePreviewGrowthHarvestAndRegrowthRules()
    {
        var session = new GameSession();
        session.NewGame();
        var treeCell = new GridPosition(23, 13);
        var beforeEnergy = session.Energy;
        Assert.True(session.Inventory.Add(DataCatalog.MoonplumSaplingId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonplumSaplingId
        ));

        var placementPreview = session.PreviewSelectedTarget(treeCell);
        var planted = session.UseSelected(treeCell);

        Assert.True(placementPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.FruitTree, placementPreview.Kind);
        Assert.Equal("target.action.plant_tree", placementPreview.LabelKey);
        Assert.True(planted.Succeeded);
        Assert.Equal("notice.fruit_tree_planted", planted.MessageKey);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonplumSaplingId));
        Assert.True(session.Orchard.HasFruitTree(treeCell));
        Assert.True(session.Orchard.BlocksMovement(treeCell));
        Assert.Equal(beforeEnergy, session.Energy);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(treeCell);
        var wrongToolAction = session.UseSelected(treeCell);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal("target.need.hand", wrongTool.LabelKey);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);

        session.Inventory.Select(0);
        var growing = session.PreviewSelectedTarget(treeCell);
        Assert.Equal(TargetPreviewState.Blocked, growing.State);
        Assert.Equal("target.status.fruit_tree_growing", growing.LabelKey);
        Assert.Equal(
            "notice.fruit_tree_growing",
            session.UseSelected(treeCell).MessageKey
        );

        for (var night = 0;
             night < DataCatalog.FruitTree(DataCatalog.MoonplumTreeId)
                 .MatureAfterNights;
             night++)
        {
            session.EndDay();
        }

        var ready = session.PreviewSelectedTarget(treeCell);
        var harvested = session.UseSelected(treeCell);
        Assert.True(ready.IsAvailable);
        Assert.Equal("target.action.harvest_fruit", ready.LabelKey);
        Assert.True(harvested.Succeeded);
        Assert.Equal(DataCatalog.MoonplumId, harvested.GrantedItemId);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonplumId));
        Assert.False(session.Orchard.FruitTreeAt(treeCell)!.FruitReady);

        var recovering = session.PreviewSelectedTarget(treeCell);
        Assert.Equal(TargetPreviewState.Blocked, recovering.State);
        Assert.Equal(
            "target.status.fruit_tree_recovering",
            recovering.LabelKey
        );

        for (var night = 0;
             night < DataCatalog.FruitTree(DataCatalog.MoonplumTreeId)
                 .RegrowthNights;
             night++)
        {
            session.EndDay();
        }

        Assert.True(session.Orchard.FruitTreeAt(treeCell)!.FruitReady);
        Assert.True(session.UseSelected(treeCell).Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.MoonplumId));
    }

    [Fact]
    public void FruitTreePlacementRejectsOccupiedFarmAndWorldCells()
    {
        var session = new GameSession();
        session.NewGame();
        var plantingBed = new GridPosition(12, 16);
        var chestCell = new GridPosition(25, 13);
        var farmObjectCell = new GridPosition(26, 13);
        var treeCell = new GridPosition(23, 13);
        var tilledCell = new GridPosition(13, 16);
        var outside = new GridPosition(60, 60);
        Assert.True(session.Inventory.Add(DataCatalog.StarwovenChestId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarwovenChestId
        ));
        Assert.True(session.UseSelected(chestCell).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.MoonstonePathId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonstonePathId
        ));
        Assert.True(session.UseSelected(farmObjectCell).Succeeded);
        Assert.True(session.Farm.TryTill(tilledCell, session.Energy).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.MoonplumSaplingId, 8));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonplumSaplingId
        ));

        Assert.Equal(
            "target.blocked.sapling_ground",
            session.PreviewSelectedTarget(plantingBed).LabelKey
        );
        Assert.False(session.UseSelected(plantingBed).Succeeded);
        Assert.False(session.UseSelected(chestCell).Succeeded);
        Assert.False(session.UseSelected(farmObjectCell).Succeeded);
        Assert.False(session.UseSelected(tilledCell).Succeeded);
        Assert.False(session.UseSelected(outside).Succeeded);

        Assert.True(session.UseSelected(treeCell).Succeeded);
        Assert.False(session.UseSelected(treeCell).Succeeded);
        Assert.Equal(7, session.Inventory.Count(DataCatalog.MoonplumSaplingId));
        Assert.Single(session.Orchard.FruitTrees);
    }

    [Fact]
    public void GlowcombHivesCraftPlaceProduceAndCollectAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        var treeCell = new GridPosition(23, 13);
        var hiveCell = new GridPosition(27, 13);
        Assert.True(session.Inventory.Add(DataCatalog.MoonplumSaplingId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonplumSaplingId
        ));
        Assert.True(session.UseSelected(treeCell).Succeeded);
        GrowMoonplumTree(session);
        session.Inventory.Select(0);
        Assert.True(session.UseSelected(treeCell).Succeeded);
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 8));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 2));

        var crafted = session.CraftItem(DataCatalog.GlowcombHiveRecipeId);
        Assert.True(crafted.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.GlowcombHiveId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonplumId));

        var failedOnTree = session.UseSelected(treeCell);
        Assert.False(failedOnTree.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.GlowcombHiveId));

        var placed = session.UseSelected(hiveCell);
        Assert.True(placed.Succeeded);
        Assert.True(session.FarmObjects.HasObject(hiveCell));
        Assert.True(session.Orchard.HasBeehive(hiveCell));
        Assert.True(session.FarmObjects.BlocksMovement(hiveCell));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.GlowcombHiveId));

        session.Inventory.Select(0);
        var brewing = session.PreviewSelectedTarget(hiveCell);
        Assert.Equal(TargetPreviewState.Blocked, brewing.State);
        Assert.Equal("target.status.beehive_brewing", brewing.LabelKey);
        session.EndDay();
        session.EndDay();
        Assert.True(session.Orchard.BeehiveAt(hiveCell)!.HasHoney);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(hiveCell);
        var wrongToolAction = session.UseSelected(hiveCell);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);
        Assert.True(session.Orchard.BeehiveAt(hiveCell)!.HasHoney);

        session.Inventory.Select(0);
        var ready = session.PreviewSelectedTarget(hiveCell);
        var collected = session.UseSelected(hiveCell);
        Assert.True(ready.IsAvailable);
        Assert.Equal("target.action.collect_honey", ready.LabelKey);
        Assert.True(collected.Succeeded);
        Assert.Equal(DataCatalog.StarhoneyId, collected.GrantedItemId);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarhoneyId));
        Assert.False(session.Orchard.BeehiveAt(hiveCell)!.HasHoney);

        var lonely = new GameSession();
        lonely.NewGame();
        var lonelyHiveCell = new GridPosition(31, 13);
        Assert.True(lonely.Inventory.Add(DataCatalog.GlowcombHiveId, 1));
        Assert.True(lonely.Inventory.PromoteToHotbar(
            DataCatalog.GlowcombHiveId
        ));
        Assert.True(lonely.UseSelected(lonelyHiveCell).Succeeded);
        lonely.Inventory.Select(0);
        Assert.Equal(
            "target.status.beehive_needs_tree",
            lonely.PreviewSelectedTarget(lonelyHiveCell).LabelKey
        );
        lonely.EndDay();
        lonely.EndDay();
        Assert.False(lonely.Orchard.BeehiveAt(lonelyHiveCell)!.HasHoney);
        Assert.Equal(0, lonely.Orchard.BeehiveAt(lonelyHiveCell)!.ProgressNights);

        var full = SessionWithReadyHive(treeCell, hiveCell);
        FillBackpack(full.Inventory);
        full.Inventory.Select(0);
        var failedFull = full.UseSelected(hiveCell);
        Assert.False(failedFull.Succeeded);
        Assert.Equal("notice.inventory_full", failedFull.MessageKey);
        Assert.Equal(1, full.Orchard.BeehiveAt(hiveCell)!.PendingHoney);
        Assert.Equal(0, full.Inventory.Count(DataCatalog.StarhoneyId));
    }

    private static void GrowMoonplumTree(GameSession session)
    {
        for (var night = 0;
             night < DataCatalog.FruitTree(DataCatalog.MoonplumTreeId)
                 .MatureAfterNights;
             night++)
        {
            session.EndDay();
        }
    }

    private static GameSession SessionWithReadyHive(
        GridPosition treeCell,
        GridPosition hiveCell
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = treeCell.X,
                Y = treeCell.Y,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                PendingHoney = 1
            }
        ];
        session.Restore(save);
        return session;
    }

    private static void FillBackpack(Inventory inventory)
    {
        var fillerSlots = DataCatalog.StorableItemIds
            .Where(itemId => itemId != DataCatalog.StarhoneyId)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .Select(itemId => new InventorySlot
            {
                ItemId = itemId,
                Count = DataCatalog.Item(itemId).MaxStack
            });
        inventory.Restore(fillerSlots, 0);
    }
}

public sealed class AnimalSystemTests
{
    [Fact]
    public void StarfeatherCoopFeedPetEggAndProcessorLoopIsAtomic()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Coins = AnimalCatalog.CoopBuildCostCoins;
        session.Restore(save);
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 10));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 3));
        session.Inventory.Select(0);

        var buildPreview = session.PreviewSelectedTarget(AnimalCatalog.CoopCell);
        var built = session.UseSelected(AnimalCatalog.CoopCell);

        Assert.True(buildPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.ChickenCoop, buildPreview.Kind);
        Assert.Equal("target.action.build_coop", buildPreview.LabelKey);
        Assert.True(built.Succeeded);
        Assert.True(session.Animals.CoopBuilt);
        Assert.Equal(0, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Single(session.Animals.Chickens);

        var missingFeed = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.False(missingFeed.Succeeded);
        Assert.Equal("animal.chicken.need_feed", missingFeed.MessageKey);
        Assert.Equal(0, session.Animals.FirstChicken!.Affection);
        Assert.Equal(0, session.Animals.FirstChicken.PendingEggs);

        var missingFeedCraft = session.CraftItem(
            DataCatalog.StargrainFeedRecipeId
        );
        Assert.False(missingFeedCraft.Succeeded);
        Assert.DoesNotContain(
            session.Capture().GleamriseSeason.Counters,
            counter => counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterAnimalFeedPrepared
        );

        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.CloudleafId, 1));
        var craftedFeed = session.CraftItem(DataCatalog.StargrainFeedRecipeId);
        Assert.True(craftedFeed.Succeeded);
        Assert.Equal(3, session.Inventory.Count(DataCatalog.StargrainFeedId));
        Assert.Equal(
            1,
            session.Capture().GleamriseSeason.Counters.Single(counter =>
                counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterAnimalFeedPrepared
            ).Count
        );
        session.Inventory.Select(0);

        var feedPreview = session.PreviewSelectedTarget(AnimalCatalog.CoopCell);
        var fed = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.True(feedPreview.IsAvailable);
        Assert.Equal("target.action.feed_chicken", feedPreview.LabelKey);
        Assert.True(fed.Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StargrainFeedId));
        Assert.Equal(
            AnimalSystem.FeedAffectionGain,
            session.Animals.FirstChicken.Affection
        );
        Assert.Equal(session.Clock.Day, session.Animals.FirstChicken.LastFedDay);

        var repeatFeed = session.Animals.FeedFirstChicken(
            session.Inventory,
            session.Clock.Day
        );
        Assert.False(repeatFeed.Succeeded);
        Assert.Equal("animal.chicken.already_fed", repeatFeed.MessageKey);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StargrainFeedId));

        var petted = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.True(petted.Succeeded);
        Assert.Equal("animal.chicken.petted", petted.MessageKey);
        Assert.Equal(
            AnimalSystem.FeedAffectionGain + AnimalSystem.PetAffectionGain,
            session.Animals.FirstChicken.Affection
        );

        var repeatCare = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.False(repeatCare.Succeeded);
        Assert.Equal("animal.chicken.already_cared", repeatCare.MessageKey);
        Assert.Equal(0, session.Animals.FirstChicken.PendingEggs);

        session.EndDay();
        Assert.Equal(1, session.Animals.FirstChicken.PendingEggs);

        session.Inventory.Select(1);
        var wrongToolPreview = session.PreviewSelectedTarget(AnimalCatalog.CoopCell);
        var wrongTool = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.False(wrongTool.Succeeded);
        Assert.Equal("notice.needs_hand", wrongTool.MessageKey);
        Assert.Equal(1, session.Animals.FirstChicken.PendingEggs);

        session.Inventory.Select(0);
        var collectPreview = session.PreviewSelectedTarget(AnimalCatalog.CoopCell);
        var collected = session.UseSelected(AnimalCatalog.CoopCell);
        Assert.True(collectPreview.IsAvailable);
        Assert.Equal("target.action.collect_eggs", collectPreview.LabelKey);
        Assert.True(collected.Succeeded);
        Assert.Equal(DataCatalog.StarfeatherEggId, collected.GrantedItemId);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarfeatherEggId));
        Assert.Equal(0, session.Animals.FirstChicken.PendingEggs);
        Assert.Equal(
            1,
            session.Capture().GleamriseSeason.Counters.Single(counter =>
                counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterAnimalFirstEgg
            ).Count
        );

        var full = SessionWithPendingEgg();
        FillBackpackExceptEggs(full.Inventory);
        full.Inventory.Select(0);
        var failedFull = full.UseSelected(AnimalCatalog.CoopCell);
        Assert.False(failedFull.Succeeded);
        Assert.Equal("notice.inventory_full", failedFull.MessageKey);
        Assert.Equal(1, full.Animals.FirstChicken!.PendingEggs);
        Assert.Equal(0, full.Inventory.Count(DataCatalog.StarfeatherEggId));
        Assert.DoesNotContain(
            full.Capture().GleamriseSeason.Counters,
            counter => counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterAnimalFirstEgg
        );

        Assert.True(session.Inventory.Add(DataCatalog.StarfeatherEggId, 1));
        var started = session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.GlowcustardRecipeId
        );
        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarfeatherEggId));
        session.EndDay();
        var processed = session.CollectProcessedItem(
            ProcessorCatalog.PrismPreserveVatId
        );
        Assert.True(processed.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.GlowcustardId));
    }

    [Fact]
    public void AnimalSaveRoundTripsAndFiltersConflictingCells()
    {
        var empty = AnimalSystem.NormalizeSave(null);
        Assert.False(empty.CoopBuilt);
        Assert.Empty(empty.Chickens);

        var normalized = AnimalSystem.NormalizeSave(new AnimalSave
        {
            CoopBuilt = true,
            Chickens =
            [
                new StarfeatherChickenSave
                {
                    ChickenId = "unknown_chicken",
                    PendingEggs = 2
                },
                new StarfeatherChickenSave
                {
                    ChickenId = AnimalCatalog.FirstChickenId,
                    Affection = 500,
                    LastFedDay = -8,
                    LastPettedDay = -3,
                    PendingEggs = 9,
                    MoodId = "glitched"
                },
                new StarfeatherChickenSave
                {
                    ChickenId = AnimalCatalog.FirstChickenId,
                    Affection = 1
                }
            ]
        });
        var chicken = Assert.Single(normalized.Chickens);
        Assert.Equal(AnimalCatalog.FirstChickenId, chicken.ChickenId);
        Assert.Equal(AnimalSystem.MaxAffection, chicken.Affection);
        Assert.Equal(0, chicken.LastFedDay);
        Assert.Equal(0, chicken.LastPettedDay);
        Assert.Equal(AnimalSystem.MaxPendingEggs, chicken.PendingEggs);
        Assert.Equal(AnimalMoodIds.Content, chicken.MoodId);

        var fallback = AnimalSystem.NormalizeSave(new AnimalSave
        {
            CoopBuilt = true,
            Chickens =
            [
                new StarfeatherChickenSave
                {
                    ChickenId = "unknown_chicken"
                }
            ]
        });
        Assert.Equal(AnimalCatalog.FirstChickenId, Assert.Single(fallback.Chickens).ChickenId);

        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Animals = new AnimalSave
        {
            CoopBuilt = true,
            Chickens =
            [
                new StarfeatherChickenSave
                {
                    ChickenId = AnimalCatalog.FirstChickenId,
                    PendingEggs = 1,
                    MoodId = AnimalMoodIds.Happy
                }
            ]
        };
        save.Storage.Chests =
        [
            new PlacedChestSave
            {
                X = AnimalCatalog.CoopCell.X,
                Y = AnimalCatalog.CoopCell.Y
            },
            new PlacedChestSave
            {
                X = 25,
                Y = 13
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = 29,
                Y = 14,
                ItemId = DataCatalog.MoonstonePathId
            },
            new PlacedFarmObjectSave
            {
                X = 27,
                Y = 13,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = 30,
                Y = 14,
                TreeId = DataCatalog.MoonplumTreeId,
                FruitReady = true
            },
            new FruitTreeSave
            {
                X = 23,
                Y = 13,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = 27,
                Y = 13,
                PendingHoney = 1
            }
        ];

        var servicePath = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-animal-save-{Guid.NewGuid():N}.json"
        );
        try
        {
            var service = new SaveService(servicePath);
            service.Save(save);

            var result = service.Load();

            Assert.Equal(SaveLoadStatus.Loaded, result.Status);
            Assert.NotNull(result.Save);
            Assert.Single(result.Save.Storage.Chests);
            Assert.Equal(25, result.Save.Storage.Chests[0].X);
            Assert.Single(result.Save.FarmObjects.Objects);
            Assert.Equal(27, result.Save.FarmObjects.Objects[0].X);
            Assert.Single(result.Save.Orchard.FruitTrees);
            Assert.Equal(23, result.Save.Orchard.FruitTrees[0].X);
            Assert.Single(result.Save.Orchard.Beehives);
            Assert.True(result.Save.Animals.CoopBuilt);
            Assert.Equal(1, Assert.Single(result.Save.Animals.Chickens).PendingEggs);

            var restored = new GameSession();
            restored.Restore(result.Save);
            Assert.True(restored.Animals.CoopBuilt);
            Assert.True(restored.Storage.HasChest(new GridPosition(25, 13)));
            Assert.False(restored.Storage.HasChest(AnimalCatalog.CoopCell));
            Assert.True(restored.Orchard.HasFruitTree(new GridPosition(23, 13)));
            Assert.False(restored.Orchard.HasFruitTree(new GridPosition(30, 14)));
        }
        finally
        {
            if (File.Exists(servicePath))
            {
                File.Delete(servicePath);
            }
        }
    }

    private static GameSession SessionWithPendingEgg()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Animals = new AnimalSave
        {
            CoopBuilt = true,
            Chickens =
            [
                new StarfeatherChickenSave
                {
                    ChickenId = AnimalCatalog.FirstChickenId,
                    PendingEggs = 1,
                    MoodId = AnimalMoodIds.Happy
                }
            ]
        };
        session.Restore(save);
        return session;
    }

    private static void FillBackpackExceptEggs(Inventory inventory)
    {
        var fillerAmount = 99 * (Inventory.SlotCount - Inventory.StartingToolCount);
        Assert.True(inventory.Add(DataCatalog.DuskbellSeedId, fillerAmount));
    }
}

public sealed class TwilightEmporiumRotationTests
{
    [Fact]
    public void AccessRuleSharesHoursAndLanternrestBoundaries()
    {
        var beforeOpen = TwilightEmporiumSystem.CheckAccess(
            1,
            VillageCatalog.TwilightEmporiumOpenMinute - 1
        );
        var atOpen = TwilightEmporiumSystem.CheckAccess(
            1,
            VillageCatalog.TwilightEmporiumOpenMinute
        );
        var beforeClose = TwilightEmporiumSystem.CheckAccess(
            1,
            VillageCatalog.TwilightEmporiumCloseMinute - 1
        );
        var atClose = TwilightEmporiumSystem.CheckAccess(
            1,
            VillageCatalog.TwilightEmporiumCloseMinute
        );
        var lanternrest = TwilightEmporiumSystem.CheckAccess(
            CalendarSystem.DaysPerWeek,
            VillageCatalog.TwilightEmporiumOpenMinute
        );

        Assert.False(beforeOpen.IsOpen);
        Assert.Equal("notice.emporium_closed", beforeOpen.NoticeKey);
        Assert.True(atOpen.IsOpen);
        Assert.True(beforeClose.IsOpen);
        Assert.False(atClose.IsOpen);
        Assert.Equal("notice.emporium_closed", atClose.NoticeKey);
        Assert.False(lanternrest.IsOpen);
        Assert.Equal("notice.emporium_restday", lanternrest.NoticeKey);
        Assert.Equal(
            "target.status.emporium_restday",
            lanternrest.TargetStatusKey
        );
    }

    [Fact]
    public void StockIsDeterministicAndRotatesByWeekAndSeason()
    {
        var firstWeek = TwilightEmporiumSystem.StockForDay(1).ToArray();
        var firstWeekAgain = TwilightEmporiumSystem
            .StockForDay(1)
            .ToArray();
        var secondWeek = TwilightEmporiumSystem.StockForDay(8).ToArray();
        var nextSeason = TwilightEmporiumSystem.StockForDay(15).ToArray();

        Assert.Equal(firstWeek, firstWeekAgain);
        Assert.Equal(TwilightEmporiumSystem.StockSize, firstWeek.Length);
        Assert.Equal(DataCatalog.GleamriseSeedItemIds, firstWeek);
        Assert.Equal(firstWeek.Length, firstWeek.Distinct().Count());
        Assert.False(firstWeek.SequenceEqual(secondWeek));
        Assert.False(secondWeek.SequenceEqual(nextSeason));
        Assert.DoesNotContain(
            nextSeason,
            itemId => DataCatalog.GleamriseSeedItemIds.Contains(itemId)
        );
        Assert.All(firstWeek.Concat(secondWeek).Concat(nextSeason), itemId =>
        {
            Assert.Contains(itemId, DataCatalog.SeedItemIds);
            Assert.True(DataCatalog.Item(itemId).BuyPrice > 0);
        });
    }

    [Fact]
    public void PurchaseChecksLocationStockAndCommitsCoinsWithItem()
    {
        var session = OpenEmporiumSession(1);
        var stock = TwilightEmporiumSystem.StockForDay(1);
        var itemId = stock[0];
        var price = DataCatalog.Item(itemId).BuyPrice;

        var bought = session.BuyTwilightEmporiumItem(itemId);

        Assert.True(bought.Succeeded);
        Assert.Equal(GameSession.NewGameCoins - price, session.Coins);
        Assert.Equal(1, session.Inventory.Count(itemId));

        var unavailableItemId = DataCatalog.SeedItemIds
            .First(candidate => !stock.Contains(candidate));
        var beforeCoins = session.Coins;
        var beforeInventory = session.Inventory.Capture();
        var unavailable = session.BuyTwilightEmporiumItem(
            unavailableItemId
        );

        Assert.False(unavailable.Succeeded);
        Assert.Equal("emporium.shop.unavailable", unavailable.MessageKey);
        Assert.Equal(beforeCoins, session.Coins);
        Assert.Equal(
            beforeInventory.Select(slot => (slot.ItemId, slot.Count)),
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
        );

        session.SetPlayerLocation(
            VillageCatalog.TwilightEmporiumDoorCell.X * 16 + 8,
            (VillageCatalog.TwilightEmporiumDoorCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.False(session.BuyTwilightEmporiumItem(stock[1]).Succeeded);
    }

    [Fact]
    public void FailedPurchaseKeepsCoinsAndInventoryUnchanged()
    {
        var insufficient = OpenEmporiumSession(1);
        var itemId = TwilightEmporiumSystem.StockForDay(1)[0];
        var save = insufficient.Capture();
        save.Coins = DataCatalog.Item(itemId).BuyPrice - 1;
        insufficient.Restore(save);
        insufficient.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        var beforeCoins = insufficient.Coins;

        Assert.False(
            insufficient.BuyTwilightEmporiumItem(itemId).Succeeded
        );
        Assert.Equal(beforeCoins, insufficient.Coins);
        Assert.Equal(0, insufficient.Inventory.Count(itemId));

        var full = OpenEmporiumSession(1);
        var filler = DataCatalog.SeedItemIds
            .First(candidate => candidate != itemId);
        Assert.True(full.Inventory.Add(
            filler,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));
        var fullCoins = full.Coins;
        var fullInventory = full.Inventory.Capture();

        Assert.False(full.BuyTwilightEmporiumItem(itemId).Succeeded);
        Assert.Equal(fullCoins, full.Coins);
        Assert.Equal(
            fullInventory.Select(slot => (slot.ItemId, slot.Count)),
            full.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
        );

        var restday = OpenEmporiumSession(CalendarSystem.DaysPerWeek);
        var restdayItem = TwilightEmporiumSystem.StockForDay(
            CalendarSystem.DaysPerWeek
        )[0];
        var restdayCoins = restday.Coins;
        var restdayResult = restday.BuyTwilightEmporiumItem(restdayItem);
        Assert.False(restdayResult.Succeeded);
        Assert.Equal("notice.emporium_restday", restdayResult.MessageKey);
        Assert.Equal(restdayCoins, restday.Coins);
        Assert.Equal(0, restday.Inventory.Count(restdayItem));
    }

    [Fact]
    public void FarmStallPurchaseRemainsAvailableIndependently()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(CalendarSystem.DaysPerWeek, 3 * 60);

        var result = session.BuyItem(DataCatalog.StarbudSeedId);

        Assert.True(result.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudSeedId));
        Assert.Equal(
            GameSession.NewGameCoins -
                DataCatalog.Item(DataCatalog.StarbudSeedId).BuyPrice,
            session.Coins
        );
    }

    private static GameSession OpenEmporiumSession(int day)
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(
            day,
            VillageCatalog.TwilightEmporiumOpenMinute
        );
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        return session;
    }
}

public sealed class EconomyAndProcessorTests
{
    [Fact]
    public void BuyingAndSellingMutateCoinsAndInventoryAtomically()
    {
        var session = new GameSession();
        session.NewGame();

        var bought = session.BuyItem(DataCatalog.MoonrootSeedId);

        Assert.True(bought.Succeeded);
        Assert.Equal(GameSession.NewGameCoins - 24, session.Coins);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonrootSeedId));

        session.Inventory.Add(DataCatalog.StarbudId, 1);
        var sold = session.SellItem(DataCatalog.StarbudId);

        Assert.True(sold.Succeeded);
        Assert.Equal(GameSession.NewGameCoins - 24 + 22, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
    }

    [Fact]
    public void FailedPurchaseDoesNotSpendCoinsOrAddItems()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));
        var beforeCoins = session.Coins;

        var result = session.BuyItem(DataCatalog.MoonrootSeedId);

        Assert.False(result.Succeeded);
        Assert.Equal(beforeCoins, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonrootSeedId));
    }

    [Fact]
    public void ProcessorConsumesTwoCropsAndFinishesAfterOneNight()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 2));

        var started = session.StartProcessing(DataCatalog.StarbudPreserveRecipeId);

        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.False(session.Processor.IsReady);
        Assert.False(session.StartProcessing(DataCatalog.MoonrootTonicRecipeId).Succeeded);

        session.EndDay();

        Assert.True(session.Processor.IsReady);
        var collected = session.CollectProcessedItem();
        Assert.True(collected.Succeeded);
        Assert.True(session.Processor.IsIdle);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudPreserveId));
    }

    [Fact]
    public void ProcessorConsumesRegularQualityBeforeHigherQualityCrops()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudLuminousId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudStarlightId, 1));

        var started = session.StartProcessing(
            DataCatalog.StarbudPreserveRecipeId
        );

        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(
            0,
            session.Inventory.Count(DataCatalog.StarbudLuminousId)
        );
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarbudStarlightId)
        );
    }

    [Fact]
    public void ProcessorMachinesAdvanceTheirOwnRecipesIndependently()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.MoonrootId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.CloudleafId, 3));

        Assert.True(session.StartProcessing(
            ProcessorCatalog.MoonwellInfuserId,
            DataCatalog.MoonrootTonicRecipeId
        ).Succeeded);
        Assert.True(session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);
        Assert.True(session.StartProcessing(
            ProcessorCatalog.StarweaveDryingLoomId,
            DataCatalog.CloudleafTeaRecipeId
        ).Succeeded);

        session.EndDay();

        Assert.True(session.Processor.Machine(
            ProcessorCatalog.MoonwellInfuserId
        ).IsReady);
        Assert.True(session.Processor.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).IsReady);
        var dryingLoom = session.Processor.Machine(
            ProcessorCatalog.StarweaveDryingLoomId
        );
        Assert.False(dryingLoom.IsReady);
        Assert.Equal(1, dryingLoom.RemainingNights);

        session.EndDay();

        Assert.Equal(3, session.Processor.ReadyCount);
    }

    [Fact]
    public void CollectAllProcessorsIsAtomicAndNotifiesInventoryOnce()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.MoonrootId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.CloudleafId, 3));
        Assert.True(session.StartProcessing(
            ProcessorCatalog.MoonwellInfuserId,
            DataCatalog.MoonrootTonicRecipeId
        ).Succeeded);
        Assert.True(session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);
        Assert.True(session.StartProcessing(
            ProcessorCatalog.StarweaveDryingLoomId,
            DataCatalog.CloudleafTeaRecipeId
        ).Succeeded);
        session.EndDay();
        session.EndDay();
        Assert.True(session.Inventory.Add(
            DataCatalog.DuskbellSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount - 1)
        ));
        var inventoryChanges = 0;
        var processorChanges = 0;
        session.Inventory.Changed += () => inventoryChanges++;
        session.Processor.Changed += () => processorChanges++;

        var failed = session.CollectAllProcessedItems();

        Assert.False(failed.Succeeded);
        Assert.Equal(0, inventoryChanges);
        Assert.Equal(0, processorChanges);
        Assert.Equal(3, session.Processor.ReadyCount);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonrootTonicId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudPreserveId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CloudleafTeaId));

        Assert.True(session.Inventory.Remove(
            DataCatalog.DuskbellSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount - 1)
        ));
        inventoryChanges = 0;
        processorChanges = 0;
        var collected = session.CollectAllProcessedItems();

        Assert.True(collected.Succeeded);
        Assert.Equal(1, inventoryChanges);
        Assert.Equal(1, processorChanges);
        Assert.Equal(0, session.Processor.ReadyCount);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonrootTonicId));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudPreserveId));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.CloudleafTeaId));
    }

    [Fact]
    public void ProcessorPreviewMatchesToolMaterialBusyReadyAndCapacityRules()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Select(1);
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewProcessorMachine(
                ProcessorCatalog.PrismPreserveVatId
            ).State
        );

        session.Inventory.Select(0);
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewProcessorMachine(
                ProcessorCatalog.PrismPreserveVatId
            ).State
        );
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 2));
        Assert.True(session.PreviewProcessorMachine(
            ProcessorCatalog.PrismPreserveVatId
        ).IsAvailable);
        Assert.True(session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);
        Assert.Equal(
            "processor.busy",
            session.PreviewProcessorMachine(
                ProcessorCatalog.PrismPreserveVatId
            ).LabelKey
        );

        session.EndDay();
        Assert.Equal(
            "target.action.open_processor_ready",
            session.PreviewProcessorMachine(
                ProcessorCatalog.PrismPreserveVatId
            ).LabelKey
        );
        Assert.True(session.Inventory.Add(
            DataCatalog.DuskbellSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));
        Assert.Equal(
            "notice.inventory_full",
            session.PreviewProcessorMachine(
                ProcessorCatalog.PrismPreserveVatId
            ).LabelKey
        );
        var failedCollect = session.CollectProcessedItem(
            ProcessorCatalog.PrismPreserveVatId
        );
        Assert.False(failedCollect.Succeeded);
        Assert.True(session.Processor.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).IsReady);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudPreserveId));
    }

    [Fact]
    public void LegacyProcessorMigratesOnlyWhenNoModernMachineEntryExists()
    {
        var processor = new ProcessorSystem();
        processor.Reset();
        processor.Restore(new ProcessorSave
        {
            RecipeId = DataCatalog.StarbudPreserveRecipeId,
            RemainingNights = 99
        });

        Assert.Equal(
            DataCatalog.StarbudPreserveRecipeId,
            processor.MainMachine.ActiveRecipeId
        );
        Assert.Equal(1, processor.MainMachine.RemainingNights);
        Assert.True(processor.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).IsIdle);

        processor.Restore(new ProcessorSave
        {
            RecipeId = DataCatalog.MoonrootTonicRecipeId,
            RemainingNights = 1,
            Machines =
            [
                new ProcessorMachineSave
                {
                    MachineId = ProcessorCatalog.PrismPreserveVatId,
                    RecipeId = DataCatalog.StarbudPreserveRecipeId,
                    RemainingNights = 0
                },
                new ProcessorMachineSave
                {
                    MachineId = "unknown_machine",
                    RecipeId = DataCatalog.MoonrootTonicRecipeId,
                    RemainingNights = 1
                }
            ]
        });

        Assert.True(processor.MainMachine.IsIdle);
        Assert.True(processor.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).IsReady);

        processor.Restore(new ProcessorSave
        {
            RecipeId = DataCatalog.MoonrootTonicRecipeId,
            RemainingNights = 1,
            Machines =
            [
                new ProcessorMachineSave
                {
                    MachineId = "unknown_machine",
                    RecipeId = DataCatalog.StarbudPreserveRecipeId,
                    RemainingNights = 0
                }
            ]
        });

        Assert.Equal(
            DataCatalog.MoonrootTonicRecipeId,
            processor.MainMachine.ActiveRecipeId
        );
        Assert.Equal(
            1,
            processor.Machines.Values.Count(machine => !machine.IsIdle)
        );
    }

    [Fact]
    public void QualityProduceKeepsItsOwnShippingAndSaleValue()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudStarlightId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudLuminousId, 1));
        var beforeCoins = session.Coins;

        Assert.True(session.SellItem(
            DataCatalog.StarbudStarlightId
        ).Succeeded);
        Assert.True(session.QueueForShipping(
            DataCatalog.StarbudLuminousId
        ).Succeeded);
        var settlement = session.EndDay();

        Assert.Equal(
            beforeCoins +
            DataCatalog.Item(DataCatalog.StarbudStarlightId).SellPrice +
            DataCatalog.Item(DataCatalog.StarbudLuminousId).SellPrice,
            session.Coins
        );
        Assert.Single(settlement.Lines);
        Assert.Equal(
            DataCatalog.StarbudLuminousId,
            settlement.Lines[0].ItemId
        );
    }

    [Fact]
    public void ArtisanGoodsAreWorthMoreThanTheirRawIngredients()
    {
        var preserve = DataCatalog.Item(DataCatalog.StarbudPreserveId);
        var starbud = DataCatalog.Item(DataCatalog.StarbudId);
        var tonic = DataCatalog.Item(DataCatalog.MoonrootTonicId);
        var moonroot = DataCatalog.Item(DataCatalog.MoonrootId);
        var tea = DataCatalog.Item(DataCatalog.CloudleafTeaId);
        var cloudleaf = DataCatalog.Item(DataCatalog.CloudleafId);

        Assert.True(preserve.SellPrice > starbud.SellPrice * 2);
        Assert.True(tonic.SellPrice > moonroot.SellPrice * 2);
        Assert.True(tea.SellPrice > cloudleaf.SellPrice * 3);
    }

    [Fact]
    public void ShippingChestQueuesReclaimsAndSettlesAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarbudId, 2);
        var beforeCoins = session.Coins;

        Assert.False(session.QueueForShipping(DataCatalog.HandId).Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarbudId));

        Assert.True(session.QueueForShipping(DataCatalog.StarbudId).Succeeded);
        Assert.True(session.QueueForShipping(DataCatalog.StarbudId).Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(2, session.Shipping.PendingCount(DataCatalog.StarbudId));
        Assert.True(session.ReclaimFromShipping(DataCatalog.StarbudId).Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(1, session.Shipping.PendingCount(DataCatalog.StarbudId));

        var settlement = session.EndDay();

        Assert.Equal(1, settlement.TotalItems);
        Assert.Equal(DataCatalog.Item(DataCatalog.StarbudId).SellPrice, settlement.TotalCoins);
        Assert.Equal(beforeCoins + settlement.TotalCoins, session.Coins);
        Assert.Equal(0, session.Shipping.PendingItemCount);
        Assert.Equal(settlement, session.Shipping.LastSettlement);
    }

    [Fact]
    public void FullBackpackCannotReclaimOrLoseAQueuedItem()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarbudId, 1);
        Assert.True(session.QueueForShipping(DataCatalog.StarbudId).Succeeded);
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));

        var result = session.ReclaimFromShipping(DataCatalog.StarbudId);

        Assert.False(result.Succeeded);
        Assert.Equal("notice.inventory_full", result.MessageKey);
        Assert.Equal(1, session.Shipping.PendingCount(DataCatalog.StarbudId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
    }
}

public sealed class QuestAndSessionTests
{
    [Fact]
    public void TutorialLoopRunsFromSeedGiftToCompletion()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.Equal(GameSession.NewGamePlayerX, session.PlayerX);
        Assert.Equal(GameSession.NewGamePlayerY, session.PlayerY);
        Assert.True(session.InteractWithMira());
        Assert.Equal(5, session.Inventory.Count(DataCatalog.StarbudSeedId));

        var positions = new[]
        {
            new GridPosition(12, 16),
            new GridPosition(13, 16),
            new GridPosition(14, 16)
        };

        session.Inventory.Select(1);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(QuestStage.Plant, session.Quest.Stage);

        session.Inventory.Select(6);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(QuestStage.Water, session.Quest.Stage);

        session.Inventory.Select(3);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(QuestStage.Grow, session.Quest.Stage);

        session.EndDay();
        Assert.Equal(DataCatalog.RainWeatherId, session.Weather.CurrentId);
        Assert.All(positions, position =>
            Assert.True(session.Farm.Tiles[position].Watered)
        );
        session.EndDay();
        Assert.Equal(QuestStage.Harvest, session.Quest.Stage);

        session.Inventory.Select(0);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(3, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(QuestStage.ReturnToMira, session.Quest.Stage);

        Assert.False(session.InteractWithMira());
        Assert.Equal(QuestStage.Complete, session.Quest.Stage);
    }

    [Fact]
    public void MatureCropsRequireTheHandInsteadOfAnySelectedTool()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        session.Farm.Restore(
        [
            new FarmTileState
            {
                X = position.X,
                Y = position.Y,
                Tilled = true,
                CropId = DataCatalog.StarbudId,
                WateredNights = 2
            }
        ]);

        session.Inventory.Select(1);
        var wrongToolPreview = session.PreviewSelectedTarget(position);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal(TargetPreviewKind.Crop, wrongToolPreview.Kind);
        Assert.Equal("target.need.hand", wrongToolPreview.LabelKey);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.NotNull(session.Farm.Tiles[position].CropId);

        session.Inventory.Select(0);
        var harvestPreview = session.PreviewSelectedTarget(position);
        Assert.True(harvestPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Crop, harvestPreview.Kind);
        Assert.Equal("target.action.harvest", harvestPreview.LabelKey);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudId));
    }

    [Fact]
    public void TargetPreviewExplainsTillAndGatherToolRequirements()
    {
        var session = new GameSession();
        session.NewGame();
        var soil = new GridPosition(12, 16);
        var tree = FindWorldResource(WorldResourceKind.Tree);

        var needsShovel = session.PreviewSelectedTarget(soil);
        Assert.Equal(TargetPreviewState.NeedsTool, needsShovel.State);
        Assert.Equal(TargetPreviewKind.Ground, needsShovel.Kind);
        Assert.Equal("target.need.shovel_till", needsShovel.LabelKey);

        session.Inventory.Select(1);
        var canTill = session.PreviewSelectedTarget(soil);
        Assert.True(canTill.IsAvailable);
        Assert.Equal("target.action.till", canTill.LabelKey);

        var needsMachete = session.PreviewSelectedTarget(tree);
        Assert.Equal(TargetPreviewState.NeedsTool, needsMachete.State);
        Assert.Equal(TargetPreviewKind.Tree, needsMachete.Kind);
        Assert.Equal("target.need.machete", needsMachete.LabelKey);

        session.Inventory.Select(2);
        var canChop = session.PreviewSelectedTarget(tree);
        Assert.True(canChop.IsAvailable);
        Assert.Equal(TargetPreviewKind.Tree, canChop.Kind);
        Assert.Equal("target.action.chop", canChop.LabelKey);

        Assert.True(session.UseSelected(tree).Succeeded);
        Assert.Equal(
            TargetPreviewState.Neutral,
            session.PreviewSelectedTarget(tree).State
        );
    }

    [Fact]
    public void MacheteAndShovelGatherPersistentWorldResources()
    {
        var session = new GameSession();
        session.NewGame();
        var tree = FindWorldResource(WorldResourceKind.Tree);
        var crystal = FindWorldResource(WorldResourceKind.Crystal);

        session.Inventory.Select(1);
        Assert.False(session.UseSelected(tree).Succeeded);
        Assert.Equal(GameSession.MaxEnergy, session.Energy);
        Assert.False(session.Resources.IsRemoved(tree));

        session.Inventory.Select(2);
        var wood = session.UseSelected(tree);
        Assert.True(wood.Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(GameSession.MaxEnergy - 4, session.Energy);
        Assert.True(session.Resources.IsRemoved(tree));
        Assert.False(session.UseSelected(tree).Succeeded);

        Assert.False(session.UseSelected(crystal).Succeeded);
        Assert.Equal(GameSession.MaxEnergy - 4, session.Energy);

        session.Inventory.Select(1);
        var shard = session.UseSelected(crystal);
        Assert.True(shard.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(GameSession.MaxEnergy - 8, session.Energy);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Resources.IsRemoved(tree));
        Assert.True(restored.Resources.IsRemoved(crystal));
    }

    [Fact]
    public void CrystalsRegrowAfterTwoDaysAndTreesAfterOneWeek()
    {
        var session = new GameSession();
        session.NewGame();
        var tree = FindWorldResource(WorldResourceKind.Tree);
        var crystal = FindWorldResource(WorldResourceKind.Crystal);

        session.Inventory.Select(2);
        Assert.True(session.UseSelected(tree).Succeeded);
        session.Inventory.Select(1);
        Assert.True(session.UseSelected(crystal).Succeeded);

        session.EndDay();
        Assert.True(session.Resources.IsRemoved(tree));
        Assert.True(session.Resources.IsRemoved(crystal));
        Assert.Equal(0, session.LastRespawnedResources);

        session.EndDay();
        Assert.True(session.Resources.IsRemoved(tree));
        Assert.False(session.Resources.IsRemoved(crystal));
        Assert.Equal(1, session.LastRespawnedResources);

        while (session.Clock.Day < 8)
        {
            session.EndDay();
        }

        Assert.False(session.Resources.IsRemoved(tree));
        Assert.Equal(1, session.LastRespawnedResources);
    }

    [Fact]
    public void BucketRefillsTheFiniteWateringCanOnlyAtWater()
    {
        var session = new GameSession();
        session.NewGame();
        var soil = new GridPosition(12, 16);
        Assert.True(session.Farm.TryTill(soil, session.Energy).Succeeded);

        session.Inventory.Select(3);
        Assert.True(session.UseSelected(soil).Succeeded);
        Assert.Equal(
            GameSession.MaxWateringCanWater - 1,
            session.WateringCanWater
        );

        session.Inventory.Select(4);
        Assert.False(session.UseSelected(new GridPosition(20, 20)).Succeeded);
        Assert.True(session.UseSelected(new GridPosition(38, 21)).Succeeded);
        Assert.Equal(GameSession.MaxWateringCanWater, session.WateringCanWater);
    }

    [Fact]
    public void WaterTargetPreviewDistinguishesToolAndCapacityStates()
    {
        var session = new GameSession();
        session.NewGame();
        var water = new GridPosition(38, 21);

        var needsBucket = session.PreviewSelectedTarget(water);
        Assert.Equal(TargetPreviewState.NeedsTool, needsBucket.State);
        Assert.Equal(TargetPreviewKind.Water, needsBucket.Kind);
        Assert.Equal("target.need.bucket_or_rod", needsBucket.LabelKey);

        session.Inventory.Select(4);
        var alreadyFull = session.PreviewSelectedTarget(water);
        Assert.Equal(TargetPreviewState.Blocked, alreadyFull.State);
        Assert.Equal("target.status.water_full", alreadyFull.LabelKey);

        var save = session.Capture();
        save.Player.WateringCanWater = 3;
        session.Restore(save);
        session.Inventory.Select(4);
        var canDrawWater = session.PreviewSelectedTarget(water);
        Assert.True(canDrawWater.IsAvailable);
        Assert.Equal("target.action.draw_water", canDrawWater.LabelKey);
    }

    [Fact]
    public void FishingRodCatchesStarterFishAtThreeWaterKinds()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Select(5);

        AssertStarterFish(
            session,
            new GridPosition(38, 21),
            DataCatalog.PondglowMinnowId
        );
        AssertStarterFish(
            session,
            FindWaterSource(WorldBiome.CrystalVale),
            DataCatalog.CrystalfinDaceId
        );
        AssertStarterFish(
            session,
            FindWaterSource(WorldBiome.MoonwaterWetlands),
            DataCatalog.MoonwaterMinnowId
        );
    }

    [Fact]
    public void FishingPreviewActionAndSaveUseTheSameAtomicRules()
    {
        var session = new GameSession();
        session.NewGame();
        var water = new GridPosition(38, 21);
        session.Inventory.Select(5);

        var preview = session.PreviewSelectedTarget(water);
        Assert.True(preview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Water, preview.Kind);
        Assert.Equal("target.action.fish", preview.LabelKey);

        var result = session.UseSelected(water);
        Assert.True(result.Succeeded);
        Assert.Equal(FishingSystem.CastEnergyCost, result.EnergyCost);
        Assert.Equal(DataCatalog.PondglowMinnowId, result.GrantedItemId);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.PondglowMinnowId));
        Assert.Contains(DataCatalog.PondglowMinnowId, session.Fishing.CaughtFishIds);
        Assert.Equal(
            GameSession.MaxEnergy - FishingSystem.CastEnergyCost,
            session.Energy
        );

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Contains(
            DataCatalog.PondglowMinnowId,
            restored.Fishing.CaughtFishIds
        );

        var energyBlockedSave = session.Capture();
        energyBlockedSave.Player.Energy = FishingSystem.CastEnergyCost - 1;
        var energyBlocked = new GameSession();
        energyBlocked.Restore(energyBlockedSave);
        energyBlocked.Inventory.Select(5);

        Assert.Equal(
            TargetPreviewState.Blocked,
            energyBlocked.PreviewSelectedTarget(water).State
        );
        var tired = energyBlocked.UseSelected(water);
        Assert.False(tired.Succeeded);
        Assert.Equal("notice.no_energy", tired.MessageKey);
        Assert.Equal(
            FishingSystem.CastEnergyCost - 1,
            energyBlocked.Energy
        );

        var full = new GameSession();
        full.NewGame();
        Assert.True(full.Inventory.Add(
            DataCatalog.StarbudSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));
        full.Inventory.Select(5);

        var fullPreview = full.PreviewSelectedTarget(water);
        Assert.Equal(TargetPreviewState.Blocked, fullPreview.State);
        Assert.Equal("target.blocked.backpack_full", fullPreview.LabelKey);
        var fullResult = full.UseSelected(water);
        Assert.False(fullResult.Succeeded);
        Assert.Equal("notice.inventory_full", fullResult.MessageKey);
        Assert.Equal(GameSession.MaxEnergy, full.Energy);
        Assert.Empty(full.Fishing.CaughtFishIds);

        var notWater = session.UseSelected(new GridPosition(12, 16));
        Assert.False(notWater.Succeeded);
        Assert.Equal("notice.not_fishing_water", notWater.MessageKey);
    }

    [Fact]
    public void FishingCollectionEntriesTrackCaughtProgress()
    {
        var session = new GameSession();
        session.NewGame();

        var initialEntries = session.Fishing.CollectionEntries();
        Assert.Equal(DataCatalog.Fishes.Count, session.Fishing.TotalFishCount);
        Assert.Equal(0, session.Fishing.CaughtCount);
        Assert.All(initialEntries, entry => Assert.False(entry.Caught));

        session.Inventory.Select(5);
        var result = session.UseSelected(new GridPosition(38, 21));

        Assert.True(result.Succeeded);
        Assert.Equal(1, session.Fishing.CaughtCount);
        var caughtEntries = session.Fishing.CollectionEntries();
        var pondEntry = Assert.Single(caughtEntries, entry =>
            entry.Fish.Id == DataCatalog.PondglowMinnowId
        );
        Assert.True(pondEntry.Caught);
        Assert.Equal(
            DataCatalog.Fishes.Count - 1,
            caughtEntries.Count(entry => !entry.Caught)
        );

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Fishing.IsCaught(DataCatalog.PondglowMinnowId));
        Assert.False(restored.Fishing.IsCaught(DataCatalog.CrystalfinDaceId));
    }

    [Fact]
    public void FishingCollectionRewardsClaimMilestonesAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        var firstReward = FishingSystem.CollectionRewardDefinitions.First();

        var locked = Assert.Single(
            session.FishingCollectionRewards(),
            reward => reward.Definition.Id == firstReward.Id
        );
        Assert.Equal(FishingCollectionRewardStatus.Locked, locked.Status);
        var notReady = session.ClaimFishingCollectionReward(firstReward.Id);
        Assert.False(notReady.Succeeded);
        Assert.Equal("fishing.reward.not_ready", notReady.MessageKey);
        Assert.Equal(GameSession.NewGameCoins, session.Coins);

        var save = session.Capture();
        save.Fishing.CaughtFishIds = DataCatalog.FishItemIds
            .Take(firstReward.RequiredCaughtCount)
            .ToList();
        var ready = new GameSession();
        ready.Restore(save);
        var readySnapshot = Assert.Single(
            ready.FishingCollectionRewards(),
            reward => reward.Definition.Id == firstReward.Id
        );
        Assert.Equal(FishingCollectionRewardStatus.Ready, readySnapshot.Status);
        Assert.True(ready.Inventory.Add(
            DataCatalog.StarbudSeedId,
            99 * (Inventory.SlotCount - Inventory.StartingToolCount)
        ));

        var full = ready.ClaimFishingCollectionReward(firstReward.Id);

        Assert.False(full.Succeeded);
        Assert.Equal("notice.inventory_full", full.MessageKey);
        Assert.DoesNotContain(
            firstReward.Id,
            ready.Fishing.ClaimedRewardIds
        );
        Assert.Equal(GameSession.NewGameCoins, ready.Coins);
        Assert.Equal(0, ready.Inventory.Count(firstReward.RewardItemId));

        Assert.True(ready.Inventory.Remove(DataCatalog.StarbudSeedId, 99));
        var claimed = ready.ClaimFishingCollectionReward(firstReward.Id);

        Assert.True(claimed.Succeeded);
        Assert.Equal("fishing.reward.claimed", claimed.MessageKey);
        Assert.Equal(
            firstReward.RewardCoins,
            claimed.RewardCoins
        );
        Assert.Equal(
            GameSession.NewGameCoins + firstReward.RewardCoins,
            ready.Coins
        );
        Assert.Equal(
            firstReward.RewardItemCount,
            ready.Inventory.Count(firstReward.RewardItemId)
        );
        Assert.Contains(firstReward.Id, ready.Fishing.ClaimedRewardIds);

        var duplicate = ready.ClaimFishingCollectionReward(firstReward.Id);
        Assert.False(duplicate.Succeeded);
        Assert.Equal("fishing.reward.already_claimed", duplicate.MessageKey);
        Assert.Equal(
            GameSession.NewGameCoins + firstReward.RewardCoins,
            ready.Coins
        );

        var restored = new GameSession();
        restored.Restore(ready.Capture());
        Assert.Contains(firstReward.Id, restored.Fishing.ClaimedRewardIds);
        var restoredSnapshot = Assert.Single(
            restored.FishingCollectionRewards(),
            reward => reward.Definition.Id == firstReward.Id
        );
        Assert.Equal(
            FishingCollectionRewardStatus.Claimed,
            restoredSnapshot.Status
        );
    }

    [Fact]
    public void FishingCatalogDefinesTwentyFourConditionedFish()
    {
        Assert.Equal(24, DataCatalog.Fishes.Count);
        Assert.Equal(
            DataCatalog.Fishes.Count,
            DataCatalog.FishItemIds.Distinct(StringComparer.Ordinal).Count()
        );

        foreach (var waterKind in Enum.GetValues<FishingWaterKind>())
        {
            Assert.Equal(
                8,
                DataCatalog.Fishes.Values.Count(fish =>
                    fish.WaterKind == waterKind
                )
            );
        }

        foreach (var fish in DataCatalog.Fishes.Values)
        {
            Assert.Equal(fish.Id, fish.ItemId);
            Assert.Contains(fish.ItemId, DataCatalog.FishItemIds);
            Assert.Contains(fish.ItemId, DataCatalog.SellableItemIds);
            Assert.Contains(fish.ItemId, DataCatalog.StorableItemIds);
            Assert.Equal(ItemKind.Fish, DataCatalog.Item(fish.ItemId).Kind);
        }

        Assert.Contains(DataCatalog.Fishes.Values, fish =>
            fish.WeatherId == DataCatalog.RainWeatherId
        );
        Assert.Contains(DataCatalog.Fishes.Values, fish =>
            fish.WeatherId == DataCatalog.StardustWindWeatherId
        );
        Assert.Contains(DataCatalog.Fishes.Values, fish =>
            fish.SeasonIds is { Count: > 0 }
        );
        Assert.Contains(DataCatalog.Fishes.Values, fish =>
            fish.StartMinute != GameClock.StartMinute ||
            fish.EndMinute != GameClock.EndMinute
        );
    }

    [Fact]
    public void FishingSpecificConditionsOverrideCommonWaterFish()
    {
        var fishing = new FishingSystem();
        var home = new GridPosition(38, 21);
        var crystal = FindWaterSource(WorldBiome.CrystalVale);
        var wetlands = FindWaterSource(WorldBiome.MoonwaterWetlands);

        Assert.Equal(
            DataCatalog.PondglowMinnowId,
            fishing.PreviewCatch(
                home,
                1,
                GameClock.StartMinute,
                DataCatalog.ClearWeatherId
            )?.Id
        );
        Assert.Equal(
            DataCatalog.RainpetalLoachId,
            fishing.PreviewCatch(
                home,
                2,
                8 * 60,
                DataCatalog.RainWeatherId
            )?.Id
        );
        Assert.Equal(
            DataCatalog.StardustPikeId,
            fishing.PreviewCatch(
                crystal,
                4,
                13 * 60,
                DataCatalog.StardustWindWeatherId
            )?.Id
        );
        Assert.Equal(
            DataCatalog.RainveilLampreyId,
            fishing.PreviewCatch(
                wetlands,
                15,
                8 * 60,
                DataCatalog.RainWeatherId
            )?.Id
        );
    }

    private static GridPosition FindWorldResource(WorldResourceKind kind)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.ResourceAt(cell) == kind)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException($"No world resource found for {kind}.");
    }

    private static GridPosition FindWaterSource(WorldBiome biome)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.GetBiome(cell) == biome &&
                    WorldDefinition.IsWaterSource(cell))
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException($"No water source found for {biome}.");
    }

    private static void AssertStarterFish(
        GameSession session,
        GridPosition water,
        string expectedFishId
    )
    {
        var beforeEnergy = session.Energy;
        var preview = session.PreviewSelectedTarget(water);
        Assert.True(preview.IsAvailable);

        var result = session.UseSelected(water);
        Assert.True(result.Succeeded);
        Assert.Equal(expectedFishId, result.GrantedItemId);
        Assert.Equal(1, result.GrantedItemCount);
        Assert.Contains(expectedFishId, session.Fishing.CaughtFishIds);
        Assert.Equal(1, session.Inventory.Count(expectedFishId));
        Assert.Equal(beforeEnergy - FishingSystem.CastEnergyCost, session.Energy);
    }
}

public sealed class DailyCommissionTests
{
    [Fact]
    public void BoardPreviewAndActionShareTheHandRuleWithoutMutatingOnFailure()
    {
        var session = new GameSession();
        session.NewGame();
        var board = FarmLayout.CommissionBoardCell;

        var handPreview = session.PreviewSelectedTarget(board);

        Assert.True(handPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.CommissionBoard, handPreview.Kind);
        Assert.Equal("target.action.open_commission", handPreview.LabelKey);
        Assert.True(session.UseSelected(board).Succeeded);

        session.Inventory.Select(1);
        var energy = session.Energy;
        var wrongToolPreview = session.PreviewSelectedTarget(board);
        var wrongToolAction = session.UseSelected(board);

        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal("target.need.hand", wrongToolPreview.LabelKey);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);
        Assert.Equal(energy, session.Energy);
        Assert.Empty(session.Farm.Tiles);
        Assert.True(FarmLayout.IsStaticBlocked(board));
    }

    [Fact]
    public void CommissionRotationRefreshesAfterSleepAndExpiresOldProgress()
    {
        var session = new GameSession();
        session.NewGame();

        Assert.Equal(
            DataCatalog.PlantStarbudCommissionId,
            session.Commission.Current.Id
        );
        Assert.True(session.AcceptDailyCommission().Succeeded);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        Assert.Equal(1, session.Commission.Progress);

        session.EndDay();

        Assert.Equal(
            DataCatalog.GatherLumenwoodCommissionId,
            session.Commission.Current.Id
        );
        Assert.False(session.Commission.Accepted);
        Assert.Equal(0, session.Commission.Progress);

        session.EndDay();
        Assert.Equal(
            DataCatalog.DeliverStarbudCommissionId,
            session.Commission.Current.Id
        );

        session.EndDay();
        Assert.Equal(
            DataCatalog.PlantStarbudCommissionId,
            session.Commission.Current.Id
        );
    }

    [Fact]
    public void PlantingProgressOnlyCountsSuccessfulMatchingActions()
    {
        var session = new GameSession();
        session.NewGame();
        session.AcceptDailyCommission();
        session.Inventory.Add(DataCatalog.StarbudSeedId, 2);
        session.Inventory.Select(6);
        var soil = new GridPosition(12, 16);

        Assert.False(session.UseSelected(soil).Succeeded);
        Assert.Equal(0, session.Commission.Progress);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarbudSeedId));

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(soil).Succeeded);
        session.Inventory.Select(6);
        Assert.True(session.UseSelected(soil).Succeeded);
        Assert.Equal(1, session.Commission.Progress);

        Assert.False(session.UseSelected(soil).Succeeded);
        Assert.Equal(1, session.Commission.Progress);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudSeedId));
    }

    [Fact]
    public void GatheringProgressCountsGrantedItemsAndIgnoresDepletedNodes()
    {
        var session = new GameSession();
        session.NewGame();
        session.EndDay();
        session.AcceptDailyCommission();
        var trees = FindWorldResources(WorldResourceKind.Tree, 2);
        session.Inventory.Select(2);

        var first = session.UseSelected(trees[0]);
        Assert.True(first.Succeeded);
        Assert.Equal(2, first.GrantedItemCount);
        Assert.Equal(2, session.Commission.Progress);

        Assert.False(session.UseSelected(trees[0]).Succeeded);
        Assert.Equal(2, session.Commission.Progress);

        Assert.True(session.UseSelected(trees[1]).Succeeded);
        Assert.Equal(3, session.Commission.Progress);
        Assert.True(session.Commission.IsReady(session.Inventory));
    }

    [Fact]
    public void DeliveryClaimIsAtomicAndCannotRewardTwice()
    {
        var session = new GameSession();
        session.NewGame();
        session.EndDay();
        session.EndDay();
        session.AcceptDailyCommission();
        session.Inventory.Add(DataCatalog.StarbudId, 1);
        var startingCoins = session.Coins;

        var missing = session.ClaimDailyCommission();

        Assert.False(missing.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(startingCoins, session.Coins);
        Assert.False(session.Commission.Claimed);

        session.Inventory.Add(DataCatalog.StarbudId, 1);
        var claimed = session.ClaimDailyCommission();

        Assert.True(claimed.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(startingCoins + 70, session.Coins);
        Assert.True(session.Commission.Claimed);

        Assert.False(session.ClaimDailyCommission().Succeeded);
        Assert.Equal(startingCoins + 70, session.Coins);
    }

    [Fact]
    public void DeliveryCommissionAcceptsMixedCropQualitiesAsOneFamily()
    {
        var session = new GameSession();
        session.NewGame();
        session.EndDay();
        session.EndDay();
        Assert.Equal(
            DataCatalog.DeliverStarbudCommissionId,
            session.Commission.Current.Id
        );
        Assert.True(session.AcceptDailyCommission().Succeeded);
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudLuminousId,
            1
        ));
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudStarlightId,
            1
        ));

        Assert.Equal(
            2,
            session.Commission.DisplayProgress(session.Inventory)
        );
        Assert.True(session.Commission.IsReady(session.Inventory));
        Assert.True(session.ClaimDailyCommission().Succeeded);
        Assert.Equal(0, session.Inventory.CountFamily(DataCatalog.StarbudId));
    }

    private static IReadOnlyList<GridPosition> FindWorldResources(
        WorldResourceKind kind,
        int count
    )
    {
        var cells = new List<GridPosition>();
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.ResourceAt(cell) != kind)
                {
                    continue;
                }

                cells.Add(cell);
                if (cells.Count == count)
                {
                    return cells;
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not find {count} world resources for {kind}."
        );
    }
}

public sealed class WeeklyCommissionTests
{
    [Fact]
    public void SameWeekProgressSurvivesSleepAndSaveRestore()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);

        session.EndDay();

        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(1, session.WeeklyCommission.Week);
        Assert.Equal(2, session.WeeklyCommission.Progress);
        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Equal(1, restored.WeeklyCommission.Week);
        Assert.True(restored.WeeklyCommission.Accepted);
        Assert.Equal(
            DataCatalog.StarlitRoutePlantStageId,
            restored.WeeklyCommission.CurrentStage.Id
        );
        Assert.Equal(2, restored.WeeklyCommission.Progress);
    }

    [Fact]
    public void DaySevenToEightRefreshesWeeklyStateWithoutChangingDailyRules()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);

        while (session.Clock.Day < 7)
        {
            session.EndDay();
        }

        Assert.Equal(1, session.WeeklyCommission.Week);
        Assert.True(session.WeeklyCommission.Accepted);
        Assert.Equal(1, session.WeeklyCommission.Progress);

        session.EndDay();

        Assert.Equal(8, session.Clock.Day);
        Assert.Equal(2, session.WeeklyCommission.Week);
        Assert.False(session.WeeklyCommission.Accepted);
        Assert.Equal(0, session.WeeklyCommission.Progress);
        Assert.Equal(
            DataCatalog.GatherLumenwoodCommissionId,
            session.Commission.Current.Id
        );
    }

    [Fact]
    public void OnlyTheActiveStageCountsAndConfirmationIsStrictlyOrdered()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.AcceptWeeklyCommission().Succeeded);

        session.WeeklyCommission.RecordGather(DataCatalog.LumenwoodId, 4);
        session.WeeklyCommission.RecordPlant(DataCatalog.MoonrootId);
        Assert.Equal(0, session.WeeklyCommission.Progress);
        Assert.False(session.AdvanceWeeklyCommissionStage().Succeeded);

        RecordStarbudPlanting(session, 3);
        session.WeeklyCommission.RecordGather(DataCatalog.LumenwoodId, 4);
        Assert.True(session.WeeklyCommission.IsReady(session.Inventory));
        Assert.True(session.AdvanceWeeklyCommissionStage().Succeeded);
        Assert.Equal(
            DataCatalog.StarlitRouteGatherStageId,
            session.WeeklyCommission.CurrentStage.Id
        );
        Assert.Equal(0, session.WeeklyCommission.Progress);

        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        Assert.Equal(0, session.WeeklyCommission.Progress);
        session.WeeklyCommission.RecordGather(DataCatalog.CrystalShardId, 4);
        Assert.Equal(0, session.WeeklyCommission.Progress);
        session.WeeklyCommission.RecordGather(DataCatalog.LumenwoodId, 4);
        Assert.True(session.AdvanceWeeklyCommissionStage().Succeeded);
        Assert.Equal(
            DataCatalog.StarlitRouteDeliverStageId,
            session.WeeklyCommission.CurrentStage.Id
        );
        Assert.True(session.WeeklyCommission.IsFinalStage);
    }

    [Fact]
    public void FailedPlantingAndWrongToolGatheringDoNotCount()
    {
        var session = new GameSession();
        session.NewGame();
        session.AcceptWeeklyCommission();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudSeedId, 1));
        session.Inventory.Select(6);
        var soil = new GridPosition(12, 16);

        Assert.False(session.UseSelected(soil).Succeeded);
        Assert.Equal(0, session.WeeklyCommission.Progress);

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(soil).Succeeded);
        session.Inventory.Select(6);
        Assert.True(session.UseSelected(soil).Succeeded);
        Assert.Equal(1, session.WeeklyCommission.Progress);
        RecordStarbudPlanting(session, 2);
        Assert.True(session.AdvanceWeeklyCommissionStage().Succeeded);
        var tree = FindWorldResource(WorldResourceKind.Tree);
        session.Inventory.Select(1);

        Assert.False(session.UseSelected(tree).Succeeded);
        Assert.Equal(0, session.WeeklyCommission.Progress);
        session.Inventory.Select(2);
        Assert.True(session.UseSelected(tree).Succeeded);
        Assert.Equal(2, session.WeeklyCommission.Progress);
    }

    [Fact]
    public void InsufficientFinalDeliveryNeverDeductsOrAdvances()
    {
        var session = PrepareFinalStage();
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 2));
        var startingCoins = session.Coins;

        var result = session.ClaimWeeklyCommission();

        Assert.False(result.Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonstonePathId));
        Assert.Equal(startingCoins, session.Coins);
        Assert.False(session.WeeklyCommission.Claimed);
        Assert.Equal(
            DataCatalog.StarlitRouteDeliverStageId,
            session.WeeklyCommission.CurrentStage.Id
        );
    }

    [Fact]
    public void FullBackpackMakesFinalRewardAnAtomicFailure()
    {
        var session = PrepareFinalStage();
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 4));
        var seedStack = DataCatalog.Item(DataCatalog.StarbudSeedId).MaxStack;
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            (Inventory.SlotCount - Inventory.StartingToolCount - 1) * seedStack
        ));
        var before = session.Inventory.Capture()
            .Select(slot => (slot.ItemId, slot.Count))
            .ToArray();
        var startingCoins = session.Coins;

        var result = session.ClaimWeeklyCommission();

        Assert.False(result.Succeeded);
        Assert.Equal("weekly_commission.backpack_full", result.MessageKey);
        Assert.Equal(
            before,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );
        Assert.Equal(startingCoins, session.Coins);
        Assert.False(session.WeeklyCommission.Claimed);
    }

    [Fact]
    public void FinalClaimOnlyRemovesCrystalsAndRewardsExactlyOnce()
    {
        var session = PrepareFinalStage();
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 3));
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 5));
        var startingCoins = session.Coins;

        var claimed = session.ClaimWeeklyCommission();

        Assert.True(claimed.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(5, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(4, session.Inventory.Count(DataCatalog.MoonstonePathId));
        Assert.Equal(startingCoins + 120, session.Coins);
        Assert.True(session.WeeklyCommission.Claimed);

        Assert.False(session.ClaimWeeklyCommission().Succeeded);
        Assert.Equal(4, session.Inventory.Count(DataCatalog.MoonstonePathId));
        Assert.Equal(startingCoins + 120, session.Coins);
    }

    [Fact]
    public void UnknownOrDamagedWeeklyStateReturnsToTheCurrentOffer()
    {
        var unknownDefinition = WeeklyCommissionSystem.NormalizeSave(
            new WeeklyCommissionSave
            {
                Week = 2,
                DefinitionId = "unknown_weekly_commission",
                Accepted = true,
                StageId = DataCatalog.StarlitRouteDeliverStageId,
                Progress = 999,
                Claimed = true
            },
            8
        );
        var unknownStage = WeeklyCommissionSystem.NormalizeSave(
            new WeeklyCommissionSave
            {
                Week = 2,
                DefinitionId =
                    DataCatalog.StarlitRouteRestorationWeeklyCommissionId,
                Accepted = true,
                StageId = "unknown_stage",
                Progress = -7
            },
            8
        );

        Assert.Equal(2, unknownDefinition.Week);
        Assert.False(unknownDefinition.Accepted);
        Assert.Equal(
            DataCatalog.StarlitRoutePlantStageId,
            unknownDefinition.StageId
        );
        Assert.False(unknownStage.Accepted);
        Assert.Equal(0, unknownStage.Progress);
        Assert.Equal(
            DataCatalog.StarlitRoutePlantStageId,
            unknownStage.StageId
        );
    }

    [Fact]
    public void DailyAndWeeklyCommissionsProgressAndRefreshIndependently()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.AcceptDailyCommission().Succeeded);
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);

        session.EndDay();

        Assert.False(session.Commission.Accepted);
        Assert.Equal(0, session.Commission.Progress);
        Assert.True(session.WeeklyCommission.Accepted);
        Assert.Equal(1, session.WeeklyCommission.Progress);
    }

    private static GameSession PrepareFinalStage()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        RecordStarbudPlanting(session, 3);
        Assert.True(session.AdvanceWeeklyCommissionStage().Succeeded);
        session.WeeklyCommission.RecordGather(DataCatalog.LumenwoodId, 4);
        Assert.True(session.AdvanceWeeklyCommissionStage().Succeeded);
        return session;
    }

    private static void RecordStarbudPlanting(GameSession session, int count)
    {
        for (var index = 0; index < count; index++)
        {
            session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        }
    }

    private static GridPosition FindWorldResource(WorldResourceKind kind)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.ResourceAt(cell) == kind)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not find a world resource for {kind}."
        );
    }
}

public sealed class ConstructionSystemTests
{
    [Fact]
    public void CottageUpgradeStartsAtomicallyAndAdvancesAcrossTwoSleeps()
    {
        var session = PreparedSession();
        var changedSnapshots = new List<ConstructionTransactionSnapshot>();
        session.Changed += () => changedSnapshots.Add(new(
            session.Coins,
            session.Inventory.Count(DataCatalog.LumenwoodId),
            session.Inventory.Count(DataCatalog.CrystalShardId),
            session.Construction.Phase
        ));

        var result = session.StartCottageFirstUpgrade();

        Assert.True(result.Succeeded);
        Assert.Equal(0, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.True(session.Construction.IsInProgress);
        Assert.Equal(2, session.Construction.RemainingNights);
        var transaction = Assert.Single(changedSnapshots);
        Assert.Equal(ConstructionPhase.InProgress, transaction.Phase);
        Assert.Equal(0, transaction.Coins);
        Assert.Equal(0, transaction.Lumenwood);
        Assert.Equal(0, transaction.CrystalShards);

        session.EndDay();
        Assert.True(session.Construction.IsInProgress);
        Assert.Equal(1, session.Construction.RemainingNights);

        session.EndDay();
        Assert.True(session.Construction.IsCompleted);
        Assert.Equal(0, session.Construction.RemainingNights);
        Assert.Equal(ConstructionPhase.Completed, session.Construction.Phase);
    }

    [Theory]
    [InlineData(239, 12, 4, "construction.insufficient_coins")]
    [InlineData(240, 11, 4, "construction.insufficient_lumenwood")]
    [InlineData(240, 12, 3, "construction.insufficient_crystal")]
    public void FailedStartLeavesCoinsMaterialsAndStateUnchanged(
        int coins,
        int lumenwood,
        int crystal,
        string expectedMessageKey
    )
    {
        var session = PreparedSession(coins, lumenwood, crystal);

        var result = session.StartCottageFirstUpgrade();

        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessageKey, result.MessageKey);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            lumenwood,
            session.Inventory.Count(DataCatalog.LumenwoodId)
        );
        Assert.Equal(
            crystal,
            session.Inventory.Count(DataCatalog.CrystalShardId)
        );
        Assert.Equal(ConstructionPhase.NotStarted, session.Construction.Phase);
    }

    [Fact]
    public void ConstructionCanOnlyStartFromTheWorkshop()
    {
        var session = PreparedSession();
        session.SetPlayerLocation(328, 280, PlayerLocationIds.World);

        var result = session.StartCottageFirstUpgrade();

        Assert.False(result.Succeeded);
        Assert.Equal("construction.workshop_only", result.MessageKey);
        Assert.Equal(240, session.Coins);
        Assert.Equal(12, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(4, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(ConstructionPhase.NotStarted, session.Construction.Phase);
    }

    [Fact]
    public void CompletedKitchenReserveUsesTheHandAndSharedPreviewRule()
    {
        var session = PreparedSession();
        Assert.True(session.StartCottageFirstUpgrade().Succeeded);
        session.EndDay();
        session.EndDay();
        session.SetPlayerLocation(26 * 16 + 8, 14 * 16 + 8, PlayerLocationIds.Cottage);

        var available = session.PreviewSelectedTarget(
            CottageLayout.KitchenReserveCell
        );
        Assert.True(available.IsAvailable);
        Assert.Equal(TargetPreviewKind.KitchenReserve, available.Kind);
        Assert.Equal(
            "target.action.inspect_kitchen_reserve",
            available.LabelKey
        );
        Assert.True(session.InspectKitchenReserve(
            CottageLayout.KitchenReserveCell
        ).Succeeded);
        session.SetPlayerLocation(
            35 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        var farEdge = session.PreviewSelectedTarget(
            new GridPosition(35, 17)
        );
        Assert.True(farEdge.IsAvailable);
        Assert.Equal(
            CottageLayout.KitchenReserveCell,
            farEdge.Target
        );
        Assert.False(session.InspectKitchenReserve(
            new GridPosition(27, 10)
        ).Succeeded);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            new GridPosition(35, 17)
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(session.InspectKitchenReserve(
            new GridPosition(35, 17)
        ).Succeeded);
    }

    [Fact]
    public void SharedCottageLayoutKeepsDoorAndBedRoutesOpen()
    {
        Assert.True(CottageLayout.IsWalkable(
            CottageLayout.SafeArrivalCell,
            upgraded: true
        ));
        Assert.True(CottageLayout.IsWalkable(
            new GridPosition(20, 10),
            upgraded: true
        ));
        Assert.False(CottageLayout.IsWalkable(
            CottageLayout.BedCell,
            upgraded: true
        ));
        Assert.True(CottageLayout.IsWalkable(
            new GridPosition(27, 14),
            upgraded: false
        ));
        Assert.False(CottageLayout.IsWalkable(
            new GridPosition(27, 14),
            upgraded: true
        ));
        Assert.True(CottageLayout.IsAdjacentToKitchenReserve(
            new GridPosition(26, 10)
        ));
        Assert.True(CottageLayout.IsAdjacentToKitchenReserve(
            new GridPosition(35, 18)
        ));
        Assert.False(CottageLayout.IsAdjacentToKitchenReserve(
            new GridPosition(24, 14)
        ));
    }

    [Fact]
    public void CompletedUpgradeMovesLegacyKitchenPositionToSafeArrival()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.Cottage;
        save.Player.X = 27 * 16 + 8;
        save.Player.Y = 14 * 16 + 8;
        save.Construction = new ConstructionSave
        {
            ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
            Completed = true
        };

        session.Restore(save);

        Assert.Equal(CottageLayout.SafeArrivalCell, session.PlayerCell);
        Assert.True(CottageLayout.IsWalkable(
            session.PlayerCell,
            upgraded: true
        ));
    }

    [Fact]
    public void LegacyKitchenExtensionPositionStaysValidBeforeUpgrade()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.Cottage;
        save.Player.X = 27 * 16 + 8;
        save.Player.Y = 14 * 16 + 8;

        session.Restore(save);

        Assert.Equal(new GridPosition(27, 14), session.PlayerCell);
        Assert.True(CottageLayout.IsWalkable(
            session.PlayerCell,
            upgraded: false
        ));
    }

    private static GameSession PreparedSession(
        int coins = 240,
        int lumenwood = 12,
        int crystal = 4
    )
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.LumenwoodId, lumenwood);
        session.Inventory.Add(DataCatalog.CrystalShardId, crystal);
        var save = session.Capture();
        save.Coins = coins;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        session.Restore(save);
        return session;
    }

    private sealed record ConstructionTransactionSnapshot(
        int Coins,
        int Lumenwood,
        int CrystalShards,
        ConstructionPhase Phase
    );
}

public sealed class StarlightSystemTests
{
    [Fact]
    public void HarvestNodeAcceptsQualityVariantsButRecordsTheBaseCropId()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudStarlightId,
            1
        ));

        var result = session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ContributedCount);
        Assert.Equal(
            1,
            session.Starlight.ContributionCount(
                DataCatalog.WoodlandHarvestNodeId,
                DataCatalog.StarbudId
            )
        );
        Assert.Equal(
            0,
            session.Inventory.Count(DataCatalog.StarbudStarlightId)
        );
    }

    [Fact]
    public void PedestalPreviewAndActionShareTheHandRuleWithoutMutatingOnFailure()
    {
        var session = new GameSession();
        session.NewGame();
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        session.Inventory.Select(1);
        var startingEnergy = session.Energy;

        var wrongToolPreview = session.PreviewSelectedTarget(pedestal);
        var wrongToolAction = session.UseSelected(pedestal);

        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal(TargetPreviewKind.StarlightPedestal, wrongToolPreview.Kind);
        Assert.Equal("target.need.hand", wrongToolPreview.LabelKey);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);
        Assert.Equal(startingEnergy, session.Energy);
        Assert.False(session.Starlight.Discovered);
        Assert.Equal(0, session.Starlight.CompletedNodeCount);

        session.Inventory.Select(0);
        var handPreview = session.PreviewSelectedTarget(pedestal);
        var handAction = session.UseSelected(pedestal);

        Assert.True(handPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.StarlightPedestal, handPreview.Kind);
        Assert.Equal("target.action.open_starlight", handPreview.LabelKey);
        Assert.True(handAction.Succeeded);
        Assert.Equal("starlight.opened", handAction.MessageKey);
        Assert.True(session.Starlight.Discovered);
        Assert.Equal(startingEnergy, session.Energy);
    }

    [Fact]
    public void MoonwaterPedestalPreviewAndActionUseTheWetlandTargetOnly()
    {
        var session = new GameSession();
        session.NewGame();
        var pedestal = WorldDefinition.MoonwaterStarlightCell;
        session.Inventory.Select(1);
        var startingEnergy = session.Energy;

        var wrongToolPreview = session.PreviewSelectedTarget(pedestal);
        var wrongToolAction = session.UseSelected(pedestal);

        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal(TargetPreviewKind.StarlightPedestal, wrongToolPreview.Kind);
        Assert.Equal("target.need.hand", wrongToolPreview.LabelKey);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);
        Assert.Equal(startingEnergy, session.Energy);
        Assert.False(session.Starlight.Discovered);
        Assert.False(session.Starlight.IsDiscovered(
            DataCatalog.MoonwaterStarlightId
        ));

        session.Inventory.Select(0);
        var handPreview = session.PreviewSelectedTarget(pedestal);
        var handAction = session.UseSelected(pedestal);

        Assert.True(handPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.StarlightPedestal, handPreview.Kind);
        Assert.Equal("target.action.open_starlight", handPreview.LabelKey);
        Assert.True(handAction.Succeeded);
        Assert.Equal("starlight.opened.moonwater", handAction.MessageKey);
        Assert.False(session.Starlight.Discovered);
        Assert.True(session.Starlight.IsDiscovered(
            DataCatalog.MoonwaterStarlightId
        ));
        Assert.Equal(startingEnergy, session.Energy);
    }

    [Fact]
    public void ThreeNodesAcceptPartialDistinctOfferingsAndActivateOnlyOnce()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.StarbudId, 1);
        session.Inventory.Add(DataCatalog.MoonrootId, 1);

        var harvestPartial = session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );

        Assert.True(harvestPartial.Succeeded);
        Assert.Equal(2, harvestPartial.ContributedCount);
        Assert.Equal(2, session.Starlight.Progress(
            DataCatalog.WoodlandHarvestNodeId
        ));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonrootId));

        var noItems = session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        Assert.False(noItems.Succeeded);
        Assert.Equal("starlight.nothing_available", noItems.MessageKey);
        Assert.Equal(2, session.Starlight.Progress(
            DataCatalog.WoodlandHarvestNodeId
        ));

        session.Inventory.Add(DataCatalog.StarbudId, 1);
        var duplicateCrop = session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        Assert.False(duplicateCrop.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(2, session.Starlight.Progress(
            DataCatalog.WoodlandHarvestNodeId
        ));

        session.Inventory.Add(DataCatalog.CloudleafId, 1);
        var harvestComplete = session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        Assert.True(harvestComplete.Succeeded);
        Assert.Equal("starlight.node_completed", harvestComplete.MessageKey);
        Assert.True(session.Starlight.IsNodeComplete(
            DataCatalog.WoodlandHarvestNodeId
        ));

        session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        var materialsPartial = session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        Assert.True(materialsPartial.Succeeded);
        Assert.Equal(6, materialsPartial.ContributedCount);
        Assert.Equal(6, session.Starlight.Progress(
            DataCatalog.WoodlandMaterialsNodeId
        ));

        session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        var materialsComplete = session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        Assert.True(materialsComplete.Succeeded);
        Assert.True(session.Starlight.IsNodeComplete(
            DataCatalog.WoodlandMaterialsNodeId
        ));
        Assert.False(session.Starlight.RewardUnlocked);

        session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        session.Inventory.Add(DataCatalog.MoonrootTonicId, 1);
        session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        var activated = session.ContributeToStarlightNode(
            DataCatalog.WoodlandCraftNodeId
        );

        Assert.True(activated.Succeeded);
        Assert.True(activated.Activated);
        Assert.Equal("starlight.activated", activated.MessageKey);
        Assert.True(session.Starlight.RewardUnlocked);
        Assert.Equal(3, session.Starlight.CompletedNodeCount);

        var secondActivation = session.ContributeToStarlightNode(
            DataCatalog.WoodlandCraftNodeId
        );
        Assert.False(secondActivation.Succeeded);
        Assert.False(secondActivation.Activated);
        Assert.Equal(
            "starlight.node_already_complete",
            secondActivation.MessageKey
        );
        Assert.Equal(3, session.Starlight.CompletedNodeCount);
    }

    [Fact]
    public void MoonwaterNodesAcceptFishWithoutUnlockingWoodlandRenewal()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.MoonwaterMinnowId, 1);
        session.Inventory.Add(DataCatalog.MarshveilKilliId, 1);
        session.Inventory.Add(DataCatalog.SilverreedMudfishId, 1);

        var local = session.ContributeToStarlightNode(
            DataCatalog.MoonwaterLocalFishNodeId
        );

        Assert.True(local.Succeeded);
        Assert.Equal("starlight.node_completed", local.MessageKey);
        Assert.Equal(DataCatalog.MoonwaterStarlightId, local.PedestalId);
        Assert.Equal(3, session.Starlight.Progress(
            DataCatalog.MoonwaterLocalFishNodeId
        ));
        Assert.Equal(0, session.Inventory.Count(
            DataCatalog.MoonwaterMinnowId
        ));

        session.Inventory.Add(DataCatalog.RainveilLampreyId, 1);
        session.Inventory.Add(DataCatalog.StardustRayId, 1);
        var weather = session.ContributeToStarlightNode(
            DataCatalog.MoonwaterWeatherFishNodeId
        );
        Assert.True(weather.Succeeded);
        Assert.Equal("starlight.node_completed", weather.MessageKey);

        session.Inventory.Add(DataCatalog.StarharvestOrbfinId, 1);
        session.Inventory.Add(DataCatalog.LongnightWispfishId, 1);
        var activated = session.ContributeToStarlightNode(
            DataCatalog.MoonwaterSeasonalFishNodeId
        );

        Assert.True(activated.Succeeded);
        Assert.True(activated.Activated);
        Assert.Equal("starlight.activated.moonwater", activated.MessageKey);
        Assert.True(session.Starlight.MoonwaterTideUnlocked);
        Assert.False(session.Starlight.WoodlandRenewalUnlocked);
        Assert.False(session.Starlight.RewardUnlocked);
        Assert.Equal(3, session.Starlight.CompletedNodeCountFor(
            DataCatalog.MoonwaterStarlightId
        ));
        Assert.Equal(0, session.Starlight.CompletedNodeCount);

        var save = session.Capture();
        Assert.False(save.Starlight.RewardUnlocked);
        var moonwater = save.Starlight.Pedestals.Single(pedestal =>
            pedestal.PedestalId == DataCatalog.MoonwaterStarlightId
        );
        Assert.True(moonwater.Discovered);
        Assert.True(moonwater.RewardUnlocked);
    }

    [Fact]
    public void WoodlandRenewalShortensOnlyWhisperingWoodsTreeRespawn()
    {
        var resources = new WorldResourceSystem();
        var inventory = new Inventory();
        inventory.Reset();
        var woodlandTree = FindTree(WorldBiome.WhisperingWoods);
        var otherTree = FindTree(WorldBiome.StarfallMeadow);

        Assert.True(resources.TryGather(
            woodlandTree,
            DataCatalog.MacheteId,
            GameSession.MaxEnergy,
            inventory,
            1
        ).Succeeded);
        Assert.True(resources.TryGather(
            otherTree,
            DataCatalog.MacheteId,
            GameSession.MaxEnergy,
            inventory,
            1
        ).Succeeded);

        Assert.Equal(1, resources.ResolveDay(5, true));
        Assert.False(resources.IsRemoved(woodlandTree));
        Assert.True(resources.IsRemoved(otherTree));

        Assert.Equal(1, resources.ResolveDay(8, true));
        Assert.False(resources.IsRemoved(otherTree));
    }

    private static GridPosition FindTree(WorldBiome biome)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.GetBiome(cell) == biome &&
                    WorldDefinition.ResourceAt(cell) == WorldResourceKind.Tree)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            $"No tree found for biome {biome}."
        );
    }
}

public sealed class VillageSystemTests
{
    private static readonly string[] ScheduleWeatherIds =
    [
        DataCatalog.ClearWeatherId,
        DataCatalog.RainWeatherId,
        DataCatalog.StardustWindWeatherId
    ];

    [Fact]
    public void VillagersWalkOneCardinalCellPerClockTickWithoutOverlap()
    {
        var village = new VillageSystem();

        foreach (var day in new[] { 1, CalendarSystem.DaysPerWeek })
        {
            IReadOnlyDictionary<string, VillageNpcState>? previous = null;
            for (var minute = GameClock.StartMinute;
                 minute <= GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var current = village.AllCurrentNpcs(day, minute);
                Assert.Equal(8, current.Count);
                Assert.Equal(
                    current.Count,
                    current
                        .Select(npc => (npc.LocationId, npc.Position))
                        .Distinct()
                        .Count()
                );
                Assert.All(current, npc =>
                {
                    Assert.True(NpcNavigationMap.IsNpcPassable(
                        npc.LocationId,
                        npc.Position
                    ));
                    Assert.False(
                        NpcNavigationMap.IsCriticalEntranceCell(
                            npc.LocationId,
                            npc.Position
                        )
                    );
                });

                var byId = current.ToDictionary(npc => npc.Definition.Id);
                if (previous is not null)
                {
                    foreach (var npcId in VillageCatalog.Npcs.Keys)
                    {
                        var before = previous[npcId];
                        var after = byId[npcId];
                        if (before.LocationId != after.LocationId)
                        {
                            Assert.Equal(
                                NpcNavigationMap.SafeArrivalCell(
                                    before.LocationId,
                                    after.LocationId
                                ),
                                after.Position
                            );
                            continue;
                        }

                        var distance = Math.Abs(
                            before.Position.X - after.Position.X
                        ) + Math.Abs(
                            before.Position.Y - after.Position.Y
                        );
                        Assert.InRange(distance, 0, 1);
                        if (distance == 1)
                        {
                            Assert.Equal(
                                FacingFromStep(before.Position, after.Position),
                                after.Facing
                            );
                        }
                    }
                }

                previous = byId;
            }
        }
    }

    [Fact]
    public void CrossSceneSchedulesUseSafeEntrancesThenContinueWalking()
    {
        var village = new VillageSystem();
        var beforeEntry = village.AllCurrentNpcs(1, 8 * 60 + 50)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        var atEntry = village.AllCurrentNpcs(1, 9 * 60)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        var afterEntry = village.AllCurrentNpcs(1, 9 * 60 + 10)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);

        Assert.Equal(PlayerLocationIds.World, beforeEntry.LocationId);
        Assert.Equal(PlayerLocationIds.MoonlitArchive, atEntry.LocationId);
        Assert.Equal(
            NpcNavigationMap.SafeArrivalCell(
                PlayerLocationIds.World,
                PlayerLocationIds.MoonlitArchive
            ),
            atEntry.Position
        );
        Assert.NotEqual(
            VillageCatalog.MoonlitArchiveExitCell,
            atEntry.Position
        );
        Assert.Equal(1, Distance(atEntry.Position, afterEntry.Position));

        var beforeExit = village.AllCurrentNpcs(1, 12 * 60 + 50)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        var atExit = village.AllCurrentNpcs(1, 13 * 60)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        Assert.Equal(PlayerLocationIds.MoonlitArchive, beforeExit.LocationId);
        Assert.Equal(PlayerLocationIds.World, atExit.LocationId);
        Assert.Equal(
            NpcNavigationMap.SafeArrivalCell(
                PlayerLocationIds.MoonlitArchive,
                PlayerLocationIds.World
            ),
            atExit.Position
        );
        Assert.NotEqual(
            VillageCatalog.MoonlitArchiveDoorCell,
            atExit.Position
        );
    }

    [Fact]
    public void PathfinderSharesWorldAndInteriorCollisionGeometry()
    {
        var worldPath = NpcPathfinder.FindPath(
            PlayerLocationIds.World,
            new GridPosition(86, 42),
            new GridPosition(104, 43)
        );
        Assert.NotEmpty(worldPath);
        Assert.All(worldPath, cell =>
        {
            Assert.False(WorldDefinition.IsBlocked(cell));
            Assert.True(NpcNavigationMap.IsNpcPassable(
                PlayerLocationIds.World,
                cell
            ));
        });

        var archivePath = NpcPathfinder.FindPath(
            PlayerLocationIds.MoonlitArchive,
            new GridPosition(20, 17),
            new GridPosition(12, 9)
        );
        Assert.NotEmpty(archivePath);
        Assert.DoesNotContain(new GridPosition(20, 10), archivePath);
        Assert.All(archivePath, cell => Assert.True(
            NpcNavigationMap.IsNpcPassable(
                PlayerLocationIds.MoonlitArchive,
                cell
            )
        ));
    }

    [Fact]
    public void FailedRouteFallsBackToTheSafeScheduleAnchor()
    {
        var definition = VillageCatalog.Npcs[VillageCatalog.LioraId];
        var entry = NpcScheduleSystem.SelectEntry(
            definition,
            1,
            14 * 60,
            WeatherSystem.WeatherForDay(1)
        );
        Assert.NotNull(entry);
        var invalidPrevious = new VillageNpcState(
            definition,
            PlayerLocationIds.World,
            new GridPosition(86, 36),
            NpcFacing.Down,
            entry.DialogueKey
        );

        var projected = NpcScheduleSystem.ProjectRouteOrFallback(
            definition,
            entry,
            invalidPrevious
        );

        Assert.Equal(entry.Position, projected.Position);
        Assert.Equal(entry.LocationId, projected.LocationId);
        Assert.True(NpcNavigationMap.IsNpcPassable(
            projected.LocationId,
            projected.Position
        ));
    }

    [Fact]
    public void RuntimePlayerReservationAccumulatesWaitsAndReleasesOneStep()
    {
        var probe = new VillageSystem();
        var farPlayer = new GridPosition(80, 60);
        var before = probe.AllCurrentNpcs(
            1,
            13 * 60,
            PlayerLocationIds.World,
            farPlayer
        );
        var expectedNext = probe.AllCurrentNpcs(
            1,
            13 * 60 + 10,
            PlayerLocationIds.World,
            farPlayer
        ).Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        var lioraBefore = before.Single(
            npc => npc.Definition.Id == VillageCatalog.LioraId
        );
        Assert.Equal(1, Distance(lioraBefore.Position, expectedNext.Position));

        var blocked = new VillageSystem();
        var initial = blocked.AllCurrentNpcs(
            1,
            13 * 60,
            PlayerLocationIds.World,
            farPlayer
        );
        var start = initial.Single(
            npc => npc.Definition.Id == VillageCatalog.LioraId
        );
        foreach (var minute in new[]
                 {
                     13 * 60 + 10,
                     13 * 60 + 20,
                     13 * 60 + 30
                 })
        {
            var states = blocked.AllCurrentNpcs(
                1,
                minute,
                PlayerLocationIds.World,
                expectedNext.Position
            );
            var liora = states.Single(
                npc => npc.Definition.Id == VillageCatalog.LioraId
            );
            Assert.Equal(start.Position, liora.Position);
            Assert.DoesNotContain(states, npc =>
                npc.LocationId == PlayerLocationIds.World &&
                npc.Position == expectedNext.Position
            );
            Assert.Equal(
                states.Count,
                states
                    .Select(npc => (npc.LocationId, npc.Position))
                    .Distinct()
                    .Count()
            );
        }

        var released = blocked.AllCurrentNpcs(
            1,
            13 * 60 + 40,
            PlayerLocationIds.World,
            farPlayer
        ).Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        Assert.Equal(1, Distance(start.Position, released.Position));
    }

    [Fact]
    public void ScheduleProjectionCacheAndSaveRebuildStayDeterministic()
    {
        var village = new VillageSystem();
        var first = village.AllCurrentNpcs(3, 14 * 60 + 30);
        var repeated = village.AllCurrentNpcs(3, 14 * 60 + 30);
        Assert.Same(first, repeated);

        var player = new GridPosition(80, 60);
        var runtimeFirst = village.AllCurrentNpcs(
            3,
            14 * 60 + 40,
            PlayerLocationIds.World,
            player
        );
        var runtimeRepeated = village.AllCurrentNpcs(
            3,
            14 * 60 + 40,
            PlayerLocationIds.World,
            player
        );
        Assert.Same(runtimeFirst, runtimeRepeated);

        var restored = new VillageSystem();
        restored.Restore(village.Capture());
        Assert.Equal(
            first.Select(StateIdentity),
            restored
                .AllCurrentNpcs(3, 14 * 60 + 30)
                .Select(StateIdentity)
        );
    }

    [Fact]
    public void EightVillagersHaveCompleteDistinctDailySchedules()
    {
        var village = new VillageSystem();

        foreach (var day in new[] { 1, CalendarSystem.DaysPerWeek })
        {
            for (var minute = GameClock.StartMinute;
                 minute < GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var current = village.AllCurrentNpcs(day, minute);
                Assert.Equal(8, current.Count);
                Assert.Equal(
                    current.Count,
                    current
                        .Select(npc => (npc.LocationId, npc.Position))
                        .Distinct()
                        .Count()
                );
                Assert.All(current, npc =>
                {
                    Assert.True(PlayerLocationIds.IsValid(npc.LocationId));
                    if (npc.LocationId == PlayerLocationIds.World)
                    {
                        Assert.True(
                            VillageCatalog.IsVillageCell(npc.Position)
                        );
                        Assert.False(
                            WorldDefinition.IsBlocked(npc.Position)
                        );
                        Assert.NotEqual(
                            VillageCatalog.MoonlitArchiveDoorCell,
                            npc.Position
                        );
                        Assert.NotEqual(
                            VillageCatalog.TwilightEmporiumDoorCell,
                            npc.Position
                        );
                        Assert.NotEqual(
                            VillageCatalog.VillageGateCell,
                            npc.Position
                        );
                    }
                });
            }
        }

        var weekday = village.AllCurrentNpcs(1, 10 * 60)
            .ToDictionary(npc => npc.Definition.Id);
        var restday = village.AllCurrentNpcs(7, 10 * 60)
            .ToDictionary(npc => npc.Definition.Id);
        Assert.All(VillageCatalog.Npcs.Keys, npcId =>
            Assert.NotEqual(
                (
                    weekday[npcId].LocationId,
                    weekday[npcId].Position
                ),
                (
                    restday[npcId].LocationId,
                    restday[npcId].Position
                )
            )
        );
    }

    [Fact]
    public void EveryVillagerHasBaseAndConditionalScheduleData()
    {
        Assert.All(VillageCatalog.Npcs.Values, definition =>
        {
            Assert.Contains(
                definition.Schedule,
                entry => entry.Priority ==
                    VillageCatalog.BaseSchedulePriority
            );
            Assert.Contains(
                definition.Schedule,
                entry => entry.WeatherIds is { Count: > 0 } ||
                    entry.SeasonIds is { Count: > 0 }
            );
            Assert.All(
                definition.Schedule.Where(entry =>
                    entry.LocationId == PlayerLocationIds.World &&
                    (entry.WeatherIds is { Count: > 0 } ||
                        entry.SeasonIds is { Count: > 0 })
                ),
                entry => Assert.True(
                    VillageCatalog.IsVillagePath(entry.Position)
                )
            );
        });
    }

    [Fact]
    public void ConditionalSchedulesStayCompleteDistinctAndPassable()
    {
        var village = new VillageSystem();

        for (var day = 1; day <= CalendarSystem.DaysPerYear; day++)
        {
            foreach (var weatherId in ScheduleWeatherIds)
            {
                IReadOnlyDictionary<string, VillageNpcState>? previous = null;
                for (var minute = GameClock.StartMinute;
                     minute <= GameClock.EndMinute;
                     minute += GameClock.MinutesPerTick)
                {
                    var current = village.AllCurrentNpcs(
                        day,
                        minute,
                        weatherId
                    );
                    Assert.Equal(VillageCatalog.Npcs.Count, current.Count);
                    Assert.Equal(
                        current.Count,
                        current
                            .Select(npc =>
                                (npc.LocationId, npc.Position)
                            )
                            .Distinct()
                            .Count()
                    );
                    Assert.All(current, npc =>
                        AssertScheduleAnchorPassable(npc)
                    );

                    var byId = current.ToDictionary(
                        npc => npc.Definition.Id
                    );
                    if (previous is not null)
                    {
                        AssertScheduleTransition(previous, byId);
                    }

                    previous = byId;
                }
            }
        }
    }

    [Fact]
    public void SameDayWeatherChangesRebuildTheConditionalPathTimeline()
    {
        var village = new VillageSystem();
        var clear = village.AllCurrentNpcs(
            1,
            14 * 60,
            DataCatalog.ClearWeatherId
        );
        var rain = village.AllCurrentNpcs(
            1,
            14 * 60,
            DataCatalog.RainWeatherId
        );
        var clearAgain = village.AllCurrentNpcs(
            1,
            14 * 60,
            DataCatalog.ClearWeatherId
        );

        var clearNemi = clear.Single(
            npc => npc.Definition.Id == VillageCatalog.NemiId
        );
        var rainyNemi = rain.Single(
            npc => npc.Definition.Id == VillageCatalog.NemiId
        );
        Assert.Equal(
            "village.npc.nemi.season_gleamrise",
            clearNemi.DialogueKey
        );
        Assert.Equal(
            "village.npc.nemi.weather_rain",
            rainyNemi.DialogueKey
        );
        Assert.NotEqual(clearNemi.LocationId, rainyNemi.LocationId);
        Assert.Equal(
            clear.Select(StateIdentity),
            clearAgain.Select(StateIdentity)
        );
    }

    [Fact]
    public void RestdayOverridesWeatherAndSeasonConditions()
    {
        var rainyRestday = VillageCatalog.CurrentNpc(
            VillageCatalog.SelaId,
            CalendarSystem.DaysPerWeek,
            14 * 60,
            DataCatalog.RainWeatherId
        );
        var windyRestday = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            CalendarSystem.DaysPerWeek,
            14 * 60,
            DataCatalog.StardustWindWeatherId
        );

        Assert.NotNull(rainyRestday);
        Assert.Equal(PlayerLocationIds.World, rainyRestday.LocationId);
        Assert.Equal("village.npc.sela.restday", rainyRestday.DialogueKey);
        Assert.NotNull(windyRestday);
        Assert.Equal(PlayerLocationIds.World, windyRestday.LocationId);
        Assert.Equal("village.npc.tavi.restday", windyRestday.DialogueKey);
    }

    [Fact]
    public void WeatherConditionsOverrideSeasonWhileClearUsesAllFourSeasons()
    {
        var gleamrise = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            14 * 60,
            DataCatalog.ClearWeatherId
        );
        var rainveil = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            15,
            14 * 60,
            DataCatalog.ClearWeatherId
        );
        var starharvest = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            29,
            14 * 60,
            DataCatalog.ClearWeatherId
        );
        var longnight = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            43,
            14 * 60,
            DataCatalog.ClearWeatherId
        );
        var rainOverridesGleamrise = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            14 * 60,
            DataCatalog.RainWeatherId
        );

        Assert.Equal(
            "village.npc.nemi.season_gleamrise",
            gleamrise?.DialogueKey
        );
        Assert.Equal(
            "village.npc.vessa.season_rainveil",
            rainveil?.DialogueKey
        );
        Assert.Equal(
            "village.npc.orin.season_starharvest",
            starharvest?.DialogueKey
        );
        Assert.Equal(
            "village.npc.liora.season_longnight",
            longnight?.DialogueKey
        );
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            rainOverridesGleamrise?.LocationId
        );
        Assert.Equal(
            "village.npc.nemi.weather_rain",
            rainOverridesGleamrise?.DialogueKey
        );
    }

    [Fact]
    public void RestoredCurrentWeatherDrivesProjectionPreviewAndEventEligibility()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = 1,
            CurrentId = DataCatalog.StardustWindWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Village.MetNpcIds = [VillageCatalog.TaviId];
        save.Village.Relationships =
        [
            new VillageRelationshipSave
            {
                NpcId = VillageCatalog.TaviId,
                Points = 25
            }
        ];
        session.Restore(save);

        Assert.NotEqual(
            WeatherSystem.WeatherForDay(session.Clock.Day),
            session.Weather.CurrentId
        );
        var tavi = session.Village.AllCurrentNpcs(
                session.Clock.Day,
                session.Clock.MinuteOfDay
            )
            .Single(npc => npc.Definition.Id == VillageCatalog.TaviId);
        Assert.Equal(PlayerLocationIds.MoonstoneWorkshop, tavi.LocationId);
        Assert.Equal(
            "village.npc.tavi.weather_stardust",
            tavi.DialogueKey
        );

        var preview = session.PreviewSelectedTarget(tavi.Position);
        Assert.True(preview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);
        var eligible = session.CharacterEvents.EligibleEvent(
            tavi.Position,
            session.Clock.Day,
            session.Clock.MinuteOfDay,
            session.PlayerLocationId,
            DataCatalog.HandId,
            session.Village,
            session.PlayerCell
        );
        Assert.NotNull(eligible);
        Assert.Equal(
            CharacterEventCatalog.TaviCrackedMoonRuneId,
            eligible.Id
        );
        var conversation = session.InteractWithVillager(
            tavi.Position,
            out var result
        );
        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.TaviCrackedMoonRuneId,
            conversation.CharacterEvent.EventId
        );
    }

    private static void AssertScheduleAnchorPassable(
        VillageNpcState npc
    )
    {
        Assert.True(PlayerLocationIds.IsValid(npc.LocationId));
        Assert.True(NpcNavigationMap.IsNpcPassable(
            npc.LocationId,
            npc.Position
        ));
        Assert.False(NpcNavigationMap.IsCriticalEntranceCell(
            npc.LocationId,
            npc.Position
        ));
    }

    private static void AssertScheduleTransition(
        IReadOnlyDictionary<string, VillageNpcState> previous,
        IReadOnlyDictionary<string, VillageNpcState> current
    )
    {
        foreach (var npcId in VillageCatalog.Npcs.Keys)
        {
            var before = previous[npcId];
            var after = current[npcId];
            if (before.LocationId != after.LocationId)
            {
                Assert.Equal(
                    NpcNavigationMap.SafeArrivalCell(
                        before.LocationId,
                        after.LocationId
                    ),
                    after.Position
                );
                continue;
            }

            Assert.InRange(Distance(before.Position, after.Position), 0, 1);
        }

        var npcIds = VillageCatalog.Npcs.Keys.ToArray();
        for (var firstIndex = 0;
             firstIndex < npcIds.Length;
             firstIndex++)
        {
            for (var secondIndex = firstIndex + 1;
                 secondIndex < npcIds.Length;
                 secondIndex++)
            {
                var firstBefore = previous[npcIds[firstIndex]];
                var secondBefore = previous[npcIds[secondIndex]];
                var firstAfter = current[npcIds[firstIndex]];
                var secondAfter = current[npcIds[secondIndex]];
                var sameScene = firstBefore.LocationId ==
                    secondBefore.LocationId &&
                    firstAfter.LocationId == secondAfter.LocationId;
                var swapped = firstBefore.Position == secondAfter.Position &&
                    secondBefore.Position == firstAfter.Position;
                Assert.False(sameScene && swapped);
            }
        }
    }

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static NpcFacing FacingFromStep(
        GridPosition start,
        GridPosition destination
    )
    {
        if (destination.X < start.X)
        {
            return NpcFacing.Left;
        }

        if (destination.X > start.X)
        {
            return NpcFacing.Right;
        }

        if (destination.Y < start.Y)
        {
            return NpcFacing.Up;
        }

        return NpcFacing.Down;
    }

    private static object StateIdentity(VillageNpcState state) => new
    {
        NpcId = state.Definition.Id,
        state.LocationId,
        state.Position,
        state.Facing,
        state.DialogueKey
    };
    [Theory]
    [InlineData(VillageCatalog.LioraId, DataCatalog.MoonrootId)]
    [InlineData(VillageCatalog.TaviId, DataCatalog.LumenwoodId)]
    [InlineData(VillageCatalog.NemiId, DataCatalog.StarbudId)]
    [InlineData(VillageCatalog.SelaId, DataCatalog.CrystalShardId)]
    [InlineData(VillageCatalog.ElowenId, DataCatalog.DewmelonId)]
    [InlineData(VillageCatalog.VessaId, DataCatalog.CloudleafId)]
    [InlineData(VillageCatalog.OrinId, DataCatalog.StarbudPreserveId)]
    [InlineData(VillageCatalog.KaelId, DataCatalog.StarlightTorchId)]
    public void EveryVillagerSupportsTalkGiftAndRelationshipProgress(
        string npcId,
        string lovedGiftId
    )
    {
        var session = new GameSession();
        session.NewGame();
        var npc = VillageCatalog.CurrentNpc(
            npcId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        );
        Assert.NotNull(npc);

        var introduction = session.InteractWithVillager(
            npc.Position,
            out var talkResult
        );
        Assert.True(talkResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(2, introduction.RelationshipPoints);

        Assert.True(session.Inventory.Add(lovedGiftId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(lovedGiftId));
        var preview = session.PreviewSelectedTarget(npc.Position);
        Assert.True(preview.IsAvailable);
        Assert.Equal("target.action.gift", preview.LabelKey);

        var gift = session.InteractWithVillager(
            npc.Position,
            out var giftResult
        );
        Assert.True(giftResult.Succeeded);
        Assert.NotNull(gift);
        Assert.Equal(GiftReaction.Loved, gift.GiftReaction);
        Assert.Equal(14, gift.RelationshipPoints);
        Assert.Equal(
            14,
            session.Village.Relationship(npcId).Points
        );
        Assert.Equal(0, session.Inventory.Count(lovedGiftId));
    }

    [Fact]
    public void VillagerPreviewAndInteractionShareTheHandRule()
    {
        var session = new GameSession();
        session.NewGame();
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        );
        Assert.NotNull(liora);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(liora.Position);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal(TargetPreviewKind.Character, wrongTool.Kind);
        Assert.Null(session.InteractWithVillager(
            liora.Position,
            out var blocked
        ));
        Assert.False(blocked.Succeeded);
        Assert.Empty(session.Village.MetNpcIds);

        session.Inventory.Select(0);
        var ready = session.PreviewSelectedTarget(liora.Position);
        Assert.True(ready.IsAvailable);
        Assert.Equal("target.action.talk", ready.LabelKey);

        var introduction = session.InteractWithVillager(
            liora.Position,
            out var firstResult
        );
        Assert.True(firstResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.liora.intro",
            introduction.DialogueKey
        );
        Assert.Contains(
            VillageCatalog.LioraId,
            session.Village.MetNpcIds
        );

        var repeat = session.InteractWithVillager(
            liora.Position,
            out var repeatResult
        );
        Assert.True(repeatResult.Succeeded);
        Assert.NotNull(repeat);
        Assert.False(repeat.FirstMeeting);
        Assert.Equal(liora.DialogueKey, repeat.DialogueKey);
        Assert.Equal(
            2,
            session.Village.Relationship(
                VillageCatalog.LioraId
            ).Points
        );
    }

    [Fact]
    public void LioraWorksInsideArchiveAndAcceptsOneGiftPerDay()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(1, 10 * 60);
        session.SetPlayerLocation(
            12 * 16 + 8,
            10 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        );
        Assert.NotNull(liora);
        Assert.Equal(
            PlayerLocationIds.MoonlitArchive,
            liora.LocationId
        );
        Assert.True(session.Inventory.Add(DataCatalog.MoonrootId, 2));
        Assert.True(
            session.Inventory.PromoteToHotbar(DataCatalog.MoonrootId)
        );

        var preview = session.PreviewSelectedTarget(liora.Position);
        Assert.True(preview.IsAvailable);
        Assert.Equal("target.action.gift", preview.LabelKey);
        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.Equal(GiftReaction.Loved, conversation.GiftReaction);
        Assert.Equal(12, conversation.RelationshipPoints);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonrootId));

        var repeatPreview = session.PreviewSelectedTarget(
            liora.Position
        );
        Assert.Equal(TargetPreviewState.Blocked, repeatPreview.State);
        Assert.Equal(
            "village.gift.already_today",
            repeatPreview.LabelKey
        );
        var repeat = session.InteractWithVillager(
            liora.Position,
            out var repeatResult
        );
        Assert.Null(repeat);
        Assert.False(repeatResult.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonrootId));

        session.Clock.Reset(2, 10 * 60);
        Assert.True(
            session.PreviewSelectedTarget(liora.Position).IsAvailable
        );
    }

    [Fact]
    public void TaviWorksInsideWorkshopWhileLanternrestStaysIndependent()
    {
        var beforeWork = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            1,
            VillageCatalog.MoonstoneWorkshopOpenMinute + 30
        );
        var atWork = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            1,
            10 * 60
        );
        var afterWork = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            1,
            14 * 60
        );
        var lanternrest = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            CalendarSystem.DaysPerWeek,
            10 * 60
        );

        Assert.NotNull(beforeWork);
        Assert.Equal(PlayerLocationIds.World, beforeWork.LocationId);
        Assert.NotNull(atWork);
        Assert.Equal(
            PlayerLocationIds.MoonstoneWorkshop,
            atWork.LocationId
        );
        Assert.True(NpcNavigationMap.IsNpcPassable(
            atWork.LocationId,
            atWork.Position
        ));
        Assert.NotEqual(
            VillageCatalog.MoonstoneWorkshopExitCell,
            atWork.Position
        );
        Assert.NotNull(afterWork);
        Assert.Equal(PlayerLocationIds.World, afterWork.LocationId);
        Assert.NotNull(lanternrest);
        Assert.Equal(PlayerLocationIds.World, lanternrest.LocationId);
        Assert.NotEqual(atWork.Position, lanternrest.Position);
    }

    [Fact]
    public void TaviTalkAndGiftUseTheWorkshopSceneProjection()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(1, 10 * 60);
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        var tavi = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        );
        Assert.NotNull(tavi);
        Assert.Equal(
            PlayerLocationIds.MoonstoneWorkshop,
            tavi.LocationId
        );

        var talkPreview = session.PreviewSelectedTarget(tavi.Position);
        Assert.True(talkPreview.IsAvailable);
        Assert.Equal("target.action.talk", talkPreview.LabelKey);
        var talk = session.InteractWithVillager(
            tavi.Position,
            out var talkResult
        );
        Assert.True(talkResult.Succeeded);
        Assert.NotNull(talk);

        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 2));
        Assert.True(
            session.Inventory.PromoteToHotbar(DataCatalog.LumenwoodId)
        );
        var giftPreview = session.PreviewSelectedTarget(tavi.Position);
        Assert.True(giftPreview.IsAvailable);
        Assert.Equal("target.action.gift", giftPreview.LabelKey);
        var gift = session.InteractWithVillager(
            tavi.Position,
            out var giftResult
        );
        Assert.True(giftResult.Succeeded);
        Assert.NotNull(gift);
        Assert.Equal(GiftReaction.Loved, gift.GiftReaction);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.LumenwoodId));

        session.SetPlayerLocation(
            VillageCatalog.MoonstoneWorkshopDoorCell.X * 16 + 8,
            VillageCatalog.MoonstoneWorkshopDoorCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.Null(session.InteractWithVillager(
            tavi.Position,
            out var wrongScene
        ));
        Assert.False(wrongScene.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.LumenwoodId));
    }

    [Fact]
    public void VessaWorksInsideTeaHouseWhileLanternrestStaysIndependent()
    {
        var beforeWork = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            1,
            VillageCatalog.StarweaverTeaHouseOpenMinute - 30
        );
        var atWork = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            1,
            10 * 60
        );
        var afterWork = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            1,
            14 * 60
        );
        var lanternrest = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            CalendarSystem.DaysPerWeek,
            10 * 60
        );

        Assert.NotNull(beforeWork);
        Assert.Equal(PlayerLocationIds.World, beforeWork.LocationId);
        Assert.NotNull(atWork);
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            atWork.LocationId
        );
        Assert.True(NpcNavigationMap.IsNpcPassable(
            atWork.LocationId,
            atWork.Position
        ));
        Assert.NotEqual(
            VillageCatalog.StarweaverTeaHouseExitCell,
            atWork.Position
        );
        Assert.NotNull(afterWork);
        Assert.Equal(PlayerLocationIds.World, afterWork.LocationId);
        Assert.NotNull(lanternrest);
        Assert.Equal(PlayerLocationIds.World, lanternrest.LocationId);
        Assert.NotEqual(atWork.Position, lanternrest.Position);
    }

    [Fact]
    public void VessaTalkAndGiftUseTheTeaHouseSceneProjection()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(1, 10 * 60);
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarweaverTeaHouse
        );
        var vessa = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        );
        Assert.NotNull(vessa);
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            vessa.LocationId
        );

        var talkPreview = session.PreviewSelectedTarget(vessa.Position);
        Assert.True(talkPreview.IsAvailable);
        Assert.Equal("target.action.talk", talkPreview.LabelKey);
        var talk = session.InteractWithVillager(
            vessa.Position,
            out var talkResult
        );
        Assert.True(talkResult.Succeeded);
        Assert.NotNull(talk);

        Assert.True(session.Inventory.Add(DataCatalog.CloudleafId, 2));
        Assert.True(
            session.Inventory.PromoteToHotbar(DataCatalog.CloudleafId)
        );
        var giftPreview = session.PreviewSelectedTarget(vessa.Position);
        Assert.True(giftPreview.IsAvailable);
        Assert.Equal("target.action.gift", giftPreview.LabelKey);
        var gift = session.InteractWithVillager(
            vessa.Position,
            out var giftResult
        );
        Assert.True(giftResult.Succeeded);
        Assert.NotNull(gift);
        Assert.Equal(GiftReaction.Loved, gift.GiftReaction);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.CloudleafId));

        session.SetPlayerLocation(
            VillageCatalog.StarweaverTeaHouseDoorCell.X * 16 + 8,
            VillageCatalog.StarweaverTeaHouseDoorCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.Null(session.InteractWithVillager(
            vessa.Position,
            out var wrongScene
        ));
        Assert.False(wrongScene.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.CloudleafId));
    }

    [Fact]
    public void OrinWorksInsideEmporiumWhileLanternrestStaysIndependent()
    {
        var beforeWork = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            1,
            VillageCatalog.TwilightEmporiumOpenMinute - 30
        );
        var atWork = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            1,
            10 * 60
        );
        var afterWork = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            1,
            14 * 60
        );
        var lanternrest = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            CalendarSystem.DaysPerWeek,
            10 * 60
        );

        Assert.NotNull(beforeWork);
        Assert.Equal(PlayerLocationIds.World, beforeWork.LocationId);
        Assert.NotNull(atWork);
        Assert.Equal(
            PlayerLocationIds.TwilightEmporium,
            atWork.LocationId
        );
        Assert.True(NpcNavigationMap.IsNpcPassable(
            atWork.LocationId,
            atWork.Position
        ));
        Assert.NotEqual(
            VillageCatalog.TwilightEmporiumExitCell,
            atWork.Position
        );
        Assert.NotNull(afterWork);
        Assert.Equal(PlayerLocationIds.World, afterWork.LocationId);
        Assert.NotNull(lanternrest);
        Assert.Equal(PlayerLocationIds.World, lanternrest.LocationId);
        Assert.NotEqual(atWork.Position, lanternrest.Position);
    }

    [Fact]
    public void OrinTalkAndGiftUseTheEmporiumSceneProjection()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(1, 10 * 60);
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        var orin = session.Village.CurrentNpcs(
            session.Clock.Day,
            session.Clock.MinuteOfDay,
            PlayerLocationIds.TwilightEmporium,
            session.PlayerCell
        ).Single(npc => npc.Definition.Id == VillageCatalog.OrinId);
        Assert.Equal(
            PlayerLocationIds.TwilightEmporium,
            orin.LocationId
        );

        var talkPreview = session.PreviewSelectedTarget(orin.Position);
        Assert.True(talkPreview.IsAvailable);
        Assert.Equal("target.action.talk", talkPreview.LabelKey);
        var talk = session.InteractWithVillager(
            orin.Position,
            out var talkResult
        );
        Assert.True(talkResult.Succeeded);
        Assert.NotNull(talk);

        Assert.True(session.Inventory.Add(DataCatalog.StarbudPreserveId, 2));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudPreserveId
        ));
        var giftPreview = session.PreviewSelectedTarget(orin.Position);
        Assert.True(giftPreview.IsAvailable);
        Assert.Equal("target.action.gift", giftPreview.LabelKey);
        var gift = session.InteractWithVillager(
            orin.Position,
            out var giftResult
        );
        Assert.True(giftResult.Succeeded);
        Assert.NotNull(gift);
        Assert.Equal(GiftReaction.Loved, gift.GiftReaction);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarbudPreserveId)
        );

        session.SetPlayerLocation(
            VillageCatalog.TwilightEmporiumDoorCell.X * 16 + 8,
            VillageCatalog.TwilightEmporiumDoorCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.Null(session.InteractWithVillager(
            orin.Position,
            out var wrongScene
        ));
        Assert.False(wrongScene.Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarbudPreserveId)
        );
    }

    [Fact]
    public void ArchiveDoorDeskAndExitShareLocationAwareRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.MoonlitArchiveDoorCell;

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.False(session.TryEnterMoonlitArchive().Succeeded);

        session.Clock.Reset(
            1,
            VillageCatalog.MoonlitArchiveOpenMinute
        );
        Assert.True(session.PreviewSelectedTarget(door).IsAvailable);
        Assert.True(session.TryEnterMoonlitArchive().Succeeded);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(session.TryEnterMoonlitArchive().Succeeded);

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            20 * 16 + 8,
            19 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        var desk = session.PreviewSelectedTarget(
            VillageCatalog.MoonlitArchiveDeskCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.MoonlitArchiveExitCell
        );
        Assert.True(desk.IsAvailable);
        Assert.Equal("target.action.read_archive", desk.LabelKey);
        Assert.True(exit.IsAvailable);
        Assert.True(session.InspectMoonlitArchiveDesk().Succeeded);
        Assert.True(session.TryExitMoonlitArchive().Succeeded);
    }

    [Fact]
    public void WorkshopDoorWorkbenchAndExitShareLocationAwareHandRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.MoonstoneWorkshopDoorCell;

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal("target.status.workshop_closed", closed.LabelKey);
        Assert.False(session.TryEnterMoonstoneWorkshop().Succeeded);

        session.Clock.Reset(
            1,
            VillageCatalog.MoonstoneWorkshopOpenMinute
        );
        var open = session.PreviewSelectedTarget(door);
        Assert.True(open.IsAvailable);
        Assert.Equal("target.action.enter_workshop", open.LabelKey);
        Assert.True(session.TryEnterMoonstoneWorkshop().Succeeded);

        session.Inventory.Select(1);
        var wrongDoorTool = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongDoorTool.State);
        Assert.False(session.TryEnterMoonstoneWorkshop().Succeeded);

        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        var energy = session.Energy;
        var workbenchWithTool = session.PreviewSelectedTarget(
            VillageCatalog.MoonRuneWorkbenchCell
        );
        var exitWithTool = session.PreviewSelectedTarget(
            VillageCatalog.MoonstoneWorkshopExitCell
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            workbenchWithTool.State
        );
        Assert.Equal(TargetPreviewState.NeedsTool, exitWithTool.State);
        Assert.False(session.OpenConstructionPanel().Succeeded);
        Assert.False(session.TryExitMoonstoneWorkshop().Succeeded);
        Assert.Equal(energy, session.Energy);

        session.Inventory.Select(0);
        var workbench = session.PreviewSelectedTarget(
            VillageCatalog.MoonRuneWorkbenchCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.MoonstoneWorkshopExitCell
        );
        Assert.True(workbench.IsAvailable);
        Assert.Equal(
            "target.action.open_construction",
            workbench.LabelKey
        );
        Assert.True(exit.IsAvailable);
        Assert.True(session.OpenConstructionPanel().Succeeded);
        Assert.True(session.TryExitMoonstoneWorkshop().Succeeded);

        session.SetPlayerLocation(
            door.X * 16 + 8,
            (door.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Clock.Reset(
            1,
            VillageCatalog.MoonstoneWorkshopCloseMinute
        );
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(door).State
        );
    }

    [Fact]
    public void TeaHouseDoorCounterAndExitShareLocationAwareHandRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.StarweaverTeaHouseDoorCell;
        session.Clock.Reset(
            1,
            VillageCatalog.StarweaverTeaHouseOpenMinute - 1
        );

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal("target.status.tea_house_closed", closed.LabelKey);
        Assert.False(session.TryEnterStarweaverTeaHouse().Succeeded);

        session.Clock.Reset(
            1,
            VillageCatalog.StarweaverTeaHouseOpenMinute
        );
        var open = session.PreviewSelectedTarget(door);
        Assert.True(open.IsAvailable);
        Assert.Equal("target.action.enter_tea_house", open.LabelKey);
        Assert.True(session.TryEnterStarweaverTeaHouse().Succeeded);

        session.Inventory.Select(1);
        var energy = session.Energy;
        var coins = session.Coins;
        var wrongDoorTool = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongDoorTool.State);
        Assert.False(session.TryEnterStarweaverTeaHouse().Succeeded);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);

        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarweaverTeaHouse
        );
        var counterWithTool = session.PreviewSelectedTarget(
            VillageCatalog.StarwovenTeaCounterCell
        );
        var exitWithTool = session.PreviewSelectedTarget(
            VillageCatalog.StarweaverTeaHouseExitCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, counterWithTool.State);
        Assert.Equal(TargetPreviewState.NeedsTool, exitWithTool.State);
        Assert.False(session.InspectStarwovenTeaCounter().Succeeded);
        Assert.False(session.TryExitStarweaverTeaHouse().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);

        session.Inventory.Select(0);
        var counter = session.PreviewSelectedTarget(
            VillageCatalog.StarwovenTeaCounterCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.StarweaverTeaHouseExitCell
        );
        Assert.True(counter.IsAvailable);
        Assert.Equal(
            "target.action.inspect_tea_counter",
            counter.LabelKey
        );
        Assert.True(exit.IsAvailable);
        Assert.True(session.InspectStarwovenTeaCounter().Succeeded);
        Assert.True(session.TryExitStarweaverTeaHouse().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);

        session.SetPlayerLocation(
            door.X * 16 + 8,
            (door.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Clock.Reset(
            1,
            VillageCatalog.StarweaverTeaHouseCloseMinute
        );
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterStarweaverTeaHouse().Succeeded);
    }

    [Fact]
    public void EmporiumDoorManifestAndExitShareLocationAwareHandRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.TwilightEmporiumDoorCell;
        session.Clock.Reset(
            1,
            VillageCatalog.TwilightEmporiumOpenMinute - 1
        );

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal("target.status.emporium_closed", closed.LabelKey);
        Assert.False(session.TryEnterTwilightEmporium().Succeeded);

        session.Clock.Reset(
            1,
            VillageCatalog.TwilightEmporiumOpenMinute
        );
        var open = session.PreviewSelectedTarget(door);
        Assert.True(open.IsAvailable);
        Assert.Equal("target.action.enter_emporium", open.LabelKey);
        Assert.True(session.TryEnterTwilightEmporium().Succeeded);

        session.Inventory.Select(1);
        var energy = session.Energy;
        var coins = session.Coins;
        var inventory = session.Inventory.Capture()
            .Select(slot => (slot.ItemId, slot.Count))
            .ToArray();
        var wrongDoorTool = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongDoorTool.State);
        Assert.False(session.TryEnterTwilightEmporium().Succeeded);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);

        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        var manifestWithTool = session.PreviewSelectedTarget(
            VillageCatalog.TravelManifestCell
        );
        var exitWithTool = session.PreviewSelectedTarget(
            VillageCatalog.TwilightEmporiumExitCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, manifestWithTool.State);
        Assert.Equal(TargetPreviewState.NeedsTool, exitWithTool.State);
        Assert.False(session.InspectTravelManifest().Succeeded);
        Assert.False(session.TryExitTwilightEmporium().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.Inventory.Select(0);
        var manifest = session.PreviewSelectedTarget(
            VillageCatalog.TravelManifestCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.TwilightEmporiumExitCell
        );
        Assert.True(manifest.IsAvailable);
        Assert.Equal("target.action.inspect_manifest", manifest.LabelKey);
        Assert.True(exit.IsAvailable);
        var inspected = session.InspectTravelManifest();
        Assert.True(inspected.Succeeded);
        Assert.Equal("emporium.manifest.opened", inspected.MessageKey);
        Assert.True(session.TryExitTwilightEmporium().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.SetPlayerLocation(
            door.X * 16 + 8,
            (door.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Clock.Reset(
            1,
            VillageCatalog.TwilightEmporiumCloseMinute
        );
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterTwilightEmporium().Succeeded);

        session.Clock.Reset(
            CalendarSystem.DaysPerWeek,
            VillageCatalog.TwilightEmporiumOpenMinute
        );
        var restday = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, restday.State);
        Assert.Equal(
            "target.status.emporium_restday",
            restday.LabelKey
        );
        var restdayEntry = session.TryEnterTwilightEmporium();
        Assert.False(restdayEntry.Succeeded);
        Assert.Equal("notice.emporium_restday", restdayEntry.MessageKey);

        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        var restdayManifest = session.PreviewSelectedTarget(
            VillageCatalog.TravelManifestCell
        );
        Assert.Equal(TargetPreviewState.Blocked, restdayManifest.State);
        Assert.Equal(
            "target.status.emporium_restday",
            restdayManifest.LabelKey
        );
        var inspectOnRestday = session.InspectTravelManifest();
        Assert.False(inspectOnRestday.Succeeded);
        Assert.Equal(
            "notice.emporium_restday",
            inspectOnRestday.MessageKey
        );
    }

    [Fact]
    public void NemiWorksInsideStarlightPostWhileConditionsKeepPriority()
    {
        var beforeWork = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            VillageCatalog.StarlightPostOpenMinute + 60
        );
        var atWork = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            9 * 60
        );
        var afterWork = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            13 * 60,
            DataCatalog.ClearWeatherId
        );
        var rainyAfternoon = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            1,
            13 * 60,
            DataCatalog.RainWeatherId
        );
        var lanternrest = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            CalendarSystem.DaysPerWeek,
            9 * 60
        );

        Assert.NotNull(beforeWork);
        Assert.Equal(PlayerLocationIds.World, beforeWork.LocationId);
        Assert.NotNull(atWork);
        Assert.Equal(PlayerLocationIds.StarlightPost, atWork.LocationId);
        Assert.Equal(
            "village.npc.nemi.starlight_post",
            atWork.DialogueKey
        );
        Assert.True(NpcNavigationMap.IsNpcPassable(
            atWork.LocationId,
            atWork.Position
        ));
        Assert.NotEqual(VillageCatalog.StarlightPostExitCell, atWork.Position);
        Assert.NotNull(afterWork);
        Assert.Equal(PlayerLocationIds.World, afterWork.LocationId);
        Assert.NotNull(rainyAfternoon);
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            rainyAfternoon.LocationId
        );
        Assert.NotNull(lanternrest);
        Assert.Equal(PlayerLocationIds.World, lanternrest.LocationId);
    }

    [Fact]
    public void StarlightPostDoorCounterAndExitShareReadOnlyHandRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.StarlightPostDoorCell;
        session.Clock.Reset(
            1,
            VillageCatalog.StarlightPostOpenMinute - 1
        );

        Assert.False(VillageCatalog.IsStarlightPostOpen(6 * 60 + 59));
        Assert.True(VillageCatalog.IsStarlightPostOpen(7 * 60));
        Assert.True(VillageCatalog.IsStarlightPostOpen(18 * 60 + 59));
        Assert.False(VillageCatalog.IsStarlightPostOpen(19 * 60));

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal(
            "target.status.starlight_post_closed",
            closed.LabelKey
        );
        Assert.False(session.TryEnterStarlightPost().Succeeded);

        session.Clock.Reset(1, VillageCatalog.StarlightPostOpenMinute);
        var open = session.PreviewSelectedTarget(door);
        Assert.True(open.IsAvailable);
        Assert.Equal("target.action.enter_starlight_post", open.LabelKey);
        Assert.True(session.TryEnterStarlightPost().Succeeded);

        session.Inventory.Select(1);
        var energy = session.Energy;
        var coins = session.Coins;
        var inventory = session.Inventory.Capture()
            .Select(slot => (slot.ItemId, slot.Count))
            .ToArray();
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterStarlightPost().Succeeded);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);

        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarlightPost
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(
                VillageCatalog.RouteSortingCounterCell
            ).State
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(
                VillageCatalog.StarlightPostExitCell
            ).State
        );
        Assert.False(session.InspectRouteSortingCounter().Succeeded);
        Assert.False(session.TryExitStarlightPost().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.Inventory.Select(0);
        var counter = session.PreviewSelectedTarget(
            VillageCatalog.RouteSortingCounterCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.StarlightPostExitCell
        );
        Assert.True(counter.IsAvailable);
        Assert.Equal(
            "target.action.inspect_sorting_counter",
            counter.LabelKey
        );
        Assert.True(exit.IsAvailable);
        var inspected = session.InspectRouteSortingCounter();
        Assert.True(inspected.Succeeded);
        Assert.Equal("starlight_post.counter.dialogue", inspected.MessageKey);
        Assert.True(session.TryExitStarlightPost().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.SetPlayerLocation(
            door.X * 16 + 8,
            (door.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Clock.Reset(1, VillageCatalog.StarlightPostCloseMinute);
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterStarlightPost().Succeeded);
    }

    [Fact]
    public void StarlightPostUsesSharedCollisionAndSafeArrivalRules()
    {
        var exteriorArrival = NpcNavigationMap.SafeArrivalCell(
            PlayerLocationIds.StarlightPost,
            PlayerLocationIds.World
        );
        var interiorArrival = NpcNavigationMap.SafeArrivalCell(
            PlayerLocationIds.World,
            PlayerLocationIds.StarlightPost
        );

        Assert.Equal(new GridPosition(77, 42), exteriorArrival);
        Assert.Equal(new GridPosition(19, 18), interiorArrival);
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.StarlightPostDoorCell
        ));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.StarlightPostDoorCell
        ));
        Assert.True(NpcNavigationMap.IsCriticalEntranceCell(
            PlayerLocationIds.World,
            VillageCatalog.StarlightPostDoorCell
        ));
        Assert.True(NpcNavigationMap.IsCriticalEntranceCell(
            PlayerLocationIds.StarlightPost,
            VillageCatalog.StarlightPostExitCell
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarlightPost,
            new GridPosition(20, 10)
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarlightPost,
            new GridPosition(20, 4)
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarlightPost,
            new GridPosition(10, 4)
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarlightPost,
            new GridPosition(30, 4)
        ));
        Assert.True(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarlightPost,
            new GridPosition(13, 12)
        ));
        Assert.True(NpcNavigationMap.IsNpcPassable(
            PlayerLocationIds.World,
            exteriorArrival!.Value
        ));
        Assert.True(NpcNavigationMap.IsNpcPassable(
            PlayerLocationIds.StarlightPost,
            interiorArrival!.Value
        ));
        Assert.NotEmpty(NpcPathfinder.FindPath(
            PlayerLocationIds.World,
            exteriorArrival.Value,
            new GridPosition(84, 43)
        ));
        Assert.NotEmpty(NpcPathfinder.FindPath(
            PlayerLocationIds.StarlightPost,
            interiorArrival.Value,
            new GridPosition(13, 12)
        ));
    }

    [Fact]
    public void KaelWorksInsideStarfallWatchWhileConditionsKeepPriority()
    {
        var beforeWork = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            1,
            VillageCatalog.StarfallWatchOpenMinute + 60
        );
        var atWork = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            1,
            9 * 60
        );
        var afterWork = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            1,
            13 * 60,
            DataCatalog.ClearWeatherId
        );
        var stardustAfternoon = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            1,
            13 * 60,
            DataCatalog.StardustWindWeatherId
        );
        var lanternrest = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            CalendarSystem.DaysPerWeek,
            9 * 60
        );

        Assert.NotNull(beforeWork);
        Assert.Equal(PlayerLocationIds.World, beforeWork.LocationId);
        Assert.NotNull(atWork);
        Assert.Equal(PlayerLocationIds.StarfallWatch, atWork.LocationId);
        Assert.Equal(
            "village.npc.kael.starfall_watch",
            atWork.DialogueKey
        );
        Assert.True(NpcNavigationMap.IsNpcPassable(
            atWork.LocationId,
            atWork.Position
        ));
        Assert.NotEqual(VillageCatalog.StarfallWatchExitCell, atWork.Position);
        Assert.NotNull(afterWork);
        Assert.Equal(PlayerLocationIds.World, afterWork.LocationId);
        Assert.NotNull(stardustAfternoon);
        Assert.Equal(PlayerLocationIds.World, stardustAfternoon.LocationId);
        Assert.Equal(
            "village.npc.kael.weather_stardust",
            stardustAfternoon.DialogueKey
        );
        Assert.NotNull(lanternrest);
        Assert.Equal(PlayerLocationIds.World, lanternrest.LocationId);
    }

    [Fact]
    public void StarfallWatchDoorTableAndExitShareReadOnlyHandRules()
    {
        var session = new GameSession();
        session.NewGame();
        var door = VillageCatalog.StarfallWatchDoorCell;
        session.Clock.Reset(
            1,
            VillageCatalog.StarfallWatchCloseMinute
        );

        Assert.False(VillageCatalog.IsStarfallWatchOpen(5 * 60 + 59));
        Assert.True(VillageCatalog.IsStarfallWatchOpen(6 * 60));
        Assert.True(VillageCatalog.IsStarfallWatchOpen(19 * 60 + 59));
        Assert.False(VillageCatalog.IsStarfallWatchOpen(20 * 60));

        var closed = session.PreviewSelectedTarget(door);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal(
            "target.status.starfall_watch_closed",
            closed.LabelKey
        );
        Assert.False(session.TryEnterStarfallWatch().Succeeded);

        session.Clock.Reset(1, VillageCatalog.StarfallWatchOpenMinute);
        var open = session.PreviewSelectedTarget(door);
        Assert.True(open.IsAvailable);
        Assert.Equal("target.action.enter_starfall_watch", open.LabelKey);
        Assert.True(session.TryEnterStarfallWatch().Succeeded);

        session.Inventory.Select(1);
        var energy = session.Energy;
        var coins = session.Coins;
        var inventory = session.Inventory.Capture()
            .Select(slot => (slot.ItemId, slot.Count))
            .ToArray();
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterStarfallWatch().Succeeded);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);

        session.SetPlayerLocation(
            20 * 16 + 8,
            14 * 16 + 8,
            PlayerLocationIds.StarfallWatch
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(
                VillageCatalog.SealRouteTableCell
            ).State
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(
                VillageCatalog.StarfallWatchExitCell
            ).State
        );
        Assert.False(session.InspectSealRouteTable().Succeeded);
        Assert.False(session.TryExitStarfallWatch().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.Inventory.Select(0);
        var table = session.PreviewSelectedTarget(
            VillageCatalog.SealRouteTableCell
        );
        var exit = session.PreviewSelectedTarget(
            VillageCatalog.StarfallWatchExitCell
        );
        Assert.True(table.IsAvailable);
        Assert.Equal(
            "target.action.inspect_seal_route_table",
            table.LabelKey
        );
        Assert.True(exit.IsAvailable);
        var inspected = session.InspectSealRouteTable();
        Assert.True(inspected.Succeeded);
        Assert.Equal("starfall_watch.table.dialogue", inspected.MessageKey);
        Assert.True(session.TryExitStarfallWatch().Succeeded);
        Assert.Equal(energy, session.Energy);
        Assert.Equal(coins, session.Coins);
        Assert.Equal(
            inventory,
            session.Inventory.Capture()
                .Select(slot => (slot.ItemId, slot.Count))
                .ToArray()
        );

        session.SetPlayerLocation(
            door.X * 16 + 8,
            (door.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Clock.Reset(1, VillageCatalog.StarfallWatchCloseMinute);
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(door).State
        );
        Assert.False(session.TryEnterStarfallWatch().Succeeded);
    }

    [Fact]
    public void StarfallWatchUsesSharedCollisionAndSafeArrivalRules()
    {
        var exteriorArrival = NpcNavigationMap.SafeArrivalCell(
            PlayerLocationIds.StarfallWatch,
            PlayerLocationIds.World
        );
        var interiorArrival = NpcNavigationMap.SafeArrivalCell(
            PlayerLocationIds.World,
            PlayerLocationIds.StarfallWatch
        );

        Assert.Equal(new GridPosition(77, 55), exteriorArrival);
        Assert.Equal(new GridPosition(19, 18), interiorArrival);
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.StarfallWatchDoorCell
        ));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.StarfallWatchDoorCell
        ));
        Assert.True(WorldDefinition.IsBlocked(new GridPosition(77, 53)));
        Assert.True(NpcNavigationMap.IsCriticalEntranceCell(
            PlayerLocationIds.World,
            VillageCatalog.StarfallWatchDoorCell
        ));
        Assert.True(NpcNavigationMap.IsCriticalEntranceCell(
            PlayerLocationIds.StarfallWatch,
            VillageCatalog.StarfallWatchExitCell
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarfallWatch,
            new GridPosition(20, 8)
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarfallWatch,
            new GridPosition(5, 7)
        ));
        Assert.False(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarfallWatch,
            new GridPosition(34, 7)
        ));
        Assert.True(NpcNavigationMap.IsWalkableGeometry(
            PlayerLocationIds.StarfallWatch,
            new GridPosition(13, 12)
        ));
        Assert.True(NpcNavigationMap.IsNpcPassable(
            PlayerLocationIds.World,
            exteriorArrival!.Value
        ));
        Assert.True(NpcNavigationMap.IsNpcPassable(
            PlayerLocationIds.StarfallWatch,
            interiorArrival!.Value
        ));
        Assert.NotEmpty(NpcPathfinder.FindPath(
            PlayerLocationIds.World,
            exteriorArrival.Value,
            new GridPosition(84, 54)
        ));
        Assert.NotEmpty(NpcPathfinder.FindPath(
            PlayerLocationIds.StarfallWatch,
            interiorArrival.Value,
            new GridPosition(13, 12)
        ));
    }

    [Fact]
    public void VillageLandmarksHaveStableIdsAndPassableEntrances()
    {
        Assert.Equal(11, VillageCatalog.Landmarks.Count);
        Assert.Equal(
            11,
            VillageCatalog.Landmarks
                .Select(landmark => landmark.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.Equal(
            Enumerable.Range(0, 11),
            VillageCatalog.Landmarks
                .Select(landmark => landmark.AtlasIndex)
                .Order()
        );
        Assert.True(WorldDefinition.IsBlocked(new GridPosition(86, 36)));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.VillageGateCell
        ));
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.VillageGateCell
        ));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.TwilightEmporiumDoorCell
        ));
        Assert.False(WorldDefinition.IsBlocked(new GridPosition(
            VillageCatalog.TwilightEmporiumDoorCell.X,
            VillageCatalog.TwilightEmporiumDoorCell.Y + 1
        )));
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.TwilightEmporiumDoorCell
        ));
        Assert.True(WorldDefinition.IsPath(new GridPosition(
            VillageCatalog.TwilightEmporiumDoorCell.X,
            VillageCatalog.TwilightEmporiumDoorCell.Y + 1
        )));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.StarlightPostDoorCell
        ));
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.StarlightPostDoorCell
        ));
        Assert.True(WorldDefinition.IsPath(new GridPosition(
            VillageCatalog.StarlightPostDoorCell.X,
            VillageCatalog.StarlightPostDoorCell.Y + 1
        )));
        Assert.False(WorldDefinition.IsBlocked(
            VillageCatalog.StarfallWatchDoorCell
        ));
        Assert.True(WorldDefinition.IsPath(
            VillageCatalog.StarfallWatchDoorCell
        ));
        Assert.True(WorldDefinition.IsPath(new GridPosition(84, 54)));
        Assert.False(WorldDefinition.IsBlocked(new GridPosition(106, 58)));
        Assert.True(WorldDefinition.IsBlocked(new GridPosition(107, 58)));
        Assert.Equal(
            WorldBiome.LumenVillage,
            WorldDefinition.GetBiome(new GridPosition(97, 48))
        );
    }

    [Fact]
    public void VillageDecorationsAreDeterministicAndDoNotBlockRoutes()
    {
        var cells = Enumerable
            .Range(
                VillageCatalog.VillageBounds.MinY,
                VillageCatalog.VillageBounds.MaxY -
                    VillageCatalog.VillageBounds.MinY + 1
            )
            .SelectMany(y => Enumerable.Range(
                    VillageCatalog.VillageBounds.MinX,
                    VillageCatalog.VillageBounds.MaxX -
                        VillageCatalog.VillageBounds.MinX + 1
                )
                .Select(x => new GridPosition(x, y)))
            .ToList();
        var decorations = cells
            .Select(cell => (
                Cell: cell,
                AtlasIndex: WorldDefinition.PropAtlasIndex(cell)
            ))
            .Where(value => value.AtlasIndex >= 0)
            .ToList();

        Assert.True(decorations.Count >= 20);
        Assert.Equal(
            decorations,
            cells
                .Select(cell => (
                    Cell: cell,
                    AtlasIndex: WorldDefinition.PropAtlasIndex(cell)
                ))
                .Where(value => value.AtlasIndex >= 0)
                .ToList()
        );
        Assert.All(decorations, value =>
        {
            Assert.Contains(value.AtlasIndex, new[] { 4, 5, 13 });
            Assert.False(WorldDefinition.IsBlocked(value.Cell));
        });
        Assert.All(
            cells.Where(cell =>
                WorldDefinition.IsPath(cell) ||
                VillageCatalog.IsBlocked(cell)
            ),
            cell => Assert.Equal(
                -1,
                WorldDefinition.PropAtlasIndex(cell)
            )
        );
    }
}

public sealed class CharacterEventSystemTests
{
    [Fact]
    public void FirstMeetingAndThresholdCrossingDoNotTriggerEarly()
    {
        var firstMeeting = PrepareLioraSession(
            day: 2,
            relationshipPoints: 25,
            metLiora: false
        );

        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.LioraPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.liora.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareLioraSession(
            day: 2,
            relationshipPoints: 23,
            metLiora: true,
            lastTalkDay: 0
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.LioraPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
    }

    [Fact]
    public void FadedReturnRouteRequiresEveryConditionAndCompletesOnce()
    {
        var prepared = PrepareLioraSession(
            day: 2,
            relationshipPoints: 25,
            metLiora: true
        );

        var conversation = prepared.Session.InteractWithVillager(
            prepared.LioraPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.LioraFadedReturnRouteId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.LioraFadedReturnRouteId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.LioraFadedReturnRouteId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            2,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.LioraFadedReturnRouteId
            )
        );
        var saved = Assert.Single(
            prepared.Session.Capture().CharacterEvents.Entries
        );
        Assert.Equal(
            CharacterEventCatalog.LioraFadedReturnRouteId,
            saved.EventId
        );
        Assert.Equal(2, saved.CompletedDay);
        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            2,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.LioraFadedReturnRouteId
            )
        );

        var repeat = prepared.Session.InteractWithVillager(
            prepared.LioraPosition,
            out var repeatResult
        );
        Assert.True(repeatResult.Succeeded);
        Assert.NotNull(repeat);
        Assert.Null(repeat.CharacterEvent);
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.LioraFadedReturnRouteId
        ).Succeeded);
    }

    [Fact]
    public void RememberedWayHomeRequiresAnEarlierCompletionDay()
    {
        var sameDay = PrepareLioraSession(
            day: 2,
            relationshipPoints: 60,
            metLiora: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 2
                    }
                ]
            }
        );

        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.LioraPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var laterDay = PrepareLioraSession(
            day: 3,
            relationshipPoints: 60,
            metLiora: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 2
                    }
                ]
            }
        );

        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.LioraPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.LioraRememberedWayHomeId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.LioraRememberedWayHomeId
        ).Succeeded);

        laterDay.Session.Clock.Reset(4, 10 * 60);
        var completedTalk = laterDay.Session.InteractWithVillager(
            laterDay.LioraPosition,
            out var completedResult
        );
        Assert.True(completedResult.Succeeded);
        Assert.NotNull(completedTalk);
        Assert.Null(completedTalk.CharacterEvent);
    }

    [Fact]
    public void GiftsWrongToolsAndWrongScenesNeverProgressTheEvent()
    {
        var wrongTool = PrepareLioraSession(
            day: 2,
            relationshipPoints: 25,
            metLiora: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.LioraPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);
        Assert.False(wrongTool.Session.CompleteCharacterEvent(
            CharacterEventCatalog.LioraFadedReturnRouteId
        ).Succeeded);

        var gift = PrepareLioraSession(
            day: 2,
            relationshipPoints: 25,
            metLiora: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.MoonrootId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.MoonrootId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.LioraPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareLioraSession(
            day: 2,
            relationshipPoints: 25,
            metLiora: true
        );
        wrongScene.Session.SetPlayerLocation(
            VillageCatalog.MoonlitArchiveDoorCell.X * 16 + 8,
            VillageCatalog.MoonlitArchiveDoorCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.LioraPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void TaviFirstMeetingThresholdCrossingAndPreviewDoNotTriggerEarly()
    {
        var firstMeeting = PrepareTaviSession(
            day: 2,
            relationshipPoints: 25,
            metTavi: false
        );

        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.TaviPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.tavi.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareTaviSession(
            day: 2,
            relationshipPoints: 23,
            metTavi: true,
            lastTalkDay: 0
        );
        var preview = thresholdCrossing.Session.PreviewSelectedTarget(
            thresholdCrossing.TaviPosition
        );

        Assert.True(preview.IsAvailable);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.TaviPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
    }

    [Fact]
    public void TaviCrackedMoonRuneCompletesOnlyAfterDialogueCallback()
    {
        var prepared = PrepareTaviSession(
            day: 2,
            relationshipPoints: 25,
            metTavi: true
        );

        var conversation = prepared.Session.InteractWithVillager(
            prepared.TaviPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.TaviCrackedMoonRuneId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.Equal(
            CharacterEventCatalog.TaviCrackedMoonRuneId,
            prepared.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.TaviCrackedMoonRuneId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.TaviCrackedMoonRuneId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            2,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.TaviCrackedMoonRuneId
            )
        );
        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            2,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.TaviCrackedMoonRuneId
            )
        );
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.TaviCrackedMoonRuneId
        ).Succeeded);
    }

    [Fact]
    public void TaviMendedLightRequiresAnEarlierCompletionDay()
    {
        var sameDay = PrepareTaviSession(
            day: 2,
            relationshipPoints: 60,
            metTavi: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 2
                    }
                ]
            }
        );

        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.TaviPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var laterDay = PrepareTaviSession(
            day: 3,
            relationshipPoints: 60,
            metTavi: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 2
                    }
                ]
            }
        );

        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.TaviPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.TaviMendedLightId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.TaviMendedLightId
        ).Succeeded);
    }

    [Fact]
    public void TaviGiftsWrongToolsAndWrongScenesNeverProgressEvents()
    {
        var wrongTool = PrepareTaviSession(
            day: 2,
            relationshipPoints: 25,
            metTavi: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.TaviPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);

        var gift = PrepareTaviSession(
            day: 2,
            relationshipPoints: 25,
            metTavi: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.LumenwoodId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.LumenwoodId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.TaviPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareTaviSession(
            day: 2,
            relationshipPoints: 25,
            metTavi: true
        );
        wrongScene.Session.SetPlayerLocation(
            VillageCatalog.MoonstoneWorkshopDoorCell.X * 16 + 8,
            VillageCatalog.MoonstoneWorkshopDoorCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.TaviPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void NemiFirstMeetingThresholdCrossingAndPreviewDoNotTriggerEarly()
    {
        var firstMeeting = PrepareNemiSession(
            day: 15,
            relationshipPoints: 25,
            metNemi: false
        );
        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.NemiPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.nemi.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareNemiSession(
            day: 15,
            relationshipPoints: 23,
            metNemi: true,
            lastTalkDay: 0
        );
        var preview = thresholdCrossing.Session.PreviewSelectedTarget(
            thresholdCrossing.NemiPosition
        );

        Assert.True(preview.IsAvailable);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.NemiPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
    }

    [Fact]
    public void NemiUndeliverableLetterCompletesOnlyAfterDialogueCallback()
    {
        var prepared = PrepareNemiSession(
            day: 15,
            relationshipPoints: 25,
            metNemi: true
        );
        var conversation = prepared.Session.InteractWithVillager(
            prepared.NemiPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.NemiUndeliverableLetterId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.Equal(
            CharacterEventCatalog.NemiUndeliverableLetterId,
            prepared.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.NemiUndeliverableLetterId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.NemiUndeliverableLetterId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            15,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.NemiUndeliverableLetterId
            )
        );
        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            15,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.NemiUndeliverableLetterId
            )
        );
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.NemiUndeliverableLetterId
        ).Succeeded);
    }

    [Fact]
    public void NemiStarChartRouteRequiresAnEarlierCompletionDay()
    {
        var sameDay = PrepareNemiSession(
            day: 15,
            relationshipPoints: 60,
            metNemi: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.NemiUndeliverableLetterId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.NemiPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var laterDay = PrepareNemiSession(
            day: 17,
            relationshipPoints: 60,
            metNemi: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.NemiUndeliverableLetterId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.NemiPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.NemiStarChartRouteId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.Equal(3, laterTalk.CharacterEvent.DialogueKeys.Count);
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.NemiStarChartRouteId
        ).Succeeded);
    }

    [Fact]
    public void NemiGiftsWrongToolsAndWrongScenesNeverProgressEvents()
    {
        var wrongTool = PrepareNemiSession(
            day: 15,
            relationshipPoints: 25,
            metNemi: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.NemiPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);

        var gift = PrepareNemiSession(
            day: 15,
            relationshipPoints: 25,
            metNemi: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.StarbudId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.NemiPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareNemiSession(
            day: 15,
            relationshipPoints: 25,
            metNemi: true
        );
        wrongScene.Session.SetPlayerLocation(
            8,
            8,
            PlayerLocationIds.Cottage
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.NemiPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void KaelFirstMeetingThresholdCrossingAndPreviewDoNotTriggerEarly()
    {
        var firstMeeting = PrepareKaelSession(
            day: 15,
            relationshipPoints: 25,
            metKael: false
        );
        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.KaelPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.kael.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareKaelSession(
            day: 15,
            relationshipPoints: 23,
            metKael: true,
            lastTalkDay: 0
        );
        var preview = thresholdCrossing.Session.PreviewSelectedTarget(
            thresholdCrossing.KaelPosition
        );

        Assert.True(preview.IsAvailable);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.KaelPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );

        var secondThresholdCrossing = PrepareKaelSession(
            day: 17,
            relationshipPoints: 58,
            metKael: true,
            lastTalkDay: 0,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.KaelBrokenBlueRuneId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var secondTalk =
            secondThresholdCrossing.Session.InteractWithVillager(
                secondThresholdCrossing.KaelPosition,
                out var secondResult
            );

        Assert.True(secondResult.Succeeded);
        Assert.NotNull(secondTalk);
        Assert.Equal(60, secondTalk.RelationshipPoints);
        Assert.Null(secondTalk.CharacterEvent);
        Assert.Null(
            secondThresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(secondThresholdCrossing.Session.CharacterEvents
            .IsCompleted(CharacterEventCatalog.KaelSafeReturnRouteId));
    }

    [Fact]
    public void KaelBrokenBlueRuneCompletesOnlyAfterDialogueCallback()
    {
        var prepared = PrepareKaelSession(
            day: 15,
            relationshipPoints: 25,
            metKael: true
        );
        var conversation = prepared.Session.InteractWithVillager(
            prepared.KaelPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.KaelBrokenBlueRuneId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.Equal(
            CharacterEventCatalog.KaelBrokenBlueRuneId,
            prepared.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            15,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.KaelBrokenBlueRuneId
            )
        );
        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            15,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.KaelBrokenBlueRuneId
            )
        );
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        ).Succeeded);
    }

    [Fact]
    public void KaelSafeReturnRouteRequiresThresholdAndEarlierCompletionDay()
    {
        var sameDay = PrepareKaelSession(
            day: 15,
            relationshipPoints: 60,
            metKael: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.KaelBrokenBlueRuneId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.KaelPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var belowThreshold = PrepareKaelSession(
            day: 17,
            relationshipPoints: 59,
            metKael: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.KaelBrokenBlueRuneId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var belowThresholdTalk =
            belowThreshold.Session.InteractWithVillager(
                belowThreshold.KaelPosition,
                out var belowThresholdResult
            );

        Assert.True(belowThresholdResult.Succeeded);
        Assert.NotNull(belowThresholdTalk);
        Assert.Null(belowThresholdTalk.CharacterEvent);

        var laterDay = PrepareKaelSession(
            day: 17,
            relationshipPoints: 60,
            metKael: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.KaelBrokenBlueRuneId,
                        CompletedDay = 15
                    }
                ]
            }
        );
        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.KaelPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.KaelSafeReturnRouteId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.Equal(3, laterTalk.CharacterEvent.DialogueKeys.Count);
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.KaelSafeReturnRouteId
        ).Succeeded);
    }

    [Fact]
    public void KaelGiftsWrongToolsAndWrongScenesNeverProgressEvents()
    {
        var wrongTool = PrepareKaelSession(
            day: 15,
            relationshipPoints: 25,
            metKael: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.KaelPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);
        Assert.False(wrongTool.Session.CompleteCharacterEvent(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        ).Succeeded);

        var gift = PrepareKaelSession(
            day: 15,
            relationshipPoints: 25,
            metKael: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.CrystalShardId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.CrystalShardId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.KaelPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareKaelSession(
            day: 15,
            relationshipPoints: 25,
            metKael: true
        );
        wrongScene.Session.SetPlayerLocation(
            8,
            8,
            PlayerLocationIds.Cottage
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.KaelPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongScene.Session.CharacterEvents.Capture().Entries);
    }

    [Fact]
    public void SelaFirstMeetingThresholdCrossingAndPreviewDoNotTriggerEarly()
    {
        var firstMeeting = PrepareSelaSession(
            day: 15,
            relationshipPoints: 25,
            metSela: false
        );
        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.SelaPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.sela.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareSelaSession(
            day: 15,
            relationshipPoints: 23,
            metSela: true,
            lastTalkDay: 0
        );
        var preview = thresholdCrossing.Session.PreviewSelectedTarget(
            thresholdCrossing.SelaPosition
        );

        Assert.True(preview.IsAvailable);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.SelaPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );
    }

    [Fact]
    public void SelaTemperedStarlightCompletesOnlyAfterDialogueCallback()
    {
        var prepared = PrepareSelaSession(
            day: 15,
            relationshipPoints: 25,
            metSela: true
        );
        var conversation = prepared.Session.InteractWithVillager(
            prepared.SelaPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.SelaTemperedStarlightId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.Equal(
            CharacterEventCatalog.SelaTemperedStarlightId,
            prepared.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.SelaTemperedStarlightId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.SelaTemperedStarlightId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            15,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.SelaTemperedStarlightId
            )
        );
        var saved = Assert.Single(
            prepared.Session.Capture().CharacterEvents.Entries
        );
        Assert.Equal(
            CharacterEventCatalog.SelaTemperedStarlightId,
            saved.EventId
        );
        Assert.Equal(15, saved.CompletedDay);

        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            15,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.SelaTemperedStarlightId
            )
        );
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.SelaTemperedStarlightId
        ).Succeeded);
    }

    [Fact]
    public void SelaSharedForgeRhythmRequiresThresholdAndEarlierCompletionDay()
    {
        var sameDay = PrepareSelaSession(
            day: 15,
            relationshipPoints: 60,
            metSela: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        15
                    )
                ]
            }
        );
        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.SelaPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var belowThreshold = PrepareSelaSession(
            day: 17,
            relationshipPoints: 59,
            metSela: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        15
                    )
                ]
            }
        );
        var belowThresholdTalk =
            belowThreshold.Session.InteractWithVillager(
                belowThreshold.SelaPosition,
                out var belowThresholdResult
            );

        Assert.True(belowThresholdResult.Succeeded);
        Assert.NotNull(belowThresholdTalk);
        Assert.Null(belowThresholdTalk.CharacterEvent);

        var laterDay = PrepareSelaSession(
            day: 17,
            relationshipPoints: 60,
            metSela: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        15
                    )
                ]
            }
        );
        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.SelaPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.SelaSharedForgeRhythmId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.Equal(3, laterTalk.CharacterEvent.DialogueKeys.Count);
        Assert.False(laterDay.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.SelaSharedForgeRhythmId
        ));
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.SelaSharedForgeRhythmId
        ).Succeeded);
    }

    [Fact]
    public void SelaInvalidInteractionsNeverProgressEvents()
    {
        var wrongTool = PrepareSelaSession(
            day: 15,
            relationshipPoints: 25,
            metSela: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.SelaPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongTool.Session.CharacterEvents.Capture().Entries);
        Assert.False(wrongTool.Session.CompleteCharacterEvent(
            CharacterEventCatalog.SelaTemperedStarlightId
        ).Succeeded);

        var gift = PrepareSelaSession(
            day: 15,
            relationshipPoints: 25,
            metSela: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.CrystalShardId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.CrystalShardId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.SelaPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareSelaSession(
            day: 15,
            relationshipPoints: 25,
            metSela: true
        );
        wrongScene.Session.SetPlayerLocation(
            8,
            8,
            PlayerLocationIds.Cottage
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.SelaPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongScene.Session.CharacterEvents.Capture().Entries);
    }

    [Fact]
    public void OrinFirstMeetingThresholdCrossingAndPreviewDoNotTriggerEarly()
    {
        var firstMeeting = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: false
        );
        var introduction = firstMeeting.Session.InteractWithVillager(
            firstMeeting.OrinPosition,
            out var introductionResult
        );

        Assert.True(introductionResult.Succeeded);
        Assert.NotNull(introduction);
        Assert.True(introduction.FirstMeeting);
        Assert.Equal(
            "village.npc.orin.intro",
            introduction.DialogueKey
        );
        Assert.Null(introduction.CharacterEvent);
        Assert.Null(firstMeeting.Session.CharacterEvents.ActiveEventId);

        var thresholdCrossing = PrepareOrinSession(
            day: 15,
            relationshipPoints: 23,
            metOrin: true,
            lastTalkDay: 0
        );
        var preview = thresholdCrossing.Session.PreviewSelectedTarget(
            thresholdCrossing.OrinPosition
        );

        Assert.True(preview.IsAvailable);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var normalTalk = thresholdCrossing.Session.InteractWithVillager(
            thresholdCrossing.OrinPosition,
            out var talkResult
        );

        Assert.True(talkResult.Succeeded);
        Assert.NotNull(normalTalk);
        Assert.Equal(25, normalTalk.RelationshipPoints);
        Assert.Null(normalTalk.CharacterEvent);
        Assert.Null(
            thresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Empty(
            thresholdCrossing.Session.Capture().CharacterEvents.Entries
        );

        var secondThresholdCrossing = PrepareOrinSession(
            day: 17,
            relationshipPoints: 58,
            metOrin: true,
            lastTalkDay: 0,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        15
                    )
                ]
            }
        );
        var secondNormalTalk =
            secondThresholdCrossing.Session.InteractWithVillager(
                secondThresholdCrossing.OrinPosition,
                out var secondTalkResult
            );

        Assert.True(secondTalkResult.Succeeded);
        Assert.NotNull(secondNormalTalk);
        Assert.Equal(60, secondNormalTalk.RelationshipPoints);
        Assert.Null(secondNormalTalk.CharacterEvent);
        Assert.Null(
            secondThresholdCrossing.Session.CharacterEvents.ActiveEventId
        );
        Assert.Single(
            secondThresholdCrossing.Session.Capture().CharacterEvents.Entries
        );
    }

    [Fact]
    public void OrinUnpricedWaybillCompletesOnlyAfterDialogueCallback()
    {
        var prepared = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: true
        );
        var conversation = prepared.Session.InteractWithVillager(
            prepared.OrinPosition,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.NotNull(conversation.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.OrinUnpricedWaybillId,
            conversation.CharacterEvent.EventId
        );
        Assert.Equal(3, conversation.CharacterEvent.DialogueKeys.Count);
        Assert.Equal(
            CharacterEventCatalog.OrinUnpricedWaybillId,
            prepared.Session.CharacterEvents.ActiveEventId
        );
        Assert.False(prepared.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.OrinUnpricedWaybillId
        ));
        Assert.Empty(prepared.Session.Capture().CharacterEvents.Entries);

        var completed = prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.OrinUnpricedWaybillId
        );

        Assert.True(completed.Succeeded);
        Assert.Equal(
            15,
            prepared.Session.CharacterEvents.CompletedDay(
                CharacterEventCatalog.OrinUnpricedWaybillId
            )
        );
        var saved = Assert.Single(
            prepared.Session.Capture().CharacterEvents.Entries
        );
        Assert.Equal(
            CharacterEventCatalog.OrinUnpricedWaybillId,
            saved.EventId
        );
        Assert.Equal(15, saved.CompletedDay);

        var restored = new GameSession();
        restored.Restore(prepared.Session.Capture());
        Assert.Equal(
            15,
            restored.CharacterEvents.CompletedDay(
                CharacterEventCatalog.OrinUnpricedWaybillId
            )
        );
        Assert.False(prepared.Session.CompleteCharacterEvent(
            CharacterEventCatalog.OrinUnpricedWaybillId
        ).Succeeded);
    }

    [Fact]
    public void OrinSharedLanternRouteRequiresThresholdAndEarlierCompletionDay()
    {
        var sameDay = PrepareOrinSession(
            day: 15,
            relationshipPoints: 60,
            metOrin: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        15
                    )
                ]
            }
        );
        var sameDayTalk = sameDay.Session.InteractWithVillager(
            sameDay.OrinPosition,
            out var sameDayResult
        );

        Assert.True(sameDayResult.Succeeded);
        Assert.NotNull(sameDayTalk);
        Assert.Null(sameDayTalk.CharacterEvent);

        var belowThreshold = PrepareOrinSession(
            day: 17,
            relationshipPoints: 59,
            metOrin: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        15
                    )
                ]
            }
        );
        var belowThresholdTalk =
            belowThreshold.Session.InteractWithVillager(
                belowThreshold.OrinPosition,
                out var belowThresholdResult
            );

        Assert.True(belowThresholdResult.Succeeded);
        Assert.NotNull(belowThresholdTalk);
        Assert.Null(belowThresholdTalk.CharacterEvent);

        var laterDay = PrepareOrinSession(
            day: 17,
            relationshipPoints: 60,
            metOrin: true,
            characterEvents: new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        15
                    )
                ]
            }
        );
        var laterTalk = laterDay.Session.InteractWithVillager(
            laterDay.OrinPosition,
            out var laterResult
        );

        Assert.True(laterResult.Succeeded);
        Assert.NotNull(laterTalk);
        Assert.NotNull(laterTalk.CharacterEvent);
        Assert.Equal(
            CharacterEventCatalog.OrinSharedLanternRouteId,
            laterTalk.CharacterEvent.EventId
        );
        Assert.Equal(3, laterTalk.CharacterEvent.DialogueKeys.Count);
        Assert.False(laterDay.Session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.OrinSharedLanternRouteId
        ));
        Assert.True(laterDay.Session.CompleteCharacterEvent(
            CharacterEventCatalog.OrinSharedLanternRouteId
        ).Succeeded);
    }

    [Fact]
    public void OrinInvalidInteractionsNeverProgressEvents()
    {
        var wrongTool = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: true
        );
        wrongTool.Session.Inventory.Select(1);

        var blocked = wrongTool.Session.InteractWithVillager(
            wrongTool.OrinPosition,
            out var blockedResult
        );

        Assert.Null(blocked);
        Assert.False(blockedResult.Succeeded);
        Assert.Null(wrongTool.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongTool.Session.CharacterEvents.Capture().Entries);
        Assert.False(wrongTool.Session.CompleteCharacterEvent(
            CharacterEventCatalog.OrinUnpricedWaybillId
        ).Succeeded);

        var gift = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: true
        );
        Assert.True(gift.Session.Inventory.Add(
            DataCatalog.StarbudPreserveId,
            1
        ));
        Assert.True(gift.Session.Inventory.PromoteToHotbar(
            DataCatalog.StarbudPreserveId
        ));

        var giftConversation = gift.Session.InteractWithVillager(
            gift.OrinPosition,
            out var giftResult
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation);
        Assert.Equal(GiftReaction.Loved, giftConversation.GiftReaction);
        Assert.Null(giftConversation.CharacterEvent);
        Assert.Null(gift.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(gift.Session.CharacterEvents.Capture().Entries);

        var wrongScene = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: true
        );
        wrongScene.Session.SetPlayerLocation(
            8,
            8,
            PlayerLocationIds.Cottage
        );

        var absent = wrongScene.Session.InteractWithVillager(
            wrongScene.OrinPosition,
            out var wrongSceneResult
        );

        Assert.Null(absent);
        Assert.False(wrongSceneResult.Succeeded);
        Assert.Null(wrongScene.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongScene.Session.CharacterEvents.Capture().Entries);

        var wrongTime = PrepareOrinSession(
            day: 15,
            relationshipPoints: 25,
            metOrin: true
        );
        wrongTime.Session.Clock.Reset(15, 10 * 60);

        var offRoute = wrongTime.Session.InteractWithVillager(
            wrongTime.OrinPosition,
            out var wrongTimeResult
        );

        Assert.Null(offRoute);
        Assert.False(wrongTimeResult.Succeeded);
        Assert.Null(wrongTime.Session.CharacterEvents.ActiveEventId);
        Assert.Empty(wrongTime.Session.CharacterEvents.Capture().Entries);
    }

    [Theory]
    [InlineData(7, 14 * 60, "village.npc.orin.restday")]
    [InlineData(15, 8 * 60, "village.npc.orin.morning")]
    [InlineData(15, 19 * 60, "village.npc.orin.evening")]
    public void OrinEventsRequireTheOrdinaryAfternoonPlazaSchedule(
        int day,
        int minuteOfDay,
        string expectedDialogueKey
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.OrinId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.OrinId,
                    Points = 25,
                    LastTalkDay = day
                }
            ]
        };
        session.Restore(save);
        session.Inventory.Select(0);
        var orin = session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        ).Single(state => state.Definition.Id == VillageCatalog.OrinId);
        Assert.Equal(expectedDialogueKey, orin.DialogueKey);

        var conversation = session.InteractWithVillager(
            orin.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.Equal(expectedDialogueKey, conversation.DialogueKey);
        Assert.Null(conversation.CharacterEvent);
        Assert.Null(session.CharacterEvents.ActiveEventId);
        Assert.Empty(session.CharacterEvents.Capture().Entries);
    }

    [Fact]
    public void CharacterEventSaveFiltersUnknownDuplicatesAndBadOrder()
    {
        Assert.Empty(
            CharacterEventSystem.NormalizeSave(null, 5).Entries
        );

        var normalized = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId = "unknown_event",
                        CompletedDay = 1
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 3
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 2
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraRememberedWayHomeId,
                        CompletedDay = 5
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraRememberedWayHomeId,
                        CompletedDay = 4
                    }
                ]
            },
            5
        );

        Assert.Collection(
            normalized.Entries,
            first =>
            {
                Assert.Equal(
                    CharacterEventCatalog.LioraFadedReturnRouteId,
                    first.EventId
                );
                Assert.Equal(2, first.CompletedDay);
            },
            second =>
            {
                Assert.Equal(
                    CharacterEventCatalog.LioraRememberedWayHomeId,
                    second.EventId
                );
                Assert.Equal(4, second.CompletedDay);
            }
        );

        var invalidOrder = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 3
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraRememberedWayHomeId,
                        CompletedDay = 3
                    }
                ]
            },
            5
        );

        Assert.Single(invalidOrder.Entries);
        Assert.Equal(
            CharacterEventCatalog.LioraFadedReturnRouteId,
            invalidOrder.Entries[0].EventId
        );
    }

    [Fact]
    public void CharacterEventSaveNormalizesNpcChainsIndependently()
    {
        var normalized = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 1
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraRememberedWayHomeId,
                        CompletedDay = 2
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 4
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 3
                    },
                    new CharacterEventEntrySave
                    {
                        EventId = CharacterEventCatalog.TaviMendedLightId,
                        CompletedDay = 3
                    },
                    new CharacterEventEntrySave
                    {
                        EventId = "unknown_tavi_event",
                        CompletedDay = 1
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.NemiUndeliverableLetterId,
                        CompletedDay = 2
                    },
                    new CharacterEventEntrySave
                    {
                        EventId = CharacterEventCatalog.NemiStarChartRouteId,
                        CompletedDay = 4
                    }
                ]
            },
            5
        );

        Assert.Collection(
            normalized.Entries,
            first => Assert.Equal(
                CharacterEventCatalog.LioraFadedReturnRouteId,
                first.EventId
            ),
            second => Assert.Equal(
                CharacterEventCatalog.LioraRememberedWayHomeId,
                second.EventId
            ),
            third =>
            {
                Assert.Equal(
                    CharacterEventCatalog.TaviCrackedMoonRuneId,
                    third.EventId
                );
                Assert.Equal(3, third.CompletedDay);
            },
            fourth => Assert.Equal(
                CharacterEventCatalog.NemiUndeliverableLetterId,
                fourth.EventId
            ),
            fifth => Assert.Equal(
                CharacterEventCatalog.NemiStarChartRouteId,
                fifth.EventId
            )
        );

        var independentTaviChain = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraRememberedWayHomeId,
                        CompletedDay = 4
                    },
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 2
                    },
                    new CharacterEventEntrySave
                    {
                        EventId = CharacterEventCatalog.TaviMendedLightId,
                        CompletedDay = 4
                    },
                    new CharacterEventEntrySave
                    {
                        EventId = CharacterEventCatalog.NemiStarChartRouteId,
                        CompletedDay = 4
                    }
                ]
            },
            5
        );

        Assert.Collection(
            independentTaviChain.Entries,
            first => Assert.Equal(
                CharacterEventCatalog.TaviCrackedMoonRuneId,
                first.EventId
            ),
            second => Assert.Equal(
                CharacterEventCatalog.TaviMendedLightId,
                second.EventId
            )
        );
    }

    [Fact]
    public void KaelSaveNormalizationPreservesIndependentNpcChains()
    {
        var normalized = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.LioraFadedReturnRouteId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.LioraRememberedWayHomeId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviCrackedMoonRuneId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviMendedLightId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiUndeliverableLetterId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiStarChartRouteId,
                        4
                    ),
                    EventEntry("unknown_kael_event", 1),
                    EventEntry(
                        CharacterEventCatalog.KaelBrokenBlueRuneId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelBrokenBlueRuneId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        5
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        3
                    )
                ]
            },
            8
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.LioraFadedReturnRouteId,
                CharacterEventCatalog.LioraRememberedWayHomeId,
                CharacterEventCatalog.TaviCrackedMoonRuneId,
                CharacterEventCatalog.TaviMendedLightId,
                CharacterEventCatalog.NemiUndeliverableLetterId,
                CharacterEventCatalog.NemiStarChartRouteId,
                CharacterEventCatalog.KaelBrokenBlueRuneId
            },
            normalized.Entries.Select(entry => entry.EventId)
        );
        Assert.Equal(
            3,
            normalized.Entries.Single(entry =>
                entry.EventId ==
                    CharacterEventCatalog.KaelBrokenBlueRuneId
            ).CompletedDay
        );
        Assert.DoesNotContain(
            normalized.Entries,
            entry => entry.EventId ==
                CharacterEventCatalog.KaelSafeReturnRouteId
        );

        var orphan = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.LioraFadedReturnRouteId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.LioraRememberedWayHomeId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        4
                    )
                ]
            },
            5
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.LioraFadedReturnRouteId,
                CharacterEventCatalog.LioraRememberedWayHomeId
            },
            orphan.Entries.Select(entry => entry.EventId)
        );
    }

    [Fact]
    public void SelaSaveNormalizationFiltersCorruptionWithoutDroppingOtherChains()
    {
        var normalized = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.LioraFadedReturnRouteId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.LioraRememberedWayHomeId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviCrackedMoonRuneId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviMendedLightId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiUndeliverableLetterId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiStarChartRouteId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelBrokenBlueRuneId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        5
                    ),
                    EventEntry("unknown_sela_event", 1),
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaSharedForgeRhythmId,
                        6
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaSharedForgeRhythmId,
                        3
                    )
                ]
            },
            8
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.LioraFadedReturnRouteId,
                CharacterEventCatalog.LioraRememberedWayHomeId,
                CharacterEventCatalog.TaviCrackedMoonRuneId,
                CharacterEventCatalog.TaviMendedLightId,
                CharacterEventCatalog.NemiUndeliverableLetterId,
                CharacterEventCatalog.NemiStarChartRouteId,
                CharacterEventCatalog.KaelBrokenBlueRuneId,
                CharacterEventCatalog.KaelSafeReturnRouteId,
                CharacterEventCatalog.SelaTemperedStarlightId
            },
            normalized.Entries.Select(entry => entry.EventId)
        );
        Assert.Equal(
            3,
            normalized.Entries.Single(entry =>
                entry.EventId ==
                    CharacterEventCatalog.SelaTemperedStarlightId
            ).CompletedDay
        );
        Assert.DoesNotContain(
            normalized.Entries,
            entry => entry.EventId ==
                CharacterEventCatalog.SelaSharedForgeRhythmId
        );

        var orphan = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.KaelBrokenBlueRuneId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaSharedForgeRhythmId,
                        5
                    )
                ]
            },
            6
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.KaelBrokenBlueRuneId,
                CharacterEventCatalog.KaelSafeReturnRouteId
            },
            orphan.Entries.Select(entry => entry.EventId)
        );
    }

    [Fact]
    public void OrinSaveNormalizationPreservesFiveIndependentNpcChains()
    {
        var normalized = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.LioraFadedReturnRouteId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.LioraRememberedWayHomeId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviCrackedMoonRuneId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.TaviMendedLightId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiUndeliverableLetterId,
                        1
                    ),
                    EventEntry(
                        CharacterEventCatalog.NemiStarChartRouteId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelBrokenBlueRuneId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.KaelSafeReturnRouteId,
                        5
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaSharedForgeRhythmId,
                        6
                    ),
                    EventEntry("unknown_orin_event", 1),
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.OrinUnpricedWaybillId,
                        3
                    ),
                    EventEntry(
                        CharacterEventCatalog.OrinSharedLanternRouteId,
                        6
                    ),
                    EventEntry(
                        CharacterEventCatalog.OrinSharedLanternRouteId,
                        3
                    )
                ]
            },
            8
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.LioraFadedReturnRouteId,
                CharacterEventCatalog.LioraRememberedWayHomeId,
                CharacterEventCatalog.TaviCrackedMoonRuneId,
                CharacterEventCatalog.TaviMendedLightId,
                CharacterEventCatalog.NemiUndeliverableLetterId,
                CharacterEventCatalog.NemiStarChartRouteId,
                CharacterEventCatalog.KaelBrokenBlueRuneId,
                CharacterEventCatalog.KaelSafeReturnRouteId,
                CharacterEventCatalog.SelaTemperedStarlightId,
                CharacterEventCatalog.SelaSharedForgeRhythmId,
                CharacterEventCatalog.OrinUnpricedWaybillId
            },
            normalized.Entries.Select(entry => entry.EventId)
        );
        Assert.Equal(
            3,
            normalized.Entries.Single(entry =>
                entry.EventId ==
                    CharacterEventCatalog.OrinUnpricedWaybillId
            ).CompletedDay
        );
        Assert.DoesNotContain(
            normalized.Entries,
            entry => entry.EventId ==
                CharacterEventCatalog.OrinSharedLanternRouteId
        );

        var orphan = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        CharacterEventCatalog.SelaTemperedStarlightId,
                        2
                    ),
                    EventEntry(
                        CharacterEventCatalog.SelaSharedForgeRhythmId,
                        4
                    ),
                    EventEntry(
                        CharacterEventCatalog.OrinSharedLanternRouteId,
                        5
                    )
                ]
            },
            6
        );

        Assert.Equal(
            new[]
            {
                CharacterEventCatalog.SelaTemperedStarlightId,
                CharacterEventCatalog.SelaSharedForgeRhythmId
            },
            orphan.Entries.Select(entry => entry.EventId)
        );
    }

    private static CharacterEventEntrySave EventEntry(
        string eventId,
        int completedDay
    ) => new()
    {
        EventId = eventId,
        CompletedDay = completedDay
    };

    private static (
        GameSession Session,
        GridPosition LioraPosition
    ) PrepareLioraSession(
        int day,
        int relationshipPoints,
        bool metLiora,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 10 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metLiora ? [VillageCatalog.LioraId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            day,
            minuteOfDay
        );
        Assert.NotNull(liora);
        Assert.Equal(
            PlayerLocationIds.MoonlitArchive,
            liora.LocationId
        );
        return (session, liora.Position);
    }

    private static (
        GameSession Session,
        GridPosition TaviPosition
    ) PrepareTaviSession(
        int day,
        int relationshipPoints,
        bool metTavi,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 10 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 18 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metTavi ? [VillageCatalog.TaviId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.TaviId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        var tavi = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            day,
            minuteOfDay
        );
        Assert.NotNull(tavi);
        Assert.Equal(
            PlayerLocationIds.MoonstoneWorkshop,
            tavi.LocationId
        );
        return (session, tavi.Position);
    }

    private static (
        GameSession Session,
        GridPosition NemiPosition
    ) PrepareNemiSession(
        int day,
        int relationshipPoints,
        bool metNemi,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 14 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metNemi ? [VillageCatalog.NemiId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.NemiId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        Assert.Equal(DataCatalog.ClearWeatherId, session.Weather.CurrentId);
        Assert.NotEqual(
            CalendarSystem.LanternrestWeekdayIndex,
            CalendarSystem.WeekdayIndex(day)
        );
        var nemi = session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        ).Single(state => state.Definition.Id == VillageCatalog.NemiId);
        Assert.Equal(PlayerLocationIds.World, nemi.LocationId);
        Assert.Equal("village.npc.nemi.route", nemi.DialogueKey);
        return (session, nemi.Position);
    }

    private static (
        GameSession Session,
        GridPosition KaelPosition
    ) PrepareKaelSession(
        int day,
        int relationshipPoints,
        bool metKael,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 14 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metKael ? [VillageCatalog.KaelId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.KaelId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        Assert.Equal(DataCatalog.ClearWeatherId, session.Weather.CurrentId);
        Assert.NotEqual(
            CalendarSystem.LanternrestWeekdayIndex,
            CalendarSystem.WeekdayIndex(day)
        );
        var kael = session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        ).Single(state => state.Definition.Id == VillageCatalog.KaelId);
        Assert.Equal(PlayerLocationIds.World, kael.LocationId);
        Assert.Equal("village.npc.kael.plaza", kael.DialogueKey);
        return (session, kael.Position);
    }

    private static (
        GameSession Session,
        GridPosition SelaPosition
    ) PrepareSelaSession(
        int day,
        int relationshipPoints,
        bool metSela,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 14 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metSela ? [VillageCatalog.SelaId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.SelaId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        Assert.Equal(DataCatalog.ClearWeatherId, session.Weather.CurrentId);
        Assert.NotEqual(
            CalendarSystem.LanternrestWeekdayIndex,
            CalendarSystem.WeekdayIndex(day)
        );
        var sela = session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        ).Single(state => state.Definition.Id == VillageCatalog.SelaId);
        Assert.Equal(PlayerLocationIds.World, sela.LocationId);
        Assert.Equal("village.npc.sela.plaza", sela.DialogueKey);
        return (session, sela.Position);
    }

    private static (
        GameSession Session,
        GridPosition OrinPosition
    ) PrepareOrinSession(
        int day,
        int relationshipPoints,
        bool metOrin,
        int? lastTalkDay = null,
        CharacterEventSave? characterEvents = null
    )
    {
        const int minuteOfDay = 14 * 60;
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = metOrin ? [VillageCatalog.OrinId] : [],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.OrinId,
                    Points = relationshipPoints,
                    LastTalkDay = lastTalkDay ?? day
                }
            ]
        };
        save.CharacterEvents = characterEvents ?? new CharacterEventSave();
        session.Restore(save);
        session.Inventory.Select(0);

        Assert.Equal(DataCatalog.ClearWeatherId, session.Weather.CurrentId);
        Assert.NotEqual(
            CalendarSystem.LanternrestWeekdayIndex,
            CalendarSystem.WeekdayIndex(day)
        );
        var orin = session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        ).Single(state => state.Definition.Id == VillageCatalog.OrinId);
        Assert.Equal(PlayerLocationIds.World, orin.LocationId);
        Assert.Equal("village.npc.orin.plaza", orin.DialogueKey);
        return (session, orin.Position);
    }
}

public sealed class LocaleTests
{
    [Fact]
    public void ChineseAndEnglishHaveExactlyTheSameKeys()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(LocaleService.SimplifiedChinese, ReadLocale("zh_CN.json"));

        var english = locale.Keys(LocaleService.English).Order().ToArray();
        var chinese = locale.Keys(LocaleService.SimplifiedChinese).Order().ToArray();
        Assert.Equal(english, chinese);
        Assert.DoesNotContain(english, key => locale.Tr(key).StartsWith('['));
    }

    [Fact]
    public void GleamriseCropItemAndSeasonRuleKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = DataCatalog.GleamriseCropIds
            .Select(cropId => DataCatalog.Crop(cropId).NameKey)
            .Concat(DataCatalog.GleamriseCropIds.SelectMany(cropId =>
            {
                var crop = DataCatalog.Crop(cropId);
                return DataCatalog.ItemFamilyIds(cropId)
                    .Append(crop.SeedItemId)
                    .Select(itemId => DataCatalog.Item(itemId).NameKey);
            }))
            .Concat(
            [
                "target.blocked.seed_out_of_season",
                "notice.seed_out_of_season",
                "shop.seed_out_of_season"
            ])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void OrchardItemActionAndStatusKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = DataCatalog.FruitTrees.Values
            .SelectMany(tree => new[]
            {
                tree.NameKey,
                DataCatalog.Item(tree.SaplingItemId).NameKey,
                DataCatalog.Item(tree.HarvestItemId).NameKey
            })
            .Concat(new[]
            {
                DataCatalog.Item(DataCatalog.StarhoneyId).NameKey,
                DataCatalog.Item(DataCatalog.GlowcombHiveId).NameKey,
                DataCatalog.CraftingRecipes[
                    DataCatalog.GlowcombHiveRecipeId
                ].NameKey,
                "target.action.plant_tree",
                "target.action.harvest_fruit",
                "target.action.place_hive",
                "target.action.collect_honey",
                "target.status.fruit_tree_growing",
                "target.status.fruit_tree_recovering",
                "target.status.beehive_needs_tree",
                "target.status.beehive_brewing",
                "target.blocked.no_sapling",
                "target.blocked.sapling_home",
                "target.blocked.sapling_ground",
                "target.blocked.sapling_occupied",
                "target.blocked.sapling_clear",
                "target.blocked.sapling_out_of_season",
                "notice.no_sapling",
                "notice.sapling_out_of_season",
                "notice.sapling_home_only",
                "notice.sapling_ground_only",
                "notice.sapling_occupied",
                "notice.sapling_blocked",
                "notice.fruit_tree_planted",
                "notice.fruit_tree_growing",
                "notice.fruit_tree_recovering",
                "notice.fruit_tree_harvested",
                "notice.honey_not_ready",
                "notice.honey_collected",
                "shop.sapling_out_of_season"
            })
            .Distinct(StringComparer.Ordinal);

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void AnimalItemActionAndNoticeKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = new[]
        {
            DataCatalog.Item(DataCatalog.StargrainFeedId).NameKey,
            DataCatalog.Item(DataCatalog.StarfeatherEggId).NameKey,
            DataCatalog.Item(DataCatalog.GlowcustardId).NameKey,
            DataCatalog.CraftingRecipes[
                DataCatalog.StargrainFeedRecipeId
            ].NameKey,
            DataCatalog.ProcessorRecipes[
                DataCatalog.GlowcustardRecipeId
            ].NameKey,
            "target.action.build_coop",
            "target.action.feed_chicken",
            "target.action.pet_chicken",
            "target.action.collect_eggs",
            "animal.coop.built",
            "animal.coop.not_built",
            "animal.coop.already_built",
            "animal.coop.need_coins",
            "animal.coop.need_materials",
            "animal.chicken.need_feed",
            "animal.chicken.fed",
            "animal.chicken.petted",
            "animal.chicken.already_fed",
            "animal.chicken.already_cared",
            "animal.chicken.no_eggs",
            "animal.chicken.eggs_collected"
        };

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys.Distinct(StringComparer.Ordinal),
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void FishingItemActionAndNoticeKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = DataCatalog.Fishes.Values
            .SelectMany(fish => new[]
            {
                fish.NameKey,
                DataCatalog.Item(fish.ItemId).NameKey
            })
            .Concat(FishingSystem.CollectionRewardDefinitions.SelectMany(
                reward => new[]
                {
                    reward.TitleKey,
                    reward.DescriptionKey
                }
            ))
            .Concat(new[]
            {
                DataCatalog.Item(DataCatalog.FishingRodId).NameKey,
                "menu.fishing_collection",
                "target.action.fish",
                "target.need.bucket_or_rod",
                "target.status.no_fish",
                "notice.not_fishing_water",
                "notice.fish_not_biting",
                "notice.fish_caught",
                "fishing.collection.title",
                "fishing.collection.summary",
                "fishing.collection.hint",
                "fishing.collection.detail",
                "fishing.collection.hidden_name",
                "fishing.collection.hidden_detail",
                "fishing.collection.caught",
                "fishing.collection.unseen",
                "fishing.reward.detail",
                "fishing.reward.coins",
                "fishing.reward.item",
                "fishing.reward.action.claim",
                "fishing.reward.status.claimed",
                "fishing.reward.status.locked",
                "fishing.reward.claimed",
                "fishing.reward.not_ready",
                "fishing.reward.already_claimed",
                "fishing.reward.unknown",
                "fishing.water.homestead_pond",
                "fishing.water.crystal_stream",
                "fishing.water.moonwater_wetlands",
                "fishing.condition.all_seasons",
                "fishing.condition.seasoned",
                "fishing.condition.any_time",
                "fishing.condition.any_weather",
                "fishing.condition.time",
                "fishing.condition.weather",
                "fishing.condition.weather_time"
            })
            .Distinct(StringComparer.Ordinal);

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void StarlightPedestalAndNodeKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var definitionKeys = DataCatalog.StarlightPedestals.Values
            .SelectMany(pedestal => new[]
            {
                pedestal.NameKey,
                pedestal.RegionKey,
                pedestal.RewardTitleKey,
                pedestal.RewardDescriptionKey
            }.Concat(pedestal.Nodes.SelectMany(node => new[]
            {
                node.TitleKey,
                node.DescriptionKey
            })));
        var keys = definitionKeys.Concat(new[]
            {
                "target.action.open_starlight",
                "starlight.state.restored",
                "starlight.state.progress",
                "starlight.node.progress",
                "starlight.node.action.contribute",
                "starlight.node.action.missing",
                "starlight.node.action.complete",
                "starlight.reward.unlocked",
                "starlight.opened",
                "starlight.opened.moonwater",
                "starlight.contributed",
                "starlight.node_completed",
                "starlight.activated",
                "starlight.activated.moonwater",
                "starlight.nothing_available",
                "starlight.node_already_complete",
                "starlight.unknown_node",
                "starlight.hud"
            })
            .Distinct(StringComparer.Ordinal);

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void GleamriseFestivalKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = new[]
        {
            DataCatalog.Item(DataCatalog.GleamriseFestivalTokenId).NameKey,
            "target.action.enter_gleamrise_festival",
            "target.action.exit_gleamrise_festival",
            "target.action.open_festival_activity",
            "target.action.open_festival_exchange",
            "target.status.gleamrise_festival_closed",
            "notice.enter_gleamrise_festival",
            "notice.leave_gleamrise_festival",
            "notice.festival_world_only",
            "notice.gleamrise_festival_only",
            "festival.gleamrise.closed",
            "festival.gleamrise.joined",
            "festival.gleamrise.completed",
            "festival.gleamrise.festival_stage_lay_moonstone_rows.completed",
            "festival.gleamrise.festival_stage_sow_dawnlace.completed",
            "festival.gleamrise.festival_stage_tune_glimmerpod.completed",
            "festival.exchange.unknown",
            "festival.exchange.need_tokens",
            "festival.exchange.changed",
            "festival.exchange.done",
            "festival.stage.rows.title",
            "festival.stage.rows.description",
            "festival.stage.dawnlace.title",
            "festival.stage.dawnlace.description",
            "festival.stage.glimmerpod.title",
            "festival.stage.glimmerpod.description",
            "festival.exchange.dawnlace",
            "festival.exchange.glimmerpod",
            "festival.exchange.mint",
            "festival.exchange.tuber",
            "festival.exchange.fertilizer",
            "festival.exchange.sapling",
            "festival.ui.title",
            "festival.ui.subtitle",
            "festival.ui.summary",
            "festival.ui.stage_header",
            "festival.ui.exchange_header",
            "festival.ui.stage_reward",
            "festival.ui.stage_reward_tokens",
            "festival.ui.stage_do",
            "festival.ui.stage_wait",
            "festival.ui.stage_complete",
            "festival.ui.exchange_action"
        };

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys.Distinct(StringComparer.Ordinal),
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void TeaHouseInteractionKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = new[]
        {
            "target.action.enter_tea_house",
            "target.action.exit_tea_house",
            "target.action.inspect_tea_counter",
            "target.status.tea_house_closed",
            "notice.enter_tea_house",
            "notice.leave_tea_house",
            "notice.tea_house_closed",
            "notice.tea_house_world_only",
            "tea_house.counter.name",
            "tea_house.counter.dialogue"
        };

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void EmporiumInteractionKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = new[]
        {
            "target.action.enter_emporium",
            "target.action.exit_emporium",
            "target.action.inspect_manifest",
            "target.status.emporium_closed",
            "notice.enter_emporium",
            "notice.leave_emporium",
            "notice.emporium_closed",
            "notice.emporium_world_only",
            "emporium.manifest.name",
            "emporium.manifest.dialogue",
            "village.landmark.twilight_emporium",
            "village.npc.orin.emporium"
        };

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void StarfallWatchInteractionKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = new[]
        {
            "target.action.enter_starfall_watch",
            "target.action.exit_starfall_watch",
            "target.action.inspect_seal_route_table",
            "target.status.starfall_watch_closed",
            "notice.enter_starfall_watch",
            "notice.leave_starfall_watch",
            "notice.starfall_watch_closed",
            "notice.starfall_watch_world_only",
            "starfall_watch.table.name",
            "starfall_watch.table.dialogue",
            "village.landmark.starfall_watch",
            "village.npc.kael.starfall_watch"
        };

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void WeeklyCommissionKeysAreBilingual()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var definition = DataCatalog.WeeklyCommission;
        var keys = new[]
        {
            "commission.tab.daily",
            "commission.tab.weekly",
            "weekly_commission.board.week",
            "weekly_commission.board.stage",
            definition.TitleKey,
            "weekly_commission.reward",
            "weekly_commission.state.offered",
            "weekly_commission.state.stage_ready",
            "weekly_commission.state.reward_ready",
            "weekly_commission.action.accept",
            "weekly_commission.action.advance",
            "weekly_commission.action.claim",
            "weekly_commission.hud",
            "weekly_commission.backpack_full"
        }.Concat(
            definition.Stages.Select(stage => stage.DescriptionKey)
        );

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void EveryVillagerDefinitionHasBilingualDialogueAndGiftFeedback()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var definitionKeys = VillageCatalog.Npcs.Values.SelectMany(npc =>
            new[]
            {
                npc.NameKey,
                npc.RoleKey,
                npc.IntroductionKey,
                $"village.npc.{npc.Id}.gift.loved",
                $"village.npc.{npc.Id}.gift.liked",
                $"village.npc.{npc.Id}.gift.neutral",
                $"village.npc.{npc.Id}.gift.disliked"
            }.Concat(npc.Schedule.Select(slot => slot.DialogueKey))
        ).Distinct(StringComparer.Ordinal);

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                definitionKeys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void EveryCharacterEventPageExistsInBothLanguages()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var dialogueKeys = CharacterEventCatalog.Definitions
            .SelectMany(definition => definition.DialogueKeys)
            .ToArray();

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                dialogueKeys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    [Fact]
    public void FormatsObjectivesInBothLanguages()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(LocaleService.SimplifiedChinese, ReadLocale("zh_CN.json"));

        locale.SetLocale(LocaleService.English);
        Assert.Contains("2/3", locale.Tr("objective.till", 2));
        locale.SetLocale(LocaleService.SimplifiedChinese);
        Assert.Contains("2/3", locale.Tr("objective.till", 2));
    }

    [Fact]
    public void FarmingSpecializationTextExistsInBothLanguages()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );
        var keys = FarmingSkillCatalog.Specializations.Values
            .SelectMany(definition => new[]
            {
                definition.NameKey,
                definition.DescriptionKey
            })
            .Concat(new[]
            {
                "hud.farming_skill",
                "hud.farming_skill_max",
                "farming.specialization.title",
                "farming.specialization.body",
                "farming.specialization.warning"
            });

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                keys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    private static string ReadLocale(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "localization", name));
}

public sealed class SaveServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"luminfield-tests-{Guid.NewGuid():N}"
    );

    [Fact]
    public void RoundTripsTheFullSession()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame(LocaleService.English);
        Assert.True(session.AcceptDailyCommission().Succeeded);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.InteractWithMira();
        session.Inventory.Select(1);
        session.UseSelected(new GridPosition(12, 16));
        session.Inventory.Select(3);
        session.UseSelected(new GridPosition(12, 16));
        session.Inventory.Select(1);
        session.UseSelected(FindWorldResource(WorldResourceKind.Crystal));
        session.BuyItem(DataCatalog.MoonrootSeedId);
        session.Inventory.Add(DataCatalog.StarbudId, 2);
        session.StartProcessing(DataCatalog.StarbudPreserveRecipeId);
        session.Inventory.Add(DataCatalog.MoonrootId, 1);
        session.QueueForShipping(DataCatalog.MoonrootId);
        session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        Assert.True(session.CraftItem(DataCatalog.StarwovenChestRecipeId).Succeeded);
        var storagePosition = new GridPosition(25, 13);
        Assert.True(session.UseSelected(storagePosition).Succeeded);
        session.Inventory.Add(DataCatalog.CloudleafId, 1);
        Assert.True(session.StoreInChest(
            storagePosition,
            DataCatalog.CloudleafId
        ).Succeeded);
        var pathPosition = new GridPosition(26, 13);
        session.Inventory.Add(DataCatalog.MoonstonePathId, 1);
        session.Inventory.PromoteToHotbar(DataCatalog.MoonstonePathId);
        Assert.True(session.UseSelected(pathPosition).Succeeded);
        session.Inventory.Add(DataCatalog.StarbudId, 1);
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        ).Succeeded);
        session.SetPlayerState(70 * 16 + 8, 70 * 16 + 8, false);
        var skillReadySave = session.Capture();
        skillReadySave.FarmingSkill.Experience = 100;
        session.Restore(skillReadySave);
        Assert.True(session.ChooseFarmingSpecialization(
            FarmingSkillCatalog.ResonanceScholarId
        ).Succeeded);
        session.Inventory.Select(5);
        Assert.True(session.UseSelected(new GridPosition(38, 21)).Succeeded);

        service.Save(session.Capture());
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(LocaleService.English, result.Save.Locale);
        Assert.Equal(70 * 16 + 8, result.Save.Player.X);
        Assert.Single(result.Save.FarmTiles);
        Assert.Equal(GameSession.NewGameCoins - 24, result.Save.Coins);
        Assert.Equal(DataCatalog.StarbudPreserveRecipeId, result.Save.Processor.RecipeId);
        Assert.Equal(1, result.Save.Processor.RemainingNights);
        Assert.Equal(3, result.Save.Processor.Machines.Count);
        Assert.Equal(
            DataCatalog.StarbudPreserveRecipeId,
            result.Save.Processor.Machines.Single(entry =>
                entry.MachineId == ProcessorCatalog.MainMachineId
            ).RecipeId
        );
        Assert.Equal(
            GameSession.MaxWateringCanWater - 1,
            result.Save.Player.WateringCanWater
        );
        Assert.Contains("2:2", result.Save.Exploration.DiscoveredChunks);
        Assert.Single(result.Save.Resources.RemovedNodes);
        Assert.Single(result.Save.Resources.DepletedNodes);
        Assert.Equal(1, result.Save.Resources.DepletedNodes[0].RemovedDay);
        Assert.Equal(DataCatalog.ClearWeatherId, result.Save.Weather.CurrentId);
        Assert.Equal(DataCatalog.RainWeatherId, result.Save.Weather.ForecastId);
        Assert.Single(result.Save.Shipping.Pending);
        Assert.Equal(DataCatalog.MoonrootId, result.Save.Shipping.Pending[0].ItemId);
        Assert.Single(result.Save.Storage.Chests);
        Assert.Equal(storagePosition.X, result.Save.Storage.Chests[0].X);
        Assert.Equal(storagePosition.Y, result.Save.Storage.Chests[0].Y);
        Assert.Single(result.Save.Storage.Chests[0].Items);
        Assert.Equal(
            DataCatalog.CloudleafId,
            result.Save.Storage.Chests[0].Items[0].ItemId
        );
        Assert.Single(result.Save.FarmObjects.Objects);
        Assert.Equal(
            DataCatalog.MoonstonePathId,
            result.Save.FarmObjects.Objects[0].ItemId
        );
        Assert.Equal(pathPosition.X, result.Save.FarmObjects.Objects[0].X);
        Assert.Equal(pathPosition.Y, result.Save.FarmObjects.Objects[0].Y);
        Assert.Equal(100, result.Save.FarmingSkill.Experience);
        Assert.Equal(
            FarmingSkillCatalog.ResonanceScholarId,
            result.Save.FarmingSkill.SpecializationId
        );
        var restoredSession = new GameSession();
        restoredSession.Restore(result.Save);
        Assert.Equal(
            DataCatalog.MoonstonePathId,
            restoredSession.FarmObjects.ItemAt(pathPosition)
        );
        Assert.Equal(
            FarmingSkillCatalog.ResonanceScholarId,
            restoredSession.FarmingSkill.SpecializationId
        );
        Assert.Equal(
            DataCatalog.PlantStarbudCommissionId,
            result.Save.Commission.DefinitionId
        );
        Assert.True(result.Save.Commission.Accepted);
        Assert.Equal(1, result.Save.Commission.Progress);
        Assert.False(result.Save.Commission.Claimed);
        Assert.Equal(1, result.Save.WeeklyCommission.Week);
        Assert.Equal(
            DataCatalog.StarlitRouteRestorationWeeklyCommissionId,
            result.Save.WeeklyCommission.DefinitionId
        );
        Assert.True(result.Save.WeeklyCommission.Accepted);
        Assert.Equal(
            DataCatalog.StarlitRoutePlantStageId,
            result.Save.WeeklyCommission.StageId
        );
        Assert.Equal(2, result.Save.WeeklyCommission.Progress);
        Assert.False(result.Save.WeeklyCommission.Claimed);
        Assert.Empty(result.Save.Construction.ProjectId);
        Assert.False(result.Save.Construction.Completed);
        Assert.True(result.Save.Starlight.Discovered);
        Assert.False(result.Save.Starlight.RewardUnlocked);
        Assert.Equal(
            1,
            result.Save.Starlight.Nodes
                .Single(node =>
                    node.NodeId == DataCatalog.WoodlandHarvestNodeId
                )
                .Contributions
                .Single(entry => entry.ItemId == DataCatalog.StarbudId)
                .Count
        );
        Assert.Single(result.Save.Fishing.CaughtFishIds);
        Assert.Equal(
            DataCatalog.PondglowMinnowId,
            result.Save.Fishing.CaughtFishIds[0]
        );
        Assert.Equal(5, result.Save.Inventory.Sum(slot =>
            slot.ItemId == DataCatalog.StarbudSeedId ? slot.Count : 0));
    }

    [Fact]
    public void FishingSaveNormalizesUnknownFishAndRewardIds()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        var save = new GameSaveV1
        {
            Fishing = new FishingSave
            {
                CaughtFishIds =
                [
                    DataCatalog.PondglowMinnowId,
                    "unknown_fish",
                    DataCatalog.PondglowMinnowId
                ],
                ClaimedRewardIds =
                [
                    FishingSystem.FirstWatersRewardId,
                    "unknown_reward",
                    FishingSystem.FirstWatersRewardId
                ]
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(save));

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            [DataCatalog.PondglowMinnowId],
            result.Save.Fishing.CaughtFishIds
        );
        Assert.Equal(
            [FishingSystem.FirstWatersRewardId],
            result.Save.Fishing.ClaimedRewardIds
        );
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.Fishing.IsCaught(DataCatalog.PondglowMinnowId));
        Assert.Contains(
            FishingSystem.FirstWatersRewardId,
            restored.Fishing.ClaimedRewardIds
        );
    }

    [Fact]
    public void FertilizerQualityAndShippingRoundTripWithoutRerolling()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        Assert.True(session.Farm.TryTill(position, 100).Succeeded);
        Assert.True(session.Farm.TryFertilize(
            position,
            DataCatalog.StarsoilFertilizerId
        ).Succeeded);
        Assert.True(session.Farm.TryPlant(
            position,
            DataCatalog.StarbudId,
            plantedDay: 5
        ).Succeeded);
        var qualityBefore = session.Farm.HarvestQualityAt(position);
        var rollBefore = session.Farm.Tiles[position].QualityRoll;
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudStarlightId,
            2
        ));
        Assert.True(session.QueueForShipping(
            DataCatalog.StarbudStarlightId
        ).Succeeded);

        service.Save(session.Capture());
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var tile = Assert.Single(result.Save.FarmTiles);
        Assert.Equal(DataCatalog.StarsoilFertilizerId, tile.FertilizerId);
        Assert.Equal(DataCatalog.StarbudId, tile.CropId);
        Assert.Equal(rollBefore, tile.QualityRoll);
        Assert.Single(result.Save.Shipping.Pending);
        Assert.Equal(
            DataCatalog.StarbudStarlightId,
            result.Save.Shipping.Pending[0].ItemId
        );

        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.Equal(qualityBefore, restored.Farm.HarvestQualityAt(position));
        Assert.Equal(
            1,
            restored.Inventory.Count(DataCatalog.StarbudStarlightId)
        );
    }

    [Fact]
    public void ResonanceStateRoundTripsAndRejectsUnknownOrCrossCropIds()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var dawnlace = DataCatalog.Crop(DataCatalog.DawnlaceId);
        var glimmerpod = DataCatalog.Crop(DataCatalog.GlimmerpodId);
        var save = new GameSaveV1
        {
            FarmTiles =
            [
                new FarmTileState
                {
                    X = 12,
                    Y = 16,
                    Tilled = true,
                    CropId = dawnlace.Id,
                    WateredNights = dawnlace.MatureAfterWateredNights,
                    PlantedDay = 3,
                    ResonanceItemId = DataCatalog.RainwovenDawnlaceId
                },
                new FarmTileState
                {
                    X = 13,
                    Y = 16,
                    Tilled = true,
                    CropId = glimmerpod.Id,
                    WateredNights = glimmerpod.MatureAfterWateredNights,
                    PlantedDay = 0,
                    ResonanceItemId = DataCatalog.RainwovenDawnlaceId
                },
                new FarmTileState
                {
                    X = 14,
                    Y = 16,
                    Tilled = true,
                    CropId = dawnlace.Id,
                    WateredNights = dawnlace.MatureAfterWateredNights,
                    PlantedDay = 2,
                    ResonanceItemId = "removed_resonance"
                }
            ]
        };

        service.Save(save);
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var valid = result.Save.FarmTiles.Single(tile => tile.X == 12);
        var crossCrop = result.Save.FarmTiles.Single(tile => tile.X == 13);
        var unknown = result.Save.FarmTiles.Single(tile => tile.X == 14);
        Assert.Equal(DataCatalog.RainwovenDawnlaceId, valid.ResonanceItemId);
        Assert.Equal(3, valid.PlantedDay);
        Assert.Null(crossCrop.ResonanceItemId);
        Assert.Equal(1, crossCrop.PlantedDay);
        Assert.Null(unknown.ResonanceItemId);

        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.Equal(
            DataCatalog.RainwovenDawnlaceId,
            restored.Farm.HarvestItemIdAt(new GridPosition(12, 16))
        );
    }

    [Fact]
    public void ConstructionStateRoundTripsAndCompletesAfterTheSecondSleep()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Add(DataCatalog.LumenwoodId, 12);
        session.Inventory.Add(DataCatalog.CrystalShardId, 4);
        var prepared = session.Capture();
        prepared.Coins = 240;
        prepared.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        session.Restore(prepared);
        Assert.True(session.StartCottageFirstUpgrade().Succeeded);
        session.EndDay();

        service.Save(session.Capture());
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(1, result.Save.SchemaVersion);
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            result.Save.Construction.ProjectId
        );
        Assert.Equal(1, result.Save.Construction.RemainingNights);
        Assert.False(result.Save.Construction.Completed);
        var restored = new GameSession();
        restored.Restore(result.Save);
        restored.EndDay();
        Assert.True(restored.Construction.IsCompleted);
        Assert.True(restored.Capture().Construction.Completed);
    }

    [Fact]
    public void UnknownAndDamagedConstructionStateNormalizesSafely()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "construction": {
                "projectId": "removed_construction",
                "remainingNights": -7,
                "completed": true
              }
            }
            """
        );

        var unknown = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, unknown.Status);
        Assert.NotNull(unknown.Save);
        Assert.Empty(unknown.Save.Construction.ProjectId);
        Assert.Equal(0, unknown.Save.Construction.RemainingNights);
        Assert.False(unknown.Save.Construction.Completed);

        File.WriteAllText(
            path,
            $$"""
            {
              "schemaVersion": 1,
              "construction": {
                "projectId": "{{ConstructionCatalog.CottageFirstUpgradeId}}",
                "remainingNights": 99,
                "completed": false
              }
            }
            """
        );

        var clamped = new SaveService(path).Load();
        Assert.Equal(SaveLoadStatus.Loaded, clamped.Status);
        Assert.NotNull(clamped.Save);
        Assert.Equal(2, clamped.Save.Construction.RemainingNights);
        Assert.False(clamped.Save.Construction.Completed);
    }

    [Fact]
    public void CompletedConstructionRoundTripsAsPermanentUpgrade()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var save = new GameSaveV1
        {
            Construction = new ConstructionSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                RemainingNights = 99,
                Completed = true
            }
        };

        service.Save(save);
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            result.Save.Construction.ProjectId
        );
        Assert.Equal(0, result.Save.Construction.RemainingNights);
        Assert.True(result.Save.Construction.Completed);
        var restored = new GameSession();
        restored.Restore(result.Save);
        restored.EndDay();
        Assert.True(restored.Construction.IsCompleted);
    }

    [Fact]
    public void KnownConstructionWithNonPositiveRemainingNightsDoesNotUnlock()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            $$"""
            {
              "schemaVersion": 1,
              "construction": {
                "projectId": "{{ConstructionCatalog.CottageFirstUpgradeId}}",
                "remainingNights": 0,
                "completed": false
              }
            }
            """
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Empty(result.Save.Construction.ProjectId);
        Assert.Equal(0, result.Save.Construction.RemainingNights);
        Assert.False(result.Save.Construction.Completed);
    }

    [Fact]
    public void MissingFieldsReceiveSafeDefaults()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """{"schemaVersion":1,"day":0,"locale":"unknown"}""");

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(1, result.Save.Day);
        Assert.Equal(LocaleService.SimplifiedChinese, result.Save.Locale);
        Assert.NotNull(result.Save.Player);
        Assert.NotNull(result.Save.Inventory);
        Assert.Equal(GameSession.NewGameCoins, result.Save.Coins);
        Assert.NotNull(result.Save.Processor);
        Assert.Empty(result.Save.Processor.Machines);
        Assert.Contains("0:0", result.Save.Exploration.DiscoveredChunks);
        Assert.Equal(
            GameSession.MaxWateringCanWater,
            result.Save.Player.WateringCanWater
        );
        Assert.Empty(result.Save.Resources.RemovedNodes);
        Assert.Equal(DataCatalog.ClearWeatherId, result.Save.Weather.CurrentId);
        Assert.Equal(DataCatalog.RainWeatherId, result.Save.Weather.ForecastId);
        Assert.Empty(result.Save.Shipping.Pending);
        Assert.Empty(result.Save.Shipping.LastSettlement.Entries);
        Assert.Empty(result.Save.Storage.Chests);
        Assert.Empty(result.Save.FarmObjects.Objects);
        Assert.Equal(1, result.Save.Commission.Day);
        Assert.Equal(
            DataCatalog.PlantStarbudCommissionId,
            result.Save.Commission.DefinitionId
        );
        Assert.False(result.Save.Commission.Accepted);
        Assert.Equal(1, result.Save.WeeklyCommission.Week);
        Assert.Equal(
            DataCatalog.StarlitRouteRestorationWeeklyCommissionId,
            result.Save.WeeklyCommission.DefinitionId
        );
        Assert.Equal(
            DataCatalog.StarlitRoutePlantStageId,
            result.Save.WeeklyCommission.StageId
        );
        Assert.Empty(result.Save.Construction.ProjectId);
        Assert.False(result.Save.Construction.Completed);
        Assert.False(result.Save.WeeklyCommission.Accepted);
        Assert.False(result.Save.WeeklyCommission.Claimed);
        Assert.Equal(
            DataCatalog.WoodlandStarlightId,
            result.Save.Starlight.PedestalId
        );
        Assert.False(result.Save.Starlight.Discovered);
        Assert.False(result.Save.Starlight.RewardUnlocked);
        Assert.Equal(
            DataCatalog.WoodlandStarlight.Nodes.Count,
            result.Save.Starlight.Nodes.Count
        );
        Assert.Equal(
            DataCatalog.StarlightPedestals.Count,
            result.Save.Starlight.Pedestals.Count
        );
        var moonwaterStarlight = result.Save.Starlight.Pedestals
            .Single(pedestal =>
                pedestal.PedestalId == DataCatalog.MoonwaterStarlightId
            );
        Assert.False(moonwaterStarlight.Discovered);
        Assert.False(moonwaterStarlight.RewardUnlocked);
        Assert.Equal(
            DataCatalog.MoonwaterStarlight.Nodes.Count,
            moonwaterStarlight.Nodes.Count
        );
        Assert.NotNull(result.Save.CharacterEvents);
        Assert.Empty(result.Save.CharacterEvents.Entries);
    }

    [Fact]
    public void LegacyProcessorSaveMigratesToMainMachineOnRestore()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                processor = new
                {
                    recipeId = DataCatalog.MoonrootTonicRecipeId,
                    remainingNights = 99
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Empty(result.Save.Processor.Machines);
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.Equal(
            DataCatalog.MoonrootTonicRecipeId,
            restored.Processor.MainMachine.ActiveRecipeId
        );
        Assert.Equal(1, restored.Processor.MainMachine.RemainingNights);
    }

    [Fact]
    public void ModernProcessorEntriesOverrideLegacyAndNormalizeUnknownIds()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                processor = new
                {
                    recipeId = DataCatalog.MoonrootTonicRecipeId,
                    remainingNights = 1,
                    machines = new object[]
                    {
                        new
                        {
                            machineId = ProcessorCatalog.PrismPreserveVatId,
                            recipeId = DataCatalog.StarbudPreserveRecipeId,
                            remainingNights = 99
                        },
                        new
                        {
                            machineId = "unknown_machine",
                            recipeId = DataCatalog.MoonrootTonicRecipeId,
                            remainingNights = 1
                        }
                    }
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var machine = Assert.Single(result.Save.Processor.Machines);
        Assert.Equal(ProcessorCatalog.PrismPreserveVatId, machine.MachineId);
        Assert.Equal(1, machine.RemainingNights);
        Assert.Empty(result.Save.Processor.RecipeId);
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.Processor.MainMachine.IsIdle);
        Assert.Equal(
            DataCatalog.StarbudPreserveRecipeId,
            restored.Processor.Machine(
                ProcessorCatalog.PrismPreserveVatId
            ).ActiveRecipeId
        );

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                processor = new
                {
                    recipeId = DataCatalog.MoonrootTonicRecipeId,
                    remainingNights = 1,
                    machines = new[]
                    {
                        new
                        {
                            machineId = "unknown_machine",
                            recipeId = DataCatalog.StarbudPreserveRecipeId,
                            remainingNights = 0
                        }
                    }
                }
            })
        );

        var unknownOnly = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, unknownOnly.Status);
        Assert.NotNull(unknownOnly.Save);
        Assert.Empty(unknownOnly.Save.Processor.Machines);
        var fallback = new GameSession();
        fallback.Restore(unknownOnly.Save);
        Assert.Equal(
            DataCatalog.MoonrootTonicRecipeId,
            fallback.Processor.MainMachine.ActiveRecipeId
        );
        Assert.Equal(
            1,
            fallback.Processor.Machines.Values.Count(machine => !machine.IsIdle)
        );
    }

    [Fact]
    public void FixedProcessorCellsRejectConflictsWhileLegacyLegalCellsKeepContents()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                storage = new
                {
                    chests = new object[]
                    {
                        new
                        {
                            x = ProcessorCatalog.Machine(
                                ProcessorCatalog.PrismPreserveVatId
                            ).Position.X,
                            y = ProcessorCatalog.Machine(
                                ProcessorCatalog.PrismPreserveVatId
                            ).Position.Y,
                            items = Array.Empty<object>()
                        },
                        new
                        {
                            x = 32,
                            y = 14,
                            items = new[]
                            {
                                new
                                {
                                    itemId = DataCatalog.StarbudId,
                                    count = 2
                                }
                            }
                        }
                    }
                },
                farmObjects = new
                {
                    objects = new object[]
                    {
                        new
                        {
                            x = ProcessorCatalog.Machine(
                                ProcessorCatalog.StarweaveDryingLoomId
                            ).Position.X,
                            y = ProcessorCatalog.Machine(
                                ProcessorCatalog.StarweaveDryingLoomId
                            ).Position.Y,
                            itemId = DataCatalog.MoonstonePathId
                        },
                        new
                        {
                            x = 40,
                            y = 14,
                            itemId = DataCatalog.MoonstonePathId
                        }
                    }
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var chest = Assert.Single(result.Save.Storage.Chests);
        Assert.Equal(32, chest.X);
        Assert.Equal(14, chest.Y);
        var stack = Assert.Single(chest.Items);
        Assert.Equal(DataCatalog.StarbudId, stack.ItemId);
        Assert.Equal(2, stack.Count);
        var farmObject = Assert.Single(result.Save.FarmObjects.Objects);
        Assert.Equal(40, farmObject.X);
        Assert.Equal(14, farmObject.Y);
    }

    [Fact]
    public void InvalidOrOverlappingFarmObjectsAreRemovedWithoutClearingValidOnes()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                storage = new
                {
                    chests = new[]
                    {
                        new { x = 25, y = 13, items = Array.Empty<object>() }
                    }
                },
                farmObjects = new
                {
                    objects = new object[]
                    {
                        new
                        {
                            x = 25,
                            y = 13,
                            itemId = DataCatalog.MoonstonePathId
                        },
                        new { x = 26, y = 13, itemId = "unknown_object" },
                        new
                        {
                            x = 26,
                            y = 13,
                            itemId = DataCatalog.MoonstonePathId
                        },
                        new
                        {
                            x = 15,
                            y = 16,
                            itemId = DataCatalog.DewfallSprinklerId
                        },
                        new
                        {
                            x = 15,
                            y = 16,
                            itemId = DataCatalog.DewfallSprinklerId
                        }
                    }
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(2, result.Save.FarmObjects.Objects.Count);
        Assert.Contains(result.Save.FarmObjects.Objects, entry =>
            entry.X == 26 &&
            entry.Y == 13 &&
            entry.ItemId == DataCatalog.MoonstonePathId
        );
        Assert.Contains(result.Save.FarmObjects.Objects, entry =>
            entry.X == 15 &&
            entry.Y == 16 &&
            entry.ItemId == DataCatalog.DewfallSprinklerId
        );
    }

    [Fact]
    public void UnknownAndOverfilledStarlightStateIsNormalizedWithoutUnlocking()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                starlight = new
                {
                    pedestalId = "unknown_pedestal",
                    discovered = false,
                    rewardUnlocked = true,
                    nodes = new object[]
                    {
                        new
                        {
                            nodeId = DataCatalog.WoodlandHarvestNodeId,
                            contributions = new[]
                            {
                                new
                                {
                                    itemId = DataCatalog.StarbudId,
                                    count = 99
                                },
                                new
                                {
                                    itemId = "unknown_crop",
                                    count = 99
                                }
                            }
                        },
                        new
                        {
                            nodeId = DataCatalog.WoodlandMaterialsNodeId,
                            contributions = new[]
                            {
                                new
                                {
                                    itemId = DataCatalog.LumenwoodId,
                                    count = 99
                                },
                                new
                                {
                                    itemId = DataCatalog.CrystalShardId,
                                    count = 99
                                }
                            }
                        },
                        new
                        {
                            nodeId = DataCatalog.WoodlandCraftNodeId,
                            contributions = new[]
                            {
                                new
                                {
                                    itemId = DataCatalog.StarbudPreserveId,
                                    count = 1
                                },
                                new
                                {
                                    itemId = DataCatalog.MoonrootTonicId,
                                    count = 1
                                },
                                new
                                {
                                    itemId = "unknown_craft",
                                    count = 99
                                }
                            }
                        },
                        new
                        {
                            nodeId = "unknown_node",
                            contributions = Array.Empty<object>()
                        }
                    }
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            DataCatalog.WoodlandStarlightId,
            result.Save.Starlight.PedestalId
        );
        Assert.False(result.Save.Starlight.Discovered);
        Assert.False(result.Save.Starlight.RewardUnlocked);
        Assert.Equal(
            DataCatalog.WoodlandStarlight.Nodes.Count,
            result.Save.Starlight.Nodes.Count
        );
        Assert.Equal(
            1,
            NodeProgress(
                result.Save.Starlight,
                DataCatalog.WoodlandHarvestNodeId
            )
        );
        Assert.Equal(
            8,
            NodeProgress(
                result.Save.Starlight,
                DataCatalog.WoodlandMaterialsNodeId
            )
        );
        Assert.Equal(
            2,
            NodeProgress(
                result.Save.Starlight,
                DataCatalog.WoodlandCraftNodeId
            )
        );
        var moonwater = result.Save.Starlight.Pedestals.Single(pedestal =>
            pedestal.PedestalId == DataCatalog.MoonwaterStarlightId
        );
        Assert.False(moonwater.Discovered);
        Assert.False(moonwater.RewardUnlocked);
        Assert.Equal(
            DataCatalog.MoonwaterStarlight.Nodes.Count,
            moonwater.Nodes.Count
        );
    }

    [Fact]
    public void UnknownCommissionStateResetsToTheCurrentDayOffer()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "day": 2,
              "commission": {
                "day": 2,
                "definitionId": "unknown_commission",
                "accepted": true,
                "progress": 999,
                "claimed": true
              }
            }
            """
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(2, result.Save.Commission.Day);
        Assert.Equal(
            DataCatalog.GatherLumenwoodCommissionId,
            result.Save.Commission.DefinitionId
        );
        Assert.False(result.Save.Commission.Accepted);
        Assert.Equal(0, result.Save.Commission.Progress);
        Assert.False(result.Save.Commission.Claimed);
    }

    [Fact]
    public void LegacyRemovedResourceStartsACompatibleRespawnCycle()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        var crystal = FindWorldResource(WorldResourceKind.Crystal);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                day = 5,
                resources = new
                {
                    removedNodes = new[] { WorldDefinition.CellId(crystal) }
                }
            })
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Single(result.Save.Resources.DepletedNodes);
        Assert.Equal(5, result.Save.Resources.DepletedNodes[0].RemovedDay);

        var session = new GameSession();
        session.Restore(result.Save);
        Assert.True(session.Resources.IsRemoved(crystal));
        session.EndDay();
        Assert.True(session.Resources.IsRemoved(crystal));
        session.EndDay();
        Assert.False(session.Resources.IsRemoved(crystal));
    }

    [Fact]
    public void LegacyHoeSaveMigratesToFixedToolOrderAndKeepsItems()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": { "selectedSlot": 0 },
              "inventory": [
                { "itemId": "hoe", "count": 1 },
                { "itemId": "watering_can", "count": 1 },
                { "itemId": "starbud_seed", "count": 5 }
              ]
            }
            """
        );

        var result = new SaveService(path).Load();
        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);

        var session = new GameSession();
        session.Restore(result.Save);
        Assert.Equal(DataCatalog.HandId, session.Inventory.Slots[0].ItemId);
        Assert.Equal(DataCatalog.ShovelId, session.Inventory.Slots[1].ItemId);
        Assert.Equal(DataCatalog.MacheteId, session.Inventory.Slots[2].ItemId);
        Assert.Equal(DataCatalog.WateringCanId, session.Inventory.Slots[3].ItemId);
        Assert.Equal(DataCatalog.BucketId, session.Inventory.Slots[4].ItemId);
        Assert.Equal(DataCatalog.FishingRodId, session.Inventory.Slots[5].ItemId);
        Assert.Equal(5, session.Inventory.Count(DataCatalog.StarbudSeedId));
        Assert.Equal(1, session.Inventory.SelectedIndex);
    }

    [Fact]
    public void PlayerCoordinatesAreClampedToTheLargeWorld()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": { "x": -500, "y": 999999, "energy": 100, "selectedSlot": 0 }
            }
            """
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(8, result.Save.Player.X);
        Assert.Equal(WorldDefinition.Height * 16 - 8, result.Save.Player.Y);
    }

    [Fact]
    public void LegacyCottageFlagMigratesToStableLocationId()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 280,
                "energy": 100,
                "selectedSlot": 0,
                "insideCottage": true
              }
            }
            """
        );

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            PlayerLocationIds.Cottage,
            result.Save.Player.LocationId
        );
        Assert.True(result.Save.Player.InsideCottage);
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.InsideCottage);
    }

    [Fact]
    public void AllVillageRelationshipsRoundTripAndUnknownNpcIdsAreFiltered()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var session = new GameSession();
        session.NewGame();
        foreach (var npc in session.Village.AllCurrentNpcs(
                     session.Clock.Day,
                     session.Clock.MinuteOfDay
                 ))
        {
            var conversation = session.InteractWithVillager(
                npc.Position,
                out var interaction
            );
            Assert.True(interaction.Succeeded);
            Assert.NotNull(conversation);
        }
        session.SetPlayerLocation(
            20 * 16 + 8,
            19 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );

        var save = session.Capture();
        save.Village.MetNpcIds.Add("unknown_villager");
        save.Village.Relationships.Add(new VillageRelationshipSave
        {
            NpcId = "unknown_villager",
            Points = 999,
            LastTalkDay = 999,
            LastGiftDay = 999
        });
        var service = new SaveService(path);
        service.Save(save);
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            VillageCatalog.Npcs.Keys.Order(StringComparer.Ordinal),
            result.Save.Village.MetNpcIds.Order(StringComparer.Ordinal)
        );
        Assert.Equal(8, result.Save.Village.Relationships.Count);
        Assert.All(
            result.Save.Village.Relationships,
            relationship => Assert.Equal(2, relationship.Points)
        );
        Assert.Equal(
            PlayerLocationIds.MoonlitArchive,
            result.Save.Player.LocationId
        );

        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.Equal(
            VillageCatalog.Npcs.Keys.Order(StringComparer.Ordinal),
            restored.Village.MetNpcIds.Order(StringComparer.Ordinal)
        );
        Assert.True(restored.InsideArchive);
        Assert.All(
            VillageCatalog.Npcs.Keys,
            npcId => Assert.Equal(
                2,
                restored.Village.Relationship(npcId).Points
            )
        );
    }

    [Fact]
    public void WorkshopLocationRoundTripsAndUnknownLocationsFallbackToWorld()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        service.Save(session.Capture());

        var workshopResult = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, workshopResult.Status);
        Assert.NotNull(workshopResult.Save);
        Assert.Equal(
            PlayerLocationIds.MoonstoneWorkshop,
            workshopResult.Save.Player.LocationId
        );
        var restored = new GameSession();
        restored.Restore(workshopResult.Save);
        Assert.True(restored.InsideWorkshop);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "locationId": "removed_interior"
              }
            }
            """
        );

        var migrated = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, migrated.Status);
        Assert.NotNull(migrated.Save);
        Assert.Equal(
            PlayerLocationIds.World,
            migrated.Save.Player.LocationId
        );
    }

    [Fact]
    public void TeaHouseLocationAndLegacyLocationsLoadSafely()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarweaverTeaHouse
        );
        service.Save(session.Capture());

        var teaHouseResult = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, teaHouseResult.Status);
        Assert.NotNull(teaHouseResult.Save);
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            teaHouseResult.Save.Player.LocationId
        );
        var restored = new GameSession();
        restored.Restore(teaHouseResult.Save);
        Assert.True(restored.InsideTeaHouse);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 504,
                "y": 152,
                "energy": 100,
                "selectedSlot": 0
              }
            }
            """
        );

        var legacy = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, legacy.Status);
        Assert.NotNull(legacy.Save);
        Assert.Equal(PlayerLocationIds.World, legacy.Save.Player.LocationId);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "locationId": "retired_tea_room"
              }
            }
            """
        );

        var unknown = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, unknown.Status);
        Assert.NotNull(unknown.Save);
        Assert.Equal(
            PlayerLocationIds.World,
            unknown.Save.Player.LocationId
        );
    }

    [Fact]
    public void EmporiumLocationRoundTripsWithoutChangingLegacyFallbacks()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        service.Save(session.Capture());

        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            PlayerLocationIds.TwilightEmporium,
            result.Save.Player.LocationId
        );
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.InsideTwilightEmporium);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "insideCottage": true
              }
            }
            """
        );
        var legacyCottage = service.Load();
        Assert.Equal(SaveLoadStatus.Loaded, legacyCottage.Status);
        Assert.NotNull(legacyCottage.Save);
        Assert.Equal(
            PlayerLocationIds.Cottage,
            legacyCottage.Save.Player.LocationId
        );

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "locationId": "retired_travel_shop"
              }
            }
            """
        );
        var unknown = service.Load();
        Assert.Equal(SaveLoadStatus.Loaded, unknown.Status);
        Assert.NotNull(unknown.Save);
        Assert.Equal(
            PlayerLocationIds.World,
            unknown.Save.Player.LocationId
        );
    }

    [Fact]
    public void StarlightPostLocationRoundTripsAndLegacyFallbacksStaySafe()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarlightPost
        );
        service.Save(session.Capture());

        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            PlayerLocationIds.StarlightPost,
            result.Save.Player.LocationId
        );
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.InsideStarlightPost);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 328,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "locationId": "retired_delivery_hall"
              }
            }
            """
        );
        var unknown = service.Load();
        Assert.Equal(SaveLoadStatus.Loaded, unknown.Status);
        Assert.NotNull(unknown.Save);
        Assert.Equal(
            PlayerLocationIds.World,
            unknown.Save.Player.LocationId
        );
    }

    [Fact]
    public void StarfallWatchLocationRoundTripsAndLegacyFallbacksStaySafe()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            19 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.StarfallWatch
        );
        service.Save(session.Capture());

        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(
            PlayerLocationIds.StarfallWatch,
            result.Save.Player.LocationId
        );
        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.InsideStarfallWatch);

        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "player": {
                "x": 312,
                "y": 296,
                "energy": 100,
                "selectedSlot": 0,
                "locationId": "retired_ruins_barracks"
              }
            }
            """
        );
        var unknown = service.Load();
        Assert.Equal(SaveLoadStatus.Loaded, unknown.Status);
        Assert.NotNull(unknown.Save);
        Assert.Equal(
            PlayerLocationIds.World,
            unknown.Save.Player.LocationId
        );
    }

    [Fact]
    public void MailDeliversNextDayExactlyOnceForMeetingAndTrustedTiers()
    {
        var session = new GameSession();
        session.NewGame();
        session.Village.Restore(new VillageSave
        {
            MetNpcIds = [VillageCatalog.NemiId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 25
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.TaviId,
                    Points = 25
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.NemiId,
                    Points = 25
                }
            ]
        });

        Assert.Empty(session.Mail.Delivered);
        session.EndDay();

        Assert.Equal(4, session.Mail.Delivered.Count);
        Assert.All(
            session.Mail.Delivered,
            mail => Assert.Equal(2, mail.DeliveredDay)
        );
        var delivered = session.Mail.Delivered.ToDictionary(
            mail => mail.Definition.Id,
            StringComparer.Ordinal
        );
        Assert.False(
            delivered[MailCatalog.NemiWelcomeId].Definition.HasAttachment
        );
        Assert.Equal(
            (DataCatalog.CrystalShardId, 2),
            (
                delivered[MailCatalog.LioraTrustedId]
                    .Definition.AttachmentItemId,
                delivered[MailCatalog.LioraTrustedId]
                    .Definition.AttachmentCount
            )
        );
        Assert.Equal(
            (DataCatalog.LumenwoodId, 4),
            (
                delivered[MailCatalog.TaviTrustedId]
                    .Definition.AttachmentItemId,
                delivered[MailCatalog.TaviTrustedId]
                    .Definition.AttachmentCount
            )
        );
        Assert.Equal(
            (DataCatalog.StarbudSeedId, 3),
            (
                delivered[MailCatalog.NemiTrustedId]
                    .Definition.AttachmentItemId,
                delivered[MailCatalog.NemiTrustedId]
                    .Definition.AttachmentCount
            )
        );
        session.EndDay();
        Assert.Equal(4, session.Mail.Delivered.Count);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        restored.EndDay();
        Assert.Equal(4, restored.Mail.Delivered.Count);
    }

    [Fact]
    public void MailAttachmentClaimIsAtomicWhenBackpackIsFull()
    {
        var session = new GameSession();
        session.NewGame();
        session.Mail.Restore(new MailSave
        {
            Entries =
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.LioraTrustedId,
                    DeliveredDay = 2
                }
            ]
        });
        session.Inventory.Restore(
            Enumerable.Range(0, Inventory.SlotCount - Inventory.StartingToolCount)
                .Select(_ => new InventorySlot
                {
                    ItemId = DataCatalog.StarbudId,
                    Count = 99
                }),
            0
        );

        var failed = session.ClaimMailAttachment(
            MailCatalog.LioraTrustedId
        );

        Assert.False(failed.Succeeded);
        Assert.Equal("mail.notice.backpack_full", failed.MessageKey);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.False(session.Mail.Delivered.Single().AttachmentClaimed);

        Assert.True(session.Inventory.Remove(DataCatalog.StarbudId, 99));
        var claimed = session.ClaimMailAttachment(
            MailCatalog.LioraTrustedId
        );
        Assert.True(claimed.Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.True(session.Mail.Delivered.Single().AttachmentClaimed);

        var duplicate = session.ClaimMailAttachment(
            MailCatalog.LioraTrustedId
        );
        Assert.False(duplicate.Succeeded);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.CrystalShardId));
    }

    [Fact]
    public void MailboxPreviewAndActionShareTheHandRule()
    {
        var session = new GameSession();
        session.NewGame();
        var energy = session.Energy;

        var available = session.PreviewSelectedTarget(
            FarmLayout.StarlightMailboxCell
        );
        Assert.True(available.IsAvailable);
        Assert.Equal(TargetPreviewKind.Mailbox, available.Kind);
        Assert.True(
            session.UseSelected(FarmLayout.StarlightMailboxCell).Succeeded
        );

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            FarmLayout.StarlightMailboxCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(
            session.UseSelected(FarmLayout.StarlightMailboxCell).Succeeded
        );
        Assert.Equal(energy, session.Energy);
    }

    [Fact]
    public void MailSaveFiltersUnknownIdsAndPreservesClaimState()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 4;
        save.Coins = 177;
        save.Mail.Entries =
        [
            new MailEntrySave
            {
                MailId = MailCatalog.TaviTrustedId,
                DeliveredDay = 3,
                IsRead = true,
                AttachmentClaimed = true
            },
            new MailEntrySave
            {
                MailId = "unknown_mail",
                DeliveredDay = 99
            }
        ];
        var service = new SaveService(path);
        service.Save(save);

        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(4, result.Save.Day);
        Assert.Equal(177, result.Save.Coins);
        var mail = Assert.Single(result.Save.Mail.Entries);
        Assert.Equal(MailCatalog.TaviTrustedId, mail.MailId);
        Assert.True(mail.IsRead);
        Assert.True(mail.AttachmentClaimed);
    }

    [Fact]
    public void OrchardSaveRoundTripsAndNormalizesInvalidStates()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Storage.Chests =
        [
            new PlacedChestSave
            {
                X = 25,
                Y = 13
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = 27,
                Y = 13,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = 23,
                Y = 13,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = 99,
                FruitReady = true,
                RegrowthProgress = 99
            },
            new FruitTreeSave
            {
                X = 23,
                Y = 13,
                TreeId = DataCatalog.MoonplumTreeId
            },
            new FruitTreeSave
            {
                X = 25,
                Y = 13,
                TreeId = DataCatalog.MoonplumTreeId,
                FruitReady = true
            },
            new FruitTreeSave
            {
                X = 27,
                Y = 13,
                TreeId = DataCatalog.MoonplumTreeId,
                FruitReady = true
            },
            new FruitTreeSave
            {
                X = 12,
                Y = 16,
                TreeId = DataCatalog.MoonplumTreeId,
                FruitReady = true
            },
            new FruitTreeSave
            {
                X = 24,
                Y = 13,
                TreeId = "unknown_tree",
                FruitReady = true
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = 27,
                Y = 13,
                PendingHoney = 7,
                ProgressNights = 4
            },
            new BeehiveSave
            {
                X = 31,
                Y = 13,
                PendingHoney = 1
            }
        ];
        service.Save(save);

        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var tree = Assert.Single(result.Save.Orchard.FruitTrees);
        Assert.Equal((23, 13), (tree.X, tree.Y));
        Assert.Equal(DataCatalog.MoonplumTreeId, tree.TreeId);
        Assert.Equal(
            DataCatalog.FruitTree(DataCatalog.MoonplumTreeId)
                .MatureAfterNights,
            tree.AgeNights
        );
        Assert.True(tree.FruitReady);
        Assert.Equal(0, tree.RegrowthProgress);
        var hive = Assert.Single(result.Save.Orchard.Beehives);
        Assert.Equal((27, 13), (hive.X, hive.Y));
        Assert.Equal(1, hive.PendingHoney);
        Assert.Equal(0, hive.ProgressNights);

        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.True(restored.Orchard.HasFruitTree(new GridPosition(23, 13)));
        Assert.True(restored.Orchard.HasBeehive(new GridPosition(27, 13)));
        Assert.Equal(1, restored.Orchard.BeehiveAt(
            new GridPosition(27, 13)
        )!.PendingHoney);
    }

    [Fact]
    public void CorruptSaveIsPreservedInsteadOfOverwritten()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{not-json");

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Corrupt, result.Status);
        Assert.False(File.Exists(path));
        Assert.NotNull(result.PreservedPath);
        Assert.True(File.Exists(result.PreservedPath));
    }

    [Fact]
    public void FutureSchemaIsReportedWithoutMovingTheFile()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = SaveService.CurrentSchemaVersion + 1
        }));

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Unsupported, result.Status);
        Assert.True(File.Exists(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private static int NodeProgress(StarlightSave save, string nodeId) =>
        save.Nodes
            .Single(node => node.NodeId == nodeId)
            .Contributions
            .Sum(entry => entry.Count);

    private static GridPosition FindWorldResource(WorldResourceKind kind)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.ResourceAt(cell) == kind)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException($"No world resource found for {kind}.");
    }
}
