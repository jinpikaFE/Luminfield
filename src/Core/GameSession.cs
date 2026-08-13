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
    public QuestSystem Quest { get; } = new();
    public ProcessorSystem Processor { get; } = new();
    public ExplorationSystem Exploration { get; } = new();
    public WorldResourceSystem Resources { get; } = new();
    public WeatherSystem Weather { get; } = new();
    public ShippingBinSystem Shipping { get; } = new();
    public CraftingSystem Crafting { get; } = new();
    public StorageSystem Storage { get; } = new();
    public FarmObjectSystem FarmObjects { get; } = new();
    public DailyCommissionSystem Commission { get; } = new();
    public WeeklyCommissionSystem WeeklyCommission { get; } = new();
    public StarlightSystem Starlight { get; } = new();
    public VillageSystem Village { get; }
    public MailSystem Mail { get; } = new();
    public CharacterEventSystem CharacterEvents { get; } = new();
    public ConstructionSystem Construction { get; } = new();

    private bool _suppressChanged;
    private bool _changedWhileSuppressed;

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
    public string Locale { get; private set; } = LocaleService.SimplifiedChinese;

    public event Action? Changed;
    public event Action? EnergyChanged;
    public event Action? WaterChanged;
    public event Action? DayEnded;
    public event Action? PlayerMoved;

    public GameSession()
    {
        Village = new VillageSystem(Weather);
        Clock.TimeChanged += NotifyChanged;
        Inventory.Changed += NotifyChanged;
        Farm.TileChanged += _ => NotifyChanged();
        Quest.Changed += NotifyChanged;
        Processor.Changed += NotifyChanged;
        Exploration.Changed += NotifyChanged;
        Resources.Changed += _ => NotifyChanged();
        Weather.Changed += NotifyChanged;
        Shipping.Changed += NotifyChanged;
        Storage.Changed += _ => NotifyChanged();
        FarmObjects.Changed += _ => NotifyChanged();
        Commission.Changed += NotifyChanged;
        WeeklyCommission.Changed += NotifyChanged;
        Starlight.Changed += NotifyChanged;
        Village.Changed += NotifyChanged;
        Mail.Changed += NotifyChanged;
        CharacterEvents.Changed += NotifyChanged;
        Construction.Changed += NotifyChanged;
    }

    public void NewGame(string locale = LocaleService.SimplifiedChinese)
    {
        Clock.Reset();
        Inventory.Reset();
        Farm.Reset();
        Quest.Reset();
        Processor.Reset();
        Exploration.Reset();
        Resources.Reset();
        Weather.Reset(Clock.Day);
        Shipping.Reset();
        Storage.Reset();
        FarmObjects.Reset();
        Commission.Reset(Clock.Day);
        WeeklyCommission.Reset(Clock.Day);
        Starlight.Reset();
        Village.Reset();
        Mail.Reset();
        CharacterEvents.Reset();
        Construction.Reset();
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
        Clock.Reset(save.Day, save.MinuteOfDay);
        Inventory.Restore(save.Inventory, save.Player.SelectedSlot);
        Farm.Restore(save.FarmTiles);
        Storage.Restore(save.Storage, Farm);
        FarmObjects.Restore(save.FarmObjects, Farm, Storage);
        Quest.Restore(save.Quest);
        Processor.Restore(save.Processor);
        Exploration.Restore(save.Exploration);
        Starlight.Restore(save.Starlight);
        Village.Restore(save.Village);
        Mail.Restore(save.Mail);
        CharacterEvents.Restore(save.CharacterEvents, save.Day);
        Construction.Restore(save.Construction);
        Resources.Restore(
            save.Resources,
            save.Day,
            Starlight.WoodlandRenewalUnlocked
        );
        Weather.Restore(save.Weather, save.Day);
        Shipping.Restore(save.Shipping);
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
        Locale = save.Locale;
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
        PlayerX = x;
        PlayerY = y;
        PlayerLocationId = PlayerLocationIds.Normalize(locationId);
        if (PlayerLocationId == PlayerLocationIds.World)
        {
            Exploration.Discover(
                new GridPosition(
                    (int)MathF.Floor(x / 16),
                    (int)MathF.Floor(y / 16)
                )
            );
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

        if (WorldDefinition.IsWoodlandStarlightCell(target))
        {
            if (selected.ItemId != DataCatalog.HandId)
            {
                return ActionResult.Fail("notice.needs_hand");
            }

            return UseHand(WorldDefinition.WoodlandStarlightCell);
        }

        if (FarmObjects.HasObject(target))
        {
            return ActionResult.Fail("notice.placeable_occupied");
        }

        ActionResult result;
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
                result = Farm.TryWater(target, Energy);
                if (result.Succeeded)
                {
                    WateringCanWater--;
                    WaterChanged?.Invoke();
                    Quest.OnWatered(cropId);
                }
                break;
            case DataCatalog.BucketId:
                return RefillWateringCan(target);
            default:
                var item = DataCatalog.Item(selected.ItemId);
                if (item.Kind == ItemKind.Placeable)
                {
                    if (item.Id == DataCatalog.StarwovenChestId)
                    {
                        return Storage.Place(
                            target,
                            Farm,
                            Inventory,
                            FarmObjects.HasObject
                        );
                    }

                    if (DataCatalog.FarmObjects.ContainsKey(item.Id))
                    {
                        return FarmObjects.Place(
                            item.Id,
                            target,
                            Farm,
                            Storage,
                            Inventory
                        );
                    }

                    return ActionResult.Fail("notice.not_ready");
                }

                if (item.Kind == ItemKind.Fertilizer)
                {
                    result = Farm.TryFertilize(target, item.Id);
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
                    ApplyCurrentWeatherTo(target);
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
        }

        if (result.Succeeded && result.EnergyCost > 0)
        {
            Energy = Math.Max(0, Energy - result.EnergyCost);
            EnergyChanged?.Invoke();
            Changed?.Invoke();
        }

        return result;
    }

    public TargetPreview PreviewSelectedTarget(GridPosition target)
    {
        if (!WorldDefinition.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
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
        if (WorldDefinition.IsWoodlandStarlightCell(target))
        {
            if (selectedId == DataCatalog.HandId)
            {
                return TargetPreview.Available(
                    WorldDefinition.WoodlandStarlightCell,
                    TargetPreviewKind.StarlightPedestal,
                    "target.action.open_starlight"
                );
            }

            return TargetPreview.NeedsTool(
                WorldDefinition.WoodlandStarlightCell,
                TargetPreviewKind.StarlightPedestal,
                "target.need.hand"
            );
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
            if (selectedId != DataCatalog.BucketId)
            {
                return TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.Water,
                    "target.need.bucket"
                );
            }

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

        if (selectedId == DataCatalog.StarwovenChestId)
        {
            var issue = Storage.CheckPlacement(
                target,
                Farm,
                FarmObjects.HasObject
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

            return Energy < 2
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

    private ActionResult UseHand(GridPosition target)
    {
        if (WorldDefinition.IsWoodlandStarlightCell(target))
        {
            Starlight.Discover();
            return ActionResult.Success(messageKey: "starlight.opened");
        }

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

    public ActionResult InspectMoonlitArchiveDesk()
    {
        if (!InsideArchive)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return ActionResult.Success(
            messageKey: "archive.desk.dialogue"
        );
    }

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

    public ActionResult StartCottageFirstUpgrade()
    {
        if (!InsideWorkshop)
        {
            return ActionResult.Fail("construction.workshop_only");
        }

        var check = Construction.CheckStart(Inventory, Coins);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            if (!Inventory.TryRemoveMany(Construction.Project.Materials))
            {
                return ActionResult.Fail("construction.materials_changed");
            }

            Coins -= Construction.Project.CoinCost;
            Construction.BeginChecked();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Success(messageKey: "construction.started");
    }

    public ActionResult InspectKitchenReserve()
    {
        var check = CheckKitchenReserve();
        return check.Succeeded
            ? ActionResult.Success(
                messageKey: "construction.kitchen_reserve.dialogue"
            )
            : check;
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

        if (!TwilightEmporiumSystem.IsStocked(Clock.Day, itemId))
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
        if (item.BuyPrice <= 0)
        {
            return ActionResult.Fail("shop.not_for_sale");
        }

        if (Coins < item.BuyPrice)
        {
            return ActionResult.Fail("shop.not_enough_coins");
        }

        if (!Inventory.Add(itemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        Coins -= item.BuyPrice;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: successKey);
    }

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

        Coins += item.SellPrice;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "shop.sold");
    }

    public ActionResult QueueForShipping(string itemId) =>
        Shipping.QueueOne(itemId, Inventory);

    public ActionResult ReclaimFromShipping(string itemId) =>
        Shipping.ReclaimOne(itemId, Inventory);

    public ActionResult StartProcessing(string recipeId) =>
        Processor.Start(recipeId, Inventory);

    public ActionResult CollectProcessedItem() =>
        Processor.Collect(Inventory);

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
    )
    {
        var result = Starlight.Contribute(nodeId, Inventory);
        if (!result.Succeeded)
        {
            return result;
        }

        if (result.Activated)
        {
            LastRespawnedResources += Resources.ResolveDay(
                Clock.Day,
                Starlight.WoodlandRenewalUnlocked
            );
        }

        Changed?.Invoke();
        return result;
    }

    public ShippingSettlement EndDay()
    {
        var endedDay = Clock.Day;
        FarmObjects.ApplySprinklers(Farm);
        Farm.EndDay();
        Processor.ResolveNight();
        Construction.ResolveNight();
        Quest.OnNightResolved(Farm.CountMatureCrop(DataCatalog.StarbudId));
        var settlement = Shipping.Settle(endedDay);
        Coins += settlement.TotalCoins;
        Clock.StartNextDay();
        Commission.RefreshForDay(Clock.Day);
        WeeklyCommission.RefreshForDay(Clock.Day);
        Mail.DeliverForDay(Clock.Day, Village);
        LastRespawnedResources = Resources.ResolveDay(
            Clock.Day,
            Starlight.WoodlandRenewalUnlocked
        );
        Weather.AdvanceToDay(Clock.Day);
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
        Quest = Quest.Capture(),
        Coins = Coins,
        Processor = Processor.Capture(),
        Exploration = Exploration.Capture(),
        Resources = Resources.Capture(),
        Weather = Weather.Capture(),
        Shipping = Shipping.Capture(),
        Storage = Storage.Capture(),
        FarmObjects = FarmObjects.Capture(),
        Commission = Commission.Capture(),
        WeeklyCommission = WeeklyCommission.Capture(),
        Starlight = Starlight.Capture(),
        Village = Village.Capture(),
        Mail = Mail.Capture(),
        CharacterEvents = CharacterEvents.Capture(),
        Construction = Construction.Capture()
    };

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

        if (!Construction.IsCompleted ||
            target != CottageLayout.KitchenReserveCell)
        {
            return TargetPreview.Neutral(target);
        }

        var result = CheckKitchenReserve();
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

    private ActionResult CheckKitchenReserve()
    {
        if (!InsideCottage || !Construction.IsCompleted)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: "construction.kitchen_reserve.dialogue"
            )
            : ActionResult.Fail("notice.needs_hand");
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

        if (target == VillageCatalog.MoonlitArchiveDeskCell)
        {
            return selectedId == DataCatalog.HandId
                ? TargetPreview.Available(
                    VillageCatalog.MoonlitArchiveDeskCell,
                    TargetPreviewKind.Station,
                    "target.action.read_archive"
                )
                : TargetPreview.NeedsTool(
                    VillageCatalog.MoonlitArchiveDeskCell,
                    TargetPreviewKind.Station,
                    "target.need.hand"
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
            Storage
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
            _ => TargetPreviewKind.Ground
        };

    private static string ActionKeyForFarmObject(string itemId) =>
        DataCatalog.FarmObject(itemId).Kind switch
        {
            FarmObjectKind.Path => "target.action.place_path",
            FarmObjectKind.Fence => "target.action.place_fence",
            FarmObjectKind.Torch => "target.action.place_torch",
            FarmObjectKind.Sprinkler => "target.action.place_sprinkler",
            _ => "target.action.place"
        };

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
}
