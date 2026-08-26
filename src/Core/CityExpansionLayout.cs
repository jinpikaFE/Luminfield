namespace Luminfield.Core;

public static class CityExpansionLayout
{
    public static readonly GridPosition ConstructionWorkbenchCell =
        new(112, 116);
    public static readonly GridPosition GreenhouseDoorCell = new(136, 116);
    public static readonly GridPosition GreenhouseReturnCell = new(136, 117);
    public static readonly GridPosition StarfeatherCoopDoorCell = new(156, 116);
    public static readonly GridPosition StarfeatherCoopReturnCell =
        new(156, 117);
    public static readonly GridPosition MoonfleeceBarnDoorCell = new(182, 116);
    public static readonly GridPosition MoonfleeceBarnReturnCell = new(182, 117);
    public static readonly GridPosition CityStarlightCell = new(116, 72);
    public static readonly GridPosition StarGateCell = new(140, 73);
    public static readonly GridPosition StarGateArrivalCell = new(140, 74);
    public static readonly GridArea FacilityGatewayReservedArea =
        new(119, 102, 137, 112);

    public static readonly IReadOnlyList<GridPosition>
        StarfeatherPastureCells = Array.AsReadOnly<GridPosition>(
        [
            new(154, 119),
            new(155, 119),
            new(156, 119),
            new(157, 119)
        ]);

    public static readonly IReadOnlyList<GridPosition>
        MoonfleecePastureCells = Array.AsReadOnly<GridPosition>(
        [
            new(180, 119),
            new(181, 119),
            new(182, 119),
            new(183, 119)
        ]);

    private static readonly IReadOnlyList<GridArea> CollisionAreas =
    [
        new(112, 116, 112, 116),
        new(132, 108, 140, 116),
        new(152, 108, 160, 116),
        new(178, 108, 186, 116),
        new(114, 68, 118, 72),
        new(137, 67, 143, 73),
        new(120, 106, 123, 112),
        new(133, 106, 136, 112)
    ];

    public static bool IsBlocked(GridPosition cell) =>
        CollisionAreas.Any(area => area.Contains(cell));

    public static bool IsReserved(GridPosition cell) =>
        IsBlocked(cell) ||
        FacilityGatewayReservedArea.Contains(cell) ||
        StarfeatherPastureCells.Contains(cell) ||
        MoonfleecePastureCells.Contains(cell) ||
        cell == GreenhouseReturnCell ||
        cell == StarfeatherCoopReturnCell ||
        cell == MoonfleeceBarnReturnCell ||
        cell == StarGateArrivalCell;
}
