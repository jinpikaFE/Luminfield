namespace Luminfield.Core;

public sealed record FishCollectionEntry(
    FishDefinition Fish,
    bool Caught
);

public sealed record FishingDonationEntry(
    FishDefinition Fish,
    bool Caught,
    bool Donated,
    int OwnedCount
);

public sealed record FishingDonationResult(
    bool Succeeded,
    string MessageKey,
    string FishItemId = "",
    int DonatedCount = 0
);

public sealed record FishingCollectionRewardDefinition(
    string Id,
    int RequiredCaughtCount,
    int RewardCoins,
    string RewardItemId,
    int RewardItemCount,
    string TitleKey,
    string DescriptionKey
);

public enum FishingCollectionRewardStatus
{
    Locked,
    Ready,
    Claimed
}

public sealed record FishingCollectionRewardSnapshot(
    FishingCollectionRewardDefinition Definition,
    int Progress,
    FishingCollectionRewardStatus Status
);

public sealed record FishingCollectionRewardClaimResult(
    bool Succeeded,
    string MessageKey,
    int RewardCoins = 0,
    string RewardItemId = "",
    int RewardItemCount = 0
);

public sealed class FishingSystem
{
    public const int CastEnergyCost = 4;
    public const string FirstWatersRewardId = "fish_reward_first_waters";
    public const string EightSpeciesRewardId = "fish_reward_eight_species";
    public const string SixteenSpeciesRewardId = "fish_reward_sixteen_species";
    public const string FullLedgerRewardId = "fish_reward_full_ledger";

    public static readonly IReadOnlyList<FishingCollectionRewardDefinition>
        CollectionRewardDefinitions =
        [
            new(
                FirstWatersRewardId,
                3,
                60,
                DataCatalog.CrystalShardId,
                2,
                "fishing.reward.first_waters.title",
                "fishing.reward.first_waters.description"
            ),
            new(
                EightSpeciesRewardId,
                8,
                90,
                DataCatalog.StarsoilFertilizerId,
                2,
                "fishing.reward.eight_species.title",
                "fishing.reward.eight_species.description"
            ),
            new(
                SixteenSpeciesRewardId,
                16,
                140,
                DataCatalog.MoonstonePathId,
                4,
                "fishing.reward.sixteen_species.title",
                "fishing.reward.sixteen_species.description"
            ),
            new(
                FullLedgerRewardId,
                24,
                240,
                DataCatalog.GlowcombHiveId,
                1,
                "fishing.reward.full_ledger.title",
                "fishing.reward.full_ledger.description"
            )
        ];

    private readonly HashSet<string> _caughtFishIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _claimedRewardIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _donatedFishIds =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CaughtFishIds => _caughtFishIds;
    public IReadOnlyCollection<string> ClaimedRewardIds => _claimedRewardIds;
    public IReadOnlyCollection<string> DonatedFishIds => _donatedFishIds;
    public int CaughtCount => _caughtFishIds.Count;
    public int DonatedCount => _donatedFishIds.Count;
    public int TotalFishCount => DataCatalog.Fishes.Count;

    public event Action? Changed;

    public void Reset()
    {
        _caughtFishIds.Clear();
        _claimedRewardIds.Clear();
        _donatedFishIds.Clear();
        Changed?.Invoke();
    }

    public void Restore(FishingSave? save)
    {
        _caughtFishIds.Clear();
        _claimedRewardIds.Clear();
        _donatedFishIds.Clear();

        foreach (var fishId in save?.CaughtFishIds ?? [])
        {
            if (DataCatalog.Fishes.ContainsKey(fishId))
            {
                _caughtFishIds.Add(fishId);
            }
        }

        foreach (var rewardId in save?.ClaimedRewardIds ?? [])
        {
            if (IsCollectionRewardId(rewardId))
            {
                _claimedRewardIds.Add(rewardId);
            }
        }

        foreach (var fishId in save?.DonatedFishIds ?? [])
        {
            if (DataCatalog.Fishes.ContainsKey(fishId))
            {
                _donatedFishIds.Add(fishId);
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
    public bool IsDonated(string fishId) => _donatedFishIds.Contains(fishId);

    public static bool IsCollectionRewardId(string rewardId) =>
        CollectionRewardDefinitions.Any(definition =>
            definition.Id == rewardId
        );

    public IReadOnlyList<FishingCollectionRewardSnapshot> RewardSnapshots() =>
        CollectionRewardDefinitions
            .Select(definition =>
            {
                var status = RewardStatus(definition);
                return new FishingCollectionRewardSnapshot(
                    definition,
                    Math.Min(CaughtCount, definition.RequiredCaughtCount),
                    status
                );
            })
            .ToArray();

    public FishingCollectionRewardClaimResult ClaimReward(
        string rewardId,
        Inventory inventory
    )
    {
        var definition = CollectionRewardDefinitions.FirstOrDefault(
            reward => reward.Id == rewardId
        );
        if (definition is null)
        {
            return new FishingCollectionRewardClaimResult(
                false,
                "fishing.reward.unknown"
            );
        }

        var status = RewardStatus(definition);
        if (status == FishingCollectionRewardStatus.Claimed)
        {
            return new FishingCollectionRewardClaimResult(
                false,
                "fishing.reward.already_claimed"
            );
        }

        if (status != FishingCollectionRewardStatus.Ready)
        {
            return new FishingCollectionRewardClaimResult(
                false,
                "fishing.reward.not_ready"
            );
        }

        if (!string.IsNullOrWhiteSpace(definition.RewardItemId) &&
            definition.RewardItemCount > 0 &&
            !inventory.Add(
                definition.RewardItemId,
                definition.RewardItemCount
            ))
        {
            return new FishingCollectionRewardClaimResult(
                false,
                "notice.inventory_full"
            );
        }

        _claimedRewardIds.Add(definition.Id);
        Changed?.Invoke();
        return new FishingCollectionRewardClaimResult(
            true,
            "fishing.reward.claimed",
            definition.RewardCoins,
            definition.RewardItemId,
            definition.RewardItemCount
        );
    }

    public IReadOnlyList<FishCollectionEntry> CollectionEntries() =>
        DataCatalog.FishItemIds
            .Select(fishId => DataCatalog.Fishes[fishId])
            .Select(fish => new FishCollectionEntry(
                fish,
                _caughtFishIds.Contains(fish.Id)
            ))
            .ToArray();

    public IReadOnlyList<FishingDonationEntry> DonationEntries(
        Inventory inventory
    ) => DataCatalog.FishItemIds
        .Select(fishId => DataCatalog.Fishes[fishId])
        .Select(fish => new FishingDonationEntry(
            fish,
            _caughtFishIds.Contains(fish.Id),
            _donatedFishIds.Contains(fish.Id),
            inventory.Count(fish.ItemId)
        ))
        .ToArray();

    public FishingDonationResult DonateFish(
        string fishId,
        Inventory inventory
    )
    {
        if (!DataCatalog.Fishes.TryGetValue(fishId, out var fish))
        {
            return new FishingDonationResult(
                false,
                "fishing.donation.unknown"
            );
        }

        if (_donatedFishIds.Contains(fish.Id))
        {
            return new FishingDonationResult(
                false,
                "fishing.donation.already_donated"
            );
        }

        if (!_caughtFishIds.Contains(fish.Id))
        {
            return new FishingDonationResult(
                false,
                "fishing.donation.not_discovered"
            );
        }

        if (inventory.Count(fish.ItemId) <= 0 ||
            !inventory.Remove(fish.ItemId, 1))
        {
            return new FishingDonationResult(
                false,
                "fishing.donation.missing_fish"
            );
        }

        _donatedFishIds.Add(fish.Id);
        Changed?.Invoke();
        return new FishingDonationResult(
            true,
            "fishing.donation.donated",
            fish.ItemId,
            1
        );
    }

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
            .ToList(),
        ClaimedRewardIds = _claimedRewardIds
            .Order(StringComparer.Ordinal)
            .ToList(),
        DonatedFishIds = _donatedFishIds
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

    private FishingCollectionRewardStatus RewardStatus(
        FishingCollectionRewardDefinition definition
    )
    {
        if (_claimedRewardIds.Contains(definition.Id))
        {
            return FishingCollectionRewardStatus.Claimed;
        }

        return CaughtCount >= definition.RequiredCaughtCount
            ? FishingCollectionRewardStatus.Ready
            : FishingCollectionRewardStatus.Locked;
    }
}
