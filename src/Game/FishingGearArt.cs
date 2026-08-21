using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class FishingGearArt
{
    private const float Cell = 512;
    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/activities/fishing/fishing_gear.png"
    );

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        region = itemId switch
        {
            DataCatalog.GlowgrubBaitId => Region(3, 0),
            DataCatalog.MoonmoteBaitId => Region(0, 1),
            DataCatalog.StillwaterBobberId => Region(1, 1),
            DataCatalog.StormglassBobberId => Region(2, 1),
            DataCatalog.MoonreedCrabPotId => Region(3, 2),
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    public static Texture2D RodTierIcon(string tierId) => Icon(
        tierId switch
        {
            FishingProgressionCatalog.MoonthreadRodTierId => Region(1, 0),
            FishingProgressionCatalog.TideglassRodTierId => Region(2, 0),
            _ => Region(0, 0)
        }
    );

    public static Texture2D SpecializationIcon(string specializationId) =>
        Icon(specializationId ==
            FishingProgressionCatalog.DeepThreaderSpecializationId
                ? Region(1, 3)
                : Region(0, 3));

    public static Texture2D CastRippleIcon() => Icon(Region(2, 3));

    public static Texture2D HookedFishIcon() => Icon(Region(3, 3));

    public static Sprite2D CreateCrabPotSprite(CrabPotState state)
    {
        var column = 0;
        if (state.IsReady)
        {
            column = 2;
        }
        else if (state.IsBaited)
        {
            column = 1;
        }

        return new Sprite2D
        {
            Texture = Atlas,
            RegionEnabled = true,
            RegionRect = Region(column, 2),
            Scale = Vector2.One * (34f / Cell),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(0, -Cell * 0.12f)
        };
    }

    private static AtlasTexture Icon(Rect2 region) => new()
    {
        Atlas = Atlas,
        Region = region,
        FilterClip = true
    };

    private static Rect2 Region(int column, int row) => new(
        column * Cell,
        row * Cell,
        Cell,
        Cell
    );
}
