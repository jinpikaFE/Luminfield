namespace Luminfield.Core;

public static class FarmLayout
{
    public static readonly GridPosition MiraCell = new(32, 9);
    public static readonly GridPosition CottageDoorCell = new(16, 11);
    public static readonly GridPosition ShopCell = new(43, 14);
    public static readonly GridPosition ProcessorCell =
        ProcessorCatalog.Machine(ProcessorCatalog.MainMachineId).Position;
    public static readonly GridPosition ShippingCell = new(8, 14);
    public static readonly GridPosition CommissionBoardCell = new(27, 10);
    public static readonly GridPosition StarlightMailboxCell = new(19, 10);

    private static readonly HashSet<GridPosition> StaticBlocked =
    [
        new(2, 5), new(3, 5), new(2, 6),
        new(43, 4), new(44, 4), new(44, 5),
        new(4, 25), new(5, 25), new(4, 26),
        new(30, 3), new(30, 4),
        new(14, 8), new(14, 9),
        new(42, 8), new(42, 9),
        new(42, 12), new(43, 12), new(44, 12),
        new(42, 13), new(43, 13), new(44, 13),
        new(42, 14), new(43, 14), new(44, 14),
        new(35, 12), new(36, 12), new(37, 12),
        new(35, 13), new(36, 13), new(37, 13),
        new(35, 14), new(36, 14), new(37, 14),
        new(26, 10), new(28, 10),
        MiraCell,
        CottageDoorCell,
        ShopCell,
        ProcessorCell,
        ShippingCell,
        CommissionBoardCell,
        StarlightMailboxCell
    ];

    public static bool IsStaticBlocked(GridPosition position) =>
        StaticBlocked.Contains(position);

    public static bool IsCommissionBoardCell(GridPosition position) =>
        position.Y == CommissionBoardCell.Y &&
        position.X is >= 26 and <= 28;

    public static string? ProcessorMachineIdAt(GridPosition position) =>
        ProcessorCatalog.MachineIdAt(position);
}
