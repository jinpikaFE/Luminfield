using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class LongnightLanternFeastTests
{
    public static TheoryData<string, string, int, string, int> DishPairs =>
        new()
        {
            {
                DataCatalog.SunvaultHashId,
                DataCatalog.MoonmistStewId,
                16,
                FestivalCatalog.LongnightHearthsidePlaceAwardId,
                4
            },
            {
                DataCatalog.SunvaultHashId,
                DataCatalog.LanternrootBrothId,
                16,
                FestivalCatalog.LongnightHearthsidePlaceAwardId,
                4
            },
            {
                DataCatalog.SunvaultHashId,
                DataCatalog.StarhoneyCustardId,
                17,
                FestivalCatalog.LongnightSharedGlowWreathAwardId,
                7
            },
            {
                DataCatalog.MoonmistStewId,
                DataCatalog.LanternrootBrothId,
                18,
                FestivalCatalog.LongnightSharedGlowWreathAwardId,
                7
            },
            {
                DataCatalog.MoonmistStewId,
                DataCatalog.StarhoneyCustardId,
                19,
                FestivalCatalog.LongnightStarwardHostAwardId,
                10
            },
            {
                DataCatalog.LanternrootBrothId,
                DataCatalog.StarhoneyCustardId,
                19,
                FestivalCatalog.LongnightStarwardHostAwardId,
                10
            }
        };

    [Theory]
    [InlineData(54, 17 * 60, false)]
    [InlineData(55, 16 * 60 + 59, false)]
    [InlineData(55, 17 * 60, true)]
    [InlineData(55, 21 * 60 + 59, true)]
    [InlineData(55, 22 * 60, false)]
    [InlineData(56, 17 * 60, false)]
    [InlineData(111, 17 * 60, true)]
    public void FeastUsesAnnualLongnightDayThirteenWindow(
        int day,
        int minute,
        bool expected
    ) => Assert.Equal(
        expected,
        FestivalCatalog.IsOpen(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            day,
            minute
        )
    );

    [Theory]
    [MemberData(nameof(DishPairs))]
    public void SixDishPairsUseFrozenScoresAndAwards(
        string first,
        string second,
        int expectedScore,
        string expectedAward,
        int expectedKnots
    )
    {
        var session = FeastSession(LongnightLanternFeastLayout.SharedTableCell);
        Add(session, first, second, DataCatalog.StarbudPreserveId);

        var preview = session.CheckLongnightFeastParticipation(
            LongnightLanternFeastLayout.SharedTableCell,
            [first, second],
            FestivalCatalog.LongnightStarbudPreserveExchangeId
        );

        Assert.True(preview.CanComplete);
        Assert.Equal(expectedScore, preview.Score);
        Assert.Equal(expectedAward, preview.AwardId);
        Assert.Equal(expectedKnots, preview.LanternKnotReward);
    }

    [Fact]
    public void RiteAtomicallySettlesDishesGiftRewardAndAnnualResult()
    {
        var session = FeastSession(LongnightLanternFeastLayout.RitualCell);
        Add(
            session,
            DataCatalog.MoonmistStewId,
            DataCatalog.StarhoneyCustardId,
            DataCatalog.CloudleafTeaId
        );
        var starlightBefore = JsonSerializer.Serialize(
            session.Starlight.Capture()
        );

        var result = session.CompleteLongnightFeast(
            LongnightLanternFeastLayout.RitualCell,
            [DataCatalog.MoonmistStewId, DataCatalog.StarhoneyCustardId],
            FestivalCatalog.LongnightCloudleafTeaExchangeId
        );

        Assert.True(result.Succeeded);
        Assert.Equal(19, result.Result?.Score);
        Assert.Equal(10, session.Festival.LanternKnots);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonmistStewId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.StarhoneyCustardId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CloudleafTeaId));
        Assert.Equal(2, session.Inventory.Count(
            DataCatalog.StarsoilFertilizerId
        ));
        Assert.Equal(
            FestivalCatalog.LongnightStarlightRiteId,
            result.Result?.RitualId
        );
        Assert.Equal(
            starlightBefore,
            JsonSerializer.Serialize(session.Starlight.Capture())
        );
    }

    [Theory]
    [InlineData("one")]
    [InlineData("duplicate")]
    [InlineData("ineligible")]
    [InlineData("gift_missing")]
    [InlineData("already_done")]
    public void InvalidRitePathsKeepCompleteSnapshotUnchanged(string mode)
    {
        var session = FeastSession(LongnightLanternFeastLayout.SharedTableCell);
        Add(
            session,
            DataCatalog.MoonmistStewId,
            DataCatalog.StarhoneyCustardId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.AuricShootId
        );
        if (mode == "already_done")
        {
            Assert.True(session.CompleteLongnightFeast(
                LongnightLanternFeastLayout.SharedTableCell,
                [DataCatalog.MoonmistStewId, DataCatalog.StarhoneyCustardId],
                FestivalCatalog.LongnightStarbudPreserveExchangeId
            ).Succeeded);
        }
        var before = JsonSerializer.Serialize(session.Capture());
        var dishes = mode switch
        {
            "one" => new[] { DataCatalog.MoonmistStewId },
            "duplicate" => new[]
            {
                DataCatalog.MoonmistStewId,
                DataCatalog.MoonmistStewId
            },
            "ineligible" => new[]
            {
                DataCatalog.MoonmistStewId,
                DataCatalog.AuricShootId
            },
            _ => new[]
            {
                DataCatalog.MoonmistStewId,
                DataCatalog.StarhoneyCustardId
            }
        };
        var exchangeId = mode == "gift_missing"
            ? FestivalCatalog.LongnightMoonrootTonicExchangeId
            : FestivalCatalog.LongnightStarbudPreserveExchangeId;

        var result = session.CompleteLongnightFeast(
            LongnightLanternFeastLayout.SharedTableCell,
            dishes,
            exchangeId
        );

        Assert.False(result.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void FullBackpackRejectsReturnGiftBeforeAnyRemoval()
    {
        var session = FeastSession(LongnightLanternFeastLayout.SharedTableCell);
        Assert.True(session.Inventory.Add(DataCatalog.MoonmistStewId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.StarhoneyCustardId, 2));
        Assert.True(session.Inventory.Add(DataCatalog.StarbudPreserveId, 2));
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(id => id is not (
                         DataCatalog.MoonmistStewId or
                         DataCatalog.StarhoneyCustardId or
                         DataCatalog.StarbudPreserveId or
                         DataCatalog.MeadowFodderId))
                     .Distinct(StringComparer.Ordinal))
        {
            if (!session.Inventory.Add(itemId, 1))
            {
                break;
            }
        }
        var before = JsonSerializer.Serialize(session.Capture());

        var result = session.CompleteLongnightFeast(
            LongnightLanternFeastLayout.SharedTableCell,
            [DataCatalog.MoonmistStewId, DataCatalog.StarhoneyCustardId],
            FestivalCatalog.LongnightStarbudPreserveExchangeId
        );

        Assert.False(result.Succeeded);
        Assert.Equal(
            "festival.longnight.activity.backpack_full",
            result.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void LanternStallKeepsAllThreeFestivalCurrenciesIndependent()
    {
        var session = FeastSession(LongnightLanternFeastLayout.StallCell);
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
                    Balance = 3
                }
            ]
        });

        var bought = session.BuyLongnightStallItem(
            LongnightLanternFeastLayout.StallCell,
            FestivalCatalog.LongnightFodderBundleOfferId
        );

        Assert.True(bought.Succeeded);
        Assert.Equal(6, session.Inventory.Count(DataCatalog.MeadowFodderId));
        Assert.Equal(0, session.Festival.LanternKnots);
        Assert.Equal(6, session.Festival.BloomTokens);
        Assert.Equal(8, session.Festival.Scrip);
    }

    [Fact]
    public void SaveNormalizationRequiresTwoDishesGiftMappingAndRitual()
    {
        var normalized = FestivalSystem.NormalizeSave(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.LongnightLanternKnotId,
                    Balance = 7
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
                    FestivalId =
                        FestivalCatalog.LongnightLanternFeastFestivalId,
                    Year = 1,
                    ItemIds =
                    [
                        DataCatalog.StarhoneyCustardId,
                        DataCatalog.MoonmistStewId
                    ],
                    Score = 999,
                    AwardId = "broken",
                    AuctionCoins = 999,
                    GiftItemId = DataCatalog.CloudleafTeaId,
                    GiftRewardItemId = DataCatalog.StarsoilFertilizerId,
                    RitualId = FestivalCatalog.LongnightStarlightRiteId
                },
                new FestivalYearResultSave
                {
                    FestivalId =
                        FestivalCatalog.LongnightLanternFeastFestivalId,
                    Year = 2,
                    ItemIds =
                    [
                        DataCatalog.MoonmistStewId,
                        DataCatalog.SunvaultHashId
                    ]
                }
            ]
        });

        var result = Assert.Single(normalized.Results);
        Assert.Equal(19, result.Score);
        Assert.Equal(FestivalCatalog.LongnightStarwardHostAwardId,
            result.AwardId);
        Assert.Equal(0, result.AuctionCoins);
        Assert.Equal(7, Assert.Single(normalized.CurrencyBalances).Balance);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(FestivalSystem.NormalizeSave(normalized))
        );
    }

    [Theory]
    [InlineData(20, 14, TargetPreviewKind.FestivalFeastTable)]
    [InlineData(9, 14, TargetPreviewKind.FestivalGiftExchange)]
    [InlineData(31, 14, TargetPreviewKind.FestivalShop)]
    [InlineData(20, 6, TargetPreviewKind.FestivalRitual)]
    public void AllRealStationsSharePreviewAndAtomicToolRules(
        int x,
        int y,
        TargetPreviewKind kind
    )
    {
        var target = new GridPosition(x, y);
        var session = FeastSession(target);
        var available = session.PreviewSelectedTarget(target);
        Assert.Equal(kind, available.Kind);
        Assert.True(available.IsAvailable);

        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());
        var wrongTool = session.PreviewSelectedTarget(target);
        var action = session.CheckFestivalStation(
            FestivalSpatialCatalog.LongnightLanternFeast.Stations
                .Single(station => station.Cell == target).Id,
            target
        );

        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(action.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void EightVillagersUseUniqueWalkableLongnightAnchors()
    {
        var states = VillageCatalog.Npcs.Values
            .Select(definition => NpcScheduleSystem.SelectEntry(
                definition,
                55,
                17 * 60,
                WeatherSystem.WeatherForDay(55)
            ))
            .ToArray();

        Assert.All(states, entry =>
        {
            Assert.NotNull(entry);
            Assert.Equal(
                PlayerLocationIds.LongnightLanternFeast,
                entry.LocationId
            );
            Assert.True(LongnightLanternFeastLayout.IsWalkable(
                entry.Position
            ));
            Assert.StartsWith("festival.longnight.dialogue.",
                entry.DialogueKey);
        });
        Assert.Equal(
            VillageCatalog.Npcs.Count,
            states.Select(entry => entry!.Position).Distinct().Count()
        );
    }

    private static GameSession FeastSession(GridPosition target)
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(55, 17 * 60);
        session.Weather.AdvanceToDay(55);
        session.SetPlayerLocation(
            target.X * 16 + 8,
            (target.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
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
