namespace Luminfield.Core;

public static class MoonfleeceBarnLayout
{
    public const int MapWidth = 40;
    public const int MapHeight = 22;

    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 18);
    public static readonly GridPosition FeedTroughCell = new(8, 13);
    public static readonly GridPosition CollectionRackCell = new(31, 13);
    public static readonly GridPosition MilkingStationCell = new(31, 17);
    public static readonly GridPosition AutomationStationCell = new(8, 17);

    public static readonly IReadOnlyList<GridPosition> IndoorAnimalCells =
        Array.AsReadOnly<GridPosition>(
        [
            new(20, 13),
            new(18, 13),
            new(22, 13),
            new(20, 15)
        ]);

    public static readonly IReadOnlyList<GridPosition> WorldPastureCells =
        CityExpansionLayout.MoonfleecePastureCells;

    public static bool IsInBounds(GridPosition position) =>
        position.X >= 0 &&
        position.Y >= 0 &&
        position.X < MapWidth &&
        position.Y < MapHeight;

    public static bool IsWalkable(GridPosition position)
    {
        if (!IsInBounds(position) ||
            position.X < 2 ||
            position.X > 37 ||
            position.Y < 7 ||
            position.Y > 20 ||
            position == ExitCell)
        {
            return false;
        }

        var trough = position.X is >= 5 and <= 11 &&
            position.Y is >= 11 and <= 13;
        var rack = position.X is >= 28 and <= 35 &&
            position.Y is >= 10 and <= 13;
        var milkingStation = position.X is >= 29 and <= 33 &&
            position.Y is >= 16 and <= 17;
        var automation = position.X is >= 7 and <= 9 &&
            position.Y is >= 15 and <= 17;
        return !trough && !rack && !milkingStation && !automation;
    }
}
