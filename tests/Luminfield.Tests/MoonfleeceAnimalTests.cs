using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class MoonfleeceAnimalTests
{
    [Fact]
    public void CatalogFreezesBarnSpeciesAndQualityProductContracts()
    {
        var project = ConstructionCatalog.HomesteadMoonfleeceBarn;
        Assert.Equal("homestead_moonfleece_barn", project.Id);
        Assert.Equal(780, project.CoinCost);
        Assert.Equal(4, project.RequiredNights);
        Assert.Equal(
            ConstructionCatalog.HomesteadStarfeatherCoopProjectId,
            project.RequiredProjectId
        );
        Assert.Equal(
            [30, 12],
            project.Materials.Select(material => material.Count).ToArray()
        );

        Assert.Equal(4, AnimalCatalog.MoonfleeceBarn.Capacity);
        Assert.Equal(3, AnimalCatalog.MoonfleeceSheep.AdultAfterFedNights);
        Assert.Equal(3, AnimalCatalog.MoonfleeceSheep.ProductAfterFedNights);
        Assert.Equal(
            [84, 126, 189],
            DataCatalog.ItemFamilyIds(DataCatalog.MoonfleeceId)
                .Select(itemId => DataCatalog.Item(itemId).SellPrice)
                .ToArray()
        );
        Assert.All(
            DataCatalog.ItemFamilyIds(DataCatalog.MoonfleeceId),
            itemId =>
            {
                Assert.Contains(itemId, DataCatalog.StorableItemIds);
                Assert.Contains(itemId, DataCatalog.SellableItemIds);
            }
        );
    }

    [Fact]
    public void BarnConstructionRequiresCompletedCoopAndStartsAtomically()
    {
        var session = PreparedBarnConstructionSession(coopCompleted: false);
        var before = JsonSerializer.Serialize(session.Capture());

        var blocked = session.StartConstruction(
            ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
        );

        Assert.False(blocked.Succeeded);
        Assert.Equal(
            "construction.homestead_moonfleece_barn.requires_coop",
            blocked.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        session = PreparedBarnConstructionSession(coopCompleted: true);
        Assert.True(session.StartConstruction(
            ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
        ).Succeeded);
        Assert.Equal(
            ConstructionPhase.InProgress,
            session.Construction.PhaseFor(
                ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
            )
        );
        Assert.Equal(0, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
    }

    [Fact]
    public void CompletionNightRegistersJuvenileWithoutAdvancingIt()
    {
        var session = RestoredBarnSession(
            barnCompleted: false,
            barnRemainingNights: 1,
            sheep: null
        );

        session.EndDay();

        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
        ));
        var sheep = session.Animals.Animal(
            AnimalCatalog.StarterMoonfleeceSheepId
        );
        Assert.NotNull(sheep);
        Assert.Equal(0, sheep!.AgeNights);
        Assert.Equal(AnimalSystem.InitialMood, sheep.Mood);
    }

    [Fact]
    public void DoorPreviewActionAndSaveFallbackShareRealBarnContract()
    {
        var session = RestoredBarnSession(barnCompleted: true);
        session.SetPlayerLocation(
            FarmLayout.MoonfleeceBarnReturnCell.X * 16 + 8,
            FarmLayout.MoonfleeceBarnReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var preview = session.PreviewSelectedTarget(
            FarmLayout.MoonfleeceBarnDoorCell
        );
        Assert.Equal(TargetPreviewKind.MoonfleeceBarnPortal, preview.Kind);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.True(session.TryEnterAnimalBuilding(
            AnimalCatalog.MoonfleeceBarnId,
            FarmLayout.MoonfleeceBarnDoorCell
        ).Succeeded);

        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.MoonfleeceBarn;
        save.Construction = new ConstructionSave();
        session.Restore(save);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        Assert.Equal(FarmLayout.MoonfleeceBarnReturnCell, session.PlayerCell);
    }

    [Fact]
    public void ClearDayProjectsSheepToAvailablePastureAndBlockedPastureNeedsFeed()
    {
        var session = RestoredBarnSession(
            barnCompleted: true,
            day: 29,
            minuteOfDay: 9 * 60
        );
        session.SetPlayerLocation(
            FarmLayout.MoonfleeceBarnReturnCell.X * 16 + 8,
            FarmLayout.MoonfleeceBarnReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var projection = Assert.Single(
            session.VisibleAnimalProjections,
            candidate => candidate.InstanceId ==
                AnimalCatalog.StarterMoonfleeceSheepId
        );
        Assert.True(projection.IsOutdoors);
        Assert.Contains(
            projection.Cell,
            MoonfleeceBarnLayout.WorldPastureCells
        );
        Assert.NotEqual(
            session.VisibleAnimalProjections.Single(candidate =>
                candidate.InstanceId == AnimalCatalog.StarterDewhornId
            ).Cell,
            projection.Cell
        );

        var save = session.Capture();
        save.Storage = new StorageSave
        {
            Chests = MoonfleeceBarnLayout.WorldPastureCells.Select(cell =>
                new PlacedChestSave { X = cell.X, Y = cell.Y }
            ).ToList()
        };
        session.Restore(save);
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.FeedTroughCell.X * 16 + 8,
            (MoonfleeceBarnLayout.FeedTroughCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        Assert.DoesNotContain(
            session.VisibleAnimalProjections,
            candidate => candidate.IsOutdoors &&
                candidate.BuildingId == AnimalCatalog.MoonfleeceBarnId
        );
        Assert.Equal(
            "animal.feed.insufficient_fodder",
            session.CheckAnimalFeedTrough(
                AnimalCatalog.MoonfleeceBarnId,
                MoonfleeceBarnLayout.FeedTroughCell
            ).MessageKey
        );
    }

    [Fact]
    public void AdultCaredSheepProducesStarlightMoonfleeceAfterThreeNights()
    {
        var animals = new AnimalSystem();
        animals.Restore(new AnimalSave
        {
            Animals =
            [
                SheepEntry(age: 3, mood: 5, fed: 1, petted: 1)
            ]
        }, 1);
        var grazing = new HashSet<string>(StringComparer.Ordinal)
        {
            AnimalCatalog.StarterMoonfleeceSheepId
        };

        animals.ResolveNight(AnimalCatalog.MoonfleeceBarnId, 1, grazing);
        animals.ResolveNight(AnimalCatalog.MoonfleeceBarnId, 2, grazing);
        animals.ResolveNight(AnimalCatalog.MoonfleeceBarnId, 3, grazing);

        Assert.Equal(
            DataCatalog.MoonfleeceStarlightId,
            Assert.Single(animals.Animals).PendingProductItemId
        );
    }

    [Fact]
    public void FullBackpackDoesNotClearMoonfleeceAndSuccessClearsOnlyBarnFamily()
    {
        var session = RestoredBarnSession(
            barnCompleted: true,
            sheep: SheepEntry(
                age: 3,
                pending: DataCatalog.MoonfleeceLuminousId
            )
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId => itemId != DataCatalog.MoonfleeceLuminousId)
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.CollectionRackCell.X * 16 + 8,
            (MoonfleeceBarnLayout.CollectionRackCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.Equal(
            "notice.inventory_full",
            session.CollectAnimalProducts(
                AnimalCatalog.MoonfleeceBarnId,
                MoonfleeceBarnLayout.CollectionRackCell
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        var save = session.Capture();
        save.Inventory = [];
        session.Restore(save);
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.CollectionRackCell.X * 16 + 8,
            (MoonfleeceBarnLayout.CollectionRackCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        Assert.True(session.CollectAnimalProducts(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.CollectionRackCell
        ).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.MoonfleeceLuminousId)
        );
        Assert.False(session.Animals.Animal(
            AnimalCatalog.StarterMoonfleeceSheepId
        )!.HasPendingProduct);
        Assert.Equal(4, session.FarmingSkill.Experience);
    }

    [Fact]
    public void BarnApproachAndPastureAreProtectedForNewPlacements()
    {
        var farm = new FarmSystem();
        Assert.All(
            MoonfleeceBarnLayout.WorldPastureCells.Append(
                FarmLayout.MoonfleeceBarnReturnCell
            ),
            cell => Assert.True(
                FarmLayout.IsAnimalBuildingProtectedCell(cell)
            )
        );

        var storage = new StorageSystem();
        Assert.Equal(
            ChestPlacementIssue.Blocked,
            storage.CheckPlacement(FarmLayout.MoonfleeceBarnReturnCell, farm)
        );
        var orchard = new OrchardSystem();
        Assert.Equal(
            OrchardPlacementIssue.Blocked,
            orchard.CheckTreePlacement(
                MoonfleeceBarnLayout.WorldPastureCells[0],
                farm,
                storage,
                new FarmObjectSystem()
            )
        );
    }

    private static GameSession PreparedBarnConstructionSession(
        bool coopCompleted
    )
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 30));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 12));
        var save = session.Capture();
        save.Coins = 780;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        if (coopCompleted)
        {
            save.Construction.Projects.Add(new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadStarfeatherCoopProjectId,
                Completed = true
            });
        }
        session.Restore(save);
        return session;
    }

    private static GameSession RestoredBarnSession(
        bool barnCompleted,
        int barnRemainingNights = 0,
        AnimalEntrySave? sheep = null,
        int day = 1,
        int minuteOfDay = GameClock.StartMinute
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
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
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadMoonfleeceBarnProjectId,
                    Completed = barnCompleted,
                    RemainingNights = barnRemainingNights
                }
            ]
        };
        save.Animals = new AnimalSave
        {
            Animals = sheep is null ? [] : [sheep]
        };
        session.Restore(save);
        return session;
    }

    private static AnimalEntrySave SheepEntry(
        int age = 0,
        int mood = AnimalSystem.InitialMood,
        int fed = 0,
        int petted = 0,
        string pending = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterMoonfleeceSheepId,
        SpeciesId = AnimalCatalog.MoonfleeceSheepId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = age,
        Mood = mood,
        LastFedDay = fed,
        LastPettedDay = petted,
        PendingProductItemId = pending
    };
}
