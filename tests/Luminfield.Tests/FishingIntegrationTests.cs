using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class FishingIntegrationTests
{
    [Fact]
    public void CatalogFreezesTwentyFourFishAcrossThreeWaters()
    {
        Assert.Equal(24, DataCatalog.FishItemIds.Count);
        Assert.Equal(
            DataCatalog.FishItemIds.Count,
            DataCatalog.FishItemIds.Distinct(StringComparer.Ordinal).Count()
        );
        Assert.Equal(24, DataCatalog.Fishes.Count);
        foreach (var waterKind in Enum.GetValues<FishingWaterKind>())
        {
            Assert.Equal(
                8,
                DataCatalog.Fishes.Values.Count(fish =>
                    fish.WaterKind == waterKind
                )
            );
        }

        Assert.All(DataCatalog.FishItemIds, itemId =>
        {
            var item = DataCatalog.Item(itemId);
            Assert.Equal(ItemKind.Fish, item.Kind);
            Assert.True(item.SellPrice > 0);
            Assert.Contains(itemId, DataCatalog.SellableItemIds);
            Assert.Contains(itemId, DataCatalog.StorableItemIds);
        });
        Assert.Equal(
            DataCatalog.FishItemIds,
            CompendiumCatalog.FishEntries.Select(entry => entry.Id)
        );
    }

    [Fact]
    public void StartingRodPreviewCatchAndCodexDiscoveryShareOneCommit()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.Equal(6, Inventory.StartingToolCount);
        Assert.Equal(
            DataCatalog.FishingRodId,
            session.Inventory.Slots[5].ItemId
        );
        session.Inventory.Select(5);
        var water = new GridPosition(38, 21);
        var startingEnergy = session.Energy;
        var preview = session.PreviewSelectedTarget(water);

        Assert.True(preview.IsAvailable);
        Assert.Equal(TargetPreviewKind.Water, preview.Kind);
        Assert.Equal("target.action.fish", preview.LabelKey);

        var result = session.UseSelected(water);

        Assert.True(result.Succeeded);
        Assert.Equal(DataCatalog.PondglowMinnowId, result.GrantedItemId);
        Assert.Equal(startingEnergy - FishingSystem.CastEnergyCost, session.Energy);
        Assert.True(session.Fishing.IsCaught(DataCatalog.PondglowMinnowId));
        Assert.True(session.Collection.IsDiscovered(DataCatalog.PondglowMinnowId));
        Assert.Equal(
            1,
            session.Collection.DiscoveredCount(CollectionCategoryIds.Fish)
        );
    }

    [Fact]
    public void SpecificConditionsOverrideGeneralFishInEveryWater()
    {
        var fishing = new FishingSystem();
        var home = new GridPosition(38, 21);
        var crystal = FindWaterSource(WorldBiome.CrystalVale);
        var wetlands = FindWaterSource(WorldBiome.MoonwaterWetlands);

        Assert.Equal(
            DataCatalog.RainpetalLoachId,
            fishing.PreviewCatch(
                home,
                2,
                8 * 60,
                DataCatalog.RainWeatherId
            )?.Id
        );
        Assert.Equal(
            DataCatalog.StardustPikeId,
            fishing.PreviewCatch(
                crystal,
                4,
                13 * 60,
                DataCatalog.StardustWindWeatherId
            )?.Id
        );
        Assert.Equal(
            DataCatalog.RainveilLampreyId,
            fishing.PreviewCatch(
                wetlands,
                15,
                8 * 60,
                DataCatalog.RainWeatherId
            )?.Id
        );
    }

    [Fact]
    public void FullBackpackFishingFailureChangesNothing()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            (Inventory.SlotCount - Inventory.StartingToolCount) * 99
        ));
        session.Inventory.Select(5);
        var before = JsonSerializer.Serialize(session.Capture());
        var water = new GridPosition(38, 21);

        var preview = session.PreviewSelectedTarget(water);
        var result = session.UseSelected(water);

        Assert.Equal(TargetPreviewState.Blocked, preview.State);
        Assert.Equal("target.blocked.backpack_full", preview.LabelKey);
        Assert.False(result.Succeeded);
        Assert.Equal("notice.inventory_full", result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void FishingSaveAndRewardsNormalizeAndCommitAtomically()
    {
        var normalized = new FishingSave
        {
            CaughtFishIds =
            [
                DataCatalog.PondglowMinnowId,
                "unknown_fish",
                DataCatalog.PondglowMinnowId,
                DataCatalog.CrystalfinDaceId,
                DataCatalog.MoonwaterMinnowId
            ],
            ClaimedRewardIds = ["unknown_reward"]
        };
        var save = new GameSaveV1 { Fishing = normalized };
        var servicePath = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-fishing-{Guid.NewGuid():N}.json"
        );
        try
        {
            File.WriteAllText(servicePath, JsonSerializer.Serialize(save));
            var loaded = new SaveService(servicePath).Load();
            Assert.NotNull(loaded.Save);
            Assert.Equal(
                [
                    DataCatalog.CrystalfinDaceId,
                    DataCatalog.MoonwaterMinnowId,
                    DataCatalog.PondglowMinnowId
                ],
                loaded.Save.Fishing.CaughtFishIds
            );

            var session = new GameSession();
            session.Restore(loaded.Save);
            var startingCoins = session.Coins;
            var reward = session.ClaimFishingCollectionReward(
                FishingSystem.FirstWatersRewardId
            );

            Assert.True(reward.Succeeded);
            Assert.Equal(startingCoins + 60, session.Coins);
            Assert.Equal(2, session.Inventory.Count(DataCatalog.CrystalShardId));
            Assert.Contains(
                FishingSystem.FirstWatersRewardId,
                session.Fishing.ClaimedRewardIds
            );
            var duplicate = session.ClaimFishingCollectionReward(
                FishingSystem.FirstWatersRewardId
            );
            Assert.False(duplicate.Succeeded);
            Assert.Equal(startingCoins + 60, session.Coins);
        }
        finally
        {
            File.Delete(servicePath);
        }
    }

    [Fact]
    public void MoonwaterFishNodesRestoreOnlyTheWetlandPedestal()
    {
        var session = new GameSession();
        session.NewGame();
        foreach (var itemId in new[]
                 {
                     DataCatalog.MoonwaterMinnowId,
                     DataCatalog.MarshveilKilliId,
                     DataCatalog.SilverreedMudfishId,
                     DataCatalog.RainveilLampreyId,
                     DataCatalog.StardustRayId,
                     DataCatalog.StarharvestOrbfinId,
                     DataCatalog.LongnightWispfishId
                 })
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }

        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterLocalFishNodeId
        ).Succeeded);
        Assert.True(session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterWeatherFishNodeId
        ).Succeeded);
        var final = session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterSeasonalFishNodeId
        );

        Assert.True(final.Succeeded);
        Assert.True(final.Activated);
        Assert.True(session.Starlight.MoonwaterTideUnlocked);
        Assert.False(session.Starlight.WoodlandRenewalUnlocked);
        Assert.Equal(
            3,
            session.CompletedStarlightNodeCount(
                DataCatalog.MoonwaterStarlightId
            )
        );

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Starlight.MoonwaterTideUnlocked);
        Assert.False(restored.Starlight.WoodlandRenewalUnlocked);
    }

    private static GridPosition FindWaterSource(WorldBiome biome)
    {
        for (var y = FarmSystem.MapHeight; y < WorldDefinition.Height; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.GetBiome(cell) == biome &&
                    WorldDefinition.IsWaterSource(cell))
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            $"No water source found for {biome}."
        );
    }
}
