namespace Luminfield.Core;

public static class FarmLayout
{
    public static readonly GridPosition MiraCell = new(32, 9);
    public static readonly GridPosition CottageDoorCell = new(16, 11);
    public static readonly GridPosition ShopCell = new(45, 10);
    public static readonly GridPosition ProcessorCell =
        ProcessorCatalog.Machine(ProcessorCatalog.MainMachineId).Position;
    public static readonly GridPosition ShippingCell = new(8, 14);
    public static readonly GridPosition CommissionBoardCell = new(27, 10);
    public static readonly GridPosition StarlightMailboxCell = new(19, 10);
    public static readonly GridPosition HomesteadWorkbenchCell =
        CityExpansionLayout.ConstructionWorkbenchCell;
    public static readonly GridPosition GreenhouseDoorCell =
        CityExpansionLayout.GreenhouseDoorCell;
    public static readonly GridPosition GreenhouseReturnCell =
        CityExpansionLayout.GreenhouseReturnCell;
    public static readonly GridPosition StarfeatherCoopDoorCell =
        CityExpansionLayout.StarfeatherCoopDoorCell;
    public static readonly GridPosition StarfeatherCoopReturnCell =
        CityExpansionLayout.StarfeatherCoopReturnCell;
    public static readonly GridPosition MoonfleeceBarnDoorCell =
        CityExpansionLayout.MoonfleeceBarnDoorCell;
    public static readonly GridPosition MoonfleeceBarnReturnCell =
        CityExpansionLayout.MoonfleeceBarnReturnCell;
    public static readonly GridPosition HomesteadStarlightCell =
        CityExpansionLayout.CityStarlightCell;
    public static readonly GridPosition StarGateCell =
        CityExpansionLayout.StarGateCell;
    public static readonly GridPosition StarGateArrivalCell =
        CityExpansionLayout.StarGateArrivalCell;

    private static readonly HashSet<GridPosition> StaticBlocked =
    [
        new(2, 5), new(3, 5), new(2, 6),
        new(43, 4), new(44, 4), new(44, 5),
        new(4, 25), new(5, 25), new(4, 26),
        new(14, 8), new(14, 9),
        new(44, 8), new(45, 8), new(46, 8),
        new(44, 9), new(45, 9), new(46, 9),
        new(44, 10), new(45, 10), new(46, 10),
        ProcessorCatalog.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).Position,
        ProcessorCatalog.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).Position,
        ProcessorCatalog.Machine(
            ProcessorCatalog.StarweaveDryingLoomId
        ).Position,
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
