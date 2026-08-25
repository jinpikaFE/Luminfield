namespace Luminfield.Core;

public sealed record StarfallWatchPatrolDefinition(
    string Id,
    WorldBiome TargetBiome,
    int RewardCoins,
    string RewardItemId,
    int RewardItemCount,
    string NameKey,
    string DescriptionKey
);

public sealed record StarfallWatchBountyDefinition(
    string Id,
    string EnemyId,
    int RequiredCount,
    int RewardCoins,
    string RewardItemId,
    int RewardItemCount,
    string NameKey,
    string DescriptionKey
);

public sealed record StarfallWatchPreparationDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    float IncomingDamageMultiplier = 1f,
    float EnemySpeedMultiplier = 1f,
    bool IsFieldRation = false
);

public sealed record StarfallWatchReward(
    string SourceId,
    string SourceKind,
    int RewardCoins,
    string RewardItemId,
    int RewardItemCount,
    int RelationshipPoints
);

public sealed record StarfallWatchBoardSnapshot(
    int Day,
    IReadOnlyList<StarfallWatchPatrolDefinition> PatrolOffers,
    StarfallWatchBountyDefinition BountyOffer,
    IReadOnlyList<StarfallWatchPreparationDefinition> Preparations,
    string ActivePatrolId,
    bool PatrolTargetReached,
    IReadOnlySet<string> CompletedPatrolIds,
    string ActiveBountyId,
    int ActiveBountyProgress,
    string FailedBountyId,
    IReadOnlySet<string> CompletedBountyIds,
    string PreparationId,
    bool PreparationConsumed,
    int DailyPatrolCompletionLimit
)
{
    public bool HasActivePatrol => !string.IsNullOrWhiteSpace(ActivePatrolId);
    public bool HasActiveBounty => !string.IsNullOrWhiteSpace(ActiveBountyId);
    public int CompletedPatrolCount => CompletedPatrolIds.Count;
    public bool BountyFailedToday => FailedBountyId == BountyOffer.Id;
    public bool BountyCompletedToday => CompletedBountyIds.Contains(BountyOffer.Id);
    public bool HasPreparation => !string.IsNullOrWhiteSpace(PreparationId);
}

public sealed class StarfallWatchSystem
{
    public const int DailyPatrolOfferCount = 2;
    public const int DailyPatrolCompletionLimit = 2;
    public const int PatrolRelationshipRewardPoints = 2;
    public const int BountyRelationshipRewardPoints = 3;

    public const string VillageSouthSealPatrolId =
        "watch_patrol_village_south_seal";
    public const string MeadowLanternRoadPatrolId =
        "watch_patrol_meadow_lantern_road";
    public const string CrystalValeMarkerPatrolId =
        "watch_patrol_crystal_vale_marker";
    public const string RuinsThresholdPatrolId =
        "watch_patrol_ruins_threshold";
    public const string WetlandReedCrossingPatrolId =
        "watch_patrol_wetland_reed_crossing";
    public const string WoodsOldWatchPatrolId =
        "watch_patrol_woods_old_watch";

    public const string ShardlingBountyId = "watch_bounty_enemy_shardling";
    public const string PrismWispBountyId =
        "watch_bounty_enemy_prism_wisp";
    public const string MoonshardMiteBountyId =
        "watch_bounty_enemy_moonshard_mite";
    public const string VeilwingBatBountyId =
        "watch_bounty_enemy_veilwing_bat";
    public const string HollowSentinelBountyId =
        "watch_bounty_enemy_hollow_sentinel";
    public const string StarironBurrowerBountyId =
        "watch_bounty_enemy_stariron_burrower";

    public const string SealWardPreparationId = "watch_prep_seal_ward";
    public const string RouteThreadsPreparationId =
        "watch_prep_route_threads";
    public const string FieldRationPreparationId =
        "watch_prep_field_ration";

    public const string PatrolSourceKind = "patrol";
    public const string BountySourceKind = "bounty";

    private int _day = 1;
    private string _activePatrolId = string.Empty;
    private bool _patrolTargetReached;
    private readonly HashSet<string> _completedPatrolIds =
        new(StringComparer.Ordinal);
    private string _activeBountyId = string.Empty;
    private int _activeBountyProgress;
    private string _failedBountyId = string.Empty;
    private readonly HashSet<string> _completedBountyIds =
        new(StringComparer.Ordinal);
    private string _preparationId = string.Empty;
    private bool _preparationConsumed;

    public static IReadOnlyList<StarfallWatchPatrolDefinition>
        Patrols { get; } = BuildPatrols();

    public static IReadOnlyDictionary<string, StarfallWatchPatrolDefinition>
        PatrolsById { get; } = Patrols.ToDictionary(
            patrol => patrol.Id,
            StringComparer.Ordinal
        );

    public static IReadOnlyList<StarfallWatchBountyDefinition>
        Bounties { get; } = BuildBounties();

    public static IReadOnlyDictionary<string, StarfallWatchBountyDefinition>
        BountiesById { get; } = Bounties.ToDictionary(
            bounty => bounty.Id,
            StringComparer.Ordinal
        );

    public static IReadOnlyList<StarfallWatchPreparationDefinition>
        Preparations { get; } =
    [
        new(
            SealWardPreparationId,
            "starfall_watch.prep.seal_ward.name",
            "starfall_watch.prep.seal_ward.description",
            IncomingDamageMultiplier: 0.9f
        ),
        new(
            RouteThreadsPreparationId,
            "starfall_watch.prep.route_threads.name",
            "starfall_watch.prep.route_threads.description",
            EnemySpeedMultiplier: 0.9f
        ),
        new(
            FieldRationPreparationId,
            "starfall_watch.prep.field_ration.name",
            "starfall_watch.prep.field_ration.description",
            IsFieldRation: true
        )
    ];

    public static IReadOnlyDictionary<string, StarfallWatchPreparationDefinition>
        PreparationsById { get; } = Preparations.ToDictionary(
            preparation => preparation.Id,
            StringComparer.Ordinal
        );

    public event Action? Changed;

    public int Day => _day;
    public string ActivePatrolId => _activePatrolId;
    public bool PatrolTargetReached => _patrolTargetReached;
    public IReadOnlySet<string> CompletedPatrolIds => _completedPatrolIds;
    public string ActiveBountyId => _activeBountyId;
    public int ActiveBountyProgress => _activeBountyProgress;
    public string FailedBountyId => _failedBountyId;
    public IReadOnlySet<string> CompletedBountyIds => _completedBountyIds;
    public string PreparationId => _preparationId;
    public bool PreparationConsumed => _preparationConsumed;
    public float IncomingDamageMultiplier =>
        IncomingDamageMultiplierForDay(_day);
    public float EnemySpeedMultiplier => EnemySpeedMultiplierForDay(_day);
    public bool HasFieldRationAvailable =>
        HasFieldRationAvailableForDay(_day);

    public void Reset(int day)
    {
        Restore(new StarfallWatchSave { Day = Math.Max(1, day) }, day);
    }

    public void Restore(StarfallWatchSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _day = normalized.Day;
        _activePatrolId = normalized.ActivePatrolId;
        _patrolTargetReached = normalized.PatrolTargetReached;
        _completedPatrolIds.Clear();
        foreach (var patrolId in normalized.CompletedPatrolIds)
        {
            _completedPatrolIds.Add(patrolId);
        }

        _activeBountyId = normalized.ActiveBountyId;
        _activeBountyProgress = normalized.ActiveBountyProgress;
        _failedBountyId = normalized.FailedBountyId;
        _completedBountyIds.Clear();
        foreach (var bountyId in normalized.CompletedBountyIds)
        {
            _completedBountyIds.Add(bountyId);
        }

        _preparationId = normalized.PreparationId;
        _preparationConsumed = normalized.PreparationConsumed;
        Changed?.Invoke();
    }

    public void AdvanceToDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day == normalizedDay)
        {
            return;
        }

        Restore(new StarfallWatchSave { Day = normalizedDay }, normalizedDay);
    }

    public StarfallWatchSave Capture() => new()
    {
        Day = _day,
        ActivePatrolId = _activePatrolId,
        PatrolTargetReached = _patrolTargetReached,
        CompletedPatrolIds = _completedPatrolIds
            .Order(StringComparer.Ordinal)
            .ToList(),
        ActiveBountyId = _activeBountyId,
        ActiveBountyProgress = _activeBountyProgress,
        FailedBountyId = _failedBountyId,
        CompletedBountyIds = _completedBountyIds
            .Order(StringComparer.Ordinal)
            .ToList(),
        PreparationId = _preparationId,
        PreparationConsumed = _preparationConsumed
    };

    public static IReadOnlyList<StarfallWatchPatrolDefinition> PatrolsForDay(
        int day
    )
    {
        var normalizedDay = Math.Max(1, day);
        var start = ((normalizedDay - 1) * DailyPatrolOfferCount) %
            Patrols.Count;
        return Enumerable.Range(0, DailyPatrolOfferCount)
            .Select(offset => Patrols[(start + offset) % Patrols.Count])
            .ToArray();
    }

    public static StarfallWatchBountyDefinition BountyForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        return Bounties[(normalizedDay - 1) % Bounties.Count];
    }

    public StarfallWatchBoardSnapshot BoardForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        var stateMatchesDay = _day == normalizedDay;
        var activePatrolId = string.Empty;
        var patrolTargetReached = false;
        var completedPatrolIds = new HashSet<string>(StringComparer.Ordinal);
        var activeBountyId = string.Empty;
        var activeBountyProgress = 0;
        var failedBountyId = string.Empty;
        var completedBountyIds = new HashSet<string>(StringComparer.Ordinal);
        var preparationId = string.Empty;
        var preparationConsumed = false;

        if (stateMatchesDay)
        {
            activePatrolId = _activePatrolId;
            patrolTargetReached = _patrolTargetReached;
            completedPatrolIds = _completedPatrolIds.ToHashSet(
                StringComparer.Ordinal
            );
            activeBountyId = _activeBountyId;
            activeBountyProgress = _activeBountyProgress;
            failedBountyId = _failedBountyId;
            completedBountyIds = _completedBountyIds.ToHashSet(
                StringComparer.Ordinal
            );
            preparationId = _preparationId;
            preparationConsumed = _preparationConsumed;
        }

        return new StarfallWatchBoardSnapshot(
            normalizedDay,
            PatrolsForDay(normalizedDay),
            BountyForDay(normalizedDay),
            Preparations,
            activePatrolId,
            patrolTargetReached,
            completedPatrolIds,
            activeBountyId,
            activeBountyProgress,
            failedBountyId,
            completedBountyIds,
            preparationId,
            preparationConsumed,
            DailyPatrolCompletionLimit
        );
    }

    public float IncomingDamageMultiplierForDay(int day)
    {
        if (_day != Math.Max(1, day))
        {
            return 1f;
        }

        if (_preparationId == SealWardPreparationId &&
            PreparationsById.TryGetValue(_preparationId, out var preparation))
        {
            return preparation.IncomingDamageMultiplier;
        }

        return 1f;
    }

    public float EnemySpeedMultiplierForDay(int day)
    {
        if (_day != Math.Max(1, day))
        {
            return 1f;
        }

        if (_preparationId == RouteThreadsPreparationId &&
            PreparationsById.TryGetValue(_preparationId, out var preparation))
        {
            return preparation.EnemySpeedMultiplier;
        }

        return 1f;
    }

    public bool HasFieldRationAvailableForDay(int day)
    {
        if (_day != Math.Max(1, day))
        {
            return false;
        }

        return _preparationId == FieldRationPreparationId &&
            !_preparationConsumed;
    }

    public ActionResult CheckPatrol(string patrolId, int day) =>
        CheckAcceptPatrol(patrolId, day);

    public ActionResult CheckAcceptPatrol(string patrolId, int day)
    {
        var board = BoardForDay(day);
        if (!board.PatrolOffers.Any(patrol => patrol.Id == patrolId))
        {
            return ActionResult.Fail("starfall_watch.patrol.unavailable");
        }

        if (board.CompletedPatrolIds.Contains(patrolId))
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.already_completed"
            );
        }

        if (board.CompletedPatrolCount >= board.DailyPatrolCompletionLimit)
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.daily_limit_reached"
            );
        }

        if (board.HasActivePatrol)
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.active_exists"
            );
        }

        return ActionResult.Success(messageKey: "starfall_watch.patrol.ready");
    }

    public ActionResult AcceptPatrol(string patrolId, int day)
    {
        var check = CheckAcceptPatrol(patrolId, day);
        if (!check.Succeeded)
        {
            return check;
        }

        AdvanceToDay(day);
        _activePatrolId = patrolId;
        _patrolTargetReached = false;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.patrol.accepted"
        );
    }

    public ActionResult CheckPatrolClaim(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            string.IsNullOrWhiteSpace(_activePatrolId) ||
            !PatrolsById.ContainsKey(_activePatrolId))
        {
            return ActionResult.Fail("starfall_watch.patrol.no_active");
        }

        if (_completedPatrolIds.Contains(_activePatrolId))
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.already_completed"
            );
        }

        if (!_patrolTargetReached)
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.target_not_reached"
            );
        }

        return ActionResult.Success(messageKey: "starfall_watch.patrol.ready");
    }

    public ActionResult ClaimPatrol(
        int day,
        out StarfallWatchReward? reward
    )
    {
        reward = null;
        var check = CheckPatrolClaim(day);
        if (!check.Succeeded)
        {
            return check;
        }

        var patrol = PatrolsById[_activePatrolId];
        _completedPatrolIds.Add(patrol.Id);
        _activePatrolId = string.Empty;
        _patrolTargetReached = false;
        reward = new StarfallWatchReward(
            patrol.Id,
            PatrolSourceKind,
            patrol.RewardCoins,
            patrol.RewardItemId,
            patrol.RewardItemCount,
            PatrolRelationshipRewardPoints
        );
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.patrol.completed"
        );
    }

    public ActionResult RecordPatrolVisit(WorldBiome biome) =>
        RecordPatrolVisit(biome, _day);

    public ActionResult RecordPatrolVisit(WorldBiome biome, int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            string.IsNullOrWhiteSpace(_activePatrolId) ||
            _patrolTargetReached ||
            _completedPatrolIds.Contains(_activePatrolId) ||
            !PatrolsById.TryGetValue(_activePatrolId, out var patrol))
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.target_not_reached"
            );
        }

        if (patrol.TargetBiome != biome)
        {
            return ActionResult.Fail(
                "starfall_watch.patrol.target_not_reached"
            );
        }

        _patrolTargetReached = true;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.patrol.target_reached"
        );
    }

    public ActionResult CheckBounty(string bountyId, int day) =>
        CheckAcceptBounty(bountyId, day);

    public ActionResult CheckAcceptBounty(string bountyId, int day)
    {
        var board = BoardForDay(day);
        if (board.BountyOffer.Id != bountyId)
        {
            return ActionResult.Fail("starfall_watch.bounty.unavailable");
        }

        if (board.CompletedBountyIds.Contains(bountyId))
        {
            return ActionResult.Fail(
                "starfall_watch.bounty.already_completed"
            );
        }

        if (board.FailedBountyId == bountyId)
        {
            return ActionResult.Fail(
                "starfall_watch.bounty.failed_today"
            );
        }

        if (board.HasActiveBounty)
        {
            return ActionResult.Fail(
                "starfall_watch.bounty.active_exists"
            );
        }

        return ActionResult.Success(messageKey: "starfall_watch.bounty.ready");
    }

    public ActionResult AcceptBounty(string bountyId, int day)
    {
        var check = CheckAcceptBounty(bountyId, day);
        if (!check.Succeeded)
        {
            return check;
        }

        AdvanceToDay(day);
        _activeBountyId = bountyId;
        _activeBountyProgress = 0;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.bounty.accepted"
        );
    }

    public ActionResult CheckBountyClaim(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            string.IsNullOrWhiteSpace(_activeBountyId) ||
            !BountiesById.TryGetValue(_activeBountyId, out var bounty))
        {
            return ActionResult.Fail("starfall_watch.bounty.no_active");
        }

        if (_completedBountyIds.Contains(_activeBountyId))
        {
            return ActionResult.Fail(
                "starfall_watch.bounty.already_completed"
            );
        }

        if (_failedBountyId == _activeBountyId)
        {
            return ActionResult.Fail(
                "starfall_watch.bounty.failed_today"
            );
        }

        if (_activeBountyProgress < bounty.RequiredCount)
        {
            return ActionResult.Fail("starfall_watch.bounty.not_complete");
        }

        return ActionResult.Success(messageKey: "starfall_watch.bounty.ready");
    }

    public ActionResult ClaimBounty(
        int day,
        out StarfallWatchReward? reward
    )
    {
        reward = null;
        var check = CheckBountyClaim(day);
        if (!check.Succeeded)
        {
            return check;
        }

        var bounty = BountiesById[_activeBountyId];
        _completedBountyIds.Add(bounty.Id);
        _activeBountyId = string.Empty;
        _activeBountyProgress = 0;
        reward = new StarfallWatchReward(
            bounty.Id,
            BountySourceKind,
            bounty.RewardCoins,
            bounty.RewardItemId,
            bounty.RewardItemCount,
            BountyRelationshipRewardPoints
        );
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.bounty.completed"
        );
    }

    public ActionResult RecordEnemyDefeated(string enemyId) =>
        RecordEnemyDefeated(enemyId, _day);

    public ActionResult RecordEnemyDefeated(string enemyId, int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            string.IsNullOrWhiteSpace(_activeBountyId) ||
            _failedBountyId == _activeBountyId ||
            _completedBountyIds.Contains(_activeBountyId) ||
            !BountiesById.TryGetValue(_activeBountyId, out var bounty))
        {
            return ActionResult.Fail("starfall_watch.bounty.not_complete");
        }

        if (bounty.EnemyId != enemyId)
        {
            return ActionResult.Fail("starfall_watch.bounty.not_complete");
        }

        if (_activeBountyProgress >= bounty.RequiredCount)
        {
            return ActionResult.Success(
                messageKey: "starfall_watch.bounty.ready"
            );
        }

        _activeBountyProgress = Math.Min(
            bounty.RequiredCount,
            _activeBountyProgress + 1
        );
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.bounty.progressed"
        );
    }

    public ActionResult FailActiveBounty() => FailActiveBounty(_day);

    public ActionResult FailActiveBounty(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            string.IsNullOrWhiteSpace(_activeBountyId) ||
            !BountiesById.TryGetValue(_activeBountyId, out var bounty))
        {
            return ActionResult.Fail("starfall_watch.bounty.no_active");
        }

        if (_activeBountyProgress >= bounty.RequiredCount)
        {
            return ActionResult.Fail("starfall_watch.bounty.ready");
        }

        _failedBountyId = _activeBountyId;
        _activeBountyId = string.Empty;
        _activeBountyProgress = 0;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.bounty.failed_today"
        );
    }

    public ActionResult CheckSelectPreparation(string preparationId, int day)
    {
        var board = BoardForDay(day);
        if (!PreparationsById.ContainsKey(preparationId))
        {
            return ActionResult.Fail("starfall_watch.prep.unavailable");
        }

        if (board.HasPreparation)
        {
            return ActionResult.Fail("starfall_watch.prep.already_selected");
        }

        return ActionResult.Success(messageKey: "starfall_watch.prep.ready");
    }

    public ActionResult SelectPreparation(string preparationId, int day)
    {
        var check = CheckSelectPreparation(preparationId, day);
        if (!check.Succeeded)
        {
            return check;
        }

        AdvanceToDay(day);
        _preparationId = preparationId;
        _preparationConsumed = false;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.prep.selected"
        );
    }

    public ActionResult ConsumeFieldRation(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day != normalizedDay ||
            _preparationId != FieldRationPreparationId ||
            _preparationConsumed)
        {
            return ActionResult.Fail("starfall_watch.prep.unavailable");
        }

        _preparationConsumed = true;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "starfall_watch.prep.consumed"
        );
    }

    public static StarfallWatchSave NormalizeSave(
        StarfallWatchSave? save,
        int currentDay
    )
    {
        var day = Math.Max(1, currentDay);
        if (save is null || save.Day != day)
        {
            return new StarfallWatchSave { Day = day };
        }

        var patrolOfferIds = PatrolsForDay(day)
            .Select(patrol => patrol.Id)
            .ToHashSet(StringComparer.Ordinal);
        var completedPatrolIds = (save.CompletedPatrolIds ?? [])
            .Where(patrolOfferIds.Contains)
            .Distinct(StringComparer.Ordinal)
            .Take(DailyPatrolCompletionLimit)
            .Order(StringComparer.Ordinal)
            .ToList();
        var activePatrolId = save.ActivePatrolId;
        if (!patrolOfferIds.Contains(activePatrolId) ||
            completedPatrolIds.Contains(activePatrolId) ||
            completedPatrolIds.Count >= DailyPatrolCompletionLimit)
        {
            activePatrolId = string.Empty;
        }

        var patrolTargetReached = false;
        if (!string.IsNullOrWhiteSpace(activePatrolId))
        {
            patrolTargetReached = save.PatrolTargetReached;
        }

        var dailyBountyId = BountyForDay(day).Id;
        var completedBountyIds = (save.CompletedBountyIds ?? [])
            .Where(id => id == dailyBountyId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var failedBountyId = save.FailedBountyId == dailyBountyId &&
            completedBountyIds.Count == 0
                ? dailyBountyId
                : string.Empty;
        var activeBountyId = save.ActiveBountyId;
        if (activeBountyId != dailyBountyId ||
            completedBountyIds.Contains(activeBountyId) ||
            failedBountyId == activeBountyId)
        {
            activeBountyId = string.Empty;
        }

        var activeBountyProgress = 0;
        if (!string.IsNullOrWhiteSpace(activeBountyId))
        {
            var bounty = BountiesById[activeBountyId];
            activeBountyProgress = Math.Clamp(
                save.ActiveBountyProgress,
                0,
                bounty.RequiredCount
            );
        }

        var preparationId = save.PreparationId;
        if (!PreparationsById.ContainsKey(preparationId))
        {
            preparationId = string.Empty;
        }

        var preparationConsumed = false;
        if (!string.IsNullOrWhiteSpace(preparationId) &&
            PreparationsById[preparationId].IsFieldRation)
        {
            preparationConsumed = save.PreparationConsumed;
        }

        return new StarfallWatchSave
        {
            Day = day,
            ActivePatrolId = activePatrolId,
            PatrolTargetReached = patrolTargetReached,
            CompletedPatrolIds = completedPatrolIds,
            ActiveBountyId = activeBountyId,
            ActiveBountyProgress = activeBountyProgress,
            FailedBountyId = failedBountyId,
            CompletedBountyIds = completedBountyIds,
            PreparationId = preparationId,
            PreparationConsumed = preparationConsumed
        };
    }

    private static IReadOnlyList<StarfallWatchPatrolDefinition> BuildPatrols()
    {
        var patrols = new StarfallWatchPatrolDefinition[]
        {
            new(
                VillageSouthSealPatrolId,
                WorldBiome.LumenVillage,
                52,
                DataCatalog.MoonstonePathId,
                1,
                "starfall_watch.patrol.village_south_seal.name",
                "starfall_watch.patrol.village_south_seal.description"
            ),
            new(
                MeadowLanternRoadPatrolId,
                WorldBiome.StarfallMeadow,
                54,
                DataCatalog.CrystalShardId,
                2,
                "starfall_watch.patrol.meadow_lantern_road.name",
                "starfall_watch.patrol.meadow_lantern_road.description"
            ),
            new(
                CrystalValeMarkerPatrolId,
                WorldBiome.CrystalVale,
                62,
                DataCatalog.LumenSlateOreId,
                1,
                "starfall_watch.patrol.crystal_vale_marker.name",
                "starfall_watch.patrol.crystal_vale_marker.description"
            ),
            new(
                RuinsThresholdPatrolId,
                WorldBiome.StarfallRuins,
                72,
                DataCatalog.StarironOreId,
                1,
                "starfall_watch.patrol.ruins_threshold.name",
                "starfall_watch.patrol.ruins_threshold.description"
            ),
            new(
                WetlandReedCrossingPatrolId,
                WorldBiome.MoonwaterWetlands,
                58,
                DataCatalog.MoonveinOreId,
                1,
                "starfall_watch.patrol.wetland_reed_crossing.name",
                "starfall_watch.patrol.wetland_reed_crossing.description"
            ),
            new(
                WoodsOldWatchPatrolId,
                WorldBiome.WhisperingWoods,
                56,
                DataCatalog.CrystalShardId,
                3,
                "starfall_watch.patrol.woods_old_watch.name",
                "starfall_watch.patrol.woods_old_watch.description"
            )
        };

        if (patrols.Length < DailyPatrolOfferCount ||
            patrols.Any(InvalidPatrol) ||
            patrols.Select(patrol => patrol.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() != patrols.Length)
        {
            throw new InvalidOperationException(
                "Invalid starfall watch patrol catalog."
            );
        }

        return patrols;
    }

    private static IReadOnlyList<StarfallWatchBountyDefinition> BuildBounties()
    {
        var bounties = new StarfallWatchBountyDefinition[]
        {
            new(
                ShardlingBountyId,
                StarfallRuinsTrialCatalog.ShardlingEnemyId,
                2,
                90,
                DataCatalog.CrystalShardId,
                3,
                "starfall_watch.bounty.shardling.name",
                "starfall_watch.bounty.shardling.description"
            ),
            new(
                PrismWispBountyId,
                StarfallRuinsTrialCatalog.PrismWispEnemyId,
                2,
                104,
                DataCatalog.LumenSlateOreId,
                2,
                "starfall_watch.bounty.prism_wisp.name",
                "starfall_watch.bounty.prism_wisp.description"
            ),
            new(
                MoonshardMiteBountyId,
                StarfallRuinsTrialCatalog.MoonshardMiteEnemyId,
                2,
                112,
                DataCatalog.MoonveinOreId,
                2,
                "starfall_watch.bounty.moonshard_mite.name",
                "starfall_watch.bounty.moonshard_mite.description"
            ),
            new(
                VeilwingBatBountyId,
                StarfallRuinsTrialCatalog.VeilwingBatEnemyId,
                2,
                118,
                DataCatalog.PrismheartOreId,
                1,
                "starfall_watch.bounty.veilwing_bat.name",
                "starfall_watch.bounty.veilwing_bat.description"
            ),
            new(
                HollowSentinelBountyId,
                StarfallRuinsTrialCatalog.HollowSentinelEnemyId,
                1,
                138,
                DataCatalog.PrismheartOreId,
                2,
                "starfall_watch.bounty.hollow_sentinel.name",
                "starfall_watch.bounty.hollow_sentinel.description"
            ),
            new(
                StarironBurrowerBountyId,
                StarfallRuinsTrialCatalog.StarironBurrowerEnemyId,
                1,
                150,
                DataCatalog.StarironOreId,
                2,
                "starfall_watch.bounty.stariron_burrower.name",
                "starfall_watch.bounty.stariron_burrower.description"
            )
        };

        var enemyIds = StarfallRuinsTrialCatalog.Enemies
            .Select(enemy => enemy.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (bounties.Length == 0 ||
            bounties.Any(bounty => InvalidBounty(bounty, enemyIds)) ||
            bounties.Select(bounty => bounty.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() != bounties.Length)
        {
            throw new InvalidOperationException(
                "Invalid starfall watch bounty catalog."
            );
        }

        return bounties;
    }

    private static bool InvalidPatrol(
        StarfallWatchPatrolDefinition patrol
    ) => string.IsNullOrWhiteSpace(patrol.Id) ||
        patrol.RewardCoins <= 0 ||
        string.IsNullOrWhiteSpace(patrol.RewardItemId) ||
        patrol.RewardItemCount <= 0 ||
        string.IsNullOrWhiteSpace(patrol.NameKey) ||
        string.IsNullOrWhiteSpace(patrol.DescriptionKey);

    private static bool InvalidBounty(
        StarfallWatchBountyDefinition bounty,
        IReadOnlySet<string> enemyIds
    ) => string.IsNullOrWhiteSpace(bounty.Id) ||
        !enemyIds.Contains(bounty.EnemyId) ||
        bounty.RequiredCount <= 0 ||
        bounty.RewardCoins <= 0 ||
        string.IsNullOrWhiteSpace(bounty.RewardItemId) ||
        bounty.RewardItemCount <= 0 ||
        string.IsNullOrWhiteSpace(bounty.NameKey) ||
        string.IsNullOrWhiteSpace(bounty.DescriptionKey);
}
