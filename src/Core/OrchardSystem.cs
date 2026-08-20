namespace Luminfield.Core;

public enum OrchardPlacementIssue
{
    None,
    NotHome,
    WrongSurface,
    Blocked,
    Occupied
}

public sealed class FruitTreeState
{
    public FruitTreeState(GridPosition position, string treeId)
    {
        Position = position;
        TreeId = treeId;
    }

    public GridPosition Position { get; }
    public string TreeId { get; }
    public int AgeNights { get; set; }
    public bool FruitReady { get; set; }
    public int RegrowthProgress { get; set; }

    public bool IsMature =>
        AgeNights >= DataCatalog.FruitTree(TreeId).MatureAfterNights;
}

public sealed class BeehiveState
{
    public BeehiveState(GridPosition position)
    {
        Position = position;
    }

    public GridPosition Position { get; }
    public int PendingHoney { get; set; }
    public int ProgressNights { get; set; }
    public bool HasHoney => PendingHoney > 0;
}

public sealed class OrchardSystem
{
    public const int BeehivePollinationRange = 4;
    public const int BeehiveProductionNights = 2;

    private readonly Dictionary<GridPosition, FruitTreeState> _fruitTrees = [];
    private readonly Dictionary<GridPosition, BeehiveState> _beehives = [];

    public IReadOnlyDictionary<GridPosition, FruitTreeState> FruitTrees =>
        _fruitTrees;

    public IReadOnlyDictionary<GridPosition, BeehiveState> Beehives =>
        _beehives;

    public event Action<GridPosition>? Changed;

    public void Reset()
    {
        _fruitTrees.Clear();
        _beehives.Clear();
    }

    public bool HasFruitTree(GridPosition position) =>
        _fruitTrees.ContainsKey(position);

    public FruitTreeState? FruitTreeAt(GridPosition position) =>
        _fruitTrees.GetValueOrDefault(position);

    public bool HasBeehive(GridPosition position) =>
        _beehives.ContainsKey(position);

    public BeehiveState? BeehiveAt(GridPosition position) =>
        _beehives.GetValueOrDefault(position);

    public bool BlocksMovement(GridPosition position) =>
        _fruitTrees.ContainsKey(position);

    public IEnumerable<GridPosition> InteractiveCells =>
        _fruitTrees.Keys.Concat(_beehives.Keys);

    public OrchardPlacementIssue CheckTreePlacement(
        GridPosition position,
        FarmSystem farm,
        StorageSystem storage,
        FarmObjectSystem farmObjects,
        Func<GridPosition, bool>? extraOccupied = null
    )
    {
        if (!WorldDefinition.IsHomeCell(position))
        {
            return OrchardPlacementIssue.NotHome;
        }

        if (_fruitTrees.ContainsKey(position) ||
            storage.HasChest(position) ||
            farmObjects.HasObject(position) ||
            extraOccupied?.Invoke(position) == true)
        {
            return OrchardPlacementIssue.Occupied;
        }

        if (WorldDefinition.IsBlocked(position) ||
            FarmLayout.IsStaticBlocked(position) ||
            farm.IsReserved(position) ||
            farm.Tiles.ContainsKey(position))
        {
            return OrchardPlacementIssue.Blocked;
        }

        return FarmSystem.IsPlantingBed(position)
            ? OrchardPlacementIssue.WrongSurface
            : OrchardPlacementIssue.None;
    }

    public ActionResult TryPlantTree(
        string treeId,
        GridPosition position,
        Inventory inventory,
        FarmSystem farm,
        StorageSystem storage,
        FarmObjectSystem farmObjects,
        int day,
        Func<GridPosition, bool>? extraOccupied = null
    )
    {
        if (!DataCatalog.FruitTrees.TryGetValue(treeId, out var definition))
        {
            return ActionResult.Fail("notice.not_ready");
        }

        if (!definition.IsAvailableOnDay(day))
        {
            return ActionResult.Fail("notice.sapling_out_of_season");
        }

        var issue = CheckTreePlacement(
            position,
            farm,
            storage,
            farmObjects,
            extraOccupied
        );
        if (issue != OrchardPlacementIssue.None)
        {
            return ActionResult.Fail(MessageForIssue(issue));
        }

        if (!inventory.Remove(definition.SaplingItemId, 1))
        {
            return ActionResult.Fail("notice.no_sapling");
        }

        _fruitTrees[position] = new FruitTreeState(position, treeId);
        Changed?.Invoke(position);
        return ActionResult.Success(messageKey: "notice.fruit_tree_planted");
    }

    public ActionResult TryHarvestFruit(
        GridPosition position,
        Inventory inventory
    )
    {
        if (!_fruitTrees.TryGetValue(position, out var tree))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (!tree.IsMature)
        {
            return ActionResult.Fail("notice.fruit_tree_growing");
        }

        if (!tree.FruitReady)
        {
            return ActionResult.Fail("notice.fruit_tree_recovering");
        }

        var definition = DataCatalog.FruitTree(tree.TreeId);
        if (!inventory.CanAdd(definition.HarvestItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        inventory.Add(definition.HarvestItemId, 1);
        tree.FruitReady = false;
        tree.RegrowthProgress = 0;
        Changed?.Invoke(position);
        return ActionResult.Grant(
            definition.HarvestItemId,
            1,
            0,
            "notice.fruit_tree_harvested"
        );
    }

    public void EnsureBeehive(GridPosition position)
    {
        if (_beehives.ContainsKey(position))
        {
            return;
        }

        _beehives[position] = new BeehiveState(position);
        Changed?.Invoke(position);
    }

    public bool HasPollinationSource(GridPosition beehivePosition) =>
        _fruitTrees.Values.Any(tree =>
            tree.IsMature &&
            Distance(tree.Position, beehivePosition) <= BeehivePollinationRange
        );

    public ActionResult TryCollectHoney(
        GridPosition position,
        Inventory inventory
    )
    {
        if (!_beehives.TryGetValue(position, out var hive))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (!hive.HasHoney)
        {
            return ActionResult.Fail("notice.honey_not_ready");
        }

        if (!inventory.CanAdd(DataCatalog.StarhoneyId, hive.PendingHoney))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var collected = hive.PendingHoney;
        inventory.Add(DataCatalog.StarhoneyId, collected);
        hive.PendingHoney = 0;
        hive.ProgressNights = 0;
        Changed?.Invoke(position);
        return ActionResult.Grant(
            DataCatalog.StarhoneyId,
            collected,
            0,
            "notice.honey_collected"
        );
    }

    public void ResolveNight(FarmObjectSystem farmObjects)
    {
        var changed = new HashSet<GridPosition>(
            SynchronizeBeehives(farmObjects)
        );

        foreach (var tree in _fruitTrees.Values)
        {
            if (ResolveFruitTreeNight(tree))
            {
                changed.Add(tree.Position);
            }
        }

        foreach (var hive in _beehives.Values)
        {
            if (ResolveBeehiveNight(hive))
            {
                changed.Add(hive.Position);
            }
        }

        foreach (var position in changed)
        {
            Changed?.Invoke(position);
        }
    }

    public void Restore(
        OrchardSave? save,
        FarmSystem farm,
        StorageSystem storage,
        FarmObjectSystem farmObjects,
        IEnumerable<GridPosition>? extraOccupiedCells = null
    )
    {
        _fruitTrees.Clear();
        _beehives.Clear();
        var normalized = NormalizeSave(
            save,
            farmObjects.Capture(),
            farm.Tiles.Keys,
            storage.Chests.Keys,
            extraOccupiedCells
        );

        foreach (var entry in normalized.FruitTrees)
        {
            _fruitTrees[new GridPosition(entry.X, entry.Y)] =
                new FruitTreeState(new GridPosition(entry.X, entry.Y), entry.TreeId)
                {
                    AgeNights = entry.AgeNights,
                    FruitReady = entry.FruitReady,
                    RegrowthProgress = entry.RegrowthProgress
                };
        }

        foreach (var entry in normalized.Beehives)
        {
            _beehives[new GridPosition(entry.X, entry.Y)] =
                new BeehiveState(new GridPosition(entry.X, entry.Y))
                {
                    PendingHoney = entry.PendingHoney,
                    ProgressNights = entry.ProgressNights
                };
        }
    }

    public OrchardSave Capture() => new()
    {
        FruitTrees = _fruitTrees
            .OrderBy(pair => pair.Key.Y)
            .ThenBy(pair => pair.Key.X)
            .Select(pair => new FruitTreeSave
            {
                X = pair.Key.X,
                Y = pair.Key.Y,
                TreeId = pair.Value.TreeId,
                AgeNights = pair.Value.AgeNights,
                FruitReady = pair.Value.FruitReady,
                RegrowthProgress = pair.Value.RegrowthProgress
            })
            .ToList(),
        Beehives = _beehives
            .OrderBy(pair => pair.Key.Y)
            .ThenBy(pair => pair.Key.X)
            .Select(pair => new BeehiveSave
            {
                X = pair.Key.X,
                Y = pair.Key.Y,
                PendingHoney = pair.Value.PendingHoney,
                ProgressNights = pair.Value.ProgressNights
            })
            .ToList()
    };

    public static OrchardSave NormalizeSave(
        OrchardSave? save,
        FarmObjectSave? farmObjects,
        IEnumerable<GridPosition>? occupiedFarmTiles,
        IEnumerable<GridPosition>? occupiedStorageCells,
        IEnumerable<GridPosition>? extraOccupiedCells = null
    )
    {
        var farmTileCells = occupiedFarmTiles?.ToHashSet() ?? [];
        var storageCells = occupiedStorageCells?.ToHashSet() ?? [];
        var extraCells = extraOccupiedCells?.ToHashSet() ?? [];
        var objectCells = (farmObjects?.Objects ?? [])
            .Where(entry => entry is not null)
            .Select(entry => new GridPosition(entry.X, entry.Y))
            .ToHashSet();
        var treeCells = new HashSet<GridPosition>();
        var trees = new List<FruitTreeSave>();

        foreach (var entry in save?.FruitTrees ?? [])
        {
            if (entry is null ||
                !DataCatalog.FruitTrees.TryGetValue(entry.TreeId, out var tree))
            {
                continue;
            }

            var position = new GridPosition(entry.X, entry.Y);
            if (!CanKeepTree(
                    position,
                    farmTileCells,
                    storageCells,
                    objectCells,
                    extraCells,
                    treeCells
                ))
            {
                continue;
            }

            treeCells.Add(position);
            var age = Math.Clamp(entry.AgeNights, 0, tree.MatureAfterNights);
            var fruitReady = age >= tree.MatureAfterNights && entry.FruitReady;
            var regrowthProgress = fruitReady
                ? 0
                : Math.Clamp(entry.RegrowthProgress, 0, tree.RegrowthNights);
            if (age < tree.MatureAfterNights)
            {
                regrowthProgress = 0;
            }

            trees.Add(new FruitTreeSave
            {
                X = position.X,
                Y = position.Y,
                TreeId = tree.Id,
                AgeNights = age,
                FruitReady = fruitReady,
                RegrowthProgress = regrowthProgress
            });
        }

        var savedHives = (save?.Beehives ?? [])
            .Where(entry => entry is not null)
            .GroupBy(entry => new GridPosition(entry.X, entry.Y))
            .ToDictionary(group => group.Key, group => group.First());
        var hives = (farmObjects?.Objects ?? [])
            .Where(entry => entry.ItemId == DataCatalog.GlowcombHiveId)
            .Select(entry => new GridPosition(entry.X, entry.Y))
            .Distinct()
            .OrderBy(position => position.Y)
            .ThenBy(position => position.X)
            .Select(position =>
            {
                savedHives.TryGetValue(position, out var savedHive);
                var pending = Math.Clamp(savedHive?.PendingHoney ?? 0, 0, 1);
                var progress = pending > 0
                    ? 0
                    : Math.Clamp(
                        savedHive?.ProgressNights ?? 0,
                        0,
                        BeehiveProductionNights
                    );
                return new BeehiveSave
                {
                    X = position.X,
                    Y = position.Y,
                    PendingHoney = pending,
                    ProgressNights = progress
                };
            })
            .ToList();

        return new OrchardSave
        {
            FruitTrees = trees,
            Beehives = hives
        };
    }

    public static string MessageForIssue(OrchardPlacementIssue issue) =>
        issue switch
        {
            OrchardPlacementIssue.NotHome => "notice.sapling_home_only",
            OrchardPlacementIssue.WrongSurface => "notice.sapling_ground_only",
            OrchardPlacementIssue.Occupied => "notice.sapling_occupied",
            _ => "notice.sapling_blocked"
        };

    private static bool CanKeepTree(
        GridPosition position,
        IReadOnlySet<GridPosition> farmTileCells,
        IReadOnlySet<GridPosition> storageCells,
        IReadOnlySet<GridPosition> objectCells,
        IReadOnlySet<GridPosition> extraCells,
        IReadOnlySet<GridPosition> treeCells
    )
    {
        if (!WorldDefinition.IsHomeCell(position) ||
            WorldDefinition.IsBlocked(position) ||
            FarmLayout.IsStaticBlocked(position) ||
            FarmSystem.IsPlantingBed(position))
        {
            return false;
        }

        var farm = new FarmSystem();
        return !farm.IsReserved(position) &&
            !farmTileCells.Contains(position) &&
            !storageCells.Contains(position) &&
            !objectCells.Contains(position) &&
            !extraCells.Contains(position) &&
            !treeCells.Contains(position);
    }

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private bool ResolveFruitTreeNight(FruitTreeState tree)
    {
        var definition = DataCatalog.FruitTree(tree.TreeId);
        if (tree.AgeNights < definition.MatureAfterNights)
        {
            tree.AgeNights++;
            if (tree.AgeNights >= definition.MatureAfterNights)
            {
                tree.FruitReady = true;
                tree.RegrowthProgress = 0;
            }

            return true;
        }

        if (tree.FruitReady)
        {
            return false;
        }

        tree.RegrowthProgress++;
        if (tree.RegrowthProgress >= definition.RegrowthNights)
        {
            tree.FruitReady = true;
            tree.RegrowthProgress = 0;
        }

        return true;
    }

    private bool ResolveBeehiveNight(BeehiveState hive)
    {
        if (hive.HasHoney)
        {
            return false;
        }

        if (!HasPollinationSource(hive.Position))
        {
            if (hive.ProgressNights == 0)
            {
                return false;
            }

            hive.ProgressNights = 0;
            return true;
        }

        hive.ProgressNights++;
        if (hive.ProgressNights >= BeehiveProductionNights)
        {
            hive.PendingHoney = 1;
            hive.ProgressNights = 0;
        }

        return true;
    }

    private IEnumerable<GridPosition> SynchronizeBeehives(
        FarmObjectSystem farmObjects
    )
    {
        var placed = farmObjects.Objects
            .Where(pair => pair.Value == DataCatalog.GlowcombHiveId)
            .Select(pair => pair.Key)
            .ToHashSet();
        var changed = new List<GridPosition>();

        foreach (var stale in _beehives.Keys
                     .Where(position => !placed.Contains(position))
                     .ToArray())
        {
            _beehives.Remove(stale);
            changed.Add(stale);
        }

        foreach (var position in placed)
        {
            if (_beehives.ContainsKey(position))
            {
                continue;
            }

            _beehives[position] = new BeehiveState(position);
            changed.Add(position);
        }

        return changed;
    }
}
