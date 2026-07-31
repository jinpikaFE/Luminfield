using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class WorkshopView : Node2D
{
    public static readonly GridPosition WorkbenchCell =
        VillageCatalog.MoonRuneWorkbenchCell;
    public static readonly GridPosition DoorCell =
        VillageCatalog.MoonstoneWorkshopExitCell;

    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public WorkshopView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new WorkshopBackdrop());
        AddChild(new WorkshopNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideWorkshop
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(new GridPosition(20, 18)),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.MoonstoneWorkshop
            );
        AddChild(_player);
        AddChild(new WorkshopInteractionHints(
            () => _player.CurrentCell,
            () => ResolveNpcTarget(
                _player.TargetCell,
                _player.CurrentCell
            )
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
    public event Action? WorkbenchRequested;
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

        if (IsWorkbenchArea(target) ||
            IsAdjacent(player, WorkbenchCell))
        {
            return _session.PreviewSelectedTarget(WorkbenchCell);
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
        else if (IsWorkbenchArea(target) ||
            IsAdjacent(player, WorkbenchCell))
        {
            WorkbenchRequested?.Invoke();
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
        if (cell.X is < 2 or > 37 || cell.Y is < 3 or > 20)
        {
            return false;
        }

        if (IsWorkbenchArea(cell) ||
            IsForgeArea(cell) ||
            IsWallFurniture(cell))
        {
            return false;
        }

        return _session.Village.NpcAt(
            cell,
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.MoonstoneWorkshop
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
            PlayerLocationIds.MoonstoneWorkshop
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

    private static bool IsWorkbenchArea(GridPosition cell) =>
        cell.X is >= 15 and <= 24 &&
        cell.Y is >= 4 and <= 9;

    private static bool IsForgeArea(GridPosition cell) =>
        cell.X is >= 3 and <= 10 &&
        cell.Y is >= 3 and <= 9;

    private static bool IsWallFurniture(GridPosition cell) =>
        (cell.X is >= 27 and <= 36 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 2 and <= 7 && cell.Y is >= 10 and <= 18) ||
        (cell.X is >= 32 and <= 37 && cell.Y is >= 10 and <= 18) ||
        (cell.X is >= 2 and <= 12 && cell.Y is >= 17 and <= 20) ||
        (cell.X is >= 27 and <= 37 && cell.Y is >= 17 and <= 20);

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) +
        Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class WorkshopNpcLayer : Node2D
{
    private readonly GameSession _session;

    public WorkshopNpcLayer(GameSession session)
    {
        _session = session;
        ZIndex = 8;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Clock.TimeChanged += Refresh;
    }

    public override void _Draw()
    {
        foreach (var npc in _session.Village.CurrentNpcs(
                     _session.Clock.Day,
                     _session.Clock.MinuteOfDay,
                     PlayerLocationIds.MoonstoneWorkshop
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
                GeneratedArt.VillageNpcTexture,
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
    }

    private void Refresh()
    {
        QueueRedraw();
    }
}

internal sealed partial class WorkshopInteractionHints : Node2D
{
    private readonly Func<GridPosition> _playerCell;
    private readonly Func<VillageNpcState?> _npcTarget;
    private double _time;

    public WorkshopInteractionHints(
        Func<GridPosition> playerCell,
        Func<VillageNpcState?> npcTarget
    )
    {
        _playerCell = playerCell;
        _npcTarget = npcTarget;
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
            WorkshopView.WorkbenchCell,
            Distance(player, WorkshopView.WorkbenchCell) <= 4,
            ThemeFactory.Mint
        );
        DrawHint(
            WorkshopView.DoorCell,
            Distance(player, WorkshopView.DoorCell) <= 3,
            ThemeFactory.Gold
        );

        var npc = _npcTarget();
        if (npc is not null)
        {
            DrawHint(npc.Position, true, ThemeFactory.Mint);
        }
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
