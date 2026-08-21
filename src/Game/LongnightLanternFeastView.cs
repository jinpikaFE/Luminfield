using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class LongnightLanternFeastView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public LongnightLanternFeastView(
        GameSession session,
        LocaleService locale
    )
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new LongnightLanternFeastBackdrop());
        AddChild(new LongnightLanternFeastStationLayer(session));
        AddChild(new LongnightLanternFeastNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideLongnightLanternFeast
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(LongnightLanternFeastLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.LongnightLanternFeast
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
    public event Action? StallRequested;
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
            LongnightLanternFeastLayout.ExitCell
        ))
        {
            var result = _session.TryExitFestival(
                LongnightLanternFeastLayout.ExitCell
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
            else if (station.Id ==
                FestivalCatalog.LongnightLanternStallId)
            {
                StallRequested?.Invoke();
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
                LongnightLanternFeastLayout.ExitCell
            ))
        {
            return _session.PreviewSelectedTarget(
                LongnightLanternFeastLayout.ExitCell
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
        var exact = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(station => station.Cell == target);
        return exact ?? FestivalSpatialCatalog.LongnightLanternFeast.Stations
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
            PlayerLocationIds.LongnightLanternFeast,
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
        return LongnightLanternFeastLayout.IsWalkable(cell) &&
            _session.Village.NpcAt(
                cell,
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.LongnightLanternFeast,
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

internal sealed partial class LongnightLanternFeastBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/longnight_lantern_feast_backdrop.png"
    );

    public LongnightLanternFeastBackdrop()
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

internal sealed partial class LongnightLanternFeastStationLayer : Node2D
{
    private static readonly Texture2D Props = GD.Load<Texture2D>(
        "res://assets/generated/longnight_lantern_feast_props.png"
    );
    private static readonly Rect2 TableRegion = new(43, 297, 540, 293);
    private static readonly Rect2 GiftRegion = new(739, 100, 402, 490);
    private static readonly Rect2 DormantRitualRegion =
        new(85, 844, 457, 373);
    private static readonly Rect2 LitRitualRegion =
        new(712, 844, 457, 373);

    private readonly GameSession _session;

    public LongnightLanternFeastStationLayer(GameSession session)
    {
        _session = session;
        ZIndex = 7;
        TextureFilter = TextureFilterEnum.Nearest;
        var stall = GeneratedArt.CreateMarketStallSprite();
        stall.Name = FestivalCatalog.LongnightLanternStallId;
        stall.Position = CellCenter(LongnightLanternFeastLayout.StallCell) +
            new Vector2(0, 8);
        stall.ZIndex = 1;
        AddChild(stall);
        session.Festival.Changed += Refresh;
    }

    public override void _Draw()
    {
        var result = _session.Festival.ResultFor(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            CalendarSystem.YearNumber(_session.Clock.Day)
        );
        var tableAnchor = CellCenter(
            LongnightLanternFeastLayout.SharedTableCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(tableAnchor.X - 46, tableAnchor.Y - 50, 92, 50),
            TableRegion
        );
        if (result is not null)
        {
            for (var index = 0;
                 index < Math.Min(2, result.ItemIds.Count);
                 index++)
            {
                DrawItemIcon(
                    result.ItemIds[index],
                    tableAnchor + new Vector2((index * 2 - 1) * 18, -31),
                    26
                );
            }
        }

        var giftAnchor = CellCenter(
            LongnightLanternFeastLayout.GiftExchangeCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(giftAnchor.X - 31, giftAnchor.Y - 70, 62, 70),
            GiftRegion
        );
        if (result is not null)
        {
            DrawItemIcon(
                result.GiftItemId,
                giftAnchor + new Vector2(-14, -31),
                21
            );
            DrawItemIcon(
                result.GiftRewardItemId,
                giftAnchor + new Vector2(14, -31),
                21
            );
        }

        var ritualAnchor = CellCenter(
            LongnightLanternFeastLayout.RitualCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(ritualAnchor.X - 39, ritualAnchor.Y - 64, 78, 64),
            result is null ? DormantRitualRegion : LitRitualRegion
        );
    }

    public override void _ExitTree()
    {
        _session.Festival.Changed -= Refresh;
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
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class LongnightLanternFeastNpcLayer : Node2D
{
    private readonly GameSession _session;

    public LongnightLanternFeastNpcLayer(GameSession session)
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
                     PlayerLocationIds.LongnightLanternFeast,
                     _session.PlayerCell
                 ))
        {
            var art = NpcArtCatalog.Resolve(
                npc.Definition.Id,
                npc.Facing
            );
            var source = art.Region;
            var height = art.TargetHeight;
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
                art.Texture,
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

    private void Refresh() => QueueRedraw();
}
