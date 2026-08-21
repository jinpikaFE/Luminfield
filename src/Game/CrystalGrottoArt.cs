using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class CrystalGrottoArt
{
    public const float CellSize = 256;

    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/crystal_grotto_assets.png"
    );

    private static readonly IReadOnlyDictionary<string, int> MineralColumns =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [DataCatalog.LumenSlateOreId] = 0,
            [DataCatalog.MoonveinOreId] = 1,
            [DataCatalog.PrismheartOreId] = 2,
            [DataCatalog.StarironOreId] = 3
        };

    public static Rect2 MineralVeinRegion(string itemId) =>
        Region(MineralColumn(itemId), 0);

    public static Rect2 MineralIconRegion(string itemId) =>
        Region(MineralColumn(itemId), 1);

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        if (!MineralColumns.TryGetValue(itemId, out var column))
        {
            region = default;
            return false;
        }

        region = Region(column, 1);
        return true;
    }

    public static Rect2 EntranceRegion => Region(0, 2);
    public static Rect2 SealRegion => Region(1, 2);
    public static Rect2 DepthAnchorRegion => Region(2, 2);
    public static Rect2 BronzeStarShovelRegion => Region(3, 2);

    private static int MineralColumn(string itemId) =>
        MineralColumns.TryGetValue(itemId, out var column)
            ? column
            : throw new KeyNotFoundException(
                $"Missing crystal-grotto art for mineral '{itemId}'."
            );

    private static Rect2 Region(int column, int row) => new(
        column * CellSize,
        row * CellSize,
        CellSize,
        CellSize
    );
}

internal static class CrystalValeStarlightArt
{
    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/crystal_vale_starlight_pedestal.png"
    );

    private static readonly Rect2 DormantRegion =
        new(91, 70, 444, 520);
    private static readonly Rect2 RestoredRegion =
        new(718, 70, 445, 520);
    private static readonly Rect2 NodeSealRegion =
        new(150, 743, 326, 395);
    private static readonly Rect2 PassageRewardRegion =
        new(799, 759, 283, 362);

    public static Rect2 PedestalRegion(bool restored) =>
        restored ? RestoredRegion : DormantRegion;

    public static AtlasTexture NodeSealTexture() => new()
    {
        Atlas = Atlas,
        Region = NodeSealRegion,
        FilterClip = true
    };

    public static AtlasTexture PassageRewardTexture() => new()
    {
        Atlas = Atlas,
        Region = PassageRewardRegion,
        FilterClip = true
    };
}
