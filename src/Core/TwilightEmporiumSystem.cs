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
        var seasonOffset = CalendarSystem.SeasonIndex(day) * 2;
        var weekOffset = CalendarSystem.WeekNumber(day) - 1;
        var start = (seasonOffset + weekOffset) %
            DataCatalog.SeedItemIds.Count;
        var stock = new string[StockSize];
        for (var index = 0; index < StockSize; index++)
        {
            stock[index] = DataCatalog.SeedItemIds[
                (start + index) % DataCatalog.SeedItemIds.Count
            ];
        }

        return stock;
    }

    public static bool IsStocked(int day, string itemId) =>
        StockForDay(day).Contains(itemId, StringComparer.Ordinal);
}
