namespace Luminfield.Core;

public sealed partial class GameSession
{
    public bool StarfeatherChickenCanGrazeToday => GrazingInstanceIdsFor(
        AnimalCatalog.StarfeatherCoopId
    ).Contains(AnimalCatalog.StarterStarfeatherChickenId);

    public bool StarfeatherChickenIsOutdoors =>
        VisibleAnimalProjections.Any(projection =>
            projection.InstanceId ==
                AnimalCatalog.StarterStarfeatherChickenId &&
            projection.IsOutdoors
        );

    public GridPosition? StarfeatherChickenWorldCell
    {
        get
        {
            var assignments = OutdoorAnimalAssignmentsFor(
                AnimalCatalog.StarfeatherCoopId
            );
            return assignments.TryGetValue(
                AnimalCatalog.StarterStarfeatherChickenId,
                out var cell
            )
                ? cell
                : null;
        }
    }

    public GridPosition? VisibleStarfeatherChickenCell =>
        VisibleAnimalProjections.FirstOrDefault(projection =>
            projection.InstanceId ==
                AnimalCatalog.StarterStarfeatherChickenId
        )?.Cell;

    public ActionResult CheckStarfeatherFeedTrough(GridPosition target) =>
        CheckAnimalFeedTrough(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult FeedStarfeatherCoop(GridPosition target) =>
        FeedAnimalBuilding(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult CheckAnimalFeedTrough(
        string buildingId,
        GridPosition target
    )
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            PlayerLocationId != spatial.LocationId ||
            target != spatial.FeedTroughCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return Animals.CheckFeedBuilding(
            buildingId,
            Clock.Day,
            GrazingInstanceIdsFor(buildingId),
            Inventory
        );
    }

    public ActionResult FeedAnimalBuilding(
        string buildingId,
        GridPosition target
    )
    {
        var check = CheckAnimalFeedTrough(buildingId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        var building = AnimalCatalog.Building(buildingId);
        var grazing = GrazingInstanceIdsFor(buildingId);
        var unfedCount = Animals.AnimalsInBuilding(buildingId)
            .Count(animal =>
                !grazing.Contains(animal.InstanceId) &&
                animal.LastFedDay != Clock.Day
            );
        BeginChangedBatch();
        try
        {
            if (!Inventory.Remove(building.FeedItemId, unfedCount))
            {
                return ActionResult.Fail(
                    "animal.feed.insufficient_fodder"
                );
            }

            Animals.FeedBuildingChecked(buildingId, Clock.Day, grazing);
            RecordFarmingSkillAction(
                FarmingSkillAction.FeedAnimal
            );
            GleamriseSeason.RecordMilestone(
                GleamriseSeasonGoalSystem.CounterAnimalFeedPrepared
            );
        }
        finally
        {
            EndChangedBatch();
        }

        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        return ActionResult.Success(messageKey: spatial.FeedCompletedKey);
    }

    public ActionResult CheckPetAnimal(
        string instanceId,
        GridPosition target
    )
    {
        var projection = AnimalProjectionAtCurrentLocation(target);
        if (projection is null ||
            projection.InstanceId != instanceId ||
            Distance(PlayerCell, target) != 1 ||
            Animals.Animal(instanceId) is null)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return Animals.CheckPet(instanceId, Clock.Day);
    }

    public ActionResult PetAnimal(
        string instanceId,
        GridPosition target
    )
    {
        var check = CheckPetAnimal(instanceId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            Animals.PetChecked(instanceId, Clock.Day);
            RecordFarmingSkillAction(
                FarmingSkillAction.PetAnimal
            );
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "animal.pet.completed");
    }

    public ActionResult CheckStarfeatherNest(GridPosition target) =>
        CheckAnimalProductStation(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult CollectStarfeatherEggs(GridPosition target) =>
        CollectAnimalProducts(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult CheckAnimalProductStation(
        string buildingId,
        GridPosition target
    )
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            PlayerLocationId != spatial.LocationId ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var station = spatial.ProductStations.FirstOrDefault(candidate =>
            candidate.Cell == target
        );
        if (station is null)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        var products = Animals.PendingProductsForBuilding(
            buildingId,
            station.ProductBaseItemId
        );
        if (products.Count == 0)
        {
            return ActionResult.Fail(station.NotReadyKey);
        }

        return Inventory.CanAddMany(products)
            ? ActionResult.Success()
            : ActionResult.Fail("notice.inventory_full");
    }

    public ActionResult CollectAnimalProducts(
        string buildingId,
        GridPosition target
    )
    {
        var check = CheckAnimalProductStation(buildingId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        var station = spatial.ProductStations.Single(candidate =>
            candidate.Cell == target
        );
        var products = Animals.PendingProductsForBuilding(
            buildingId,
            station.ProductBaseItemId
        );
        BeginChangedBatch();
        try
        {
            if (!Inventory.TryAddMany(products))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            Animals.ClearCollectedProductsChecked(
                buildingId,
                station.ProductBaseItemId
            );
            RecordFarmingSkillAction(
                FarmingSkillAction.CollectAnimalProduct
            );
            if (products.Any(product =>
                    DataCatalog.BaseItemId(product.ItemId) ==
                        DataCatalog.StarfeatherEggId
                ))
            {
                GleamriseSeason.RecordMilestone(
                    GleamriseSeasonGoalSystem.CounterAnimalFirstEgg
                );
            }
        }
        finally
        {
            EndChangedBatch();
        }

        return products.Count == 1
            ? ActionResult.Grant(
                products[0].ItemId,
                products[0].Count,
                0,
                station.CollectedKey
            )
            : ActionResult.Success(
                messageKey: station.CollectedKey
            );
    }

    public AnimalAutomationState AnimalAutomationFor(string buildingId) =>
        Animals.AutomationFor(buildingId);

    public int AnimalAutomationFeedNeedFor(string buildingId)
    {
        var grazing = GrazingInstanceIdsFor(buildingId);
        return Animals.AnimalsInBuilding(buildingId).Count(animal =>
            !grazing.Contains(animal.InstanceId) &&
            animal.LastFedDay != Clock.Day
        );
    }

    public ActionResult CheckAnimalAutomationStation(
        string buildingId,
        GridPosition target
    )
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            PlayerLocationId != spatial.LocationId ||
            target != spatial.AutomationStationCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var phase = Construction.PhaseFor(
            ConstructionCatalog.HomesteadLivestockAutomationProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return ActionResult.Fail(
                "construction.homestead_livestock_automation.not_started"
            );
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return ActionResult.Fail(
                "construction.homestead_livestock_automation.in_progress"
            );
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: "animal.automation.panel.opened"
        );
    }

    public ActionResult OpenAnimalAutomationStation(
        string buildingId,
        GridPosition target
    ) => CheckAnimalAutomationStation(buildingId, target);

    public ActionResult CheckDepositAnimalAutomationFeed(
        string buildingId,
        GridPosition target,
        int count
    )
    {
        var access = CheckAnimalAutomationStation(buildingId, target);
        return access.Succeeded
            ? Animals.CheckStoreAutomationFeed(buildingId, count, Inventory)
            : access;
    }

    public ActionResult DepositAnimalAutomationFeed(
        string buildingId,
        GridPosition target,
        int count
    )
    {
        var check = CheckDepositAnimalAutomationFeed(
            buildingId,
            target,
            count
        );
        if (!check.Succeeded)
        {
            return check;
        }

        var feedItemId = AnimalCatalog.Building(buildingId).FeedItemId;
        BeginChangedBatch();
        try
        {
            if (!Inventory.Remove(feedItemId, count))
            {
                return ActionResult.Fail(
                    "animal.feed.insufficient_fodder"
                );
            }

            Animals.StoreAutomationFeedChecked(buildingId, count);
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(
            messageKey: "animal.automation.feed_deposited"
        );
    }

    public ActionResult CheckWithdrawAnimalAutomationFeed(
        string buildingId,
        GridPosition target,
        int count
    )
    {
        var access = CheckAnimalAutomationStation(buildingId, target);
        return access.Succeeded
            ? Animals.CheckTakeAutomationFeed(buildingId, count, Inventory)
            : access;
    }

    public ActionResult WithdrawAnimalAutomationFeed(
        string buildingId,
        GridPosition target,
        int count
    )
    {
        var check = CheckWithdrawAnimalAutomationFeed(
            buildingId,
            target,
            count
        );
        if (!check.Succeeded)
        {
            return check;
        }

        var feedItemId = AnimalCatalog.Building(buildingId).FeedItemId;
        BeginChangedBatch();
        try
        {
            if (!Inventory.Add(feedItemId, count))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            Animals.TakeAutomationFeedChecked(buildingId, count);
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(
            messageKey: "animal.automation.feed_withdrawn"
        );
    }

    public ActionResult CheckCollectAnimalAutomationProducts(
        string buildingId,
        GridPosition target
    )
    {
        var access = CheckAnimalAutomationStation(buildingId, target);
        if (!access.Succeeded)
        {
            return access;
        }

        var products = Animals.StoredAutomationProducts(buildingId);
        if (products.Count == 0)
        {
            return ActionResult.Fail("animal.automation.no_products");
        }

        return Inventory.CanAddMany(products)
            ? ActionResult.Success()
            : ActionResult.Fail("notice.inventory_full");
    }

    public ActionResult CollectAnimalAutomationProducts(
        string buildingId,
        GridPosition target
    )
    {
        var check = CheckCollectAnimalAutomationProducts(buildingId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        var products = Animals.StoredAutomationProducts(buildingId);
        BeginChangedBatch();
        try
        {
            if (!Inventory.TryAddMany(products))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            Animals.ClearAutomationProductsChecked(buildingId);
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(
            messageKey: "animal.automation.products_collected"
        );
    }

    private ActionResult UseAnimalBuildingSelected(
        string buildingId,
        GridPosition target
    )
    {
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        if (target == spatial.ExitCell)
        {
            return TryExitAnimalBuilding(buildingId, target);
        }

        if (target == spatial.FeedTroughCell)
        {
            return FeedAnimalBuilding(buildingId, target);
        }

        if (target == spatial.AutomationStationCell)
        {
            return OpenAnimalAutomationStation(buildingId, target);
        }

        if (spatial.ProductStations.Any(station => station.Cell == target))
        {
            return CollectAnimalProducts(buildingId, target);
        }

        return AnimalAtCurrentLocation(target) is { } animal
            ? PetAnimal(animal.InstanceId, target)
            : ActionResult.Fail("notice.nothing_to_interact");
    }

    private TargetPreview PreviewAnimalBuildingEntrance(
        string buildingId,
        GridPosition target
    )
    {
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        var check = CheckAnimalBuildingEntrance(buildingId, target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                spatial.WorldDoorCell,
                spatial.PortalKind,
                spatial.EnterActionKey
            );
        }

        if (check.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                spatial.WorldDoorCell,
                spatial.PortalKind,
                "target.need.hand"
            );
        }

        if (check.MessageKey.StartsWith(
                "construction.",
                StringComparison.Ordinal
            ))
        {
            return TargetPreview.Blocked(
                spatial.WorldDoorCell,
                spatial.PortalKind,
                check.MessageKey
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewAnimalBuildingTarget(
        string buildingId,
        GridPosition target
    )
    {
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        if (PlayerLocationId != spatial.LocationId)
        {
            return TargetPreview.Neutral(target);
        }

        if (target == spatial.ExitCell)
        {
            var exit = CheckAnimalBuildingExit(buildingId, target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    spatial.ExitKind,
                    spatial.ExitActionKey
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        spatial.ExitKind,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        if (target == spatial.FeedTroughCell)
        {
            var feed = CheckAnimalFeedTrough(buildingId, target);
            if (feed.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.AnimalFeedTrough,
                    "target.action.feed_animals"
                );
            }

            if (feed.MessageKey == "notice.needs_hand")
            {
                return TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.AnimalFeedTrough,
                    "target.need.hand"
                );
            }

            var feedLabel = feed.MessageKey switch
            {
                "animal.feed.grazing" => "target.status.animals_grazing",
                "animal.feed.all_fed" => "target.status.animals_fed",
                "animal.feed.no_animals" => "target.status.no_animals",
                _ => "target.blocked.no_fodder"
            };
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.AnimalFeedTrough,
                feedLabel
            );
        }

        if (target == spatial.AutomationStationCell)
        {
            var automation = CheckAnimalAutomationStation(
                buildingId,
                target
            );
            if (automation.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.AnimalAutomationStation,
                    "target.action.open_livestock_automation"
                );
            }

            if (automation.MessageKey == "notice.needs_hand")
            {
                return TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.AnimalAutomationStation,
                    "target.need.hand"
                );
            }

            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.AnimalAutomationStation,
                automation.MessageKey
            );
        }

        var station = spatial.ProductStations.FirstOrDefault(candidate =>
            candidate.Cell == target
        );
        if (station is not null)
        {
            var product = CheckAnimalProductStation(buildingId, target);
            if (product.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    station.Kind,
                    station.ActionKey
                );
            }

            if (product.MessageKey == "notice.needs_hand")
            {
                return TargetPreview.NeedsTool(
                    target,
                    station.Kind,
                    "target.need.hand"
                );
            }

            return TargetPreview.Blocked(
                target,
                station.Kind,
                product.MessageKey == "notice.inventory_full"
                    ? "target.blocked.backpack_full"
                    : station.NotReadyStatusKey
            );
        }

        return AnimalAtCurrentLocation(target) is { } animal
            ? PreviewAnimal(animal, target)
            : TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewAnimal(
        AnimalState animal,
        GridPosition target
    )
    {
        var kind = animal.SpeciesId switch
        {
            AnimalCatalog.MoonfleeceSheepId =>
                TargetPreviewKind.MoonfleeceSheep,
            AnimalCatalog.DewhornId => TargetPreviewKind.Dewhorn,
            _ => TargetPreviewKind.Animal
        };
        var check = CheckPetAnimal(animal.InstanceId, target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                kind,
                "target.action.pet_animal"
            );
        }

        return check.MessageKey switch
        {
            "notice.needs_hand" => TargetPreview.NeedsTool(
                target,
                kind,
                "target.need.hand"
            ),
            "animal.pet.already_petted" => TargetPreview.Blocked(
                target,
                kind,
                "target.status.animal_petted"
            ),
            _ => TargetPreview.Neutral(target)
        };
    }

    private AnimalState? AnimalAtCurrentLocation(GridPosition target)
    {
        var projection = AnimalProjectionAtCurrentLocation(target);
        return projection is null
            ? null
            : Animals.Animal(projection.InstanceId);
    }

    private AnimalProjection? AnimalProjectionAtCurrentLocation(
        GridPosition target
    ) => VisibleAnimalProjections.FirstOrDefault(projection =>
        projection.Cell == target
    );

    private IReadOnlyDictionary<string, GridPosition>
        OutdoorAnimalAssignmentsFor(string buildingId)
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            !AnimalCatalog.TryBuilding(buildingId, out var building) ||
            !Construction.IsCompletedFor(building.ConstructionProjectId) ||
            !AnimalSystem.CanGraze(Clock.Day, Weather.CurrentId))
        {
            return new Dictionary<string, GridPosition>(
                StringComparer.Ordinal
            );
        }

        var available = spatial.WorldPastureCells.Where(cell =>
            !Storage.HasChest(cell) &&
            FarmObjects.ItemAt(cell) is null &&
            !Orchard.BlocksMovement(cell) &&
            string.IsNullOrWhiteSpace(
                Farm.Tiles.GetValueOrDefault(cell)?.CropId
            )
        ).ToArray();
        return Animals.AnimalsInBuilding(buildingId)
            .Zip(available, (animal, cell) => (animal.InstanceId, cell))
            .ToDictionary(
                pair => pair.InstanceId,
                pair => pair.cell,
                StringComparer.Ordinal
            );
    }

    private IReadOnlySet<string> GrazingInstanceIdsFor(string buildingId) =>
        OutdoorAnimalAssignmentsFor(buildingId).Keys.ToHashSet(
            StringComparer.Ordinal
        );

    private ActionResult UseGreenhouseSelected(GridPosition target)
    {
        if (target == GreenhouseLayout.ExitCell)
        {
            return TryExitGreenhouse(target);
        }

        if (target == GreenhouseLayout.CisternCell)
        {
            return RefillFromGreenhouseCistern(target);
        }

        if (!GreenhouseLayout.IsPlantingBed(target))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var selected = Inventory.Selected;
        ActionResult result;
        FarmingSkillAction? farmingSkillAction = null;
        switch (selected.ItemId)
        {
            case DataCatalog.HandId:
                return HarvestGreenhouseCrop(target);
            case DataCatalog.ShovelId:
                result = GreenhouseFarm.TryTill(target, Energy);
                if (result.Succeeded)
                {
                    Quest.OnTilled();
                    farmingSkillAction = FarmingSkillAction.Till;
                }
                break;
            case DataCatalog.WateringCanId:
                if (WateringCanWater <= 0)
                {
                    return ActionResult.Fail("notice.watering_can_empty");
                }

                var cropId = GreenhouseFarm.Tiles
                    .GetValueOrDefault(target)?.CropId;
                result = GreenhouseFarm.TryWater(
                    target,
                    Energy,
                    EffectiveWateringEnergyCost
                );
                if (result.Succeeded)
                {
                    WateringCanWater--;
                    WaterChanged?.Invoke();
                    Quest.OnWatered(cropId);
                    farmingSkillAction = FarmingSkillAction.Water;
                }
                break;
            default:
                var item = DataCatalog.Item(selected.ItemId);
                if (item.Kind == ItemKind.Fertilizer)
                {
                    result = GreenhouseFarm.TryFertilize(target, item.Id);
                    if (result.Succeeded)
                    {
                        Inventory.Remove(item.Id, 1);
                    }
                    break;
                }

                if (item.Kind != ItemKind.Seed || item.CropId is null)
                {
                    return ActionResult.Fail("notice.not_ready");
                }

                if (selected.Count <= 0)
                {
                    return ActionResult.Fail("notice.no_seed");
                }

                result = GreenhouseFarm.TryPlant(
                    target,
                    item.CropId,
                    Clock.Day
                );
                if (result.Succeeded)
                {
                    Inventory.Remove(selected.ItemId, 1);
                    Quest.OnPlanted(item.CropId);
                    Commission.RecordPlant(item.CropId);
                    WeeklyCommission.RecordPlant(item.CropId);
                    farmingSkillAction = FarmingSkillAction.Plant;
                }
                break;
        }

        if (result.Succeeded && result.EnergyCost > 0)
        {
            Energy = Math.Max(0, Energy - result.EnergyCost);
            EnergyChanged?.Invoke();
            Changed?.Invoke();
        }

        if (result.Succeeded && farmingSkillAction is { } successfulAction)
        {
            RecordFarmingSkillAction(successfulAction);
        }

        return result;
    }

    private ActionResult HarvestGreenhouseCrop(GridPosition target)
    {
        var tile = GreenhouseFarm.Tiles.GetValueOrDefault(target);
        if (tile?.CropId is null)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var crop = DataCatalog.Crop(tile.CropId);
        if (!crop.IsMature(tile.WateredNights))
        {
            return ActionResult.Fail("notice.not_ready");
        }

        var harvestItemId = GreenhouseFarm.HarvestItemIdAt(target)
            ?? crop.HarvestItemId;
        if (!Inventory.CanAdd(harvestItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var harvested = GreenhouseFarm.TryHarvest(target);
        if (!harvested.Succeeded || harvested.GrantedItemId is null)
        {
            return harvested;
        }

        Inventory.Add(
            harvested.GrantedItemId,
            harvested.GrantedItemCount
        );
        var baseItemId = DataCatalog.BaseItemId(harvested.GrantedItemId);
        Quest.OnHarvested(baseItemId);
        Commission.RecordGather(baseItemId, harvested.GrantedItemCount);
        WeeklyCommission.RecordGather(
            baseItemId,
            harvested.GrantedItemCount
        );
        RecordFarmingSkillAction(FarmingSkillAction.Harvest);
        return harvested;
    }

    private ActionResult RefillFromGreenhouseCistern(GridPosition target)
    {
        var check = CheckGreenhouseCistern(target);
        if (!check.Succeeded)
        {
            return check;
        }

        WateringCanWater = MaxWateringCanWater;
        WaterChanged?.Invoke();
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "notice.water_refilled");
    }

    private TargetPreview PreviewGreenhouseEntrance(GridPosition target)
    {
        var check = CheckGreenhouseEntrance(target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                FarmLayout.GreenhouseDoorCell,
                TargetPreviewKind.GreenhousePortal,
                "target.action.enter_greenhouse"
            );
        }

        if (check.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                FarmLayout.GreenhouseDoorCell,
                TargetPreviewKind.GreenhousePortal,
                "target.need.hand"
            );
        }

        if (check.MessageKey is
            "construction.homestead_greenhouse.not_started" or
            "construction.homestead_greenhouse.in_progress")
        {
            return TargetPreview.Blocked(
                FarmLayout.GreenhouseDoorCell,
                TargetPreviewKind.GreenhousePortal,
                check.MessageKey
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewGreenhouseTarget(GridPosition target)
    {
        if (!GreenhouseLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        if (target == GreenhouseLayout.ExitCell)
        {
            var exit = CheckGreenhouseExit(target);
            if (exit.Succeeded)
            {
                return TargetPreview.Available(
                    GreenhouseLayout.ExitCell,
                    TargetPreviewKind.GreenhouseExit,
                    "target.action.exit_greenhouse"
                );
            }

            return exit.MessageKey == "notice.needs_hand"
                ? TargetPreview.NeedsTool(
                    GreenhouseLayout.ExitCell,
                    TargetPreviewKind.GreenhouseExit,
                    "target.need.hand"
                )
                : TargetPreview.Neutral(target);
        }

        if (target == GreenhouseLayout.CisternCell)
        {
            var cistern = CheckGreenhouseCistern(target);
            if (cistern.Succeeded)
            {
                return TargetPreview.Available(
                    GreenhouseLayout.CisternCell,
                    TargetPreviewKind.Cistern,
                    "target.action.draw_water"
                );
            }

            return cistern.MessageKey switch
            {
                "target.need.bucket" => TargetPreview.NeedsTool(
                    GreenhouseLayout.CisternCell,
                    TargetPreviewKind.Cistern,
                    "target.need.bucket"
                ),
                "notice.water_full" => TargetPreview.Blocked(
                    GreenhouseLayout.CisternCell,
                    TargetPreviewKind.Cistern,
                    "target.status.water_full"
                ),
                _ => TargetPreview.Neutral(target)
            };
        }

        return PreviewGreenhouseCultivationTarget(target);
    }

    private TargetPreview PreviewGreenhouseCultivationTarget(
        GridPosition target
    )
    {
        var selected = Inventory.Selected;
        var selectedId = selected.IsEmpty ? string.Empty : selected.ItemId;
        if (!GreenhouseLayout.IsPlantingBed(target))
        {
            return selectedId == DataCatalog.StarsoilFertilizerId
                ? TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Ground,
                    "target.blocked.fertilizer_needs_tilled"
                )
                : TargetPreview.Neutral(target);
        }

        GreenhouseFarm.Tiles.TryGetValue(target, out var tile);
        if (!string.IsNullOrWhiteSpace(tile?.CropId))
        {
            if (selectedId == DataCatalog.StarsoilFertilizerId)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Crop,
                    "target.blocked.fertilizer_before_planting"
                );
            }

            var crop = DataCatalog.Crop(tile.CropId);
            if (crop.IsMature(tile.WateredNights))
            {
                if (selectedId != DataCatalog.HandId)
                {
                    return TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.Crop,
                        "target.need.hand"
                    );
                }

                var harvestItemId = GreenhouseFarm.HarvestItemIdAt(target)
                    ?? crop.HarvestItemId;
                return Inventory.CanAdd(harvestItemId, 1)
                    ? TargetPreview.Available(
                        target,
                        TargetPreviewKind.Crop,
                        "target.action.harvest"
                    )
                    : TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Crop,
                        "target.blocked.backpack_full"
                    );
            }

            if (tile.Watered)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Crop,
                    "target.status.watered"
                );
            }

            if (selectedId != DataCatalog.WateringCanId)
            {
                return TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.Crop,
                    "target.need.watering_can"
                );
            }

            if (WateringCanWater <= 0)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Crop,
                    "target.blocked.no_water"
                );
            }

            return Energy < EffectiveWateringEnergyCost
                ? TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Crop,
                    "target.blocked.no_energy"
                )
                : TargetPreview.Available(
                    target,
                    TargetPreviewKind.Crop,
                    "target.action.water"
                );
        }

        if (tile?.Tilled == true)
        {
            if (selectedId == DataCatalog.StarsoilFertilizerId)
            {
                if (!string.IsNullOrWhiteSpace(tile.FertilizerId))
                {
                    return TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Soil,
                        "target.status.fertilized"
                    );
                }

                return selected.Count > 0
                    ? TargetPreview.Available(
                        target,
                        TargetPreviewKind.Soil,
                        "target.action.fertilize"
                    )
                    : TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Soil,
                        "target.blocked.no_fertilizer"
                    );
            }

            if (DataCatalog.Items.TryGetValue(selectedId, out var item) &&
                item.Kind == ItemKind.Seed &&
                item.CropId is not null)
            {
                if (!GreenhouseFarm.IsCropAvailableForPlanting(
                        item.CropId,
                        Clock.Day
                    ))
                {
                    return TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Soil,
                        "target.blocked.seed_out_of_season"
                    );
                }

                return selected.Count > 0
                    ? TargetPreview.Available(
                        target,
                        TargetPreviewKind.Soil,
                        "target.action.plant"
                    )
                    : TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Soil,
                        "target.blocked.no_seed"
                    );
            }

            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.Soil,
                "target.need.seed"
            );
        }

        if (selectedId == DataCatalog.StarsoilFertilizerId)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.Ground,
                "target.blocked.fertilizer_needs_tilled"
            );
        }

        if (selectedId != DataCatalog.ShovelId)
        {
            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.Ground,
                "target.need.shovel_till"
            );
        }

        return Energy < 2
            ? TargetPreview.Blocked(
                target,
                TargetPreviewKind.Ground,
                "target.blocked.no_energy"
            )
            : TargetPreview.Available(
                target,
                TargetPreviewKind.Ground,
                "target.action.till"
            );
    }

    private ActionResult CheckGreenhouseEntrance(GridPosition target)
    {
        if (PlayerLocationId != PlayerLocationIds.World ||
            target != FarmLayout.GreenhouseDoorCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var phase = Construction.PhaseFor(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return ActionResult.Fail(
                "construction.homestead_greenhouse.not_started"
            );
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return ActionResult.Fail(
                "construction.homestead_greenhouse.in_progress"
            );
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success()
            : ActionResult.Fail("notice.needs_hand");
    }

    private ActionResult CheckStarfeatherCoopEntrance(GridPosition target) =>
        CheckAnimalBuildingEntrance(AnimalCatalog.StarfeatherCoopId, target);

    private ActionResult CheckAnimalBuildingEntrance(
        string buildingId,
        GridPosition target
    )
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            !AnimalCatalog.TryBuilding(buildingId, out var building) ||
            PlayerLocationId != PlayerLocationIds.World ||
            target != spatial.WorldDoorCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var phase = Construction.PhaseFor(
            building.ConstructionProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return ActionResult.Fail(
                $"construction.{building.ConstructionProjectId}.not_started"
            );
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return ActionResult.Fail(
                $"construction.{building.ConstructionProjectId}.in_progress"
            );
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success()
            : ActionResult.Fail("notice.needs_hand");
    }

    private ActionResult CheckStarfeatherCoopExit(GridPosition target) =>
        CheckAnimalBuildingExit(AnimalCatalog.StarfeatherCoopId, target);

    private ActionResult CheckAnimalBuildingExit(
        string buildingId,
        GridPosition target
    )
    {
        if (!AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ) ||
            PlayerLocationId != spatial.LocationId ||
            target != spatial.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success()
            : ActionResult.Fail("notice.needs_hand");
    }

    private ActionResult CheckGreenhouseExit(GridPosition target)
    {
        if (!InsideGreenhouse ||
            target != GreenhouseLayout.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success()
            : ActionResult.Fail("notice.needs_hand");
    }

    private ActionResult CheckGreenhouseCistern(GridPosition target)
    {
        if (!InsideGreenhouse ||
            target != GreenhouseLayout.CisternCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.BucketId)
        {
            return ActionResult.Fail("target.need.bucket");
        }

        return WateringCanWater >= MaxWateringCanWater
            ? ActionResult.Fail("notice.water_full")
            : ActionResult.Success();
    }

    private static int Distance(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

}
