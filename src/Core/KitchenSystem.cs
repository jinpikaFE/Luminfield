namespace Luminfield.Core;

public sealed class KitchenSystem
{
    public const int PantrySlotCount = 24;

    private readonly List<InventorySlot> _pantry = Enumerable
        .Range(0, PantrySlotCount)
        .Select(_ => new InventorySlot())
        .ToList();

    public IReadOnlyList<InventorySlot> Pantry => _pantry;
    public int UsedPantrySlots => _pantry.Count(slot => !slot.IsEmpty);

    public event Action? Changed;

    public void Reset()
    {
        ApplyPantrySnapshot(EmptyPantry());
        Changed?.Invoke();
    }

    public void Restore(KitchenSave? save)
    {
        var normalized = NormalizeSave(save);
        var pantry = EmptyPantry();
        foreach (var slot in normalized.PantryItems)
        {
            _ = AddTo(pantry, slot.ItemId, slot.Count);
        }

        ApplyPantrySnapshot(pantry);
        Changed?.Invoke();
    }

    public int Count(string itemId) => _pantry
        .Where(slot => slot.ItemId == itemId)
        .Sum(slot => slot.Count);

    public int CountFamily(string itemId) => DataCatalog
        .ItemFamilyIds(itemId)
        .Sum(Count);

    public ActionResult CheckStoreIngredient(
        string itemId,
        int count,
        Inventory inventory
    )
    {
        if (count <= 0 || !IsPantryItem(itemId))
        {
            return ActionResult.Fail("kitchen.pantry.not_ingredient");
        }

        if (inventory.Count(itemId) < count)
        {
            return ActionResult.Fail("kitchen.pantry.none_in_backpack");
        }

        var simulated = CloneSlots(_pantry);
        return AddTo(simulated, itemId, count)
            ? ActionResult.Success(messageKey: "kitchen.pantry.ready")
            : ActionResult.Fail("kitchen.pantry.full");
    }

    public ActionResult StoreIngredient(
        string itemId,
        int count,
        Inventory inventory
    )
    {
        var check = CheckStoreIngredient(itemId, count, inventory);
        if (!check.Succeeded)
        {
            return check;
        }

        var simulated = CloneSlots(_pantry);
        _ = AddTo(simulated, itemId, count);
        if (!inventory.Remove(itemId, count))
        {
            return ActionResult.Fail("kitchen.pantry.none_in_backpack");
        }

        ApplyPantrySnapshot(simulated);
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "kitchen.pantry.stored");
    }

    public ActionResult CheckTakeIngredient(
        string itemId,
        int count,
        Inventory inventory
    )
    {
        if (count <= 0 || Count(itemId) < count)
        {
            return ActionResult.Fail("kitchen.pantry.none_stored");
        }

        return inventory.CanAdd(itemId, count)
            ? ActionResult.Success(messageKey: "kitchen.pantry.ready")
            : ActionResult.Fail("kitchen.pantry.backpack_full");
    }

    public ActionResult TakeIngredient(
        string itemId,
        int count,
        Inventory inventory
    )
    {
        var check = CheckTakeIngredient(itemId, count, inventory);
        if (!check.Succeeded)
        {
            return check;
        }

        var simulated = CloneSlots(_pantry);
        _ = RemoveFrom(simulated, itemId, count);
        if (!inventory.Add(itemId, count))
        {
            return ActionResult.Fail("kitchen.pantry.backpack_full");
        }

        ApplyPantrySnapshot(simulated);
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "kitchen.pantry.taken");
    }

    public ActionResult CheckCook(string recipeId, Inventory inventory) =>
        BuildCookPlan(recipeId, inventory).Result;

    public ActionResult Cook(string recipeId, Inventory inventory)
    {
        var plan = BuildCookPlan(recipeId, inventory);
        if (!plan.Result.Succeeded || plan.Recipe is null)
        {
            return plan.Result;
        }

        var committed = plan.BackpackRemovals.Count == 0
            ? inventory.TryAddMany(
                [
                    new CraftingIngredient(
                        plan.Recipe.OutputItemId,
                        plan.Recipe.OutputCount
                    )
                ]
            )
            : inventory.TryExchange(
                plan.BackpackRemovals,
                plan.Recipe.OutputItemId,
                plan.Recipe.OutputCount
            );
        if (!committed)
        {
            return ActionResult.Fail("cooking.backpack_full");
        }

        ApplyPantrySnapshot(plan.PantryAfter);
        Changed?.Invoke();
        return ActionResult.Grant(
            plan.Recipe.OutputItemId,
            plan.Recipe.OutputCount,
            0,
            "cooking.cooked"
        );
    }

    public KitchenSave Capture() => new()
    {
        PantryItems = _pantry
            .Where(slot => !slot.IsEmpty)
            .Select(slot => slot.Clone())
            .ToList()
    };

    public static KitchenSave NormalizeSave(KitchenSave? save)
    {
        var normalized = EmptyPantry();
        var items = save?.PantryItems ?? [];
        foreach (var itemId in items
                     .Where(slot => slot is not null)
                     .Select(slot => slot.ItemId)
                     .Where(IsPantryItem)
                     .Distinct(StringComparer.Ordinal))
        {
            var definition = DataCatalog.Item(itemId);
            var total = items
                .Where(slot => slot is not null && slot.ItemId == itemId)
                .Sum(slot => Math.Max(0L, slot.Count));
            _ = AddTo(
                normalized,
                itemId,
                (int)Math.Min(total, (long)definition.MaxStack * PantrySlotCount)
            );
        }

        return new KitchenSave
        {
            PantryItems = normalized
                .Where(slot => !slot.IsEmpty)
                .Select(slot => slot.Clone())
                .ToList()
        };
    }

    public static bool IsPantryItem(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) ||
            !DataCatalog.Items.TryGetValue(itemId, out var item))
        {
            return false;
        }

        if (item.Kind is ItemKind.Produce or ItemKind.Artisan or
            ItemKind.CookedDish)
        {
            return true;
        }

        if (item.Kind != ItemKind.AnimalProduct)
        {
            return false;
        }

        var baseItemId = DataCatalog.BaseItemId(item.Id);
        return baseItemId is DataCatalog.StarfeatherEggId or
            DataCatalog.DewhornMilkId;
    }

    private CookPlan BuildCookPlan(string recipeId, Inventory inventory)
    {
        if (!DataCatalog.CookingRecipes.TryGetValue(
                recipeId,
                out var recipe
            ))
        {
            return CookPlan.Fail("cooking.unknown_recipe");
        }

        var pantry = CloneSlots(_pantry);
        var backpack = inventory.Capture();
        var backpackRemovalCounts = new Dictionary<string, int>(
            StringComparer.Ordinal
        );
        foreach (var ingredient in recipe.Ingredients)
        {
            var remaining = ingredient.Count;
            foreach (var familyItemId in DataCatalog.ItemFamilyIds(
                         ingredient.ItemId
                     ))
            {
                if (remaining <= 0)
                {
                    break;
                }

                var pantryCount = CountIn(pantry, familyItemId);
                var pantryRemoval = Math.Min(remaining, pantryCount);
                if (pantryRemoval > 0)
                {
                    _ = RemoveFrom(pantry, familyItemId, pantryRemoval);
                    remaining -= pantryRemoval;
                }

                if (remaining <= 0)
                {
                    break;
                }

                var backpackCount = CountIn(backpack, familyItemId);
                var backpackRemoval = Math.Min(remaining, backpackCount);
                if (backpackRemoval > 0)
                {
                    _ = RemoveFrom(
                        backpack,
                        familyItemId,
                        backpackRemoval
                    );
                    backpackRemovalCounts[familyItemId] =
                        backpackRemovalCounts.GetValueOrDefault(
                            familyItemId
                        ) + backpackRemoval;
                    remaining -= backpackRemoval;
                }
            }

            if (remaining > 0)
            {
                return CookPlan.Fail("cooking.missing_ingredients");
            }
        }

        if (!AddTo(backpack, recipe.OutputItemId, recipe.OutputCount))
        {
            return CookPlan.Fail("cooking.backpack_full");
        }

        var removals = backpackRemovalCounts
            .Select(pair => new CraftingIngredient(pair.Key, pair.Value))
            .ToArray();
        return new CookPlan(
            ActionResult.Success(messageKey: "cooking.ready"),
            recipe,
            pantry,
            removals
        );
    }

    private void ApplyPantrySnapshot(IReadOnlyList<InventorySlot> snapshot)
    {
        for (var index = 0; index < PantrySlotCount; index++)
        {
            _pantry[index].ItemId = snapshot[index].ItemId;
            _pantry[index].Count = snapshot[index].Count;
        }
    }

    private static List<InventorySlot> EmptyPantry() => Enumerable
        .Range(0, PantrySlotCount)
        .Select(_ => new InventorySlot())
        .ToList();

    private static List<InventorySlot> CloneSlots(
        IEnumerable<InventorySlot> slots
    ) => slots.Select(slot => slot.Clone()).ToList();

    private static int CountIn(
        IEnumerable<InventorySlot> slots,
        string itemId
    ) => slots
        .Where(slot => slot.ItemId == itemId)
        .Sum(slot => slot.Count);

    private static bool RemoveFrom(
        IList<InventorySlot> slots,
        string itemId,
        int count
    )
    {
        if (count <= 0 || CountIn(slots, itemId) < count)
        {
            return false;
        }

        var remaining = count;
        for (var index = slots.Count - 1;
             index >= 0 && remaining > 0;
             index--)
        {
            var slot = slots[index];
            if (slot.ItemId != itemId)
            {
                continue;
            }

            var removed = Math.Min(remaining, slot.Count);
            slot.Count -= removed;
            remaining -= removed;
            if (slot.Count == 0)
            {
                slot.ItemId = string.Empty;
            }
        }

        return remaining == 0;
    }

    private static bool AddTo(
        IList<InventorySlot> slots,
        string itemId,
        int count
    )
    {
        if (count <= 0 || !DataCatalog.Items.ContainsKey(itemId))
        {
            return false;
        }

        var definition = DataCatalog.Item(itemId);
        var capacity = slots
            .Where(slot => slot.IsEmpty || slot.ItemId == itemId)
            .Sum(slot => slot.IsEmpty
                ? definition.MaxStack
                : definition.MaxStack - slot.Count);
        if (capacity < count)
        {
            return false;
        }

        var remaining = count;
        foreach (var slot in slots.Where(slot =>
                     slot.ItemId == itemId &&
                     slot.Count < definition.MaxStack))
        {
            var added = Math.Min(remaining, definition.MaxStack - slot.Count);
            slot.Count += added;
            remaining -= added;
            if (remaining == 0)
            {
                return true;
            }
        }

        foreach (var slot in slots.Where(slot => slot.IsEmpty))
        {
            var added = Math.Min(remaining, definition.MaxStack);
            slot.ItemId = itemId;
            slot.Count = added;
            remaining -= added;
            if (remaining == 0)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record CookPlan(
        ActionResult Result,
        CookingRecipeDefinition? Recipe,
        IReadOnlyList<InventorySlot> PantryAfter,
        IReadOnlyList<CraftingIngredient> BackpackRemovals
    )
    {
        public static CookPlan Fail(string messageKey) => new(
            ActionResult.Fail(messageKey),
            null,
            [],
            []
        );
    }
}
