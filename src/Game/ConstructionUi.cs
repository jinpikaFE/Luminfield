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
    private readonly VBoxContainer _materials;
    private readonly Label _duration;
    private readonly Label _notice;
    private readonly TextureRect _projectIcon;
    private readonly Button _action;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _projectButtons =
        new(StringComparer.Ordinal);
    private string _selectedProjectId;

    public ConstructionOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        string? initialProjectId = null
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _selectedProjectId = ConstructionCatalog.TryProject(
            initialProjectId,
            out _
        )
            ? initialProjectId!
            : session.Construction.ActiveProjectId ??
                ConstructionCatalog.Projects[0].Id;

        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(526, 338)
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
        column.AddThemeConstantOverride("separation", 2);
        panel.AddChild(column);

        _title = ThemeFactory.Label(
            size: 18,
            color: ThemeFactory.Mint
        );
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        var selector = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        selector.AddThemeConstantOverride("h_separation", 7);
        var group = new ButtonGroup();
        foreach (var project in ConstructionCatalog.Projects)
        {
            var projectId = project.Id;
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(142, 24);
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.Pressed += () => SelectProject(projectId);
            _projectButtons[projectId] = button;
            selector.AddChild(button);
        }
        var selectorScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(462, 53),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            FollowFocus = true
        };
        selectorScroll.AddChild(selector);
        column.AddChild(selectorScroll);

        _description = ThemeFactory.Label(size: 10);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.HorizontalAlignment = HorizontalAlignment.Center;
        _description.CustomMinimumSize = new Vector2(462, 22);
        column.AddChild(_description);

        var projectPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(472, 104)
        };
        projectPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f2"),
                ThemeFactory.Gold,
                1,
                7,
                8
            )
        );
        var detailRow = new HBoxContainer();
        detailRow.AddThemeConstantOverride("separation", 8);
        projectPanel.AddChild(detailRow);

        _projectIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(42, 42),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore
        };
        detailRow.AddChild(_projectIcon);

        var details = new VBoxContainer();
        details.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        details.AddThemeConstantOverride("separation", 3);
        detailRow.AddChild(details);

        _phase = ThemeFactory.Label(size: 14, color: ThemeFactory.Gold);
        _coins = ThemeFactory.Label(size: 10);
        _materials = new VBoxContainer();
        _materials.AddThemeConstantOverride("separation", 1);
        _duration = ThemeFactory.Label(
            size: 10,
            color: ThemeFactory.MutedInk
        );
        details.AddChild(_phase);
        details.AddChild(_coins);
        details.AddChild(_materials);
        details.AddChild(_duration);
        column.AddChild(projectPanel);

        _action = ThemeFactory.Button("");
        _action.CustomMinimumSize = new Vector2(330, 23);
        _action.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _action.Pressed += Execute;
        column.AddChild(_action);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(462, 10);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 20);
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
        var project = ConstructionCatalog.Project(_selectedProjectId);
        var construction = _session.Construction;
        var phase = construction.PhaseFor(project.Id);

        _title.Text = _locale.Tr("construction.panel.title");
        foreach (var definition in ConstructionCatalog.Projects)
        {
            var button = _projectButtons[definition.Id];
            button.Text = _locale.Tr(definition.NameKey);
            button.ButtonPressed = definition.Id == _selectedProjectId;
        }

        _description.Text = _locale.Tr(project.DescriptionKey);
        _coins.Text = _locale.Tr(
            "construction.cost.coins",
            project.CoinCost,
            _session.Coins
        );
        RebuildMaterialRows(project);
        _close.Text = _locale.Tr("menu.back");
        _projectIcon.Texture = project.Id switch
        {
            ConstructionCatalog.CottageSecondUpgradeId =>
                CottageKitchenArt.ProjectIconTexture(),
            ConstructionCatalog.HomesteadWorkshopProjectId =>
                HomesteadWorkshopArt.ProjectIconTexture(),
            ConstructionCatalog.HomesteadGreenhouseProjectId =>
                HomesteadGreenhouseArt.ProjectIconTexture(),
            ConstructionCatalog.HomesteadStarfeatherCoopProjectId =>
                HomesteadStarfeatherCoopArt.ProjectIconTexture(),
            ConstructionCatalog.HomesteadMoonfleeceBarnProjectId =>
                HomesteadMoonfleeceBarnArt.ProjectIconTexture(),
            ConstructionCatalog.HomesteadLivestockAutomationProjectId =>
                LivestockAutomationArt.ProjectIconTexture(),
            _ => null
        };
        _projectIcon.Visible = _projectIcon.Texture is not null;

        if (phase == ConstructionPhase.Completed)
        {
            _phase.Text = _locale.Tr("construction.state.completed");
            _duration.Text = _locale.Tr("construction.completed.detail");
            _action.Text = _locale.Tr("construction.action.completed");
            _action.Disabled = true;
            return;
        }

        if (phase == ConstructionPhase.InProgress)
        {
            _phase.Text = _locale.Tr("construction.state.in_progress");
            _duration.Text = _locale.Tr(
                "construction.remaining_nights",
                construction.RemainingNightsFor(project.Id)
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

    private void SelectProject(string projectId)
    {
        _selectedProjectId = projectId;
        _notice.Text = string.Empty;
        RefreshText();
        _action.GrabFocus();
    }

    private void RebuildMaterialRows(
        ConstructionProjectDefinition project
    )
    {
        foreach (var child in _materials.GetChildren())
        {
            _materials.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var material in project.Materials)
        {
            var label = ThemeFactory.Label(size: 10);
            label.Text = _locale.Tr(
                "construction.cost.material",
                _locale.Tr(DataCatalog.Item(material.ItemId).NameKey),
                material.Count,
                _session.Inventory.Count(material.ItemId)
            );
            _materials.AddChild(label);
        }
    }

    private void Execute()
    {
        var result = _session.StartConstruction(_selectedProjectId);
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ConstructionChanged?.Invoke();
        }
        RefreshText();
    }
}
