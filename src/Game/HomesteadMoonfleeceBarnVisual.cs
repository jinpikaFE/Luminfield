using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class HomesteadMoonfleeceBarnArt
{
    public const int AtlasCellSize = 627;
    public const float RegisteredBaseline = 590;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/animals/moonfleece/homestead_moonfleece_barn.png"
    );

    public static AtlasTexture ProjectIconTexture() => new()
    {
        Atlas = Atlas,
        Region = new Rect2(744, 813, 393, 254),
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

internal sealed partial class HomesteadMoonfleeceBarnVisual : Node2D
{
    private readonly GameSession _session;
    private readonly Sprite2D _sprite;

    public HomesteadMoonfleeceBarnVisual(GameSession session)
    {
        _session = session;
        Name = "HomesteadMoonfleeceBarn";
        _sprite = new Sprite2D
        {
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(
                0,
                -(HomesteadMoonfleeceBarnArt.RegisteredBaseline -
                    HomesteadMoonfleeceBarnArt.AtlasCellSize / 2f)
            ),
            Scale = Vector2.One * 0.105f
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
        _sprite.Texture = HomesteadMoonfleeceBarnArt.TextureFor(
            _session.Construction.PhaseFor(
                ConstructionCatalog.HomesteadMoonfleeceBarnProjectId
            )
        );
    }
}
