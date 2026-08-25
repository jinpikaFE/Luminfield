using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRuntimeOccupancyTests
{
    [Fact]
    public void SessionWorldOccupancyTreatsPlacedStorageAsBlocked()
    {
        var session = new GameSession();
        session.NewGame("en");
        var cell = FirstPlaceableStorageCell(session);

        Assert.True(session.CanOccupyWorldCell(cell));
        Assert.True(session.Inventory.Add(DataCatalog.StarwovenChestId, 1));
        var placed = session.Storage.Place(
            cell,
            session.Farm,
            session.Inventory
        );

        Assert.True(placed.Succeeded);
        Assert.True(session.Storage.HasChest(cell));
        Assert.False(session.CanOccupyWorldCell(cell));
    }

    private static GridPosition FirstPlaceableStorageCell(
        GameSession session
    )
    {
        for (var y = 0; y < FarmSystem.MapHeight; y++)
        {
            for (var x = 0; x < FarmSystem.MapWidth; x++)
            {
                var cell = new GridPosition(x, y);
                if (session.CanOccupyWorldCell(cell) &&
                    session.Storage.CheckPlacement(cell, session.Farm) ==
                    ChestPlacementIssue.None)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException(
            "No placeable storage cell was found."
        );
    }
}
