namespace Luminfield.Core;

public sealed class CollectionSystem
{
    private readonly HashSet<string> _initializedCategoryIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _discoveredEntryIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _donatedEntryIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _claimedRewardIds =
        new(StringComparer.Ordinal);

    public bool Initialized => CompendiumCatalog.CategoryIds.All(
        _initializedCategoryIds.Contains
    );
    public IReadOnlySet<string> InitializedCategoryIds =>
        _initializedCategoryIds;
    public IReadOnlySet<string> DiscoveredEntryIds => _discoveredEntryIds;
    public IReadOnlySet<string> DonatedEntryIds => _donatedEntryIds;
    public IReadOnlySet<string> ClaimedRewardIds => _claimedRewardIds;

    public event Action<string>? EntryDiscovered;
    public event Action? Changed;

    public void Reset()
    {
        _initializedCategoryIds.Clear();
        _initializedCategoryIds.UnionWith(CompendiumCatalog.CategoryIds);
        _discoveredEntryIds.Clear();
        _donatedEntryIds.Clear();
        _claimedRewardIds.Clear();
        Changed?.Invoke();
    }

    public void Restore(
        CollectionSave? save,
        IEnumerable<string>? legacyEvidenceItemIds = null
    )
    {
        var normalized = NormalizeSave(save, legacyEvidenceItemIds);
        _initializedCategoryIds.Clear();
        _initializedCategoryIds.UnionWith(
            normalized.InitializedCategoryIds
        );
        _discoveredEntryIds.Clear();
        _discoveredEntryIds.UnionWith(normalized.DiscoveredEntryIds);
        _donatedEntryIds.Clear();
        _donatedEntryIds.UnionWith(normalized.DonatedEntryIds);
        _claimedRewardIds.Clear();
        _claimedRewardIds.UnionWith(normalized.ClaimedRewardIds);
        Changed?.Invoke();
    }

    public bool RecordObtainedItem(string itemId)
    {
        if (!CompendiumCatalog.TryResolveObtainedItem(itemId, out var entry) ||
            !_discoveredEntryIds.Add(entry.Id))
        {
            return false;
        }

        EntryDiscovered?.Invoke(entry.Id);
        Changed?.Invoke();
        return true;
    }

    public bool RecordDiscovery(string entryId)
    {
        if (!CompendiumCatalog.Entries.ContainsKey(entryId) ||
            !_discoveredEntryIds.Add(entryId))
        {
            return false;
        }

        EntryDiscovered?.Invoke(entryId);
        Changed?.Invoke();
        return true;
    }

    public bool ObserveInventory(Inventory inventory)
    {
        var newlyDiscovered = inventory.Slots
            .Where(slot => !slot.IsEmpty)
            .Select(slot => slot.ItemId)
            .Select(itemId =>
                CompendiumCatalog.TryResolveObtainedItem(itemId, out var entry)
                    ? entry.Id
                    : null
            )
            .Where(entryId => entryId is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Where(_discoveredEntryIds.Add)
            .ToArray();
        if (newlyDiscovered.Length == 0)
        {
            return false;
        }

        foreach (var entryId in newlyDiscovered)
        {
            EntryDiscovered?.Invoke(entryId);
        }
        Changed?.Invoke();
        return true;
    }

    public bool IsDiscovered(string entryId) =>
        _discoveredEntryIds.Contains(entryId);

    public bool IsDonated(string entryId) =>
        _donatedEntryIds.Contains(entryId);

    public int DonatedCount(string categoryId) =>
        CompendiumCatalog.Category(categoryId).EntryIds.Count(IsDonated);

    public ActionResult CheckDonateEntry(
        string entryId,
        Inventory inventory
    )
    {
        if (!CompendiumCatalog.Entries.TryGetValue(entryId, out var entry) ||
            entry.CategoryId != CollectionCategoryIds.Artifacts)
        {
            return ActionResult.Fail("collection.donation.unknown");
        }

        if (_donatedEntryIds.Contains(entryId))
        {
            return ActionResult.Fail("collection.donation.already_donated");
        }

        if (!_discoveredEntryIds.Contains(entryId) ||
            inventory.Count(entry.ItemId) <= 0)
        {
            return ActionResult.Fail("collection.donation.missing_item");
        }

        return ActionResult.Success(messageKey: "collection.donation.ready");
    }

    public ActionResult DonateEntry(string entryId, Inventory inventory)
    {
        var check = CheckDonateEntry(entryId, inventory);
        if (!check.Succeeded)
        {
            return check;
        }

        var itemId = CompendiumCatalog.Entry(entryId).ItemId;
        if (!inventory.Remove(itemId, 1))
        {
            return ActionResult.Fail("collection.donation.missing_item");
        }

        _donatedEntryIds.Add(entryId);
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "collection.donation.completed"
        );
    }

    public int DiscoveredCount(string categoryId) =>
        CompendiumCatalog.Category(categoryId).EntryIds.Count(IsDiscovered);

    public bool IsCategoryComplete(string categoryId)
    {
        var category = CompendiumCatalog.Category(categoryId);
        return category.EntryIds.Count > 0 &&
            category.EntryIds.All(IsDiscovered);
    }

    public bool IsRewardClaimed(string rewardId) =>
        _claimedRewardIds.Contains(rewardId);

    public ActionResult CheckClaimReward(string rewardId)
    {
        if (!CompendiumCatalog.Rewards.TryGetValue(rewardId, out var reward))
        {
            return ActionResult.Fail("collection.reward.unknown");
        }

        if (_claimedRewardIds.Contains(rewardId))
        {
            return ActionResult.Fail("collection.reward.already_claimed");
        }

        return reward.RequiredEntryIds.All(IsDiscovered)
            ? ActionResult.Success(
                messageKey: reward.ClaimedMessageKey
            )
            : ActionResult.Fail("collection.reward.not_ready");
    }

    public ActionResult ClaimReward(string rewardId)
    {
        var check = CheckClaimReward(rewardId);
        if (!check.Succeeded)
        {
            return check;
        }

        _claimedRewardIds.Add(rewardId);
        Changed?.Invoke();
        return check;
    }

    public CollectionSave Capture() => new()
    {
        Initialized = Initialized,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds
            .Where(_initializedCategoryIds.Contains)
            .ToList(),
        DiscoveredEntryIds = CompendiumCatalog.EntriesInOrder
            .Select(entry => entry.Id)
            .Where(_discoveredEntryIds.Contains)
            .ToList(),
        DonatedEntryIds = CompendiumCatalog.ArtifactEntries
            .Select(entry => entry.Id)
            .Where(_donatedEntryIds.Contains)
            .ToList(),
        ClaimedRewardIds = CompendiumCatalog.Rewards.Keys
            .Where(_claimedRewardIds.Contains)
            .ToList()
    };

    public static CollectionSave NormalizeSave(
        CollectionSave? save,
        IEnumerable<string>? legacyEvidenceItemIds = null
    )
    {
        save ??= new CollectionSave();
        var initializedCategories = new HashSet<string>(
            save.InitializedCategoryIds ?? [],
            StringComparer.Ordinal
        );
        initializedCategories.IntersectWith(CompendiumCatalog.CategoryIds);
        if (save.Initialized && initializedCategories.Count == 0)
        {
            initializedCategories.Add(CollectionCategoryIds.Crops);
        }

        var discovered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entryOrItemId in save.DiscoveredEntryIds ?? [])
        {
            if (CompendiumCatalog.Entries.ContainsKey(entryOrItemId))
            {
                discovered.Add(entryOrItemId);
            }
            else if (CompendiumCatalog.TryResolveObtainedItem(
                         entryOrItemId,
                         out var entry
                     ))
            {
                discovered.Add(entry.Id);
            }
        }

        var evidenceEntries = (legacyEvidenceItemIds ?? [])
            .Select(entryOrItemId =>
                CompendiumCatalog.Entries.TryGetValue(
                    entryOrItemId,
                    out var directEntry
                )
                    ? directEntry
                    : CompendiumCatalog.TryResolveObtainedItem(
                        entryOrItemId,
                        out var itemEntry
                    )
                        ? itemEntry
                        : null
            )
            .Where(entry => entry is not null)
            .Cast<CompendiumEntryDefinition>()
            .ToArray();
        foreach (var categoryId in CompendiumCatalog.CategoryIds)
        {
            if (initializedCategories.Contains(categoryId))
            {
                continue;
            }

            foreach (var entry in evidenceEntries.Where(entry =>
                         entry.CategoryId == categoryId
                     ))
            {
                discovered.Add(entry.Id);
            }
            initializedCategories.Add(categoryId);
        }

        var claimed = new HashSet<string>(
            save.ClaimedRewardIds ?? [],
            StringComparer.Ordinal
        );
        claimed.IntersectWith(CompendiumCatalog.Rewards.Keys);
        var donated = new HashSet<string>(
            save.DonatedEntryIds ?? [],
            StringComparer.Ordinal
        );
        donated.IntersectWith(
            CompendiumCatalog.ArtifactEntries.Select(entry => entry.Id)
        );
        discovered.UnionWith(donated);
        return new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds
                .Where(initializedCategories.Contains)
                .ToList(),
            DiscoveredEntryIds = CompendiumCatalog.EntriesInOrder
                .Select(entry => entry.Id)
                .Where(discovered.Contains)
                .ToList(),
            DonatedEntryIds = CompendiumCatalog.ArtifactEntries
                .Select(entry => entry.Id)
                .Where(donated.Contains)
                .ToList(),
            ClaimedRewardIds = CompendiumCatalog.Rewards.Keys
                .Where(claimed.Contains)
                .ToList()
        };
    }

    public static IEnumerable<string> LegacyEvidenceItemIds(GameSaveV1 save)
    {
        foreach (var itemId in (save.Inventory ?? [])
                     .Select(slot => slot.ItemId))
        {
            yield return itemId;
        }

        foreach (var itemId in (save.Storage?.Chests ?? [])
                     .SelectMany(chest => chest.Items ?? [])
                     .Select(slot => slot.ItemId))
        {
            yield return itemId;
        }

        foreach (var itemId in (save.Kitchen?.PantryItems ?? [])
                     .Select(slot => slot.ItemId))
        {
            yield return itemId;
        }

        foreach (var itemId in (save.Shipping?.Pending ?? [])
                     .Concat(save.Shipping?.LastSettlement?.Entries ?? [])
                     .Select(entry => entry.ItemId))
        {
            yield return itemId;
        }

        foreach (var fishId in save.Fishing?.CaughtFishIds ?? [])
        {
            yield return fishId;
        }

        foreach (var itemId in MiningSystem.EvidenceItemIds(save.Mining))
        {
            yield return itemId;
        }

        foreach (var entryId in StarfallRuinsTrialSystem.EvidenceEntryIds(
                     save.StarfallRuinsTrial
                 ))
        {
            yield return entryId;
        }

        foreach (var result in save.Festival?.Results ?? [])
        {
            foreach (var itemId in result.ItemIds ?? [])
            {
                yield return itemId;
            }
            yield return result.GiftItemId;
            yield return result.GiftRewardItemId;
        }

        foreach (var itemId in (save.Starlight?.Nodes ?? [])
                     .Concat((save.Starlight?.Pedestals ?? [])
                         .SelectMany(pedestal => pedestal.Nodes ?? []))
                     .SelectMany(node => node.Contributions ?? [])
                     .Select(contribution => contribution.ItemId))
        {
            yield return itemId;
        }

        var recipeIds = (save.Processor?.Machines ?? [])
            .Select(machine => machine.RecipeId)
            .Append(save.Processor?.RecipeId ?? string.Empty);
        foreach (var recipeId in recipeIds)
        {
            if (DataCatalog.ProcessorRecipes.TryGetValue(
                    recipeId,
                    out var recipe
                ))
            {
                yield return recipe.InputItemId;
            }
        }

        if ((save.Quest?.Harvested ?? 0) > 0)
        {
            yield return DataCatalog.StarbudId;
        }
    }
}
