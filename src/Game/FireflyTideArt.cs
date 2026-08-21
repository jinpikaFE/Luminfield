using Godot;

namespace Luminfield.Game;

internal static class FireflyTideArt
{
    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/firefly-tide/firefly_tide_props.png"
    );

    public static readonly Rect2 LanternLaunchRegion =
        new(101, 111, 525, 479);
    public static readonly Rect2 FishBasinRegion =
        new(707, 154, 438, 408);
    public static readonly Rect2 GlowshopRegion =
        new(120, 680, 430, 438);
    public static readonly Rect2 TideAltarRegion =
        new(732, 670, 362, 458);
}
