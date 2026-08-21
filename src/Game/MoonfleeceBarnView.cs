using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class MoonfleeceBarnView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public MoonfleeceBarnView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new MoonfleeceBarnBackdrop());
        AddChild(new DewhornMilkingStationVisual(session));
        AddChild(new MoonfleeceSheepVisual(session, worldProjection: false));
        AddChild(new DewhornVisual(session, worldProjection: false));
        AddChild(new LivestockAutomationConsoleVisual(
            session,
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        ));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideMoonfleeceBarn
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(MoonfleeceBarnLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.MoonfleeceBarn
            );
        AddChild(_player);

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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        var actual = ResolveActualTarget(
            _player.TargetCell,
            _player.CurrentCell
        );
        if (actual == MoonfleeceBarnLayout.ExitCell)
        {
            var result = _session.TryExitAnimalBuilding(
                AnimalCatalog.MoonfleeceBarnId,
                actual
            );
            if (result.Succeeded)
            {
                ExitRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else
        {
            UseRequested?.Invoke(actual);
        }

        GetViewport().SetInputAsHandled();
    }

    private TargetPreview ResolveTargetPreview() =>
        _session.PreviewSelectedTarget(ResolveActualTarget(
            _player.TargetCell,
            _player.CurrentCell
        ));

    private GridPosition ResolveActualTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var stableTargets = new List<GridPosition>
        {
            MoonfleeceBarnLayout.ExitCell,
            MoonfleeceBarnLayout.FeedTroughCell,
            MoonfleeceBarnLayout.CollectionRackCell,
            MoonfleeceBarnLayout.MilkingStationCell,
            MoonfleeceBarnLayout.AutomationStationCell
        };
        stableTargets.AddRange(_session.VisibleAnimalProjections
            .Where(projection =>
                projection.BuildingId == AnimalCatalog.MoonfleeceBarnId)
            .Select(projection => projection.Cell));

        var exact = stableTargets.FirstOrDefault(cell => cell == target);
        if (exact != default)
        {
            return exact;
        }

        return stableTargets
            .Where(cell => IsAdjacent(player, cell))
            .OrderBy(cell =>
                Math.Abs(cell.X - target.X) + Math.Abs(cell.Y - target.Y)
            )
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .FirstOrDefault(target);
    }

    private static bool CanOccupy(Vector2 worldPosition) =>
        MoonfleeceBarnLayout.IsWalkable(new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        ));

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class MoonfleeceBarnBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/moonfleece_barn_interior.png"
    );

    public MoonfleeceBarnBackdrop()
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
