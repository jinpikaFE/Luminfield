using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class FireflyTideOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _sourceCell;
    private readonly List<string> _selectedFish = [];
    private readonly Label _title;
    private readonly Label _balance;
    private readonly Label _instruction;
    private readonly GridContainer _fishGrid;
    private readonly Label _preview;
    private readonly Label _status;
    private readonly Button _complete;
    private readonly Button _close;

    public FireflyTideOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition sourceCell
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _sourceCell = sourceCell;
        AddChild(Dim(new Color(0.004f, 0.025f, 0.08f, 0.9f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(548, 338)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#081c35fa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);
        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(510, 38)
        };
        header.AddChild(Icon(
            FireflyTideArt.TideAltarRegion,
            new Vector2(38, 38)
        ));
        _title = ThemeFactory.Label(size: 17, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_title);
        _balance = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _balance.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_balance);
        column.AddChild(header);

        _instruction = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _instruction.HorizontalAlignment = HorizontalAlignment.Center;
        _instruction.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _instruction.CustomMinimumSize = new Vector2(510, 28);
        column.AddChild(_instruction);

        _fishGrid = new GridContainer { Columns = 4 };
        _fishGrid.AddThemeConstantOverride("h_separation", 4);
        _fishGrid.AddThemeConstantOverride("v_separation", 4);
        column.AddChild(_fishGrid);

        _preview = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _preview.HorizontalAlignment = HorizontalAlignment.Center;
        _preview.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _preview.CustomMinimumSize = new Vector2(510, 38);
        column.AddChild(_preview);

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(510, 18);
        column.AddChild(_status);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 8);
        _complete = ThemeFactory.Button("");
        _complete.CustomMinimumSize = new Vector2(210, 26);
        _complete.Pressed += Complete;
        actions.AddChild(_complete);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(140, 26);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);
        column.AddChild(actions);

        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;
    public event Action? ParticipationCompleted;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        var year = CalendarSystem.YearNumber(_session.Clock.Day);
        var result = _session.Festival.ResultFor(
            FestivalCatalog.FireflyTideFestivalId,
            year
        );
        _title.Text = _locale.Tr("festival.firefly.activity.title");
        _balance.Text = _locale.Tr(
            "festival.firefly.glowmarks.balance",
            _session.Festival.Glowmarks
        );
        _instruction.Text = _locale.Tr(
            "festival.firefly.activity.instruction"
        );
        _complete.Text = _locale.Tr("festival.firefly.activity.confirm");
        _close.Text = _locale.Tr("menu.back");
        RebuildFishButtons(result is not null);

        if (result is not null)
        {
            _selectedFish.Clear();
            var award = FestivalCatalog.FireflyAwards.First(entry =>
                entry.Id == result.AwardId);
            _preview.Text = _locale.Tr(
                "festival.firefly.activity.result",
                result.Score,
                _locale.Tr(award.NameKey),
                award.ScripReward
            );
            _status.Text = _locale.Tr(
                "festival.firefly.activity.lanterns_launched"
            );
            _complete.Disabled = true;
            return;
        }

        var preview = _session.CheckFireflyTideParticipation(
            _sourceCell,
            _selectedFish
        );
        if (preview.CanSubmit)
        {
            _preview.Text = _locale.Tr(
                "festival.firefly.activity.preview",
                preview.Score,
                _locale.Tr(AwardKey(preview.AwardId)),
                preview.ScripReward
            );
            _complete.Disabled = false;
        }
        else
        {
            _preview.Text = _locale.Tr(preview.FailureKey);
            _complete.Disabled = true;
        }
    }

    private void RebuildFishButtons(bool completed)
    {
        Clear(_fishGrid);
        foreach (var itemId in FestivalCatalog.FireflyTideFishIds)
        {
            var selected = _selectedFish.Contains(
                itemId,
                StringComparer.Ordinal
            );
            var itemName = ItemName(itemId);
            var count = _session.Inventory.Count(itemId);
            var buttonText = _locale.CurrentLocale == LocaleService.English
                ? EnglishFishButtonText(itemName, count, selected)
                : $"{(selected ? "✓ " : string.Empty)}" +
                    $"{itemName} ×{count}";
            var button = ThemeFactory.Button(
                buttonText
            );
            button.CustomMinimumSize = new Vector2(124, 52);
            ThemeFactory.SetFontSize(
                button,
                _locale.CurrentLocale == LocaleService.English ? 7 : 10
            );
            button.AddThemeConstantOverride("icon_max_width", 36);
            button.ClipText = true;
            button.Icon = ItemAtlas(itemId);
            button.ExpandIcon = true;
            button.Disabled = completed;
            button.Pressed += () => ToggleFish(itemId);
            _fishGrid.AddChild(button);
        }
    }

    private void ToggleFish(string itemId)
    {
        if (_selectedFish.Remove(itemId))
        {
            _status.Text = string.Empty;
            Refresh();
            return;
        }

        if (_selectedFish.Count >= 3)
        {
            _status.Text = _locale.Tr(
                "festival.firefly.activity.select_limit"
            );
            return;
        }

        _selectedFish.Add(itemId);
        _status.Text = string.Empty;
        Refresh();
    }

    private void Complete()
    {
        var result = _session.CompleteFireflyTide(
            _sourceCell,
            _selectedFish
        );
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ParticipationCompleted?.Invoke();
        }
        Refresh();
    }

    private string ItemName(string itemId) =>
        _locale.Tr(DataCatalog.Item(itemId).NameKey);

    private static string EnglishFishButtonText(
        string itemName,
        int count,
        bool selected
    )
    {
        var split = itemName.IndexOf(' ');
        if (split <= 0 || split >= itemName.Length - 1)
        {
            return $"{(selected ? "✓ " : string.Empty)}" +
                $"{itemName} ×{count}";
        }

        return $"{(selected ? "✓ " : string.Empty)}" +
            $"{itemName[..split]}\n{itemName[(split + 1)..]} ×{count}";
    }

    private static string AwardKey(string awardId) =>
        FestivalCatalog.FireflyAwards.First(entry => entry.Id == awardId)
            .NameKey;

    private static AtlasTexture? ItemAtlas(string itemId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            return null;
        }
        return new AtlasTexture { Atlas = texture, Region = region };
    }

    private static TextureRect Icon(Rect2 region, Vector2 size) => new()
    {
        Texture = new AtlasTexture
        {
            Atlas = FireflyTideArt.Atlas,
            Region = region
        },
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private static void Clear(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }
}

public sealed partial class FireflyTideShopOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _target;
    private readonly Label _title;
    private readonly Label _balance;
    private readonly Label _description;
    private readonly VBoxContainer _offers;
    private readonly Label _status;
    private readonly Button _close;

    public FireflyTideShopOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition target
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _target = target;
        AddChild(Dim(new Color(0.004f, 0.025f, 0.08f, 0.88f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(440, 306)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#081c35fa"),
                ThemeFactory.Gold,
                2,
                8
            )
        );
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var header = new HBoxContainer();
        header.AddChild(Icon(
            FireflyTideArt.GlowshopRegion,
            new Vector2(38, 38)
        ));
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        _balance = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        header.AddChild(_balance);
        column.AddChild(header);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _description.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_description);
        _offers = new VBoxContainer();
        _offers.AddThemeConstantOverride("separation", 4);
        column.AddChild(_offers);
        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(400, 18);
        column.AddChild(_status);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(160, 26);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);
        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;
    public event Action? PurchaseCompleted;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        _title.Text = _locale.Tr("festival.firefly.shop.title");
        _balance.Text = _locale.Tr(
            "festival.firefly.glowmarks.balance",
            _session.Festival.Glowmarks
        );
        _description.Text = _locale.Tr(
            "festival.firefly.shop.description"
        );
        _close.Text = _locale.Tr("menu.back");
        Clear(_offers);
        foreach (var offer in FestivalCatalog.FireflyOffers.Values)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.AddChild(ItemIcon(offer.ItemId, new Vector2(30, 30)));
            var name = ThemeFactory.Label(size: 10, color: ThemeFactory.Ink);
            name.Text = _locale.Tr(
                "festival.firefly.shop.offer",
                _locale.Tr(DataCatalog.Item(offer.ItemId).NameKey),
                offer.Count,
                offer.ScripCost
            );
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            name.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(name);
            var buy = ThemeFactory.Button(
                _locale.Tr("festival.firefly.shop.buy")
            );
            buy.CustomMinimumSize = new Vector2(92, 28);
            buy.Disabled = !_session.CheckFireflyShopPurchase(
                _target,
                offer.Id
            ).CanPurchase;
            buy.Pressed += () => Buy(offer.Id);
            row.AddChild(buy);
            _offers.AddChild(row);
        }
    }

    private void Buy(string offerId)
    {
        var result = _session.BuyFireflyShopItem(_target, offerId);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            PurchaseCompleted?.Invoke();
        }
        Refresh();
    }

    private static TextureRect ItemIcon(string itemId, Vector2 size) => new()
    {
        Texture = ItemAtlas(itemId),
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private static TextureRect Icon(Rect2 region, Vector2 size) => new()
    {
        Texture = new AtlasTexture
        {
            Atlas = FireflyTideArt.Atlas,
            Region = region
        },
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private static AtlasTexture? ItemAtlas(string itemId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            return null;
        }
        return new AtlasTexture { Atlas = texture, Region = region };
    }

    private static void Clear(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }
}
