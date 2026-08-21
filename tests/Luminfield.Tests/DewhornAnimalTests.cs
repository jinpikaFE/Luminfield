using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class DewhornAnimalTests
{
    [Fact]
    public void CatalogFreezesThirdAnimalAndCondensedDewmilkFamily()
    {
        Assert.Equal("dewhorn", AnimalCatalog.Dewhorn.Id);
        Assert.Equal(
            AnimalCatalog.MoonfleeceBarnId,
            AnimalCatalog.Dewhorn.BuildingId
        );
        Assert.Equal(4, AnimalCatalog.Dewhorn.AdultAfterFedNights);
        Assert.Equal(2, AnimalCatalog.Dewhorn.ProductAfterFedNights);
        Assert.Equal(
            [96, 144, 216],
            DataCatalog.ItemFamilyIds(DataCatalog.DewhornMilkId)
                .Select(itemId => DataCatalog.Item(itemId).SellPrice)
                .ToArray()
        );
        Assert.Equal(
            [
                AnimalCatalog.StarterMoonfleeceSheepId,
                AnimalCatalog.StarterDewhornId
            ],
            AnimalCatalog.StartersForBuilding(AnimalCatalog.MoonfleeceBarnId)
                .Select(starter => starter.InstanceId)
                .ToArray()
        );
        Assert.All(
            DataCatalog.ItemFamilyIds(DataCatalog.DewhornMilkId),
            itemId =>
            {
                Assert.Contains(itemId, DataCatalog.StorableItemIds);
                Assert.Contains(itemId, DataCatalog.SellableItemIds);
            }
        );
    }

    [Fact]
    public void CompletedBarnRegistersBothFixedStartersWithoutSchemaChange()
    {
        var session = BarnSession([]);

        Assert.Equal(
            [
                AnimalCatalog.StarterDewhornId,
                AnimalCatalog.StarterMoonfleeceSheepId
            ],
            session.Animals.AnimalsInBuilding(AnimalCatalog.MoonfleeceBarnId)
                .Select(animal => animal.InstanceId)
                .ToArray()
        );
        Assert.Equal(2, SaveService.CurrentSchemaVersion);
        Assert.Equal(
            session.Animals.Capture().Animals.Select(entry => entry.InstanceId),
            AnimalSystem.NormalizeSave(
                    session.Capture().Animals,
                    session.Clock.Day
                )
                .Animals
                .Select(entry => entry.InstanceId)
        );
    }

    [Fact]
    public void BarnFeedRequiresOneFodderPerNonGrazingResidentAtomically()
    {
        var session = BarnSession(
            [DewhornEntry(), SheepEntry()],
            weatherId: DataCatalog.RainWeatherId
        );
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.FeedTroughCell.X * 16 + 8,
            (MoonfleeceBarnLayout.FeedTroughCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        Assert.True(session.Inventory.Add(DataCatalog.MeadowFodderId, 1));
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.Equal(
            "animal.feed.insufficient_fodder",
            session.FeedAnimalBuilding(
                AnimalCatalog.MoonfleeceBarnId,
                MoonfleeceBarnLayout.FeedTroughCell
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        Assert.True(session.Inventory.Add(DataCatalog.MeadowFodderId, 1));
        Assert.True(session.FeedAnimalBuilding(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.FeedTroughCell
        ).Succeeded);
        Assert.All(
            session.Animals.AnimalsInBuilding(AnimalCatalog.MoonfleeceBarnId),
            animal => Assert.Equal(session.Clock.Day, animal.LastFedDay)
        );
    }

    [Fact]
    public void AdultHappyDewhornProducesStarlightMilkAfterTwoCaredNights()
    {
        var animals = new AnimalSystem();
        animals.Restore(new AnimalSave
        {
            Animals = [DewhornEntry(age: 4, mood: 5)]
        }, 2);
        var grazing = new HashSet<string>(StringComparer.Ordinal)
        {
            AnimalCatalog.StarterDewhornId
        };

        animals.ResolveNight(AnimalCatalog.MoonfleeceBarnId, 1, grazing);
        animals.ResolveNight(AnimalCatalog.MoonfleeceBarnId, 2, grazing);

        Assert.Equal(
            DataCatalog.DewhornMilkStarlightId,
            Assert.Single(animals.Animals).PendingProductItemId
        );
    }

    [Fact]
    public void MilkStationCollectsOnlyDewhornFamilyAndIsCapacitySafe()
    {
        var session = BarnSession(
            [
                DewhornEntry(
                    age: 4,
                    pending: DataCatalog.DewhornMilkLuminousId
                ),
                SheepEntry(
                    age: 3,
                    pending: DataCatalog.MoonfleeceStarlightId
                )
            ],
            weatherId: DataCatalog.LongnightSnowWeatherId,
            day: 43
        );
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.MilkingStationCell.X * 16 + 8,
            (MoonfleeceBarnLayout.MilkingStationCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.DewhornMilkLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.Equal(
            "notice.inventory_full",
            session.CollectAnimalProducts(
                AnimalCatalog.MoonfleeceBarnId,
                MoonfleeceBarnLayout.MilkingStationCell
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        var save = session.Capture();
        save.Inventory = [];
        session.Restore(save);
        session.SetPlayerLocation(
            MoonfleeceBarnLayout.MilkingStationCell.X * 16 + 8,
            (MoonfleeceBarnLayout.MilkingStationCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        Assert.True(session.CollectAnimalProducts(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.MilkingStationCell
        ).Succeeded);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.DewhornMilkLuminousId)
        );
        Assert.False(session.Animals.Animal(
            AnimalCatalog.StarterDewhornId
        )!.HasPendingProduct);
        Assert.True(session.Animals.Animal(
            AnimalCatalog.StarterMoonfleeceSheepId
        )!.HasPendingProduct);
    }

    [Fact]
    public void InteriorPreviewBindsDewhornAndMilkStationToRealTargets()
    {
        var session = BarnSession(
            [DewhornEntry(), SheepEntry()],
            weatherId: DataCatalog.RainWeatherId
        );
        var dewhornCell = MoonfleeceBarnLayout.IndoorAnimalCells[0];
        session.SetPlayerLocation(
            dewhornCell.X * 16 + 8,
            (dewhornCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );

        var animal = session.PreviewSelectedTarget(dewhornCell);
        Assert.Equal(TargetPreviewKind.Dewhorn, animal.Kind);
        Assert.Equal(TargetPreviewState.Available, animal.State);

        session.SetPlayerLocation(
            MoonfleeceBarnLayout.MilkingStationCell.X * 16 + 8,
            (MoonfleeceBarnLayout.MilkingStationCell.Y + 1) * 16 + 8,
            PlayerLocationIds.MoonfleeceBarn
        );
        var station = session.PreviewSelectedTarget(
            MoonfleeceBarnLayout.MilkingStationCell
        );
        Assert.Equal(TargetPreviewKind.DewhornMilkingStation, station.Kind);
        Assert.Equal(TargetPreviewState.Blocked, station.State);
        Assert.Equal("target.status.dewhorn_milk_not_ready", station.LabelKey);
    }

    private static GameSession BarnSession(
        IReadOnlyList<AnimalEntrySave> animals,
        string weatherId = DataCatalog.ClearWeatherId,
        int day = 1
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
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadMoonfleeceBarnProjectId,
                    Completed = true
                }
            ]
        };
        save.Animals = new AnimalSave { Animals = animals.ToList() };
        session.Restore(save);
        return session;
    }

    private static AnimalEntrySave DewhornEntry(
        int age = 0,
        int mood = AnimalSystem.InitialMood,
        string pending = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterDewhornId,
        SpeciesId = AnimalCatalog.DewhornId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = age,
        Mood = mood,
        PendingProductItemId = pending
    };

    private static AnimalEntrySave SheepEntry(
        int age = 0,
        int mood = AnimalSystem.InitialMood,
        string pending = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterMoonfleeceSheepId,
        SpeciesId = AnimalCatalog.MoonfleeceSheepId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = age,
        Mood = mood,
        PendingProductItemId = pending
    };
}
