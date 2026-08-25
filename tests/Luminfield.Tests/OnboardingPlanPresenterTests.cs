using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class OnboardingPlanPresenterTests
{
    [Fact]
    public void PresenterCreatesDisplayCardsFromTheReadOnlyPlan()
    {
        var session = new GameSession();
        session.NewGame();
        var locale = TestLocale();

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(session),
            locale
        );

        Assert.False(display.IsEmpty);
        Assert.Equal("Next step", display.Cards[0].Title);
        Assert.Equal("Talk to Mira", display.Cards[0].Body);
        Assert.All(display.Cards, card => Assert.True(card.CanDismiss));
        Assert.Empty(display.CapabilityProgress);
    }

    [Fact]
    public void PresenterExposesTheLocalizationKeysNeededForIntegration()
    {
        Assert.Contains("onboarding.action.next", OnboardingPlanPresenter.RequiredLocalizationKeys);
        Assert.Contains("onboarding.shipping.pending", OnboardingPlanPresenter.RequiredLocalizationKeys);
        Assert.Contains("onboarding.exploration.first_route", OnboardingPlanPresenter.RequiredLocalizationKeys);
        Assert.Contains("onboarding.coverage.quest.action", OnboardingPlanPresenter.RequiredLocalizationKeys);
        Assert.Contains(
            "onboarding.coverage.exploration.result.complete",
            OnboardingPlanPresenter.RequiredLocalizationKeys
        );
        Assert.DoesNotContain("building.freeform.title", OnboardingPlanPresenter.RequiredLocalizationKeys);
    }

    [Fact]
    public void PresenterIncludesActionLocationAndCurrentResultInEachCard()
    {
        var session = new GameSession();
        session.NewGame();
        var locale = TestLocale();

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(session),
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session),
            locale
        );
        var quest = Assert.Single(display.Cards, card =>
            card.Kind == OnboardingPlanCardKind.Quest
        );

        Assert.Contains("Do the action", quest.Body, StringComparison.Ordinal);
        Assert.Contains("Find the entrance", quest.Body, StringComparison.Ordinal);
        Assert.Contains("Not started", quest.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void PresenterAddsSixCapabilityProgressItemsFromCoverageContract()
    {
        var session = new GameSession();
        session.NewGame();
        var locale = TestLocale();

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(session),
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session),
            locale
        );

        Assert.Equal(
            [
                OnboardingPlanCardKind.Quest,
                OnboardingPlanCardKind.Weather,
                OnboardingPlanCardKind.Shipping,
                OnboardingPlanCardKind.Processor,
                OnboardingPlanCardKind.Commission,
                OnboardingPlanCardKind.Exploration
            ],
            display.CapabilityProgress.Select(item => item.Kind)
        );
        Assert.All(
            display.CapabilityProgress,
            item =>
            {
                Assert.Equal(OnboardingCoverageState.NewGame, item.State);
                Assert.False(string.IsNullOrWhiteSpace(item.Title));
                Assert.Contains(
                    "ready to start",
                    item.Status,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    nameof(OnboardingCoverageState.NewGame),
                    item.Status,
                    StringComparison.Ordinal
                );
            }
        );
    }

    [Fact]
    public void DismissedCardsDoNotRemoveCapabilityProgressSummary()
    {
        var session = new GameSession();
        session.NewGame();
        var locale = TestLocale();

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(
                session,
                [
                    OnboardingPlanSystem.ShippingCardId,
                    OnboardingPlanSystem.ProcessorCardId
                ]
            ),
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session),
            locale
        );

        Assert.Equal(4, display.Cards.Count);
        Assert.DoesNotContain(
            display.Cards,
            card => card.Id == OnboardingPlanSystem.ShippingCardId
        );
        Assert.DoesNotContain(
            display.Cards,
            card => card.Id == OnboardingPlanSystem.ProcessorCardId
        );
        Assert.Equal(6, display.CapabilityProgress.Count);
        Assert.Contains(
            display.CapabilityProgress,
            item => item.CardId == OnboardingPlanSystem.ShippingCardId
        );
        Assert.Contains(
            display.CapabilityProgress,
            item => item.CardId == OnboardingPlanSystem.ProcessorCardId
        );
    }

    [Fact]
    public void CapabilityProgressUsesCoverageStatesAndLocalizedResultText()
    {
        var session = new GameSession();
        session.NewGame();
        session.InteractWithMira();
        session.Clock.AdvanceRealTime(GameClock.SecondsPerTick);
        session.Inventory.Add(DataCatalog.StarbudId, 3);
        Assert.True(session.QueueForShipping(DataCatalog.StarbudId).Succeeded);
        Assert.True(session.AcceptDailyCommission().Succeeded);
        Assert.True(session.StartProcessing(
            DataCatalog.StarbudPreserveRecipeId
        ).Succeeded);

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(session),
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(session),
            TestLocale()
        );

        Assert.All(
            display.CapabilityProgress,
            item =>
            {
                Assert.Equal(OnboardingCoverageState.InProgress, item.State);
                Assert.Contains("moving now", item.Status, StringComparison.Ordinal);
                Assert.DoesNotContain(
                    nameof(OnboardingCoverageState.InProgress),
                    item.Status,
                    StringComparison.Ordinal
                );
            }
        );
    }

    [Fact]
    public void PresenterFormatsStableValuesWithoutShowingWeatherIds()
    {
        var session = new GameSession();
        session.NewGame();
        var locale = TestLocale();

        var display = OnboardingPlanPresenter.Create(
            OnboardingPlanSystem.Create(session),
            locale
        );
        var weather = Assert.Single(display.Cards, card =>
            card.Kind == OnboardingPlanCardKind.Weather
        );

        Assert.Contains("Clear", weather.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(
            DataCatalog.ClearWeatherId,
            weather.Body,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void StartupFlagIsExplicit()
    {
        Assert.False(OnboardingPlanStartup.ShouldOpen([]));
        Assert.True(OnboardingPlanStartup.ShouldOpen([
            OnboardingPlanStartup.OpenFlag
        ]));
    }

    private static LocaleService TestLocale()
    {
        var locale = new LocaleService();
        var values = OnboardingPlanPresenter.RequiredLocalizationKeys
            .ToDictionary(key => key, key => key, StringComparer.Ordinal);
        values["onboarding.quest.title"] = "Next step";
        values["onboarding.weather.title"] = "Today and tomorrow";
        values["onboarding.shipping.title"] = "Shipping bin";
        values["onboarding.processor.title"] = "Homestead processing";
        values["onboarding.commission.title"] = "Village commission";
        values["onboarding.exploration.title"] = "First route";
        values["objective.talk"] = "Talk to Mira";
        values["onboarding.weather.body"] =
            "Today {0}; tomorrow {1}; day {2}.";
        values["weather.clear"] = "Clear";
        foreach (var prefix in new[]
        {
            "quest",
            "weather",
            "shipping",
            "processor",
            "commission",
            "exploration"
        })
        {
            values[$"onboarding.coverage.{prefix}.action"] = "Do the action";
            values[$"onboarding.coverage.{prefix}.location"] =
                "Find the entrance";
            values[$"onboarding.coverage.{prefix}.result.new_game"] =
                $"{prefix} ready to start";
            values[$"onboarding.coverage.{prefix}.result.in_progress"] =
                $"{prefix} moving now";
            values[$"onboarding.coverage.{prefix}.result.complete"] =
                $"{prefix} done";
        }
        values["onboarding.coverage.quest.result.new_game"] =
            "Not started; quest ready to start";
        locale.LoadJson(LocaleService.English, JsonSerializer.Serialize(values));
        locale.LoadJson(LocaleService.SimplifiedChinese, JsonSerializer.Serialize(values));
        return locale;
    }
}
