using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class FireflyTideFestivalTests
{
    [Theory]
    [InlineData(25, 18 * 60, false)]
    [InlineData(26, 17 * 60 + 59, false)]
    [InlineData(26, 18 * 60, true)]
    [InlineData(26, 22 * 60 + 59, true)]
    [InlineData(26, 23 * 60, false)]
    [InlineData(27, 18 * 60, false)]
    [InlineData(82, 18 * 60, true)]
    public void FestivalUsesAnnualRainveilDayTwelveNightWindow(
        int day,
        int minute,
        bool expected
    ) => Assert.Equal(
        expected,
        FestivalCatalog.IsOpen(
            FestivalCatalog.FireflyTideFestivalId,
            day,
            minute
        )
    );

    [Fact]
    public void ThreeDistinctWetlandFishSettleAtomicallyAndAwardGlowmarks()
    {
        var session = TideSession(FireflyTideLayout.LanternLaunchCell);
        Add(
            session,
            DataCatalog.MoonwaterMinnowId,
            DataCatalog.MarshveilKilliId,
            DataCatalog.RainveilLampreyId
        );

        var preview = session.CheckFireflyTideParticipation(
            FireflyTideLayout.LanternLaunchCell,
            [
                DataCatalog.MoonwaterMinnowId,
                DataCatalog.MarshveilKilliId,
                DataCatalog.RainveilLampreyId
            ]
        );
        var result = session.CompleteFireflyTide(
            FireflyTideLayout.LanternLaunchCell,
            preview.ItemIds
        );

        Assert.True(preview.CanSubmit);
        Assert.Equal(13, preview.Score);
        Assert.Equal(FestivalCatalog.FireflyTideWreathAwardId,
            preview.AwardId);
        Assert.True(result.Succeeded);
        Assert.Equal(7, session.Festival.Glowmarks);
        Assert.All(preview.ItemIds, itemId =>
            Assert.Equal(0, session.Inventory.Count(itemId)));
        Assert.NotNull(session.Festival.ResultFor(
            FestivalCatalog.FireflyTideFestivalId,
            1
        ));
    }

    [Theory]
    [InlineData("too_few")]
    [InlineData("duplicate")]
    [InlineData("ineligible")]
    [InlineData("missing")]
    [InlineData("already_done")]
    public void InvalidParticipationLeavesCompleteSnapshotUnchanged(
        string mode
    )
    {
        var session = TideSession(FireflyTideLayout.FishBasinCell);
        Add(
            session,
            DataCatalog.MoonwaterMinnowId,
            DataCatalog.MarshveilKilliId,
            DataCatalog.RainveilLampreyId,
            DataCatalog.StarbudId
        );
        var valid = new[]
        {
            DataCatalog.MoonwaterMinnowId,
            DataCatalog.MarshveilKilliId,
            DataCatalog.RainveilLampreyId
        };
        if (mode == "already_done")
        {
            Assert.True(session.CompleteFireflyTide(
                FireflyTideLayout.FishBasinCell,
                valid
            ).Succeeded);
        }
        var before = JsonSerializer.Serialize(session.Capture());
        var fish = mode switch
        {
            "too_few" => [DataCatalog.MoonwaterMinnowId],
            "duplicate" =>
            [
                DataCatalog.MoonwaterMinnowId,
                DataCatalog.MoonwaterMinnowId,
                DataCatalog.RainveilLampreyId
            ],
            "ineligible" =>
            [
                DataCatalog.MoonwaterMinnowId,
                DataCatalog.MarshveilKilliId,
                DataCatalog.StarbudId
            ],
            "missing" =>
            [
                DataCatalog.MoonwaterMinnowId,
                DataCatalog.MarshveilKilliId,
                DataCatalog.StardustRayId
            ],
            _ => valid
        };

        var result = session.CompleteFireflyTide(
            FireflyTideLayout.FishBasinCell,
            fish
        );

        Assert.False(result.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void GlowshopCurrencyIsIndependentAndPurchaseIsAtomic()
    {
        var session = TideSession(FireflyTideLayout.ShopCell);
        session.Festival.Restore(new FestivalSave
        {
            Scrip = 8,
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                    Balance = 6
                },
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.LongnightLanternKnotId,
                    Balance = 5
                },
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.FireflyGlowmarkId,
                    Balance = 3
                }
            ]
        });

        var result = session.BuyFireflyShopItem(
            FireflyTideLayout.ShopCell,
            FestivalCatalog.FireflyPathOfferId
        );

        Assert.True(result.Succeeded);
        Assert.Equal(4, session.Inventory.Count(DataCatalog.MoonstonePathId));
        Assert.Equal(0, session.Festival.Glowmarks);
        Assert.Equal(6, session.Festival.BloomTokens);
        Assert.Equal(5, session.Festival.LanternKnots);
        Assert.Equal(8, session.Festival.Scrip);
    }

    [Fact]
    public void SaveNormalizationRebuildsScoreAwardAndFiltersCurrency()
    {
        var normalized = FestivalSystem.NormalizeSave(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.FireflyGlowmarkId,
                    Balance = 9
                },
                new FestivalCurrencySave
                {
                    CurrencyId = "unknown",
                    Balance = 999
                }
            ],
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.FireflyTideFestivalId,
                    Year = 1,
                    ItemIds =
                    [
                        DataCatalog.MoonwaterMinnowId,
                        DataCatalog.MarshveilKilliId,
                        DataCatalog.RainveilLampreyId
                    ],
                    Score = 999,
                    AwardId = "broken",
                    AuctionCoins = 999
                },
                new FestivalYearResultSave
                {
                    FestivalId = FestivalCatalog.FireflyTideFestivalId,
                    Year = 2,
                    ItemIds = [DataCatalog.MoonwaterMinnowId]
                }
            ]
        });

        var result = Assert.Single(normalized.Results);
        Assert.Equal(13, result.Score);
        Assert.Equal(FestivalCatalog.FireflyTideWreathAwardId,
            result.AwardId);
        Assert.Equal(0, result.AuctionCoins);
        var currency = Assert.Single(normalized.CurrencyBalances);
        Assert.Equal(FestivalCatalog.FireflyGlowmarkId,
            currency.CurrencyId);
        Assert.Equal(9, currency.Balance);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(FestivalSystem.NormalizeSave(normalized))
        );
    }

    [Theory]
    [InlineData(20, 14, TargetPreviewKind.FestivalLanternLaunch)]
    [InlineData(9, 14, TargetPreviewKind.FestivalFishBasin)]
    [InlineData(31, 14, TargetPreviewKind.FestivalShop)]
    [InlineData(20, 6, TargetPreviewKind.FestivalTideAltar)]
    public void RealStationsSharePreviewAndWrongToolAtomicity(
        int x,
        int y,
        TargetPreviewKind expectedKind
    )
    {
        var target = new GridPosition(x, y);
        var session = TideSession(target);
        var available = session.PreviewSelectedTarget(target);
        Assert.True(available.IsAvailable);
        Assert.Equal(expectedKind, available.Kind);

        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());
        var preview = session.PreviewSelectedTarget(target);
        var station = FestivalSpatialCatalog.FireflyTide.Stations.Single(
            value => value.Cell == target
        );
        var action = session.CheckFestivalStation(station.Id, target);

        Assert.Equal(TargetPreviewState.NeedsTool, preview.State);
        Assert.False(action.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void WetlandGateAndAllNpcAnchorsAreRealAndReachable()
    {
        Assert.True(WorldDefinition.IsPath(FireflyTideLayout.WorldReturnCell));
        Assert.False(WorldDefinition.IsBlocked(
            FireflyTideLayout.WorldReturnCell
        ));
        Assert.True(WorldDefinition.IsBlocked(
            FireflyTideLayout.WorldEntryCell
        ));

        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(26, 18 * 60);
        session.SetPlayerLocation(
            FireflyTideLayout.WorldReturnCell.X * 16 + 8,
            FireflyTideLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        Assert.True(session.CheckFestivalEntrance(
            FestivalCatalog.FireflyTideFestivalId,
            FireflyTideLayout.WorldEntryCell
        ).Succeeded);

        var states = VillageCatalog.Npcs.Values
            .Select(definition => NpcScheduleSystem.SelectEntry(
                definition,
                26,
                18 * 60,
                WeatherSystem.WeatherForDay(26)
            ))
            .ToArray();
        Assert.All(states, entry =>
        {
            Assert.NotNull(entry);
            Assert.Equal(PlayerLocationIds.FireflyTide, entry.LocationId);
            Assert.True(FireflyTideLayout.IsWalkable(entry.Position));
            Assert.StartsWith("festival.firefly.dialogue.",
                entry.DialogueKey);
        });
        Assert.Equal(
            VillageCatalog.Npcs.Count,
            states.Select(entry => entry!.Position).Distinct().Count()
        );
    }

    private static GameSession TideSession(GridPosition target)
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(26, 18 * 60);
        session.Weather.AdvanceToDay(26);
        session.SetPlayerLocation(
            target.X * 16 + 8,
            (target.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        session.Inventory.Select(0);
        return session;
    }

    private static void Add(GameSession session, params string[] itemIds)
    {
        foreach (var itemId in itemIds)
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
    }
}
