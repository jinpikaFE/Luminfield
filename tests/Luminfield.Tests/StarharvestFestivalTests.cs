using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class StarharvestFestivalTests
{
    [Theory]
    [InlineData(38, 600, false)]
    [InlineData(39, 599, false)]
    [InlineData(39, 600, true)]
    [InlineData(39, 1079, true)]
    [InlineData(39, 1080, false)]
    [InlineData(40, 600, false)]
    [InlineData(95, 600, true)]
    public void MarketUsesAnnualStarharvestDayElevenWindow(
        int day,
        int minute,
        bool expected
    ) => Assert.Equal(
        expected,
        FestivalCatalog.IsOpen(
            FestivalCatalog.StarharvestMarketFestivalId,
            day,
            minute
        )
    );

    [Fact]
    public void ShowcaseScoringMatchesFrozenExamples()
    {
        var regular = new[]
        {
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId
        };
        var starlight = new[]
        {
            DataCatalog.AuricShootStarlightId,
            DataCatalog.SunvaultGourdStarlightId,
            DataCatalog.CrownstarSaffronStarlightId
        };
        var artisan = new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId
        };

        Assert.Equal(21, FestivalCatalog.Score(regular));
        Assert.Equal(365, FestivalCatalog.AuctionCoins(regular));
        Assert.Equal(
            FestivalCatalog.SilverSheafAwardId,
            FestivalCatalog.AwardForScore(
                FestivalCatalog.Score(regular)
            ).Id
        );
        Assert.Equal(29, FestivalCatalog.Score(starlight));
        Assert.Equal(822, FestivalCatalog.AuctionCoins(starlight));
        Assert.Equal(
            FestivalCatalog.GoldenCrownAwardId,
            FestivalCatalog.AwardForScore(
                FestivalCatalog.Score(starlight)
            ).Id
        );
        Assert.Equal(22, FestivalCatalog.Score(artisan));
    }

    [Fact]
    public void SuccessfulSubmissionRemovesExactItemsAndAwardsAtomically()
    {
        var session = FestivalSession();
        var items = new[]
        {
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId
        };
        foreach (var itemId in items)
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        Assert.True(session.Inventory.Add(
            DataCatalog.AuricShootLuminousId,
            1
        ));
        var beforeCoins = session.Coins;

        var preview = session.PreviewFestivalSubmission(items);
        var result = session.SubmitFestivalExhibit(items);

        Assert.True(preview.CanSubmit);
        Assert.True(result.Succeeded);
        Assert.Equal(21, result.Result?.Score);
        Assert.Equal(beforeCoins + 365, session.Coins);
        Assert.Equal(7, session.Festival.Scrip);
        Assert.All(items, itemId => Assert.Equal(
            0,
            session.Inventory.Count(itemId)
        ));
        Assert.Equal(
            1,
            session.Inventory.Count(DataCatalog.AuricShootLuminousId)
        );
        Assert.True(session.Festival.HasParticipated(
            FestivalCatalog.StarharvestMarketFestivalId,
            1
        ));
    }

    [Theory]
    [InlineData(
        DataCatalog.AuricShootId,
        DataCatalog.AuricShootLuminousId,
        DataCatalog.CrownstarSaffronId,
        "festival.submission.distinct_families"
    )]
    [InlineData(
        DataCatalog.AuricShootSeedId,
        DataCatalog.SunvaultGourdId,
        DataCatalog.CrownstarSaffronId,
        "festival.submission.ineligible"
    )]
    public void InvalidSubmissionLeavesCompleteSnapshotUnchanged(
        string first,
        string second,
        string third,
        string failureKey
    )
    {
        var session = FestivalSession();
        foreach (var itemId in new[] { first, second, third }.Distinct())
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        var before = JsonSerializer.Serialize(session.Capture());

        var result = session.SubmitFestivalExhibit(
            [first, second, third]
        );

        Assert.False(result.Succeeded);
        Assert.Equal(failureKey, result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void MissingItemAndRepeatSubmissionAreAtomicAndNextYearReopens()
    {
        var session = FestivalSession();
        var items = new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId
        };
        foreach (var itemId in items)
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        Assert.True(session.SubmitFestivalExhibit(items).Succeeded);
        var completed = JsonSerializer.Serialize(session.Capture());

        Assert.False(session.SubmitFestivalExhibit(items).Succeeded);
        Assert.Equal(completed, JsonSerializer.Serialize(session.Capture()));

        var save = session.Capture();
        save.Day = 95;
        save.MinuteOfDay = 600;
        save.Player.LocationId = PlayerLocationIds.StarharvestMarket;
        save.Player.X = StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8;
        save.Player.Y = StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8;
        session.Restore(save);
        foreach (var itemId in items)
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }

        Assert.True(session.PreviewFestivalSubmission(items).CanSubmit);
    }

    [Fact]
    public void ScripShopCapacityAndBalanceFailuresAreAtomic()
    {
        var session = FestivalSession();
        session.Festival.Restore(new FestivalSave { Scrip = 3 });
        var purchased = session.BuyFestivalItem(
            FestivalCatalog.TorchBundleOfferId
        );
        Assert.True(purchased.Succeeded);
        Assert.Equal(0, session.Festival.Scrip);
        Assert.Equal(2, session.Inventory.Count(DataCatalog.StarlightTorchId));

        var before = JsonSerializer.Serialize(session.Capture());
        var insufficient = session.BuyFestivalItem(
            FestivalCatalog.FenceBundleOfferId
        );
        Assert.False(insufficient.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void EntryExitPreviewUsesRealTargetsAdjacencyAndHand()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(39, 600);
        session.Weather.AdvanceToDay(39);
        session.SetPlayerLocation(
            StarharvestMarketLayout.WorldEntryCell.X * 16 + 8,
            (StarharvestMarketLayout.WorldEntryCell.Y - 1) * 16 + 8,
            PlayerLocationIds.World
        );

        var available = session.PreviewSelectedTarget(
            StarharvestMarketLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewKind.FestivalPortal, available.Kind);
        Assert.True(available.IsAvailable);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            StarharvestMarketLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(session.TryEnterStarharvestMarket(
            StarharvestMarketLayout.WorldEntryCell
        ).Succeeded);

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
            StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        Assert.True(session.PreviewSelectedTarget(
            StarharvestMarketLayout.ExitCell
        ).IsAvailable);
        Assert.True(session.TryExitStarharvestMarket(
            StarharvestMarketLayout.ExitCell
        ).Succeeded);
    }

    [Theory]
    [InlineData(20, 14, TargetPreviewKind.FestivalExhibit)]
    [InlineData(9, 14, TargetPreviewKind.FestivalBidBoard)]
    [InlineData(31, 14, TargetPreviewKind.FestivalShop)]
    public void FestivalStationsUseRealObjectPreviewAndAtomicToolFailure(
        int targetX,
        int targetY,
        TargetPreviewKind expectedKind
    )
    {
        var session = FestivalSession();
        var target = new GridPosition(targetX, targetY);
        session.SetPlayerLocation(
            target.X * 16 + 8,
            (target.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );

        var available = session.PreviewSelectedTarget(target);
        Assert.Equal(expectedKind, available.Kind);
        Assert.True(available.IsAvailable);

        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());
        var wrongTool = session.PreviewSelectedTarget(target);
        var action = session.CheckFestivalStation(target);

        Assert.Equal(expectedKind, wrongTool.Kind);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(action.Succeeded);
        Assert.Equal("notice.needs_hand", action.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void ClosingMinuteForcesSafeVillageReturnWithoutFestivalMutation()
    {
        var session = FestivalSession();
        var beforeFestival = JsonSerializer.Serialize(
            session.Festival.Capture()
        );

        session.Clock.Reset(39, 18 * 60);

        Assert.True(session.LeaveFestivalIfClosed());
        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        Assert.Equal(
            StarharvestMarketLayout.WorldReturnCell,
            session.PlayerCell
        );
        Assert.Equal(
            beforeFestival,
            JsonSerializer.Serialize(session.Festival.Capture())
        );
        Assert.False(session.LeaveFestivalIfClosed());
    }

    [Fact]
    public void FestivalSaveNormalizationIsDeterministicAndPreservesCompletion()
    {
        var normalized = FestivalSystem.NormalizeSave(new FestivalSave
        {
            Scrip = -4,
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 1,
                    Score = 21,
                    AwardId = "broken",
                    AuctionCoins = 365,
                    ItemIds = [DataCatalog.AuricShootId]
                },
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 1,
                    Score = 29,
                    AwardId = "broken",
                    AuctionCoins = 822,
                    ItemIds = ["unknown"]
                },
                new FestivalYearResultSave
                {
                    FestivalId = "unknown",
                    Year = 1,
                    Score = 999
                }
            ]
        });

        Assert.Equal(0, normalized.Scrip);
        var result = Assert.Single(normalized.Results);
        Assert.Equal(29, result.Score);
        Assert.Equal(FestivalCatalog.GoldenCrownAwardId, result.AwardId);
        Assert.Empty(result.ItemIds);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(FestivalSystem.NormalizeSave(normalized))
        );
    }

    [Fact]
    public void AllCatalogVillagersUseFestivalDialogueAndSafeUniqueAnchors()
    {
        var states = VillageCatalog.Npcs.Values
            .Select(definition => NpcScheduleSystem.SelectEntry(
                definition,
                39,
                10 * 60,
                WeatherSystem.WeatherForDay(39)
            ))
            .ToArray();

        Assert.All(states, entry =>
        {
            Assert.NotNull(entry);
            Assert.Equal(PlayerLocationIds.StarharvestMarket, entry.LocationId);
            Assert.Equal(VillageCatalog.FestivalSchedulePriority, entry.Priority);
            Assert.True(StarharvestMarketLayout.IsWalkable(entry.Position));
            Assert.StartsWith("festival.starharvest.dialogue.", entry.DialogueKey);
        });
        Assert.Equal(
            VillageCatalog.Npcs.Count,
            states.Select(entry => entry!.Position).Distinct().Count()
        );

        var runtimeStates = new VillageSystem()
            .CurrentNpcs(
                39,
                10 * 60,
                PlayerLocationIds.StarharvestMarket,
                new GridPosition(20, 19)
            );
        Assert.Equal(VillageCatalog.Npcs.Count, runtimeStates.Count);
        Assert.Equal(
            StarharvestMarketLayout.NpcAnchors.Values
                .OrderBy(position => position.X)
                .ThenBy(position => position.Y),
            runtimeStates
                .Select(state => state.Position)
                .OrderBy(position => position.X)
                .ThenBy(position => position.Y)
        );
    }

    [Fact]
    public void InvalidFestivalLocationRestoreReturnsToVillageGate()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 40;
        save.MinuteOfDay = 600;
        save.Player.LocationId = PlayerLocationIds.StarharvestMarket;

        session.Restore(save);

        Assert.Equal(PlayerLocationIds.World, session.PlayerLocationId);
        Assert.Equal(StarharvestMarketLayout.WorldReturnCell, session.PlayerCell);
        Assert.Equal(SaveService.CurrentSchemaVersion, session.Capture().SchemaVersion);
    }

    private static GameSession FestivalSession()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(39, 600);
        session.Weather.AdvanceToDay(39);
        session.SetPlayerLocation(
            StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
            StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        return session;
    }
}
