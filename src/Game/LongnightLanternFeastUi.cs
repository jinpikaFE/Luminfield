using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

internal static class LongnightLanternFeastArt
{
    private static readonly Texture2D Props = GD.Load<Texture2D>(
        "res://assets/generated/activities/festivals/longnight/longnight_lantern_feast_props.png"
    );

    public static Texture2D RitualTexture(bool lit) => new AtlasTexture
    {
        Atlas = Props,
        Region = lit
            ? new Rect2(712, 844, 457, 373)
            : new Rect2(85, 844, 457, 373)
    };
}

public sealed partial class LongnightLanternFeastOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _sourceCell;
    private readonly List<string> _selectedDishes = [];
    private string _selectedExchangeId = string.Empty;
    private readonly TextureRect _ritualIcon;
    private readonly Label _title;
    private readonly Label _balance;
    private readonly Label _instruction;
    private readonly GridContainer _dishes;
    private readonly GridContainer _exchanges;
    private readonly Label _selection;
    private readonly Label _preview;
    private readonly Label _status;
    private readonly Button _complete;
    private readonly Button _close;

    public LongnightLanternFeastOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition sourceCell
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _sourceCell = sourceCell;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.9f)));

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
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);
        var header = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(510, 36)
        };
        header.AddThemeConstantOverride("separation", 8);
        _ritualIcon = Icon(
            LongnightLanternFeastArt.RitualTexture(false),
            new Vector2(42, 34)
        );
        header.AddChild(_ritualIcon);
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

        _dishes = new GridContainer { Columns = 2 };
        _dishes.AddThemeConstantOverride("h_separation", 5);
        _dishes.AddThemeConstantOverride("v_separation", 3);
        column.AddChild(_dishes);

        _exchanges = new GridContainer { Columns = 2 };
        _exchanges.AddThemeConstantOverride("h_separation", 5);
        _exchanges.AddThemeConstantOverride("v_separation", 3);
        column.AddChild(_exchanges);

        _selection = ThemeFactory.Label(size: 9, color: ThemeFactory.Ink);
        _selection.HorizontalAlignment = HorizontalAlignment.Center;
        _selection.CustomMinimumSize = new Vector2(510, 18);
        column.AddChild(_selection);

        _preview = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
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
        _complete = ThemeFactory.Button("");
        _complete.CustomMinimumSize = new Vector2(210, 25);
        _complete.Pressed += Complete;
        actions.AddChild(_complete);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(140, 25);
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
            FestivalCatalog.LongnightLanternFeastFestivalId,
            year
        );
        _title.Text = _locale.Tr("festival.longnight.activity.title");
        _balance.Text = _locale.Tr(
            "festival.longnight.knots.balance",
            _session.Festival.LanternKnots
        );
        _instruction.Text = _locale.Tr(
            "festival.longnight.activity.instruction"
        );
        _complete.Text = _locale.Tr("festival.longnight.activity.confirm");
        _close.Text = _locale.Tr("menu.back");
        _ritualIcon.Texture = LongnightLanternFeastArt.RitualTexture(
            result is not null
        );
        RebuildDishButtons(result is not null);
        RebuildExchangeButtons(result is not null);

        if (result is not null)
        {
            _selectedDishes.Clear();
            _selectedExchangeId = string.Empty;
            var award = FestivalCatalog.AwardsFor(
                    FestivalCatalog.LongnightLanternFeastFestivalId
                )
                .First(entry => entry.Id == result.AwardId);
            _selection.Text = _locale.Tr(
                "festival.longnight.activity.exchange_result",
                ItemName(result.GiftItemId),
                ItemName(result.GiftRewardItemId)
            );
            _preview.Text = _locale.Tr(
                "festival.longnight.activity.result",
                result.Score,
                _locale.Tr(award.NameKey),
                award.ScripReward
            );
            _status.Text = _locale.Tr(
                "festival.longnight.activity.rite_complete"
            );
            _complete.Disabled = true;
            return;
        }

        _selection.Text = _locale.Tr(
            "festival.longnight.activity.selection",
            _selectedDishes.Count,
            string.IsNullOrEmpty(_selectedExchangeId) ? 0 : 1
        );
        var preview = _session.CheckLongnightFeastParticipation(
            _sourceCell,
            _selectedDishes,
            _selectedExchangeId
        );
        if (preview.CanComplete && preview.Exchange is not null)
        {
            _preview.Text = _locale.Tr(
                "festival.longnight.activity.preview",
                preview.Score,
                _locale.Tr(AwardKey(preview.AwardId)),
                preview.LanternKnotReward,
                ItemName(preview.Exchange.RewardItemId),
                preview.Exchange.RewardCount
            );
            _complete.Disabled = false;
        }
        else
        {
            _preview.Text = _locale.Tr(preview.FailureKey);
            _complete.Disabled = true;
        }
    }

    private void RebuildDishButtons(bool completed)
    {
        Clear(_dishes);
        foreach (var itemId in FestivalCatalog.LongnightDishScores.Keys)
        {
            var selected = _selectedDishes.Contains(
                itemId,
                StringComparer.Ordinal
            );
            var button = ThemeFactory.Button(
                $"{(selected ? "✓ " : string.Empty)}" +
                $"{ItemName(itemId)} ×{_session.Inventory.Count(itemId)}"
            );
            button.CustomMinimumSize = new Vector2(250, 29);
            if (_locale.CurrentLocale == LocaleService.English)
            {
                ThemeFactory.SetFontSize(button, 11);
            }
            button.ClipText = true;
            button.Icon = ItemAtlas(itemId);
            button.ExpandIcon = true;
            button.Disabled = completed;
            button.Pressed += () => ToggleDish(itemId);
            _dishes.AddChild(button);
        }
    }

    private void RebuildExchangeButtons(bool completed)
    {
        Clear(_exchanges);
        foreach (var exchange in FestivalCatalog.LongnightGiftExchanges.Values)
        {
            var row = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(250, 29)
            };
            row.AddThemeConstantOverride("separation", 3);
            row.AddChild(ItemIcon(exchange.GiftItemId, new Vector2(24, 24)));
            var selected = _selectedExchangeId == exchange.Id;
            var button = ThemeFactory.Button(
                $"{(selected ? "✓ " : string.Empty)}" +
                _locale.Tr(
                    "festival.longnight.activity.exchange_option",
                    ItemName(exchange.GiftItemId),
                    ItemName(exchange.RewardItemId),
                    exchange.RewardCount
                )
            );
            button.CustomMinimumSize = new Vector2(194, 29);
            ThemeFactory.SetFontSize(
                button,
                _locale.CurrentLocale == LocaleService.English ? 8 : 12
            );
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.ClipText = true;
            button.Disabled = completed;
            button.Pressed += () => SelectExchange(exchange.Id);
            row.AddChild(button);
            row.AddChild(ItemIcon(
                exchange.RewardItemId,
                new Vector2(24, 24)
            ));
            _exchanges.AddChild(row);
        }
    }

    private void ToggleDish(string itemId)
    {
        if (_selectedDishes.Remove(itemId))
        {
            _status.Text = string.Empty;
            Refresh();
            return;
        }

        if (_selectedDishes.Count >= 2)
        {
            _status.Text = _locale.Tr(
                "festival.longnight.activity.select_limit"
            );
            return;
        }

        _selectedDishes.Add(itemId);
        _status.Text = string.Empty;
        Refresh();
    }

    private void SelectExchange(string exchangeId)
    {
        _selectedExchangeId = _selectedExchangeId == exchangeId
            ? string.Empty
            : exchangeId;
        _status.Text = string.Empty;
        Refresh();
    }

    private void Complete()
    {
        var result = _session.CompleteLongnightFeast(
            _sourceCell,
            _selectedDishes,
            _selectedExchangeId
        );
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            ParticipationCompleted?.Invoke();
        }
        Refresh();
    }

    private string ItemName(string itemId) =>
        DataCatalog.Items.TryGetValue(itemId, out var item)
            ? _locale.Tr(item.NameKey)
            : string.Empty;

    private static string AwardKey(string awardId) =>
        FestivalCatalog.LongnightAwards.First(entry => entry.Id == awardId)
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

    private static TextureRect ItemIcon(string itemId, Vector2 size) => new()
    {
        Texture = ItemAtlas(itemId),
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
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

    private static void Clear(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }
}

public sealed partial class LongnightLanternStallOverlay : FullScreenUi
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

    public LongnightLanternStallOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition target
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _target = target;
        AddChild(Dim(new Color(0.008f, 0.014f, 0.065f, 0.88f)));
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
                new Color("#08152ffa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var header = new HBoxContainer();
        header.AddChild(ItemIcon(
            DataCatalog.StarlightTorchId,
            new Vector2(34, 34)
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
        _title.Text = _locale.Tr("festival.longnight.stall.title");
        _balance.Text = _locale.Tr(
            "festival.longnight.knots.balance",
            _session.Festival.LanternKnots
        );
        _description.Text = _locale.Tr(
            "festival.longnight.stall.description"
        );
        _close.Text = _locale.Tr("menu.back");
        Clear(_offers);
        foreach (var offer in FestivalCatalog.LongnightOffers.Values)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.AddChild(ItemIcon(offer.ItemId, new Vector2(30, 30)));
            var item = DataCatalog.Item(offer.ItemId);
            var name = ThemeFactory.Label(size: 10, color: ThemeFactory.Ink);
            name.Text = _locale.Tr(
                "festival.longnight.stall.offer",
                _locale.Tr(item.NameKey),
                offer.Count,
                offer.ScripCost
            );
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            name.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(name);
            var buy = ThemeFactory.Button(
                _locale.Tr("festival.longnight.stall.buy")
            );
            buy.CustomMinimumSize = new Vector2(92, 28);
            buy.Disabled = !_session.CheckLongnightStallPurchase(
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
        var result = _session.BuyLongnightStallItem(_target, offerId);
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
