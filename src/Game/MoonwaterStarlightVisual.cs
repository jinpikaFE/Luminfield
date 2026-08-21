using Godot;

namespace Luminfield.Game;

internal static class MoonwaterStarlightArt
{
    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/features/starlights/moonwater_starlight_pedestal.png"
    );

    private static readonly Rect2 DormantRegion = new(54, 70, 520, 520);
    private static readonly Rect2 RestoredRegion = new(681, 70, 520, 520);
    private static readonly Rect2 NodeSealRegion = new(74, 701, 480, 480);
    private static readonly Rect2 TideBlessingRegion =
        new(701, 701, 480, 480);

    public static Rect2 PedestalRegion(bool restored) =>
        restored ? RestoredRegion : DormantRegion;

    public static AtlasTexture NodeSealTexture() => new()
    {
        Atlas = Atlas,
        Region = NodeSealRegion,
        FilterClip = true
    };

    public static AtlasTexture TideBlessingTexture() => new()
    {
        Atlas = Atlas,
        Region = TideBlessingRegion,
        FilterClip = true
    };
}
