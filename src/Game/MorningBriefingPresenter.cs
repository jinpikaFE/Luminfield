using Luminfield.Core;

namespace Luminfield.Game;

public sealed record MorningBriefingDisplayCard(
    string Id,
    MorningBriefingCardKind Kind,
    MorningBriefingPriority Priority,
    string Title,
    string Body,
    string Action,
    string ReferenceId
);

public sealed record MorningBriefingDecisionSummaryItem(
    string Id,
    MorningBriefingCardKind Kind,
    MorningBriefingPriority Priority,
    string Title,
    string Action,
    string ReferenceId
);

public sealed record MorningBriefingDisplay(
    IReadOnlyList<MorningBriefingDisplayCard> Cards,
    IReadOnlyList<MorningBriefingDecisionSummaryItem> DecisionSummary
)
{
    public bool IsEmpty => Cards.Count == 0;
}

public static class MorningBriefingPresenter
{
    public const int MaxDecisionSummaryItems = 3;

    public static readonly IReadOnlyList<string> RequiredLocalizationKeys =
    [
        "morning.briefing.title",
        "morning.briefing.close",
        "morning.briefing.empty",
        "morning.weather.title",
        "morning.weather.clear",
        "morning.weather.rain",
        "morning.weather.stardust_wind",
        "morning.weather.longnight_snow",
        "morning.mail.title",
        "morning.mail.none",
        "morning.mail.unread",
        "morning.festival.title",
        "morning.festival.open_today",
        "morning.festival.today_later",
        "morning.festival.tomorrow",
        "morning.festival.none",
        "morning.character_event.title",
        "morning.character_event.ready_one",
        "morning.character_event.ready_many",
        "morning.character_event.none",
        "morning.daily_commission.title",
        "morning.daily_commission.not_accepted",
        "morning.daily_commission.in_progress",
        "morning.daily_commission.ready",
        "morning.daily_commission.claimed",
        "morning.weekly_commission.title",
        "morning.weekly_commission.not_accepted",
        "morning.weekly_commission.in_progress",
        "morning.weekly_commission.ready_stage",
        "morning.weekly_commission.ready_final",
        "morning.weekly_commission.claimed",
        "morning.region.title",
        "morning.region.undiscovered_landmark",
        "morning.region.all_known",
        "morning.action.check_mail",
        "morning.action.visit_festival",
        "morning.action.prepare_festival",
        "morning.action.find_friend",
        "morning.action.open_commission_board",
        "morning.action.explore_region"
    ];

    public static MorningBriefingDisplay Create(
        MorningBriefing briefing,
        LocaleService locale
    )
    {
        ArgumentNullException.ThrowIfNull(briefing);
        ArgumentNullException.ThrowIfNull(locale);

        var cards = briefing.Cards
            .Select(card => ToDisplayCard(card, locale))
            .ToList();
        var summary = briefing.Cards
            .Where(IsDecisionSummaryCandidate)
            .OrderBy(card => DecisionPriorityRank(card.Priority))
            .ThenBy(card => card.SortOrder)
            .ThenBy(card => card.Id, StringComparer.Ordinal)
            .Take(MaxDecisionSummaryItems)
            .Select(card => new MorningBriefingDecisionSummaryItem(
                card.Id,
                card.Kind,
                card.Priority,
                locale.Tr(card.TitleKey),
                locale.Tr(card.ActionKey),
                card.ReferenceId
            ))
            .ToList();

        return new MorningBriefingDisplay(
            cards,
            summary
        );
    }

    private static MorningBriefingDisplayCard ToDisplayCard(
        MorningBriefingCard card,
        LocaleService locale
    ) => new(
        card.Id,
        card.Kind,
        card.Priority,
        locale.Tr(card.TitleKey),
        locale.Tr(
            card.BodyKey,
            card.Arguments.Select(argument =>
                FormatArgument(argument, locale)).ToArray()
        ),
        string.IsNullOrWhiteSpace(card.ActionKey)
            ? string.Empty
            : locale.Tr(card.ActionKey),
        card.ReferenceId
    );

    private static bool IsDecisionSummaryCandidate(
        MorningBriefingCard card
    ) =>
        !string.IsNullOrWhiteSpace(card.ActionKey) &&
        card.Priority is MorningBriefingPriority.Primary
            or MorningBriefingPriority.Secondary;

    private static int DecisionPriorityRank(MorningBriefingPriority priority) =>
        priority switch
        {
            MorningBriefingPriority.Primary => 0,
            MorningBriefingPriority.Secondary => 1,
            _ => 2
        };

    private static object FormatArgument(
        MorningBriefingArgument argument,
        LocaleService locale
    ) => argument.Kind switch
    {
        MorningBriefingArgumentKind.LocalizationKey =>
            locale.Tr(argument.LocalizationKey),
        MorningBriefingArgumentKind.Number => argument.Number,
        _ => string.Empty
    };
}
