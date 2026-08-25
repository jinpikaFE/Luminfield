using System.Text.Json;
using System.Text.Json.Nodes;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class Story01NarrativeTests
{
    public static TheoryData<
        string,
        GridPosition,
        WorldBiome,
        string,
        string,
        string,
        string
    > StarlightChains => new()
    {
        {
            DataCatalog.WoodlandStarlightId,
            WorldDefinition.WoodlandStarlightCell,
            WorldBiome.WhisperingWoods,
            StarlightStoryCatalog.WoodlandDiscoveryId,
            StarlightStoryCatalog.WoodlandRestorationId,
            StarlightStoryCatalog.WoodlandResponseId,
            StarlightStoryCatalog.WoodlandRevisitId
        },
        {
            DataCatalog.HomesteadStarlightId,
            FarmLayout.HomesteadStarlightCell,
            WorldBiome.Home,
            StarlightStoryCatalog.HomesteadDiscoveryId,
            StarlightStoryCatalog.HomesteadRestorationId,
            StarlightStoryCatalog.HomesteadResponseId,
            StarlightStoryCatalog.HomesteadRevisitId
        },
        {
            DataCatalog.MeadowStarlightId,
            WorldDefinition.MeadowStarlightCell,
            WorldBiome.StarfallMeadow,
            StarlightStoryCatalog.MeadowDiscoveryId,
            StarlightStoryCatalog.MeadowRestorationId,
            StarlightStoryCatalog.MeadowResponseId,
            StarlightStoryCatalog.MeadowRevisitId
        },
        {
            DataCatalog.MoonwaterStarlightId,
            WorldDefinition.MoonwaterStarlightCell,
            WorldBiome.MoonwaterWetlands,
            StarlightStoryCatalog.MoonwaterDiscoveryId,
            StarlightStoryCatalog.MoonwaterRestorationId,
            StarlightStoryCatalog.MoonwaterResponseId,
            StarlightStoryCatalog.MoonwaterRevisitId
        },
        {
            DataCatalog.CrystalValeStarlightId,
            WorldDefinition.CrystalWellCell,
            WorldBiome.CrystalVale,
            StarlightStoryCatalog.CrystalValeDiscoveryId,
            StarlightStoryCatalog.CrystalValeRestorationId,
            StarlightStoryCatalog.CrystalValeResponseId,
            StarlightStoryCatalog.CrystalValeRevisitId
        },
        {
            DataCatalog.StarfallRuinsStarlightId,
            WorldDefinition.StarfallRuinsStarlightCell,
            WorldBiome.StarfallRuins,
            StarlightStoryCatalog.StarfallRuinsDiscoveryId,
            StarlightStoryCatalog.StarfallRuinsRestorationId,
            StarlightStoryCatalog.StarfallRuinsResponseId,
            StarlightStoryCatalog.StarfallRuinsRevisitId
        }
    };

    [Fact]
    public void StoryCatalogHasSixStableOrderedFourBeatChains()
    {
        var beats = StarlightStoryCatalog.Beats;

        Assert.Equal(24, beats.Count);
        Assert.Empty(StarlightStoryCatalog.ValidationErrors);
        Assert.All(beats, beat =>
        {
            Assert.NotEmpty(beat.DialogueKeys);
            Assert.True(PlayerLocationIds.IsValid(
                Assert.IsType<string>(beat.RequiredLocationId)
            ));
        });
        Assert.Equal(
            beats.Count,
            beats.Select(beat => beat.Id).Distinct(StringComparer.Ordinal).Count()
        );
        foreach (var pedestalId in DataCatalog.StarlightPedestals.Keys)
        {
            var chain = StarlightStoryCatalog.ForPedestal(pedestalId);
            Assert.Equal(
                Enum.GetValues<StarlightStoryBeatKind>(),
                chain.Select(beat => beat.Kind)
            );
            Assert.Equal([3, 3, 2, 3], chain.Select(beat =>
                beat.DialogueKeys.Count
            ));
            Assert.Empty(chain[0].PrerequisiteBeatIds);
            Assert.Equal([chain[0].Id], chain[1].PrerequisiteBeatIds);
            Assert.Equal([chain[1].Id], chain[2].PrerequisiteBeatIds);
            Assert.Equal([chain[2].Id], chain[3].PrerequisiteBeatIds);
            Assert.Equal(1, chain[2].MinimumDaysAfterPrerequisites);
            Assert.NotNull(chain[2].RequiredWorldCell);
            Assert.Equal(
                chain[2].RequiredBiome,
                WorldDefinition.GetBiome(chain[2].RequiredWorldCell!.Value)
            );
        }
        Assert.Equal(
            18,
            beats.Where(beat =>
                    beat.Kind != StarlightStoryBeatKind.MainStoryRevisit
                )
                .Select(beat => beat.StatusKey)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
    }

    [Fact]
    public void RegionResponsesUseExactWorldAnchorsAndTwoCellRadius()
    {
        var expected = new Dictionary<string, (WorldBiome Biome, GridPosition Cell)>(
            StringComparer.Ordinal
        )
        {
            [StarlightStoryCatalog.WoodlandResponseId] = (
                WorldBiome.WhisperingWoods,
                WorldDefinition.WoodlandStarlightCell
            ),
            [StarlightStoryCatalog.HomesteadResponseId] = (
                WorldBiome.Home,
                FarmLayout.HomesteadStoryResponseCell
            ),
            [StarlightStoryCatalog.MeadowResponseId] = (
                WorldBiome.StarfallMeadow,
                WorldDefinition.MeadowStarlightCell
            ),
            [StarlightStoryCatalog.MoonwaterResponseId] = (
                WorldBiome.MoonwaterWetlands,
                WorldDefinition.MoonwaterStarlightCell
            ),
            [StarlightStoryCatalog.CrystalValeResponseId] = (
                WorldBiome.StarfallRuins,
                StarfallRuinsTrialLayout.WorldEntryCell
            ),
            [StarlightStoryCatalog.StarfallRuinsResponseId] = (
                WorldBiome.LumenVillage,
                FarmLayout.StarGateCell
            )
        };

        Assert.Equal(6, expected.Count);
        foreach (var (beatId, response) in expected)
        {
            var beat = StarlightStoryCatalog.ById[beatId];
            Assert.Equal(response.Biome, beat.RequiredBiome);
            Assert.Equal(response.Cell, beat.RequiredWorldCell);
            Assert.Equal(2, beat.RequiredWorldRadius);
        }
    }

    [Fact]
    public void EveryStoryAndRecapKeyExistsInBothLocales()
    {
        using var zh = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "localization",
            "zh_CN.json"
        )));
        using var en = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "localization",
            "en.json"
        )));
        var keys = StarlightStoryCatalog.Beats.SelectMany(beat =>
                beat.DialogueKeys.Append(beat.SpeakerKey)
                    .Append(beat.StatusKey)
            )
            .Concat(
            [
                "story01.recap.title",
                "story01.recap.subtitle",
                "story01.recap.lights",
                "story01.recap.relationships",
                "story01.recap.highest",
                "story01.recap.highest.none",
                "story01.recap.exploration",
                "story01.recap.events",
                "story01.recap.light.recorded",
                "story01.recap.light.legacy",
                "story01.recap.light.unrestored",
                "story01.recap.companions",
                "story01.recap.companion.entry",
                "story01.recap.companions.none",
                "story01.recap.lights.none",
                "story01.recap.list.separator",
                "story01.recap.confirm",
                "story01.recap.back"
            ])
            .Distinct(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            Assert.True(zh.RootElement.TryGetProperty(key, out _), key);
            Assert.True(en.RootElement.TryGetProperty(key, out _), key);
        }
        Assert.Equal(
            "最终汇聚前，星图只读呈现你的真实旅程",
            zh.RootElement.GetProperty("story01.recap.subtitle").GetString()
        );
        Assert.Equal(
            "确认最终汇聚",
            zh.RootElement.GetProperty("story01.recap.confirm").GetString()
        );
        Assert.Equal(
            "Before final convergence, the chart presents your real journey without changing it",
            en.RootElement.GetProperty("story01.recap.subtitle").GetString()
        );
        Assert.Equal(
            "Begin Final Convergence",
            en.RootElement.GetProperty("story01.recap.confirm").GetString()
        );
    }

    [Fact]
    public void DiscoveryRequiresRealAdjacentHandActionAndCompletesOnLastPage()
    {
        var session = NewAdjacentWoodlandSession();
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.False(session.UseSelected(pedestal).Succeeded);
        Assert.Null(session.BeginStarlightDiscoveryStory(
            DataCatalog.WoodlandStarlightId
        ));
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        session.Inventory.Select(0);
        Assert.True(session.UseSelected(pedestal).Succeeded);
        var story = session.BeginStarlightDiscoveryStory(
            DataCatalog.WoodlandStarlightId
        );

        Assert.NotNull(story);
        Assert.Equal(
            StarlightStoryCatalog.WoodlandDiscoveryId,
            story.BeatId
        );
        Assert.Empty(session.Capture().StarlightStory.Entries);
        Assert.True(session.CompleteStarlightStoryBeat(story.BeatId).Succeeded);
        Assert.True(session.StarlightStory.IsCompleted(story.BeatId));
        Assert.Null(session.BeginStarlightDiscoveryStory(
            DataCatalog.WoodlandStarlightId
        ));
    }

    [Theory]
    [MemberData(nameof(StarlightChains))]
    public void EveryPedestalUsesRealAdjacentHandBeforeItsDiscovery(
        string pedestalId,
        GridPosition pedestalCell,
        WorldBiome _,
        string discoveryId,
        string restorationId,
        string responseId,
        string revisitId
    )
    {
        Assert.Equal(
            [discoveryId, restorationId, responseId, revisitId],
            StarlightStoryCatalog.ForPedestal(pedestalId)
                .Select(beat => beat.Id)
        );
        var session = NewAdjacentPedestalSession(pedestalCell);
        session.Inventory.Select(0);
        session.SetPlayerLocation(
            (pedestalCell.X + 4) * 16 + 8,
            pedestalCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        var outOfReachBefore = JsonSerializer.Serialize(session.Capture());
        Assert.False(session.UseSelected(pedestalCell).Succeeded);
        Assert.Null(session.BeginNextPedestalStory(pedestalId));
        Assert.Equal(
            outOfReachBefore,
            JsonSerializer.Serialize(session.Capture())
        );
        session.SetPlayerLocation(
            pedestalCell.X * 16 + 8,
            (pedestalCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());

        Assert.False(session.UseSelected(pedestalCell).Succeeded);
        Assert.Null(session.BeginNextPedestalStory(pedestalId));
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        session.Inventory.Select(0);
        Assert.True(session.UseSelected(pedestalCell).Succeeded);
        var story = Assert.IsType<StarlightStoryDialogue>(
            session.BeginNextPedestalStory(pedestalId)
        );
        Assert.Equal(discoveryId, story.BeatId);
        Assert.True(session.CompleteStarlightStoryBeat(story.BeatId).Succeeded);
    }

    [Fact]
    public void InterruptedBeatDoesNotPersistAndCanRestartAfterRestore()
    {
        var session = NewAdjacentWoodlandSession();
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        Assert.True(session.UseSelected(pedestal).Succeeded);
        var story = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightDiscoveryStory(
                DataCatalog.WoodlandStarlightId
            )
        );

        var captured = session.Capture();
        Assert.Empty(captured.StarlightStory.Entries);

        var restored = new GameSession();
        restored.Restore(captured);
        Assert.Null(restored.StarlightStory.ActiveBeatId);
        Assert.NotNull(restored.BeginStarlightDiscoveryStory(
            DataCatalog.WoodlandStarlightId
        ));
        Assert.Equal(
            story.BeatId,
            restored.StarlightStory.ActiveBeatId
        );
    }

    [Fact]
    public void CompletingAnotherFinishedBeatDoesNotCancelTheActiveStory()
    {
        var story = new StarlightStorySystem();
        var context = StoryContext(
            currentDay: 2,
            discovered:
            [
                DataCatalog.WoodlandStarlightId,
                DataCatalog.HomesteadStarlightId
            ]
        );
        story.Restore(
            StorySave((StarlightStoryCatalog.WoodlandDiscoveryId, 1)),
            currentDay: 2,
            context
        );
        var active = Assert.IsType<StarlightStoryDialogue>(story.TryBegin(
            StarlightStoryCatalog.HomesteadDiscoveryId,
            context
        ));
        var before = JsonSerializer.Serialize(story.Capture());

        var result = story.Complete(
            StarlightStoryCatalog.WoodlandDiscoveryId,
            currentDay: 2
        );

        Assert.False(result.Succeeded);
        Assert.Equal(
            "story01.starlight.beat_already_complete",
            result.MessageKey
        );
        Assert.Equal(active.BeatId, story.ActiveBeatId);
        Assert.Equal(before, JsonSerializer.Serialize(story.Capture()));
    }

    [Fact]
    public void WoodlandStoryCompletesDiscoveryRestorationResponseAndRevisit()
    {
        var session = NewAdjacentWoodlandSession();
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        Assert.True(session.UseSelected(pedestal).Succeeded);
        CompleteActive(session, Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightDiscoveryStory(
                DataCatalog.WoodlandStarlightId
            )
        ));
        AddWoodlandOfferings(session);

        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.WoodlandStarlightId,
            DataCatalog.WoodlandHarvestNodeId
        ).Succeeded);
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.WoodlandStarlightId,
            DataCatalog.WoodlandMaterialsNodeId
        ).Succeeded);
        Assert.Null(session.BeginStarlightRestorationStory(
            DataCatalog.WoodlandStarlightId
        ));
        var finalContribution = session.ContributeToStarlightNode(
            DataCatalog.WoodlandStarlightId,
            DataCatalog.WoodlandCraftNodeId
        );

        Assert.True(finalContribution.Succeeded);
        Assert.True(finalContribution.Activated);
        Assert.True(session.Starlight.WoodlandRenewalUnlocked);
        var restoration = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightRestorationStory(
                DataCatalog.WoodlandStarlightId
            )
        );
        CompleteActive(session, restoration);

        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            pedestal.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.Null(session.BeginStarlightRegionResponse(
            WorldBiome.WhisperingWoods
        ));

        var nextDay = session.Capture();
        nextDay.Day++;
        nextDay.MinuteOfDay = 10 * 60;
        session.Restore(nextDay);
        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            pedestal.Y * 16 + 8,
            PlayerLocationIds.World
        );
        var response = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightRegionResponse(
                WorldBiome.WhisperingWoods
            )
        );
        CompleteActive(session, response);

        var archiveSave = session.Capture();
        archiveSave.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        archiveSave.Village.MetNpcIds.Add(VillageCatalog.LioraId);
        session.Restore(archiveSave);
        var liora = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        ));
        Assert.Equal(PlayerLocationIds.MoonlitArchive, liora.LocationId);
        NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        session.Inventory.Select(0);
        var unrelatedBefore = UnrelatedWorldState(session);

        var conversation = Assert.IsType<VillageConversation>(
            session.InteractWithVillager(liora.Position, out var result)
        );
        Assert.True(result.Succeeded);
        var revisit = Assert.IsType<StarlightStoryDialogue>(
            conversation.StarlightStory
        );
        CompleteActive(session, revisit);

        Assert.True(
            session.Village.Relationship(VillageCatalog.LioraId).Points > 0
        );
        Assert.Equal(unrelatedBefore, UnrelatedWorldState(session));
        Assert.Equal(
            4,
            session.Capture().StarlightStory.Entries.Count
        );
    }

    [Theory]
    [MemberData(nameof(StarlightChains))]
    public void EveryRestoredLightRespondsNextDayAndRevisitsThroughLiora(
        string pedestalId,
        GridPosition pedestalCell,
        WorldBiome catalogBiome,
        string discoveryId,
        string restorationId,
        string responseId,
        string revisitId
    )
    {
        Assert.Equal(
            pedestalCell,
            StarlightSpatialCatalog.ForPedestal(pedestalId).Cell
        );
        Assert.Equal(
            pedestalId switch
            {
                DataCatalog.WoodlandStarlightId =>
                    WorldBiome.WhisperingWoods,
                DataCatalog.HomesteadStarlightId => WorldBiome.Home,
                DataCatalog.MeadowStarlightId => WorldBiome.StarfallMeadow,
                DataCatalog.MoonwaterStarlightId =>
                    WorldBiome.MoonwaterWetlands,
                DataCatalog.CrystalValeStarlightId => WorldBiome.CrystalVale,
                DataCatalog.StarfallRuinsStarlightId =>
                    WorldBiome.StarfallRuins,
                _ => throw new ArgumentOutOfRangeException(nameof(pedestalId))
            },
            catalogBiome
        );
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(session, pedestalId);
        var responseDefinition = StarlightStoryCatalog.ById[responseId];
        var responseCell = Assert.IsType<GridPosition>(
            responseDefinition.RequiredWorldCell
        );
        var responseBiome = Assert.IsType<WorldBiome>(
            responseDefinition.RequiredBiome
        );
        var withinRadiusCell = ResponseCellAtDistance(
            responseCell,
            responseBiome,
            2
        );
        var wrongCell = ResponseCellAtDistance(
            responseCell,
            responseBiome,
            3
        );
        Assert.Equal(responseBiome, WorldDefinition.GetBiome(withinRadiusCell));
        Assert.Equal(responseBiome, WorldDefinition.GetBiome(wrongCell));
        session.SetPlayerLocation(
            responseCell.X * 16 + 8,
            responseCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        session.StarlightStory.Restore(
            StorySave((discoveryId, 2), (restorationId, 2)),
            session.Clock.Day,
            StoryContext(session)
        );
        Assert.Null(session.BeginStarlightRegionResponse(responseBiome));
        session.SetPlayerLocation(
            wrongCell.X * 16 + 8,
            wrongCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        session.StarlightStory.Restore(
            StorySave((discoveryId, 1), (restorationId, 1)),
            session.Clock.Day,
            StoryContext(session)
        );
        Assert.Null(session.BeginStarlightRegionResponse(responseBiome));
        session.SetPlayerLocation(
            withinRadiusCell.X * 16 + 8,
            withinRadiusCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var response = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightRegionResponse(responseBiome)
        );
        Assert.Equal(responseId, response.BeatId);
        var nonStoryBefore = NonStoryState(session);
        CompleteActive(session, response);
        Assert.Equal(nonStoryBefore, NonStoryState(session));

        session.Village.Restore(new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId]
        });
        session.SetPlayerLocation(
            8,
            8,
            PlayerLocationIds.MoonlitArchive
        );
        var liora = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        ));
        NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        session.Inventory.Select(0);

        var conversation = Assert.IsType<VillageConversation>(
            session.InteractWithVillager(liora.Position, out var result)
        );
        Assert.True(result.Succeeded);
        Assert.Equal(
            revisitId,
            Assert.IsType<StarlightStoryDialogue>(
                conversation.StarlightStory
            ).BeatId
        );
    }

    [Fact]
    public void StorySaveFiltersUnknownDuplicatesAndOutOfOrderEntries()
    {
        var normalized = StarlightStorySystem.NormalizeSave(
            new StarlightStorySave
            {
                Entries =
                [
                    new StarlightStoryEntrySave
                    {
                        BeatId = StarlightStoryCatalog.WoodlandDiscoveryId,
                        CompletedDay = 4
                    },
                    new StarlightStoryEntrySave
                    {
                        BeatId = StarlightStoryCatalog.WoodlandDiscoveryId,
                        CompletedDay = 2
                    },
                    new StarlightStoryEntrySave
                    {
                        BeatId = StarlightStoryCatalog.WoodlandRestorationId,
                        CompletedDay = 1
                    },
                    new StarlightStoryEntrySave
                    {
                        BeatId = StarlightStoryCatalog.WoodlandResponseId,
                        CompletedDay = 5
                    },
                    new StarlightStoryEntrySave
                    {
                        BeatId = "removed_story_beat",
                        CompletedDay = 1
                    }
                ]
            },
            currentDay: 5
        );

        var discovery = Assert.Single(normalized.Entries);
        Assert.Equal(StarlightStoryCatalog.WoodlandDiscoveryId, discovery.BeatId);
        Assert.Equal(2, discovery.CompletedDay);
    }

    [Fact]
    public void StorySaveUsesDomainFactsAndEarliestValidDelayedDuplicate()
    {
        var save = StorySave(
            (StarlightStoryCatalog.WoodlandDiscoveryId, 1),
            (StarlightStoryCatalog.WoodlandRestorationId, 1),
            (StarlightStoryCatalog.WoodlandResponseId, 1),
            (StarlightStoryCatalog.WoodlandResponseId, 2),
            (StarlightStoryCatalog.WoodlandRevisitId, 2)
        );
        var discoveredOnly = StoryContext(
            currentDay: 2,
            discovered: [DataCatalog.WoodlandStarlightId]
        );

        var contradictory = StarlightStorySystem.NormalizeSave(
            save,
            2,
            discoveredOnly
        );
        Assert.Equal(
            [StarlightStoryCatalog.WoodlandDiscoveryId],
            contradictory.Entries.Select(entry => entry.BeatId)
        );

        var withoutRecordedMeeting = StarlightStorySystem.NormalizeSave(
            save,
            2,
            StoryContext(
                currentDay: 2,
                discovered: [DataCatalog.WoodlandStarlightId],
                restored: [DataCatalog.WoodlandStarlightId],
                exploredBiomes: [WorldBiome.WhisperingWoods]
            )
        );
        Assert.Equal(
            [
                StarlightStoryCatalog.WoodlandDiscoveryId,
                StarlightStoryCatalog.WoodlandRestorationId,
                StarlightStoryCatalog.WoodlandResponseId
            ],
            withoutRecordedMeeting.Entries.Select(entry => entry.BeatId)
        );

        var supported = StarlightStorySystem.NormalizeSave(
            save,
            2,
            StoryContext(
                currentDay: 2,
                discovered: [DataCatalog.WoodlandStarlightId],
                restored: [DataCatalog.WoodlandStarlightId],
                metNpcIds: [VillageCatalog.LioraId],
                exploredBiomes: [WorldBiome.WhisperingWoods]
            )
        );

        Assert.Equal(4, supported.Entries.Count);
        Assert.Equal(
            2,
            supported.Entries.Single(entry =>
                entry.BeatId == StarlightStoryCatalog.WoodlandResponseId
            ).CompletedDay
        );
    }

    [Fact]
    public void RuntimeRestorationEventFiresOnceAndRestoreDoesNotReplayIt()
    {
        var session = NewAdjacentWoodlandSession();
        var restoredIds = new List<string>();
        session.StarlightPedestalRestored += restoredIds.Add;
        Assert.True(session.UseSelected(
            WorldDefinition.WoodlandStarlightCell
        ).Succeeded);
        AddWoodlandOfferings(session);
        foreach (var node in DataCatalog.WoodlandStarlight.Nodes)
        {
            Assert.True(session.ContributeToStarlightNode(
                DataCatalog.WoodlandStarlightId,
                node.Id
            ).Succeeded);
        }

        Assert.Equal([DataCatalog.WoodlandStarlightId], restoredIds);
        Assert.False(session.Starlight.RefreshRewardUnlocks());
        Assert.Single(restoredIds);

        var restored = new GameSession();
        var replayed = new List<string>();
        restored.StarlightPedestalRestored += replayed.Add;
        restored.Restore(session.Capture());
        Assert.Empty(replayed);
    }

    [Fact]
    public void AutomaticAndManualRestorationPathsReportExactPedestalIds()
    {
        var meadowDefinition = DataCatalog.MeadowStarlight;
        var meadow = new StarlightSystem();
        meadow.Restore(
            new StarlightSave
            {
                Pedestals =
                [CompletedPedestalState(meadowDefinition, false)]
            },
            StarlightProgressContext.Empty
        );
        var restoredIds = new List<string>();
        meadow.PedestalRestored += restoredIds.Add;

        Assert.True(meadow.RefreshRewardUnlocks(
            FullProgressContext(meadowDefinition)
        ));
        Assert.Equal([DataCatalog.MeadowStarlightId], restoredIds);
        Assert.False(meadow.RefreshRewardUnlocks(
            FullProgressContext(meadowDefinition)
        ));

        var ruinsDefinition = DataCatalog.StarfallRuinsStarlight;
        var ruins = new StarlightSystem();
        var ruinsContext = FullProgressContext(ruinsDefinition);
        ruins.Restore(
            new StarlightSave
            {
                Pedestals =
                [CompletedPedestalState(ruinsDefinition, false)]
            },
            ruinsContext
        );
        ruins.PedestalRestored += restoredIds.Add;

        Assert.True(ruins.ActivateManually(
            DataCatalog.StarfallRuinsStarlightId,
            ruinsContext
        ).Succeeded);
        Assert.Equal(
            [
                DataCatalog.MeadowStarlightId,
                DataCatalog.StarfallRuinsStarlightId
            ],
            restoredIds
        );
    }

    [Fact]
    public void JourneyRecapIsPureProjectionOfLightsBondsExplorationAndEvents()
    {
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(session, DataCatalog.WoodlandStarlightId);
        session.Village.Restore(new VillageSave
        {
            MetNpcIds =
            [
                VillageCatalog.LioraId,
                VillageCatalog.TaviId,
                VillageCatalog.NemiId
            ],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 10
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.TaviId,
                    Points = 30
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.NemiId,
                    Points = 65
                }
            ]
        });
        session.Exploration.Discover(WorldDefinition.WoodlandStarlightCell);
        session.Exploration.Discover(WorldDefinition.MeadowStarlightCell);
        session.CharacterEvents.Restore(new CharacterEventSave
        {
            Entries =
            [
                new CharacterEventEntrySave
                {
                    EventId = CharacterEventCatalog.LioraFadedReturnRouteId,
                    CompletedDay = 1
                }
            ]
        }, session.Clock.Day);
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.WoodlandDiscoveryId, 1),
                (StarlightStoryCatalog.WoodlandRestorationId, 1)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        var before = JsonSerializer.Serialize(session.Capture());

        var recap = session.JourneyRecap();

        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
        Assert.Equal([DataCatalog.WoodlandStarlightId],
            recap.RestoredPedestalIds);
        Assert.Equal(3, recap.MetNpcCount);
        Assert.Equal(1, recap.NewAcquaintanceCount);
        Assert.Equal(2, recap.TrustedFriendCount);
        Assert.Equal(1, recap.KindredLightCount);
        Assert.Equal(VillageCatalog.NemiId, recap.HighestRelationshipNpcId);
        Assert.Equal(65, recap.HighestRelationshipPoints);
        Assert.Equal(3, recap.ExploredChunkCount);
        Assert.Equal(3, recap.ExploredRegionCount);
        Assert.Equal(1, recap.CompletedCharacterEventCount);
        Assert.Equal(2, recap.CompletedStarlightStoryBeatCount);
        Assert.Equal(24, recap.TotalStarlightStoryBeatCount);
        Assert.False(recap.MainStoryCompleted);
    }

    [Fact]
    public void LegacyRestoredLightStartsAtDiscoveryWithoutInventingAStoryDay()
    {
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(session, DataCatalog.WoodlandStarlightId);
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            (pedestal.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Inventory.Select(0);

        var legacyRecap = session.JourneyRecap();
        var woodland = Assert.Single(
            legacyRecap.Starlights,
            starlight =>
                starlight.PedestalId == DataCatalog.WoodlandStarlightId
        );
        Assert.True(woodland.Restored);
        Assert.Null(woodland.RestorationStoryDay);
        Assert.Empty(session.Capture().StarlightStory.Entries);

        var story = Assert.IsType<StarlightStoryDialogue>(
            session.BeginNextPedestalStory(DataCatalog.WoodlandStarlightId)
        );

        Assert.Equal(StarlightStoryCatalog.WoodlandDiscoveryId, story.BeatId);
        Assert.Empty(session.Capture().StarlightStory.Entries);
        Assert.Null(session.JourneyRecap().Starlights.Single(starlight =>
            starlight.PedestalId == DataCatalog.WoodlandStarlightId
        ).RestorationStoryDay);
    }

    [Fact]
    public void JourneyRecapHandlesEmptyProgressAndStableCompanionTies()
    {
        var emptySession = new GameSession();
        emptySession.NewGame();

        var empty = emptySession.JourneyRecap();

        Assert.Equal(6, empty.Starlights.Count);
        Assert.Empty(empty.RestoredPedestalIds);
        Assert.All(empty.Starlights, starlight =>
        {
            Assert.False(starlight.Restored);
            Assert.Null(starlight.RestorationStoryDay);
        });
        Assert.Equal(0, empty.MetNpcCount);
        Assert.Empty(empty.TopCompanions);
        Assert.Null(empty.HighestRelationshipNpcId);
        Assert.Equal(0, empty.HighestRelationshipPoints);
        Assert.Equal(0, empty.CompletedStarlightStoryBeatCount);

        var tiedNpcIds = VillageCatalog.Npcs.Values
            .OrderBy(npc => npc.ScheduleOrder)
            .Take(4)
            .Select(npc => npc.Id)
            .ToArray();
        var tiedSession = new GameSession();
        tiedSession.NewGame();
        tiedSession.Village.Restore(new VillageSave
        {
            MetNpcIds = tiedNpcIds.Reverse().ToList(),
            Relationships = tiedNpcIds.Reverse().Select(npcId =>
                new VillageRelationshipSave
                {
                    NpcId = npcId,
                    Points = VillageSystem.TrustedFriendThreshold
                }
            ).ToList()
        });

        var tied = tiedSession.JourneyRecap();

        Assert.Equal(tiedNpcIds.Take(3), tied.TopCompanions.Select(companion =>
            companion.NpcId
        ));
        Assert.Equal(4, tied.TrustedFriendCount);
        Assert.Equal(0, tied.KindredLightCount);
    }

    [Fact]
    public void MoonwaterResponseRequiresTheRealRewardAndLeavesFishingUntouched()
    {
        var session = NewDayTwoSession();
        var seededSave = session.Capture();
        var crabPotCell = FirstWaterSource();
        seededSave.Fishing = new FishingSave
        {
            CaughtFishIds = [DataCatalog.MoonwaterMinnowId],
            ClaimedRewardIds = [FishingSystem.FirstWatersRewardId],
            DonatedFishIds = [DataCatalog.MoonwaterMinnowId],
            RodTierId = FishingProgressionCatalog.MoonthreadRodTierId,
            OwnedBobberIds = [DataCatalog.StillwaterBobberId],
            EquippedBaitId = DataCatalog.MoonmoteBaitId,
            EquippedBobberId = DataCatalog.StillwaterBobberId,
            Experience = 140,
            Level = 3,
            SpecializationId =
                FishingProgressionCatalog.CurrentListenerSpecializationId,
            CrabPots =
            [
                new CrabPotSave
                {
                    X = crabPotCell.X,
                    Y = crabPotCell.Y,
                    BaitItemId = DataCatalog.GlowgrubBaitId,
                    CatchItemId = DataCatalog.MoonwaterMinnowId
                }
            ]
        };
        session.Restore(seededSave);
        var response = StarlightStoryCatalog.ById[
            StarlightStoryCatalog.MoonwaterResponseId
        ];
        var responseCell = Assert.IsType<GridPosition>(
            response.RequiredWorldCell
        );
        session.SetPlayerLocation(
            responseCell.X * 16 + 8,
            responseCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        session.Starlight.Restore(
            new StarlightSave
            {
                Pedestals =
                [
                    new StarlightPedestalSave
                    {
                        PedestalId = DataCatalog.MoonwaterStarlightId,
                        Discovered = true,
                        RewardUnlocked = false
                    }
                ]
            },
            StarlightProgressContext.Empty
        );
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.MoonwaterDiscoveryId, 1),
                (StarlightStoryCatalog.MoonwaterRestorationId, 1),
                (StarlightStoryCatalog.MoonwaterResponseId, 2)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        var before = NonStoryState(session);

        Assert.False(session.Starlight.MoonwaterTideUnlocked);
        Assert.Null(session.BeginStarlightRegionResponse(
            Assert.IsType<WorldBiome>(response.RequiredBiome)
        ));
        Assert.Equal(before, NonStoryState(session));
        Assert.False(session.Starlight.MoonwaterTideUnlocked);
        Assert.DoesNotContain(
            StarlightStoryCatalog.MoonwaterRestorationId,
            session.StarlightStory.CompletedDays.Keys
        );
    }

    [Fact]
    public void FinalLioraRevisitUsesCurrentJourneyAndNeedsNoPriorMeeting()
    {
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(
            session,
            DataCatalog.StarfallRuinsStarlightId
        );
        session.Exploration.Discover(FarmLayout.StarGateCell);
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.StarfallRuinsDiscoveryId, 1),
                (StarlightStoryCatalog.StarfallRuinsRestorationId, 1),
                (StarlightStoryCatalog.StarfallRuinsResponseId, 2)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        session.SetPlayerLocation(8, 8, PlayerLocationIds.MoonlitArchive);
        var liora = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        ));
        Assert.DoesNotContain(
            VillageCatalog.LioraId,
            session.Village.MetNpcIds
        );
        NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        session.Inventory.Select(0);

        var conversation = Assert.IsType<VillageConversation>(
            session.InteractWithVillager(liora.Position, out var result)
        );

        Assert.True(result.Succeeded);
        var story = Assert.IsType<StarlightStoryDialogue>(
            conversation.StarlightStory
        );
        Assert.Equal(
            StarlightStoryCatalog.StarfallRuinsRevisitId,
            story.BeatId
        );
        var arguments = Assert.IsAssignableFrom<
            IReadOnlyList<IReadOnlyList<object>>
        >(story.DialogueArguments);
        Assert.Equal(3, arguments.Count);
        Assert.Equal(1, Assert.IsType<int>(arguments[0][0]));
        Assert.Equal(1, Assert.IsType<int>(arguments[1][0]));
        Assert.True(Assert.IsType<int>(arguments[2][0]) > 0);
        Assert.Contains(VillageCatalog.LioraId, session.Village.MetNpcIds);
    }

    [Fact]
    public void FinalLioraRevisitRefreshesAndFormatsDifferentJourneysInBothLocales()
    {
        var smallerJourney = EligibleFinalRevisitSession();
        var smallerBefore = NonStoryState(smallerJourney);
        var smallerStory = BeginLioraRevisit(smallerJourney);
        Assert.Equal(smallerBefore, NonStoryState(smallerJourney));

        var largerJourney = EligibleFinalRevisitSession();
        largerJourney.Exploration.Discover(
            WorldDefinition.WoodlandStarlightCell
        );
        largerJourney.Village.Restore(new VillageSave
        {
            MetNpcIds =
            [
                VillageCatalog.LioraId,
                VillageCatalog.TaviId,
                VillageCatalog.NemiId
            ],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 10
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.TaviId,
                    Points = 30
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.NemiId,
                    Points = 65
                }
            ]
        });
        var largerStory = BeginLioraRevisit(largerJourney);

        Assert.NotEqual(
            JsonSerializer.Serialize(smallerStory.DialogueArguments),
            JsonSerializer.Serialize(largerStory.DialogueArguments)
        );
        foreach (var locale in new[]
                 {
                     LocaleService.SimplifiedChinese,
                     LocaleService.English
                 })
        {
            var smallerPages = FormatStoryPages(smallerStory, locale);
            var largerPages = FormatStoryPages(largerStory, locale);
            Assert.Equal(3, smallerPages.Count);
            Assert.Equal(3, largerPages.Count);
            Assert.NotEqual(
                JsonSerializer.Serialize(smallerPages),
                JsonSerializer.Serialize(largerPages)
            );
            Assert.All(smallerPages.Concat(largerPages), page =>
            {
                Assert.DoesNotContain("{0}", page, StringComparison.Ordinal);
                Assert.DoesNotContain("{1}", page, StringComparison.Ordinal);
                Assert.DoesNotContain("{2}", page, StringComparison.Ordinal);
                Assert.DoesNotContain("{3}", page, StringComparison.Ordinal);
                Assert.DoesNotContain("{4}", page, StringComparison.Ordinal);
            });
        }
    }

    [Fact]
    public void RevisitDoesNotStartForGiftsOtherNpcsOrLioraOutsideTheArchive()
    {
        var giftSession = EligibleWoodlandRevisitSession();
        Assert.True(giftSession.Inventory.Add(DataCatalog.StarbudId, 1));
        giftSession.Inventory.Select(giftSession.Inventory.Slots
            .Select((slot, index) => (slot, index))
            .Single(entry => entry.slot.ItemId == DataCatalog.StarbudId)
            .index);
        var liora = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            giftSession.Clock.Day,
            giftSession.Clock.MinuteOfDay
        ));
        NpcTestPositioning.PlacePlayerAdjacent(giftSession, liora);

        var giftConversation = Assert.IsType<VillageConversation>(
            giftSession.InteractWithVillager(liora.Position, out var giftResult)
        );

        Assert.True(giftResult.Succeeded);
        Assert.NotNull(giftConversation.GiftReaction);
        Assert.Null(giftConversation.StarlightStory);
        Assert.Null(giftSession.StarlightStory.ActiveBeatId);

        var otherNpcSession = EligibleWoodlandRevisitSession();
        var otherNpc = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            otherNpcSession.Clock.Day,
            otherNpcSession.Clock.MinuteOfDay
        ));
        otherNpcSession.SetPlayerLocation(
            otherNpc.Position.X * 16 + 8,
            otherNpc.Position.Y * 16 + 8,
            otherNpc.LocationId
        );
        otherNpc = NpcTestPositioning.PlacePlayerAdjacent(
            otherNpcSession,
            otherNpc
        );
        otherNpcSession.Inventory.Select(0);

        var otherConversation = Assert.IsType<VillageConversation>(
            otherNpcSession.InteractWithVillager(
                otherNpc.Position,
                out var otherResult
            )
        );

        Assert.True(otherResult.Succeeded);
        Assert.Null(otherConversation.StarlightStory);
        Assert.Null(otherNpcSession.StarlightStory.ActiveBeatId);

        var awaySession = EligibleWoodlandRevisitSession();
        var awayLiora = Enumerable.Range(2, 6)
            .SelectMany(day => Enumerable.Range(6, 16).Select(hour =>
                (day, minute: hour * 60, npc: VillageCatalog.CurrentNpc(
                    VillageCatalog.LioraId,
                    day,
                    hour * 60
                ))
            ))
            .First(entry => entry.npc is not null &&
                entry.npc.LocationId != PlayerLocationIds.MoonlitArchive);
        var awaySave = awaySession.Capture();
        awaySave.Day = awayLiora.day;
        awaySave.MinuteOfDay = awayLiora.minute;
        awaySave.Player.LocationId = awayLiora.npc!.LocationId;
        awaySave.Player.X = awayLiora.npc.Position.X * 16 + 8;
        awaySave.Player.Y = awayLiora.npc.Position.Y * 16 + 8;
        awaySession.Restore(awaySave);
        var currentAwayLiora = Assert.IsType<VillageNpcState>(
            VillageCatalog.CurrentNpc(
                VillageCatalog.LioraId,
                awaySession.Clock.Day,
                awaySession.Clock.MinuteOfDay
            )
        );
        currentAwayLiora = NpcTestPositioning.PlacePlayerAdjacent(
            awaySession,
            currentAwayLiora
        );
        awaySession.Inventory.Select(0);

        var awayConversation = Assert.IsType<VillageConversation>(
            awaySession.InteractWithVillager(
                currentAwayLiora.Position,
                out var awayResult
            )
        );

        Assert.True(awayResult.Succeeded);
        Assert.Null(awayConversation.StarlightStory);
        Assert.Null(awaySession.StarlightStory.ActiveBeatId);
    }

    [Fact]
    public void LegacyCompletedGameKeepsFinaleWithoutStoryEntries()
    {
        var session = CompletedFinaleSession();

        Assert.True(session.StarGate.Activated);
        Assert.Equal(7, session.StarGate.TravelCount);
        Assert.True(session.StellarResonance.MainStoryCompleted);
        Assert.Equal(30, session.StellarResonance.CompletionDay);
        Assert.Empty(session.Capture().StarlightStory.Entries);
        Assert.True(session.JourneyRecap().MainStoryCompleted);
    }

    [Fact]
    public void LegacySaveFileWithoutStoryFieldKeepsRestoredLightAndStartsDiscovery()
    {
        var original = NewDayTwoSession();
        var save = original.Capture();
        save.Starlight = new StarlightSave
        {
            Pedestals =
            [
                CompletedPedestalState(
                    DataCatalog.WoodlandStarlight,
                    rewardUnlocked: true
                )
            ]
        };
        var pedestal = WorldDefinition.WoodlandStarlightCell;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = pedestal.X * 16 + 8;
        save.Player.Y = (pedestal.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        var path = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-story01-legacy-light-{Guid.NewGuid():N}.json"
        );

        try
        {
            var service = new SaveService(path);
            service.Save(save);
            RemoveStoryField(path);

            var loaded = service.Load();

            Assert.Equal(SaveLoadStatus.Loaded, loaded.Status);
            var restored = new GameSession();
            restored.Restore(Assert.IsType<GameSaveV1>(loaded.Save));
            Assert.True(restored.Starlight.WoodlandRenewalUnlocked);
            Assert.Empty(restored.Capture().StarlightStory.Entries);
            Assert.Null(Assert.Single(
                restored.JourneyRecap().Starlights,
                starlight =>
                    starlight.PedestalId == DataCatalog.WoodlandStarlightId
            ).RestorationStoryDay);
            Assert.Equal(
                StarlightStoryCatalog.WoodlandDiscoveryId,
                Assert.IsType<StarlightStoryDialogue>(
                    restored.BeginNextPedestalStory(
                        DataCatalog.WoodlandStarlightId
                    )
                ).BeatId
            );
        }
        finally
        {
            DeleteSaveFamily(path);
        }
    }

    [Fact]
    public void LegacyCompletedSaveFileWithoutStoryFieldKeepsGateTravelAndFinale()
    {
        var save = CompletedFinaleSession().Capture();
        var path = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-story01-legacy-finale-{Guid.NewGuid():N}.json"
        );

        try
        {
            var service = new SaveService(path);
            service.Save(save);
            RemoveStoryField(path);

            var loaded = service.Load();

            Assert.Equal(SaveLoadStatus.Loaded, loaded.Status);
            var restored = new GameSession();
            restored.Restore(Assert.IsType<GameSaveV1>(loaded.Save));
            Assert.Empty(restored.Capture().StarlightStory.Entries);
            Assert.True(restored.Construction.IsCompletedFor(
                ConstructionCatalog.SixfoldStarGateProjectId
            ));
            Assert.True(restored.StarGate.Activated);
            Assert.Equal(StarGateCatalog.Destinations[0].Id,
                restored.StarGate.LastDestinationId);
            Assert.Equal(7, restored.StarGate.TravelCount);
            Assert.True(restored.StellarResonance.MainStoryCompleted);
            Assert.Equal(30, restored.StellarResonance.CompletionDay);
        }
        finally
        {
            DeleteSaveFamily(path);
        }
    }

    [Fact]
    public void RuinsResponsePreservesConstructionGateTravelAndFinaleState()
    {
        var session = CompletedFinaleSession();
        RestoreCompletedPedestal(
            session,
            DataCatalog.StarfallRuinsStarlightId
        );
        var response = StarlightStoryCatalog.ById[
            StarlightStoryCatalog.StarfallRuinsResponseId
        ];
        var responseCell = Assert.IsType<GridPosition>(
            response.RequiredWorldCell
        );
        session.SetPlayerLocation(
            responseCell.X * 16 + 8,
            responseCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.StarfallRuinsDiscoveryId, 1),
                (StarlightStoryCatalog.StarfallRuinsRestorationId, 1)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        var before = NonStoryState(session);

        var story = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightRegionResponse(
                Assert.IsType<WorldBiome>(response.RequiredBiome)
            )
        );
        CompleteActive(session, story);

        Assert.Equal(before, NonStoryState(session));
        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.SixfoldStarGateProjectId
        ));
        Assert.True(session.StarGate.Activated);
        Assert.Equal(7, session.StarGate.TravelCount);
        Assert.True(session.StellarResonance.MainStoryCompleted);
        Assert.Equal(30, session.StellarResonance.CompletionDay);
    }

    [Fact]
    public void CompletedStoryRoundTripsThroughSaveService()
    {
        var session = NewAdjacentWoodlandSession();
        Assert.True(session.UseSelected(
            WorldDefinition.WoodlandStarlightCell
        ).Succeeded);
        var discovery = Assert.IsType<StarlightStoryDialogue>(
            session.BeginStarlightDiscoveryStory(
                DataCatalog.WoodlandStarlightId
            )
        );
        CompleteActive(session, discovery);
        var path = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-story01-{Guid.NewGuid():N}.json"
        );

        try
        {
            var service = new SaveService(path);
            service.Save(session.Capture());
            var loaded = service.Load();

            Assert.Equal(SaveLoadStatus.Loaded, loaded.Status);
            var restored = new GameSession();
            restored.Restore(Assert.IsType<GameSaveV1>(loaded.Save));
            Assert.True(restored.StarlightStory.IsCompleted(
                StarlightStoryCatalog.WoodlandDiscoveryId
            ));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static GameSession NewAdjacentWoodlandSession()
    {
        return NewAdjacentPedestalSession(
            WorldDefinition.WoodlandStarlightCell
        );
    }

    private static GameSession NewAdjacentPedestalSession(
        GridPosition pedestal
    )
    {
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            (pedestal.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Inventory.Select(0);
        return session;
    }

    private static GameSession NewDayTwoSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 2;
        save.MinuteOfDay = 10 * 60;
        session.Restore(save);
        return session;
    }

    private static GameSession EligibleWoodlandRevisitSession()
    {
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(session, DataCatalog.WoodlandStarlightId);
        session.Exploration.Discover(WorldDefinition.WoodlandStarlightCell);
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.WoodlandDiscoveryId, 1),
                (StarlightStoryCatalog.WoodlandRestorationId, 1),
                (StarlightStoryCatalog.WoodlandResponseId, 2)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        session.SetPlayerLocation(8, 8, PlayerLocationIds.MoonlitArchive);
        return session;
    }

    private static GameSession EligibleFinalRevisitSession()
    {
        var session = NewDayTwoSession();
        RestoreCompletedPedestal(
            session,
            DataCatalog.StarfallRuinsStarlightId
        );
        session.Exploration.Discover(FarmLayout.StarGateCell);
        session.StarlightStory.Restore(
            StorySave(
                (StarlightStoryCatalog.StarfallRuinsDiscoveryId, 1),
                (StarlightStoryCatalog.StarfallRuinsRestorationId, 1),
                (StarlightStoryCatalog.StarfallRuinsResponseId, 2)
            ),
            session.Clock.Day,
            StoryContext(session)
        );
        session.SetPlayerLocation(8, 8, PlayerLocationIds.MoonlitArchive);
        return session;
    }

    private static StarlightStoryDialogue BeginLioraRevisit(
        GameSession session
    )
    {
        var liora = Assert.IsType<VillageNpcState>(VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            session.Clock.Day,
            session.Clock.MinuteOfDay
        ));
        NpcTestPositioning.PlacePlayerAdjacent(session, liora);
        session.Inventory.Select(0);
        var conversation = Assert.IsType<VillageConversation>(
            session.InteractWithVillager(liora.Position, out var result)
        );
        Assert.True(result.Succeeded);
        return Assert.IsType<StarlightStoryDialogue>(
            conversation.StarlightStory
        );
    }

    private static IReadOnlyList<string> FormatStoryPages(
        StarlightStoryDialogue story,
        string localeId
    )
    {
        var locale = new LocaleService();
        foreach (var id in new[]
                 {
                     LocaleService.SimplifiedChinese,
                     LocaleService.English
                 })
        {
            locale.LoadJson(
                id,
                File.ReadAllText(Path.Combine(
                    AppContext.BaseDirectory,
                    "localization",
                    $"{id}.json"
                ))
            );
        }
        locale.SetLocale(localeId);
        var arguments = Assert.IsAssignableFrom<
            IReadOnlyList<IReadOnlyList<object>>
        >(story.DialogueArguments);

        return story.DialogueKeys.Select((key, index) => locale.Tr(
            key,
            arguments[index].Select(argument =>
                argument is StarlightStoryLocalizedListArgument list
                    ? list.Keys.Count == 0
                        ? locale.Tr(list.EmptyKey)
                        : string.Join(
                            locale.Tr(list.SeparatorKey),
                            list.Keys.Select(key => locale.Tr(key))
                        )
                    : argument
            ).ToArray()
        )).ToArray();
    }

    private static GameSession CompletedFinaleSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 40;
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.SixfoldStarGateProjectId,
                Completed = true
            }
        ];
        save.StarGate = new StarGateSave
        {
            Activated = true,
            LastDestinationId = StarGateCatalog.Destinations[0].Id,
            TravelCount = 7
        };
        save.StellarResonance = new StellarResonanceSave
        {
            MainStoryCompleted = true,
            CompletionDay = 30
        };
        save.StarlightStory = new StarlightStorySave();
        session.Restore(save);
        return session;
    }

    private static void RemoveStoryField(string path)
    {
        var root = Assert.IsType<JsonObject>(
            JsonNode.Parse(File.ReadAllText(path))
        );
        Assert.True(root.Remove("starlightStory"));
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private static void DeleteSaveFamily(string path)
    {
        foreach (var candidate in new[]
                 {
                     path,
                     $"{path}.tmp",
                     $"{path}.bak.1",
                     $"{path}.bak.2",
                     $"{path}.bak.3"
                 })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }

    private static GridPosition FirstWaterSource()
    {
        for (var y = 0; y < WorldDefinition.Height; y++)
        {
            for (var x = 0; x < WorldDefinition.Width; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.IsWaterSource(cell))
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException("World has no water source.");
    }

    private static void RestoreCompletedPedestal(
        GameSession session,
        string pedestalId
    )
    {
        var definition = DataCatalog.StarlightPedestal(pedestalId);
        var state = CompletedPedestalState(definition, rewardUnlocked: true);
        session.Starlight.Restore(
            new StarlightSave { Pedestals = [state] },
            FullProgressContext(definition)
        );
        Assert.True(session.Starlight.IsRewardUnlocked(pedestalId));
    }

    private static StarlightPedestalSave CompletedPedestalState(
        StarlightPedestalDefinition definition,
        bool rewardUnlocked
    )
    {
        var state = new StarlightPedestalSave
        {
            PedestalId = definition.Id,
            Discovered = true,
            RewardUnlocked = rewardUnlocked
        };
        foreach (var node in definition.Nodes)
        {
            var nodeState = new StarlightNodeSave { NodeId = node.Id };
            if (node.SourceKind == StarlightNodeSourceKind.Inventory)
            {
                var remaining = node.RequiredCount;
                foreach (var option in node.Options)
                {
                    var count = Math.Min(option.MaximumCount, remaining);
                    if (count <= 0)
                    {
                        continue;
                    }
                    nodeState.Contributions.Add(new StarlightContributionSave
                    {
                        ItemId = option.ItemId,
                        Count = count
                    });
                    remaining -= count;
                }
            }
            state.Nodes.Add(nodeState);
        }

        return state;
    }

    private static StarlightProgressContext FullProgressContext(
        StarlightPedestalDefinition definition
    )
    {
        var festivalIds = definition.Nodes
            .Where(node =>
                node.SourceKind == StarlightNodeSourceKind.FestivalResults
            )
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal);
        var milestoneIds = definition.Nodes
            .Where(node => node.SourceKind == StarlightNodeSourceKind.Milestones)
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal);
        var completedPedestalIds = definition.Nodes
            .Where(node =>
                node.SourceKind == StarlightNodeSourceKind.PedestalRewards
            )
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal);
        return new StarlightProgressContext(
            festivalIds,
            milestoneIds,
            completedPedestalIds
        );
    }

    private static StarlightStorySave StorySave(
        params (string BeatId, int CompletedDay)[] entries
    ) => new()
    {
        Entries = entries.Select(entry => new StarlightStoryEntrySave
        {
            BeatId = entry.BeatId,
            CompletedDay = entry.CompletedDay
        }).ToList()
    };

    private static StarlightStoryProgressContext StoryContext(
        GameSession session
    ) => new(
        session.Clock.Day,
        session.PlayerLocationId,
        session.PlayerLocationId == PlayerLocationIds.World
            ? WorldDefinition.GetBiome(session.PlayerCell)
            : null,
        DataCatalog.StarlightPedestals.Keys
            .Where(session.Starlight.IsDiscovered)
            .ToHashSet(StringComparer.Ordinal),
        DataCatalog.StarlightPedestals.Keys
            .Where(session.Starlight.IsRewardUnlocked)
            .ToHashSet(StringComparer.Ordinal),
        session.Village.MetNpcIds.ToHashSet(StringComparer.Ordinal),
        StarlightStoryProgressContext.ExploredBiomesFrom(
            session.Exploration.DiscoveredChunks
        ),
        session.CharacterEvents.Capture().Entries
            .Select(entry => entry.EventId)
            .ToHashSet(StringComparer.Ordinal),
        session.StellarResonance.MainStoryCompleted,
        session.PlayerLocationId == PlayerLocationIds.World
            ? session.PlayerCell
            : null
    );

    private static StarlightStoryProgressContext StoryContext(
        int currentDay,
        IEnumerable<string>? discovered = null,
        IEnumerable<string>? restored = null,
        IEnumerable<string>? metNpcIds = null,
        IEnumerable<WorldBiome>? exploredBiomes = null
    ) => new(
        currentDay,
        PlayerLocationIds.World,
        null,
        (discovered ?? []).ToHashSet(StringComparer.Ordinal),
        (restored ?? []).ToHashSet(StringComparer.Ordinal),
        (metNpcIds ?? []).ToHashSet(StringComparer.Ordinal),
        (exploredBiomes ?? []).ToHashSet(),
        new HashSet<string>(StringComparer.Ordinal),
        false
    );

    private static void AddWoodlandOfferings(GameSession session)
    {
        foreach (var itemId in new[]
        {
            DataCatalog.StarbudId,
            DataCatalog.MoonrootId,
            DataCatalog.CloudleafId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.StarwovenChestId
        })
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 6));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 2));
    }

    private static GridPosition ResponseCellAtDistance(
        GridPosition anchor,
        WorldBiome biome,
        int distance
    )
    {
        for (var y = anchor.Y - distance; y <= anchor.Y + distance; y++)
        {
            for (var x = anchor.X - distance; x <= anchor.X + distance; x++)
            {
                var cell = new GridPosition(x, y);
                if (Math.Abs(cell.X - anchor.X) +
                        Math.Abs(cell.Y - anchor.Y) == distance &&
                    WorldDefinition.IsInBounds(cell) &&
                    WorldDefinition.GetBiome(cell) == biome)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            $"No {biome} cell exists at Manhattan distance {distance} from {anchor}."
        );
    }

    private static void CompleteActive(
        GameSession session,
        StarlightStoryDialogue story
    ) => Assert.True(session.CompleteStarlightStoryBeat(
        story.BeatId
    ).Succeeded);

    private static string UnrelatedWorldState(GameSession session) =>
        NonStoryState(session);

    private static string NonStoryState(GameSession session)
    {
        var save = session.Capture();
        return JsonSerializer.Serialize(new
        {
            save.Starlight,
            save.Inventory,
            save.Coins,
            save.Construction,
            save.StarGate,
            save.StellarResonance,
            save.Fishing,
            save.Exploration
        });
    }
}
