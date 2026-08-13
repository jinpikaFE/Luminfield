using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CottageView : Node2D
{
    public static readonly GridPosition BedCell = CottageLayout.BedCell;
    public static readonly GridPosition DoorCell = CottageLayout.DoorCell;
    public static readonly GridPosition KitchenReserveCell =
        CottageLayout.KitchenReserveCell;

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public CottageView(GameSession session, LocaleService locale)
    {
        _session = session;
        AddChild(new CottageBackdrop(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideCottage
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(CottageLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerState(position.X, position.Y, true);
        AddChild(_player);
        AddChild(new CottageInteractionHints(
            () => _player.CurrentCell,
            () => _session.Construction.IsCompleted
        ));

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
    public event Action? KitchenReserveRequested;
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
            return _session.PreviewSelectedTarget(target);
        }

        if (target == DoorCell || IsAdjacent(_player.CurrentCell, DoorCell))
        {
            return _session.PreviewSelectedTarget(DoorCell);
        }

        if (_session.Construction.IsCompleted &&
            (target == KitchenReserveCell ||
             Distance(_player.CurrentCell, KitchenReserveCell) <= 3))
        {
            return _session.PreviewSelectedTarget(KitchenReserveCell);
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
            if (_session.RestInCottage().Succeeded)
            {
                SleepRequested?.Invoke();
            }
        }
        else if (target == DoorCell || IsAdjacent(_player.CurrentCell, DoorCell))
        {
            if (_session.ExitCottage().Succeeded)
            {
                ExitRequested?.Invoke();
            }
        }
        else if (_session.Construction.IsCompleted &&
            (target == KitchenReserveCell ||
             Distance(_player.CurrentCell, KitchenReserveCell) <= 3))
        {
            KitchenReserveRequested?.Invoke();
        }

        GetViewport().SetInputAsHandled();
    }

    private static bool CanOccupy(Vector2 worldPosition)
    {
        return CottageLayout.IsWalkable(new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        ));
    }

    private static bool IsBedArea(GridPosition cell) =>
        CottageLayout.IsBedArea(cell);

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
}

internal sealed partial class CottageInteractionHints : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private readonly Func<bool> _hasKitchenReserve;
    private double _time;

    public CottageInteractionHints(
        Func<GridPosition> playerCell,
        Func<bool> hasKitchenReserve
    )
    {
        _playerCell = playerCell;
        _hasKitchenReserve = hasKitchenReserve;
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
        if (_hasKitchenReserve())
        {
            DrawHint(
                new Vector2(
                    CottageView.KitchenReserveCell.X * 16 + 8,
                    CottageView.KitchenReserveCell.Y * 16 + 8
                ),
                Math.Abs(player.X - CottageView.KitchenReserveCell.X) +
                    Math.Abs(player.Y - CottageView.KitchenReserveCell.Y) <= 3,
                ThemeFactory.Mint
            );
        }
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
