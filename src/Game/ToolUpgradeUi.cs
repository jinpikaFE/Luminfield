using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class ToolUpgradeOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _benchTarget;
    private readonly ToolUpgradeDefinition _upgrade;
    private readonly Label _title;
    private readonly Label _tier;
    private readonly Label _cost;
    private readonly Label _materials;
    private readonly Label _duration;
    private readonly Label _status;
    private readonly Button _start;
    private readonly Button _close;

    public ToolUpgradeOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition benchTarget
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _benchTarget = benchTarget;
        _upgrade = ToolProgressionCatalog.ShovelBronzeStarUpgrade;

        AddChild(Dim(new Color(0.01f, 0.018f, 0.07f, 0.84f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(492, 274)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0c1938fa"),
                ThemeFactory.Mint,
                2,
                9
            )
        );
        center.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 7);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        header.AddChild(new TextureRect
        {
            Texture = new AtlasTexture
            {
                Atlas = CrystalGrottoArt.Atlas,
                Region = CrystalGrottoArt.BronzeStarShovelRegion,
                FilterClip = true
            },
            CustomMinimumSize = new Vector2(54, 54),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        });
        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _tier = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        headerText.AddChild(_title);
        headerText.AddChild(_tier);
        header.AddChild(headerText);
        root.AddChild(header);
        root.AddChild(new HSeparator());

        _cost = ThemeFactory.Label(size: 11);
        _materials = ThemeFactory.Label(size: 10);
        _materials.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _materials.CustomMinimumSize = new Vector2(452, 42);
        _duration = ThemeFactory.Label(size: 10, color: ThemeFactory.MutedInk);
        root.AddChild(_cost);
        root.AddChild(_materials);
        root.AddChild(_duration);

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(452, 24);
        root.AddChild(_status);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 10);
        _start = ThemeFactory.Button("");
        _start.CustomMinimumSize = new Vector2(210, 26);
        _start.Pressed += StartUpgrade;
        actions.AddChild(_start);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 26);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);
        root.AddChild(actions);

        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
        _start.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? UpgradeStarted;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        var currentTier = ToolProgressionCatalog.Tier(
            _session.ToolProgression.TierIdFor(_upgrade.ToolId)
        );
        _title.Text = _locale.Tr("tool.upgrade.panel.title");
        _tier.Text = _locale.Tr(
            "tool.upgrade.panel.current_tier",
            _locale.Tr(currentTier.NameKey),
            _locale.Tr(_upgrade.NameKey)
        );
        _cost.Text = _locale.Tr(
            "tool.upgrade.panel.coin_cost",
            _upgrade.CoinCost,
            _session.Coins
        );
        _materials.Text = string.Join(
            "\n",
            _upgrade.Materials.Select(material => _locale.Tr(
                "tool.upgrade.panel.material_line",
                _locale.Tr(DataCatalog.Item(material.ItemId).NameKey),
                material.Count,
                _session.Inventory.CountFamily(material.ItemId)
            ))
        );
        _duration.Text = _locale.Tr(
            "tool.upgrade.panel.duration",
            _upgrade.RequiredNights
        );
        _close.Text = _locale.Tr("menu.back");

        if (_session.ToolProgression.IsUpgradeCompleted(_upgrade.Id))
        {
            _start.Disabled = true;
            _start.Text = _locale.Tr("tool.upgrade.panel.completed");
            _status.Text = _locale.Tr("tool.upgrade.completed");
            return;
        }

        if (_session.ToolProgression.IsUpgradeInProgress)
        {
            _start.Disabled = true;
            _start.Text = _locale.Tr(
                "tool.upgrade.panel.in_progress",
                _session.ToolProgression.RemainingNights
            );
            _status.Text = _locale.Tr("tool.upgrade.in_progress");
            return;
        }

        var check = _session.CheckStartToolUpgrade(
            _benchTarget,
            _upgrade.Id
        );
        _start.Disabled = !check.Succeeded;
        _start.Text = _locale.Tr("tool.upgrade.action.start");
        _status.Text = _locale.Tr(check.MessageKey);
    }

    private void StartUpgrade()
    {
        var result = _session.StartToolUpgrade(_benchTarget, _upgrade.Id);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            UpgradeStarted?.Invoke();
        }
        Refresh();
    }
}
