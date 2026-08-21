using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class HomesteadStarlightArt
{
    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/features/starlights/homestead_starlight_pedestal.png"
    );
    private static readonly Rect2 DormantRegion = new(54, 70, 520, 520);
    private static readonly Rect2 RestoredRegion = new(681, 70, 520, 520);
    private static readonly Rect2 NodeSealRegion = new(74, 701, 480, 480);
    private static readonly Rect2 IrrigationBlessingRegion =
        new(701, 701, 480, 480);

    public static AtlasTexture PedestalTexture(bool restored) => new()
    {
        Atlas = Atlas,
        Region = restored ? RestoredRegion : DormantRegion,
        FilterClip = true
    };

    public static AtlasTexture NodeSealTexture() => new()
    {
        Atlas = Atlas,
        Region = NodeSealRegion,
        FilterClip = true
    };

    public static AtlasTexture IrrigationBlessingTexture() => new()
    {
        Atlas = Atlas,
        Region = IrrigationBlessingRegion,
        FilterClip = true
    };
}

internal sealed partial class HomesteadStarlightVisual : Node2D
{
    private readonly GameSession _session;
    private readonly Sprite2D _sprite;

    public HomesteadStarlightVisual(GameSession session)
    {
        _session = session;
        Name = "HomesteadStarlight";
        _sprite = new Sprite2D
        {
            Name = "PedestalState",
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(0, -260),
            Scale = Vector2.One * (78f / 520f)
        };
        AddChild(_sprite);
        session.Starlight.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _session.Starlight.Changed -= Refresh;
    }

    private void Refresh()
    {
        _sprite.Texture = HomesteadStarlightArt.PedestalTexture(
            _session.Starlight.HomesteadIrrigationUnlocked
        );
    }
}
