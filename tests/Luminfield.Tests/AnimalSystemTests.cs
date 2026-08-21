using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class AnimalSystemTests
{
    [Fact]
    public void CatalogFreezesCoopFeedAndQualityProductContracts()
    {
        var project = ConstructionCatalog.HomesteadStarfeatherCoop;
        Assert.Equal("homestead_starfeather_coop", project.Id);
        Assert.Equal(420, project.CoinCost);
        Assert.Equal(3, project.RequiredNights);
        Assert.Equal(
            [18, 6],
            project.Materials.Select(material => material.Count).ToArray()
        );
        Assert.Contains(project, ConstructionCatalog.Projects);

        Assert.Equal(8, DataCatalog.Item(DataCatalog.MeadowFodderId).BuyPrice);
        Assert.Equal(
            [48, 72, 108],
            DataCatalog.ItemFamilyIds(DataCatalog.StarfeatherEggId)
                .Select(itemId => DataCatalog.Item(itemId).SellPrice)
                .ToArray()
        );
        Assert.All(
            DataCatalog.ItemFamilyIds(DataCatalog.StarfeatherEggId),
            itemId => Assert.Contains(itemId, DataCatalog.StorableItemIds)
        );
    }

    [Fact]
    public void AnimalSaveNormalizationIsAdditiveDeterministicAndBounded()
    {
        var normalized = AnimalSystem.NormalizeSave(
            new AnimalSave
            {
                Animals =
                [
                    Entry(
                        AnimalCatalog.StarterStarfeatherChickenId,
                        mood: 99,
                        age: 99,
                        fed: 99,
                        pending: "removed_product"
                    ),
                    Entry(
                        AnimalCatalog.StarterStarfeatherChickenId,
                        mood: 1
                    ),
                    Entry("future_unprojected_animal"),
                    new AnimalEntrySave
                    {
                        InstanceId = "unknown",
                        SpeciesId = "removed_species",
                        BuildingId = AnimalCatalog.StarfeatherCoopId
                    }
                ]
            },
            5
        );

        var starter = Assert.Single(normalized.Animals);
        Assert.Equal(
            AnimalCatalog.StarterStarfeatherChickenId,
            starter.InstanceId
        );
        Assert.Equal(AnimalSystem.MaximumMood, starter.Mood);
        Assert.Equal(2, starter.AgeNights);
        Assert.Equal(5, starter.LastFedDay);
        Assert.Empty(starter.PendingProductItemId);
    }

    [Fact]
    public void CoopConstructionStartsWithoutWorkshopAndGrantsStarterAfterThirdNight()
    {
        var session = PreparedConstructionSession();
        var before = JsonSerializer.Serialize(session.Capture());

        var started = session.StartConstruction(
            ConstructionCatalog.HomesteadStarfeatherCoopProjectId
        );

        Assert.True(started.Succeeded);
        Assert.NotEqual(before, JsonSerializer.Serialize(session.Capture()));
        Assert.Empty(session.Animals.Animals);
        session.EndDay();
        session.EndDay();
        Assert.Empty(session.Animals.Animals);
        session.EndDay();
        var starter = Assert.Single(session.Animals.Animals);
        Assert.Equal(
            AnimalCatalog.StarterStarfeatherChickenId,
            starter.InstanceId
        );
        Assert.Equal(0, starter.AgeNights);
        Assert.Equal(AnimalSystem.InitialMood, starter.Mood);
    }

    [Fact]
    public void PreviewAndActionShareRealCoopDoorAndToolRules()
    {
        var session = CompletedCoopSession();
        session.SetPlayerLocation(
            FarmLayout.StarfeatherCoopReturnCell.X * 16 + 8,
            FarmLayout.StarfeatherCoopReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var hand = session.PreviewSelectedTarget(
            FarmLayout.StarfeatherCoopDoorCell
        );
        Assert.Equal(TargetPreviewKind.AnimalBuildingPortal, hand.Kind);
        Assert.Equal(TargetPreviewState.Available, hand.State);
        Assert.True(session.TryEnterStarfeatherCoop(
            FarmLayout.StarfeatherCoopDoorCell
        ).Succeeded);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            FarmLayout.StarfeatherCoopDoorCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal(
            "notice.needs_hand",
            session.TryEnterStarfeatherCoop(
                FarmLayout.StarfeatherCoopDoorCell
            ).MessageKey
        );
    }

    [Fact]
    public void CoopApproachAndPastureRejectNewPlacementButPreserveLegacyData()
    {
        var farm = new FarmSystem();
        var approach = FarmLayout.StarfeatherCoopReturnCell;
        Assert.True(FarmLayout.IsStarfeatherCoopApproachCell(approach));
        Assert.All(
            StarfeatherCoopLayout.WorldPastureCells.Append(approach),
            cell => Assert.True(
                FarmLayout.IsStarfeatherCoopProtectedCell(cell)
            )
        );

        var storage = new StorageSystem();
        Assert.Equal(
            ChestPlacementIssue.Blocked,
            storage.CheckPlacement(approach, farm)
        );
        storage.Restore(
            new StorageSave
            {
                Chests = [new PlacedChestSave
                {
                    X = approach.X,
                    Y = approach.Y
                }]
            },
            farm
        );
        Assert.True(storage.HasChest(approach));

        var emptyStorage = new StorageSystem();
        var farmObjects = new FarmObjectSystem();
        Assert.Equal(
            FarmObjectPlacementIssue.Blocked,
            farmObjects.CheckPlacement(
                DataCatalog.StarwoodFenceId,
                approach,
                farm,
                emptyStorage
            )
        );
        farmObjects.Restore(
            new FarmObjectSave
            {
                Objects = [new PlacedFarmObjectSave
                {
                    X = approach.X,
                    Y = approach.Y,
                    ItemId = DataCatalog.StarwoodFenceId
                }]
            },
            farm,
            emptyStorage
        );
        Assert.Equal(
            DataCatalog.StarwoodFenceId,
            farmObjects.ItemAt(approach)
        );

        var orchard = new OrchardSystem();
        Assert.Equal(
            OrchardPlacementIssue.Blocked,
            orchard.CheckTreePlacement(
                approach,
                farm,
                emptyStorage,
                new FarmObjectSystem()
            )
        );
        orchard.Restore(
            new OrchardSave
            {
                FruitTrees = [new FruitTreeSave
                {
                    X = approach.X,
                    Y = approach.Y,
                    TreeId = DataCatalog.MoonplumTreeId
                }]
            },
            farm,
            emptyStorage,
            new FarmObjectSystem()
        );
        Assert.True(orchard.HasFruitTree(approach));
    }

    [Fact]
    public void ExhaustedPastureKeepsChickenInsideAndRequiresFodder()
    {
        var save = CompletedCoopSave();
        save.MinuteOfDay = 9 * 60;
        save.Storage = new StorageSave
        {
            Chests = StarfeatherCoopLayout.WorldPastureCells
                .Select(cell => new PlacedChestSave
                {
                    X = cell.X,
                    Y = cell.Y
                })
                .ToList()
        };
        var session = new GameSession();
        session.NewGame();
        session.Restore(save);

        Assert.Null(session.StarfeatherChickenWorldCell);
        Assert.False(session.StarfeatherChickenCanGrazeToday);
        Assert.False(session.StarfeatherChickenIsOutdoors);
        Assert.Null(session.VisibleStarfeatherChickenCell);

        session.SetPlayerLocation(
            StarfeatherCoopLayout.FeedTroughCell.X * 16 + 8,
            (StarfeatherCoopLayout.FeedTroughCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarfeatherCoop
        );
        var before = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "animal.feed.insufficient_fodder",
            session.FeedStarfeatherCoop(
                StarfeatherCoopLayout.FeedTroughCell
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void ClearNonLongnightGrazingFeedsAutomaticallyButLongnightDoesNot()
    {
        Assert.True(AnimalSystem.CanGraze(1, DataCatalog.ClearWeatherId));
        Assert.False(AnimalSystem.CanGraze(1, DataCatalog.RainWeatherId));
        Assert.False(AnimalSystem.CanGraze(43, DataCatalog.ClearWeatherId));
        Assert.False(
            AnimalSystem.CanGraze(43, DataCatalog.LongnightSnowWeatherId)
        );

        var animals = new AnimalSystem();
        animals.EnsureStarterStarfeatherChicken();
        animals.ResolveNight(
            AnimalCatalog.StarfeatherCoopId,
            1,
            grazed: true
        );
        Assert.Equal(1, Assert.Single(animals.Animals).AgeNights);

        animals.ResolveNight(
            AnimalCatalog.StarfeatherCoopId,
            43,
            grazed: false
        );
        var sheltered = Assert.Single(animals.Animals);
        Assert.Equal(1, sheltered.AgeNights);
        Assert.Equal(1, sheltered.Mood);
    }

    [Fact]
    public void FeedingAndPettingAreDailyAtomicAndAwardCareExperience()
    {
        var session = CompletedCoopSession(
            day: 43,
            weatherId: DataCatalog.LongnightSnowWeatherId
        );
        Assert.True(session.Inventory.Add(DataCatalog.MeadowFodderId, 1));
        session.SetPlayerLocation(
            StarfeatherCoopLayout.FeedTroughCell.X * 16 + 8,
            (StarfeatherCoopLayout.FeedTroughCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarfeatherCoop
        );

        Assert.True(session.FeedStarfeatherCoop(
            StarfeatherCoopLayout.FeedTroughCell
        ).Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MeadowFodderId));
        Assert.Equal(1, session.FarmingSkill.Experience);
        var afterFeed = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "animal.feed.all_fed",
            session.FeedStarfeatherCoop(
                StarfeatherCoopLayout.FeedTroughCell
            ).MessageKey
        );
        Assert.Equal(afterFeed, JsonSerializer.Serialize(session.Capture()));

        session.SetPlayerLocation(
            StarfeatherCoopLayout.IndoorAnimalCell.X * 16 + 8,
            (StarfeatherCoopLayout.IndoorAnimalCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarfeatherCoop
        );
        Assert.Equal(
            "notice.nothing_to_interact",
            session.PetAnimal(
                "future_unprojected_animal",
                StarfeatherCoopLayout.IndoorAnimalCell
            ).MessageKey
        );
        Assert.True(session.PetAnimal(
            AnimalCatalog.StarterStarfeatherChickenId,
            StarfeatherCoopLayout.IndoorAnimalCell
        ).Succeeded);
        Assert.Equal(2, session.FarmingSkill.Experience);
        var afterPet = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "animal.pet.already_petted",
            session.PetAnimal(
                AnimalCatalog.StarterStarfeatherChickenId,
                StarfeatherCoopLayout.IndoorAnimalCell
            ).MessageKey
        );
        Assert.Equal(afterPet, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void FedAdultProducesEveryTwoNightsAndLocksQualityByMood()
    {
        var animals = new AnimalSystem();
        animals.Restore(
            new AnimalSave
            {
                Animals =
                [
                    Entry(
                        AnimalCatalog.StarterStarfeatherChickenId,
                        age: 2,
                        mood: 4,
                        fed: 1,
                        petted: 1
                    )
                ]
            },
            2
        );

        animals.ResolveNight(
            AnimalCatalog.StarfeatherCoopId,
            1,
            grazed: true
        );
        Assert.False(Assert.Single(animals.Animals).HasPendingProduct);
        animals.ResolveNight(
            AnimalCatalog.StarfeatherCoopId,
            2,
            grazed: true
        );
        var produced = Assert.Single(animals.Animals);
        Assert.Equal(5, produced.Mood);
        Assert.Equal(
            DataCatalog.StarfeatherEggStarlightId,
            produced.PendingProductItemId
        );

        animals.ResolveNight(
            AnimalCatalog.StarfeatherCoopId,
            3,
            grazed: true
        );
        Assert.Equal(
            DataCatalog.StarfeatherEggStarlightId,
            Assert.Single(animals.Animals).PendingProductItemId
        );
    }

    [Fact]
    public void FullBackpackDoesNotClearPendingEggAndSuccessfulCollectionDoes()
    {
        var session = CompletedCoopSession(
            animal: Entry(
                AnimalCatalog.StarterStarfeatherChickenId,
                age: 2,
                pending: DataCatalog.StarfeatherEggLuminousId
            )
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId => itemId != DataCatalog.StarfeatherEggLuminousId)
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        session.SetPlayerLocation(
            StarfeatherCoopLayout.NestCell.X * 16 + 8,
            (StarfeatherCoopLayout.NestCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarfeatherCoop
        );
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.Equal(
            "notice.inventory_full",
            session.CollectStarfeatherEggs(
                StarfeatherCoopLayout.NestCell
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        var save = session.Capture();
        save.Inventory = [];
        session.Restore(save);
        session.SetPlayerLocation(
            StarfeatherCoopLayout.NestCell.X * 16 + 8,
            (StarfeatherCoopLayout.NestCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarfeatherCoop
        );
        Assert.True(session.CollectStarfeatherEggs(
            StarfeatherCoopLayout.NestCell
        ).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.StarfeatherEggLuminousId)
        );
        Assert.False(Assert.Single(session.Animals.Animals).HasPendingProduct);
        Assert.Equal(4, session.FarmingSkill.Experience);
    }

    [Fact]
    public void CoopLocationFallsBackWhenUnbuiltAndRoundTripsWhenCompleted()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.StarfeatherCoop;
        save.Player.X = 8;
        save.Player.Y = 8;
        session.Restore(save);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        Assert.Equal(FarmLayout.StarfeatherCoopReturnCell, session.PlayerCell);

        save = CompletedCoopSave();
        save.Player.LocationId = PlayerLocationIds.StarfeatherCoop;
        save.Player.X = 8;
        save.Player.Y = 8;
        session.Restore(save);
        Assert.True(session.InsideStarfeatherCoop);
        Assert.Equal(StarfeatherCoopLayout.SafeArrivalCell, session.PlayerCell);
        Assert.Equal(
            JsonSerializer.Serialize(session.Capture().Animals),
            JsonSerializer.Serialize(
                AnimalSystem.NormalizeSave(session.Capture().Animals, 1)
            )
        );
    }

    [Fact]
    public void CompletedCoopAddsPermanentFodderWithoutReplacingRotatingSeeds()
    {
        var incomplete = new GameSession();
        incomplete.NewGame();
        Assert.Equal(4, incomplete.TwilightEmporiumItemIds().Count);
        Assert.DoesNotContain(
            DataCatalog.MeadowFodderId,
            incomplete.TwilightEmporiumItemIds()
        );

        var completed = CompletedCoopSession();
        Assert.Equal(
            TwilightEmporiumSystem.StockForDay(completed.Clock.Day),
            completed.TwilightEmporiumItemIds().Take(4)
        );
        Assert.Equal(
            DataCatalog.MeadowFodderId,
            completed.TwilightEmporiumItemIds()[^1]
        );
    }

    private static GameSession PreparedConstructionSession()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 18));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 6));
        var save = session.Capture();
        save.Coins = 420;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        session.Restore(save);
        return session;
    }

    private static GameSession CompletedCoopSession(
        int day = 1,
        string weatherId = DataCatalog.ClearWeatherId,
        AnimalEntrySave? animal = null
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = CompletedCoopSave(day, weatherId, animal);
        session.Restore(save);
        return session;
    }

    private static GameSaveV1 CompletedCoopSave(
        int day = 1,
        string weatherId = DataCatalog.ClearWeatherId,
        AnimalEntrySave? animal = null
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadStarfeatherCoopProjectId,
                    Completed = true
                }
            ]
        };
        save.Animals = new AnimalSave
        {
            Animals = animal is null ? [] : [animal]
        };
        return save;
    }

    private static AnimalEntrySave Entry(
        string instanceId,
        int mood = AnimalSystem.InitialMood,
        int age = 0,
        int fed = 0,
        int petted = 0,
        string pending = ""
    ) => new()
    {
        InstanceId = instanceId,
        SpeciesId = AnimalCatalog.StarfeatherChickenId,
        BuildingId = AnimalCatalog.StarfeatherCoopId,
        Mood = mood,
        AgeNights = age,
        LastFedDay = fed,
        LastPettedDay = petted,
        PendingProductItemId = pending
    };
}
