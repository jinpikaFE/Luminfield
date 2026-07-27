namespace Luminfield.Core;

public sealed class ProcessorSystem
{
    public string ActiveRecipeId { get; private set; } = string.Empty;
    public int RemainingNights { get; private set; }
    public bool IsIdle => string.IsNullOrWhiteSpace(ActiveRecipeId);
    public bool IsReady => !IsIdle && RemainingNights == 0;

    public event Action? Changed;

    public void Reset()
    {
        ActiveRecipeId = string.Empty;
        RemainingNights = 0;
        Changed?.Invoke();
    }

    public void Restore(ProcessorSave? save)
    {
        if (save is null ||
            string.IsNullOrWhiteSpace(save.RecipeId) ||
            !DataCatalog.ProcessorRecipes.ContainsKey(save.RecipeId))
        {
            ActiveRecipeId = string.Empty;
            RemainingNights = 0;
        }
        else
        {
            ActiveRecipeId = save.RecipeId;
            var recipe = DataCatalog.ProcessorRecipe(ActiveRecipeId);
            RemainingNights = Math.Clamp(save.RemainingNights, 0, recipe.Nights);
        }

        Changed?.Invoke();
    }

    public ActionResult Start(string recipeId, Inventory inventory)
    {
        if (!IsIdle)
        {
            return ActionResult.Fail("processor.busy");
        }

        if (!DataCatalog.ProcessorRecipes.TryGetValue(recipeId, out var recipe))
        {
            return ActionResult.Fail("processor.unknown_recipe");
        }

        if (inventory.Count(recipe.InputItemId) < recipe.InputCount)
        {
            return ActionResult.Fail("processor.missing_ingredients");
        }

        if (!inventory.Remove(recipe.InputItemId, recipe.InputCount))
        {
            return ActionResult.Fail("processor.missing_ingredients");
        }

        ActiveRecipeId = recipe.Id;
        RemainingNights = recipe.Nights;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "processor.started");
    }

    public void ResolveNight()
    {
        if (IsIdle || RemainingNights <= 0)
        {
            return;
        }

        RemainingNights--;
        Changed?.Invoke();
    }

    public ActionResult Collect(Inventory inventory)
    {
        if (!IsReady)
        {
            return ActionResult.Fail("processor.not_ready");
        }

        var recipe = DataCatalog.ProcessorRecipe(ActiveRecipeId);
        if (!inventory.Add(recipe.OutputItemId, recipe.OutputCount))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        ActiveRecipeId = string.Empty;
        RemainingNights = 0;
        Changed?.Invoke();
        return new ActionResult(
            true,
            MessageKey: "processor.collected",
            GrantedItemId: recipe.OutputItemId,
            GrantedItemCount: recipe.OutputCount
        );
    }

    public ProcessorSave Capture() => new()
    {
        RecipeId = ActiveRecipeId,
        RemainingNights = RemainingNights
    };
}
