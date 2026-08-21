using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class LongnightSnowWeatherTests
{
    public static TheoryData<int, string> FirstYearLongnightPattern => new()
    {
        { 43, DataCatalog.LongnightSnowWeatherId },
        { 44, DataCatalog.ClearWeatherId },
        { 45, DataCatalog.ClearWeatherId },
        { 46, DataCatalog.StardustWindWeatherId },
        { 47, DataCatalog.LongnightSnowWeatherId },
        { 48, DataCatalog.ClearWeatherId },
        { 49, DataCatalog.ClearWeatherId },
        { 50, DataCatalog.LongnightSnowWeatherId },
        { 51, DataCatalog.ClearWeatherId },
        { 52, DataCatalog.ClearWeatherId },
        { 53, DataCatalog.StardustWindWeatherId },
        { 54, DataCatalog.LongnightSnowWeatherId },
        { 55, DataCatalog.ClearWeatherId },
        { 56, DataCatalog.ClearWeatherId }
    };

    [Theory]
    [MemberData(nameof(FirstYearLongnightPattern))]
    public void LongnightUsesFrozenFourteenDayNaturalPattern(
        int day,
        string expectedWeatherId
    )
    {
        Assert.Equal(expectedWeatherId, WeatherSystem.WeatherForDay(day));
        Assert.Equal(
            expectedWeatherId == DataCatalog.LongnightSnowWeatherId,
            WeatherSystem.IsLongnightSnowDay(day)
        );
        Assert.NotEqual(DataCatalog.RainWeatherId, expectedWeatherId);
    }

    [Theory]
    [InlineData(42, false)]
    [InlineData(43, true)]
    [InlineData(56, false)]
    [InlineData(57, false)]
    [InlineData(99, true)]
    [InlineData(103, true)]
    [InlineData(106, true)]
    [InlineData(110, true)]
    public void SnowDaysWrapByAbsoluteYear(int day, bool expected)
    {
        Assert.Equal(expected, WeatherSystem.IsLongnightSnowDay(day));
    }

    [Fact]
    public void RestorePreservesCurrentWeatherButNormalizesFixedSnowForecast()
    {
        var weather = new WeatherSystem();
        weather.Restore(
            new WeatherSave
            {
                Day = 42,
                CurrentId = DataCatalog.RainWeatherId,
                ForecastId = DataCatalog.RainWeatherId
            },
            42
        );

        Assert.Equal(DataCatalog.RainWeatherId, weather.CurrentId);
        Assert.Equal(DataCatalog.LongnightSnowWeatherId, weather.ForecastId);

        weather.Restore(
            new WeatherSave
            {
                Day = 43,
                CurrentId = DataCatalog.RainWeatherId,
                ForecastId = DataCatalog.RainWeatherId
            },
            43
        );

        Assert.Equal(DataCatalog.RainWeatherId, weather.CurrentId);
        Assert.Equal(DataCatalog.RainWeatherId, weather.ForecastId);
    }

    [Fact]
    public void SnowDefinitionDoesNotWaterAndOwnsOutdoorMovementPenalty()
    {
        var definition = DataCatalog.Weather(
            DataCatalog.LongnightSnowWeatherId
        );

        Assert.False(definition.AutoWatersCrops);
        Assert.Equal(0.85f, definition.OutdoorMovementMultiplier, 3);
        Assert.Equal(
            "weather.longnight_snow.effect",
            definition.EffectKey
        );
    }

    [Fact]
    public void MovementPenaltyOnlyAppliesOutdoorsDuringSnow()
    {
        var session = RestoreDayWithWeather(
            43,
            DataCatalog.LongnightSnowWeatherId
        );

        Assert.Equal(0.85f, session.PlayerMovementMultiplier, 3);

        session.SetPlayerLocation(
            session.PlayerX,
            session.PlayerY,
            PlayerLocationIds.Cottage
        );
        Assert.Equal(1f, session.PlayerMovementMultiplier);

        session.SetPlayerLocation(
            session.PlayerX,
            session.PlayerY,
            PlayerLocationIds.World
        );
        session.Weather.AdvanceToDay(44);
        Assert.Equal(1f, session.PlayerMovementMultiplier);
    }

    [Fact]
    public void SnowNeitherWatersNorCreatesWeatherResonance()
    {
        var session = RestoreDayWithWeather(
            43,
            DataCatalog.LongnightSnowWeatherId
        );
        var target = new GridPosition(22, 16);
        session.Farm.Restore(
        [
            new FarmTileState
            {
                X = target.X,
                Y = target.Y,
                Tilled = true,
                CropId = DataCatalog.DawnlaceId,
                PlantedDay = 42,
                QualityRoll = 0
            }
        ]);

        session.EndDay();

        var tile = session.Farm.Tiles[target];
        Assert.Equal(0, tile.WateredNights);
        Assert.False(tile.Watered);
        Assert.Null(tile.ResonanceItemId);
    }

    [Fact]
    public void SnowRoundTripsThroughSchemaOneWithoutNewFields()
    {
        var source = RestoreDayWithWeather(
            43,
            DataCatalog.LongnightSnowWeatherId
        );
        var json = JsonSerializer.Serialize(source.Capture());
        var save = JsonSerializer.Deserialize<GameSaveV1>(json);
        var restored = new GameSession();

        restored.Restore(save!);

        Assert.Equal(SaveService.CurrentSchemaVersion, save!.SchemaVersion);
        Assert.Equal(DataCatalog.LongnightSnowWeatherId, restored.Weather.CurrentId);
        Assert.Equal(43, restored.Clock.Day);
    }

    private static GameSession RestoreDayWithWeather(
        int day,
        string weatherId
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = WeatherSystem.WeatherForDay(day + 1)
        };
        session.Restore(save);
        return session;
    }
}
