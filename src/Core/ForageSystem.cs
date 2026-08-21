namespace Luminfield.Core;

public sealed record ForageSpawn(
    string SlotId,
    string ItemId,
    GridPosition Cell,
    bool Collected
);

public sealed class ForageSystem
{
    public const int BaseCollectionYield = 1;

    private static readonly GridPosition[] NeighborOffsets =
    [
        new(0, -1),
        new(1, 0),
        new(0, 1),
        new(-1, 0)
    ];

    private readonly Dictionary<string, ForageSpawn> _spawns =
        new(StringComparer.Ordinal);

    public int ResolvedDay { get; private set; } = 1;
    public IReadOnlyList<ForageSpawn> Spawns => ForageCatalog.Slots
        .Where(slot => _spawns.ContainsKey(slot.Id))
        .Select(slot => _spawns[slot.Id])
        .ToArray();
    public IReadOnlyList<ForageSpawn> ActiveSpawns => Spawns
        .Where(spawn => !spawn.Collected)
        .ToArray();

    public event Action<GridPosition>? Changed;

    public void Reset(int day, string weatherId)
    {
        _spawns.Clear();
        ResolvedDay = Math.Max(1, day);
        foreach (var spawn in Generate(ResolvedDay, weatherId))
        {
            _spawns[spawn.SlotId] = spawn;
        }
    }

    public void Restore(ForageSave? save, int day, string weatherId)
    {
        var currentDay = Math.Max(1, day);
        var generated = Generate(currentDay, weatherId)
            .ToDictionary(spawn => spawn.SlotId, StringComparer.Ordinal);
        var normalized = new Dictionary<string, ForageSpawn>(
            StringComparer.Ordinal
        );
        var occupied = new HashSet<GridPosition>();

        if (save?.ResolvedDay == currentDay)
        {
            foreach (var entry in save.Spawns ?? [])
            {
                if (!generated.TryGetValue(entry.SlotId, out var expected) ||
                    entry.ItemId != expected.ItemId)
                {
                    continue;
                }

                var cell = new GridPosition(entry.X, entry.Y);
                if (cell != expected.Cell ||
                    !IsCandidate(cell, ForageCatalog.ByItemId[entry.ItemId].Biome) ||
                    !occupied.Add(cell))
                {
                    continue;
                }

                normalized[entry.SlotId] = new ForageSpawn(
                    entry.SlotId,
                    entry.ItemId,
                    cell,
                    entry.Collected
                );
            }
        }

        foreach (var slot in ForageCatalog.ActiveSlots(weatherId))
        {
            if (normalized.ContainsKey(slot.Id))
            {
                continue;
            }

            var fallback = generated[slot.Id];
            if (occupied.Add(fallback.Cell))
            {
                normalized[slot.Id] = fallback;
            }
        }

        _spawns.Clear();
        foreach (var pair in normalized)
        {
            _spawns[pair.Key] = pair.Value;
        }
        ResolvedDay = currentDay;
    }

    public void ResolveDay(int day, string weatherId)
    {
        var oldCells = ActiveSpawns.Select(spawn => spawn.Cell).ToArray();
        Reset(day, weatherId);
        foreach (var cell in oldCells.Concat(ActiveSpawns.Select(spawn => spawn.Cell)))
        {
            Changed?.Invoke(cell);
        }
    }

    public ForageSpawn? SpawnAt(GridPosition cell) => ActiveSpawns
        .FirstOrDefault(spawn => spawn.Cell == cell);

    public ActionResult CheckCollect(
        GridPosition target,
        string locationId,
        GridPosition playerCell,
        string selectedItemId,
        Inventory inventory,
        int quantity = BaseCollectionYield
    )
    {
        var spawn = SpawnAt(target);
        if (locationId != PlayerLocationIds.World ||
            spawn is null ||
            ManhattanDistance(playerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (selectedItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        var safeQuantity = Math.Max(1, quantity);
        return inventory.CanAdd(spawn.ItemId, safeQuantity)
            ? ActionResult.Success(messageKey: "target.action.collect_forage")
            : ActionResult.Fail("notice.inventory_full");
    }

    public ActionResult TryCollect(
        GridPosition target,
        string locationId,
        GridPosition playerCell,
        string selectedItemId,
        Inventory inventory,
        int quantity = BaseCollectionYield
    )
    {
        var check = CheckCollect(
            target,
            locationId,
            playerCell,
            selectedItemId,
            inventory,
            quantity
        );
        if (!check.Succeeded)
        {
            return check;
        }

        var spawn = SpawnAt(target)!;
        var safeQuantity = Math.Max(1, quantity);
        if (!inventory.Add(spawn.ItemId, safeQuantity))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _spawns[spawn.SlotId] = spawn with { Collected = true };
        Changed?.Invoke(target);
        return ActionResult.Grant(
            spawn.ItemId,
            safeQuantity,
            0,
            "notice.forage_collected"
        );
    }

    public ForageSave Capture() => new()
    {
        ResolvedDay = ResolvedDay,
        Spawns = Spawns.Select(spawn => new ForageSpawnSave
        {
            SlotId = spawn.SlotId,
            ItemId = spawn.ItemId,
            X = spawn.Cell.X,
            Y = spawn.Cell.Y,
            Collected = spawn.Collected
        }).ToList()
    };

    public static ForageSave NormalizeSave(
        ForageSave? save,
        int day,
        string weatherId
    )
    {
        var forage = new ForageSystem();
        forage.Restore(save, day, weatherId);
        return forage.Capture();
    }

    public static IReadOnlyList<ForageSpawn> Generate(
        int day,
        string weatherId
    )
    {
        var currentDay = Math.Max(1, day);
        var seasonId = CalendarSystem.SeasonId(currentDay);
        var chosen = new List<ForageSpawn>();
        foreach (var slot in ForageCatalog.ActiveSlots(weatherId))
        {
            var definition = ForageCatalog.ForSeasonAndBiome(
                seasonId,
                slot.Biome
            );
            var cell = CandidateCells(slot.Biome)
                .Where(candidate => chosen.All(existing =>
                    ManhattanDistance(existing.Cell, candidate) >= 2
                ))
                .OrderBy(candidate => SpawnScore(
                    candidate,
                    currentDay,
                    slot.Ordinal
                ))
                .ThenBy(candidate => candidate.Y)
                .ThenBy(candidate => candidate.X)
                .First();
            chosen.Add(new ForageSpawn(
                slot.Id,
                definition.ItemId,
                cell,
                false
            ));
        }

        return chosen;
    }

    public static bool IsCandidate(GridPosition cell, WorldBiome biome) =>
        WorldDefinition.IsInBounds(cell) &&
        !WorldDefinition.IsBoundaryCell(cell) &&
        !WorldDefinition.IsHomeCell(cell) &&
        WorldDefinition.GetBiome(cell) == biome &&
        !WorldDefinition.IsPath(cell) &&
        !WorldDefinition.IsWater(cell) &&
        WorldDefinition.Landmarks.All(landmark =>
            ManhattanDistance(cell, landmark.Position) >= 2
        ) &&
        !WorldDefinition.IsMeadowStarlightReservedCell(cell) &&
        WorldDefinition.PropAtlasIndex(cell) < 0 &&
        WorldDefinition.ResourceAt(cell) == WorldResourceKind.None &&
        !WorldDefinition.IsBlocked(cell) &&
        NeighborOffsets.Any(offset => IsStaticApproach(cell, offset));

    private static IEnumerable<GridPosition> CandidateCells(WorldBiome biome)
    {
        for (var y = 1; y < WorldDefinition.Height - 1; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (IsCandidate(cell, biome))
                {
                    yield return cell;
                }
            }
        }
    }

    private static bool IsStaticApproach(
        GridPosition cell,
        GridPosition offset
    )
    {
        var approach = new GridPosition(cell.X + offset.X, cell.Y + offset.Y);
        return WorldDefinition.IsInBounds(approach) &&
            !WorldDefinition.IsBoundaryCell(approach) &&
            !WorldDefinition.IsBlocked(approach);
    }

    private static uint SpawnScore(
        GridPosition cell,
        int day,
        int slotOrdinal
    )
    {
        unchecked
        {
            var daySalt = day * 977 + slotOrdinal * 131;
            return WorldDefinition.Hash(
                cell.X + daySalt,
                cell.Y + day * 53 + slotOrdinal * 389
            );
        }
    }

    private static int ManhattanDistance(GridPosition left, GridPosition right) =>
        Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
}
