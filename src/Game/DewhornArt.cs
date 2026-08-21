using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class DewhornArt
{
    private const int CellSize = 256;

    private static readonly Texture2D Atlas = GD.Load<Texture2D>(
        "res://assets/generated/animals/dewhorn/dewhorn.png"
    );

    public static bool TryItemIcon(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = Atlas;
        region = DataCatalog.BaseItemId(itemId) == DataCatalog.DewhornMilkId
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
        return Cell(row, column);
    }

    public static AtlasTexture MoodIcon(int mood) => Icon(
        mood >= 4 ? 3 : mood >= 2 ? 2 : 1
    );

    public static AtlasTexture CaredIcon() => Icon(4);
    public static AtlasTexture ProductReadyIcon() => Icon(5);
    public static AtlasTexture MilkingStation(bool full) => Cell(
        3,
        full ? 7 : 6
    );

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

    private static AtlasTexture Cell(int row, int column) => new()
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

internal sealed partial class DewhornVisual : Node2D
{
    private readonly GameSession _session;
    private readonly bool _worldProjection;
    private readonly Sprite2D _animal;
    private readonly Sprite2D _status;
    private double _elapsed;
    private bool _step;

    public DewhornVisual(GameSession session, bool worldProjection)
    {
        _session = session;
        _worldProjection = worldProjection;
        Name = worldProjection ? "DewhornPasture" : "DewhornInterior";
        ZIndex = 8;

        _animal = new Sprite2D
        {
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _animal.AddChild(new ActorShadow { ZIndex = -1 });
        AddChild(_animal);

        _status = new Sprite2D
        {
            Position = new Vector2(0, -35),
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
        var animal = _session.Animals.Animal(AnimalCatalog.StarterDewhornId);
        var projection = Projection();
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
            ? DewhornArt.ProductReadyIcon()
            : animal.LastPettedDay == _session.Clock.Day
                ? DewhornArt.CaredIcon()
                : DewhornArt.MoodIcon(animal.Mood);
    }

    private void RefreshAnimalTexture()
    {
        var animal = _session.Animals.Animal(AnimalCatalog.StarterDewhornId);
        if (animal is null)
        {
            return;
        }

        _animal.Texture = DewhornArt.AnimalTexture(
            animal,
            FacingPlayer(),
            _step
        );
        _animal.Offset = new Vector2(0, -104);
        _animal.Scale = Vector2.One * (!animal.IsAdult ? 23f / 170f : 32f / 210f);
    }

    private NpcFacing FacingPlayer()
    {
        var projection = Projection();
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

    private AnimalProjection? Projection() =>
        _session.VisibleAnimalProjections.FirstOrDefault(candidate =>
            candidate.InstanceId == AnimalCatalog.StarterDewhornId
        );
}

internal sealed partial class DewhornMilkingStationVisual : Sprite2D
{
    private readonly GameSession _session;

    public DewhornMilkingStationVisual(GameSession session)
    {
        _session = session;
        Name = "DewhornMilkingStation";
        Position = new Vector2(
            MoonfleeceBarnLayout.MilkingStationCell.X * 16 + 8,
            MoonfleeceBarnLayout.MilkingStationCell.Y * 16 + 15
        );
        Offset = new Vector2(0, -104);
        Scale = Vector2.One * (48f / 214f);
        TextureFilter = TextureFilterEnum.Nearest;
        ZIndex = 6;
        session.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
    }

    private void Refresh()
    {
        var full = _session.Animals.PendingProductsForBuilding(
            AnimalCatalog.MoonfleeceBarnId,
            DataCatalog.DewhornMilkId
        ).Count > 0;
        Texture = DewhornArt.MilkingStation(full);
    }
}
