using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CrystalGrottoView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public CrystalGrottoView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new CrystalGrottoBackdrop());
        AddChild(new CrystalGrottoEntityLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideCrystalGrottoSurvey
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(CrystalGrottoSurveyLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.CrystalGrottoSurvey
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
    public event Action<GridPosition>? UpgradeRequested;
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

        var player = _player.CurrentCell;
        var target = ResolveActualTarget(_player.TargetCell, player);
        if (target is { } actualExit &&
            actualExit == CrystalGrottoSurveyLayout.ExitCell)
        {
            var result = _session.TryExitCrystalGrottoSurvey(actualExit);
            if (result.Succeeded)
            {
                ExitRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (target is { } actualBench &&
            actualBench == CrystalGrottoSurveyLayout.UpgradeBenchCell)
        {
            var result = _session.OpenCrystalGrottoUpgradeBench(actualBench);
            if (result.Succeeded)
            {
                UpgradeRequested?.Invoke(actualBench);
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (target is { } actual)
        {
            UseRequested?.Invoke(actual);
        }

        GetViewport().SetInputAsHandled();
    }

    private TargetPreview ResolveTargetPreview()
    {
        var target = _player.TargetCell;
        return ResolveActualTarget(target, _player.CurrentCell) is { } actual
            ? _session.PreviewSelectedTarget(actual)
            : TargetPreview.Neutral(target);
    }

    private GridPosition? ResolveActualTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var fixedTargets = new[]
        {
            CrystalGrottoSurveyLayout.ExitCell,
            CrystalGrottoSurveyLayout.UpgradeBenchCell,
            CrystalGrottoSurveyLayout.DepthAnchorCell,
            CrystalGrottoSurveyLayout.SealCell
        };
        var exactFixed = fixedTargets.FirstOrDefault(cell => cell == target);
        if (fixedTargets.Contains(target))
        {
            if (target != CrystalGrottoSurveyLayout.SealCell || SealActive)
            {
                return exactFixed;
            }
        }

        var exactVein = MiningCatalog.TryVeinAt(target, out var vein) &&
            !_session.Mining.IsDepleted(vein.Id)
                ? vein
                : null;
        if (exactVein is not null)
        {
            return exactVein.Cell;
        }

        var nearbyFixed = fixedTargets
            .Where(cell => IsAdjacent(player, cell))
            .Where(cell => cell != CrystalGrottoSurveyLayout.SealCell ||
                SealActive)
            .OrderBy(cell => Distance(cell, target))
            .ThenBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .Cast<GridPosition?>()
            .FirstOrDefault();
        if (nearbyFixed is not null)
        {
            return nearbyFixed;
        }

        return MiningCatalog.Veins
            .Where(candidate => !_session.Mining.IsDepleted(candidate.Id))
            .Where(candidate => IsAdjacent(player, candidate.Cell))
            .OrderBy(candidate => Distance(candidate.Cell, target))
            .ThenBy(candidate => candidate.Cell.Y)
            .ThenBy(candidate => candidate.Cell.X)
            .Select(candidate => (GridPosition?)candidate.Cell)
            .FirstOrDefault();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (!CrystalGrottoSurveyLayout.IsWalkable(cell) ||
            cell == CrystalGrottoSurveyLayout.ExitCell ||
            cell == CrystalGrottoSurveyLayout.UpgradeBenchCell ||
            cell == CrystalGrottoSurveyLayout.DepthAnchorCell ||
            SealActive && cell == CrystalGrottoSurveyLayout.SealCell)
        {
            return false;
        }

        return !MiningCatalog.TryVeinAt(cell, out var vein) ||
            _session.Mining.IsDepleted(vein.Id);
    }

    private bool SealActive => !_session.ToolProgression.IsUpgradeCompleted(
        ToolProgressionCatalog.ShovelBronzeStarUpgradeId
    );

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Distance(first, second) == 1;

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class CrystalGrottoBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/crystal_grotto_survey_interior.png"
    );

    public CrystalGrottoBackdrop()
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

internal sealed partial class CrystalGrottoEntityLayer : Node2D
{
    private readonly GameSession _session;

    public CrystalGrottoEntityLayer(GameSession session)
    {
        _session = session;
        ZIndex = 7;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Mining.Changed += OnMiningChanged;
        session.ToolProgression.Changed += OnToolProgressionChanged;
        session.Changed += QueueRedraw;
    }

    public override void _Draw()
    {
        foreach (var vein in MiningCatalog.Veins)
        {
            if (_session.Mining.IsDepleted(vein.Id))
            {
                continue;
            }

            DrawAt(
                vein.Cell,
                CrystalGrottoArt.MineralVeinRegion(vein.MineralItemId),
                new Vector2(52, 52)
            );
        }

        if (!_session.ToolProgression.IsUpgradeCompleted(
                ToolProgressionCatalog.ShovelBronzeStarUpgradeId
            ))
        {
            DrawAt(
                CrystalGrottoSurveyLayout.SealCell,
                CrystalGrottoArt.SealRegion,
                new Vector2(58, 58)
            );
        }

        DrawAt(
            CrystalGrottoSurveyLayout.DepthAnchorCell,
            CrystalGrottoArt.DepthAnchorRegion,
            new Vector2(52, 52),
            _session.Mining.FifthRoomAnchorReached
                ? Colors.White
                : new Color(0.68f, 0.72f, 0.86f, 0.82f)
        );
    }

    public override void _ExitTree()
    {
        _session.Mining.Changed -= OnMiningChanged;
        _session.ToolProgression.Changed -= OnToolProgressionChanged;
        _session.Changed -= QueueRedraw;
    }

    private void OnMiningChanged(GridPosition _) => QueueRedraw();
    private void OnToolProgressionChanged() => QueueRedraw();

    private void DrawAt(
        GridPosition cell,
        Rect2 source,
        Vector2 size,
        Color? modulate = null
    )
    {
        var anchor = new Vector2(cell.X * 16 + 8, cell.Y * 16 + 15);
        var destination = new Rect2(
            anchor - new Vector2(size.X / 2, size.Y),
            size
        );
        DrawTextureRectRegion(
            CrystalGrottoArt.Atlas,
            destination,
            source,
            modulate ?? Colors.White
        );
    }
}
