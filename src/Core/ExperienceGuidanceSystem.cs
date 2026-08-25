namespace Luminfield.Core;

public sealed class ExperienceGuidanceSystem
{
    public int LastMorningBriefingDay { get; private set; }

    public event Action? Changed;

    public void Reset()
    {
        LastMorningBriefingDay = 0;
        Changed?.Invoke();
    }

    public void Restore(ExperienceGuidanceSave? save, int currentDay)
    {
        var minimumDay = currentDay <= 1 ? 1 : 0;
        LastMorningBriefingDay = Math.Clamp(
            save?.LastMorningBriefingDay ?? 0,
            minimumDay,
            Math.Max(1, currentDay)
        );
    }

    public bool WasMorningBriefingShown(int day) =>
        day > 0 && LastMorningBriefingDay >= day;

    public bool MarkMorningBriefingShown(int day)
    {
        if (day <= 0 || WasMorningBriefingShown(day))
        {
            return false;
        }

        LastMorningBriefingDay = day;
        Changed?.Invoke();
        return true;
    }

    public ExperienceGuidanceSave Capture() => new()
    {
        LastMorningBriefingDay = LastMorningBriefingDay
    };
}
