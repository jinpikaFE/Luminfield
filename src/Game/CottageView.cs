using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CottageView : Node2D
{
    public static readonly GridPosition BedCell = new(12, 9);
    public static readonly GridPosition DoorCell = new(20, 18);

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public CottageView(GameSession session, LocaleService locale)
    {
        _session = session;
        AddChild(new CottageBackdrop());

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
        AddChild(new CottageInteractionHints(() => _player.CurrentCell));

        _cursor = new TargetCursor(ResolveTargetPreview, locale) { ZIndex = 20 };
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

    private TargetPreview ResolveTargetPreview()
    {
        var target = _player.TargetCell;
        if (IsBedArea(target))
        {
            return TargetPreview.Available(
                target,
                TargetPreviewKind.Bed,
                "target.action.rest"
            );
        }

        if (target == DoorCell || IsAdjacent(_player.CurrentCell, DoorCell))
        {
            return TargetPreview.Available(
                DoorCell,
                TargetPreviewKind.Door,
                "target.action.exit"
            );
        }

        return TargetPreview.Neutral(target);
    }

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
        if (cell.X is < 3 or > 36 || cell.Y is < 3 or > 18)
        {
            return false;
        }

        var bedArea = IsBedArea(cell);
        var table = cell.X is >= 29 and <= 35 && cell.Y is >= 10 and <= 17;
        var bookshelf = cell.X is >= 3 and <= 8 && cell.Y is >= 3 and <= 17;
        var fireplace = cell.X is >= 28 and <= 35 && cell.Y is >= 3 and <= 8;
        return !bedArea && !table && !bookshelf && !fireplace;
    }

    private static bool IsBedArea(GridPosition cell) =>
        cell.X is >= 9 and <= 14 && cell.Y is >= 3 and <= 9;

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class CottageInteractionHints : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private double _time;

    public CottageInteractionHints(Func<GridPosition> playerCell)
    {
        _playerCell = playerCell;
        ZIndex = 18;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var player = _playerCell();
        DrawHint(
            new Vector2(CottageView.BedCell.X * 16 + 8, CottageView.BedCell.Y * 16 + 8),
            Math.Abs(player.X - CottageView.BedCell.X) + Math.Abs(player.Y - CottageView.BedCell.Y) <= 4,
            ThemeFactory.Mint
        );
        DrawHint(
            new Vector2(CottageView.DoorCell.X * 16 + 8, CottageView.DoorCell.Y * 16 + 8),
            Math.Abs(player.X - CottageView.DoorCell.X) + Math.Abs(player.Y - CottageView.DoorCell.Y) <= 3,
            ThemeFactory.Gold
        );
    }

    private void DrawHint(Vector2 center, bool nearby, Color color)
    {
        var pulse = 0.68f + Mathf.Sin((float)_time * 3.8f + center.X) * 0.2f;
        var alpha = nearby ? pulse : pulse * 0.42f;
        DrawArc(center, nearby ? 11 : 8, 0, Mathf.Tau, 20, new Color(color, alpha), nearby ? 2 : 1);
        DrawCircle(center, nearby ? 3 : 2, new Color(color, alpha * 0.2f));

        if (!nearby)
        {
            return;
        }

        var sparkle = center + new Vector2(0, -15 + Mathf.Sin((float)_time * 4) * 2);
        DrawLine(sparkle + new Vector2(-3, 0), sparkle + new Vector2(3, 0), new Color(color, alpha), 1);
        DrawLine(sparkle + new Vector2(0, -3), sparkle + new Vector2(0, 3), new Color(color, alpha), 1);
    }
}
