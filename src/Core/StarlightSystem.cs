namespace Luminfield.Core;

public sealed record StarlightContributionResult(
    bool Succeeded,
    string MessageKey,
    int ContributedCount = 0,
    bool Activated = false
);

public sealed record StarlightProgressContext(
    IReadOnlySet<string> CompletedFestivalIds,
    IReadOnlySet<string> CompletedMilestoneIds,
    IReadOnlySet<string> CompletedPedestalIds
)
{
    public static StarlightProgressContext Empty { get; } = new(
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal)
    );

    public StarlightProgressContext(
        IReadOnlySet<string> completedFestivalIds
    ) : this(
        completedFestivalIds,
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal)
    )
    {
    }

    public StarlightProgressContext(
        IReadOnlySet<string> completedFestivalIds,
        IReadOnlySet<string> completedMilestoneIds
    ) : this(
        completedFestivalIds,
        completedMilestoneIds,
        new HashSet<string>(StringComparer.Ordinal)
    )
    {
    }
}

public sealed class StarlightSystem
{
    private Dictionary<string, StarlightPedestalSave> _states =
        CreateEmptyStates();

    // Compatibility members remain explicit aliases for the original woodland
    // pedestal so existing callers and schema-v1 saves keep their meaning.
    public StarlightPedestalDefinition Current => DataCatalog.WoodlandStarlight;
    public bool Discovered => IsDiscovered(DataCatalog.WoodlandStarlightId);
    public bool RewardUnlocked =>
        IsRewardUnlocked(DataCatalog.WoodlandStarlightId);
    public bool WoodlandRenewalUnlocked => RewardUnlocked;
    public bool HomesteadIrrigationUnlocked =>
        IsRewardUnlocked(DataCatalog.HomesteadStarlightId);
    public bool MeadowPollinationUnlocked =>
        IsRewardUnlocked(DataCatalog.MeadowStarlightId);
    public bool MoonwaterTideUnlocked =>
        IsRewardUnlocked(DataCatalog.MoonwaterStarlightId);
    public bool CrystalRuinsPassageUnlocked =>
        IsRewardUnlocked(DataCatalog.CrystalValeStarlightId);
    public bool StarfallSixfoldConvergenceUnlocked =>
        IsRewardUnlocked(DataCatalog.StarfallRuinsStarlightId);
    public int CompletedNodeCount =>
        CompletedNodeCountFor(DataCatalog.WoodlandStarlightId);

    public event Action? Changed;

    public StarlightPedestalDefinition Definition(string pedestalId) =>
        DataCatalog.StarlightPedestal(pedestalId);

    public bool IsDiscovered(string pedestalId) =>
        StateForPedestal(pedestalId).Discovered;

    public bool IsRewardUnlocked(string pedestalId) =>
        StateForPedestal(pedestalId).RewardUnlocked;

    public int CompletedNodeCountFor(
        string pedestalId,
        StarlightProgressContext? context = null
    ) =>
        Definition(pedestalId).Nodes.Count(node =>
            IsNodeComplete(pedestalId, node.Id, context)
        );

    public void Reset()
    {
        _states = CreateEmptyStates();
        Changed?.Invoke();
    }

    public void Restore(
        StarlightSave? save,
        StarlightProgressContext? context = null
    )
    {
        _states = StatesFromNormalized(NormalizeSave(save, context));
        Changed?.Invoke();
    }

    public void Discover() => Discover(DataCatalog.WoodlandStarlightId);

    public void Discover(string pedestalId)
    {
        var state = StateForPedestal(pedestalId);
        if (state.Discovered)
        {
            return;
        }

        state.Discovered = true;
        Changed?.Invoke();
    }

    public int ContributionCount(string nodeId, string itemId) =>
        ContributionCount(DataCatalog.WoodlandStarlightId, nodeId, itemId);

    public int ContributionCount(
        string pedestalId,
        string nodeId,
        string itemId
    )
    {
        RequireNode(pedestalId, nodeId);
        return StateForNode(pedestalId, nodeId).Contributions
            .Where(entry => entry.ItemId == itemId)
            .Sum(entry => entry.Count);
    }

    public int Progress(string nodeId) =>
        Progress(DataCatalog.WoodlandStarlightId, nodeId, null);

    public int Progress(
        string pedestalId,
        string nodeId,
        StarlightProgressContext? context = null
    )
    {
        var definition = RequireNode(pedestalId, nodeId);
        if (definition.SourceKind != StarlightNodeSourceKind.Inventory)
        {
            var completed = CompletedSourceIds(definition.SourceKind, context);
            return Math.Min(
                definition.RequiredCount,
                (definition.SourceIds ?? [])
                    .Distinct(StringComparer.Ordinal)
                    .Count(completed.Contains)
            );
        }

        return StateForNode(pedestalId, nodeId).Contributions
            .Sum(entry => entry.Count);
    }

    public bool IsNodeComplete(string nodeId) =>
        IsNodeComplete(DataCatalog.WoodlandStarlightId, nodeId);

    public bool IsNodeComplete(
        string pedestalId,
        string nodeId,
        StarlightProgressContext? context = null
    )
    {
        var definition = RequireNode(pedestalId, nodeId);
        return Progress(pedestalId, nodeId, context) >=
            definition.RequiredCount;
    }

    public bool CanContribute(string nodeId, Inventory inventory) =>
        CanContribute(DataCatalog.WoodlandStarlightId, nodeId, inventory);

    public bool CanContribute(
        string pedestalId,
        string nodeId,
        Inventory inventory,
        StarlightProgressContext? context = null
    ) => BuildAvailableContributions(
        pedestalId,
        nodeId,
        inventory,
        context
    ).Count > 0;

    public StarlightContributionResult Contribute(
        string nodeId,
        Inventory inventory
    ) => Contribute(DataCatalog.WoodlandStarlightId, nodeId, inventory);

    public StarlightContributionResult Contribute(
        string pedestalId,
        string nodeId,
        Inventory inventory,
        StarlightProgressContext? context = null
    )
    {
        if (!DataCatalog.StarlightPedestals.TryGetValue(
                pedestalId,
                out var pedestal) ||
            !pedestal.Nodes.Any(node => node.Id == nodeId))
        {
            return new StarlightContributionResult(
                false,
                "starlight.unknown_node"
            );
        }

        if (IsNodeComplete(pedestalId, nodeId, context))
        {
            return new StarlightContributionResult(
                false,
                "starlight.node_already_complete"
            );
        }

        var available = BuildAvailableContributions(
            pedestalId,
            nodeId,
            inventory,
            context
        );
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

        var state = StateForNode(pedestalId, nodeId);
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

        var pedestalState = StateForPedestal(pedestalId);
        pedestalState.Discovered = true;
        var activated = !pedestal.RequiresManualActivation &&
            !pedestalState.RewardUnlocked &&
            pedestal.Nodes.All(node =>
                IsNodeComplete(pedestalId, node.Id, context)
            );
        if (activated)
        {
            pedestalState.RewardUnlocked = true;
        }

        Changed?.Invoke();
        var messageKey = "starlight.contributed";
        if (activated)
        {
            messageKey = pedestal.ActivationMessageKey;
        }
        else if (IsNodeComplete(pedestalId, nodeId, context))
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

    public bool RefreshRewardUnlocks(
        StarlightProgressContext? context = null
    )
    {
        var changed = false;
        foreach (var pedestal in DataCatalog.StarlightPedestals.Values)
        {
            var state = StateForPedestal(pedestal.Id);
            if (pedestal.RequiresManualActivation ||
                state.RewardUnlocked || !pedestal.Nodes.All(node =>
                    IsNodeComplete(pedestal.Id, node.Id, context)))
            {
                continue;
            }

            state.RewardUnlocked = true;
            state.Discovered = true;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return changed;
    }

    public ActionResult CheckManualActivation(
        string pedestalId,
        StarlightProgressContext? context = null
    )
    {
        if (!DataCatalog.StarlightPedestals.TryGetValue(
                pedestalId,
                out var pedestal
            ) || !pedestal.RequiresManualActivation)
        {
            return ActionResult.Fail("starlight.activation_not_required");
        }

        if (IsRewardUnlocked(pedestalId))
        {
            return ActionResult.Fail("starlight.already_activated");
        }

        return pedestal.Nodes.All(node =>
            IsNodeComplete(pedestalId, node.Id, context)
        )
            ? ActionResult.Success(
                messageKey: pedestal.ActivationMessageKey
            )
            : ActionResult.Fail("starlight.activation_not_ready");
    }

    public ActionResult ActivateManually(
        string pedestalId,
        StarlightProgressContext? context = null
    )
    {
        var check = CheckManualActivation(pedestalId, context);
        if (!check.Succeeded)
        {
            return check;
        }

        var state = StateForPedestal(pedestalId);
        state.Discovered = true;
        state.RewardUnlocked = true;
        Changed?.Invoke();
        return check;
    }

    public StarlightSave Capture()
    {
        var pedestals = DataCatalog.StarlightPedestals.Values
            .Select(definition => CloneState(
                StateForPedestal(definition.Id)
            ))
            .ToList();
        var woodland = pedestals.First(state =>
            state.PedestalId == DataCatalog.WoodlandStarlightId
        );
        return new StarlightSave
        {
            PedestalId = woodland.PedestalId,
            Discovered = woodland.Discovered,
            RewardUnlocked = woodland.RewardUnlocked,
            Nodes = CloneNodes(woodland.Nodes),
            Pedestals = pedestals
        };
    }

    public static StarlightSave NormalizeSave(
        StarlightSave? save,
        StarlightProgressContext? context = null
    )
    {
        var normalizedPedestals = new List<StarlightPedestalSave>();
        foreach (var definition in DataCatalog.StarlightPedestals.Values)
        {
            normalizedPedestals.Add(NormalizePedestal(
                definition,
                CandidatesFor(definition, save),
                context
            ));
        }

        var completedPedestalIds = normalizedPedestals
            .Where(state => state.RewardUnlocked)
            .Select(state => state.PedestalId)
            .Concat(
                context?.CompletedPedestalIds ??
                    StarlightProgressContext.Empty.CompletedPedestalIds
            )
            .ToHashSet(StringComparer.Ordinal);
        var effectiveContext = new StarlightProgressContext(
            context?.CompletedFestivalIds ??
                StarlightProgressContext.Empty.CompletedFestivalIds,
            context?.CompletedMilestoneIds ??
                StarlightProgressContext.Empty.CompletedMilestoneIds,
            completedPedestalIds
        );
        foreach (var definition in DataCatalog.StarlightPedestals.Values
                     .Where(definition => definition.Nodes.Any(node =>
                         node.SourceKind ==
                            StarlightNodeSourceKind.PedestalRewards
                     )))
        {
            var index = normalizedPedestals.FindIndex(state =>
                state.PedestalId == definition.Id
            );
            normalizedPedestals[index] = NormalizePedestal(
                definition,
                CandidatesFor(definition, save),
                effectiveContext
            );
        }

        var woodland = normalizedPedestals.First(state =>
            state.PedestalId == DataCatalog.WoodlandStarlightId
        );
        return new StarlightSave
        {
            PedestalId = woodland.PedestalId,
            Discovered = woodland.Discovered,
            RewardUnlocked = woodland.RewardUnlocked,
            Nodes = CloneNodes(woodland.Nodes),
            Pedestals = normalizedPedestals
        };
    }

    private IReadOnlyList<StarlightContributionSave> BuildAvailableContributions(
        string pedestalId,
        string nodeId,
        Inventory inventory,
        StarlightProgressContext? context = null
    )
    {
        var definition = RequireNode(pedestalId, nodeId);
        if (definition.SourceKind != StarlightNodeSourceKind.Inventory)
        {
            return [];
        }

        var remainingTotal = definition.RequiredCount -
            Progress(pedestalId, nodeId, context);
        var available = new List<StarlightContributionSave>();
        foreach (var option in definition.Options)
        {
            if (remainingTotal <= 0)
            {
                break;
            }

            var alreadyContributed = ContributionCount(
                pedestalId,
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

    private static StarlightPedestalSave NormalizePedestal(
        StarlightPedestalDefinition definition,
        IReadOnlyList<StarlightPedestalSave> candidates,
        StarlightProgressContext? context
    )
    {
        var normalized = new StarlightPedestalSave
        {
            PedestalId = definition.Id,
            Discovered = candidates.Any(state => state.Discovered)
        };

        foreach (var node in definition.Nodes)
        {
            if (node.SourceKind != StarlightNodeSourceKind.Inventory)
            {
                normalized.Nodes.Add(new StarlightNodeSave
                {
                    NodeId = node.Id
                });
                continue;
            }

            var sourceCounts = candidates
                .SelectMany(state => state.Nodes ?? [])
                .Where(state => state.NodeId == node.Id)
                .SelectMany(state => state.Contributions ?? [])
                .Where(entry => entry.Count > 0)
                .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
                // Duplicate portfolio entries and the woodland legacy mirror
                // describe the same state, so retain the greatest valid count.
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(entry => entry.Count),
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

        var eligible = definition.Nodes.All(node =>
            node.SourceKind != StarlightNodeSourceKind.Inventory
                ? Math.Min(
                    node.RequiredCount,
                    (node.SourceIds ?? [])
                        .Distinct(StringComparer.Ordinal)
                        .Count(CompletedSourceIds(
                            node.SourceKind,
                            context
                        ).Contains)
                ) >= node.RequiredCount
                : normalized.Nodes
                    .First(state => state.NodeId == node.Id)
                    .Contributions
                    .Sum(entry => entry.Count) >= node.RequiredCount
        );
        normalized.RewardUnlocked = eligible &&
            (!definition.RequiresManualActivation ||
                candidates.Any(state => state.RewardUnlocked));
        if (normalized.RewardUnlocked)
        {
            normalized.Discovered = true;
        }
        return normalized;
    }

    private static IReadOnlyList<StarlightPedestalSave> CandidatesFor(
        StarlightPedestalDefinition definition,
        StarlightSave? save
    )
    {
        var candidates = (save?.Pedestals ?? [])
            .Where(state => state.PedestalId == definition.Id)
            .ToList();
        if (definition.Id == DataCatalog.WoodlandStarlightId &&
            save is not null)
        {
            candidates.Add(new StarlightPedestalSave
            {
                PedestalId = definition.Id,
                Discovered = save.Discovered,
                RewardUnlocked = save.RewardUnlocked,
                Nodes = save.Nodes ?? []
            });
        }

        return candidates;
    }

    private static IReadOnlySet<string> CompletedSourceIds(
        StarlightNodeSourceKind sourceKind,
        StarlightProgressContext? context
    ) => sourceKind switch
    {
        StarlightNodeSourceKind.FestivalResults =>
            context?.CompletedFestivalIds ??
                StarlightProgressContext.Empty.CompletedFestivalIds,
        StarlightNodeSourceKind.PedestalRewards =>
            context?.CompletedPedestalIds ??
                StarlightProgressContext.Empty.CompletedPedestalIds,
        _ => context?.CompletedMilestoneIds ??
            StarlightProgressContext.Empty.CompletedMilestoneIds
    };

    private StarlightPedestalSave StateForPedestal(string pedestalId)
    {
        _ = Definition(pedestalId);
        return _states[pedestalId];
    }

    private StarlightNodeSave StateForNode(
        string pedestalId,
        string nodeId
    ) => StateForPedestal(pedestalId).Nodes.First(node =>
        node.NodeId == nodeId
    );

    private StarlightNodeDefinition RequireNode(
        string pedestalId,
        string nodeId
    )
    {
        var pedestal = Definition(pedestalId);
        return pedestal.Nodes.FirstOrDefault(node => node.Id == nodeId)
            ?? throw new KeyNotFoundException(
                $"Starlight node '{nodeId}' does not belong to '{pedestalId}'."
            );
    }

    private static Dictionary<string, StarlightPedestalSave>
        CreateEmptyStates() => StatesFromNormalized(NormalizeSave(null));

    private static Dictionary<string, StarlightPedestalSave>
        StatesFromNormalized(StarlightSave save) => save.Pedestals
            .ToDictionary(
                state => state.PedestalId,
                CloneState,
                StringComparer.Ordinal
            );

    private static StarlightPedestalSave CloneState(
        StarlightPedestalSave state
    ) => new()
    {
        PedestalId = state.PedestalId,
        Discovered = state.Discovered,
        RewardUnlocked = state.RewardUnlocked,
        Nodes = CloneNodes(state.Nodes)
    };

    private static List<StarlightNodeSave> CloneNodes(
        IEnumerable<StarlightNodeSave> nodes
    ) => nodes.Select(node => new StarlightNodeSave
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
    }).ToList();
}
