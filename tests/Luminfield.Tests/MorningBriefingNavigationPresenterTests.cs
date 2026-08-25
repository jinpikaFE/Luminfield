using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class MorningBriefingNavigationPresenterTests
{
    [Theory]
    [InlineData(MorningBriefingCardKind.Mail)]
    [InlineData(MorningBriefingCardKind.DailyCommission)]
    [InlineData(MorningBriefingCardKind.WeeklyCommission)]
    public void HomeActionsResolveToHomestead(
        MorningBriefingCardKind kind
    )
    {
        var item = Item(kind, referenceId: "");

        Assert.Equal(
            WorldBiome.Home,
            MorningBriefingNavigationPresenter.DestinationFor(
                item,
                NewSessionForDay(1)
            )
        );
    }

    [Fact]
    public void HomeActionsExposeExactWorldTargets()
    {
        var session = NewSessionForDay(1);

        var mail = MorningBriefingNavigationPresenter.TargetFor(
            Item(MorningBriefingCardKind.Mail, referenceId: ""),
            session
        );
        var daily = MorningBriefingNavigationPresenter.TargetFor(
            Item(MorningBriefingCardKind.DailyCommission, referenceId: ""),
            session
        );

        Assert.NotNull(mail);
        Assert.Equal(FarmLayout.StarlightMailboxCell, mail.TargetCell);
        Assert.Equal(WorldNavigationDestinationKind.Mailbox, mail.Kind);
        Assert.Equal("morning.mail.title", mail.NameKey);
        Assert.NotNull(daily);
        Assert.Equal(FarmLayout.CommissionBoardCell, daily.TargetCell);
        Assert.Equal(WorldNavigationDestinationKind.CommissionBoard, daily.Kind);
    }

    [Fact]
    public void WeatherAndUnknownReferencesStayUnrouted()
    {
        var session = NewSessionForDay(1);

        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.Weather, referenceId: ""),
            session
        ));
        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.Festival, "missing_festival"),
            session
        ));
        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.CharacterEvent, "missing_event"),
            session
        ));
        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.RegionSuggestion, ""),
            session
        ));
        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.RegionSuggestion, "missing_region"),
            session
        ));
        Assert.Null(MorningBriefingNavigationPresenter.DestinationFor(
            Item(MorningBriefingCardKind.RegionSuggestion, "999"),
            session
        ));
    }

    [Fact]
    public void FestivalActionsResolveFromTheirWorldDoorCells()
    {
        var session = NewSessionForDay(4, 10 * 60);

        foreach (var festival in FestivalSpatialCatalog.All)
        {
            var expected = WorldDefinition.GetBiome(festival.WorldEntryCell);

            Assert.Equal(
                expected,
                MorningBriefingNavigationPresenter.DestinationFor(
                    Item(MorningBriefingCardKind.Festival, festival.FestivalId),
                    session
                )
            );
            var target = MorningBriefingNavigationPresenter.TargetFor(
                Item(MorningBriefingCardKind.Festival, festival.FestivalId),
                session
            );
            Assert.NotNull(target);
            Assert.Equal(festival.WorldEntryCell, target.TargetCell);
            Assert.Equal(
                WorldNavigationDestinationKind.FestivalEntrance,
                target.Kind
            );
            Assert.Equal(
                expected,
                WorldDefinition.GetBiome(festival.WorldReturnCell)
            );
        }
    }

    [Fact]
    public void CharacterEventsUseMatchingScheduleDestination()
    {
        var session = NewSessionForDay(2);
        var lioraArchive = CharacterEventCatalog.ById[
            CharacterEventCatalog.LioraFadedReturnRouteId
        ];
        var nemiRoute = CharacterEventCatalog.ById[
            CharacterEventCatalog.NemiUndeliverableLetterId
        ];

        Assert.Equal(
            WorldBiome.LumenVillage,
            MorningBriefingNavigationPresenter.DestinationFor(
                Item(
                    MorningBriefingCardKind.CharacterEvent,
                    lioraArchive.Id
                ),
                session
            )
        );
        Assert.Equal(
            ExpectedCharacterEventDestination(nemiRoute, session),
            MorningBriefingNavigationPresenter.DestinationFor(
                Item(MorningBriefingCardKind.CharacterEvent, nemiRoute.Id),
                session
            )
        );

        var archiveTarget = MorningBriefingNavigationPresenter.TargetFor(
            Item(
                MorningBriefingCardKind.CharacterEvent,
                lioraArchive.Id
            ),
            session
        );
        Assert.NotNull(archiveTarget);
        Assert.Equal(
            VillageCatalog.MoonlitArchiveDoorCell,
            archiveTarget.TargetCell
        );
        Assert.Equal(
            WorldNavigationDestinationKind.Character,
            archiveTarget.Kind
        );
        Assert.Equal(PlayerLocationIds.MoonlitArchive, archiveTarget.LocationId);
        Assert.True(archiveTarget.HasLocationTargetCell);
        Assert.Equal(
            ExpectedCharacterEventEntry(lioraArchive, session).Position,
            archiveTarget.LocationTargetCell
        );
        Assert.True(archiveTarget.TryGetTargetCell(
            PlayerLocationIds.World,
            out var worldTarget
        ));
        Assert.Equal(VillageCatalog.MoonlitArchiveDoorCell, worldTarget);
        Assert.True(archiveTarget.TryGetTargetCell(
            PlayerLocationIds.MoonlitArchive,
            out var interiorTarget
        ));
        Assert.Equal(archiveTarget.LocationTargetCell, interiorTarget);
    }

    [Fact]
    public void RegionSuggestionsResolveLandmarksAndStableBiomeReferences()
    {
        var session = NewSessionForDay(1);

        Assert.Equal(
            WorldBiome.WhisperingWoods,
            MorningBriefingNavigationPresenter.DestinationFor(
                Item(
                    MorningBriefingCardKind.RegionSuggestion,
                    WorldDefinition.WoodlandStarlightLandmarkId
                ),
                session
            )
        );
        var landmark = WorldDefinition.Landmarks.Single(entry =>
            entry.Id == WorldDefinition.WoodlandStarlightLandmarkId);
        var target = MorningBriefingNavigationPresenter.TargetFor(
            Item(
                MorningBriefingCardKind.RegionSuggestion,
                landmark.Id
            ),
            session
        );
        Assert.NotNull(target);
        Assert.Equal(landmark.Position, target.TargetCell);
        Assert.Equal(landmark.NameKey, target.NameKey);

        Assert.Equal(
            WorldBiome.CrystalVale,
            MorningBriefingNavigationPresenter.DestinationFor(
                Item(
                    MorningBriefingCardKind.RegionSuggestion,
                    WorldBiome.CrystalVale.ToString()
                ),
                session
            )
        );
        var regionOnly = MorningBriefingNavigationPresenter.TargetFor(
            Item(
                MorningBriefingCardKind.RegionSuggestion,
                WorldBiome.CrystalVale.ToString()
            ),
            session
        );
        Assert.NotNull(regionOnly);
        Assert.False(regionOnly.HasTargetCell);
    }

    [Fact]
    public void DecisionSummaryNavigationIsReadOnly()
    {
        var session = NewSessionForDay(4, 10 * 60);
        var save = session.Capture();
        save.Mail = new MailSave
        {
            Entries =
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 1,
                    IsRead = false
                }
            ]
        };
        session.Restore(save);
        var display = MorningBriefingPresenter.Create(
            MorningBriefingSystem.Create(session),
            TestLocale()
        );
        var before = SerializeSave(session);

        foreach (var item in display.DecisionSummary)
        {
            MorningBriefingNavigationPresenter.DestinationFor(item, session);
            MorningBriefingNavigationPresenter.TargetFor(item, session);
        }

        Assert.Equal(before, SerializeSave(session));
    }

    private static WorldBiome? ExpectedCharacterEventDestination(
        CharacterEventDefinition definition,
        GameSession session
    ) => RouteGuidanceOriginPresenter.Resolve(
        definition.RequiredLocationId,
        ExpectedCharacterEventEntry(definition, session).Position
    );

    private static NpcScheduleEntry ExpectedCharacterEventEntry(
        CharacterEventDefinition definition,
        GameSession session
    )
    {
        var npc = VillageCatalog.Npcs[definition.NpcId];
        return npc.Schedule
            .Where(candidate =>
                candidate.LocationId == definition.RequiredLocationId)
            .Where(candidate =>
                definition.RequiredNpcDialogueKey is null ||
                candidate.DialogueKey == definition.RequiredNpcDialogueKey)
            .First(candidate => candidate.Matches(
                session.Clock.Day,
                Math.Max(candidate.StartMinute, GameClock.StartMinute),
                session.Weather.CurrentId
            ));
    }

    private static MorningBriefingDecisionSummaryItem Item(
        MorningBriefingCardKind kind,
        string referenceId
    ) => new(
        $"test.{kind}",
        kind,
        MorningBriefingPriority.Primary,
        kind.ToString(),
        "Go",
        referenceId
    );

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

    private static string SerializeSave(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());

    private static LocaleService TestLocale()
    {
        var locale = new LocaleService();
        var values = MorningBriefingPresenter.RequiredLocalizationKeys
            .ToDictionary(key => key, key => key, StringComparer.Ordinal);
        values["weather.clear"] = "Clear";
        values["weather.rain"] = "Rain";
        values["weather.stardust_wind"] = "Stardust wind";
        values["weather.longnight_snow"] = "Longnight snow";
        values["festival.gleamrise.name"] = "Gleamrise planting festival";
        values["objective.daily.starbud"] = "Plant starbuds";
        values["objective.weekly.route_stones"] = "Repair star roads";
        values["biome.homestead"] = "Homestead";
        values["biome.meadow"] = "Meadow";
        values["biome.forest"] = "Forest";
        values["biome.wetlands"] = "Wetlands";
        values["biome.crystal_valley"] = "Crystal vale";
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
