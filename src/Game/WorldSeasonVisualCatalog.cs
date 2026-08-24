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
    string WorldBackdropTexturePath
);

public static class WorldSeasonVisualCatalog
{
    public const int PropAtlasColumns = 4;
    public const int PropAtlasRows = 4;
    public const int PropAtlasEntryCount = 16;
    public const float PropAtlasCellSize = 313.5f;

    public const string DefaultPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props.png";
    public const string RainveilPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_rainveil.png";
    public const string StarharvestPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_starharvest.png";
    public const string LongnightPropAtlasTexturePath =
        "res://assets/generated/world/exploration/exploration_props_longnight.png";
    public const string DefaultWorldBackdropTexturePath =
        "res://assets/generated/world/overworld/world_composite_gleamrise.png";
    public const string RainveilWorldBackdropTexturePath =
        "res://assets/generated/world/overworld/world_composite_rainveil.png";
    public const string StarharvestWorldBackdropTexturePath =
        "res://assets/generated/world/overworld/world_composite_starharvest.png";
    public const string LongnightWorldBackdropTexturePath =
        "res://assets/generated/world/overworld/world_composite_longnight.png";

    private static readonly WorldSeasonVisualProfile Default = new(
        WorldSeasonVisualVariant.Default,
        DefaultPropAtlasTexturePath,
        DefaultWorldBackdropTexturePath
    );

    private static readonly WorldSeasonVisualProfile Rainveil = new(
        WorldSeasonVisualVariant.Rainveil,
        RainveilPropAtlasTexturePath,
        RainveilWorldBackdropTexturePath
    );

    private static readonly WorldSeasonVisualProfile Starharvest = new(
        WorldSeasonVisualVariant.Starharvest,
        StarharvestPropAtlasTexturePath,
        StarharvestWorldBackdropTexturePath
    );

    private static readonly WorldSeasonVisualProfile Longnight = new(
        WorldSeasonVisualVariant.Longnight,
        LongnightPropAtlasTexturePath,
        LongnightWorldBackdropTexturePath
    );

    public static WorldSeasonVisualProfile ForDay(int day) =>
        CalendarSystem.SeasonId(day) switch
        {
            CalendarSystem.RainveilSeasonId => Rainveil,
            CalendarSystem.StarharvestSeasonId => Starharvest,
            CalendarSystem.LongnightSeasonId => Longnight,
            _ => Default
        };

}
