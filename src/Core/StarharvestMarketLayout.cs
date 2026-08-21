namespace Luminfield.Core;

public static class StarharvestMarketLayout
{
    public const int Width = 40;
    public const int Height = 22;

    public static readonly GridPosition WorldEntryCell =
        new(97, 59);
    public static readonly GridPosition WorldReturnCell = new(97, 58);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 19);
    public static readonly GridPosition ExhibitCell = new(20, 14);
    public static readonly GridPosition BidBoardCell = new(9, 14);
    public static readonly GridPosition ShopCell = new(31, 14);

    public static readonly IReadOnlyDictionary<string, GridPosition>
        NpcAnchors = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [VillageCatalog.LioraId] = new(20, 8),
            [VillageCatalog.TaviId] = new(13, 10),
            [VillageCatalog.NemiId] = new(27, 10),
            [VillageCatalog.SelaId] = new(7, 9),
            [VillageCatalog.ElowenId] = new(33, 9),
            [VillageCatalog.VessaId] = new(13, 17),
            [VillageCatalog.OrinId] = new(33, 16),
            [VillageCatalog.KaelId] = new(27, 17),
            [VillageCatalog.HaldenId] = new(5, 15),
            [VillageCatalog.MaveaId] = new(35, 15),
            [VillageCatalog.SivrenId] = new(5, 7),
            [VillageCatalog.DorrikId] = new(35, 7),
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

        if (cell == ExhibitCell || cell == BidBoardCell || cell == ShopCell)
        {
            return false;
        }

        var judgePlatform = cell.X is >= 14 and <= 26 &&
            cell.Y is >= 3 and <= 6;
        return !judgePlatform;
    }
}
