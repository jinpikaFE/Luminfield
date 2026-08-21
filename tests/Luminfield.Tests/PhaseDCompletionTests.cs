using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseDCompletionTests
{
    private static readonly GridPosition HomesteadWater = new(38, 21);

    [Fact]
    public void LineControlConsumesCastCostAndOnlyCommitsSuccessfulCatch()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.GlowgrubBaitId, 2));
        Assert.True(session.EquipFishingBait(
            DataCatalog.GlowgrubBaitId
        ).Succeeded);
        var energyBefore = session.Energy;

        var started = session.BeginFishingChallenge(HomesteadWater);

        Assert.True(started.Succeeded);
        Assert.Equal(energyBefore - FishingSystem.CastEnergyCost, session.Energy);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.GlowgrubBaitId));
        Assert.Empty(session.Fishing.CaughtFishIds);

        var snapshot = session.FishingMinigame.Snapshot();
        for (var step = 0;
             step < 1200 &&
             snapshot.Status == FishingChallengeStatus.Active;
             step++)
        {
            snapshot = session.AdvanceFishingChallenge(
                0.05f,
                snapshot.HookPosition < snapshot.FishPosition
            );
        }

        Assert.Equal(FishingChallengeStatus.Succeeded, snapshot.Status);
        var caught = session.ResolveFishingChallenge();
        Assert.True(caught.Succeeded);
        Assert.True(session.Fishing.IsCaught(snapshot.FishId));
        Assert.True(session.FishingProgression.Experience > 0);
        Assert.Equal(FishingChallengeStatus.Idle,
            session.FishingMinigame.Status);
    }

    [Fact]
    public void RodTackleAndFishingSpecializationUseStableSavedProgression()
    {
        var seed = new GameSession();
        seed.NewGame();
        var save = seed.Capture();
        save.Coins = 5000;
        save.Fishing.Experience = 500;
        save.Inventory = AddItems(
            save.Inventory,
            (DataCatalog.CrystalShardId, 20),
            (DataCatalog.MoonveinOreId, 10),
            (DataCatalog.LumenwoodId, 10)
        );

        var session = new GameSession();
        session.Restore(save);
        Assert.Equal(5, session.FishingProgression.Level);
        Assert.True(session.UpgradeFishingRod().Succeeded);
        Assert.True(session.UpgradeFishingRod().Succeeded);
        Assert.Equal(
            FishingProgressionCatalog.TideglassRodTierId,
            session.FishingProgression.RodTierId
        );
        Assert.True(session.PurchaseFishingGear(
            DataCatalog.StormglassBobberId
        ).Succeeded);
        Assert.True(session.EquipFishingBobber(
            DataCatalog.StormglassBobberId
        ).Succeeded);
        Assert.True(session.ChooseFishingSpecialization(
            FishingProgressionCatalog.DeepThreaderSpecializationId
        ).Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Equal(
            FishingProgressionCatalog.TideglassRodTierId,
            restored.FishingProgression.RodTierId
        );
        Assert.Equal(
            DataCatalog.StormglassBobberId,
            restored.FishingProgression.EquippedBobberId
        );
        Assert.Equal(
            FishingProgressionCatalog.DeepThreaderSpecializationId,
            restored.FishingProgression.SpecializationId
        );
    }

    [Fact]
    public void CrabPotPlacesBaitsResolvesAndCollectsThroughFishingCatalog()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.MoonreedCrabPotId, 1));
        Assert.True(session.Inventory.Add(DataCatalog.GlowgrubBaitId, 1));
        SelectItem(session, DataCatalog.MoonreedCrabPotId);

        var placed = session.UseSelected(HomesteadWater);
        Assert.True(placed.Succeeded);
        Assert.True(session.CrabPots.HasPot(HomesteadWater));

        session.Inventory.Select(0);
        Assert.Equal(
            "target.action.bait_crab_pot",
            session.PreviewSelectedTarget(HomesteadWater).LabelKey
        );
        Assert.True(session.UseSelected(HomesteadWater).Succeeded);
        session.EndDay();
        Assert.True(session.CrabPots.PotAt(HomesteadWater).IsReady);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        var readyFishId = restored.CrabPots
            .PotAt(HomesteadWater)
            .CatchItemId;
        Assert.True(restored.UseSelected(HomesteadWater).Succeeded);
        Assert.True(restored.Fishing.IsCaught(readyFishId));
        Assert.Equal(1, restored.Inventory.Count(readyFishId));
        Assert.True(restored.CrabPots.PotAt(HomesteadWater).IsEmpty);
    }

    [Fact]
    public void BlockedFishingAndTacklePurchasesDoNotMutateState()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.GlowgrubBaitId, 1));
        Assert.True(session.EquipFishingBait(
            DataCatalog.GlowgrubBaitId
        ).Succeeded);
        Assert.True(session.Inventory.Add(
            DataCatalog.StarbudSeedId,
            (Inventory.SlotCount - Inventory.StartingToolCount - 1) * 99
        ));
        var beforeCast = JsonSerializer.Serialize(session.Capture());

        var blockedCast = session.BeginFishingChallenge(HomesteadWater);

        Assert.False(blockedCast.Succeeded);
        Assert.Equal(beforeCast, JsonSerializer.Serialize(session.Capture()));

        var richSave = session.Capture();
        richSave.Coins = 5000;
        richSave.Fishing.Experience = 500;
        var richSession = new GameSession();
        richSession.Restore(richSave);
        var beforePurchase = JsonSerializer.Serialize(richSession.Capture());
        var blockedPurchase = richSession.PurchaseFishingGear(
            DataCatalog.StormglassBobberId
        );

        Assert.False(blockedPurchase.Succeeded);
        Assert.Equal(
            beforePurchase,
            JsonSerializer.Serialize(richSession.Capture())
        );
    }

    private static List<InventorySlot> AddItems(
        List<InventorySlot> slots,
        params (string ItemId, int Count)[] items
    )
    {
        var inventory = new Inventory();
        inventory.Restore(slots, 0);
        foreach (var item in items)
        {
            Assert.True(inventory.Add(item.ItemId, item.Count));
        }
        return inventory.Capture();
    }

    private static void SelectItem(GameSession session, string itemId)
    {
        var index = session.Inventory.Slots
            .Select((slot, slotIndex) => (slot, slotIndex))
            .Single(entry => entry.slot.ItemId == itemId)
            .slotIndex;
        session.Inventory.Select(index);
    }
}
