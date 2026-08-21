namespace Luminfield.Core;

public sealed class GameSession
{
    public const int MaxEnergy = 100;
    public const int MaxWateringCanWater = 12;
    public const int NewGameCoins = 60;
    public const float NewGamePlayerX = 504;
    public const float NewGamePlayerY = 152;

    public GameClock Clock { get; } = new();
    public Inventory Inventory { get; } = new();
    public FarmSystem Farm { get; } = new();
    public FarmSystem GreenhouseFarm { get; } = new(
        CultivationZoneCatalog.Greenhouse
    );
    public QuestSystem Quest { get; } = new();
    public ProcessorSystem Processor { get; } = new();
    public ExplorationSystem Exploration { get; } = new();
    public WorldResourceSystem Resources { get; } = new();
    public MiningSystem Mining { get; } = new();
    public DeepMineSystem DeepMine { get; } = new();
    public ToolProgressionSystem ToolProgression { get; } = new();
    public CombatSystem Combat { get; } = new();
    public StarfallRuinsTrialSystem StarfallRuinsTrial { get; } = new();
    public ForageSystem Forage { get; } = new();
    public FishingSystem Fishing { get; } = new();
    public FishingProgressionSystem FishingProgression { get; } = new();
    public FishingMinigameSystem FishingMinigame { get; } = new();
    public CrabPotSystem CrabPots { get; } = new();
    public WeatherSystem Weather { get; } = new();
    public ShippingBinSystem Shipping { get; } = new();
    public CraftingSystem Crafting { get; } = new();
    public KitchenSystem Kitchen { get; } = new();
    public StorageSystem Storage { get; } = new();
    public FarmObjectSystem FarmObjects { get; } = new();
    public OrchardSystem Orchard { get; } = new();
    public DailyCommissionSystem Commission { get; } = new();
    public WeeklyCommissionSystem WeeklyCommission { get; } = new();
    public StarlightSystem Starlight { get; } = new();
    public VillageSystem Village { get; }
    public MailSystem Mail { get; } = new();
    public CharacterEventSystem CharacterEvents { get; } = new();
    public ConstructionSystem Construction { get; } = new();
    public StarGateSystem StarGate { get; } = new();
    public FarmingSkillSystem FarmingSkill { get; } = new();
    public GleamriseSeasonGoalSystem GleamriseSeason { get; } = new();
    public FestivalSystem Festival { get; } = new();
    public AnimalSystem Animals { get; } = new();
    public CollectionSystem Collection { get; } = new();

    private bool _suppressChanged;
    private bool _changedWhileSuppressed;
    private bool _restoring;

    public int Energy { get; private set; } = MaxEnergy;
    public int WateringCanWater { get; private set; } = MaxWateringCanWater;
    public int Coins { get; private set; } = NewGameCoins;
    public int LastRespawnedResources { get; private set; }
    public float PlayerX { get; private set; } = NewGamePlayerX;
    public float PlayerY { get; private set; } = NewGamePlayerY;
    public string PlayerLocationId { get; private set; } =
        PlayerLocationIds.World;
    public GridPosition PlayerCell => new(
        (int)MathF.Floor(PlayerX / 16),
        (int)MathF.Floor(PlayerY / 16)
    );
    public bool InsideCottage =>
        PlayerLocationId == PlayerLocationIds.Cottage;
    public int CottageUpgradeLevel => Construction.IsCompletedFor(
        ConstructionCatalog.CottageSecondUpgradeId
    )
        ? 2
        : Construction.IsCompletedFor(
            ConstructionCatalog.CottageFirstUpgradeId
        )
            ? 1
            : 0;
    public bool InsideArchive =>
        PlayerLocationId == PlayerLocationIds.MoonlitArchive;
    public bool InsideWorkshop =>
        PlayerLocationId == PlayerLocationIds.MoonstoneWorkshop;
    public bool InsideTeaHouse =>
        PlayerLocationId == PlayerLocationIds.StarweaverTeaHouse;
    public bool InsideTwilightEmporium =>
        PlayerLocationId == PlayerLocationIds.TwilightEmporium;
    public bool InsideStarlightPost =>
        PlayerLocationId == PlayerLocationIds.StarlightPost;
    public bool InsideStarfallWatch =>
        PlayerLocationId == PlayerLocationIds.StarfallWatch;
    public bool InsideGreenhouse =>
        PlayerLocationId == PlayerLocationIds.Greenhouse;
    public bool InsideStarfeatherCoop =>
        PlayerLocationId == PlayerLocationIds.StarfeatherCoop;
    public bool InsideMoonfleeceBarn =>
        PlayerLocationId == PlayerLocationIds.MoonfleeceBarn;
    public bool LivestockAutomationUnlocked =>
        Construction.IsCompletedFor(
            ConstructionCatalog.HomesteadLivestockAutomationProjectId
        );
    public int BeehivePollinationRange =>
        Starlight.MeadowPollinationUnlocked
            ? OrchardSystem.FarReachingBeehivePollinationRange
            : OrchardSystem.BeehivePollinationRange;
    public bool ForageMapUnlocked => Collection.IsRewardClaimed(
        CollectionRewardIds.StarpathForagersGuide
    );
    public string? CurrentAnimalBuildingId =>
        AnimalBuildingSpatialCatalog.TryByLocationId(
            PlayerLocationId,
            out var animalBuilding
        )
            ? animalBuilding.BuildingId
            : null;
    public bool InsideStarharvestMarket =>
        PlayerLocationId == PlayerLocationIds.StarharvestMarket;
    public bool InsideGleamrisePlantingFestival =>
        PlayerLocationId == PlayerLocationIds.GleamrisePlantingFestival;
    public bool InsideLongnightLanternFeast =>
        PlayerLocationId == PlayerLocationIds.LongnightLanternFeast;
    public bool InsideFireflyTide =>
        PlayerLocationId == PlayerLocationIds.FireflyTide;
    public bool InsideCrystalGrottoSurvey =>
        PlayerLocationId == PlayerLocationIds.CrystalGrottoSurvey;
    public bool InsideStarfallRuinsTrial =>
        PlayerLocationId == PlayerLocationIds.StarfallRuinsTrial;
    public IReadOnlyList<StarfallTrialEnemySnapshot>
        VisibleStarfallTrialEnemies => InsideStarfallRuinsTrial
            ? StarfallRuinsTrial.Enemies()
            : [];
    public bool IsStarfallRuinsTrialCellWalkable(GridPosition cell) =>
        InsideStarfallRuinsTrial &&
        StarfallRuinsTrial.IsCellPassable(cell);
    public string? CurrentFestivalId =>
        FestivalCatalog.FestivalAtLocation(PlayerLocationId)?.Id;
    public float PlayerMovementMultiplier =>
        PlayerLocationId == PlayerLocationIds.World
            ? Weather.Current.OutdoorMovementMultiplier
            : 1f;
    public string Locale { get; private set; } = LocaleService.SimplifiedChinese;

    public event Action? Changed;
    public event Action? EnergyChanged;
    public event Action? WaterChanged;
    public event Action? DayEnded;
    public event Action? PlayerMoved;
    public event Action<string>? CollectionEntryDiscovered;

    public GameSession()
    {
        Village = new VillageSystem(Weather);
        Clock.TimeChanged += NotifyChanged;
        Clock.TimeChanged += ResolveFestivalAttemptsForCurrentTime;
        Inventory.Changed += OnInventoryChanged;
        Farm.TileChanged += _ => NotifyChanged();
        GreenhouseFarm.TileChanged += _ => NotifyChanged();
        Quest.Changed += NotifyChanged;
        Processor.Changed += NotifyChanged;
        Exploration.Changed += NotifyChanged;
        Resources.Changed += _ => NotifyChanged();
        Mining.Changed += _ => NotifyChanged();
        DeepMine.Changed += NotifyChanged;
        ToolProgression.Changed += NotifyChanged;
        Combat.Changed += NotifyChanged;
        StarfallRuinsTrial.Changed += NotifyChanged;
        Forage.Changed += _ => NotifyChanged();
        Fishing.Changed += NotifyChanged;
        FishingProgression.Changed += NotifyChanged;
        FishingMinigame.Changed += NotifyChanged;
        CrabPots.Changed += NotifyChanged;
        Weather.Changed += NotifyChanged;
        Shipping.Changed += NotifyChanged;
        Kitchen.Changed += NotifyChanged;
        Storage.Changed += _ => NotifyChanged();
        FarmObjects.Changed += _ => NotifyChanged();
        Orchard.Changed += _ => NotifyChanged();
        Commission.Changed += NotifyChanged;
        WeeklyCommission.Changed += NotifyChanged;
        Starlight.Changed += NotifyChanged;
        Village.Changed += NotifyChanged;
        Mail.Changed += NotifyChanged;
        CharacterEvents.Changed += NotifyChanged;
        Construction.Changed += NotifyChanged;
        StarGate.Changed += NotifyChanged;
        FarmingSkill.Changed += NotifyChanged;
        GleamriseSeason.Changed += NotifyChanged;
        Festival.Changed += NotifyChanged;
        Animals.Changed += NotifyChanged;
        Collection.Changed += NotifyChanged;
        Collection.EntryDiscovered += entryId =>
        {
            if (!_restoring)
            {
                CollectionEntryDiscovered?.Invoke(entryId);
            }
        };
    }

    public void NewGame(string locale = LocaleService.SimplifiedChinese)
    {
        Clock.Reset();
        Collection.Reset();
        Inventory.Reset();
        Farm.Reset();
        GreenhouseFarm.Reset();
        Quest.Reset();
        Processor.Reset();
        Exploration.Reset();
        Resources.Reset();
        Mining.Reset();
        DeepMine.Reset();
        ToolProgression.Reset();
        Combat.Reset();
        StarfallRuinsTrial.Reset();
        Weather.Reset(Clock.Day);
        Forage.Reset(Clock.Day, Weather.CurrentId);
        Fishing.Reset();
        FishingProgression.Reset();
        FishingMinigame.Reset();
        CrabPots.Reset();
        Shipping.Reset();
        Kitchen.Reset();
        Storage.Reset();
        FarmObjects.Reset();
        Orchard.Reset();
        Commission.Reset(Clock.Day);
        WeeklyCommission.Reset(Clock.Day);
        Starlight.Reset();
        Village.Reset();
        Mail.Reset();
        CharacterEvents.Reset();
        Construction.Reset();
        StarGate.Reset();
        FarmingSkill.Reset();
        GleamriseSeason.Reset(Clock.Day);
        Festival.Reset();
        Animals.Reset();
        Energy = MaxEnergy;
        WateringCanWater = MaxWateringCanWater;
        Coins = NewGameCoins;
        LastRespawnedResources = 0;
        PlayerX = NewGamePlayerX;
        PlayerY = NewGamePlayerY;
        PlayerLocationId = PlayerLocationIds.World;
        Locale = locale;
        EnergyChanged?.Invoke();
        WaterChanged?.Invoke();
        Changed?.Invoke();
    }

    public void Restore(GameSaveV1 save)
    {
        _restoring = true;
        Clock.Reset(save.Day, save.MinuteOfDay);
        Collection.Restore(
            save.Collection,
            CollectionSystem.LegacyEvidenceItemIds(save)
        );
        Inventory.Restore(save.Inventory, save.Player.SelectedSlot);
        Farm.Restore(save.FarmTiles);
        GreenhouseFarm.Restore(save.Greenhouse?.Tiles);
        Storage.Restore(save.Storage, Farm);
        FarmObjects.Restore(save.FarmObjects, Farm, Storage);
        Orchard.Restore(save.Orchard, Farm, Storage, FarmObjects);
        Quest.Restore(save.Quest);
        Processor.Restore(save.Processor);
        Exploration.Restore(save.Exploration);
        Festival.Restore(save.Festival);
        Mining.Restore(save.Mining);
        DeepMine.Restore(save.Mining);
        ToolProgression.Restore(save.ToolProgression);
        Combat.Restore(save.Combat);
        StarfallRuinsTrial.Restore(save.StarfallRuinsTrial);
        if (Inventory.Count(DataCatalog.MoonsteelShortbladeId) > 0 ||
            Storage.Capture().Chests.Any(chest => chest.Items.Any(item =>
                item.ItemId == DataCatalog.MoonsteelShortbladeId &&
                item.Count > 0
            )))
        {
            StarfallRuinsTrial.EnsureWeaponClaimed();
        }
        Village.Restore(save.Village);
        Starlight.Restore(
            save.Starlight,
            StarlightProgress(includeLivePedestals: false)
        );
        Mail.Restore(save.Mail);
        CharacterEvents.Restore(save.CharacterEvents, save.Day);
        Construction.Restore(save.Construction);
        StarGate.Restore(
            save.StarGate,
            Construction.IsCompletedFor(
                ConstructionCatalog.SixfoldStarGateProjectId
            )
        );
        Animals.Restore(save.Animals, save.Day);
        EnsureCompletedAnimalStarters();
        EnsureCompletedAnimalAutomation();
        FarmingSkill.Restore(save.FarmingSkill);
        GleamriseSeason.Restore(save.GleamriseSeason, save.Day);
        ResolveFestivalAttemptsForCurrentTime();
        Resources.Restore(
            save.Resources,
            save.Day,
            Starlight.WoodlandRenewalUnlocked
        );
        Weather.Restore(save.Weather, save.Day);
        Forage.Restore(save.Forage, save.Day, Weather.CurrentId);
        Fishing.Restore(save.Fishing);
        FishingProgression.Restore(save.Fishing);
        FishingMinigame.Reset();
        CrabPots.Restore(save.Fishing);
        Shipping.Restore(save.Shipping);
        Kitchen.Restore(save.Kitchen);
        Commission.Restore(save.Commission, save.Day);
        WeeklyCommission.Restore(save.WeeklyCommission, save.Day);
        if (Weather.Current.AutoWatersCrops)
        {
            Farm.ApplyWeatherWatering();
        }
        Energy = Math.Clamp(save.Player.Energy, 0, MaxEnergy);
        WateringCanWater = Math.Clamp(
            save.Player.WateringCanWater,
            0,
            MaxWateringCanWater
        );
        Coins = Math.Max(0, save.Coins);
        LastRespawnedResources = 0;
        PlayerX = save.Player.X;
        PlayerY = save.Player.Y;
        PlayerLocationId = PlayerLocationIds.Normalize(
            save.Player.LocationId,
            save.Player.InsideCottage
        );
        NormalizeCottagePlayerPositionForUpgrade();
        NormalizeGreenhousePlayerPosition();
        NormalizeAnimalBuildingPlayerPosition();
        NormalizeFestivalPlayerPosition();
        NormalizeCrystalGrottoSurveyPlayerPosition();
        NormalizeStarfallRuinsTrialPlayerPosition();
        Locale = save.Locale;
        _restoring = false;
        EnergyChanged?.Invoke();
        WaterChanged?.Invoke();
        Changed?.Invoke();
    }

    public void SetLocale(string locale)
    {
        Locale = locale;
        Changed?.Invoke();
    }

    public void SetPlayerState(float x, float y, bool insideCottage)
    {
        SetPlayerLocation(
            x,
            y,
            insideCottage
                ? PlayerLocationIds.Cottage
                : PlayerLocationIds.World
        );
    }

    public void SetPlayerLocation(float x, float y, string locationId)
    {
        var previousLocationId = PlayerLocationId;
        PlayerX = x;
        PlayerY = y;
        PlayerLocationId = PlayerLocationIds.Normalize(locationId);
        if (previousLocationId == PlayerLocationIds.StarfallRuinsTrial &&
            PlayerLocationId != PlayerLocationIds.StarfallRuinsTrial)
        {
            StarfallRuinsTrial.ResetUnclearedRooms();
        }
        else if (previousLocationId != PlayerLocationIds.StarfallRuinsTrial &&
            PlayerLocationId == PlayerLocationIds.StarfallRuinsTrial)
        {
            StarfallRuinsTrial.ResetUnclearedRooms();
        }
        if (PlayerLocationId == PlayerLocationIds.World)
        {
            Exploration.Discover(
                new GridPosition(
                    (int)MathF.Floor(x / 16),
                    (int)MathF.Floor(y / 16)
                )
            );
        }
        else if (PlayerLocationId == PlayerLocationIds.Greenhouse &&
            !GreenhouseLayout.IsWalkable(PlayerCell))
        {
            PlayerX = GreenhouseLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = GreenhouseLayout.SafeArrivalCell.Y * 16 + 8;
        }
        else if (PlayerLocationId == PlayerLocationIds.StarfeatherCoop &&
            !StarfeatherCoopLayout.IsWalkable(PlayerCell))
        {
            PlayerX = StarfeatherCoopLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = StarfeatherCoopLayout.SafeArrivalCell.Y * 16 + 8;
        }
        else if (PlayerLocationId == PlayerLocationIds.MoonfleeceBarn &&
            !MoonfleeceBarnLayout.IsWalkable(PlayerCell))
        {
            PlayerX = MoonfleeceBarnLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = MoonfleeceBarnLayout.SafeArrivalCell.Y * 16 + 8;
        }
        else if (FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var festivalSpatial
            ) && !festivalSpatial.IsWalkable(PlayerCell))
        {
            PlayerX = festivalSpatial.SafeArrivalCell.X * 16 + 8;
            PlayerY = festivalSpatial.SafeArrivalCell.Y * 16 + 8;
        }
        else if (PlayerLocationId == PlayerLocationIds.CrystalGrottoSurvey &&
            !CrystalGrottoSurveyLayout.IsWalkable(PlayerCell))
        {
            PlayerX = CrystalGrottoSurveyLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = CrystalGrottoSurveyLayout.SafeArrivalCell.Y * 16 + 8;
        }
        else if (PlayerLocationId == PlayerLocationIds.StarfallRuinsTrial &&
            !Starlight.CrystalRuinsPassageUnlocked)
        {
            PlayerLocationId = PlayerLocationIds.World;
            PlayerX = StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8;
            PlayerY = StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8;
        }
        else if (PlayerLocationId == PlayerLocationIds.StarfallRuinsTrial &&
            !StarfallRuinsTrial.IsCellAccessible(PlayerCell))
        {
            PlayerX = StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8;
        }

        if (PlayerLocationId == PlayerLocationIds.CrystalGrottoSurvey &&
            Mining.ReachRoom(
                Math.Min(
                    4,
                    CrystalGrottoSurveyLayout.RoomNumberAt(PlayerCell)
                )
            ))
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }
        PlayerMoved?.Invoke();
    }

    public ActionResult UseSelected(GridPosition target)
    {
        var selected = Inventory.Selected;
        if (selected.IsEmpty)
        {
            return ActionResult.Fail("notice.not_ready");
        }

        if (InsideCrystalGrottoSurvey)
        {
            return UseCrystalGrottoSurveySelected(target);
        }

        if (InsideStarfallRuinsTrial)
        {
            return UseStarfallRuinsTrialSelected(target);
        }

        if (InsideStarharvestMarket)
        {
            return UseStarharvestMarketSelected(target);
        }

        if (InsideGleamrisePlantingFestival)
        {
            return UseGleamrisePlantingFestivalSelected(target);
        }

        if (InsideLongnightLanternFeast)
        {
            return UseLongnightLanternFeastSelected(target);
        }

        if (InsideFireflyTide)
        {
            return UseFireflyTideSelected(target);
        }

        if (CurrentAnimalBuildingId is { } currentAnimalBuildingId)
        {
            return UseAnimalBuildingSelected(
                currentAnimalBuildingId,
                target
            );
        }

        if (FestivalCatalog.FestivalOnDay(Clock.Day) is { } festivalToday &&
            FestivalSpatialCatalog.TryByFestivalId(
                festivalToday.Id,
                out var festivalSpatial
            ) && target == festivalSpatial.WorldEntryCell)
        {
            return TryEnterFestival(festivalToday.Id, target);
        }

        if (InsideGreenhouse)
        {
            return UseGreenhouseSelected(target);
        }

        if (CrabPots.HasPot(target))
        {
            if (selected.ItemId != DataCatalog.HandId)
            {
                return ActionResult.Fail("notice.needs_hand");
            }

            return CrabPots.Interact(target, Inventory, Fishing);
        }

        if (Forage.SpawnAt(target) is not null)
        {
            return CollectForage(target);
        }

        if (target == FarmLayout.GreenhouseDoorCell)
        {
            return TryEnterGreenhouse(target);
        }

        if (AnimalBuildingSpatialCatalog.TryAtWorldDoor(
                target,
                out var animalBuildingDoor
            ))
        {
            return TryEnterAnimalBuilding(
                animalBuildingDoor.BuildingId,
                target
            );
        }

        if (target == CrystalGrottoSurveyLayout.WorldEntryCell)
        {
            return TryEnterCrystalGrottoSurvey(target);
        }

        if (target == StarfallRuinsTrialLayout.WorldEntryCell)
        {
            return TryEnterStarfallRuinsTrial(target);
        }

        if (AnimalAtCurrentLocation(target) is { } worldAnimal)
        {
            return PetAnimal(worldAnimal.InstanceId, target);
        }

        if (target == FarmLayout.HomesteadWorkbenchCell)
        {
            return OpenHomesteadWorkbench(target);
        }

        if (target == FarmLayout.StarGateCell)
        {
            return UseStarGate(target, selected.ItemId);
        }

        if (StarlightSpatialCatalog.TryAtCell(
                target,
                out var starlightTarget
            ))
        {
            return OpenStarlightPedestal(
                starlightTarget.PedestalId,
                target
            );
        }

        if (FarmLayout.IsCommissionBoardCell(target))
        {
            if (selected.ItemId != DataCatalog.HandId)
            {
                return ActionResult.Fail("notice.needs_hand");
            }

            return UseHand(FarmLayout.CommissionBoardCell);
        }

        if (target == FarmLayout.StarlightMailboxCell)
        {
            if (selected.ItemId != DataCatalog.HandId)
            {
                return ActionResult.Fail("notice.needs_hand");
            }

            return UseHand(FarmLayout.StarlightMailboxCell);
        }

        var farmObjectId = FarmObjects.ItemAt(target);
        if (Orchard.HasFruitTree(target) ||
            farmObjectId == DataCatalog.GlowcombHiveId)
        {
            if (selected.ItemId != DataCatalog.HandId)
            {
                return ActionResult.Fail("notice.needs_hand");
            }

            return UseHand(target);
        }

        if (farmObjectId is not null)
        {
            return ActionResult.Fail("notice.placeable_occupied");
        }

        ActionResult result;
        FarmingSkillAction? farmingSkillAction = null;
        switch (selected.ItemId)
        {
            case DataCatalog.HandId:
                return UseHand(target);
            case DataCatalog.ShovelId:
                if (!WorldDefinition.IsHomeCell(target))
                {
                    result = Resources.TryGather(
                        target,
                        selected.ItemId,
                        Energy,
                        Inventory,
                        Clock.Day
                    );
                    break;
                }

                result = Farm.TryTill(target, Energy);
                if (result.Succeeded)
                {
                    Quest.OnTilled();
                    ApplyCurrentWeatherTo(target);
                    farmingSkillAction = FarmingSkillAction.Till;
                }
                break;
            case DataCatalog.MacheteId:
                result = Resources.TryGather(
                    target,
                    selected.ItemId,
                    Energy,
                    Inventory,
                    Clock.Day
                );
                break;
            case DataCatalog.WateringCanId:
                if (WateringCanWater <= 0)
                {
                    return ActionResult.Fail("notice.watering_can_empty");
                }

                var cropId = Farm.Tiles.GetValueOrDefault(target)?.CropId;
                result = Farm.TryWater(
                    target,
                    Energy,
                    FarmingSkill.WateringEnergyCost
                );
                if (result.Succeeded)
                {
                    WateringCanWater--;
                    WaterChanged?.Invoke();
                    Quest.OnWatered(cropId);
                    GleamriseSeason.RecordWateredCrop(cropId, Clock.Day);
                    farmingSkillAction = FarmingSkillAction.Water;
                }
                break;
            case DataCatalog.BucketId:
                return RefillWateringCan(target);
            case DataCatalog.FishingRodId:
                return CastFishingLine(target);
            default:
                var item = DataCatalog.Item(selected.ItemId);
                if (item.Kind == ItemKind.Placeable)
                {
                    if (item.Id == DataCatalog.MoonreedCrabPotId)
                    {
                        return CrabPots.Place(target, Inventory);
                    }

                    if (item.Id == DataCatalog.StarwovenChestId)
                    {
                        return Storage.Place(
                            target,
                            Farm,
                            Inventory,
                            IsPlacementOccupiedByFarmObjectOrOrchard
                        );
                    }

                    if (DataCatalog.FarmObjects.ContainsKey(item.Id))
                    {
                        var placed = FarmObjects.Place(
                            item.Id,
                            target,
                            Farm,
                            Storage,
                            Inventory,
                            Orchard.BlocksMovement
                        );
                        if (placed.Succeeded &&
                            item.Id == DataCatalog.GlowcombHiveId)
                        {
                            Orchard.EnsureBeehive(target);
                            GleamriseSeason.RecordGlowcombHivePlaced(
                                Clock.Day
                            );
                        }

                        return placed;
                    }

                    return ActionResult.Fail("notice.not_ready");
                }

                if (item.Kind == ItemKind.Sapling &&
                    item.FruitTreeId is not null)
                {
                    if (selected.Count <= 0)
                    {
                        return ActionResult.Fail("notice.no_sapling");
                    }

                    result = Orchard.TryPlantTree(
                        item.FruitTreeId,
                        target,
                        Inventory,
                        Farm,
                        Storage,
                        FarmObjects,
                        Clock.Day
                    );
                    if (result.Succeeded)
                    {
                        GleamriseSeason.RecordMoonplumTreePlanted(Clock.Day);
                        farmingSkillAction = FarmingSkillAction.Plant;
                    }

                    break;
                }

                if (item.Kind == ItemKind.Fertilizer)
                {
                    result = Farm.TryFertilize(target, item.Id);
                    if (result.Succeeded)
                    {
                        Inventory.Remove(item.Id, 1);
                        GleamriseSeason.RecordFertilized(Clock.Day);
                    }
                    break;
                }

                if (item.Kind != ItemKind.Seed || item.CropId is null)
                {
                    return ActionResult.Fail("notice.not_ready");
                }

                if (selected.Count <= 0 || item.CropId is null)
                {
                    return ActionResult.Fail("notice.no_seed");
                }

                result = Farm.TryPlant(target, item.CropId, Clock.Day);
                if (result.Succeeded)
                {
                    Inventory.Remove(selected.ItemId, 1);
                    Quest.OnPlanted(item.CropId);
                    Commission.RecordPlant(item.CropId);
                    WeeklyCommission.RecordPlant(item.CropId);
                    GleamriseSeason.RecordPlant(item.CropId, Clock.Day);
                    ApplyCurrentWeatherTo(target);
                    farmingSkillAction = FarmingSkillAction.Plant;
                }
                break;
        }

        if (result.Succeeded && result.GrantedItemId is not null)
        {
            Commission.RecordGather(
                DataCatalog.BaseItemId(result.GrantedItemId),
                result.GrantedItemCount
            );
            WeeklyCommission.RecordGather(
                DataCatalog.BaseItemId(result.GrantedItemId),
                result.GrantedItemCount
            );
            GleamriseSeason.RecordGatheredItem(
                result.GrantedItemId,
                result.GrantedItemCount,
                Clock.Day
            );
        }

        if (result.Succeeded && result.EnergyCost > 0)
        {
            Energy = Math.Max(0, Energy - result.EnergyCost);
            EnergyChanged?.Invoke();
            Changed?.Invoke();
        }

        if (result.Succeeded && farmingSkillAction is { } successfulAction)
        {
            FarmingSkill.RecordSuccessfulAction(successfulAction);
        }

        return result;
    }

    public TargetPreview PreviewSelectedTarget(GridPosition target)
    {
        if (!WorldDefinition.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        if (InsideCrystalGrottoSurvey)
        {
            return PreviewCrystalGrottoSurveyTarget(target);
        }

        if (InsideStarfallRuinsTrial)
        {
            return PreviewStarfallRuinsTrialTarget(target);
        }

        if (InsideStarharvestMarket)
        {
            return PreviewStarharvestMarketTarget(target);
        }

        if (InsideGleamrisePlantingFestival)
        {
            return PreviewGleamrisePlantingFestivalTarget(target);
        }

        if (InsideLongnightLanternFeast)
        {
            return PreviewLongnightLanternFeastTarget(target);
        }

        if (InsideFireflyTide)
        {
            return PreviewFireflyTideTarget(target);
        }

        if (CurrentAnimalBuildingId is { } currentAnimalBuildingId)
        {
            return PreviewAnimalBuildingTarget(
                currentAnimalBuildingId,
                target
            );
        }

        if (InsideGreenhouse)
        {
            return PreviewGreenhouseTarget(target);
        }

        if (InsideArchive)
        {
            return PreviewArchiveTarget(target);
        }

        if (InsideCottage)
        {
            return PreviewCottageTarget(target);
        }

        if (InsideWorkshop)
        {
            return PreviewWorkshopTarget(target);
        }

        if (InsideTeaHouse)
        {
            return PreviewTeaHouseTarget(target);
        }

        if (InsideTwilightEmporium)
        {
            return PreviewTwilightEmporiumTarget(target);
        }

        if (InsideStarlightPost)
        {
            return PreviewStarlightPostTarget(target);
        }

        if (InsideStarfallWatch)
        {
            return PreviewStarfallWatchTarget(target);
        }

        var selected = Inventory.Selected;
        var selectedId = selected.IsEmpty ? string.Empty : selected.ItemId;
        if (CrabPots.HasPot(target))
        {
            return PreviewCrabPot(target, selectedId);
        }
        if (Forage.SpawnAt(target) is not null)
        {
            return PreviewForage(target, selectedId);
        }
        if (FestivalCatalog.FestivalOnDay(Clock.Day) is { } festivalToday &&
            FestivalSpatialCatalog.TryByFestivalId(
                festivalToday.Id,
                out var festivalSpatial
            ) && target == festivalSpatial.WorldEntryCell)
        {
            return PreviewFestivalEntrance(festivalToday.Id, target);
        }
        if (target == FarmLayout.GreenhouseDoorCell)
        {
            return PreviewGreenhouseEntrance(target);
        }

        if (AnimalBuildingSpatialCatalog.TryAtWorldDoor(
                target,
                out var animalBuildingDoor
            ))
        {
            return PreviewAnimalBuildingEntrance(
                animalBuildingDoor.BuildingId,
                target
            );
        }

        if (AnimalAtCurrentLocation(target) is { } worldAnimal)
        {
            return PreviewAnimal(worldAnimal, target);
        }

        if (target == FarmLayout.HomesteadWorkbenchCell)
        {
            var workbench = CheckHomesteadWorkbench(target);
            if (workbench.Succeeded)
            {
                return TargetPreview.Available(
                    FarmLayout.HomesteadWorkbenchCell,
                    TargetPreviewKind.HomesteadWorkshop,
                    "target.action.open_construction"
                );
            }

            if (workbench.MessageKey == "notice.needs_hand")
            {
                return TargetPreview.NeedsTool(
                    FarmLayout.HomesteadWorkbenchCell,
                    TargetPreviewKind.HomesteadWorkshop,
                    "target.need.hand"
                );
            }

            if (workbench.MessageKey is
                "construction.homestead_workshop.not_started" or
                "construction.homestead_workshop.in_progress")
            {
                return TargetPreview.Blocked(
                    FarmLayout.HomesteadWorkbenchCell,
                    TargetPreviewKind.HomesteadWorkshop,
                    workbench.MessageKey
                );
            }

            return TargetPreview.Neutral(target);
        }

        if (target == FarmLayout.StarGateCell)
        {
            return PreviewStarGate(target, selectedId);
        }

        if (StarlightSpatialCatalog.TryAtCell(
                target,
                out var starlightTarget
            ))
        {
            return PreviewStarlightPedestal(
                starlightTarget.PedestalId,
                target
            );
        }

        if (target == CrystalGrottoSurveyLayout.WorldEntryCell)
        {
            return PreviewCrystalGrottoSurveyEntrance(target);
        }

        if (target == StarfallRuinsTrialLayout.WorldEntryCell)
        {
            return PreviewStarfallRuinsTrialEntrance(target);
        }

        if (FarmLayout.IsCommissionBoardCell(target))
        {
            return selectedId == DataCatalog.HandId
                ? TargetPreview.Available(
                    FarmLayout.CommissionBoardCell,
                    TargetPreviewKind.CommissionBoard,
                    "target.action.open_commission"
                )
                : TargetPreview.NeedsTool(
                    FarmLayout.CommissionBoardCell,
                    TargetPreviewKind.CommissionBoard,
                    "target.need.hand"
                );
        }

        if (target == FarmLayout.StarlightMailboxCell)
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    FarmLayout.StarlightMailboxCell,
                    TargetPreviewKind.Mailbox,
                    "target.need.hand"
                );
            }

            var actionKey = Mail.HasUnread
                ? "target.action.open_unread_mail"
                : "target.action.check_mail";
            return TargetPreview.Available(
                FarmLayout.StarlightMailboxCell,
                TargetPreviewKind.Mailbox,
                actionKey
            );
        }

        if (Storage.HasChest(target))
        {
            return selectedId == DataCatalog.HandId
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.StorageChest,
                    "target.action.open_storage"
                )
                : TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.StorageChest,
                    "target.need.hand"
                );
        }

        if (VillageCatalog.IsMoonlitArchiveDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.MoonlitArchiveDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            return VillageCatalog.IsMoonlitArchiveOpen(Clock.MinuteOfDay)
                ? TargetPreview.Available(
                    VillageCatalog.MoonlitArchiveDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_archive"
                )
                : TargetPreview.Blocked(
                    VillageCatalog.MoonlitArchiveDoorCell,
                    TargetPreviewKind.Door,
                    "target.status.archive_closed"
                );
        }

        if (VillageCatalog.IsMoonstoneWorkshopDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.MoonstoneWorkshopDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            if (VillageCatalog.IsMoonstoneWorkshopOpen(
                    Clock.MinuteOfDay
                ))
            {
                return TargetPreview.Available(
                    VillageCatalog.MoonstoneWorkshopDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_workshop"
                );
            }

            return TargetPreview.Blocked(
                VillageCatalog.MoonstoneWorkshopDoorCell,
                TargetPreviewKind.Door,
                "target.status.workshop_closed"
            );
        }

        if (VillageCatalog.IsStarweaverTeaHouseDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.StarweaverTeaHouseDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            if (VillageCatalog.IsStarweaverTeaHouseOpen(
                    Clock.MinuteOfDay
                ))
            {
                return TargetPreview.Available(
                    VillageCatalog.StarweaverTeaHouseDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_tea_house"
                );
            }

            return TargetPreview.Blocked(
                VillageCatalog.StarweaverTeaHouseDoorCell,
                TargetPreviewKind.Door,
                "target.status.tea_house_closed"
            );
        }

        if (VillageCatalog.IsTwilightEmporiumDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.TwilightEmporiumDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            var access = VillageCatalog.TwilightEmporiumAccess(
                Clock.Day,
                Clock.MinuteOfDay
            );
            if (access.IsOpen)
            {
                return TargetPreview.Available(
                    VillageCatalog.TwilightEmporiumDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_emporium"
                );
            }

            return TargetPreview.Blocked(
                VillageCatalog.TwilightEmporiumDoorCell,
                TargetPreviewKind.Door,
                access.TargetStatusKey
            );
        }

        if (VillageCatalog.IsStarlightPostDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.StarlightPostDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            if (VillageCatalog.IsStarlightPostOpen(
                    Clock.MinuteOfDay
                ))
            {
                return TargetPreview.Available(
                    VillageCatalog.StarlightPostDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_starlight_post"
                );
            }

            return TargetPreview.Blocked(
                VillageCatalog.StarlightPostDoorCell,
                TargetPreviewKind.Door,
                "target.status.starlight_post_closed"
            );
        }

        if (VillageCatalog.IsStarfallWatchDoor(target))
        {
            if (selectedId != DataCatalog.HandId)
            {
                return TargetPreview.NeedsTool(
                    VillageCatalog.StarfallWatchDoorCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
            }

            if (VillageCatalog.IsStarfallWatchOpen(
                    Clock.MinuteOfDay
                ))
            {
                return TargetPreview.Available(
                    VillageCatalog.StarfallWatchDoorCell,
                    TargetPreviewKind.Door,
                    "target.action.enter_starfall_watch"
                );
            }

            return TargetPreview.Blocked(
                VillageCatalog.StarfallWatchDoorCell,
                TargetPreviewKind.Door,
                "target.status.starfall_watch_closed"
                );
        }

        if (Orchard.HasFruitTree(target))
        {
            return PreviewFruitTree(target, selectedId);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        var placedFarmObject = FarmObjects.ItemAt(target);
        if (placedFarmObject is not null)
        {
            if (placedFarmObject == DataCatalog.GlowcombHiveId)
            {
                return PreviewBeehive(target, selectedId);
            }

            return TargetPreview.Blocked(
                target,
                PreviewKindForFarmObject(placedFarmObject),
                "target.status.placed"
            );
        }

        var landmark = WorldDefinition.LandmarkAt(target);
        if (landmark is not null)
        {
            return selectedId == DataCatalog.HandId
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.Landmark,
                    "target.action.inspect"
                )
                : TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.Landmark,
                    "target.need.hand"
                );
        }

        var resource = WorldDefinition.ResourceAt(target);
        if (resource != WorldResourceKind.None && !Resources.IsRemoved(target))
        {
            return PreviewResource(target, resource, selectedId);
        }

        if (WorldDefinition.IsWaterSource(target))
        {
            return PreviewWaterSource(target, selectedId);
        }

        if (DataCatalog.Items.TryGetValue(selectedId, out var previewItem) &&
            previewItem.Kind == ItemKind.Sapling)
        {
            return PreviewFruitTreePlacement(
                previewItem,
                selected.Count,
                target
            );
        }

        if (selectedId == DataCatalog.StarwovenChestId)
        {
            var issue = Storage.CheckPlacement(
                target,
                Farm,
                IsPlacementOccupiedByFarmObjectOrOrchard
            );
            return issue switch
            {
                ChestPlacementIssue.None when selected.Count > 0 =>
                    TargetPreview.Available(
                        target,
                        TargetPreviewKind.StorageChest,
                        "target.action.place_chest"
                    ),
                ChestPlacementIssue.NotHome => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.StorageChest,
                    "target.blocked.place_home"
                ),
                ChestPlacementIssue.None => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.StorageChest,
                    "target.blocked.no_chest_item"
                ),
                _ => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.StorageChest,
                    "target.blocked.place_clear"
                )
            };
        }

        if (DataCatalog.FarmObjects.ContainsKey(selectedId))
        {
            return PreviewFarmObjectPlacement(
                selectedId,
                selected.Count,
                target
            );
        }

        if (!WorldDefinition.IsHomeCell(target) ||
            !FarmSystem.IsPlantingBed(target))
        {
            if (selectedId == DataCatalog.StarsoilFertilizerId)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Ground,
                    "target.blocked.fertilizer_needs_tilled"
                );
            }

            return TargetPreview.Neutral(target);
        }

        Farm.Tiles.TryGetValue(target, out var tile);
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

                var harvestItemId = Farm.HarvestItemIdAt(target)
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

            return Energy < FarmingSkill.WateringEnergyCost
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

            if (DataCatalog.Items.TryGetValue(selectedId, out var selectedItem) &&
                selectedItem.Kind == ItemKind.Seed &&
                selectedItem.CropId is not null)
            {
                var plantingCheck = Farm.CheckCropPlanting(
                    selectedItem.CropId,
                    Clock.Day
                );
                if (!plantingCheck.Succeeded)
                {
                    return TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.Soil,
                        plantingCheck.MessageKey ==
                            "notice.longnight_outdoor_planting"
                            ? "target.blocked.longnight_outdoor_planting"
                            : "target.blocked.seed_out_of_season"
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

    private TargetPreview PreviewCrabPot(
        GridPosition target,
        string selectedId
    )
    {
        if (selectedId != DataCatalog.HandId)
        {
            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.CrabPot,
                "target.need.hand"
            );
        }

        var pot = CrabPots.PotAt(target);
        if (pot.IsReady)
        {
            var fish = DataCatalog.Fishes[pot.CatchItemId];
            if (!Inventory.CanAdd(fish.ItemId, 1))
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.CrabPot,
                    "target.blocked.backpack_full"
                );
            }

            return TargetPreview.Available(
                target,
                TargetPreviewKind.CrabPot,
                "target.action.collect_crab_pot"
            );
        }

        if (pot.IsBaited)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.CrabPot,
                "target.status.crab_pot_waiting"
            );
        }

        var hasBait = Inventory.Count(DataCatalog.GlowgrubBaitId) > 0 ||
            Inventory.Count(DataCatalog.MoonmoteBaitId) > 0;
        if (!hasBait)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.CrabPot,
                "target.blocked.crab_pot_needs_bait"
            );
        }

        return TargetPreview.Available(
            target,
            TargetPreviewKind.CrabPot,
            "target.action.bait_crab_pot"
        );
    }

    private TargetPreview PreviewWaterSource(
        GridPosition target,
        string selectedId
    )
    {
        if (selectedId == DataCatalog.MoonreedCrabPotId)
        {
            if (CrabPots.Pots.Count >= CrabPotSystem.MaximumPlacedPots)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.CrabPot,
                    "target.blocked.crab_pot_limit"
                );
            }

            return TargetPreview.Available(
                target,
                TargetPreviewKind.CrabPot,
                "target.action.place_crab_pot"
            );
        }

        if (selectedId == DataCatalog.BucketId)
        {
            return WateringCanWater >= MaxWateringCanWater
                ? TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Water,
                    "target.status.water_full"
                )
                : TargetPreview.Available(
                    target,
                    TargetPreviewKind.Water,
                    "target.action.draw_water"
                );
        }

        if (selectedId == DataCatalog.FishingRodId)
        {
            if (Energy < FishingSystem.CastEnergyCost)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Water,
                    "target.blocked.no_energy"
                );
            }

            var fish = Fishing.PreviewCatch(
                target,
                Clock.Day,
                Clock.MinuteOfDay,
                Weather.CurrentId
            );
            if (fish is null)
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Water,
                    "target.status.no_fish"
                );
            }

            return Inventory.CanAdd(fish.ItemId, 1)
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.Water,
                    "target.action.fish"
                )
                : TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Water,
                    "target.blocked.backpack_full"
                );
        }

        return TargetPreview.NeedsTool(
            target,
            TargetPreviewKind.Water,
            "target.need.bucket_or_rod"
        );
    }

    private TargetPreview PreviewFruitTree(
        GridPosition target,
        string selectedId
    )
    {
        if (selectedId != DataCatalog.HandId)
        {
            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.FruitTree,
                "target.need.hand"
            );
        }

        var tree = Orchard.FruitTreeAt(target);
        if (tree is null)
        {
            return TargetPreview.Neutral(target);
        }

        if (!tree.IsMature)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.status.fruit_tree_growing"
            );
        }

        if (!tree.FruitReady)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.status.fruit_tree_recovering"
            );
        }

        var treeDefinition = DataCatalog.FruitTree(tree.TreeId);
        if (!Inventory.CanAdd(treeDefinition.HarvestItemId, 1))
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.blocked.backpack_full"
            );
        }

        return TargetPreview.Available(
            target,
            TargetPreviewKind.FruitTree,
            "target.action.harvest_fruit"
        );
    }

    private TargetPreview PreviewBeehive(
        GridPosition target,
        string selectedId
    )
    {
        if (selectedId != DataCatalog.HandId)
        {
            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.Beehive,
                "target.need.hand"
            );
        }

        var hive = Orchard.BeehiveAt(target);
        if (hive is null)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.Beehive,
                "target.status.placed"
            );
        }

        if (hive.HasHoney)
        {
            if (!Inventory.CanAdd(DataCatalog.StarhoneyId, hive.PendingHoney))
            {
                return TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.Beehive,
                    "target.blocked.backpack_full"
                );
            }

            return TargetPreview.Available(
                target,
                TargetPreviewKind.Beehive,
                "target.action.collect_honey"
            );
        }

        if (!Orchard.HasPollinationSource(target, BeehivePollinationRange))
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.Beehive,
                "target.status.beehive_needs_tree"
            );
        }

        return TargetPreview.Blocked(
            target,
            TargetPreviewKind.Beehive,
            "target.status.beehive_brewing"
        );
    }

    private TargetPreview PreviewFruitTreePlacement(
        ItemDefinition item,
        int count,
        GridPosition target
    )
    {
        if (item.FruitTreeId is null ||
            !DataCatalog.FruitTrees.TryGetValue(
                item.FruitTreeId,
                out var definition
            ))
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.blocked.sapling_clear"
            );
        }

        if (!definition.IsAvailableOnDay(Clock.Day))
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.blocked.sapling_out_of_season"
            );
        }

        var issue = Orchard.CheckTreePlacement(
            target,
            Farm,
            Storage,
            FarmObjects
        );
        if (issue == OrchardPlacementIssue.None && count > 0)
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.FruitTree,
                "target.action.plant_tree"
            );
        }

        if (issue == OrchardPlacementIssue.None)
        {
            return TargetPreview.Blocked(
                target,
                TargetPreviewKind.FruitTree,
                "target.blocked.no_sapling"
            );
        }

        var labelKey = issue switch
        {
            OrchardPlacementIssue.NotHome => "target.blocked.sapling_home",
            OrchardPlacementIssue.WrongSurface => "target.blocked.sapling_ground",
            OrchardPlacementIssue.Occupied => "target.blocked.sapling_occupied",
            _ => "target.blocked.sapling_clear"
        };
        return TargetPreview.Blocked(
            target,
            TargetPreviewKind.FruitTree,
            labelKey
        );
    }

    private TargetPreview PreviewResource(
        GridPosition target,
        WorldResourceKind resource,
        string selectedId
    )
    {
        var isTree = resource == WorldResourceKind.Tree;
        var requiredTool = isTree ? DataCatalog.MacheteId : DataCatalog.ShovelId;
        var kind = isTree ? TargetPreviewKind.Tree : TargetPreviewKind.Crystal;
        if (selectedId != requiredTool)
        {
            return TargetPreview.NeedsTool(
                target,
                kind,
                isTree ? "target.need.machete" : "target.need.shovel_mine"
            );
        }

        if (Energy < 4)
        {
            return TargetPreview.Blocked(
                target,
                kind,
                "target.blocked.no_energy"
            );
        }

        var itemId = isTree ? DataCatalog.LumenwoodId : DataCatalog.CrystalShardId;
        var count = isTree ? 2 : 1;
        if (!Inventory.CanAdd(itemId, count))
        {
            return TargetPreview.Blocked(
                target,
                kind,
                "target.blocked.backpack_full"
            );
        }

        return TargetPreview.Available(
            target,
            kind,
            isTree ? "target.action.chop" : "target.action.mine"
        );
    }

    public ActionResult CheckMineCrystalGrottoVein(GridPosition target)
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        return Mining.CheckMineVein(
            PlayerLocationId,
            PlayerCell,
            target,
            selectedId,
            ToolProgression.TierIdFor(DataCatalog.ShovelId),
            Energy,
            Inventory
        );
    }

    public ActionResult CheckCrystalGrottoSurveyEntrance(
        GridPosition target
    )
    {
        if (PlayerLocationId != PlayerLocationIds.World ||
            target != CrystalGrottoSurveyLayout.WorldEntryCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "mining.survey.enter")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterCrystalGrottoSurvey(GridPosition target) =>
        CheckCrystalGrottoSurveyEntrance(target);

    public ActionResult CheckCrystalGrottoSurveyExit(GridPosition target)
    {
        if (!InsideCrystalGrottoSurvey ||
            target != CrystalGrottoSurveyLayout.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "mining.survey.exit")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryExitCrystalGrottoSurvey(GridPosition target) =>
        CheckCrystalGrottoSurveyExit(target);

    public ActionResult CheckStarfallRuinsTrialEntrance(
        GridPosition target
    )
    {
        if (PlayerLocationId != PlayerLocationIds.World ||
            target != StarfallRuinsTrialLayout.WorldEntryCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (!Starlight.CrystalRuinsPassageUnlocked)
        {
            return ActionResult.Fail("ruins.trial.passage_locked");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "ruins.trial.enter")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterStarfallRuinsTrial(GridPosition target) =>
        CheckStarfallRuinsTrialEntrance(target);

    public ActionResult CheckStarfallRuinsTrialExit(GridPosition target)
    {
        if (!InsideStarfallRuinsTrial ||
            target != StarfallRuinsTrialLayout.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "ruins.trial.exit")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryExitStarfallRuinsTrial(GridPosition target) =>
        CheckStarfallRuinsTrialExit(target);

    public ActionResult CheckRecoverMoonsteelShortblade(
        GridPosition target
    ) => InsideStarfallRuinsTrial
        ? StarfallRuinsTrial.CheckRecoverWeapon(
            PlayerCell,
            target,
            Inventory.Selected.IsEmpty
                ? string.Empty
                : Inventory.Selected.ItemId,
            Inventory
        )
        : ActionResult.Fail("notice.nothing_to_interact");

    public ActionResult RecoverMoonsteelShortblade(GridPosition target)
    {
        var check = CheckRecoverMoonsteelShortblade(target);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            return StarfallRuinsTrial.RecoverWeaponChecked(Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckRecoverStarfallArtifact(GridPosition target) =>
        InsideStarfallRuinsTrial
            ? StarfallRuinsTrial.CheckRecoverArtifact(
                PlayerCell,
                target,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId,
                Inventory
            )
            : ActionResult.Fail("notice.nothing_to_interact");

    public ActionResult RecoverStarfallArtifact(GridPosition target)
    {
        var check = CheckRecoverStarfallArtifact(target);
        if (!check.Succeeded ||
            !StarfallRuinsTrialCatalog.TryArtifactAt(
                target,
                out var artifact
            ))
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            var result = StarfallRuinsTrial.RecoverArtifactChecked(
                artifact.ItemId,
                Inventory
            );
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckAttackStarfallEnemy(
        string enemyInstanceId,
        GridPosition target
    )
    {
        if (!InsideStarfallRuinsTrial)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var targetCheck = StarfallRuinsTrial.CheckDamageEnemy(
            enemyInstanceId,
            target
        );
        if (!targetCheck.Succeeded)
        {
            return targetCheck;
        }

        var selectedItemId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var combatCheck = Combat.CheckAttack(selectedItemId);
        if (!combatCheck.Succeeded)
        {
            return combatCheck;
        }

        var enemy = StarfallRuinsTrial.Enemy(enemyInstanceId);
        var weapon = StarfallRuinsTrialCatalog.Weapon(selectedItemId);
        var dx = PlayerX - enemy.CurrentX;
        var dy = PlayerY - enemy.CurrentY;
        return MathF.Sqrt(dx * dx + dy * dy) <=
            weapon.RangePixels
                ? ActionResult.Success(messageKey: "combat.attack.ready")
                : ActionResult.Fail("combat.target_out_of_range");
    }

    public StarfallTrialAttackResult AttackStarfallEnemy(
        string enemyInstanceId,
        GridPosition target
    )
    {
        var check = CheckAttackStarfallEnemy(enemyInstanceId, target);
        if (!check.Succeeded)
        {
            return new StarfallTrialAttackResult(
                false,
                check.MessageKey
            );
        }

        BeginChangedBatch();
        try
        {
            var weaponItemId = Inventory.Selected.ItemId;
            var weapon = StarfallRuinsTrialCatalog.Weapon(weaponItemId);
            Combat.BeginCheckedAttack(weaponItemId);
            var damage = StarfallRuinsTrial.ApplyDamageChecked(
                enemyInstanceId,
                weapon.Damage
            );
            if (damage.EnemyDefeated)
            {
                Collection.RecordDiscovery(damage.EnemyId);
                Starlight.RefreshRewardUnlocks(StarlightProgress());
            }

            return new StarfallTrialAttackResult(
                true,
                damage.MessageKey,
                damage.EnemyInstanceId,
                damage.EnemyId,
                damage.DamageDealt,
                damage.RemainingHealth,
                damage.EnemyDefeated,
                damage.ClearedRoomId
            );
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public CombatDamageResult ReceiveStarfallEnemyHit(
        string enemyInstanceId
    )
    {
        if (!InsideStarfallRuinsTrial ||
            !StarfallRuinsTrialCatalog.TryEnemyInstance(
                enemyInstanceId,
                out var instance
            ) || StarfallRuinsTrial.Enemy(enemyInstanceId).Defeated)
        {
            return new CombatDamageResult(
                false,
                "combat.hit.invalid",
                RemainingHealth: Combat.CurrentHealth,
                PlayerDefeated: Combat.IsDefeated
            );
        }

        return Combat.ReceiveHit(
            StarfallRuinsTrialCatalog.Enemy(instance.EnemyId).Damage
        );
    }

    public ActionResult CheckMoveStarfallEnemy(
        string enemyInstanceId,
        float x,
        float y
    )
    {
        if (!InsideStarfallRuinsTrial)
        {
            return ActionResult.Fail("combat.enemy_move.trial_only");
        }

        var targetCell = new GridPosition(
            (int)MathF.Floor(x / 16),
            (int)MathF.Floor(y / 16)
        );
        return targetCell == PlayerCell
            ? ActionResult.Fail("combat.enemy_move.blocked")
            : StarfallRuinsTrial.CheckMoveEnemy(enemyInstanceId, x, y);
    }

    public ActionResult MoveStarfallEnemyChecked(
        string enemyInstanceId,
        float x,
        float y
    )
    {
        var check = CheckMoveStarfallEnemy(enemyInstanceId, x, y);
        return check.Succeeded
            ? StarfallRuinsTrial.MoveEnemyChecked(enemyInstanceId, x, y)
            : check;
    }

    public CombatDodgeResult DodgeInStarfallRuinsTrial() =>
        InsideStarfallRuinsTrial
            ? Combat.BeginDodge()
            : new CombatDodgeResult(false, "combat.dodge.trial_only");

    public void AdvanceStarfallCombat(float deltaSeconds)
    {
        if (InsideStarfallRuinsTrial)
        {
            Combat.Advance(deltaSeconds);
        }
    }

    public ActionResult InspectStarfallRuinsSeal(GridPosition target)
    {
        if (!InsideStarfallRuinsTrial ||
            !StarfallRuinsTrialLayout.IsSealCell(target) ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return StarfallRuinsTrial.IsSealOpen(target)
            ? ActionResult.Success(messageKey: "ruins.seal.open")
            : ActionResult.Fail("ruins.seal.room_not_cleared");
    }

    public StarfallTrialDefeatResolution ResolveStarfallTrialDefeat(
        bool forcedByClosingTime = false
    )
    {
        if (!InsideStarfallRuinsTrial ||
            (!forcedByClosingTime && !Combat.IsDefeated))
        {
            return new StarfallTrialDefeatResolution(
                false,
                "combat.defeat.not_ready"
            );
        }

        var settlement = EndDay();
        Energy = 50;
        EnergyChanged?.Invoke();
        Combat.RestoreFullHealth();
        StarfallRuinsTrial.ResetUnclearedRooms();
        SetPlayerLocation(
            CottageLayout.SafeArrivalCell.X * 16 + 8,
            CottageLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.Cottage
        );
        Changed?.Invoke();
        return new StarfallTrialDefeatResolution(
            true,
            "combat.defeat.resolved",
            settlement
        );
    }

    public ActionResult CheckCrystalGrottoUpgradeBench(GridPosition target)
    {
        if (!InsideCrystalGrottoSurvey ||
            target != CrystalGrottoSurveyLayout.UpgradeBenchCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "tool.upgrade.panel_opened")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult OpenCrystalGrottoUpgradeBench(
        GridPosition target
    ) => CheckCrystalGrottoUpgradeBench(target);

    public ActionResult CheckCrystalGrottoDepthAnchor(GridPosition target)
    {
        if (!InsideCrystalGrottoSurvey ||
            target != CrystalGrottoSurveyLayout.DepthAnchorCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: Mining.FifthRoomAnchorReached
                ? "deep_mine.ready"
                : "mining.anchor_ready"
        );
    }

    public ActionResult ActivateCrystalGrottoDepthAnchor(
        GridPosition target
    )
    {
        var check = CheckCrystalGrottoDepthAnchor(target);
        if (!check.Succeeded)
        {
            return check;
        }

        if (Mining.FifthRoomAnchorReached)
        {
            return DeepMine.Start(Clock.Day, Inventory);
        }

        Mining.ReachRoom(CrystalGrottoSurveyLayout.RoomCount);
        Starlight.RefreshRewardUnlocks(StarlightProgress());
        NotifyChanged();
        return ActionResult.Success(messageKey: "mining.anchor_activated");
    }

    public DeepMineAttackResult AttackDeepMineEnemy()
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        return DeepMine.Attack(
            selectedId,
            Inventory,
            Combat,
            Collection
        );
    }

    public CombatDodgeResult DodgeInDeepMine() =>
        DeepMine.PrepareDodge(Combat);

    public void AdvanceDeepMineCombat(float deltaSeconds)
    {
        if (DeepMine.Active)
        {
            Combat.Advance(deltaSeconds);
        }
    }

    public ActionResult ExcavateDeepMineRoom()
    {
        var result = DeepMine.Excavate(
            ToolProgression.TierIdFor(DataCatalog.ShovelId),
            Energy,
            Inventory
        );
        if (!result.Succeeded)
        {
            return result;
        }

        Energy = Math.Max(0, Energy - result.EnergyCost);
        EnergyChanged?.Invoke();
        NotifyChanged();
        return result;
    }

    public ActionResult AdvanceDeepMineRoom() => DeepMine.AdvanceRoom();

    public ActionResult ChooseAdventureSpecialization(
        AdventureSkillKind kind,
        string specializationId
    ) => kind == AdventureSkillKind.CrystalMining
        ? DeepMine.CrystalMiningSkill.ChooseSpecialization(specializationId)
        : DeepMine.NightwatchSkill.ChooseSpecialization(specializationId);

    public StarfallTrialDefeatResolution ResolveDeepMineDefeat()
    {
        if (!DeepMine.Active || !Combat.IsDefeated)
        {
            return new StarfallTrialDefeatResolution(
                false,
                "combat.defeat.not_ready"
            );
        }

        var settlement = EndDay();
        Energy = 50;
        EnergyChanged?.Invoke();
        Combat.RestoreFullHealth();
        DeepMine.RecoverFromDefeat();
        SetPlayerLocation(
            CrystalGrottoSurveyLayout.SafeArrivalCell.X * 16 + 8,
            CrystalGrottoSurveyLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        NotifyChanged();
        return new StarfallTrialDefeatResolution(
            true,
            "deep_mine.defeat_recovered",
            settlement
        );
    }

    public ActionResult InspectCrystalGrottoSeal(GridPosition target)
    {
        if (!InsideCrystalGrottoSurvey ||
            target != CrystalGrottoSurveyLayout.SealCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "mining.seal.inspected")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryMineCrystalGrottoVein(GridPosition target)
    {
        BeginChangedBatch();
        try
        {
            var selectedId = Inventory.Selected.IsEmpty
                ? string.Empty
                : Inventory.Selected.ItemId;
            var result = Mining.TryMineVein(
                PlayerLocationId,
                PlayerCell,
                target,
                selectedId,
                ToolProgression.TierIdFor(DataCatalog.ShovelId),
                Energy,
                Inventory
            );
            if (!result.Succeeded)
            {
                return result;
            }

            Energy = Math.Max(0, Energy - result.EnergyCost);
            EnergyChanged?.Invoke();
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    private TargetPreview PreviewCrystalGrottoSurveyTarget(
        GridPosition target
    )
    {
        if (target == CrystalGrottoSurveyLayout.ExitCell)
        {
            return PreviewHandCheckedTarget(
                target,
                TargetPreviewKind.CrystalGrottoExit,
                CheckCrystalGrottoSurveyExit(target),
                "target.action.exit"
            );
        }

        if (target == CrystalGrottoSurveyLayout.UpgradeBenchCell)
        {
            return PreviewHandCheckedTarget(
                target,
                TargetPreviewKind.ToolUpgradeBench,
                CheckCrystalGrottoUpgradeBench(target),
                "target.action.open_tool_upgrade"
            );
        }

        if (target == CrystalGrottoSurveyLayout.DepthAnchorCell)
        {
            var anchorCheck = CheckCrystalGrottoDepthAnchor(target);
            if (anchorCheck.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.MineDepthAnchor,
                    Mining.FifthRoomAnchorReached
                        ? "target.action.enter_deep_mine"
                        : "target.action.activate_depth_anchor"
                );
            }

            if (anchorCheck.MessageKey == "notice.needs_hand")
            {
                return TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.MineDepthAnchor,
                    "target.need.hand"
                );
            }

            return TargetPreview.Neutral(target);
        }

        if (target == CrystalGrottoSurveyLayout.SealCell)
        {
            return PreviewHandCheckedTarget(
                target,
                TargetPreviewKind.GrottoSeal,
                InspectCrystalGrottoSeal(target),
                "target.action.inspect"
            );
        }

        if (!MiningCatalog.TryVeinAt(target, out _))
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckMineCrystalGrottoVein(target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.MineralVein,
                "target.action.mine"
            );
        }

        return check.MessageKey switch
        {
            "notice.needs_shovel" => TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.MineralVein,
                "target.need.shovel_mine"
            ),
            "mining.requires_bronze_star_shovel" =>
                TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.MineralVein,
                    check.MessageKey
                ),
            "notice.no_energy" or "notice.inventory_full" or
                "mining.vein_depleted" => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.MineralVein,
                    check.MessageKey
                ),
            _ => TargetPreview.Neutral(target)
        };
    }

    private ActionResult UseCrystalGrottoSurveySelected(
        GridPosition target
    )
    {
        if (target == CrystalGrottoSurveyLayout.ExitCell)
        {
            return TryExitCrystalGrottoSurvey(target);
        }

        if (target == CrystalGrottoSurveyLayout.UpgradeBenchCell)
        {
            return OpenCrystalGrottoUpgradeBench(target);
        }

        if (target == CrystalGrottoSurveyLayout.DepthAnchorCell)
        {
            return ActivateCrystalGrottoDepthAnchor(target);
        }

        if (target == CrystalGrottoSurveyLayout.SealCell)
        {
            return InspectCrystalGrottoSeal(target);
        }

        return TryMineCrystalGrottoVein(target);
    }

    private TargetPreview PreviewCrystalGrottoSurveyEntrance(
        GridPosition target
    ) => PreviewHandCheckedTarget(
        target,
        TargetPreviewKind.CrystalGrottoPortal,
        CheckCrystalGrottoSurveyEntrance(target),
        "target.action.enter_crystal_grotto"
    );

    private TargetPreview PreviewStarfallRuinsTrialEntrance(
        GridPosition target
    )
    {
        var check = CheckStarfallRuinsTrialEntrance(target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.StarfallRuinsPortal,
                "target.action.enter_starfall_ruins_trial"
            );
        }

        return check.MessageKey switch
        {
            "notice.needs_hand" => TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.StarfallRuinsPortal,
                "target.need.hand"
            ),
            "ruins.trial.passage_locked" => TargetPreview.Blocked(
                target,
                TargetPreviewKind.StarfallRuinsPortal,
                check.MessageKey
            ),
            _ => TargetPreview.Neutral(target)
        };
    }

    private TargetPreview PreviewStarfallRuinsTrialTarget(
        GridPosition target
    )
    {
        if (!StarfallRuinsTrialLayout.IsWalkable(target))
        {
            return TargetPreview.Neutral(target);
        }

        if (target == StarfallRuinsTrialLayout.ExitCell)
        {
            return PreviewHandCheckedTarget(
                target,
                TargetPreviewKind.StarfallRuinsExit,
                CheckStarfallRuinsTrialExit(target),
                "target.action.exit"
            );
        }

        if (target == StarfallRuinsTrialLayout.WeaponRackCell)
        {
            var check = CheckRecoverMoonsteelShortblade(target);
            if (check.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.RuinsWeaponRack,
                    "target.action.recover_shortblade"
                );
            }

            return check.MessageKey switch
            {
                "notice.needs_hand" => TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.RuinsWeaponRack,
                    "target.need.hand"
                ),
                "notice.inventory_full" => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.RuinsWeaponRack,
                    "target.blocked.backpack_full"
                ),
                "ruins.weapon.already_recovered" => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.RuinsWeaponRack,
                    check.MessageKey
                ),
                _ => TargetPreview.Neutral(target)
            };
        }

        if (StarfallRuinsTrialCatalog.TryArtifactAt(target, out _))
        {
            var check = CheckRecoverStarfallArtifact(target);
            if (check.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.RuinsArtifact,
                    "target.action.recover_artifact"
                );
            }

            return check.MessageKey switch
            {
                "notice.needs_hand" => TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.RuinsArtifact,
                    "target.need.hand"
                ),
                "notice.inventory_full" => TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.RuinsArtifact,
                    "target.blocked.backpack_full"
                ),
                "ruins.artifact.room_not_cleared" or
                    "ruins.artifact.already_recovered" =>
                    TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.RuinsArtifact,
                        check.MessageKey
                    ),
                _ => TargetPreview.Neutral(target)
            };
        }

        if (StarfallRuinsTrial.EnemyAt(target) is { } enemy)
        {
            var check = CheckAttackStarfallEnemy(enemy.InstanceId, target);
            if (check.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.RuinsEnemy,
                    "target.action.attack"
                );
            }

            return check.MessageKey switch
            {
                "combat.requires_weapon" => TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.RuinsEnemy,
                    "target.need.weapon"
                ),
                "combat.enemy_defeated" or "combat.attack.cooldown" or
                    "combat.target_out_of_range" or
                    "combat.player_defeated" => TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.RuinsEnemy,
                        check.MessageKey
                    ),
                _ => TargetPreview.Neutral(target)
            };
        }

        if (StarfallRuinsTrialLayout.IsSealCell(target))
        {
            var check = InspectStarfallRuinsSeal(target);
            if (check.Succeeded)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.RuinsSeal,
                    "target.action.inspect"
                );
            }

            return check.MessageKey == "notice.needs_hand"
                ? TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.RuinsSeal,
                    "target.need.hand"
                )
                : check.MessageKey == "ruins.seal.room_not_cleared"
                    ? TargetPreview.Blocked(
                        target,
                        TargetPreviewKind.RuinsSeal,
                        check.MessageKey
                    )
                    : TargetPreview.Neutral(target);
        }

        return TargetPreview.Neutral(target);
    }

    private ActionResult UseStarfallRuinsTrialSelected(GridPosition target)
    {
        if (target == StarfallRuinsTrialLayout.ExitCell)
        {
            return TryExitStarfallRuinsTrial(target);
        }

        if (target == StarfallRuinsTrialLayout.WeaponRackCell)
        {
            return RecoverMoonsteelShortblade(target);
        }

        if (StarfallRuinsTrialCatalog.TryArtifactAt(target, out _))
        {
            return RecoverStarfallArtifact(target);
        }

        if (StarfallRuinsTrial.EnemyAt(target) is { } enemy)
        {
            var attack = AttackStarfallEnemy(enemy.InstanceId, target);
            return attack.Succeeded
                ? ActionResult.Success(messageKey: attack.MessageKey)
                : ActionResult.Fail(attack.MessageKey);
        }

        if (StarfallRuinsTrialLayout.IsSealCell(target))
        {
            return InspectStarfallRuinsSeal(target);
        }

        return ActionResult.Fail("notice.nothing_to_interact");
    }

    private static TargetPreview PreviewHandCheckedTarget(
        GridPosition target,
        TargetPreviewKind kind,
        ActionResult check,
        string actionKey
    )
    {
        if (check.Succeeded)
        {
            return TargetPreview.Available(target, kind, actionKey);
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(target, kind, "target.need.hand")
            : TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewForage(
        GridPosition target,
        string selectedItemId
    )
    {
        var check = Forage.CheckCollect(
            target,
            PlayerLocationId,
            PlayerCell,
            selectedItemId,
            Inventory
        );
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.Forage,
                "target.action.collect_forage"
            );
        }

        if (check.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                target,
                TargetPreviewKind.Forage,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            target,
            TargetPreviewKind.Forage,
            check.MessageKey == "notice.inventory_full"
                ? "target.blocked.backpack_full"
                : check.MessageKey
        );
    }

    private ActionResult CollectForage(GridPosition target)
    {
        var selectedItemId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        BeginChangedBatch();
        try
        {
            var result = Forage.TryCollect(
                target,
                PlayerLocationId,
                PlayerCell,
                selectedItemId,
                Inventory
            );
            if (result.Succeeded && result.GrantedItemId is not null)
            {
                Commission.RecordGather(
                    result.GrantedItemId,
                    result.GrantedItemCount
                );
                WeeklyCommission.RecordGather(
                    result.GrantedItemId,
                    result.GrantedItemCount
                );
            }

            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckStarlightPedestal(
        string pedestalId,
        GridPosition target
    )
    {
        if (!DataCatalog.StarlightPedestals.ContainsKey(pedestalId) ||
            !StarlightSpatialCatalog.TryAtCell(target, out var spatial) ||
            spatial.PedestalId != pedestalId ||
            PlayerLocationId != spatial.LocationId ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.not_ready");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "starlight.opened")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult OpenStarlightPedestal(
        string pedestalId,
        GridPosition target
    )
    {
        var check = CheckStarlightPedestal(pedestalId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        Starlight.Discover(pedestalId);
        Starlight.RefreshRewardUnlocks(StarlightProgress());
        return check;
    }

    public ActionResult CheckActivateStarlightPedestal(
        string pedestalId,
        GridPosition target
    )
    {
        var access = CheckStarlightPedestal(pedestalId, target);
        return access.Succeeded
            ? Starlight.CheckManualActivation(
                pedestalId,
                StarlightProgress()
            )
            : access;
    }

    public ActionResult ActivateStarlightPedestal(
        string pedestalId,
        GridPosition target
    )
    {
        var check = CheckActivateStarlightPedestal(pedestalId, target);
        if (!check.Succeeded)
        {
            return check;
        }

        return Starlight.ActivateManually(
            pedestalId,
            StarlightProgress()
        );
    }

    private TargetPreview PreviewStarlightPedestal(
        string pedestalId,
        GridPosition target
    )
    {
        var check = CheckStarlightPedestal(pedestalId, target);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                StarlightCell(pedestalId),
                TargetPreviewKind.StarlightPedestal,
                "target.action.open_starlight"
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                StarlightCell(pedestalId),
                TargetPreviewKind.StarlightPedestal,
                "target.need.hand"
            )
            : TargetPreview.Neutral(target);
    }

    private static GridPosition StarlightCell(string pedestalId) =>
        StarlightSpatialCatalog.ForPedestal(pedestalId).Cell;

    private ActionResult UseHand(GridPosition target)
    {
        if (target == FarmLayout.CommissionBoardCell)
        {
            return ActionResult.Success(messageKey: "commission.opened");
        }

        if (target == FarmLayout.StarlightMailboxCell)
        {
            return ActionResult.Success(messageKey: "mail.opened");
        }

        if (Storage.HasChest(target))
        {
            return ActionResult.Success(messageKey: "storage.opened");
        }

        if (Orchard.HasFruitTree(target))
        {
            var harvested = Orchard.TryHarvestFruit(target, Inventory);
            if (harvested.Succeeded &&
                harvested.GrantedItemId is not null)
            {
                Commission.RecordGather(
                    DataCatalog.BaseItemId(harvested.GrantedItemId),
                    harvested.GrantedItemCount
                );
                WeeklyCommission.RecordGather(
                    DataCatalog.BaseItemId(harvested.GrantedItemId),
                    harvested.GrantedItemCount
                );
                FarmingSkill.RecordSuccessfulAction(
                    FarmingSkillAction.Harvest
                );
                GleamriseSeason.RecordGatheredItem(
                    harvested.GrantedItemId,
                    harvested.GrantedItemCount,
                    Clock.Day
                );
            }

            return harvested;
        }

        if (FarmObjects.ItemAt(target) == DataCatalog.GlowcombHiveId)
        {
            Orchard.EnsureBeehive(target);
            var collected = Orchard.TryCollectHoney(target, Inventory);
            if (collected.Succeeded &&
                collected.GrantedItemId is not null)
            {
                Commission.RecordGather(
                    DataCatalog.BaseItemId(collected.GrantedItemId),
                    collected.GrantedItemCount
                );
                WeeklyCommission.RecordGather(
                    DataCatalog.BaseItemId(collected.GrantedItemId),
                    collected.GrantedItemCount
                );
                GleamriseSeason.RecordGatheredItem(
                    collected.GrantedItemId,
                    collected.GrantedItemCount,
                    Clock.Day
                );
            }

            return collected;
        }

        var tile = Farm.Tiles.GetValueOrDefault(target);
        if (tile?.CropId is not null)
        {
            var crop = DataCatalog.Crop(tile.CropId);
            if (!crop.IsMature(tile.WateredNights))
            {
                return ActionResult.Fail("notice.not_ready");
            }

            var harvestItemId = Farm.HarvestItemIdAt(target)
                ?? crop.HarvestItemId;
            if (!Inventory.CanAdd(harvestItemId, 1))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            var harvested = Farm.TryHarvest(target);
            if (harvested.Succeeded && harvested.GrantedItemId is not null)
            {
                Inventory.Add(harvested.GrantedItemId, harvested.GrantedItemCount);
                Quest.OnHarvested(
                    DataCatalog.BaseItemId(harvested.GrantedItemId)
                );
                FarmingSkill.RecordSuccessfulAction(
                    FarmingSkillAction.Harvest
                );
                GleamriseSeason.RecordGatheredItem(
                    harvested.GrantedItemId,
                    harvested.GrantedItemCount,
                    Clock.Day
                );
            }

            return harvested;
        }

        var landmark = WorldDefinition.LandmarkAt(target);
        if (landmark is not null)
        {
            return ActionResult.Success(messageKey: landmark.NameKey);
        }

        var resource = WorldDefinition.ResourceAt(target);
        if (resource != WorldResourceKind.None)
        {
            if (Resources.IsRemoved(target))
            {
                return ActionResult.Fail("notice.resource_depleted");
            }

            return ActionResult.Fail(
                resource == WorldResourceKind.Tree
                    ? "notice.needs_machete"
                    : "notice.needs_shovel"
            );
        }

        return ActionResult.Fail("notice.nothing_to_interact");
    }

    private ActionResult RefillWateringCan(GridPosition target)
    {
        if (!WorldDefinition.IsWaterSource(target))
        {
            return ActionResult.Fail("notice.not_water_source");
        }

        if (WateringCanWater >= MaxWateringCanWater)
        {
            return ActionResult.Fail("notice.water_full");
        }

        WateringCanWater = MaxWateringCanWater;
        WaterChanged?.Invoke();
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "notice.water_refilled");
    }

    private ActionResult CastFishingLine(GridPosition target)
    {
        if (!WorldDefinition.IsWaterSource(target))
        {
            return ActionResult.Fail("notice.not_fishing_water");
        }

        if (Energy < FishingSystem.CastEnergyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        var result = Fishing.TryCatch(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            Weather.CurrentId,
            Inventory
        );
        if (!result.Succeeded)
        {
            return result;
        }

        Energy = Math.Max(0, Energy - result.EnergyCost);
        EnergyChanged?.Invoke();
        Changed?.Invoke();
        return result;
    }

    public ActionResult BeginFishingChallenge(GridPosition target)
    {
        if (!WorldDefinition.IsWaterSource(target))
        {
            return ActionResult.Fail("notice.not_fishing_water");
        }
        if (FishingMinigame.IsActive)
        {
            return ActionResult.Fail("fishing.minigame.active");
        }
        if (Energy < FishingSystem.CastEnergyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        var fish = Fishing.PreviewCatch(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            Weather.CurrentId
        );
        if (fish is null)
        {
            return ActionResult.Fail("notice.fish_not_biting");
        }
        if (!Inventory.CanAdd(fish.ItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var baitId = FishingProgression.EquippedBaitId;
        if (!string.IsNullOrWhiteSpace(baitId) &&
            Inventory.Count(baitId) <= 0)
        {
            FishingProgression.ClearBaitIfMissing(Inventory);
            baitId = string.Empty;
        }

        BeginChangedBatch();
        try
        {
            if (!string.IsNullOrWhiteSpace(baitId) &&
                !Inventory.Remove(baitId, 1))
            {
                return ActionResult.Fail("fishing.gear.bait_missing");
            }

            FishingMinigame.Begin(fish, FishingProgression);
            FishingProgression.ClearBaitIfMissing(Inventory);
            Energy -= FishingSystem.CastEnergyCost;
            EnergyChanged?.Invoke();
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(
            FishingSystem.CastEnergyCost,
            "fishing.minigame.started"
        );
    }

    public FishingChallengeSnapshot AdvanceFishingChallenge(
        float deltaSeconds,
        bool reeling
    ) => FishingMinigame.Advance(deltaSeconds, reeling);

    public ActionResult ResolveFishingChallenge()
    {
        var challenge = FishingMinigame.Snapshot();
        if (challenge.Status == FishingChallengeStatus.Active)
        {
            return ActionResult.Fail("fishing.minigame.active");
        }
        if (challenge.Status == FishingChallengeStatus.Idle)
        {
            return ActionResult.Fail("fishing.minigame.idle");
        }

        FishingMinigame.Reset();
        if (challenge.Status == FishingChallengeStatus.Failed)
        {
            return ActionResult.Fail("fishing.minigame.failed");
        }

        var result = Fishing.CommitCatch(
            challenge.FishId,
            Inventory,
            0
        );
        if (result.Succeeded)
        {
            FishingProgression.RecordCatch(challenge.Difficulty);
        }
        return result;
    }

    public ActionResult PurchaseFishingGear(string itemId)
    {
        var offer = FishingProgressionCatalog.GearOffers.FirstOrDefault(
            candidate => candidate.ItemId == itemId
        );
        if (offer is null)
        {
            return ActionResult.Fail("fishing.gear.unknown");
        }
        if (FishingProgression.Level < offer.RequiredLevel)
        {
            return ActionResult.Fail("fishing.gear.level_locked");
        }
        if (offer.Kind == FishingGearOfferKind.Bobber &&
            FishingProgression.OwnsBobber(itemId))
        {
            return ActionResult.Fail("fishing.gear.already_owned");
        }
        if (Coins < offer.CoinCost)
        {
            return ActionResult.Fail("shop.not_enough_coins");
        }
        if (!HasFishingMaterials(offer.Materials))
        {
            return ActionResult.Fail("fishing.gear.materials_missing");
        }
        if (offer.Kind != FishingGearOfferKind.Bobber &&
            !Inventory.CanAdd(offer.ItemId, offer.Quantity))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        BeginChangedBatch();
        try
        {
            if (offer.Materials.Count > 0 &&
                !Inventory.TryRemoveMany(offer.Materials))
            {
                return ActionResult.Fail("fishing.gear.materials_changed");
            }

            if (offer.Kind == FishingGearOfferKind.Bobber)
            {
                FishingProgression.RegisterOwnedBobber(offer.ItemId);
            }
            else if (!Inventory.Add(offer.ItemId, offer.Quantity))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            Coins -= offer.CoinCost;
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "fishing.gear.purchased");
    }

    public ActionResult UpgradeFishingRod()
    {
        var next = FishingProgressionCatalog.RodTiers.FirstOrDefault(tier =>
            tier.Rank == FishingProgression.RodTier.Rank + 1
        );
        if (next is null)
        {
            return ActionResult.Fail("fishing.rod.max_tier");
        }
        if (FishingProgression.Level < next.RequiredLevel)
        {
            return ActionResult.Fail("fishing.gear.level_locked");
        }
        if (Coins < next.CoinCost)
        {
            return ActionResult.Fail("shop.not_enough_coins");
        }
        if (!HasFishingMaterials(next.Materials))
        {
            return ActionResult.Fail("fishing.gear.materials_missing");
        }

        BeginChangedBatch();
        try
        {
            if (!Inventory.TryRemoveMany(next.Materials))
            {
                return ActionResult.Fail("fishing.gear.materials_changed");
            }
            Coins -= next.CoinCost;
            FishingProgression.ApplyNextRodTier();
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "fishing.rod.upgraded");
    }

    public ActionResult EquipFishingBait(string itemId) =>
        FishingProgression.EquipBait(itemId, Inventory);

    public ActionResult EquipFishingBobber(string itemId) =>
        FishingProgression.EquipBobber(itemId);

    public ActionResult ChooseFishingSpecialization(string specializationId) =>
        FishingProgression.ChooseSpecialization(specializationId);

    private bool HasFishingMaterials(
        IReadOnlyList<CraftingIngredient> materials
    ) => materials.All(material =>
        Inventory.Count(material.ItemId) >= material.Count
    );

    public bool InteractWithMira()
    {
        var givesSeeds = Quest.InteractWithMira();
        if (givesSeeds)
        {
            Inventory.Add(DataCatalog.StarbudSeedId, 5);
        }

        Changed?.Invoke();
        return givesSeeds;
    }

    public VillageConversation? InteractWithVillager(
        GridPosition target,
        out ActionResult result
    )
    {
        var eligibleCharacterEvent = CharacterEvents.EligibleEvent(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            Inventory.Selected.ItemId,
            Village,
            PlayerCell
        );
        var conversation = Village.Interact(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            Inventory.Selected.ItemId,
            Inventory,
            out result,
            PlayerCell
        );
        if (conversation is not null)
        {
            if (conversation.GiftReaction is null &&
                eligibleCharacterEvent is not null)
            {
                var characterEvent = CharacterEvents.BeginEvent(
                    eligibleCharacterEvent
                );
                conversation = conversation with
                {
                    CharacterEvent = characterEvent
                };
            }

            Changed?.Invoke();
        }

        return conversation;
    }

    public ActionResult CompleteCharacterEvent(string eventId) =>
        CharacterEvents.CompleteActiveEvent(eventId, Clock.Day);

    public ActionResult CheckFestivalEntrance(
        string festivalId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByFestivalId(
                festivalId,
                out var spatial
            ) || PlayerLocationId != PlayerLocationIds.World ||
            target != spatial.WorldEntryCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return FestivalCatalog.IsOpen(
            festivalId,
            Clock.Day,
            Clock.MinuteOfDay
        )
            ? ActionResult.Success(
                messageKey: FestivalCatalog.EnterNoticeKey(festivalId)
            )
            : ActionResult.Fail(FestivalCatalog.ClosedKey(festivalId));
    }

    public ActionResult TryEnterFestival(
        string festivalId,
        GridPosition target
    )
    {
        var result = CheckFestivalEntrance(festivalId, target);
        if (result.Succeeded &&
            festivalId == FestivalCatalog.GleamrisePlantingFestivalId)
        {
            GleamriseSeason.RecordMilestone(
                GleamriseSeasonGoalSystem.CounterFestivalJoined
            );
        }

        return result;
    }

    public ActionResult CheckStarharvestMarketEntrance(GridPosition target) =>
        CheckFestivalEntrance(
            FestivalCatalog.StarharvestMarketFestivalId,
            target
        );

    public ActionResult TryEnterStarharvestMarket(GridPosition target) =>
        TryEnterFestival(
            FestivalCatalog.StarharvestMarketFestivalId,
            target
        );

    public ActionResult CheckFestivalExit(GridPosition target)
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ) || target != spatial.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: FestivalCatalog.LeaveNoticeKey(
                    spatial.FestivalId
                )
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryExitFestival(GridPosition target) =>
        CheckFestivalExit(target);

    public ActionResult CheckStarharvestMarketExit(GridPosition target) =>
        InsideStarharvestMarket
            ? CheckFestivalExit(target)
            : ActionResult.Fail("notice.nothing_to_interact");

    public ActionResult TryExitStarharvestMarket(GridPosition target) =>
        CheckStarharvestMarketExit(target);

    public ActionResult CheckFestivalStation(
        string stationId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var station = spatial.Stations.FirstOrDefault(entry =>
            entry.Id == stationId);
        if (station is null || target != station.Cell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        if (!FestivalCatalog.IsOpen(
                spatial.FestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return ActionResult.Fail(
                FestivalCatalog.ClosedKey(spatial.FestivalId)
            );
        }

        return ActionResult.Success(messageKey: station.Id switch
        {
            FestivalCatalog.StarharvestShopId => "festival.shop.opened",
            FestivalCatalog.GleamriseSharedBloomfieldActivityId =>
                "festival.gleamrise.activity.opened",
            FestivalCatalog.GleamriseSeedExchangeId =>
                "festival.gleamrise.exchange.opened",
            FestivalCatalog.LongnightSharedTableId =>
                "festival.longnight.activity.opened",
            FestivalCatalog.LongnightGiftExchangeId =>
                "festival.longnight.exchange.opened",
            FestivalCatalog.LongnightStarlightRiteId =>
                "festival.longnight.rite.opened",
            FestivalCatalog.LongnightLanternStallId =>
                "festival.longnight.stall.opened",
            FestivalCatalog.FireflyLanternLaunchId =>
                "festival.firefly.activity.opened",
            FestivalCatalog.FireflyFishBasinId =>
                "festival.firefly.basin.opened",
            FestivalCatalog.FireflyTideAltarId =>
                "festival.firefly.altar.opened",
            FestivalCatalog.FireflyGlowshopId =>
                "festival.firefly.shop.opened",
            _ => "festival.showcase.opened"
        });
    }

    public ActionResult CheckFestivalStation(GridPosition target)
    {
        if (!InsideStarharvestMarket)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var station = FestivalSpatialCatalog.StarharvestMarket.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    public FestivalSubmissionPreview PreviewFestivalSubmission(
        IReadOnlyList<string>? itemIds
    )
    {
        if (!InsideStarharvestMarket ||
            !FestivalCatalog.IsOpen(
                FestivalCatalog.StarharvestMarketFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalSubmissionPreview(
                false,
                "festival.starharvest.closed",
                itemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        return Festival.CheckSubmission(
            FestivalCatalog.StarharvestMarketFestivalId,
            CalendarSystem.YearNumber(Clock.Day),
            itemIds,
            Inventory
        );
    }

    public FestivalSubmissionResult SubmitFestivalExhibit(
        IReadOnlyList<string> itemIds
    )
    {
        var preview = PreviewFestivalSubmission(itemIds);
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(false, preview.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.Submit(
                FestivalCatalog.StarharvestMarketFestivalId,
                CalendarSystem.YearNumber(Clock.Day),
                itemIds,
                Inventory
            );
            if (!result.Succeeded || result.Result is null)
            {
                return result;
            }

            Coins += result.Result.AuctionCoins;
            Starlight.RefreshRewardUnlocks(StarlightProgress());
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckFestivalPurchase(string offerId)
    {
        if (!InsideStarharvestMarket ||
            !FestivalCatalog.IsOpen(
                FestivalCatalog.StarharvestMarketFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.starharvest.closed",
                null
            );
        }

        return Festival.CheckPurchase(offerId, Inventory);
    }

    public ActionResult BuyFestivalItem(string offerId)
    {
        var check = CheckFestivalPurchase(offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.Purchase(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalLongnightPreview CheckLongnightFeastParticipation(
        GridPosition target,
        IReadOnlyList<string>? dishItemIds,
        string exchangeId
    )
    {
        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null || station.Id is not (
                FestivalCatalog.LongnightSharedTableId or
                FestivalCatalog.LongnightGiftExchangeId or
                FestivalCatalog.LongnightStarlightRiteId))
        {
            return InvalidLongnightPreview(
                "notice.nothing_to_interact",
                dishItemIds
            );
        }

        var access = CheckFestivalStation(station.Id, target);
        if (!access.Succeeded)
        {
            return InvalidLongnightPreview(
                access.MessageKey,
                dishItemIds
            );
        }

        return Festival.CheckLongnightContribution(
            CalendarSystem.YearNumber(Clock.Day),
            dishItemIds,
            exchangeId,
            Inventory
        );
    }

    public FestivalLongnightResult CompleteLongnightFeast(
        GridPosition target,
        IReadOnlyList<string> dishItemIds,
        string exchangeId
    )
    {
        var preview = CheckLongnightFeastParticipation(
            target,
            dishItemIds,
            exchangeId
        );
        if (!preview.CanComplete)
        {
            return new FestivalLongnightResult(
                false,
                preview.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.SubmitLongnightContribution(
                CalendarSystem.YearNumber(Clock.Day),
                dishItemIds,
                exchangeId,
                Inventory
            );
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
                NotifyChanged();
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckLongnightStallPurchase(
        GridPosition target,
        string offerId
    )
    {
        var access = CheckFestivalStation(
            FestivalCatalog.LongnightLanternStallId,
            target
        );
        return access.Succeeded
            ? Festival.CheckLongnightPurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, access.MessageKey, null);
    }

    public ActionResult BuyLongnightStallItem(
        GridPosition target,
        string offerId
    )
    {
        var check = CheckLongnightStallPurchase(target, offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseLongnightOffer(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalSubmissionPreview CheckFireflyTideParticipation(
        GridPosition target,
        IReadOnlyList<string>? fishItemIds
    )
    {
        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null || station.Id is not (
                FestivalCatalog.FireflyLanternLaunchId or
                FestivalCatalog.FireflyFishBasinId or
                FestivalCatalog.FireflyTideAltarId))
        {
            return new FestivalSubmissionPreview(
                false,
                "notice.nothing_to_interact",
                fishItemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        var access = CheckFestivalStation(station.Id, target);
        if (!access.Succeeded)
        {
            return new FestivalSubmissionPreview(
                false,
                access.MessageKey,
                fishItemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        return Festival.CheckFireflyTideContribution(
            CalendarSystem.YearNumber(Clock.Day),
            fishItemIds,
            Inventory
        );
    }

    public FestivalSubmissionResult CompleteFireflyTide(
        GridPosition target,
        IReadOnlyList<string> fishItemIds
    )
    {
        var preview = CheckFireflyTideParticipation(target, fishItemIds);
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(
                false,
                preview.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.SubmitFireflyTideContribution(
                CalendarSystem.YearNumber(Clock.Day),
                fishItemIds,
                Inventory
            );
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
                NotifyChanged();
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckFireflyShopPurchase(
        GridPosition target,
        string offerId
    )
    {
        var access = CheckFestivalStation(
            FestivalCatalog.FireflyGlowshopId,
            target
        );
        return access.Succeeded
            ? Festival.CheckFireflyPurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, access.MessageKey, null);
    }

    public ActionResult BuyFireflyShopItem(
        GridPosition target,
        string offerId
    )
    {
        var check = CheckFireflyShopPurchase(target, offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseFireflyOffer(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingStartCheck CheckStartGleamriseChallenge(
        IReadOnlyList<string>? selectedSeedItemIds
    )
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSharedBloomfieldActivityId,
            GleamrisePlantingFestivalLayout.ActivityTableCell
        );
        if (!station.Succeeded)
        {
            return new FestivalPlantingStartCheck(
                false,
                station.MessageKey,
                selectedSeedItemIds?.ToArray() ?? []
            );
        }

        return Festival.CheckStartPlantingChallenge(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            selectedSeedItemIds
        );
    }

    public ActionResult StartGleamriseChallenge(
        IReadOnlyList<string> selectedSeedItemIds
    )
    {
        var check = CheckStartGleamriseChallenge(selectedSeedItemIds);
        if (!check.CanStart)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.StartPlantingChallenge(
                CalendarSystem.YearNumber(Clock.Day),
                Clock.MinuteOfDay,
                selectedSeedItemIds
            );
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckSelectGleamriseSeed(string seedItemId)
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSharedBloomfieldActivityId,
            GleamrisePlantingFestivalLayout.ActivityTableCell
        );
        return station.Succeeded
            ? Festival.CheckSelectPlantingSeed(
                CalendarSystem.YearNumber(Clock.Day),
                seedItemId
            )
            : station;
    }

    public ActionResult SelectGleamriseSeed(string seedItemId)
    {
        var check = CheckSelectGleamriseSeed(seedItemId);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            return Festival.SelectPlantingSeed(
                CalendarSystem.YearNumber(Clock.Day),
                seedItemId
            );
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingCheck CheckGleamrisePlot(GridPosition target)
    {
        if (!InsideGleamrisePlantingFestival ||
            !GleamrisePlantingFestivalLayout.PlotIdsByCell.TryGetValue(
                target,
                out var plotId
            ) || Distance(PlayerCell, target) != 1)
        {
            return new FestivalPlantingCheck(
                false,
                "notice.nothing_to_interact",
                string.Empty,
                string.Empty
            );
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return new FestivalPlantingCheck(
                false,
                "notice.needs_hand",
                plotId,
                string.Empty
            );
        }

        if (!FestivalCatalog.IsOpen(
                FestivalCatalog.GleamrisePlantingFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalPlantingCheck(
                false,
                "festival.gleamrise.closed",
                plotId,
                string.Empty
            );
        }

        return Festival.CheckPlantingPlot(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            plotId
        );
    }

    public FestivalPlantingResolution PlantGleamrisePlot(
        GridPosition target
    )
    {
        var check = CheckGleamrisePlot(target);
        if (!check.CanPlant)
        {
            return new FestivalPlantingResolution(
                false,
                false,
                check.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.PlantPlot(
                CalendarSystem.YearNumber(Clock.Day),
                Clock.MinuteOfDay,
                check.PlotId
            );
            if (result.Completed)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
            }

            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckGleamriseSeedPurchase(
        string offerId
    )
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSeedExchangeId,
            GleamrisePlantingFestivalLayout.SeedExchangeCell
        );
        return station.Succeeded
            ? Festival.CheckGleamrisePurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, station.MessageKey, null);
    }

    public ActionResult BuyGleamriseSeeds(string offerId)
    {
        var check = CheckGleamriseSeedPurchase(offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseGleamriseSeeds(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingResolution ResolveGleamriseChallengeDeadline(
        bool force = false
    )
    {
        var result = Festival.ResolvePlantingAttempt(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            force
        );
        if (result.Completed)
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }

        return result;
    }

    public bool LeaveFestivalIfClosed()
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ) ||
            FestivalCatalog.IsOpen(
                spatial.FestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return false;
        }

        if (spatial.FestivalId ==
            FestivalCatalog.GleamrisePlantingFestivalId)
        {
            _ = ResolveGleamriseChallengeDeadline(true);
        }

        SetPlayerLocation(
            spatial.WorldReturnCell.X * 16 + 8,
            spatial.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        return true;
    }

    public ActionResult TryEnterGreenhouse(GridPosition target)
    {
        var check = CheckGreenhouseEntrance(target);
        return check.Succeeded
            ? ActionResult.Success(messageKey: "notice.enter_greenhouse")
            : check;
    }

    public ActionResult TryExitGreenhouse(GridPosition target)
    {
        var check = CheckGreenhouseExit(target);
        return check.Succeeded
            ? ActionResult.Success(messageKey: "notice.leave_greenhouse")
            : check;
    }

    public ActionResult TryEnterStarfeatherCoop(GridPosition target)
        => TryEnterAnimalBuilding(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult TryExitStarfeatherCoop(GridPosition target)
        => TryExitAnimalBuilding(AnimalCatalog.StarfeatherCoopId, target);

    public ActionResult TryEnterAnimalBuilding(
        string buildingId,
        GridPosition target
    )
    {
        var check = CheckAnimalBuildingEntrance(buildingId, target);
        return check.Succeeded
            ? ActionResult.Success(
                messageKey: buildingId == AnimalCatalog.MoonfleeceBarnId
                    ? "notice.enter_moonfleece_barn"
                    : "notice.enter_starfeather_coop"
            )
            : check;
    }

    public ActionResult TryExitAnimalBuilding(
        string buildingId,
        GridPosition target
    )
    {
        var check = CheckAnimalBuildingExit(buildingId, target);
        return check.Succeeded
            ? ActionResult.Success(
                messageKey: buildingId == AnimalCatalog.MoonfleeceBarnId
                    ? "notice.leave_moonfleece_barn"
                    : "notice.leave_starfeather_coop"
            )
            : check;
    }

    public IReadOnlyList<string> TwilightEmporiumItemIds()
    {
        var stock = TwilightEmporiumSystem.StockForDay(Clock.Day).ToList();
        if (AnimalCatalog.Buildings.Any(building =>
                building.FeedItemId == DataCatalog.MeadowFodderId &&
                Construction.IsCompletedFor(building.ConstructionProjectId)
            ))
        {
            stock.Add(DataCatalog.MeadowFodderId);
        }

        return stock.Distinct(StringComparer.Ordinal).ToArray();
    }

    public ActionResult TryEnterMoonlitArchive()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.archive_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsMoonlitArchiveOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(messageKey: "notice.enter_archive")
            : ActionResult.Fail("notice.archive_closed");
    }

    public ActionResult CheckMoonlitArchiveCompendium(GridPosition target)
    {
        if (!InsideArchive ||
            !VillageCatalog.IsMoonlitArchiveDeskCell(target) ||
            !VillageCatalog.IsAdjacentToMoonlitArchiveDesk(PlayerCell))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(messageKey: "collection.archive.opened");
    }

    public ActionResult OpenMoonlitArchiveCompendium(GridPosition target) =>
        CheckMoonlitArchiveCompendium(target);

    public ActionResult InspectMoonlitArchiveDesk() =>
        OpenMoonlitArchiveCompendium(VillageCatalog.MoonlitArchiveDeskCell);

    public ActionResult CheckCollectionReward(
        GridPosition target,
        string rewardId
    )
    {
        var access = CheckMoonlitArchiveCompendium(target);
        return access.Succeeded
            ? Collection.CheckClaimReward(rewardId)
            : access;
    }

    public ActionResult ClaimCollectionReward(
        GridPosition target,
        string rewardId
    )
    {
        var access = CheckMoonlitArchiveCompendium(target);
        if (!access.Succeeded)
        {
            return access;
        }

        return Collection.ClaimReward(rewardId);
    }

    public ActionResult CheckDonateCollectionEntry(
        GridPosition target,
        string entryId
    )
    {
        var access = CheckMoonlitArchiveCompendium(target);
        return access.Succeeded
            ? Collection.CheckDonateEntry(entryId, Inventory)
            : access;
    }

    public ActionResult DonateCollectionEntry(
        GridPosition target,
        string entryId
    )
    {
        var check = CheckDonateCollectionEntry(target, entryId);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            var result = Collection.DonateEntry(entryId, Inventory);
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public IReadOnlyList<FishingCollectionRewardSnapshot>
        FishingCollectionRewards() => Fishing.RewardSnapshots();

    public IReadOnlyList<GleamriseGoalSnapshot> GleamriseSeasonGoals() =>
        GleamriseSeason.Snapshots(Clock.Day, Inventory);

    public GleamriseGoalClaimResult ClaimGleamriseSeasonGoal(string goalId)
    {
        BeginChangedBatch();
        try
        {
            var result = GleamriseSeason.Claim(goalId, Clock.Day, Inventory);
            if (!result.Succeeded)
            {
                return result;
            }

            Coins += result.RewardCoins;
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FishingCollectionRewardClaimResult ClaimFishingCollectionReward(
        string rewardId
    )
    {
        BeginChangedBatch();
        try
        {
            var result = Fishing.ClaimReward(rewardId, Inventory);
            if (!result.Succeeded)
            {
                return result;
            }

            Coins += result.RewardCoins;
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public IReadOnlyList<FishingDonationEntry> FishingDonationEntries() =>
        Fishing.DonationEntries(Inventory);

    public FishingDonationResult DonateFishToArchive(string fishId)
    {
        if (!InsideArchive)
        {
            return new FishingDonationResult(
                false,
                "notice.nothing_to_interact"
            );
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return new FishingDonationResult(false, "notice.needs_hand");
        }

        return Fishing.DonateFish(fishId, Inventory);
    }

    public void RecordGleamriseSeasonMilestone(
        string milestoneId,
        int count = 1
    ) => GleamriseSeason.RecordMilestone(milestoneId, count);

    public ActionResult TryExitMoonlitArchive()
    {
        if (!InsideArchive)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "notice.leave_archive")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterMoonstoneWorkshop()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.workshop_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsMoonstoneWorkshopOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(messageKey: "notice.enter_workshop")
            : ActionResult.Fail("notice.workshop_closed");
    }

    public ActionResult OpenConstructionPanel()
    {
        if (!InsideWorkshop)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(messageKey: "construction.panel.opened");
    }

    public ActionResult CheckStartToolUpgrade(
        GridPosition target,
        string upgradeId
    )
    {
        var access = CheckToolUpgradeAccess(target);
        return access.Succeeded
            ? ToolProgression.CheckStartUpgrade(
                upgradeId,
                Inventory,
                Coins
            )
            : access;
    }

    public ActionResult StartToolUpgrade(
        GridPosition target,
        string upgradeId
    )
    {
        var check = CheckStartToolUpgrade(target, upgradeId);
        if (!check.Succeeded)
        {
            return check;
        }

        var upgrade = ToolProgressionCatalog.Upgrade(upgradeId);
        BeginChangedBatch();
        try
        {
            if (!Inventory.TryRemoveMany(upgrade.Materials))
            {
                return ActionResult.Fail("tool.upgrade.materials_changed");
            }

            Coins -= upgrade.CoinCost;
            ToolProgression.BeginCheckedUpgrade(upgrade.Id);
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "tool.upgrade.started");
    }

    private ActionResult CheckToolUpgradeAccess(GridPosition target)
        => CheckCrystalGrottoUpgradeBench(target);

    public bool StarGateVisible =>
        Starlight.StarfallSixfoldConvergenceUnlocked ||
        Construction.PhaseFor(
            ConstructionCatalog.SixfoldStarGateProjectId
        ) != ConstructionPhase.NotStarted ||
        StarGate.Activated;

    private ActionResult UseStarGate(
        GridPosition target,
        string selectedItemId
    )
    {
        if (!IsStarGateInReach(target) || !StarGateVisible)
        {
            return ActionResult.Fail("star_gate.unavailable");
        }

        var projectId = ConstructionCatalog.SixfoldStarGateProjectId;
        var phase = Construction.PhaseFor(projectId);
        if (phase == ConstructionPhase.NotStarted)
        {
            return ActionResult.Fail("star_gate.construction_required");
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return ActionResult.Fail("star_gate.construction_in_progress");
        }

        if (selectedItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        if (!StarGate.Activated)
        {
            return StarGate.Activate(constructionCompleted: true);
        }

        return ActionResult.Success(messageKey: "star_gate.travel_opened");
    }

    private TargetPreview PreviewStarGate(
        GridPosition target,
        string selectedItemId
    )
    {
        if (!IsStarGateInReach(target) || !StarGateVisible)
        {
            return TargetPreview.Neutral(target);
        }

        var phase = Construction.PhaseFor(
            ConstructionCatalog.SixfoldStarGateProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return TargetPreview.Blocked(
                FarmLayout.StarGateCell,
                TargetPreviewKind.StarGate,
                "star_gate.construction_required"
            );
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return TargetPreview.Blocked(
                FarmLayout.StarGateCell,
                TargetPreviewKind.StarGate,
                "star_gate.construction_in_progress"
            );
        }

        if (selectedItemId != DataCatalog.HandId)
        {
            return TargetPreview.NeedsTool(
                FarmLayout.StarGateCell,
                TargetPreviewKind.StarGate,
                "target.need.hand"
            );
        }

        var actionKey = StarGate.Activated
            ? "target.action.open_star_gate"
            : "target.action.activate_star_gate";
        return TargetPreview.Available(
            FarmLayout.StarGateCell,
            TargetPreviewKind.StarGate,
            actionKey
        );
    }

    private bool IsStarGateInReach(GridPosition target) =>
        PlayerLocationId == PlayerLocationIds.World &&
        target == FarmLayout.StarGateCell &&
        Math.Abs(PlayerCell.X - target.X) +
            Math.Abs(PlayerCell.Y - target.Y) == 1;

    public ActionResult TravelStarGate(string destinationId)
    {
        var result = StarGate.Travel(destinationId);
        if (!result.Succeeded ||
            !StarGateCatalog.TryDestination(
                destinationId,
                out var destination
            ))
        {
            return result;
        }

        SetPlayerLocation(
            destination.ArrivalCell.X * 16 + 8,
            destination.ArrivalCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        return result;
    }

    public ActionResult StartConstruction(string projectId)
    {
        var access = CheckConstructionStartAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        if (projectId == ConstructionCatalog.SixfoldStarGateProjectId &&
            !Starlight.StarfallSixfoldConvergenceUnlocked)
        {
            return ActionResult.Fail(
                "construction.sixfold_star_gate.requires_six_lights"
            );
        }

        var check = Construction.CheckStart(projectId, Inventory, Coins);
        if (!check.Succeeded)
        {
            return check;
        }

        var project = ConstructionCatalog.Project(projectId);
        BeginChangedBatch();
        try
        {
            if (!Inventory.TryRemoveMany(project.Materials))
            {
                return ActionResult.Fail("construction.materials_changed");
            }

            Coins -= project.CoinCost;
            Construction.BeginChecked(projectId);
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "construction.started");
    }

    public ActionResult StartCottageFirstUpgrade() => StartConstruction(
        ConstructionCatalog.CottageFirstUpgradeId
    );

    public ActionResult CheckHomesteadWorkbench(GridPosition target)
    {
        return CheckHomesteadWorkbenchAccess(target, requireHand: true);
    }

    public ActionResult OpenHomesteadWorkbench(GridPosition target) =>
        CheckHomesteadWorkbench(target);

    private ActionResult CheckConstructionStartAccess()
    {
        if (InsideWorkshop)
        {
            return ActionResult.Success();
        }

        var access = CheckHomesteadWorkbenchAccess(
            FarmLayout.HomesteadWorkbenchCell,
            requireHand: false
        );
        if (access.Succeeded || access.MessageKey is
            "construction.homestead_workshop.not_started" or
            "construction.homestead_workshop.in_progress")
        {
            return access;
        }

        return ActionResult.Fail("construction.workshop_only");
    }

    private ActionResult CheckHomesteadWorkbenchAccess(
        GridPosition target,
        bool requireHand
    )
    {
        if (PlayerLocationId != PlayerLocationIds.World ||
            target != FarmLayout.HomesteadWorkbenchCell ||
            Math.Abs(PlayerCell.X - target.X) +
                Math.Abs(PlayerCell.Y - target.Y) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var phase = Construction.PhaseFor(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        if (phase == ConstructionPhase.NotStarted)
        {
            return ActionResult.Fail(
                "construction.homestead_workshop.not_started"
            );
        }

        if (phase == ConstructionPhase.InProgress)
        {
            return ActionResult.Fail(
                "construction.homestead_workshop.in_progress"
            );
        }

        return !requireHand ||
            Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: "construction.panel.opened"
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult InspectKitchenReserve(GridPosition target)
    {
        var check = CheckKitchenReserve(target);
        return check.Succeeded
            ? ActionResult.Success(
                messageKey: "construction.kitchen_reserve.dialogue"
            )
            : check;
    }

    public ActionResult CheckKitchenStation(GridPosition target) =>
        CheckCottageFacility(
            target,
            CottageLayout.IsKitchenStationArea,
            CottageLayout.IsAdjacentToKitchenStation,
            "kitchen.opened"
        );

    public ActionResult OpenKitchenStation(GridPosition target) =>
        CheckKitchenStation(target);

    public ActionResult CheckIngredientPantry(GridPosition target) =>
        CheckCottageFacility(
            target,
            CottageLayout.IsIngredientPantryArea,
            CottageLayout.IsAdjacentToIngredientPantry,
            "kitchen.pantry.opened"
        );

    public ActionResult OpenIngredientPantry(GridPosition target) =>
        CheckIngredientPantry(target);

    public ActionResult CheckCookRecipe(
        GridPosition target,
        string recipeId
    )
    {
        var access = CheckKitchenStation(target);
        return access.Succeeded
            ? Kitchen.CheckCook(recipeId, Inventory)
            : access;
    }

    public ActionResult CookRecipe(
        GridPosition target,
        string recipeId
    )
    {
        var access = CheckKitchenStation(target);
        if (!access.Succeeded)
        {
            return access;
        }

        BeginChangedBatch();
        try
        {
            return Kitchen.Cook(recipeId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckStoreKitchenIngredient(
        GridPosition target,
        string itemId,
        int count = 1
    )
    {
        var access = CheckIngredientPantry(target);
        return access.Succeeded
            ? Kitchen.CheckStoreIngredient(itemId, count, Inventory)
            : access;
    }

    public ActionResult StoreKitchenIngredient(
        GridPosition target,
        string itemId,
        int count = 1
    )
    {
        var access = CheckIngredientPantry(target);
        if (!access.Succeeded)
        {
            return access;
        }

        BeginChangedBatch();
        try
        {
            return Kitchen.StoreIngredient(itemId, count, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckTakeKitchenIngredient(
        GridPosition target,
        string itemId,
        int count = 1
    )
    {
        var access = CheckIngredientPantry(target);
        return access.Succeeded
            ? Kitchen.CheckTakeIngredient(itemId, count, Inventory)
            : access;
    }

    public ActionResult TakeKitchenIngredient(
        GridPosition target,
        string itemId,
        int count = 1
    )
    {
        var access = CheckIngredientPantry(target);
        if (!access.Succeeded)
        {
            return access;
        }

        BeginChangedBatch();
        try
        {
            return Kitchen.TakeIngredient(itemId, count, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckEatCookedDish(string itemId)
    {
        if (!DataCatalog.CookedDishes.ContainsKey(itemId) ||
            !DataCatalog.Items.TryGetValue(itemId, out var item) ||
            item.Kind != ItemKind.CookedDish)
        {
            return ActionResult.Fail("cooking.not_cooked_dish");
        }

        if (Inventory.Count(itemId) <= 0)
        {
            return ActionResult.Fail("cooking.dish_missing");
        }

        return Energy >= MaxEnergy
            ? ActionResult.Fail("cooking.energy_full")
            : ActionResult.Success(messageKey: "cooking.ready_to_eat");
    }

    public int EffectiveDishEnergyRestore(string itemId)
    {
        if (!DataCatalog.CookedDishes.TryGetValue(itemId, out var dish))
        {
            return 0;
        }

        var journal = CompendiumCatalog.Rewards[
            CollectionRewardIds.MoonhearthRecipeJournal
        ];
        var bonus = Collection.IsRewardClaimed(journal.Id) &&
            journal.RequiredEntryIds.Contains(itemId, StringComparer.Ordinal)
                ? CompendiumCatalog.MoonhearthRecipeJournalEnergyBonus
                : 0;
        return dish.EnergyRestore + bonus;
    }

    public ActionResult EatCookedDish(string itemId)
    {
        var check = CheckEatCookedDish(itemId);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            if (!Inventory.Remove(itemId, 1))
            {
                return ActionResult.Fail("cooking.dish_missing");
            }

            Energy = Math.Min(
                MaxEnergy,
                Energy + EffectiveDishEnergyRestore(itemId)
            );
            EnergyChanged?.Invoke();
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "cooking.ate");
    }

    public ActionResult RestInCottage()
    {
        if (!InsideCottage)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return ActionResult.Success(messageKey: "target.action.rest");
    }

    public ActionResult ExitCottage()
    {
        if (!InsideCottage)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return ActionResult.Success(messageKey: "target.action.exit");
    }

    public ActionResult TryExitMoonstoneWorkshop()
    {
        if (!InsideWorkshop)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "notice.leave_workshop")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterStarweaverTeaHouse()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.tea_house_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarweaverTeaHouseOpen(
                Clock.MinuteOfDay
            )
            ? ActionResult.Success(messageKey: "notice.enter_tea_house")
            : ActionResult.Fail("notice.tea_house_closed");
    }

    public ActionResult InspectStarwovenTeaCounter()
    {
        if (!InsideTeaHouse)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: "tea_house.counter.dialogue"
        );
    }

    public ActionResult TryExitStarweaverTeaHouse()
    {
        if (!InsideTeaHouse)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "notice.leave_tea_house")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterTwilightEmporium()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.emporium_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        var access = VillageCatalog.TwilightEmporiumAccess(
            Clock.Day,
            Clock.MinuteOfDay
        );
        return access.IsOpen
            ? ActionResult.Success(messageKey: "notice.enter_emporium")
            : ActionResult.Fail(access.NoticeKey);
    }

    public ActionResult InspectTravelManifest()
    {
        if (!InsideTwilightEmporium)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        var access = VillageCatalog.TwilightEmporiumAccess(
            Clock.Day,
            Clock.MinuteOfDay
        );
        if (!access.IsOpen)
        {
            return ActionResult.Fail(access.NoticeKey);
        }

        return ActionResult.Success(messageKey: "emporium.manifest.opened");
    }

    public ActionResult TryExitTwilightEmporium()
    {
        if (!InsideTwilightEmporium)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: "notice.leave_emporium")
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterStarlightPost()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.starlight_post_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarlightPostOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(
                messageKey: "notice.enter_starlight_post"
            )
            : ActionResult.Fail("notice.starlight_post_closed");
    }

    public ActionResult InspectRouteSortingCounter()
    {
        if (!InsideStarlightPost)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: "starlight_post.counter.dialogue"
        );
    }

    public ActionResult TryExitStarlightPost()
    {
        if (!InsideStarlightPost)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: "notice.leave_starlight_post"
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryEnterStarfallWatch()
    {
        if (PlayerLocationId != PlayerLocationIds.World)
        {
            return ActionResult.Fail("notice.starfall_watch_world_only");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarfallWatchOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(
                messageKey: "notice.enter_starfall_watch"
            )
            : ActionResult.Fail("notice.starfall_watch_closed");
    }

    public ActionResult InspectSealRouteTable()
    {
        if (!InsideStarfallWatch)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: "starfall_watch.table.dialogue"
        );
    }

    public ActionResult TryExitStarfallWatch()
    {
        if (!InsideStarfallWatch)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: "notice.leave_starfall_watch"
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult BuyItem(string itemId)
    {
        return PurchaseItem(itemId, "shop.bought");
    }

    public ActionResult BuyTwilightEmporiumItem(string itemId)
    {
        if (!InsideTwilightEmporium)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var access = VillageCatalog.TwilightEmporiumAccess(
            Clock.Day,
            Clock.MinuteOfDay
        );
        if (!access.IsOpen)
        {
            return ActionResult.Fail(access.NoticeKey);
        }

        if (!DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return ActionResult.Fail("emporium.shop.unavailable");
        }

        if (item.Kind == ItemKind.Seed &&
            !DataCatalog.IsSeedAvailableOnDay(itemId, Clock.Day))
        {
            return ActionResult.Fail("shop.seed_out_of_season");
        }

        if (!TwilightEmporiumItemIds().Contains(
                itemId,
                StringComparer.Ordinal
            ))
        {
            return ActionResult.Fail("emporium.shop.unavailable");
        }

        return PurchaseItem(itemId, "emporium.shop.bought");
    }

    private ActionResult PurchaseItem(
        string itemId,
        string successKey
    )
    {
        var item = DataCatalog.Item(itemId);
        var price = PurchasePrice(itemId);
        if (price <= 0)
        {
            return ActionResult.Fail("shop.not_for_sale");
        }

        if (item.Kind == ItemKind.Seed &&
            !DataCatalog.IsSeedAvailableOnDay(itemId, Clock.Day))
        {
            return ActionResult.Fail("shop.seed_out_of_season");
        }

        if (item.Kind == ItemKind.Sapling &&
            !DataCatalog.IsSaplingAvailableOnDay(itemId, Clock.Day))
        {
            return ActionResult.Fail("shop.sapling_out_of_season");
        }

        if (Coins < price)
        {
            return ActionResult.Fail("shop.not_enough_coins");
        }

        if (!Inventory.CanAdd(itemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        BeginChangedBatch();
        Inventory.Add(itemId, 1);
        Coins -= price;
        GleamriseSeason.RecordPurchasedItem(itemId, Clock.Day);
        NotifyChanged();
        EndChangedBatch();
        return ActionResult.Success(messageKey: successKey);
    }

    public int PurchasePrice(string itemId)
    {
        var item = DataCatalog.Item(itemId);
        if (item.BuyPrice <= 0 ||
            !Collection.IsRewardClaimed(
                CollectionRewardIds.MoonlitAlmanac
            ) ||
            item.Kind != ItemKind.Seed ||
            string.IsNullOrWhiteSpace(item.CropId) ||
            !DataCatalog.CropIds.Contains(
                item.CropId,
                StringComparer.Ordinal
            ))
        {
            return item.BuyPrice;
        }

        return Math.Max(1, (item.BuyPrice * 9 + 9) / 10);
    }

    public int SalePrice(string itemId)
    {
        var item = DataCatalog.Item(itemId);
        if (item.SellPrice <= 0 ||
            !Collection.IsRewardClaimed(
                CollectionRewardIds.StarlitAppraisalLedger
            ) ||
            !CompendiumCatalog.ArtisanEntries.Any(entry =>
                entry.ItemId == itemId
            ))
        {
            return item.SellPrice;
        }

        return Math.Max(1, (item.SellPrice * 11 + 9) / 10);
    }

    public int PendingShippingValue => Shipping.PendingValueFor(SalePrice);

    public ActionResult SellItem(string itemId)
    {
        var item = DataCatalog.Item(itemId);
        if (item.SellPrice <= 0)
        {
            return ActionResult.Fail("shop.cannot_sell");
        }

        if (!Inventory.Remove(itemId, 1))
        {
            return ActionResult.Fail("shop.nothing_to_sell");
        }

        Coins += SalePrice(itemId);
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "shop.sold");
    }

    public ActionResult QueueForShipping(string itemId) =>
        Shipping.QueueOne(itemId, Inventory);

    public ActionResult ReclaimFromShipping(string itemId) =>
        Shipping.ReclaimOne(itemId, Inventory);

    public ActionResult StartProcessing(string recipeId) =>
        StartProcessing(ProcessorCatalog.MainMachineId, recipeId);

    public ActionResult StartProcessing(string machineId, string recipeId)
    {
        var result = Processor.Start(machineId, recipeId, Inventory);
        if (result.Succeeded)
        {
            GleamriseSeason.RecordProcessorStarted(Clock.Day);
        }

        return result;
    }

    public ActionResult CollectProcessedItem() =>
        CollectProcessedItem(ProcessorCatalog.MainMachineId);

    public ActionResult CollectProcessedItem(string machineId)
    {
        var result = Processor.Collect(machineId, Inventory);
        if (result.Succeeded && result.GrantedItemId is not null)
        {
            GleamriseSeason.RecordProcessorCollected(
                result.GrantedItemId,
                result.GrantedItemCount,
                Clock.Day
            );
        }

        return result;
    }

    public ActionResult CollectAllProcessedItems()
    {
        var readyOutputs = Processor.Machines.Values
            .Where(machine => machine.IsReady)
            .Select(machine =>
                DataCatalog.ProcessorRecipe(machine.ActiveRecipeId)
            )
            .ToArray();
        var result = Processor.CollectAllReady(Inventory);
        if (!result.Succeeded)
        {
            return result;
        }

        foreach (var recipe in readyOutputs)
        {
            GleamriseSeason.RecordProcessorCollected(
                recipe.OutputItemId,
                recipe.OutputCount,
                Clock.Day
            );
        }

        return result;
    }

    public TargetPreview PreviewProcessorMachine(string machineId)
    {
        if (!ProcessorCatalog.Machines.TryGetValue(machineId, out var definition))
        {
            return TargetPreview.Neutral(PlayerCell);
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return TargetPreview.NeedsTool(
                definition.Position,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        var machine = Processor.Machine(machineId);
        if (machine.IsReady)
        {
            var recipe = DataCatalog.ProcessorRecipe(machine.ActiveRecipeId);
            if (!Inventory.CanAdd(recipe.OutputItemId, recipe.OutputCount))
            {
                return TargetPreview.Blocked(
                    definition.Position,
                    TargetPreviewKind.Station,
                    "notice.inventory_full"
                );
            }

            return TargetPreview.Available(
                definition.Position,
                TargetPreviewKind.Station,
                "target.action.open_processor_ready"
            );
        }

        if (!machine.IsIdle)
        {
            return TargetPreview.Blocked(
                definition.Position,
                TargetPreviewKind.Station,
                "processor.busy"
            );
        }

        var hasIngredients = definition.RecipeIds.Any(recipeId =>
        {
            var recipe = DataCatalog.ProcessorRecipe(recipeId);
            return Inventory.CountFamily(recipe.InputItemId) >= recipe.InputCount;
        });
        if (!hasIngredients)
        {
            return TargetPreview.Blocked(
                definition.Position,
                TargetPreviewKind.Station,
                "processor.missing_ingredients"
            );
        }

        return TargetPreview.Available(
            definition.Position,
            TargetPreviewKind.Station,
            "target.action.open_processor"
        );
    }

    public ActionResult CraftItem(string recipeId) =>
        Crafting.Craft(recipeId, Inventory);

    public ActionResult StoreInChest(GridPosition position, string itemId) =>
        Storage.StoreOne(position, itemId, Inventory);

    public ActionResult TakeFromChest(GridPosition position, string itemId) =>
        Storage.TakeOne(position, itemId, Inventory);

    public ActionResult AcceptDailyCommission() =>
        Commission.Accept();

    public DailyCommissionClaimResult ClaimDailyCommission()
    {
        var result = Commission.Claim(Inventory);
        if (!result.Succeeded)
        {
            return result;
        }

        Coins += result.RewardCoins;
        Changed?.Invoke();
        return result;
    }

    public ActionResult AcceptWeeklyCommission() =>
        WeeklyCommission.Accept();

    public ActionResult AdvanceWeeklyCommissionStage() =>
        WeeklyCommission.AdvanceStage(Inventory);

    public WeeklyCommissionClaimResult ClaimWeeklyCommission()
    {
        var result = WeeklyCommission.Claim(Inventory);
        if (!result.Succeeded)
        {
            return result;
        }

        Coins += result.RewardCoins;
        Changed?.Invoke();
        return result;
    }

    public ActionResult ReadMail(string mailId) => Mail.Read(mailId);

    public ActionResult ClaimMailAttachment(string mailId) =>
        Mail.ClaimAttachment(mailId, Inventory);

    public StarlightContributionResult ContributeToStarlightNode(
        string nodeId
    ) => ContributeToStarlightNode(
        DataCatalog.WoodlandStarlightId,
        nodeId
    );

    public StarlightContributionResult ContributeToStarlightNode(
        string pedestalId,
        string nodeId
    )
    {
        var result = Starlight.Contribute(
            pedestalId,
            nodeId,
            Inventory,
            StarlightProgress()
        );
        if (!result.Succeeded)
        {
            return result;
        }

        if (result.Activated &&
            pedestalId == DataCatalog.WoodlandStarlightId)
        {
            LastRespawnedResources += Resources.ResolveDay(
                Clock.Day,
                Starlight.WoodlandRenewalUnlocked
            );
        }

        Changed?.Invoke();
        return result;
    }

    public int StarlightNodeProgress(
        string pedestalId,
        string nodeId
    ) => Starlight.Progress(
        pedestalId,
        nodeId,
        StarlightProgress()
    );

    public bool IsStarlightNodeComplete(
        string pedestalId,
        string nodeId
    ) => Starlight.IsNodeComplete(
        pedestalId,
        nodeId,
        StarlightProgress()
    );

    public int CompletedStarlightNodeCount(string pedestalId) =>
        Starlight.CompletedNodeCountFor(
            pedestalId,
            StarlightProgress()
        );

    public bool CanContributeToStarlightNode(
        string pedestalId,
        string nodeId
    ) => Starlight.CanContribute(
        pedestalId,
        nodeId,
        Inventory,
        StarlightProgress()
    );

    public ShippingSettlement EndDay()
    {
        var endedDay = Clock.Day;
        var completedAnimalBuildings = AnimalCatalog.Buildings
            .Where(building => Construction.IsCompletedFor(
                building.ConstructionProjectId
            ))
            .ToArray();
        var grazingByBuilding = completedAnimalBuildings.ToDictionary(
            building => building.Id,
            building => GrazingInstanceIdsFor(building.Id),
            StringComparer.Ordinal
        );
        var livestockAutomationWasCompleted =
            LivestockAutomationUnlocked;
        FarmObjects.ApplySprinklers(
            Farm,
            Starlight.HomesteadIrrigationUnlocked
        );
        Farm.EndDay(Weather.CurrentId);
        GreenhouseFarm.EndDay(DataCatalog.ClearWeatherId);
        Orchard.ResolveNight(FarmObjects, BeehivePollinationRange);
        foreach (var building in completedAnimalBuildings)
        {
            if (livestockAutomationWasCompleted)
            {
                Animals.BeginAutomationNight(building.Id, endedDay);
                Animals.ResolveAutomaticFeed(
                    building.Id,
                    endedDay,
                    grazingByBuilding[building.Id]
                );
                Animals.ResolveAutomaticCollection(building.Id);
            }

            Animals.ResolveNight(
                building.Id,
                endedDay,
                grazingByBuilding[building.Id]
            );
            if (livestockAutomationWasCompleted)
            {
                Animals.ResolveAutomaticCollection(building.Id);
            }
        }
        Processor.ResolveNight();
        var completedToolUpgrade = ToolProgression.ResolveNight();
        if (completedToolUpgrade is not null)
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }
        Construction.ResolveNight();
        EnsureCompletedAnimalStarters();
        EnsureCompletedAnimalAutomation();
        NormalizeCottagePlayerPositionForUpgrade();
        NormalizeGreenhousePlayerPosition();
        NormalizeAnimalBuildingPlayerPosition();
        Quest.OnNightResolved(Farm.CountMatureCrop(DataCatalog.StarbudId));
        var settlement = Shipping.Settle(endedDay, SalePrice);
        Coins += settlement.TotalCoins;
        Clock.StartNextDay();
        NormalizeFestivalPlayerPosition();
        Commission.RefreshForDay(Clock.Day);
        WeeklyCommission.RefreshForDay(Clock.Day);
        GleamriseSeason.RefreshForDay(Clock.Day);
        Mail.DeliverForDay(Clock.Day, Village, CharacterEvents);
        LastRespawnedResources = Resources.ResolveDay(
            Clock.Day,
            Starlight.WoodlandRenewalUnlocked
        );
        Weather.AdvanceToDay(Clock.Day);
        Forage.ResolveDay(Clock.Day, Weather.CurrentId);
        CrabPots.ResolveNight(Clock.Day, Weather.CurrentId, Fishing);
        if (Weather.Current.AutoWatersCrops)
        {
            Farm.ApplyWeatherWatering();
        }
        Energy = MaxEnergy;
        EnergyChanged?.Invoke();
        DayEnded?.Invoke();
        Changed?.Invoke();
        return settlement;
    }

    public GameSaveV1 Capture() => new()
    {
        SchemaVersion = SaveService.CurrentSchemaVersion,
        Day = Clock.Day,
        MinuteOfDay = Clock.MinuteOfDay,
        Locale = Locale,
        Player = new PlayerSave
        {
            X = PlayerX,
            Y = PlayerY,
            Energy = Energy,
            WateringCanWater = WateringCanWater,
            SelectedSlot = Inventory.SelectedIndex,
            LocationId = PlayerLocationId,
            InsideCottage = InsideCottage
        },
        Inventory = Inventory.Capture(),
        FarmTiles = Farm.Capture(),
        Greenhouse = new GreenhouseSave
        {
            Tiles = GreenhouseFarm.Capture()
        },
        Animals = Animals.Capture(),
        Quest = Quest.Capture(),
        Coins = Coins,
        Processor = Processor.Capture(),
        Exploration = Exploration.Capture(),
        Resources = Resources.Capture(),
        Mining = CaptureMining(),
        ToolProgression = ToolProgression.Capture(),
        Combat = Combat.Capture(),
        StarfallRuinsTrial = StarfallRuinsTrial.Capture(),
        Forage = Forage.Capture(),
        Fishing = CaptureFishing(),
        Weather = Weather.Capture(),
        Shipping = Shipping.Capture(),
        Kitchen = Kitchen.Capture(),
        Storage = Storage.Capture(),
        FarmObjects = FarmObjects.Capture(),
        Orchard = Orchard.Capture(),
        Commission = Commission.Capture(),
        WeeklyCommission = WeeklyCommission.Capture(),
        Starlight = Starlight.Capture(),
        Village = Village.Capture(),
        Mail = Mail.Capture(),
        CharacterEvents = CharacterEvents.Capture(),
        Construction = Construction.Capture(),
        FarmingSkill = FarmingSkill.Capture(),
        GleamriseSeason = GleamriseSeason.Capture(),
        Festival = Festival.Capture(),
        Collection = Collection.Capture(),
        StarGate = StarGate.Capture()
    };

    private FishingSave CaptureFishing()
    {
        var save = Fishing.Capture();
        FishingProgression.CaptureInto(save);
        CrabPots.CaptureInto(save);
        return save;
    }

    private MiningSave CaptureMining()
    {
        var save = Mining.Capture();
        DeepMine.CaptureInto(save);
        return save;
    }

    public ActionResult ChooseFarmingSpecialization(
        string specializationId
    ) => FarmingSkill.ChooseSpecialization(specializationId);

    private ActionResult UseStarharvestMarketSelected(GridPosition target)
    {
        if (target == StarharvestMarketLayout.ExitCell)
        {
            return TryExitStarharvestMarket(target);
        }

        if (target == StarharvestMarketLayout.ExhibitCell ||
            target == StarharvestMarketLayout.BidBoardCell ||
            target == StarharvestMarketLayout.ShopCell)
        {
            return CheckFestivalStation(target);
        }

        return ActionResult.Fail("notice.nothing_to_interact");
    }

    private ActionResult UseGleamrisePlantingFestivalSelected(
        GridPosition target
    )
    {
        if (target == GleamrisePlantingFestivalLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        if (GleamrisePlantingFestivalLayout.PlotIdsByCell.ContainsKey(target))
        {
            var planted = PlantGleamrisePlot(target);
            return planted.Succeeded
                ? ActionResult.Success(messageKey: planted.MessageKey)
                : ActionResult.Fail(planted.MessageKey);
        }

        var station = FestivalSpatialCatalog.GleamrisePlanting.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private ActionResult UseLongnightLanternFeastSelected(
        GridPosition target
    )
    {
        if (target == LongnightLanternFeastLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private ActionResult UseFireflyTideSelected(GridPosition target)
    {
        if (target == FireflyTideLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private TargetPreview PreviewFestivalEntrance(
        string festivalId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByFestivalId(
                festivalId,
                out var spatial
            ))
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalEntrance(festivalId, target);
        var actionKey = FestivalCatalog.EnterActionKey(festivalId);
        var blockedKey = FestivalCatalog.ClosedKey(festivalId);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                spatial.WorldEntryCell,
                TargetPreviewKind.FestivalPortal,
                actionKey
            );
        }

        if (check.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                spatial.WorldEntryCell,
                TargetPreviewKind.FestivalPortal,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            spatial.WorldEntryCell,
            TargetPreviewKind.FestivalPortal,
            blockedKey
        );
    }

    private TargetPreview PreviewStarharvestMarketEntrance(
        GridPosition target
    ) => PreviewFestivalEntrance(
        FestivalCatalog.StarharvestMarketFestivalId,
        target
    );

    private TargetPreview PreviewStarharvestMarketTarget(GridPosition target)
    {
        if (!StarharvestMarketLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.StarharvestMarket,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == StarharvestMarketLayout.ExitCell)
        {
            var check = CheckStarharvestMarketExit(target);
            return check.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    "target.action.exit_starharvest_market"
                )
                : check.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var kind = target == StarharvestMarketLayout.ExhibitCell
            ? TargetPreviewKind.FestivalExhibit
            : target == StarharvestMarketLayout.BidBoardCell
                ? TargetPreviewKind.FestivalBidBoard
                : target == StarharvestMarketLayout.ShopCell
                    ? TargetPreviewKind.FestivalShop
                    : TargetPreviewKind.None;
        if (kind == TargetPreviewKind.None)
        {
            return TargetPreview.Neutral(target);
        }

        var station = CheckFestivalStation(target);
        if (station.Succeeded)
        {
            var actionKey = kind switch
            {
                TargetPreviewKind.FestivalShop =>
                    "target.action.open_festival_shop",
                TargetPreviewKind.FestivalBidBoard =>
                    "target.action.review_festival_bid",
                _ => "target.action.open_festival_showcase"
            };
            return TargetPreview.Available(target, kind, actionKey);
        }

        if (station.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                target,
                kind,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            target,
            kind,
            station.MessageKey
        );
    }

    private TargetPreview PreviewGleamrisePlantingFestivalTarget(
        GridPosition target
    )
    {
        if (!GleamrisePlantingFestivalLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.GleamrisePlantingFestival,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == GleamrisePlantingFestivalLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    "target.action.exit_gleamrise_festival"
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        if (GleamrisePlantingFestivalLayout.PlotIdsByCell.ContainsKey(target))
        {
            var planting = CheckGleamrisePlot(target);
            if (planting.CanPlant)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    "target.action.plant_festival_seed"
                );
            }

            return planting.FailureKey == "notice.needs_hand"
                ? TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    "target.need.hand"
                )
                : TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    planting.FailureKey
                );
        }

        var station = FestivalSpatialCatalog.GleamrisePlanting.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var actionKey = station.PreviewKind ==
            TargetPreviewKind.FestivalSeedExchange
                ? "target.action.open_sowing_seed_exchange"
                : "target.action.open_sowing_activity";
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    private TargetPreview PreviewLongnightLanternFeastTarget(
        GridPosition target
    )
    {
        if (!LongnightLanternFeastLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.LongnightLanternFeast,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == LongnightLanternFeastLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    FestivalCatalog.ExitActionKey(
                        FestivalCatalog.LongnightLanternFeastFestivalId
                    )
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var completed = Festival.HasParticipated(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            CalendarSystem.YearNumber(Clock.Day)
        );
        var actionKey = station.Id switch
        {
            FestivalCatalog.LongnightLanternStallId =>
                "target.action.open_longnight_stall",
            FestivalCatalog.LongnightGiftExchangeId => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_exchange",
            FestivalCatalog.LongnightStarlightRiteId => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_rite",
            _ => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_feast"
        };
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    private TargetPreview PreviewFireflyTideTarget(GridPosition target)
    {
        if (!FireflyTideLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.FireflyTide,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == FireflyTideLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    FestivalCatalog.ExitActionKey(
                        FestivalCatalog.FireflyTideFestivalId
                    )
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var completed = Festival.HasParticipated(
            FestivalCatalog.FireflyTideFestivalId,
            CalendarSystem.YearNumber(Clock.Day)
        );
        var actionKey = station.Id switch
        {
            FestivalCatalog.FireflyGlowshopId =>
                "target.action.open_firefly_shop",
            FestivalCatalog.FireflyFishBasinId => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_basin",
            FestivalCatalog.FireflyTideAltarId => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_altar",
            _ => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_launch"
        };
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    public IReadOnlyList<AnimalProjection> VisibleAnimalProjections
    {
        get
        {
            var projections = new List<AnimalProjection>();
            foreach (var spatial in AnimalBuildingSpatialCatalog.Definitions)
            {
                var residents = Animals.AnimalsInBuilding(spatial.BuildingId);
                if (residents.Count == 0)
                {
                    continue;
                }

                var outdoor = OutdoorAnimalAssignmentsFor(spatial.BuildingId);
                var physicallyOutside =
                    Clock.MinuteOfDay >= StarfeatherCoopLayout.GrazingStartMinute &&
                    Clock.MinuteOfDay < StarfeatherCoopLayout.GrazingEndMinute;
                if (PlayerLocationId == PlayerLocationIds.World &&
                    physicallyOutside)
                {
                    projections.AddRange(residents
                        .Where(animal => outdoor.ContainsKey(animal.InstanceId))
                        .Select(animal => new AnimalProjection(
                            animal.InstanceId,
                            animal.SpeciesId,
                            animal.BuildingId,
                            PlayerLocationIds.World,
                            outdoor[animal.InstanceId],
                            true
                        )));
                    continue;
                }

                if (PlayerLocationId != spatial.LocationId)
                {
                    continue;
                }

                var indoors = residents
                    .Where(animal =>
                        !physicallyOutside ||
                        !outdoor.ContainsKey(animal.InstanceId)
                    )
                    .Zip(
                        spatial.IndoorAnimalCells,
                        (animal, cell) => new AnimalProjection(
                            animal.InstanceId,
                            animal.SpeciesId,
                            animal.BuildingId,
                            spatial.LocationId,
                            cell,
                            false
                        )
                    );
                projections.AddRange(indoors);
            }

            return projections;
        }
    }

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
            FarmingSkill.RecordSuccessfulAction(
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
            FarmingSkill.RecordSuccessfulAction(
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
            FarmingSkill.RecordSuccessfulAction(
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
                    FarmingSkill.WateringEnergyCost
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
            FarmingSkill.RecordSuccessfulAction(successfulAction);
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
        FarmingSkill.RecordSuccessfulAction(FarmingSkillAction.Harvest);
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

            return Energy < FarmingSkill.WateringEnergyCost
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

    private TargetPreview PreviewCottageTarget(GridPosition target)
    {
        if (CottageLayout.IsBedArea(target))
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.Bed,
                "target.action.rest"
            );
        }

        if (target == CottageLayout.DoorCell)
        {
            return TargetPreview.Available(
                CottageLayout.DoorCell,
                TargetPreviewKind.Door,
                "target.action.exit"
            );
        }

        if (CottageUpgradeLevel == 2 &&
            CottageLayout.IsKitchenStationArea(target))
        {
            var stationCheck = CheckKitchenStation(target);
            return stationCheck.Succeeded
                ? TargetPreview.Available(
                    CottageLayout.KitchenStationCell,
                    TargetPreviewKind.KitchenStation,
                    "target.action.open_kitchen"
                )
                : TargetPreview.NeedsTool(
                    CottageLayout.KitchenStationCell,
                    TargetPreviewKind.KitchenStation,
                    "target.need.hand"
                );
        }

        if (CottageUpgradeLevel == 2 &&
            CottageLayout.IsIngredientPantryArea(target))
        {
            var pantryCheck = CheckIngredientPantry(target);
            return pantryCheck.Succeeded
                ? TargetPreview.Available(
                    CottageLayout.IngredientPantryCell,
                    TargetPreviewKind.IngredientPantry,
                    "target.action.open_ingredient_pantry"
                )
                : TargetPreview.NeedsTool(
                    CottageLayout.IngredientPantryCell,
                    TargetPreviewKind.IngredientPantry,
                    "target.need.hand"
                );
        }

        if (CottageUpgradeLevel != 1 ||
            !CottageLayout.IsKitchenReserveArea(target))
        {
            return TargetPreview.Neutral(target);
        }

        var result = CheckKitchenReserve(target);
        if (result.Succeeded)
        {
            return TargetPreview.Available(
                CottageLayout.KitchenReserveCell,
                TargetPreviewKind.KitchenReserve,
                "target.action.inspect_kitchen_reserve"
            );
        }

        return TargetPreview.NeedsTool(
            CottageLayout.KitchenReserveCell,
            TargetPreviewKind.KitchenReserve,
            "target.need.hand"
        );
    }

    private ActionResult CheckKitchenReserve(GridPosition target)
    {
        if (!InsideCottage ||
            CottageUpgradeLevel != 1 ||
            !CottageLayout.IsKitchenReserveArea(target) ||
            Math.Abs(PlayerCell.X - target.X) +
                Math.Abs(PlayerCell.Y - target.Y) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: "construction.kitchen_reserve.dialogue"
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    private ActionResult CheckCottageFacility(
        GridPosition target,
        Func<GridPosition, bool> isTargetArea,
        Func<GridPosition, bool> isPlayerAdjacent,
        string successMessageKey
    )
    {
        if (!InsideCottage ||
            CottageUpgradeLevel != 2 ||
            !isTargetArea(target) ||
            !isPlayerAdjacent(PlayerCell))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(messageKey: successMessageKey)
            : ActionResult.Fail("notice.needs_hand");
    }

    private void NormalizeCottagePlayerPositionForUpgrade()
    {
        if (!InsideCottage ||
            !Construction.IsCompletedFor(
                ConstructionCatalog.CottageFirstUpgradeId
            ) ||
            !CottageLayout.IsKitchenReserveArea(PlayerCell))
        {
            return;
        }

        PlayerX = CottageLayout.SafeArrivalCell.X * 16 + 8;
        PlayerY = CottageLayout.SafeArrivalCell.Y * 16 + 8;
    }

    private void NormalizeGreenhousePlayerPosition()
    {
        if (!InsideGreenhouse)
        {
            return;
        }

        if (!Construction.IsCompletedFor(
                ConstructionCatalog.HomesteadGreenhouseProjectId
            ))
        {
            PlayerLocationId = PlayerLocationIds.World;
            PlayerX = FarmLayout.GreenhouseReturnCell.X * 16 + 8;
            PlayerY = FarmLayout.GreenhouseReturnCell.Y * 16 + 8;
            return;
        }

        if (GreenhouseLayout.IsWalkable(PlayerCell))
        {
            return;
        }

        PlayerX = GreenhouseLayout.SafeArrivalCell.X * 16 + 8;
        PlayerY = GreenhouseLayout.SafeArrivalCell.Y * 16 + 8;
    }

    private void EnsureCompletedAnimalStarters()
    {
        foreach (var building in AnimalCatalog.Buildings.Where(building =>
                     Construction.IsCompletedFor(
                         building.ConstructionProjectId
                     )))
        {
            Animals.EnsureStarter(building.Id);
        }
    }

    private void EnsureCompletedAnimalAutomation()
    {
        if (!LivestockAutomationUnlocked)
        {
            return;
        }

        foreach (var building in AnimalCatalog.Buildings)
        {
            Animals.EnsureAutomation(building.Id);
        }
    }

    private void NormalizeAnimalBuildingPlayerPosition()
    {
        if (!AnimalBuildingSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ))
        {
            return;
        }

        var building = AnimalCatalog.Building(spatial.BuildingId);
        if (!Construction.IsCompletedFor(building.ConstructionProjectId))
        {
            PlayerLocationId = PlayerLocationIds.World;
            PlayerX = spatial.WorldReturnCell.X * 16 + 8;
            PlayerY = spatial.WorldReturnCell.Y * 16 + 8;
            return;
        }

        if (spatial.IsInteriorWalkable(PlayerCell))
        {
            return;
        }

        PlayerX = spatial.SafeArrivalCell.X * 16 + 8;
        PlayerY = spatial.SafeArrivalCell.Y * 16 + 8;
    }

    private void NormalizeFestivalPlayerPosition()
    {
        ResolveFestivalAttemptsForCurrentTime();
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ))
        {
            return;
        }

        if (!FestivalCatalog.IsOpen(
                spatial.FestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            PlayerLocationId = PlayerLocationIds.World;
            PlayerX = spatial.WorldReturnCell.X * 16 + 8;
            PlayerY = spatial.WorldReturnCell.Y * 16 + 8;
            return;
        }

        if (spatial.IsWalkable(PlayerCell))
        {
            return;
        }

        PlayerX = spatial.SafeArrivalCell.X * 16 + 8;
        PlayerY = spatial.SafeArrivalCell.Y * 16 + 8;
    }

    private void NormalizeCrystalGrottoSurveyPlayerPosition()
    {
        if (!InsideCrystalGrottoSurvey)
        {
            return;
        }

        if (!CrystalGrottoSurveyLayout.IsWalkable(PlayerCell))
        {
            PlayerX = CrystalGrottoSurveyLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = CrystalGrottoSurveyLayout.SafeArrivalCell.Y * 16 + 8;
        }

        if (Mining.ReachRoom(
                Math.Min(
                    4,
                    CrystalGrottoSurveyLayout.RoomNumberAt(PlayerCell)
                )
            ))
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }
    }

    private void NormalizeStarfallRuinsTrialPlayerPosition()
    {
        if (!InsideStarfallRuinsTrial)
        {
            return;
        }

        if (!Starlight.CrystalRuinsPassageUnlocked)
        {
            PlayerLocationId = PlayerLocationIds.World;
            PlayerX = StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8;
            PlayerY = StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8;
            return;
        }

        if (!StarfallRuinsTrial.IsCellAccessible(PlayerCell))
        {
            PlayerX = StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8;
            PlayerY = StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8;
        }
    }

    private void ResolveFestivalAttemptsForCurrentTime()
    {
        var year = CalendarSystem.YearNumber(Clock.Day);
        var attempt = Festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        if (attempt is null)
        {
            return;
        }

        var force = !FestivalCatalog.OccursOnDay(
            FestivalCatalog.GleamrisePlantingFestivalId,
            Clock.Day
        ) || Clock.MinuteOfDay >=
            FestivalCatalog.GleamrisePlanting.CloseMinute;
        var resolution = Festival.ResolvePlantingAttempt(
            year,
            Clock.MinuteOfDay,
            force
        );
        if (resolution.Completed)
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }
    }

    private StarlightProgressContext StarlightProgress(
        bool includeLivePedestals = true
    )
    {
        var milestones = Mining.CompletedMilestoneIds()
            .Concat(ToolProgression.CompletedMilestoneIds())
            .Concat(StarfallRuinsTrial.CompletedMilestoneIds())
            .Concat(Collection.DonatedEntryIds)
            .Concat(Collection.DiscoveredEntryIds.Where(entryId =>
                CompendiumCatalog.Entries.TryGetValue(entryId, out var entry) &&
                entry.CategoryId == CollectionCategoryIds.Enemies
            ))
            .ToHashSet(StringComparer.Ordinal);
        if (Village.Relationship(VillageCatalog.KaelId).Points >= 60)
        {
            milestones.Add(DataCatalog.KaelTrustedRelationshipMilestoneId);
        }
        if (Village.Relationship(VillageCatalog.LioraId).Points >= 60)
        {
            milestones.Add(DataCatalog.LioraTrustedRelationshipMilestoneId);
        }

        var completedPedestals = includeLivePedestals
            ? DataCatalog.StarlightPedestals.Keys
                .Where(Starlight.IsRewardUnlocked)
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
        return new StarlightProgressContext(
            Festival.Results
                .Select(result => result.FestivalId)
                .Where(FestivalCatalog.Festivals.ContainsKey)
                .ToHashSet(StringComparer.Ordinal),
            milestones,
            completedPedestals
        );
    }

    private TargetPreview PreviewArchiveTarget(GridPosition target)
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.MoonlitArchive,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (VillageCatalog.IsMoonlitArchiveDeskCell(target))
        {
            var deskCheck = CheckMoonlitArchiveCompendium(target);
            return deskCheck.Succeeded
                ? TargetPreview.Available(
                    VillageCatalog.MoonlitArchiveDeskCell,
                    TargetPreviewKind.ArchiveResearchDesk,
                    "target.action.open_crop_codex"
                )
                : deskCheck.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        VillageCatalog.MoonlitArchiveDeskCell,
                        TargetPreviewKind.ArchiveResearchDesk,
                        "target.need.hand"
                    )
                    : TargetPreview.Blocked(
                        VillageCatalog.MoonlitArchiveDeskCell,
                        TargetPreviewKind.ArchiveResearchDesk,
                        deskCheck.MessageKey
                    );
        }

        if (target == VillageCatalog.MoonlitArchiveExitCell)
        {
            return selectedId == DataCatalog.HandId
                ? TargetPreview.Available(
                    VillageCatalog.MoonlitArchiveExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_archive"
                )
                : TargetPreview.NeedsTool(
                    VillageCatalog.MoonlitArchiveExitCell,
                    TargetPreviewKind.Door,
                    "target.need.hand"
                );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewWorkshopTarget(GridPosition target)
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.MoonstoneWorkshop,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (target == VillageCatalog.MoonRuneWorkbenchCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.MoonRuneWorkbenchCell,
                    TargetPreviewKind.Station,
                    "target.action.open_construction"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.MoonRuneWorkbenchCell,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        if (target == VillageCatalog.MoonstoneWorkshopExitCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.MoonstoneWorkshopExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_workshop"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.MoonstoneWorkshopExitCell,
                TargetPreviewKind.Door,
                "target.need.hand"
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewTeaHouseTarget(GridPosition target)
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.StarweaverTeaHouse,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (target == VillageCatalog.StarwovenTeaCounterCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.StarwovenTeaCounterCell,
                    TargetPreviewKind.Station,
                    "target.action.inspect_tea_counter"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.StarwovenTeaCounterCell,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        if (target == VillageCatalog.StarweaverTeaHouseExitCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.StarweaverTeaHouseExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_tea_house"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.StarweaverTeaHouseExitCell,
                TargetPreviewKind.Door,
                "target.need.hand"
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewTwilightEmporiumTarget(
        GridPosition target
    )
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.TwilightEmporium,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (target == VillageCatalog.TravelManifestCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                var access = VillageCatalog.TwilightEmporiumAccess(
                    Clock.Day,
                    Clock.MinuteOfDay
                );
                if (!access.IsOpen)
                {
                    return TargetPreview.Blocked(
                        VillageCatalog.TravelManifestCell,
                        TargetPreviewKind.Station,
                        access.TargetStatusKey
                    );
                }

                return TargetPreview.Available(
                    VillageCatalog.TravelManifestCell,
                    TargetPreviewKind.Station,
                    "target.action.inspect_manifest"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.TravelManifestCell,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        if (target == VillageCatalog.TwilightEmporiumExitCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.TwilightEmporiumExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_emporium"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.TwilightEmporiumExitCell,
                TargetPreviewKind.Door,
                "target.need.hand"
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewStarlightPostTarget(
        GridPosition target
    )
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.StarlightPost,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (target == VillageCatalog.RouteSortingCounterCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.RouteSortingCounterCell,
                    TargetPreviewKind.Station,
                    "target.action.inspect_sorting_counter"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.RouteSortingCounterCell,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        if (target == VillageCatalog.StarlightPostExitCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.StarlightPostExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_starlight_post"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.StarlightPostExitCell,
                TargetPreviewKind.Door,
                "target.need.hand"
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewStarfallWatchTarget(
        GridPosition target
    )
    {
        var selectedId = Inventory.Selected.IsEmpty
            ? string.Empty
            : Inventory.Selected.ItemId;
        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.StarfallWatch,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(villager, selectedId);
        }

        if (target == VillageCatalog.SealRouteTableCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.SealRouteTableCell,
                    TargetPreviewKind.Station,
                    "target.action.inspect_seal_route_table"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.SealRouteTableCell,
                TargetPreviewKind.Station,
                "target.need.hand"
            );
        }

        if (target == VillageCatalog.StarfallWatchExitCell)
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    VillageCatalog.StarfallWatchExitCell,
                    TargetPreviewKind.Door,
                    "target.action.exit_starfall_watch"
                );
            }

            return TargetPreview.NeedsTool(
                VillageCatalog.StarfallWatchExitCell,
                TargetPreviewKind.Door,
                "target.need.hand"
            );
        }

        return TargetPreview.Neutral(target);
    }

    private TargetPreview PreviewVillagerInteraction(
        VillageNpcState villager,
        string selectedItemId
    )
    {
        var check = Village.CheckInteraction(
            villager.Position,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            selectedItemId,
            PlayerCell
        );
        if (check.IsAvailable)
        {
            return TargetPreview.Available(
                villager.Position,
                TargetPreviewKind.Character,
                check.IsGift
                    ? "target.action.gift"
                    : "target.action.talk"
            );
        }

        if (check.FailureKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                villager.Position,
                TargetPreviewKind.Character,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            villager.Position,
            TargetPreviewKind.Character,
            check.FailureKey
        );
    }

    private void ApplyCurrentWeatherTo(GridPosition target)
    {
        if (Weather.Current.AutoWatersCrops)
        {
            Farm.ApplyWeatherWatering(target);
        }
    }

    private bool IsPlacementOccupiedByFarmObjectOrOrchard(
        GridPosition position
    ) =>
        FarmObjects.HasObject(position) ||
        Orchard.BlocksMovement(position);

    private TargetPreview PreviewFarmObjectPlacement(
        string itemId,
        int count,
        GridPosition target
    )
    {
        var kind = PreviewKindForFarmObject(itemId);
        var issue = FarmObjects.CheckPlacement(
            itemId,
            target,
            Farm,
            Storage,
            Orchard.BlocksMovement
        );
        if (issue == FarmObjectPlacementIssue.None && count > 0)
        {
            return TargetPreview.Available(
                target,
                kind,
                ActionKeyForFarmObject(itemId)
            );
        }

        if (issue == FarmObjectPlacementIssue.None)
        {
            return TargetPreview.Blocked(
                target,
                kind,
                "target.blocked.no_placeable_item"
            );
        }

        var labelKey = issue switch
        {
            FarmObjectPlacementIssue.NotHome => "target.blocked.place_home",
            FarmObjectPlacementIssue.WrongSurface =>
                itemId == DataCatalog.DewfallSprinklerId
                    ? "target.blocked.sprinkler_bed"
                    : "target.blocked.place_ground",
            FarmObjectPlacementIssue.Occupied => "target.blocked.place_occupied",
            _ => "target.blocked.place_clear"
        };
        return TargetPreview.Blocked(target, kind, labelKey);
    }

    private static TargetPreviewKind PreviewKindForFarmObject(string itemId) =>
        DataCatalog.FarmObject(itemId).Kind switch
        {
            FarmObjectKind.Path => TargetPreviewKind.Path,
            FarmObjectKind.Fence => TargetPreviewKind.Fence,
            FarmObjectKind.Torch => TargetPreviewKind.Torch,
            FarmObjectKind.Sprinkler => TargetPreviewKind.Sprinkler,
            FarmObjectKind.Beehive => TargetPreviewKind.Beehive,
            _ => TargetPreviewKind.Ground
        };

    private static string ActionKeyForFarmObject(string itemId) =>
        DataCatalog.FarmObject(itemId).Kind switch
        {
            FarmObjectKind.Path => "target.action.place_path",
            FarmObjectKind.Fence => "target.action.place_fence",
            FarmObjectKind.Torch => "target.action.place_torch",
            FarmObjectKind.Sprinkler => "target.action.place_sprinkler",
            FarmObjectKind.Beehive => "target.action.place_hive",
            _ => "target.action.place"
        };

    private static FestivalLongnightPreview InvalidLongnightPreview(
        string failureKey,
        IReadOnlyList<string>? dishItemIds
    ) => new(
        false,
        failureKey,
        dishItemIds?.ToArray() ?? [],
        null,
        0,
        string.Empty,
        0
    );

    private void BeginChangedBatch()
    {
        _suppressChanged = true;
        _changedWhileSuppressed = false;
    }

    private void EndChangedBatch()
    {
        _suppressChanged = false;
        if (_changedWhileSuppressed)
        {
            _changedWhileSuppressed = false;
            Changed?.Invoke();
        }
    }

    private void NotifyChanged()
    {
        if (_suppressChanged)
        {
            _changedWhileSuppressed = true;
            return;
        }

        Changed?.Invoke();
    }

    private void OnInventoryChanged()
    {
        if (!Collection.ObserveInventory(Inventory))
        {
            NotifyChanged();
        }
    }
}
