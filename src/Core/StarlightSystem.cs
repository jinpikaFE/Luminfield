namespace Luminfield.Core;

public sealed record StarlightContributionResult(
    bool Succeeded,
    string MessageKey,
    int ContributedCount = 0,
    bool Activated = false
);

public sealed class StarlightSystem
{
    private StarlightSave _state = CreateEmpty();

    public StarlightPedestalDefinition Current =>
        DataCatalog.StarlightPedestal(_state.PedestalId);
    public bool Discovered => _state.Discovered;
    public bool RewardUnlocked => _state.RewardUnlocked;
    public bool WoodlandRenewalUnlocked => RewardUnlocked;
    public int CompletedNodeCount =>
        Current.Nodes.Count(node => IsNodeComplete(node.Id));

    public event Action? Changed;

    public void Reset()
    {
        _state = CreateEmpty();
        Changed?.Invoke();
    }

    public void Restore(StarlightSave? save)
    {
        _state = NormalizeSave(save);
        Changed?.Invoke();
    }

    public void Discover()
    {
        if (_state.Discovered)
        {
            return;
        }

        _state.Discovered = true;
        Changed?.Invoke();
    }

    public int ContributionCount(string nodeId, string itemId)
    {
        var node = StateFor(nodeId);
        return node.Contributions
            .Where(entry => entry.ItemId == itemId)
            .Sum(entry => entry.Count);
    }

    public int Progress(string nodeId) =>
        StateFor(nodeId).Contributions.Sum(entry => entry.Count);

    public bool IsNodeComplete(string nodeId)
    {
        var definition = DataCatalog.StarlightNode(nodeId);
        return Progress(nodeId) >= definition.RequiredCount;
    }

    public bool CanContribute(string nodeId, Inventory inventory) =>
        BuildAvailableContributions(nodeId, inventory).Count > 0;

    public StarlightContributionResult Contribute(
        string nodeId,
        Inventory inventory
    )
    {
        if (!DataCatalog.StarlightNodes.ContainsKey(nodeId))
        {
            return new StarlightContributionResult(
                false,
                "starlight.unknown_node"
            );
        }

        if (IsNodeComplete(nodeId))
        {
            return new StarlightContributionResult(
                false,
                "starlight.node_already_complete"
            );
        }

        var available = BuildAvailableContributions(nodeId, inventory);
        if (available.Count == 0)
        {
            return new StarlightContributionResult(
                false,
                "starlight.nothing_available"
            );
        }

        var removals = available
            .Select(entry => new CraftingIngredient(entry.ItemId, entry.Count))
            .ToArray();
        if (!inventory.TryRemoveFamilies(removals))
        {
            return new StarlightContributionResult(
                false,
                "starlight.nothing_available"
            );
        }

        var state = StateFor(nodeId);
        foreach (var entry in available)
        {
            var existing = state.Contributions.FirstOrDefault(value =>
                value.ItemId == entry.ItemId
            );
            if (existing is null)
            {
                state.Contributions.Add(new StarlightContributionSave
                {
                    ItemId = entry.ItemId,
                    Count = entry.Count
                });
            }
            else
            {
                existing.Count += entry.Count;
            }
        }

        _state.Discovered = true;
        var activated = !_state.RewardUnlocked &&
            Current.Nodes.All(node => IsNodeComplete(node.Id));
        if (activated)
        {
            _state.RewardUnlocked = true;
        }

        Changed?.Invoke();
        var messageKey = "starlight.contributed";
        if (activated)
        {
            messageKey = "starlight.activated";
        }
        else if (IsNodeComplete(nodeId))
        {
            messageKey = "starlight.node_completed";
        }

        return new StarlightContributionResult(
            true,
            messageKey,
            available.Sum(entry => entry.Count),
            activated
        );
    }

    public StarlightSave Capture() => new()
    {
        PedestalId = _state.PedestalId,
        Discovered = _state.Discovered,
        RewardUnlocked = _state.RewardUnlocked,
        Nodes = _state.Nodes.Select(node => new StarlightNodeSave
        {
            NodeId = node.NodeId,
            Contributions = node.Contributions
                .OrderBy(entry => entry.ItemId, StringComparer.Ordinal)
                .Select(entry => new StarlightContributionSave
                {
                    ItemId = entry.ItemId,
                    Count = entry.Count
                })
                .ToList()
        }).ToList()
    };

    public static StarlightSave NormalizeSave(StarlightSave? save)
    {
        var definition = DataCatalog.WoodlandStarlight;
        var normalized = new StarlightSave
        {
            PedestalId = definition.Id,
            Discovered = save?.Discovered == true
        };

        foreach (var node in definition.Nodes)
        {
            var source = save?.Nodes?
                .FirstOrDefault(value => value.NodeId == node.Id);
            var sourceCounts = (source?.Contributions ?? [])
                .Where(entry => entry.Count > 0)
                .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(entry => entry.Count),
                    StringComparer.Ordinal
                );
            var remaining = node.RequiredCount;
            var target = new StarlightNodeSave { NodeId = node.Id };
            foreach (var option in node.Options)
            {
                if (remaining <= 0 ||
                    !sourceCounts.TryGetValue(option.ItemId, out var count))
                {
                    continue;
                }

                var accepted = Math.Min(
                    remaining,
                    Math.Min(option.MaximumCount, count)
                );
                if (accepted <= 0)
                {
                    continue;
                }

                target.Contributions.Add(new StarlightContributionSave
                {
                    ItemId = option.ItemId,
                    Count = accepted
                });
                remaining -= accepted;
            }
            normalized.Nodes.Add(target);
        }

        normalized.RewardUnlocked = definition.Nodes.All(node =>
            normalized.Nodes
                .First(state => state.NodeId == node.Id)
                .Contributions
                .Sum(entry => entry.Count) >= node.RequiredCount
        );
        if (normalized.RewardUnlocked)
        {
            normalized.Discovered = true;
        }

        return normalized;
    }

    private IReadOnlyList<StarlightContributionSave> BuildAvailableContributions(
        string nodeId,
        Inventory inventory
    )
    {
        var definition = DataCatalog.StarlightNode(nodeId);
        var remainingTotal = definition.RequiredCount - Progress(nodeId);
        var available = new List<StarlightContributionSave>();
        foreach (var option in definition.Options)
        {
            if (remainingTotal <= 0)
            {
                break;
            }

            var alreadyContributed = ContributionCount(
                nodeId,
                option.ItemId
            );
            var optionRemaining = option.MaximumCount - alreadyContributed;
            var count = Math.Min(
                remainingTotal,
                Math.Min(
                    optionRemaining,
                    inventory.CountFamily(option.ItemId)
                )
            );
            if (count <= 0)
            {
                continue;
            }

            available.Add(new StarlightContributionSave
            {
                ItemId = option.ItemId,
                Count = count
            });
            remainingTotal -= count;
        }

        return available;
    }

    private StarlightNodeSave StateFor(string nodeId) =>
        _state.Nodes.First(node => node.NodeId == nodeId);

    private static StarlightSave CreateEmpty() =>
        NormalizeSave(null);
}
