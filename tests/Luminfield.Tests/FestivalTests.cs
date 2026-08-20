using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class FestivalTests
{
    [Fact]
    public void GleamriseFestivalGateOpensOnlyOnSeasonDaySeven()
    {
        var session = NewSession(day: 6);

        var closed = session.PreviewSelectedTarget(FestivalSystem.GateCell);
        Assert.Equal(TargetPreviewState.Blocked, closed.State);
        Assert.Equal(
            "target.status.gleamrise_festival_closed",
            closed.LabelKey
        );
        Assert.False(session.TryEnterGleamriseFestival().Succeeded);
        Assert.DoesNotContain(
            session.Capture().GleamriseSeason.Counters,
            counter => counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterFestivalJoined
        );

        session.Clock.Reset(FestivalSystem.FestivalSeasonDay, 10 * 60);
        var open = session.PreviewSelectedTarget(FestivalSystem.GateCell);
        Assert.Equal(TargetPreviewState.Available, open.State);
        Assert.Equal(
            "target.action.enter_gleamrise_festival",
            open.LabelKey
        );
        Assert.True(session.TryEnterGleamriseFestival().Succeeded);
        Assert.Equal(
            1,
            session.Capture().GleamriseSeason.Counters.Single(counter =>
                counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterFestivalJoined
            ).Count
        );
        Assert.True(session.TryEnterGleamriseFestival().Succeeded);
        Assert.Equal(
            1,
            session.Capture().GleamriseSeason.Counters.Single(counter =>
                counter.CounterId ==
                GleamriseSeasonGoalSystem.CounterFestivalJoined
            ).Count
        );

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(FestivalSystem.GateCell);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal("target.need.hand", wrongTool.LabelKey);
    }

    [Fact]
    public void FestivalStagesAdvanceInOrderAndGrantStableRewards()
    {
        var session = NewFestivalSession();

        var first = session.AdvanceGleamriseFestivalStage();
        Assert.True(first.Succeeded);
        Assert.Equal(
            "festival.gleamrise.festival_stage_lay_moonstone_rows.completed",
            first.MessageKey
        );
        Assert.Equal(2, session.Inventory.Count(
            DataCatalog.GleamriseFestivalTokenId
        ));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.DawnlaceSeedId));

        var second = session.AdvanceGleamriseFestivalStage();
        Assert.True(second.Succeeded);
        Assert.Equal(4, session.Inventory.Count(
            DataCatalog.GleamriseFestivalTokenId
        ));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.GlimmerpodSeedId));

        var third = session.AdvanceGleamriseFestivalStage();
        Assert.True(third.Succeeded);
        Assert.Equal(7, session.Inventory.Count(
            DataCatalog.GleamriseFestivalTokenId
        ));
        Assert.Equal(1, session.Inventory.Count(
            DataCatalog.StarsoilFertilizerId
        ));

        var completed = session.AdvanceGleamriseFestivalStage();
        Assert.False(completed.Succeeded);
        Assert.Equal("festival.gleamrise.completed", completed.MessageKey);
        Assert.True(session.GleamriseFestivalSnapshot().Completed);
    }

    [Fact]
    public void StageRewardsAreAtomicWhenBackpackCannotFitBothRewards()
    {
        var session = NewFestivalSession();
        FillInventoryLeavingEmptySlots(
            session,
            1,
            DataCatalog.GleamriseFestivalTokenId,
            DataCatalog.DawnlaceSeedId
        );
        var before = InventoryState(session);

        var result = session.AdvanceGleamriseFestivalStage();

        Assert.False(result.Succeeded);
        Assert.Equal("notice.inventory_full", result.MessageKey);
        Assert.Equal(before, InventoryState(session));
        Assert.Empty(session.Festival.CompletedStageIds);
    }

    [Fact]
    public void ExchangeRequiresTokensAndTradesAtomically()
    {
        var session = NewFestivalSession();
        var before = InventoryState(session);

        var missing = session.ExchangeGleamriseFestivalItem(
            DataCatalog.MoonplumSaplingId
        );
        Assert.False(missing.Succeeded);
        Assert.Equal("festival.exchange.need_tokens", missing.MessageKey);
        Assert.Equal(before, InventoryState(session));

        Assert.True(session.Inventory.Add(
            DataCatalog.GleamriseFestivalTokenId,
            4
        ));
        var exchanged = session.ExchangeGleamriseFestivalItem(
            DataCatalog.MoonplumSaplingId
        );
        Assert.True(exchanged.Succeeded);
        Assert.Equal(0, session.Inventory.Count(
            DataCatalog.GleamriseFestivalTokenId
        ));
        Assert.Equal(1, session.Inventory.Count(
            DataCatalog.MoonplumSaplingId
        ));
    }

    [Fact]
    public void FestivalActionsRequireFestivalLocation()
    {
        var session = NewSession(FestivalSystem.FestivalSeasonDay);
        Assert.True(session.Inventory.Add(
            DataCatalog.GleamriseFestivalTokenId,
            4
        ));

        var stage = session.AdvanceGleamriseFestivalStage();
        var exchange = session.ExchangeGleamriseFestivalItem(
            DataCatalog.MoonplumSaplingId
        );

        Assert.False(stage.Succeeded);
        Assert.False(exchange.Succeeded);
        Assert.Equal("notice.gleamrise_festival_only", stage.MessageKey);
        Assert.Equal("notice.gleamrise_festival_only", exchange.MessageKey);
        Assert.Empty(session.Festival.CompletedStageIds);
        Assert.Equal(4, session.Inventory.Count(
            DataCatalog.GleamriseFestivalTokenId
        ));
        Assert.Equal(0, session.Inventory.Count(
            DataCatalog.MoonplumSaplingId
        ));
    }

    [Fact]
    public void FestivalSaveRoundTripsAndKeepsFestivalLocation()
    {
        var session = NewSession(FestivalSystem.FestivalSeasonDay);
        Assert.True(session.TryEnterGleamriseFestival().Succeeded);
        session.SetPlayerLocation(
            FestivalSystem.ActivityCell.X * 16 + 8,
            (FestivalSystem.ActivityCell.Y + 2) * 16 + 8,
            PlayerLocationIds.GleamriseFestival
        );
        Assert.True(session.AdvanceGleamriseFestivalStage().Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.True(restored.InsideGleamriseFestival);
        Assert.Equal(
            FestivalSystem.StageLayMoonstoneRows,
            Assert.Single(restored.Festival.CompletedStageIds)
        );
    }

    [Fact]
    public void FestivalSaveNormalizesUnknownStagesAndPreservesOrder()
    {
        var normalized = FestivalSystem.NormalizeSave(
            new FestivalSave
            {
                Year = 1,
                FestivalId = "old_festival",
                Joined = false,
                CompletedStageIds =
                [
                    FestivalSystem.StageTuneGlimmerpod,
                    "unknown",
                    FestivalSystem.StageLayMoonstoneRows,
                    FestivalSystem.StageLayMoonstoneRows
                ]
            },
            FestivalSystem.FestivalSeasonDay
        );

        Assert.True(normalized.Joined);
        Assert.Equal(FestivalSystem.GleamriseSowingFestivalId, normalized.FestivalId);
        Assert.Equal(
        [
            FestivalSystem.StageLayMoonstoneRows,
            FestivalSystem.StageTuneGlimmerpod
        ], normalized.CompletedStageIds);
    }

    [Fact]
    public void FestivalTokenIsStorableButNotSellable()
    {
        Assert.Contains(
            DataCatalog.GleamriseFestivalTokenId,
            DataCatalog.StorableItemIds
        );
        Assert.DoesNotContain(
            DataCatalog.GleamriseFestivalTokenId,
            DataCatalog.SellableItemIds
        );
    }

    private static GameSession NewSession(int day)
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(day, 10 * 60);
        return session;
    }

    private static GameSession NewFestivalSession()
    {
        var session = NewSession(FestivalSystem.FestivalSeasonDay);
        Assert.True(session.TryEnterGleamriseFestival().Succeeded);
        session.SetPlayerLocation(
            FestivalSystem.ActivityCell.X * 16 + 8,
            (FestivalSystem.ActivityCell.Y + 2) * 16 + 8,
            PlayerLocationIds.GleamriseFestival
        );
        return session;
    }

    private static void FillInventoryLeavingEmptySlots(
        GameSession session,
        int emptySlots,
        params string[] additionalExclusions
    )
    {
        var excluded = new HashSet<string>(
            additionalExclusions,
            StringComparer.Ordinal
        )
        {
            DataCatalog.HandId,
            DataCatalog.ShovelId,
            DataCatalog.MacheteId,
            DataCatalog.WateringCanId,
            DataCatalog.BucketId
        };
        foreach (var itemId in DataCatalog.Items.Keys)
        {
            if (EmptySlotCount(session) <= emptySlots)
            {
                break;
            }

            if (excluded.Contains(itemId) || session.Inventory.Count(itemId) > 0)
            {
                continue;
            }

            _ = session.Inventory.Add(itemId, 1);
        }

        Assert.Equal(emptySlots, EmptySlotCount(session));
    }

    private static int EmptySlotCount(GameSession session) =>
        session.Inventory.Slots.Count(slot => slot.IsEmpty);

    private static IReadOnlyList<(string ItemId, int Count)> InventoryState(
        GameSession session
    ) => session.Inventory.Slots
        .Select(slot => (slot.ItemId, slot.Count))
        .ToArray();
}
