using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class GleamrisePlantingFestivalTests
{
    private static readonly string[] Seeds =
    [
        DataCatalog.DawnlaceSeedId,
        DataCatalog.GlimmerpodSeedId,
        DataCatalog.MistsongMintSeedId
    ];

    [Theory]
    [InlineData(3, 540, false)]
    [InlineData(4, 539, false)]
    [InlineData(4, 540, true)]
    [InlineData(4, 1019, true)]
    [InlineData(4, 1020, false)]
    [InlineData(5, 540, false)]
    [InlineData(60, 540, true)]
    public void PlantingFestivalUsesAnnualGleamriseDayFourWindow(
        int day,
        int minute,
        bool expected
    ) => Assert.Equal(
        expected,
        FestivalCatalog.IsOpen(
            FestivalCatalog.GleamrisePlantingFestivalId,
            day,
            minute
        )
    );

    [Fact]
    public void FrozenPlantingScoresMatchProductExamples()
    {
        var maximum = Pattern([
            0, 1, 2, 0,
            1, 2, 0, 1,
            2, 0, 1, 2
        ]);
        var noHarmony = Pattern([
            0, 0, 0, 0,
            1, 1, 1, 1,
            2, 2, 2, 2
        ]);
        var partial = Pattern([
            0, 1, 2, 0,
            1, 2, 0, 1
        ]);

        Assert.Equal(30, FestivalCatalog.GleamrisePlantingScore(
            maximum,
            120
        ));
        Assert.Equal(20, FestivalCatalog.GleamrisePlantingScore(
            noHarmony,
            180
        ));
        Assert.Equal(18, FestivalCatalog.GleamrisePlantingScore(
            partial,
            180
        ));
    }

    [Fact]
    public void ChallengeUsesTemporarySeedsAndCompletesAtomically()
    {
        var festival = new FestivalSystem();
        festival.Reset();
        var started = festival.StartPlantingChallenge(1, 9 * 60, Seeds);
        Assert.True(started.Succeeded);

        for (var index = 0;
             index < GleamrisePlantingFestivalLayout.PlotIds.Count;
             index++)
        {
            var seedId = Seeds[index % Seeds.Length];
            Assert.True(festival.SelectPlantingSeed(1, seedId).Succeeded);
            var planted = festival.PlantPlot(
                1,
                10 * 60,
                GleamrisePlantingFestivalLayout.PlotIds[index]
            );
            Assert.True(planted.Succeeded);
            Assert.Equal(index == 11, planted.Completed);
        }

        var result = festival.ResultFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        );
        Assert.NotNull(result);
        Assert.Equal(30, result.Score);
        Assert.Equal(
            FestivalCatalog.GleamriseStarfieldCrownAwardId,
            result.AwardId
        );
        Assert.Equal(10, festival.BloomTokens);
        Assert.Null(festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        ));
    }

    [Fact]
    public void InvalidPlantingAndExpiredEmptyAttemptAreAtomic()
    {
        var festival = new FestivalSystem();
        festival.Reset();
        Assert.True(festival.StartPlantingChallenge(
            1,
            9 * 60,
            Seeds
        ).Succeeded);
        var before = JsonSerializer.Serialize(festival.Capture());

        var unknown = festival.PlantPlot(1, 9 * 60, "unknown");
        Assert.False(unknown.Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(festival.Capture()));

        var resolved = festival.ResolvePlantingAttempt(1, 12 * 60);
        Assert.True(resolved.Succeeded);
        Assert.False(resolved.Completed);
        Assert.Null(festival.ResultFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        ));
        Assert.Equal(0, festival.BloomTokens);
    }

    [Fact]
    public void BloomTokenExchangeKeepsStarharvestScripSeparate()
    {
        var festival = new FestivalSystem();
        festival.Restore(new FestivalSave
        {
            Scrip = 7,
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                    Balance = 3
                }
            ]
        });
        var inventory = new Inventory();
        inventory.Reset();

        var purchased = festival.PurchaseGleamriseSeeds(
            FestivalCatalog.GleamriseGlimmerpodOfferId,
            inventory
        );

        Assert.True(purchased.Succeeded);
        Assert.Equal(2, inventory.Count(DataCatalog.GlimmerpodSeedId));
        Assert.Equal(0, festival.BloomTokens);
        Assert.Equal(7, festival.Scrip);
    }

    [Fact]
    public void SaveNormalizationKeepsTwoFestivalPortfoliosIndependent()
    {
        var save = new FestivalSave
        {
            Scrip = 5,
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                    Balance = 8
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
                    FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 1,
                    Score = 21,
                    AwardId = FestivalCatalog.SilverSheafAwardId,
                    ItemIds = [DataCatalog.AuricShootId]
                }
            ],
            PlantingAttempts =
            [
                new FestivalPlantingAttemptSave
                {
                    FestivalId = FestivalCatalog.GleamrisePlantingFestivalId,
                    Year = 1,
                    StartedMinute = 9 * 60,
                    SelectedSeedItemIds = Seeds.ToList(),
                    ActiveSeedItemId = Seeds[1],
                    Plantings =
                    [
                        new FestivalPlotPlantingSave
                        {
                            PlotId = GleamrisePlantingFestivalLayout.PlotIds[0],
                            SeedItemId = Seeds[0]
                        },
                        new FestivalPlotPlantingSave
                        {
                            PlotId = "unknown",
                            SeedItemId = Seeds[1]
                        }
                    ]
                }
            ]
        };

        var normalized = FestivalSystem.NormalizeSave(save);

        Assert.Equal(5, normalized.Scrip);
        Assert.Equal(8, Assert.Single(normalized.CurrencyBalances).Balance);
        Assert.Single(normalized.Results);
        Assert.Single(Assert.Single(normalized.PlantingAttempts).Plantings);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(FestivalSystem.NormalizeSave(normalized))
        );
    }

    [Fact]
    public void EntryExitPreviewUsesSharedWorldGateAndRealFestivalExit()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(4, 9 * 60);
        session.Weather.AdvanceToDay(4);
        session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.WorldReturnCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

        var entry = session.PreviewSelectedTarget(
            GleamrisePlantingFestivalLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewKind.FestivalPortal, entry.Kind);
        Assert.True(entry.IsAvailable);

        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());
        var wrongTool = session.PreviewSelectedTarget(
            GleamrisePlantingFestivalLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(session.TryEnterFestival(
            FestivalCatalog.GleamrisePlantingFestivalId,
            GleamrisePlantingFestivalLayout.WorldEntryCell
        ).Succeeded);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        var exit = session.PreviewSelectedTarget(
            GleamrisePlantingFestivalLayout.ExitCell
        );
        Assert.Equal(TargetPreviewKind.FestivalExit, exit.Kind);
        Assert.True(exit.IsAvailable);
        Assert.True(session.TryExitFestival(
            GleamrisePlantingFestivalLayout.ExitCell
        ).Succeeded);
    }

    [Theory]
    [InlineData(9, 14, TargetPreviewKind.FestivalSeedRack)]
    [InlineData(31, 14, TargetPreviewKind.FestivalSeedExchange)]
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
            PlayerLocationIds.GleamrisePlantingFestival
        );

        var available = session.PreviewSelectedTarget(target);
        Assert.Equal(expectedKind, available.Kind);
        Assert.True(available.IsAvailable);

        session.Inventory.Select(1);
        var before = JsonSerializer.Serialize(session.Capture());
        var wrongTool = session.PreviewSelectedTarget(target);
        var station = expectedKind == TargetPreviewKind.FestivalSeedExchange
            ? FestivalCatalog.GleamriseSeedExchangeId
            : FestivalCatalog.GleamriseSharedBloomfieldActivityId;
        var action = session.CheckFestivalStation(station, target);

        Assert.Equal(expectedKind, wrongTool.Kind);
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(action.Succeeded);
        Assert.Equal("notice.needs_hand", action.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void RealPlotPlantingConsumesNoInventoryEnergyOrWater()
    {
        var session = FestivalSession();
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        Assert.True(session.StartGleamriseChallenge(Seeds).Succeeded);

        var plot = GleamrisePlantingFestivalLayout.PlotCells[0];
        session.SetPlayerLocation(
            plot.X * 16 + 8,
            (plot.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        var inventoryBefore = JsonSerializer.Serialize(
            session.Inventory.Capture()
        );
        var energyBefore = session.Energy;
        var waterBefore = session.WateringCanWater;

        var preview = session.PreviewSelectedTarget(plot);
        var planted = session.PlantGleamrisePlot(plot);

        Assert.Equal(TargetPreviewKind.FestivalPlantingPlot, preview.Kind);
        Assert.True(preview.IsAvailable);
        Assert.True(planted.Succeeded);
        Assert.False(planted.Completed);
        Assert.Equal(inventoryBefore, JsonSerializer.Serialize(
            session.Inventory.Capture()
        ));
        Assert.Equal(energyBefore, session.Energy);
        Assert.Equal(waterBefore, session.WateringCanWater);
        Assert.Single(session.Festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        )!.Plantings);
    }

    [Fact]
    public void RestoredExpiredAttemptResolvesAndReturnsFromClosedScene()
    {
        var session = FestivalSession();
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        Assert.True(session.StartGleamriseChallenge(Seeds).Succeeded);
        var plot = GleamrisePlantingFestivalLayout.PlotCells[0];
        session.SetPlayerLocation(
            plot.X * 16 + 8,
            (plot.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        Assert.True(session.PlantGleamrisePlot(plot).Succeeded);

        var save = session.Capture();
        save.MinuteOfDay = 17 * 60;
        var restored = new GameSession();
        restored.NewGame();
        restored.Restore(save);

        Assert.Null(restored.Festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        ));
        var result = restored.Festival.ResultFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            1
        );
        Assert.NotNull(result);
        Assert.Single(result.Plantings);
        Assert.Equal(PlayerLocationIds.World, restored.PlayerLocationId);
        Assert.Equal(
            GleamrisePlantingFestivalLayout.WorldReturnCell,
            restored.PlayerCell
        );
    }

    [Fact]
    public void AllEightVillagersUseGleamriseDialogueAndSafeUniqueAnchors()
    {
        var entries = VillageCatalog.Npcs.Values
            .Select(definition => NpcScheduleSystem.SelectEntry(
                definition,
                4,
                9 * 60,
                WeatherSystem.WeatherForDay(4)
            ))
            .ToArray();

        Assert.All(entries, entry =>
        {
            Assert.NotNull(entry);
            Assert.Equal(
                PlayerLocationIds.GleamrisePlantingFestival,
                entry.LocationId
            );
            Assert.Equal(VillageCatalog.FestivalSchedulePriority, entry.Priority);
            Assert.True(GleamrisePlantingFestivalLayout.IsWalkable(
                entry.Position
            ));
            Assert.StartsWith("festival.gleamrise.dialogue.", entry.DialogueKey);
        });
        Assert.Equal(
            VillageCatalog.Npcs.Count,
            entries.Select(entry => entry!.Position).Distinct().Count()
        );

        var runtime = new VillageSystem().CurrentNpcs(
            4,
            9 * 60,
            PlayerLocationIds.GleamrisePlantingFestival,
            GleamrisePlantingFestivalLayout.SafeArrivalCell
        );
        Assert.Equal(VillageCatalog.Npcs.Count, runtime.Count);
        Assert.Equal(
            GleamrisePlantingFestivalLayout.NpcAnchors.Values.OrderBy(
                position => position.X
            ).ThenBy(position => position.Y),
            runtime.Select(state => state.Position).OrderBy(
                position => position.X
            ).ThenBy(position => position.Y)
        );
    }

    private static List<FestivalPlotPlantingSave> Pattern(
        IReadOnlyList<int> seedIndexes
    ) => seedIndexes.Select((seedIndex, plotIndex) =>
        new FestivalPlotPlantingSave
        {
            PlotId = GleamrisePlantingFestivalLayout.PlotIds[plotIndex],
            SeedItemId = Seeds[seedIndex]
        }).ToList();

    private static GameSession FestivalSession()
    {
        var session = new GameSession();
        session.NewGame();
        session.Clock.Reset(4, 9 * 60);
        session.Weather.AdvanceToDay(4);
        session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        return session;
    }
}
