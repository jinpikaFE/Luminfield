using Godot;

namespace Luminfield.Game;

public static class ThemeFactory
{
    public static readonly Color Ink = new("#f7f0d9");
    public static readonly Color MutedInk = new("#aeb9cc");
    public static readonly Color DeepIndigo = new("#08142e");
    public static readonly Color Panel = new("#0b1834ed");
    public static readonly Color PanelLight = new("#17294bef");
    public static readonly Color PanelEdge = new("#4e7d88");
    public static readonly Color Mint = new("#8ee6be");
    public static readonly Color Teal = new("#4bc5bd");
    public static readonly Color Gold = new("#f3ca78");
    public static readonly Color Violet = new("#7f75c8");
    public static readonly Color Soil = new("#6b4f5f");

    public static Theme CreateTheme()
    {
        var font = GD.Load<FontFile>("res://assets/fonts/NotoSansCJKsc-Regular.otf");
        var theme = new Theme
        {
            DefaultFont = font,
            DefaultFontSize = 13
        };

        theme.SetColor("font_color", "Label", Ink);
        theme.SetColor("font_shadow_color", "Label", new Color(0, 0, 0, 0.5f));
        theme.SetConstant("shadow_offset_x", "Label", 1);
        theme.SetConstant("shadow_offset_y", "Label", 1);

        theme.SetStylebox("normal", "Button", Box(PanelLight, Teal, 1, 5));
        theme.SetStylebox("hover", "Button", Box(new Color("#32466b"), Mint, 2, 5));
        theme.SetStylebox("pressed", "Button", Box(new Color("#122f46"), Gold, 2, 5));
        theme.SetStylebox("disabled", "Button", Box(new Color("#1a2034"), new Color("#3b4358"), 1, 5));
        theme.SetColor("font_color", "Button", Ink);
        theme.SetColor("font_hover_color", "Button", Mint);
        theme.SetColor("font_pressed_color", "Button", Gold);
        theme.SetColor("font_disabled_color", "Button", new Color("#697087"));
        theme.SetConstant("outline_size", "Button", 0);

        theme.SetStylebox("panel", "Panel", Box(Panel, PanelEdge, 1, 6));
        theme.SetStylebox("panel", "PanelContainer", Box(Panel, PanelEdge, 1, 6));
        theme.SetStylebox("background", "ProgressBar", Box(new Color("#151c35"), new Color("#34415f"), 1, 4));
        theme.SetStylebox("fill", "ProgressBar", Box(new Color("#4bc5bd"), Mint, 1, 4));
        theme.SetColor("font_color", "ProgressBar", DeepIndigo);

        return theme;
    }

    public static StyleBoxFlat Box(Color background, Color border, int borderWidth, int radius)
    {
        var pixelRadius = Math.Min(radius, 3);
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = pixelRadius,
            CornerRadiusTopRight = pixelRadius,
            CornerRadiusBottomLeft = pixelRadius,
            CornerRadiusBottomRight = pixelRadius,
            CornerDetail = 1,
            AntiAliasing = false,
            ShadowColor = new Color(0, 0, 0, 0.42f),
            ShadowSize = 1,
            ShadowOffset = new Vector2(1, 1),
            ContentMarginLeft = 10,
            ContentMarginTop = 7,
            ContentMarginRight = 10,
            ContentMarginBottom = 7
        };
    }

    public static StyleBoxFlat CompactBox(
        Color background,
        Color border,
        int borderWidth,
        int radius,
        float margin = 3
    )
    {
        var box = Box(background, border, borderWidth, radius);
        box.ContentMarginLeft = margin;
        box.ContentMarginTop = margin;
        box.ContentMarginRight = margin;
        box.ContentMarginBottom = margin;
        return box;
    }

    public static Label Label(string text = "", int size = 13, Color? color = null)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Ink);
        return label;
    }

    public static Button Button(string text)
    {
        return new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(220, 34),
            FocusMode = Control.FocusModeEnum.All
        };
    }
}
