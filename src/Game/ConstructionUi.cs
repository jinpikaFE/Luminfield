using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class ConstructionOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _description;
    private readonly Label _phase;
    private readonly Label _coins;
    private readonly Label _lumenwood;
    private readonly Label _crystal;
    private readonly Label _duration;
    private readonly Label _notice;
    private readonly Button _action;
    private readonly Button _close;

    public ConstructionOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(486, 326)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fa"),
                ThemeFactory.Mint,
                2,
                9
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _description = ThemeFactory.Label(size: 10);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.HorizontalAlignment = HorizontalAlignment.Center;
        _description.CustomMinimumSize = new Vector2(430, 42);
        column.AddChild(_description);

        var project = new PanelContainer
        {
            CustomMinimumSize = new Vector2(438, 128)
        };
        project.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f2"),
                ThemeFactory.Gold,
                1,
                7,
                8
            )
        );
        var details = new VBoxContainer();
        details.AddThemeConstantOverride("separation", 4);
        project.AddChild(details);
        _phase = ThemeFactory.Label(size: 15, color: ThemeFactory.Gold);
        _coins = ThemeFactory.Label(size: 10);
        _lumenwood = ThemeFactory.Label(size: 10);
        _crystal = ThemeFactory.Label(size: 10);
        _duration = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        details.AddChild(_phase);
        details.AddChild(_coins);
        details.AddChild(_lumenwood);
        details.AddChild(_crystal);
        details.AddChild(_duration);
        column.AddChild(project);

        _action = ThemeFactory.Button("");
        _action.CustomMinimumSize = new Vector2(318, 30);
        _action.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _action.Pressed += Execute;
        column.AddChild(_action);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(430, 15);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _action.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? ConstructionChanged;

    public void RefreshText()
    {
        var construction = _session.Construction;
        var project = construction.Project;
        var lumenwood = project.Materials.Single(
            material => material.ItemId == DataCatalog.LumenwoodId
        );
        var crystal = project.Materials.Single(
            material => material.ItemId == DataCatalog.CrystalShardId
        );

        _title.Text = _locale.Tr("construction.panel.title");
        _description.Text = _locale.Tr(project.DescriptionKey);
        _coins.Text = _locale.Tr(
            "construction.cost.coins",
            project.CoinCost,
            _session.Coins
        );
        _lumenwood.Text = _locale.Tr(
            "construction.cost.material",
            _locale.Tr(DataCatalog.Item(lumenwood.ItemId).NameKey),
            lumenwood.Count,
            _session.Inventory.Count(lumenwood.ItemId)
        );
        _crystal.Text = _locale.Tr(
            "construction.cost.material",
            _locale.Tr(DataCatalog.Item(crystal.ItemId).NameKey),
            crystal.Count,
            _session.Inventory.Count(crystal.ItemId)
        );
        _close.Text = _locale.Tr("menu.back");

        if (construction.IsCompleted)
        {
            _phase.Text = _locale.Tr("construction.state.completed");
            _duration.Text = _locale.Tr("construction.completed.detail");
            _action.Text = _locale.Tr("construction.action.completed");
            _action.Disabled = true;
            return;
        }

        if (construction.IsInProgress)
        {
            _phase.Text = _locale.Tr("construction.state.in_progress");
            _duration.Text = _locale.Tr(
                "construction.remaining_nights",
                construction.RemainingNights
            );
            _action.Text = _locale.Tr("construction.action.in_progress");
            _action.Disabled = true;
            return;
        }

        _phase.Text = _locale.Tr(project.NameKey);
        _duration.Text = _locale.Tr(
            "construction.duration",
            project.RequiredNights
        );
        _action.Text = _locale.Tr("construction.action.start");
        _action.Disabled = false;
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void Execute()
    {
        var result = _session.StartCottageFirstUpgrade();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ConstructionChanged?.Invoke();
        }
        RefreshText();
    }
}
