using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class FestivalArt
{
    private static readonly Texture2D Props = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/starharvest/starharvest_market_props.png"
    );

    public static Texture2D BadgeTexture() => new AtlasTexture
    {
        Atlas = Props,
        Region = new Rect2(785, 735, 306, 342)
    };
}

public sealed partial class FestivalShowcaseOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly List<string> _selected = [];
    private readonly Label _title;
    private readonly Label _scrip;
    private readonly Label _instruction;
    private readonly GridContainer _items;
    private readonly Label _selection;
    private readonly Label _preview;
    private readonly Label _status;
    private readonly Button _submit;
    private readonly Button _close;

    public FestivalShowcaseOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.88f)));

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
                new Color("#08152ffa"),
                ThemeFactory.Gold,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(510, 38)
        };
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(Icon(FestivalArt.BadgeTexture(), new Vector2(34, 34)));
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_title);
        _scrip = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _scrip.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_scrip);
        column.AddChild(header);

        _instruction = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _instruction.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_instruction);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(510, 112),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _items = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _items.AddThemeConstantOverride("h_separation", 5);
        _items.AddThemeConstantOverride("v_separation", 3);
        scroll.AddChild(_items);
        column.AddChild(scroll);

        _selection = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _selection.HorizontalAlignment = HorizontalAlignment.Center;
        _selection.CustomMinimumSize = new Vector2(510, 18);
        column.AddChild(_selection);

        _preview = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _preview.HorizontalAlignment = HorizontalAlignment.Center;
        _preview.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _preview.CustomMinimumSize = new Vector2(510, 34);
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
        _submit = ThemeFactory.Button("");
        _submit.CustomMinimumSize = new Vector2(190, 25);
        _submit.Pressed += Submit;
        actions.AddChild(_submit);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 25);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);
        column.AddChild(actions);

        session.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
    }

    public event Action? CloseRequested;
    public event Action? SubmissionCompleted;

    public override void _ExitTree()
    {
        _session.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void Refresh()
    {
        _title.Text = _locale.Tr("festival.starharvest.showcase.title");
        _scrip.Text = _locale.Tr(
            "festival.scrip.balance",
            _session.Festival.Scrip
        );
        _instruction.Text = _locale.Tr(
            "festival.starharvest.showcase.instruction"
        );
        _submit.Text = _locale.Tr("festival.starharvest.showcase.submit");
        _close.Text = _locale.Tr("menu.back");
        RebuildItems();

        var result = _session.Festival.ResultFor(
            FestivalCatalog.StarharvestMarketFestivalId,
            CalendarSystem.YearNumber(_session.Clock.Day)
        );
        if (result is not null)
        {
            _selected.Clear();
            _selection.Text = _locale.Tr(
                "festival.starharvest.showcase.submitted_items",
                ItemList(result.ItemIds)
            );
            _preview.Text = _locale.Tr(
                "festival.starharvest.showcase.result",
                result.Score,
                _locale.Tr(AwardNameKey(result.AwardId)),
                result.AuctionCoins
            );
            _status.Text = _locale.Tr(
                "festival.starharvest.showcase.already_done"
            );
            _submit.Disabled = true;
            return;
        }

        _selection.Text = _selected.Count == 0
            ? _locale.Tr("festival.starharvest.showcase.none_selected")
            : _locale.Tr(
                "festival.starharvest.showcase.selected",
                _selected.Count,
                ItemList(_selected)
            );
        var preview = _session.PreviewFestivalSubmission(_selected);
        if (preview.CanSubmit)
        {
            _preview.Text = _locale.Tr(
                "festival.starharvest.showcase.preview",
                preview.Score,
                _locale.Tr(AwardNameKey(preview.AwardId)),
                preview.AuctionCoins,
                preview.ScripReward
            );
            _submit.Disabled = false;
        }
        else
        {
            _preview.Text = _locale.Tr(preview.FailureKey);
            _submit.Disabled = true;
        }
    }

    private void RebuildItems()
    {
        foreach (var child in _items.GetChildren())
        {
            child.QueueFree();
        }

        var resultExists = _session.Festival.HasParticipated(
            FestivalCatalog.StarharvestMarketFestivalId,
            CalendarSystem.YearNumber(_session.Clock.Day)
        );
        var candidates = _session.Inventory.Slots
            .Where(slot =>
                !slot.IsEmpty &&
                FestivalCatalog.IsEligibleExhibitItem(slot.ItemId)
            )
            .GroupBy(slot => slot.ItemId, StringComparer.Ordinal)
            .Select(group => new
            {
                ItemId = group.Key,
                Count = group.Sum(slot => slot.Count)
            })
            .OrderBy(entry => DataCatalog.Item(entry.ItemId).NameKey)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var selected = _selected.Contains(
                candidate.ItemId,
                StringComparer.Ordinal
            );
            var button = ThemeFactory.Button(
                $"{(selected ? "✓ " : string.Empty)}" +
                _locale.Tr(DataCatalog.Item(candidate.ItemId).NameKey) +
                $" ×{candidate.Count}"
            );
            button.CustomMinimumSize = new Vector2(250, 25);
            button.Disabled = resultExists;
            if (HotbarSlotContent.TryGetIconRegion(
                    candidate.ItemId,
                    out var texture,
                    out var region
                ))
            {
                button.Icon = new AtlasTexture
                {
                    Atlas = texture,
                    Region = region
                };
                button.ExpandIcon = true;
            }
            button.Pressed += () => Toggle(candidate.ItemId);
            _items.AddChild(button);
        }

        if (candidates.Length == 0)
        {
            var empty = ThemeFactory.Label(
                _locale.Tr("festival.starharvest.showcase.no_items"),
                9,
                ThemeFactory.MutedInk
            );
            empty.CustomMinimumSize = new Vector2(510, 28);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            _items.AddChild(empty);
        }
    }

    private void Toggle(string itemId)
    {
        if (_selected.Remove(itemId))
        {
            _status.Text = string.Empty;
            Refresh();
            return;
        }

        if (_selected.Count >= 3)
        {
            _status.Text = _locale.Tr(
                "festival.starharvest.showcase.select_limit"
            );
            return;
        }

        _selected.Add(itemId);
        _status.Text = string.Empty;
        Refresh();
    }

    private void Submit()
    {
        var result = _session.SubmitFestivalExhibit(_selected);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            SubmissionCompleted?.Invoke();
        }
        Refresh();
    }

    private string ItemList(IEnumerable<string> itemIds) => string.Join(
        _locale.CurrentLocale == LocaleService.SimplifiedChinese ? "、" : ", ",
        itemIds.Select(itemId => _locale.Tr(DataCatalog.Item(itemId).NameKey))
    );

    private static string AwardNameKey(string awardId) => awardId switch
    {
        FestivalCatalog.GoldenCrownAwardId =>
            "festival.starharvest.award.gold",
        FestivalCatalog.SilverSheafAwardId =>
            "festival.starharvest.award.silver",
        _ => "festival.starharvest.award.bronze"
    };

    private static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}

public sealed partial class FestivalShopOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _scrip;
    private readonly Label _description;
    private readonly VBoxContainer _offers;
    private readonly Label _status;
    private readonly Button _close;

    public FestivalShopOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.88f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 304)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#08152ffa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);
        var header = new HBoxContainer();
        header.AddChild(Icon(FestivalArt.BadgeTexture(), new Vector2(34, 34)));
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Gold);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_title);
        _scrip = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        header.AddChild(_scrip);
        column.AddChild(header);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _description.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_description);
        _offers = new VBoxContainer();
        _offers.AddThemeConstantOverride("separation", 4);
        column.AddChild(_offers);
        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(390, 18);
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
        _title.Text = _locale.Tr("festival.starharvest.shop.title");
        _scrip.Text = _locale.Tr(
            "festival.scrip.balance",
            _session.Festival.Scrip
        );
        _description.Text = _locale.Tr("festival.starharvest.shop.description");
        _close.Text = _locale.Tr("menu.back");
        foreach (var child in _offers.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var offer in FestivalCatalog.StarharvestOffers.Values)
        {
            var item = DataCatalog.Item(offer.ItemId);
            var button = ThemeFactory.Button(
                _locale.Tr(
                    "festival.shop.offer",
                    _locale.Tr(item.NameKey),
                    offer.Count,
                    offer.ScripCost
                )
            );
            button.CustomMinimumSize = new Vector2(390, 30);
            button.Disabled = !_session.CheckFestivalPurchase(offer.Id)
                .CanPurchase;
            if (HotbarSlotContent.TryGetIconRegion(
                    offer.ItemId,
                    out var texture,
                    out var region
                ))
            {
                button.Icon = new AtlasTexture
                {
                    Atlas = texture,
                    Region = region
                };
                button.ExpandIcon = true;
            }
            button.Pressed += () => Purchase(offer.Id);
            _offers.AddChild(button);
        }
    }

    private void Purchase(string offerId)
    {
        var result = _session.BuyFestivalItem(offerId);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            PurchaseCompleted?.Invoke();
        }
        Refresh();
    }

    private static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}
