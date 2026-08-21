using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class CrystalGrottoMiningTests
{
    [Fact]
    public void CatalogFreezesFiveRoomMineralsUpgradeAndCrystalStarlight()
    {
        Assert.Equal("crystal_grotto_survey", MiningCatalog.CrystalGrottoSurveyId);
        Assert.Equal(5, CrystalGrottoSurveyLayout.RoomCount);
        Assert.Equal(40, CrystalGrottoSurveyLayout.Width);
        Assert.Equal(22, CrystalGrottoSurveyLayout.Height);
        Assert.Equal(new GridPosition(70, 108),
            CrystalGrottoSurveyLayout.WorldEntryCell);
        Assert.Equal(new GridPosition(70, 109),
            CrystalGrottoSurveyLayout.WorldReturnCell);
        Assert.Equal(new GridPosition(20, 20),
            CrystalGrottoSurveyLayout.ExitCell);
        Assert.Equal(new GridPosition(20, 18),
            CrystalGrottoSurveyLayout.SafeArrivalCell);
        Assert.Equal(new GridPosition(17, 16),
            CrystalGrottoSurveyLayout.UpgradeBenchCell);
        Assert.Equal(new GridPosition(7, 11),
            CrystalGrottoSurveyLayout.SealCell);
        Assert.Equal(new GridPosition(35, 7),
            CrystalGrottoSurveyLayout.DepthAnchorCell);
        Assert.Equal(
            [
                (DataCatalog.LumenSlateOreId, 32, 4,
                    ToolProgressionCatalog.BasicTierId, new[] { 1, 2 }),
                (DataCatalog.MoonveinOreId, 48, 4,
                    ToolProgressionCatalog.BasicTierId, new[] { 2 }),
                (DataCatalog.PrismheartOreId, 72, 5,
                    ToolProgressionCatalog.BronzeStarTierId, new[] { 3, 4 }),
                (DataCatalog.StarironOreId, 96, 6,
                    ToolProgressionCatalog.BronzeStarTierId, new[] { 4, 5 })
            ],
            MiningCatalog.Minerals.Select(mineral => (
                mineral.ItemId,
                mineral.SellPrice,
                mineral.EnergyCost,
                mineral.RequiredToolTierId,
                mineral.RoomNumbers.ToArray()
            ))
        );
        Assert.All(MiningCatalog.Minerals, mineral => Assert.Equal(
            mineral.SellPrice,
            DataCatalog.Item(mineral.ItemId).SellPrice
        ));
        Assert.All(MiningCatalog.Veins, vein =>
        {
            Assert.True(CrystalGrottoSurveyLayout.IsWalkable(vein.Cell));
            Assert.Equal(
                vein.RoomNumber,
                CrystalGrottoSurveyLayout.RoomNumberAt(vein.Cell)
            );
            Assert.Contains(
                vein.RoomNumber,
                MiningCatalog.Mineral(vein.MineralItemId).RoomNumbers
            );
        });
        Assert.Equal(
            MiningCatalog.Veins.Count,
            MiningCatalog.Veins.Select(vein => vein.Id)
                .Distinct(StringComparer.Ordinal).Count()
        );
        Assert.True(MiningCatalog.Veins.Count(vein =>
            vein.MineralItemId == DataCatalog.LumenSlateOreId) >= 6);
        Assert.True(MiningCatalog.Veins.Count(vein =>
            vein.MineralItemId == DataCatalog.MoonveinOreId) >= 3);

        var upgrade = ToolProgressionCatalog.ShovelBronzeStarUpgrade;
        Assert.Equal("tool_upgrade_shovel_bronze_star", upgrade.Id);
        Assert.Equal(DataCatalog.ShovelId, upgrade.ToolId);
        Assert.Equal(420, upgrade.CoinCost);
        Assert.Equal(2, upgrade.RequiredNights);
        Assert.Equal(
            [(DataCatalog.LumenSlateOreId, 6), (DataCatalog.MoonveinOreId, 3)],
            upgrade.Materials.Select(value => (value.ItemId, value.Count))
        );

        var pedestal = DataCatalog.CrystalValeStarlight;
        Assert.Equal("starlight_crystal_vale", pedestal.Id);
        Assert.Equal(
            DataCatalog.CrystalRuinsPassageRewardId,
            pedestal.RewardId
        );
        Assert.Equal(
            [
                DataCatalog.CrystalValeMineralChorusNodeId,
                DataCatalog.CrystalValeTemperedShovelNodeId,
                DataCatalog.CrystalValeDepthAnchorNodeId
            ],
            pedestal.Nodes.Select(node => node.Id)
        );
        Assert.Equal(
            WorldDefinition.CrystalWellCell,
            StarlightSpatialCatalog.ForPedestal(pedestal.Id).Cell
        );
    }

    [Fact]
    public void MiningPreviewAndActionShareTargetToolTierEnergyAndCapacityRules()
    {
        var lumen = MiningCatalog.Veins.First(vein =>
            vein.MineralItemId == DataCatalog.LumenSlateOreId
        );
        var prism = MiningCatalog.Veins.First(vein =>
            vein.MineralItemId == DataCatalog.PrismheartOreId
        );
        var session = PositionedAtVein(lumen);

        var wrongToolBefore = Snapshot(session);
        var wrongToolPreview = session.PreviewSelectedTarget(lumen.Cell);
        var wrongTool = session.UseSelected(lumen.Cell);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal(TargetPreviewKind.MineralVein, wrongToolPreview.Kind);
        Assert.Equal("notice.needs_shovel", wrongTool.MessageKey);
        Assert.Equal(wrongToolBefore, Snapshot(session));

        session.Inventory.Select(1);
        session.SetPlayerLocation(
            (lumen.Cell.X + 3) * 16 + 8,
            lumen.Cell.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        var distantBefore = Snapshot(session);
        Assert.Equal(
            TargetPreviewState.Neutral,
            session.PreviewSelectedTarget(lumen.Cell).State
        );
        Assert.False(session.UseSelected(lumen.Cell).Succeeded);
        Assert.Equal(distantBefore, Snapshot(session));

        session.SetPlayerLocation(
            (prism.Cell.X - 1) * 16 + 8,
            prism.Cell.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        var tierBefore = Snapshot(session);
        var tierPreview = session.PreviewSelectedTarget(prism.Cell);
        var tierBlocked = session.UseSelected(prism.Cell);
        Assert.Equal(TargetPreviewState.NeedsTool, tierPreview.State);
        Assert.Equal(
            "mining.requires_bronze_star_shovel",
            tierBlocked.MessageKey
        );
        Assert.Equal(tierBefore, Snapshot(session));

        var lowEnergy = PositionedAtVein(lumen, energy: 3);
        lowEnergy.Inventory.Select(1);
        var energyBefore = Snapshot(lowEnergy);
        Assert.Equal(
            TargetPreviewState.Blocked,
            lowEnergy.PreviewSelectedTarget(lumen.Cell).State
        );
        Assert.Equal("notice.no_energy", lowEnergy.UseSelected(lumen.Cell).MessageKey);
        Assert.Equal(energyBefore, Snapshot(lowEnergy));

        var full = PositionedAtVein(lumen);
        full.Inventory.Select(1);
        FillInventory(full.Inventory);
        var fullBefore = Snapshot(full);
        Assert.Equal(
            TargetPreviewState.Blocked,
            full.PreviewSelectedTarget(lumen.Cell).State
        );
        Assert.Equal("notice.inventory_full", full.UseSelected(lumen.Cell).MessageKey);
        Assert.Equal(fullBefore, Snapshot(full));
    }

    [Fact]
    public void SuccessfulMiningIsAtomicPersistentAndDiscoversMineral()
    {
        var vein = MiningCatalog.Veins.First(value =>
            value.MineralItemId == DataCatalog.LumenSlateOreId
        );
        var session = PositionedAtVein(vein);
        session.Inventory.Select(1);

        var preview = session.PreviewSelectedTarget(vein.Cell);
        var result = session.UseSelected(vein.Cell);

        Assert.True(preview.IsAvailable);
        Assert.True(result.Succeeded);
        Assert.Equal(4, result.EnergyCost);
        Assert.Equal(96, session.Energy);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.LumenSlateOreId));
        Assert.True(session.Mining.IsDepleted(vein.Id));
        Assert.True(session.Collection.IsDiscovered(DataCatalog.LumenSlateOreId));
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(vein.Cell).State
        );

        var restored = new GameSession();
        restored.NewGame();
        restored.Restore(session.Capture());
        Assert.True(restored.Mining.IsDepleted(vein.Id));
        Assert.Equal(1, restored.Inventory.Count(DataCatalog.LumenSlateOreId));
        Assert.True(restored.Collection.IsDiscovered(DataCatalog.LumenSlateOreId));
    }

    [Fact]
    public void PortalBenchExitAndDepthAnchorPreviewTheSameRealTargetsAsActions()
    {
        var session = new GameSession();
        session.NewGame();
        var entry = CrystalGrottoSurveyLayout.WorldEntryCell;
        session.SetPlayerLocation(
            entry.X * 16 + 8,
            (entry.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );

        var entryPreview = session.PreviewSelectedTarget(entry);
        Assert.True(entryPreview.IsAvailable);
        Assert.Equal(TargetPreviewKind.CrystalGrottoPortal, entryPreview.Kind);
        Assert.True(session.TryEnterCrystalGrottoSurvey(entry).Succeeded);

        session.Inventory.Select(1);
        var wrongToolBefore = Snapshot(session);
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(entry).State
        );
        Assert.Equal("notice.needs_hand",
            session.TryEnterCrystalGrottoSurvey(entry).MessageKey);
        Assert.Equal(wrongToolBefore, Snapshot(session));

        session.Inventory.Select(0);
        var bench = CrystalGrottoSurveyLayout.UpgradeBenchCell;
        session.SetPlayerLocation(
            (bench.X - 1) * 16 + 8,
            bench.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        Assert.Equal(
            TargetPreviewKind.ToolUpgradeBench,
            session.PreviewSelectedTarget(bench).Kind
        );
        Assert.True(session.OpenCrystalGrottoUpgradeBench(bench).Succeeded);

        var anchor = CrystalGrottoSurveyLayout.DepthAnchorCell;
        session.SetPlayerLocation(
            (anchor.X - 1) * 16 + 8,
            anchor.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        Assert.True(session.PreviewSelectedTarget(anchor).IsAvailable);
        Assert.True(session.UseSelected(anchor).Succeeded);
        Assert.True(session.Mining.FifthRoomAnchorReached);
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(anchor).State
        );

        var exit = CrystalGrottoSurveyLayout.ExitCell;
        session.SetPlayerLocation(
            exit.X * 16 + 8,
            (exit.Y - 1) * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        Assert.Equal(
            TargetPreviewKind.CrystalGrottoExit,
            session.PreviewSelectedTarget(exit).Kind
        );
        Assert.True(session.TryExitCrystalGrottoSurvey(exit).Succeeded);
    }

    [Fact]
    public void BronzeShovelUpgradeConsumesOnceCompletesAfterTwoNightsAndKeepsStableTool()
    {
        var session = PreparedUpgradeSession();
        var target = CrystalGrottoSurveyLayout.UpgradeBenchCell;

        var started = session.StartToolUpgrade(
            target,
            ToolProgressionCatalog.ShovelBronzeStarUpgradeId
        );

        Assert.True(started.Succeeded);
        Assert.Equal(80, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenSlateOreId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonveinOreId));
        Assert.Equal(DataCatalog.ShovelId, session.Inventory.Slots[1].ItemId);
        Assert.Equal(2, session.ToolProgression.RemainingNights);
        Assert.Equal(
            ToolProgressionCatalog.BasicTierId,
            session.ToolProgression.TierIdFor(DataCatalog.ShovelId)
        );

        var repeatBefore = Snapshot(session);
        Assert.Equal(
            "tool.upgrade.in_progress",
            session.StartToolUpgrade(
                target,
                ToolProgressionCatalog.ShovelBronzeStarUpgradeId
            ).MessageKey
        );
        Assert.Equal(repeatBefore, Snapshot(session));

        session.EndDay();
        Assert.Equal(1, session.ToolProgression.RemainingNights);
        Assert.False(session.ToolProgression.IsUpgradeCompleted(
            ToolProgressionCatalog.ShovelBronzeStarUpgradeId
        ));
        session.EndDay();

        Assert.Equal(
            ToolProgressionCatalog.BronzeStarTierId,
            session.ToolProgression.TierIdFor(DataCatalog.ShovelId)
        );
        Assert.True(session.ToolProgression.IsUpgradeCompleted(
            ToolProgressionCatalog.ShovelBronzeStarUpgradeId
        ));
        Assert.Equal(DataCatalog.ShovelId, session.Inventory.Slots[1].ItemId);

        var completedBefore = Snapshot(session);
        Assert.Equal(
            "tool.upgrade.already_completed",
            session.StartToolUpgrade(
                target,
                ToolProgressionCatalog.ShovelBronzeStarUpgradeId
            ).MessageKey
        );
        Assert.Equal(completedBefore, Snapshot(session));
    }

    [Fact]
    public void FifthRoomAndUpgradeAreRealMilestonesForCrystalValeStarlight()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.ToolProgression = new ToolProgressionSave
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };
        session.Restore(save);
        session.SetPlayerLocation(
            34 * 16 + 8,
            7 * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        Assert.True(session.ActivateCrystalGrottoDepthAnchor(
            CrystalGrottoSurveyLayout.DepthAnchorCell
        ).Succeeded);

        Assert.True(session.Mining.FifthRoomAnchorReached);
        Assert.Equal(
            1,
            session.StarlightNodeProgress(
                DataCatalog.CrystalValeStarlightId,
                DataCatalog.CrystalValeTemperedShovelNodeId
            )
        );
        Assert.Equal(
            1,
            session.StarlightNodeProgress(
                DataCatalog.CrystalValeStarlightId,
                DataCatalog.CrystalValeDepthAnchorNodeId
            )
        );

        foreach (var mineral in MiningCatalog.Minerals)
        {
            Assert.True(session.Inventory.Add(mineral.ItemId, 1));
        }
        var contribution = session.ContributeToStarlightNode(
            DataCatalog.CrystalValeStarlightId,
            DataCatalog.CrystalValeMineralChorusNodeId
        );

        Assert.True(contribution.Succeeded);
        Assert.True(contribution.Activated);
        Assert.True(session.Starlight.CrystalRuinsPassageUnlocked);
        Assert.Empty(DataCatalog.CrystalValeStarlight.Nodes
            .Where(node => node.SourceKind == StarlightNodeSourceKind.Milestones)
            .SelectMany(node => node.Options));
    }

    [Fact]
    public void SchemaOneNormalizationAddsRootsAndCanonicalizesDirtyEntries()
    {
        var normalizedTools = ToolProgressionSystem.NormalizeSave(
            new ToolProgressionSave
            {
                Tools =
                [
                    new ToolProgressionEntrySave
                    {
                        ToolId = "unknown",
                        TierId = ToolProgressionCatalog.BronzeStarTierId
                    },
                    new ToolProgressionEntrySave
                    {
                        ToolId = DataCatalog.ShovelId,
                        TierId = ToolProgressionCatalog.BasicTierId,
                        ActiveUpgradeId =
                            ToolProgressionCatalog.ShovelBronzeStarUpgradeId,
                        RemainingNights = 99
                    },
                    new ToolProgressionEntrySave
                    {
                        ToolId = DataCatalog.ShovelId,
                        TierId = ToolProgressionCatalog.BronzeStarTierId,
                        ActiveUpgradeId = "unknown",
                        RemainingNights = -4
                    }
                ]
            }
        );
        var shovel = Assert.Single(normalizedTools.Tools);
        Assert.Equal(ToolProgressionCatalog.BronzeStarTierId, shovel.TierId);
        Assert.Equal(string.Empty, shovel.ActiveUpgradeId);
        Assert.Equal(0, shovel.RemainingNights);

        var knownVein = MiningCatalog.Veins[0];
        var normalizedMining = MiningSystem.NormalizeSave(new MiningSave
        {
            DepletedVeinIds = [knownVein.Id, knownVein.Id, "unknown"],
            DeepestRoomReached = 99
        });
        Assert.Equal([knownVein.Id], normalizedMining.DepletedVeinIds);
        Assert.Equal(5, normalizedMining.DeepestRoomReached);

        var collection = CollectionSystem.NormalizeSave(
            new CollectionSave
            {
                Initialized = true,
                InitializedCategoryIds =
                    CompendiumCatalog.CategoryIds
                        .Where(id => id != CollectionCategoryIds.Minerals)
                        .ToList()
            },
            [knownVein.MineralItemId]
        );
        Assert.Contains(CollectionCategoryIds.Minerals,
            collection.InitializedCategoryIds);
        Assert.Contains(knownVein.MineralItemId,
            collection.DiscoveredEntryIds);
        Assert.Equal(1, SaveService.CurrentSchemaVersion);
    }

    private static GameSession PositionedAtVein(
        MiningVeinDefinition vein,
        int energy = GameSession.MaxEnergy
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.Energy = energy;
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = (vein.Cell.X - 1) * 16 + 8;
        save.Player.Y = vein.Cell.Y * 16 + 8;
        session.Restore(save);
        return session;
    }

    private static GameSession PreparedUpgradeSession()
    {
        var session = new GameSession();
        session.NewGame();
        var target = CrystalGrottoSurveyLayout.UpgradeBenchCell;
        var save = session.Capture();
        save.Coins = 500;
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = (target.X - 1) * 16 + 8;
        save.Player.Y = target.Y * 16 + 8;
        session.Restore(save);
        Assert.True(session.Inventory.Add(DataCatalog.LumenSlateOreId, 6));
        Assert.True(session.Inventory.Add(DataCatalog.MoonveinOreId, 3));
        return session;
    }

    private static void FillInventory(Inventory inventory)
    {
        var fillItems = DataCatalog.Items.Values
            .Where(item => item.Kind != ItemKind.Tool &&
                item.Id != DataCatalog.LumenSlateOreId)
            .Select(item => item.Id)
            .Distinct(StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .ToArray();
        Assert.Equal(
            Inventory.SlotCount - Inventory.StartingToolCount,
            fillItems.Length
        );
        foreach (var itemId in fillItems)
        {
            Assert.True(inventory.Add(itemId, DataCatalog.Item(itemId).MaxStack));
        }
        Assert.False(inventory.CanAdd(DataCatalog.LumenSlateOreId, 1));
    }

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
