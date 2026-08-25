using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcACharacterEventTests
{
    public static IEnumerable<object[]> NewPersonalEvents()
    {
        foreach (var definition in CharacterEventCatalog.Definitions.Where(
                     definition => definition.Id.StartsWith(
                         "npc_a_",
                         StringComparison.Ordinal
                     )
                 ))
        {
            yield return [definition.Id];
        }
    }

    [Fact]
    public void NpcAAddsEightPersonalEventsWithoutChangingExistingChains()
    {
        var expectedOld = new Dictionary<string, string[]>(
            StringComparer.Ordinal
        )
        {
            [VillageCatalog.LioraId] =
            [
                CharacterEventCatalog.LioraFadedReturnRouteId,
                CharacterEventCatalog.LioraRememberedWayHomeId
            ],
            [VillageCatalog.TaviId] =
            [
                CharacterEventCatalog.TaviCrackedMoonRuneId,
                CharacterEventCatalog.TaviMendedLightId
            ],
            [VillageCatalog.OrinId] =
            [
                CharacterEventCatalog.OrinUnpricedWaybillId,
                CharacterEventCatalog.OrinSharedLanternRouteId
            ],
            [VillageCatalog.VessaId] =
            [
                CharacterEventCatalog.VessaBitterLeafWarmCupId,
                CharacterEventCatalog.VessaPathThatListensBackId
            ]
        };

        foreach (var npcId in CharacterEventCatalog.NpcAIds)
        {
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
                expectedOld[npcId],
                chain.Take(2).Select(definition => definition.Id)
            );
            Assert.All(
                chain.Skip(2),
                definition => Assert.StartsWith("npc_a_", definition.Id)
            );
        }

        Assert.Equal(
            8,
            CharacterEventCatalog.Definitions.Count(definition =>
                definition.Id.StartsWith("npc_a_", StringComparison.Ordinal)
            )
        );
        Assert.All(
            VillageCatalog.Npcs.Keys.Except(
                CharacterEventCatalog.FourEventNpcIds,
                StringComparer.Ordinal
            ),
            npcId => Assert.Equal(
                2,
                CharacterEventCatalog.Definitions.Count(definition =>
                    definition.NpcId == npcId
                )
            )
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
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);
        Assert.Equal(definition.RequiredNpcDialogueKey, npc.DialogueKey);

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

    [Fact]
    public void OldTwoEventSaveContinuesAtThirdWithoutRepeatingOldEvents()
    {
        var third = CharacterEventCatalog.ById[
            CharacterEventCatalog.NpcALioraMarginOfLivingRoutesId
        ];
        var trigger = FindTrigger(third);
        var session = PrepareEventSession(third, trigger);
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                third.RequiredLocationId
            )
            .Single(state => state.Definition.Id == third.NpcId);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(third.Id, conversation?.CharacterEvent?.EventId);
        Assert.True(session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.LioraFadedReturnRouteId
        ));
        Assert.True(session.CharacterEvents.IsCompleted(
            CharacterEventCatalog.LioraRememberedWayHomeId
        ));
    }

    [Fact]
    public void FourthEventRequiresThirdOnAnEarlierDayAtRuntimeAndRestore()
    {
        var fourthId = CharacterEventCatalog.NpcALioraFirstUncopiedChartId;
        var thirdId = CharacterEventCatalog.NpcALioraMarginOfLivingRoutesId;
        var sameDay = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries = [
                    Entry(CharacterEventCatalog.LioraFadedReturnRouteId, 1),
                    Entry(CharacterEventCatalog.LioraRememberedWayHomeId, 2),
                    Entry(thirdId, 3),
                    Entry(fourthId, 3)
                ]
            },
            3
        );
        var later = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries = [
                    Entry(CharacterEventCatalog.LioraFadedReturnRouteId, 1),
                    Entry(CharacterEventCatalog.LioraRememberedWayHomeId, 2),
                    Entry(thirdId, 3),
                    Entry(fourthId, 4)
                ]
            },
            4
        );

        Assert.DoesNotContain(sameDay.Entries, entry => entry.EventId == fourthId);
        Assert.Contains(later.Entries, entry => entry.EventId == fourthId);
    }

    [Fact]
    public void WrongToolAndInvalidCompletionLeaveEventStateUnchanged()
    {
        var definition = CharacterEventCatalog.ById[
            CharacterEventCatalog.NpcAVessaCupBrewedForHerselfId
        ];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var wrongTool = session.InteractWithVillager(npc.Position, out var result);
        var invalidCompletion = session.CompleteCharacterEvent(definition.Id);

        Assert.Null(wrongTool);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void GiftMayChangeInventoryAndRelationshipButNeverStartsEvent()
    {
        var definition = CharacterEventCatalog.ById[
            CharacterEventCatalog.NpcAVessaCupBrewedForHerselfId
        ];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);
        Assert.True(session.Inventory.Add(DataCatalog.CloudleafId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.CloudleafId
        ));
        var beforeEvents = JsonSerializer.Serialize(
            session.CharacterEvents.Capture()
        );
        var beforeCount = session.Inventory.Count(DataCatalog.CloudleafId);
        var beforeRelationship = session.Village.Relationship(
            definition.NpcId
        ).Points;

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
        Assert.Equal(
            beforeCount - 1,
            session.Inventory.Count(DataCatalog.CloudleafId)
        );
        Assert.True(
            session.Village.Relationship(definition.NpcId).Points >
                beforeRelationship
        );
    }

    [Fact]
    public void RelationshipCrossingSeventyFiveTriggersOnlyOnNextTalk()
    {
        var definition = CharacterEventCatalog.ById[
            CharacterEventCatalog.NpcALioraMarginOfLivingRoutesId
        ];
        var trigger = FindTrigger(definition);
        var prepared = PrepareEventSession(definition, trigger);
        var save = prepared.Capture();
        var relationship = save.Village.Relationships.Single(entry =>
            entry.NpcId == definition.NpcId
        );
        relationship.Points = 74;
        relationship.LastTalkDay = 0;
        var session = new GameSession();
        session.NewGame();
        session.Restore(save);
        session.Inventory.Select(0);
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        npc = NpcTestPositioning.PlacePlayerAdjacent(session, npc);

        var crossingTalk = session.InteractWithVillager(
            npc.Position,
            out var crossingResult
        );

        Assert.True(crossingResult.Succeeded);
        Assert.NotNull(crossingTalk);
        Assert.True(crossingTalk.RelationshipPoints >= 75);
        Assert.Null(crossingTalk.CharacterEvent);
        Assert.Null(session.CharacterEvents.ActiveEventId);

        var nextTalk = session.InteractWithVillager(
            npc.Position,
            out var nextResult
        );

        Assert.True(nextResult.Succeeded);
        Assert.Equal(definition.Id, nextTalk?.CharacterEvent?.EventId);
        Assert.Equal(definition.Id, session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void NewConditionResponsesUsePriorityWithoutMutatingNpcIdentity()
    {
        var checks = new[]
        {
            (VillageCatalog.LioraId, 1, DataCatalog.RainWeatherId,
                "village.npc.liora.weather_rain"),
            (VillageCatalog.TaviId, 43, DataCatalog.ClearWeatherId,
                "village.npc.tavi.season_longnight"),
            (VillageCatalog.VessaId, 1, DataCatalog.StardustWindWeatherId,
                "village.npc.vessa.weather_stardust"),
            (VillageCatalog.OrinId, 43, DataCatalog.LongnightSnowWeatherId,
                "village.npc.orin.weather_longnight_snow")
        };

        foreach (var (npcId, day, weatherId, dialogueKey) in checks)
        {
            var entry = NpcScheduleSystem.SelectEntry(
                VillageCatalog.Npcs[npcId],
                day,
                14 * 60,
                weatherId
            );
            Assert.Equal(dialogueKey, entry?.DialogueKey);
        }

        var restday = NpcScheduleSystem.SelectEntry(
            VillageCatalog.Npcs[VillageCatalog.TaviId],
            7,
            14 * 60,
            DataCatalog.StardustWindWeatherId
        );
        Assert.Equal(
            "village.npc.tavi.npc_a_group",
            restday?.DialogueKey
        );
        Assert.Equal(
            VillageCatalog.GroupEventSchedulePriority,
            restday?.Priority
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
            Relationships = [new VillageRelationshipSave
            {
                NpcId = definition.NpcId,
                Points = definition.RequiredRelationshipPoints,
                LastTalkDay = trigger.Day
            }]
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
        session.Restore(save);
        session.Inventory.Select(0);
        return session;
    }

    private static (int Day, int Minute) FindTrigger(
        CharacterEventDefinition definition
    )
    {
        for (var day = 4; day <= CalendarSystem.DaysPerYear; day++)
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

    private static CharacterEventEntrySave Entry(string id, int day) =>
        new() { EventId = id, CompletedDay = day };
}
