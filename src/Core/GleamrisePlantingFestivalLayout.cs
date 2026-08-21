namespace Luminfield.Core;

public static class GleamrisePlantingFestivalLayout
{
    public const int Width = 40;
    public const int Height = 22;

    public static readonly GridPosition WorldEntryCell = new(97, 59);
    public static readonly GridPosition WorldReturnCell = new(97, 58);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 19);
    public static readonly GridPosition ActivityTableCell = new(9, 14);
    public static readonly GridPosition SeedExchangeCell = new(31, 14);
    public static readonly GridPosition FieldAnchorCell = new(18, 12);

    public static readonly IReadOnlyList<GridPosition> PlotCells =
    [
        new(15, 8), new(17, 8), new(19, 8), new(21, 8),
        new(15, 10), new(17, 10), new(19, 10), new(21, 10),
        new(15, 12), new(17, 12), new(19, 12), new(21, 12)
    ];

    public static readonly IReadOnlyList<string> PlotIds =
        Enumerable.Range(1, 12)
            .Select(index => $"gleamrise_planting_plot_{index:00}")
            .ToArray();

    public static readonly IReadOnlyDictionary<string, GridPosition>
        PlotCellsById = PlotIds
            .Select((id, index) => (id, Cell: PlotCells[index]))
            .ToDictionary(entry => entry.id, entry => entry.Cell,
                StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<GridPosition, string>
        PlotIdsByCell = PlotCellsById.ToDictionary(
            entry => entry.Value,
            entry => entry.Key
        );

    public static readonly IReadOnlyDictionary<string, GridPosition>
        NpcAnchors = new Dictionary<string, GridPosition>(StringComparer.Ordinal)
        {
            [VillageCatalog.LioraId] = new(20, 6),
            [VillageCatalog.TaviId] = new(11, 8),
            [VillageCatalog.NemiId] = new(29, 8),
            [VillageCatalog.SelaId] = new(7, 11),
            [VillageCatalog.ElowenId] = new(33, 11),
            [VillageCatalog.VessaId] = new(11, 17),
            [VillageCatalog.OrinId] = new(29, 17),
            [VillageCatalog.KaelId] = new(25, 6),
            [VillageCatalog.HaldenId] = new(5, 7),
            [VillageCatalog.MaveaId] = new(35, 7),
            [VillageCatalog.SivrenId] = new(5, 15),
            [VillageCatalog.DorrikId] = new(35, 15),
            [VillageCatalog.YvaraId] = new(10, 6),
            [VillageCatalog.BrialId] = new(30, 6),
            [VillageCatalog.PavriId] = new(9, 18),
            [VillageCatalog.RovenId] = new(31, 18)
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

        if (cell == ActivityTableCell || cell == SeedExchangeCell ||
            PlotIdsByCell.ContainsKey(cell))
        {
            return false;
        }

        var ceremonyPlatform = cell.X is >= 14 and <= 26 &&
            cell.Y is >= 3 and <= 5;
        return !ceremonyPlatform;
    }
}
