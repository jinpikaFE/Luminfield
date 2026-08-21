using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class StarfeatherArt
{
    private const int CellSize = 256;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/animals/starfeather/starfeather_chicken.png"
    );

    private static readonly Rect2[] IconRegions =
    [
        new(70, 2 * CellSize + 34, 115, 188),
        new(CellSize + 86, 2 * CellSize + 34, 85, 188),
        new(2 * CellSize + 76, 2 * CellSize + 34, 104, 188),
        new(3 * CellSize + 76, 2 * CellSize + 34, 103, 188),
        new(4 * CellSize + 73, 2 * CellSize + 34, 110, 188),
        new(5 * CellSize + 64, 2 * CellSize + 34, 129, 188),
        new(6 * CellSize + 66, 2 * CellSize + 34, 124, 188),
        new(7 * CellSize + 84, 2 * CellSize + 34, 87, 188)
    ];

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        var visualItemId = DataCatalog.BaseItemId(itemId);
        region = visualItemId switch
        {
            DataCatalog.MeadowFodderId => IconRegions[0],
            DataCatalog.StarfeatherEggId => IconRegions[1],
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    public static AtlasTexture AnimalTexture(
        bool adult,
        NpcFacing facing,
        bool step
    )
    {
        var column = facing switch
        {
            NpcFacing.Down => step ? 1 : 0,
            NpcFacing.Up => step ? 3 : 2,
            NpcFacing.Left => step ? 5 : 4,
            NpcFacing.Right => step ? 7 : 6,
            _ => 0
        };
        var region = adult
            ? new Rect2(column * CellSize + 38, CellSize + 28, 180, 204)
            : new Rect2(column * CellSize + 57, 72, 142, 160);
        return new AtlasTexture
        {
            Atlas = Atlas,
            Region = region,
            FilterClip = true
        };
    }

    public static AtlasTexture MoodIcon(int mood) => Icon(
        mood >= 4 ? 4 : mood >= 2 ? 3 : 2
    );

    public static AtlasTexture CaredIcon() => Icon(5);
    public static AtlasTexture ProductReadyIcon() => Icon(6);
    public static AtlasTexture GrazingIcon() => Icon(7);

    private static AtlasTexture Icon(int column) => new()
    {
        Atlas = Atlas,
        Region = IconRegions[column],
        FilterClip = true
    };
}

internal sealed partial class StarfeatherChickenVisual : Node2D
{
    private readonly GameSession _session;
    private readonly bool _worldProjection;
    private readonly Sprite2D _animal;
    private readonly Sprite2D _status;
    private double _elapsed;
    private bool _step;

    public StarfeatherChickenVisual(
        GameSession session,
        bool worldProjection
    )
    {
        _session = session;
        _worldProjection = worldProjection;
        Name = worldProjection
            ? "StarfeatherChickenPasture"
            : "StarfeatherChickenInterior";
        ZIndex = 8;

        _animal = new Sprite2D
        {
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _animal.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 0),
            ZIndex = -1
        });
        AddChild(_animal);

        _status = new Sprite2D
        {
            Position = new Vector2(0, -31),
            Scale = Vector2.One * 0.085f,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ZIndex = 2
        };
        AddChild(_status);

        session.Changed += Refresh;
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _elapsed += delta;
        if (_elapsed < 0.45)
        {
            return;
        }

        _elapsed = 0;
        _step = !_step;
        RefreshAnimalTexture();
    }

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
    }

    private void Refresh()
    {
        var animal = _session.Animals.Animal(
            AnimalCatalog.StarterStarfeatherChickenId
        );
        var cell = _session.VisibleStarfeatherChickenCell;
        var expectedLocation = _worldProjection
            ? PlayerLocationIds.World
            : PlayerLocationIds.StarfeatherCoop;
        Visible = animal is not null &&
            cell is not null &&
            _session.PlayerLocationId == expectedLocation;
        if (!Visible || animal is null || cell is null)
        {
            return;
        }

        Position = new Vector2(
            cell.Value.X * 16 + 8,
            cell.Value.Y * 16 + 15
        );
        RefreshAnimalTexture();
        _status.Texture = animal.HasPendingProduct
            ? StarfeatherArt.ProductReadyIcon()
            : animal.LastPettedDay == _session.Clock.Day
                ? StarfeatherArt.CaredIcon()
                : _session.StarfeatherChickenIsOutdoors
                    ? StarfeatherArt.GrazingIcon()
                    : StarfeatherArt.MoodIcon(animal.Mood);
    }

    private void RefreshAnimalTexture()
    {
        var animal = _session.Animals.Animal(
            AnimalCatalog.StarterStarfeatherChickenId
        );
        if (animal is null)
        {
            return;
        }

        var facing = FacingPlayer();
        var sourceHeight = animal.IsAdult ? 204f : 160f;
        var destinationHeight = animal.IsAdult ? 30f : 22f;
        _animal.Texture = StarfeatherArt.AnimalTexture(
            animal.IsAdult,
            facing,
            _step
        );
        _animal.Offset = new Vector2(0, -sourceHeight / 2f);
        _animal.Scale = Vector2.One * (destinationHeight / sourceHeight);
    }

    private NpcFacing FacingPlayer()
    {
        var cell = _session.VisibleStarfeatherChickenCell;
        if (cell is null)
        {
            return NpcFacing.Down;
        }

        var dx = _session.PlayerCell.X - cell.Value.X;
        var dy = _session.PlayerCell.Y - cell.Value.Y;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx < 0 ? NpcFacing.Left : NpcFacing.Right;
        }

        return dy < 0 ? NpcFacing.Up : NpcFacing.Down;
    }
}
