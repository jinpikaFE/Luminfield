using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class GleamrisePlantingFestivalView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public GleamrisePlantingFestivalView(
        GameSession session,
        LocaleService locale
    )
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new GleamrisePlantingFestivalBackdrop());
        AddChild(new GleamrisePlantingFestivalStationLayer(session));
        AddChild(new GleamrisePlantingFestivalNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideGleamrisePlantingFestival
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(GleamrisePlantingFestivalLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.GleamrisePlantingFestival
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
    public event Action? ActivityRequested;
    public event Action? ExchangeRequested;
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
            GleamrisePlantingFestivalLayout.ExitCell
        ))
        {
            var result = _session.TryExitFestival(
                GleamrisePlantingFestivalLayout.ExitCell
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
        else if (IsTargetOrAdjacent(
            target,
            player,
            GleamrisePlantingFestivalLayout.ActivityTableCell
        ))
        {
            var result = _session.CheckFestivalStation(
                FestivalCatalog.GleamriseSharedBloomfieldActivityId,
                GleamrisePlantingFestivalLayout.ActivityTableCell
            );
            if (result.Succeeded)
            {
                ActivityRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (IsTargetOrAdjacent(
            target,
            player,
            GleamrisePlantingFestivalLayout.SeedExchangeCell
        ))
        {
            var result = _session.CheckFestivalStation(
                FestivalCatalog.GleamriseSeedExchangeId,
                GleamrisePlantingFestivalLayout.SeedExchangeCell
            );
            if (result.Succeeded)
            {
                ExchangeRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (ResolvePlotTarget(target, player) is { } plot)
        {
            var result = _session.PlantGleamrisePlot(plot);
            NoticeRequested?.Invoke(result.MessageKey);
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

        foreach (var station in new[]
        {
            GleamrisePlantingFestivalLayout.ExitCell,
            GleamrisePlantingFestivalLayout.ActivityTableCell,
            GleamrisePlantingFestivalLayout.SeedExchangeCell
        })
        {
            if (IsTargetOrAdjacent(target, player, station))
            {
                return _session.PreviewSelectedTarget(station);
            }
        }

        return ResolvePlotTarget(target, player) is { } plot
            ? _session.PreviewSelectedTarget(plot)
            : TargetPreview.Neutral(target);
    }

    private VillageNpcState? ResolveNpcTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.GleamrisePlantingFestival,
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

    private static GridPosition? ResolvePlotTarget(
        GridPosition target,
        GridPosition player
    )
    {
        if (GleamrisePlantingFestivalLayout.PlotIdsByCell.ContainsKey(target))
        {
            return target;
        }

        return GleamrisePlantingFestivalLayout.PlotCells
            .Where(plot => IsAdjacent(player, plot))
            .OrderBy(plot => Distance(plot, target))
            .ThenBy(plot => plot.Y)
            .ThenBy(plot => plot.X)
            .Cast<GridPosition?>()
            .FirstOrDefault();
    }

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        return GleamrisePlantingFestivalLayout.IsWalkable(cell) &&
            _session.Village.NpcAt(
                cell,
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.GleamrisePlantingFestival,
                _player.CurrentCell
            ) is null;
    }

    private void HandleTimeChanged()
    {
        _ = _session.ResolveGleamriseChallengeDeadline();
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

internal sealed partial class GleamrisePlantingFestivalBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/gleamrise/gleamrise_planting_festival_backdrop.png"
    );

    public GleamrisePlantingFestivalBackdrop()
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

internal sealed partial class GleamrisePlantingFestivalStationLayer : Node2D
{
    private static readonly Texture2D Props = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/gleamrise/gleamrise_planting_festival_props.png"
    );
    private static readonly Rect2 EmptyFieldRegion = new(43, 165, 540, 425);
    private static readonly Rect2 BloomFieldRegion =
        new(43, 792, 540, 425);
    private static readonly Rect2 ActivityTableRegion =
        new(715, 869, 450, 348);

    private readonly GameSession _session;

    public GleamrisePlantingFestivalStationLayer(GameSession session)
    {
        _session = session;
        ZIndex = 7;
        TextureFilter = TextureFilterEnum.Nearest;

        var exchange = GeneratedArt.CreateMarketStallSprite();
        exchange.Name = FestivalCatalog.GleamriseSeedExchangeId;
        exchange.Position = CellCenter(
            GleamrisePlantingFestivalLayout.SeedExchangeCell
        ) + new Vector2(0, 8);
        exchange.ZIndex = 1;
        AddChild(exchange);
        session.Festival.Changed += Refresh;
    }

    public override void _Draw()
    {
        var year = CalendarSystem.YearNumber(_session.Clock.Day);
        var attempt = _session.Festival.PlantingAttemptFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        var result = _session.Festival.ResultFor(
            FestivalCatalog.GleamrisePlantingFestivalId,
            year
        );
        var plantings = result?.Plantings ?? attempt?.Plantings ?? [];
        var fieldRegion = result?.Plantings.Count ==
            GleamrisePlantingFestivalLayout.PlotIds.Count
                ? BloomFieldRegion
                : EmptyFieldRegion;
        var fieldAnchor = CellCenter(
            GleamrisePlantingFestivalLayout.FieldAnchorCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(fieldAnchor.X - 60, fieldAnchor.Y - 94, 120, 94),
            fieldRegion
        );

        foreach (var planting in plantings)
        {
            if (!GleamrisePlantingFestivalLayout.PlotCellsById.TryGetValue(
                    planting.PlotId,
                    out var cell
                ) || !DataCatalog.Items.TryGetValue(
                    planting.SeedItemId,
                    out var item
                ) || item.CropId is null ||
                !GeneratedArt.TryGleamriseCropRow(item.CropId, out var row))
            {
                continue;
            }

            var center = CellCenter(cell);
            DrawTextureRectRegion(
                GeneratedArt.GleamriseCropsTexture,
                new Rect2(center.X - 9, center.Y - 13, 18, 18),
                GeneratedArt.GleamriseCropRegion(row, 2)
            );
        }

        var tableAnchor = CellCenter(
            GleamrisePlantingFestivalLayout.ActivityTableCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(tableAnchor.X - 26, tableAnchor.Y - 40, 52, 40),
            ActivityTableRegion
        );
    }

    public override void _ExitTree()
    {
        _session.Festival.Changed -= Refresh;
    }

    private void Refresh() => QueueRedraw();

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class GleamrisePlantingFestivalNpcLayer : Node2D
{
    private readonly GameSession _session;

    public GleamrisePlantingFestivalNpcLayer(GameSession session)
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
                     PlayerLocationIds.GleamrisePlantingFestival,
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
