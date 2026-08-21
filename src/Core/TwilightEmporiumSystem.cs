namespace Luminfield.Core;

public sealed record TwilightEmporiumAccessCheck(
    bool IsOpen,
    string NoticeKey,
    string TargetStatusKey
);

public static class TwilightEmporiumSystem
{
    public const int StockSize = 4;
    public const string MeadowFodderOfferId = "emporium_meadow_fodder";

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
        var weekOffset = (CalendarSystem.SeasonDay(day) - 1) /
            CalendarSystem.DaysPerWeek;
        var seasonId = CalendarSystem.SeasonId(day);
        if (seasonId == CalendarSystem.LongnightSeasonId)
        {
            var longnightStart = weekOffset * StockSize;
            return Enumerable.Range(0, StockSize)
                .Select(index => DataCatalog.LongnightGreenhouseSeedItemIds[
                    (longnightStart + index) %
                    DataCatalog.LongnightGreenhouseSeedItemIds.Count
                ])
                .ToArray();
        }

        var availableSeeds = DataCatalog.SeedItemIdsForDay(day);
        var start = CalendarSystem.SeasonIndex(day) * 2 + weekOffset;
        var seasonalSeeds = availableSeeds.Where(itemId =>
        {
            var cropId = DataCatalog.Item(itemId).CropId;
            return cropId is not null &&
                DataCatalog.Crop(cropId).SeasonIds?.Contains(
                    seasonId,
                    StringComparer.Ordinal
                ) == true;
        }).ToArray();
        if (seasonalSeeds.Length >= StockSize)
        {
            availableSeeds = seasonalSeeds;
            start = weekOffset * 2;
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
