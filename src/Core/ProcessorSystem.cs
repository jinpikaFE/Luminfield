namespace Luminfield.Core;

public sealed class ProcessorMachineState
{
    public ProcessorMachineState(string machineId)
    {
        MachineId = machineId;
    }

    public string MachineId { get; }
    public string ActiveRecipeId { get; internal set; } = string.Empty;
    public int RemainingNights { get; internal set; }
    public bool IsIdle => string.IsNullOrWhiteSpace(ActiveRecipeId);
    public bool IsReady => !IsIdle && RemainingNights == 0;
}

public sealed class ProcessorSystem
{
    private readonly Dictionary<string, ProcessorMachineState> _machines = [];

    public ProcessorSystem()
    {
        ResetWithoutNotification();
    }

    public IReadOnlyDictionary<string, ProcessorMachineState> Machines => _machines;
    public ProcessorMachineState MainMachine => Machine(ProcessorCatalog.MainMachineId);
    public string ActiveRecipeId => MainMachine.ActiveRecipeId;
    public int RemainingNights => MainMachine.RemainingNights;
    public bool IsIdle => MainMachine.IsIdle;
    public bool IsReady => MainMachine.IsReady;
    public int ReadyCount => _machines.Values.Count(machine => machine.IsReady);

    public event Action? Changed;

    public void Reset()
    {
        ResetWithoutNotification();
        Changed?.Invoke();
    }

    public void Restore(ProcessorSave? save)
    {
        ResetWithoutNotification();
        var validEntries = (save?.Machines ?? [])
            .Where(entry =>
                entry is not null &&
                ProcessorCatalog.Machines.ContainsKey(entry.MachineId)
            )
            .GroupBy(entry => entry.MachineId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (validEntries.Count > 0)
        {
            foreach (var entry in validEntries)
            {
                RestoreMachine(entry.MachineId, entry.RecipeId, entry.RemainingNights);
            }
        }
        else if (save is not null)
        {
            RestoreMachine(
                ProcessorCatalog.MainMachineId,
                save.RecipeId,
                save.RemainingNights
            );
        }

        Changed?.Invoke();
    }

    public ProcessorMachineState Machine(string machineId) =>
        _machines.TryGetValue(machineId, out var machine)
            ? machine
            : throw new KeyNotFoundException(
                $"Unknown processor machine id '{machineId}'."
            );

    public ActionResult Start(string recipeId, Inventory inventory) =>
        Start(ProcessorCatalog.MainMachineId, recipeId, inventory);

    public ActionResult Start(
        string machineId,
        string recipeId,
        Inventory inventory
    )
    {
        if (!_machines.TryGetValue(machineId, out var machine))
        {
            return ActionResult.Fail("processor.unknown_machine");
        }

        if (!machine.IsIdle)
        {
            return ActionResult.Fail("processor.busy");
        }

        if (!DataCatalog.ProcessorRecipes.TryGetValue(recipeId, out var recipe) ||
            !ProcessorCatalog.SupportsRecipe(machineId, recipeId))
        {
            return ActionResult.Fail("processor.unknown_recipe");
        }

        if (inventory.CountFamily(recipe.InputItemId) < recipe.InputCount ||
            !inventory.RemoveFamily(recipe.InputItemId, recipe.InputCount))
        {
            return ActionResult.Fail("processor.missing_ingredients");
        }

        machine.ActiveRecipeId = recipe.Id;
        machine.RemainingNights = recipe.Nights;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "processor.started");
    }

    public void ResolveNight()
    {
        var changed = false;
        foreach (var machine in _machines.Values)
        {
            if (machine.IsIdle || machine.RemainingNights <= 0)
            {
                continue;
            }

            machine.RemainingNights--;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public ActionResult Collect(Inventory inventory) =>
        Collect(ProcessorCatalog.MainMachineId, inventory);

    public ActionResult Collect(string machineId, Inventory inventory)
    {
        if (!_machines.TryGetValue(machineId, out var machine))
        {
            return ActionResult.Fail("processor.unknown_machine");
        }

        if (!machine.IsReady)
        {
            return ActionResult.Fail("processor.not_ready");
        }

        var recipe = DataCatalog.ProcessorRecipe(machine.ActiveRecipeId);
        if (!inventory.Add(recipe.OutputItemId, recipe.OutputCount))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        Clear(machine);
        Changed?.Invoke();
        return new ActionResult(
            true,
            MessageKey: "processor.collected",
            GrantedItemId: recipe.OutputItemId,
            GrantedItemCount: recipe.OutputCount
        );
    }

    public ActionResult CollectAllReady(Inventory inventory)
    {
        var ready = ProcessorCatalog.Machines.Keys
            .Select(Machine)
            .Where(machine => machine.IsReady)
            .ToList();
        if (ready.Count == 0)
        {
            return ActionResult.Fail("processor.none_ready");
        }

        var additions = ready
            .Select(machine => DataCatalog.ProcessorRecipe(machine.ActiveRecipeId))
            .GroupBy(recipe => recipe.OutputItemId, StringComparer.Ordinal)
            .Select(group => new CraftingIngredient(
                group.Key,
                group.Sum(recipe => recipe.OutputCount)
            ))
            .ToList();
        if (!inventory.TryAddMany(additions))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        foreach (var machine in ready)
        {
            Clear(machine);
        }

        Changed?.Invoke();
        return ActionResult.Success(messageKey: "processor.collected_all");
    }

    public ProcessorSave Capture()
    {
        var main = MainMachine;
        return new ProcessorSave
        {
            RecipeId = main.ActiveRecipeId,
            RemainingNights = main.RemainingNights,
            Machines = ProcessorCatalog.Machines.Keys
                .Select(machineId =>
                {
                    var state = Machine(machineId);
                    return new ProcessorMachineSave
                    {
                        MachineId = state.MachineId,
                        RecipeId = state.ActiveRecipeId,
                        RemainingNights = state.RemainingNights
                    };
                })
                .ToList()
        };
    }

    private void ResetWithoutNotification()
    {
        _machines.Clear();
        foreach (var machineId in ProcessorCatalog.Machines.Keys)
        {
            _machines[machineId] = new ProcessorMachineState(machineId);
        }
    }

    private void RestoreMachine(
        string machineId,
        string recipeId,
        int remainingNights
    )
    {
        var machine = Machine(machineId);
        if (string.IsNullOrWhiteSpace(recipeId) ||
            !DataCatalog.ProcessorRecipes.TryGetValue(recipeId, out var recipe) ||
            !ProcessorCatalog.SupportsRecipe(machineId, recipeId))
        {
            Clear(machine);
            return;
        }

        machine.ActiveRecipeId = recipeId;
        machine.RemainingNights = Math.Clamp(remainingNights, 0, recipe.Nights);
    }

    private static void Clear(ProcessorMachineState machine)
    {
        machine.ActiveRecipeId = string.Empty;
        machine.RemainingNights = 0;
    }
}
