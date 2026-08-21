namespace Luminfield.Core;

public sealed record ToolTierDefinition(
    string Id,
    int Rank,
    string NameKey
);

public sealed record ToolUpgradeDefinition(
    string Id,
    string ToolId,
    string FromTierId,
    string ToTierId,
    int CoinCost,
    IReadOnlyList<CraftingIngredient> Materials,
    int RequiredNights,
    string NameKey
);

public static class ToolProgressionCatalog
{
    public const string BasicTierId = "tool_tier_basic";
    public const string BronzeStarTierId = "tool_tier_bronze_star";
    public const string ShovelBronzeStarUpgradeId =
        "tool_upgrade_shovel_bronze_star";

    public static ToolTierDefinition BasicTier { get; } = new(
        BasicTierId,
        0,
        "tool.tier.basic"
    );

    public static ToolTierDefinition BronzeStarTier { get; } = new(
        BronzeStarTierId,
        1,
        "tool.tier.bronze_star"
    );

    public static IReadOnlyList<ToolTierDefinition> Tiers { get; } =
        Array.AsReadOnly([BasicTier, BronzeStarTier]);

    public static ToolUpgradeDefinition ShovelBronzeStarUpgrade { get; } =
        new(
            ShovelBronzeStarUpgradeId,
            DataCatalog.ShovelId,
            BasicTierId,
            BronzeStarTierId,
            420,
            Array.AsReadOnly(
            [
                new CraftingIngredient(DataCatalog.LumenSlateOreId, 6),
                new CraftingIngredient(DataCatalog.MoonveinOreId, 3)
            ]),
            2,
            "tool.upgrade.shovel_bronze_star"
        );

    public static IReadOnlyList<ToolUpgradeDefinition> Upgrades { get; } =
        Array.AsReadOnly([ShovelBronzeStarUpgrade]);

    private static readonly IReadOnlyDictionary<string, ToolTierDefinition>
        TiersById = Tiers.ToDictionary(tier => tier.Id, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, ToolUpgradeDefinition>
        UpgradesById = Upgrades.ToDictionary(
            upgrade => upgrade.Id,
            StringComparer.Ordinal
        );

    public static ToolTierDefinition Tier(string tierId) =>
        TiersById.TryGetValue(tierId, out var tier)
            ? tier
            : throw new KeyNotFoundException(
                $"Unknown tool tier '{tierId}'."
            );

    public static bool TryTier(
        string? tierId,
        out ToolTierDefinition tier
    ) => TiersById.TryGetValue(tierId ?? string.Empty, out tier!);

    public static ToolUpgradeDefinition Upgrade(string upgradeId) =>
        UpgradesById.TryGetValue(upgradeId, out var upgrade)
            ? upgrade
            : throw new KeyNotFoundException(
                $"Unknown tool upgrade '{upgradeId}'."
            );

    public static bool TryUpgrade(
        string? upgradeId,
        out ToolUpgradeDefinition upgrade
    ) => UpgradesById.TryGetValue(upgradeId ?? string.Empty, out upgrade!);
}

public sealed class ToolProgressionSystem
{
    private string _shovelTierId = ToolProgressionCatalog.BasicTierId;
    private string _activeUpgradeId = string.Empty;
    private int _remainingNights;

    public event Action? Changed;

    public string TierIdFor(string toolId) => toolId == DataCatalog.ShovelId
        ? _shovelTierId
        : ToolProgressionCatalog.BasicTierId;

    public int TierRankFor(string toolId) =>
        ToolProgressionCatalog.Tier(TierIdFor(toolId)).Rank;

    public string ActiveUpgradeId => _activeUpgradeId;
    public int RemainingNights => _remainingNights;
    public bool IsUpgradeInProgress =>
        !string.IsNullOrWhiteSpace(_activeUpgradeId);

    public bool IsUpgradeCompleted(string upgradeId) =>
        ToolProgressionCatalog.TryUpgrade(upgradeId, out var upgrade) &&
        TierRankFor(upgrade.ToolId) >=
        ToolProgressionCatalog.Tier(upgrade.ToTierId).Rank;

    public void Reset()
    {
        _shovelTierId = ToolProgressionCatalog.BasicTierId;
        _activeUpgradeId = string.Empty;
        _remainingNights = 0;
        Changed?.Invoke();
    }

    public void Restore(ToolProgressionSave? save)
    {
        var normalized = NormalizeSave(save);
        var shovel = normalized.Tools.Single(entry =>
            entry.ToolId == DataCatalog.ShovelId
        );
        _shovelTierId = shovel.TierId;
        _activeUpgradeId = shovel.ActiveUpgradeId;
        _remainingNights = shovel.RemainingNights;
        Changed?.Invoke();
    }

    public ActionResult CheckStartUpgrade(
        string upgradeId,
        Inventory inventory,
        int coins
    )
    {
        if (!ToolProgressionCatalog.TryUpgrade(upgradeId, out var upgrade))
        {
            return ActionResult.Fail("tool.upgrade.unknown");
        }

        if (IsUpgradeInProgress)
        {
            return ActionResult.Fail("tool.upgrade.in_progress");
        }

        if (IsUpgradeCompleted(upgradeId) ||
            TierIdFor(upgrade.ToolId) != upgrade.FromTierId)
        {
            return ActionResult.Fail("tool.upgrade.already_completed");
        }

        if (coins < upgrade.CoinCost)
        {
            return ActionResult.Fail("tool.upgrade.insufficient_coins");
        }

        return upgrade.Materials.All(material =>
            inventory.CountFamily(material.ItemId) >= material.Count
        )
            ? ActionResult.Success(messageKey: "tool.upgrade.ready")
            : ActionResult.Fail("tool.upgrade.insufficient_materials");
    }

    public void BeginCheckedUpgrade(string upgradeId)
    {
        var upgrade = ToolProgressionCatalog.Upgrade(upgradeId);
        if (IsUpgradeInProgress ||
            TierIdFor(upgrade.ToolId) != upgrade.FromTierId)
        {
            throw new InvalidOperationException(
                $"Tool upgrade '{upgradeId}' is no longer startable."
            );
        }

        _activeUpgradeId = upgrade.Id;
        _remainingNights = upgrade.RequiredNights;
        Changed?.Invoke();
    }

    public string? ResolveNight()
    {
        if (!IsUpgradeInProgress)
        {
            return null;
        }

        _remainingNights--;
        if (_remainingNights > 0)
        {
            Changed?.Invoke();
            return null;
        }

        var completedId = _activeUpgradeId;
        var upgrade = ToolProgressionCatalog.Upgrade(completedId);
        _shovelTierId = upgrade.ToTierId;
        _activeUpgradeId = string.Empty;
        _remainingNights = 0;
        Changed?.Invoke();
        return completedId;
    }

    public IReadOnlySet<string> CompletedMilestoneIds() =>
        ToolProgressionCatalog.Upgrades
            .Where(upgrade => IsUpgradeCompleted(upgrade.Id))
            .Select(upgrade => upgrade.Id)
            .ToHashSet(StringComparer.Ordinal);

    public ToolProgressionSave Capture() => new()
    {
        Tools =
        [
            new ToolProgressionEntrySave
            {
                ToolId = DataCatalog.ShovelId,
                TierId = _shovelTierId,
                ActiveUpgradeId = _activeUpgradeId,
                RemainingNights = _remainingNights
            }
        ]
    };

    public static ToolProgressionSave NormalizeSave(
        ToolProgressionSave? save
    )
    {
        var candidates = (save?.Tools ?? [])
            .Where(entry => entry.ToolId == DataCatalog.ShovelId)
            .ToArray();
        var completed = candidates.Any(entry =>
            entry.TierId == ToolProgressionCatalog.BronzeStarTierId
        );
        var activeRemaining = candidates
            .Where(entry =>
                entry.ActiveUpgradeId ==
                    ToolProgressionCatalog.ShovelBronzeStarUpgradeId &&
                entry.RemainingNights > 0
            )
            .Select(entry => Math.Clamp(
                entry.RemainingNights,
                1,
                ToolProgressionCatalog.ShovelBronzeStarUpgrade.RequiredNights
            ))
            .DefaultIfEmpty(0)
            .Min();

        return new ToolProgressionSave
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = completed
                        ? ToolProgressionCatalog.BronzeStarTierId
                        : ToolProgressionCatalog.BasicTierId,
                    ActiveUpgradeId = !completed && activeRemaining > 0
                        ? ToolProgressionCatalog.ShovelBronzeStarUpgradeId
                        : string.Empty,
                    RemainingNights = !completed ? activeRemaining : 0
                }
            ]
        };
    }

    public static IReadOnlySet<string> CompletedMilestoneIds(
        ToolProgressionSave? save
    )
    {
        var normalized = NormalizeSave(save);
        return normalized.Tools.Any(entry =>
            entry.ToolId == DataCatalog.ShovelId &&
            entry.TierId == ToolProgressionCatalog.BronzeStarTierId
        )
            ? new HashSet<string>(StringComparer.Ordinal)
            {
                ToolProgressionCatalog.ShovelBronzeStarUpgradeId
            }
            : new HashSet<string>(StringComparer.Ordinal);
    }
}
