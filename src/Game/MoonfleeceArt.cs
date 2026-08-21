using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class MoonfleeceArt
{
    private const int CellSize = 256;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/animals/moonfleece/moonfleece_sheep.png"
    );

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        region = DataCatalog.BaseItemId(itemId) == DataCatalog.MoonfleeceId
            ? new Rect2(34, 3 * CellSize + 34, 188, 188)
            : default;
        return region.Size != Vector2.Zero;
    }

    public static AtlasTexture AnimalTexture(
        AnimalState animal,
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
        var row = !animal.IsAdult ? 0 : animal.HasPendingProduct ? 2 : 1;
        return new AtlasTexture
        {
            Atlas = Atlas,
            Region = new Rect2(
                column * CellSize,
                row * CellSize,
                CellSize,
                CellSize
            ),
            FilterClip = true
        };
    }

    public static AtlasTexture MoodIcon(int mood) => Icon(
        mood >= 4 ? 3 : mood >= 2 ? 2 : 1
    );

    public static AtlasTexture CaredIcon() => Icon(4);
    public static AtlasTexture ProductReadyIcon() => Icon(5);

    private static AtlasTexture Icon(int column) => new()
    {
        Atlas = Atlas,
        Region = new Rect2(
            column * CellSize + 34,
            3 * CellSize + 34,
            188,
            188
        ),
        FilterClip = true
    };
}

internal sealed partial class MoonfleeceSheepVisual : Node2D
{
    private readonly GameSession _session;
    private readonly bool _worldProjection;
    private readonly Sprite2D _animal;
    private readonly Sprite2D _status;
    private double _elapsed;
    private bool _step;

    public MoonfleeceSheepVisual(
        GameSession session,
        bool worldProjection
    )
    {
        _session = session;
        _worldProjection = worldProjection;
        Name = worldProjection
            ? "MoonfleeceSheepPasture"
            : "MoonfleeceSheepInterior";
        ZIndex = 8;

        _animal = new Sprite2D
        {
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _animal.AddChild(new ActorShadow
        {
            ZIndex = -1
        });
        AddChild(_animal);

        _status = new Sprite2D
        {
            Position = new Vector2(0, -34),
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
        if (_elapsed < 0.48)
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
            AnimalCatalog.StarterMoonfleeceSheepId
        );
        var projection = _session.VisibleAnimalProjections
            .FirstOrDefault(candidate => candidate.InstanceId ==
                AnimalCatalog.StarterMoonfleeceSheepId);
        Visible = animal is not null &&
            projection is not null &&
            projection.IsOutdoors == _worldProjection;
        if (!Visible || animal is null || projection is null)
        {
            return;
        }

        Position = new Vector2(
            projection.Cell.X * 16 + 8,
            projection.Cell.Y * 16 + 15
        );
        RefreshAnimalTexture();
        _status.Texture = animal.HasPendingProduct
            ? MoonfleeceArt.ProductReadyIcon()
            : animal.LastPettedDay == _session.Clock.Day
                ? MoonfleeceArt.CaredIcon()
                : MoonfleeceArt.MoodIcon(animal.Mood);
    }

    private void RefreshAnimalTexture()
    {
        var animal = _session.Animals.Animal(
            AnimalCatalog.StarterMoonfleeceSheepId
        );
        if (animal is null)
        {
            return;
        }

        var destinationHeight = !animal.IsAdult
            ? 24f
            : animal.HasPendingProduct ? 34f : 30f;
        var sourceHeight = !animal.IsAdult ? 160f : 214f;
        _animal.Texture = MoonfleeceArt.AnimalTexture(
            animal,
            FacingPlayer(),
            _step
        );
        _animal.Offset = new Vector2(0, -104);
        _animal.Scale = Vector2.One * (destinationHeight / sourceHeight);
    }

    private NpcFacing FacingPlayer()
    {
        var projection = _session.VisibleAnimalProjections
            .FirstOrDefault(candidate => candidate.InstanceId ==
                AnimalCatalog.StarterMoonfleeceSheepId);
        if (projection is null)
        {
            return NpcFacing.Down;
        }

        var dx = _session.PlayerCell.X - projection.Cell.X;
        var dy = _session.PlayerCell.Y - projection.Cell.Y;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx < 0 ? NpcFacing.Left : NpcFacing.Right;
        }

        return dy < 0 ? NpcFacing.Up : NpcFacing.Down;
    }
}
