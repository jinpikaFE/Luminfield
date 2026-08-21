namespace Luminfield.Core;

public sealed record StarlightSpatialDefinition(
    string PedestalId,
    string LocationId,
    GridPosition Cell
);

public static class StarlightSpatialCatalog
{
    public static IReadOnlyList<StarlightSpatialDefinition> Pedestals { get; } =
        Array.AsReadOnly(
        [
            new StarlightSpatialDefinition(
                DataCatalog.WoodlandStarlightId,
                PlayerLocationIds.World,
                WorldDefinition.WoodlandStarlightCell
            ),
            new StarlightSpatialDefinition(
                DataCatalog.HomesteadStarlightId,
                PlayerLocationIds.World,
                FarmLayout.HomesteadStarlightCell
            ),
            new StarlightSpatialDefinition(
                DataCatalog.MeadowStarlightId,
                PlayerLocationIds.World,
                WorldDefinition.MeadowStarlightCell
            ),
            new StarlightSpatialDefinition(
                DataCatalog.MoonwaterStarlightId,
                PlayerLocationIds.World,
                WorldDefinition.MoonwaterStarlightCell
            ),
            new StarlightSpatialDefinition(
                DataCatalog.CrystalValeStarlightId,
                PlayerLocationIds.World,
                WorldDefinition.CrystalWellCell
            ),
            new StarlightSpatialDefinition(
                DataCatalog.StarfallRuinsStarlightId,
                PlayerLocationIds.World,
                WorldDefinition.StarfallRuinsStarlightCell
            )
        ]);

    private static readonly IReadOnlyDictionary<string,
        StarlightSpatialDefinition> ByPedestalId = Pedestals.ToDictionary(
            definition => definition.PedestalId,
            StringComparer.Ordinal
        );

    private static readonly IReadOnlyDictionary<GridPosition,
        StarlightSpatialDefinition> ByCell = Pedestals.ToDictionary(
            definition => definition.Cell
        );

    public static StarlightSpatialDefinition ForPedestal(
        string pedestalId
    ) => ByPedestalId.TryGetValue(pedestalId, out var definition)
        ? definition
        : throw new KeyNotFoundException(
            $"Unknown starlight pedestal id '{pedestalId}'."
        );

    public static bool TryAtCell(
        GridPosition cell,
        out StarlightSpatialDefinition definition
    ) => ByCell.TryGetValue(cell, out definition!);
}
