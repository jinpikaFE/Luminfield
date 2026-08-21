using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcExpansionTests
{
    private static readonly string[] NewNpcIds =
    [
        VillageCatalog.HaldenId,
        VillageCatalog.MaveaId,
        VillageCatalog.SivrenId,
        VillageCatalog.DorrikId
    ];

    private static readonly string[] WeatherIds =
    [
        DataCatalog.ClearWeatherId,
        DataCatalog.RainWeatherId,
        DataCatalog.StardustWindWeatherId,
        DataCatalog.LongnightSnowWeatherId
    ];

    [Fact]
    public void FirstNpcExpansionAddsFourStableOrderedDefinitions()
    {
        Assert.True(VillageCatalog.Npcs.Count >= 12);
        Assert.Equal(
            Enumerable.Range(0, VillageCatalog.Npcs.Count),
            VillageCatalog.Npcs.Values
                .OrderBy(definition => definition.ScheduleOrder)
                .Select(definition => definition.ScheduleOrder)
        );
        Assert.All(NewNpcIds, npcId =>
        {
            var definition = VillageCatalog.Npcs[npcId];
            Assert.Equal(npcId, definition.Id);
            Assert.NotEmpty(definition.LovedGiftIds);
            Assert.Empty(
                definition.LikedGiftKinds.Intersect(
                    definition.DislikedGiftKinds
                )
            );
            Assert.Contains(
                definition.Schedule,
                entry => entry.Priority ==
                    VillageCatalog.RestdaySchedulePriority
            );
            Assert.Contains(
                definition.Schedule,
                entry => entry.Priority is
                    VillageCatalog.SeasonSchedulePriority or
                    VillageCatalog.WeatherSchedulePriority
            );
        });
    }

    [Fact]
    public void NpcArtCatalogMapsEveryNpcWithoutFallbackRows()
    {
        Assert.Equal(
            VillageCatalog.Npcs.Keys.Order(StringComparer.Ordinal),
            NpcArtCatalog.All.Keys.Order(StringComparer.Ordinal)
        );
        for (var row = 0; row < NewNpcIds.Length; row++)
        {
            var definition = NpcArtCatalog.DefinitionFor(NewNpcIds[row]);
            Assert.Equal(NpcArtCatalog.Wave2AtlasId, definition.AtlasId);
            Assert.Equal(row, definition.Row);
            Assert.Equal(52, definition.TargetHeight);
        }
        Assert.Throws<KeyNotFoundException>(() =>
            NpcArtCatalog.DefinitionFor("unregistered_npc")
        );
    }

    [Fact]
    public void CatalogNpcSchedulesStayCompleteUniqueAndPassableAcrossYear()
    {
        var village = new VillageSystem();
        for (var day = 1; day <= CalendarSystem.DaysPerYear; day++)
        {
            foreach (var weatherId in WeatherIds)
            {
                for (var minute = GameClock.StartMinute;
                     minute < GameClock.EndMinute;
                     minute += GameClock.MinutesPerTick)
                {
                    var current = village.AllCurrentNpcs(
                        day,
                        minute,
                        weatherId
                    );
                    Assert.Equal(VillageCatalog.Npcs.Count, current.Count);
                    Assert.Equal(
                        current.Count,
                        current.Select(state =>
                            (state.LocationId, state.Position)
                        ).Distinct().Count()
                    );
                    Assert.All(current, state =>
                    {
                        Assert.True(NpcNavigationMap.IsNpcPassable(
                            state.LocationId,
                            state.Position
                        ));
                        Assert.False(NpcNavigationMap.IsCriticalEntranceCell(
                            state.LocationId,
                            state.Position
                        ));
                    });
                }
            }
        }
    }

    [Fact]
    public void MissingGiftDoesNotMeetNpcOrMutateRelationshipState()
    {
        var village = new VillageSystem();
        var state = village.AllCurrentNpcs(
                1,
                14 * 60,
                DataCatalog.ClearWeatherId
            )
            .Single(entry => entry.Definition.Id == VillageCatalog.HaldenId);
        var inventory = new Inventory();
        inventory.Reset();
        var before = JsonSerializer.Serialize(village.Capture());

        var conversation = village.Interact(
            state.Position,
            1,
            14 * 60,
            state.LocationId,
            DataCatalog.StarfeatherEggId,
            inventory,
            out var result
        );

        Assert.Null(conversation);
        Assert.False(result.Succeeded);
        Assert.Equal("village.gift.missing_item", result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(village.Capture()));
    }

    [Theory]
    [InlineData(
        VillageCatalog.HaldenId,
        DataCatalog.StarfeatherEggId,
        DataCatalog.MeadowFodderId,
        DataCatalog.CrystalShardId,
        DataCatalog.StarbudId
    )]
    [InlineData(
        VillageCatalog.MaveaId,
        DataCatalog.MoonmistStewId,
        DataCatalog.StarhoneyId,
        DataCatalog.CrystalShardId,
        DataCatalog.StarbudSeedId
    )]
    [InlineData(
        VillageCatalog.SivrenId,
        DataCatalog.CloudleafTeaId,
        DataCatalog.StarhoneyId,
        DataCatalog.MeadowFodderId,
        DataCatalog.MoonstonePathId
    )]
    [InlineData(
        VillageCatalog.DorrikId,
        DataCatalog.MoonstonePathId,
        DataCatalog.LumenwoodId,
        DataCatalog.StarbudSeedId,
        DataCatalog.StarfeatherEggId
    )]
    public void NewNpcGiftChecksCoverEveryReactionAndHandRule(
        string npcId,
        string lovedId,
        string likedId,
        string dislikedId,
        string neutralId
    )
    {
        const int day = 1;
        const int minute = 14 * 60;
        var village = new VillageSystem();
        var state = village.AllCurrentNpcs(
                day,
                minute,
                DataCatalog.ClearWeatherId
            )
            .Single(entry => entry.Definition.Id == npcId);

        var hand = village.CheckInteraction(
            state.Position,
            day,
            minute,
            state.LocationId,
            DataCatalog.HandId,
            DataCatalog.ClearWeatherId
        );
        Assert.True(hand.IsAvailable);
        Assert.False(hand.IsGift);

        var wrongTool = village.CheckInteraction(
            state.Position,
            day,
            minute,
            state.LocationId,
            DataCatalog.ShovelId,
            DataCatalog.ClearWeatherId
        );
        Assert.False(wrongTool.IsAvailable);
        Assert.Equal("notice.needs_hand", wrongTool.FailureKey);

        Assert.Equal(GiftReaction.Loved, Reaction(lovedId));
        Assert.Equal(GiftReaction.Liked, Reaction(likedId));
        Assert.Equal(GiftReaction.Disliked, Reaction(dislikedId));
        Assert.Equal(GiftReaction.Neutral, Reaction(neutralId));
        return;

        GiftReaction? Reaction(string itemId) => village.CheckInteraction(
            state.Position,
            day,
            minute,
            state.LocationId,
            itemId,
            DataCatalog.ClearWeatherId
        ).GiftReaction;
    }

    [Fact]
    public void NewRelationshipEventChainsRequireEarlierDayPrerequisites()
    {
        var definitions = CharacterEventCatalog.Definitions
            .Where(definition => NewNpcIds.Contains(definition.NpcId))
            .ToArray();
        Assert.Equal(8, definitions.Length);
        Assert.All(definitions, definition =>
        {
            Assert.Equal(3, definition.DialogueKeys.Count);
            Assert.Contains(
                definition.RequiredRelationshipPoints,
                new[] { 25, 60 }
            );
        });

        foreach (var npcId in NewNpcIds)
        {
            var chain = definitions
                .Where(definition => definition.NpcId == npcId)
                .ToArray();
            Assert.Equal(2, chain.Length);
            Assert.Null(chain[0].RequiredPreviousEventId);
            Assert.Equal(chain[0].Id, chain[1].RequiredPreviousEventId);

            var normalized = CharacterEventSystem.NormalizeSave(
                new CharacterEventSave
                {
                    Entries =
                    [
                        new CharacterEventEntrySave
                        {
                            EventId = chain[1].Id,
                            CompletedDay = 2
                        },
                        new CharacterEventEntrySave
                        {
                            EventId = chain[0].Id,
                            CompletedDay = 2
                        }
                    ]
                },
                2
            );
            Assert.Single(normalized.Entries);
            Assert.Equal(chain[0].Id, normalized.Entries[0].EventId);
        }
    }

    [Fact]
    public void EachNewNpcCanStartItsFirstRelationshipEvent()
    {
        foreach (var definition in CharacterEventCatalog.Definitions.Where(
                     entry =>
                         NewNpcIds.Contains(entry.NpcId) &&
                         entry.RequiredPreviousEventId is null
                 ))
        {
            var trigger = FindTrigger(definition);
            var session = new GameSession();
            session.NewGame();
            var save = session.Capture();
            save.Day = trigger.Day;
            save.MinuteOfDay = trigger.Minute;
            save.Player.LocationId = definition.RequiredLocationId;
            save.Player.X = 0 * 16 + 8;
            save.Player.Y = 0 * 16 + 8;
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
            session.Restore(save);
            session.Inventory.Select(0);
            var npc = PlacePlayerAdjacent(session, definition, trigger);
            Assert.Equal(definition.RequiredNpcDialogueKey, npc.DialogueKey);

            var conversation = session.InteractWithVillager(
                npc.Position,
                out var result
            );

            Assert.True(result.Succeeded);
            Assert.NotNull(conversation);
            Assert.NotNull(conversation.CharacterEvent);
            Assert.Equal(
                definition.Id,
                conversation.CharacterEvent.EventId
            );
        }
    }

    [Fact]
    public void KindredMailsWaitUntilDayAfterSecondEventAndDeliverOnce()
    {
        var completed = CharacterEventCatalog.Definitions
            .Where(definition => NewNpcIds.Contains(definition.NpcId))
            .Select(definition => new CharacterEventEntrySave
            {
                EventId = definition.Id,
                CompletedDay = definition.RequiredPreviousEventId is null
                    ? 1
                    : 2
            })
            .ToList();
        var events = new CharacterEventSystem();
        events.Restore(new CharacterEventSave { Entries = completed }, 2);
        var mail = new MailSystem();
        var village = new VillageSystem();

        Assert.Equal(0, mail.DeliverForDay(2, village, events));
        Assert.Equal(4, mail.DeliverForDay(3, village, events));
        Assert.Equal(0, mail.DeliverForDay(4, village, events));

        var delivered = mail.Delivered.ToDictionary(
            entry => entry.Definition.Id,
            StringComparer.Ordinal
        );
        Assert.Equal(
            (DataCatalog.MeadowFodderId, 14),
            Attachment(delivered[MailCatalog.HaldenKindredId])
        );
        Assert.Equal(
            (DataCatalog.StarhoneyCustardId, 1),
            Attachment(delivered[MailCatalog.MaveaKindredId])
        );
        Assert.Equal(
            (DataCatalog.StarlightTorchId, 2),
            Attachment(delivered[MailCatalog.SivrenKindredId])
        );
        Assert.Equal(
            (DataCatalog.LumenwoodId, 8),
            Attachment(delivered[MailCatalog.DorrikKindredId])
        );
    }

    [Fact]
    public void ExistingSaveRestoresWithNewNpcStateEmptyAndUnknownIdsFiltered()
    {
        var normalized = VillageSystem.NormalizeSave(new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId, "retired_npc"],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 37
                },
                new VillageRelationshipSave
                {
                    NpcId = "retired_npc",
                    Points = 100
                }
            ]
        });

        Assert.Equal([VillageCatalog.LioraId], normalized.MetNpcIds);
        var relationship = Assert.Single(normalized.Relationships);
        Assert.Equal(VillageCatalog.LioraId, relationship.NpcId);
        Assert.DoesNotContain(
            normalized.Relationships,
            entry => NewNpcIds.Contains(entry.NpcId)
        );
    }

    private static (string? ItemId, int Count) Attachment(
        DeliveredMail mail
    ) => (
        mail.Definition.AttachmentItemId,
        mail.Definition.AttachmentCount
    );

    private static (int Day, int Minute) FindTrigger(
        CharacterEventDefinition definition
    )
    {
        var npc = VillageCatalog.Npcs[definition.NpcId];
        for (var day = 1; day <= CalendarSystem.DaysPerYear; day++)
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
            $"No schedule trigger exists for {definition.Id}."
        );
    }

    private static VillageNpcState PlacePlayerAdjacent(
        GameSession session,
        CharacterEventDefinition definition,
        (int Day, int Minute) trigger
    )
    {
        var npc = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Single(state => state.Definition.Id == definition.NpcId);
        var occupied = session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .Select(state => state.Position)
            .ToHashSet();
        var approach = new[]
            {
                new GridPosition(npc.Position.X, npc.Position.Y + 1),
                new GridPosition(npc.Position.X - 1, npc.Position.Y),
                new GridPosition(npc.Position.X + 1, npc.Position.Y),
                new GridPosition(npc.Position.X, npc.Position.Y - 1)
            }
            .First(candidate =>
                NpcNavigationMap.IsWalkableGeometry(
                    definition.RequiredLocationId,
                    candidate
                ) &&
                !NpcNavigationMap.IsCriticalEntranceCell(
                    definition.RequiredLocationId,
                    candidate
                ) &&
                !occupied.Contains(candidate)
            );

        session.SetPlayerLocation(
            approach.X * 16 + 8,
            approach.Y * 16 + 8,
            definition.RequiredLocationId
        );
        return session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId,
                session.PlayerCell
            )
            .Single(state => state.Definition.Id == definition.NpcId);
    }
}
