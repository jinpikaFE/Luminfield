namespace Luminfield.Core;

public sealed class Inventory
{
    public const int SlotCount = 8;

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

        _slots[0].ItemId = DataCatalog.HoeId;
        _slots[0].Count = 1;
        _slots[1].ItemId = DataCatalog.WateringCanId;
        _slots[1].Count = 1;
        SelectedIndex = 0;
        Changed?.Invoke();
    }

    public void Restore(IEnumerable<InventorySlot>? slots, int selectedIndex)
    {
        for (var index = 0; index < SlotCount; index++)
        {
            var source = slots?.ElementAtOrDefault(index);
            _slots[index].ItemId = source?.ItemId ?? string.Empty;
            _slots[index].Count = Math.Max(0, source?.Count ?? 0);
        }

        SelectedIndex = Math.Clamp(selectedIndex, 0, SlotCount - 1);
        Changed?.Invoke();
    }

    public void Select(int index)
    {
        var next = Math.Clamp(index, 0, SlotCount - 1);
        if (next == SelectedIndex)
        {
            return;
        }

        SelectedIndex = next;
        Changed?.Invoke();
    }

    public void SelectRelative(int direction)
    {
        var wrapped = (SelectedIndex + direction) % SlotCount;
        if (wrapped < 0)
        {
            wrapped += SlotCount;
        }

        Select(wrapped);
    }

    public InventorySlot Selected => _slots[SelectedIndex];

    public int Count(string itemId) =>
        _slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Count);

    public bool Add(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        var definition = DataCatalog.Item(itemId);
        var capacity = _slots
            .Where(slot => slot.IsEmpty || slot.ItemId == itemId)
            .Sum(slot => slot.IsEmpty ? definition.MaxStack : definition.MaxStack - slot.Count);

        if (capacity < amount)
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
}
