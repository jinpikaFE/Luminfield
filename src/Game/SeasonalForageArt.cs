using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal sealed record SeasonalForageArtDefinition(
    int Column,
    int WorldRow,
    int IconRow,
    Vector2 WorldSize
);

internal static class SeasonalForageArt
{
    public const float CellSize = 256;

    public static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/seasonal_forage.png"
    );

    private static readonly IReadOnlyDictionary<string, SeasonalForageArtDefinition>
        Definitions = new Dictionary<string, SeasonalForageArtDefinition>(
            StringComparer.Ordinal
        )
        {
            [DataCatalog.WhisperbloomId] = new(0, 0, 1, new(32, 31)),
            [DataCatalog.DewglassCloverId] = new(0, 2, 3, new(32, 30)),
            [DataCatalog.RainbellMossId] = new(1, 0, 1, new(32, 31)),
            [DataCatalog.MistcoilFernId] = new(1, 2, 3, new(29, 31)),
            [DataCatalog.GloamgoldBerryId] = new(2, 0, 1, new(31, 30)),
            [DataCatalog.SunwispPodId] = new(2, 2, 3, new(32, 28)),
            [DataCatalog.NightlampLichenId] = new(3, 0, 1, new(34, 29)),
            [DataCatalog.FrostwickRootId] = new(3, 2, 3, new(29, 31))
        };

    public static Rect2 WorldRegion(string itemId)
    {
        var definition = Definition(itemId);
        return Region(definition.Column, definition.WorldRow);
    }

    public static Rect2 IconRegion(string itemId)
    {
        var definition = Definition(itemId);
        return Region(definition.Column, definition.IconRow);
    }

    public static Vector2 WorldSize(string itemId) => Definition(itemId).WorldSize;

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        if (!Definitions.TryGetValue(itemId, out var definition))
        {
            region = default;
            return false;
        }

        region = Region(definition.Column, definition.IconRow);
        return true;
    }

    private static SeasonalForageArtDefinition Definition(string itemId) =>
        Definitions.TryGetValue(itemId, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Missing seasonal forage art for '{itemId}'."
            );

    private static Rect2 Region(int column, int row) => new(
        column * CellSize,
        row * CellSize,
        CellSize,
        CellSize
    );
}
