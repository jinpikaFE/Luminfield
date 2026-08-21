namespace Luminfield.Core;

public enum FarmObjectPlacementIssue
{
    None,
    NotHome,
    WrongSurface,
    Blocked,
    Occupied
}

public sealed class FarmObjectSystem
{
    private static readonly GridPosition[] SprinklerOffsets =
    [
        new(0, -1),
        new(1, 0),
        new(0, 1),
        new(-1, 0)
    ];
    private static readonly GridPosition[] DiagonalSprinklerOffsets =
    [
        new(-1, -1),
        new(1, -1),
        new(1, 1),
        new(-1, 1)
    ];

    private readonly Dictionary<GridPosition, string> _objects = [];

    public IReadOnlyDictionary<GridPosition, string> Objects => _objects;

    public event Action<GridPosition>? Changed;

    public void Reset() => _objects.Clear();

    public bool HasObject(GridPosition position) => _objects.ContainsKey(position);

    public string? ItemAt(GridPosition position) => _objects.GetValueOrDefault(position);

    public bool BlocksMovement(GridPosition position)
    {
        var itemId = ItemAt(position);
        return itemId is not null &&
            DataCatalog.FarmObject(itemId).BlocksMovement;
    }

    public FarmObjectPlacementIssue CheckPlacement(
        string itemId,
        GridPosition position,
        FarmSystem farm,
        StorageSystem storage,
        Func<GridPosition, bool>? extraOccupied = null,
        bool allowAnimalBuildingLegacyCell = false
    )
    {
        if (!DataCatalog.FarmObjects.TryGetValue(itemId, out var definition))
        {
            return FarmObjectPlacementIssue.Blocked;
        }

        if (!WorldDefinition.IsHomeCell(position))
        {
            return FarmObjectPlacementIssue.NotHome;
        }

        if (_objects.ContainsKey(position) ||
            storage.HasChest(position) ||
            extraOccupied?.Invoke(position) == true)
        {
            return FarmObjectPlacementIssue.Occupied;
        }

        if (WorldDefinition.IsBlocked(position) ||
            FarmLayout.IsStaticBlocked(position) ||
            (!allowAnimalBuildingLegacyCell &&
             FarmLayout.IsAnimalBuildingProtectedCell(position)) ||
            farm.IsReserved(position) ||
            farm.Tiles.ContainsKey(position))
        {
            return FarmObjectPlacementIssue.Blocked;
        }

        var isPlantingBed = FarmSystem.IsPlantingBed(position);
        if (definition.Surface == FarmObjectSurface.PlantingBed)
        {
            return isPlantingBed
                ? FarmObjectPlacementIssue.None
                : FarmObjectPlacementIssue.WrongSurface;
        }

        return isPlantingBed
            ? FarmObjectPlacementIssue.WrongSurface
            : FarmObjectPlacementIssue.None;
    }

    public ActionResult Place(
        string itemId,
        GridPosition position,
        FarmSystem farm,
        StorageSystem storage,
        Inventory inventory,
        Func<GridPosition, bool>? extraOccupied = null
    )
    {
        var issue = CheckPlacement(
            itemId,
            position,
            farm,
            storage,
            extraOccupied
        );
        if (issue != FarmObjectPlacementIssue.None)
        {
            return ActionResult.Fail(MessageForIssue(issue));
        }

        if (!inventory.Remove(itemId, 1))
        {
            return ActionResult.Fail("notice.no_placeable_item");
        }

        _objects[position] = itemId;
        Changed?.Invoke(position);
        return ActionResult.Success(messageKey: "notice.placeable_placed");
    }

    public int ApplySprinklers(
        FarmSystem farm,
        bool includeDiagonals = false
    )
    {
        var watered = 0;
        foreach (var pair in _objects)
        {
            if (pair.Value != DataCatalog.DewfallSprinklerId)
            {
                continue;
            }

            foreach (var offset in SprinklerOffsets)
            {
                var target = new GridPosition(
                    pair.Key.X + offset.X,
                    pair.Key.Y + offset.Y
                );
                if (farm.ApplyAutomaticWatering(target))
                {
                    watered++;
                }
            }

            if (!includeDiagonals)
            {
                continue;
            }

            foreach (var offset in DiagonalSprinklerOffsets)
            {
                var target = new GridPosition(
                    pair.Key.X + offset.X,
                    pair.Key.Y + offset.Y
                );
                if (farm.ApplyAutomaticWatering(target))
                {
                    watered++;
                }
            }
        }

        return watered;
    }

    public void Restore(
        FarmObjectSave? save,
        FarmSystem farm,
        StorageSystem storage
    )
    {
        _objects.Clear();
        foreach (var entry in save?.Objects ?? [])
        {
            var position = new GridPosition(entry.X, entry.Y);
            if (CheckPlacement(
                    entry.ItemId,
                    position,
                    farm,
                    storage,
                    allowAnimalBuildingLegacyCell: true
                ) !=
                FarmObjectPlacementIssue.None)
            {
                continue;
            }

            _objects[position] = entry.ItemId;
        }
    }

    public FarmObjectSave Capture() => new()
    {
        Objects = _objects
            .OrderBy(pair => pair.Key.Y)
            .ThenBy(pair => pair.Key.X)
            .Select(pair => new PlacedFarmObjectSave
            {
                X = pair.Key.X,
                Y = pair.Key.Y,
                ItemId = pair.Value
            })
            .ToList()
    };

    public static string MessageForIssue(FarmObjectPlacementIssue issue) =>
        issue switch
        {
            FarmObjectPlacementIssue.NotHome => "notice.placeable_home_only",
            FarmObjectPlacementIssue.WrongSurface => "notice.placeable_wrong_surface",
            FarmObjectPlacementIssue.Occupied => "notice.placeable_occupied",
            _ => "notice.placeable_blocked"
        };
}
