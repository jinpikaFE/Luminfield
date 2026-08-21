using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseFCompletionTests
{
    [Fact]
    public void StarGateCatalogHasStableSixRegionRouteAndFinalBuildContract()
    {
        var project = ConstructionCatalog.SixfoldStarGate;
        Assert.Equal(2400, project.CoinCost);
        Assert.Equal(5, project.RequiredNights);
        Assert.Equal(4, project.Materials.Count);
        Assert.Equal(
            [
                ConstructionCatalog.HomesteadWorkshopProjectId,
                ConstructionCatalog.HomesteadGreenhouseProjectId,
                ConstructionCatalog.CottageSecondUpgradeId
            ],
            project.RequiredProjectIds
        );

        Assert.Equal(6, StarGateCatalog.Destinations.Count);
        Assert.Equal(
            6,
            StarGateCatalog.Destinations
                .Select(destination => destination.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(StarGateCatalog.Destinations, destination =>
        {
            Assert.True(WorldDefinition.IsInBounds(destination.ArrivalCell));
            Assert.False(WorldDefinition.IsBlocked(destination.ArrivalCell));
        });
    }

    [Fact]
    public void SixLightsAreRequiredAndFailureLeavesWholeSessionUnchanged()
    {
        var session = PreparedStarGateSession(restoreSixLights: false);
        var before = Snapshot(session);

        var result = session.StartConstruction(
            ConstructionCatalog.SixfoldStarGateProjectId
        );

        Assert.False(result.Succeeded);
        Assert.Equal(
            "construction.sixfold_star_gate.requires_six_lights",
            result.MessageKey
        );
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void GateBuildsForFiveNightsThenRequiresPureHandActivation()
    {
        var session = PreparedStarGateSession(restoreSixLights: true);
        var started = session.StartConstruction(
            ConstructionCatalog.SixfoldStarGateProjectId
        );

        Assert.True(started.Succeeded);
        Assert.Equal(2600, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.PrismheartOreId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarironOreId));
        Assert.Equal(
            ConstructionPhase.InProgress,
            session.Construction.PhaseFor(
                ConstructionCatalog.SixfoldStarGateProjectId
            )
        );

        for (var night = 0; night < 5; night++)
        {
            session.EndDay();
        }

        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.SixfoldStarGateProjectId
        ));
        SetAdjacentToGate(session);
        session.Inventory.Select(1);
        var beforeWrongTool = Snapshot(session);
        var preview = session.PreviewSelectedTarget(FarmLayout.StarGateCell);
        var wrongTool = session.UseSelected(FarmLayout.StarGateCell);

        Assert.Equal(TargetPreviewState.NeedsTool, preview.State);
        Assert.Equal(TargetPreviewKind.StarGate, preview.Kind);
        Assert.False(wrongTool.Succeeded);
        Assert.Equal("notice.needs_hand", wrongTool.MessageKey);
        Assert.Equal(beforeWrongTool, Snapshot(session));

        session.Inventory.Select(0);
        var available = session.PreviewSelectedTarget(FarmLayout.StarGateCell);
        var activated = session.UseSelected(FarmLayout.StarGateCell);
        Assert.Equal(TargetPreviewState.Available, available.State);
        Assert.Equal(
            "target.action.activate_star_gate",
            available.LabelKey
        );
        Assert.True(activated.Succeeded);
        Assert.Equal("star_gate.activated", activated.MessageKey);
        Assert.True(session.StarGate.Activated);
    }

    [Fact]
    public void AllSixDestinationsTravelAndGateStatePersists()
    {
        var session = CompletedStarGateSession();

        foreach (var destination in StarGateCatalog.Destinations)
        {
            var result = session.TravelStarGate(destination.Id);
            Assert.True(result.Succeeded);
            Assert.Equal(destination.ArrivalCell, session.PlayerCell);
            Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        }

        Assert.Equal(6, session.StarGate.TravelCount);
        Assert.Equal(
            StarGateCatalog.StarfallRuinsId,
            session.StarGate.LastDestinationId
        );

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.StarGate.Activated);
        Assert.Equal(6, restored.StarGate.TravelCount);
        Assert.Equal(
            StarGateCatalog.StarfallRuinsId,
            restored.StarGate.LastDestinationId
        );
    }

    [Fact]
    public void InvalidOrLegacyGateStateNormalizesWithoutInventingProgress()
    {
        var missingConstruction = StarGateSystem.NormalizeSave(
            new StarGateSave
            {
                Activated = true,
                LastDestinationId = StarGateCatalog.CrystalValeId,
                TravelCount = 12
            },
            constructionCompleted: false
        );
        Assert.False(missingConstruction.Activated);
        Assert.Equal(0, missingConstruction.TravelCount);
        Assert.Empty(missingConstruction.LastDestinationId);

        var invalidDestination = StarGateSystem.NormalizeSave(
            new StarGateSave
            {
                Activated = true,
                LastDestinationId = "removed_region",
                TravelCount = -4
            },
            constructionCompleted: true
        );
        Assert.True(invalidDestination.Activated);
        Assert.Equal(0, invalidDestination.TravelCount);
        Assert.Empty(invalidDestination.LastDestinationId);
    }

    private static GameSession CompletedStarGateSession()
    {
        var session = PreparedStarGateSession(restoreSixLights: true);
        Assert.True(session.StartConstruction(
            ConstructionCatalog.SixfoldStarGateProjectId
        ).Succeeded);
        for (var night = 0; night < 5; night++)
        {
            session.EndDay();
        }
        SetAdjacentToGate(session);
        Assert.True(session.UseSelected(FarmLayout.StarGateCell).Succeeded);
        return session;
    }

    private static GameSession PreparedStarGateSession(bool restoreSixLights)
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 60));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 20));
        Assert.True(session.Inventory.Add(DataCatalog.PrismheartOreId, 8));
        Assert.True(session.Inventory.Add(DataCatalog.StarironOreId, 12));

        var save = session.Capture();
        save.Coins = 5000;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Construction = CompletedGatePrerequisites();
        session.Restore(save);
        if (restoreSixLights)
        {
            RestoreAllStarlights(session);
        }
        return session;
    }

    private static ConstructionSave CompletedGatePrerequisites() => new()
    {
        Projects =
        [
            Completed(ConstructionCatalog.CottageFirstUpgradeId),
            Completed(ConstructionCatalog.HomesteadWorkshopProjectId),
            Completed(ConstructionCatalog.HomesteadGreenhouseProjectId),
            Completed(ConstructionCatalog.CottageSecondUpgradeId)
        ]
    };

    private static ConstructionProjectSave Completed(string projectId) =>
        new()
        {
            ProjectId = projectId,
            Completed = true
        };

    private static void RestoreAllStarlights(GameSession session)
    {
        var allSources = DataCatalog.StarlightPedestals.Values
            .SelectMany(pedestal => pedestal.Nodes)
            .SelectMany(node => node.SourceIds ?? [])
            .ToHashSet(StringComparer.Ordinal);
        var allPedestalIds = DataCatalog.StarlightPedestals.Keys
            .ToHashSet(StringComparer.Ordinal);
        var save = new StarlightSave
        {
            Pedestals = DataCatalog.StarlightPedestals.Values
                .Select(pedestal => new StarlightPedestalSave
                {
                    PedestalId = pedestal.Id,
                    Discovered = true,
                    RewardUnlocked = true,
                    Nodes = pedestal.Nodes
                        .Select(CompletedNode)
                        .ToList()
                })
                .ToList()
        };
        var context = new StarlightProgressContext(
            allSources,
            allSources,
            allPedestalIds
        );
        session.Starlight.Restore(save, context);
        Assert.True(session.Starlight.StarfallSixfoldConvergenceUnlocked);
    }

    private static StarlightNodeSave CompletedNode(
        StarlightNodeDefinition node
    )
    {
        var state = new StarlightNodeSave { NodeId = node.Id };
        if (node.SourceKind != StarlightNodeSourceKind.Inventory)
        {
            return state;
        }

        var remaining = node.RequiredCount;
        foreach (var option in node.Options)
        {
            var count = Math.Min(remaining, option.MaximumCount);
            if (count > 0)
            {
                state.Contributions.Add(new StarlightContributionSave
                {
                    ItemId = option.ItemId,
                    Count = count
                });
                remaining -= count;
            }
        }
        Assert.Equal(0, remaining);
        return state;
    }

    private static void SetAdjacentToGate(GameSession session) =>
        session.SetPlayerLocation(
            FarmLayout.StarGateCell.X * 16 + 8,
            (FarmLayout.StarGateCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
