using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class GleamriseSeasonGoalTests
{
    [Fact]
    public void CatalogProvidesFourteenDataDrivenSeasonGoals()
    {
        var session = new GameSession();
        session.NewGame();
        var goals = session.GleamriseSeasonGoals();

        Assert.Equal(CalendarSystem.DaysPerSeason, goals.Count);
        Assert.Equal(
            Enumerable.Range(1, CalendarSystem.DaysPerSeason),
            goals.Select(goal => goal.Definition.SeasonDay)
        );
        Assert.Equal(
            goals.Count,
            goals.Select(goal => goal.Definition.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(
            goals,
            goal =>
            {
                Assert.NotEmpty(goal.Definition.TitleKey);
                Assert.NotEmpty(goal.Definition.DescriptionKey);
                Assert.NotEmpty(goal.Definition.HintKey);
                Assert.NotEmpty(goal.Definition.Requirements);
            }
        );
        Assert.True(goals.Single(goal =>
            goal.Definition.IsSeasonFinal
        ).Definition.RewardCoins > 0);
    }

    [Fact]
    public void SeasonGoalsTrackFourteenDayProgressAndFinalReward()
    {
        var session = new GameSession();
        session.NewGame();
        CompleteNineNonFinalGoals(session);
        RestoreDay(session, 14);

        var final = session.GleamriseSeasonGoals().Single(goal =>
            goal.Definition.IsSeasonFinal
        );
        Assert.Equal(GleamriseGoalStatus.Ready, final.Status);

        var coins = session.Coins;
        var claimed = session.ClaimGleamriseSeasonGoal(
            final.Definition.Id
        );

        Assert.True(claimed.Succeeded);
        Assert.Equal("gleamrise.goal.claimed", claimed.MessageKey);
        Assert.Equal(coins + final.Definition.RewardCoins, session.Coins);
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.MoonplumSaplingId)
        );
        Assert.Equal(
            GleamriseGoalStatus.Claimed,
            session.GleamriseSeasonGoals().Single(goal =>
                goal.Definition.Id == final.Definition.Id
            ).Status
        );
    }

    [Fact]
    public void FinalGoalShowsIncompleteWithoutPermanentPenalty()
    {
        var session = new GameSession();
        session.NewGame();
        RestoreDay(session, 15);

        var final = session.GleamriseSeasonGoals().Single(goal =>
            goal.Definition.IsSeasonFinal
        );
        var failed = session.ClaimGleamriseSeasonGoal(final.Definition.Id);

        Assert.Equal(GleamriseGoalStatus.Open, final.Status);
        Assert.False(failed.Succeeded);
        Assert.Equal("gleamrise.goal.season_incomplete", failed.MessageKey);

        RestoreDay(session, CalendarSystem.DaysPerYear + 1);

        var nextYearGoals = session.GleamriseSeasonGoals();
        Assert.All(
            nextYearGoals,
            goal => Assert.NotEqual(GleamriseGoalStatus.Claimed, goal.Status)
        );
        Assert.Equal(2, session.GleamriseSeason.Year);
    }

    [Fact]
    public void ClaimIsAtomicWhenRewardBackpackIsFullAndRejectsDuplicate()
    {
        var session = new GameSession();
        session.NewGame();
        CompleteNineNonFinalGoals(session);
        RestoreDay(session, 14);
        FillBackpackWithCrystal(session);
        var final = session.GleamriseSeasonGoals().Single(goal =>
            goal.Definition.IsSeasonFinal
        );

        var failed = session.ClaimGleamriseSeasonGoal(final.Definition.Id);

        Assert.False(failed.Succeeded);
        Assert.Equal("notice.inventory_full", failed.MessageKey);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonplumSaplingId));
        Assert.NotEqual(
            GleamriseGoalStatus.Claimed,
            session.GleamriseSeasonGoals().Single(goal =>
                goal.Definition.Id == final.Definition.Id
            ).Status
        );

        Assert.True(session.Inventory.Remove(DataCatalog.CrystalShardId, 99));
        var claimed = session.ClaimGleamriseSeasonGoal(final.Definition.Id);
        var duplicate = session.ClaimGleamriseSeasonGoal(final.Definition.Id);

        Assert.True(claimed.Succeeded);
        Assert.False(duplicate.Succeeded);
        Assert.Equal(
            "gleamrise.goal.already_claimed",
            duplicate.MessageKey
        );
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonplumSaplingId));
    }

    [Fact]
    public void SaveRoundTripsAndFiltersUnknownSeasonState()
    {
        var path = Path.Combine(
            System.IO.Path.GetTempPath(),
            $"luminfield-season-{Guid.NewGuid():N}.json"
        );
        var service = new SaveService(path);
        var session = new GameSession();
        session.NewGame();
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterBuyGleamriseSeed,
            4
        );
        var save = session.Capture();
        save.GleamriseSeason.Goals =
        [
            new GleamriseGoalEntrySave
            {
                GoalId = "gleamrise_day_01_seed_spark",
                ClaimedDay = 1
            },
            new GleamriseGoalEntrySave
            {
                GoalId = "unknown_goal",
                ClaimedDay = 99
            }
        ];
        save.GleamriseSeason.Counters.Add(
            new GleamriseGoalCounterSave
            {
                CounterId = "unknown_counter",
                Count = 99
            }
        );

        service.Save(save);
        var result = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        var goal = Assert.Single(result.Save.GleamriseSeason.Goals);
        Assert.Equal("gleamrise_day_01_seed_spark", goal.GoalId);
        Assert.DoesNotContain(
            result.Save.GleamriseSeason.Counters,
            counter => counter.CounterId == "unknown_counter"
        );

        var restored = new GameSession();
        restored.Restore(result.Save);
        Assert.Equal(
            GleamriseGoalStatus.Claimed,
            restored.GleamriseSeasonGoals().First().Status
        );

        File.Delete(path);
    }

    [Fact]
    public void AnimalAndFestivalHooksUseStableMilestones()
    {
        var session = new GameSession();
        session.NewGame();
        RestoreDay(session, 13);

        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterAnimalFeedPrepared
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterAnimalFirstEgg
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterFestivalJoined
        );

        var goals = session.GleamriseSeasonGoals().ToDictionary(
            goal => goal.Definition.SeasonDay
        );
        Assert.Equal(GleamriseGoalStatus.Ready, goals[11].Status);
        Assert.Equal(GleamriseGoalStatus.Ready, goals[12].Status);
        Assert.Equal(GleamriseGoalStatus.Ready, goals[13].Status);
    }

    private static void CompleteNineNonFinalGoals(GameSession session)
    {
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterBuyGleamriseSeed,
            4
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantGleamriseCrop,
            3
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterWaterGleamriseCrop,
            3
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterFertilizeGleamriseSoil
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestGleamriseCrop,
            2
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterStartProcessor
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterCollectProcessor
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantMoonplumTree
        );
        session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestMoonplum
        );
    }

    private static void RestoreDay(GameSession session, int day)
    {
        var save = session.Capture();
        save.Day = day;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = WeatherSystem.WeatherForDay(day),
            ForecastId = WeatherSystem.WeatherForDay(day + 1)
        };
        session.Restore(save);
    }

    private static void FillBackpackWithCrystal(GameSession session)
    {
        session.Inventory.Restore(
            Enumerable.Range(0, Inventory.SlotCount)
                .Select(_ => new InventorySlot
                {
                    ItemId = DataCatalog.CrystalShardId,
                    Count = DataCatalog.Item(DataCatalog.CrystalShardId)
                        .MaxStack
                }),
            0
        );
    }
}
