namespace Luminfield.Core;

public sealed class CultivationZoneDefinition
{
    private readonly HashSet<GridPosition> _plantingCells;

    public CultivationZoneDefinition(
        string id,
        int width,
        int height,
        IEnumerable<GridPosition> plantingCells,
        bool ignoresSeasonRestrictions,
        bool receivesOutdoorWeather
    )
    {
        Id = id;
        Width = width;
        Height = height;
        _plantingCells = plantingCells.ToHashSet();
        IgnoresSeasonRestrictions = ignoresSeasonRestrictions;
        ReceivesOutdoorWeather = receivesOutdoorWeather;
    }

    public string Id { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyCollection<GridPosition> PlantingCells => _plantingCells;
    public bool IgnoresSeasonRestrictions { get; }
    public bool ReceivesOutdoorWeather { get; }

    public bool IsInBounds(GridPosition position) =>
        position.X >= 0 &&
        position.X < Width &&
        position.Y >= 0 &&
        position.Y < Height;

    public bool IsPlantingCell(GridPosition position) =>
        _plantingCells.Contains(position);
}

public static class CultivationZoneCatalog
{
    public const string OutdoorFarmId = "outdoor_farm";
    public const string GreenhouseId = "greenhouse";

    public static CultivationZoneDefinition OutdoorFarm { get; } = new(
        OutdoorFarmId,
        FarmSystem.MapWidth,
        FarmSystem.MapHeight,
        CreateOutdoorPlantingCells(),
        ignoresSeasonRestrictions: false,
        receivesOutdoorWeather: true
    );

    public static CultivationZoneDefinition Greenhouse { get; } = new(
        GreenhouseId,
        GreenhouseLayout.Width,
        GreenhouseLayout.Height,
        GreenhouseLayout.PlantingCells,
        ignoresSeasonRestrictions: true,
        receivesOutdoorWeather: false
    );

    private static IEnumerable<GridPosition> CreateOutdoorPlantingCells()
    {
        for (var y = 15; y <= 21; y++)
        {
            if (y == 18)
            {
                continue;
            }

            for (var x = 11; x <= 32; x++)
            {
                if (x == 18 || x == 25)
                {
                    continue;
                }

                yield return new GridPosition(x, y);
            }
        }
    }
}
