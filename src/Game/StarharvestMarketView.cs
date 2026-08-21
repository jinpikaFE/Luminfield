using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarharvestMarketView : Node2D
{
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly TargetCursor _cursor;

    public StarharvestMarketView(
        GameSession session,
        LocaleService locale
    )
    {
        _session = session;
        YSortEnabled = true;
        AddChild(new StarharvestMarketBackdrop());
        AddChild(new StarharvestMarketStationLayer(session));
        AddChild(new StarharvestMarketNpcLayer(session));

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = session.InsideStarharvestMarket
                ? new Vector2(session.PlayerX, session.PlayerY)
                : CellCenter(StarharvestMarketLayout.SafeArrivalCell),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerLocation(
                position.X,
                position.Y,
                PlayerLocationIds.StarharvestMarket
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

    public Vector2 PlayerPosition => _player.Position;

    public event Action? ExitRequested;
    public event Action? ClosedRequested;
    public event Action? ShowcaseRequested;
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
            StarharvestMarketLayout.ExitCell
        ))
        {
            var result = _session.TryExitStarharvestMarket(
                StarharvestMarketLayout.ExitCell
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
            StarharvestMarketLayout.ExhibitCell
        ) || IsTargetOrAdjacent(
            target,
            player,
            StarharvestMarketLayout.BidBoardCell
        ))
        {
            var cell = IsTargetOrAdjacent(
                target,
                player,
                StarharvestMarketLayout.ExhibitCell
            )
                ? StarharvestMarketLayout.ExhibitCell
                : StarharvestMarketLayout.BidBoardCell;
            var result = _session.CheckFestivalStation(cell);
            if (result.Succeeded)
            {
                ShowcaseRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
            }
        }
        else if (IsTargetOrAdjacent(
            target,
            player,
            StarharvestMarketLayout.ShopCell
        ))
        {
            var result = _session.CheckFestivalStation(
                StarharvestMarketLayout.ShopCell
            );
            if (result.Succeeded)
            {
                ShopRequested?.Invoke();
            }
            else
            {
                NoticeRequested?.Invoke(result.MessageKey);
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

        foreach (var station in new[]
        {
            StarharvestMarketLayout.ExitCell,
            StarharvestMarketLayout.ExhibitCell,
            StarharvestMarketLayout.BidBoardCell,
            StarharvestMarketLayout.ShopCell
        })
        {
            if (IsTargetOrAdjacent(target, player, station))
            {
                return _session.PreviewSelectedTarget(station);
            }
        }

        return TargetPreview.Neutral(target);
    }

    private VillageNpcState? ResolveNpcTarget(
        GridPosition target,
        GridPosition player
    )
    {
        var current = _session.Village.CurrentNpcs(
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.StarharvestMarket,
            player
        );
        var exact = current.FirstOrDefault(npc => npc.Position == target);
        if (exact is not null)
        {
            return exact;
        }

        return current
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
        if (!StarharvestMarketLayout.IsWalkable(cell))
        {
            return false;
        }

        return _session.Village.NpcAt(
            cell,
            _session.Clock.Day,
            _session.Clock.MinuteOfDay,
            PlayerLocationIds.StarharvestMarket,
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

internal sealed partial class StarharvestMarketBackdrop : Node2D
{
    private static readonly Texture2D Background = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/starharvest/starharvest_market_backdrop.png"
    );

    public StarharvestMarketBackdrop()
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

internal sealed partial class StarharvestMarketStationLayer : Node2D
{
    private static readonly Texture2D Props = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/starharvest/starharvest_market_props.png"
    );
    private static readonly Rect2 EmptyExhibitRegion =
        new(36, 155, 555, 352);
    private static readonly Rect2 SubmittedExhibitRegion =
        new(662, 155, 556, 352);
    private static readonly Rect2 BidBoardRegion =
        new(138, 638, 363, 494);

    private readonly GameSession _session;

    public StarharvestMarketStationLayer(GameSession session)
    {
        _session = session;
        ZIndex = 7;
        TextureFilter = TextureFilterEnum.Nearest;

        var stall = GeneratedArt.CreateMarketStallSprite();
        stall.Name = FestivalCatalog.StarharvestShopId;
        stall.Position = CellCenter(StarharvestMarketLayout.ShopCell) +
            new Vector2(0, 8);
        stall.ZIndex = 1;
        AddChild(stall);
        session.Festival.Changed += Refresh;
    }

    public override void _Draw()
    {
        var result = _session.Festival.ResultFor(
            FestivalCatalog.StarharvestMarketFestivalId,
            CalendarSystem.YearNumber(_session.Clock.Day)
        );
        var exhibitAnchor = CellCenter(
            StarharvestMarketLayout.ExhibitCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(
                exhibitAnchor.X - 46,
                exhibitAnchor.Y - 58,
                92,
                58
            ),
            result is null
                ? EmptyExhibitRegion
                : SubmittedExhibitRegion
        );

        if (result is not null)
        {
            for (var index = 0;
                 index < Math.Min(3, result.ItemIds.Count);
                 index++)
            {
                if (!HotbarSlotContent.TryGetIconRegion(
                        result.ItemIds[index],
                        out var texture,
                        out var region
                    ))
                {
                    continue;
                }

                DrawTextureRectRegion(
                    texture,
                    new Rect2(
                        exhibitAnchor.X - 12 + (index - 1) * 25,
                        exhibitAnchor.Y - 47,
                        24,
                        24
                    ),
                    region
                );
            }
        }

        var boardAnchor = CellCenter(
            StarharvestMarketLayout.BidBoardCell
        ) + new Vector2(0, 8);
        DrawTextureRectRegion(
            Props,
            new Rect2(
                boardAnchor.X - 23,
                boardAnchor.Y - 64,
                46,
                64
            ),
            BidBoardRegion
        );
    }

    public override void _ExitTree()
    {
        _session.Festival.Changed -= Refresh;
    }

    private void Refresh()
    {
        QueueRedraw();
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}

internal sealed partial class StarharvestMarketNpcLayer : Node2D
{
    private readonly GameSession _session;

    public StarharvestMarketNpcLayer(GameSession session)
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
                     PlayerLocationIds.StarharvestMarket,
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

    private void Refresh()
    {
        QueueRedraw();
    }
}
