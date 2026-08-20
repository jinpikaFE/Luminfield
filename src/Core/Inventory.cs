namespace Luminfield.Core;

public sealed class Inventory
{
    public const int HotbarSlotCount = 8;
    public const int SlotCount = 24;
    public const int StartingToolCount = 6;

    private static readonly string[] StartingTools =
    [
        DataCatalog.HandId,
        DataCatalog.ShovelId,
        DataCatalog.MacheteId,
        DataCatalog.WateringCanId,
        DataCatalog.BucketId,
        DataCatalog.FishingRodId
    ];

    private readonly List<InventorySlot> _slots =
        Enumerable.Range(0, SlotCount).Select(_ => new InventorySlot()).ToList();

    public IReadOnlyList<InventorySlot> Slots => _slots;
    public int SelectedIndex { get; private set; }

    public event Action? Changed;

    public void Reset()
    {
        foreach (var slot in _slots)
        {
            slot.ItemId = string.Empty;
            slot.Count = 0;
        }

        PlaceStartingTools();
        SelectedIndex = 0;
        Changed?.Invoke();
    }

    public void Restore(IEnumerable<InventorySlot>? slots, int selectedIndex)
    {
        var saved = slots?.Select(slot => slot.Clone()).ToList() ?? [];
        var selectedItemId = saved.ElementAtOrDefault(selectedIndex)?.ItemId ?? string.Empty;
        if (selectedItemId == DataCatalog.LegacyHoeId)
        {
            selectedItemId = DataCatalog.ShovelId;
        }

        foreach (var slot in _slots)
        {
            slot.ItemId = string.Empty;
            slot.Count = 0;
        }

        PlaceStartingTools();
        foreach (var source in saved)
        {
            var itemId = source.ItemId == DataCatalog.LegacyHoeId
                ? DataCatalog.ShovelId
                : source.ItemId;
            if (source.Count <= 0 ||
                StartingTools.Contains(itemId, StringComparer.Ordinal) ||
                !DataCatalog.Items.ContainsKey(itemId))
            {
                continue;
            }

            AddWithoutNotification(itemId, source.Count);
        }

        SelectedIndex = FindRestoredSelection(selectedItemId);
        Changed?.Invoke();
    }

    public void Select(int index)
    {
        var next = Math.Clamp(index, 0, HotbarSlotCount - 1);
        if (next == SelectedIndex)
        {
            return;
        }

        SelectedIndex = next;
        Changed?.Invoke();
    }

    public void SelectRelative(int direction)
    {
        var wrapped = (SelectedIndex + direction) % HotbarSlotCount;
        if (wrapped < 0)
        {
            wrapped += HotbarSlotCount;
        }

        Select(wrapped);
    }

    public InventorySlot Selected => _slots[SelectedIndex];

    public int Count(string itemId) =>
        _slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Count);

    public int CountFamily(string itemId) =>
        DataCatalog.ItemFamilyIds(itemId).Sum(Count);

    public bool CanAdd(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        var definition = DataCatalog.Item(itemId);
        var capacity = _slots
            .Where(slot => slot.IsEmpty || slot.ItemId == itemId)
            .Sum(slot => slot.IsEmpty ? definition.MaxStack : definition.MaxStack - slot.Count);
        return capacity >= amount;
    }

    public bool Add(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        var definition = DataCatalog.Item(itemId);
        if (!CanAdd(itemId, amount))
        {
            return false;
        }

        var remaining = amount;
        foreach (var slot in _slots.Where(slot => slot.ItemId == itemId && slot.Count < definition.MaxStack))
        {
            var moved = Math.Min(remaining, definition.MaxStack - slot.Count);
            slot.Count += moved;
            remaining -= moved;
            if (remaining == 0)
            {
                Changed?.Invoke();
                return true;
            }
        }

        foreach (var slot in _slots.Where(slot => slot.IsEmpty))
        {
            var moved = Math.Min(remaining, definition.MaxStack);
            slot.ItemId = itemId;
            slot.Count = moved;
            remaining -= moved;
            if (remaining == 0)
            {
                Changed?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool Remove(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Count(itemId) < amount)
        {
            return false;
        }

        var remaining = amount;
        for (var index = _slots.Count - 1; index >= 0 && remaining > 0; index--)
        {
            var slot = _slots[index];
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

        Changed?.Invoke();
        return true;
    }

    public bool RemoveFamily(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        var simulated = _slots.Select(slot => slot.Clone()).ToList();
        if (!RemoveFamilyFrom(simulated, itemId, amount))
        {
            return false;
        }

        ApplySimulation(simulated);
        Changed?.Invoke();
        return true;
    }

    public bool TryExchange(
        IReadOnlyList<CraftingIngredient> removals,
        string outputItemId,
        int outputCount
    )
    {
        var simulated = _slots.Select(slot => slot.Clone()).ToList();
        foreach (var removal in removals)
        {
            if (!RemoveFrom(simulated, removal.ItemId, removal.Count))
            {
                return false;
            }
        }

        if (!AddTo(simulated, outputItemId, outputCount))
        {
            return false;
        }

        foreach (var removal in removals)
        {
            _ = RemoveFrom(_slots, removal.ItemId, removal.Count);
        }
        _ = AddTo(_slots, outputItemId, outputCount);
        Changed?.Invoke();
        return true;
    }

    public bool TryRemoveMany(IReadOnlyList<CraftingIngredient> removals)
    {
        if (removals.Count == 0 ||
            removals.Any(removal => removal.Count <= 0))
        {
            return false;
        }

        var simulated = _slots.Select(slot => slot.Clone()).ToList();
        foreach (var removal in removals)
        {
            if (!RemoveFrom(
                    simulated,
                    removal.ItemId,
                    removal.Count
                ))
            {
                return false;
            }
        }

        ApplySimulation(simulated);

        Changed?.Invoke();
        return true;
    }

    public bool TryRemoveFamilies(
        IReadOnlyList<CraftingIngredient> removals
    )
    {
        if (removals.Count == 0 ||
            removals.Any(removal => removal.Count <= 0))
        {
            return false;
        }

        var simulated = _slots.Select(slot => slot.Clone()).ToList();
        foreach (var removal in removals)
        {
            if (!RemoveFamilyFrom(
                    simulated,
                    removal.ItemId,
                    removal.Count
                ))
            {
                return false;
            }
        }

        ApplySimulation(simulated);
        Changed?.Invoke();
        return true;
    }

    public bool TryAddMany(IReadOnlyList<CraftingIngredient> additions)
    {
        if (additions.Count == 0 ||
            additions.Any(addition => addition.Count <= 0))
        {
            return false;
        }

        var simulated = _slots.Select(slot => slot.Clone()).ToList();
        foreach (var addition in additions)
        {
            if (!DataCatalog.Items.ContainsKey(addition.ItemId) ||
                !AddTo(simulated, addition.ItemId, addition.Count))
            {
                return false;
            }
        }

        ApplySimulation(simulated);
        Changed?.Invoke();
        return true;
    }

    public bool PromoteToHotbar(string itemId)
    {
        var sourceIndex = _slots.FindIndex(slot => slot.ItemId == itemId && !slot.IsEmpty);
        if (sourceIndex < 0)
        {
            return false;
        }

        if (sourceIndex < HotbarSlotCount)
        {
            Select(sourceIndex);
            return true;
        }

        var targetIndex = Enumerable.Range(
            StartingToolCount,
            HotbarSlotCount - StartingToolCount
        ).FirstOrDefault(index => _slots[index].IsEmpty, -1);
        if (targetIndex < 0)
        {
            targetIndex = StartingToolCount;
        }

        (_slots[targetIndex], _slots[sourceIndex]) = (_slots[sourceIndex], _slots[targetIndex]);
        SelectedIndex = targetIndex;
        Changed?.Invoke();
        return true;
    }

    public List<InventorySlot> Capture() => _slots.Select(slot => slot.Clone()).ToList();

    private void PlaceStartingTools()
    {
        for (var index = 0; index < StartingTools.Length; index++)
        {
            _slots[index].ItemId = StartingTools[index];
            _slots[index].Count = 1;
        }
    }

    private void AddWithoutNotification(string itemId, int amount)
    {
        _ = AddTo(_slots, itemId, amount);
    }

    private static bool RemoveFrom(
        IList<InventorySlot> slots,
        string itemId,
        int amount
    )
    {
        if (amount <= 0)
        {
            return true;
        }

        if (slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Count) < amount)
        {
            return false;
        }

        var remaining = amount;
        for (var index = slots.Count - 1; index >= 0 && remaining > 0; index--)
        {
            var slot = slots[index];
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

    private static bool RemoveFamilyFrom(
        IList<InventorySlot> slots,
        string itemId,
        int amount
    )
    {
        if (amount <= 0)
        {
            return true;
        }

        var family = DataCatalog.ItemFamilyIds(itemId);
        var available = family.Sum(id =>
            slots.Where(slot => slot.ItemId == id).Sum(slot => slot.Count)
        );
        if (available < amount)
        {
            return false;
        }

        var remaining = amount;
        foreach (var familyItemId in family)
        {
            if (remaining <= 0)
            {
                break;
            }

            var count = slots
                .Where(slot => slot.ItemId == familyItemId)
                .Sum(slot => slot.Count);
            var removed = Math.Min(remaining, count);
            _ = RemoveFrom(slots, familyItemId, removed);
            remaining -= removed;
        }

        return remaining == 0;
    }

    private static bool AddTo(
        IList<InventorySlot> slots,
        string itemId,
        int amount
    )
    {
        if (amount <= 0)
        {
            return true;
        }

        var definition = DataCatalog.Item(itemId);
        var capacity = slots
            .Where(slot => slot.IsEmpty || slot.ItemId == itemId)
            .Sum(slot => slot.IsEmpty ? definition.MaxStack : definition.MaxStack - slot.Count);
        if (capacity < amount)
        {
            return false;
        }

        var remaining = amount;
        foreach (var slot in slots.Where(slot =>
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

        foreach (var slot in slots.Where(slot => slot.IsEmpty))
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

    private int FindRestoredSelection(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        var index = _slots.FindIndex(
            0,
            HotbarSlotCount,
            slot => slot.ItemId == itemId
        );
        return index >= 0 ? index : 0;
    }

    private void ApplySimulation(IReadOnlyList<InventorySlot> simulated)
    {
        for (var index = 0; index < _slots.Count; index++)
        {
            _slots[index].ItemId = simulated[index].ItemId;
            _slots[index].Count = simulated[index].Count;
        }
    }
}
