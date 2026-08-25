using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcCGroupCharacterEventTests
{
    private const string GroupId = "npc_c_one_open_corner_four_uses";

    private static readonly string[] ParticipantIds =
    [
        VillageCatalog.ElowenId,
        VillageCatalog.MaveaId,
        VillageCatalog.SivrenId,
        VillageCatalog.DorrikId
    ];

    private static GroupCharacterEventDefinition Definition =>
        GroupCharacterEventCatalog.ById[GroupId];

    public static IEnumerable<object[]> Participants() =>
        ParticipantIds.Select(npcId => new object[] { npcId });

    public static IEnumerable<object[]> WeatherCases()
    {
        yield return [DataCatalog.ClearWeatherId];
        yield return [DataCatalog.RainWeatherId];
        yield return [DataCatalog.StardustWindWeatherId];
        yield return [DataCatalog.LongnightSnowWeatherId];
    }

    [Fact]
    public void NpcCGroupCatalogHasStablePlaceBoundContract()
    {
        Assert.Equal(GroupId, Definition.Id);
        Assert.Equal(VillageCatalog.DorrikId, Definition.TriggerNpcId);
        Assert.Equal(ParticipantIds, Definition.ParticipantNpcIds);
        Assert.Equal(90, Definition.RequiredRelationshipPoints);
        Assert.Equal(PlayerLocationIds.World, Definition.RequiredLocationId);
        Assert.Equal(17 * 60, Definition.RequiredStartMinute);
        Assert.Equal(20 * 60, Definition.RequiredEndMinute);
        Assert.Equal(
            new GridArea(144, 76, 152, 84),
            Definition.RequiredParticipantArea
        );
        Assert.Equal(
            [
                CharacterEventCatalog.NpcCElowenMarkerAllowedToDriftId,
                CharacterEventCatalog.NpcCMaveaLastJarOpenedOnAnOrdinaryDayId,
                CharacterEventCatalog.NpcCSivrenFirstPersonFootnoteId,
                CharacterEventCatalog.NpcCDorrikPlanReturnedToItsUsersId
            ],
            Definition.RequiredCharacterEventIds
        );
        Assert.Equal(
            [
                VillageCatalog.DorrikId,
                VillageCatalog.ElowenId,
                VillageCatalog.MaveaId,
                VillageCatalog.SivrenId,
                VillageCatalog.DorrikId
            ],
            Definition.Pages.Select(page => page.SpeakerNpcId)
        );
        Assert.All(ParticipantIds, npcId => Assert.Equal(
            $"village.npc.{npcId}.npc_c_group",
            Definition.RequiredNpcDialogueKeys[npcId]
        ));
    }

    [Fact]
    public void NpcCGroupStartsOnlyFromRealDorrikAndCompletesOnCallback()
    {
        var session = PrepareEligibleSession();
        var current = CurrentParticipants(session);
        Assert.Equal(4, current.Count);
        Assert.All(current, npc =>
        {
            Assert.Equal(
                VillageCatalog.NpcCGroupMeetingCells[npc.Definition.Id],
                npc.Position
            );
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });

        var nonTrigger = current.Single(npc =>
            npc.Definition.Id == VillageCatalog.ElowenId);
        nonTrigger = NpcTestPositioning.PlacePlayerAdjacent(
            session,
            nonTrigger
        );
        var ordinary = session.InteractWithVillager(
            nonTrigger.Position,
            out var ordinaryResult
        );
        Assert.True(ordinaryResult.Succeeded);
        Assert.Null(ordinary?.GroupCharacterEvent);

        var dorrik = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        dorrik = NpcTestPositioning.PlacePlayerAdjacent(session, dorrik);
        var conversation = session.InteractWithVillager(
            dorrik.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(GroupId, conversation?.GroupCharacterEvent?.EventId);
        Assert.Equal(GroupId, session.GroupCharacterEvents.ActiveEventId);
        Assert.False(session.GroupCharacterEvents.IsCompleted(GroupId));
        Assert.True(session.CompleteGroupCharacterEvent(GroupId).Succeeded);
        Assert.Equal(session.Clock.Day,
            session.GroupCharacterEvents.CompletedDay(GroupId));
    }

    [Fact]
    public void NpcCGroupConvergesOneCellPerTickAndUsesHalfOpenBoundary()
    {
        IReadOnlyDictionary<string, GridPosition>? previous = null;
        for (var minute = 16 * 60 + 20;
             minute <= Definition.RequiredStartMinute;
             minute += GameClock.MinutesPerTick)
        {
            var save = PrepareEligibleSave();
            save.MinuteOfDay = minute;
            var current = CurrentParticipants(Restore(save));
            Assert.Equal(4, current.Count);
            Assert.Equal(4, current.Select(npc => npc.Position).Distinct().Count());
            var positions = current.ToDictionary(
                npc => npc.Definition.Id,
                npc => npc.Position,
                StringComparer.Ordinal
            );
            if (previous is not null)
            {
                Assert.All(ParticipantIds, npcId => Assert.InRange(
                    Distance(previous[npcId], positions[npcId]),
                    0,
                    1
                ));
            }
            previous = positions;
        }

        var stagingSave = PrepareEligibleSave();
        stagingSave.MinuteOfDay = 16 * 60 + 20;
        Assert.All(CurrentParticipants(Restore(stagingSave)), npc =>
        {
            Assert.Equal(
                VillageCatalog.NpcCGroupStagingCells[npc.Definition.Id],
                npc.Position
            );
            Assert.EndsWith("_wait", npc.DialogueKey);
        });

        var earlySave = PrepareEligibleSave();
        earlySave.MinuteOfDay = Definition.RequiredStartMinute - 10;
        var earlySession = Restore(earlySave);
        var earlyDorrik = CurrentParticipants(earlySession).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        earlyDorrik = NpcTestPositioning.PlacePlayerAdjacent(
            earlySession,
            earlyDorrik
        );
        Assert.Null(earlySession.InteractWithVillager(
            earlyDorrik.Position,
            out var earlyResult
        )?.GroupCharacterEvent);
        Assert.True(earlyResult.Succeeded);

        var endSave = PrepareEligibleSave();
        endSave.MinuteOfDay = Definition.RequiredEndMinute;
        var after = Restore(endSave).Village.CurrentNpcs(
            endSave.Day,
            endSave.MinuteOfDay,
            PlayerLocationIds.World
        );
        Assert.DoesNotContain(after, npc =>
            Definition.RequiredNpcDialogueKeys.Values.Contains(
                npc.DialogueKey,
                StringComparer.Ordinal
            )
        );
    }

    [Theory]
    [MemberData(nameof(Participants))]
    public void PlayerBlockingEachNpcCAnchorKeepsEveryoneDistinctAndReady(
        string blockedNpcId
    )
    {
        var save = PrepareEligibleSave();
        var blocked = VillageCatalog.NpcCGroupMeetingCells[blockedNpcId];
        save.Player.X = blocked.X * 16 + 8;
        save.Player.Y = blocked.Y * 16 + 8;
        var session = Restore(save);
        var current = CurrentParticipants(session);

        Assert.Equal(4, current.Count);
        Assert.Equal(4, current.Select(npc => npc.Position).Distinct().Count());
        Assert.DoesNotContain(current, npc => npc.Position == session.PlayerCell);
        Assert.All(current, npc =>
        {
            Assert.True(Definition.RequiredParticipantArea.Contains(
                npc.Position
            ));
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });
    }

    [Theory]
    [MemberData(nameof(WeatherCases))]
    public void NpcCGroupMeetingOverridesWeatherAndSeasonRoutes(
        string weatherId
    )
    {
        var save = PrepareEligibleSave();
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        var current = CurrentParticipants(Restore(save));

        Assert.Equal(4, current.Count);
        Assert.All(current, npc =>
        {
            Assert.Equal(PlayerLocationIds.World, npc.LocationId);
            Assert.True(Definition.RequiredParticipantArea.Contains(
                npc.Position
            ));
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });
    }

    [Fact]
    public void NpcCGroupRequiresEveryRelationshipAndEarlierFinalEvent()
    {
        foreach (var participantId in ParticipantIds)
        {
            var lowRelationship = PrepareEligibleSave();
            lowRelationship.Village.Relationships.Single(relationship =>
                relationship.NpcId == participantId
            ).Points = 89;
            AssertGroupDoesNotStart(lowRelationship);

            var sameDayPrerequisite = PrepareEligibleSave();
            var requiredId = Definition.RequiredCharacterEventIds.Single(id =>
                CharacterEventCatalog.ById[id].NpcId == participantId
            );
            sameDayPrerequisite.CharacterEvents.Entries.Single(entry =>
                entry.EventId == requiredId
            ).CompletedDay = sameDayPrerequisite.Day;
            AssertGroupDoesNotStart(sameDayPrerequisite);
        }
    }

    [Fact]
    public void WrongToolAndInvalidCompletionLeaveWholeSessionUnchanged()
    {
        var session = PrepareEligibleSession();
        var dorrik = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        dorrik = NpcTestPositioning.PlacePlayerAdjacent(session, dorrik);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var conversation = session.InteractWithVillager(
            dorrik.Position,
            out var result
        );
        var invalidCompletion = session.CompleteGroupCharacterEvent(GroupId);

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void NpcCGroupRoundTripsOnceWithoutPersistingAnActiveDialogue()
    {
        var session = PrepareEligibleSession();
        var dorrik = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        dorrik = NpcTestPositioning.PlacePlayerAdjacent(session, dorrik);
        Assert.NotNull(session.InteractWithVillager(
            dorrik.Position,
            out var startResult
        )?.GroupCharacterEvent);
        Assert.True(startResult.Succeeded);

        var activeReload = Restore(session.Capture());
        Assert.Null(activeReload.GroupCharacterEvents.ActiveEventId);
        Assert.False(activeReload.GroupCharacterEvents.IsCompleted(GroupId));

        Assert.True(session.CompleteGroupCharacterEvent(GroupId).Succeeded);
        var completedReload = Restore(session.Capture());
        Assert.True(completedReload.GroupCharacterEvents.IsCompleted(GroupId));
        var reloadedDorrik = CurrentParticipants(completedReload).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        reloadedDorrik = NpcTestPositioning.PlacePlayerAdjacent(
            completedReload,
            reloadedDorrik
        );
        Assert.Null(completedReload.InteractWithVillager(
            reloadedDorrik.Position,
            out var replayResult
        )?.GroupCharacterEvent);
        Assert.True(replayResult.Succeeded);
    }

    [Fact]
    public void NpcANpcBAndNpcCGroupCompletionsRoundTripIndependently()
    {
        var allIds = GroupCharacterEventCatalog.Definitions
            .Select(definition => definition.Id)
            .ToArray();
        var all = PrepareAllGroupsEligibleSave();
        all.Day = 8;
        all.GroupCharacterEvents = new GroupCharacterEventSave
        {
            Entries = allIds.Select(id => GroupEntry(id, 7)).ToList()
        };
        var allReload = Restore(Restore(all).Capture());
        Assert.All(allIds, id => Assert.True(
            allReload.GroupCharacterEvents.IsCompleted(id)));

        foreach (var onlyId in allIds)
        {
            var only = PrepareAllGroupsEligibleSave();
            only.Day = 8;
            only.GroupCharacterEvents = new GroupCharacterEventSave
            {
                Entries = [GroupEntry(onlyId, 7)]
            };
            var reload = Restore(only);
            Assert.All(allIds, id => Assert.Equal(
                id == onlyId,
                reload.GroupCharacterEvents.IsCompleted(id)
            ));
        }
    }

    [Fact]
    public void ActiveNpcCGroupCannotBeOverwrittenByNpcAOrNpcB()
    {
        var session = Restore(PrepareAllGroupsEligibleSave());
        StartGroup(session, Definition);
        Assert.Equal(GroupId, session.GroupCharacterEvents.ActiveEventId);

        foreach (var other in GroupCharacterEventCatalog.Definitions.Where(
                     definition => definition.Id != GroupId))
        {
            session.Clock.Reset(7, other.RequiredStartMinute);
            var trigger = CurrentNpcs(session, other.ParticipantNpcIds)
                .Single(npc => npc.Definition.Id == other.TriggerNpcId);
            trigger = NpcTestPositioning.PlacePlayerAdjacent(session, trigger);
            var conversation = session.InteractWithVillager(
                trigger.Position,
                out var result
            );
            Assert.True(result.Succeeded);
            Assert.Null(conversation?.GroupCharacterEvent);
            Assert.Equal(GroupId, session.GroupCharacterEvents.ActiveEventId);
        }
    }

    [Theory]
    [InlineData("npc_a_four_routes_one_lantern")]
    [InlineData("npc_b_last_lamp_waits_for_return")]
    public void ActiveNpcAOrNpcBGroupCannotBeOverwrittenByNpcC(
        string activeGroupId
    )
    {
        var session = Restore(PrepareAllGroupsEligibleSave());
        var activeDefinition = GroupCharacterEventCatalog.ById[activeGroupId];
        StartGroup(session, activeDefinition);
        Assert.Equal(activeGroupId,
            session.GroupCharacterEvents.ActiveEventId);

        session.Clock.Reset(7, Definition.RequiredStartMinute);
        var dorrik = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        dorrik = NpcTestPositioning.PlacePlayerAdjacent(session, dorrik);
        var conversation = session.InteractWithVillager(
            dorrik.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(activeGroupId,
            session.GroupCharacterEvents.ActiveEventId);
    }

    private static void StartGroup(
        GameSession session,
        GroupCharacterEventDefinition definition
    )
    {
        session.Clock.Reset(7, definition.RequiredStartMinute);
        var trigger = CurrentNpcs(session, definition.ParticipantNpcIds)
            .Single(npc => npc.Definition.Id == definition.TriggerNpcId);
        trigger = NpcTestPositioning.PlacePlayerAdjacent(session, trigger);
        var conversation = session.InteractWithVillager(
            trigger.Position,
            out var result
        );
        Assert.True(result.Succeeded);
        Assert.Equal(definition.Id,
            conversation?.GroupCharacterEvent?.EventId);
    }

    private static void AssertGroupDoesNotStart(GameSaveV1 save)
    {
        var session = Restore(save);
        var dorrik = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        dorrik = NpcTestPositioning.PlacePlayerAdjacent(session, dorrik);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            dorrik.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(before, JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()));
    }

    private static GameSession PrepareEligibleSession() =>
        Restore(PrepareEligibleSave());

    private static GameSaveV1 PrepareEligibleSave()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 7;
        save.MinuteOfDay = Definition.RequiredStartMinute;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.SelectedSlot = 0;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Village = Relationships(ParticipantIds, save.Day);
        save.CharacterEvents = CompletedPersonalChains(ParticipantIds, 6);
        return save;
    }

    private static GameSaveV1 PrepareAllGroupsEligibleSave()
    {
        var allNpcIds = GroupCharacterEventCatalog.Definitions
            .SelectMany(definition => definition.ParticipantNpcIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var save = PrepareEligibleSave();
        save.Village = Relationships(allNpcIds, save.Day);
        save.CharacterEvents = CompletedPersonalChains(allNpcIds, 6);
        return save;
    }

    private static VillageSave Relationships(
        IReadOnlyList<string> npcIds,
        int day
    ) => new()
    {
        MetNpcIds = npcIds.ToList(),
        Relationships = npcIds.Select(npcId => new VillageRelationshipSave
        {
            NpcId = npcId,
            Points = 90,
            LastTalkDay = day
        }).ToList()
    };

    private static CharacterEventSave CompletedPersonalChains(
        IReadOnlyList<string> npcIds,
        int finalDay
    )
    {
        var entries = new List<CharacterEventEntrySave>();
        foreach (var npcId in npcIds)
        {
            var chain = CharacterEventCatalog.Definitions
                .Where(definition => definition.NpcId == npcId)
                .OrderBy(definition =>
                    definition.RequiredRelationshipPoints)
                .ToArray();
            for (var index = 0; index < chain.Length; index++)
            {
                entries.Add(new CharacterEventEntrySave
                {
                    EventId = chain[index].Id,
                    CompletedDay = finalDay - chain.Length + index + 1
                });
            }
        }
        return new CharacterEventSave { Entries = entries };
    }

    private static GameSession Restore(GameSaveV1 save)
    {
        var session = new GameSession();
        session.NewGame();
        session.Restore(save);
        session.Inventory.Select(0);
        return session;
    }

    private static IReadOnlyList<VillageNpcState> CurrentParticipants(
        GameSession session
    ) => CurrentNpcs(session, ParticipantIds);

    private static IReadOnlyList<VillageNpcState> CurrentNpcs(
        GameSession session,
        IReadOnlyList<string> npcIds
    ) => session.Village.CurrentNpcs(
            session.Clock.Day,
            session.Clock.MinuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        )
        .Where(npc => npcIds.Contains(
            npc.Definition.Id,
            StringComparer.Ordinal
        ))
        .ToArray();

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static GroupCharacterEventEntrySave GroupEntry(
        string eventId,
        int day
    ) => new() { EventId = eventId, CompletedDay = day };
}
