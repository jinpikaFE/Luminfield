using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarlightPostView : Node2D
{
    public static readonly GridPosition SortingCounterCell =
        VillageCatalog.RouteSortingCounterCell;
    public static readonly GridPosition DoorCell =
        VillageCatalog.StarlightPostExitCell;

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public StarlightPostView(
        GameSession session,
        LocaleService locale
    )
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new StarlightPostBackdrop());
        AddChild(new StarlightPostNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideStarlightPost
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(new GridPosition(20, 18)),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.StarlightPost
            );
        AddChild(_player);
        AddChild(new StarlightPostInteractionHints(
            () => _player.CurrentCell,
            () => ResolveNpcTarget(
                _player.TargetCell,
                _player.CurrentCell
            ),
            () => _session.PreviewSelectedTarget(SortingCounterCell)
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
    public event Action? SortingCounterRequested;
    public event Action<GridPosition>? VillagerRequested;
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
        var villager = ResolveNpcTarget(target, player);
        if (villager is not null)
        {
            return _session.PreviewSelectedTarget(villager.Position);
        }

        if (IsSortingCounterArea(target) ||
            IsAdjacent(player, SortingCounterCell))
        {
            return _session.PreviewSelectedTarget(SortingCounterCell);
        }

        if (target == DoorCell || IsAdjacent(player, DoorCell))
        {
            return _session.PreviewSelectedTarget(DoorCell);
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
        var villager = ResolveNpcTarget(target, player);
        if (villager is not null)
        {
            VillagerRequested?.Invoke(villager.Position);
        }
        else if (IsSortingCounterArea(target) ||
            IsAdjacent(player, SortingCounterCell))
        {
            SortingCounterRequested?.Invoke();
        }
        else if (target == DoorCell || IsAdjacent(player, DoorCell))
        {
            ExitRequested?.Invoke();
        }

        GetViewport().SetInputAsHandled();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (!NpcNavigationMap.IsWalkableGeometry(
                PlayerLocationIds.StarlightPost,
                cell
            ))
        {
            return false;
        }

        return _session.Village.NpcAt(
            cell,
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.StarlightPost,
            _player.CurrentCell
        ) is null;
    }

    private VillageNpcState? ResolveNpcTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.StarlightPost,
            player
        );
        var exact = current.FirstOrDefault(npc => npc.Position == target);
        if (exact is not null)
        {
            return exact;
        }

        return current
            .Where(npc => IsAdjacent(player, npc.Position))
            .OrderBy(npc =>
                Math.Abs(npc.Position.X - target.X) +
                Math.Abs(npc.Position.Y - target.Y)
            )
            .ThenBy(npc => npc.Position.Y)
            .ThenBy(npc => npc.Position.X)
            .FirstOrDefault();
    }

    private static bool IsSortingCounterArea(GridPosition cell) =>
        cell.X is >= 11 and <= 28 &&
        cell.Y is >= 7 and <= 10;

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) +
        Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class StarlightPostNpcLayer : Node2D
{
    private readonly GameSession _session;

    public StarlightPostNpcLayer(GameSession session)
    {
        _session = session;
        ZIndex = 8;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Clock.TimeChanged += Refresh;
        session.Weather.Changed += Refresh;
    }

    public override void _Draw()
    {
        foreach (var npc in _session.Village.CurrentNpcs(
                     _session.Clock.Day,
                     _session.Clock.MinuteOfDay,
                     PlayerLocationIds.StarlightPost,
                     _session.PlayerCell
                 ))
        {
            var source = GeneratedArt.VillageNpcRegion(
                npc.Definition.AtlasRow,
                npc.Facing
            );
            var height = npc.Definition.AtlasRow == 0 ? 54f : 52f;
            var width = height * source.Size.X / source.Size.Y;
            var anchor = new Vector2(
                npc.Position.X * 16 + 8,
                npc.Position.Y * 16 + 15
            );
            DrawCircle(
                anchor - new Vector2(0, 1),
                7,
                new Color(0.01f, 0.03f, 0.08f, 0.44f)
            );
            DrawTextureRectRegion(
                GeneratedArt.VillageNpcTexture(npc.Definition.AtlasRow),
                new Rect2(
                    anchor - new Vector2(width / 2, height),
                    new Vector2(width, height)
                ),
                source
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= Refresh;
        _session.Weather.Changed -= Refresh;
    }

    private void Refresh()
    {
        QueueRedraw();
    }
}

internal sealed partial class StarlightPostInteractionHints : Node2D
{
    private static readonly Rect2 SortingCounterBounds = new(
        11 * 16,
        7 * 16,
        18 * 16,
        4 * 16
    );

    private readonly Func<GridPosition> _playerCell;
    private readonly Func<VillageNpcState?> _npcTarget;
    private readonly Func<TargetPreview> _counterPreview;
    private double _time;

    public StarlightPostInteractionHints(
        Func<GridPosition> playerCell,
        Func<VillageNpcState?> npcTarget,
        Func<TargetPreview> counterPreview
    )
    {
        _playerCell = playerCell;
        _npcTarget = npcTarget;
        _counterPreview = counterPreview;
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
        var counterPreview = _counterPreview();
        DrawSortingCounterHint(
            Distance(player, StarlightPostView.SortingCounterCell) <= 4,
            PreviewColor(counterPreview.State)
        );
        DrawCellHint(
            StarlightPostView.DoorCell,
            Distance(player, StarlightPostView.DoorCell) <= 3,
            ThemeFactory.Gold
        );

        var npc = _npcTarget();
        if (npc is not null)
        {
            DrawCellHint(npc.Position, true, ThemeFactory.Mint);
        }
    }

    private void DrawSortingCounterHint(bool nearby, Color color)
    {
        var pulse = Pulse(SortingCounterBounds.GetCenter().X);
        var alpha = nearby ? pulse : pulse * 0.35f;
        DrawRect(
            SortingCounterBounds.Grow(2),
            new Color(color, alpha),
            false,
            nearby ? 2 : 1
        );
        DrawCircle(
            CellCenter(StarlightPostView.SortingCounterCell),
            nearby ? 4 : 2,
            new Color(color, alpha * 0.24f)
        );
    }

    private void DrawCellHint(
        GridPosition cell,
        bool nearby,
        Color color
    )
    {
        var center = CellCenter(cell);
        var pulse = Pulse(center.X);
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

    private float Pulse(float offset) =>
        0.68f + Mathf.Sin((float)_time * 3.8f + offset) * 0.2f;

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static int Distance(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) +
        Math.Abs(first.Y - second.Y);

    private static Color PreviewColor(TargetPreviewState state) => state switch
    {
        TargetPreviewState.Available => ThemeFactory.Mint,
        TargetPreviewState.NeedsTool => ThemeFactory.Gold,
        TargetPreviewState.Blocked => new Color("#e58a9f"),
        _ => new Color("#8294b8")
    };
}
