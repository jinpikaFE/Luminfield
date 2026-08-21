namespace Luminfield.Core;

public enum FishingGearOfferKind
{
    Bait,
    Bobber,
    CrabPot
}

public sealed record FishingRodTierDefinition(
    string Id,
    int Rank,
    string NameKey,
    int RequiredLevel,
    int CoinCost,
    IReadOnlyList<CraftingIngredient> Materials
);

public sealed record FishingGearOfferDefinition(
    string ItemId,
    FishingGearOfferKind Kind,
    int Quantity,
    int RequiredLevel,
    int CoinCost,
    IReadOnlyList<CraftingIngredient> Materials
);

public static class FishingProgressionCatalog
{
    public const string ReedstarRodTierId = "fishing_rod_reedstar";
    public const string MoonthreadRodTierId = "fishing_rod_moonthread";
    public const string TideglassRodTierId = "fishing_rod_tideglass";
    public const string CurrentListenerSpecializationId =
        "fishing_specialization_current_listener";
    public const string DeepThreaderSpecializationId =
        "fishing_specialization_deep_threader";

    public static IReadOnlyList<int> LevelThresholds { get; } =
        [0, 25, 70, 140, 240, 380];

    public static IReadOnlyList<FishingRodTierDefinition> RodTiers { get; } =
        Array.AsReadOnly(
        [
            new FishingRodTierDefinition(
                ReedstarRodTierId,
                0,
                "fishing.rod.reedstar",
                0,
                0,
                []
            ),
            new FishingRodTierDefinition(
                MoonthreadRodTierId,
                1,
                "fishing.rod.moonthread",
                2,
                280,
                Array.AsReadOnly(
                [
                    new CraftingIngredient(DataCatalog.CrystalShardId, 4)
                ])
            ),
            new FishingRodTierDefinition(
                TideglassRodTierId,
                2,
                "fishing.rod.tideglass",
                4,
                680,
                Array.AsReadOnly(
                [
                    new CraftingIngredient(DataCatalog.MoonveinOreId, 4),
                    new CraftingIngredient(DataCatalog.CrystalShardId, 6)
                ])
            )
        ]);

    public static IReadOnlyList<FishingGearOfferDefinition> GearOffers
        { get; } = Array.AsReadOnly(
        [
            new FishingGearOfferDefinition(
                DataCatalog.GlowgrubBaitId,
                FishingGearOfferKind.Bait,
                5,
                0,
                45,
                []
            ),
            new FishingGearOfferDefinition(
                DataCatalog.MoonmoteBaitId,
                FishingGearOfferKind.Bait,
                3,
                2,
                95,
                []
            ),
            new FishingGearOfferDefinition(
                DataCatalog.StillwaterBobberId,
                FishingGearOfferKind.Bobber,
                1,
                1,
                180,
                Array.AsReadOnly(
                [
                    new CraftingIngredient(DataCatalog.CrystalShardId, 2)
                ])
            ),
            new FishingGearOfferDefinition(
                DataCatalog.StormglassBobberId,
                FishingGearOfferKind.Bobber,
                1,
                3,
                420,
                Array.AsReadOnly(
                [
                    new CraftingIngredient(DataCatalog.MoonveinOreId, 2)
                ])
            ),
            new FishingGearOfferDefinition(
                DataCatalog.MoonreedCrabPotId,
                FishingGearOfferKind.CrabPot,
                1,
                1,
                160,
                Array.AsReadOnly(
                [
                    new CraftingIngredient(DataCatalog.LumenwoodId, 4)
                ])
            )
        ]);

    public static IReadOnlySet<string> BaitItemIds { get; } =
        new HashSet<string>(
        [
            DataCatalog.GlowgrubBaitId,
            DataCatalog.MoonmoteBaitId
        ], StringComparer.Ordinal);

    public static IReadOnlySet<string> BobberItemIds { get; } =
        new HashSet<string>(
        [
            DataCatalog.StillwaterBobberId,
            DataCatalog.StormglassBobberId
        ], StringComparer.Ordinal);

    public static FishingRodTierDefinition RodTier(string tierId) =>
        RodTiers.FirstOrDefault(tier => tier.Id == tierId) ??
        throw new KeyNotFoundException(
            $"Unknown fishing rod tier '{tierId}'."
        );

    public static FishingGearOfferDefinition GearOffer(string itemId) =>
        GearOffers.FirstOrDefault(offer => offer.ItemId == itemId) ??
        throw new KeyNotFoundException(
            $"Unknown fishing gear offer '{itemId}'."
        );
}

public sealed class FishingProgressionSystem
{
    private readonly HashSet<string> _ownedBobberIds =
        new(StringComparer.Ordinal);

    public string RodTierId { get; private set; } =
        FishingProgressionCatalog.ReedstarRodTierId;
    public IReadOnlySet<string> OwnedBobberIds => _ownedBobberIds;
    public string EquippedBaitId { get; private set; } = string.Empty;
    public string EquippedBobberId { get; private set; } = string.Empty;
    public int Experience { get; private set; }
    public int Level { get; private set; }
    public string SpecializationId { get; private set; } = string.Empty;
    public bool CanChooseSpecialization =>
        Level >= 3 && string.IsNullOrWhiteSpace(SpecializationId);
    public FishingRodTierDefinition RodTier =>
        FishingProgressionCatalog.RodTier(RodTierId);

    public float CatchZoneBonus =>
        RodTier.Rank * 0.035f +
        (EquippedBobberId == DataCatalog.StillwaterBobberId ? 0.08f : 0) +
        (SpecializationId ==
            FishingProgressionCatalog.CurrentListenerSpecializationId
                ? 0.07f
                : 0);

    public float ProgressRateBonus =>
        (EquippedBaitId == DataCatalog.MoonmoteBaitId ? 0.12f : 0) +
        (SpecializationId ==
            FishingProgressionCatalog.DeepThreaderSpecializationId
                ? 0.16f
                : 0);

    public float TensionRecoveryBonus =>
        EquippedBobberId == DataCatalog.StormglassBobberId ? 0.08f : 0;

    public event Action? Changed;

    public void Reset()
    {
        RodTierId = FishingProgressionCatalog.ReedstarRodTierId;
        _ownedBobberIds.Clear();
        EquippedBaitId = string.Empty;
        EquippedBobberId = string.Empty;
        Experience = 0;
        Level = 0;
        SpecializationId = string.Empty;
        Changed?.Invoke();
    }

    public void Restore(FishingSave? save)
    {
        RodTierId = FishingProgressionCatalog.RodTiers.Any(tier =>
            tier.Id == save?.RodTierId
        )
            ? save!.RodTierId
            : FishingProgressionCatalog.ReedstarRodTierId;
        _ownedBobberIds.Clear();
        foreach (var bobberId in save?.OwnedBobberIds ?? [])
        {
            if (FishingProgressionCatalog.BobberItemIds.Contains(bobberId))
            {
                _ownedBobberIds.Add(bobberId);
            }
        }

        EquippedBaitId = FishingProgressionCatalog.BaitItemIds.Contains(
            save?.EquippedBaitId ?? string.Empty
        )
            ? save!.EquippedBaitId
            : string.Empty;
        EquippedBobberId = _ownedBobberIds.Contains(
            save?.EquippedBobberId ?? string.Empty
        )
            ? save!.EquippedBobberId
            : string.Empty;
        Experience = Math.Clamp(save?.Experience ?? 0, 0, 999999);
        Level = LevelForExperience(Experience);
        SpecializationId = IsSpecializationId(save?.SpecializationId)
            ? save!.SpecializationId
            : string.Empty;
        if (Level < 3)
        {
            SpecializationId = string.Empty;
        }

        Changed?.Invoke();
    }

    public void CaptureInto(FishingSave save)
    {
        save.RodTierId = RodTierId;
        save.OwnedBobberIds = _ownedBobberIds
            .Order(StringComparer.Ordinal)
            .ToList();
        save.EquippedBaitId = EquippedBaitId;
        save.EquippedBobberId = EquippedBobberId;
        save.Experience = Experience;
        save.Level = Level;
        save.SpecializationId = SpecializationId;
    }

    public bool OwnsBobber(string itemId) => _ownedBobberIds.Contains(itemId);

    public ActionResult EquipBait(string itemId, Inventory inventory)
    {
        if (!FishingProgressionCatalog.BaitItemIds.Contains(itemId))
        {
            return ActionResult.Fail("fishing.gear.unknown");
        }

        if (inventory.Count(itemId) <= 0)
        {
            return ActionResult.Fail("fishing.gear.bait_missing");
        }

        EquippedBaitId = itemId;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "fishing.gear.bait_equipped");
    }

    public ActionResult EquipBobber(string itemId)
    {
        if (!FishingProgressionCatalog.BobberItemIds.Contains(itemId))
        {
            return ActionResult.Fail("fishing.gear.unknown");
        }

        if (!_ownedBobberIds.Contains(itemId))
        {
            return ActionResult.Fail("fishing.gear.not_owned");
        }

        EquippedBobberId = itemId;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "fishing.gear.bobber_equipped");
    }

    public void RegisterOwnedBobber(string itemId)
    {
        if (!_ownedBobberIds.Add(itemId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EquippedBobberId))
        {
            EquippedBobberId = itemId;
        }
        Changed?.Invoke();
    }

    public ActionResult ApplyNextRodTier()
    {
        var next = FishingProgressionCatalog.RodTiers.FirstOrDefault(tier =>
            tier.Rank == RodTier.Rank + 1
        );
        if (next is null)
        {
            return ActionResult.Fail("fishing.rod.max_tier");
        }

        RodTierId = next.Id;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "fishing.rod.upgraded");
    }

    public void RecordCatch(int difficulty)
    {
        Experience = Math.Min(
            999999,
            Experience + Math.Max(1, 8 + difficulty * 4)
        );
        Level = LevelForExperience(Experience);
        Changed?.Invoke();
    }

    public ActionResult ChooseSpecialization(string specializationId)
    {
        if (!CanChooseSpecialization)
        {
            return ActionResult.Fail("fishing.skill.specialization_locked");
        }

        if (!IsSpecializationId(specializationId))
        {
            return ActionResult.Fail("fishing.skill.specialization_unknown");
        }

        SpecializationId = specializationId;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "fishing.skill.specialization_chosen"
        );
    }

    public void ClearBaitIfMissing(Inventory inventory)
    {
        if (string.IsNullOrWhiteSpace(EquippedBaitId) ||
            inventory.Count(EquippedBaitId) > 0)
        {
            return;
        }

        EquippedBaitId = string.Empty;
        Changed?.Invoke();
    }

    public static int LevelForExperience(int experience)
    {
        var level = 0;
        foreach (var threshold in FishingProgressionCatalog.LevelThresholds)
        {
            if (experience < threshold)
            {
                break;
            }
            level++;
        }

        return Math.Clamp(level - 1, 0, 5);
    }

    private static bool IsSpecializationId(string? specializationId) =>
        specializationId is
            FishingProgressionCatalog.CurrentListenerSpecializationId or
            FishingProgressionCatalog.DeepThreaderSpecializationId;
}

public enum FishingChallengeStatus
{
    Idle,
    Active,
    Succeeded,
    Failed
}

public sealed record FishingChallengeSnapshot(
    FishingChallengeStatus Status,
    string FishId,
    int Difficulty,
    float FishPosition,
    float HookPosition,
    float CatchZoneSize,
    float Progress,
    float Tension,
    float ElapsedSeconds
);

public sealed class FishingMinigameSystem
{
    private string _fishId = string.Empty;
    private int _difficulty;
    private float _fishPosition = 0.5f;
    private float _hookPosition = 0.5f;
    private float _hookVelocity;
    private float _catchZoneSize;
    private float _progress;
    private float _tension = 1;
    private float _elapsed;
    private float _progressRateBonus;
    private float _tensionRecoveryBonus;
    private float _phase;

    public FishingChallengeStatus Status { get; private set; } =
        FishingChallengeStatus.Idle;
    public bool IsActive => Status == FishingChallengeStatus.Active;
    public event Action? Changed;

    public FishingChallengeSnapshot Snapshot() => new(
        Status,
        _fishId,
        _difficulty,
        _fishPosition,
        _hookPosition,
        _catchZoneSize,
        _progress,
        _tension,
        _elapsed
    );

    public FishingChallengeSnapshot Begin(
        FishDefinition fish,
        FishingProgressionSystem progression,
        float additionalCatchZoneBonus = 0
    )
    {
        _fishId = fish.Id;
        _difficulty = DifficultyFor(fish);
        _fishPosition = 0.5f;
        _hookPosition = 0.5f;
        _hookVelocity = 0;
        _catchZoneSize = Math.Clamp(
            0.26f - _difficulty * 0.018f + progression.CatchZoneBonus +
                Math.Max(0, additionalCatchZoneBonus),
            0.13f,
            0.42f
        );
        _progress = 0;
        _tension = 1;
        _elapsed = 0;
        _progressRateBonus = progression.ProgressRateBonus;
        _tensionRecoveryBonus = progression.TensionRecoveryBonus;
        _phase = StablePhase(fish.Id);
        Status = FishingChallengeStatus.Active;
        Changed?.Invoke();
        return Snapshot();
    }

    public FishingChallengeSnapshot Advance(float deltaSeconds, bool reeling)
    {
        if (!IsActive || deltaSeconds <= 0)
        {
            return Snapshot();
        }

        var delta = Math.Min(deltaSeconds, 0.1f);
        _elapsed += delta;
        var amplitude = 0.16f + _difficulty * 0.035f;
        var speed = 1.05f + _difficulty * 0.22f;
        _fishPosition = Math.Clamp(
            0.5f +
            MathF.Sin(_elapsed * speed + _phase) * amplitude +
            MathF.Sin(_elapsed * speed * 0.43f + _phase * 1.7f) * 0.08f,
            0.04f,
            0.96f
        );

        var acceleration = reeling ? 1.55f : -1.1f;
        _hookVelocity = Math.Clamp(
            (_hookVelocity + acceleration * delta) * 0.88f,
            -0.8f,
            0.8f
        );
        _hookPosition = Math.Clamp(
            _hookPosition + _hookVelocity * delta,
            0.02f,
            0.98f
        );

        var hooked = MathF.Abs(_fishPosition - _hookPosition) <=
            _catchZoneSize / 2;
        if (hooked)
        {
            _progress = Math.Min(
                1,
                _progress + delta * (0.27f + _progressRateBonus)
            );
            _tension = Math.Min(
                1,
                _tension + delta * (0.10f + _tensionRecoveryBonus)
            );
        }
        else
        {
            _progress = Math.Max(
                0,
                _progress - delta * (0.08f + _difficulty * 0.008f)
            );
            _tension = Math.Max(
                0,
                _tension - delta * (0.045f + _difficulty * 0.01f)
            );
        }

        if (_progress >= 1)
        {
            Status = FishingChallengeStatus.Succeeded;
        }
        else if (_tension <= 0 || _elapsed >= 30)
        {
            Status = FishingChallengeStatus.Failed;
        }

        Changed?.Invoke();
        return Snapshot();
    }

    public void Reset()
    {
        _fishId = string.Empty;
        _difficulty = 0;
        _fishPosition = 0.5f;
        _hookPosition = 0.5f;
        _hookVelocity = 0;
        _catchZoneSize = 0;
        _progress = 0;
        _tension = 1;
        _elapsed = 0;
        _progressRateBonus = 0;
        _tensionRecoveryBonus = 0;
        _phase = 0;
        Status = FishingChallengeStatus.Idle;
        Changed?.Invoke();
    }

    public static int DifficultyFor(FishDefinition fish) => Math.Clamp(
        1 + fish.AvailabilitySpecificity +
        DataCatalog.Item(fish.ItemId).SellPrice / 90,
        1,
        5
    );

    private static float StablePhase(string id)
    {
        uint hash = 2166136261;
        foreach (var character in id)
        {
            hash ^= character;
            hash *= 16777619;
        }

        return hash % 6283 / 1000f;
    }
}

public sealed class CrabPotState
{
    public CrabPotState(GridPosition position)
    {
        Position = position;
    }

    public GridPosition Position { get; }
    public string BaitItemId { get; internal set; } = string.Empty;
    public string CatchItemId { get; internal set; } = string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(BaitItemId) &&
        string.IsNullOrWhiteSpace(CatchItemId);
    public bool IsBaited => !string.IsNullOrWhiteSpace(BaitItemId) &&
        string.IsNullOrWhiteSpace(CatchItemId);
    public bool IsReady => !string.IsNullOrWhiteSpace(CatchItemId);
}

public sealed class CrabPotSystem
{
    public const int MaximumPlacedPots = 12;
    private readonly Dictionary<GridPosition, CrabPotState> _pots = [];

    public IReadOnlyDictionary<GridPosition, CrabPotState> Pots => _pots;
    public event Action? Changed;

    public void Reset()
    {
        _pots.Clear();
        Changed?.Invoke();
    }

    public void Restore(FishingSave? save)
    {
        _pots.Clear();
        foreach (var entry in save?.CrabPots ?? [])
        {
            var position = new GridPosition(entry.X, entry.Y);
            if (_pots.Count >= MaximumPlacedPots ||
                _pots.ContainsKey(position) ||
                !WorldDefinition.IsWaterSource(position))
            {
                continue;
            }

            var state = new CrabPotState(position);
            if (FishingProgressionCatalog.BaitItemIds.Contains(
                    entry.BaitItemId
                ))
            {
                state.BaitItemId = entry.BaitItemId;
            }
            if (DataCatalog.Fishes.ContainsKey(entry.CatchItemId))
            {
                state.CatchItemId = entry.CatchItemId;
            }
            if (!string.IsNullOrWhiteSpace(state.CatchItemId) &&
                string.IsNullOrWhiteSpace(state.BaitItemId))
            {
                state.BaitItemId = DataCatalog.GlowgrubBaitId;
            }
            _pots[position] = state;
        }
        Changed?.Invoke();
    }

    public void CaptureInto(FishingSave save)
    {
        save.CrabPots = _pots.Values
            .OrderBy(state => state.Position.Y)
            .ThenBy(state => state.Position.X)
            .Select(state => new CrabPotSave
            {
                X = state.Position.X,
                Y = state.Position.Y,
                BaitItemId = state.BaitItemId,
                CatchItemId = state.CatchItemId
            })
            .ToList();
    }

    public bool HasPot(GridPosition position) => _pots.ContainsKey(position);

    public CrabPotState PotAt(GridPosition position) =>
        _pots.TryGetValue(position, out var state)
            ? state
            : throw new KeyNotFoundException(
                $"No crab pot exists at {position}."
            );

    public ActionResult Place(GridPosition position, Inventory inventory)
    {
        if (!WorldDefinition.IsWaterSource(position))
        {
            return ActionResult.Fail("fishing.crab_pot.requires_water");
        }
        if (_pots.ContainsKey(position))
        {
            return ActionResult.Fail("fishing.crab_pot.occupied");
        }
        if (_pots.Count >= MaximumPlacedPots)
        {
            return ActionResult.Fail("fishing.crab_pot.limit");
        }
        if (inventory.Count(DataCatalog.MoonreedCrabPotId) <= 0 ||
            !inventory.Remove(DataCatalog.MoonreedCrabPotId, 1))
        {
            return ActionResult.Fail("fishing.crab_pot.missing");
        }

        _pots[position] = new CrabPotState(position);
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "fishing.crab_pot.placed");
    }

    public ActionResult Interact(
        GridPosition position,
        Inventory inventory,
        FishingSystem fishing
    )
    {
        if (!_pots.TryGetValue(position, out var state))
        {
            return ActionResult.Fail("fishing.crab_pot.missing");
        }

        if (state.IsReady)
        {
            var result = fishing.CommitCatch(state.CatchItemId, inventory, 0);
            if (!result.Succeeded)
            {
                return result;
            }

            state.BaitItemId = string.Empty;
            state.CatchItemId = string.Empty;
            Changed?.Invoke();
            return result with { MessageKey = "fishing.crab_pot.collected" };
        }

        if (state.IsBaited)
        {
            return ActionResult.Fail("fishing.crab_pot.waiting");
        }

        var baitId = inventory.Count(DataCatalog.MoonmoteBaitId) > 0
            ? DataCatalog.MoonmoteBaitId
            : DataCatalog.GlowgrubBaitId;
        if (inventory.Count(baitId) <= 0 || !inventory.Remove(baitId, 1))
        {
            return ActionResult.Fail("fishing.crab_pot.needs_bait");
        }

        state.BaitItemId = baitId;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "fishing.crab_pot.baited");
    }

    public void ResolveNight(
        int day,
        string weatherId,
        FishingSystem fishing
    )
    {
        var changed = false;
        foreach (var state in _pots.Values.Where(state => state.IsBaited))
        {
            var fish = fishing.PreviewCatch(
                state.Position,
                day,
                GameClock.StartMinute,
                weatherId
            );
            if (fish is null)
            {
                continue;
            }

            state.CatchItemId = fish.Id;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }
}
