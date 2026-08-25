using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class MorningBriefingPresenterTests
{
    [Fact]
    public void RequiredLocalizationKeysAreStableAndExcludeBuildingScope()
    {
        var expected = new[]
        {
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
        };

        Assert.Equal(expected, MorningBriefingPresenter.RequiredLocalizationKeys);
        Assert.Contains(
            MorningBriefingOverlay.NavigateLocalizationKey,
            MorningBriefingOverlay.RequiredLocalizationKeys
        );
        Assert.Equal(
            MorningBriefingPresenter.RequiredLocalizationKeys.Count + 1,
            MorningBriefingOverlay.RequiredLocalizationKeys.Count
        );
        Assert.Equal(
            expected.Length,
            expected.Distinct(StringComparer.Ordinal).Count()
        );
        Assert.Equal(
            MorningBriefingOverlay.RequiredLocalizationKeys.Count,
            MorningBriefingOverlay.RequiredLocalizationKeys
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.DoesNotContain(
            MorningBriefingOverlay.RequiredLocalizationKeys,
            key => key.Contains(
                "building",
                StringComparison.OrdinalIgnoreCase
            ) ||
                key.Contains(
                    "construction",
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }

    [Fact]
    public void OverlayExposesNavigationRequestedEventContract()
    {
        var overlay = typeof(MorningBriefingOverlay);

        Assert.Equal(
            typeof(Action<WorldNavigationDestination>),
            overlay.GetEvent(nameof(MorningBriefingOverlay.NavigationRequested))
                ?.EventHandlerType
        );
        Assert.Equal(
            typeof(Action),
            overlay.GetEvent(nameof(MorningBriefingOverlay.CloseRequested))
                ?.EventHandlerType
        );
    }

    [Fact]
    public void OverlayNavigationSummaryUsesPresenterAndStandardButtons()
    {
        var source = File.ReadAllText(SourcePath(
            "src",
            "Game",
            "MorningBriefingOverlay.cs"
        ));

        Assert.Contains(
            "MorningBriefingNavigationPresenter.TargetFor",
            source
        );
        Assert.Contains(
            "ThemeFactory.Button(",
            source
        );
        Assert.Contains(
            "_locale.Tr(NavigateLocalizationKey, SummaryText(item))",
            source
        );
        Assert.Contains(
            "NavigationRequested?.Invoke(destination)",
            source
        );
        Assert.Contains(
            "_firstNavigationButton ??= button",
            source
        );
    }

    [Fact]
    public void OverlayNavigationSummaryDoesNotWriteSessionOrSaveState()
    {
        var source = File.ReadAllText(SourcePath(
            "src",
            "Game",
            "MorningBriefingOverlay.cs"
        ));

        Assert.DoesNotContain("SaveNow(", source);
        Assert.DoesNotContain(".Capture(", source);
        Assert.DoesNotContain(".Restore(", source);
        Assert.DoesNotContain("MarkMorningBriefingShown", source);
        Assert.DoesNotContain("new GameSession(", source);
        Assert.DoesNotContain("new SaveService(", source);
        Assert.Contains("session.Changed += RefreshText", source);
        Assert.Contains("session.Changed -= RefreshText", source);
    }

    [Fact]
    public void CreateFormatsLocalizationKeyAndNumberArguments()
    {
        var locale = new LocaleService();
        locale.LoadJson(
            LocaleService.English,
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["test.title"] = "Title",
                ["test.body"] = "Forecast {0}, count {1}",
                ["test.argument.weather"] = "Clear",
                ["test.action"] = "Go"
            })
        );
        var briefing = new MorningBriefing(
            [
                new MorningBriefingCard(
                    "test.card",
                    MorningBriefingCardKind.Weather,
                    MorningBriefingPriority.Secondary,
                    10,
                    "test.title",
                    "test.body",
                    [
                        MorningBriefingArgument.Key(
                            "test.argument.weather"
                        ),
                        MorningBriefingArgument.Count(2)
                    ],
                    "test.action",
                    "clear"
                )
            ]
        );

        var card = Assert.Single(
            MorningBriefingPresenter.Create(briefing, locale).Cards
        );

        Assert.Equal("Title", card.Title);
        Assert.Equal("Forecast Clear, count 2", card.Body);
        Assert.Equal("Go", card.Action);
        Assert.Equal("clear", card.ReferenceId);
    }

    [Fact]
    public void CreateSummarizesActionablePrimaryCardsBeforeSecondaryCards()
    {
        var locale = DecisionSummaryLocale();
        var briefing = new MorningBriefing(
            [
                TestCard(
                    "weather.primary",
                    MorningBriefingPriority.Primary,
                    10,
                    "",
                    "weather.title"
                ),
                TestCard(
                    "festival.secondary",
                    MorningBriefingPriority.Secondary,
                    30,
                    "festival.action",
                    "festival.title"
                ),
                TestCard(
                    "daily.optional",
                    MorningBriefingPriority.Optional,
                    5,
                    "daily.action",
                    "daily.title"
                ),
                TestCard(
                    "mail.primary",
                    MorningBriefingPriority.Primary,
                    20,
                    "mail.action",
                    "mail.title"
                ),
                TestCard(
                    "friend.primary",
                    MorningBriefingPriority.Primary,
                    40,
                    "friend.action",
                    "friend.title"
                ),
                TestCard(
                    "board.primary",
                    MorningBriefingPriority.Primary,
                    50,
                    "board.action",
                    "board.title"
                ),
                TestCard(
                    "region.secondary",
                    MorningBriefingPriority.Secondary,
                    70,
                    "region.action",
                    "region.title"
                )
            ]
        );

        var display = MorningBriefingPresenter.Create(briefing, locale);

        Assert.Equal(
            ["mail.primary", "friend.primary", "board.primary"],
            display.DecisionSummary.Select(item => item.Id)
        );
        Assert.Equal(
            ["Check mail", "Find friend", "Open board"],
            display.DecisionSummary.Select(item => item.Action)
        );
        Assert.Equal(
            MorningBriefingPresenter.MaxDecisionSummaryItems,
            display.DecisionSummary.Count
        );
    }

    [Fact]
    public void CreateFillsDecisionSummaryWithSecondaryCardsWhenUnderThreePrimary()
    {
        var locale = DecisionSummaryLocale();
        var briefing = new MorningBriefing(
            [
                TestCard(
                    "weather.primary",
                    MorningBriefingPriority.Primary,
                    10,
                    "",
                    "weather.title"
                ),
                TestCard(
                    "region.secondary",
                    MorningBriefingPriority.Secondary,
                    70,
                    "region.action",
                    "region.title"
                ),
                TestCard(
                    "daily.optional",
                    MorningBriefingPriority.Optional,
                    5,
                    "daily.action",
                    "daily.title"
                ),
                TestCard(
                    "mail.primary",
                    MorningBriefingPriority.Primary,
                    20,
                    "mail.action",
                    "mail.title"
                ),
                TestCard(
                    "festival.secondary",
                    MorningBriefingPriority.Secondary,
                    30,
                    "festival.action",
                    "festival.title"
                )
            ]
        );

        var display = MorningBriefingPresenter.Create(briefing, locale);

        Assert.Equal(
            ["mail.primary", "festival.secondary", "region.secondary"],
            display.DecisionSummary.Select(item => item.Id)
        );
        Assert.DoesNotContain(
            display.DecisionSummary,
            item => item.Id == "weather.primary" || item.Id == "daily.optional"
        );
    }

    private static MorningBriefingCard TestCard(
        string id,
        MorningBriefingPriority priority,
        int sortOrder,
        string actionKey,
        string titleKey
    ) => new(
        id,
        MorningBriefingCardKind.DailyCommission,
        priority,
        sortOrder,
        titleKey,
        "test.body",
        [],
        actionKey,
        id
    );

    private static LocaleService DecisionSummaryLocale()
    {
        var locale = new LocaleService();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.body"] = "Body",
            ["weather.title"] = "Weather",
            ["festival.title"] = "Festival",
            ["festival.action"] = "Visit festival",
            ["daily.title"] = "Daily",
            ["daily.action"] = "Open daily",
            ["mail.title"] = "Mail",
            ["mail.action"] = "Check mail",
            ["friend.title"] = "Friend",
            ["friend.action"] = "Find friend",
            ["board.title"] = "Board",
            ["board.action"] = "Open board",
            ["region.title"] = "Region",
            ["region.action"] = "Explore region"
        };
        locale.LoadJson(LocaleService.English, JsonSerializer.Serialize(values));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            JsonSerializer.Serialize(values)
        );
        return locale;
    }

    private static string SourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Luminfield.csproj")))
            {
                return Path.Combine([directory.FullName, .. parts]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
