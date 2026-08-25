namespace Luminfield.Core;

public static class ProcessorCatalog
{
    public const string MoonwellInfuserId = "machine_moonwell_infuser";
    public const string PrismPreserveVatId = "machine_prism_preserve_vat";
    public const string StarweaveDryingLoomId = "machine_starweave_drying_loom";
    public const string MoonpearlEggPressId = "machine_moonpearl_egg_press";
    public const string MainMachineId = MoonwellInfuserId;

    public static readonly IReadOnlyDictionary<string, ProcessorMachineDefinition>
        Machines = new Dictionary<string, ProcessorMachineDefinition>(
            StringComparer.Ordinal
        )
        {
            [MoonwellInfuserId] = new(
                MoonwellInfuserId,
                new GridPosition(34, 34),
                "processor.machine.moonwell",
                [
                    DataCatalog.MoonrootTonicRecipeId,
                    DataCatalog.StarbudPreserveRecipeId
                ]
            ),
            [PrismPreserveVatId] = new(
                PrismPreserveVatId,
                new GridPosition(42, 34),
                "processor.machine.prism_vat",
                [DataCatalog.StarbudPreserveRecipeId]
            ),
            [StarweaveDryingLoomId] = new(
                StarweaveDryingLoomId,
                new GridPosition(34, 40),
                "processor.machine.drying_loom",
                [DataCatalog.CloudleafTeaRecipeId]
            ),
            [MoonpearlEggPressId] = new(
                MoonpearlEggPressId,
                new GridPosition(42, 40),
                "processor.machine.moonpearl_egg_press",
                [DataCatalog.StarfeatherCreamRecipeId]
            )
        };

    public static ProcessorMachineDefinition Machine(string id) =>
        Machines.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Unknown processor machine id '{id}'."
            );

    public static bool SupportsRecipe(string machineId, string recipeId) =>
        Machines.TryGetValue(machineId, out var machine) &&
        machine.RecipeIds.Contains(recipeId, StringComparer.Ordinal);

    public static string? MachineIdAt(GridPosition position) =>
        Machines.Values.FirstOrDefault(machine => machine.Position == position)?.Id;
}
