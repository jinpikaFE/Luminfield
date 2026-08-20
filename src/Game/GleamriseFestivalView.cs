using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GleamriseFestivalView : Node2D
{
    public static readonly GridPosition ExitCell = FestivalSystem.ExitCell;
    public static readonly GridPosition ActivityCell =
        FestivalSystem.ActivityCell;
    public static readonly GridPosition ExchangeStallCell =
        FestivalSystem.ExchangeStallCell;

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public GleamriseFestivalView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new GleamriseFestivalBackdrop());

        var exchangeStall = GeneratedArt.CreateGleamriseFestivalStallSprite();
        exchangeStall.Name = "GleamriseFestivalSeedExchange";
        exchangeStall.Position =
            CellCenter(ExchangeStallCell) + new Vector2(0, 8);
        exchangeStall.ZIndex = 7;
        exchangeStall.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 1),
            ZIndex = -1
        });
        AddChild(exchangeStall);

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideGleamriseFestival
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(new GridPosition(20, 19)),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.GleamriseFestival
            );
        AddChild(_player);
        AddChild(new GleamriseFestivalInteractionHints(
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

    public event Action? ExitRequested;
    public event Action? ActivityRequested;
    public event Action? ExchangeRequested;
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
        if (IsExitTarget(target, player))
        {
            return _session.PreviewSelectedTarget(ExitCell);
        }

        if (IsActivityTarget(target, player))
        {
            return _session.PreviewSelectedTarget(ActivityCell);
        }

        if (IsExchangeTarget(target, player))
        {
            return _session.PreviewSelectedTarget(ExchangeStallCell);
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
        var player = _player.CurrentCell;
        if (IsExitTarget(target, player))
        {
            RequestHand(ExitRequested);
        }
        else if (IsActivityTarget(target, player))
        {
            RequestHand(ActivityRequested);
        }
        else if (IsExchangeTarget(target, player))
        {
            RequestHand(ExchangeRequested);
        }

        GetViewport().SetInputAsHandled();
    }

    private void RequestHand(Action? action)
    {
        if (_session.Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            NoticeRequested?.Invoke("notice.needs_hand");
            return;
        }

        action?.Invoke();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (cell.X is < 7 or > 33 || cell.Y is < 6 or > 21)
        {
            return false;
        }

        return !IsActivityFootprint(cell) &&
            !IsExchangeFootprint(cell) &&
            cell != ExitCell;
    }

    private static bool IsExitTarget(
        GridPosition target,
        GridPosition player
    ) => target == ExitCell || IsAdjacent(player, ExitCell);

    private static bool IsActivityTarget(
        GridPosition target,
        GridPosition player
    ) => IsActivityFootprint(target) || DistanceToActivity(player) <= 1;

    private static bool IsExchangeTarget(
        GridPosition target,
        GridPosition player
    ) => IsExchangeFootprint(target) || DistanceToExchange(player) <= 1;

    private static bool IsActivityFootprint(GridPosition cell) =>
        cell.X is >= 11 and <= 26 &&
        cell.Y is >= 5 and <= 15;

    private static bool IsExchangeFootprint(GridPosition cell) =>
        cell.X is >= 27 and <= 31 &&
        cell.Y is >= 10 and <= 14;

    private static int DistanceToActivity(GridPosition cell) =>
        DistanceToRect(cell, 11, 5, 26, 15);

    private static int DistanceToExchange(GridPosition cell) =>
        DistanceToRect(cell, 27, 10, 31, 14);

    private static int DistanceToRect(
        GridPosition cell,
        int left,
        int top,
        int right,
        int bottom
    )
    {
        var dx = 0;
        if (cell.X < left)
        {
            dx = left - cell.X;
        }
        else if (cell.X > right)
        {
            dx = cell.X - right;
        }

        var dy = 0;
        if (cell.Y < top)
        {
            dy = top - cell.Y;
        }
        else if (cell.Y > bottom)
        {
            dy = cell.Y - bottom;
        }

        return dx + dy;
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) +
        Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class GleamriseFestivalBackdrop : Node2D
{
    private static readonly Texture2D Background =
        GD.Load<Texture2D>(
            "res://assets/generated/gleamrise_festival_plaza.png"
        );

    public GleamriseFestivalBackdrop()
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

internal sealed partial class GleamriseFestivalInteractionHints : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private double _time;

    public GleamriseFestivalInteractionHints(
        Func<GridPosition> playerCell
    )
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
            new Vector2(320, 318),
            IsNear(player, FestivalSystem.ExitCell),
            ThemeFactory.Gold
        );
        DrawHint(
            new Vector2(320, 190),
            DistanceToRect(player, 11, 5, 26, 15) <= 1,
            ThemeFactory.Mint
        );
        DrawHint(
            new Vector2(472, 214),
            DistanceToRect(player, 27, 10, 31, 14) <= 1,
            ThemeFactory.Gold
        );
    }

    private void DrawHint(Vector2 center, bool nearby, Color accent)
    {
        var pulse = 0.62f + Mathf.Sin((float)_time * 4.4f) * 0.18f;
        var color = new Color(accent, nearby ? pulse + 0.14f : pulse * 0.34f);
        DrawArc(center, 12 + pulse * 2, 0, Mathf.Tau, 24, color, nearby ? 2 : 1);
        if (!nearby)
        {
            return;
        }

        DrawRect(new Rect2(center.X - 8, center.Y - 30, 16, 11), new Color("#07132bee"), true);
        DrawRect(new Rect2(center.X - 8, center.Y - 30, 16, 11), color, false, 1);
        DrawString(
            GD.Load<Font>("res://assets/fonts/NotoSansCJKsc-Regular.otf"),
            new Vector2(center.X - 3.5f, center.Y - 21),
            "E",
            HorizontalAlignment.Left,
            -1,
            8,
            ThemeFactory.Ink
        );
    }

    private static bool IsNear(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;

    private static int DistanceToRect(
        GridPosition cell,
        int left,
        int top,
        int right,
        int bottom
    )
    {
        var dx = 0;
        if (cell.X < left)
        {
            dx = left - cell.X;
        }
        else if (cell.X > right)
        {
            dx = cell.X - right;
        }

        var dy = 0;
        if (cell.Y < top)
        {
            dy = top - cell.Y;
        }
        else if (cell.Y > bottom)
        {
            dy = cell.Y - bottom;
        }

        return dx + dy;
    }
}
