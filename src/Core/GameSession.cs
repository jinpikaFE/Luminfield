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

    public int Energy { get; private set; } = MaxEnergy;
    public int WateringCanWater { get; private set; } = MaxWateringCanWater;
    public int Coins { get; private set; } = NewGameCoins;
    public float PlayerX { get; private set; } = NewGamePlayerX;
    public float PlayerY { get; private set; } = NewGamePlayerY;
    public bool InsideCottage { get; private set; }
    public string Locale { get; private set; } = LocaleService.SimplifiedChinese;

    public event Action? Changed;
    public event Action? EnergyChanged;
    public event Action? WaterChanged;
    public event Action? DayEnded;
    public event Action? PlayerMoved;

    public GameSession()
    {
        Clock.TimeChanged += NotifyChanged;
        Inventory.Changed += NotifyChanged;
        Farm.TileChanged += _ => NotifyChanged();
        Quest.Changed += NotifyChanged;
        Processor.Changed += NotifyChanged;
        Exploration.Changed += NotifyChanged;
        Resources.Changed += _ => NotifyChanged();
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
        Energy = MaxEnergy;
        WateringCanWater = MaxWateringCanWater;
        Coins = NewGameCoins;
        PlayerX = NewGamePlayerX;
        PlayerY = NewGamePlayerY;
        InsideCottage = false;
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
        Quest.Restore(save.Quest);
        Processor.Restore(save.Processor);
        Exploration.Restore(save.Exploration);
        Resources.Restore(save.Resources);
        Energy = Math.Clamp(save.Player.Energy, 0, MaxEnergy);
        WateringCanWater = Math.Clamp(
            save.Player.WateringCanWater,
            0,
            MaxWateringCanWater
        );
        Coins = Math.Max(0, save.Coins);
        PlayerX = save.Player.X;
        PlayerY = save.Player.Y;
        InsideCottage = save.Player.InsideCottage;
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
        PlayerX = x;
        PlayerY = y;
        InsideCottage = insideCottage;
        if (!insideCottage)
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
                        Inventory
                    );
                    break;
                }

                result = Farm.TryTill(target, Energy);
                if (result.Succeeded)
                {
                    Quest.OnTilled();
                }
                break;
            case DataCatalog.MacheteId:
                result = Resources.TryGather(
                    target,
                    selected.ItemId,
                    Energy,
                    Inventory
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
            case DataCatalog.StarbudSeedId:
            case DataCatalog.MoonrootSeedId:
                var item = DataCatalog.Item(selected.ItemId);
                if (selected.Count <= 0 || item.CropId is null)
                {
                    return ActionResult.Fail("notice.no_seed");
                }

                result = Farm.TryPlant(target, item.CropId);
                if (result.Succeeded)
                {
                    Inventory.Remove(selected.ItemId, 1);
                    Quest.OnPlanted(item.CropId);
                }
                break;
            default:
                return ActionResult.Fail("notice.not_ready");
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

        var selected = Inventory.Selected;
        var selectedId = selected.IsEmpty ? string.Empty : selected.ItemId;
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

        if (!WorldDefinition.IsHomeCell(target) ||
            !FarmSystem.IsPlantingBed(target))
        {
            return TargetPreview.Neutral(target);
        }

        Farm.Tiles.TryGetValue(target, out var tile);
        if (!string.IsNullOrWhiteSpace(tile?.CropId))
        {
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

                return Inventory.CanAdd(crop.HarvestItemId, 1)
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
            if (selectedId is DataCatalog.StarbudSeedId or DataCatalog.MoonrootSeedId)
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
        var tile = Farm.Tiles.GetValueOrDefault(target);
        if (tile?.CropId is not null)
        {
            var crop = DataCatalog.Crop(tile.CropId);
            if (!crop.IsMature(tile.WateredNights))
            {
                return ActionResult.Fail("notice.not_ready");
            }

            if (!Inventory.CanAdd(crop.HarvestItemId, 1))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            var harvested = Farm.TryHarvest(target);
            if (harvested.Succeeded && harvested.GrantedItemId is not null)
            {
                Inventory.Add(harvested.GrantedItemId, harvested.GrantedItemCount);
                Quest.OnHarvested(harvested.GrantedItemId);
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

    public ActionResult BuyItem(string itemId)
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
        return ActionResult.Success(messageKey: "shop.bought");
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

    public ActionResult StartProcessing(string recipeId) =>
        Processor.Start(recipeId, Inventory);

    public ActionResult CollectProcessedItem() =>
        Processor.Collect(Inventory);

    public void EndDay()
    {
        Farm.EndDay();
        Processor.ResolveNight();
        Quest.OnNightResolved(Farm.CountMatureCrop(DataCatalog.StarbudId));
        Clock.StartNextDay();
        Energy = MaxEnergy;
        EnergyChanged?.Invoke();
        DayEnded?.Invoke();
        Changed?.Invoke();
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
            InsideCottage = InsideCottage
        },
        Inventory = Inventory.Capture(),
        FarmTiles = Farm.Capture(),
        Quest = Quest.Capture(),
        Coins = Coins,
        Processor = Processor.Capture(),
        Exploration = Exploration.Capture(),
        Resources = Resources.Capture()
    };

    private void NotifyChanged() => Changed?.Invoke();
}
