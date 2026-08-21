using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FireflyTideView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public FireflyTideView(GameSession session, LocaleService locale)
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new FireflyTideBackdrop());
        AddChild(new FireflyTideStationLayer(session));
        AddChild(new FireflyTideNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideFireflyTide
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(FireflyTideLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.FireflyTide
            );
        AddChild(_player);

        _cursor = new TargetCursor(ResolveTargetPreview, locale)
        {
            ZIndex = 20
        };
        AddChild(_cursor);
        session.Clock.TimeChanged += HandleTimeChanged;
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
    public event Action? ClosedRequested;
    public event Action<GridPosition>? ActivityRequested;
    public event Action? ShopRequested;
    public event Action<GridPosition>? VillagerRequested;
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

        var target = _player.TargetCell;
        var player = _player.CurrentCell;
        var villager = ResolveNpcTarget(target, player);
        if (villager is not null)
        {
            VillagerRequested?.Invoke(villager.Position);
        }
        else if (IsTargetOrAdjacent(
            target,
            player,
            FireflyTideLayout.ExitCell
        ))
        {
            var result = _session.TryExitFestival(
                FireflyTideLayout.ExitCell
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
        else if (ResolveStationTarget(target, player) is { } station)
        {
            var result = _session.CheckFestivalStation(
                station.Id,
                station.Cell
            );
            if (!result.Succeeded)
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
            else if (station.Id == FestivalCatalog.FireflyGlowshopId)
            {
                ShopRequested?.Invoke();
            }
            else
            {
                ActivityRequested?.Invoke(station.Cell);
            }
        }

        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= HandleTimeChanged;
    }

    private TargetPreview ResolveTargetPreview()
    {
        var target = _player.TargetCell;
        var player = _player.CurrentCell;
        var villager = ResolveNpcTarget(target, player);
        if (villager is not null)
        {
            return _session.PreviewSelectedTarget(villager.Position);
        }

        if (IsTargetOrAdjacent(
                target,
                player,
                FireflyTideLayout.ExitCell
            ))
        {
            return _session.PreviewSelectedTarget(
                FireflyTideLayout.ExitCell
            );
        }

        return ResolveStationTarget(target, player) is { } station
            ? _session.PreviewSelectedTarget(station.Cell)
            : TargetPreview.Neutral(target);
    }

    private FestivalStationDefinition? ResolveStationTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var exact = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(station => station.Cell == target);
        return exact ?? FestivalSpatialCatalog.FireflyTide.Stations
            .Where(station => IsAdjacent(player, station.Cell))
            .OrderBy(station => Distance(station.Cell, target))
            .ThenBy(station => station.Cell.Y)
            .ThenBy(station => station.Cell.X)
            .FirstOrDefault();
    }

    private VillageNpcState? ResolveNpcTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.FireflyTide,
            player
        );
        var exact = current.FirstOrDefault(npc => npc.Position == target);
        return exact ?? current
            .Where(npc => IsAdjacent(player, npc.Position))
            .OrderBy(npc => Distance(npc.Position, target))
            .ThenBy(npc => npc.Position.Y)
            .ThenBy(npc => npc.Position.X)
            .FirstOrDefault();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        return FireflyTideLayout.IsWalkable(cell) &&
            _session.Village.NpcAt(
                cell,
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.FireflyTide,
                _player.CurrentCell
            ) is null;
    }

    private void HandleTimeChanged()
    {
        if (_session.LeaveFestivalIfClosed())
        {
            ClosedRequested?.Invoke();
        }
    }

    private static bool IsTargetOrAdjacent(
        GridPosition target,
        GridPosition player,
        GridPosition actual
    ) => target == actual || IsAdjacent(player, actual);

    private static bool IsAdjacent(
        GridPosition first,
        GridPosition second
    ) => Distance(first, second) <= 1;

    private static int Distance(
        GridPosition first,
        GridPosition second
    ) => Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class FireflyTideBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/firefly-tide/firefly_tide_backdrop.png"
    );

    public FireflyTideBackdrop()
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

internal sealed partial class FireflyTideStationLayer : Node2D
{
    private readonly GameSession _session;

    public FireflyTideStationLayer(GameSession session)
    {
        _session = session;
        ZIndex = 7;
        TextureFilter = TextureFilterEnum.Nearest;
        session.Festival.Changed += Refresh;
    }

    public override void _Draw()
    {
        var result = _session.Festival.ResultFor(
            FestivalCatalog.FireflyTideFestivalId,
            CalendarSystem.YearNumber(_session.Clock.Day)
        );
        DrawStation(
            FireflyTideLayout.LanternLaunchCell,
            FireflyTideArt.LanternLaunchRegion,
            new Vector2(92, 72)
        );
        DrawStation(
            FireflyTideLayout.FishBasinCell,
            FireflyTideArt.FishBasinRegion,
            new Vector2(70, 65)
        );
        DrawStation(
            FireflyTideLayout.ShopCell,
            FireflyTideArt.GlowshopRegion,
            new Vector2(76, 78)
        );
        DrawStation(
            FireflyTideLayout.TideAltarCell,
            FireflyTideArt.TideAltarRegion,
            new Vector2(62, 76)
        );

        if (result is null)
        {
            return;
        }

        for (var index = 0;
             index < Math.Min(3, result.ItemIds.Count);
             index++)
        {
            DrawItemIcon(
                result.ItemIds[index],
                CellCenter(FireflyTideLayout.LanternLaunchCell) +
                    new Vector2((index - 1) * 23, -33),
                23
            );
        }
        DrawCircle(
            CellCenter(FireflyTideLayout.TideAltarCell) +
                new Vector2(0, -34),
            7,
            new Color(0.41f, 0.88f, 0.81f, 0.42f)
        );
    }

    public override void _ExitTree()
    {
        _session.Festival.Changed -= Refresh;
    }

    private void DrawStation(
        GridPosition cell,
        Rect2 source,
        Vector2 size
    )
    {
        var anchor = CellCenter(cell) + new Vector2(0, 8);
        DrawTextureRectRegion(
            FireflyTideArt.Atlas,
            new Rect2(anchor - new Vector2(size.X / 2, size.Y), size),
            source
        );
    }

    private void DrawItemIcon(string itemId, Vector2 center, float size)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            return;
        }

        DrawTextureRectRegion(
            texture,
            new Rect2(
                center - new Vector2(size / 2, size / 2),
                new Vector2(size, size)
            ),
            region
        );
    }

    private void Refresh() => QueueRedraw();

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 15);
}

internal sealed partial class FireflyTideNpcLayer : Node2D
{
    private readonly GameSession _session;

    public FireflyTideNpcLayer(GameSession session)
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
                     PlayerLocationIds.FireflyTide,
                     _session.PlayerCell
                 ))
        {
            var art = NpcArtCatalog.Resolve(npc.Definition.Id, npc.Facing);
            var height = art.TargetHeight;
            var width = height * art.Region.Size.X / art.Region.Size.Y;
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
                art.Texture,
                new Rect2(
                    anchor - new Vector2(width / 2, height),
                    new Vector2(width, height)
                ),
                art.Region
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= Refresh;
    }

    private void Refresh() => QueueRedraw();
}
