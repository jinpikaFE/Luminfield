namespace Luminfield.Core;

public sealed class GameClock
{
    public const int StartMinute = 6 * 60;
    public const int EndMinute = 22 * 60;
    public const int MinutesPerTick = 10;
    public const double SecondsPerTick = 2.5;

    private double _accumulator;

    public int Day { get; private set; } = 1;
    public int MinuteOfDay { get; private set; } = StartMinute;
    public bool EndOfDayReached => MinuteOfDay >= EndMinute;

    public event Action? TimeChanged;
    public event Action? EndOfDayRequested;

    public void Reset(int day = 1, int minuteOfDay = StartMinute)
    {
        Day = Math.Max(1, day);
        MinuteOfDay = Math.Clamp(minuteOfDay, StartMinute, EndMinute);
        _accumulator = 0;
        TimeChanged?.Invoke();
    }

    public bool AdvanceRealTime(double deltaSeconds)
    {
        if (deltaSeconds <= 0 || EndOfDayReached)
        {
            return false;
        }

        _accumulator += deltaSeconds;
        var changed = false;
        while (_accumulator >= SecondsPerTick && !EndOfDayReached)
        {
            _accumulator -= SecondsPerTick;
            MinuteOfDay = Math.Min(EndMinute, MinuteOfDay + MinutesPerTick);
            changed = true;
            TimeChanged?.Invoke();
        }

        if (changed && EndOfDayReached)
        {
            EndOfDayRequested?.Invoke();
        }

        return changed;
    }

    public void StartNextDay()
    {
        Day++;
        MinuteOfDay = StartMinute;
        _accumulator = 0;
        TimeChanged?.Invoke();
    }

    public string DisplayTime
    {
        get
        {
            var hours = MinuteOfDay / 60;
            var minutes = MinuteOfDay % 60;
            return $"{hours:00}:{minutes:00}";
        }
    }
}
