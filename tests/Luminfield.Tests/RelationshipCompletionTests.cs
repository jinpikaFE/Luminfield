using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class RelationshipCompletionTests
{
    private static readonly string[] CompletedMailIds =
    [
        MailCatalog.KaelKindredId,
        MailCatalog.SelaKindredId,
        MailCatalog.ElowenKindredId,
        MailCatalog.VessaKindredId,
        MailCatalog.OrinKindredId
    ];

    [Fact]
    public void EveryCatalogNpcHasItsOrderedThreePageEventsAndRelationshipMail()
    {
        Assert.Equal(
            VillageCatalog.Npcs.Count * 2 +
                CharacterEventCatalog.FourEventNpcIds.Count * 2,
            CharacterEventCatalog.Definitions.Count
        );

        foreach (var npc in VillageCatalog.Npcs.Values)
        {
            var chain = CharacterEventCatalog.Definitions
                .Where(definition => definition.NpcId == npc.Id)
                .OrderBy(definition => definition.RequiredRelationshipPoints)
                .ToArray();
            var expectedThresholds = CharacterEventCatalog.FourEventNpcIds.Contains(
                npc.Id
            )
                ? new[] { 25, 60, 75, 90 }
                : new[] { 25, 60 };
            Assert.Equal(expectedThresholds.Length, chain.Length);
            for (var index = 0; index < chain.Length; index++)
            {
                Assert.Equal(
                    expectedThresholds[index],
                    chain[index].RequiredRelationshipPoints
                );
                Assert.Equal(
                    index == 0 ? null : chain[index - 1].Id,
                    chain[index].RequiredPreviousEventId
                );
            }
            Assert.All(chain, definition =>
            {
                Assert.Equal(3, definition.DialogueKeys.Count);
                Assert.Equal(
                    3,
                    definition.DialogueKeys
                        .Distinct(StringComparer.Ordinal)
                        .Count()
                );
            });

            Assert.Contains(
                MailCatalog.Definitions,
                mail => MailOwnerId(mail) == npc.Id &&
                    mail.DeliveryRule?.Kind is
                        MailDeliveryTriggerKind.RelationshipTier or
                        MailDeliveryTriggerKind.CharacterEventCompleted
            );
        }
    }

    [Theory]
    [InlineData(CharacterEventCatalog.ElowenTideMarksAtTheWellId)]
    [InlineData(CharacterEventCatalog.ElowenWaterlineReadTogetherId)]
    [InlineData(CharacterEventCatalog.VessaBitterLeafWarmCupId)]
    [InlineData(CharacterEventCatalog.VessaPathThatListensBackId)]
    public void ElowenAndVessaEventsStartOnlyAtTheirRealScheduleProjection(
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

        var preview = session.PreviewSelectedTarget(npc.Position);
        Assert.True(
            preview.IsAvailable,
            $"{eventId}: {preview.State}/{preview.Kind}/{preview.LabelKey} at {npc.Position} from {session.PlayerCell}."
        );
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);

        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation?.CharacterEvent);
        Assert.Equal(eventId, conversation.CharacterEvent.EventId);
        Assert.Equal(eventId, session.CharacterEvents.ActiveEventId);
        Assert.DoesNotContain(
            session.CharacterEvents.Capture().Entries,
            entry => entry.EventId == eventId
        );

        var completed = session.CompleteCharacterEvent(eventId);
        Assert.True(completed.Succeeded);
        Assert.Equal(trigger.Day, session.CharacterEvents.CompletedDay(eventId));
    }

    [Theory]
    [InlineData(
        CharacterEventCatalog.ElowenTideMarksAtTheWellId,
        CharacterEventCatalog.ElowenWaterlineReadTogetherId
    )]
    [InlineData(
        CharacterEventCatalog.VessaBitterLeafWarmCupId,
        CharacterEventCatalog.VessaPathThatListensBackId
    )]
    public void NewSecondEventsRequireTheirFirstEventOnAnEarlierDay(
        string firstId,
        string secondId
    )
    {
        var sameDay = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(firstId, 2),
                    EventEntry(secondId, 2)
                ]
            },
            2
        );
        var laterDay = CharacterEventSystem.NormalizeSave(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(firstId, 1),
                    EventEntry(secondId, 2)
                ]
            },
            2
        );

        Assert.Single(sameDay.Entries);
        Assert.Equal(firstId, sameDay.Entries[0].EventId);
        Assert.Equal([firstId, secondId], laterDay.Entries.Select(entry => entry.EventId));
    }

    [Fact]
    public void FiveCompletionMailsArriveNextDayOnceWithExactAttachments()
    {
        var events = new CharacterEventSystem();
        events.Restore(
            new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(CharacterEventCatalog.KaelBrokenBlueRuneId, 1),
                    EventEntry(CharacterEventCatalog.KaelSafeReturnRouteId, 2),
                    EventEntry(CharacterEventCatalog.SelaTemperedStarlightId, 1),
                    EventEntry(CharacterEventCatalog.SelaSharedForgeRhythmId, 2),
                    EventEntry(CharacterEventCatalog.ElowenTideMarksAtTheWellId, 1),
                    EventEntry(CharacterEventCatalog.ElowenWaterlineReadTogetherId, 2),
                    EventEntry(CharacterEventCatalog.VessaBitterLeafWarmCupId, 1),
                    EventEntry(CharacterEventCatalog.VessaPathThatListensBackId, 2),
                    EventEntry(CharacterEventCatalog.OrinUnpricedWaybillId, 1),
                    EventEntry(CharacterEventCatalog.OrinSharedLanternRouteId, 2)
                ]
            },
            2
        );
        var mail = new MailSystem();
        var village = new VillageSystem();

        Assert.Equal(0, mail.DeliverForDay(2, village, events));
        Assert.Equal(5, mail.DeliverForDay(3, village, events));
        Assert.Equal(0, mail.DeliverForDay(4, village, events));

        var delivered = mail.Delivered.ToDictionary(
            entry => entry.Definition.Id,
            StringComparer.Ordinal
        );
        Assert.Equal(
            (DataCatalog.MoonstonePathId, 12),
            Attachment(delivered[MailCatalog.KaelKindredId])
        );
        Assert.Equal(
            (DataCatalog.CrystalShardId, 4),
            Attachment(delivered[MailCatalog.SelaKindredId])
        );
        Assert.Equal(
            (DataCatalog.DewfallSprinklerId, 1),
            Attachment(delivered[MailCatalog.ElowenKindredId])
        );
        Assert.Equal(
            (DataCatalog.CloudleafTeaId, 2),
            Attachment(delivered[MailCatalog.VessaKindredId])
        );
        Assert.Equal(
            (DataCatalog.StarbudPreserveId, 2),
            Attachment(delivered[MailCatalog.OrinKindredId])
        );
        Assert.Equal(
            CompletedMailIds.Order(StringComparer.Ordinal),
            delivered.Keys.Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void FarVillagerPreviewAndActionAreBlockedWithoutStateMutation()
    {
        var definition = CharacterEventCatalog.ById[
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        ];
        var trigger = FindTrigger(definition);
        var session = PrepareEventSession(definition, trigger);
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        session.SetPlayerLocation(
            VillageCatalog.VillageGateCell.X * 16 + 8,
            VillageCatalog.VillageGateCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.True(Distance(session.PlayerCell, npc.Position) > 1);
        var before = JsonSerializer.Serialize(session.Capture());

        var preview = session.PreviewSelectedTarget(npc.Position);
        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);
        Assert.Equal("notice.nothing_to_interact", preview.LabelKey);
        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.Equal("notice.nothing_to_interact", result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void ActiveRelationshipEventCannotBeOverwrittenByAnotherNpc()
    {
        var definition = CharacterEventCatalog.ById[
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        ];
        var trigger = FindTrigger(definition);
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = trigger.Day;
        save.MinuteOfDay = trigger.Minute;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Village = new VillageSave
        {
            MetNpcIds = VillageCatalog.Npcs.Keys.ToList(),
            Relationships = VillageCatalog.Npcs.Keys
                .Select(npcId => new VillageRelationshipSave
                {
                    NpcId = npcId,
                    Points = 25,
                    LastTalkDay = trigger.Day
                })
                .ToList()
        };
        session.Restore(save);
        session.Inventory.Select(0);
        var elowen = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                PlayerLocationIds.World
            )
            .Single(state => state.Definition.Id == VillageCatalog.ElowenId);
        elowen = NpcTestPositioning.PlacePlayerAdjacent(session, elowen);
        var opening = session.InteractWithVillager(
            elowen.Position,
            out var openingResult
        );
        Assert.True(openingResult.Succeeded);
        Assert.Equal(
            definition.Id,
            opening?.CharacterEvent?.EventId
        );

        var other = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                PlayerLocationIds.World,
                session.PlayerCell
            )
            .First(state =>
                state.Definition.Id != VillageCatalog.ElowenId &&
                CharacterEventCatalog.Definitions.Any(eventDefinition =>
                    eventDefinition.NpcId == state.Definition.Id &&
                    eventDefinition.RequiredPreviousEventId is null
                )
            );
        other = NpcTestPositioning.PlacePlayerAdjacent(session, other);
        var conversation = session.InteractWithVillager(
            other.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.NotNull(conversation);
        Assert.Null(conversation.CharacterEvent);
        Assert.Equal(definition.Id, session.CharacterEvents.ActiveEventId);
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
        if (definition.RequiredPreviousEventId is not null)
        {
            save.CharacterEvents = new CharacterEventSave
            {
                Entries =
                [
                    EventEntry(
                        definition.RequiredPreviousEventId,
                        trigger.Day - 1
                    )
                ]
            };
        }
        session.Restore(save);
        session.Inventory.Select(0);
        return session;
    }

    private static (int Day, int Minute) FindTrigger(
        CharacterEventDefinition definition
    )
    {
        var minimumDay = definition.RequiredPreviousEventId is null ? 1 : 2;
        var npc = VillageCatalog.Npcs[definition.NpcId];
        for (var day = minimumDay; day <= CalendarSystem.DaysPerYear; day++)
        {
            var weatherId = WeatherSystem.WeatherForDay(day);
            for (var minute = GameClock.StartMinute;
                 minute < GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var entry = NpcScheduleSystem.SelectEntry(
                    npc,
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

    private static string? MailOwnerId(MailDefinition mail)
    {
        var rule = mail.DeliveryRule;
        return rule?.Kind switch
        {
            MailDeliveryTriggerKind.MetNpc or
                MailDeliveryTriggerKind.RelationshipTier => rule.ReferenceId,
            MailDeliveryTriggerKind.CharacterEventCompleted =>
                CharacterEventCatalog.ById[rule.ReferenceId].NpcId,
            _ => null
        };
    }

    private static CharacterEventEntrySave EventEntry(
        string eventId,
        int completedDay
    ) => new() { EventId = eventId, CompletedDay = completedDay };

    private static (string? ItemId, int Count) Attachment(
        DeliveredMail mail
    ) => (
        mail.Definition.AttachmentItemId,
        mail.Definition.AttachmentCount
    );

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
}
