namespace Luminfield.Core;

public static class CalendarSystem
{
    public const int DaysPerWeek = 7;

    public static int WeekNumber(int day) =>
        (Math.Max(1, day) - 1) / DaysPerWeek + 1;

    public static int WeekdayIndex(int day) =>
        (Math.Max(1, day) - 1) % DaysPerWeek;

    public static string WeekdayKey(int day) =>
        $"calendar.weekday.{WeekdayIndex(day) + 1}";
}

public sealed class WeatherSystem
{
    private static readonly string[] WeeklyPattern =
    [
        DataCatalog.ClearWeatherId,
        DataCatalog.RainWeatherId,
        DataCatalog.ClearWeatherId,
        DataCatalog.StardustWindWeatherId,
        DataCatalog.ClearWeatherId,
        DataCatalog.RainWeatherId,
        DataCatalog.ClearWeatherId
    ];

    public int Day { get; private set; } = 1;
    public string CurrentId { get; private set; } = DataCatalog.ClearWeatherId;
    public string ForecastId { get; private set; } = DataCatalog.RainWeatherId;
    public WeatherDefinition Current => DataCatalog.Weather(CurrentId);
    public WeatherDefinition Forecast => DataCatalog.Weather(ForecastId);

    public event Action? Changed;

    public void Reset(int day = 1)
    {
        SetDay(day);
    }

    public void Restore(WeatherSave? save, int currentDay)
    {
        var day = Math.Max(1, currentDay);
        Day = day;
        CurrentId = save?.Day == day && IsKnown(save.CurrentId)
            ? save.CurrentId
            : WeatherForDay(day);
        ForecastId = save?.Day == day && IsKnown(save.ForecastId)
            ? save.ForecastId
            : WeatherForDay(day + 1);
        Changed?.Invoke();
    }

    public void AdvanceToDay(int day)
    {
        SetDay(day);
    }

    public WeatherSave Capture() => new()
    {
        Day = Day,
        CurrentId = CurrentId,
        ForecastId = ForecastId
    };

    public static string WeatherForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        var week = (normalizedDay - 1) / CalendarSystem.DaysPerWeek;
        var index = (normalizedDay - 1 + week * 2) % WeeklyPattern.Length;
        return WeeklyPattern[index];
    }

    private void SetDay(int day)
    {
        Day = Math.Max(1, day);
        CurrentId = WeatherForDay(Day);
        ForecastId = WeatherForDay(Day + 1);
        Changed?.Invoke();
    }

    private static bool IsKnown(string? weatherId) =>
        !string.IsNullOrWhiteSpace(weatherId) &&
        DataCatalog.WeatherDefinitions.ContainsKey(weatherId);
}
