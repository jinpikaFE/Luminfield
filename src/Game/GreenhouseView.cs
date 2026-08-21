using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GreenhouseView : Node2D
{
    public static readonly GridPosition ExitCell = GreenhouseLayout.ExitCell;
    public static readonly GridPosition CisternCell =
        GreenhouseLayout.CisternCell;

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public GreenhouseView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new GreenhouseBackdrop());
        AddChild(new FarmSoilStateLayer(session.GreenhouseFarm));
        AddChild(new GeneratedCropLayer(session.GreenhouseFarm));
        AddChild(new CropGlowLayer(session.GreenhouseFarm));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideGreenhouse
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(GreenhouseLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.Greenhouse
            );
        AddChild(_player);
        AddChild(new GreenhouseInteractionHints(
            () => _player.CurrentCell
        ));

        _cursor = new TargetCursor(ResolveTargetPreview, locale)
        {
            ZIndex = 20
        };
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

    public event Action<GridPosition>? UseRequested;
    public event Action? ExitRequested;
    public event Action<string>? NoticeRequested;
    public event Action? StepRequested
    {
        add => _player.Stepped += value;
        remove => _player.Stepped -= value;
    }

    public Vector2 PlayerPosition => _player.Position;

    private TargetPreview ResolveTargetPreview()
    {
        var target = _player.TargetCell;
        var player = _player.CurrentCell;
        if (target == ExitCell || IsAdjacent(player, ExitCell))
        {
            return _session.PreviewSelectedTarget(ExitCell);
        }

        if (target == CisternCell || IsAdjacent(player, CisternCell))
        {
            return _session.PreviewSelectedTarget(CisternCell);
        }

        return _session.PreviewSelectedTarget(target);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        var target = _player.TargetCell;
        var player = _player.CurrentCell;
        if (target == ExitCell || IsAdjacent(player, ExitCell))
        {
            var result = _session.TryExitGreenhouse(ExitCell);
            if (result.Succeeded)
            {
                ExitRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (target == CisternCell || IsAdjacent(player, CisternCell))
        {
            UseRequested?.Invoke(CisternCell);
        }
        else
        {
            UseRequested?.Invoke(target);
        }

        GetViewport().SetInputAsHandled();
    }

    private static bool CanOccupy(Vector2 worldPosition) =>
        GreenhouseLayout.IsWalkable(new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        ));

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class GreenhouseBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/features/construction/homestead_greenhouse_interior.png"
    );

    public GreenhouseBackdrop()
    {
        ZIndex = -100;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            Background,
            new Rect2(0, 0, 640, 360),
            new Rect2(0, 80, 1536, 864)
        );
    }
}

internal sealed partial class GreenhouseInteractionHints : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private double _time;

    public GreenhouseInteractionHints(Func<GridPosition> playerCell)
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
        DrawHint(
            GreenhouseView.ExitCell,
            Distance(_playerCell(), GreenhouseView.ExitCell) <= 3,
            ThemeFactory.Gold
        );
        DrawHint(
            GreenhouseView.CisternCell,
            Distance(_playerCell(), GreenhouseView.CisternCell) <= 4,
            ThemeFactory.Mint
        );
    }

    private void DrawHint(
        GridPosition cell,
        bool nearby,
        Color color
    )
    {
        var center = new Vector2(cell.X * 16 + 8, cell.Y * 16 + 8);
        var pulse = 0.68f +
            Mathf.Sin((float)_time * 3.8f + center.X) * 0.2f;
        var alpha = nearby ? pulse : pulse * 0.35f;
        DrawArc(
            center,
            nearby ? 11 : 7,
            0,
            Mathf.Tau,
            20,
            new Color(color, alpha),
            nearby ? 2 : 1
        );
        DrawCircle(
            center,
            nearby ? 3 : 2,
            new Color(color, alpha * 0.2f)
        );
    }

    private static int Distance(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) +
        Math.Abs(first.Y - second.Y);
}
