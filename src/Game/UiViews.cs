using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public abstract partial class FullScreenUi : Control
{
    protected FullScreenUi(Theme theme)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Theme = theme;
        MouseFilter = MouseFilterEnum.Stop;
    }

    protected static ColorRect Dim(Color color)
    {
        var dim = new ColorRect
        {
            Color = color,
            MouseFilter = MouseFilterEnum.Stop
        };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return dim;
    }
}

public sealed partial class TitleMenu : FullScreenUi
{
    private readonly LocaleService _locale;
    private readonly SaveService _saveService;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Button _newGame;
    private readonly Button _continue;
    private readonly Button _settings;
    private readonly Button _quit;
    private readonly Label _notice;

    public TitleMenu(
        Theme theme,
        LocaleService locale,
        SaveService saveService,
        string? notice = null
    ) : base(theme)
    {
        _locale = locale;
        _saveService = saveService;
        AddChild(new TitleBackdrop());

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(336, 282) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#101a3aee"), ThemeFactory.Teal, 2, 10)
        );
        center.AddChild(panel);

        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 9);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 32, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);

        _subtitle = ThemeFactory.Label(size: 14, color: ThemeFactory.Gold);
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_subtitle);
        column.AddChild(new HSeparator());

        _newGame = ThemeFactory.Button("");
        _continue = ThemeFactory.Button("");
        _settings = ThemeFactory.Button("");
        _quit = ThemeFactory.Button("");
        column.AddChild(_newGame);
        column.AddChild(_continue);
        column.AddChild(_settings);
        column.AddChild(_quit);

        _notice = ThemeFactory.Label(notice ?? "", 11, ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _notice.CustomMinimumSize = new Vector2(280, 34);
        column.AddChild(_notice);

        _newGame.Pressed += () => NewGameRequested?.Invoke();
        _continue.Pressed += () => ContinueRequested?.Invoke();
        _settings.Pressed += () => LanguageRequested?.Invoke();
        _quit.Pressed += () => QuitRequested?.Invoke();

        RefreshText();
        _newGame.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? NewGameRequested;
    public event Action? ContinueRequested;
    public event Action? LanguageRequested;
    public event Action? QuitRequested;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("game.title");
        _subtitle.Text = _locale.Tr("game.subtitle");
        _newGame.Text = _locale.Tr("menu.new_game");
        _continue.Text = _locale.Tr("menu.continue");
        _continue.Disabled = !_saveService.Exists;
        _continue.TooltipText = _continue.Disabled ? _locale.Tr("menu.no_save") : string.Empty;
        _settings.Text = _locale.Tr("menu.settings");
        _quit.Text = _locale.Tr("menu.quit");
    }
}

public sealed partial class HudView : Control
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _day;
    private readonly Label _time;
    private readonly Label _objective;
    private readonly ProgressBar _energy;
    private readonly Label _energyText;
    private readonly Label _selected;
    private readonly Label _controls;
    private readonly PanelContainer _noticePanel;
    private readonly Label _notice;
    private readonly List<PanelContainer> _slots = [];
    private readonly List<HotbarSlotContent> _slotContents = [];
    private double _noticeRemaining;

    public HudView(Theme theme, GameSession session, LocaleService locale)
    {
        Theme = theme;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _session = session;
        _locale = locale;
        AddChild(new HudChrome());

        var clockPanel = PanelAt(new Vector2(8, 8), new Vector2(94, 46));
        clockPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#151d37ee"),
                ThemeFactory.Violet,
                1,
                7,
                6
            )
        );
        var clockColumn = new VBoxContainer();
        clockColumn.AddThemeConstantOverride("separation", 0);
        clockPanel.AddChild(clockColumn);
        _day = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _time = ThemeFactory.Label(size: 16, color: ThemeFactory.Mint);
        clockColumn.AddChild(_day);
        clockColumn.AddChild(_time);

        var objectivePanel = PanelAt(new Vector2(110, 8), new Vector2(330, 46));
        objectivePanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#17253def"),
                ThemeFactory.Mint,
                2,
                7,
                7
            )
        );
        _objective = ThemeFactory.Label(size: 11);
        _objective.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _objective.VerticalAlignment = VerticalAlignment.Center;
        objectivePanel.AddChild(_objective);

        var energyPanel = PanelAt(new Vector2(448, 8), new Vector2(184, 46));
        energyPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#151d37ee"),
                ThemeFactory.PanelEdge,
                1,
                7,
                6
            )
        );
        var energyColumn = new VBoxContainer();
        energyColumn.AddThemeConstantOverride("separation", 2);
        energyPanel.AddChild(energyColumn);
        _energyText = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _energy = new ProgressBar
        {
            MinValue = 0,
            MaxValue = GameSession.MaxEnergy,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(148, 10)
        };
        energyColumn.AddChild(_energyText);
        energyColumn.AddChild(_energy);

        var hotbar = new HBoxContainer
        {
            Position = new Vector2(146, 309),
            Size = new Vector2(348, 40),
            MouseFilter = MouseFilterEnum.Ignore
        };
        hotbar.AddThemeConstantOverride("separation", 4);
        AddChild(hotbar);
        for (var index = 0; index < Inventory.SlotCount; index++)
        {
            var slot = new PanelContainer
            {
                CustomMinimumSize = new Vector2(40, 40),
                MouseFilter = MouseFilterEnum.Ignore
            };
            var content = new HotbarSlotContent();
            slot.AddChild(content);
            hotbar.AddChild(slot);
            _slots.Add(slot);
            _slotContents.Add(content);
        }

        var selectedPanel = PanelAt(new Vector2(204, 283), new Vector2(232, 20));
        selectedPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#111a30dd"),
                new Color("#765f75"),
                1,
                6,
                3
            )
        );
        _selected = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _selected.HorizontalAlignment = HorizontalAlignment.Center;
        _selected.VerticalAlignment = VerticalAlignment.Center;
        selectedPanel.AddChild(_selected);

        _controls = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _controls.Position = new Vector2(9, 349);
        _controls.Size = new Vector2(622, 10);
        _controls.HorizontalAlignment = HorizontalAlignment.Center;
        _controls.Modulate = new Color(1, 1, 1, 0.72f);
        AddChild(_controls);

        _noticePanel = PanelAt(new Vector2(183, 244), new Vector2(274, 32));
        _noticePanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#241f38f3"),
                ThemeFactory.Gold,
                1,
                7,
                5
            )
        );
        _noticePanel.Visible = false;
        _notice = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.VerticalAlignment = VerticalAlignment.Center;
        _noticePanel.AddChild(_notice);

        session.Changed += Refresh;
        session.EnergyChanged += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_noticeRemaining <= 0)
        {
            return;
        }

        _noticeRemaining -= delta;
        if (_noticeRemaining <= 0)
        {
            _noticePanel.Visible = false;
        }
    }

    public void ShowNotice(string key, double seconds = 2.2)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _notice.Text = _locale.Tr(key);
        _noticePanel.Visible = true;
        _noticeRemaining = seconds;
    }

    public void Refresh()
    {
        _day.Text = _locale.Tr("hud.day", _session.Clock.Day);
        _time.Text = _locale.Tr("hud.time", _session.Clock.DisplayTime);
        _energy.Value = _session.Energy;
        _energyText.Text = _locale.Tr("hud.energy", _session.Energy, GameSession.MaxEnergy);
        _objective.Text = $"✦ {_locale.Tr(
            _session.Quest.ObjectiveKey,
            _session.Quest.ObjectiveCount
        )}";
        _controls.Text = _locale.Tr("hud.controls");

        for (var index = 0; index < _slots.Count; index++)
        {
            var slot = _session.Inventory.Slots[index];
            var selected = index == _session.Inventory.SelectedIndex;
            _slots[index].AddThemeStyleboxOverride(
                "panel",
                ThemeFactory.CompactBox(
                    selected ? new Color("#2f3d59f5") : new Color("#111a30e8"),
                    selected ? ThemeFactory.Gold : new Color("#466477"),
                    selected ? 2 : 1,
                    7,
                    2
                )
            );

            _slotContents[index].SetState(
                slot.IsEmpty ? string.Empty : slot.ItemId,
                slot.IsEmpty ? 0 : slot.Count,
                index + 1,
                selected
            );
        }

        var selectedSlot = _session.Inventory.Selected;
        _selected.Text = selectedSlot.IsEmpty
            ? _locale.Tr("hud.selected", "—")
            : _locale.Tr("hud.selected", _locale.Tr(DataCatalog.Item(selectedSlot.ItemId).NameKey));
    }

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _session.EnergyChanged -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private PanelContainer PanelAt(Vector2 position, Vector2 size)
    {
        var panel = new PanelContainer
        {
            Position = position,
            Size = size,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(panel);
        return panel;
    }

}

internal sealed partial class HudChrome : Control
{
    public HudChrome()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        DrawStyleBox(
            ThemeFactory.CompactBox(
                new Color("#07132be8"),
                new Color("#927857"),
                1,
                2,
                4
            ),
            new Rect2(138, 304, 364, 50)
        );
        DrawLine(new Vector2(144, 307), new Vector2(496, 307), ThemeFactory.Mint, 1);
        DrawLine(new Vector2(144, 352), new Vector2(496, 352), new Color("#3b6972"), 1);

        DrawColoredPolygon(
            [new Vector2(132, 329), new Vector2(138, 323), new Vector2(138, 335)],
            new Color("#927857")
        );
        DrawColoredPolygon(
            [new Vector2(508, 329), new Vector2(502, 323), new Vector2(502, 335)],
            new Color("#927857")
        );

        foreach (var x in new[] { 18f, 106f, 444f, 626f })
        {
            DrawCircle(new Vector2(x, 5), 2, new Color("#f3ca78"));
            DrawCircle(new Vector2(x + 4, 5), 1, new Color("#8ee6be"));
        }
    }
}

internal sealed partial class HotbarSlotContent : Control
{
    private static readonly Texture2D ItemIcons =
        GD.Load<Texture2D>("res://assets/generated/item_icons_final.png");

    private readonly Label _key;
    private readonly Label _count;
    private string _itemId = string.Empty;
    private bool _selected;

    public HotbarSlotContent()
    {
        CustomMinimumSize = new Vector2(34, 34);
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

        _key = ThemeFactory.Label(size: 8, color: ThemeFactory.MutedInk);
        _key.Position = new Vector2(1, 0);
        _key.Size = new Vector2(12, 11);
        AddChild(_key);

        _count = ThemeFactory.Label(size: 8, color: ThemeFactory.Ink);
        _count.Position = new Vector2(16, 27);
        _count.Size = new Vector2(18, 10);
        _count.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_count);
    }

    public void SetState(string itemId, int count, int key, bool selected)
    {
        _itemId = itemId;
        _selected = selected;
        _key.Text = key.ToString();
        _key.AddThemeColorOverride(
            "font_color",
            selected ? ThemeFactory.Gold : ThemeFactory.MutedInk
        );
        _count.Text = count > 1 ? count.ToString() : string.Empty;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (string.IsNullOrWhiteSpace(_itemId))
        {
            DrawRect(new Rect2(18, 20, 2, 2), new Color(0.45f, 0.55f, 0.65f, 0.38f));
            return;
        }

        if (_selected)
        {
            DrawRect(new Rect2(5, 6, 30, 29), new Color(0.95f, 0.79f, 0.46f, 0.08f));
        }

        if (!TryGetIconRegion(_itemId, out var source))
        {
            DrawColoredPolygon(
                [new Vector2(20, 9), new Vector2(30, 20), new Vector2(20, 31), new Vector2(10, 20)],
                ThemeFactory.Violet
            );
            return;
        }

        const float iconSize = 28;
        var scale = Math.Min(iconSize / source.Size.X, iconSize / source.Size.Y);
        var destinationSize = source.Size * scale;
        var destination = new Rect2(
            new Vector2(
                20 - destinationSize.X / 2,
                20 - destinationSize.Y / 2
            ),
            destinationSize
        );
        DrawTextureRectRegion(ItemIcons, destination, source);
    }

    private static bool TryGetIconRegion(string itemId, out Rect2 region)
    {
        region = itemId switch
        {
            DataCatalog.HoeId => new Rect2(85, 185, 290, 360),
            DataCatalog.WateringCanId => new Rect2(438, 220, 368, 320),
            DataCatalog.StarbudSeedId => new Rect2(867, 220, 341, 327),
            DataCatalog.MoonrootSeedId => new Rect2(45, 690, 340, 340),
            DataCatalog.StarbudId => new Rect2(465, 650, 315, 420),
            DataCatalog.MoonrootId => new Rect2(865, 635, 335, 450),
            _ => default
        };
        return region.Size != Vector2.Zero;
    }
}

public sealed partial class DialogueOverlay : FullScreenUi
{
    private readonly Label _speaker;
    private readonly Label _body;
    private readonly Label _continue;
    private Action? _closed;
    private double _inputDelay = 0.15;

    public DialogueOverlay(Theme theme, LocaleService locale) : base(theme)
    {
        MouseFilter = MouseFilterEnum.Stop;
        var shade = Dim(new Color(0.02f, 0.03f, 0.1f, 0.24f));
        AddChild(shade);

        var panel = new PanelContainer
        {
            Position = new Vector2(38, 238),
            Size = new Vector2(564, 104)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#101a3af5"), ThemeFactory.Mint, 2, 7)
        );
        AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);
        _speaker = ThemeFactory.Label(size: 15, color: ThemeFactory.Gold);
        _body = ThemeFactory.Label(size: 12);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.CustomMinimumSize = new Vector2(525, 48);
        _continue = ThemeFactory.Label(locale.Tr("dialogue.continue"), 9, ThemeFactory.MutedInk);
        _continue.HorizontalAlignment = HorizontalAlignment.Right;
        column.AddChild(_speaker);
        column.AddChild(_body);
        column.AddChild(_continue);
    }

    public void ShowDialogue(string speaker, string body, Action closed)
    {
        _speaker.Text = speaker;
        _body.Text = body;
        _closed = closed;
    }

    public override void _Process(double delta)
    {
        _inputDelay = Math.Max(0, _inputDelay - delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_inputDelay > 0 || !@event.IsActionPressed(InputSetup.Interact))
        {
            return;
        }

        var callback = _closed;
        _closed = null;
        callback?.Invoke();
        QueueFree();
        GetViewport().SetInputAsHandled();
    }
}

public sealed partial class PauseOverlay : FullScreenUi
{
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Button _resume;
    private readonly Button _language;
    private readonly Button _saveQuit;

    public PauseOverlay(Theme theme, LocaleService locale) : base(theme)
    {
        _locale = locale;
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.7f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(300, 220) };
        center.AddChild(panel);
        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 12);
        panel.AddChild(column);
        _title = ThemeFactory.Label(size: 24, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _resume = ThemeFactory.Button("");
        _language = ThemeFactory.Button("");
        _saveQuit = ThemeFactory.Button("");
        column.AddChild(_title);
        column.AddChild(_resume);
        column.AddChild(_language);
        column.AddChild(_saveQuit);

        _resume.Pressed += () => ResumeRequested?.Invoke();
        _language.Pressed += () => LanguageRequested?.Invoke();
        _saveQuit.Pressed += () => SaveQuitRequested?.Invoke();
        RefreshText();
        _resume.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? ResumeRequested;
    public event Action? LanguageRequested;
    public event Action? SaveQuitRequested;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("menu.pause");
        _resume.Text = _locale.Tr("menu.resume");
        _language.Text = _locale.Tr("menu.settings");
        _saveQuit.Text = _locale.Tr("menu.save_quit");
    }
}

public sealed partial class CompletionOverlay : FullScreenUi
{
    public CompletionOverlay(
        Theme theme,
        LocaleService locale,
        GameSession session
    ) : base(theme)
    {
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.78f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(420, 270) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#101a3af8"), ThemeFactory.Gold, 2, 10)
        );
        center.AddChild(panel);
        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 12);
        panel.AddChild(column);
        var title = ThemeFactory.Label(locale.Tr("complete.title"), 28, ThemeFactory.Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        var body = ThemeFactory.Label(locale.Tr("complete.body"), 13);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.HorizontalAlignment = HorizontalAlignment.Center;
        body.CustomMinimumSize = new Vector2(360, 58);
        var stats = ThemeFactory.Label(
            locale.Tr("complete.stats", session.Clock.Day, session.Quest.Harvested),
            12,
            ThemeFactory.Mint
        );
        stats.HorizontalAlignment = HorizontalAlignment.Center;
        var continueButton = ThemeFactory.Button(locale.Tr("complete.continue"));
        var menuButton = ThemeFactory.Button(locale.Tr("complete.menu"));
        column.AddChild(title);
        column.AddChild(body);
        column.AddChild(stats);
        column.AddChild(continueButton);
        column.AddChild(menuButton);
        continueButton.Pressed += () => ContinueRequested?.Invoke();
        menuButton.Pressed += () => MenuRequested?.Invoke();
        continueButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? ContinueRequested;
    public event Action? MenuRequested;
}

public sealed partial class FadeTransition : ColorRect
{
    private readonly Action _midpoint;
    private readonly Action _finished;
    private double _elapsed;
    private bool _midpointCalled;

    public FadeTransition(Action midpoint, Action finished)
    {
        _midpoint = midpoint;
        _finished = finished;
        Color = new Color(0.03f, 0.04f, 0.12f, 0);
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 200;
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        var alpha = _elapsed < 0.55
            ? Mathf.Clamp((float)(_elapsed / 0.55), 0, 1)
            : Mathf.Clamp((float)((1.3 - _elapsed) / 0.75), 0, 1);
        Color = new Color(Color.R, Color.G, Color.B, alpha);

        if (!_midpointCalled && _elapsed >= 0.55)
        {
            _midpointCalled = true;
            _midpoint();
        }

        if (_elapsed < 1.3)
        {
            return;
        }

        _finished();
        QueueFree();
    }
}

internal sealed partial class TitleBackdrop : Control
{
    private readonly Texture2D _background =
        GD.Load<Texture2D>("res://assets/generated/farm_twilight_backdrop.png");
    private double _time;

    public TitleBackdrop()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawTextureRectRegion(
            _background,
            new Rect2(Vector2.Zero, Size),
            new Rect2(0, 80, 1536, 864)
        );
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("#06122a63"));

        for (var index = 0; index < 28; index++)
        {
            var x = Mathf.PosMod(index * 83 + (float)_time * (2 + index % 2), 640);
            var y = Mathf.PosMod(index * 47 - (float)_time * (1 + index % 3), 340);
            var color = index % 4 == 0 ? ThemeFactory.Gold : ThemeFactory.Mint;
            DrawRect(
                new Rect2(x, y, index % 5 == 0 ? 2 : 1, index % 5 == 0 ? 2 : 1),
                color
            );
        }
    }
}
