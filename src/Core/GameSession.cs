namespace Luminfield.Core;

public sealed class GameSession
{
    public const int MaxEnergy = 100;
    public const float NewGamePlayerX = 504;
    public const float NewGamePlayerY = 152;

    public GameClock Clock { get; } = new();
    public Inventory Inventory { get; } = new();
    public FarmSystem Farm { get; } = new();
    public QuestSystem Quest { get; } = new();

    public int Energy { get; private set; } = MaxEnergy;
    public float PlayerX { get; private set; } = NewGamePlayerX;
    public float PlayerY { get; private set; } = NewGamePlayerY;
    public bool InsideCottage { get; private set; }
    public string Locale { get; private set; } = LocaleService.SimplifiedChinese;

    public event Action? Changed;
    public event Action? EnergyChanged;
    public event Action? DayEnded;

    public GameSession()
    {
        Clock.TimeChanged += NotifyChanged;
        Inventory.Changed += NotifyChanged;
        Farm.TileChanged += _ => NotifyChanged();
        Quest.Changed += NotifyChanged;
    }

    public void NewGame(string locale = LocaleService.SimplifiedChinese)
    {
        Clock.Reset();
        Inventory.Reset();
        Farm.Reset();
        Quest.Reset();
        Energy = MaxEnergy;
        PlayerX = NewGamePlayerX;
        PlayerY = NewGamePlayerY;
        InsideCottage = false;
        Locale = locale;
        EnergyChanged?.Invoke();
        Changed?.Invoke();
    }

    public void Restore(GameSaveV1 save)
    {
        Clock.Reset(save.Day, save.MinuteOfDay);
        Inventory.Restore(save.Inventory, save.Player.SelectedSlot);
        Farm.Restore(save.FarmTiles);
        Quest.Restore(save.Quest);
        Energy = Math.Clamp(save.Player.Energy, 0, MaxEnergy);
        PlayerX = save.Player.X;
        PlayerY = save.Player.Y;
        InsideCottage = save.Player.InsideCottage;
        Locale = save.Locale;
        EnergyChanged?.Invoke();
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
    }

    public ActionResult UseSelected(GridPosition target)
    {
        var tile = Farm.Tiles.GetValueOrDefault(target);
        if (tile?.CropId is not null)
        {
            var crop = DataCatalog.Crop(tile.CropId);
            if (crop.IsMature(tile.WateredNights))
            {
                var harvested = Farm.TryHarvest(target);
                if (harvested.Succeeded && harvested.GrantedItemId is not null)
                {
                    Inventory.Add(harvested.GrantedItemId, harvested.GrantedItemCount);
                    Quest.OnHarvested(harvested.GrantedItemId);
                }

                return harvested;
            }
        }

        var selected = Inventory.Selected;
        if (selected.IsEmpty)
        {
            return ActionResult.Fail("notice.not_ready");
        }

        ActionResult result;
        switch (selected.ItemId)
        {
            case DataCatalog.HoeId:
                result = Farm.TryTill(target, Energy);
                if (result.Succeeded)
                {
                    Quest.OnTilled();
                }
                break;
            case DataCatalog.WateringCanId:
                var cropId = Farm.Tiles.GetValueOrDefault(target)?.CropId;
                result = Farm.TryWater(target, Energy);
                if (result.Succeeded)
                {
                    Quest.OnWatered(cropId);
                }
                break;
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

    public void EndDay()
    {
        Farm.EndDay();
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
            SelectedSlot = Inventory.SelectedIndex,
            InsideCottage = InsideCottage
        },
        Inventory = Inventory.Capture(),
        FarmTiles = Farm.Capture(),
        Quest = Quest.Capture()
    };

    private void NotifyChanged() => Changed?.Invoke();
}
