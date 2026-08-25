using Luminfield.Core;

namespace Luminfield.Game;

public sealed record OnboardingPlanDisplayCard(
    string Id,
    OnboardingPlanCardKind Kind,
    OnboardingPlanPriority Priority,
    string Title,
    string Body,
    bool CanDismiss
);

public sealed record OnboardingCapabilityProgressItem(
    string CardId,
    OnboardingPlanCardKind Kind,
    OnboardingCoverageState State,
    string Title,
    string Status
);

public sealed record OnboardingPlanDisplay(
    IReadOnlyList<OnboardingPlanDisplayCard> Cards,
    IReadOnlyList<OnboardingCapabilityProgressItem> CapabilityProgress
)
{
    public bool IsEmpty => Cards.Count == 0;
}

public static class OnboardingPlanPresenter
{
    public static readonly IReadOnlyList<string> RequiredLocalizationKeys =
    [
        "onboarding.quest.title",
        "onboarding.weather.title",
        "onboarding.weather.body",
        "onboarding.shipping.title",
        "onboarding.shipping.pending",
        "onboarding.shipping.hint",
        "onboarding.processor.title",
        "onboarding.processor.ready",
        "onboarding.processor.active",
        "onboarding.processor.hint",
        "onboarding.commission.title",
        "onboarding.commission.offer",
        "onboarding.commission.ready",
        "onboarding.commission.active",
        "onboarding.exploration.title",
        "onboarding.exploration.first_route",
        "onboarding.exploration.return_route",
        "onboarding.coverage.quest.action",
        "onboarding.coverage.quest.location",
        "onboarding.coverage.quest.result.new_game",
        "onboarding.coverage.quest.result.in_progress",
        "onboarding.coverage.quest.result.complete",
        "onboarding.coverage.weather.action",
        "onboarding.coverage.weather.location",
        "onboarding.coverage.weather.result.new_game",
        "onboarding.coverage.weather.result.in_progress",
        "onboarding.coverage.weather.result.complete",
        "onboarding.coverage.shipping.action",
        "onboarding.coverage.shipping.location",
        "onboarding.coverage.shipping.result.new_game",
        "onboarding.coverage.shipping.result.in_progress",
        "onboarding.coverage.shipping.result.complete",
        "onboarding.coverage.processor.action",
        "onboarding.coverage.processor.location",
        "onboarding.coverage.processor.result.new_game",
        "onboarding.coverage.processor.result.in_progress",
        "onboarding.coverage.processor.result.complete",
        "onboarding.coverage.commission.action",
        "onboarding.coverage.commission.location",
        "onboarding.coverage.commission.result.new_game",
        "onboarding.coverage.commission.result.in_progress",
        "onboarding.coverage.commission.result.complete",
        "onboarding.coverage.exploration.action",
        "onboarding.coverage.exploration.location",
        "onboarding.coverage.exploration.result.new_game",
        "onboarding.coverage.exploration.result.in_progress",
        "onboarding.coverage.exploration.result.complete",
        "onboarding.action.next",
        "onboarding.action.dismiss",
        "onboarding.action.close"
    ];

    public static OnboardingPlanDisplay Create(
        OnboardingPlan plan,
        LocaleService locale
    ) => Create(plan, null, locale);

    public static OnboardingPlanDisplay Create(
        OnboardingPlan plan,
        OnboardingNinetyMinuteCoverageContract? coverage,
        LocaleService locale
    )
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(locale);
        var coverageByCard = coverage?.Capabilities.ToDictionary(
            capability => capability.CardId,
            StringComparer.Ordinal
        );
        return new OnboardingPlanDisplay(
            plan.Cards.Select(card => DisplayCard(
                card,
                coverageByCard,
                locale
            )).ToArray(),
            CapabilityProgress(coverage, locale)
        );
    }

    private static IReadOnlyList<OnboardingCapabilityProgressItem>
        CapabilityProgress(
            OnboardingNinetyMinuteCoverageContract? coverage,
            LocaleService locale
        )
    {
        if (coverage is null)
        {
            return [];
        }

        return coverage.Capabilities
            .Select(capability => new OnboardingCapabilityProgressItem(
                capability.CardId,
                capability.Kind,
                capability.State,
                locale.Tr(TitleKey(capability.Kind)),
                locale.Tr(capability.Prompt.ResultKey)
            ))
            .ToArray();
    }

    private static OnboardingPlanDisplayCard DisplayCard(
        OnboardingPlanCard card,
        IReadOnlyDictionary<string, OnboardingCapabilityCoverage>? coverageByCard,
        LocaleService locale
    )
    {
        var body = locale.Tr(card.BodyKey, BodyArguments(card, locale));
        if (coverageByCard is not null &&
            coverageByCard.TryGetValue(card.Id, out var coverage))
        {
            body = string.Join(
                "\n",
                body,
                locale.Tr(coverage.Prompt.ActionKey),
                locale.Tr(coverage.Prompt.LocationKey),
                locale.Tr(coverage.Prompt.ResultKey)
            );
        }

        return new OnboardingPlanDisplayCard(
            card.Id,
            card.Kind,
            card.Priority,
            locale.Tr(card.TitleKey),
            body,
            card.CanDismiss
        );
    }

    private static object[] BodyArguments(
        OnboardingPlanCard card,
        LocaleService locale
    )
    {
        string Value(string key) => card.Values.TryGetValue(key, out var value)
            ? value
            : "0";

        switch (card.Kind)
        {
            case OnboardingPlanCardKind.Quest:
                return [Value("count")];
            case OnboardingPlanCardKind.Weather:
                return [
                    locale.Tr($"weather.{Value("currentWeatherId")}"),
                    locale.Tr($"weather.{Value("forecastWeatherId")}"),
                    Value("seasonDay")
                ];
            case OnboardingPlanCardKind.Shipping:
                return [Value("pendingItems"), Value("pendingValue")];
            case OnboardingPlanCardKind.Processor:
                return [Value("readyCount"), Value("remainingNights")];
            case OnboardingPlanCardKind.Commission:
                return [Value("progress"), Value("required")];
            case OnboardingPlanCardKind.Exploration:
                return [Value("discoveredChunks")];
            default:
                return [];
        }
    }

    private static string TitleKey(OnboardingPlanCardKind kind) =>
        kind switch
        {
            OnboardingPlanCardKind.Quest => "onboarding.quest.title",
            OnboardingPlanCardKind.Weather => "onboarding.weather.title",
            OnboardingPlanCardKind.Shipping => "onboarding.shipping.title",
            OnboardingPlanCardKind.Processor => "onboarding.processor.title",
            OnboardingPlanCardKind.Commission => "onboarding.commission.title",
            OnboardingPlanCardKind.Exploration => "onboarding.exploration.title",
            _ => "onboarding.quest.title"
        };
}
