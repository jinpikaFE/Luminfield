namespace Luminfield.Core;

public enum OnboardingPlanCardKind
{
    Quest,
    Weather,
    Shipping,
    Processor,
    Commission,
    Exploration
}

public enum OnboardingPlanPriority
{
    Primary,
    Secondary,
    Optional
}

public enum OnboardingCoverageState
{
    NewGame,
    InProgress,
    Complete
}

public sealed record OnboardingPlanCard(
    string Id,
    OnboardingPlanCardKind Kind,
    OnboardingPlanPriority Priority,
    string TitleKey,
    string BodyKey,
    IReadOnlyDictionary<string, string> Values,
    bool CanDismiss
);

public sealed record OnboardingPlan(IReadOnlyList<OnboardingPlanCard> Cards)
{
    public bool IsEmpty => Cards.Count == 0;
}

public sealed record OnboardingCoveragePrompt(
    string ActionKey,
    string LocationKey,
    string ResultKey
)
{
    public bool HasActionLocationAndResultKeys =>
        !string.IsNullOrWhiteSpace(ActionKey) &&
        !string.IsNullOrWhiteSpace(LocationKey) &&
        !string.IsNullOrWhiteSpace(ResultKey);
}

public sealed record OnboardingCapabilityCoverage(
    string CardId,
    OnboardingPlanCardKind Kind,
    OnboardingCoverageState State,
    OnboardingCoveragePrompt Prompt,
    IReadOnlyDictionary<string, string> Evidence
)
{
    public bool HasActionLocationAndResultPrompts =>
        Prompt.HasActionLocationAndResultKeys;
}

public sealed record OnboardingNinetyMinuteCoverageContract(
    int WindowMinutes,
    IReadOnlyList<OnboardingCapabilityCoverage> Capabilities
)
{
    public bool HasCompletePromptCoverage =>
        Capabilities.All(capability =>
            capability.HasActionLocationAndResultPrompts
        );
}

public sealed record OnboardingFlowStepAudit(
    string StepId,
    OnboardingPlanCardKind Kind,
    string ActionKey,
    string LocationKey,
    string ResultKey,
    bool Succeeded,
    string FailureKey,
    IReadOnlyDictionary<string, string> Evidence
)
{
    public bool HasStableEvidence => Evidence.Count > 0;
}

public sealed record OnboardingNinetyMinuteFlowAudit(
    IReadOnlyList<OnboardingFlowStepAudit> Steps
)
{
    public bool Succeeded => Steps.All(step => step.Succeeded);
    public IReadOnlyList<OnboardingFlowStepAudit> Failures =>
        Steps.Where(step => !step.Succeeded).ToArray();
}

public static class OnboardingPlanSystem
{
    public const int OpeningCoverageWindowMinutes = 90;

    public const string QuestCardId = "onboarding.quest";
    public const string WeatherCardId = "onboarding.weather";
    public const string ShippingCardId = "onboarding.shipping";
    public const string ProcessorCardId = "onboarding.processor";
    public const string CommissionCardId = "onboarding.commission";
    public const string ExplorationCardId = "onboarding.exploration";

    public static OnboardingPlan Create(
        GameSession session,
        IEnumerable<string>? dismissedCardIds = null
    )
    {
        ArgumentNullException.ThrowIfNull(session);
        var dismissed = new HashSet<string>(
            dismissedCardIds ?? [],
            StringComparer.Ordinal
        );
        var cards = new List<OnboardingPlanCard>
        {
            QuestCard(session),
            WeatherCard(session),
            ShippingCard(session),
            ProcessorCard(session),
            CommissionCard(session),
            ExplorationCard(session)
        };

        return new OnboardingPlan(
            cards
                .Where(card => !card.CanDismiss || !dismissed.Contains(card.Id))
                .ToArray()
        );
    }

    public static OnboardingNinetyMinuteCoverageContract CreateNinetyMinuteCoverageContract(
        GameSession session
    )
    {
        ArgumentNullException.ThrowIfNull(session);
        return new OnboardingNinetyMinuteCoverageContract(
            OpeningCoverageWindowMinutes,
            [
                CapabilityCoverage(
                    QuestCardId,
                    OnboardingPlanCardKind.Quest,
                    QuestCoverageState(session),
                    Values([
                        Pair("stage", session.Quest.Stage.ToString()),
                        Pair("count", session.Quest.ObjectiveCount.ToString())
                    ])
                ),
                CapabilityCoverage(
                    WeatherCardId,
                    OnboardingPlanCardKind.Weather,
                    WeatherCoverageState(session),
                    Values([
                        Pair("day", session.Clock.Day.ToString()),
                        Pair("minuteOfDay", session.Clock.MinuteOfDay.ToString()),
                        Pair("currentWeatherId", session.Weather.CurrentId),
                        Pair("forecastWeatherId", session.Weather.ForecastId)
                    ])
                ),
                CapabilityCoverage(
                    ShippingCardId,
                    OnboardingPlanCardKind.Shipping,
                    ShippingCoverageState(session),
                    Values([
                        Pair("pendingItems", session.Shipping.PendingItemCount.ToString()),
                        Pair("pendingValue", session.Shipping.PendingValue.ToString()),
                        Pair("lastSettlementDay", session.Shipping.LastSettlement.Day.ToString()),
                        Pair("lastSettlementItems", session.Shipping.LastSettlement.TotalItems.ToString()),
                        Pair("lastSettlementCoins", session.Shipping.LastSettlement.TotalCoins.ToString())
                    ])
                ),
                CapabilityCoverage(
                    ProcessorCardId,
                    OnboardingPlanCardKind.Processor,
                    ProcessorCoverageState(session),
                    Values([
                        Pair("activeRecipeId", session.Processor.ActiveRecipeId),
                        Pair("readyCount", session.Processor.ReadyCount.ToString()),
                        Pair("remainingNights", session.Processor.RemainingNights.ToString())
                    ])
                ),
                CapabilityCoverage(
                    CommissionCardId,
                    OnboardingPlanCardKind.Commission,
                    CommissionCoverageState(session),
                    Values([
                        Pair("commissionId", session.Commission.Current.Id),
                        Pair("accepted", session.Commission.Accepted.ToString()),
                        Pair("claimed", session.Commission.Claimed.ToString()),
                        Pair("progress", session.Commission.DisplayProgress(session.Inventory).ToString()),
                        Pair("required", session.Commission.Current.RequiredCount.ToString())
                    ])
                ),
                CapabilityCoverage(
                    ExplorationCardId,
                    OnboardingPlanCardKind.Exploration,
                    ExplorationCoverageState(session),
                    Values([
                        Pair("discoveredChunks", session.Exploration.DiscoveredChunks.Count.ToString()),
                        Pair("minuteOfDay", session.Clock.MinuteOfDay.ToString())
                    ])
                )
            ]
        );
    }

    private static OnboardingPlanCard QuestCard(GameSession session) =>
        new(
            QuestCardId,
            OnboardingPlanCardKind.Quest,
            OnboardingPlanPriority.Primary,
            "onboarding.quest.title",
            session.Quest.ObjectiveKey,
            Values([
                Pair("stage", session.Quest.Stage.ToString()),
                Pair("count", session.Quest.ObjectiveCount.ToString())
            ]),
            CanDismiss: true
        );

    private static OnboardingPlanCard WeatherCard(GameSession session) =>
        new(
            WeatherCardId,
            OnboardingPlanCardKind.Weather,
            OnboardingPlanPriority.Secondary,
            "onboarding.weather.title",
            "onboarding.weather.body",
            Values([
                Pair("currentWeatherId", session.Weather.CurrentId),
                Pair("forecastWeatherId", session.Weather.ForecastId),
                Pair("seasonDay", CalendarSystem.SeasonDay(session.Clock.Day).ToString())
            ]),
            CanDismiss: true
        );

    private static OnboardingPlanCard ShippingCard(GameSession session)
    {
        var bodyKey = session.Shipping.PendingItemCount > 0
            ? "onboarding.shipping.pending"
            : "onboarding.shipping.hint";
        var priority = session.Shipping.PendingItemCount > 0
            ? OnboardingPlanPriority.Secondary
            : OnboardingPlanPriority.Optional;

        return new OnboardingPlanCard(
            ShippingCardId,
            OnboardingPlanCardKind.Shipping,
            priority,
            "onboarding.shipping.title",
            bodyKey,
            Values([
                Pair("pendingItems", session.Shipping.PendingItemCount.ToString()),
                Pair("pendingValue", session.Shipping.PendingValue.ToString())
            ]),
            CanDismiss: true
        );
    }

    private static OnboardingPlanCard ProcessorCard(GameSession session)
    {
        var bodyKey = "onboarding.processor.hint";
        var priority = OnboardingPlanPriority.Optional;
        if (session.Processor.ReadyCount > 0)
        {
            bodyKey = "onboarding.processor.ready";
            priority = OnboardingPlanPriority.Primary;
        }
        else if (!session.Processor.IsIdle)
        {
            bodyKey = "onboarding.processor.active";
            priority = OnboardingPlanPriority.Secondary;
        }

        return new OnboardingPlanCard(
            ProcessorCardId,
            OnboardingPlanCardKind.Processor,
            priority,
            "onboarding.processor.title",
            bodyKey,
            Values([
                Pair("readyCount", session.Processor.ReadyCount.ToString()),
                Pair("remainingNights", session.Processor.RemainingNights.ToString())
            ]),
            CanDismiss: true
        );
    }

    private static OnboardingPlanCard CommissionCard(GameSession session)
    {
        var bodyKey = "onboarding.commission.offer";
        var priority = OnboardingPlanPriority.Optional;
        if (session.Commission.IsReady(session.Inventory))
        {
            bodyKey = "onboarding.commission.ready";
            priority = OnboardingPlanPriority.Primary;
        }
        else if (session.Commission.Accepted && !session.Commission.Claimed)
        {
            bodyKey = "onboarding.commission.active";
            priority = OnboardingPlanPriority.Secondary;
        }

        return new OnboardingPlanCard(
            CommissionCardId,
            OnboardingPlanCardKind.Commission,
            priority,
            "onboarding.commission.title",
            bodyKey,
            Values([
                Pair("commissionId", session.Commission.Current.Id),
                Pair("progress", session.Commission.DisplayProgress(session.Inventory).ToString()),
                Pair("required", session.Commission.Current.RequiredCount.ToString())
            ]),
            CanDismiss: true
        );
    }

    private static OnboardingPlanCard ExplorationCard(GameSession session) =>
        new(
            ExplorationCardId,
            OnboardingPlanCardKind.Exploration,
            OnboardingPlanPriority.Optional,
            "onboarding.exploration.title",
            session.Exploration.DiscoveredChunks.Count <= 1
                ? "onboarding.exploration.first_route"
                : "onboarding.exploration.return_route",
            Values([
                Pair("discoveredChunks", session.Exploration.DiscoveredChunks.Count.ToString())
            ]),
            CanDismiss: true
        );

    private static OnboardingCapabilityCoverage CapabilityCoverage(
        string cardId,
        OnboardingPlanCardKind kind,
        OnboardingCoverageState state,
        IReadOnlyDictionary<string, string> evidence
    ) => new(
        cardId,
        kind,
        state,
        CoveragePrompt(kind, state),
        evidence
    );

    private static OnboardingCoveragePrompt CoveragePrompt(
        OnboardingPlanCardKind kind,
        OnboardingCoverageState state
    )
    {
        var prefix = CoveragePrefix(kind);
        var stateKey = CoverageStateKey(state);
        return new OnboardingCoveragePrompt(
            $"onboarding.coverage.{prefix}.action",
            $"onboarding.coverage.{prefix}.location",
            $"onboarding.coverage.{prefix}.result.{stateKey}"
        );
    }

    private static OnboardingCoverageState QuestCoverageState(GameSession session)
    {
        if (session.Quest.Stage == QuestStage.Complete)
        {
            return OnboardingCoverageState.Complete;
        }

        if (session.Quest.Stage == QuestStage.TalkToMira)
        {
            return OnboardingCoverageState.NewGame;
        }

        return OnboardingCoverageState.InProgress;
    }

    private static OnboardingCoverageState WeatherCoverageState(GameSession session)
    {
        if (session.Clock.Day > 1)
        {
            return OnboardingCoverageState.Complete;
        }

        if (session.Clock.MinuteOfDay > GameClock.StartMinute)
        {
            return OnboardingCoverageState.InProgress;
        }

        return OnboardingCoverageState.NewGame;
    }

    private static OnboardingCoverageState ShippingCoverageState(GameSession session)
    {
        if (session.Shipping.LastSettlement.TotalItems > 0)
        {
            return OnboardingCoverageState.Complete;
        }

        if (session.Shipping.PendingItemCount > 0)
        {
            return OnboardingCoverageState.InProgress;
        }

        return OnboardingCoverageState.NewGame;
    }

    private static OnboardingCoverageState ProcessorCoverageState(GameSession session)
    {
        if (session.Processor.ReadyCount > 0 ||
            CompendiumCatalog.ArtisanEntries.Any(entry =>
                session.Collection.IsDiscovered(entry.Id)
            ))
        {
            return OnboardingCoverageState.Complete;
        }

        if (!session.Processor.IsIdle)
        {
            return OnboardingCoverageState.InProgress;
        }

        return OnboardingCoverageState.NewGame;
    }

    private static OnboardingCoverageState CommissionCoverageState(GameSession session)
    {
        if (session.Commission.Claimed)
        {
            return OnboardingCoverageState.Complete;
        }

        if (session.Commission.Accepted ||
            session.Commission.IsReady(session.Inventory))
        {
            return OnboardingCoverageState.InProgress;
        }

        return OnboardingCoverageState.NewGame;
    }

    private static OnboardingCoverageState ExplorationCoverageState(GameSession session)
    {
        if (session.Exploration.DiscoveredChunks.Count > 1)
        {
            return OnboardingCoverageState.Complete;
        }

        if (session.Clock.MinuteOfDay > GameClock.StartMinute)
        {
            return OnboardingCoverageState.InProgress;
        }

        return OnboardingCoverageState.NewGame;
    }

    private static string CoveragePrefix(OnboardingPlanCardKind kind) =>
        kind switch
        {
            OnboardingPlanCardKind.Quest => "quest",
            OnboardingPlanCardKind.Weather => "weather",
            OnboardingPlanCardKind.Shipping => "shipping",
            OnboardingPlanCardKind.Processor => "processor",
            OnboardingPlanCardKind.Commission => "commission",
            OnboardingPlanCardKind.Exploration => "exploration",
            _ => "quest"
        };

    private static string CoverageStateKey(OnboardingCoverageState state) =>
        state switch
        {
            OnboardingCoverageState.NewGame => "new_game",
            OnboardingCoverageState.InProgress => "in_progress",
            OnboardingCoverageState.Complete => "complete",
            _ => "new_game"
        };

    private static KeyValuePair<string, string> Pair(string key, string value) =>
        new(key, value);

    private static IReadOnlyDictionary<string, string> Values(
        IEnumerable<KeyValuePair<string, string>> pairs
    ) => pairs.ToDictionary(
        pair => pair.Key,
        pair => pair.Value,
        StringComparer.Ordinal
    );
}
