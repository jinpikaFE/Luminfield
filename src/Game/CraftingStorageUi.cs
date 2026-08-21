using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CraftingOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _description;
    private readonly Label _status;
    private readonly Button _close;
    private readonly List<RecipeRow> _rows = [];

    public CraftingOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(526, 338) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#0c1735fa"), ThemeFactory.Mint, 2, 8)
        );
        center.AddChild(panel);

        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        header.AddChild(Icon(GeneratedArt.CreateCraftingIcon(), new Vector2(48, 44)));
        var headerText = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.CustomMinimumSize = new Vector2(420, 26);
        headerText.AddChild(_title);
        headerText.AddChild(_description);
        header.AddChild(headerText);
        column.AddChild(header);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(488, 210),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var recipes = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        recipes.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(recipes);
        column.AddChild(scroll);
        foreach (var recipe in DataCatalog.CraftingRecipes.Values)
        {
            var row = CreateRecipeRow(recipe);
            _rows.Add(row);
            recipes.AddChild(row.Panel);
        }

        _status = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(470, 16);
        column.AddChild(_status);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(160, 24);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _rows[0].Craft.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? Crafted;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("crafting.title");
        _description.Text = _locale.Tr("crafting.description");
        foreach (var row in _rows)
        {
            row.Name.Text = _locale.Tr(
                "crafting.output",
                _locale.Tr(row.Recipe.NameKey),
                row.Recipe.OutputCount
            );
            row.Materials.Text = string.Join(
                "  ·  ",
                row.Recipe.Ingredients.Select(ingredient =>
                    _locale.Tr(
                        "crafting.material_entry",
                        _locale.Tr(DataCatalog.Item(ingredient.ItemId).NameKey),
                        ingredient.Count,
                        _session.Inventory.Count(ingredient.ItemId)
                    )
                )
            );
            row.Craft.Text = _locale.Tr("crafting.action");
            row.Craft.Disabled = !_session.Crafting.HasIngredients(
                row.Recipe.Id,
                _session.Inventory
            );
        }
        _close.Text = _locale.Tr("menu.back");
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private RecipeRow CreateRecipeRow(CraftingRecipe recipe)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(470, 62)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746ef"),
                ThemeFactory.Gold,
                1,
                4,
                5
            )
        );
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", 7);
        panel.AddChild(content);
        content.AddChild(Icon(
            RecipeIcon(recipe.OutputItemId),
            new Vector2(50, 50)
        ));
        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var name = ThemeFactory.Label(size: 12, color: ThemeFactory.Gold);
        var materials = ThemeFactory.Label(size: 8);
        materials.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        materials.CustomMinimumSize = new Vector2(290, 28);
        text.AddChild(name);
        text.AddChild(materials);
        content.AddChild(text);
        var craft = ThemeFactory.Button("");
        craft.CustomMinimumSize = new Vector2(96, 32);
        craft.Pressed += () => Execute(recipe.Id);
        content.AddChild(craft);
        return new RecipeRow(recipe, panel, name, materials, craft);
    }

    private void Execute(string recipeId)
    {
        var result = _session.CraftItem(recipeId);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            Crafted?.Invoke();
        }
        RefreshText();
    }

    private static Texture2D RecipeIcon(string itemId)
    {
        if (itemId == DataCatalog.StarsoilFertilizerId)
        {
            return GeneratedArt.CreateStarsoilFertilizerCraftIcon();
        }

        if (itemId == DataCatalog.StarwovenChestId)
        {
            return GeneratedArt.CreateStarwovenChestItemIcon();
        }

        return GeneratedArt.CreateFarmObjectItemIcon(itemId);
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

    private sealed record RecipeRow(
        CraftingRecipe Recipe,
        PanelContainer Panel,
        Label Name,
        Label Materials,
        Button Craft
    );
}

public sealed partial class StorageOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _position;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly Label _backpackHeader;
    private readonly Label _chestHeader;
    private readonly Label _status;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _storeButtons = [];
    private readonly Dictionary<string, Button> _takeButtons = [];

    public StorageOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition position
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _position = position;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.82f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(568, 332) };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(new Color("#0c1735fa"), ThemeFactory.Gold, 2, 8)
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(Icon(
            GeneratedArt.CreateStarwovenChestItemIcon(),
            new Vector2(48, 42)
        ));
        _title = ThemeFactory.Label(size: 20, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _summary = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _summary.HorizontalAlignment = HorizontalAlignment.Right;
        _summary.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_title);
        header.AddChild(_summary);
        column.AddChild(header);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 10);
        column.AddChild(columns);
        var backpackColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var chestColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        backpackColumn.AddThemeConstantOverride("separation", 3);
        chestColumn.AddThemeConstantOverride("separation", 3);
        columns.AddChild(backpackColumn);
        columns.AddChild(chestColumn);

        _backpackHeader = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        _chestHeader = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        backpackColumn.AddChild(_backpackHeader);
        chestColumn.AddChild(_chestHeader);

        var backpackScroll = Scroll();
        var chestScroll = Scroll();
        var backpackItems = ItemColumn();
        var chestItems = ItemColumn();
        backpackScroll.AddChild(backpackItems);
        chestScroll.AddChild(chestItems);
        backpackColumn.AddChild(backpackScroll);
        chestColumn.AddChild(chestScroll);

        foreach (var itemId in DataCatalog.StorableItemIds)
        {
            var store = ItemButton();
            store.Pressed += () => Execute(
                () => _session.StoreInChest(_position, itemId)
            );
            backpackItems.AddChild(store);
            _storeButtons[itemId] = store;

            var take = ItemButton();
            take.Pressed += () => Execute(
                () => _session.TakeFromChest(_position, itemId)
            );
            chestItems.AddChild(take);
            _takeButtons[itemId] = take;
        }

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(520, 18);
        column.AddChild(_status);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(180, 28);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? StorageChanged;

    public void RefreshText()
    {
        var chest = _session.Storage.ChestAt(_position);
        _title.Text = _locale.Tr("storage.title");
        _summary.Text = _locale.Tr(
            "storage.capacity",
            chest?.UsedSlots ?? 0,
            StorageChestState.SlotCount
        );
        _backpackHeader.Text = _locale.Tr("storage.backpack");
        _chestHeader.Text = _locale.Tr("storage.chest");
        _close.Text = _locale.Tr("menu.back");

        foreach (var itemId in DataCatalog.StorableItemIds)
        {
            var name = _locale.Tr(DataCatalog.Item(itemId).NameKey);
            var backpackCount = _session.Inventory.Count(itemId);
            var chestCount = chest?.Count(itemId) ?? 0;
            _storeButtons[itemId].Text = _locale.Tr(
                "storage.store_action",
                name,
                backpackCount
            );
            var isQualityProduce =
                DataCatalog.ItemQuality(itemId) != CropQuality.Regular;
            _storeButtons[itemId].Visible =
                !isQualityProduce || backpackCount > 0;
            _storeButtons[itemId].Disabled =
                chest is null ||
                backpackCount <= 0 ||
                !chest.CanAdd(itemId, 1);
            _takeButtons[itemId].Text = _locale.Tr(
                "storage.take_action",
                name,
                chestCount
            );
            _takeButtons[itemId].Visible =
                !isQualityProduce || chestCount > 0;
            _takeButtons[itemId].Disabled =
                chest is null ||
                chestCount <= 0 ||
                !_session.Inventory.CanAdd(itemId, 1);
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void Execute(Func<ActionResult> action)
    {
        var result = action();
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            StorageChanged?.Invoke();
        }
        RefreshText();
    }

    private static ScrollContainer Scroll() => new()
    {
        CustomMinimumSize = new Vector2(258, 186),
        SizeFlagsVertical = SizeFlags.ExpandFill
    };

    private static VBoxContainer ItemColumn()
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(246, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        column.AddThemeConstantOverride("separation", 3);
        return column;
    }

    private static Button ItemButton()
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(246, 28);
        ThemeFactory.SetFontSize(button, 9);
        return button;
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
