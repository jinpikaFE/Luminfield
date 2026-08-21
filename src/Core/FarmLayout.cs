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
    public static readonly GridPosition HomesteadWorkbenchCell = new(42, 9);
    public static readonly GridPosition GreenhouseDoorCell = new(38, 10);
    public static readonly GridPosition GreenhouseReturnCell = new(38, 11);
    // The interaction target stays inside the original cottage reserve so
    // adding the coop never invalidates an old placeable on the approach cell.
    public static readonly GridPosition StarfeatherCoopDoorCell = new(6, 10);
    public static readonly GridPosition StarfeatherCoopReturnCell = new(6, 11);
    public static readonly GridPosition MoonfleeceBarnDoorCell = new(42, 20);
    public static readonly GridPosition MoonfleeceBarnReturnCell = new(42, 21);
    public static readonly GridPosition HomesteadStarlightCell = new(30, 12);
    public static readonly GridPosition StarGateCell = new(24, 7);
    public static readonly GridPosition StarGateArrivalCell = new(24, 10);

    private static readonly HashSet<GridPosition> StaticBlocked =
    [
        new(2, 5), new(3, 5), new(2, 6),
        new(43, 4), new(44, 4), new(44, 5),
        new(4, 25), new(5, 25), new(4, 26),
        new(30, 3), new(30, 4), HomesteadStarlightCell,
        new(14, 8), new(14, 9),
        new(42, 8), HomesteadWorkbenchCell,
        new(42, 12), new(43, 12), new(44, 12),
        new(42, 13), new(43, 13), new(44, 13),
        new(42, 14), new(43, 14), new(44, 14),
        new(35, 12), new(36, 12), new(37, 12),
        new(35, 13), new(36, 13), new(37, 13),
        new(35, 14), new(36, 14), new(37, 14),
        ProcessorCatalog.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).Position,
        new(26, 10), new(28, 10),
        new(22, 4), new(23, 4), new(24, 4), new(25, 4), new(26, 4),
        new(22, 5), new(23, 5), new(24, 5), new(25, 5), new(26, 5),
        new(22, 6), new(23, 6), new(24, 6), new(25, 6), new(26, 6),
        new(22, 7), new(23, 7), StarGateCell, new(25, 7), new(26, 7),
        MiraCell,
        CottageDoorCell,
        ShopCell,
        ProcessorCell,
        ShippingCell,
        CommissionBoardCell,
        StarlightMailboxCell,
        GreenhouseDoorCell,
        StarfeatherCoopDoorCell,
        MoonfleeceBarnDoorCell
    ];

    public static bool IsStaticBlocked(GridPosition position) =>
        StaticBlocked.Contains(position);

    public static bool IsStarfeatherCoopApproachCell(
        GridPosition position
    ) => IsAnimalBuildingApproachCell(position);

    public static bool IsStarfeatherCoopProtectedCell(
        GridPosition position
    ) => IsAnimalBuildingProtectedCell(position);

    public static bool IsAnimalBuildingApproachCell(GridPosition position) =>
        AnimalBuildingSpatialCatalog.IsApproachCell(position);

    public static bool IsAnimalBuildingProtectedCell(GridPosition position) =>
        AnimalBuildingSpatialCatalog.IsProtectedWorldCell(position);

    public static bool IsCommissionBoardCell(GridPosition position) =>
        position.Y == CommissionBoardCell.Y &&
        position.X is >= 26 and <= 28;

    public static string? ProcessorMachineIdAt(GridPosition position) =>
        ProcessorCatalog.MachineIdAt(position);
}
