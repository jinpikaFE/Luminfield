using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class HomesteadGreenhouseArt
{
    public const int AtlasCellSize = 627;
    public const float RegisteredBaseline = 590;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/features/construction/homestead_greenhouse_portal.png"
    );

    public static AtlasTexture ProjectIconTexture() => new()
    {
        Atlas = Atlas,
        Region = new Rect2(846, 1006, 188, 211),
        FilterClip = true
    };

    public static AtlasTexture TextureFor(ConstructionPhase phase) =>
        phase switch
        {
            ConstructionPhase.InProgress => Region(1, 0),
            ConstructionPhase.Completed => Region(0, 1),
            _ => Region(0, 0)
        };

    private static AtlasTexture Region(int column, int row) => new()
    {
        Atlas = Atlas,
        Region = new Rect2(
            column * AtlasCellSize,
            row * AtlasCellSize,
            AtlasCellSize,
            AtlasCellSize
        ),
        FilterClip = true
    };
}

internal sealed partial class HomesteadGreenhouseVisual : Node2D
{
    private readonly GameSession _session;
    private readonly Sprite2D _sprite;

    public HomesteadGreenhouseVisual(GameSession session)
    {
        _session = session;
        Name = "HomesteadGreenhousePortal";
        _sprite = new Sprite2D
        {
            Name = "GreenhousePortalState",
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(
                0,
                -(HomesteadGreenhouseArt.RegisteredBaseline -
                    HomesteadGreenhouseArt.AtlasCellSize / 2f)
            ),
            Scale = Vector2.One * 0.10f
        };
        AddChild(_sprite);
        session.Construction.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _session.Construction.Changed -= Refresh;
    }

    private void Refresh()
    {
        _sprite.Texture = HomesteadGreenhouseArt.TextureFor(
            _session.Construction.PhaseFor(
                ConstructionCatalog.HomesteadGreenhouseProjectId
            )
        );
    }
}
