namespace Luminfield.Core;

public enum ChestPlacementIssue
{
    None,
    NotHome,
    Blocked,
    Occupied
}

public sealed class StorageChestState
{
    public const int SlotCount = 16;

    private readonly List<InventorySlot> _items =
        Enumerable.Range(0, SlotCount).Select(_ => new InventorySlot()).ToList();

    public StorageChestState(GridPosition position)
    {
        Position = position;
    }

    public GridPosition Position { get; }
    public IReadOnlyList<InventorySlot> Items => _items;
    public int UsedSlots => _items.Count(slot => !slot.IsEmpty);

    public int Count(string itemId) =>
        _items.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Count);

    public bool CanAdd(string itemId, int count)
    {
        if (count <= 0)
        {
            return true;
        }

        var definition = DataCatalog.Item(itemId);
        var capacity = _items
            .Where(slot => slot.IsEmpty || slot.ItemId == itemId)
            .Sum(slot => slot.IsEmpty
                ? definition.MaxStack
                : definition.MaxStack - slot.Count);
        return capacity >= count;
    }

    public bool Add(string itemId, int count)
    {
        if (!CanAdd(itemId, count))
        {
            return false;
        }

        var definition = DataCatalog.Item(itemId);
        var remaining = count;
        foreach (var slot in _items.Where(slot =>
                     slot.ItemId == itemId &&
                     slot.Count < definition.MaxStack))
        {
            var moved = Math.Min(remaining, definition.MaxStack - slot.Count);
            slot.Count += moved;
            remaining -= moved;
            if (remaining == 0)
            {
                return true;
            }
        }

        foreach (var slot in _items.Where(slot => slot.IsEmpty))
        {
            var moved = Math.Min(remaining, definition.MaxStack);
            slot.ItemId = itemId;
            slot.Count = moved;
            remaining -= moved;
            if (remaining == 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool Remove(string itemId, int count)
    {
        if (Count(itemId) < count)
        {
            return false;
        }

        var remaining = count;
        for (var index = _items.Count - 1; index >= 0 && remaining > 0; index--)
        {
            var slot = _items[index];
            if (slot.ItemId != itemId)
            {
                continue;
            }

            var removed = Math.Min(remaining, slot.Count);
            slot.Count -= removed;
            remaining -= removed;
            if (slot.Count == 0)
            {
                slot.ItemId = string.Empty;
            }
        }

        return true;
    }

    public void Restore(IEnumerable<InventorySlot>? items)
    {
        foreach (var slot in _items)
        {
            slot.ItemId = string.Empty;
            slot.Count = 0;
        }

        foreach (var source in items ?? [])
        {
            if (source.Count <= 0 ||
                !DataCatalog.StorableItemIds.Contains(
                    source.ItemId,
                    StringComparer.Ordinal
                ))
            {
                continue;
            }

            _ = Add(source.ItemId, source.Count);
        }
    }

    public List<InventorySlot> Capture() =>
        _items.Where(slot => !slot.IsEmpty).Select(slot => slot.Clone()).ToList();
}

public sealed class StorageSystem
{
    private readonly Dictionary<GridPosition, StorageChestState> _chests = [];

    public IReadOnlyDictionary<GridPosition, StorageChestState> Chests => _chests;

    public event Action<GridPosition>? Changed;

    public void Reset() => _chests.Clear();

    public void Restore(StorageSave? save, FarmSystem farm)
    {
        _chests.Clear();
        foreach (var chestSave in save?.Chests ?? [])
        {
            var position = new GridPosition(chestSave.X, chestSave.Y);
            if (CheckPlacement(position, farm) != ChestPlacementIssue.None)
            {
                continue;
            }

            var chest = new StorageChestState(position);
            chest.Restore(chestSave.Items);
            _chests[position] = chest;
        }
    }

    public bool HasChest(GridPosition position) => _chests.ContainsKey(position);

    public StorageChestState? ChestAt(GridPosition position) =>
        _chests.GetValueOrDefault(position);

    public ChestPlacementIssue CheckPlacement(
        GridPosition position,
        FarmSystem farm,
        Func<GridPosition, bool>? extraOccupied = null
    )
    {
        if (!WorldDefinition.IsHomeCell(position))
        {
            return ChestPlacementIssue.NotHome;
        }

        if (_chests.ContainsKey(position) ||
            extraOccupied?.Invoke(position) == true)
        {
            return ChestPlacementIssue.Occupied;
        }

        if (WorldDefinition.IsBlocked(position) ||
            FarmLayout.IsStaticBlocked(position) ||
            FarmSystem.IsPlantingBed(position) ||
            farm.IsReserved(position) ||
            farm.Tiles.ContainsKey(position))
        {
            return ChestPlacementIssue.Blocked;
        }

        return ChestPlacementIssue.None;
    }

    public ActionResult Place(
        GridPosition position,
        FarmSystem farm,
        Inventory inventory,
        Func<GridPosition, bool>? extraOccupied = null
    )
    {
        var issue = CheckPlacement(position, farm, extraOccupied);
        if (issue != ChestPlacementIssue.None)
        {
            return ActionResult.Fail(issue == ChestPlacementIssue.NotHome
                ? "notice.chest_home_only"
                : "notice.chest_place_blocked");
        }

        if (!inventory.Remove(DataCatalog.StarwovenChestId, 1))
        {
            return ActionResult.Fail("notice.no_chest_item");
        }

        _chests[position] = new StorageChestState(position);
        Changed?.Invoke(position);
        return ActionResult.Success(messageKey: "notice.chest_placed");
    }

    public ActionResult StoreOne(
        GridPosition position,
        string itemId,
        Inventory inventory
    )
    {
        if (!_chests.TryGetValue(position, out var chest))
        {
            return ActionResult.Fail("storage.missing_chest");
        }

        if (!DataCatalog.StorableItemIds.Contains(itemId, StringComparer.Ordinal))
        {
            return ActionResult.Fail("storage.cannot_store");
        }

        if (inventory.Count(itemId) <= 0)
        {
            return ActionResult.Fail("storage.none_in_backpack");
        }

        if (!chest.CanAdd(itemId, 1))
        {
            return ActionResult.Fail("storage.chest_full");
        }

        _ = inventory.Remove(itemId, 1);
        _ = chest.Add(itemId, 1);
        Changed?.Invoke(position);
        return ActionResult.Success(messageKey: "storage.stored");
    }

    public ActionResult TakeOne(
        GridPosition position,
        string itemId,
        Inventory inventory
    )
    {
        if (!_chests.TryGetValue(position, out var chest))
        {
            return ActionResult.Fail("storage.missing_chest");
        }

        if (chest.Count(itemId) <= 0)
        {
            return ActionResult.Fail("storage.none_in_chest");
        }

        if (!inventory.CanAdd(itemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _ = chest.Remove(itemId, 1);
        _ = inventory.Add(itemId, 1);
        Changed?.Invoke(position);
        return ActionResult.Success(messageKey: "storage.taken");
    }

    public StorageSave Capture() => new()
    {
        Chests = _chests.Values
            .OrderBy(chest => chest.Position.Y)
            .ThenBy(chest => chest.Position.X)
            .Select(chest => new PlacedChestSave
            {
                X = chest.Position.X,
                Y = chest.Position.Y,
                Items = chest.Capture()
            })
            .ToList()
    };
}
