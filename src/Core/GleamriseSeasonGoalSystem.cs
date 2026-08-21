namespace Luminfield.Core;

public sealed record GleamriseGoalRequirement(
    string CounterId,
    int RequiredCount
);

public sealed record GleamriseGoalDefinition(
    string Id,
    int SeasonDay,
    string TitleKey,
    string DescriptionKey,
    string HintKey,
    IReadOnlyList<GleamriseGoalRequirement> Requirements,
    int RewardCoins = 0,
    string RewardItemId = "",
    int RewardItemCount = 0,
    bool IsSeasonFinal = false
);

public enum GleamriseGoalStatus
{
    Locked,
    Open,
    Ready,
    Claimed
}

public sealed record GleamriseGoalSnapshot(
    GleamriseGoalDefinition Definition,
    GleamriseGoalStatus Status,
    int Progress,
    int RequiredCount,
    string RequirementCounterId
);

public sealed record GleamriseGoalClaimResult(
    bool Succeeded,
    string MessageKey,
    int RewardCoins = 0,
    string RewardItemId = "",
    int RewardItemCount = 0
);

public sealed class GleamriseSeasonGoalSystem
{
    public const int SeasonCompletionRequiredGoals = 9;
    public const string CounterPlantGleamriseCrop =
        "gleamrise.plant_crop";
    public const string CounterWaterGleamriseCrop =
        "gleamrise.water_crop";
    public const string CounterHarvestGleamriseCrop =
        "gleamrise.harvest_crop";
    public const string CounterBuyGleamriseSeed =
        "gleamrise.buy_seed";
    public const string CounterFertilizeGleamriseSoil =
        "gleamrise.fertilize_soil";
    public const string CounterStartProcessor =
        "gleamrise.start_processor";
    public const string CounterCollectProcessor =
        "gleamrise.collect_processor";
    public const string CounterPlantMoonplumTree =
        "gleamrise.plant_moonplum_tree";
    public const string CounterHarvestMoonplum =
        "gleamrise.harvest_moonplum";
    public const string CounterPlaceGlowcombHive =
        "gleamrise.place_glowcomb_hive";
    public const string CounterCollectStarhoney =
        "gleamrise.collect_starhoney";
    public const string CounterAnimalFeedPrepared =
        "gleamrise.starchicken.feed_prepared";
    public const string CounterAnimalFirstEgg =
        "gleamrise.starchicken.first_egg_family";
    public const string CounterFestivalJoined =
        "gleamrise.sowing_festival.joined";
    public const string CounterCompletedNonFinalGoals =
        "gleamrise.completed_non_final_goals";

    private const string InventoryFamilyPrefix = "inventory.family:";
    private const string InventoryItemPrefix = "inventory.item:";

    private readonly HashSet<string> _claimedGoalIds =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _claimedDays =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _counters =
        new(StringComparer.Ordinal);

    private int _year = 1;
    private string _seasonId = CalendarSystem.GleamriseSeasonId;

    public int Year => _year;
    public string SeasonId => _seasonId;
    public int ClaimedCount => _claimedGoalIds.Count;

    public event Action? Changed;

    public void Reset(int day)
    {
        Restore(CreateForDay(day), day);
    }

    public void Restore(GleamriseSeasonSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _year = normalized.Year;
        _seasonId = normalized.SeasonId;
        _claimedGoalIds.Clear();
        _claimedDays.Clear();
        foreach (var entry in normalized.Goals)
        {
            _claimedGoalIds.Add(entry.GoalId);
            _claimedDays[entry.GoalId] = entry.ClaimedDay;
        }

        _counters.Clear();
        foreach (var counter in normalized.Counters)
        {
            _counters[counter.CounterId] = counter.Count;
        }

        Changed?.Invoke();
    }

    public void RefreshForDay(int day)
    {
        var year = CalendarSystem.YearNumber(day);
        var seasonId = CalendarSystem.SeasonId(day);
        if (seasonId != CalendarSystem.GleamriseSeasonId || year == _year)
        {
            return;
        }

        Restore(CreateForDay(day), day);
    }

    public IReadOnlyList<GleamriseGoalSnapshot> Snapshots(
        int day,
        Inventory inventory
    ) => Catalog.Goals
        .Select(goal => Snapshot(goal, day, inventory))
        .ToArray();

    public GleamriseGoalClaimResult Claim(
        string goalId,
        int day,
        Inventory inventory
    )
    {
        if (!Catalog.GoalsById.TryGetValue(goalId, out var goal))
        {
            return new GleamriseGoalClaimResult(
                false,
                "gleamrise.goal.unknown"
            );
        }

        var snapshot = Snapshot(goal, day, inventory);
        if (snapshot.Status == GleamriseGoalStatus.Claimed)
        {
            return new GleamriseGoalClaimResult(
                false,
                "gleamrise.goal.already_claimed"
            );
        }

        if (snapshot.Status == GleamriseGoalStatus.Locked)
        {
            return new GleamriseGoalClaimResult(
                false,
                "gleamrise.goal.locked"
            );
        }

        if (snapshot.Status != GleamriseGoalStatus.Ready)
        {
            return new GleamriseGoalClaimResult(
                false,
                goal.IsSeasonFinal
                    ? "gleamrise.goal.season_incomplete"
                    : "gleamrise.goal.not_ready"
            );
        }

        if (!string.IsNullOrWhiteSpace(goal.RewardItemId) &&
            goal.RewardItemCount > 0 &&
            !inventory.Add(goal.RewardItemId, goal.RewardItemCount))
        {
            return new GleamriseGoalClaimResult(
                false,
                "notice.inventory_full"
            );
        }

        _claimedGoalIds.Add(goal.Id);
        _claimedDays[goal.Id] = Math.Max(1, day);
        Changed?.Invoke();
        return new GleamriseGoalClaimResult(
            true,
            "gleamrise.goal.claimed",
            goal.RewardCoins,
            goal.RewardItemId,
            goal.RewardItemCount
        );
    }

    public void RecordPurchasedItem(string itemId, int day, int count = 1)
    {
        if (count <= 0 ||
            !IsGleamriseContext(day) ||
            !DataCatalog.GleamriseSeedItemIds.Contains(
                itemId,
                StringComparer.Ordinal
            ))
        {
            return;
        }

        AddCounter(CounterBuyGleamriseSeed, count);
    }

    public void RecordPlant(string cropId, int day)
    {
        if (!IsGleamriseContext(day) ||
            !DataCatalog.GleamriseCropIds.Contains(
                cropId,
                StringComparer.Ordinal
            ))
        {
            return;
        }

        AddCounter(CounterPlantGleamriseCrop, 1);
    }

    public void RecordWateredCrop(string? cropId, int day)
    {
        if (string.IsNullOrWhiteSpace(cropId) ||
            !IsGleamriseContext(day) ||
            !DataCatalog.GleamriseCropIds.Contains(
                cropId,
                StringComparer.Ordinal
            ))
        {
            return;
        }

        AddCounter(CounterWaterGleamriseCrop, 1);
    }

    public void RecordFertilized(int day)
    {
        if (!IsGleamriseContext(day))
        {
            return;
        }

        AddCounter(CounterFertilizeGleamriseSoil, 1);
    }

    public void RecordGatheredItem(string itemId, int count, int day)
    {
        if (count <= 0 || !IsGleamriseContext(day))
        {
            return;
        }

        var baseItemId = DataCatalog.BaseItemId(itemId);
        if (DataCatalog.GleamriseCropIds.Contains(
                baseItemId,
                StringComparer.Ordinal
            ))
        {
            AddCounter(CounterHarvestGleamriseCrop, count);
        }

        if (baseItemId == DataCatalog.MoonplumId)
        {
            AddCounter(CounterHarvestMoonplum, count);
        }

        if (baseItemId == DataCatalog.StarhoneyId)
        {
            AddCounter(CounterCollectStarhoney, count);
        }
    }

    public void RecordProcessorStarted(int day)
    {
        if (IsGleamriseContext(day))
        {
            AddCounter(CounterStartProcessor, 1);
        }
    }

    public void RecordProcessorCollected(string itemId, int count, int day)
    {
        if (count <= 0 || !IsGleamriseContext(day))
        {
            return;
        }

        AddCounter(CounterCollectProcessor, count);
    }

    public void RecordMoonplumTreePlanted(int day)
    {
        if (IsGleamriseContext(day))
        {
            AddCounter(CounterPlantMoonplumTree, 1);
        }
    }

    public void RecordGlowcombHivePlaced(int day)
    {
        if (IsGleamriseContext(day))
        {
            AddCounter(CounterPlaceGlowcombHive, 1);
        }
    }

    public void RecordMilestone(string milestoneId, int count = 1)
    {
        if (count <= 0 || !Catalog.CounterIds.Contains(milestoneId))
        {
            return;
        }

        AddCounter(milestoneId, count);
    }

    public GleamriseSeasonSave Capture() => new()
    {
        Year = _year,
        SeasonId = _seasonId,
        Goals = _claimedGoalIds
            .OrderBy(id => Catalog.GoalsById[id].SeasonDay)
            .Select(id => new GleamriseGoalEntrySave
            {
                GoalId = id,
                ClaimedDay = _claimedDays.TryGetValue(id, out var day)
                    ? day
                    : 1
            })
            .ToList(),
        Counters = _counters
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new GleamriseGoalCounterSave
            {
                CounterId = pair.Key,
                Count = pair.Value
            })
            .ToList()
    };

    public static GleamriseSeasonSave NormalizeSave(
        GleamriseSeasonSave? save,
        int currentDay
    )
    {
        var expected = CreateForDay(currentDay);
        if (save is null)
        {
            return expected;
        }

        var currentSeason = CalendarSystem.SeasonId(currentDay);
        var currentYear = CalendarSystem.YearNumber(currentDay);
        if (currentSeason == CalendarSystem.GleamriseSeasonId &&
            save.Year != currentYear)
        {
            return expected;
        }

        var year = Math.Max(1, save.Year);
        var seasonId = save.SeasonId == CalendarSystem.GleamriseSeasonId
            ? save.SeasonId
            : CalendarSystem.GleamriseSeasonId;
        var goals = (save.Goals ?? [])
            .Where(entry => Catalog.GoalsById.ContainsKey(entry.GoalId))
            .GroupBy(entry => entry.GoalId, StringComparer.Ordinal)
            .Select(group => new GleamriseGoalEntrySave
            {
                GoalId = group.Key,
                ClaimedDay = Math.Max(
                    1,
                    group.Min(entry => Math.Max(1, entry.ClaimedDay))
                )
            })
            .OrderBy(entry => Catalog.GoalsById[entry.GoalId].SeasonDay)
            .ToList();
        var counters = (save.Counters ?? [])
            .Where(counter => Catalog.CounterIds.Contains(counter.CounterId))
            .GroupBy(counter => counter.CounterId, StringComparer.Ordinal)
            .Select(group => new GleamriseGoalCounterSave
            {
                CounterId = group.Key,
                Count = Math.Clamp(
                    group.Sum(counter => Math.Max(0, counter.Count)),
                    0,
                    999
                )
            })
            .Where(counter => counter.Count > 0)
            .OrderBy(counter => counter.CounterId, StringComparer.Ordinal)
            .ToList();
        return new GleamriseSeasonSave
        {
            Year = year,
            SeasonId = seasonId,
            Goals = goals,
            Counters = counters
        };
    }

    private static GleamriseSeasonSave CreateForDay(int day) => new()
    {
        Year = CalendarSystem.YearNumber(day),
        SeasonId = CalendarSystem.GleamriseSeasonId
    };

    private GleamriseGoalSnapshot Snapshot(
        GleamriseGoalDefinition goal,
        int day,
        Inventory inventory
    )
    {
        var best = BestRequirement(goal, day, inventory);
        var status = GleamriseGoalStatus.Open;
        if (_claimedGoalIds.Contains(goal.Id))
        {
            status = GleamriseGoalStatus.Claimed;
        }
        else if (!IsAccessible(goal, day))
        {
            status = GleamriseGoalStatus.Locked;
        }
        else if (best.Progress >= best.RequiredCount)
        {
            status = GleamriseGoalStatus.Ready;
        }

        return new GleamriseGoalSnapshot(
            goal,
            status,
            best.Progress,
            best.RequiredCount,
            best.CounterId
        );
    }

    private (int Progress, int RequiredCount, string CounterId) BestRequirement(
        GleamriseGoalDefinition goal,
        int day,
        Inventory inventory,
        bool includeAggregateRequirements = true
    )
    {
        var bestProgress = 0;
        var bestRequired = 1;
        var bestCounterId = string.Empty;
        var bestRatio = -1f;
        foreach (var requirement in goal.Requirements)
        {
            if (!includeAggregateRequirements &&
                requirement.CounterId == CounterCompletedNonFinalGoals)
            {
                continue;
            }

            var required = Math.Max(1, requirement.RequiredCount);
            var progress = Math.Min(
                required,
                CounterValue(requirement.CounterId, day, inventory)
            );
            var ratio = progress / (float)required;
            if (ratio <= bestRatio)
            {
                continue;
            }

            bestProgress = progress;
            bestRequired = required;
            bestCounterId = requirement.CounterId;
            bestRatio = ratio;
        }

        return (bestProgress, bestRequired, bestCounterId);
    }

    private int CounterValue(
        string counterId,
        int day,
        Inventory inventory
    )
    {
        if (counterId == CounterCompletedNonFinalGoals)
        {
            return CompletedNonFinalGoalCount(day, inventory);
        }

        if (counterId.StartsWith(
                InventoryFamilyPrefix,
                StringComparison.Ordinal
            ))
        {
            var itemId = counterId[InventoryFamilyPrefix.Length..];
            return DataCatalog.Items.ContainsKey(itemId)
                ? inventory.CountFamily(itemId)
                : 0;
        }

        if (counterId.StartsWith(
                InventoryItemPrefix,
                StringComparison.Ordinal
            ))
        {
            var itemId = counterId[InventoryItemPrefix.Length..];
            return DataCatalog.Items.ContainsKey(itemId)
                ? inventory.Count(itemId)
                : 0;
        }

        return _counters.TryGetValue(counterId, out var value)
            ? value
            : 0;
    }

    private int CompletedNonFinalGoalCount(int day, Inventory inventory) =>
        Catalog.Goals
            .Where(goal => !goal.IsSeasonFinal)
            .Count(goal =>
            {
                if (_claimedGoalIds.Contains(goal.Id))
                {
                    return true;
                }

                if (!IsAccessible(goal, day))
                {
                    return false;
                }

                var best = BestRequirement(
                    goal,
                    day,
                    inventory,
                    includeAggregateRequirements: false
                );
                return best.Progress >= best.RequiredCount;
            });

    private bool IsAccessible(GleamriseGoalDefinition goal, int day)
    {
        var currentYear = CalendarSystem.YearNumber(day);
        if (currentYear != _year)
        {
            return false;
        }

        var currentSeasonId = CalendarSystem.SeasonId(day);
        if (currentSeasonId != CalendarSystem.GleamriseSeasonId)
        {
            return true;
        }

        return CalendarSystem.SeasonDay(day) >= goal.SeasonDay;
    }

    private bool IsGleamriseContext(int day) =>
        CalendarSystem.YearNumber(day) == _year &&
        CalendarSystem.SeasonId(day) == CalendarSystem.GleamriseSeasonId;

    private void AddCounter(string counterId, int count)
    {
        if (count <= 0 || !Catalog.CounterIds.Contains(counterId))
        {
            return;
        }

        _counters[counterId] = Math.Clamp(
            _counters.GetValueOrDefault(counterId) + count,
            0,
            999
        );
        Changed?.Invoke();
    }

    private static string InventoryFamilyCounter(string itemId) =>
        $"{InventoryFamilyPrefix}{itemId}";

    private static class Catalog
    {
        public static readonly IReadOnlyList<GleamriseGoalDefinition> Goals =
        [
            new(
                "gleamrise_day_01_seed_spark",
                1,
                "gleamrise.goal.day01.title",
                "gleamrise.goal.day01.description",
                "gleamrise.goal.day01.hint",
                [new GleamriseGoalRequirement(CounterBuyGleamriseSeed, 4)],
                RewardCoins: 25
            ),
            new(
                "gleamrise_day_02_first_beds",
                2,
                "gleamrise.goal.day02.title",
                "gleamrise.goal.day02.description",
                "gleamrise.goal.day02.hint",
                [new GleamriseGoalRequirement(CounterPlantGleamriseCrop, 3)],
                RewardItemId: DataCatalog.StarsoilFertilizerId,
                RewardItemCount: 1
            ),
            new(
                "gleamrise_day_03_dew_round",
                3,
                "gleamrise.goal.day03.title",
                "gleamrise.goal.day03.description",
                "gleamrise.goal.day03.hint",
                [new GleamriseGoalRequirement(CounterWaterGleamriseCrop, 3)],
                RewardCoins: 30
            ),
            new(
                "gleamrise_day_04_starsoil",
                4,
                "gleamrise.goal.day04.title",
                "gleamrise.goal.day04.description",
                "gleamrise.goal.day04.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterFertilizeGleamriseSoil,
                        1
                    )
                ],
                RewardItemId: DataCatalog.DawnlaceSeedId,
                RewardItemCount: 1
            ),
            new(
                "gleamrise_day_05_first_harvest",
                5,
                "gleamrise.goal.day05.title",
                "gleamrise.goal.day05.description",
                "gleamrise.goal.day05.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterHarvestGleamriseCrop,
                        2
                    )
                ],
                RewardCoins: 45
            ),
            new(
                "gleamrise_day_06_machine_started",
                6,
                "gleamrise.goal.day06.title",
                "gleamrise.goal.day06.description",
                "gleamrise.goal.day06.hint",
                [new GleamriseGoalRequirement(CounterStartProcessor, 1)],
                RewardItemId: DataCatalog.CrystalShardId,
                RewardItemCount: 1
            ),
            new(
                "gleamrise_day_07_machine_claimed",
                7,
                "gleamrise.goal.day07.title",
                "gleamrise.goal.day07.description",
                "gleamrise.goal.day07.hint",
                [new GleamriseGoalRequirement(CounterCollectProcessor, 1)],
                RewardCoins: 60
            ),
            new(
                "gleamrise_day_08_moonplum_sapling",
                8,
                "gleamrise.goal.day08.title",
                "gleamrise.goal.day08.description",
                "gleamrise.goal.day08.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterPlantMoonplumTree,
                        1
                    )
                ],
                RewardItemId: DataCatalog.LumenwoodId,
                RewardItemCount: 2
            ),
            new(
                "gleamrise_day_09_moonplum_bloom",
                9,
                "gleamrise.goal.day09.title",
                "gleamrise.goal.day09.description",
                "gleamrise.goal.day09.hint",
                [new GleamriseGoalRequirement(CounterHarvestMoonplum, 1)],
                RewardCoins: 75
            ),
            new(
                "gleamrise_day_10_glowcomb_home",
                10,
                "gleamrise.goal.day10.title",
                "gleamrise.goal.day10.description",
                "gleamrise.goal.day10.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterPlaceGlowcombHive,
                        1
                    ),
                    new GleamriseGoalRequirement(
                        CounterCollectStarhoney,
                        1
                    )
                ],
                RewardItemId: DataCatalog.MistsongMintSeedId,
                RewardItemCount: 1
            ),
            new(
                "gleamrise_day_11_star_chicken_ready",
                11,
                "gleamrise.goal.day11.title",
                "gleamrise.goal.day11.description",
                "gleamrise.goal.day11.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterAnimalFeedPrepared,
                        1
                    ),
                    new GleamriseGoalRequirement(
                        InventoryFamilyCounter(DataCatalog.GlimmerpodId),
                        2
                    )
                ],
                RewardCoins: 50
            ),
            new(
                "gleamrise_day_12_egg_chain_ready",
                12,
                "gleamrise.goal.day12.title",
                "gleamrise.goal.day12.description",
                "gleamrise.goal.day12.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterAnimalFirstEgg,
                        1
                    ),
                    new GleamriseGoalRequirement(CounterCollectProcessor, 2)
                ],
                RewardItemId: DataCatalog.StarbudSeedId,
                RewardItemCount: 2
            ),
            new(
                "gleamrise_day_13_sowing_festival",
                13,
                "gleamrise.goal.day13.title",
                "gleamrise.goal.day13.description",
                "gleamrise.goal.day13.hint",
                [
                    new GleamriseGoalRequirement(CounterFestivalJoined, 1),
                    new GleamriseGoalRequirement(
                        CounterCompletedNonFinalGoals,
                        8
                    )
                ],
                RewardCoins: 80
            ),
            new(
                "gleamrise_day_14_season_close",
                14,
                "gleamrise.goal.day14.title",
                "gleamrise.goal.day14.description",
                "gleamrise.goal.day14.hint",
                [
                    new GleamriseGoalRequirement(
                        CounterCompletedNonFinalGoals,
                        SeasonCompletionRequiredGoals
                    )
                ],
                RewardCoins: 160,
                RewardItemId: DataCatalog.MoonplumSaplingId,
                RewardItemCount: 1,
                IsSeasonFinal: true
            )
        ];

        public static readonly IReadOnlyDictionary<string, GleamriseGoalDefinition>
            GoalsById = Goals.ToDictionary(
                goal => goal.Id,
                StringComparer.Ordinal
            );

        public static readonly IReadOnlySet<string> CounterIds =
            Goals.SelectMany(goal => goal.Requirements)
                .Select(requirement => requirement.CounterId)
                .Concat(
                [
                    CounterBuyGleamriseSeed,
                    CounterPlantGleamriseCrop,
                    CounterWaterGleamriseCrop,
                    CounterHarvestGleamriseCrop,
                    CounterFertilizeGleamriseSoil,
                    CounterStartProcessor,
                    CounterCollectProcessor,
                    CounterPlantMoonplumTree,
                    CounterHarvestMoonplum,
                    CounterPlaceGlowcombHive,
                    CounterCollectStarhoney,
                    CounterAnimalFeedPrepared,
                    CounterAnimalFirstEgg,
                    CounterFestivalJoined,
                    CounterCompletedNonFinalGoals
                ])
                .ToHashSet(StringComparer.Ordinal);
    }
}
