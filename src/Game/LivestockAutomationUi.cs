using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class LivestockAutomationOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly string _buildingId;
    private readonly GridPosition _target;
    private readonly Label _title;
    private readonly Label _building;
    private readonly Label _feed;
    private readonly Label _feedNeed;
    private readonly Label _lastFeed;
    private readonly Label _products;
    private readonly Label _lastCollection;
    private readonly VBoxContainer _productRows;
    private readonly Label _notice;
    private readonly Button _deposit;
    private readonly Button _withdraw;
    private readonly Button _collect;
    private readonly Button _close;

    public LivestockAutomationOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        string buildingId,
        GridPosition target
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _buildingId = buildingId;
        _target = target;
        AddChild(Dim(new Color(0.012f, 0.018f, 0.075f, 0.84f)));

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
                new Color("#0b1734fa"),
                ThemeFactory.Mint,
                2,
                9
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(new TextureRect
        {
            Texture = LivestockAutomationArt.ProjectIconTexture(),
            CustomMinimumSize = new Vector2(46, 46),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore
        });
        var titles = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 19, color: ThemeFactory.Mint);
        _building = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        titles.AddChild(_title);
        titles.AddChild(_building);
        header.AddChild(titles);
        column.AddChild(header);

        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        column.AddChild(body);

        var feedPanel = PanelColumn(new Vector2(252, 170));
        var feedColumn = (VBoxContainer)feedPanel.GetChild(0);
        _feed = ThemeFactory.Label(size: 13, color: ThemeFactory.Gold);
        _feedNeed = ThemeFactory.Label(size: 10);
        _lastFeed = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _lastFeed.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        feedColumn.AddChild(_feed);
        feedColumn.AddChild(_feedNeed);
        feedColumn.AddChild(_lastFeed);
        _deposit = ThemeFactory.Button("");
        _withdraw = ThemeFactory.Button("");
        _deposit.CustomMinimumSize = new Vector2(226, 26);
        _withdraw.CustomMinimumSize = new Vector2(226, 26);
        _deposit.Pressed += DepositFeed;
        _withdraw.Pressed += WithdrawFeed;
        feedColumn.AddChild(_deposit);
        feedColumn.AddChild(_withdraw);
        body.AddChild(feedPanel);

        var productPanel = PanelColumn(new Vector2(252, 170));
        var productColumn = (VBoxContainer)productPanel.GetChild(0);
        _products = ThemeFactory.Label(size: 13, color: ThemeFactory.Gold);
        _lastCollection = ThemeFactory.Label(
            size: 9,
            color: ThemeFactory.MutedInk
        );
        _lastCollection.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _productRows = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(226, 67)
        };
        _productRows.AddThemeConstantOverride("separation", 1);
        _collect = ThemeFactory.Button("");
        _collect.CustomMinimumSize = new Vector2(226, 26);
        _collect.Pressed += CollectProducts;
        productColumn.AddChild(_products);
        productColumn.AddChild(_lastCollection);
        productColumn.AddChild(_productRows);
        productColumn.AddChild(_collect);
        body.AddChild(productPanel);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(510, 15);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(180, 25);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _deposit.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? AutomationChanged;

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    public void RefreshText()
    {
        var state = _session.AnimalAutomationFor(_buildingId);
        var buildingKey = _buildingId == AnimalCatalog.StarfeatherCoopId
            ? "animal.automation.building.coop"
            : "animal.automation.building.barn";
        _title.Text = _locale.Tr("animal.automation.panel.title");
        _building.Text = _locale.Tr(buildingKey);
        _feed.Text = _locale.Tr(
            "animal.automation.feed_capacity_label",
            state.StoredFeed,
            AnimalSystem.AutomationFeedCapacity
        );
        _feedNeed.Text = _locale.Tr(
            "animal.automation.feed_need",
            _session.AnimalAutomationFeedNeedFor(_buildingId)
        );
        _lastFeed.Text = _locale.Tr(
            "animal.automation.last_feed",
            _locale.Tr(FeedStatusKey(state.LastFeedStatusId)),
            state.LastAutoFedCount
        );
        _products.Text = _locale.Tr(
            "animal.automation.product_capacity_label",
            state.StoredProductCount,
            AnimalSystem.AutomationProductCapacity
        );
        _lastCollection.Text = _locale.Tr(
            "animal.automation.last_collection",
            _locale.Tr(CollectionStatusKey(state.LastCollectionStatusId)),
            state.LastAutoCollectedCount
        );
        _deposit.Text = _locale.Tr("animal.automation.deposit_feed");
        _withdraw.Text = _locale.Tr("animal.automation.withdraw_feed");
        _collect.Text = _locale.Tr("animal.automation.collect_products");
        _close.Text = _locale.Tr("menu.back");
        var feedItemId = AnimalCatalog.Building(_buildingId).FeedItemId;
        _deposit.Disabled = state.StoredFeed >=
                AnimalSystem.AutomationFeedCapacity ||
            _session.Inventory.Count(feedItemId) <= 0;
        _withdraw.Disabled = state.StoredFeed <= 0;
        _collect.Disabled = state.StoredProductCount <= 0;
        RebuildProducts(state.StoredProducts);
    }

    private void DepositFeed()
    {
        var state = _session.AnimalAutomationFor(_buildingId);
        var feedItemId = AnimalCatalog.Building(_buildingId).FeedItemId;
        var count = Math.Min(
            _session.Inventory.Count(feedItemId),
            AnimalSystem.AutomationFeedCapacity - state.StoredFeed
        );
        Execute(() => _session.DepositAnimalAutomationFeed(
            _buildingId,
            _target,
            count
        ));
    }

    private void WithdrawFeed()
    {
        var count = _session.AnimalAutomationFor(_buildingId).StoredFeed;
        Execute(() => _session.WithdrawAnimalAutomationFeed(
            _buildingId,
            _target,
            count
        ));
    }

    private void CollectProducts() => Execute(() =>
        _session.CollectAnimalAutomationProducts(_buildingId, _target));

    private void Execute(Func<ActionResult> action)
    {
        var result = action();
        _notice.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            AutomationChanged?.Invoke();
        }
        RefreshText();
    }

    private void RebuildProducts(
        IReadOnlyList<CraftingIngredient> products
    )
    {
        foreach (var child in _productRows.GetChildren())
        {
            _productRows.RemoveChild(child);
            child.QueueFree();
        }

        if (products.Count == 0)
        {
            _productRows.AddChild(ThemeFactory.Label(
                _locale.Tr("animal.automation.products_empty"),
                9,
                ThemeFactory.MutedInk
            ));
            return;
        }

        foreach (var product in products)
        {
            var row = new HBoxContainer();
            var icon = new HotbarSlotContent();
            icon.SetState(product.ItemId, product.Count, 0, false);
            row.AddChild(icon);
            var label = ThemeFactory.Label(size: 9);
            label.Text = _locale.Tr(
                "animal.automation.product_row",
                _locale.Tr(DataCatalog.Item(product.ItemId).NameKey),
                product.Count
            );
            label.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(label);
            _productRows.AddChild(row);
        }
    }

    private static PanelContainer PanelColumn(Vector2 size)
    {
        var panel = new PanelContainer { CustomMinimumSize = size };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f2"),
                ThemeFactory.Gold,
                1,
                6,
                8
            )
        );
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 3);
        panel.AddChild(column);
        return panel;
    }

    private static string FeedStatusKey(string statusId) => statusId switch
    {
        AnimalAutomationStatusIds.Succeeded =>
            "animal.automation.status.feed_succeeded",
        AnimalAutomationStatusIds.InsufficientFeed =>
            "animal.automation.status.feed_insufficient",
        AnimalAutomationStatusIds.NoNeed =>
            "animal.automation.status.feed_no_need",
        _ => "animal.automation.status.not_run"
    };

    private static string CollectionStatusKey(string statusId) =>
        statusId switch
        {
            AnimalAutomationStatusIds.Succeeded =>
                "animal.automation.status.collection_succeeded",
            AnimalAutomationStatusIds.ProductCapacity =>
                "animal.automation.status.collection_full",
            AnimalAutomationStatusIds.NoNeed =>
                "animal.automation.status.collection_no_need",
            _ => "animal.automation.status.not_run"
        };
}
