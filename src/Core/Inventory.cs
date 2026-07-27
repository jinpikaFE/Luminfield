namespace Luminfield.Core;

public sealed class Inventory
{
    public const int HotbarSlotCount = 8;
    public const int SlotCount = 24;
    public const int StartingToolCount = 5;

    private static readonly string[] StartingTools =
    [
        DataCatalog.HandId,
        DataCatalog.ShovelId,
        DataCatalog.MacheteId,
        DataCatalog.WateringCanId,
        DataCatalog.BucketId
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
        var definition = DataCatalog.Item(itemId);
        var remaining = amount;
        foreach (var slot in _slots.Where(slot =>
                     slot.ItemId == itemId &&
                     slot.Count < definition.MaxStack))
        {
            var moved = Math.Min(remaining, definition.MaxStack - slot.Count);
            slot.Count += moved;
            remaining -= moved;
            if (remaining == 0)
            {
                return;
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
                return;
            }
        }
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
}
