using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class ForageSystemTests
{
    [Fact]
    public void CatalogFreezesEightSeasonalSellableStorableEntries()
    {
        Assert.Equal(8, ForageCatalog.Definitions.Count);
        Assert.Equal(
            8,
            ForageCatalog.Definitions
                .Select(definition => definition.ItemId)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(
            new[]
            {
                CalendarSystem.GleamriseSeasonId,
                CalendarSystem.RainveilSeasonId,
                CalendarSystem.StarharvestSeasonId,
                CalendarSystem.LongnightSeasonId
            },
            seasonId => Assert.Equal(
                2,
                ForageCatalog.Definitions.Count(definition =>
                    definition.SeasonId == seasonId
                )
            )
        );
        Assert.Equal(
            ForageCatalog.Definitions.Select(definition => definition.ItemId),
            CompendiumCatalog.ForageEntries.Select(entry => entry.Id)
        );
        Assert.All(ForageCatalog.Definitions, definition =>
        {
            var item = DataCatalog.Item(definition.ItemId);
            Assert.Equal(ItemKind.Resource, item.Kind);
            Assert.Equal(99, item.MaxStack);
            Assert.Equal(definition.SellPrice, item.SellPrice);
            Assert.Contains(definition.ItemId, DataCatalog.SellableItemIds);
            Assert.Contains(definition.ItemId, DataCatalog.StorableItemIds);
        });
    }

    [Theory]
    [InlineData(1, "whisperbloom", "dewglass_clover")]
    [InlineData(14, "whisperbloom", "dewglass_clover")]
    [InlineData(15, "rainbell_moss", "mistcoil_fern")]
    [InlineData(29, "gloamgold_berry", "sunwisp_pod")]
    [InlineData(43, "nightlamp_lichen", "frostwick_root")]
    [InlineData(57, "whisperbloom", "dewglass_clover")]
    public void AbsoluteDaySelectsTwoSeasonEntries(
        int day,
        string woodsItemId,
        string meadowItemId
    )
    {
        var spawns = ForageSystem.Generate(day, DataCatalog.ClearWeatherId);

        Assert.Equal(2, spawns.Count);
        Assert.Equal(
            woodsItemId,
            Assert.Single(spawns, spawn =>
                spawn.SlotId == ForageCatalog.WoodsSlotOneId
            ).ItemId
        );
        Assert.Equal(
            meadowItemId,
            Assert.Single(spawns, spawn =>
                spawn.SlotId == ForageCatalog.MeadowSlotOneId
            ).ItemId
        );
    }

    [Theory]
    [InlineData("clear", 2)]
    [InlineData("rain", 2)]
    [InlineData("longnight_snow", 2)]
    [InlineData("stardust_wind", 4)]
    public void StardustWindAddsOneSlotPerRegion(
        string weatherId,
        int expectedCount
    )
    {
        var spawns = ForageSystem.Generate(29, weatherId);

        Assert.Equal(expectedCount, spawns.Count);
        Assert.Equal(expectedCount, spawns.Select(spawn => spawn.Cell).Distinct().Count());
        Assert.All(spawns, spawn =>
        {
            var definition = ForageCatalog.ByItemId[spawn.ItemId];
            Assert.Equal(definition.Biome, WorldDefinition.GetBiome(spawn.Cell));
            Assert.True(ForageSystem.IsCandidate(spawn.Cell, definition.Biome));
        });
        Assert.All(
            spawns.SelectMany((left, index) =>
                spawns.Skip(index + 1).Select(right => (left, right))
            ),
            pair => Assert.True(
                Distance(pair.left.Cell, pair.right.Cell) >= 2
            )
        );
    }

    [Fact]
    public void GenerationIsDeterministicForTheSameDayAndWeather()
    {
        var first = ForageSystem.Generate(29, DataCatalog.StardustWindWeatherId);
        var second = ForageSystem.Generate(29, DataCatalog.StardustWindWeatherId);

        Assert.Equal(first, second);
        Assert.NotEqual(
            first.Select(spawn => spawn.Cell),
            ForageSystem.Generate(30, DataCatalog.StardustWindWeatherId)
                .Select(spawn => spawn.Cell)
        );
    }

    [Fact]
    public void HandCollectionIsAtomicAndDiscoversTheEntry()
    {
        var session = NewSession();
        var spawn = session.Forage.ActiveSpawns[0];
        PositionBeside(session, spawn.Cell);
        var energy = session.Energy;

        var preview = session.PreviewSelectedTarget(spawn.Cell);
        var result = session.UseSelected(spawn.Cell);

        Assert.Equal(TargetPreviewKind.Forage, preview.Kind);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.True(result.Succeeded);
        Assert.Equal(1, session.Inventory.Count(spawn.ItemId));
        Assert.Equal(energy, session.Energy);
        Assert.Null(session.Forage.SpawnAt(spawn.Cell));
        Assert.True(session.Collection.IsDiscovered(spawn.ItemId));
        Assert.False(session.UseSelected(spawn.Cell).Succeeded);
    }

    [Fact]
    public void WrongToolAndDistanceFailuresDoNotMutateState()
    {
        var session = NewSession();
        var spawn = session.Forage.ActiveSpawns[0];
        PositionBeside(session, spawn.Cell);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var preview = session.PreviewSelectedTarget(spawn.Cell);
        var wrongTool = session.UseSelected(spawn.Cell);

        Assert.Equal(TargetPreviewState.NeedsTool, preview.State);
        Assert.False(wrongTool.Succeeded);
        Assert.Equal("notice.needs_hand", wrongTool.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        session.Inventory.Select(0);
        session.SetPlayerLocation(20 * 16 + 8, 20 * 16 + 8, PlayerLocationIds.World);
        before = JsonSerializer.Serialize(session.Capture());
        Assert.False(session.UseSelected(spawn.Cell).Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void FullBackpackFailureKeepsSpawnAndCollectionUnchanged()
    {
        var session = NewSession();
        var spawn = session.Forage.ActiveSpawns[0];
        var save = session.Capture();
        FillInventory(save.Inventory, spawn.ItemId);
        session.Restore(Normalize(save));
        spawn = session.Forage.ActiveSpawns.Single(candidate =>
            candidate.SlotId == spawn.SlotId
        );
        PositionBeside(session, spawn.Cell);
        var before = JsonSerializer.Serialize(session.Capture());

        var preview = session.PreviewSelectedTarget(spawn.Cell);
        var result = session.UseSelected(spawn.Cell);

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.False(result.Succeeded);
        Assert.Equal("notice.inventory_full", result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
        Assert.NotNull(session.Forage.SpawnAt(spawn.Cell));
        Assert.False(session.Collection.IsDiscovered(spawn.ItemId));
    }

    [Fact]
    public void SameDayRoundTripKeepsPositionsAndCollectedState()
    {
        var session = NewSession();
        var collected = session.Forage.ActiveSpawns[0];
        PositionBeside(session, collected.Cell);
        Assert.True(session.UseSelected(collected.Cell).Succeeded);
        var save = session.Capture();

        var restored = new GameSession();
        restored.Restore(Normalize(save));

        Assert.Equal(save.Forage.ResolvedDay, restored.Forage.ResolvedDay);
        Assert.Equal(
            save.Forage.Spawns.Select(SpawnSignature),
            restored.Forage.Capture().Spawns.Select(SpawnSignature)
        );
        Assert.Null(restored.Forage.SpawnAt(collected.Cell));
    }

    [Fact]
    public void CrossDaySaveDropsOldSlotsAndUnknownEntries()
    {
        var save = new GameSaveV1
        {
            Day = 15,
            Weather = new WeatherSave
            {
                Day = 15,
                CurrentId = DataCatalog.ClearWeatherId,
                ForecastId = DataCatalog.ClearWeatherId
            },
            Forage = new ForageSave
            {
                ResolvedDay = 14,
                Spawns =
                [
                    new ForageSpawnSave
                    {
                        SlotId = "unknown",
                        ItemId = "unknown",
                        X = 1,
                        Y = 1,
                        Collected = true
                    }
                ]
            }
        };

        var normalized = Normalize(save);

        Assert.Equal(15, normalized.Forage.ResolvedDay);
        Assert.Equal(2, normalized.Forage.Spawns.Count);
        Assert.All(normalized.Forage.Spawns, spawn =>
        {
            Assert.False(spawn.Collected);
            Assert.Contains(spawn.ItemId, new[]
            {
                DataCatalog.RainbellMossId,
                DataCatalog.MistcoilFernId
            });
        });
    }

    [Fact]
    public void CompleteForageCodexClaimsMapRewardOnce()
    {
        var session = NewSession();
        foreach (var entry in CompendiumCatalog.ForageEntries)
        {
            Assert.True(session.Collection.RecordObtainedItem(entry.ItemId));
        }
        session.SetPlayerLocation(
            20 * 16 + 8,
            12 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );

        Assert.True(session.ClaimCollectionReward(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionRewardIds.StarpathForagersGuide
        ).Succeeded);
        Assert.True(session.ForageMapUnlocked);
        Assert.False(session.ClaimCollectionReward(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionRewardIds.StarpathForagersGuide
        ).Succeeded);
    }

    private static GameSession NewSession()
    {
        var session = new GameSession();
        session.NewGame();
        session.Inventory.Select(0);
        return session;
    }

    private static void PositionBeside(GameSession session, GridPosition target)
    {
        var approach = new[]
        {
            new GridPosition(target.X, target.Y - 1),
            new GridPosition(target.X + 1, target.Y),
            new GridPosition(target.X, target.Y + 1),
            new GridPosition(target.X - 1, target.Y)
        }.First(cell => !WorldDefinition.IsBlocked(cell));
        session.SetPlayerLocation(
            approach.X * 16 + 8,
            approach.Y * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private static void FillInventory(
        List<InventorySlot> inventory,
        string excludedItemId
    )
    {
        inventory.Clear();
        inventory.AddRange(new[]
        {
            DataCatalog.HandId,
            DataCatalog.ShovelId,
            DataCatalog.MacheteId,
            DataCatalog.WateringCanId,
            DataCatalog.BucketId,
            DataCatalog.FishingRodId
        }.Select(itemId => new InventorySlot { ItemId = itemId, Count = 1 }));
        inventory.AddRange(DataCatalog.StorableItemIds
            .Where(itemId => itemId != excludedItemId)
            .Distinct(StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .Select(itemId => new InventorySlot
            {
                ItemId = itemId,
                Count = DataCatalog.Item(itemId).MaxStack
            }));
        Assert.Equal(Inventory.SlotCount, inventory.Count);
    }

    private static GameSaveV1 Normalize(GameSaveV1 save)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-forage-{Guid.NewGuid():N}"
        );
        var path = Path.Combine(directory, "slot.json");
        try
        {
            var service = new SaveService(path);
            service.Save(save);
            var loaded = service.Load();
            Assert.Equal(SaveLoadStatus.Loaded, loaded.Status);
            return Assert.IsType<GameSaveV1>(loaded.Save);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static string SpawnSignature(ForageSpawnSave spawn) =>
        $"{spawn.SlotId}:{spawn.ItemId}:{spawn.X}:{spawn.Y}:{spawn.Collected}";

    private static int Distance(GridPosition left, GridPosition right) =>
        Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
}
