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

public sealed class InventoryTests
{
    [Fact]
    public void StacksItemsAndRejectsAnOverflowingFullHotbar()
    {
        var inventory = new Inventory();
        inventory.Reset();

        Assert.True(inventory.Add(DataCatalog.StarbudSeedId, 99 * 6));
        Assert.Equal(99 * 6, inventory.Count(DataCatalog.StarbudSeedId));
        Assert.False(inventory.Add(DataCatalog.StarbudSeedId, 1));
        Assert.Equal(99 * 6, inventory.Count(DataCatalog.StarbudSeedId));
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

        session.Inventory.Select(0);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(QuestStage.Plant, session.Quest.Stage);

        session.Inventory.Select(2);
        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(QuestStage.Water, session.Quest.Stage);

        session.Inventory.Select(1);
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

        foreach (var position in positions)
        {
            Assert.True(session.UseSelected(position).Succeeded);
        }
        Assert.Equal(3, session.Inventory.Count(DataCatalog.StarbudId));
        Assert.Equal(QuestStage.ReturnToMira, session.Quest.Stage);

        Assert.False(session.InteractWithMira());
        Assert.Equal(QuestStage.Complete, session.Quest.Stage);
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
        session.Inventory.Select(0);
        session.UseSelected(new GridPosition(12, 16));
        session.SetPlayerState(128, 256, false);

        service.Save(session.Capture());
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(LocaleService.English, result.Save.Locale);
        Assert.Equal(128, result.Save.Player.X);
        Assert.Single(result.Save.FarmTiles);
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
}
