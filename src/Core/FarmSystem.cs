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
            Tilled = true
        };
        TileChanged?.Invoke(position);
        return ActionResult.Success(2);
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

    public ActionResult TryPlant(GridPosition position, string cropId)
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
        TileChanged?.Invoke(position);
        return ActionResult.Success();
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

        tile.CropId = null;
        tile.WateredNights = 0;
        TileChanged?.Invoke(position);
        return new ActionResult(true, 0, string.Empty, definition.HarvestItemId, 1);
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
}
