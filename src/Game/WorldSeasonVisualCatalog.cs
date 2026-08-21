using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum WorldSeasonVisualVariant
{
    Default,
    Rainveil,
    Starharvest,
    Longnight
}

public sealed record WorldSeasonVisualProfile(
    WorldSeasonVisualVariant Variant,
    string PropAtlasTexturePath,
    Color GroundModulate,
    Color WaterModulate,
    Color DetailModulate,
    Color PathModulate
);

public static class WorldSeasonVisualCatalog
{
    public const int GroundAtlasColumns = 4;
    public const int GroundAtlasRows = 8;
    public const int GroundAtlasCellSize = 16;
    public const int WaterAtlasRow = 7;
    public const int ShoreAtlasColumns = 4;
    public const int ShoreAtlasRows = 4;
    public const int PropAtlasColumns = 4;
    public const int PropAtlasRows = 4;
    public const int PropAtlasEntryCount = 16;
    public const float PropAtlasCellSize = 313.5f;

    public const string DefaultPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props.png";
    public const string GroundAtlasTexturePath =
        "res://assets/generated/world/terrain/world_ground_biomes.png";
    public const string ShoreAtlasTexturePath =
        "res://assets/generated/world/terrain/world_shore_tiles.png";
    public const string RainveilPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_rainveil.png";
    public const string StarharvestPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_starharvest.png";
    public const string LongnightPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_longnight.png";

    private static readonly WorldSeasonVisualProfile Default = new(
        WorldSeasonVisualVariant.Default,
        DefaultPropAtlasTexturePath,
        Colors.White,
        Colors.White,
        Colors.White,
        new Color(0.84f, 0.86f, 1f, 1f)
    );

    private static readonly WorldSeasonVisualProfile Rainveil = new(
        WorldSeasonVisualVariant.Rainveil,
        RainveilPropAtlasTexturePath,
        new Color(0.76f, 0.9f, 0.98f, 1f),
        new Color(0.74f, 0.94f, 1f, 1f),
        new Color(0.78f, 0.96f, 1f, 1f),
        new Color(0.76f, 0.9f, 0.98f, 1f)
    );

    private static readonly WorldSeasonVisualProfile Starharvest = new(
        WorldSeasonVisualVariant.Starharvest,
        StarharvestPropAtlasTexturePath,
        new Color(1f, 0.84f, 0.7f, 1f),
        new Color(0.92f, 0.86f, 0.76f, 1f),
        new Color(1f, 0.88f, 0.64f, 1f),
        new Color(1f, 0.86f, 0.72f, 1f)
    );

    private static readonly WorldSeasonVisualProfile Longnight = new(
        WorldSeasonVisualVariant.Longnight,
        LongnightPropAtlasTexturePath,
        new Color(0.7f, 0.78f, 0.96f, 1f),
        new Color(0.68f, 0.84f, 1f, 1f),
        new Color(0.76f, 0.82f, 1f, 1f),
        new Color(0.7f, 0.76f, 0.92f, 1f)
    );

    public static WorldSeasonVisualProfile ForDay(int day) =>
        CalendarSystem.SeasonId(day) switch
        {
            CalendarSystem.RainveilSeasonId => Rainveil,
            CalendarSystem.StarharvestSeasonId => Starharvest,
            CalendarSystem.LongnightSeasonId => Longnight,
            _ => Default
        };

    public static int GroundAtlasRow(WorldBiome biome) => biome switch
    {
        WorldBiome.Home => 0,
        WorldBiome.WhisperingWoods => 1,
        WorldBiome.StarfallMeadow => 2,
        WorldBiome.LumenVillage => 3,
        WorldBiome.CrystalVale => 4,
        WorldBiome.MoonwaterWetlands => 5,
        WorldBiome.StarfallRuins => 6,
        _ => 0
    };

    public static int ShoreMaskAt(GridPosition cell)
    {
        if (!WorldDefinition.IsWater(cell))
        {
            return 0;
        }

        var mask = 0;
        if (IsLand(new GridPosition(cell.X, cell.Y - 1)))
        {
            mask |= 1;
        }
        if (IsLand(new GridPosition(cell.X + 1, cell.Y)))
        {
            mask |= 2;
        }
        if (IsLand(new GridPosition(cell.X, cell.Y + 1)))
        {
            mask |= 4;
        }
        if (IsLand(new GridPosition(cell.X - 1, cell.Y)))
        {
            mask |= 8;
        }
        return mask;
    }

    private static bool IsLand(GridPosition cell) =>
        !WorldDefinition.IsInBounds(cell) || !WorldDefinition.IsWater(cell);
}
