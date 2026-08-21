namespace Luminfield.Core;

public sealed record ShippingLine(
    string ItemId,
    int Count,
    int UnitPrice
)
{
    public int TotalCoins => Count * UnitPrice;
}

public sealed record ShippingSettlement(
    int Day,
    IReadOnlyList<ShippingLine> Lines
)
{
    public static readonly ShippingSettlement Empty = new(0, []);

    public int TotalCoins => Lines.Sum(line => line.TotalCoins);
    public int TotalItems => Lines.Sum(line => line.Count);
}

public sealed class ShippingBinSystem
{
    private readonly Dictionary<string, int> _pending = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> Pending => _pending;
    public int PendingItemCount => _pending.Values.Sum();
    public int PendingValue => PendingValueFor();
    public ShippingSettlement LastSettlement { get; private set; } =
        ShippingSettlement.Empty;

    public event Action? Changed;

    public void Reset()
    {
        _pending.Clear();
        LastSettlement = ShippingSettlement.Empty;
        Changed?.Invoke();
    }

    public void Restore(ShippingSave? save)
    {
        _pending.Clear();
        foreach (var group in (save?.Pending ?? [])
                     .Where(entry => IsSellable(entry.ItemId) && entry.Count > 0)
                     .GroupBy(entry => entry.ItemId, StringComparer.Ordinal))
        {
            _pending[group.Key] = Math.Min(
                group.Sum(entry => entry.Count),
                Inventory.SlotCount * 99
            );
        }

        var last = save?.LastSettlement;
        var lines = (last?.Entries ?? [])
            .Where(entry => IsSellable(entry.ItemId) && entry.Count > 0)
            .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
            .Select(group =>
            {
                var savedPrice = group.Select(entry => entry.UnitPrice)
                    .FirstOrDefault(price => price > 0);
                return new ShippingLine(
                    group.Key,
                    Math.Min(
                        group.Sum(entry => entry.Count),
                        Inventory.SlotCount * 99
                    ),
                    savedPrice > 0
                        ? savedPrice
                        : DataCatalog.Item(group.Key).SellPrice
                );
            })
            .OrderBy(line => SellableOrder(line.ItemId))
            .ToArray();
        LastSettlement = last is { Day: > 0 } && lines.Length > 0
            ? new ShippingSettlement(last.Day, lines)
            : ShippingSettlement.Empty;
        Changed?.Invoke();
    }

    public ActionResult QueueOne(string itemId, Inventory inventory)
    {
        if (!IsSellable(itemId))
        {
            return ActionResult.Fail("shipping.cannot_ship");
        }

        if (!inventory.Remove(itemId, 1))
        {
            return ActionResult.Fail("shipping.none_available");
        }

        _pending[itemId] = PendingCount(itemId) + 1;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "shipping.queued");
    }

    public ActionResult ReclaimOne(string itemId, Inventory inventory)
    {
        if (PendingCount(itemId) <= 0)
        {
            return ActionResult.Fail("shipping.none_queued");
        }

        if (!inventory.Add(itemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var remaining = _pending[itemId] - 1;
        if (remaining <= 0)
        {
            _pending.Remove(itemId);
        }
        else
        {
            _pending[itemId] = remaining;
        }

        Changed?.Invoke();
        return ActionResult.Success(messageKey: "shipping.reclaimed");
    }

    public int PendingCount(string itemId) =>
        _pending.GetValueOrDefault(itemId);

    public int PendingValueFor(Func<string, int>? unitPrice = null)
    {
        unitPrice ??= itemId => DataCatalog.Item(itemId).SellPrice;
        return _pending.Sum(pair => pair.Value * unitPrice(pair.Key));
    }

    public ShippingSettlement Settle(
        int day,
        Func<string, int>? unitPrice = null
    )
    {
        unitPrice ??= itemId => DataCatalog.Item(itemId).SellPrice;
        var lines = _pending
            .Select(pair => new ShippingLine(
                pair.Key,
                pair.Value,
                unitPrice(pair.Key)
            ))
            .OrderBy(line => SellableOrder(line.ItemId))
            .ToArray();
        _pending.Clear();
        LastSettlement = new ShippingSettlement(Math.Max(1, day), lines);
        Changed?.Invoke();
        return LastSettlement;
    }

    public ShippingSave Capture() => new()
    {
        Pending = _pending
            .OrderBy(pair => SellableOrder(pair.Key))
            .Select(pair => new ShippingEntrySave
            {
                ItemId = pair.Key,
                Count = pair.Value
            })
            .ToList(),
        LastSettlement = new ShippingSettlementSave
        {
            Day = LastSettlement.Day,
            Entries = LastSettlement.Lines
                .Select(line => new ShippingEntrySave
                {
                    ItemId = line.ItemId,
                    Count = line.Count,
                    UnitPrice = line.UnitPrice
                })
                .ToList()
        }
    };

    private static bool IsSellable(string itemId) =>
        DataCatalog.Items.TryGetValue(itemId, out var item) &&
        item.SellPrice > 0;

    private static int SellableOrder(string itemId)
    {
        for (var index = 0; index < DataCatalog.SellableItemIds.Count; index++)
        {
            if (DataCatalog.SellableItemIds[index] == itemId)
            {
                return index;
            }
        }

        return int.MaxValue;
    }
}
