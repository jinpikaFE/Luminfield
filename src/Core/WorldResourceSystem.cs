namespace Luminfield.Core;

public sealed class WorldResourceSystem
{
    private readonly HashSet<string> _removed = new(StringComparer.Ordinal);

    public event Action<GridPosition>? Changed;

    public void Reset()
    {
        _removed.Clear();
    }

    public void Restore(ResourceSave? save)
    {
        _removed.Clear();
        foreach (var id in save?.RemovedNodes ?? [])
        {
            if (WorldDefinition.TryParseCellId(id, out var cell) &&
                WorldDefinition.ResourceAt(cell) != WorldResourceKind.None)
            {
                _removed.Add(id);
            }
        }
    }

    public bool IsRemoved(GridPosition cell) =>
        _removed.Contains(WorldDefinition.CellId(cell));

    public ActionResult TryGather(
        GridPosition cell,
        string toolId,
        int availableEnergy,
        Inventory inventory
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

        const int energyCost = 4;
        if (availableEnergy < energyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        var itemId = resource == WorldResourceKind.Tree
            ? DataCatalog.LumenwoodId
            : DataCatalog.CrystalShardId;
        var count = resource == WorldResourceKind.Tree ? 2 : 1;
        if (!inventory.Add(itemId, count))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _removed.Add(WorldDefinition.CellId(cell));
        Changed?.Invoke(cell);
        return ActionResult.Grant(
            itemId,
            count,
            energyCost,
            resource == WorldResourceKind.Tree
                ? "notice.gathered_wood"
                : "notice.gathered_crystal"
        );
    }

    public ResourceSave Capture() => new()
    {
        RemovedNodes = _removed.Order(StringComparer.Ordinal).ToList()
    };
}
