namespace Luminfield.Core;

public sealed record FishCollectionEntry(
    FishDefinition Fish,
    bool Caught
);

public sealed class FishingSystem
{
    public const int CastEnergyCost = 4;

    private readonly HashSet<string> _caughtFishIds =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CaughtFishIds => _caughtFishIds;
    public int CaughtCount => _caughtFishIds.Count;
    public int TotalFishCount => DataCatalog.Fishes.Count;

    public event Action? Changed;

    public void Reset()
    {
        _caughtFishIds.Clear();
        Changed?.Invoke();
    }

    public void Restore(FishingSave? save)
    {
        _caughtFishIds.Clear();

        foreach (var fishId in save?.CaughtFishIds ?? [])
        {
            if (DataCatalog.Fishes.ContainsKey(fishId))
            {
                _caughtFishIds.Add(fishId);
            }
        }

        Changed?.Invoke();
    }

    public ActionResult TryCatch(
        GridPosition target,
        int day,
        int minuteOfDay,
        string weatherId,
        Inventory inventory
    )
    {
        var fish = PreviewCatch(target, day, minuteOfDay, weatherId);
        if (fish is null)
        {
            return ActionResult.Fail("notice.fish_not_biting");
        }

        if (!inventory.CanAdd(fish.ItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        if (!inventory.Add(fish.ItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _caughtFishIds.Add(fish.Id);
        Changed?.Invoke();
        return ActionResult.Grant(
            fish.ItemId,
            1,
            CastEnergyCost,
            "notice.fish_caught"
        );
    }

    public bool IsCaught(string fishId) => _caughtFishIds.Contains(fishId);

    public IReadOnlyList<FishCollectionEntry> CollectionEntries() =>
        DataCatalog.FishItemIds
            .Select(fishId => DataCatalog.Fishes[fishId])
            .Select(fish => new FishCollectionEntry(
                fish,
                _caughtFishIds.Contains(fish.Id)
            ))
            .ToArray();

    public FishDefinition? PreviewCatch(
        GridPosition target,
        int day,
        int minuteOfDay,
        string weatherId
    )
    {
        if (!WorldDefinition.IsWaterSource(target))
        {
            return null;
        }

        var waterKind = WaterKindFor(target);
        var candidates = DataCatalog.Fishes.Values
            .Where(fish =>
                fish.WaterKind == waterKind &&
                fish.IsAvailable(day, minuteOfDay, weatherId)
            )
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var specificity = candidates.Max(fish => fish.AvailabilitySpecificity);
        candidates = candidates
            .Where(fish => fish.AvailabilitySpecificity == specificity)
            .OrderBy(fish => fish.Id, StringComparer.Ordinal)
            .ToArray();
        var roll = WorldDefinition.Hash(
            target.X + day * 31,
            target.Y + minuteOfDay
        );
        return candidates[(int)(roll % (uint)candidates.Length)];
    }

    public FishingSave Capture() => new()
    {
        CaughtFishIds = _caughtFishIds
            .Order(StringComparer.Ordinal)
            .ToList()
    };

    public static FishingWaterKind WaterKindFor(GridPosition target)
    {
        if (WorldDefinition.IsHomeCell(target))
        {
            return FishingWaterKind.HomesteadPond;
        }

        return WorldDefinition.GetBiome(target) switch
        {
            WorldBiome.MoonwaterWetlands => FishingWaterKind.MoonwaterWetlands,
            WorldBiome.CrystalVale => FishingWaterKind.CrystalStream,
            _ => FishingWaterKind.HomesteadPond
        };
    }
}
