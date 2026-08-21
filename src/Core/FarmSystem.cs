namespace Luminfield.Core;

public sealed class FarmSystem
{
    public const int MapWidth = 48;
    public const int MapHeight = 32;

    private readonly Dictionary<GridPosition, FarmTileState> _tiles = [];

    public FarmSystem() : this(CultivationZoneCatalog.OutdoorFarm)
    {
    }

    public FarmSystem(CultivationZoneDefinition zone)
    {
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));
    }

    public IReadOnlyDictionary<GridPosition, FarmTileState> Tiles => _tiles;
    public CultivationZoneDefinition Zone { get; }

    public event Action<GridPosition>? TileChanged;

    public void Reset()
    {
        _tiles.Clear();
    }

    public void Restore(IEnumerable<FarmTileState>? tiles)
    {
        _tiles.Clear();
        if (tiles is not null)
        {
            foreach (var tile in tiles)
            {
                if (Zone.Id == CultivationZoneCatalog.GreenhouseId &&
                    !Zone.IsPlantingCell(tile.Position))
                {
                    continue;
                }

                _tiles[tile.Position] = tile.Clone();
            }
        }
    }

    public static bool IsPlantingBed(GridPosition position) =>
        CultivationZoneCatalog.OutdoorFarm.IsPlantingCell(position);

    public bool IsTillable(GridPosition position) =>
        Zone.IsPlantingCell(position) &&
        (Zone.Id != CultivationZoneCatalog.OutdoorFarmId ||
            !IsReserved(position));

    public bool IsReserved(GridPosition position)
    {
        var cottage = position.X is >= 2 and <= 18 && position.Y is >= 3 and <= 10;
        var greenhouse = position.X is >= 34 and <= 41 && position.Y is >= 3 and <= 10;
        var pond = position.X is >= 37 and <= 47 && position.Y is >= 20 and <= 31;
        var mira = position == new GridPosition(32, 9);
        return cottage || greenhouse || pond || mira;
    }

    public ActionResult TryTill(GridPosition position, int availableEnergy)
    {
        if (!IsTillable(position))
        {
            return ActionResult.Fail("notice.not_tillable");
        }

        if (_tiles.TryGetValue(position, out var existing) && existing.Tilled)
        {
            return ActionResult.Fail("notice.already_tilled");
        }

        if (availableEnergy < 2)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        _tiles[position] = new FarmTileState
        {
            X = position.X,
            Y = position.Y,
            Tilled = true,
            QualityRoll = -1
        };
        TileChanged?.Invoke(position);
        return ActionResult.Success(2);
    }

    public ActionResult TryFertilize(
        GridPosition position,
        string fertilizerId
    )
    {
        if (!DataCatalog.Items.TryGetValue(
                fertilizerId,
                out var fertilizer
            ) ||
            fertilizer.Kind != ItemKind.Fertilizer)
        {
            return ActionResult.Fail("fertilizer.invalid");
        }

        if (!_tiles.TryGetValue(position, out var tile) || !tile.Tilled)
        {
            return ActionResult.Fail("fertilizer.needs_tilled_soil");
        }

        if (!string.IsNullOrWhiteSpace(tile.CropId))
        {
            return ActionResult.Fail("fertilizer.before_planting");
        }

        if (!string.IsNullOrWhiteSpace(tile.FertilizerId))
        {
            return ActionResult.Fail("fertilizer.already_applied");
        }

        tile.FertilizerId = fertilizer.Id;
        TileChanged?.Invoke(position);
        return ActionResult.Success(messageKey: "fertilizer.applied");
    }

    public ActionResult TryWater(
        GridPosition position,
        int availableEnergy,
        int energyCost = FarmingSkillSystem.BaseWateringEnergyCost
    )
    {
        if (!_tiles.TryGetValue(position, out var tile) || !tile.Tilled)
        {
            return ActionResult.Fail("notice.needs_water");
        }

        if (tile.Watered)
        {
            return ActionResult.Fail("notice.already_watered");
        }

        var normalizedEnergyCost = Math.Max(1, energyCost);
        if (availableEnergy < normalizedEnergyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        tile.Watered = true;
        TileChanged?.Invoke(position);
        return ActionResult.Success(normalizedEnergyCost);
    }

    public bool ApplyWeatherWatering(GridPosition position)
    {
        if (!Zone.ReceivesOutdoorWeather)
        {
            return false;
        }

        return ApplyAutomaticWatering(position);
    }

    public bool ApplyAutomaticWatering(GridPosition position)
    {
        if (!_tiles.TryGetValue(position, out var tile) ||
            !tile.Tilled ||
            tile.Watered)
        {
            return false;
        }

        tile.Watered = true;
        TileChanged?.Invoke(position);
        return true;
    }

    public int ApplyWeatherWatering()
    {
        if (!Zone.ReceivesOutdoorWeather)
        {
            return 0;
        }

        var watered = 0;
        foreach (var position in _tiles.Keys)
        {
            if (ApplyWeatherWatering(position))
            {
                watered++;
            }
        }

        return watered;
    }

    public ActionResult TryPlant(
        GridPosition position,
        string cropId,
        int plantedDay = 1
    )
    {
        if (!_tiles.TryGetValue(position, out var tile) || !tile.Tilled)
        {
            return ActionResult.Fail("notice.needs_tilling");
        }

        if (!string.IsNullOrWhiteSpace(tile.CropId))
        {
            return ActionResult.Fail("notice.already_planted");
        }

        _ = DataCatalog.Crop(cropId);
        var plantingCheck = CheckCropPlanting(cropId, plantedDay);
        if (!plantingCheck.Succeeded)
        {
            return plantingCheck;
        }

        tile.CropId = cropId;
        tile.WateredNights = 0;
        tile.PlantedDay = Math.Max(1, plantedDay);
        tile.ResonanceItemId = null;
        tile.QualityRoll = StableQualityRoll(
            position,
            cropId,
            plantedDay
        );
        TileChanged?.Invoke(position);
        return ActionResult.Success();
    }

    public ActionResult CheckCropPlanting(string cropId, int day)
    {
        if (Zone.IgnoresSeasonRestrictions)
        {
            return ActionResult.Success();
        }

        if (CalendarSystem.SeasonId(day) ==
            CalendarSystem.LongnightSeasonId)
        {
            return ActionResult.Fail("notice.longnight_outdoor_planting");
        }

        return DataCatalog.Crop(cropId).IsAvailableOnDay(day)
            ? ActionResult.Success()
            : ActionResult.Fail("notice.seed_out_of_season");
    }

    public bool IsCropAvailableForPlanting(string cropId, int day) =>
        CheckCropPlanting(cropId, day).Succeeded;

    public CropQuality HarvestQualityAt(GridPosition position)
    {
        if (!_tiles.TryGetValue(position, out var tile) ||
            string.IsNullOrWhiteSpace(tile.CropId) ||
            tile.FertilizerId != DataCatalog.StarsoilFertilizerId)
        {
            return CropQuality.Regular;
        }

        return tile.QualityRoll is >= 0 and < 20
            ? CropQuality.Starlight
            : CropQuality.Luminous;
    }

    public string? HarvestItemIdAt(GridPosition position)
    {
        if (!_tiles.TryGetValue(position, out var tile) ||
            string.IsNullOrWhiteSpace(tile.CropId))
        {
            return null;
        }

        var crop = DataCatalog.Crop(tile.CropId);
        if (crop.AllowsResonanceItem(tile.ResonanceItemId))
        {
            return tile.ResonanceItemId;
        }

        return DataCatalog.ProduceItemId(
            crop.HarvestItemId,
            HarvestQualityAt(position)
        );
    }

    public ActionResult TryHarvest(GridPosition position)
    {
        if (!_tiles.TryGetValue(position, out var tile) || string.IsNullOrWhiteSpace(tile.CropId))
        {
            return ActionResult.Fail("notice.not_ready");
        }

        var definition = DataCatalog.Crop(tile.CropId);
        if (!definition.IsMature(tile.WateredNights))
        {
            return ActionResult.Fail("notice.not_ready");
        }

        var harvestItemId = HarvestItemIdAt(position)
            ?? definition.HarvestItemId;
        tile.Watered = false;
        tile.ResonanceItemId = null;
        if (definition.RegrowthNights > 0)
        {
            tile.WateredNights = definition.RegrowthWateredNights;
        }
        else
        {
            tile.CropId = null;
            tile.FertilizerId = null;
            tile.WateredNights = 0;
            tile.QualityRoll = -1;
            tile.PlantedDay = 0;
        }

        TileChanged?.Invoke(position);
        return new ActionResult(true, 0, string.Empty, harvestItemId, 1);
    }

    public int EndDay(string weatherId = DataCatalog.ClearWeatherId)
    {
        var cultivationWeatherId = Zone.ReceivesOutdoorWeather
            ? weatherId
            : DataCatalog.ClearWeatherId;
        var advanced = 0;
        foreach (var pair in _tiles)
        {
            var tile = pair.Value;
            if (!string.IsNullOrWhiteSpace(tile.CropId) && tile.Watered)
            {
                var crop = DataCatalog.Crop(tile.CropId);
                var wasMature = crop.IsMature(tile.WateredNights);
                tile.WateredNights = Math.Min(
                    crop.MatureAfterWateredNights,
                    tile.WateredNights + 1
                );
                if (!wasMature && crop.IsMature(tile.WateredNights))
                {
                    tile.ResonanceItemId = ResolveResonanceItemId(
                        pair.Key,
                        tile,
                        crop,
                        cultivationWeatherId
                    );
                }

                advanced++;
            }

            if (tile.Watered)
            {
                tile.Watered = false;
            }

            TileChanged?.Invoke(pair.Key);
        }

        return advanced;
    }

    public int CountCrop(string cropId) =>
        _tiles.Values.Count(tile => tile.CropId == cropId);

    public int CountWateredCrop(string cropId) =>
        _tiles.Values.Count(tile => tile.CropId == cropId && tile.Watered);

    public int CountMatureCrop(string cropId)
    {
        var definition = DataCatalog.Crop(cropId);
        return _tiles.Values.Count(tile =>
            tile.CropId == cropId && definition.IsMature(tile.WateredNights)
        );
    }

    public List<FarmTileState> Capture() => _tiles.Values
        .OrderBy(tile => tile.Y)
        .ThenBy(tile => tile.X)
        .Select(tile => tile.Clone())
        .ToList();

    private static int StableQualityRoll(
        GridPosition position,
        string cropId,
        int plantedDay
    )
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in cropId)
            {
                hash = (hash ^ character) * 16777619u;
            }

            hash = (hash ^ (uint)position.X) * 16777619u;
            hash = (hash ^ (uint)position.Y) * 16777619u;
            hash = (hash ^ (uint)Math.Max(1, plantedDay)) * 16777619u;
            return (int)(hash % 100);
        }
    }

    private static string? ResolveResonanceItemId(
        GridPosition position,
        FarmTileState tile,
        CropDefinition crop,
        string weatherId
    )
    {
        if (crop.Resonances is not { Count: > 0 })
        {
            return null;
        }

        foreach (var resonance in crop.Resonances)
        {
            if (resonance.WeatherId != weatherId ||
                resonance.RollModulo <= 0)
            {
                continue;
            }

            var roll = StableResonanceRoll(
                position,
                crop.Id,
                weatherId,
                tile.PlantedDay,
                resonance.RollModulo
            );
            if (roll == resonance.RollResidue)
            {
                return resonance.ItemId;
            }
        }

        return null;
    }

    private static int StableResonanceRoll(
        GridPosition position,
        string cropId,
        string weatherId,
        int plantedDay,
        int modulo
    )
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in cropId)
            {
                hash = (hash ^ character) * 16777619u;
            }

            foreach (var character in weatherId)
            {
                hash = (hash ^ character) * 16777619u;
            }

            hash = (hash ^ (uint)position.X) * 16777619u;
            hash = (hash ^ (uint)position.Y) * 16777619u;
            hash = (hash ^ (uint)Math.Max(1, plantedDay)) * 16777619u;
            return (int)(hash % (uint)modulo);
        }
    }
}
