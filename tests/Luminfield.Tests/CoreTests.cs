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
    public void ArtisanGoodsAreWorthMoreThanTheirRawIngredients()
    {
        var preserve = DataCatalog.Item(DataCatalog.StarbudPreserveId);
        var starbud = DataCatalog.Item(DataCatalog.StarbudId);
        var tonic = DataCatalog.Item(DataCatalog.MoonrootTonicId);
        var moonroot = DataCatalog.Item(DataCatalog.MoonrootId);

        Assert.True(preserve.SellPrice > starbud.SellPrice * 2);
        Assert.True(tonic.SellPrice > moonroot.SellPrice * 2);
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
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
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
        Assert.Equal(5, result.Save.Inventory.Sum(slot =>
            slot.ItemId == DataCatalog.StarbudSeedId ? slot.Count : 0));
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
