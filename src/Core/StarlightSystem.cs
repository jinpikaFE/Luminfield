namespace Luminfield.Core;

public sealed record StarlightContributionResult(
    bool Succeeded,
    string MessageKey,
    int ContributedCount = 0,
    bool Activated = false,
    string PedestalId = ""
);

public sealed class StarlightSystem
{
    private StarlightSave _state = CreateEmpty();

    public StarlightPedestalDefinition Current =>
        Pedestal(DataCatalog.WoodlandStarlightId);
    public bool Discovered => IsDiscovered(DataCatalog.WoodlandStarlightId);
    public bool RewardUnlocked =>
        IsRewardUnlocked(DataCatalog.WoodlandStarlightId);
    public bool WoodlandRenewalUnlocked => RewardUnlocked;
    public bool MoonwaterTideUnlocked =>
        IsRewardUnlocked(DataCatalog.MoonwaterStarlightId);
    public int CompletedNodeCount =>
        CompletedNodeCountFor(DataCatalog.WoodlandStarlightId);

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
        Discover(DataCatalog.WoodlandStarlightId);
    }

    public void Discover(string pedestalId)
    {
        var state = StateForPedestal(pedestalId);
        if (state.Discovered)
        {
            return;
        }

        state.Discovered = true;
        MirrorWoodlandLegacy();
        Changed?.Invoke();
    }

    public StarlightPedestalDefinition Pedestal(string pedestalId) =>
        DataCatalog.StarlightPedestal(pedestalId);

    public bool IsDiscovered(string pedestalId) =>
        StateForPedestal(pedestalId).Discovered;

    public bool IsRewardUnlocked(string pedestalId) =>
        StateForPedestal(pedestalId).RewardUnlocked;

    public int CompletedNodeCountFor(string pedestalId)
    {
        var pedestal = DataCatalog.StarlightPedestal(pedestalId);
        return pedestal.Nodes.Count(node => IsNodeComplete(node.Id));
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
        var pedestalId = PedestalIdForNode(nodeId);
        var pedestalState = StateForPedestal(pedestalId);
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

        pedestalState.Discovered = true;
        var pedestal = DataCatalog.StarlightPedestal(pedestalId);
        var activated = !pedestalState.RewardUnlocked &&
            pedestal.Nodes.All(node => IsNodeComplete(node.Id));
        if (activated)
        {
            pedestalState.RewardUnlocked = true;
        }

        MirrorWoodlandLegacy();
        Changed?.Invoke();
        var messageKey = "starlight.contributed";
        if (activated)
        {
            messageKey = ActivationMessageKey(pedestalId);
        }
        else if (IsNodeComplete(nodeId))
        {
            messageKey = "starlight.node_completed";
        }

        return new StarlightContributionResult(
            true,
            messageKey,
            available.Sum(entry => entry.Count),
            activated,
            pedestalId
        );
    }

    public StarlightSave Capture() =>
        BuildSave(_state.Pedestals.Select(ClonePedestal));

    public static StarlightSave NormalizeSave(StarlightSave? save)
    {
        var sourcePedestals = SourcePedestals(save);
        var pedestals = DataCatalog.StarlightPedestals.Values
            .Select(definition =>
            {
                sourcePedestals.TryGetValue(definition.Id, out var source);
                return NormalizePedestal(definition, source);
            });

        return BuildSave(pedestals);
    }

    public static string OpenedMessageKey(string pedestalId)
    {
        if (pedestalId == DataCatalog.MoonwaterStarlightId)
        {
            return "starlight.opened.moonwater";
        }

        return "starlight.opened";
    }

    private static StarlightPedestalSave NormalizePedestal(
        StarlightPedestalDefinition definition,
        StarlightPedestalSave? source
    )
    {
        var normalized = new StarlightPedestalSave
        {
            PedestalId = definition.Id,
            Discovered = source?.Discovered == true
        };
        foreach (var node in definition.Nodes)
        {
            var sourceNode = source?.Nodes?
                .FirstOrDefault(value => value.NodeId == node.Id);
            var sourceCounts = (sourceNode?.Contributions ?? [])
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

    private static IReadOnlyDictionary<string, StarlightPedestalSave>
        SourcePedestals(StarlightSave? save)
    {
        var sources = new Dictionary<string, StarlightPedestalSave>(
            StringComparer.Ordinal
        );
        if (save?.Pedestals is { Count: > 0 })
        {
            foreach (var pedestal in save.Pedestals)
            {
                if (!DataCatalog.StarlightPedestals.ContainsKey(
                        pedestal.PedestalId
                    ))
                {
                    continue;
                }

                sources[pedestal.PedestalId] = pedestal;
            }
        }

        var legacy = LegacyWoodlandSource(save);
        if (!sources.ContainsKey(DataCatalog.WoodlandStarlightId) &&
            legacy is not null)
        {
            sources[DataCatalog.WoodlandStarlightId] = legacy;
        }

        return sources;
    }

    private static StarlightPedestalSave? LegacyWoodlandSource(
        StarlightSave? save
    )
    {
        if (save is null)
        {
            return null;
        }

        return new StarlightPedestalSave
        {
            PedestalId = DataCatalog.WoodlandStarlightId,
            Discovered = save.Discovered,
            RewardUnlocked = save.RewardUnlocked,
            Nodes = save.Nodes
        };
    }

    private static StarlightSave BuildSave(
        IEnumerable<StarlightPedestalSave> pedestals
    )
    {
        var sources = pedestals.ToList();
        var ordered = DataCatalog.StarlightPedestals.Keys
            .Select(pedestalId => sources
                .First(pedestal => pedestal.PedestalId == pedestalId))
            .Select(ClonePedestal)
            .ToList();
        var woodland = ordered.First(pedestal =>
            pedestal.PedestalId == DataCatalog.WoodlandStarlightId
        );
        return new StarlightSave
        {
            PedestalId = woodland.PedestalId,
            Discovered = woodland.Discovered,
            RewardUnlocked = woodland.RewardUnlocked,
            Nodes = CloneNodes(woodland.Nodes),
            Pedestals = ordered
        };
    }

    private static StarlightPedestalSave ClonePedestal(
        StarlightPedestalSave source
    ) => new()
    {
        PedestalId = source.PedestalId,
        Discovered = source.Discovered,
        RewardUnlocked = source.RewardUnlocked,
        Nodes = CloneNodes(source.Nodes)
    };

    private static List<StarlightNodeSave> CloneNodes(
        IEnumerable<StarlightNodeSave> nodes
    ) => nodes
        .Select(node => new StarlightNodeSave
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
        })
        .ToList();

    private static string ActivationMessageKey(string pedestalId)
    {
        if (pedestalId == DataCatalog.MoonwaterStarlightId)
        {
            return "starlight.activated.moonwater";
        }

        return "starlight.activated";
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

    private StarlightPedestalSave StateForPedestal(string pedestalId)
    {
        DataCatalog.StarlightPedestal(pedestalId);
        return _state.Pedestals.First(pedestal =>
            pedestal.PedestalId == pedestalId
        );
    }

    private StarlightNodeSave StateFor(string nodeId)
    {
        var pedestalId = PedestalIdForNode(nodeId);
        return StateForPedestal(pedestalId)
            .Nodes
            .First(node => node.NodeId == nodeId);
    }

    private static string PedestalIdForNode(string nodeId)
    {
        foreach (var pedestal in DataCatalog.StarlightPedestals.Values)
        {
            if (pedestal.Nodes.Any(node => node.Id == nodeId))
            {
                return pedestal.Id;
            }
        }

        throw new KeyNotFoundException(
            $"Unknown starlight node id '{nodeId}'."
        );
    }

    private void MirrorWoodlandLegacy()
    {
        var woodland = StateForPedestal(DataCatalog.WoodlandStarlightId);
        _state.PedestalId = woodland.PedestalId;
        _state.Discovered = woodland.Discovered;
        _state.RewardUnlocked = woodland.RewardUnlocked;
        _state.Nodes = CloneNodes(woodland.Nodes);
    }

    private static StarlightSave CreateEmpty() =>
        NormalizeSave(null);
}
