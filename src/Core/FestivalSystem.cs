namespace Luminfield.Core;

public sealed record FestivalStageDefinition(
    string Id,
    string TitleKey,
    string DescriptionKey,
    int RewardTokens,
    string RewardItemId = "",
    int RewardItemCount = 0
);

public sealed record FestivalExchangeDefinition(
    string ItemId,
    int TokenCost,
    int Count,
    string DescriptionKey
);

public sealed record FestivalStageSnapshot(
    FestivalStageDefinition Definition,
    bool Completed,
    bool IsCurrent
);

public sealed record FestivalExchangeSnapshot(
    FestivalExchangeDefinition Definition,
    int OwnedTokens,
    int OwnedItemCount,
    bool CanAfford
);

public sealed record FestivalActivitySnapshot(
    bool IsFestivalDay,
    bool Joined,
    bool Completed,
    IReadOnlyList<FestivalStageSnapshot> Stages,
    IReadOnlyList<FestivalExchangeSnapshot> ExchangeItems
);

public sealed class FestivalSystem
{
    public const string GleamriseSowingFestivalId =
        "gleamrise_sowing_festival";
    public const int FestivalSeasonDay = 7;
    public const string StageLayMoonstoneRows =
        "festival_stage_lay_moonstone_rows";
    public const string StageSowDawnlace =
        "festival_stage_sow_dawnlace";
    public const string StageTuneGlimmerpod =
        "festival_stage_tune_glimmerpod";
    public static readonly GridPosition GateCell = new(101, 50);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition ActivityCell = new(20, 12);
    public static readonly GridPosition ExchangeStallCell = new(29, 13);

    private readonly HashSet<string> _completedStages =
        new(StringComparer.Ordinal);

    private int _year = 1;
    private bool _joined;

    public int Year => _year;
    public bool Joined => _joined;
    public bool Completed => _completedStages.Count == Catalog.Stages.Count;
    public IReadOnlySet<string> CompletedStageIds => _completedStages;

    public event Action? Changed;

    public void Reset(int day)
    {
        Restore(CreateForDay(day), day);
    }

    public void Restore(FestivalSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _year = normalized.Year;
        _joined = normalized.Joined;
        _completedStages.Clear();
        foreach (var stageId in normalized.CompletedStageIds)
        {
            _completedStages.Add(stageId);
        }
        Changed?.Invoke();
    }

    public void RefreshForDay(int day)
    {
        if (CalendarSystem.SeasonId(day) != CalendarSystem.GleamriseSeasonId)
        {
            return;
        }

        var year = CalendarSystem.YearNumber(day);
        if (year != _year)
        {
            Restore(CreateForDay(day), day);
        }
    }

    public ActionResult Join(int day)
    {
        if (!IsFestivalDay(day))
        {
            return ActionResult.Fail("festival.gleamrise.closed");
        }

        if (!_joined)
        {
            _joined = true;
            Changed?.Invoke();
        }

        return ActionResult.Success(
            messageKey: "festival.gleamrise.joined"
        );
    }

    public ActionResult AdvanceStage(int day, Inventory inventory)
    {
        if (!IsFestivalDay(day))
        {
            return ActionResult.Fail("festival.gleamrise.closed");
        }

        if (!_joined)
        {
            Join(day);
        }

        var stage = Catalog.Stages.FirstOrDefault(definition =>
            !_completedStages.Contains(definition.Id)
        );
        if (stage is null)
        {
            return ActionResult.Fail("festival.gleamrise.completed");
        }

        if (!inventory.TryAddMany(StageRewards(stage)))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _completedStages.Add(stage.Id);
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: $"festival.gleamrise.{stage.Id}.completed"
        );
    }

    public ActionResult Exchange(string itemId, Inventory inventory)
    {
        if (!Catalog.ExchangeItemsByItemId.TryGetValue(
                itemId,
                out var exchange))
        {
            return ActionResult.Fail("festival.exchange.unknown");
        }

        if (inventory.Count(DataCatalog.GleamriseFestivalTokenId) <
            exchange.TokenCost)
        {
            return ActionResult.Fail("festival.exchange.need_tokens");
        }

        if (!inventory.CanAdd(exchange.ItemId, exchange.Count))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        if (!inventory.TryExchange(
                [
                    new CraftingIngredient(
                        DataCatalog.GleamriseFestivalTokenId,
                        exchange.TokenCost
                    )
                ],
                exchange.ItemId,
                exchange.Count))
        {
            return ActionResult.Fail("festival.exchange.changed");
        }

        Changed?.Invoke();
        return ActionResult.Success(messageKey: "festival.exchange.done");
    }

    public FestivalActivitySnapshot Snapshot(int day, Inventory inventory)
    {
        var currentStage = Catalog.Stages.FirstOrDefault(definition =>
            !_completedStages.Contains(definition.Id)
        );
        var stages = Catalog.Stages
            .Select(definition => new FestivalStageSnapshot(
                definition,
                _completedStages.Contains(definition.Id),
                currentStage?.Id == definition.Id
            ))
            .ToArray();
        var ownedTokens = inventory.Count(DataCatalog.GleamriseFestivalTokenId);
        var exchanges = Catalog.ExchangeItems
            .Select(definition => new FestivalExchangeSnapshot(
                definition,
                ownedTokens,
                inventory.Count(definition.ItemId),
                ownedTokens >= definition.TokenCost
            ))
            .ToArray();
        return new FestivalActivitySnapshot(
            IsFestivalDay(day),
            _joined,
            Completed,
            stages,
            exchanges
        );
    }

    public FestivalSave Capture() => new()
    {
        Year = _year,
        FestivalId = GleamriseSowingFestivalId,
        Joined = _joined,
        CompletedStageIds = _completedStages
            .Order(StringComparer.Ordinal)
            .ToList()
    };

    public static bool IsFestivalDay(int day) =>
        CalendarSystem.SeasonId(day) == CalendarSystem.GleamriseSeasonId &&
        CalendarSystem.SeasonDay(day) == FestivalSeasonDay;

    public static FestivalSave NormalizeSave(
        FestivalSave? save,
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

        var completed = (save.CompletedStageIds ?? [])
            .Where(Catalog.StageIds.Contains)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(stageId => Catalog.StageOrder[stageId])
            .ToList();

        return new FestivalSave
        {
            Year = Math.Max(1, save.Year),
            FestivalId = GleamriseSowingFestivalId,
            Joined = save.Joined || completed.Count > 0,
            CompletedStageIds = completed
        };
    }

    private static FestivalSave CreateForDay(int day) => new()
    {
        Year = CalendarSystem.YearNumber(day),
        FestivalId = GleamriseSowingFestivalId
    };

    private static IReadOnlyList<CraftingIngredient> StageRewards(
        FestivalStageDefinition stage
    )
    {
        var rewards = new List<CraftingIngredient>();
        if (stage.RewardTokens > 0)
        {
            rewards.Add(new CraftingIngredient(
                DataCatalog.GleamriseFestivalTokenId,
                stage.RewardTokens
            ));
        }

        if (!string.IsNullOrWhiteSpace(stage.RewardItemId) &&
            stage.RewardItemCount > 0)
        {
            rewards.Add(new CraftingIngredient(
                stage.RewardItemId,
                stage.RewardItemCount
            ));
        }

        return rewards;
    }

    private static class Catalog
    {
        public static readonly IReadOnlyList<FestivalStageDefinition> Stages =
        [
            new(
                StageLayMoonstoneRows,
                "festival.stage.rows.title",
                "festival.stage.rows.description",
                2,
                DataCatalog.DawnlaceSeedId,
                1
            ),
            new(
                StageSowDawnlace,
                "festival.stage.dawnlace.title",
                "festival.stage.dawnlace.description",
                2,
                DataCatalog.GlimmerpodSeedId,
                1
            ),
            new(
                StageTuneGlimmerpod,
                "festival.stage.glimmerpod.title",
                "festival.stage.glimmerpod.description",
                3,
                DataCatalog.StarsoilFertilizerId,
                1
            )
        ];

        public static readonly IReadOnlyList<FestivalExchangeDefinition>
            ExchangeItems =
            [
                new(
                    DataCatalog.DawnlaceSeedId,
                    1,
                    1,
                    "festival.exchange.dawnlace"
                ),
                new(
                    DataCatalog.GlimmerpodSeedId,
                    1,
                    1,
                    "festival.exchange.glimmerpod"
                ),
                new(
                    DataCatalog.MistsongMintSeedId,
                    1,
                    1,
                    "festival.exchange.mint"
                ),
                new(
                    DataCatalog.CometTuberSeedId,
                    1,
                    1,
                    "festival.exchange.tuber"
                ),
                new(
                    DataCatalog.StarsoilFertilizerId,
                    2,
                    1,
                    "festival.exchange.fertilizer"
                ),
                new(
                    DataCatalog.MoonplumSaplingId,
                    4,
                    1,
                    "festival.exchange.sapling"
                )
            ];

        public static readonly IReadOnlySet<string> StageIds =
            Stages.Select(stage => stage.Id)
                .ToHashSet(StringComparer.Ordinal);

        public static readonly IReadOnlyDictionary<string, int> StageOrder =
            Stages.Select((stage, index) => new { stage.Id, index })
                .ToDictionary(pair => pair.Id, pair => pair.index);

        public static readonly IReadOnlyDictionary<string, FestivalExchangeDefinition>
            ExchangeItemsByItemId =
                ExchangeItems.ToDictionary(
                    item => item.ItemId,
                    StringComparer.Ordinal
                );
    }
}
