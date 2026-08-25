using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcAGroupCharacterEventTests
{
    private static readonly GroupCharacterEventDefinition Definition =
        GroupCharacterEventCatalog.ById[
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ];

    [Fact]
    public void GroupCatalogKeepsNpcAStableWithFourRealParticipants()
    {
        Assert.Contains(
            GroupCharacterEventCatalog.Definitions,
            definition => definition.Id == Definition.Id
        );
        Assert.Equal(
            [
                VillageCatalog.LioraId,
                VillageCatalog.TaviId,
                VillageCatalog.VessaId,
                VillageCatalog.OrinId
            ],
            Definition.ParticipantNpcIds
        );
        Assert.Equal(5, Definition.Pages.Count);
        Assert.All(
            Definition.ParticipantNpcIds,
            participantId => Assert.Contains(
                Definition.Pages,
                page => page.SpeakerNpcId == participantId
            )
        );
    }

    [Fact]
    public void GroupStartsFromRealLioraWhenAllFourAreTogether()
    {
        var session = PrepareEligibleSession();
        var current = CurrentGroup(session);
        Assert.Equal(4, current.Count);
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
        var liora = current.Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(session, liora);

        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(
            Definition.Id,
            conversation?.GroupCharacterEvent?.EventId
        );
        Assert.Equal(
            Definition.Pages,
            conversation?.GroupCharacterEvent?.Pages
        );
        Assert.Equal(Definition.Id, session.GroupCharacterEvents.ActiveEventId);
        Assert.False(session.GroupCharacterEvents.IsCompleted(Definition.Id));
        Assert.True(session.CompleteGroupCharacterEvent(Definition.Id).Succeeded);
        Assert.Equal(
            session.Clock.Day,
            session.GroupCharacterEvents.CompletedDay(Definition.Id)
        );
    }

    [Fact]
    public void GroupConvergesSmoothlyAndIsEligibleOnlyFromThirteenHundred()
    {
        var stagingSave = PrepareEligibleSave();
        stagingSave.MinuteOfDay = 12 * 60 + 20;
        var staging = CurrentGroup(Restore(stagingSave));
        Assert.Equal(4, staging.Count);
        Assert.All(staging, npc =>
        {
            Assert.Equal(
                VillageCatalog.NpcAGroupStagingCells[npc.Definition.Id],
                npc.Position
            );
            Assert.DoesNotContain(
                npc.DialogueKey,
                Definition.RequiredNpcDialogueKeys.Values
            );
        });

        var approachSave = PrepareEligibleSave();
        approachSave.MinuteOfDay = 12 * 60 + 50;
        var approachSession = Restore(approachSave);
        var approach = CurrentGroup(approachSession);
        Assert.Equal(4, approach.Count);
        Assert.All(approach, npc => Assert.Equal(
            VillageCatalog.NpcAGroupMeetingCells[npc.Definition.Id],
            npc.Position
        ));
        var earlyLiora = approach.Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        earlyLiora = NpcTestPositioning.PlacePlayerAdjacent(
            approachSession,
            earlyLiora
        );
        var earlyConversation = approachSession.InteractWithVillager(
            earlyLiora.Position,
            out var earlyResult
        );
        Assert.True(earlyResult.Succeeded);
        Assert.Null(earlyConversation?.GroupCharacterEvent);

        var startSave = PrepareEligibleSave();
        startSave.MinuteOfDay = Definition.RequiredStartMinute;
        var atStart = CurrentGroup(Restore(startSave));
        Assert.Equal(4, atStart.Count);
        Assert.All(atStart, npc =>
        {
            Assert.Equal(
                VillageCatalog.NpcAGroupMeetingCells[npc.Definition.Id],
                npc.Position
            );
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });

        var endSave = PrepareEligibleSave();
        endSave.MinuteOfDay = Definition.RequiredEndMinute - 10;
        var beforeEnd = CurrentGroup(Restore(endSave));
        Assert.Equal(4, beforeEnd.Count);
        Assert.All(beforeEnd, npc => Assert.True(
            Definition.RequiredParticipantArea.Contains(npc.Position)
        ));

        var afterSave = PrepareEligibleSave();
        afterSave.MinuteOfDay = Definition.RequiredEndMinute;
        var after = Restore(afterSave).Village.CurrentNpcs(
            afterSave.Day,
            afterSave.MinuteOfDay,
            PlayerLocationIds.World
        );
        Assert.DoesNotContain(after, npc =>
            Definition.RequiredNpcDialogueKeys.Values.Contains(
                npc.DialogueKey,
                StringComparer.Ordinal
            )
        );
    }

    [Fact]
    public void PlayerBlockingMeetingAnchorKeepsNpcsDistinctAndInArea()
    {
        var save = PrepareEligibleSave();
        save.MinuteOfDay = Definition.RequiredStartMinute;
        var blockedCell = VillageCatalog.NpcAGroupMeetingCells[
            VillageCatalog.LioraId
        ];
        save.Player.X = blockedCell.X * 16 + 8;
        save.Player.Y = blockedCell.Y * 16 + 8;
        var session = Restore(save);
        var current = CurrentGroup(session);

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

    [Fact]
    public void GroupRequiresEveryRelationshipAndEarlierFourthEvent()
    {
        foreach (var participantId in Definition.ParticipantNpcIds)
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
    public void GroupMeetingOverridesWeatherButOnlyLioraCanTriggerIt()
    {
        var save = PrepareEligibleSave();
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        var session = Restore(save);
        var current = CurrentGroup(session);

        Assert.Equal(4, current.Count);
        Assert.All(current, npc => Assert.Equal(
            Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
            npc.DialogueKey
        ));
        var tavi = current.Single(npc =>
            npc.Definition.Id == VillageCatalog.TaviId
        );
        tavi = NpcTestPositioning.PlacePlayerAdjacent(session, tavi);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            tavi.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(before, JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        ));
    }

    [Fact]
    public void GiftAndWrongToolCannotStartOrCompleteGroup()
    {
        var session = PrepareEligibleSession();
        var liora = CurrentGroup(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );
        var invalidCompletion = session.CompleteGroupCharacterEvent(
            Definition.Id
        );

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void GiftChangesInventoryAndRelationshipButNotGroupState()
    {
        var session = PrepareEligibleSession();
        var liora = CurrentGroup(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        Assert.True(session.Inventory.Add(DataCatalog.MoonrootId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonrootId
        ));
        var beforeGroup = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );
        var beforeCount = session.Inventory.Count(DataCatalog.MoonrootId);
        var beforeRelationship = session.Village.Relationship(
            VillageCatalog.LioraId
        ).Points;

        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(GiftReaction.Loved, conversation?.GiftReaction);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Null(session.GroupCharacterEvents.ActiveEventId);
        Assert.Equal(
            beforeGroup,
            JsonSerializer.Serialize(session.GroupCharacterEvents.Capture())
        );
        Assert.Equal(
            beforeCount - 1,
            session.Inventory.Count(DataCatalog.MoonrootId)
        );
        Assert.True(
            session.Village.Relationship(VillageCatalog.LioraId).Points >
                beforeRelationship
        );
    }

    [Fact]
    public void GroupRoundTripsOnceAndDoesNotPersistActive()
    {
        var session = PrepareEligibleSession();
        var liora = CurrentGroup(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );
        Assert.True(result.Succeeded);
        Assert.NotNull(conversation?.GroupCharacterEvent);

        var activeSave = session.Capture();
        var activeReload = Restore(activeSave);
        Assert.Null(activeReload.GroupCharacterEvents.ActiveEventId);
        Assert.False(activeReload.GroupCharacterEvents.IsCompleted(Definition.Id));

        Assert.True(session.CompleteGroupCharacterEvent(Definition.Id).Succeeded);
        var completedReload = Restore(session.Capture());
        Assert.True(completedReload.GroupCharacterEvents.IsCompleted(
            Definition.Id
        ));
        var reloadedLiora = CurrentGroup(completedReload).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        reloadedLiora = NpcTestPositioning.PlacePlayerAdjacent(
            completedReload,
            reloadedLiora
        );

        var replay = completedReload.InteractWithVillager(
            reloadedLiora.Position,
            out var replayResult
        );

        Assert.True(replayResult.Succeeded);
        Assert.Null(replay?.GroupCharacterEvent);
    }

    [Fact]
    public void NormalizeDropsUnknownDuplicateFutureAndOrphanRecords()
    {
        var validPersonal = PrepareEligibleSave().CharacterEvents;
        var normalized = GroupCharacterEventSystem.NormalizeSave(
            new GroupCharacterEventSave
            {
                Entries = [
                    new GroupCharacterEventEntrySave
                    {
                        EventId = "unknown",
                        CompletedDay = 7
                    },
                    GroupEntry(Definition.Id, 14),
                    GroupEntry(Definition.Id, 8)
                ]
            },
            7,
            validPersonal
        );
        var orphan = GroupCharacterEventSystem.NormalizeSave(
            new GroupCharacterEventSave
            {
                Entries = [GroupEntry(Definition.Id, 7)]
            },
            7,
            new CharacterEventSave()
        );

        var entry = Assert.Single(normalized.Entries);
        Assert.Equal(Definition.Id, entry.EventId);
        Assert.Equal(7, entry.CompletedDay);
        Assert.Empty(orphan.Entries);
    }

    [Fact]
    public void ActiveNarrativesCannotOverwriteEachOther()
    {
        var personalSession = Restore(PrepareEligibleSaveWithNemi());
        var nemi = personalSession.Village.CurrentNpcs(
                personalSession.Clock.Day,
                personalSession.Clock.MinuteOfDay,
                PlayerLocationIds.World,
                personalSession.PlayerCell
            )
            .Single(npc => npc.Definition.Id == VillageCatalog.NemiId);
        nemi = NpcTestPositioning.PlacePlayerAdjacent(personalSession, nemi);
        var personalConversation = personalSession.InteractWithVillager(
            nemi.Position,
            out var personalResult
        );
        Assert.True(personalResult.Succeeded);
        Assert.Equal(
            CharacterEventCatalog.NemiUndeliverableLetterId,
            personalConversation?.CharacterEvent?.EventId
        );
        var personalActiveId = personalSession.CharacterEvents.ActiveEventId;
        Assert.NotNull(personalActiveId);

        var liora = CurrentGroup(personalSession).Single(npc =>
            npc.Definition.Id == VillageCatalog.LioraId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(personalSession, liora);
        var whilePersonal = personalSession.InteractWithVillager(
            liora.Position,
            out var whilePersonalResult
        );
        Assert.True(whilePersonalResult.Succeeded);
        Assert.Null(whilePersonal?.GroupCharacterEvent);
        Assert.Equal(
            personalActiveId,
            personalSession.CharacterEvents.ActiveEventId
        );
        Assert.Null(personalSession.GroupCharacterEvents.ActiveEventId);

        var groupSession = Restore(PrepareEligibleSaveWithNemi());
        var groupLiora = CurrentGroup(groupSession).Single(npc =>
            npc.Definition.Id == VillageCatalog.LioraId
        );
        groupLiora = NpcTestPositioning.PlacePlayerAdjacent(
            groupSession,
            groupLiora
        );
        var groupConversation = groupSession.InteractWithVillager(
            groupLiora.Position,
            out var groupResult
        );
        Assert.True(groupResult.Succeeded);
        Assert.NotNull(groupConversation?.GroupCharacterEvent);

        var eligibleNemi = groupSession.Village.CurrentNpcs(
                groupSession.Clock.Day,
                groupSession.Clock.MinuteOfDay,
                PlayerLocationIds.World,
                groupSession.PlayerCell
            )
            .Single(npc => npc.Definition.Id == VillageCatalog.NemiId);
        eligibleNemi = NpcTestPositioning.PlacePlayerAdjacent(
            groupSession,
            eligibleNemi
        );
        var whileGroup = groupSession.InteractWithVillager(
            eligibleNemi.Position,
            out var whileGroupResult
        );
        Assert.True(whileGroupResult.Succeeded);
        Assert.Null(whileGroup?.CharacterEvent);
        Assert.Equal(
            Definition.Id,
            groupSession.GroupCharacterEvents.ActiveEventId
        );
        Assert.Null(groupSession.CharacterEvents.ActiveEventId);

        var storySession = PrepareEligibleSession();
        storySession.StarlightStory.TryBegin(
            StarlightStoryCatalog.WoodlandDiscoveryId,
            new StarlightStoryProgressContext(
                storySession.Clock.Day,
                PlayerLocationIds.World,
                null,
                new HashSet<string>(
                    [DataCatalog.WoodlandStarlightId],
                    StringComparer.Ordinal
                ),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<WorldBiome>(),
                new HashSet<string>(StringComparer.Ordinal),
                false
            )
        );
        Assert.NotNull(storySession.StarlightStory.ActiveBeatId);
        var storyLiora = CurrentGroup(storySession).Single(npc =>
            npc.Definition.Id == VillageCatalog.LioraId
        );
        storyLiora = NpcTestPositioning.PlacePlayerAdjacent(
            storySession,
            storyLiora
        );
        var whileStory = storySession.InteractWithVillager(
            storyLiora.Position,
            out var whileStoryResult
        );
        Assert.True(whileStoryResult.Succeeded);
        Assert.Null(whileStory?.GroupCharacterEvent);
        Assert.Null(storySession.GroupCharacterEvents.ActiveEventId);
    }

    private static void AssertGroupDoesNotStart(GameSaveV1 save)
    {
        var session = Restore(save);
        var liora = CurrentGroup(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        liora = NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            liora.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(before, JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        ));
    }

    private static GameSession PrepareEligibleSession() =>
        Restore(PrepareEligibleSave());

    private static GameSaveV1 PrepareEligibleSave()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 7;
        save.MinuteOfDay = 14 * 60;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.SelectedSlot = 0;
        save.Weather = new WeatherSave
        {
            Day = 7,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Village = new VillageSave
        {
            MetNpcIds = Definition.ParticipantNpcIds.ToList(),
            Relationships = Definition.ParticipantNpcIds.Select(npcId =>
                new VillageRelationshipSave
                {
                    NpcId = npcId,
                    Points = Definition.RequiredRelationshipPoints,
                    LastTalkDay = save.Day
                }
            ).ToList()
        };
        save.CharacterEvents = CompletedPersonalChains(6);
        return save;
    }

    private static GameSaveV1 PrepareEligibleSaveWithNemi()
    {
        var save = PrepareEligibleSave();
        save.Village.MetNpcIds.Add(VillageCatalog.NemiId);
        save.Village.Relationships.Add(new VillageRelationshipSave
        {
            NpcId = VillageCatalog.NemiId,
            Points = 25,
            LastTalkDay = save.Day
        });
        return save;
    }

    private static CharacterEventSave CompletedPersonalChains(int finalDay)
    {
        var entries = new List<CharacterEventEntrySave>();
        foreach (var npcId in Definition.ParticipantNpcIds)
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

    private static IReadOnlyList<VillageNpcState> CurrentGroup(
        GameSession session
    ) => session.Village.CurrentNpcs(
            session.Clock.Day,
            session.Clock.MinuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        )
        .Where(npc => Definition.ParticipantNpcIds.Contains(
            npc.Definition.Id,
            StringComparer.Ordinal
        ))
        .ToArray();

    private static GroupCharacterEventEntrySave GroupEntry(
        string id,
        int day
    ) => new() { EventId = id, CompletedDay = day };
}
