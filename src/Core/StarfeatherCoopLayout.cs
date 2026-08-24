namespace Luminfield.Core;

public static class StarfeatherCoopLayout
{
    public const int MapWidth = 40;
    public const int MapHeight = 22;
    public const int GrazingStartMinute = 8 * 60;
    public const int GrazingEndMinute = 18 * 60;

    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 18);
    public static readonly GridPosition FeedTroughCell = new(9, 13);
    public static readonly GridPosition NestCell = new(32, 10);
    public static readonly GridPosition AutomationStationCell = new(8, 17);
    public static readonly GridPosition IndoorAnimalCell = new(20, 13);

    public static readonly IReadOnlyList<GridPosition> WorldPastureCells =
        CityExpansionLayout.StarfeatherPastureCells;

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

        var trough = position.X is >= 5 and <= 13 &&
            position.Y is >= 9 and <= 13;
        var nest = position.X is >= 29 and <= 36 &&
            position.Y is >= 6 and <= 10;
        var automation = position.X is >= 7 and <= 9 &&
            position.Y is >= 15 and <= 17;
        return !trough && !nest && !automation;
    }
}
