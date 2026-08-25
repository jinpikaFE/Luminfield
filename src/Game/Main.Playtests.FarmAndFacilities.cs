using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void StartProcessorPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 4);
        _session.Inventory.Add(DataCatalog.MoonrootId, 4);
        _session.SetPlayerState(
            FarmView.ProcessorCell.X * 16 + 8,
            (FarmView.ProcessorCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(
            () => OpenProcessor(ProcessorCatalog.MainMachineId)
        ).CallDeferred();
    }

    private void StartMultiProcessorBatchPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 2);
        _session.Inventory.Add(DataCatalog.MoonrootId, 2);
        _session.Inventory.Add(DataCatalog.CloudleafId, 3);
        _session.StartProcessing(
            ProcessorCatalog.MoonwellInfuserId,
            DataCatalog.MoonrootTonicRecipeId
        );
        _session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarbudPreserveRecipeId
        );
        _session.StartProcessing(
            ProcessorCatalog.StarweaveDryingLoomId,
            DataCatalog.CloudleafTeaRecipeId
        );
        _session.EndDay();
        _session.EndDay();
        var focus = ProcessorCatalog.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).Position;
        _session.SetPlayerState(
            focus.X * 16 + 8,
            (focus.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(
            () => OpenProcessor(ProcessorCatalog.PrismPreserveVatId)
        ).CallDeferred();
    }

    private void StartMoonpearlEggPressPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarfeatherEggId, 2);
        _session.StartProcessing(
            ProcessorCatalog.MoonpearlEggPressId,
            DataCatalog.StarfeatherCreamRecipeId
        );
        var focus = ProcessorCatalog.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).Position;
        _session.SetPlayerState(
            focus.X * 16 + 8,
            (focus.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCottagePlaytest()
    {
        StartNewGame();
        ShowCottage(true);
    }

    private void StartCottageUpgradeReadyPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        Callable.From(OpenConstructionPanel).CallDeferred();
    }

    private void StartCottageUpgradeInProgressPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        _ = _session.StartCottageFirstUpgrade();
        Callable.From(OpenConstructionPanel).CallDeferred();
    }

    private void StartCottageUpgradeCompletedPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        _ = _session.StartCottageFirstUpgrade();
        _session.EndDay();
        _session.EndDay();
        _session.SetPlayerLocation(
            27 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        ShowCottage(false);
    }

    private void StartCottageSecondUpgradeReadyPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest();
        _session.SetPlayerLocation(
            20 * 16 + 8,
            11 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.CottageSecondUpgradeId
            )
        ).CallDeferred();
    }

    private void StartCottageSecondUpgradeInProgressPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                RemainingNights = 3
            }
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
    }

    private void StartCottageKitchenPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
    }

    private void StartCottageKitchenPanelPlaytest()
    {
        StartCottageKitchenPlaytest();
        Callable.From(
            () => OpenKitchen(new GridPosition(29, 10))
        ).CallDeferred();
    }

    private void StartCottagePantryPanelPlaytest()
    {
        StartCottagePantryPlaytest();
        _session.Inventory.Select(0);
        Callable.From(
            () => OpenIngredientPantry(new GridPosition(34, 10))
        ).CallDeferred();
    }

    private void StartCottagePantryPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        _session.SetPlayerLocation(
            34 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        _session.Inventory.Select(1);
        ShowCottage(false);
    }

    private void StartCottageMealsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
        Callable.From(OpenCookedDishes).CallDeferred();
    }

    private void PrepareCottageSecondUpgradePlaytest(
        ConstructionProjectSave? secondUpgrade = null
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        foreach (var (itemId, count) in new (string, int)[]
                 {
                     (DataCatalog.LumenwoodId, 32),
                     (DataCatalog.CrystalShardId, 14),
                     (DataCatalog.RipplecapId, 2),
                     (DataCatalog.MistsongMintId, 2),
                     (DataCatalog.DewhornMilkId, 2),
                     (DataCatalog.SunvaultGourdId, 2),
                     (DataCatalog.CometTuberId, 2),
                     (DataCatalog.MoonplumId, 2),
                     (DataCatalog.LanternReedId, 2),
                     (DataCatalog.MoonrootId, 2),
                     (DataCatalog.TideglassTaroId, 2),
                     (DataCatalog.MoonmistStewId, 1),
                     (DataCatalog.SunvaultHashId, 1),
                     (DataCatalog.StarhoneyCustardId, 1),
                     (DataCatalog.LanternrootBrothId, 1)
                 })
        {
            _session.Inventory.Add(itemId, count);
        }

        var projects = new List<ConstructionProjectSave>
        {
            new()
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                Completed = true
            },
            new()
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            }
        };
        if (secondUpgrade is not null)
        {
            projects.Add(secondUpgrade);
        }

        var save = _session.Capture();
        save.Day = 43;
        save.Coins = 960;
        save.Construction = new ConstructionSave { Projects = projects };
        save.Kitchen = new KitchenSave
        {
            PantryItems =
            [
                new InventorySlot
                {
                    ItemId = DataCatalog.StarhoneyId,
                    Count = 2
                },
                new InventorySlot
                {
                    ItemId = DataCatalog.StarfeatherEggId,
                    Count = 2
                }
            ]
        };
        save.Player.LocationId = PlayerLocationIds.Cottage;
        save.Player.X = 29 * 16 + 8;
        save.Player.Y = 9 * 16 + 8;
        save.Player.Energy = 35;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtCottageKitchen()
    {
        _session.SetPlayerLocation(
            29 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
    }

    private static ConstructionProjectSave
        CompletedCottageSecondUpgradeProject() => new()
        {
            ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
            Completed = true
        };

    private void PrepareCottageConstructionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 12);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 4);
        var save = _session.Capture();
        save.Coins = 240;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
    }

    private void StartHomesteadWorkshopReadyPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest();
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.HomesteadWorkshopProjectId
            )
        ).CallDeferred();
    }

    private void StartHomesteadWorkshopInProgressPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest(15);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        PositionAtHomesteadWorkbench();
        ShowFarm(false);
    }

    private void StartHomesteadWorkshopCompletedPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest(29);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        _session.EndDay();
        _session.EndDay();
        _session.EndDay();
        PositionAtHomesteadWorkbench();
        ShowFarm(false);
    }

    private void PrepareHomesteadWorkshopPlaytest(int day = 1)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 20);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 8);
        var save = _session.Capture();
        save.Day = day;
        save.Coins = 480;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtHomesteadWorkbench()
    {
        _session.SetPlayerLocation(
            FarmLayout.HomesteadWorkbenchCell.X * 16 + 8,
            (FarmLayout.HomesteadWorkbenchCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private void StartGreenhouseReadyPlaytest()
    {
        PrepareGreenhousePlaytest();
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.HomesteadGreenhouseProjectId
            )
        ).CallDeferred();
    }

    private void StartGreenhouseInProgressPlaytest()
    {
        PrepareGreenhousePlaytest(15);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        PositionAtGreenhouseDoor();
        ShowFarm(false);
    }

    private void StartGreenhouseCompletedPlaytest()
    {
        CompleteGreenhousePlaytestConstruction();
        RestoreGreenhouseShowcaseCrops();
        _session.SetPlayerLocation(
            12 * 16 + 8,
            6 * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        ShowGreenhouse(false);
    }

    private void StartGreenhouseExteriorCompletedPlaytest()
    {
        CompleteGreenhousePlaytestConstruction(29);
        PositionAtGreenhouseDoor();
        ShowFarm(false);
    }

    private void StartGreenhouseCisternPlaytest()
    {
        CompleteGreenhousePlaytestConstruction();
        RestoreGreenhouseShowcaseCrops();
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.Greenhouse;
        save.Player.X = GreenhouseLayout.CisternCell.X * 16 + 8;
        save.Player.Y = (GreenhouseLayout.CisternCell.Y - 1) * 16 + 8;
        save.Player.SelectedSlot = 4;
        save.Player.WateringCanWater = 6;
        _session.Restore(save);
        ShowGreenhouse(false);
    }

    private void CompleteGreenhousePlaytestConstruction(int day = 43)
    {
        PrepareGreenhousePlaytest(day);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        for (var night = 0; night < 4; night++)
        {
            _session.EndDay();
        }
    }

    private void RestoreGreenhouseShowcaseCrops()
    {
        _session.GreenhouseFarm.Restore(
        [
            MatureCropState(12, 7, DataCatalog.DawnlaceId),
            CropState(13, 7, DataCatalog.RipplecapId, 1),
            MatureCropState(25, 7, DataCatalog.TideglassTaroId),
            CropState(26, 7, DataCatalog.LanternReedId, 2),
            MatureCropState(11, 12, DataCatalog.AuricShootId),
            CropState(12, 12, DataCatalog.SunvaultGourdId, 2),
            MatureCropState(26, 12, DataCatalog.CrownstarSaffronId),
            CropState(27, 12, DataCatalog.AmberthreadClusterId, 2)
        ]);
    }

    private void PrepareGreenhousePlaytest(int day = 1)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 28);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 12);
        var save = _session.Capture();
        save.Day = day;
        save.Coins = 720;
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadWorkshopProjectId,
                Completed = true
            }
        ];
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtGreenhouseDoor()
    {
        _session.SetPlayerLocation(
            FarmLayout.GreenhouseReturnCell.X * 16 + 8,
            FarmLayout.GreenhouseReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private void StartStarfeatherCoopReadyPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 1,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            project: null,
            animal: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopInProgressPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 15,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            project: new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadStarfeatherCoopProjectId,
                RemainingNights = 2
            },
            animal: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopGrazingPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 2, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopChickPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 16,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 0, mood: 2),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void StartStarfeatherCoopAdultPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 2, mood: 5),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void StartStarfeatherCoopNestBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareStarfeatherCoopPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(
                ageNights: 2,
                mood: 4,
                pendingProductItemId: DataCatalog.StarfeatherEggLuminousId
            ),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(32, 11)
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.StarfeatherEggLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void PrepareStarfeatherCoopPlaytest(
        int day,
        int minuteOfDay,
        string weatherId,
        ConstructionProjectSave? project,
        AnimalEntrySave? animal,
        string locationId,
        GridPosition playerCell
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Construction = new ConstructionSave
        {
            Projects = project is null ? [] : [project]
        };
        save.Animals = new AnimalSave
        {
            Animals = animal is null ? [] : [animal]
        };
        save.Player.LocationId = locationId;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static ConstructionProjectSave CompletedStarfeatherCoopProject() =>
        new()
        {
            ProjectId = ConstructionCatalog
                .HomesteadStarfeatherCoopProjectId,
            Completed = true
        };

    private static AnimalEntrySave StarfeatherChickenPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterStarfeatherChickenId,
        SpeciesId = AnimalCatalog.StarfeatherChickenId,
        BuildingId = AnimalCatalog.StarfeatherCoopId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private void StartMoonfleeceBarnReadyPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 1,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: null,
            sheep: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnInProgressPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 15,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            barnProject: new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadMoonfleeceBarnProjectId,
                RemainingNights = 2
            },
            sheep: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnGrazingPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnJuvenilePlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 16,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 0, mood: 2),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartMoonfleeceBarnRackBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMoonfleeceBarnPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(
                ageNights: 3,
                mood: 5,
                pendingProductItemId: DataCatalog.MoonfleeceLuminousId
            ),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(31, 14)
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.MoonfleeceLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartDewhornGrazingPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: new GridPosition(40, 17),
            dewhorn: DewhornPlaytestState(ageNights: 4, mood: 4)
        );
        ShowFarm(false);
    }

    private void StartDewhornMilkingBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMoonfleeceBarnPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(31, 18),
            dewhorn: DewhornPlaytestState(
                ageNights: 4,
                mood: 5,
                pendingProductItemId: DataCatalog.DewhornMilkLuminousId
            )
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.DewhornMilkLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartLivestockAutomationConsolePlaytest()
    {
        PrepareLivestockAutomationPlaytest(
            AnimalCatalog.StarfeatherCoopId,
            new AnimalBuildingAutomationSave
            {
                BuildingId = AnimalCatalog.StarfeatherCoopId,
                StoredFeed = 28,
                StoredProducts =
                [
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.StarfeatherEggLuminousId,
                        Count = 4
                    }
                ]
            }
        );
        ShowStarfeatherCoop(false);
    }

    private void StartLivestockAutomationPanelPlaytest()
    {
        PrepareLivestockAutomationPlaytest(
            AnimalCatalog.MoonfleeceBarnId,
            new AnimalBuildingAutomationSave
            {
                BuildingId = AnimalCatalog.MoonfleeceBarnId,
                StoredFeed = 28,
                StoredProducts =
                [
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.MoonfleeceLuminousId,
                        Count = 6
                    },
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.DewhornMilkStarlightId,
                        Count = 6
                    }
                ],
                LastResolvedDay = 43,
                LastAutoFedCount = 2,
                LastAutoCollectedCount = 2,
                LastFeedStatusId = AnimalAutomationStatusIds.Succeeded,
                LastCollectionStatusId =
                    AnimalAutomationStatusIds.Succeeded
            }
        );
        ShowMoonfleeceBarn(false);
        Callable.From(() => OpenLivestockAutomation(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        )).CallDeferred();
    }

    private void StartLivestockAutomationPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartLivestockAutomationPanelPlaytest();
    }

    private void StartLivestockAutomationConstructionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 24);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 16);
        var save = _session.Capture();
        save.Coins = 900;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        save.Construction = new ConstructionSave
        {
            Projects = LivestockAutomationPrerequisiteProjects()
        };
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        Callable.From(() => OpenConstructionOverlay(
            ConstructionCatalog.HomesteadLivestockAutomationProjectId
        )).CallDeferred();
    }

    private void PrepareLivestockAutomationPlaytest(
        string buildingId,
        AnimalBuildingAutomationSave automation
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        var save = _session.Capture();
        save.Day = 43;
        save.MinuteOfDay = 10 * 60;
        save.Weather = new WeatherSave
        {
            Day = 43,
            CurrentId = DataCatalog.LongnightSnowWeatherId,
            ForecastId = DataCatalog.LongnightSnowWeatherId
        };
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                .. LivestockAutomationPrerequisiteProjects(),
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadLivestockAutomationProjectId,
                    Completed = true
                }
            ]
        };
        save.Animals = new AnimalSave
        {
            Animals =
            [
                new AnimalEntrySave
                {
                    InstanceId = AnimalCatalog
                        .StarterStarfeatherChickenId,
                    SpeciesId = AnimalCatalog.StarfeatherChickenId,
                    BuildingId = AnimalCatalog.StarfeatherCoopId,
                    AgeNights = 2,
                    Mood = 4
                },
                MoonfleeceSheepPlaytestState(3, 4),
                DewhornPlaytestState(4, 4)
            ],
            Automation = [automation]
        };
        save.Player.LocationId = spatial.LocationId;
        save.Player.X = spatial.AutomationStationCell.X * 16 + 8;
        save.Player.Y = (spatial.AutomationStationCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static List<ConstructionProjectSave>
        LivestockAutomationPrerequisiteProjects() =>
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            },
            CompletedStarfeatherCoopProject(),
            CompletedMoonfleeceBarnProject()
        ];

    private void PrepareMoonfleeceBarnPlaytest(
        int day,
        int minuteOfDay,
        string weatherId,
        ConstructionProjectSave? barnProject,
        AnimalEntrySave? sheep,
        string locationId,
        GridPosition playerCell,
        AnimalEntrySave? dewhorn = null
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var projects = new List<ConstructionProjectSave>
        {
            CompletedStarfeatherCoopProject()
        };
        if (barnProject is not null)
        {
            projects.Add(barnProject);
        }

        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Construction = new ConstructionSave { Projects = projects };
        save.Animals = new AnimalSave { Animals = [] };
        if (sheep is not null)
        {
            save.Animals.Animals.Add(sheep);
        }
        if (dewhorn is not null)
        {
            save.Animals.Animals.Add(dewhorn);
        }
        save.Player.LocationId = locationId;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static ConstructionProjectSave CompletedMoonfleeceBarnProject() =>
        new()
        {
            ProjectId = ConstructionCatalog.HomesteadMoonfleeceBarnProjectId,
            Completed = true
        };

    private static AnimalEntrySave MoonfleeceSheepPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterMoonfleeceSheepId,
        SpeciesId = AnimalCatalog.MoonfleeceSheepId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private static AnimalEntrySave DewhornPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterDewhornId,
        SpeciesId = AnimalCatalog.DewhornId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private static FarmTileState MatureCropState(
        int x,
        int y,
        string cropId
    ) => CropState(
        x,
        y,
        cropId,
        DataCatalog.Crop(cropId).MatureAfterWateredNights
    );

    private void StartDoorPlaytest()
    {
        StartNewGame();
        _session.SetPlayerState(
            FarmView.CottageDoorCell.X * 16 + 8,
            (FarmView.CottageDoorCell.Y + 1) * 16 + 8,
            false
        );
        ShowFarm(false);
    }

    private void StartCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var columns = Enumerable.Range(0, FarmSystem.MapWidth)
            .Where(x => FarmSystem.IsPlantingBed(new GridPosition(x, 16)))
            .Take(DataCatalog.CropIds.Count)
            .ToArray();
        if (columns.Length != DataCatalog.CropIds.Count)
        {
            throw new InvalidOperationException(
                "The crop playtest requires one planting-bed column per crop."
            );
        }
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.CropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.CropIds[index]);
            crops.Add(CropState(columns[index], 16, crop.Id, crop.MatureAfterWateredNights));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }
        _session.Farm.Restore(crops);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGleamriseCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var cropIds = new[]
        {
            DataCatalog.DawnlaceId,
            DataCatalog.GlimmerpodId,
            DataCatalog.MistsongMintId,
            DataCatalog.CometTuberId
        };
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < cropIds.Length; index++)
        {
            var crop = DataCatalog.Crop(cropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGleamriseSeasonPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterBuyGleamriseSeed,
            4
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantGleamriseCrop,
            3
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterWaterGleamriseCrop,
            3
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterFertilizeGleamriseSoil
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestGleamriseCrop,
            2
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterStartProcessor
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterCollectProcessor,
            2
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantMoonplumTree
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestMoonplum
        );
        var save = _session.Capture();
        save.Day = CalendarSystem.DaysPerSeason;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = WeatherSystem.WeatherForDay(save.Day),
            ForecastId = WeatherSystem.WeatherForDay(save.Day + 1)
        };
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenGleamriseSeasonGoals).CallDeferred();
    }

    private void StartRainveilCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(15, 8 * 60);
        _session.Weather.AdvanceToDay(15);
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.RainveilCropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.RainveilCropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        foreach (var seedId in DataCatalog.RainveilSeedItemIds)
        {
            _session.Inventory.Add(seedId, 2);
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            columns[2] * 16 + 8,
            15 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartStarharvestCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(29, 8 * 60);
        _session.Weather.Restore(
            new WeatherSave
            {
                Day = 29,
                CurrentId = DataCatalog.ClearWeatherId,
                ForecastId = DataCatalog.ClearWeatherId
            },
            29
        );
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.StarharvestCropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.StarharvestCropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        foreach (var seedId in DataCatalog.StarharvestSeedItemIds)
        {
            _session.Inventory.Add(seedId, 2);
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            columns[3] * 16 + 8,
            15 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartStarharvestMarketGatePlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.WorldReturnCell.X * 16 + 8,
            StarharvestMarketLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartStarharvestMarketPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
            StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
    }

    private void StartStarharvestMarketShowcasePlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        AddStarharvestMarketExamples();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ExhibitCell.X * 16 + 8,
            (StarharvestMarketLayout.ExhibitCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
        Callable.From(OpenFestivalShowcase).CallDeferred();
    }

    private void StartStarharvestMarketResultPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        AddStarharvestMarketExamples();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ExhibitCell.X * 16 + 8,
            (StarharvestMarketLayout.ExhibitCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        _ = _session.SubmitFestivalExhibit(
        [
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId
        ]);
        ShowStarharvestMarket(false);
    }

    private void StartStarharvestMarketShopPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.Festival.Restore(new FestivalSave { Scrip = 12 });
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ShopCell.X * 16 + 8,
            (StarharvestMarketLayout.ShopCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
        Callable.From(OpenFestivalShop).CallDeferred();
    }

    private void StartStarharvestMarketShowcaseEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartStarharvestMarketShowcasePlaytest();
    }

    private void PrepareStarharvestMarketPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(39, 10 * 60);
        _session.Weather.AdvanceToDay(39);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddStarharvestMarketExamples()
    {
        foreach (var itemId in new[]
        {
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId,
            DataCatalog.AuricShootStarlightId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartGleamriseFestivalGatePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.WorldReturnCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartGleamriseFestivalPlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
    }

    private void StartGleamriseFestivalChallengePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(8);
        var plot = GleamrisePlantingFestivalLayout.PlotCells[8];
        _session.SetPlayerLocation(
            (plot.X - 1) * 16 + 8,
            plot.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
    }

    private void StartGleamriseFestivalResultPlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(12);
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamrisePlanting).CallDeferred();
    }

    private void StartGleamriseFestivalExchangePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                    Balance = 10
                }
            ]
        });
        var exchange = GleamrisePlantingFestivalLayout.SeedExchangeCell;
        _session.SetPlayerLocation(
            exchange.X * 16 + 8,
            (exchange.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamriseSeedExchange).CallDeferred();
    }

    private void StartGleamriseFestivalChallengeEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(5);
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamrisePlanting).CallDeferred();
    }

    private void PrepareGleamriseFestivalPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(4, 9 * 60);
        _session.Weather.AdvanceToDay(4);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void PrepareGleamriseChallenge(int plantedCount)
    {
        var selected = new[]
        {
            DataCatalog.DawnlaceSeedId,
            DataCatalog.GlimmerpodSeedId,
            DataCatalog.MistsongMintSeedId
        };
        _ = _session.Festival.StartPlantingChallenge(
            CalendarSystem.YearNumber(_session.Clock.Day),
            _session.Clock.MinuteOfDay,
            selected
        );
        for (var index = 0; index < plantedCount; index++)
        {
            var seedId = selected[index % selected.Length];
            _ = _session.Festival.SelectPlantingSeed(1, seedId);
            _ = _session.Festival.PlantPlot(
                1,
                _session.Clock.MinuteOfDay + Math.Min(index * 5, 60),
                GleamrisePlantingFestivalLayout.PlotIds[index]
            );
        }

        _session.Clock.Reset(
            4,
            9 * 60 + Math.Min(Math.Max(0, plantedCount - 1) * 5, 60)
        );
    }

    private void StartLongnightFeastGatePlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.SetPlayerLocation(
            LongnightLanternFeastLayout.WorldReturnCell.X * 16 + 8,
            LongnightLanternFeastLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartLongnightFeastPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.SetPlayerLocation(
            LongnightLanternFeastLayout.SafeArrivalCell.X * 16 + 8,
            LongnightLanternFeastLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
    }

    private void StartLongnightFeastActivityPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        AddLongnightFeastExamples();
        var table = LongnightLanternFeastLayout.SharedTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
        Callable.From(() => OpenLongnightFeast(table)).CallDeferred();
    }

    private void StartLongnightFeastResultPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        AddLongnightFeastExamples();
        var ritual = LongnightLanternFeastLayout.RitualCell;
        _session.SetPlayerLocation(
            ritual.X * 16 + 8,
            (ritual.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        _ = _session.CompleteLongnightFeast(
            ritual,
            [DataCatalog.MoonmistStewId, DataCatalog.StarhoneyCustardId],
            FestivalCatalog.LongnightCloudleafTeaExchangeId
        );
        ShowLongnightLanternFeast(false);
    }

    private void StartLongnightFeastStallPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.LongnightLanternKnotId,
                    Balance = 10
                }
            ]
        });
        var stall = LongnightLanternFeastLayout.StallCell;
        _session.SetPlayerLocation(
            stall.X * 16 + 8,
            (stall.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
        Callable.From(OpenLongnightStall).CallDeferred();
    }

    private void StartLongnightFeastActivityEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartLongnightFeastActivityPlaytest();
    }

    private void StartLongnightFeastWrongToolPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        var ritual = LongnightLanternFeastLayout.RitualCell;
        _session.SetPlayerLocation(
            ritual.X * 16 + 8,
            (ritual.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        _session.Inventory.Select(1);
        ShowLongnightLanternFeast(false);
    }

    private void PrepareLongnightFeastPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(55, 17 * 60);
        _session.Weather.AdvanceToDay(55);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddLongnightFeastExamples()
    {
        foreach (var itemId in new[]
        {
            DataCatalog.MoonmistStewId,
            DataCatalog.SunvaultHashId,
            DataCatalog.StarhoneyCustardId,
            DataCatalog.LanternrootBrothId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.StarhoneyId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartFireflyTideGatePlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.SetPlayerLocation(
            FireflyTideLayout.WorldReturnCell.X * 16 + 8,
            FireflyTideLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartFireflyTidePlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.SetPlayerLocation(
            FireflyTideLayout.SafeArrivalCell.X * 16 + 8,
            FireflyTideLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
    }

    private void StartFireflyTideActivityPlaytest()
    {
        PrepareFireflyTidePlaytest();
        AddFireflyTideExamples();
        var launch = FireflyTideLayout.LanternLaunchCell;
        _session.SetPlayerLocation(
            launch.X * 16 + 8,
            (launch.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
        Callable.From(() => OpenFireflyTideActivity(launch)).CallDeferred();
    }

    private void StartFireflyTideResultPlaytest()
    {
        PrepareFireflyTidePlaytest();
        AddFireflyTideExamples();
        var altar = FireflyTideLayout.TideAltarCell;
        _session.SetPlayerLocation(
            altar.X * 16 + 8,
            (altar.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        _ = _session.CompleteFireflyTide(
            altar,
            [
                DataCatalog.MooncapGobyId,
                DataCatalog.RainveilLampreyId,
                DataCatalog.StardustRayId
            ]
        );
        ShowFireflyTide(false);
        Callable.From(() => OpenFireflyTideActivity(altar)).CallDeferred();
    }

    private void StartFireflyTideShopPlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.FireflyGlowmarkId,
                    Balance = 10
                }
            ]
        });
        var shop = FireflyTideLayout.ShopCell;
        _session.SetPlayerLocation(
            shop.X * 16 + 8,
            (shop.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
        Callable.From(OpenFireflyTideShop).CallDeferred();
    }

    private void StartFireflyTideActivityEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartFireflyTideActivityPlaytest();
    }

    private void StartFireflyTideWrongToolPlaytest()
    {
        PrepareFireflyTidePlaytest();
        var altar = FireflyTideLayout.TideAltarCell;
        _session.SetPlayerLocation(
            altar.X * 16 + 8,
            (altar.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        _session.Inventory.Select(1);
        ShowFireflyTide(false);
    }

    private void PrepareFireflyTidePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(26, 18 * 60);
        _session.Weather.AdvanceToDay(26);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddFireflyTideExamples()
    {
        foreach (var itemId in FestivalCatalog.FireflyTideFishIds)
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartLongnightHomesteadPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        const int day = 43;
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 8 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);

        var target = new GridPosition(22, 16);
        _ = _session.Farm.TryTill(target, GameSession.MaxEnergy);
        _session.Inventory.Add(DataCatalog.CloudleafSeedId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.CloudleafSeedId);
        _session.SetPlayerState(
            target.X * 16 + 8,
            (target.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartLongnightEmporiumPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        const int day = 43;
        _session.Clock.Reset(day, 10 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
        Callable.From(InspectTravelManifest).CallDeferred();
    }

    private void StartLongnightSnowForecastPlaytest() =>
        StartLongnightWeatherFarmPlaytest(42);

    private void StartLongnightSnowPlaytest() =>
        StartLongnightWeatherFarmPlaytest(43);

    private void StartLongnightSnowClearPlaytest() =>
        StartLongnightWeatherFarmPlaytest(44);

    private void StartLongnightSnowIndoorPlaytest()
    {
        CompleteGreenhousePlaytestConstruction(39);
        RestoreGreenhouseShowcaseCrops();
        _session.SetPlayerLocation(
            12 * 16 + 8,
            6 * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        ShowGreenhouse(false);
    }

    private void StartLongnightWeatherFarmPlaytest(int day)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, 8 * 60);
        _session.Weather.AdvanceToDay(day);
        var target = new GridPosition(22, 16);
        _ = _session.Farm.TryTill(target, GameSession.MaxEnergy);
        _session.Inventory.Add(DataCatalog.CloudleafSeedId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.CloudleafSeedId);
        _session.SetPlayerState(
            target.X * 16 + 8,
            (target.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCraftingPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.LumenwoodId, 20);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 20);
        OpenCrafting();
    }

    private void StartQualityCraftingPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 5);
        OpenCrafting();
    }

    private void StartQualityBackpackPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.StarsoilFertilizerId, 4);
        _session.Inventory.Add(DataCatalog.StarbudId, 2);
        _session.Inventory.Add(DataCatalog.StarbudLuminousId, 2);
        _session.Inventory.Add(DataCatalog.StarbudStarlightId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootLuminousId, 2);
        _session.Inventory.Add(DataCatalog.MoonrootStarlightId, 1);
        Callable.From(OpenBackpack).CallDeferred();
    }

    private void StartQualityBackpackEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartQualityBackpackPlaytest();
    }

    private void StartQualityPlaytest()
    {
        StartNewGame();
        _session.Farm.Restore(
        [
            new FarmTileState
            {
                X = 12,
                Y = 16,
                Tilled = true
            },
            new FarmTileState
            {
                X = 14,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId
            },
            new FarmTileState
            {
                X = 16,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId,
                CropId = DataCatalog.StarbudId,
                WateredNights = 2,
                QualityRoll = 55
            },
            new FarmTileState
            {
                X = 20,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId,
                CropId = DataCatalog.MoonrootId,
                WateredNights = 3,
                QualityRoll = 5
            },
            new FarmTileState
            {
                X = 22,
                Y = 16,
                Tilled = true,
                CropId = DataCatalog.CloudleafId,
                WateredNights = 2,
                QualityRoll = 80
            }
        ]);
        _session.Inventory.Add(DataCatalog.StarsoilFertilizerId, 3);
        _session.Inventory.Add(DataCatalog.StarbudLuminousId, 2);
        _session.Inventory.Add(DataCatalog.StarbudStarlightId, 1);
        _session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        );
        _session.SetPlayerState(12 * 16 + 8, 15 * 16 + 8, false);
        ShowFarm(false);
    }

    private void StartFarmingSpecializationPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.FarmingSkill.Experience = FarmingSkillCatalog.Levels[
            FarmingSkillSystem.SpecializationUnlockLevel
        ].RequiredExperience;
        _session.Restore(save);
        _session.SetPlayerState(20 * 16 + 8, 14 * 16 + 8, false);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
    }

    private void StartOrchardHivesPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);

        var treeCell = new GridPosition(23, 13);
        var hiveCell = new GridPosition(27, 13);
        var save = _session.Capture();
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
        _session.Restore(save);

        _session.Inventory.Add(DataCatalog.MoonplumSaplingId, 2);
        _session.Inventory.Add(DataCatalog.MoonplumId, 3);
        _session.Inventory.Add(DataCatalog.StarhoneyId, 2);
        _session.Inventory.Add(DataCatalog.GlowcombHiveId, 1);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 8);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.GlowcombHiveId);
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            hiveCell.X * 16 + 8,
            (hiveCell.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartFarmPlaceablesPlaytest()
    {
        StartNewGame();
        PlacePlaytestObjects(
            DataCatalog.MoonstonePathId,
            Enumerable.Range(20, 4).Select(x => new GridPosition(x, 13))
        );
        PlacePlaytestObjects(
            DataCatalog.StarwoodFenceId,
            Enumerable.Range(25, 4).Select(x => new GridPosition(x, 13))
        );
        PlacePlaytestObjects(
            DataCatalog.StarlightTorchId,
            [
                new GridPosition(19, 13),
                new GridPosition(30, 13)
            ]
        );
        PlacePlaytestObjects(
            DataCatalog.DewfallSprinklerId,
            [
                new GridPosition(15, 16),
                new GridPosition(21, 16),
                new GridPosition(28, 16)
            ]
        );
        foreach (var target in new[]
                 {
                     new GridPosition(15, 15),
                     new GridPosition(16, 16),
                     new GridPosition(15, 17),
                     new GridPosition(14, 16)
                 })
        {
            _session.Farm.TryTill(target, GameSession.MaxEnergy);
            _session.Farm.TryPlant(target, DataCatalog.StarbudId);
        }
        _session.Inventory.Add(DataCatalog.StarlightTorchId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarlightTorchId);
        _session.SetPlayerState(31 * 16 + 8, 12 * 16 + 8, false);
        ShowFarm(false);
    }

    private void PlacePlaytestObjects(
        string itemId,
        IEnumerable<GridPosition> positions
    )
    {
        var cells = positions.ToList();
        _session.Inventory.Add(itemId, cells.Count);
        _session.Inventory.PromoteToHotbar(itemId);
        foreach (var position in cells)
        {
            _session.UseSelected(position);
        }
    }

    private void StartChestPlacementPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        _session.SetPlayerState(25 * 16 + 8, 12 * 16 + 8, false);
        ShowFarm(false);
    }

    private void StartStoragePlaytest()
    {
        StartNewGame();
        var chest = new GridPosition(25, 13);
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        _session.UseSelected(chest);
        _session.Inventory.Add(DataCatalog.StarbudSeedId, 5);
        _session.Inventory.Add(DataCatalog.CloudleafId, 3);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 4);
        _session.StoreInChest(chest, DataCatalog.StarbudSeedId);
        _session.StoreInChest(chest, DataCatalog.CloudleafId);
        _session.Inventory.Select(0);
        _session.SetPlayerState(25 * 16 + 8, 14 * 16 + 8, false);
        ShowFarm(false);
        OpenStorage(chest);
    }

}
