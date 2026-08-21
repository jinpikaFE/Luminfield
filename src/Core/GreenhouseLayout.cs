namespace Luminfield.Core;

public static class GreenhouseLayout
{
    public const int Width = 40;
    public const int Height = 22;

    public static readonly GridPosition CisternCell = new(7, 14);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 18);

    private static readonly HashSet<GridPosition> Beds =
    [
        .. CreateBed(12, 14, 7, 8),
        .. CreateBed(25, 27, 7, 8),
        .. CreateBed(11, 13, 12, 13),
        .. CreateBed(26, 28, 12, 13)
    ];

    public static IReadOnlyCollection<GridPosition> PlantingCells => Beds;

    public static bool IsInBounds(GridPosition position) =>
        position.X >= 0 &&
        position.X < Width &&
        position.Y >= 0 &&
        position.Y < Height;

    public static bool IsPlantingBed(GridPosition position) =>
        Beds.Contains(position);

    public static bool IsWalkable(GridPosition position) =>
        position.X is >= 2 and <= 37 &&
        position.Y is >= 3 and <= 20 &&
        position != CisternCell;

    private static IEnumerable<GridPosition> CreateBed(
        int minX,
        int maxX,
        int minY,
        int maxY
    )
    {
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                yield return new GridPosition(x, y);
            }
        }
    }
}
