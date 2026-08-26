using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class WorldNavigationRuntimeOccupancyTests
{
    [Fact]
    public void SouthernVillageStoneRoadIsWalkableAtScreenshotTime()
    {
        var session = new GameSession();
        session.NewGame("en");
        var save = session.Capture();
        save.Day = 2;
        save.MinuteOfDay = 6 * 60 + 50;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 112 * 16 + 8;
        save.Player.Y = 119 * 16 + 8;
        session.Restore(save);
        var currentPlayerCell = new GridPosition(112, 119);
        var eastboundRoad = Enumerable.Range(119, 3)
            .SelectMany(y => Enumerable.Range(112, 29)
                .Select(x => new GridPosition(x, y)));

        Assert.All(eastboundRoad, cell => Assert.True(
            session.CanOccupyWorldCell(cell, currentPlayerCell),
            $"Expected screenshot road cell {cell} to be walkable."
        ));
    }

    [Fact]
    public void VillageGateKeepsCenterPassageOpenAndVisiblePillarsBlocked()
    {
        var session = new GameSession();
        session.NewGame("en");
        var approach = new GridPosition(128, 119);
        var centerPassage = Enumerable.Range(116, 17)
            .Select(y => new GridPosition(128, y));

        Assert.All(centerPassage, cell => Assert.True(
            session.CanOccupyWorldCell(cell, approach),
            $"Expected village gate center passage {cell} to be walkable."
        ));
        Assert.False(session.CanOccupyWorldCell(
            new GridPosition(127, 125),
            approach
        ));
        Assert.False(session.CanOccupyWorldCell(
            new GridPosition(129, 125),
            approach
        ));
    }

    [Fact]
    public void NavigationOccupancyCoversEveryStablePlayerLocation()
    {
        var session = new GameSession();
        session.NewGame("en");
        var safeCells = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [PlayerLocationIds.World] = new(19, 30),
            [PlayerLocationIds.Cottage] = CottageLayout.SafeArrivalCell,
            [PlayerLocationIds.MoonlitArchive] = IndoorArrival(
                PlayerLocationIds.MoonlitArchive
            ),
            [PlayerLocationIds.MoonstoneWorkshop] = IndoorArrival(
                PlayerLocationIds.MoonstoneWorkshop
            ),
            [PlayerLocationIds.StarweaverTeaHouse] = IndoorArrival(
                PlayerLocationIds.StarweaverTeaHouse
            ),
            [PlayerLocationIds.TwilightEmporium] = IndoorArrival(
                PlayerLocationIds.TwilightEmporium
            ),
            [PlayerLocationIds.StarlightPost] = IndoorArrival(
                PlayerLocationIds.StarlightPost
            ),
            [PlayerLocationIds.StarfallWatch] = IndoorArrival(
                PlayerLocationIds.StarfallWatch
            ),
            [PlayerLocationIds.Greenhouse] =
                GreenhouseLayout.SafeArrivalCell,
            [PlayerLocationIds.StarfeatherCoop] =
                StarfeatherCoopLayout.SafeArrivalCell,
            [PlayerLocationIds.MoonfleeceBarn] =
                MoonfleeceBarnLayout.SafeArrivalCell,
            [PlayerLocationIds.StarharvestMarket] =
                StarharvestMarketLayout.SafeArrivalCell,
            [PlayerLocationIds.GleamrisePlantingFestival] =
                GleamrisePlantingFestivalLayout.SafeArrivalCell,
            [PlayerLocationIds.LongnightLanternFeast] =
                LongnightLanternFeastLayout.SafeArrivalCell,
            [PlayerLocationIds.FireflyTide] =
                FireflyTideLayout.SafeArrivalCell,
            [PlayerLocationIds.CrystalGrottoSurvey] =
                CrystalGrottoSurveyLayout.SafeArrivalCell,
            [PlayerLocationIds.StarfallRuinsTrial] =
                StarfallRuinsTrialLayout.SafeArrivalCell
        };
        var stableLocationIds = typeof(PlayerLocationIds)
            .GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static
            )
            .Where(field => field.IsLiteral &&
                field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(stableLocationIds.SetEquals(safeCells.Keys));
        Assert.All(safeCells, pair =>
        {
            Assert.True(
                session.CanOccupyNavigationCell(pair.Key, pair.Value),
                $"Expected {pair.Key} {pair.Value} to be walkable."
            );
            Assert.False(session.CanOccupyNavigationCell(
                pair.Key,
                new GridPosition(0, 0)
            ));
        });
    }

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

    private static GridPosition IndoorArrival(string locationId) =>
        NpcNavigationMap.SafeArrivalCell(
            PlayerLocationIds.World,
            locationId
        ) ?? throw new InvalidOperationException(
            $"Missing indoor arrival for {locationId}."
        );
}
