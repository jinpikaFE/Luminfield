using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class MorningBriefingSystemTests
{
    [Fact]
    public void NewGameReservesFirstMorningForOnboarding()
    {
        var session = new GameSession();

        session.NewGame();

        Assert.True(session.ExperienceGuidance.WasMorningBriefingShown(1));
        Assert.Equal(
            1,
            session.Capture().ExperienceGuidance.LastMorningBriefingDay
        );
    }

    [Fact]
    public void CreateUsesStableMorningOrderAndLeavesSaveUntouched()
    {
        var session = NewSessionForDay(1);
        var before = SerializeSave(session);

        var briefing = MorningBriefingSystem.Create(session);

        Assert.Equal(before, SerializeSave(session));
        Assert.Equal(
            [
                MorningBriefingSystem.WeatherCardId,
                MorningBriefingSystem.MailCardId,
                MorningBriefingSystem.FestivalCardId,
                MorningBriefingSystem.CharacterEventCardId,
                MorningBriefingSystem.DailyCommissionCardId,
                MorningBriefingSystem.WeeklyCommissionCardId,
                MorningBriefingSystem.RegionSuggestionCardId
            ],
            briefing.Cards.Select(card => card.Id)
        );
    }

    [Fact]
    public void RestoredSessionKeepsTheSameBriefingProjection()
    {
        var session = NewSessionForDay(1);
        Assert.True(session.AcceptDailyCommission().Succeeded);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.Equal(
            SerializeBriefing(MorningBriefingSystem.Create(session)),
            SerializeBriefing(MorningBriefingSystem.Create(restored))
        );
    }

    [Fact]
    public void DisplayHistorySurvivesCaptureAndRestoreWithoutRepeatingDay()
    {
        var session = NewSessionForDay(3);

        Assert.False(session.ExperienceGuidance.WasMorningBriefingShown(3));
        Assert.True(session.ExperienceGuidance.MarkMorningBriefingShown(3));
        Assert.False(session.ExperienceGuidance.MarkMorningBriefingShown(3));

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.True(restored.ExperienceGuidance.WasMorningBriefingShown(3));
        Assert.False(restored.ExperienceGuidance.WasMorningBriefingShown(4));
    }

    [Fact]
    public void LegacyAndInvalidDisplayHistoryRestoreSafely()
    {
        var legacy = NewSessionForDay(4);
        var legacySave = legacy.Capture();
        legacySave.ExperienceGuidance = null!;
        legacy.Restore(legacySave);
        Assert.False(legacy.ExperienceGuidance.WasMorningBriefingShown(4));

        var future = NewSessionForDay(4);
        var futureSave = future.Capture();
        futureSave.ExperienceGuidance.LastMorningBriefingDay = 99;
        future.Restore(futureSave);
        Assert.Equal(
            4,
            future.ExperienceGuidance.LastMorningBriefingDay
        );

        var negative = NewSessionForDay(4);
        var negativeSave = negative.Capture();
        negativeSave.ExperienceGuidance.LastMorningBriefingDay = -8;
        negative.Restore(negativeSave);
        Assert.Equal(0, negative.ExperienceGuidance.LastMorningBriefingDay);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public void LegacySaveWithoutGuidanceReservesOnlyFirstMorning(
        int day,
        int expectedLastMorningBriefingDay
    )
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-guidance-{Guid.NewGuid():N}"
        );
        var path = Path.Combine(directory, "slot_1.json");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, $$"""
            {
              "schemaVersion": {{SaveService.CurrentSchemaVersion}},
              "day": {{day}},
              "minuteOfDay": {{GameClock.StartMinute}}
            }
            """);

            var result = new SaveService(path).Load();

            Assert.Equal(SaveLoadStatus.Loaded, result.Status);
            Assert.NotNull(result.Save);
            Assert.Equal(
                expectedLastMorningBriefingDay,
                result.Save.ExperienceGuidance.LastMorningBriefingDay
            );
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void QuietMorningShowsExplicitEmptyStatesInsteadOfDroppingCards()
    {
        var session = NewSessionForDay(1);
        var save = session.Capture();
        save.Mail = new MailSave();
        save.Village = new VillageSave();
        session.Restore(save);

        var briefing = MorningBriefingSystem.Create(session);

        Assert.Equal(
            "morning.mail.none",
            Card(briefing, MorningBriefingSystem.MailCardId).BodyKey
        );
        Assert.Equal(
            "morning.festival.none",
            Card(briefing, MorningBriefingSystem.FestivalCardId).BodyKey
        );
        Assert.Equal(
            "morning.character_event.none",
            Card(
                briefing,
                MorningBriefingSystem.CharacterEventCardId
            ).BodyKey
        );
        Assert.Equal(7, briefing.Cards.Count);
    }

    [Fact]
    public void AdvancingDayRebuildsWeatherAndSuggestionsFromCurrentFacts()
    {
        var first = NewSessionForDay(1);
        var second = NewSessionForDay(2);

        var firstWeather = Card(
            first,
            MorningBriefingSystem.WeatherCardId
        );
        var secondWeather = Card(
            second,
            MorningBriefingSystem.WeatherCardId
        );

        Assert.Equal(
            $"weather.{WeatherSystem.WeatherForDay(1)}",
            firstWeather.Arguments[0].LocalizationKey
        );
        Assert.Equal(
            $"weather.{WeatherSystem.WeatherForDay(2)}",
            secondWeather.Arguments[0].LocalizationKey
        );
        Assert.NotEqual(
            SerializeBriefing(MorningBriefingSystem.Create(first)),
            SerializeBriefing(MorningBriefingSystem.Create(second))
        );
    }

    [Fact]
    public void FestivalBriefingDifferentiatesQuietTomorrowAndOpenStates()
    {
        var quiet = Card(
            NewSessionForDay(1),
            MorningBriefingSystem.FestivalCardId
        );
        var tomorrow = Card(
            NewSessionForDay(3),
            MorningBriefingSystem.FestivalCardId
        );
        var laterToday = Card(
            NewSessionForDay(4),
            MorningBriefingSystem.FestivalCardId
        );
        var openToday = Card(
            NewSessionForDay(4, 10 * 60),
            MorningBriefingSystem.FestivalCardId
        );

        Assert.Equal("morning.festival.none", quiet.BodyKey);
        Assert.Equal("morning.festival.tomorrow", tomorrow.BodyKey);
        Assert.Equal(
            FestivalCatalog.GleamrisePlantingFestivalId,
            tomorrow.ReferenceId
        );
        Assert.Equal("morning.festival.today_later", laterToday.BodyKey);
        Assert.Equal("morning.festival.open_today", openToday.BodyKey);
    }

    [Fact]
    public void CommissionBriefingReflectsReadyProgressWithoutClaiming()
    {
        var session = NewSessionForDay(1);
        Assert.True(session.AcceptDailyCommission().Succeeded);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        session.Commission.RecordPlant(DataCatalog.StarbudId);
        Assert.True(session.AcceptWeeklyCommission().Succeeded);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);

        var daily = Card(session, MorningBriefingSystem.DailyCommissionCardId);
        var weekly = Card(session, MorningBriefingSystem.WeeklyCommissionCardId);

        Assert.Equal("morning.daily_commission.ready", daily.BodyKey);
        Assert.Equal(MorningBriefingPriority.Primary, daily.Priority);
        Assert.False(session.Commission.Claimed);
        Assert.Equal("morning.weekly_commission.ready_stage", weekly.BodyKey);
        Assert.Equal(MorningBriefingPriority.Primary, weekly.Priority);
        Assert.False(session.WeeklyCommission.Claimed);
    }

    [Fact]
    public void CharacterEventBriefingFindsEligibleNpcWithoutStartingEvent()
    {
        var session = NewSessionForDay(2);
        var save = session.Capture();
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 25
                }
            ]
        };
        session.Restore(save);

        var before = SerializeSave(session);
        var card = Card(
            session,
            MorningBriefingSystem.CharacterEventCardId
        );

        Assert.Equal("morning.character_event.ready_one", card.BodyKey);
        Assert.Equal(
            CharacterEventCatalog.LioraFadedReturnRouteId,
            card.ReferenceId
        );
        Assert.Null(session.CharacterEvents.ActiveEventId);
        Assert.Equal(before, SerializeSave(session));
    }

    [Fact]
    public void QuietMorningDecisionSummaryKeepsOnlyExecutablePriorityRoute()
    {
        var session = NewSessionForDay(1);
        var save = session.Capture();
        save.Mail = new MailSave();
        save.Village = new VillageSave();
        session.Restore(save);

        var display = MorningBriefingPresenter.Create(
            MorningBriefingSystem.Create(session),
            TestLocale()
        );

        var item = Assert.Single(display.DecisionSummary);
        Assert.Equal(MorningBriefingSystem.RegionSuggestionCardId, item.Id);
        Assert.Equal(MorningBriefingPriority.Secondary, item.Priority);
        Assert.False(string.IsNullOrWhiteSpace(item.Action));
    }

    [Fact]
    public void SevenRepresentativeDaysDecisionSummaryContractIsReadOnly()
    {
        var locale = TestLocale();
        var projections = new List<string>();

        for (var day = 1; day <= 7; day++)
        {
            var session = RepresentativeSessionForDay(day);
            var before = SerializeSave(session);
            var display = MorningBriefingPresenter.Create(
                MorningBriefingSystem.Create(session),
                locale
            );

            Assert.Equal(before, SerializeSave(session));
            Assert.InRange(
                display.DecisionSummary.Count,
                1,
                MorningBriefingPresenter.MaxDecisionSummaryItems
            );
            Assert.All(
                display.DecisionSummary,
                item => Assert.False(string.IsNullOrWhiteSpace(item.Action))
            );
            Assert.All(
                display.DecisionSummary,
                item => Assert.True(
                    item.Priority is MorningBriefingPriority.Primary
                        or MorningBriefingPriority.Secondary
                )
            );

            projections.Add(
                $"{day}:{string.Join(",", display.DecisionSummary.Select(item => item.Id))}"
            );
        }

        Assert.True(
            projections.Distinct(StringComparer.Ordinal).Count() >= 4,
            "Representative deterministic days should cover several briefing shapes."
        );
    }

    private static GameSession NewSessionForDay(
        int day,
        int minuteOfDay = GameClock.StartMinute
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = WeatherSystem.WeatherForDay(day),
            ForecastId = WeatherSystem.WeatherForDay(day + 1)
        };
        session.Restore(save);
        return session;
    }

    private static GameSession RepresentativeSessionForDay(int day)
    {
        var minute = day == 4 ? 10 * 60 : GameClock.StartMinute;
        var session = NewSessionForDay(day, minute);

        if (day is 2 or 3 or 7)
        {
            Assert.True(session.AcceptDailyCommission().Succeeded);
        }

        if (day is 3 or 7)
        {
            session.Commission.RecordPlant(DataCatalog.StarbudId);
            session.Commission.RecordPlant(DataCatalog.StarbudId);
        }

        if (day is 5 or 7)
        {
            Assert.True(session.AcceptWeeklyCommission().Succeeded);
        }

        if (day == 7)
        {
            session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
            session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
            session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        }

        if (day == 6)
        {
            var save = session.Capture();
            save.Village = new VillageSave
            {
                MetNpcIds = [VillageCatalog.LioraId],
                Relationships =
                [
                    new VillageRelationshipSave
                    {
                        NpcId = VillageCatalog.LioraId,
                        Points = 25
                    }
                ]
            };
            session.Restore(save);
        }

        return session;
    }

    private static MorningBriefingCard Card(
        GameSession session,
        string cardId
    ) => MorningBriefingSystem.Create(session)
        .Cards
        .Single(card => string.Equals(card.Id, cardId, StringComparison.Ordinal));

    private static MorningBriefingCard Card(
        MorningBriefing briefing,
        string cardId
    ) => briefing.Cards.Single(card =>
        string.Equals(card.Id, cardId, StringComparison.Ordinal)
    );

    private static string SerializeSave(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());

    private static string SerializeBriefing(MorningBriefing briefing) =>
        JsonSerializer.Serialize(
            briefing.Cards.Select(card => new
            {
                card.Id,
                card.Kind,
                card.Priority,
                card.SortOrder,
                card.TitleKey,
                card.BodyKey,
                Arguments = card.Arguments.Select(argument => new
                {
                    argument.Kind,
                    argument.LocalizationKey,
                    argument.Number
                }),
                card.ActionKey,
                card.ReferenceId
            })
        );

    private static LocaleService TestLocale()
    {
        var locale = new LocaleService();
        var values = MorningBriefingPresenter.RequiredLocalizationKeys
            .ToDictionary(key => key, key => key, StringComparer.Ordinal);
        values["weather.clear"] = "Clear";
        values["weather.rain"] = "Rain";
        values["weather.stardust_wind"] = "Stardust wind";
        values["weather.longnight_snow"] = "Longnight snow";
        values["objective.daily.starbud"] = "Plant starbuds";
        values["objective.weekly.route_stones"] = "Repair the star road";
        values["npc.liora.name"] = "Liora";
        values["biome.homestead"] = "Homestead";
        values["biome.meadow"] = "Meadow";
        values["biome.forest"] = "Forest";
        values["biome.wetlands"] = "Wetlands";
        values["biome.crystal_valley"] = "Crystal valley";
        values["biome.ruins"] = "Ruins";
        values["biome.village"] = "Village";
        locale.LoadJson(LocaleService.English, JsonSerializer.Serialize(values));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            JsonSerializer.Serialize(values)
        );
        return locale;
    }
}
