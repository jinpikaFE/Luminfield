using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class MultiStarlightPortfolioTests
{
    [Fact]
    public void CatalogHasSixPedestalsWithGloballyUniqueStableNodeIds()
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
        Assert.Equal(4, DataCatalog.HomesteadStarlight.Nodes[0].RequiredCount);
        Assert.All(
            DataCatalog.HomesteadStarlight.Nodes[0].Options,
            option => Assert.Equal(1, option.MaximumCount)
        );
    }

    [Fact]
    public void LegacyWoodlandRootMigratesWhileHomesteadStartsEmpty()
    {
        var legacy = new StarlightSave
        {
            PedestalId = DataCatalog.WoodlandStarlightId,
            Discovered = true,
            Nodes =
            [
                new StarlightNodeSave
                {
                    NodeId = DataCatalog.WoodlandHarvestNodeId,
                    Contributions =
                    [
                        new StarlightContributionSave
                        {
                            ItemId = DataCatalog.StarbudId,
                            Count = 1
                        }
                    ]
                }
            ]
        };

        var normalized = StarlightSystem.NormalizeSave(legacy);
        var woodland = normalized.Pedestals.Single(state =>
            state.PedestalId == DataCatalog.WoodlandStarlightId
        );
        var homestead = normalized.Pedestals.Single(state =>
            state.PedestalId == DataCatalog.HomesteadStarlightId
        );

        Assert.True(woodland.Discovered);
        Assert.Equal(1, Progress(woodland, DataCatalog.WoodlandHarvestNodeId));
        Assert.False(homestead.Discovered);
        Assert.False(homestead.RewardUnlocked);
        Assert.All(homestead.Nodes, node => Assert.Empty(node.Contributions));
        Assert.Equal(1, Progress(normalized, DataCatalog.WoodlandHarvestNodeId));
    }

    [Fact]
    public void PedestalProgressIsIndependentAndCaptureKeepsWoodlandRootMirror()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));

        var result = session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadHarvestNodeId
        );
        var captured = session.Capture().Starlight;

        Assert.True(result.Succeeded);
        Assert.Equal(
            1,
            session.Starlight.Progress(
                DataCatalog.HomesteadStarlightId,
                DataCatalog.HomesteadHarvestNodeId
            )
        );
        Assert.Equal(0, session.Starlight.CompletedNodeCount);
        Assert.False(session.Starlight.Discovered);
        Assert.True(session.Starlight.IsDiscovered(
            DataCatalog.HomesteadStarlightId
        ));
        Assert.Equal(DataCatalog.WoodlandStarlightId, captured.PedestalId);
        Assert.False(captured.Discovered);
        Assert.Equal(
            1,
            Progress(
                captured.Pedestals.Single(state =>
                    state.PedestalId == DataCatalog.HomesteadStarlightId
                ),
                DataCatalog.HomesteadHarvestNodeId
            )
        );
    }

    [Fact]
    public void MismatchedPedestalAndNodeFailsWithoutChangingInventoryOrState()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));
        var before = JsonSerializer.Serialize(session.Capture());

        var result = session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.WoodlandHarvestNodeId
        );

        Assert.False(result.Succeeded);
        Assert.Equal("starlight.unknown_node", result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void HomesteadPedestalPreviewAndActionRequireRealAdjacentTargetAndHand()
    {
        var session = new GameSession();
        session.NewGame();
        var pedestal = FarmLayout.HomesteadStarlightCell;
        session.SetPlayerLocation(
            pedestal.X * 16 + 8,
            (pedestal.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        session.Inventory.Select(1);

        var wrongToolPreview = session.PreviewSelectedTarget(pedestal);
        var wrongToolAction = session.UseSelected(pedestal);

        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal(TargetPreviewKind.StarlightPedestal, wrongToolPreview.Kind);
        Assert.False(wrongToolAction.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolAction.MessageKey);
        Assert.False(session.Starlight.IsDiscovered(
            DataCatalog.HomesteadStarlightId
        ));

        session.Inventory.Select(0);
        var available = session.PreviewSelectedTarget(pedestal);
        var opened = session.UseSelected(pedestal);

        Assert.True(available.IsAvailable);
        Assert.True(opened.Succeeded);
        Assert.True(session.Starlight.IsDiscovered(
            DataCatalog.HomesteadStarlightId
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
        Assert.False(session.OpenStarlightPedestal(
            DataCatalog.HomesteadStarlightId,
            new GridPosition(pedestal.X + 1, pedestal.Y)
        ).Succeeded);
    }

    [Fact]
    public void HomesteadNodesAcceptDistinctFamiliesAndUnlockOnlyHomeReward()
    {
        var session = new GameSession();
        session.NewGame();
        AddHomesteadOfferings(session);

        var harvest = session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadHarvestNodeId
        );
        var artisan = session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadArtisanNodeId
        );
        var building = session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadBuildingNodeId
        );

        Assert.True(harvest.Succeeded);
        Assert.True(artisan.Succeeded);
        Assert.True(building.Succeeded);
        Assert.True(building.Activated);
        Assert.True(session.Starlight.HomesteadIrrigationUnlocked);
        Assert.False(session.Starlight.WoodlandRenewalUnlocked);
        Assert.Equal(
            3,
            session.Starlight.CompletedNodeCountFor(
                DataCatalog.HomesteadStarlightId
            )
        );
    }

    [Fact]
    public void HomesteadRewardAddsOnlyFourDiagonalOutdoorSprinklerTiles()
    {
        var ordinary = new GameSession();
        ordinary.NewGame();
        PrepareSprinklerBed(ordinary);

        var restored = new GameSession();
        restored.NewGame();
        AddHomesteadOfferings(restored);
        CompleteHomestead(restored);
        PrepareSprinklerBed(restored);

        ordinary.EndDay();
        restored.EndDay();

        foreach (var offset in CardinalOffsets)
        {
            Assert.Equal(1, WateredNightsAt(ordinary, offset));
            Assert.Equal(1, WateredNightsAt(restored, offset));
        }
        foreach (var offset in DiagonalOffsets)
        {
            Assert.Equal(0, WateredNightsAt(ordinary, offset));
            Assert.Equal(1, WateredNightsAt(restored, offset));
        }
        Assert.Empty(restored.GreenhouseFarm.Tiles);
    }

    private static readonly GridPosition SprinklerCell = new(15, 16);
    private static readonly GridPosition[] CardinalOffsets =
    [
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0)
    ];
    private static readonly GridPosition[] DiagonalOffsets =
    [
        new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)
    ];

    private static void AddHomesteadOfferings(GameSession session)
    {
        var crops = DataCatalog.CropIds.Take(4).ToArray();
        Assert.True(session.Inventory.Add(
            DataCatalog.ProduceItemId(crops[0], CropQuality.Luminous),
            1
        ));
        foreach (var cropId in crops.Skip(1))
        {
            Assert.True(session.Inventory.Add(cropId, 1));
        }
        foreach (var itemId in new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.MoonstonePathId,
            DataCatalog.StarwoodFenceId,
            DataCatalog.StarlightTorchId
        })
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
    }

    private static void CompleteHomestead(GameSession session)
    {
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadHarvestNodeId
        ).Succeeded);
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadArtisanNodeId
        ).Succeeded);
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadBuildingNodeId
        ).Succeeded);
    }

    private static void PrepareSprinklerBed(GameSession session)
    {
        var offsets = CardinalOffsets.Concat(DiagonalOffsets);
        session.Farm.Restore(offsets.Select(offset => new FarmTileState
        {
            X = SprinklerCell.X + offset.X,
            Y = SprinklerCell.Y + offset.Y,
            Tilled = true,
            CropId = DataCatalog.StarbudId,
            PlantedDay = 1
        }));
        session.FarmObjects.Restore(
            new FarmObjectSave
            {
                Objects =
                [
                    new PlacedFarmObjectSave
                    {
                        X = SprinklerCell.X,
                        Y = SprinklerCell.Y,
                        ItemId = DataCatalog.DewfallSprinklerId
                    }
                ]
            },
            session.Farm,
            session.Storage
        );
        Assert.Equal(DataCatalog.DewfallSprinklerId,
            session.FarmObjects.ItemAt(SprinklerCell));
    }

    private static int WateredNightsAt(
        GameSession session,
        GridPosition offset
    ) => session.Farm.Tiles[new GridPosition(
        SprinklerCell.X + offset.X,
        SprinklerCell.Y + offset.Y
    )].WateredNights;

    private static int Progress(
        StarlightSave state,
        string nodeId
    ) => state.Nodes.Single(node => node.NodeId == nodeId)
        .Contributions.Sum(entry => entry.Count);

    private static int Progress(
        StarlightPedestalSave state,
        string nodeId
    ) => state.Nodes.Single(node => node.NodeId == nodeId)
        .Contributions.Sum(entry => entry.Count);
}
