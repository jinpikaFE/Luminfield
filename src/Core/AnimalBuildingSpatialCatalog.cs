namespace Luminfield.Core;

public sealed record AnimalProductStationDefinition(
    string ProductBaseItemId,
    GridPosition Cell,
    TargetPreviewKind Kind,
    string ActionKey,
    string NotReadyKey,
    string NotReadyStatusKey,
    string CollectedKey
);

public sealed record AnimalBuildingSpatialDefinition(
    string BuildingId,
    string LocationId,
    GridPosition WorldDoorCell,
    GridPosition WorldReturnCell,
    GridPosition ExitCell,
    GridPosition SafeArrivalCell,
    GridPosition FeedTroughCell,
    GridPosition AutomationStationCell,
    IReadOnlyList<AnimalProductStationDefinition> ProductStations,
    IReadOnlyList<GridPosition> IndoorAnimalCells,
    IReadOnlyList<GridPosition> WorldPastureCells,
    Func<GridPosition, bool> IsInteriorWalkable,
    TargetPreviewKind PortalKind,
    TargetPreviewKind ExitKind,
    TargetPreviewKind AnimalKind,
    string EnterActionKey,
    string ExitActionKey,
    string FeedCompletedKey
);

public static class AnimalBuildingSpatialCatalog
{
    public static AnimalBuildingSpatialDefinition StarfeatherCoop { get; } =
        new(
            AnimalCatalog.StarfeatherCoopId,
            PlayerLocationIds.StarfeatherCoop,
            FarmLayout.StarfeatherCoopDoorCell,
            FarmLayout.StarfeatherCoopReturnCell,
            StarfeatherCoopLayout.ExitCell,
            StarfeatherCoopLayout.SafeArrivalCell,
            StarfeatherCoopLayout.FeedTroughCell,
            StarfeatherCoopLayout.AutomationStationCell,
            Array.AsReadOnly<AnimalProductStationDefinition>(
            [
                new(
                    DataCatalog.StarfeatherEggId,
                    StarfeatherCoopLayout.NestCell,
                    TargetPreviewKind.AnimalNest,
                    "target.action.collect_animal_product",
                    "animal.product.not_ready",
                    "target.status.animal_product_not_ready",
                    "animal.product.collected"
                )
            ]),
            Array.AsReadOnly<GridPosition>(
            [
                StarfeatherCoopLayout.IndoorAnimalCell
            ]),
            StarfeatherCoopLayout.WorldPastureCells,
            StarfeatherCoopLayout.IsWalkable,
            TargetPreviewKind.AnimalBuildingPortal,
            TargetPreviewKind.AnimalBuildingExit,
            TargetPreviewKind.Animal,
            "target.action.enter_starfeather_coop",
            "target.action.exit_starfeather_coop",
            "animal.feed.completed"
        );

    public static AnimalBuildingSpatialDefinition MoonfleeceBarn { get; } =
        new(
            AnimalCatalog.MoonfleeceBarnId,
            PlayerLocationIds.MoonfleeceBarn,
            FarmLayout.MoonfleeceBarnDoorCell,
            FarmLayout.MoonfleeceBarnReturnCell,
            MoonfleeceBarnLayout.ExitCell,
            MoonfleeceBarnLayout.SafeArrivalCell,
            MoonfleeceBarnLayout.FeedTroughCell,
            MoonfleeceBarnLayout.AutomationStationCell,
            Array.AsReadOnly<AnimalProductStationDefinition>(
            [
                new(
                    DataCatalog.MoonfleeceId,
                    MoonfleeceBarnLayout.CollectionRackCell,
                    TargetPreviewKind.AnimalProductStation,
                    "target.action.collect_moonfleece",
                    "animal.product.moonfleece_not_ready",
                    "target.status.moonfleece_not_ready",
                    "animal.product.moonfleece_collected"
                ),
                new(
                    DataCatalog.DewhornMilkId,
                    MoonfleeceBarnLayout.MilkingStationCell,
                    TargetPreviewKind.DewhornMilkingStation,
                    "target.action.collect_dewhorn_milk",
                    "animal.product.dewhorn_milk_not_ready",
                    "target.status.dewhorn_milk_not_ready",
                    "animal.product.dewhorn_milk_collected"
                )
            ]),
            MoonfleeceBarnLayout.IndoorAnimalCells,
            MoonfleeceBarnLayout.WorldPastureCells,
            MoonfleeceBarnLayout.IsWalkable,
            TargetPreviewKind.MoonfleeceBarnPortal,
            TargetPreviewKind.MoonfleeceBarnExit,
            TargetPreviewKind.MoonfleeceSheep,
            "target.action.enter_moonfleece_barn",
            "target.action.exit_moonfleece_barn",
            "animal.feed.moonfleece_completed"
        );

    public static IReadOnlyList<AnimalBuildingSpatialDefinition> Definitions
        { get; } = Array.AsReadOnly([StarfeatherCoop, MoonfleeceBarn]);

    public static bool TryByBuildingId(
        string? buildingId,
        out AnimalBuildingSpatialDefinition definition
    )
    {
        definition = Definitions.FirstOrDefault(candidate =>
            candidate.BuildingId == buildingId
        )!;
        return definition is not null;
    }

    public static bool TryByLocationId(
        string? locationId,
        out AnimalBuildingSpatialDefinition definition
    )
    {
        definition = Definitions.FirstOrDefault(candidate =>
            candidate.LocationId == locationId
        )!;
        return definition is not null;
    }

    public static bool TryAtWorldDoor(
        GridPosition cell,
        out AnimalBuildingSpatialDefinition definition
    )
    {
        definition = Definitions.FirstOrDefault(candidate =>
            candidate.WorldDoorCell == cell
        )!;
        return definition is not null;
    }

    public static bool IsApproachCell(GridPosition cell) =>
        Definitions.Any(definition => definition.WorldReturnCell == cell);

    public static bool IsProtectedWorldCell(GridPosition cell) =>
        IsApproachCell(cell) || Definitions.Any(definition =>
            definition.WorldPastureCells.Contains(cell)
        );
}
