using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class FarmVisualLayout
{
    public static readonly Rect2I[] PlantingBeds =
    [
        new Rect2I(11, 15, 7, 3),
        new Rect2I(19, 15, 6, 3),
        new Rect2I(26, 15, 7, 3),
        new Rect2I(11, 19, 7, 3),
        new Rect2I(19, 19, 6, 3),
        new Rect2I(26, 19, 7, 3),
    ];

    public static bool IsPlantingBed(GridPosition position) => FarmSystem.IsPlantingBed(position);
}

internal sealed partial class FarmWeatherOverlay : CanvasLayer
{
    public FarmWeatherOverlay(GameSession session)
    {
        Layer = 40;
        AddChild(new WeatherEffectView(session));
    }
}

internal sealed partial class WeatherEffectView : Control
{
    private readonly GameSession _session;
    private double _time;

    public WeatherEffectView(GameSession session)
    {
        _session = session;
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        session.Weather.Changed += OnWeatherChanged;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_session.Weather.CurrentId ==
            DataCatalog.LongnightSnowWeatherId)
        {
            DrawLongnightSnow();
            return;
        }

        if (_session.Weather.CurrentId == DataCatalog.RainWeatherId)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("#10284a20"));
            for (var index = 0; index < 62; index++)
            {
                var x = Mathf.PosMod(
                    index * 73 + (float)_time * 145,
                    Math.Max(1, Size.X + 48)
                ) - 24;
                var y = Mathf.PosMod(
                    index * 41 + (float)_time * 205,
                    Math.Max(1, Size.Y + 36)
                ) - 18;
                var length = 5 + index % 4;
                DrawLine(
                    new Vector2(x, y),
                    new Vector2(x - 2, y + length),
                    index % 5 == 0
                        ? new Color("#a4f4e2a8")
                        : new Color("#79cde58c"),
                    index % 7 == 0 ? 1.5f : 1
                );
            }
            return;
        }

        if (_session.Weather.CurrentId != DataCatalog.StardustWindWeatherId)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#4c2d7420"));
        for (var index = 0; index < 34; index++)
        {
            var x = Mathf.PosMod(
                index * 97 + (float)_time * (22 + index % 4 * 5),
                Math.Max(1, Size.X + 36)
            ) - 18;
            var y = Mathf.PosMod(
                index * 53 + Mathf.Sin((float)_time * 1.3f + index) * 10,
                Math.Max(1, Size.Y)
            );
            var color = (index % 3) switch
            {
                0 => new Color("#83f0d2b8"),
                1 => new Color("#c7a2f0a8"),
                _ => new Color("#f4d486a6")
            };
            DrawCircle(new Vector2(x, y), index % 6 == 0 ? 1.8f : 1.1f, color);
            if (index % 6 == 0)
            {
                DrawLine(
                    new Vector2(x - 4, y),
                    new Vector2(x + 4, y),
                    new Color(color, 0.48f),
                    1
                );
            }
        }
    }

    private void DrawLongnightSnow()
    {
        DrawRect(
            new Rect2(Vector2.Zero, Size),
            new Color("#b7c9ff0d")
        );
        DrawSnowflakeLayer(20, 17, 24f, 5f, 0);
        DrawSnowflakeLayer(14, 83, 43f, 8f, 2);
        DrawSnowflakeLayer(6, 149, 68f, 12f, 3);

        var frame = (int)Math.Floor(_time * 7) % 3;
        for (var index = 0; index < 2; index++)
        {
            var x = Mathf.PosMod(
                index * 337 + (float)_time * 52,
                Math.Max(1, Size.X + 40)
            ) - 20;
            var y = Mathf.PosMod(
                index * 139 + 84 + (float)_time * 7,
                Math.Max(1, Size.Y - 40)
            ) + 20;
            DrawTextureRectRegion(
                GeneratedArt.LongnightSnowTexture,
                new Rect2(
                    new Vector2(Mathf.Round(x), Mathf.Round(y)),
                    new Vector2(16, 10)
                ),
                GeneratedArt.LongnightSnowGustRegion(
                    (frame + index) % 3
                )
            );
        }
    }

    private void DrawSnowflakeLayer(
        int count,
        int seed,
        float speed,
        float targetWidth,
        int firstVariant
    )
    {
        for (var index = 0; index < count; index++)
        {
            var variant = firstVariant == 0 ? index % 2 : firstVariant;
            var source = GeneratedArt.LongnightSnowflakeRegion(variant);
            var width = targetWidth + (firstVariant == 0 ? index % 2 : 0);
            var height = Mathf.Round(width * source.Size.Y / source.Size.X);
            var x = Mathf.PosMod(
                seed + index * 97 + (float)_time *
                    (speed + index % 3 * 3),
                Math.Max(1, Size.X + width * 2)
            ) - width;
            var y = Mathf.PosMod(
                seed * 2 + index * 61 + (float)_time * speed,
                Math.Max(1, Size.Y + height * 2)
            ) - height;
            x += Mathf.Sin((float)_time * 0.8f + index * 1.7f) *
                (firstVariant == 3 ? 5 : 2);
            DrawTextureRectRegion(
                GeneratedArt.LongnightSnowTexture,
                new Rect2(
                    new Vector2(Mathf.Round(x), Mathf.Round(y)),
                    new Vector2(width, height)
                ),
                source
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Weather.Changed -= OnWeatherChanged;
    }

    private void OnWeatherChanged() => QueueRedraw();
}

public static class FarmBackdropCatalog
{
    public const string DefaultTexturePath =
        "res://assets/generated/farm_twilight_backdrop.png";
    public const string RainveilTexturePath =
        "res://assets/generated/farm_rainveil_backdrop.png";
    public const string StarharvestTexturePath =
        "res://assets/generated/farm_starharvest_backdrop.png";
    public const string LongnightTexturePath =
        "res://assets/generated/farm_longnight_backdrop.png";

    public static string TexturePathForDay(int day) =>
        CalendarSystem.SeasonId(day) switch
        {
            CalendarSystem.RainveilSeasonId => RainveilTexturePath,
            CalendarSystem.StarharvestSeasonId => StarharvestTexturePath,
            CalendarSystem.LongnightSeasonId => LongnightTexturePath,
            _ => DefaultTexturePath
        };
}

internal sealed partial class FarmBackdrop : Sprite2D
{
    private readonly GameSession _session;
    private string? _texturePath;

    public FarmBackdrop(GameSession session)
    {
        _session = session;
        Centered = false;
        Position = Vector2.Zero;
        Scale = new Vector2(0.5f, 0.5f);
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        ZIndex = -100;
        RefreshTexture();
        session.Clock.TimeChanged += RefreshTexture;
    }

    public override void _ExitTree()
    {
        _session.Clock.TimeChanged -= RefreshTexture;
    }

    private void RefreshTexture()
    {
        var texturePath = FarmBackdropCatalog.TexturePathForDay(
            _session.Clock.Day
        );
        if (_texturePath == texturePath)
        {
            return;
        }

        Texture = GD.Load<Texture2D>(texturePath);
        _texturePath = texturePath;
    }
}

internal sealed partial class SouthernWorldGate : Node2D
{
    public SouthernWorldGate()
    {
        ZIndex = -8;
    }

    public override void _Draw()
    {
        const float left = 288;
        const float top = 476;
        const float width = 48;
        const float bottom = 544;

        DrawColoredPolygon(
            [
                new Vector2(left - 8, top + 9),
                new Vector2(left, top),
                new Vector2(left + width, top),
                new Vector2(left + width + 8, top + 9),
                new Vector2(left + width + 5, bottom),
                new Vector2(left - 5, bottom),
            ],
            new Color("#18243b")
        );
        DrawRect(
            new Rect2(left, top, width, bottom - top),
            new Color("#554f68")
        );

        for (var y = top + 4; y < bottom; y += 9)
        {
            var offset = ((int)((y - top) / 9) & 1) == 0 ? 3 : 9;
            DrawLine(
                new Vector2(left + 2, y),
                new Vector2(left + width - 2, y),
                new Color("#80738e"),
                1
            );
            for (var x = left + offset; x < left + width; x += 16)
            {
                DrawLine(
                    new Vector2(x, y),
                    new Vector2(x - 3, y + 8),
                    new Color("#37364f"),
                    1
                );
            }
        }

        DrawLine(
            new Vector2(left, top),
            new Vector2(left + width, top),
            new Color("#d0aa73"),
            2
        );
        DrawLine(
            new Vector2(left - 3, bottom - 1),
            new Vector2(left + width + 3, bottom - 1),
            new Color("#26354b"),
            2
        );

        DrawGatePost(new Vector2(left - 8, top + 5));
        DrawGatePost(new Vector2(left + width + 4, top + 5));
    }

    private void DrawGatePost(Vector2 origin)
    {
        DrawRect(new Rect2(origin.X, origin.Y, 5, 20), new Color("#4a3542"));
        DrawRect(new Rect2(origin.X - 1, origin.Y - 2, 7, 4), new Color("#9f7455"));
        DrawCircle(origin + new Vector2(2.5f, -4), 3.5f, new Color("#2ce0c2"));
        DrawCircle(origin + new Vector2(2.5f, -4), 1.5f, new Color("#eff7c8"));
    }
}

internal sealed partial class FarmPlotTrim : Node2D
{
    public FarmPlotTrim()
    {
        ZIndex = -9;
    }

    public override void _Draw()
    {
        foreach (var bed in FarmVisualLayout.PlantingBeds)
        {
            var rect = new Rect2(
                bed.Position.X * 16 - 2,
                bed.Position.Y * 16 - 2,
                bed.Size.X * 16 + 4,
                bed.Size.Y * 16 + 4
            );
            DrawRect(rect, new Color("#182d3d"), false, 3);
            DrawLine(
                rect.Position + new Vector2(3, 1),
                rect.Position + new Vector2(rect.Size.X - 3, 1),
                new Color("#816772"),
                1
            );
            DrawLine(
                rect.Position + new Vector2(1, 3),
                rect.Position + new Vector2(1, rect.Size.Y - 3),
                new Color("#5f515f"),
                1
            );

            foreach (var corner in new[]
                     {
                         rect.Position + new Vector2(2, 2),
                         rect.Position + new Vector2(rect.Size.X - 3, 2),
                         rect.Position + new Vector2(2, rect.Size.Y - 3),
                         rect.Position + new Vector2(rect.Size.X - 3, rect.Size.Y - 3),
                     })
            {
                DrawRect(new Rect2(corner, new Vector2(2, 2)), new Color("#d1a66d"));
            }
        }
    }
}

internal sealed partial class EdgeCanopyDecor : Node2D
{
    private static readonly Vector2[] NorthTrees =
    [
        new(20, 27), new(53, 35), new(225, 25), new(266, 34), new(304, 26),
        new(350, 32), new(405, 25), new(450, 34), new(718, 29), new(751, 38),
    ];

    public EdgeCanopyDecor()
    {
        ZIndex = -15;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 768, 34), new Color("#111936"));
        DrawRect(new Rect2(0, 26, 768, 20), new Color("#192d46"));

        foreach (var center in NorthTrees)
        {
            DrawRect(new Rect2(center.X - 3, center.Y + 11, 6, 22), new Color("#473b4b"));
            DrawColoredPolygon(
                [
                    center + new Vector2(0, -22),
                    center + new Vector2(-18, 8),
                    center + new Vector2(18, 8),
                ],
                new Color("#172548")
            );
            DrawColoredPolygon(
                [
                    center + new Vector2(0, -11),
                    center + new Vector2(-23, 18),
                    center + new Vector2(23, 18),
                ],
                new Color("#20375a")
            );
            DrawLine(
                center + new Vector2(-14, 8),
                center + new Vector2(2, -14),
                new Color("#354e72"),
                2
            );
        }

        for (var x = 10; x < 758; x += 29)
        {
            DrawCircle(new Vector2(x, 32 + (x % 3) * 4), 9, new Color("#26495b"));
            if (x % 4 == 0)
            {
                DrawCircle(new Vector2(x + 3, 27), 1.2f, new Color("#8ee6be"));
            }
        }
    }
}

internal sealed partial class NatureScatter : Node2D
{
    private readonly List<SoftProp> _props = [];

    public NatureScatter()
    {
        ZIndex = 3;
        var random = new Random(7124);
        var clusters = new[]
        {
            new Vector2(248, 94),
            new Vector2(390, 88),
            new Vector2(256, 207),
            new Vector2(410, 211),
            new Vector2(92, 275),
            new Vector2(309, 330),
            new Vector2(92, 414),
            new Vector2(540, 470),
            new Vector2(605, 248),
        };

        foreach (var center in clusters)
        {
            for (var index = 0; index < 9; index++)
            {
                var position = center + new Vector2(
                    (float)(random.NextDouble() * 58 - 29),
                    (float)(random.NextDouble() * 42 - 21)
                );
                if (!CanPlace(position))
                {
                    continue;
                }

                var kind = random.Next(0, 4);
                var accent = ((index + kind) % 3) switch
                {
                    0 => new Color("#8ee6be"),
                    1 => new Color("#b795dd"),
                    _ => new Color("#f3ca78"),
                };
                _props.Add(new SoftProp(position, kind, accent, 0.82f + (float)random.NextDouble() * 0.35f));
            }
        }
    }

    public override void _Draw()
    {
        foreach (var prop in _props)
        {
            DrawSetTransform(prop.Position, 0, Vector2.One * prop.Scale);
            switch (prop.Kind)
            {
                case 0:
                    DrawGrass(prop.Accent);
                    break;
                case 1:
                    DrawFlower(prop.Accent);
                    break;
                case 2:
                    DrawCrystalHerb(prop.Accent);
                    break;
                default:
                    DrawMushroom(prop.Accent);
                    break;
            }
        }

        DrawSetTransform(Vector2.Zero);
        DrawRockCluster(new Vector2(35, 386), new Color("#53627b"));
        DrawRockCluster(new Vector2(583, 306), new Color("#4d6675"));
        DrawRockCluster(new Vector2(731, 290), new Color("#4d5978"));
        DrawCrystalCluster(new Vector2(54, 352), new Color("#6de0d0"));
        DrawCrystalCluster(new Vector2(568, 281), new Color("#b68ee2"));
        DrawCrystalCluster(new Vector2(728, 171), new Color("#70d9bd"));
    }

    private static bool CanPlace(Vector2 position)
    {
        var cell = new GridPosition(
            Mathf.FloorToInt(position.X / 16),
            Mathf.FloorToInt(position.Y / 16)
        );
        if (FarmVisualLayout.IsPlantingBed(cell))
        {
            return false;
        }

        if (position.Y is > 155 and < 199)
        {
            return false;
        }

        if (position.X is > 45 and < 214 && position.Y < 175)
        {
            return false;
        }

        if (position.X is > 535 and < 680 && position.Y < 185)
        {
            return false;
        }

        return position.DistanceTo(new Vector2(520, 152)) > 42;
    }

    private void DrawGrass(Color accent)
    {
        DrawLine(new Vector2(-4, 3), new Vector2(-2, -5), new Color("#4c9c75"), 1.5f);
        DrawLine(new Vector2(0, 4), new Vector2(0, -7), new Color("#64ba84"), 1.5f);
        DrawLine(new Vector2(4, 3), new Vector2(3, -4), accent, 1.5f);
        DrawLine(new Vector2(-1, 0), new Vector2(-5, -2), new Color("#78cf93"), 1);
    }

    private void DrawFlower(Color accent)
    {
        DrawLine(new Vector2(0, 4), new Vector2(0, -4), new Color("#4d9d70"), 1.5f);
        DrawLine(new Vector2(0, 0), new Vector2(-4, -1), new Color("#75c88c"), 1);
        DrawCircle(new Vector2(0, -5), 2.3f, accent);
        DrawCircle(new Vector2(-2, -4), 1.5f, new Color("#d8c4f2"));
        DrawCircle(new Vector2(2, -4), 1.5f, new Color("#a5f0cc"));
        DrawCircle(new Vector2(0, -4), 0.8f, new Color("#fff0ac"));
    }

    private void DrawCrystalHerb(Color accent)
    {
        DrawLine(new Vector2(0, 4), new Vector2(0, -3), new Color("#3e8f78"), 1.5f);
        DrawColoredPolygon(
            [new Vector2(-2, -2), new Vector2(0, -8), new Vector2(2, -2), new Vector2(0, 1)],
            accent
        );
        DrawLine(new Vector2(0, -7), new Vector2(0, -2), new Color(1, 1, 1, 0.62f), 1);
        DrawCircle(new Vector2(-4, 0), 1.2f, new Color("#75caa0"));
        DrawCircle(new Vector2(4, 1), 1.2f, new Color("#75caa0"));
    }

    private void DrawMushroom(Color accent)
    {
        DrawRect(new Rect2(-1, -1, 3, 5), new Color("#d8c6ba"));
        DrawArc(new Vector2(0, -1), 4, Mathf.Pi, Mathf.Tau, 10, new Color("#332b4d"), 4);
        DrawArc(new Vector2(0, -2), 3, Mathf.Pi, Mathf.Tau, 10, accent, 3);
        DrawCircle(new Vector2(-1, -3), 0.7f, new Color("#fff0ac"));
    }

    private void DrawRockCluster(Vector2 center, Color color)
    {
        DrawCircle(center + new Vector2(-5, 1), 7, new Color("#27374f"));
        DrawCircle(center + new Vector2(4, -2), 9, color);
        DrawCircle(center + new Vector2(8, 3), 5, new Color(color, 0.82f));
        DrawArc(center + new Vector2(3, -3), 8, 3.5f, 5.5f, 10, new Color("#8792ad"), 1.5f);
    }

    private void DrawCrystalCluster(Vector2 center, Color color)
    {
        DrawColoredPolygon(
            [center + new Vector2(-8, 7), center + new Vector2(-4, -9), center, new Vector2(center.X + 2, center.Y + 7)],
            color.Darkened(0.14f)
        );
        DrawColoredPolygon(
            [center + new Vector2(-1, 7), center + new Vector2(5, -14), center + new Vector2(10, 7)],
            color
        );
        DrawLine(center + new Vector2(5, -12), center + new Vector2(5, 4), new Color(1, 1, 1, 0.62f), 1);
        DrawCircle(center + new Vector2(4, -7), 9, new Color(color, 0.08f));
    }

    private readonly record struct SoftProp(Vector2 Position, int Kind, Color Accent, float Scale);
}

internal sealed partial class PondAndCliffDecor : Node2D
{
    private double _time;

    public PondAndCliffDecor()
    {
        ZIndex = 7;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // A timber dock gives the pond a readable silhouette and focal point.
        DrawRect(new Rect2(609, 386, 82, 43), new Color("#302a3d"));
        for (var y = 389; y <= 422; y += 8)
        {
            DrawRect(new Rect2(612, y, 76, 6), new Color("#8b5e59"));
            DrawLine(new Vector2(612, y), new Vector2(688, y), new Color("#c1876c"), 1);
        }
        foreach (var post in new[] { new Vector2(610, 382), new Vector2(687, 382), new Vector2(610, 425), new Vector2(687, 425) })
        {
            DrawRect(new Rect2(post.X, post.Y, 4, 17), new Color("#5b4350"));
            DrawCircle(post + new Vector2(2, 1), 2, new Color("#d2a26c"));
        }

        // Animated water highlights and lily pads.
        for (var index = 0; index < 8; index++)
        {
            var x = 620 + (index * 37) % 133;
            var y = 346 + (index * 29) % 144;
            var drift = Mathf.Sin((float)_time * 1.4f + index) * 3;
            DrawLine(
                new Vector2(x + drift, y),
                new Vector2(x + 10 + drift, y),
                new Color(0.4f, 0.92f, 0.9f, 0.35f),
                1
            );
        }

        foreach (var lily in new[] { new Vector2(701, 353), new Vector2(739, 398), new Vector2(662, 461), new Vector2(720, 482) })
        {
            DrawCircle(lily, 6, new Color("#4e8b75"));
            DrawLine(lily, lily + new Vector2(5, -3), new Color("#9ad69a"), 1);
            DrawCircle(lily + new Vector2(-2, -2), 1.4f, new Color("#c69ce4"));
        }

        // Crystal shoreline and a small waterfall at the lower bank.
        DrawColoredPolygon(
            [new Vector2(735, 340), new Vector2(742, 316), new Vector2(749, 340)],
            new Color("#72e0d2")
        );
        DrawColoredPolygon(
            [new Vector2(749, 343), new Vector2(756, 326), new Vector2(763, 343)],
            new Color("#b591e2")
        );
        DrawRect(new Rect2(592, 472, 28, 40), new Color("#15546d"));
        DrawLine(new Vector2(598, 474), new Vector2(598, 511), new Color("#54c8c4"), 3);
        DrawLine(new Vector2(606, 474), new Vector2(606, 511), new Color("#85e7d8"), 2);
        DrawLine(new Vector2(614, 474), new Vector2(614, 511), new Color("#318ea1"), 3);

        // Layered cliff faces at the southern rim.
        for (var x = 16; x < 592; x += 24)
        {
            var top = 489 + (x / 24 % 2) * 4;
            DrawColoredPolygon(
                [
                    new Vector2(x, top),
                    new Vector2(x + 22, top - 2),
                    new Vector2(x + 20, 512),
                    new Vector2(x + 2, 512),
                ],
                new Color(x % 48 == 0 ? "#252d4b" : "#2d3554")
            );
            DrawLine(new Vector2(x + 2, top), new Vector2(x + 20, top - 2), new Color("#596080"), 1);
            DrawLine(new Vector2(x + 8, top + 5), new Vector2(x + 7, 510), new Color("#1b2944"), 1);
        }
    }
}

internal sealed partial class AmbientGlowField : Node2D
{
    private readonly GlowPoint[] _points =
    [
        new(new Vector2(107, 151), new Color("#f3ca78"), 13),
        new(new Vector2(149, 151), new Color("#f3ca78"), 13),
        new(new Vector2(151, 162), new Color("#f3ca78"), 14),
        new(new Vector2(338, 162), new Color("#f3ca78"), 14),
        new(new Vector2(535, 162), new Color("#f3ca78"), 14),
        new(new Vector2(608, 67), new Color("#72e0d2"), 20),
        new(new Vector2(54, 345), new Color("#72e0d2"), 15),
        new(new Vector2(568, 274), new Color("#b795dd"), 14),
        new(new Vector2(742, 320), new Color("#72e0d2"), 22),
        new(new Vector2(713, 415), new Color("#b795dd"), 15),
    ];
    private double _time;

    public AmbientGlowField()
    {
        ZIndex = 15;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        };
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
            var point = _points[index];
            var flicker = 0.9f + Mathf.Sin((float)_time * 2.2f + index * 1.7f) * 0.1f;
            DrawCircle(point.Position, point.Radius * flicker, new Color(point.Color, 0.035f));
            DrawCircle(point.Position, point.Radius * 0.58f * flicker, new Color(point.Color, 0.065f));
            DrawCircle(point.Position, point.Radius * 0.23f, new Color(point.Color, 0.14f));
            DrawCircle(point.Position, 1.2f, new Color(point.Color, 0.9f));
        }
    }

    private readonly record struct GlowPoint(Vector2 Position, Color Color, float Radius);
}

internal sealed partial class CropGlowLayer : Node2D
{
    private readonly FarmSystem _farm;
    private double _time;

    public CropGlowLayer(FarmSystem farm)
    {
        _farm = farm;
        ZIndex = 2;
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        };
        farm.TileChanged += OnTileChanged;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var index = 0;
        foreach (var tile in _farm.Tiles.Values)
        {
            if (string.IsNullOrWhiteSpace(tile.CropId))
            {
                continue;
            }

            var definition = DataCatalog.Crop(tile.CropId);
            if (!definition.IsMature(tile.WateredNights))
            {
                continue;
            }

            var center = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 7);
            var pulse = 0.86f + Mathf.Sin((float)_time * 3 + index) * 0.14f;
            var color = tile.CropId == DataCatalog.StarbudId
                ? new Color("#f3ca78")
                : new Color("#b795dd");
            DrawCircle(center, 8 * pulse, new Color(color, 0.045f));
            DrawCircle(center, 3.5f * pulse, new Color(color, 0.1f));
            var sparkle = center + new Vector2(
                Mathf.Sin((float)_time * 1.8f + index) * 6,
                -7 + Mathf.Cos((float)_time * 2.1f + index) * 2
            );
            DrawLine(sparkle + new Vector2(-2, 0), sparkle + new Vector2(2, 0), new Color(color, 0.7f), 1);
            DrawLine(sparkle + new Vector2(0, -2), sparkle + new Vector2(0, 2), new Color(color, 0.7f), 1);
            index++;
        }
    }

    public override void _ExitTree()
    {
        _farm.TileChanged -= OnTileChanged;
    }

    private void OnTileChanged(GridPosition position)
    {
        _ = position;
        QueueRedraw();
    }
}

internal sealed partial class ChimneySmoke : Node2D
{
    private double _time;

    public ChimneySmoke()
    {
        Position = new Vector2(170, 33);
        ZIndex = 8;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var index = 0; index < 4; index++)
        {
            var phase = Mathf.PosMod((float)_time * 7 + index * 7, 28);
            var position = new Vector2(
                Mathf.Sin((float)_time + index) * 2 + phase * 0.18f,
                -phase
            );
            DrawCircle(position, 2.5f + phase * 0.08f, new Color(0.55f, 0.63f, 0.78f, 0.18f - phase * 0.004f));
        }
    }
}
