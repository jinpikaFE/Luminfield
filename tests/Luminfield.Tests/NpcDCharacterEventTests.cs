using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcDCharacterEventTests
{
    public static IEnumerable<object[]> NpcDCases()
    {
        yield return
        [
            VillageCatalog.YvaraId,
            CharacterEventCatalog.YvaraSeedsBeyondTheCalendarId,
            CharacterEventCatalog.YvaraASeasonCarriedGentlyId,
            CharacterEventCatalog.NpcDYvaraTheDaySheLeftTheCaseClosedId,
            CharacterEventCatalog.NpcDYvaraASeedRecordInTwoHandsId,
            DataCatalog.MoonplumSaplingId
        ];
        yield return
        [
            VillageCatalog.BrialId,
            CharacterEventCatalog.BrialBlossomsBetweenHarvestsId,
            CharacterEventCatalog.BrialAPathLeftForTheBeesId,
            CharacterEventCatalog.NpcDBrialTheOrchardRoundWithAnEmptyBasketId,
            CharacterEventCatalog.NpcDBrialThePruningMarkHeErasedId,
            DataCatalog.MoonplumId
        ];
        yield return
        [
            VillageCatalog.PavriId,
            CharacterEventCatalog.PavriTheVisibleMendId,
            CharacterEventCatalog.PavriClothThatKeepsWarmthId,
            CharacterEventCatalog.NpcDPavriTheCuffTestedInMotionId,
            CharacterEventCatalog.NpcDPavriOneStitchBesideTheOldId,
            DataCatalog.MoonfleeceId
        ];
        yield return
        [
            VillageCatalog.RovenId,
            CharacterEventCatalog.RovenTheRouteWithRoomToRestId,
            CharacterEventCatalog.RovenLightsThatWaitForReturnId,
            CharacterEventCatalog.NpcDRovenTheCornerPeopleAlreadyChoseId,
            CharacterEventCatalog.NpcDRovenARouteForAnOrdinaryDayId,
            DataCatalog.MoonstonePathId
        ];
    }

    public static IEnumerable<object[]> NewPersonalEvents() =>
        NpcDCases().SelectMany(data => new[]
        {
            new object[] { (string)data[3] },
            new object[] { (string)data[4] }
        });

    [Fact]
    public void NpcDAddsEightPersonalEventsWithoutChangingExistingChains()
    {
        foreach (var data in NpcDCases())
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
                Assert.StartsWith("npc_d_", definition.Id));
            Assert.All(chain, definition =>
                Assert.Equal(3, definition.DialogueKeys.Count));
        }

        Assert.Equal(
            8,
            CharacterEventCatalog.Definitions.Count(definition =>
                definition.Id.StartsWith(
                    "npc_d_",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.Equal(
            new HashSet<string>(
                [
                    VillageCatalog.YvaraId,
                    VillageCatalog.BrialId,
                    VillageCatalog.PavriId,
                    VillageCatalog.RovenId
                ],
                StringComparer.Ordinal
            ),
            CharacterEventCatalog.NpcDIds
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
        var preview = session.PreviewSelectedTarget(npc.Position);

        Assert.True(preview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);

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
    [MemberData(nameof(NpcDCases))]
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
    [MemberData(nameof(NpcDCases))]
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
    [MemberData(nameof(NpcDCases))]
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

    [Theory]
    [MemberData(nameof(NpcDCases))]
    public void GiftMayChangeSocialStateButNeverStartsPersonalEvent(
        string npcId,
        string firstId,
        string secondId,
        string thirdId,
        string fourthId,
        string lovedGiftId
    )
    {
        _ = firstId;
        _ = secondId;
        _ = fourthId;
        var definition = CharacterEventCatalog.ById[thirdId];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = CurrentNpc(session, definition, trigger);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);
        Assert.True(session.Inventory.Add(lovedGiftId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(lovedGiftId));
        var beforeEvents = JsonSerializer.Serialize(
            session.CharacterEvents.Capture()
        );
        var beforeCount = session.Inventory.Count(lovedGiftId);
        var beforeRelationship = session.Village.Relationship(npcId).Points;

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(GiftReaction.Loved, conversation?.GiftReaction);
        Assert.Null(conversation?.CharacterEvent);
        Assert.Null(session.CharacterEvents.ActiveEventId);
        Assert.Equal(
            beforeEvents,
            JsonSerializer.Serialize(session.CharacterEvents.Capture())
        );
        Assert.Equal(beforeCount - 1, session.Inventory.Count(lovedGiftId));
        Assert.True(
            session.Village.Relationship(npcId).Points > beforeRelationship
        );
    }

    [Theory]
    [MemberData(nameof(NewPersonalEvents))]
    public void RelationshipCrossingThresholdTriggersOnlyOnNextTalk(
        string eventId
    )
    {
        var definition = CharacterEventCatalog.ById[eventId];
        var trigger = FindTrigger(definition);
        var prepared = PrepareEventSession(definition, trigger);
        var save = prepared.Capture();
        var relationship = save.Village.Relationships.Single(entry =>
            entry.NpcId == definition.NpcId
        );
        relationship.Points = definition.RequiredRelationshipPoints - 1;
        relationship.LastTalkDay = 0;
        var session = Restore(save);
        var npc = CurrentNpc(session, definition, trigger);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);

        var crossingTalk = session.InteractWithVillager(
            npc.Position,
            out var crossingResult
        );

        Assert.True(crossingResult.Succeeded);
        Assert.NotNull(crossingTalk);
        Assert.True(
            crossingTalk.RelationshipPoints >=
                definition.RequiredRelationshipPoints
        );
        Assert.Null(crossingTalk.CharacterEvent);
        Assert.Null(session.CharacterEvents.ActiveEventId);

        var nextTalk = session.InteractWithVillager(
            npc.Position,
            out var nextResult
        );

        Assert.True(nextResult.Succeeded);
        Assert.Equal(eventId, nextTalk?.CharacterEvent?.EventId);
        Assert.Equal(eventId, session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void NpcDConditionResponsesUseExactWeatherOrSeasonPriority()
    {
        var checks = new[]
        {
            (VillageCatalog.YvaraId, 1, 11 * 60,
                DataCatalog.RainWeatherId,
                PlayerLocationIds.TwilightEmporium,
                "village.npc.yvara.weather_rain",
                VillageCatalog.WeatherSchedulePriority),
            (VillageCatalog.BrialId, 43, 10 * 60,
                DataCatalog.ClearWeatherId,
                PlayerLocationIds.StarweaverTeaHouse,
                "village.npc.brial.season_longnight",
                VillageCatalog.SeasonSchedulePriority),
            (VillageCatalog.PavriId, 15, 10 * 60,
                DataCatalog.ClearWeatherId,
                PlayerLocationIds.MoonstoneWorkshop,
                "village.npc.pavri.season_rainveil",
                VillageCatalog.SeasonSchedulePriority),
            (VillageCatalog.RovenId, 1, 14 * 60,
                DataCatalog.RainWeatherId,
                PlayerLocationIds.World,
                "village.npc.roven.weather_rain",
                VillageCatalog.WeatherSchedulePriority)
        };

        foreach (var (npcId, day, minute, weatherId, locationId,
                     dialogueKey, priority) in checks)
        {
            var entry = NpcScheduleSystem.SelectEntry(
                VillageCatalog.Npcs[npcId],
                day,
                minute,
                weatherId
            );
            Assert.NotNull(entry);
            Assert.Equal(locationId, entry.LocationId);
            Assert.Equal(dialogueKey, entry.DialogueKey);
            Assert.Equal(priority, entry.Priority);
        }
    }

    [Fact]
    public void NpcDPersonalEventsStayAnchoredToTheirRealPlacesAndAreas()
    {
        Assert.Equal(
            PlayerLocationIds.TwilightEmporium,
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcDYvaraTheDaySheLeftTheCaseClosedId
            ].RequiredLocationId
        );
        Assert.Equal(
            "village.npc.brial.plaza",
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcDBrialThePruningMarkHeErasedId
            ].RequiredNpcDialogueKey
        );
        Assert.Equal(
            "village.npc.pavri.plaza",
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcDPavriTheCuffTestedInMotionId
            ].RequiredNpcDialogueKey
        );
        Assert.Equal(
            PlayerLocationIds.StarlightPost,
            CharacterEventCatalog.ById[
                CharacterEventCatalog.NpcDRovenARouteForAnOrdinaryDayId
            ].RequiredLocationId
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
