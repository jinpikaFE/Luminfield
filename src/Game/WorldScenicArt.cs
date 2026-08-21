using Godot;

namespace Luminfield.Game;

internal static class WorldScenicArt
{
    private const float CellWidth = 384f;
    private const float CellHeight = 512f;

    public static readonly Texture2D Atlas =
        GD.Load<Texture2D>(
            "res://assets/generated/world_scenic_landmarks.png"
        );

    public static Rect2 Region(int atlasIndex) => new(
        atlasIndex % 4 * CellWidth,
        atlasIndex / 4 * CellHeight,
        CellWidth,
        CellHeight
    );

    public static float DrawHeight(int atlasIndex) => atlasIndex switch
    {
        0 => 118f,
        1 => 104f,
        2 => 122f,
        3 => 102f,
        4 => 112f,
        5 => 94f,
        6 => 108f,
        7 => 96f,
        _ => 96f
    };
}
