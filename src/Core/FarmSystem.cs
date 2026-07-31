namespace Luminfield.Core;

public sealed class FarmSystem
{
    public const int MapWidth = 48;
    public const int MapHeight = 32;

    private readonly Dictionary<GridPosition, FarmTileState> _tiles = [];

    public IReadOnlyDictionary<GridPosition, FarmTileState> Tiles => _tiles;

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
                _tiles[tile.Position] = tile.Clone();
            }
        }
    }

    public static bool IsPlantingBed(GridPosition position)
    {
        var inTopRow = position.Y is >= 15 and <= 17;
        var inBottomRow = position.Y is >= 19 and <= 21;
        var inLeftBed = position.X is >= 11 and <= 17;
        var inCenterBed = position.X is >= 19 and <= 24;
        var inRightBed = position.X is >= 26 and <= 32;
        return (inTopRow || inBottomRow) && (inLeftBed || inCenterBed || inRightBed);
    }

    public bool IsTillable(GridPosition position) =>
        IsPlantingBed(position) && !IsReserved(position);

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

    public ActionResult TryWater(GridPosition position, int availableEnergy)
    {
        if (!_tiles.TryGetValue(position, out var tile) || !tile.Tilled)
        {
            return ActionResult.Fail("notice.needs_water");
        }

        if (tile.Watered)
        {
            return ActionResult.Fail("notice.already_watered");
        }

        if (availableEnergy < 2)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        tile.Watered = true;
        TileChanged?.Invoke(position);
        return ActionResult.Success(2);
    }

    public bool ApplyWeatherWatering(GridPosition position)
    {
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
        tile.CropId = cropId;
        tile.WateredNights = 0;
        tile.QualityRoll = StableQualityRoll(
            position,
            cropId,
            plantedDay
        );
        TileChanged?.Invoke(position);
        return ActionResult.Success();
    }

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
        tile.CropId = null;
        tile.FertilizerId = null;
        tile.WateredNights = 0;
        tile.QualityRoll = -1;
        TileChanged?.Invoke(position);
        return new ActionResult(true, 0, string.Empty, harvestItemId, 1);
    }

    public int EndDay()
    {
        var advanced = 0;
        foreach (var pair in _tiles)
        {
            var tile = pair.Value;
            if (!string.IsNullOrWhiteSpace(tile.CropId) && tile.Watered)
            {
                tile.WateredNights++;
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
}
