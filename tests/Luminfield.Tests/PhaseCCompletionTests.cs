using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseCCompletionTests
{
    [Fact]
    public void MoonpearlEggPressHasStableRecipeAndProtectedWorldCell()
    {
        var machine = ProcessorCatalog.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        );
        var recipe = DataCatalog.ProcessorRecipe(
            DataCatalog.StarfeatherCreamRecipeId
        );

        Assert.Equal(new GridPosition(34, 14), machine.Position);
        Assert.True(FarmLayout.IsStaticBlocked(machine.Position));
        Assert.Equal(DataCatalog.StarfeatherEggId, recipe.InputItemId);
        Assert.Equal(DataCatalog.StarfeatherCreamId, recipe.OutputItemId);
        Assert.Equal(1, recipe.InputCount);
        Assert.Equal(1, recipe.OutputCount);
        Assert.Equal(1, recipe.Nights);
        Assert.True(ProcessorCatalog.SupportsRecipe(
            machine.Id,
            recipe.Id
        ));
        Assert.Equal(132, DataCatalog.Item(recipe.OutputItemId).SellPrice);
    }

    [Fact]
    public void EggPressConsumesOneQualityFamilyEggAndRestoresReadyState()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(
            DataCatalog.StarfeatherEggLuminousId,
            1
        ));
        Assert.True(session.PreviewProcessorMachine(
            ProcessorCatalog.MoonpearlEggPressId
        ).IsAvailable);

        var started = session.StartProcessing(
            ProcessorCatalog.MoonpearlEggPressId,
            DataCatalog.StarfeatherCreamRecipeId
        );

        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Inventory.Count(
            DataCatalog.StarfeatherEggLuminousId
        ));
        Assert.Equal(
            "processor.busy",
            session.PreviewProcessorMachine(
                ProcessorCatalog.MoonpearlEggPressId
            ).LabelKey
        );

        session.EndDay();
        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Processor.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).IsReady);

        var collected = restored.CollectProcessedItem(
            ProcessorCatalog.MoonpearlEggPressId
        );

        Assert.True(collected.Succeeded);
        Assert.Equal(1, restored.Inventory.Count(
            DataCatalog.StarfeatherCreamId
        ));
        Assert.True(restored.Processor.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).IsIdle);
    }

    [Fact]
    public void WrongMachineOrMissingEggLeavesProcessorAndInventoryUnchanged()
    {
        var session = new GameSession();
        session.NewGame();
        var before = session.Capture();

        var wrongMachine = session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarfeatherCreamRecipeId
        );
        var missingEgg = session.StartProcessing(
            ProcessorCatalog.MoonpearlEggPressId,
            DataCatalog.StarfeatherCreamRecipeId
        );

        Assert.False(wrongMachine.Succeeded);
        Assert.False(missingEgg.Succeeded);
        Assert.Equal(
            before.Inventory.Select(slot => (slot.ItemId, slot.Count)),
            session.Capture().Inventory.Select(slot =>
                (slot.ItemId, slot.Count)
            )
        );
        Assert.True(session.Processor.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).IsIdle);
    }
}
