using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal sealed partial class Story01WorldResponseVisual : Node2D
{
    private static readonly Color Mint = new("#8ee6be");
    private static readonly Color Gold = new("#f3ca78");
    private static readonly Color Violet = new("#b795dd");
    private static readonly Color Rose = new("#ef7fa8");

    private readonly GameSession _session;
    private double _time;

    public Story01WorldResponseVisual(GameSession session)
    {
        _session = session;
        Name = "Story01WorldResponseVisual";
        ZIndex = 18;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var pulse = 0.5f + 0.5f * Mathf.Sin((float)_time * 3.2f);
        switch (_session.StarlightStory.ActiveBeatId)
        {
            case StarlightStoryCatalog.WoodlandResponseId:
                DrawWoodland(CellCenter(WorldDefinition.WoodlandStarlightCell), pulse);
                break;
            case StarlightStoryCatalog.HomesteadResponseId:
                DrawHomestead(
                    CellCenter(FarmLayout.HomesteadStoryResponseCell),
                    pulse
                );
                break;
            case StarlightStoryCatalog.MeadowResponseId:
                DrawMeadow(CellCenter(WorldDefinition.MeadowStarlightCell), pulse);
                break;
            case StarlightStoryCatalog.MoonwaterResponseId:
                DrawMoonwater(CellCenter(WorldDefinition.MoonwaterStarlightCell), pulse);
                break;
            case StarlightStoryCatalog.CrystalValeResponseId:
                DrawCrystalGate(
                    CellCenter(StarfallRuinsTrialLayout.WorldEntryCell),
                    pulse
                );
                break;
            case StarlightStoryCatalog.StarfallRuinsResponseId:
                DrawSixfoldGate(CellCenter(FarmLayout.StarGateCell), pulse);
                break;
        }
    }

    private void DrawWoodland(Vector2 center, float pulse)
    {
        DrawCircle(center, 31 + pulse * 5, new Color(Mint, 0.035f));
        foreach (var tip in new[]
                 {
                     new Vector2(-31, 12),
                     new Vector2(-19, 25),
                     new Vector2(20, 24),
                     new Vector2(32, 8)
                 })
        {
            var elbow = new Vector2(tip.X * 0.48f, tip.Y * 0.35f);
            DrawPolyline(
                [center, center + elbow, center + tip],
                new Color(Mint, 0.72f),
                1.5f
            );
            DrawLine(
                center + tip,
                center + tip + new Vector2(-3, -7 - pulse * 2),
                new Color(Gold, 0.9f),
                1.4f
            );
            DrawLine(
                center + tip + new Vector2(-3, -5),
                center + tip + new Vector2(-8, -8),
                new Color(Mint, 0.85f),
                1.2f
            );
            DrawLine(
                center + tip + new Vector2(-2, -4),
                center + tip + new Vector2(4, -8),
                new Color(Mint, 0.85f),
                1.2f
            );
        }
    }

    private void DrawHomestead(Vector2 center, float pulse)
    {
        DrawCircle(center, 25 + pulse * 3, new Color(Mint, 0.055f));
        foreach (var offset in new[]
                 {
                     new Vector2(0, -22), new Vector2(16, -16),
                     new Vector2(22, 0), new Vector2(16, 16),
                     new Vector2(0, 22), new Vector2(-16, 16),
                     new Vector2(-22, 0), new Vector2(-16, -16)
                 })
        {
            var point = center + offset;
            DrawLine(center, point, new Color(Mint, 0.62f), 1.2f);
            DrawCircle(point, 2.2f + pulse, new Color(Gold, 0.9f));
            DrawArc(
                point,
                4.5f + pulse,
                0,
                Mathf.Tau,
                12,
                new Color(Mint, 0.6f),
                1
            );
        }
        DrawCircle(center, 4 + pulse, new Color(Mint, 0.85f));
    }

    private void DrawMeadow(Vector2 center, float pulse)
    {
        var points = new[]
        {
            center + new Vector2(-35, 12),
            center + new Vector2(-16, -18),
            center + new Vector2(5, 15),
            center + new Vector2(27, -11),
            center + new Vector2(39, 13)
        };
        DrawPolyline(points, new Color(Gold, 0.74f), 1.4f);
        foreach (var point in points)
        {
            DrawCircle(point, 4 + pulse, new Color(Mint, 0.18f));
            DrawCircle(point, 1.8f + pulse * 0.4f, Violet);
        }
        var mote = center + new Vector2(
            Mathf.Lerp(-34, 38, pulse),
            -25 - pulse * 5
        );
        DrawCircle(mote, 2.2f, Gold);
        DrawLine(mote - new Vector2(4, 1), mote + new Vector2(4, 1), Mint, 1);
    }

    private void DrawMoonwater(Vector2 center, float pulse)
    {
        DrawCircle(center + new Vector2(0, -23), 8 + pulse * 2, new Color(Mint, 0.2f));
        DrawCircle(center + new Vector2(0, -23), 2.5f, Gold);
        var water = center + new Vector2(0, 12);
        DrawArc(water, 18 + pulse * 5, 0, Mathf.Tau, 28, new Color(Mint, 0.72f), 1.4f);
        DrawArc(water, 29 + pulse * 6, 0.2f, 5.9f, 32, new Color(Violet, 0.5f), 1.2f);
        var fish = new[]
        {
            water + new Vector2(-20, 0),
            water + new Vector2(-9, -7),
            water + new Vector2(8, -6),
            water + new Vector2(20, 0),
            water + new Vector2(8, 6),
            water + new Vector2(-9, 7),
            water + new Vector2(-20, 0),
            water + new Vector2(-29, -8),
            water + new Vector2(-29, 8),
            water + new Vector2(-20, 0)
        };
        DrawPolyline(fish, new Color(Gold, 0.9f), 1.5f);
        DrawCircle(water + new Vector2(12, -2), 1.5f, Mint);
    }

    private void DrawCrystalGate(Vector2 center, float pulse)
    {
        DrawCircle(center, 34 + pulse * 4, new Color(Violet, 0.04f));
        DrawPolyline(
            [
                center + new Vector2(-25, 18),
                center + new Vector2(-25, -18),
                center + new Vector2(-12, -31),
                center + new Vector2(0, -37)
            ],
            new Color(Mint, 0.8f),
            1.7f
        );
        DrawPolyline(
            [
                center + new Vector2(25, 18),
                center + new Vector2(25, -18),
                center + new Vector2(12, -31),
                center + new Vector2(3 + pulse * 3, -37)
            ],
            new Color(Gold, 0.86f),
            1.7f
        );
        foreach (var offset in new[]
                 {
                     new Vector2(-32, -9), new Vector2(31, -17),
                     new Vector2(-18, -34), new Vector2(20, -38)
                 })
        {
            DrawCircle(center + offset, 2 + pulse, Violet);
        }
    }

    private void DrawSixfoldGate(Vector2 center, float pulse)
    {
        var colors = new[] { Mint, Gold, Violet, Rose, Mint, Gold };
        for (var index = 0; index < colors.Length; index++)
        {
            var angle = -Mathf.Pi * 0.92f + index * Mathf.Pi * 0.184f;
            var foot = center + new Vector2((index - 2.5f) * 7, 18);
            var crown = center + new Vector2(
                Mathf.Cos(angle) * (34 + pulse * 3),
                -20 + Mathf.Sin(angle) * 18
            );
            DrawLine(foot, crown, new Color(colors[index], 0.82f), 1.6f);
            DrawCircle(crown, 2.1f + pulse * 0.7f, colors[index]);
        }
        DrawArc(
            center + new Vector2(0, 15),
            31 + pulse * 2,
            Mathf.Pi,
            Mathf.Tau,
            26,
            new Color(Gold, 0.72f),
            1.6f
        );
        DrawCircle(center + new Vector2(0, 15), 38, new Color(Mint, 0.035f));
    }

    private static Vector2 CellCenter(GridPosition cell) =>
        new(cell.X * 16 + 8, cell.Y * 16 + 8);
}
