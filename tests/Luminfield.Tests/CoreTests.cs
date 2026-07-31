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
        session.Inventory.Select(5);
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
    public void AllEightCatalogCropsPlantGrowHarvestAndRemainSellable()
    {
        Assert.Equal(8, DataCatalog.CropIds.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8, DataCatalog.SeedItemIds.Distinct(StringComparer.Ordinal).Count());

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
            session.Inventory.Select(5);
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
        Assert.Equal(16, DataCatalog.QualityProduceItemIds.Count);
        Assert.Equal(
            16,
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
        Assert.All(inventory.Slots.Take(5), slot => Assert.Equal(1, slot.Count));
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

        Assert.True(preserve.SellPrice > starbud.SellPrice * 2);
        Assert.True(tonic.SellPrice > moonroot.SellPrice * 2);
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

        session.Inventory.Select(5);
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
        Assert.Equal("target.need.bucket", needsBucket.LabelKey);

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
        session.Inventory.Select(5);
        var soil = new GridPosition(12, 16);

        Assert.False(session.UseSelected(soil).Succeeded);
        Assert.Equal(0, session.Commission.Progress);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarbudSeedId));

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(soil).Succeeded);
        session.Inventory.Select(5);
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
        Assert.Equal(
            VillageCatalog.MoonRuneWorkbenchCell.Y + 1,
            atWork.Position.Y
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
        Assert.False(session.InspectMoonRuneWorkbench().Succeeded);
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
            "target.action.inspect_workbench",
            workbench.LabelKey
        );
        Assert.True(exit.IsAvailable);
        Assert.True(session.InspectMoonRuneWorkbench().Succeeded);
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
    public void VillageLandmarksHaveStableAtlasAndPassableGate()
    {
        Assert.Equal(8, VillageCatalog.Landmarks.Count);
        Assert.Equal(
            8,
            VillageCatalog.Landmarks
                .Select(landmark => landmark.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.Equal(
            Enumerable.Range(0, 8),
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
        var restoredSession = new GameSession();
        restoredSession.Restore(result.Save);
        Assert.Equal(
            DataCatalog.MoonstonePathId,
            restoredSession.FarmObjects.ItemAt(pathPosition)
        );
        Assert.Equal(
            DataCatalog.PlantStarbudCommissionId,
            result.Save.Commission.DefinitionId
        );
        Assert.True(result.Save.Commission.Accepted);
        Assert.Equal(1, result.Save.Commission.Progress);
        Assert.False(result.Save.Commission.Claimed);
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
        Assert.Equal(5, result.Save.Inventory.Sum(slot =>
            slot.ItemId == DataCatalog.StarbudSeedId ? slot.Count : 0));
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
