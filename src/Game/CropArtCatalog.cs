using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class CropArtCatalog
{
    private static readonly Texture2D BaseCropStages =
        GD.Load<Texture2D>(
            "res://assets/generated/farming/crops/crop_stages_chroma.png"
        );

    private static readonly IReadOnlyDictionary<string, Rect2[]> BaseFrames =
        new Dictionary<string, Rect2[]>(StringComparer.Ordinal)
        {
            [DataCatalog.StarbudId] =
            [
                new Rect2(92, 315, 190, 150),
                new Rect2(405, 280, 235, 185),
                new Rect2(775, 130, 265, 345),
                new Rect2(1140, 82, 310, 395)
            ],
            [DataCatalog.MoonrootId] =
            [
                new Rect2(100, 728, 185, 160),
                new Rect2(400, 700, 255, 195),
                new Rect2(735, 632, 330, 275),
                new Rect2(1090, 530, 380, 385)
            ]
        };

    public static bool TryGrowthFrame(
        string cropId,
        int stageIndex,
        out Texture2D texture,
        out Rect2 region,
        out Material? material
    )
    {
        stageIndex = Math.Clamp(stageIndex, 0, 3);
        material = null;
        if (GeneratedArt.TryStarharvestCropRow(cropId, out var row))
        {
            texture = GeneratedArt.StarharvestCropsTexture;
            region = GeneratedArt.StarharvestCropRegion(
                row,
                stageIndex + 2
            );
            return true;
        }

        if (GeneratedArt.TryRainveilCropRow(cropId, out row))
        {
            texture = GeneratedArt.RainveilCropsTexture;
            region = GeneratedArt.RainveilCropRegion(row, stageIndex + 2);
            return true;
        }

        if (GeneratedArt.TryGleamriseCropRow(cropId, out row))
        {
            texture = GeneratedArt.GleamriseCropsTexture;
            region = GeneratedArt.GleamriseCropRegion(row, stageIndex + 2);
            return true;
        }

        if (GeneratedArt.TryCropExpansionRow(cropId, out row))
        {
            texture = GeneratedArt.CropExpansionTexture;
            region = GeneratedArt.CropExpansionRegion(row, stageIndex + 2);
            return true;
        }

        if (BaseFrames.TryGetValue(cropId, out var frames))
        {
            texture = BaseCropStages;
            region = frames[stageIndex];
            material = GeneratedArt.CreateChromaKeyMaterial();
            return true;
        }

        texture = null!;
        region = default;
        return false;
    }

    public static Texture2D ItemIcon(string itemId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            throw new InvalidOperationException(
                $"Missing crop codex icon for '{itemId}'."
            );
        }

        return new AtlasTexture
        {
            Atlas = texture,
            Region = region,
            FilterClip = true
        };
    }
}
