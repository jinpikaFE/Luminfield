using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class CottageKitchenTests
{
    [Fact]
    public void CatalogFreezesSecondUpgradeAndFourDishContracts()
    {
        var project = ConstructionCatalog.CottageSecondUpgrade;

        Assert.Equal("cottage_second_upgrade", project.Id);
        Assert.Equal(960, project.CoinCost);
        Assert.Equal(4, project.RequiredNights);
        Assert.Equal([32, 14], project.Materials
            .Select(material => material.Count)
            .ToArray());
        Assert.Equal(
            [
                ConstructionCatalog.CottageFirstUpgradeId,
                ConstructionCatalog.HomesteadWorkshopProjectId
            ],
            project.RequiredProjectIds
        );
        Assert.Equal(4, DataCatalog.CookingRecipes.Count);
        Assert.Equal(4, DataCatalog.CookedDishes.Count);
        var expected = new Dictionary<string, (
            string OutputId,
            int Energy,
            int SellPrice,
            string[] Ingredients
        )>(StringComparer.Ordinal)
        {
            [DataCatalog.MoonmistStewRecipeId] = (
                DataCatalog.MoonmistStewId,
                60,
                156,
                [
                    DataCatalog.RipplecapId,
                    DataCatalog.MistsongMintId,
                    DataCatalog.DewhornMilkId
                ]
            ),
            [DataCatalog.SunvaultHashRecipeId] = (
                DataCatalog.SunvaultHashId,
                45,
                148,
                [DataCatalog.SunvaultGourdId, DataCatalog.CometTuberId]
            ),
            [DataCatalog.StarhoneyCustardRecipeId] = (
                DataCatalog.StarhoneyCustardId,
                70,
                258,
                [
                    DataCatalog.StarhoneyId,
                    DataCatalog.StarfeatherEggId,
                    DataCatalog.MoonplumId
                ]
            ),
            [DataCatalog.LanternrootBrothRecipeId] = (
                DataCatalog.LanternrootBrothId,
                55,
                150,
                [
                    DataCatalog.LanternReedId,
                    DataCatalog.MoonrootId,
                    DataCatalog.TideglassTaroId
                ]
            )
        };
        foreach (var (recipeId, contract) in expected)
        {
            var recipe = DataCatalog.CookingRecipes[recipeId];
            Assert.Equal(contract.OutputId, recipe.OutputItemId);
            Assert.Equal(1, recipe.OutputCount);
            Assert.Equal(contract.Ingredients, recipe.Ingredients
                .Select(ingredient => ingredient.ItemId)
                .ToArray());
            Assert.All(recipe.Ingredients,
                ingredient => Assert.Equal(1, ingredient.Count));
            Assert.Equal(contract.Energy,
                DataCatalog.CookedDishes[contract.OutputId].EnergyRestore);
            Assert.Equal(contract.SellPrice,
                DataCatalog.Item(contract.OutputId).SellPrice);
        }
        Assert.All(DataCatalog.CookedDishItemIds, itemId =>
        {
            var item = DataCatalog.Item(itemId);
            Assert.Equal(ItemKind.CookedDish, item.Kind);
            Assert.Contains(itemId, DataCatalog.SellableItemIds);
            Assert.Contains(itemId, DataCatalog.StorableItemIds);
        });
    }

    [Fact]
    public void SecondUpgradeRequiresBothProjectsAndCompletesAfterFourNights()
    {
        var missing = new ConstructionSystem();
        var inventory = PreparedInventory(32, 14);
        Assert.Equal(
            "construction.cottage_second_upgrade.requires_first_and_workshop",
            missing.CheckStart(
                ConstructionCatalog.CottageSecondUpgradeId,
                inventory,
                960
            ).MessageKey
        );

        var construction = new ConstructionSystem();
        construction.Restore(CompletedUpgradePrerequisites());
        Assert.True(construction.CheckStart(
            ConstructionCatalog.CottageSecondUpgradeId,
            inventory,
            960
        ).Succeeded);
        construction.BeginChecked(
            ConstructionCatalog.CottageSecondUpgradeId
        );
        Assert.Equal(1, CottageLevel(construction));
        Assert.False(construction.ResolveNight());
        Assert.False(construction.ResolveNight());
        Assert.False(construction.ResolveNight());
        Assert.True(construction.ResolveNight());
        Assert.True(construction.IsCompletedFor(
            ConstructionCatalog.CottageSecondUpgradeId
        ));
        Assert.Equal(2, CottageLevel(construction));

        var invalid = ConstructionSystem.NormalizeSave(new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                    Completed = true
                }
            ]
        });
        Assert.DoesNotContain(invalid.Projects, state =>
            state.ProjectId == ConstructionCatalog.CottageSecondUpgradeId);
    }

    [Fact]
    public void PantryNormalizesToTwentyFourIngredientSlotsAndRejectsOthers()
    {
        var validIds = DataCatalog.Items.Values
            .Where(item => KitchenSystem.IsPantryItem(item.Id))
            .Select(item => item.Id)
            .Distinct(StringComparer.Ordinal)
            .Take(KitchenSystem.PantrySlotCount + 3)
            .ToArray();
        Assert.True(validIds.Length > KitchenSystem.PantrySlotCount);
        var normalized = KitchenSystem.NormalizeSave(new KitchenSave
        {
            PantryItems = validIds
                .Select(itemId => new InventorySlot
                {
                    ItemId = itemId,
                    Count = 99
                })
                .Append(new InventorySlot
                {
                    ItemId = DataCatalog.MoonfleeceId,
                    Count = 99
                })
                .Append(new InventorySlot
                {
                    ItemId = "removed_food",
                    Count = int.MaxValue
                })
                .ToList()
        });

        Assert.Equal(KitchenSystem.PantrySlotCount,
            normalized.PantryItems.Count);
        Assert.All(normalized.PantryItems, slot =>
        {
            Assert.True(KitchenSystem.IsPantryItem(slot.ItemId));
            Assert.Equal(DataCatalog.Item(slot.ItemId).MaxStack, slot.Count);
        });
        Assert.DoesNotContain(normalized.PantryItems, slot =>
            slot.ItemId == DataCatalog.MoonfleeceId);
        Assert.Equal(
            JsonSerializer.Serialize(normalized),
            JsonSerializer.Serialize(KitchenSystem.NormalizeSave(normalized))
        );
    }

    [Fact]
    public void PantryStoreAndTakeAreCapacityAndBackpackAtomic()
    {
        var kitchen = new KitchenSystem();
        kitchen.Reset();
        var inventory = new Inventory();
        inventory.Reset();
        Assert.True(inventory.Add(DataCatalog.RipplecapId, 2));

        Assert.True(kitchen.StoreIngredient(
            DataCatalog.RipplecapId,
            1,
            inventory
        ).Succeeded);
        Assert.Equal(1, kitchen.Count(DataCatalog.RipplecapId));
        Assert.Equal(1, inventory.Count(DataCatalog.RipplecapId));

        var beforeInvalid = Snapshot(kitchen, inventory);
        Assert.Equal(
            "kitchen.pantry.not_ingredient",
            kitchen.StoreIngredient(
                DataCatalog.MoonfleeceId,
                1,
                inventory
            ).MessageKey
        );
        Assert.Equal(beforeInvalid, Snapshot(kitchen, inventory));

        Assert.True(kitchen.TakeIngredient(
            DataCatalog.RipplecapId,
            1,
            inventory
        ).Succeeded);
        Assert.Equal(0, kitchen.Count(DataCatalog.RipplecapId));
        Assert.Equal(2, inventory.Count(DataCatalog.RipplecapId));
    }

    [Fact]
    public void CookingUsesPantryBeforeBackpackAndLowerQualityFirst()
    {
        var kitchen = new KitchenSystem();
        kitchen.Restore(new KitchenSave
        {
            PantryItems =
            [
                new InventorySlot
                {
                    ItemId = DataCatalog.StarfeatherEggId,
                    Count = 1
                },
                new InventorySlot
                {
                    ItemId = DataCatalog.StarhoneyId,
                    Count = 1
                }
            ]
        });
        var inventory = new Inventory();
        inventory.Reset();
        Assert.True(inventory.Add(
            DataCatalog.StarfeatherEggLuminousId,
            1
        ));
        Assert.True(inventory.Add(DataCatalog.MoonplumId, 1));

        var cooked = kitchen.Cook(
            DataCatalog.StarhoneyCustardRecipeId,
            inventory
        );

        Assert.True(cooked.Succeeded);
        Assert.Equal(1, inventory.Count(DataCatalog.StarhoneyCustardId));
        Assert.Equal(1, inventory.Count(
            DataCatalog.StarfeatherEggLuminousId
        ));
        Assert.Equal(0, kitchen.Count(DataCatalog.StarfeatherEggId));
        Assert.Equal(0, kitchen.Count(DataCatalog.StarhoneyId));
        Assert.Equal(0, inventory.Count(DataCatalog.MoonplumId));
    }

    [Fact]
    public void MissingIngredientsAndFullOutputLeaveBothContainersUnchanged()
    {
        var kitchen = new KitchenSystem();
        kitchen.Reset();
        var inventory = new Inventory();
        inventory.Reset();
        var beforeMissing = Snapshot(kitchen, inventory);
        Assert.Equal(
            "cooking.missing_ingredients",
            kitchen.Cook(
                DataCatalog.MoonmistStewRecipeId,
                inventory
            ).MessageKey
        );
        Assert.Equal(beforeMissing, Snapshot(kitchen, inventory));

        kitchen.Restore(new KitchenSave
        {
            PantryItems =
            [
                new InventorySlot
                {
                    ItemId = DataCatalog.RipplecapId,
                    Count = 1
                },
                new InventorySlot
                {
                    ItemId = DataCatalog.MistsongMintId,
                    Count = 1
                },
                new InventorySlot
                {
                    ItemId = DataCatalog.DewhornMilkId,
                    Count = 1
                }
            ]
        });
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId => itemId != DataCatalog.MoonmistStewId)
                     .Where(itemId => DataCatalog.Item(itemId).Kind !=
                         ItemKind.Tool)
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(inventory.Add(itemId, 1));
        }
        Assert.Equal(24, inventory.Slots.Count(slot => !slot.IsEmpty));
        var beforeFull = Snapshot(kitchen, inventory);

        Assert.Equal(
            "cooking.backpack_full",
            kitchen.Cook(
                DataCatalog.MoonmistStewRecipeId,
                inventory
            ).MessageKey
        );
        Assert.Equal(beforeFull, Snapshot(kitchen, inventory));
    }

    [Fact]
    public void CompletedCottageSplitsRealKitchenAndPantryPreviewTargets()
    {
        var session = CompletedKitchenSession();
        session.SetPlayerLocation(
            29 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.Cottage
        );

        var station = session.PreviewSelectedTarget(
            new GridPosition(29, 17)
        );
        Assert.Equal(TargetPreviewState.Available, station.State);
        Assert.Equal(TargetPreviewKind.KitchenStation, station.Kind);
        Assert.Equal(CottageLayout.KitchenStationCell, station.Target);
        Assert.True(session.OpenKitchenStation(
            new GridPosition(29, 17)
        ).Succeeded);

        session.SetPlayerLocation(
            34 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        var pantry = session.PreviewSelectedTarget(
            new GridPosition(34, 17)
        );
        Assert.Equal(TargetPreviewKind.IngredientPantry, pantry.Kind);
        Assert.Equal(CottageLayout.IngredientPantryCell, pantry.Target);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            new GridPosition(34, 17)
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal("target.need.hand", wrongTool.LabelKey);
    }

    [Fact]
    public void EatingRestoresEnergyAndFullEnergyNeverConsumesDish()
    {
        var session = CompletedKitchenSession();
        Assert.True(session.Inventory.Add(DataCatalog.MoonmistStewId, 2));
        var fullBefore = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "cooking.energy_full",
            session.EatCookedDish(DataCatalog.MoonmistStewId).MessageKey
        );
        Assert.Equal(fullBefore, JsonSerializer.Serialize(session.Capture()));

        var save = session.Capture();
        save.Player.Energy = 35;
        session.Restore(save);
        Assert.True(session.EatCookedDish(
            DataCatalog.MoonmistStewId
        ).Succeeded);
        Assert.Equal(95, session.Energy);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonmistStewId));

        save = session.Capture();
        save.Player.Energy = 80;
        session.Restore(save);
        Assert.True(session.EatCookedDish(
            DataCatalog.MoonmistStewId
        ).Succeeded);
        Assert.Equal(GameSession.MaxEnergy, session.Energy);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.MoonmistStewId));
    }

    private static Inventory PreparedInventory(int lumenwood, int crystal)
    {
        var inventory = new Inventory();
        inventory.Reset();
        Assert.True(inventory.Add(DataCatalog.LumenwoodId, lumenwood));
        Assert.True(inventory.Add(DataCatalog.CrystalShardId, crystal));
        return inventory;
    }

    private static ConstructionSave CompletedUpgradePrerequisites() => new()
    {
        Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                Completed = true
            },
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            }
        ]
    };

    private static GameSession CompletedKitchenSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Construction = CompletedUpgradePrerequisites();
        save.Construction.Projects.Add(new ConstructionProjectSave
        {
            ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
            Completed = true
        });
        save.Player.LocationId = PlayerLocationIds.Cottage;
        session.Restore(save);
        Assert.Equal(2, session.CottageUpgradeLevel);
        return session;
    }

    private static int CottageLevel(ConstructionSystem construction) =>
        construction.IsCompletedFor(
            ConstructionCatalog.CottageSecondUpgradeId
        )
            ? 2
            : construction.IsCompletedFor(
                ConstructionCatalog.CottageFirstUpgradeId
            )
                ? 1
                : 0;

    private static string Snapshot(
        KitchenSystem kitchen,
        Inventory inventory
    ) => JsonSerializer.Serialize(new
    {
        Pantry = kitchen.Capture(),
        Backpack = inventory.Capture()
    });
}
