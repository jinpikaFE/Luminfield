namespace Luminfield.Core;

public sealed record TwilightEmporiumAccessCheck(
    bool IsOpen,
    string NoticeKey,
    string TargetStatusKey
);

public static class TwilightEmporiumSystem
{
    public const int StockSize = 4;

    public static TwilightEmporiumAccessCheck CheckAccess(
        int day,
        int minuteOfDay
    )
    {
        if (CalendarSystem.WeekdayIndex(day) ==
            CalendarSystem.LanternrestWeekdayIndex)
        {
            return new TwilightEmporiumAccessCheck(
                false,
                "notice.emporium_restday",
                "target.status.emporium_restday"
            );
        }

        var isOpen = minuteOfDay >=
                VillageCatalog.TwilightEmporiumOpenMinute &&
            minuteOfDay < VillageCatalog.TwilightEmporiumCloseMinute;
        if (isOpen)
        {
            return new TwilightEmporiumAccessCheck(true, "", "");
        }

        return new TwilightEmporiumAccessCheck(
            false,
            "notice.emporium_closed",
            "target.status.emporium_closed"
        );
    }

    public static IReadOnlyList<string> StockForDay(int day)
    {
        var weekOffset = CalendarSystem.WeekNumber(day) - 1;
        var availableSeeds = DataCatalog.SeedItemIdsForDay(day);
        var start = CalendarSystem.SeasonIndex(day) * 2 + weekOffset;
        if (CalendarSystem.SeasonId(day) ==
            CalendarSystem.GleamriseSeasonId)
        {
            var gleamriseCropCount = 4;
            start = availableSeeds.Count - gleamriseCropCount + weekOffset * 2;
        }

        start %= availableSeeds.Count;
        var stock = new string[StockSize];
        for (var index = 0; index < StockSize; index++)
        {
            stock[index] = availableSeeds[
                (start + index) % availableSeeds.Count
            ];
        }

        return stock;
    }

    public static bool IsStocked(int day, string itemId) =>
        StockForDay(day).Contains(itemId, StringComparer.Ordinal);
}
