using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class GeneratedArt
{
    private static readonly Texture2D Characters =
        GD.Load<Texture2D>("res://assets/generated/character_directions_chroma.png");

    private static readonly Texture2D PlayerWalkCycle =
        GD.Load<Texture2D>("res://assets/generated/player_walk_cycle_chroma.png");

    private static readonly Texture2D EconomyAssets =
        GD.Load<Texture2D>("res://assets/generated/economy_assets_chroma.png");

    private static readonly Rect2 StarbudPreserveRegion = new(185, 125, 275, 330);
    private static readonly Rect2 MoonrootTonicRegion = new(805, 75, 220, 420);
    private static readonly Rect2 MarketStallRegion = new(55, 630, 515, 565);
    private static readonly Rect2 MoonwellInfuserRegion = new(665, 615, 505, 535);

    private static readonly Rect2[] PlayerFrames =
    [
        new Rect2(105, 38, 245, 410),
        new Rect2(482, 46, 250, 407),
        new Rect2(840, 52, 220, 405),
        new Rect2(1200, 50, 225, 405),
    ];

    private static readonly float[] PlayerFrameBottomInsets = [15, 20, 21, 19];

    private static readonly Rect2[][] PlayerWalkFrames =
    [
        [
            new Rect2(108, 55, 240, 410),
            new Rect2(490, 65, 235, 400),
            new Rect2(845, 72, 230, 395),
            new Rect2(1208, 76, 220, 395),
        ],
        [
            new Rect2(108, 530, 235, 395),
            new Rect2(490, 542, 235, 385),
            new Rect2(845, 552, 225, 380),
            new Rect2(1208, 554, 220, 385),
        ],
    ];

    private static readonly float[][] PlayerWalkFrameBottomInsets =
    [
        [25, 23, 31, 33],
        [2, 4, 14, 18],
    ];

    private static readonly Rect2[] MiraFrames =
    [
        new Rect2(112, 520, 230, 450),
        new Rect2(480, 520, 250, 450),
        new Rect2(842, 520, 215, 450),
        new Rect2(1205, 520, 215, 450),
    ];

    public static Sprite2D CreatePlayerSprite()
    {
        var sprite = CreateCharacterSprite(PlayerFrames, 48);
        SetPlayerFrame(sprite, Vector2I.Down, false, 0);
        sprite.Position = new Vector2(0, 8);
        return sprite;
    }

    public static Sprite2D CreateMiraSprite()
    {
        var sprite = CreateCharacterSprite(MiraFrames, 52);
        sprite.Position = new Vector2(0, -18);
        return sprite;
    }

    public static Sprite2D CreateMarketStallSprite() =>
        CreateEconomySprite(MarketStallRegion, 78);

    public static Sprite2D CreateMoonwellInfuserSprite() =>
        CreateEconomySprite(MoonwellInfuserRegion, 70);

    public static (Texture2D Texture, Rect2 Region) EconomyItemIcon(string itemId) =>
        itemId switch
        {
            DataCatalog.StarbudPreserveId => (EconomyAssets, StarbudPreserveRegion),
            DataCatalog.MoonrootTonicId => (EconomyAssets, MoonrootTonicRegion),
            _ => (null!, default)
        };

    public static void SetPlayerFrame(
        Sprite2D sprite,
        Vector2I facing,
        bool isWalking,
        int walkFrame
    )
    {
        var directionIndex = DirectionIndex(facing);
        var frameIndex = Math.Clamp(walkFrame, 0, PlayerWalkFrames.Length - 1);
        var source = isWalking
            ? PlayerWalkFrames[frameIndex][directionIndex]
            : PlayerFrames[directionIndex];
        var bottomInset = isWalking
            ? PlayerWalkFrameBottomInsets[frameIndex][directionIndex]
            : PlayerFrameBottomInsets[directionIndex];
        sprite.Texture = isWalking ? PlayerWalkCycle : Characters;
        sprite.RegionRect = source;
        // Pivot every generated frame at the visible boot sole instead of the region center.
        // The generated atlas has different transparent padding per direction and stride.
        sprite.Offset = new Vector2(0, bottomInset - source.Size.Y / 2f);
        var scale = 48f / source.Size.Y;
        sprite.Scale = new Vector2(scale, scale);
    }

    public static ShaderMaterial CreateChromaKeyMaterial()
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;

                void fragment() {
                    vec4 pixel = texture(TEXTURE, UV);
                    float other = max(pixel.g, pixel.b);
                    bool chroma = pixel.r > 0.45
                        && pixel.g < 0.32
                        && pixel.b < 0.32
                        && pixel.r > other * 2.0;
                    if (chroma) {
                        pixel.a = 0.0;
                    }
                    COLOR = pixel;
                }
                """
        };
        return new ShaderMaterial { Shader = shader };
    }

    private static Sprite2D CreateCharacterSprite(Rect2[] frames, float targetHeight)
    {
        var source = frames[0];
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = Characters,
            RegionEnabled = true,
            RegionRect = source,
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Material = CreateChromaKeyMaterial()
        };
    }

    private static Sprite2D CreateEconomySprite(Rect2 source, float targetHeight)
    {
        var scale = targetHeight / source.Size.Y;
        return new Sprite2D
        {
            Texture = EconomyAssets,
            RegionEnabled = true,
            RegionRect = source,
            Offset = new Vector2(0, -source.Size.Y / 2f),
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Material = CreateChromaKeyMaterial()
        };
    }

    private static int DirectionIndex(Vector2I facing)
    {
        if (facing == Vector2I.Up)
        {
            return 1;
        }

        // The generated side-profile cells describe the visible side: cell 2 faces left,
        // while cell 3 faces right.
        if (facing == Vector2I.Right)
        {
            return 3;
        }

        return facing == Vector2I.Left ? 2 : 0;
    }
}

internal sealed partial class CottageBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>("res://assets/generated/cottage_twilight_interior.png");

    public CottageBackdrop()
    {
        ZIndex = -100;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class GeneratedCropLayer : Node2D
{
    private static readonly Texture2D Crops =
        GD.Load<Texture2D>("res://assets/generated/crop_stages_chroma.png");

    private static readonly Rect2[] StarbudFrames =
    [
        new Rect2(92, 315, 190, 150),
        new Rect2(405, 280, 235, 185),
        new Rect2(775, 130, 265, 345),
        new Rect2(1140, 82, 310, 395),
    ];

    private static readonly Rect2[] MoonrootFrames =
    [
        new Rect2(100, 728, 185, 160),
        new Rect2(400, 700, 255, 195),
        new Rect2(735, 632, 330, 275),
        new Rect2(1090, 530, 380, 385),
    ];

    private readonly GameSession _session;

    public GeneratedCropLayer(GameSession session)
    {
        _session = session;
        ZIndex = 1;
        session.Farm.TileChanged += OnTileChanged;
        Rebuild();
    }

    public override void _ExitTree()
    {
        _session.Farm.TileChanged -= OnTileChanged;
    }

    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        foreach (var tile in _session.Farm.Tiles.Values)
        {
            if (string.IsNullOrWhiteSpace(tile.CropId))
            {
                continue;
            }

            var definition = DataCatalog.Crop(tile.CropId);
            var stage = definition.GetStageIndex(tile.WateredNights);
            var frames = tile.CropId == DataCatalog.StarbudId
                ? StarbudFrames
                : MoonrootFrames;
            var frameIndex = tile.CropId == DataCatalog.StarbudId && stage == 2
                ? 3
                : Math.Clamp(stage, 0, frames.Length - 1);
            var source = frames[frameIndex];
            var height = frameIndex switch
            {
                0 => 10f,
                1 => 16f,
                2 => 23f,
                _ => 29f
            };
            var baseline = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 15);
            var scale = height / source.Size.Y;
            AddChild(new Sprite2D
            {
                Texture = Crops,
                RegionEnabled = true,
                RegionRect = source,
                Scale = new Vector2(scale, scale),
                Position = baseline - new Vector2(0, height / 2),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Material = GeneratedArt.CreateChromaKeyMaterial()
            });
        }
    }

    private void OnTileChanged(GridPosition position)
    {
        _ = position;
        Rebuild();
    }
}

internal sealed partial class FarmSoilStateLayer : Node2D
{
    private readonly GameSession _session;

    public FarmSoilStateLayer(GameSession session)
    {
        _session = session;
        ZIndex = -1;
        session.Farm.TileChanged += OnTileChanged;
    }

    public override void _Draw()
    {
        foreach (var tile in _session.Farm.Tiles.Values)
        {
            if (!tile.Tilled)
            {
                continue;
            }

            var origin = new Vector2(tile.X * 16, tile.Y * 16);
            var soil = tile.Watered
                ? new Color("#18394ed9")
                : new Color("#2b202bd9");
            var ridge = tile.Watered
                ? new Color("#4f8293d0")
                : new Color("#6f4e52c9");
            DrawColoredPolygon(
                [
                    origin + new Vector2(1, 6),
                    origin + new Vector2(4, 2),
                    origin + new Vector2(12, 2),
                    origin + new Vector2(15, 6),
                    origin + new Vector2(14, 12),
                    origin + new Vector2(10, 14),
                    origin + new Vector2(4, 13),
                    origin + new Vector2(1, 10),
                ],
                soil
            );
            DrawLine(origin + new Vector2(3, 6), origin + new Vector2(13, 5), ridge, 1);
            DrawLine(origin + new Vector2(3, 10), origin + new Vector2(12, 9), ridge, 1);

            if (tile.Watered)
            {
                DrawCircle(origin + new Vector2(5, 4), 1, new Color("#8ee6becf"));
                DrawCircle(origin + new Vector2(12, 11), 0.8f, new Color("#4bc5bdc8"));
            }
        }
    }

    public override void _ExitTree()
    {
        _session.Farm.TileChanged -= OnTileChanged;
    }

    private void OnTileChanged(GridPosition position)
    {
        _ = position;
        QueueRedraw();
    }
}
