using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FarmView : Node2D
{
    public static readonly GridPosition MiraCell = new(32, 9);
    public static readonly GridPosition CottageDoorCell = new(16, 11);

    private static readonly HashSet<GridPosition> ExtraBlocked =
    [
        new(2, 5), new(3, 5), new(2, 6),
        new(43, 4), new(44, 4), new(44, 5),
        new(4, 25), new(5, 25), new(4, 26),
        new(30, 3), new(30, 4),
        new(14, 8), new(14, 9),
        new(42, 8), new(42, 9)
    ];

    private readonly GameSession _session;
    private readonly TileMapLayer _baseLayer;
    private readonly TileMapLayer _soilLayer;
    private readonly TileMapLayer _cropLayer;
    private readonly TileMapLayer _propLayer;
    private readonly CanvasModulate _canvasModulate;
    private readonly TargetCursor _cursor;
    private readonly PlayerController _player;

    public FarmView(GameSession session)
    {
        _session = session;
        YSortEnabled = true;

        var environmentTiles = TilePaletteFactory.CreateEnvironment();
        _baseLayer = Layer("Base", environmentTiles, -20);
        _soilLayer = Layer("Soil", environmentTiles, -10);
        _cropLayer = Layer("Crops", TilePaletteFactory.CreateCrops(), 0);
        _propLayer = Layer("Props", environmentTiles, 5);
        _baseLayer.Visible = false;
        _propLayer.Visible = false;

        AddChild(new FarmBackdrop());
        _canvasModulate = new CanvasModulate { Color = Colors.White };
        AddChild(_canvasModulate);

        AddChild(new CropGlowLayer(session));
        AddChild(new MoteField(new Rect2(0, 0, FarmSystem.MapWidth * 16, FarmSystem.MapHeight * 16)));

        var mira = CreateCharacterSprite(8);
        mira.Name = "Mira";
        mira.Position = CellCenter(MiraCell);
        mira.ZIndex = 8;
        mira.AddChild(new ActorShadow
        {
            Position = new Vector2(0, 9),
            ZIndex = -1,
        });
        AddChild(mira);
        AddChild(new MiraBeacon(session)
        {
            Position = CellCenter(MiraCell),
            ZIndex = 24,
        });

        _player = new PlayerController(CanOccupy)
        {
            Name = "Player",
            Position = new Vector2(session.PlayerX, session.PlayerY),
            ZIndex = 10
        };
        _player.PositionChanged += position =>
            _session.SetPlayerState(position.X, position.Y, false);
        AddChild(_player);

        var camera = new Camera2D
        {
            Zoom = Vector2.One,
            PositionSmoothingEnabled = false,
            LimitLeft = 0,
            LimitTop = 0,
            LimitRight = FarmSystem.MapWidth * 16,
            LimitBottom = FarmSystem.MapHeight * 16
        };
        _player.AddChild(camera);

        _cursor = new TargetCursor(() => _player.TargetCell);
        _cursor.ZIndex = 20;
        AddChild(_cursor);

        BuildBaseMap();
        RefreshAllFarmTiles();
        session.Farm.TileChanged += RefreshFarmTile;
        session.Clock.TimeChanged += UpdateLighting;
        UpdateLighting();
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
    public event Action? MiraRequested;
    public event Action? EnterCottageRequested;
    public event Action? StepRequested
    {
        add => _player.Stepped += value;
        remove => _player.Stepped -= value;
    }

    public Vector2 PlayerPosition => _player.Position;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!ControlsEnabled || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        var target = _player.TargetCell;
        if (target == MiraCell || IsAdjacent(_player.CurrentCell, MiraCell))
        {
            MiraRequested?.Invoke();
        }
        else if (target == CottageDoorCell)
        {
            EnterCottageRequested?.Invoke();
        }
        else
        {
            UseRequested?.Invoke(target);
        }

        GetViewport().SetInputAsHandled();
    }

    public void RefreshFarmTile(GridPosition position)
    {
        _soilLayer.EraseCell(new Vector2I(position.X, position.Y));
        _cropLayer.EraseCell(new Vector2I(position.X, position.Y));

        if (!_session.Farm.Tiles.TryGetValue(position, out var tile))
        {
            return;
        }

        var soilAtlas = tile.Watered
            ? TilePaletteFactory.WateredSoil
            : TilePaletteFactory.DrySoil;
        _soilLayer.SetCell(new Vector2I(position.X, position.Y), 0, new Vector2I(soilAtlas, 0));
        if (string.IsNullOrWhiteSpace(tile.CropId))
        {
            return;
        }

        var definition = DataCatalog.Crop(tile.CropId);
        var atlas = definition.AtlasStartIndex + definition.GetStageIndex(tile.WateredNights);
        _cropLayer.SetCell(new Vector2I(position.X, position.Y), 0, new Vector2I(atlas, 0));
    }

    public override void _ExitTree()
    {
        _session.Farm.TileChanged -= RefreshFarmTile;
        _session.Clock.TimeChanged -= UpdateLighting;
    }

    private TileMapLayer Layer(string name, TileSet tileSet, int zIndex)
    {
        var layer = new TileMapLayer
        {
            Name = name,
            TileSet = tileSet,
            ZIndex = zIndex,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(layer);
        return layer;
    }

    private void BuildBaseMap()
    {
        for (var y = 0; y < FarmSystem.MapHeight; y++)
        {
            for (var x = 0; x < FarmSystem.MapWidth; x++)
            {
                var position = new GridPosition(x, y);
                var atlas = TilePaletteFactory.Grass;
                if (x >= 37 && y >= 20)
                {
                    atlas = TilePaletteFactory.Water;
                }
                else if ((x == 36 && y >= 19) || (y == 19 && x >= 36))
                {
                    atlas = TilePaletteFactory.PondBank;
                }
                else if (IsMoonstonePath(position))
                {
                    atlas = (x + y) % 2 == 0
                        ? TilePaletteFactory.MoonstonePath
                        : TilePaletteFactory.MoonstonePathAlt;
                }
                else if (FarmVisualLayout.IsPlantingBed(position))
                {
                    atlas = (x + y * 2) % 3 == 0
                        ? TilePaletteFactory.FarmFieldAlt
                        : TilePaletteFactory.FarmField;
                }
                else if ((x * 3 + y * 5) % 23 == 0)
                {
                    atlas = TilePaletteFactory.FlowerMeadow;
                }
                else if ((x + y * 2) % 7 == 0)
                {
                    atlas = TilePaletteFactory.GrassAlt;
                }

                _baseLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(atlas, 0));

                if (x is 0 or FarmSystem.MapWidth - 1 || y is 0 or FarmSystem.MapHeight - 1)
                {
                    _propLayer.SetCell(
                        new Vector2I(x, y),
                        0,
                        new Vector2I(TilePaletteFactory.Hedge, 0)
                    );
                }

            }
        }

        _propLayer.SetCell(
            new Vector2I(CottageDoorCell.X, CottageDoorCell.Y),
            0,
            new Vector2I(TilePaletteFactory.Doorstep, 0)
        );
    }

    private void RefreshAllFarmTiles()
    {
        foreach (var position in _session.Farm.Tiles.Keys)
        {
            RefreshFarmTile(position);
        }
    }

    private void UpdateLighting()
    {
        var progress = (_session.Clock.MinuteOfDay - GameClock.StartMinute) /
            (float)(GameClock.EndMinute - GameClock.StartMinute);
        var daylight = Mathf.Sin(progress * Mathf.Pi);
        _canvasModulate.Color = new Color(
            0.78f + daylight * 0.17f,
            0.80f + daylight * 0.15f,
            0.95f + daylight * 0.05f
        );
    }

    private static bool IsMoonstonePath(GridPosition position) =>
        (position.Y == 11 && position.X is >= 7 and <= 34) ||
        (position.X is >= 7 and <= 9 && position.Y is >= 10 and <= 13) ||
        (position.X is >= 29 and <= 33 && position.Y is >= 9 and <= 12);

    private bool CanOccupy(Vector2 worldPosition)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(worldPosition.X / 16),
            Mathf.FloorToInt(worldPosition.Y / 16)
        );
        if (cell.X is < 1 or >= FarmSystem.MapWidth - 1 ||
            cell.Y is < 1 or >= FarmSystem.MapHeight - 1)
        {
            return false;
        }

        if (ExtraBlocked.Contains(cell))
        {
            return false;
        }

        return !_session.Farm.IsReserved(cell);
    }

    private static Sprite2D CreateCharacterSprite(int frame)
    {
        return new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/pixel/characters.svg"),
            RegionEnabled = true,
            RegionRect = new Rect2(frame * 16, 0, 16, 24),
            TextureFilter = TextureFilterEnum.Nearest,
            Position = new Vector2(0, -4)
        };
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);

    private static bool IsAdjacent(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) <= 1;
}

internal sealed partial class TargetCursor : Node2D
{
    private readonly Func<GridPosition> _target;
    private double _time;

    public TargetCursor(Func<GridPosition> target)
    {
        _target = target;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var target = _target();
        var origin = new Vector2(target.X * 16, target.Y * 16);
        var pulse = 0.62f + Mathf.Sin((float)_time * 4) * 0.18f;
        DrawRect(
            new Rect2(origin + new Vector2(2, 2), new Vector2(12, 12)),
            new Color(0.55f, 0.9f, 0.75f, 0.1f + pulse * 0.08f),
            true
        );
        var mint = new Color(0.55f, 0.94f, 0.75f, pulse + 0.2f);
        const float edge = 5;
        DrawPolyline([origin + new Vector2(1, edge), origin + Vector2.One, origin + new Vector2(edge, 1)], mint, 1.5f);
        DrawPolyline([origin + new Vector2(15 - edge, 1), origin + new Vector2(15, 1), origin + new Vector2(15, edge)], mint, 1.5f);
        DrawPolyline([origin + new Vector2(1, 15 - edge), origin + new Vector2(1, 15), origin + new Vector2(edge, 15)], mint, 1.5f);
        DrawPolyline([origin + new Vector2(15 - edge, 15), origin + new Vector2(15, 15), origin + new Vector2(15, 15 - edge)], mint, 1.5f);
        DrawCircle(origin + new Vector2(8, 8), 1.2f, new Color("#f4cf78"));
    }
}

internal sealed partial class FarmDecor : Node2D
{
    public FarmDecor()
    {
        ZIndex = 6;
    }

    public override void _Draw()
    {
        // Clearly bounded planting field with a broken fence at the path entrance.
        var fieldEdge = new Color("#8d7181");
        DrawLine(new Vector2(108, 202), new Vector2(580, 202), fieldEdge, 2);
        DrawLine(new Vector2(108, 202), new Vector2(108, 450), fieldEdge, 2);
        DrawLine(new Vector2(580, 202), new Vector2(580, 450), fieldEdge, 2);
        DrawLine(new Vector2(108, 450), new Vector2(580, 450), fieldEdge, 2);
        for (var x = 116; x <= 572; x += 32)
        {
            DrawRect(new Rect2(x, 197, 3, 11), new Color("#c29b78"));
            DrawCircle(new Vector2(x + 1.5f, 197), 2, new Color("#f2c66d"));
        }
        DrawRect(new Rect2(119, 191, 38, 9), new Color("#24344c"));
        DrawRect(new Rect2(121, 193, 34, 5), new Color("#8d7181"));
        DrawCircle(new Vector2(128, 195), 2, new Color("#f2c66d"));
        DrawCircle(new Vector2(148, 195), 2, new Color("#8ee6be"));

        // Cottage body, roof, windows, door, and warm entrance lanterns.
        DrawRect(new Rect2(64, 70, 128, 90), new Color("#332d49"));
        DrawRect(new Rect2(69, 76, 118, 79), new Color("#765165"));
        for (var y = 83; y <= 145; y += 11)
        {
            DrawLine(new Vector2(70, y), new Vector2(186, y), new Color("#986579"), 1);
        }
        DrawColoredPolygon(
            [new Vector2(52, 76), new Vector2(204, 76), new Vector2(172, 38), new Vector2(84, 38)],
            new Color("#171f48")
        );
        DrawPolyline(
            [new Vector2(52, 76), new Vector2(84, 38), new Vector2(172, 38), new Vector2(204, 76)],
            new Color("#7f75c8"),
            3
        );
        // Layered shingles, chimney, and roof-edge highlights.
        DrawRect(new Rect2(164, 25, 13, 34), new Color("#252841"));
        DrawRect(new Rect2(166, 28, 9, 29), new Color("#5d4d62"));
        DrawRect(new Rect2(162, 24, 17, 5), new Color("#8f7181"));
        foreach (var segment in new[]
                 {
                     new Vector4(80, 45, 176, 45),
                     new Vector4(70, 53, 186, 53),
                     new Vector4(62, 61, 194, 61),
                     new Vector4(56, 69, 200, 69),
                 })
        {
            DrawLine(
                new Vector2(segment.X, segment.Y),
                new Vector2(segment.Z, segment.W),
                new Color("#30345a"),
                2
            );
        }
        for (var x = 71; x <= 186; x += 14)
        {
            DrawLine(new Vector2(x, 53), new Vector2(x + 5, 58), new Color("#4a4670"), 1);
            DrawLine(new Vector2(x - 6, 61), new Vector2(x, 66), new Color("#4a4670"), 1);
        }
        DrawLine(new Vector2(61, 70), new Vector2(195, 70), new Color("#b795dd"), 2);
        DrawRect(new Rect2(80, 97, 23, 22), new Color("#302f52"));
        DrawRect(new Rect2(84, 101, 15, 14), new Color("#f3ca78"));
        DrawLine(new Vector2(91.5f, 101), new Vector2(91.5f, 115), new Color("#fff0ac"), 1);
        DrawRect(new Rect2(153, 97, 23, 22), new Color("#302f52"));
        DrawRect(new Rect2(157, 101, 15, 14), new Color("#f3ca78"));
        DrawLine(new Vector2(164.5f, 101), new Vector2(164.5f, 115), new Color("#fff0ac"), 1);
        DrawRect(new Rect2(116, 118, 24, 42), new Color("#252744"));
        DrawRect(new Rect2(121, 124, 14, 36), new Color("#56445f"));
        DrawCircle(new Vector2(132, 142), 1.5f, new Color("#f3ca78"));
        DrawCircle(new Vector2(128, 51), 6, new Color("#8ee6be"));
        DrawColoredPolygon(
            [new Vector2(128, 43), new Vector2(131, 49), new Vector2(138, 51), new Vector2(131, 54), new Vector2(128, 61), new Vector2(125, 54), new Vector2(118, 51), new Vector2(125, 49)],
            new Color("#f3ca78")
        );
        DrawCircle(new Vector2(107, 151), 4, new Color(0.95f, 0.71f, 0.3f, 0.2f));
        DrawCircle(new Vector2(149, 151), 4, new Color(0.95f, 0.71f, 0.3f, 0.2f));
        DrawCircle(new Vector2(107, 151), 1.7f, new Color("#f3ca78"));
        DrawCircle(new Vector2(149, 151), 1.7f, new Color("#f3ca78"));
        // Flower boxes and a covered side porch make the cottage read as a home.
        DrawRect(new Rect2(78, 118, 27, 5), new Color("#5b3e50"));
        DrawRect(new Rect2(151, 118, 27, 5), new Color("#5b3e50"));
        foreach (var flower in new[] { new Vector2(83, 117), new Vector2(90, 116), new Vector2(98, 117), new Vector2(157, 117), new Vector2(165, 116), new Vector2(172, 117) })
        {
            DrawLine(flower, flower + new Vector2(0, -4), new Color("#62b781"), 1);
            DrawCircle(flower + new Vector2(0, -5), 1.6f, (Mathf.FloorToInt(flower.X) & 1) == 0 ? new Color("#b795dd") : new Color("#8ee6be"));
        }
        DrawColoredPolygon(
            [new Vector2(47, 112), new Vector2(75, 112), new Vector2(82, 99), new Vector2(53, 99)],
            new Color("#222c4b")
        );
        DrawLine(new Vector2(49, 111), new Vector2(77, 111), new Color("#6e6d99"), 2);
        DrawRect(new Rect2(53, 111, 3, 43), new Color("#624957"));
        DrawRect(new Rect2(73, 111, 3, 43), new Color("#624957"));

        // Well, buckets, and planters form a small working yard.
        DrawCircle(new Vector2(231, 143), 14, new Color("#292b43"));
        DrawCircle(new Vector2(231, 140), 12, new Color("#766476"));
        DrawCircle(new Vector2(231, 138), 8, new Color("#173a50"));
        DrawArc(new Vector2(231, 137), 8, 3.2f, 5.9f, 12, new Color("#64c6be"), 1);
        DrawLine(new Vector2(219, 142), new Vector2(219, 121), new Color("#825d5d"), 3);
        DrawLine(new Vector2(243, 142), new Vector2(243, 121), new Color("#825d5d"), 3);
        DrawLine(new Vector2(218, 121), new Vector2(244, 121), new Color("#b27b68"), 3);
        DrawCircle(new Vector2(231, 121), 3, new Color("#f3ca78"));
        DrawRect(new Rect2(205, 145, 11, 12), new Color("#7c555d"));
        DrawLine(new Vector2(206, 149), new Vector2(215, 149), new Color("#c08a70"), 1);
        DrawRect(new Rect2(247, 145, 15, 9), new Color("#5c4052"));
        DrawCircle(new Vector2(251, 144), 2, new Color("#8ee6be"));
        DrawCircle(new Vector2(257, 143), 2, new Color("#b795dd"));

        // Greenhouse with bright glass bays, plants, and a distinct mint doorway.
        DrawRect(new Rect2(544, 78, 128, 98), new Color("#132b3a"));
        DrawRect(new Rect2(550, 81, 116, 89), new Color(0.3f, 0.8f, 0.72f, 0.16f));
        DrawArc(new Vector2(608, 80), 61, Mathf.Pi, Mathf.Tau, 32, new Color("#63ded0"), 3);
        DrawLine(new Vector2(547, 80), new Vector2(669, 80), new Color("#63ded0"), 3);
        DrawLine(new Vector2(547, 80), new Vector2(547, 173), new Color("#428b94"), 3);
        DrawLine(new Vector2(669, 80), new Vector2(669, 173), new Color("#428b94"), 3);
        DrawLine(new Vector2(608, 19), new Vector2(608, 128), new Color("#67d9cf"), 2);
        DrawLine(new Vector2(608, 20), new Vector2(559, 80), new Color("#397d8c"), 2);
        DrawLine(new Vector2(608, 20), new Vector2(657, 80), new Color("#397d8c"), 2);
        DrawArc(new Vector2(608, 80), 43, Mathf.Pi, Mathf.Tau, 24, new Color("#397d8c"), 1);
        DrawArc(new Vector2(608, 80), 24, Mathf.Pi, Mathf.Tau, 20, new Color("#397d8c"), 1);
        DrawCircle(new Vector2(608, 19), 7, new Color(0.45f, 0.9f, 0.82f, 0.18f));
        DrawColoredPolygon(
            [new Vector2(608, 9), new Vector2(613, 18), new Vector2(608, 26), new Vector2(603, 18)],
            new Color("#8ee6be")
        );
        DrawLine(new Vector2(608, 11), new Vector2(608, 22), new Color("#e8fff0"), 1);
        for (var x = 560; x <= 656; x += 16)
        {
            DrawLine(new Vector2(x, 82), new Vector2(x, 169), new Color("#327985"), 1);
        }
        for (var x = 562; x <= 650; x += 22)
        {
            DrawLine(new Vector2(x, 157), new Vector2(x - 4, 143), new Color("#6fd69d"), 2);
            DrawLine(new Vector2(x, 150), new Vector2(x - 8, 147), new Color("#9bf0ba"), 2);
            DrawLine(new Vector2(x, 151), new Vector2(x + 7, 145), new Color("#9bf0ba"), 2);
        }
        DrawRect(new Rect2(598, 128, 22, 42), new Color("#244f5e"));
        DrawRect(new Rect2(602, 132, 14, 38), new Color(0.55f, 0.94f, 0.75f, 0.24f));
        DrawCircle(new Vector2(613, 150), 1.5f, new Color("#f3ca78"));
        foreach (var shimmer in new[] { new Vector2(572, 93), new Vector2(590, 61), new Vector2(628, 47), new Vector2(650, 101) })
        {
            DrawLine(shimmer + new Vector2(-3, 0), shimmer + new Vector2(3, 0), new Color(0.78f, 1, 0.94f, 0.55f), 1);
            DrawLine(shimmer + new Vector2(0, -3), shimmer + new Vector2(0, 3), new Color(0.78f, 1, 0.94f, 0.55f), 1);
        }
        // Workbench and specimen jars beside the greenhouse.
        DrawRect(new Rect2(675, 137, 31, 5), new Color("#95675f"));
        DrawRect(new Rect2(678, 142, 3, 18), new Color("#5a4050"));
        DrawRect(new Rect2(700, 142, 3, 18), new Color("#5a4050"));
        DrawRect(new Rect2(680, 128, 7, 9), new Color("#31596c"));
        DrawRect(new Rect2(681, 130, 5, 6), new Color("#72d9c2"));
        DrawRect(new Rect2(690, 125, 8, 12), new Color("#4d3e67"));
        DrawRect(new Rect2(692, 128, 4, 8), new Color("#b795dd"));

        // Pond glints, lily pads, and luminous crystal bank.
        DrawArc(new Vector2(673, 356), 10, 0.15f, 2.5f, 16, new Color(0.55f, 0.9f, 0.75f, 0.45f), 1);
        DrawArc(new Vector2(720, 389), 14, 0.2f, 2.8f, 16, new Color(0.55f, 0.9f, 0.75f, 0.36f), 1);
        DrawCircle(new Vector2(650, 428), 6, new Color("#315f63"));
        DrawLine(new Vector2(650, 428), new Vector2(655, 424), new Color("#9bf0ba"), 1);
        DrawCircle(new Vector2(700, 422), 8, new Color(0.55f, 0.9f, 0.75f, 0.28f));
        DrawColoredPolygon(
            [new Vector2(694, 430), new Vector2(700, 407), new Vector2(706, 430)],
            new Color("#8ee6be")
        );
        DrawColoredPolygon(
            [new Vector2(708, 432), new Vector2(714, 415), new Vector2(720, 432)],
            new Color("#7f75c8")
        );

        // Two-tone trees with lit edges instead of flat silhouettes.
        foreach (var center in new[]
                 {
                     new Vector2(42, 92), new Vector2(700, 74), new Vector2(74, 430),
                     new Vector2(476, 58), new Vector2(454, 466)
                 })
        {
            DrawRect(new Rect2(center.X - 4, center.Y + 8, 8, 17), new Color("#6e4d55"));
            DrawCircle(center + new Vector2(-6, 1), 13, new Color("#213e50"));
            DrawCircle(center + new Vector2(6, -2), 15, new Color("#2c5c61"));
            DrawCircle(center + new Vector2(10, 2), 8, new Color("#3e7870"));
            DrawArc(center + new Vector2(5, -3), 15, 3.5f, 5.8f, 16, new Color("#73c996"), 2);
            DrawCircle(center + new Vector2(11, -4), 1.5f, new Color("#f3ca78"));
        }

        // Path lamps lead the eye from the cottage to Mira and the farm entrance.
        foreach (var lamp in new[] { new Vector2(151, 176), new Vector2(338, 176), new Vector2(535, 176) })
        {
            DrawLine(lamp, lamp + new Vector2(0, -12), new Color("#72596b"), 2);
            DrawCircle(lamp + new Vector2(0, -14), 5, new Color(0.95f, 0.78f, 0.4f, 0.18f));
            DrawCircle(lamp + new Vector2(0, -14), 2, new Color("#f3ca78"));
        }
    }
}

internal sealed partial class MiraBeacon : Node2D
{
    private readonly GameSession _session;
    private double _time;

    public MiraBeacon(GameSession session)
    {
        _session = session;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var actionable = _session.Quest.Stage is QuestStage.TalkToMira or QuestStage.ReturnToMira;
        var bob = Mathf.Sin((float)_time * 3) * 2;
        var marker = new Vector2(0, -27 + bob);
        var glow = actionable ? new Color("#f3ca78") : new Color("#8ee6be");
        DrawCircle(marker, actionable ? 7 : 5, new Color(glow, 0.18f));
        DrawColoredPolygon(
            [
                marker + new Vector2(0, -5),
                marker + new Vector2(5, 0),
                marker + new Vector2(0, 5),
                marker + new Vector2(-5, 0),
            ],
            new Color("#17243d")
        );
        DrawPolyline(
            [
                marker + new Vector2(0, -5),
                marker + new Vector2(5, 0),
                marker + new Vector2(0, 5),
                marker + new Vector2(-5, 0),
                marker + new Vector2(0, -5),
            ],
            glow,
            1.5f
        );
        if (actionable)
        {
            DrawLine(marker + new Vector2(0, -2.5f), marker + new Vector2(0, 1), glow, 1.5f);
            DrawCircle(marker + new Vector2(0, 3), 0.9f, glow);
        }
        else
        {
            DrawCircle(marker, 1.5f, glow);
        }
    }
}

internal sealed partial class MoteField : Node2D
{
    private readonly Rect2 _bounds;
    private readonly Vector2[] _points;
    private double _time;

    public MoteField(Rect2 bounds)
    {
        _bounds = bounds;
        ZIndex = 30;
        var random = new Random(2407);
        _points = Enumerable.Range(0, 58)
            .Select(_ => new Vector2(
                (float)(random.NextDouble() * bounds.Size.X),
                (float)(random.NextDouble() * bounds.Size.Y)
            ))
            .ToArray();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var index = 0; index < _points.Length; index++)
        {
            var basePoint = _points[index];
            var point = new Vector2(
                basePoint.X + Mathf.Sin((float)_time + index) * 3,
                Mathf.PosMod(basePoint.Y - (float)_time * (2 + index % 3), _bounds.Size.Y)
            );
            var alpha = 0.22f + Mathf.Sin((float)_time * 2 + index) * 0.14f;
            var color = (index % 7) switch
            {
                0 => new Color(0.72f, 0.58f, 0.95f, alpha),
                1 => new Color(0.98f, 0.8f, 0.42f, alpha),
                _ => new Color(0.55f, 0.95f, 0.8f, alpha),
            };
            DrawCircle(point, index % 5 == 0 ? 1.5f : 1, color);
        }
    }
}
