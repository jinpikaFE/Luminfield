namespace Luminfield.Core;

public static class FireflyTideLayout
{
    public const int Width = 40;
    public const int Height = 22;

    // Keep the festival layout self-contained. Referencing WorldDefinition here
    // creates a static initialization cycle through VillageCatalog festival slots.
    public static readonly GridPosition WorldEntryCell = new(226, 70);
    public static readonly GridPosition WorldReturnCell = new(225, 70);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 19);
    public static readonly GridPosition LanternLaunchCell = new(20, 14);
    public static readonly GridPosition FishBasinCell = new(9, 14);
    public static readonly GridPosition ShopCell = new(31, 14);
    public static readonly GridPosition TideAltarCell = new(20, 6);

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

        return cell != LanternLaunchCell &&
            cell != FishBasinCell &&
            cell != ShopCell &&
            cell != TideAltarCell;
    }
}
