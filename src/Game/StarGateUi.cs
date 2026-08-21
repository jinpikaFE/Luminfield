using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarGateOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _notice;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _destinationButtons =
        new(StringComparer.Ordinal);

    public StarGateOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.88f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 290)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fa"),
                ThemeFactory.Mint,
                2,
                12
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var icon = new TextureRect
        {
            Texture = StarGateArt.ProjectIconTexture(),
            CustomMinimumSize = new Vector2(68, 68),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        column.AddChild(icon);

        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _summary = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        _summary.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_summary);

        var grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 6);
        foreach (var destination in StarGateCatalog.Destinations)
        {
            var destinationId = destination.Id;
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(176, 26);
            button.Pressed += () => TravelRequested?.Invoke(destinationId);
            _destinationButtons[destinationId] = button;
            grid.AddChild(button);
        }
        column.AddChild(grid);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 22);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        locale.LocaleChanged += RefreshText;
        RefreshText();
        _destinationButtons[StarGateCatalog.HomesteadId]
            .CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action<string>? TravelRequested;
    public event Action? CloseRequested;

    public void ShowNotice(string messageKey)
    {
        _notice.Text = _locale.Tr(messageKey);
    }

    public void RefreshText()
    {
        _title.Text = _locale.Tr("star_gate.panel.title");
        _summary.Text = _locale.Tr(
            "star_gate.panel.summary",
            _session.StarGate.TravelCount
        );
        foreach (var destination in StarGateCatalog.Destinations)
        {
            _destinationButtons[destination.Id].Text =
                _locale.Tr(destination.NameKey);
        }
        _close.Text = _locale.Tr("menu.back");
    }

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= RefreshText;
    }
}
