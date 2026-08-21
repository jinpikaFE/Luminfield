using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class NpcExpansionWave3Tests
{
    private static readonly string[] Wave3NpcIds =
    [
        VillageCatalog.YvaraId,
        VillageCatalog.BrialId,
        VillageCatalog.PavriId,
        VillageCatalog.RovenId
    ];

    private static readonly string[] Wave3EventIds =
    [
        CharacterEventCatalog.YvaraSeedsBeyondTheCalendarId,
        CharacterEventCatalog.YvaraASeasonCarriedGentlyId,
        CharacterEventCatalog.BrialBlossomsBetweenHarvestsId,
        CharacterEventCatalog.BrialAPathLeftForTheBeesId,
        CharacterEventCatalog.PavriTheVisibleMendId,
        CharacterEventCatalog.PavriClothThatKeepsWarmthId,
        CharacterEventCatalog.RovenTheRouteWithRoomToRestId,
        CharacterEventCatalog.RovenLightsThatWaitForReturnId
    ];

    [Fact]
    public void ThirdNpcWaveReachesSixteenWithStableOrderAndArtRows()
    {
        Assert.Equal(16, VillageCatalog.Npcs.Count);
        Assert.Equal(
            Enumerable.Range(0, 16),
            VillageCatalog.Npcs.Values
                .OrderBy(definition => definition.ScheduleOrder)
                .Select(definition => definition.ScheduleOrder)
        );
        Assert.Equal(
            VillageCatalog.Npcs.Keys.Order(StringComparer.Ordinal),
            NpcArtCatalog.All.Keys.Order(StringComparer.Ordinal)
        );

        for (var row = 0; row < Wave3NpcIds.Length; row++)
        {
            var npc = VillageCatalog.Npcs[Wave3NpcIds[row]];
            Assert.Equal(12 + row, npc.ScheduleOrder);
            Assert.NotEmpty(npc.LovedGiftIds);
            Assert.Empty(
                npc.LikedGiftKinds.Intersect(npc.DislikedGiftKinds)
            );
            Assert.Contains(
                npc.Schedule,
                entry => entry.Priority ==
                    VillageCatalog.RestdaySchedulePriority
            );
            Assert.Contains(
                npc.Schedule,
                entry => entry.Priority is
                    VillageCatalog.SeasonSchedulePriority or
                    VillageCatalog.WeatherSchedulePriority
            );

            var art = NpcArtCatalog.DefinitionFor(npc.Id);
            Assert.Equal(NpcArtCatalog.Wave3AtlasId, art.AtlasId);
            Assert.Equal(row, art.Row);
            Assert.Equal(52, art.TargetHeight);
        }
    }

    [Fact]
    public void ThirdNpcWaveUsesFrozenSafeFestivalAnchors()
    {
        AssertAnchors(
            StarharvestMarketLayout.NpcAnchors,
            [
                (VillageCatalog.YvaraId, new GridPosition(10, 7)),
                (VillageCatalog.BrialId, new GridPosition(30, 7)),
                (VillageCatalog.PavriId, new GridPosition(10, 18)),
                (VillageCatalog.RovenId, new GridPosition(30, 18))
            ],
            StarharvestMarketLayout.IsWalkable
        );
        AssertAnchors(
            GleamrisePlantingFestivalLayout.NpcAnchors,
            [
                (VillageCatalog.YvaraId, new GridPosition(10, 6)),
                (VillageCatalog.BrialId, new GridPosition(30, 6)),
                (VillageCatalog.PavriId, new GridPosition(9, 18)),
                (VillageCatalog.RovenId, new GridPosition(31, 18))
            ],
            GleamrisePlantingFestivalLayout.IsWalkable
        );
        AssertAnchors(
            LongnightLanternFeastLayout.NpcAnchors,
            [
                (VillageCatalog.YvaraId, new GridPosition(10, 7)),
                (VillageCatalog.BrialId, new GridPosition(30, 7)),
                (VillageCatalog.PavriId, new GridPosition(10, 18)),
                (VillageCatalog.RovenId, new GridPosition(30, 18))
            ],
            LongnightLanternFeastLayout.IsWalkable
        );
    }

    [Theory]
    [MemberData(nameof(Wave3Events))]
    public void ThirdWaveEventsStartAtRealAdjacentNpcProjection(string eventId)
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

        var preview = session.PreviewSelectedTarget(npc.Position);
        var conversation = session.InteractWithVillager(
            npc.Position,
            out var result
        );

        Assert.True(result.Succeeded);
        Assert.True(preview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);
        Assert.Equal(eventId, conversation?.CharacterEvent?.EventId);
        Assert.Equal(eventId, session.CharacterEvents.ActiveEventId);
    }

    [Fact]
    public void ThirdWaveKindredMailsArriveNextDayOnceWithExactAttachments()
    {
        var events = new CharacterEventSystem();
        events.Restore(
            new CharacterEventSave
            {
                Entries = Wave3EventIds.Select((eventId, index) =>
                    new CharacterEventEntrySave
                    {
                        EventId = eventId,
                        CompletedDay = index % 2 == 0 ? 1 : 2
                    }
                ).ToList()
            },
            2
        );
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
            (DataCatalog.MoonplumSaplingId, 1),
            Attachment(delivered[MailCatalog.YvaraKindredId])
        );
        Assert.Equal(
            (DataCatalog.StarhoneyId, 1),
            Attachment(delivered[MailCatalog.BrialKindredId])
        );
        Assert.Equal(
            (DataCatalog.MoonfleeceId, 1),
            Attachment(delivered[MailCatalog.PavriKindredId])
        );
        Assert.Equal(
            (DataCatalog.StarlightTorchId, 4),
            Attachment(delivered[MailCatalog.RovenKindredId])
        );
    }

    [Fact]
    public void ExistingTwelveNpcSaveKeepsThirdWaveStateEmpty()
    {
        var original = new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 37
                }
            ]
        };

        var normalized = VillageSystem.NormalizeSave(original);

        Assert.Equal(
            JsonSerializer.Serialize(original),
            JsonSerializer.Serialize(normalized)
        );
        Assert.DoesNotContain(
            normalized.Relationships,
            entry => Wave3NpcIds.Contains(entry.NpcId)
        );
    }

    public static IEnumerable<object[]> Wave3Events() =>
        Wave3EventIds.Select(eventId => new object[] { eventId });

    private static void AssertAnchors(
        IReadOnlyDictionary<string, GridPosition> anchors,
        IReadOnlyList<(string NpcId, GridPosition Position)> expected,
        Func<GridPosition, bool> isWalkable
    )
    {
        foreach (var (npcId, position) in expected)
        {
            Assert.Equal(position, anchors[npcId]);
            Assert.True(isWalkable(position));
        }
        Assert.Equal(anchors.Count, anchors.Values.Distinct().Count());
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
                    new CharacterEventEntrySave
                    {
                        EventId = definition.RequiredPreviousEventId,
                        CompletedDay = trigger.Day - 1
                    }
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

    private static (string? ItemId, int Count) Attachment(
        DeliveredMail mail
    ) => (
        mail.Definition.AttachmentItemId,
        mail.Definition.AttachmentCount
    );
}
