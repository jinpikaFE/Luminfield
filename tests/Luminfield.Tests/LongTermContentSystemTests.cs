using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class LongTermContentSystemTests
{
    [Fact]
    public void RegionalEventCatalogCoversThreePacksAndAllSixOuterRegions()
    {
        var packageIds = RegionalEventCatalog.Definitions
            .Select(definition => definition.PackageId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(3, packageIds.Length);
        Assert.All(packageIds, packageId => Assert.Equal(
            5,
            RegionalEventCatalog.Definitions.Count(definition =>
                definition.PackageId == packageId
            )
        ));

        var biomes = new[]
        {
            WorldBiome.WhisperingWoods,
            WorldBiome.StarfallMeadow,
            WorldBiome.MoonwaterWetlands,
            WorldBiome.CrystalVale,
            WorldBiome.LumenVillage,
            WorldBiome.StarfallRuins
        };
        foreach (var biome in biomes)
        {
            var definitions = RegionalEventCatalog.Definitions
                .Where(definition => definition.Biome == biome)
                .ToArray();
            Assert.Single(definitions, definition =>
                definition.Kind == RegionalEventKind.RepeatableEnvironment
            );
            Assert.Single(definitions, definition =>
                definition.Kind == RegionalEventKind.OneTimeNarrative
            );
        }
    }

    [Fact]
    public void RegionalEventsSeparateRepeatableOneTimeAndPostgameRareRules()
    {
        var system = new RegionalEventSystem();
        var environment = RegionalEventCatalog.Definition(
            "regional_event_woods_moonroot_chorus"
        );

        var first = system.TryBegin(
            environment.Biome,
            1,
            7 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => 0
        );

        Assert.NotNull(first);
        Assert.Equal(environment.Id, first.EventId);
        Assert.Equal(RegionalEventKind.RepeatableEnvironment, first.Kind);
        Assert.True(system.CompleteActive(environment.Id, 1).Succeeded);
        Assert.DoesNotContain(environment.Id, system.CompletedEventIds);
        Assert.Null(system.TryBegin(
            environment.Biome,
            1,
            7 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => 0
        ));
        Assert.NotNull(system.TryBegin(
            environment.Biome,
            2,
            7 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => 0
        ));
        system.CancelActive();

        var narrative = RegionalEventCatalog.Definition(
            "regional_event_woods_vessa_listening_path"
        );
        Assert.Null(system.TryBegin(
            narrative.Biome,
            2,
            13 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => narrative.MinimumRelationshipPoints - 1
        ));

        var story = system.TryBegin(
            narrative.Biome,
            2,
            13 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            npcId => npcId == VillageCatalog.VessaId ? 25 : 0
        );

        Assert.NotNull(story);
        Assert.Equal(narrative.Id, story.EventId);
        Assert.Equal(RegionalEventKind.OneTimeNarrative, story.Kind);
        Assert.True(system.CompleteActive(narrative.Id, 2).Succeeded);
        Assert.Contains(narrative.Id, system.CompletedEventIds);
        Assert.Null(system.TryBegin(
            narrative.Biome,
            3,
            13 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => 100
        ));
    }

    [Fact]
    public void PostgameRareRegionalEventsRequireStoryCooldownAndDeduplicate()
    {
        var system = new RegionalEventSystem();
        var rare = RegionalEventCatalog.Definition(
            RegionalEventCatalog.WoodsRareEventId
        );

        Assert.Null(system.TryBegin(
            rare.Biome,
            60,
            20 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: false,
            _ => 0
        ));

        var first = system.TryBegin(
            rare.Biome,
            60,
            20 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: true,
            _ => 0
        );

        Assert.NotNull(first);
        Assert.Equal(RegionalEventKind.PostgameRare, first.Kind);
        Assert.True(system.CompleteActive(rare.Id, 60).Succeeded);
        Assert.Equal([rare.Id], system.CompletedRareEventIds);
        Assert.Null(system.TryBegin(
            rare.Biome,
            73,
            20 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: true,
            _ => 0
        ));

        var repeated = system.TryBegin(
            rare.Biome,
            74,
            20 * 60,
            DataCatalog.ClearWeatherId,
            mainStoryCompleted: true,
            _ => 0
        );

        Assert.NotNull(repeated);
        Assert.True(system.CompleteActive(rare.Id, 74).Succeeded);
        Assert.Equal([rare.Id], system.CompletedRareEventIds);
        Assert.Equal(74, Assert.Single(system.Capture().LastSeenDays).Day);
    }

    [Fact]
    public void RegionalEventSaveNormalizationKeepsOnlyKnownNonRepeatableState()
    {
        var environment = RegionalEventCatalog.Definition(
            "regional_event_woods_moonroot_chorus"
        );
        var narrative = RegionalEventCatalog.Definition(
            "regional_event_woods_vessa_listening_path"
        );
        var rare = RegionalEventCatalog.Definition(
            RegionalEventCatalog.WetlandsRareEventId
        );

        var normalized = RegionalEventSystem.NormalizeSave(
            new RegionalEventSave
            {
                CompletedEventIds =
                [
                    environment.Id,
                    narrative.Id,
                    rare.Id,
                    narrative.Id,
                    "unknown_regional_event"
                ],
                LastSeenDays =
                [
                    new RegionalEventSeenSave
                    {
                        EventId = environment.Id,
                        Day = 999
                    },
                    new RegionalEventSeenSave
                    {
                        EventId = environment.Id,
                        Day = 2
                    },
                    new RegionalEventSeenSave
                    {
                        EventId = rare.Id,
                        Day = -5
                    },
                    new RegionalEventSeenSave
                    {
                        EventId = "unknown_regional_event",
                        Day = 12
                    }
                ]
            },
            currentDay: 40
        );

        Assert.Equal(2, normalized.CompletedEventIds.Count);
        Assert.Contains(narrative.Id, normalized.CompletedEventIds);
        Assert.Contains(rare.Id, normalized.CompletedEventIds);
        Assert.Collection(
            normalized.LastSeenDays,
            entry =>
            {
                Assert.Equal(rare.Id, entry.EventId);
                Assert.Equal(1, entry.Day);
            },
            entry =>
            {
                Assert.Equal(environment.Id, entry.EventId);
                Assert.Equal(40, entry.Day);
            }
        );
    }

    [Fact]
    public void FestivalReplayRulesRotateFromYearTwoAndOldSavesRemainReadable()
    {
        foreach (var festivalId in FestivalCatalog.Festivals.Keys)
        {
            Assert.Equal(
                FestivalCatalog.ClassicRuleId,
                FestivalCatalog.ReplayRuleFor(festivalId, 1).Id
            );
            Assert.Equal(
                FestivalCatalog.SeasonalFocusRuleId,
                FestivalCatalog.ReplayRuleFor(festivalId, 2).Id
            );
            Assert.Equal(
                FestivalCatalog.CraftFocusRuleId,
                FestivalCatalog.ReplayRuleFor(festivalId, 3).Id
            );
            Assert.Equal(2, FestivalCatalog.RewardChoicesFor(festivalId).Count);
        }
        Assert.Equal(
            4,
            FestivalCatalog.ReplayScoreBonus(
                FestivalCatalog.StarharvestMarketFestivalId,
                2,
                [
                    DataCatalog.AuricShootId,
                    DataCatalog.SunvaultGourdId,
                    DataCatalog.CrownstarSaffronId
                ]
            )
        );
        Assert.Equal(
            4,
            FestivalCatalog.ReplayScoreBonus(
                FestivalCatalog.StarharvestMarketFestivalId,
                3,
                [
                    DataCatalog.AuricShootId,
                    DataCatalog.CloudleafTeaId,
                    DataCatalog.CrownstarSaffronId
                ]
            )
        );

        var normalized = FestivalSystem.NormalizeSave(new FestivalSave
        {
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 2,
                    ItemIds =
                    [
                        DataCatalog.AuricShootId,
                        DataCatalog.SunvaultGourdId,
                        DataCatalog.CrownstarSaffronId
                    ],
                    Score = 29,
                    AwardId = FestivalCatalog.GoldenCrownAwardId,
                    AuctionCoins = 500,
                    RewardChoiceId = FestivalCatalog.StarharvestSeedRewardId,
                    RewardClaimed = true
                }
            ]
        });

        var result = Assert.Single(normalized.Results);
        Assert.Equal(2, result.Year);
        Assert.Equal(FestivalCatalog.ClassicRuleId, result.RuleVariantId);
        Assert.Equal(FestivalCatalog.StarharvestSeedRewardId,
            result.RewardChoiceId);
        Assert.True(result.RewardClaimed);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(FestivalSystem.NormalizeSave(normalized))
        );
    }

    [Fact]
    public void FestivalReplayRewardChoiceIsAtomicAndSingleClaim()
    {
        var festival = new FestivalSystem();
        festival.Restore(new FestivalSave
        {
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 2,
                    Score = 26,
                    AwardId = FestivalCatalog.GoldenCrownAwardId,
                    RewardChoiceId = "unknown_reward_choice",
                    RewardClaimed = true
                }
            ]
        });
        var inventory = FullInventoryExcept(
            FestivalCatalog.RewardChoices[
                FestivalCatalog.StarharvestSeedRewardId
            ].ItemId
        );
        var beforeBlocked = Snapshot(festival, inventory);

        var blocked = festival.ClaimRewardChoice(
            FestivalCatalog.StarharvestMarketFestivalId,
            2,
            FestivalCatalog.StarharvestSeedRewardId,
            inventory
        );

        Assert.False(blocked.Succeeded);
        Assert.Equal("notice.inventory_full", blocked.MessageKey);
        Assert.Equal(beforeBlocked, Snapshot(festival, inventory));

        inventory = new Inventory();
        inventory.Reset();
        var claimed = festival.ClaimRewardChoice(
            FestivalCatalog.StarharvestMarketFestivalId,
            2,
            FestivalCatalog.StarharvestSeedRewardId,
            inventory
        );

        Assert.True(claimed.Succeeded);
        Assert.Equal(
            4,
            inventory.Count(DataCatalog.AuricShootSeedId)
        );
        var result = Assert.Single(festival.Capture().Results);
        Assert.True(result.RewardClaimed);
        Assert.Equal(FestivalCatalog.StarharvestSeedRewardId,
            result.RewardChoiceId);

        var duplicate = festival.ClaimRewardChoice(
            FestivalCatalog.StarharvestMarketFestivalId,
            2,
            FestivalCatalog.StarharvestSoilRewardId,
            inventory
        );

        Assert.False(duplicate.Succeeded);
        Assert.Equal(
            "festival.replay.reward.already_claimed",
            duplicate.MessageKey
        );
        Assert.Equal(0, inventory.Count(DataCatalog.StarsoilFertilizerId));
    }

    [Fact]
    public void PostgameObjectivesExposeFourLoopsAndClampProgressPerYear()
    {
        var session = new GameSession();
        session.Restore(new GameSaveV1
        {
            Day = 70,
            Construction = new ConstructionSave
            {
                Projects =
                [
                    new ConstructionProjectSave
                    {
                        ProjectId =
                            ConstructionCatalog.SixfoldStarGateProjectId,
                        Completed = true
                    }
                ]
            },
            StarGate = new StarGateSave { Activated = true },
            StellarResonance = new StellarResonanceSave
            {
                MainStoryCompleted = true,
                CompletionDay = 56
            },
            Festival = new FestivalSave
            {
                Results =
                [
                    Result(FestivalCatalog.StarharvestMarketFestivalId, 1),
                    Result(FestivalCatalog.StarharvestMarketFestivalId, 2),
                    Result(FestivalCatalog.GleamrisePlantingFestivalId, 2),
                    Result(FestivalCatalog.GleamrisePlantingFestivalId, 2)
                ]
            },
            RegionalEvents = new RegionalEventSave
            {
                CompletedEventIds =
                [
                    RegionalEventCatalog.WoodsRareEventId,
                    RegionalEventCatalog.WoodsRareEventId,
                    RegionalEventCatalog.RuinsRareEventId
                ]
            },
            Village = new VillageSave
            {
                MetNpcIds =
                [
                    VillageCatalog.LioraId,
                    VillageCatalog.TaviId,
                    VillageCatalog.NemiId
                ],
                Relationships =
                [
                    Relationship(VillageCatalog.LioraId, 57),
                    Relationship(VillageCatalog.TaviId, 70),
                    Relationship(VillageCatalog.NemiId, 12)
                ]
            },
            Collection = new CollectionSave
            {
                DiscoveredEntryIds = CompendiumCatalog.EntriesInOrder
                    .Take(5)
                    .Select(entry => entry.Id)
                    .ToList()
            }
        });

        var objectives = session.PostgameObjectives();

        Assert.Equal(
            [
                PostgameObjectiveKind.AnnualChallenge,
                PostgameObjectiveKind.RareEvent,
                PostgameObjectiveKind.RelationshipRevisit,
                PostgameObjectiveKind.CollectionCompletion
            ],
            objectives.Select(objective => objective.Kind)
        );
        AssertObjective(
            objectives,
            PostgameObjectiveCatalog.AnnualChallengeId,
            progress: 2,
            target: FestivalCatalog.Festivals.Count
        );
        AssertObjective(
            objectives,
            PostgameObjectiveCatalog.RareEventId,
            progress: 2,
            target: RegionalEventCatalog.RareEventIds.Count
        );
        AssertObjective(
            objectives,
            PostgameObjectiveCatalog.RelationshipRevisitId,
            progress: 2,
            target: PostgameObjectiveCatalog.RelationshipRevisitTarget
        );
        AssertObjective(
            objectives,
            PostgameObjectiveCatalog.CollectionCompletionId,
            progress: 5,
            target: CompendiumCatalog.Entries.Count
        );
    }

    [Fact]
    public void PostgameMilestonesApplyOnceAndPreserveMaximumExperience()
    {
        var resonance = new StellarResonanceSystem();
        resonance.Restore(
            new StellarResonanceSave
            {
                MainStoryCompleted = true,
                CompletionDay = 56,
                Experience =
                    StellarResonanceCatalog.RankThresholds[^1] - 5,
                CompletedMilestoneIds =
                [
                    "postgame.annual.festival_starharvest_market.2",
                    "postgame.annual.festival_starharvest_market.2",
                    ""
                ]
            },
            starGateActivated: true,
            currentDay: 70
        );

        Assert.Equal(
            ["postgame.annual.festival_starharvest_market.2"],
            resonance.CompletedMilestoneIds
        );
        Assert.Equal(
            0,
            resonance.RecordPostgameMilestone(
                "postgame.annual.festival_starharvest_market.2",
                24
            )
        );
        Assert.Equal(
            5,
            resonance.RecordPostgameMilestone(
                "postgame.rare.regional_event_postgame_woods_echo_grove.2",
                30
            )
        );
        Assert.Equal(StellarResonanceCatalog.RankThresholds[^1],
            resonance.Experience);
        Assert.Equal(
            0,
            resonance.RecordPostgameActivity(StellarSkillKind.Farming)
        );
        Assert.Equal(
            2,
            resonance.CompletedMilestoneIds.Count
        );
    }

    private static FestivalYearResultSave Result(string festivalId, int year)
        => new()
        {
            FestivalId = festivalId,
            Year = year,
            Score = 30,
            AwardId = FestivalCatalog.AwardForScore(festivalId, 30).Id
        };

    private static VillageRelationshipSave Relationship(
        string npcId,
        int lastTalkDay
    ) => new()
    {
        NpcId = npcId,
        Points = 40,
        LastTalkDay = lastTalkDay
    };

    private static Inventory FullInventoryExcept(string excludedItemId)
    {
        var inventory = new Inventory();
        inventory.Reset();
        var fillers = DataCatalog.Items.Values
            .Where(item => item.Kind != ItemKind.Tool)
            .Where(item => item.Id != excludedItemId)
            .DistinctBy(item => item.Id, StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .ToArray();
        Assert.Equal(
            Inventory.SlotCount - Inventory.StartingToolCount,
            fillers.Length
        );
        foreach (var filler in fillers)
        {
            Assert.True(inventory.Add(filler.Id, filler.MaxStack));
        }

        Assert.False(inventory.CanAdd(excludedItemId, 1));
        return inventory;
    }

    private static string Snapshot(
        FestivalSystem festival,
        Inventory inventory
    ) => JsonSerializer.Serialize(festival.Capture()) +
        JsonSerializer.Serialize(inventory.Capture());

    private static void AssertObjective(
        IReadOnlyList<PostgameObjectiveSnapshot> objectives,
        string objectiveId,
        int progress,
        int target
    )
    {
        var objective = objectives.Single(candidate =>
            candidate.Id == objectiveId
        );
        Assert.Equal(progress, objective.Progress);
        Assert.Equal(target, objective.Target);
    }
}
