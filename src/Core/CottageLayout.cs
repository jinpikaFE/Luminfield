namespace Luminfield.Core;

public static class CottageLayout
{
    public static readonly GridPosition BedCell = new(12, 9);
    public static readonly GridPosition DoorCell = new(20, 18);
    public static readonly GridPosition SafeArrivalCell = new(20, 17);
    public static readonly GridPosition KitchenReserveCell = new(27, 14);

    public static bool IsWalkable(GridPosition cell, bool upgraded)
    {
        if (cell.X is < 3 or > 36 || cell.Y is < 3 or > 18)
        {
            return false;
        }

        var kitchenBlocked = upgraded
            ? IsKitchenReserveArea(cell)
            : IsBaseTableArea(cell);
        return !IsBedArea(cell) &&
            !kitchenBlocked &&
            !IsBookshelfArea(cell) &&
            !IsFireplaceArea(cell);
    }

    public static bool IsBedArea(GridPosition cell) =>
        cell.X is >= 9 and <= 14 && cell.Y is >= 3 and <= 9;

    public static bool IsKitchenReserveArea(GridPosition cell) =>
        cell.X is >= 27 and <= 35 && cell.Y is >= 10 and <= 17;

    public static bool IsAdjacentToKitchenReserve(GridPosition cell)
    {
        var nearestX = Math.Clamp(cell.X, 27, 35);
        var nearestY = Math.Clamp(cell.Y, 10, 17);
        return Math.Abs(cell.X - nearestX) +
            Math.Abs(cell.Y - nearestY) == 1;
    }

    private static bool IsBaseTableArea(GridPosition cell) =>
        cell.X is >= 29 and <= 35 && cell.Y is >= 10 and <= 17;

    private static bool IsBookshelfArea(GridPosition cell) =>
        cell.X is >= 3 and <= 8 && cell.Y is >= 3 and <= 17;

    private static bool IsFireplaceArea(GridPosition cell) =>
        cell.X is >= 28 and <= 35 && cell.Y is >= 3 and <= 8;
}
