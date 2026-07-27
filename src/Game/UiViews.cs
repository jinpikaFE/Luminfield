using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public abstract partial class FullScreenUi : Control
{
    protected FullScreenUi(Theme theme)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = 100;
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
    private readonly Label _water;
    private readonly Label _coins;
    private readonly Label _selected;
    private readonly Label _controls;
    private readonly PanelContainer _noticePanel;
    private readonly Label _notice;
    private readonly MinimapView _minimap;
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
        var statusRow = new HBoxContainer();
        _energyText = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _energyText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _water = ThemeFactory.Label(size: 8, color: new Color("#79dff0"));
        _coins = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _coins.HorizontalAlignment = HorizontalAlignment.Right;
        _energy = new ProgressBar
        {
            MinValue = 0,
            MaxValue = GameSession.MaxEnergy,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(148, 10)
        };
        statusRow.AddChild(_energyText);
        statusRow.AddChild(_water);
        statusRow.AddChild(_coins);
        energyColumn.AddChild(statusRow);
        energyColumn.AddChild(_energy);

        var hotbar = new HBoxContainer
        {
            Position = new Vector2(146, 309),
            Size = new Vector2(348, 40),
            MouseFilter = MouseFilterEnum.Ignore
        };
        hotbar.AddThemeConstantOverride("separation", 4);
        AddChild(hotbar);
        for (var index = 0; index < Inventory.HotbarSlotCount; index++)
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

        _minimap = new MinimapView(session)
        {
            Position = new Vector2(500, 59),
            Size = new Vector2(132, 92),
            ZIndex = 20
        };
        AddChild(_minimap);

        session.Changed += Refresh;
        session.EnergyChanged += Refresh;
        session.WaterChanged += Refresh;
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
        _water.Text = _locale.Tr(
            "hud.water",
            _session.WateringCanWater,
            GameSession.MaxWateringCanWater
        );
        _coins.Text = _locale.Tr("hud.coins", _session.Coins);
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
        if (selectedSlot.IsEmpty)
        {
            _selected.Text = _locale.Tr("hud.selected", "—");
        }
        else
        {
            var selectedName = _locale.Tr(DataCatalog.Item(selectedSlot.ItemId).NameKey);
            _selected.Text = selectedSlot.ItemId is
                DataCatalog.WateringCanId or DataCatalog.BucketId
                ? _locale.Tr(
                    "hud.selected_water",
                    selectedName,
                    _session.WateringCanWater,
                    GameSession.MaxWateringCanWater
                )
                : _locale.Tr("hud.selected", selectedName);
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _session.EnergyChanged -= Refresh;
        _session.WaterChanged -= Refresh;
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

internal sealed partial class MinimapView : Control
{
    private static readonly Vector2 MapOrigin = new(6, 6);
    private static readonly Vector2 MapSize = new(120, 80);
    private readonly GameSession _session;

    public MinimapView(GameSession session)
    {
        _session = session;
        MouseFilter = MouseFilterEnum.Ignore;
        session.PlayerMoved += OnWorldChanged;
        session.Exploration.Changed += OnWorldChanged;
    }

    public override void _Draw()
    {
        DrawStyleBox(
            ThemeFactory.CompactBox(
                new Color("#07132bf2"),
                ThemeFactory.Gold,
                1,
                3,
                4
            ),
            new Rect2(Vector2.Zero, Size)
        );
        DrawRect(new Rect2(MapOrigin, MapSize), new Color("#050b1d"));

        var chunkSize = new Vector2(
            MapSize.X / WorldDefinition.ChunkColumns,
            MapSize.Y / WorldDefinition.ChunkRows
        );
        for (var y = 0; y < WorldDefinition.ChunkRows; y++)
        {
            for (var x = 0; x < WorldDefinition.ChunkColumns; x++)
            {
                var chunk = new ChunkPosition(x, y);
                var rect = new Rect2(
                    MapOrigin + new Vector2(x * chunkSize.X, y * chunkSize.Y),
                    chunkSize
                );
                if (!_session.Exploration.IsDiscovered(chunk))
                {
                    DrawRect(rect.Grow(-0.5f), new Color("#0a1128"));
                    continue;
                }

                var sample = new GridPosition(
                    x * WorldDefinition.ChunkSize + WorldDefinition.ChunkSize / 2,
                    y * WorldDefinition.ChunkSize + WorldDefinition.ChunkSize / 2
                );
                DrawRect(rect.Grow(-0.5f), BiomeColor(WorldDefinition.GetBiome(sample)));
                DrawLine(
                    rect.Position,
                    new Vector2(rect.Position.X, rect.End.Y),
                    new Color(1, 1, 1, 0.08f),
                    1
                );
            }
        }

        var homeRect = new Rect2(
            MapOrigin,
            new Vector2(
                FarmSystem.MapWidth / (float)WorldDefinition.Width * MapSize.X,
                FarmSystem.MapHeight / (float)WorldDefinition.Height * MapSize.Y
            )
        );
        DrawRect(homeRect, new Color("#f3ca78aa"), false, 1);

        foreach (var landmark in WorldDefinition.Landmarks)
        {
            var chunk = WorldDefinition.GetChunk(landmark.Position);
            if (!_session.Exploration.IsDiscovered(chunk))
            {
                continue;
            }

            var point = WorldToMap(landmark.Position.X, landmark.Position.Y);
            DrawColoredPolygon(
                [
                    point + new Vector2(0, -2),
                    point + new Vector2(2, 0),
                    point + new Vector2(0, 2),
                    point + new Vector2(-2, 0)
                ],
                ThemeFactory.Gold
            );
        }

        var player = WorldToMap(_session.PlayerX / 16f, _session.PlayerY / 16f);
        DrawCircle(player, 2.6f, new Color("#07132b"));
        DrawCircle(player, 1.8f, ThemeFactory.Mint);
        DrawLine(
            player + new Vector2(-2, 3),
            player + new Vector2(2, 3),
            new Color("#f3ca78aa"),
            1
        );
    }

    public override void _ExitTree()
    {
        _session.PlayerMoved -= OnWorldChanged;
        _session.Exploration.Changed -= OnWorldChanged;
    }

    private void OnWorldChanged() => QueueRedraw();

    private Vector2 WorldToMap(float x, float y) =>
        MapOrigin + new Vector2(
            x / WorldDefinition.Width * MapSize.X,
            y / WorldDefinition.Height * MapSize.Y
        );

    private static Color BiomeColor(WorldBiome biome) => biome switch
    {
        WorldBiome.Home => new Color("#31545d"),
        WorldBiome.WhisperingWoods => new Color("#173a42"),
        WorldBiome.StarfallMeadow => new Color("#2c6660"),
        WorldBiome.CrystalVale => new Color("#24536b"),
        WorldBiome.MoonwaterWetlands => new Color("#14506a"),
        WorldBiome.StarfallRuins => new Color("#45456d"),
        _ => new Color("#17233f")
    };
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
    private static readonly Texture2D EconomyIcons =
        GD.Load<Texture2D>("res://assets/generated/economy_assets_chroma.png");
    private static readonly Texture2D ToolIcons =
        GD.Load<Texture2D>("res://assets/generated/tool_backpack_icons_chroma.png");
    private const float ToolIconCell = 443.5f;

    private readonly Label _key;
    private readonly Label _count;
    private string _itemId = string.Empty;
    private bool _selected;

    public HotbarSlotContent()
    {
        CustomMinimumSize = new Vector2(34, 34);
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        Material = GeneratedArt.CreateChromaKeyMaterial();

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
        _key.Text = key > 0 ? key.ToString() : string.Empty;
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

        if (!TryGetIconRegion(_itemId, out var texture, out var source))
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
        DrawTextureRectRegion(texture, destination, source);
    }

    internal static bool TryGetIconRegion(
        string itemId,
        out Texture2D texture,
        out Rect2 region
    )
    {
        texture = itemId switch
        {
            DataCatalog.HandId or
            DataCatalog.ShovelId or
            DataCatalog.MacheteId or
            DataCatalog.WateringCanId or
            DataCatalog.BucketId or
            DataCatalog.LumenwoodId or
            DataCatalog.CrystalShardId or
            "__backpack__" => ToolIcons,
            DataCatalog.StarbudPreserveId or DataCatalog.MoonrootTonicId => EconomyIcons,
            _ => ItemIcons
        };
        region = itemId switch
        {
            DataCatalog.HandId => ToolRegion(0),
            DataCatalog.ShovelId => ToolRegion(1),
            DataCatalog.MacheteId => ToolRegion(2),
            DataCatalog.WateringCanId => ToolRegion(3),
            DataCatalog.BucketId => ToolRegion(4),
            "__backpack__" => ToolRegion(5),
            DataCatalog.LumenwoodId => ToolRegion(6),
            DataCatalog.CrystalShardId => ToolRegion(7),
            DataCatalog.StarbudSeedId => new Rect2(867, 220, 341, 327),
            DataCatalog.MoonrootSeedId => new Rect2(45, 690, 340, 340),
            DataCatalog.StarbudId => new Rect2(465, 650, 315, 420),
            DataCatalog.MoonrootId => new Rect2(865, 635, 335, 450),
            DataCatalog.StarbudPreserveId => new Rect2(185, 125, 275, 330),
            DataCatalog.MoonrootTonicId => new Rect2(805, 75, 220, 420),
            _ => default
        };
        return region.Size != Vector2.Zero;
    }

    private static Rect2 ToolRegion(int index) => new(
        index % 4 * ToolIconCell,
        index / 4 * ToolIconCell,
        ToolIconCell,
        ToolIconCell
    );
}

public sealed partial class BackpackOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _hint;
    private readonly Button _close;
    private readonly List<PanelContainer> _slots = [];
    private readonly List<HotbarSlotContent> _contents = [];

    public BackpackOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.8f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(506, 306) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#0c1735fa"), ThemeFactory.Mint, 2, 9)
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 7);
        var emblem = new HotbarSlotContent();
        emblem.CustomMinimumSize = new Vector2(38, 38);
        emblem.SetState("__backpack__", 0, 0, false);
        _title = ThemeFactory.Label(size: 19, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _summary = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _summary.HorizontalAlignment = HorizontalAlignment.Right;
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(emblem);
        header.AddChild(_title);
        header.AddChild(_summary);
        column.AddChild(header);

        _hint = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_hint);

        var grid = new GridContainer { Columns = Inventory.HotbarSlotCount };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(grid);

        for (var index = 0; index < Inventory.SlotCount; index++)
        {
            var slot = new PanelContainer
            {
                CustomMinimumSize = new Vector2(50, 48),
                MouseFilter = MouseFilterEnum.Stop
            };
            var content = new HotbarSlotContent();
            slot.AddChild(content);
            grid.AddChild(slot);
            _slots.Add(slot);
            _contents.Add(content);
        }

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(160, 28);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("backpack.title");
        var used = _session.Inventory.Slots.Count(slot => !slot.IsEmpty);
        _summary.Text = _locale.Tr("backpack.capacity", used, Inventory.SlotCount);
        _hint.Text = _locale.Tr("backpack.hint");
        _close.Text = _locale.Tr("backpack.close");

        for (var index = 0; index < Inventory.SlotCount; index++)
        {
            var inventorySlot = _session.Inventory.Slots[index];
            var hotbar = index < Inventory.HotbarSlotCount;
            var selected = hotbar && index == _session.Inventory.SelectedIndex;
            _slots[index].AddThemeStyleboxOverride(
                "panel",
                ThemeFactory.CompactBox(
                    selected
                        ? new Color("#354966f5")
                        : hotbar
                            ? new Color("#182746f0")
                            : new Color("#101a32e8"),
                    selected
                        ? ThemeFactory.Gold
                        : hotbar
                            ? new Color("#6d8293")
                            : new Color("#405b72"),
                    selected ? 2 : 1,
                    6,
                    3
                )
            );
            _contents[index].SetState(
                inventorySlot.IsEmpty ? string.Empty : inventorySlot.ItemId,
                inventorySlot.IsEmpty ? 0 : inventorySlot.Count,
                hotbar ? index + 1 : 0,
                selected
            );
            _slots[index].TooltipText = inventorySlot.IsEmpty
                ? _locale.Tr("backpack.empty")
                : _locale.Tr(DataCatalog.Item(inventorySlot.ItemId).NameKey);
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }
}

public sealed partial class ShopOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _coins;
    private readonly Label _buyHeader;
    private readonly Label _sellHeader;
    private readonly Label _status;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _buyButtons = [];
    private readonly Dictionary<string, Button> _sellButtons = [];

    public ShopOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.72f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(522, 300) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#101a3af8"), ThemeFactory.Gold, 2, 9)
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);

        var header = new HBoxContainer();
        _title = ThemeFactory.Label(size: 22, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _coins = ThemeFactory.Label(size: 14, color: ThemeFactory.Gold);
        header.AddChild(_title);
        header.AddChild(_coins);
        column.AddChild(header);

        var tradeColumns = new HBoxContainer();
        tradeColumns.AddThemeConstantOverride("separation", 10);
        column.AddChild(tradeColumns);

        var buyColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sellColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buyColumn.AddThemeConstantOverride("separation", 4);
        sellColumn.AddThemeConstantOverride("separation", 4);
        tradeColumns.AddChild(buyColumn);
        tradeColumns.AddChild(sellColumn);

        _buyHeader = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        _sellHeader = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        buyColumn.AddChild(_buyHeader);
        sellColumn.AddChild(_sellHeader);

        AddTradeButton(
            buyColumn,
            _buyButtons,
            DataCatalog.StarbudSeedId,
            () => _session.BuyItem(DataCatalog.StarbudSeedId)
        );
        AddTradeButton(
            buyColumn,
            _buyButtons,
            DataCatalog.MoonrootSeedId,
            () => _session.BuyItem(DataCatalog.MoonrootSeedId)
        );

        foreach (var itemId in new[]
                 {
                     DataCatalog.StarbudId,
                     DataCatalog.MoonrootId,
                     DataCatalog.StarbudPreserveId,
                     DataCatalog.MoonrootTonicId,
                     DataCatalog.LumenwoodId,
                     DataCatalog.CrystalShardId
                 })
        {
            AddTradeButton(
                sellColumn,
                _sellButtons,
                itemId,
                () => _session.SellItem(itemId)
            );
        }

        _status = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(480, 24);
        column.AddChild(_status);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(180, 30);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _buyButtons[DataCatalog.StarbudSeedId].CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? TransactionSucceeded;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("shop.title");
        _coins.Text = _locale.Tr("shop.wallet", _session.Coins);
        _buyHeader.Text = _locale.Tr("shop.buy_header");
        _sellHeader.Text = _locale.Tr("shop.sell_header");
        _close.Text = _locale.Tr("menu.back");

        foreach (var pair in _buyButtons)
        {
            var item = DataCatalog.Item(pair.Key);
            pair.Value.Text = _locale.Tr(
                "shop.buy_action",
                _locale.Tr(item.NameKey),
                item.BuyPrice,
                _session.Inventory.Count(pair.Key)
            );
        }

        foreach (var pair in _sellButtons)
        {
            var item = DataCatalog.Item(pair.Key);
            pair.Value.Text = _locale.Tr(
                "shop.sell_action",
                _locale.Tr(item.NameKey),
                item.SellPrice,
                _session.Inventory.Count(pair.Key)
            );
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void AddTradeButton(
        VBoxContainer parent,
        Dictionary<string, Button> collection,
        string itemId,
        Func<ActionResult> action
    )
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(238, 30);
        button.AddThemeFontSizeOverride("font_size", 10);
        button.Pressed += () =>
        {
            var result = action();
            _status.Text = _locale.Tr(result.MessageKey);
            if (result.Succeeded)
            {
                TransactionSucceeded?.Invoke();
            }
            RefreshText();
        };
        collection[itemId] = button;
        parent.AddChild(button);
    }
}

public sealed partial class ProcessorOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _state;
    private readonly Label _status;
    private readonly Button _starbud;
    private readonly Button _moonroot;
    private readonly Button _collect;
    private readonly Button _close;

    public ProcessorOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.72f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(430, 292) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#101a3af8"), ThemeFactory.Mint, 2, 9)
        );
        center.AddChild(panel);

        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        _title = ThemeFactory.Label(size: 22, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _state = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _state.HorizontalAlignment = HorizontalAlignment.Center;
        _state.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _state.CustomMinimumSize = new Vector2(390, 36);
        _starbud = RecipeButton(DataCatalog.StarbudPreserveRecipeId);
        _moonroot = RecipeButton(DataCatalog.MoonrootTonicRecipeId);
        _collect = ThemeFactory.Button("");
        _collect.Pressed += () => Execute(_session.CollectProcessedItem);
        _status = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(390, 24);
        _close = ThemeFactory.Button("");
        _close.Pressed += () => CloseRequested?.Invoke();

        column.AddChild(_title);
        column.AddChild(_state);
        column.AddChild(_starbud);
        column.AddChild(_moonroot);
        column.AddChild(_collect);
        column.AddChild(_status);
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _starbud.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? ProcessingSucceeded;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("processor.title");
        _close.Text = _locale.Tr("menu.back");

        var idle = _session.Processor.IsIdle;
        _starbud.Disabled = !idle;
        _moonroot.Disabled = !idle;
        SetRecipeText(_starbud, DataCatalog.StarbudPreserveRecipeId);
        SetRecipeText(_moonroot, DataCatalog.MoonrootTonicRecipeId);

        if (idle)
        {
            _state.Text = _locale.Tr("processor.idle");
        }
        else
        {
            var recipe = DataCatalog.ProcessorRecipe(_session.Processor.ActiveRecipeId);
            var outputName = _locale.Tr(DataCatalog.Item(recipe.OutputItemId).NameKey);
            _state.Text = _session.Processor.IsReady
                ? _locale.Tr("processor.ready", outputName)
                : _locale.Tr(
                    "processor.processing",
                    outputName,
                    _session.Processor.RemainingNights
                );
        }

        _collect.Visible = _session.Processor.IsReady;
        _collect.Disabled = !_session.Processor.IsReady;
        _collect.Text = _locale.Tr("processor.collect");
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private Button RecipeButton(string recipeId)
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(390, 34);
        button.AddThemeFontSizeOverride("font_size", 11);
        button.Pressed += () => Execute(() => _session.StartProcessing(recipeId));
        return button;
    }

    private void SetRecipeText(Button button, string recipeId)
    {
        var recipe = DataCatalog.ProcessorRecipe(recipeId);
        button.Text = _locale.Tr(
            "processor.recipe_action",
            _locale.Tr(DataCatalog.Item(recipe.InputItemId).NameKey),
            recipe.InputCount,
            _locale.Tr(DataCatalog.Item(recipe.OutputItemId).NameKey),
            _session.Inventory.Count(recipe.InputItemId)
        );
    }

    private void Execute(Func<ActionResult> action)
    {
        var result = action();
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ProcessingSucceeded?.Invoke();
        }
        RefreshText();
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
