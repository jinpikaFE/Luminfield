using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CottageView : Node2D
{
    public static readonly GridPosition BedCell = new(15, 10);
    public static readonly GridPosition DoorCell = new(20, 18);

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public CottageView(GameSession session)
    {
        _session = session;
        var environment = TilePaletteFactory.CreateEnvironment();
        var baseLayer = new TileMapLayer
        {
            Name = "CottageFloor",
            TileSet = environment,
            ZIndex = -10,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(baseLayer);

        for (var y = 5; y <= 19; y++)
        {
            for (var x = 9; x <= 30; x++)
            {
                var atlas = x is 9 or 30 || y is 5 or 19
                    ? TilePaletteFactory.InteriorWall
                    : TilePaletteFactory.WoodFloor;
                baseLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(atlas, 0));
            }
        }

        AddChild(new CottageDecor());

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideCottage
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(new GridPosition(20, 17)),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerState(position.X, position.Y, true);
        AddChild(_player);

        _cursor = new TargetCursor(() => _player.TargetCell) { ZIndex = 20 };
        AddChild(_cursor);
    }

    public bool ControlsEnabled
    {
        get => _player.ControlsEnabled;
        set
        {
            _player.ControlsEnabled = value;
            _cursor.Visible = value;
        }
    }

    public event Action? SleepRequested;
    public event Action? ExitRequested;
    public event Action? StepRequested
    {
        add => _player.Stepped += value;
        remove => _player.Stepped -= value;
    }

    public Vector2 PlayerPosition => _player.Position;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        var target = _player.TargetCell;
        if (IsBedArea(target))
        {
            SleepRequested?.Invoke();
        }
        else if (target == DoorCell || IsAdjacent(_player.CurrentCell, DoorCell))
        {
            ExitRequested?.Invoke();
        }

        GetViewport().SetInputAsHandled();
    }

    private static bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (cell.X is < 10 or > 29 || cell.Y is < 6 or > 18)
        {
            return false;
        }

        var bedArea = IsBedArea(cell);
        var table = cell.X is >= 24 and <= 26 && cell.Y is >= 9 and <= 11;
        var bookshelf = cell.X is >= 10 and <= 12 && cell.Y is >= 6 and <= 8;
        var fireplace = cell.X is >= 27 and <= 29 && cell.Y is >= 6 and <= 8;
        return !bedArea && !table && !bookshelf && !fireplace;
    }

    private static bool IsBedArea(GridPosition cell) =>
        cell.X is >= 13 and <= 16 && cell.Y is >= 8 and <= 11;

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class CottageDecor : Node2D
{
    public CottageDecor()
    {
        ZIndex = 2;
    }

    public override void _Draw()
    {
        // Bed.
        DrawRect(new Rect2(13 * 16, 8 * 16, 4 * 16, 4 * 16), new Color("#302d55"));
        DrawRect(new Rect2(13 * 16 + 4, 8 * 16 + 4, 56, 19), new Color("#7f75c8"));
        DrawRect(new Rect2(13 * 16 + 4, 8 * 16 + 4, 56, 7), new Color("#f7f0d9"));
        DrawCircle(new Vector2(15 * 16 + 8, 10 * 16 + 8), 3, new Color("#8ee6be"));

        // Table and glow lamp.
        DrawRect(new Rect2(24 * 16, 9 * 16, 3 * 16, 3 * 16), new Color("#6b4f5f"));
        DrawCircle(new Vector2(25 * 16 + 8, 9 * 16 + 7), 8, new Color(0.95f, 0.79f, 0.47f, 0.25f));
        DrawRect(new Rect2(25 * 16 + 5, 9 * 16 + 3, 6, 9), new Color("#f3ca78"));

        // Woven star rug in the center of the room.
        DrawRect(new Rect2(18 * 16, 12 * 16, 6 * 16, 4 * 16), new Color("#26284b"));
        DrawRect(new Rect2(18 * 16 + 4, 12 * 16 + 4, 88, 56), new Color("#6b567d"));
        DrawRect(new Rect2(18 * 16 + 8, 12 * 16 + 8, 80, 48), new Color("#3d6170"));
        DrawColoredPolygon(
            [
                new Vector2(21 * 16, 12 * 16 + 11),
                new Vector2(21 * 16 + 7, 14 * 16),
                new Vector2(21 * 16, 15 * 16 + 5),
                new Vector2(21 * 16 - 7, 14 * 16),
            ],
            new Color("#f3ca78")
        );
        DrawCircle(new Vector2(21 * 16, 14 * 16), 3, new Color("#8ee6be"));

        // Bookshelf and glowing specimen bottles.
        DrawRect(new Rect2(10 * 16 + 3, 6 * 16, 44, 47), new Color("#513b4b"));
        DrawRect(new Rect2(10 * 16 + 6, 6 * 16 + 4, 38, 39), new Color("#8a5c61"));
        for (var y = 6 * 16 + 15; y <= 6 * 16 + 39; y += 12)
        {
            DrawRect(new Rect2(10 * 16 + 5, y, 40, 3), new Color("#4b3546"));
        }
        foreach (var book in new[]
                 {
                     new Rect2(10 * 16 + 9, 6 * 16 + 7, 4, 8),
                     new Rect2(10 * 16 + 15, 6 * 16 + 6, 5, 9),
                     new Rect2(10 * 16 + 23, 6 * 16 + 8, 4, 7),
                 })
        {
            DrawRect(book, book.Position.X % 2 == 0 ? new Color("#7f75c8") : new Color("#4bc5bd"));
        }
        DrawRect(new Rect2(10 * 16 + 31, 6 * 16 + 23, 6, 9), new Color("#31596c"));
        DrawRect(new Rect2(10 * 16 + 32, 6 * 16 + 25, 4, 6), new Color("#8ee6be"));
        DrawCircle(new Vector2(10 * 16 + 34, 6 * 16 + 27), 6, new Color(0.55f, 0.9f, 0.75f, 0.12f));

        // Hearth with a violet stone surround.
        DrawRect(new Rect2(27 * 16, 6 * 16, 3 * 16, 3 * 16), new Color("#363450"));
        DrawRect(new Rect2(27 * 16 + 5, 6 * 16 + 5, 38, 38), new Color("#6b5b72"));
        DrawRect(new Rect2(27 * 16 + 11, 6 * 16 + 14, 26, 29), new Color("#20233d"));
        DrawCircle(new Vector2(28 * 16 + 8, 7 * 16 + 10), 10, new Color(0.95f, 0.52f, 0.3f, 0.14f));
        DrawColoredPolygon(
            [
                new Vector2(28 * 16 + 3, 8 * 16),
                new Vector2(28 * 16 + 8, 7 * 16 + 7),
                new Vector2(28 * 16 + 13, 8 * 16),
            ],
            new Color("#f3ca78")
        );

        // Window with twilight glow.
        DrawRect(new Rect2(19 * 16, 5 * 16 + 4, 32, 20), new Color("#101a3a"));
        DrawRect(new Rect2(20 * 16 - 1, 5 * 16 + 8, 2, 14), new Color("#4bc5bd"));
        DrawCircle(new Vector2(19 * 16 + 8, 5 * 16 + 10), 2, new Color("#f7f0d9"));

        // Door marker.
        DrawRect(new Rect2(20 * 16 + 2, 18 * 16, 12, 16), new Color("#b27b5d"));
        DrawCircle(new Vector2(20 * 16 + 11, 18 * 16 + 8), 1.5f, new Color("#f3ca78"));

        // Potted moonfern near the exit.
        DrawRect(new Rect2(17 * 16 + 3, 17 * 16 + 5, 14, 10), new Color("#7b5360"));
        DrawLine(new Vector2(17 * 16 + 10, 17 * 16 + 5), new Vector2(17 * 16 + 10, 17 * 16 - 8), new Color("#4b9b74"), 2);
        DrawLine(new Vector2(17 * 16 + 10, 17 * 16 - 3), new Vector2(17 * 16 + 2, 17 * 16 - 8), new Color("#8ee6be"), 3);
        DrawLine(new Vector2(17 * 16 + 10, 17 * 16 - 5), new Vector2(17 * 16 + 18, 17 * 16 - 12), new Color("#b795dd"), 3);
    }
}
