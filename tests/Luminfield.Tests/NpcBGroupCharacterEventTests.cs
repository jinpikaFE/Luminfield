using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcBGroupCharacterEventTests
{
    private const string NpcBGroupId = "npc_b_last_lamp_waits_for_return";
    private const string NemiFourthId =
        "npc_b_nemi_hook_for_her_own_mailbag";
    private const string KaelFourthId =
        "npc_b_kael_last_marker_on_the_return_board";
    private const string SelaFourthId =
        "npc_b_sela_hammer_fitted_to_her_hand";
    private const string HaldenFourthId =
        "npc_b_halden_bell_he_chose_not_to_ring";

    private static readonly string[] ParticipantIds =
    [
        VillageCatalog.NemiId,
        VillageCatalog.KaelId,
        VillageCatalog.SelaId,
        VillageCatalog.HaldenId
    ];

    private static readonly IReadOnlyDictionary<string, GridPosition>
        MeetingCells = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [VillageCatalog.NemiId] = new(86, 79),
            [VillageCatalog.KaelId] = new(90, 79),
            [VillageCatalog.SelaId] = new(86, 81),
            [VillageCatalog.HaldenId] = new(90, 81)
        };

    private static readonly IReadOnlyDictionary<string, GridPosition>
        StagingCells = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [VillageCatalog.NemiId] = new(83, 79),
            [VillageCatalog.KaelId] = new(93, 79),
            [VillageCatalog.SelaId] = new(83, 81),
            [VillageCatalog.HaldenId] = new(93, 81)
        };

    private static GroupCharacterEventDefinition Definition =>
        GroupCharacterEventCatalog.ById[NpcBGroupId];

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
    public void GroupCatalogHasStableContractAndEveryParticipantSpeaks()
    {
        Assert.Equal(NpcBGroupId, Definition.Id);
        Assert.Equal(VillageCatalog.NemiId, Definition.TriggerNpcId);
        Assert.Equal(ParticipantIds, Definition.ParticipantNpcIds);
        Assert.Equal(90, Definition.RequiredRelationshipPoints);
        Assert.Equal(PlayerLocationIds.World, Definition.RequiredLocationId);
        Assert.Equal(9 * 60, Definition.RequiredStartMinute);
        Assert.Equal(12 * 60, Definition.RequiredEndMinute);
        Assert.Equal(new GridArea(84, 78, 92, 82),
            Definition.RequiredParticipantArea);
        Assert.Equal(
            [NemiFourthId, KaelFourthId, SelaFourthId, HaldenFourthId],
            Definition.RequiredCharacterEventIds
        );
        Assert.Equal(
            [
                VillageCatalog.NemiId,
                VillageCatalog.KaelId,
                VillageCatalog.SelaId,
                VillageCatalog.HaldenId,
                VillageCatalog.NemiId
            ],
            Definition.Pages.Select(page => page.SpeakerNpcId)
        );
        Assert.Equal(5, Definition.Pages.Count);
        Assert.Equal(
            5,
            Definition.Pages.Select(page => page.DialogueKey)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(ParticipantIds, npcId => Assert.Equal(
            $"village.npc.{npcId}.npc_b_group",
            Definition.RequiredNpcDialogueKeys[npcId]
        ));
        Assert.Contains(
            GroupCharacterEventCatalog.Definitions,
            definition => definition.Id ==
                GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        );
    }

    [Fact]
    public void GroupStartsFromRealNemiAndCompletesOnlyAfterCallback()
    {
        var session = PrepareEligibleSession();
        var current = CurrentParticipants(session);
        Assert.Equal(4, current.Count);
        Assert.All(current, npc =>
        {
            Assert.Equal(MeetingCells[npc.Definition.Id], npc.Position);
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });
        var nemi = current.Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);

        var conversation = session.InteractWithVillager(
            nemi.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(NpcBGroupId, conversation?.GroupCharacterEvent?.EventId);
        Assert.Equal(Definition.Pages, conversation?.GroupCharacterEvent?.Pages);
        Assert.Equal(NpcBGroupId, session.GroupCharacterEvents.ActiveEventId);
        Assert.False(session.GroupCharacterEvents.IsCompleted(NpcBGroupId));
        Assert.True(session.CompleteGroupCharacterEvent(NpcBGroupId).Succeeded);
        Assert.Equal(
            session.Clock.Day,
            session.GroupCharacterEvents.CompletedDay(NpcBGroupId)
        );
    }

    [Fact]
    public void GroupConvergesOneCellPerTickAndUsesExactTimeBoundary()
    {
        IReadOnlyDictionary<string, GridPosition>? previous = null;
        for (var minute = 8 * 60 + 20;
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
        stagingSave.MinuteOfDay = 8 * 60 + 20;
        Assert.All(CurrentParticipants(Restore(stagingSave)), npc =>
        {
            Assert.Equal(StagingCells[npc.Definition.Id], npc.Position);
            Assert.DoesNotContain(
                npc.DialogueKey,
                Definition.RequiredNpcDialogueKeys.Values
            );
        });

        var earlySave = PrepareEligibleSave();
        earlySave.MinuteOfDay = Definition.RequiredStartMinute - 10;
        var earlySession = Restore(earlySave);
        var early = CurrentParticipants(earlySession);
        Assert.All(early, npc => Assert.Equal(
            MeetingCells[npc.Definition.Id],
            npc.Position
        ));
        var earlyNemi = early.Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        earlyNemi = NpcTestPositioning.PlacePlayerAdjacent(
            earlySession,
            earlyNemi
        );
        var earlyConversation = earlySession.InteractWithVillager(
            earlyNemi.Position,
            out var earlyResult
        );
        Assert.True(earlyResult.Succeeded);
        Assert.Null(earlyConversation?.GroupCharacterEvent);

        var startSave = PrepareEligibleSave();
        startSave.MinuteOfDay = Definition.RequiredStartMinute;
        Assert.All(CurrentParticipants(Restore(startSave)), npc =>
        {
            Assert.Equal(MeetingCells[npc.Definition.Id], npc.Position);
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });

        var beforeEndSave = PrepareEligibleSave();
        beforeEndSave.MinuteOfDay = Definition.RequiredEndMinute - 10;
        Assert.All(CurrentParticipants(Restore(beforeEndSave)), npc =>
            Assert.True(Definition.RequiredParticipantArea.Contains(
                npc.Position
            ))
        );

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
    public void PlayerBlockingEveryMeetingAnchorKeepsGroupDistinctAndReady(
        string blockedNpcId
    )
    {
        var save = PrepareEligibleSave();
        save.MinuteOfDay = Definition.RequiredStartMinute;
        var blockedCell = MeetingCells[blockedNpcId];
        save.Player.X = blockedCell.X * 16 + 8;
        save.Player.Y = blockedCell.Y * 16 + 8;
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
    public void GroupMeetingOverridesEveryWeatherSpecificRoute(
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
            Assert.Equal(
                VillageCatalog.GroupEventSchedulePriority,
                VillageCatalog.Npcs[npc.Definition.Id].Schedule.Single(entry =>
                    entry.DialogueKey == npc.DialogueKey &&
                    entry.StartMinute == Definition.RequiredStartMinute
                ).Priority
            );
        });
    }

    [Fact]
    public void OnlyNemiCanTriggerTheGroupEvent()
    {
        var session = PrepareEligibleSession();
        var kael = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == VillageCatalog.KaelId
        );
        kael = NpcTestPositioning.PlacePlayerAdjacent(session, kael);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            kael.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(
            before,
            JsonSerializer.Serialize(session.GroupCharacterEvents.Capture())
        );
    }

    [Fact]
    public void GroupRequiresEveryRelationshipAndEarlierFourthEvent()
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
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var conversation = session.InteractWithVillager(
            nemi.Position,
            out var result
        );
        var invalidCompletion = session.CompleteGroupCharacterEvent(
            NpcBGroupId
        );

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void GiftChangesSocialStateButNeverStartsGroupEvent()
    {
        var session = PrepareEligibleSession();
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.StarbudId));
        var beforeGroup = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );
        var beforeCount = session.Inventory.Count(DataCatalog.StarbudId);
        var beforeRelationship = session.Village.Relationship(
            VillageCatalog.NemiId
        ).Points;

        var conversation = session.InteractWithVillager(
            nemi.Position,
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
            session.Inventory.Count(DataCatalog.StarbudId)
        );
        Assert.True(
            session.Village.Relationship(VillageCatalog.NemiId).Points >
                beforeRelationship
        );
    }

    [Fact]
    public void GroupRoundTripsOnceAndDoesNotPersistActive()
    {
        var session = PrepareEligibleSession();
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);
        var conversation = session.InteractWithVillager(
            nemi.Position,
            out var result
        );
        Assert.True(result.Succeeded);
        Assert.NotNull(conversation?.GroupCharacterEvent);

        var activeReload = Restore(session.Capture());
        Assert.Null(activeReload.GroupCharacterEvents.ActiveEventId);
        Assert.False(activeReload.GroupCharacterEvents.IsCompleted(NpcBGroupId));

        Assert.True(session.CompleteGroupCharacterEvent(NpcBGroupId).Succeeded);
        var completedReload = Restore(session.Capture());
        Assert.True(completedReload.GroupCharacterEvents.IsCompleted(
            NpcBGroupId
        ));
        var reloadedNemi = CurrentParticipants(completedReload).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        reloadedNemi = NpcTestPositioning.PlacePlayerAdjacent(
            completedReload,
            reloadedNemi
        );

        var replay = completedReload.InteractWithVillager(
            reloadedNemi.Position,
            out var replayResult
        );

        Assert.True(replayResult.Succeeded);
        Assert.Null(replay?.GroupCharacterEvent);
    }

    [Fact]
    public void NpcAAndNpcBGroupCompletionsRoundTripIndependently()
    {
        var save = PrepareBothGroupsEligibleSave();
        save.GroupCharacterEvents = new GroupCharacterEventSave
        {
            Entries =
            [
                GroupEntry(
                    GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
                    7
                ),
                GroupEntry(NpcBGroupId, 7)
            ]
        };
        save.Day = 8;
        var bothReload = Restore(Restore(save).Capture());

        Assert.True(bothReload.GroupCharacterEvents.IsCompleted(
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ));
        Assert.True(bothReload.GroupCharacterEvents.IsCompleted(NpcBGroupId));

        save.GroupCharacterEvents.Entries =
        [
            GroupEntry(
                GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
                7
            )
        ];
        var onlyA = Restore(save);
        Assert.True(onlyA.GroupCharacterEvents.IsCompleted(
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ));
        Assert.False(onlyA.GroupCharacterEvents.IsCompleted(NpcBGroupId));

        save.GroupCharacterEvents.Entries = [GroupEntry(NpcBGroupId, 7)];
        var onlyB = Restore(save);
        Assert.False(onlyB.GroupCharacterEvents.IsCompleted(
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ));
        Assert.True(onlyB.GroupCharacterEvents.IsCompleted(NpcBGroupId));
    }

    [Fact]
    public void NormalizeDropsInvalidNpcBRecordsWithoutChangingNpcARecord()
    {
        var personal = PrepareBothGroupsEligibleSave().CharacterEvents;
        var normalized = GroupCharacterEventSystem.NormalizeSave(
            new GroupCharacterEventSave
            {
                Entries =
                [
                    new GroupCharacterEventEntrySave
                    {
                        EventId = "unknown_group",
                        CompletedDay = 7
                    },
                    GroupEntry(NpcBGroupId, 14),
                    GroupEntry(NpcBGroupId, 8),
                    GroupEntry(
                        GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
                        7
                    )
                ]
            },
            7,
            personal
        );

        Assert.Equal(
            [
                GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
                NpcBGroupId
            ],
            normalized.Entries.Select(entry => entry.EventId)
        );
        Assert.All(normalized.Entries, entry => Assert.Equal(
            7,
            entry.CompletedDay
        ));

        var orphan = GroupCharacterEventSystem.NormalizeSave(
            new GroupCharacterEventSave
            {
                Entries = [GroupEntry(NpcBGroupId, 7)]
            },
            7,
            new CharacterEventSave()
        );
        Assert.Empty(orphan.Entries);
    }

    [Fact]
    public void ActiveNpcAAndNpcBGroupsCannotOverwriteEachOther()
    {
        var bFirst = Restore(PrepareBothGroupsEligibleSave());
        var nemi = CurrentParticipants(bFirst).Single(npc =>
            npc.Definition.Id == VillageCatalog.NemiId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(bFirst, nemi);
        var bConversation = bFirst.InteractWithVillager(
            nemi.Position,
            out var bResult
        );
        Assert.True(bResult.Succeeded);
        Assert.NotNull(bConversation?.GroupCharacterEvent);

        bFirst.Clock.Reset(7, 14 * 60);
        var liora = CurrentNpcs(bFirst,
                GroupCharacterEventCatalog.ById[
                    GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
                ].ParticipantNpcIds)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        liora = NpcTestPositioning.PlacePlayerAdjacent(bFirst, liora);
        var whileB = bFirst.InteractWithVillager(
            liora.Position,
            out var whileBResult
        );
        Assert.True(whileBResult.Succeeded);
        Assert.Null(whileB?.GroupCharacterEvent);
        Assert.Equal(NpcBGroupId, bFirst.GroupCharacterEvents.ActiveEventId);

        var aFirst = Restore(PrepareBothGroupsEligibleSave());
        aFirst.Clock.Reset(7, 14 * 60);
        var aDefinition = GroupCharacterEventCatalog.ById[
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ];
        var aLiora = CurrentNpcs(aFirst, aDefinition.ParticipantNpcIds)
            .Single(npc => npc.Definition.Id == VillageCatalog.LioraId);
        aLiora = NpcTestPositioning.PlacePlayerAdjacent(aFirst, aLiora);
        var aConversation = aFirst.InteractWithVillager(
            aLiora.Position,
            out var aResult
        );
        Assert.True(aResult.Succeeded);
        Assert.NotNull(aConversation?.GroupCharacterEvent);

        aFirst.Clock.Reset(7, 10 * 60);
        var laterNemi = CurrentParticipants(aFirst).Single(npc =>
            npc.Definition.Id == VillageCatalog.NemiId
        );
        laterNemi = NpcTestPositioning.PlacePlayerAdjacent(
            aFirst,
            laterNemi
        );
        var whileA = aFirst.InteractWithVillager(
            laterNemi.Position,
            out var whileAResult
        );
        Assert.True(whileAResult.Succeeded);
        Assert.Null(whileA?.GroupCharacterEvent);
        Assert.Equal(
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId,
            aFirst.GroupCharacterEvents.ActiveEventId
        );
    }

    [Fact]
    public void ActiveStoryPreventsNpcBGroupFromStarting()
    {
        var session = PrepareEligibleSession();
        session.StarlightStory.TryBegin(
            StarlightStoryCatalog.WoodlandDiscoveryId,
            new StarlightStoryProgressContext(
                session.Clock.Day,
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
        Assert.NotNull(session.StarlightStory.ActiveBeatId);
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == VillageCatalog.NemiId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);

        var conversation = session.InteractWithVillager(
            nemi.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Null(session.GroupCharacterEvents.ActiveEventId);
        Assert.NotNull(session.StarlightStory.ActiveBeatId);
    }

    [Fact]
    public void ActivePersonalEventPreventsNpcBGroupFromStarting()
    {
        var personalDefinition = CharacterEventCatalog.ById[
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        ];
        var trigger = FindTrigger(personalDefinition, 8);
        var groupDay = NextLanternrestDay(trigger.Day + 1);
        var seed = new GameSession();
        seed.NewGame();
        var save = seed.Capture();
        save.Day = trigger.Day;
        save.MinuteOfDay = trigger.Minute;
        save.Player.LocationId = personalDefinition.RequiredLocationId;
        save.Player.SelectedSlot = 0;
        save.Weather = new WeatherSave
        {
            Day = trigger.Day,
            CurrentId = WeatherSystem.WeatherForDay(trigger.Day),
            ForecastId = WeatherSystem.WeatherForDay(trigger.Day + 1)
        };
        save.Village = Relationships(ParticipantIds, trigger.Day);
        save.Village.MetNpcIds.Add(VillageCatalog.ElowenId);
        save.Village.Relationships.Add(new VillageRelationshipSave
        {
            NpcId = VillageCatalog.ElowenId,
            Points = personalDefinition.RequiredRelationshipPoints,
            LastTalkDay = trigger.Day
        });
        save.CharacterEvents = CompletedPersonalChains(
            ParticipantIds,
            trigger.Day - 1
        );
        var session = Restore(save);
        var elowen = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                personalDefinition.RequiredLocationId,
                session.PlayerCell
            )
            .Single(npc => npc.Definition.Id == VillageCatalog.ElowenId);
        elowen = NpcTestPositioning.PlacePlayerAdjacent(session, elowen);
        var personalConversation = session.InteractWithVillager(
            elowen.Position,
            out var personalResult
        );
        Assert.True(personalResult.Succeeded);
        Assert.Equal(
            personalDefinition.Id,
            personalConversation?.CharacterEvent?.EventId
        );
        var activePersonalId = session.CharacterEvents.ActiveEventId;
        Assert.NotNull(activePersonalId);

        session.Clock.Reset(groupDay, 10 * 60);
        session.SetPlayerLocation(
            VillageCatalog.VillageCenterCell.X * 16 + 8,
            VillageCatalog.VillageCenterCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == VillageCatalog.NemiId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);

        var whilePersonal = session.InteractWithVillager(
            nemi.Position,
            out var whilePersonalResult
        );

        Assert.True(whilePersonalResult.Succeeded);
        Assert.Null(whilePersonal?.GroupCharacterEvent);
        Assert.Equal(activePersonalId, session.CharacterEvents.ActiveEventId);
        Assert.Null(session.GroupCharacterEvents.ActiveEventId);
    }

    [Fact]
    public void ActiveNpcBGroupPreventsEligiblePersonalEvent()
    {
        var personalDefinition = CharacterEventCatalog.ById[
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        ];
        var trigger = FindTrigger(personalDefinition, 8);
        var save = PrepareEligibleSave();
        save.Village.MetNpcIds.Add(VillageCatalog.ElowenId);
        save.Village.Relationships.Add(new VillageRelationshipSave
        {
            NpcId = VillageCatalog.ElowenId,
            Points = personalDefinition.RequiredRelationshipPoints,
            LastTalkDay = trigger.Day
        });
        var session = Restore(save);
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == VillageCatalog.NemiId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);
        Assert.NotNull(session.InteractWithVillager(
            nemi.Position,
            out var groupResult
        )?.GroupCharacterEvent);
        Assert.True(groupResult.Succeeded);

        session.Clock.Reset(trigger.Day, trigger.Minute);
        session.SetPlayerLocation(
            8,
            8,
            personalDefinition.RequiredLocationId
        );
        var elowen = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                personalDefinition.RequiredLocationId,
                session.PlayerCell
            )
            .Single(npc => npc.Definition.Id == VillageCatalog.ElowenId);
        elowen = NpcTestPositioning.PlacePlayerAdjacent(session, elowen);

        var conversation = session.InteractWithVillager(
            elowen.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.CharacterEvent);
        Assert.Equal(NpcBGroupId, session.GroupCharacterEvents.ActiveEventId);
        Assert.Null(session.CharacterEvents.ActiveEventId);
    }

    private static void AssertGroupDoesNotStart(GameSaveV1 save)
    {
        var session = Restore(save);
        var nemi = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId
        );
        nemi = NpcTestPositioning.PlacePlayerAdjacent(session, nemi);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            nemi.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Equal(
            before,
            JsonSerializer.Serialize(session.GroupCharacterEvents.Capture())
        );
    }

    private static GameSession PrepareEligibleSession() =>
        Restore(PrepareEligibleSave());

    private static GameSaveV1 PrepareEligibleSave()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 7;
        save.MinuteOfDay = 10 * 60;
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

    private static GameSaveV1 PrepareBothGroupsEligibleSave()
    {
        var save = PrepareEligibleSave();
        var aParticipants = GroupCharacterEventCatalog.ById[
            GroupCharacterEventCatalog.NpcAFourRoutesOneLanternId
        ].ParticipantNpcIds;
        foreach (var npcId in aParticipants)
        {
            if (!save.Village.MetNpcIds.Contains(npcId, StringComparer.Ordinal))
            {
                save.Village.MetNpcIds.Add(npcId);
            }
            save.Village.Relationships.Add(new VillageRelationshipSave
            {
                NpcId = npcId,
                Points = 90,
                LastTalkDay = save.Day
            });
        }
        save.CharacterEvents.Entries.AddRange(
            CompletedPersonalChains(aParticipants, 6).Entries
        );
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

    private static (int Day, int Minute) FindTrigger(
        CharacterEventDefinition definition,
        int minimumDay
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

    private static int NextLanternrestDay(int minimumDay)
    {
        for (var day = minimumDay;
             day <= CalendarSystem.DaysPerYear;
             day++)
        {
            if (CalendarSystem.WeekdayIndex(day) ==
                CalendarSystem.LanternrestWeekdayIndex)
            {
                return day;
            }
        }
        throw new InvalidOperationException(
            $"No Lanternrest day exists after day {minimumDay - 1}."
        );
    }

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static GroupCharacterEventEntrySave GroupEntry(
        string eventId,
        int day
    ) => new() { EventId = eventId, CompletedDay = day };
}
