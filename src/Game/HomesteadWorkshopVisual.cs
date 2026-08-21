using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class HomesteadWorkshopArt
{
    public const int AtlasCellSize = 627;
    public const float RegisteredBaseline = 590;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/homestead_workshop.png"
    );

    public static AtlasTexture ProjectIconTexture() => new()
    {
        Atlas = Atlas,
        Region = new Rect2(751, 945, 379, 272),
        FilterClip = true
    };

    public static AtlasTexture TextureFor(ConstructionPhase phase) =>
        phase switch
        {
            ConstructionPhase.InProgress => Region(1, 0),
            ConstructionPhase.Completed => Region(0, 1),
            _ => Region(0, 0)
        };

    public static float ScaleFor(ConstructionPhase phase) => phase switch
    {
        ConstructionPhase.InProgress => 0.09f,
        ConstructionPhase.Completed => 0.10f,
        _ => 0.075f
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

internal sealed partial class HomesteadWorkshopVisual : Node2D
{
    private readonly GameSession _session;
    private readonly Sprite2D _sprite;

    public HomesteadWorkshopVisual(GameSession session)
    {
        _session = session;
        Name = "HomesteadWorkshop";
        _sprite = new Sprite2D
        {
            Name = "WorkshopState",
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(
                0,
                -(HomesteadWorkshopArt.RegisteredBaseline -
                    HomesteadWorkshopArt.AtlasCellSize / 2f)
            )
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
        var phase = _session.Construction.PhaseFor(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        _sprite.Texture = HomesteadWorkshopArt.TextureFor(phase);
        _sprite.Scale = Vector2.One * HomesteadWorkshopArt.ScaleFor(phase);
    }
}
