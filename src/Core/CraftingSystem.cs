namespace Luminfield.Core;

public sealed class CraftingSystem
{
    public bool HasIngredients(string recipeId, Inventory inventory)
    {
        if (!DataCatalog.CraftingRecipes.TryGetValue(recipeId, out var recipe))
        {
            return false;
        }

        return recipe.Ingredients.All(ingredient =>
            inventory.Count(ingredient.ItemId) >= ingredient.Count
        );
    }

    public ActionResult Craft(string recipeId, Inventory inventory)
    {
        if (!DataCatalog.CraftingRecipes.TryGetValue(recipeId, out var recipe))
        {
            return ActionResult.Fail("crafting.unknown_recipe");
        }

        if (!HasIngredients(recipeId, inventory))
        {
            return ActionResult.Fail("crafting.missing_ingredients");
        }

        if (!inventory.TryExchange(
                recipe.Ingredients,
                recipe.OutputItemId,
                recipe.OutputCount
            ))
        {
            return ActionResult.Fail("crafting.backpack_full");
        }

        inventory.PromoteToHotbar(recipe.OutputItemId);
        return ActionResult.Success(messageKey: "crafting.crafted");
    }
}
