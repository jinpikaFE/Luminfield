using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class MeadowStarlightTests
{
    [Fact]
    public void CatalogAddsThirdPedestalWithUniqueNodesAndSafeMeadowSite()
    {
        Assert.Equal(
            [
                DataCatalog.WoodlandStarlightId,
                DataCatalog.HomesteadStarlightId,
                DataCatalog.MeadowStarlightId,
                DataCatalog.MoonwaterStarlightId,
                DataCatalog.CrystalValeStarlightId,
                DataCatalog.StarfallRuinsStarlightId
            ],
            DataCatalog.StarlightPedestals.Keys
        );
        Assert.Equal(
            19,
            DataCatalog.StarlightPedestals.Values
                .SelectMany(pedestal => pedestal.Nodes)
                .Select(node => node.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.Equal(
            WorldBiome.StarfallMeadow,
            WorldDefinition.GetBiome(WorldDefinition.MeadowStarlightCell)
        );
        Assert.True(WorldDefinition.IsBlocked(
            WorldDefinition.MeadowStarlightCell
        ));
        Assert.True(WorldDefinition.IsPath(new GridPosition(82, 21)));
        Assert.False(WorldDefinition.IsBlocked(new GridPosition(82, 25)));
        Assert.Equal(
            DataCatalog.MeadowStarlightId,
            StarlightSpatialCatalog.ForPedestal(
                DataCatalog.MeadowStarlightId
            ).PedestalId
        );

        for (var y = 20; y <= 24; y++)
        {
            for (var x = 80; x <= 84; x++)
            {
                var cell = new GridPosition(x, y);
                Assert.True(WorldDefinition.IsMeadowStarlightReservedCell(
                    cell
                ));
                Assert.Equal(-1, WorldDefinition.PropAtlasIndex(cell));
                Assert.Equal(
                    WorldResourceKind.None,
                    WorldDefinition.ResourceAt(cell)
                );
            }
        }
    }

    [Fact]
    public void LegacyTwoPedestalSaveAddsEmptyMeadowWithoutChangingOldStates()
    {
        var old = new StarlightSave
        {
            Pedestals =
            [
                new StarlightPedestalSave
                {
                    PedestalId = DataCatalog.WoodlandStarlightId,
                    Discovered = true,
                    Nodes =
                    [
                        Node(
                            DataCatalog.WoodlandHarvestNodeId,
                            DataCatalog.StarbudId
                        )
                    ]
                },
                new StarlightPedestalSave
                {
                    PedestalId = DataCatalog.HomesteadStarlightId,
                    Discovered = true
                }
            ]
        };

        var normalized = StarlightSystem.NormalizeSave(old);
        var meadow = normalized.Pedestals.Single(state =>
            state.PedestalId == DataCatalog.MeadowStarlightId
        );

        Assert.Equal(6, normalized.Pedestals.Count);
        Assert.True(normalized.Pedestals.Single(state =>
            state.PedestalId == DataCatalog.WoodlandStarlightId
        ).Discovered);
        Assert.True(normalized.Pedestals.Single(state =>
            state.PedestalId == DataCatalog.HomesteadStarlightId
        ).Discovered);
        Assert.False(meadow.Discovered);
        Assert.False(meadow.RewardUnlocked);
        Assert.Equal(3, meadow.Nodes.Count);
        Assert.All(meadow.Nodes, node => Assert.Empty(node.Contributions));
    }

    [Fact]
    public void FlowerAndBountyNodesAcceptDistinctQualityFamiliesAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        foreach (var itemId in new[]
        {
            DataCatalog.DawnlaceLuminousId,
            DataCatalog.EmberbellStarlightId,
            DataCatalog.DuskbellId,
            DataCatalog.StarhoneyId,
            DataCatalog.StarfeatherEggLuminousId,
            DataCatalog.MoonfleeceStarlightId,
            DataCatalog.DewhornMilkId
        })
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }

        var blooms = session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBloomsNodeId
        );
        var bounty = session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBountyNodeId
        );

        Assert.True(blooms.Succeeded);
        Assert.Equal(3, blooms.ContributedCount);
        Assert.True(bounty.Succeeded);
        Assert.Equal(4, bounty.ContributedCount);
        Assert.Equal(
            3,
            session.StarlightNodeProgress(
                DataCatalog.MeadowStarlightId,
                DataCatalog.MeadowBloomsNodeId
            )
        );
        Assert.Equal(
            4,
            session.StarlightNodeProgress(
                DataCatalog.MeadowStarlightId,
                DataCatalog.MeadowBountyNodeId
            )
        );
        Assert.False(session.Starlight.MeadowPollinationUnlocked);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.DawnlaceLuminousId));
        Assert.Equal(
            0,
            session.Inventory.Count(DataCatalog.StarfeatherEggLuminousId)
        );

        Assert.True(session.Inventory.Add(DataCatalog.DawnlaceId, 1));
        var before = JsonSerializer.Serialize(session.Capture());
        var repeated = session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBloomsNodeId
        );
        Assert.False(repeated.Succeeded);
        Assert.Equal(
            "starlight.node_already_complete",
            repeated.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void CompletedFestivalResultDerivesOneEchoWithoutConsumingAnything()
    {
        var session = new GameSession();
        session.NewGame();
        session.Festival.Restore(CompletedFestivalSave());
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 2));
        var inventoryBefore = session.Inventory.Count(DataCatalog.StarbudId);
        var festivalBefore = JsonSerializer.Serialize(
            session.Festival.Capture()
        );

        Assert.Equal(
            1,
            session.StarlightNodeProgress(
                DataCatalog.MeadowStarlightId,
                DataCatalog.MeadowCelebrationNodeId
            )
        );
        Assert.True(session.IsStarlightNodeComplete(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowCelebrationNodeId
        ));
        Assert.False(session.CanContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowCelebrationNodeId
        ));
        Assert.Equal(
            inventoryBefore,
            session.Inventory.Count(DataCatalog.StarbudId)
        );
        Assert.Equal(
            festivalBefore,
            JsonSerializer.Serialize(session.Festival.Capture())
        );
    }

    [Fact]
    public void AttemptAndUnknownFestivalDoNotSatisfyCelebrationNode()
    {
        var session = new GameSession();
        session.NewGame();
        session.Festival.Restore(new FestivalSave
        {
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId = "unknown_festival",
                    Year = 1
                }
            ],
            PlantingAttempts =
            [
                new FestivalPlantingAttemptSave
                {
                    FestivalId =
                        FestivalCatalog.GleamrisePlantingFestivalId,
                    Year = 1,
                    SelectedSeedItemIds =
                    [
                        DataCatalog.DawnlaceSeedId,
                        DataCatalog.GlimmerpodSeedId,
                        DataCatalog.MistsongMintSeedId
                    ]
                }
            ]
        });

        Assert.Equal(
            0,
            session.StarlightNodeProgress(
                DataCatalog.MeadowStarlightId,
                DataCatalog.MeadowCelebrationNodeId
            )
        );
        Assert.False(session.Starlight.MeadowPollinationUnlocked);
    }

    [Fact]
    public void FinalItemNodeActivatesOnlyWithFestivalEchoAndRoundTrips()
    {
        var session = CompletedMeadowSession();

        Assert.True(session.Starlight.MeadowPollinationUnlocked);
        Assert.False(session.Starlight.WoodlandRenewalUnlocked);
        Assert.False(session.Starlight.HomesteadIrrigationUnlocked);
        Assert.Equal(3, session.CompletedStarlightNodeCount(
            DataCatalog.MeadowStarlightId
        ));

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Starlight.MeadowPollinationUnlocked);
        Assert.Equal(
            1,
            restored.StarlightNodeProgress(
                DataCatalog.MeadowStarlightId,
                DataCatalog.MeadowCelebrationNodeId
            )
        );
        Assert.Equal(
            DataCatalog.WoodlandStarlightId,
            restored.Capture().Starlight.PedestalId
        );
    }

    [Fact]
    public void MeadowPreviewAndActionUseRealAdjacentTargetAndHand()
    {
        var session = new GameSession();
        session.NewGame();
        var pedestal = WorldDefinition.MeadowStarlightCell;
        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            (pedestal.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Inventory.Select(1);

        var wrongTool = session.PreviewSelectedTarget(pedestal);
        var failed = session.UseSelected(pedestal);

        Assert.Equal(TargetPreviewKind.StarlightPedestal, wrongTool.Kind);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(failed.Succeeded);
        Assert.False(session.Starlight.IsDiscovered(
            DataCatalog.MeadowStarlightId
        ));

        session.Inventory.Select(0);
        Assert.True(session.PreviewSelectedTarget(pedestal).IsAvailable);
        Assert.True(session.UseSelected(pedestal).Succeeded);
        Assert.True(session.Starlight.IsDiscovered(
            DataCatalog.MeadowStarlightId
        ));

        session.SetPlayerLocation(
            GameSession.NewGamePlayerX,
            GameSession.NewGamePlayerY,
            PlayerLocationIds.World
        );
        Assert.Equal(
            TargetPreviewState.Neutral,
            session.PreviewSelectedTarget(pedestal).State
        );
        Assert.False(session.UseSelected(pedestal).Succeeded);
    }

    [Theory]
    [InlineData(false, 5, 0)]
    [InlineData(true, 5, 1)]
    [InlineData(true, 6, 1)]
    [InlineData(true, 7, 0)]
    public void PollinationRewardUsesExactEffectiveRange(
        bool restored,
        int distance,
        int expectedProgress
    )
    {
        var session = restored
            ? CompletedMeadowSession()
            : NewSessionWithFestivalOnly();
        var hive = new GridPosition(27, 13);
        var tree = new GridPosition(27 - distance, 13);
        RestoreOrchard(session, tree, hive);

        Assert.Equal(
            restored ? 6 : 4,
            session.BeehivePollinationRange
        );
        Assert.Equal(
            expectedProgress > 0,
            session.Orchard.HasPollinationSource(
                hive,
                session.BeehivePollinationRange
            )
        );
        session.EndDay();
        Assert.Equal(
            expectedProgress,
            session.Orchard.BeehiveAt(hive)!.ProgressNights
        );

        if (expectedProgress == 1)
        {
            session.EndDay();
            Assert.Equal(1, session.Orchard.BeehiveAt(hive)!.PendingHoney);
        }
    }

    private static GameSession CompletedMeadowSession()
    {
        var session = NewSessionWithFestivalOnly();
        foreach (var itemId in new[]
        {
            DataCatalog.DawnlaceId,
            DataCatalog.EmberbellId,
            DataCatalog.DuskbellId,
            DataCatalog.StarhoneyId,
            DataCatalog.StarfeatherEggId,
            DataCatalog.MoonfleeceId,
            DataCatalog.DewhornMilkId
        })
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }

        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBloomsNodeId
        ).Succeeded);
        var final = session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBountyNodeId
        );
        Assert.True(final.Succeeded);
        Assert.True(final.Activated);
        return session;
    }

    private static GameSession NewSessionWithFestivalOnly()
    {
        var session = new GameSession();
        session.NewGame();
        session.Festival.Restore(CompletedFestivalSave());
        return session;
    }

    private static FestivalSave CompletedFestivalSave() => new()
    {
        Scrip = 7,
        Results =
        [
            new FestivalYearResultSave
            {
                FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                Year = 1,
                ItemIds =
                [
                    DataCatalog.AuricShootId,
                    DataCatalog.SunvaultGourdId,
                    DataCatalog.CrownstarSaffronId
                ],
                Score = 30,
                AwardId = FestivalCatalog.GoldenCrownAwardId
            }
        ],
        CurrencyBalances =
        [
            new FestivalCurrencySave
            {
                CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                Balance = 9
            }
        ]
    };

    private static void RestoreOrchard(
        GameSession session,
        GridPosition tree,
        GridPosition hive
    )
    {
        var save = session.Capture();
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = hive.X,
                Y = hive.Y,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = tree.X,
                Y = tree.Y,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = hive.X,
                Y = hive.Y
            }
        ];
        session.Restore(save);
    }

    private static StarlightNodeSave Node(
        string nodeId,
        string itemId
    ) => new()
    {
        NodeId = nodeId,
        Contributions =
        [
            new StarlightContributionSave
            {
                ItemId = itemId,
                Count = 1
            }
        ]
    };
}
