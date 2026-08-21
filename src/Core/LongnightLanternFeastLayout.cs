namespace Luminfield.Core;

public static class LongnightLanternFeastLayout
{
    public const int Width = 40;
    public const int Height = 22;

    public static readonly GridPosition WorldEntryCell = new(97, 59);
    public static readonly GridPosition WorldReturnCell = new(97, 58);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 19);
    public static readonly GridPosition SharedTableCell = new(20, 14);
    public static readonly GridPosition GiftExchangeCell = new(9, 14);
    public static readonly GridPosition StallCell = new(31, 14);
    public static readonly GridPosition RitualCell = new(20, 6);

    public static readonly IReadOnlyDictionary<string, GridPosition>
        NpcAnchors = new Dictionary<string, GridPosition>(StringComparer.Ordinal)
        {
            [VillageCatalog.LioraId] = new(12, 8),
            [VillageCatalog.TaviId] = new(28, 8),
            [VillageCatalog.NemiId] = new(7, 10),
            [VillageCatalog.SelaId] = new(33, 10),
            [VillageCatalog.ElowenId] = new(12, 17),
            [VillageCatalog.VessaId] = new(28, 17),
            [VillageCatalog.OrinId] = new(7, 16),
            [VillageCatalog.KaelId] = new(33, 16),
            [VillageCatalog.HaldenId] = new(5, 7),
            [VillageCatalog.MaveaId] = new(35, 7),
            [VillageCatalog.SivrenId] = new(4, 13),
            [VillageCatalog.DorrikId] = new(36, 13),
            [VillageCatalog.YvaraId] = new(10, 7),
            [VillageCatalog.BrialId] = new(30, 7),
            [VillageCatalog.PavriId] = new(10, 18),
            [VillageCatalog.RovenId] = new(30, 18)
        };

    public static bool IsInBounds(GridPosition cell) =>
        cell.X is >= 2 and <= 37 && cell.Y is >= 3 and <= 20;

    public static bool IsWalkable(GridPosition cell)
    {
        if (!IsInBounds(cell))
        {
            return false;
        }

        if (cell == ExitCell)
        {
            return true;
        }

        if (cell == SharedTableCell || cell == GiftExchangeCell ||
            cell == StallCell || cell == RitualCell)
        {
            return false;
        }

        var ritualPlatform = cell.X is >= 14 and <= 26 &&
            cell.Y is >= 3 and <= 6;
        return !ritualPlatform;
    }
}
