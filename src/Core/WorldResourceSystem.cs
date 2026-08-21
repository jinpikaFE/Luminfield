namespace Luminfield.Core;

public sealed class WorldResourceSystem
{
    public const int CrystalRespawnDays = 2;
    public const int TreeRespawnDays = CalendarSystem.DaysPerWeek;
    public const int RenewedWoodlandTreeRespawnDays = 4;
    public const int GatherEnergyCost = 4;
    public const int BaseTreeYield = 2;
    public const int BaseCrystalYield = 1;

    private readonly Dictionary<string, int> _depleted = new(StringComparer.Ordinal);

    public event Action<GridPosition>? Changed;

    public void Reset()
    {
        _depleted.Clear();
    }

    public void Restore(
        ResourceSave? save,
        int currentDay,
        bool woodlandRenewalUnlocked = false
    )
    {
        _depleted.Clear();
        foreach (var entry in save?.DepletedNodes ?? [])
        {
            if (WorldDefinition.TryParseCellId(entry.NodeId, out var cell) &&
                WorldDefinition.ResourceAt(cell) != WorldResourceKind.None)
            {
                _depleted[entry.NodeId] = Math.Clamp(entry.RemovedDay, 1, currentDay);
            }
        }

        foreach (var id in save?.RemovedNodes ?? [])
        {
            if (_depleted.ContainsKey(id) ||
                !WorldDefinition.TryParseCellId(id, out var cell) ||
                WorldDefinition.ResourceAt(cell) == WorldResourceKind.None)
            {
                continue;
            }

            // Legacy schema-v1 saves only knew that a node was absent. Treat the load
            // day as its depletion day so an old save never makes resources reappear
            // immediately and still enters the new deterministic respawn cycle.
            _depleted[id] = currentDay;
        }

        ResolveDay(currentDay, woodlandRenewalUnlocked);
    }

    public bool IsRemoved(GridPosition cell) =>
        _depleted.ContainsKey(WorldDefinition.CellId(cell));

    public ActionResult TryGather(
        GridPosition cell,
        string toolId,
        int availableEnergy,
        Inventory inventory,
        int currentDay,
        int treeYieldBonus = 0
    )
    {
        var resource = WorldDefinition.ResourceAt(cell);
        if (resource == WorldResourceKind.None)
        {
            return ActionResult.Fail(
                toolId == DataCatalog.MacheteId
                    ? "notice.no_tree"
                    : "notice.no_crystal"
            );
        }

        if (IsRemoved(cell))
        {
            return ActionResult.Fail("notice.resource_depleted");
        }

        var requiredTool = resource == WorldResourceKind.Tree
            ? DataCatalog.MacheteId
            : DataCatalog.ShovelId;
        if (toolId != requiredTool)
        {
            return ActionResult.Fail(
                resource == WorldResourceKind.Tree
                    ? "notice.needs_machete"
                    : "notice.needs_shovel"
            );
        }

        if (availableEnergy < GatherEnergyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        var itemId = resource == WorldResourceKind.Tree
            ? DataCatalog.LumenwoodId
            : DataCatalog.CrystalShardId;
        var count = resource == WorldResourceKind.Tree
            ? BaseTreeYield + Math.Max(0, treeYieldBonus)
            : BaseCrystalYield;
        if (!inventory.Add(itemId, count))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _depleted[WorldDefinition.CellId(cell)] = Math.Max(1, currentDay);
        Changed?.Invoke(cell);
        return ActionResult.Grant(
            itemId,
            count,
            GatherEnergyCost,
            resource == WorldResourceKind.Tree
                ? "notice.gathered_wood"
                : "notice.gathered_crystal"
        );
    }

    public int ResolveDay(
        int currentDay,
        bool woodlandRenewalUnlocked = false
    )
    {
        var respawned = new List<GridPosition>();
        foreach (var pair in _depleted)
        {
            if (!WorldDefinition.TryParseCellId(pair.Key, out var cell))
            {
                continue;
            }

            var resource = WorldDefinition.ResourceAt(cell);
            var interval = RespawnInterval(
                cell,
                resource,
                woodlandRenewalUnlocked
            );
            if (currentDay - pair.Value >= interval)
            {
                respawned.Add(cell);
            }
        }

        foreach (var cell in respawned)
        {
            _depleted.Remove(WorldDefinition.CellId(cell));
            Changed?.Invoke(cell);
        }

        return respawned.Count;
    }

    private static int RespawnInterval(
        GridPosition cell,
        WorldResourceKind resource,
        bool woodlandRenewalUnlocked
    )
    {
        if (resource == WorldResourceKind.Crystal)
        {
            return CrystalRespawnDays;
        }

        if (resource != WorldResourceKind.Tree)
        {
            return int.MaxValue;
        }

        if (woodlandRenewalUnlocked &&
            WorldDefinition.GetBiome(cell) == WorldBiome.WhisperingWoods)
        {
            return RenewedWoodlandTreeRespawnDays;
        }

        return TreeRespawnDays;
    }

    public ResourceSave Capture() => new()
    {
        // Keep RemovedNodes populated for older schema-v1 readers while the dated
        // records carry the new additive respawn state.
        RemovedNodes = _depleted.Keys.Order(StringComparer.Ordinal).ToList(),
        DepletedNodes = _depleted
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ResourceDepletionSave
            {
                NodeId = pair.Key,
                RemovedDay = pair.Value
            })
            .ToList()
    };
}
