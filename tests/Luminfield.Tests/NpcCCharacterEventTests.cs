using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcCCharacterEventTests
{
    public static IEnumerable<object[]> NpcCCases()
    {
        yield return
        [
            VillageCatalog.ElowenId,
            CharacterEventCatalog.ElowenTideMarksAtTheWellId,
            CharacterEventCatalog.ElowenWaterlineReadTogetherId,
            CharacterEventCatalog.NpcCElowenWaterWithTwoHonestNamesId,
            CharacterEventCatalog.NpcCElowenMarkerAllowedToDriftId,
            DataCatalog.DewmelonId
        ];
        yield return
        [
            VillageCatalog.MaveaId,
            CharacterEventCatalog.MaveaFourBowlsOneTableId,
            CharacterEventCatalog.MaveaWarmthThatKeepsId,
            CharacterEventCatalog.NpcCMaveaRecipeThatChangedWithTheTableId,
            CharacterEventCatalog.NpcCMaveaLastJarOpenedOnAnOrdinaryDayId,
            DataCatalog.MoonmistStewId
        ];
        yield return
        [
            VillageCatalog.SivrenId,
            CharacterEventCatalog.SivrenUnfiledLanternsId,
            CharacterEventCatalog.SivrenYearInThreeLightsId,
            CharacterEventCatalog.NpcCSivrenTwoMemoriesUnderOneDateId,
            CharacterEventCatalog.NpcCSivrenFirstPersonFootnoteId,
            DataCatalog.CloudleafTeaId
        ];
        yield return
        [
            VillageCatalog.DorrikId,
            CharacterEventCatalog.DorrikChalkBeyondWallsId,
            CharacterEventCatalog.DorrikRoomsThatBreatheId,
            CharacterEventCatalog.NpcCDorrikMaintenancePathBehindTheBraceId,
            CharacterEventCatalog.NpcCDorrikPlanReturnedToItsUsersId,
            DataCatalog.MoonstonePathId
        ];
    }

    public static IEnumerable<object[]> NewPersonalEvents() =>
        NpcCCases().SelectMany(data => new[]
        {
            new object[] { (string)data[3] },
            new object[] { (string)data[4] }
        });

    [Fact]
    public void NpcCAddsEightPersonalEventsWithoutChangingExistingChains()
    {
        foreach (var data in NpcCCases())
        {
            var npcId = (string)data[0];
            var firstId = (string)data[1];
            var secondId = (string)data[2];
            var thirdId = (string)data[3];
            var fourthId = (string)data[4];
            var chain = CharacterEventCatalog.Definitions
                .Where(definition => definition.NpcId == npcId)
                .OrderBy(definition =>
                    definition.RequiredRelationshipPoints)
                .ToArray();

            Assert.Equal(
                [25, 60, 75, 90],
                chain.Select(definition =>
                    definition.RequiredRelationshipPoints)
            );
            Assert.Equal(
                [firstId, secondId, thirdId, fourthId],
                chain.Select(definition => definition.Id)
            );
            Assert.Equal(
                [null, firstId, secondId, thirdId],
                chain.Select(definition =>
                    definition.RequiredPreviousEventId)
            );
            Assert.All(chain.Skip(2), definition =>
                Assert.StartsWith("npc_c_", definition.Id));
            Assert.All(chain, definition =>
                Assert.Equal(3, definition.DialogueKeys.Count));
        }

        Assert.Equal(
            8,
            CharacterEventCatalog.Definitions.Count(definition =>
                definition.Id.StartsWith(
                    "npc_c_",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.Equal(
            new HashSet<string>(
                [
                    VillageCatalog.ElowenId,
                    VillageCatalog.MaveaId,
                    VillageCatalog.SivrenId,
                    VillageCatalog.DorrikId
                ],
                StringComparer.Ordinal
            ),
            CharacterEventCatalog.NpcCIds
        );
    }

    [Theory]
    [MemberData(nameof(NewPersonalEvents))]
    public void NewPersonalEventsStartAtRealScheduleAndCompleteOnFinalPage(
        string eventId
    )
    {
        var definition = CharacterEventCatalog.ById[eventId];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = CurrentNpc(session, definition, trigger);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(eventId, conversation?.CharacterEvent?.EventId);
        Assert.Equal(eventId, session.CharacterEvents.ActiveEventId);
        Assert.False(session.CharacterEvents.IsCompleted(eventId));
        Assert.True(session.CompleteCharacterEvent(eventId).Succeeded);
        Assert.Equal(trigger.Day, session.CharacterEvents.CompletedDay(eventId));
    }

    [Theory]
    [MemberData(nameof(NpcCCases))]
    public void OldTwoEventSaveContinuesAtThirdWithoutRepeatingOldEvents(
        string npcId,
        string firstId,
        string secondId,
        string thirdId,
        string fourthId,
        string lovedGiftId
    )
    {
        _ = npcId;
        _ = fourthId;
        _ = lovedGiftId;
        var definition = CharacterEventCatalog.ById[thirdId];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = CurrentNpc(session, definition, trigger);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(thirdId, conversation?.CharacterEvent?.EventId);
        Assert.True(session.CharacterEvents.IsCompleted(firstId));
        Assert.True(session.CharacterEvents.IsCompleted(secondId));
    }

    [Theory]
    [MemberData(nameof(NpcCCases))]
    public void ThirdAndFourthEventsRequireEarlierDayPrerequisites(
        string npcId,
        string firstId,
        string secondId,
        string thirdId,
        string fourthId,
        string lovedGiftId
    )
    {
        _ = npcId;
        _ = lovedGiftId;
        var sameDayThird = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    Entry(firstId, 1),
                    Entry(secondId, 2),
                    Entry(thirdId, 2)
                ]
            },
            2
        );
        var sameDayFourth = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    Entry(firstId, 1),
                    Entry(secondId, 2),
                    Entry(thirdId, 3),
                    Entry(fourthId, 3)
                ]
            },
            3
        );
        var valid = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    Entry(firstId, 1),
                    Entry(secondId, 2),
                    Entry(thirdId, 3),
                    Entry(fourthId, 4)
                ]
            },
            4
        );

        Assert.DoesNotContain(sameDayThird.Entries,
            entry => entry.EventId == thirdId);
        Assert.DoesNotContain(sameDayFourth.Entries,
            entry => entry.EventId == fourthId);
        Assert.Contains(valid.Entries,
            entry => entry.EventId == fourthId);
    }

    [Theory]
    [MemberData(nameof(NpcCCases))]
    public void WrongToolAndInvalidCompletionLeaveEventStateUnchanged(
        string npcId,
        string firstId,
        string secondId,
        string thirdId,
        string fourthId,
        string lovedGiftId
    )
    {
        _ = npcId;
        _ = firstId;
        _ = secondId;
        _ = fourthId;
        _ = lovedGiftId;
        var definition = CharacterEventCatalog.ById[thirdId];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = CurrentNpc(session, definition, trigger);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );
        var invalidCompletion = session.CompleteCharacterEvent(thirdId);

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void NpcCConditionResponsesUseExactWeatherOrSeasonPriority()
    {
        var checks = new[]
        {
            (VillageCatalog.ElowenId, 15,
                DataCatalog.ClearWeatherId,
                "village.npc.elowen.season_rainveil",
                VillageCatalog.SeasonSchedulePriority),
            (VillageCatalog.MaveaId, 1,
                DataCatalog.RainWeatherId,
                "village.npc.mavea.weather_rain",
                VillageCatalog.WeatherSchedulePriority),
            (VillageCatalog.SivrenId, 29,
                DataCatalog.ClearWeatherId,
                "village.npc.sivren.season_starharvest",
                VillageCatalog.SeasonSchedulePriority),
            (VillageCatalog.DorrikId, 15,
                DataCatalog.ClearWeatherId,
                "village.npc.dorrik.season_rainveil",
                VillageCatalog.SeasonSchedulePriority)
        };

        foreach (var (npcId, day, weatherId, dialogueKey, priority) in checks)
        {
            var entry = NpcScheduleSystem.SelectEntry(
                VillageCatalog.Npcs[npcId],
                day,
                14 * 60,
                weatherId
            );
            Assert.NotNull(entry);
            Assert.Equal(dialogueKey, entry.DialogueKey);
            Assert.Equal(priority, entry.Priority);
        }
    }

    [Fact]
    public void NpcCPersonalEventsStayAnchoredToTheirRealPlacesAndAreas()
    {
        Assert.Equal(
            "village.npc.elowen.plaza",
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcCElowenMarkerAllowedToDriftId
            ].RequiredNpcDialogueKey
        );
        Assert.Equal(
            PlayerLocationIds.StarweaverTeaHouse,
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcCMaveaLastJarOpenedOnAnOrdinaryDayId
            ].RequiredLocationId
        );
        Assert.Equal(
            PlayerLocationIds.MoonlitArchive,
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcCSivrenTwoMemoriesUnderOneDateId
            ].RequiredLocationId
        );
        Assert.Equal(
            "village.npc.dorrik.plaza",
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcCDorrikPlanReturnedToItsUsersId
            ].RequiredNpcDialogueKey
        );
    }

    private static GameSession PrepareEventSession(
        CharacterEventDefinition definition,
        (int Day, int Minute) trigger
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = trigger.Day;
        save.MinuteOfDay = trigger.Minute;
        save.Player.LocationId = definition.RequiredLocationId;
        save.Village = new VillageSave
        {
            MetNpcIds = [definition.NpcId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = definition.NpcId,
                    Points = definition.RequiredRelationshipPoints,
                    LastTalkDay = trigger.Day
                }
            ]
        };
        var completed = new List<CharacterEventEntrySave>();
        var prerequisiteId = definition.RequiredPreviousEventId;
        var completedDay = trigger.Day - 1;
        while (prerequisiteId is not null)
        {
            completed.Add(Entry(prerequisiteId, completedDay--));
            prerequisiteId = CharacterEventCatalog.ById[
                prerequisiteId
            ].RequiredPreviousEventId;
        }
        completed.Reverse();
        save.CharacterEvents = new CharacterEventSave { Entries = completed };
        return Restore(save);
    }

    private static VillageNpcState CurrentNpc(
        GameSession session,
        CharacterEventDefinition definition,
        (int Day, int Minute) trigger
    )
    {
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId,
                session.PlayerCell
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        Assert.Equal(definition.RequiredNpcDialogueKey, npc.DialogueKey);
        return npc;
    }

    private static (int Day, int Minute) FindTrigger(
        CharacterEventDefinition definition,
        int minimumDay = 5
    )
    {
        for (var day = minimumDay; day <= CalendarSystem.DaysPerYear; day++)
        {
            var weatherId = WeatherSystem.WeatherForDay(day);
            for (var minute = GameClock.StartMinute;
                 minute < GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var entry = NpcScheduleSystem.SelectEntry(
                    VillageCatalog.Npcs[definition.NpcId],
                    day,
                    minute,
                    weatherId
                );
                if (entry?.LocationId == definition.RequiredLocationId &&
                    entry.DialogueKey == definition.RequiredNpcDialogueKey &&
                    minute >= entry.StartMinute + 60)
                {
                    return (day, minute);
                }
            }
        }

        throw new InvalidOperationException(
            $"No trigger exists for {definition.Id}."
        );
    }

    private static GameSession Restore(GameSaveV1 save)
    {
        var session = new GameSession();
        session.NewGame();
        session.Restore(save);
        session.Inventory.Select(0);
        return session;
    }

    private static CharacterEventEntrySave Entry(string eventId, int day) =>
        new() { EventId = eventId, CompletedDay = day };
}
