using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class GreenhouseCultivationTests
{
    [Fact]
    public void CatalogAndLayoutUseFrozenStableContract()
    {
        var project = ConstructionCatalog.HomesteadGreenhouse;
        Assert.Equal(
            "homestead_greenhouse",
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        Assert.Equal(720, project.CoinCost);
        Assert.Equal(4, project.RequiredNights);
        Assert.Collection(
            project.Materials,
            material =>
            {
                Assert.Equal(DataCatalog.LumenwoodId, material.ItemId);
                Assert.Equal(28, material.Count);
            },
            material =>
            {
                Assert.Equal(DataCatalog.CrystalShardId, material.ItemId);
                Assert.Equal(12, material.Count);
            }
        );
        Assert.Equal(7, ConstructionCatalog.Projects.Count);

        Assert.Equal(40, GreenhouseLayout.Width);
        Assert.Equal(22, GreenhouseLayout.Height);
        Assert.Equal(new GridPosition(7, 14), GreenhouseLayout.CisternCell);
        Assert.Equal(new GridPosition(20, 20), GreenhouseLayout.ExitCell);
        Assert.Equal(
            new GridPosition(20, 18),
            GreenhouseLayout.SafeArrivalCell
        );
        Assert.Equal(new GridPosition(38, 10), FarmLayout.GreenhouseDoorCell);
        Assert.Equal(
            new GridPosition(38, 11),
            FarmLayout.GreenhouseReturnCell
        );

        var expectedBeds = new HashSet<GridPosition>();
        AddBed(expectedBeds, 12, 14, 7, 8);
        AddBed(expectedBeds, 25, 27, 7, 8);
        AddBed(expectedBeds, 11, 13, 12, 13);
        AddBed(expectedBeds, 26, 28, 12, 13);
        Assert.Equal(24, GreenhouseLayout.PlantingCells.Count);
        Assert.True(expectedBeds.SetEquals(
            GreenhouseLayout.PlantingCells
        ));
        Assert.All(expectedBeds, cell =>
        {
            Assert.True(GreenhouseLayout.IsPlantingBed(cell));
            Assert.True(GreenhouseLayout.IsWalkable(cell));
        });

        Assert.Equal(
            CultivationZoneCatalog.GreenhouseId,
            CultivationZoneCatalog.Greenhouse.Id
        );
        Assert.True(
            CultivationZoneCatalog.Greenhouse.IgnoresSeasonRestrictions
        );
        Assert.False(
            CultivationZoneCatalog.Greenhouse.ReceivesOutdoorWeather
        );
    }

    [Fact]
    public void GreenhouseRequiresWorkshopAndConstructionConsumesExactlyOnce()
    {
        var blocked = PreparedConstructionSession(
            completedWorkshop: false
        );
        var beforeBlocked = Snapshot(blocked);
        var missingWorkshop = blocked.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        Assert.False(missingWorkshop.Succeeded);
        Assert.Equal(
            "construction.homestead_greenhouse.requires_workshop",
            missingWorkshop.MessageKey
        );
        Assert.Equal(beforeBlocked, Snapshot(blocked));

        var session = PreparedConstructionSession(completedWorkshop: true);
        var started = session.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(
            0,
            session.Inventory.Count(DataCatalog.CrystalShardId)
        );
        Assert.Equal(
            4,
            session.Construction.RemainingNightsFor(
                ConstructionCatalog.HomesteadGreenhouseProjectId
            )
        );

        for (var night = 3; night >= 0; night--)
        {
            session.EndDay();
            Assert.Equal(
                night,
                session.Construction.RemainingNightsFor(
                    ConstructionCatalog.HomesteadGreenhouseProjectId
                )
            );
        }

        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        ));
    }

    [Fact]
    public void PortalPreviewAndActionsShareCompletedProjectChecks()
    {
        var session = new GameSession();
        session.NewGame();
        SetAdjacentToExteriorDoor(session);

        var notStarted = session.PreviewSelectedTarget(
            FarmLayout.GreenhouseDoorCell
        );
        Assert.Equal(TargetPreviewState.Blocked, notStarted.State);
        Assert.Equal(TargetPreviewKind.GreenhousePortal, notStarted.Kind);
        Assert.Equal(
            "construction.homestead_greenhouse.not_started",
            notStarted.LabelKey
        );
        Assert.Equal(
            notStarted.LabelKey,
            session.TryEnterGreenhouse(
                FarmLayout.GreenhouseDoorCell
            ).MessageKey
        );

        RestoreGreenhousePhase(
            session,
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadGreenhouseProjectId,
                RemainingNights = 2
            }
        );
        SetAdjacentToExteriorDoor(session);
        var inProgress = session.PreviewSelectedTarget(
            FarmLayout.GreenhouseDoorCell
        );
        Assert.Equal(TargetPreviewState.Blocked, inProgress.State);
        Assert.Equal(
            "construction.homestead_greenhouse.in_progress",
            inProgress.LabelKey
        );

        RestoreGreenhousePhase(
            session,
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadGreenhouseProjectId,
                Completed = true
            }
        );
        SetAdjacentToExteriorDoor(session);
        var available = session.PreviewSelectedTarget(
            FarmLayout.GreenhouseDoorCell
        );
        Assert.True(available.IsAvailable);
        Assert.Equal(TargetPreviewKind.GreenhousePortal, available.Kind);
        Assert.Equal(
            "target.action.enter_greenhouse",
            available.LabelKey
        );
        Assert.True(session.TryEnterGreenhouse(
            FarmLayout.GreenhouseDoorCell
        ).Succeeded);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            FarmLayout.GreenhouseDoorCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal("target.need.hand", wrongTool.LabelKey);
        Assert.Equal(
            "notice.needs_hand",
            session.TryEnterGreenhouse(
                FarmLayout.GreenhouseDoorCell
            ).MessageKey
        );

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            GreenhouseLayout.ExitCell.X * 16 + 8,
            (GreenhouseLayout.ExitCell.Y - 1) * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        var exit = session.PreviewSelectedTarget(
            GreenhouseLayout.ExitCell
        );
        Assert.True(exit.IsAvailable);
        Assert.Equal(TargetPreviewKind.GreenhouseExit, exit.Kind);
        Assert.True(session.TryExitGreenhouse(
            GreenhouseLayout.ExitCell
        ).Succeeded);
    }

    [Fact]
    public void GreenhouseIgnoresSeasonButRemainsIndependentFromOutdoorFarm()
    {
        var session = CompletedGreenhouseSession();
        var greenhouseCell = new GridPosition(12, 7);
        session.SetPlayerLocation(
            GreenhouseLayout.SafeArrivalCell.X * 16 + 8,
            GreenhouseLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        session.Inventory.Select(1);
        Assert.True(session.UseSelected(greenhouseCell).Succeeded);

        Assert.True(session.Inventory.Add(DataCatalog.RipplecapSeedId, 1));
        SelectItem(session, DataCatalog.RipplecapSeedId);
        var preview = session.PreviewSelectedTarget(greenhouseCell);
        Assert.True(preview.IsAvailable);
        Assert.Equal("target.action.plant", preview.LabelKey);
        Assert.True(session.UseSelected(greenhouseCell).Succeeded);
        Assert.Equal(
            DataCatalog.RipplecapId,
            session.GreenhouseFarm.Tiles[greenhouseCell].CropId
        );
        Assert.False(session.Farm.Tiles.ContainsKey(greenhouseCell));

        var outdoorCell = new GridPosition(11, 15);
        Assert.True(session.Farm.TryTill(outdoorCell, 100).Succeeded);
        var outdoors = session.Farm.TryPlant(
            outdoorCell,
            DataCatalog.RipplecapId,
            plantedDay: 1
        );
        Assert.False(outdoors.Succeeded);
        Assert.Equal("notice.seed_out_of_season", outdoors.MessageKey);

        var captured = session.Capture();
        Assert.Single(captured.Greenhouse.Tiles);
        Assert.Single(captured.FarmTiles);
        Assert.Equal(greenhouseCell, captured.Greenhouse.Tiles[0].Position);
        Assert.Equal(outdoorCell, captured.FarmTiles[0].Position);
    }

    [Fact]
    public void GreenhouseRejectsOutdoorWeatherWateringAndResonance()
    {
        var farm = new FarmSystem(CultivationZoneCatalog.Greenhouse);
        foreach (var cell in GreenhouseLayout.PlantingCells)
        {
            Assert.True(farm.TryTill(cell, 100).Succeeded);
            Assert.True(farm.TryPlant(
                cell,
                DataCatalog.DawnlaceId,
                plantedDay: 15
            ).Succeeded);
        }

        Assert.Equal(0, farm.ApplyWeatherWatering());
        Assert.All(farm.Tiles.Values, tile => Assert.False(tile.Watered));

        var nights = DataCatalog.Crop(DataCatalog.DawnlaceId)
            .MatureAfterWateredNights;
        for (var night = 0; night < nights; night++)
        {
            foreach (var cell in GreenhouseLayout.PlantingCells)
            {
                Assert.True(farm.ApplyAutomaticWatering(cell));
            }

            farm.EndDay(DataCatalog.RainWeatherId);
        }

        Assert.All(GreenhouseLayout.PlantingCells, cell =>
            Assert.NotEqual(
                DataCatalog.RainwovenDawnlaceId,
                farm.HarvestItemIdAt(cell)
            )
        );
    }

    [Fact]
    public void CisternPreviewAndRefillAreAtomic()
    {
        var session = CompletedGreenhouseSession();
        session.SetPlayerLocation(
            (GreenhouseLayout.CisternCell.X - 1) * 16 + 8,
            GreenhouseLayout.CisternCell.Y * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        session.Inventory.Select(4);
        var fullBefore = Snapshot(session);
        var fullPreview = session.PreviewSelectedTarget(
            GreenhouseLayout.CisternCell
        );
        var full = session.UseSelected(GreenhouseLayout.CisternCell);
        Assert.Equal(TargetPreviewState.Blocked, fullPreview.State);
        Assert.Equal(TargetPreviewKind.Cistern, fullPreview.Kind);
        Assert.Equal("target.status.water_full", fullPreview.LabelKey);
        Assert.False(full.Succeeded);
        Assert.Equal("notice.water_full", full.MessageKey);
        Assert.Equal(fullBefore, Snapshot(session));

        var bed = new GridPosition(12, 7);
        session.Inventory.Select(1);
        Assert.True(session.UseSelected(bed).Succeeded);
        session.Inventory.Select(3);
        Assert.True(session.UseSelected(bed).Succeeded);
        Assert.Equal(GameSession.MaxWateringCanWater - 1,
            session.WateringCanWater);

        session.Inventory.Select(0);
        var needsBucket = session.PreviewSelectedTarget(
            GreenhouseLayout.CisternCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, needsBucket.State);
        Assert.Equal("target.need.bucket", needsBucket.LabelKey);

        session.Inventory.Select(4);
        Assert.True(session.PreviewSelectedTarget(
            GreenhouseLayout.CisternCell
        ).IsAvailable);
        Assert.True(session.UseSelected(
            GreenhouseLayout.CisternCell
        ).Succeeded);
        Assert.Equal(
            GameSession.MaxWateringCanWater,
            session.WateringCanWater
        );
    }

    [Fact]
    public void SaveV1AddsAndNormalizesGreenhouseWithoutClearingOldData()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-greenhouse-{Guid.NewGuid():N}.json"
        );
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "schemaVersion": 1,
                  "day": 9,
                  "coins": 123,
                  "farmTiles": [
                    { "x": 11, "y": 15, "tilled": true }
                  ]
                }
                """
            );
            var legacy = new SaveService(path).Load();
            Assert.Equal(SaveLoadStatus.Loaded, legacy.Status);
            var legacySave = Assert.IsType<GameSaveV1>(legacy.Save);
            Assert.Equal(1, legacySave.SchemaVersion);
            Assert.Equal(9, legacySave.Day);
            Assert.Equal(123, legacySave.Coins);
            Assert.Single(legacySave.FarmTiles);
            Assert.Empty(legacySave.Greenhouse.Tiles);

            File.WriteAllText(
                path,
                """
                {
                  "schemaVersion": 1,
                  "day": 15,
                  "coins": 321,
                  "farmTiles": [
                    { "x": 11, "y": 15, "tilled": true }
                  ],
                  "greenhouse": {
                    "tiles": [
                      { "x": 12, "y": 7, "tilled": true,
                        "cropId": "ripplecap", "wateredNights": 99,
                        "qualityRoll": 999, "plantedDay": 0 },
                      { "x": 12, "y": 7, "tilled": true },
                      { "x": 1, "y": 1, "tilled": true },
                      { "x": 25, "y": 7, "tilled": true,
                        "cropId": "removed_crop" }
                    ]
                  }
                }
                """
            );

            var loaded = new SaveService(path).Load();
            Assert.Equal(SaveLoadStatus.Loaded, loaded.Status);
            var save = Assert.IsType<GameSaveV1>(loaded.Save);
            Assert.Equal(1, save.SchemaVersion);
            Assert.Equal(15, save.Day);
            Assert.Equal(321, save.Coins);
            Assert.Single(save.FarmTiles);
            Assert.Equal(2, save.Greenhouse.Tiles.Count);
            var crop = save.Greenhouse.Tiles.Single(tile =>
                tile.Position == new GridPosition(12, 7)
            );
            Assert.Equal(DataCatalog.RipplecapId, crop.CropId);
            Assert.Equal(
                DataCatalog.Crop(DataCatalog.RipplecapId)
                    .MatureAfterWateredNights,
                crop.WateredNights
            );
            Assert.Equal(99, crop.QualityRoll);
            Assert.Equal(1, crop.PlantedDay);
            Assert.Null(save.Greenhouse.Tiles.Single(tile =>
                tile.Position == new GridPosition(25, 7)
            ).CropId);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void RestoreRejectsUnbuiltInteriorAndNormalizesBuiltPosition()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.Greenhouse;
        save.Player.X = 1 * 16 + 8;
        save.Player.Y = 1 * 16 + 8;
        session.Restore(save);
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        Assert.Equal(FarmLayout.GreenhouseReturnCell, session.PlayerCell);

        save.Construction = CompletedConstruction();
        session.Restore(save);
        Assert.True(session.InsideGreenhouse);
        Assert.Equal(GreenhouseLayout.SafeArrivalCell, session.PlayerCell);
    }

    private static GameSession PreparedConstructionSession(
        bool completedWorkshop
    )
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 28));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 12));
        var save = session.Capture();
        save.Coins = 720;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        if (completedWorkshop)
        {
            save.Construction = new ConstructionSave
            {
                Projects =
                [
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog
                            .HomesteadWorkshopProjectId,
                        Completed = true
                    }
                ]
            };
        }

        session.Restore(save);
        return session;
    }

    private static GameSession CompletedGreenhouseSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Construction = CompletedConstruction();
        session.Restore(save);
        return session;
    }

    private static ConstructionSave CompletedConstruction() => new()
    {
        Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            },
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadGreenhouseProjectId,
                Completed = true
            }
        ]
    };

    private static void RestoreGreenhousePhase(
        GameSession session,
        ConstructionProjectSave greenhouse
    )
    {
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadWorkshopProjectId,
                    Completed = true
                },
                greenhouse
            ]
        };
        session.Restore(save);
    }

    private static void SetAdjacentToExteriorDoor(GameSession session) =>
        session.SetPlayerLocation(
            FarmLayout.GreenhouseReturnCell.X * 16 + 8,
            FarmLayout.GreenhouseReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

    private static void SelectItem(GameSession session, string itemId)
    {
        var index = session.Inventory.Slots
            .Select((slot, index) => (slot, index))
            .Single(entry => entry.slot.ItemId == itemId)
            .index;
        session.Inventory.Select(index);
    }

    private static void AddBed(
        ISet<GridPosition> cells,
        int minX,
        int maxX,
        int minY,
        int maxY
    )
    {
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                cells.Add(new GridPosition(x, y));
            }
        }
    }

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
