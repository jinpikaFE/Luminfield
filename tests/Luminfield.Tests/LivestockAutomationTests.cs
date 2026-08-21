using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class LivestockAutomationTests
{
    [Fact]
    public void CatalogFreezesHubCostPrerequisitesAndCapacities()
    {
        var project = ConstructionCatalog.HomesteadLivestockAutomation;

        Assert.Equal("homestead_livestock_automation", project.Id);
        Assert.Equal(900, project.CoinCost);
        Assert.Equal(4, project.RequiredNights);
        Assert.Equal([24, 16], project.Materials
            .Select(material => material.Count)
            .ToArray());
        Assert.Equal(
            [
                ConstructionCatalog.HomesteadWorkshopProjectId,
                ConstructionCatalog.HomesteadStarfeatherCoopProjectId,
                ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
            ],
            project.RequiredProjectIds
        );
        Assert.Equal(28, AnimalSystem.AutomationFeedCapacity);
        Assert.Equal(12, AnimalSystem.AutomationProductCapacity);
    }

    [Fact]
    public void ConstructionRequiresAllThreeCompletedProjectsAtomically()
    {
        var session = AutomationSession(
            automationCompleted: false,
            includeWorkshop: false
        );
        var save = session.Capture();
        save.Coins = 900;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Inventory = [];
        session.Restore(save);
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 24));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 16));
        var before = JsonSerializer.Serialize(session.Capture());

        var blocked = session.StartConstruction(
            ConstructionCatalog.HomesteadLivestockAutomationProjectId
        );

        Assert.False(blocked.Succeeded);
        Assert.Equal(
            "construction.homestead_livestock_automation.requires_buildings",
            blocked.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void NormalizeCreatesIndependentEmptyBuffersAndClampsCorruption()
    {
        var normalized = AnimalSystem.NormalizeSave(new AnimalSave
        {
            Automation =
            [
                new AnimalBuildingAutomationSave
                {
                    BuildingId = AnimalCatalog.StarfeatherCoopId,
                    StoredFeed = 99,
                    StoredProducts =
                    [
                        new ShippingEntrySave
                        {
                            ItemId = DataCatalog.StarfeatherEggId,
                            Count = 20
                        },
                        new ShippingEntrySave
                        {
                            ItemId = DataCatalog.MoonfleeceId,
                            Count = 1
                        }
                    ]
                },
                new AnimalBuildingAutomationSave
                {
                    BuildingId = AnimalCatalog.StarfeatherCoopId,
                    StoredFeed = 1
                },
                new AnimalBuildingAutomationSave
                {
                    BuildingId = "removed_barn",
                    StoredFeed = 28
                }
            ]
        }, 7);

        Assert.Equal(2, normalized.Automation.Count);
        var coop = normalized.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.StarfeatherCoopId
        );
        Assert.Equal(28, coop.StoredFeed);
        Assert.Equal(
            12,
            Assert.Single(coop.StoredProducts).Count
        );
        var barn = normalized.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.MoonfleeceBarnId
        );
        Assert.Equal(0, barn.StoredFeed);
        Assert.Empty(barn.StoredProducts);
    }

    [Fact]
    public void FeedDepositAndWithdrawalAreCapacityAndBackpackAtomic()
    {
        var session = AutomationSession();
        SetAtAutomationConsole(session, AnimalCatalog.StarfeatherCoopId);
        Assert.True(session.Inventory.Add(DataCatalog.MeadowFodderId, 28));

        Assert.True(session.DepositAnimalAutomationFeed(
            AnimalCatalog.StarfeatherCoopId,
            StarfeatherCoopLayout.AutomationStationCell,
            28
        ).Succeeded);
        Assert.Equal(
            28,
            session.AnimalAutomationFor(
                AnimalCatalog.StarfeatherCoopId
            ).StoredFeed
        );
        var before = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "animal.feed.insufficient_fodder",
            session.DepositAnimalAutomationFeed(
                AnimalCatalog.StarfeatherCoopId,
                StarfeatherCoopLayout.AutomationStationCell,
                1
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        Assert.True(session.WithdrawAnimalAutomationFeed(
            AnimalCatalog.StarfeatherCoopId,
            StarfeatherCoopLayout.AutomationStationCell,
            28
        ).Succeeded);
        Assert.Equal(28, session.Inventory.Count(DataCatalog.MeadowFodderId));
        Assert.Equal(
            0,
            session.AnimalAutomationFor(
                AnimalCatalog.StarfeatherCoopId
            ).StoredFeed
        );
    }

    [Fact]
    public void AutomaticFeedIsAllOrNothingPerBuilding()
    {
        var animals = new AnimalSystem();
        animals.Restore(new AnimalSave
        {
            Animals = [Sheep(), Dewhorn()],
            Automation =
            [
                new AnimalBuildingAutomationSave
                {
                    BuildingId = AnimalCatalog.MoonfleeceBarnId,
                    StoredFeed = 1
                }
            ]
        }, 43);
        animals.BeginAutomationNight(AnimalCatalog.MoonfleeceBarnId, 43);
        var before = JsonSerializer.Serialize(animals.Capture());

        var resolution = animals.ResolveAutomaticFeed(
            AnimalCatalog.MoonfleeceBarnId,
            43,
            new HashSet<string>(StringComparer.Ordinal)
        );

        Assert.False(resolution.Succeeded);
        Assert.Equal(AnimalAutomationStatusIds.InsufficientFeed,
            resolution.StatusId);
        Assert.All(
            animals.AnimalsInBuilding(AnimalCatalog.MoonfleeceBarnId),
            animal => Assert.NotEqual(43, animal.LastFedDay)
        );
        Assert.Equal(
            1,
            animals.AutomationFor(
                AnimalCatalog.MoonfleeceBarnId
            ).StoredFeed
        );
        Assert.NotEqual(before, JsonSerializer.Serialize(animals.Capture()));
    }

    [Fact]
    public void BarnProductsMoveAsOneBatchOrRemainPendingTogether()
    {
        var animals = new AnimalSystem();
        animals.Restore(new AnimalSave
        {
            Animals =
            [
                Sheep(DataCatalog.MoonfleeceLuminousId),
                Dewhorn(DataCatalog.DewhornMilkStarlightId)
            ],
            Automation =
            [
                new AnimalBuildingAutomationSave
                {
                    BuildingId = AnimalCatalog.MoonfleeceBarnId,
                    StoredProducts =
                    [
                        new ShippingEntrySave
                        {
                            ItemId = DataCatalog.MoonfleeceId,
                            Count = 11
                        }
                    ]
                }
            ]
        }, 29);

        var blocked = animals.ResolveAutomaticCollection(
            AnimalCatalog.MoonfleeceBarnId
        );

        Assert.False(blocked.Succeeded);
        Assert.Equal(11, animals.AutomationFor(
            AnimalCatalog.MoonfleeceBarnId
        ).StoredProductCount);
        Assert.All(
            animals.AnimalsInBuilding(AnimalCatalog.MoonfleeceBarnId),
            animal => Assert.True(animal.HasPendingProduct)
        );

        var save = animals.Capture();
        save.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.MoonfleeceBarnId
        ).StoredProducts[0].Count = 10;
        animals.Restore(save, 29);
        var moved = animals.ResolveAutomaticCollection(
            AnimalCatalog.MoonfleeceBarnId
        );
        Assert.True(moved.Succeeded);
        Assert.Equal(2, moved.Count);
        Assert.Equal(12, animals.AutomationFor(
            AnimalCatalog.MoonfleeceBarnId
        ).StoredProductCount);
        Assert.All(
            animals.AnimalsInBuilding(AnimalCatalog.MoonfleeceBarnId),
            animal => Assert.False(animal.HasPendingProduct)
        );
    }

    [Fact]
    public void EndDayFeedsProducesAndCollectsWithoutPassiveExperience()
    {
        var session = AutomationSession(
            animals:
            [
                Chicken(age: 2, progress: 1)
            ],
            weatherId: DataCatalog.RainWeatherId,
            day: 15
        );
        var save = session.Capture();
        save.Animals.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.StarfeatherCoopId
        ).StoredFeed = 1;
        session.Restore(save);
        var experience = session.FarmingSkill.Experience;

        session.EndDay();

        var automation = session.AnimalAutomationFor(
            AnimalCatalog.StarfeatherCoopId
        );
        Assert.Equal(0, automation.StoredFeed);
        Assert.Equal(1, automation.StoredProductCount);
        Assert.Equal(DataCatalog.StarfeatherEggId,
            Assert.Single(automation.StoredProducts).ItemId);
        Assert.False(session.Animals.Animal(
            AnimalCatalog.StarterStarfeatherChickenId
        )!.HasPendingProduct);
        Assert.Equal(experience, session.FarmingSkill.Experience);
    }

    [Fact]
    public void CompletionNightDoesNotRetroactivelyFeedOrCollect()
    {
        var session = AutomationSession(
            automationCompleted: false,
            animals: [Chicken(age: 2, progress: 1)],
            weatherId: DataCatalog.LongnightSnowWeatherId,
            day: 43
        );
        var save = session.Capture();
        save.Construction.Projects.Add(new ConstructionProjectSave
        {
            ProjectId = ConstructionCatalog
                .HomesteadLivestockAutomationProjectId,
            RemainingNights = 1
        });
        save.Animals.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.StarfeatherCoopId
        ).StoredFeed = 1;
        session.Restore(save);

        session.EndDay();

        Assert.True(session.LivestockAutomationUnlocked);
        Assert.Equal(1, session.AnimalAutomationFor(
            AnimalCatalog.StarfeatherCoopId
        ).StoredFeed);
        Assert.Empty(session.AnimalAutomationFor(
            AnimalCatalog.StarfeatherCoopId
        ).StoredProducts);
        Assert.False(session.Animals.Animal(
            AnimalCatalog.StarterStarfeatherChickenId
        )!.HasPendingProduct);

        session.EndDay();
        Assert.Equal(0, session.AnimalAutomationFor(
            AnimalCatalog.StarfeatherCoopId
        ).StoredFeed);
        Assert.Single(session.AnimalAutomationFor(
            AnimalCatalog.StarfeatherCoopId
        ).StoredProducts);
    }

    [Fact]
    public void FullBackpackKeepsTheEntireAutomationBuffer()
    {
        var session = AutomationSession();
        var save = session.Capture();
        save.Animals.Automation.Single(entry =>
            entry.BuildingId == AnimalCatalog.MoonfleeceBarnId
        ).StoredProducts =
        [
            new ShippingEntrySave
            {
                ItemId = DataCatalog.MoonfleeceLuminousId,
                Count = 1
            },
            new ShippingEntrySave
            {
                ItemId = DataCatalog.DewhornMilkStarlightId,
                Count = 1
            }
        ];
        session.Restore(save);
        SetAtAutomationConsole(session, AnimalCatalog.MoonfleeceBarnId);
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId => itemId != DataCatalog.MoonfleeceLuminousId)
                     .Where(itemId => itemId != DataCatalog.DewhornMilkStarlightId)
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        var before = JsonSerializer.Serialize(session.Capture());

        var blocked = session.CollectAnimalAutomationProducts(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        );

        Assert.Equal("notice.inventory_full", blocked.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void PreviewAndActionBindToTheRealConsoleAndHand()
    {
        var session = AutomationSession();
        SetAtAutomationConsole(session, AnimalCatalog.MoonfleeceBarnId);

        var preview = session.PreviewSelectedTarget(
            MoonfleeceBarnLayout.AutomationStationCell
        );
        Assert.Equal(TargetPreviewKind.AnimalAutomationStation, preview.Kind);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.True(session.OpenAnimalAutomationStation(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        ).Succeeded);

        session.Inventory.Select(1);
        preview = session.PreviewSelectedTarget(
            MoonfleeceBarnLayout.AutomationStationCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, preview.State);
        Assert.Equal("notice.needs_hand", session.OpenAnimalAutomationStation(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        ).MessageKey);
    }

    private static GameSession AutomationSession(
        bool automationCompleted = true,
        bool includeWorkshop = true,
        IReadOnlyList<AnimalEntrySave>? animals = null,
        string weatherId = DataCatalog.LongnightSnowWeatherId,
        int day = 43
    )
    {
        var session = new GameSession();
        session.NewGame();
        var projects = new List<ConstructionProjectSave>();
        if (includeWorkshop)
        {
            projects.Add(Completed(
                ConstructionCatalog.HomesteadWorkshopProjectId
            ));
        }
        projects.Add(Completed(
            ConstructionCatalog.HomesteadStarfeatherCoopProjectId
        ));
        projects.Add(Completed(
            ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
        ));
        if (automationCompleted)
        {
            projects.Add(Completed(
                ConstructionCatalog.HomesteadLivestockAutomationProjectId
            ));
        }

        var save = session.Capture();
        save.Day = day;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = weatherId
        };
        save.Construction = new ConstructionSave { Projects = projects };
        save.Animals = new AnimalSave
        {
            Animals = (animals ?? [Chicken(), Sheep(), Dewhorn()]).ToList()
        };
        session.Restore(save);
        return session;
    }

    private static void SetAtAutomationConsole(
        GameSession session,
        string buildingId
    )
    {
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        session.SetPlayerLocation(
            spatial.AutomationStationCell.X * 16 + 8,
            (spatial.AutomationStationCell.Y + 1) * 16 + 8,
            spatial.LocationId
        );
        session.Inventory.Select(0);
    }

    private static ConstructionProjectSave Completed(string projectId) =>
        new() { ProjectId = projectId, Completed = true };

    private static AnimalEntrySave Chicken(
        int age = 0,
        int progress = 0,
        string pending = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterStarfeatherChickenId,
        SpeciesId = AnimalCatalog.StarfeatherChickenId,
        BuildingId = AnimalCatalog.StarfeatherCoopId,
        AgeNights = age,
        Mood = AnimalSystem.InitialMood,
        ProductionProgress = progress,
        PendingProductItemId = pending
    };

    private static AnimalEntrySave Sheep(string pending = "") => new()
    {
        InstanceId = AnimalCatalog.StarterMoonfleeceSheepId,
        SpeciesId = AnimalCatalog.MoonfleeceSheepId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        Mood = AnimalSystem.InitialMood,
        PendingProductItemId = pending
    };

    private static AnimalEntrySave Dewhorn(string pending = "") => new()
    {
        InstanceId = AnimalCatalog.StarterDewhornId,
        SpeciesId = AnimalCatalog.DewhornId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        Mood = AnimalSystem.InitialMood,
        PendingProductItemId = pending
    };
}
