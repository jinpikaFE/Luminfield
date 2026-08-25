using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcDGroupCharacterEventTests
{
    private const string GroupId = "npc_d_one_bench_four_kinds_of_readiness";

    private static readonly string[] ParticipantIds =
    [
        VillageCatalog.YvaraId,
        VillageCatalog.BrialId,
        VillageCatalog.PavriId,
        VillageCatalog.RovenId
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

    public static IEnumerable<object[]> LanternrestSeasonDays()
    {
        yield return [7];
        yield return [21];
        yield return [35];
        yield return [49];
    }

    public static IEnumerable<object[]> GroupCompletionSubsets()
    {
        for (var mask = 0;
             mask < 1 << GroupCharacterEventCatalog.Definitions.Count;
             mask++)
        {
            yield return [mask];
        }
    }

    public static IEnumerable<object[]> ActiveGroupPairs()
    {
        var ids = GroupCharacterEventCatalog.Definitions
            .Select(definition => definition.Id)
            .ToArray();
        foreach (var activeId in ids)
        {
            foreach (var attemptedId in ids.Where(id => id != activeId))
            {
                yield return [activeId, attemptedId];
            }
        }
    }

    [Fact]
    public void NpcDGroupCatalogHasStablePlaceBoundContract()
    {
        Assert.Equal(GroupId, Definition.Id);
        Assert.Equal(VillageCatalog.RovenId, Definition.TriggerNpcId);
        Assert.Equal(ParticipantIds, Definition.ParticipantNpcIds);
        Assert.Equal(90, Definition.RequiredRelationshipPoints);
        Assert.Equal(PlayerLocationIds.World, Definition.RequiredLocationId);
        Assert.Equal(7 * 60, Definition.RequiredStartMinute);
        Assert.Equal(9 * 60, Definition.RequiredEndMinute);
        Assert.Equal(
            new GridArea(142, 75, 150, 79),
            Definition.RequiredParticipantArea
        );
        Assert.Equal(
            [
                CharacterEventCatalog.NpcDYvaraASeedRecordInTwoHandsId,
                CharacterEventCatalog.NpcDBrialThePruningMarkHeErasedId,
                CharacterEventCatalog.NpcDPavriOneStitchBesideTheOldId,
                CharacterEventCatalog.NpcDRovenARouteForAnOrdinaryDayId
            ],
            Definition.RequiredCharacterEventIds
        );
        Assert.Equal(
            [
                VillageCatalog.RovenId,
                VillageCatalog.YvaraId,
                VillageCatalog.BrialId,
                VillageCatalog.PavriId,
                VillageCatalog.RovenId
            ],
            Definition.Pages.Select(page => page.SpeakerNpcId)
        );
        Assert.All(ParticipantIds, npcId => Assert.Equal(
            $"village.npc.{npcId}.npc_d_group",
            Definition.RequiredNpcDialogueKeys[npcId]
        ));
        Assert.True(VillageCatalog.FestivalSchedulePriority >
            VillageCatalog.GroupEventSchedulePriority);
        Assert.True(VillageCatalog.GroupEventSchedulePriority >
            VillageCatalog.RestdaySchedulePriority);
    }

    [Fact]
    public void NpcDGroupStartsOnlyFromRealRovenAndCompletesOnCallback()
    {
        var session = PrepareEligibleSession();
        var current = CurrentParticipants(session);
        var allAtMeeting = session.Village.CurrentNpcs(
            session.Clock.Day,
            session.Clock.MinuteOfDay,
            PlayerLocationIds.World,
            session.PlayerCell
        );
        Assert.Equal(4, current.Count);
        Assert.All(current, npc =>
        {
            var expected =
                VillageCatalog.NpcDGroupMeetingCells[npc.Definition.Id];
            var occupant = allAtMeeting.FirstOrDefault(other =>
                other.Definition.Id != npc.Definition.Id &&
                other.Position == expected
            );
            Assert.True(
                npc.Position == expected,
                $"{npc.Definition.Id} did not reach {expected}; " +
                $"the cell is occupied by {occupant?.Definition.Id ?? "none"}."
            );
            Assert.Equal(
                Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
                npc.DialogueKey
            );
        });

        var nonTrigger = current.Single(npc =>
            npc.Definition.Id == VillageCatalog.YvaraId);
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

        var roven = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        roven = NpcTestPositioning.PlacePlayerAdjacent(session, roven);
        var conversation = session.InteractWithVillager(
            roven.Position,
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
    public void NpcDGroupConvergesOneCellPerTickAndUsesHalfOpenBoundary()
    {
        IReadOnlyDictionary<string, GridPosition>? previous = null;
        for (var minute = GameClock.StartMinute;
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
            Assert.All(positions.Values, position => Assert.True(
                NpcNavigationMap.IsNpcPassable(
                    PlayerLocationIds.World,
                    position
                )
            ));
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
        stagingSave.MinuteOfDay = GameClock.StartMinute;
        Assert.All(CurrentParticipants(Restore(stagingSave)), npc =>
        {
            Assert.Equal(
                VillageCatalog.NpcDGroupStagingCells[npc.Definition.Id],
                npc.Position
            );
            Assert.EndsWith("_wait", npc.DialogueKey);
        });

        var earlySave = PrepareEligibleSave();
        earlySave.MinuteOfDay = Definition.RequiredStartMinute - 10;
        var earlySession = Restore(earlySave);
        var earlyRoven = CurrentParticipants(earlySession).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        earlyRoven = NpcTestPositioning.PlacePlayerAdjacent(
            earlySession,
            earlyRoven
        );
        Assert.Null(earlySession.InteractWithVillager(
            earlyRoven.Position,
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
    public void PlayerBlockingEachNpcDAnchorKeepsEveryoneDistinctAndReady(
        string blockedNpcId
    )
    {
        var save = PrepareEligibleSave();
        var blocked = VillageCatalog.NpcDGroupMeetingCells[blockedNpcId];
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
    public void NpcDGroupMeetingOverridesWeatherAndSeasonRoutes(
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

    [Theory]
    [MemberData(nameof(LanternrestSeasonDays))]
    public void NpcDGroupMeetingOverridesEverySeasonRoute(int day)
    {
        var save = PrepareEligibleSave();
        save.Day = day;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Village = Relationships(ParticipantIds, day);
        save.CharacterEvents = CompletedPersonalChains(
            ParticipantIds,
            day - 1
        );

        var current = CurrentParticipants(Restore(save));

        Assert.Equal(4, current.Count);
        Assert.All(current, npc => Assert.Equal(
            Definition.RequiredNpcDialogueKeys[npc.Definition.Id],
            npc.DialogueKey
        ));
    }

    [Fact]
    public void NpcDGroupAreaStagingMeetingAndTravelCellsArePassable()
    {
        var cells = new HashSet<GridPosition>();
        for (var x = Definition.RequiredParticipantArea.MinX;
             x <= Definition.RequiredParticipantArea.MaxX;
             x++)
        {
            for (var y = Definition.RequiredParticipantArea.MinY;
                 y <= Definition.RequiredParticipantArea.MaxY;
                 y++)
            {
                cells.Add(new GridPosition(x, y));
            }
        }
        foreach (var participantId in ParticipantIds)
        {
            var staging = VillageCatalog.NpcDGroupStagingCells[participantId];
            var meeting = VillageCatalog.NpcDGroupMeetingCells[participantId];
            cells.Add(staging);
            cells.Add(meeting);
            var step = Math.Sign(meeting.X - staging.X);
            for (var x = staging.X; x != meeting.X; x += step)
            {
                cells.Add(new GridPosition(x, staging.Y));
            }
        }

        Assert.All(cells, cell => Assert.True(
            NpcNavigationMap.IsNpcPassable(PlayerLocationIds.World, cell),
            $"Expected NPC-D group cell {cell} to be passable."
        ));
    }

    [Fact]
    public void NpcDGroupRequiresEveryRelationshipAndEarlierFinalEvent()
    {
        foreach (var participantId in ParticipantIds)
        {
            var lowRelationship = PrepareEligibleSave();
            lowRelationship.Village.Relationships.Single(relationship =>
                relationship.NpcId == participantId
            ).Points = 89;
            AssertGroupDoesNotStart(lowRelationship);

            var notMet = PrepareEligibleSave();
            notMet.Village.MetNpcIds.Remove(participantId);
            AssertGroupDoesNotStart(notMet);

            var sameDayPrerequisite = PrepareEligibleSave();
            var requiredId = Definition.RequiredCharacterEventIds.Single(id =>
                CharacterEventCatalog.ById[id].NpcId == participantId
            );
            sameDayPrerequisite.CharacterEvents.Entries.Single(entry =>
                entry.EventId == requiredId
            ).CompletedDay = sameDayPrerequisite.Day;
            AssertGroupDoesNotStart(sameDayPrerequisite);

            var missingPrerequisite = PrepareEligibleSave();
            missingPrerequisite.CharacterEvents.Entries.RemoveAll(entry =>
                entry.EventId == requiredId
            );
            AssertGroupDoesNotStart(missingPrerequisite);
        }
    }

    [Fact]
    public void WrongToolAndInvalidCompletionLeaveWholeSessionUnchanged()
    {
        var session = PrepareEligibleSession();
        var roven = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        roven = NpcTestPositioning.PlacePlayerAdjacent(session, roven);
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        var conversation = session.InteractWithVillager(
            roven.Position,
            out var result
        );
        var invalidCompletion = session.CompleteGroupCharacterEvent(GroupId);

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.False(invalidCompletion.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void GiftChangesSocialStateButNeverStartsNpcDGroup()
    {
        var session = PrepareEligibleSession();
        var roven = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        roven = NpcTestPositioning.PlacePlayerAdjacent(session, roven);
        Assert.True(session.Inventory.Add(DataCatalog.MoonstonePathId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonstonePathId
        ));
        var beforeGroup = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            roven.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.Equal(GiftReaction.Loved, conversation?.GiftReaction);
        Assert.Null(conversation?.GroupCharacterEvent);
        Assert.Null(session.GroupCharacterEvents.ActiveEventId);
        Assert.Equal(beforeGroup, JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        ));
    }

    [Fact]
    public void NpcDGroupRoundTripsOnceWithoutPersistingAnActiveDialogue()
    {
        var session = PrepareEligibleSession();
        var roven = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        roven = NpcTestPositioning.PlacePlayerAdjacent(session, roven);
        Assert.NotNull(session.InteractWithVillager(
            roven.Position,
            out var startResult
        )?.GroupCharacterEvent);
        Assert.True(startResult.Succeeded);

        var activeReload = Restore(session.Capture());
        Assert.Null(activeReload.GroupCharacterEvents.ActiveEventId);
        Assert.False(activeReload.GroupCharacterEvents.IsCompleted(GroupId));

        Assert.True(session.CompleteGroupCharacterEvent(GroupId).Succeeded);
        var completedReload = Restore(session.Capture());
        Assert.True(completedReload.GroupCharacterEvents.IsCompleted(GroupId));
        var reloadedRoven = CurrentParticipants(completedReload).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        reloadedRoven = NpcTestPositioning.PlacePlayerAdjacent(
            completedReload,
            reloadedRoven
        );
        Assert.Null(completedReload.InteractWithVillager(
            reloadedRoven.Position,
            out var replayResult
        )?.GroupCharacterEvent);
        Assert.True(replayResult.Succeeded);
    }

    [Theory]
    [MemberData(nameof(GroupCompletionSubsets))]
    public void AllFourGroupCompletionSubsetsRoundTripIndependently(int mask)
    {
        var allIds = GroupCharacterEventCatalog.Definitions
            .Select(definition => definition.Id)
            .ToArray();
        var save = PrepareAllGroupsEligibleSave();
        save.Day = 8;
        save.GroupCharacterEvents = new GroupCharacterEventSave
        {
            Entries = allIds
                .Where((_, index) => (mask & (1 << index)) != 0)
                .Select(id => GroupEntry(id, 7))
                .ToList()
        };
        var reload = Restore(Restore(save).Capture());

        Assert.All(allIds.Select((id, index) => (id, index)), item =>
            Assert.Equal(
                (mask & (1 << item.index)) != 0,
                reload.GroupCharacterEvents.IsCompleted(item.id)
            )
        );
    }

    [Theory]
    [MemberData(nameof(ActiveGroupPairs))]
    public void ActiveGroupCannotBeOverwrittenByAnyOtherGroup(
        string activeGroupId,
        string attemptedGroupId
    )
    {
        var session = Restore(PrepareAllGroupsEligibleSave());
        var activeDefinition = GroupCharacterEventCatalog.ById[activeGroupId];
        StartGroup(session, activeDefinition);
        Assert.Equal(activeGroupId,
            session.GroupCharacterEvents.ActiveEventId);

        var attempted = GroupCharacterEventCatalog.ById[attemptedGroupId];
        session.Clock.Reset(7, attempted.RequiredStartMinute);
        var trigger = CurrentNpcs(session, attempted.ParticipantNpcIds)
            .Single(npc => npc.Definition.Id == attempted.TriggerNpcId);
        trigger = NpcTestPositioning.PlacePlayerAdjacent(session, trigger);
        var conversation = session.InteractWithVillager(
            trigger.Position,
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
        var roven = CurrentParticipants(session).Single(npc =>
            npc.Definition.Id == Definition.TriggerNpcId);
        roven = NpcTestPositioning.PlacePlayerAdjacent(session, roven);
        var before = JsonSerializer.Serialize(
            session.GroupCharacterEvents.Capture()
        );

        var conversation = session.InteractWithVillager(
            roven.Position,
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
