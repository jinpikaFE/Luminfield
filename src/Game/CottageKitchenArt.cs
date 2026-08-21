using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public static class CottageKitchenArt
{
    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/locations/cottage/cottage_kitchen_assets.png"
    );

    private const float CellWidth = 384;
    private const float CellHeight = 512;

    public static Texture2D ProjectIconTexture() => Cell(0, 0);
    public static Texture2D KitchenIconTexture() => Cell(0, 1);
    public static Texture2D PantryIconTexture() => Cell(0, 2);
    public static Texture2D CookedDishIconTexture() => Cell(1, 3);

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        region = itemId switch
        {
            DataCatalog.MoonmistStewId => Region(0, 3),
            DataCatalog.SunvaultHashId => Region(1, 0),
            DataCatalog.StarhoneyCustardId => Region(1, 1),
            DataCatalog.LanternrootBrothId => Region(1, 2),
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    public static Texture2D ItemIconTexture(string itemId)
    {
        if (!TryItemIcon(itemId, out var texture, out var region))
        {
            throw new KeyNotFoundException(
                $"Unknown cottage kitchen item icon '{itemId}'."
            );
        }

        return new AtlasTexture
        {
            Atlas = texture,
            Region = region,
            FilterClip = true
        };
    }

    private static AtlasTexture Cell(int row, int column) => new()
    {
        Atlas = Atlas,
        Region = Region(row, column),
        FilterClip = true
    };

    private static Rect2 Region(int row, int column) => new(
        column * CellWidth,
        row * CellHeight,
        CellWidth,
        CellHeight
    );
}
