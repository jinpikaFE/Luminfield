using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class KitchenOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _target;
    private readonly Label _title;
    private readonly Label _description;
    private readonly Label _status;
    private readonly Button _close;
    private readonly List<RecipeRow> _rows = [];

    public KitchenOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition target
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _target = target;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = Panel(526, 338, ThemeFactory.Mint);
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);
        var header = Header(CottageKitchenArt.KitchenIconTexture());
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _description.CustomMinimumSize = new Vector2(416, 24);
        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        headerText.AddChild(_title);
        headerText.AddChild(_description);
        header.AddChild(headerText);
        column.AddChild(header);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(486, 214),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var recipes = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        recipes.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(recipes);
        column.AddChild(scroll);
        foreach (var recipe in DataCatalog.CookingRecipes.Values)
        {
            var row = CreateRecipeRow(recipe);
            _rows.Add(row);
            recipes.AddChild(row.Panel);
        }

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(474, 12);
        column.AddChild(_status);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 22);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _rows[0].Action.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? Cooked;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("kitchen.title");
        _description.Text = _locale.Tr("kitchen.description");
        _close.Text = _locale.Tr("menu.back");
        foreach (var row in _rows)
        {
            row.Name.Text = _locale.Tr(row.Recipe.NameKey);
            row.Materials.Text = string.Join(
                "  ·  ",
                row.Recipe.Ingredients.Select(ingredient =>
                    _locale.Tr(
                        "kitchen.ingredient_entry",
                        _locale.Tr(DataCatalog.Item(
                            ingredient.ItemId
                        ).NameKey),
                        ingredient.Count,
                        _session.Inventory.CountFamily(ingredient.ItemId) +
                            _session.Kitchen.CountFamily(ingredient.ItemId)
                    )
                )
            );
            row.Energy.Text = _locale.Tr(
                "kitchen.energy_restore",
                _session.EffectiveDishEnergyRestore(
                    row.Recipe.OutputItemId
                )
            );
            row.Action.Text = _locale.Tr("kitchen.cook_action");
            row.Action.Disabled = !_session.CheckCookRecipe(
                _target,
                row.Recipe.Id
            ).Succeeded;
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private RecipeRow CreateRecipeRow(CookingRecipeDefinition recipe)
    {
        var panel = Panel(470, 62, ThemeFactory.Gold);
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", 7);
        panel.AddChild(content);
        content.AddChild(Icon(
            CottageKitchenArt.ItemIconTexture(recipe.OutputItemId),
            new Vector2(48, 48)
        ));
        var text = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var name = ThemeFactory.Label(size: 11, color: ThemeFactory.Gold);
        var materials = ThemeFactory.Label(size: 8);
        materials.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        materials.CustomMinimumSize = new Vector2(278, 21);
        var energy = ThemeFactory.Label(size: 8, color: ThemeFactory.Mint);
        text.AddChild(name);
        text.AddChild(materials);
        text.AddChild(energy);
        content.AddChild(text);
        var action = ThemeFactory.Button("");
        action.CustomMinimumSize = new Vector2(86, 30);
        action.AutowrapMode = TextServer.AutowrapMode.Off;
        action.Pressed += () => Execute(recipe.Id);
        content.AddChild(action);
        return new RecipeRow(recipe, panel, name, materials, energy, action);
    }

    private void Execute(string recipeId)
    {
        var result = _session.CookRecipe(_target, recipeId);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            Cooked?.Invoke();
        }
        RefreshText();
    }

    private sealed record RecipeRow(
        CookingRecipeDefinition Recipe,
        PanelContainer Panel,
        Label Name,
        Label Materials,
        Label Energy,
        Button Action
    );

    internal static PanelContainer Panel(
        float width,
        float height,
        Color border
    )
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(width, height)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101a38f8"),
                border,
                1,
                6,
                6
            )
        );
        return panel;
    }

    internal static HBoxContainer Header(Texture2D texture)
    {
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        header.AddChild(Icon(texture, new Vector2(44, 40)));
        return header;
    }

    internal static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}

public sealed partial class IngredientPantryOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _target;
    private readonly Label _title;
    private readonly Label _capacity;
    private readonly Label _backpackHeader;
    private readonly Label _pantryHeader;
    private readonly Label _status;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _storeButtons = [];
    private readonly Dictionary<string, Button> _takeButtons = [];
    private readonly IReadOnlyList<string> _itemIds;

    public IngredientPantryOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition target
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _target = target;
        _itemIds = DataCatalog.Items.Values
            .Where(item => KitchenSystem.IsPantryItem(item.Id))
            .Select(item => item.Id)
            .ToArray();
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.84f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = KitchenOverlay.Panel(568, 332, ThemeFactory.Gold);
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        var header = KitchenOverlay.Header(
            CottageKitchenArt.PantryIconTexture()
        );
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _capacity = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _capacity.HorizontalAlignment = HorizontalAlignment.Right;
        header.AddChild(_title);
        header.AddChild(_capacity);
        column.AddChild(header);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 10);
        column.AddChild(columns);
        var backpack = ItemColumn();
        var pantry = ItemColumn();
        columns.AddChild(backpack);
        columns.AddChild(pantry);
        _backpackHeader = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _pantryHeader = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        backpack.AddChild(_backpackHeader);
        pantry.AddChild(_pantryHeader);
        var backpackScroll = Scroll();
        var pantryScroll = Scroll();
        var backpackItems = ItemColumn();
        var pantryItems = ItemColumn();
        backpackScroll.AddChild(backpackItems);
        pantryScroll.AddChild(pantryItems);
        backpack.AddChild(backpackScroll);
        pantry.AddChild(pantryScroll);

        foreach (var itemId in _itemIds)
        {
            var store = ItemButton(itemId);
            store.Pressed += () => Execute(() =>
                _session.StoreKitchenIngredient(_target, itemId));
            backpackItems.AddChild(store);
            _storeButtons[itemId] = store;
            var take = ItemButton(itemId);
            take.Pressed += () => Execute(() =>
                _session.TakeKitchenIngredient(_target, itemId));
            pantryItems.AddChild(take);
            _takeButtons[itemId] = take;
        }

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.CustomMinimumSize = new Vector2(520, 14);
        column.AddChild(_status);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 22);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? PantryChanged;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("kitchen.pantry.title");
        _capacity.Text = _locale.Tr(
            "kitchen.pantry.capacity",
            _session.Kitchen.UsedPantrySlots,
            KitchenSystem.PantrySlotCount
        );
        _backpackHeader.Text = _locale.Tr("storage.backpack");
        _pantryHeader.Text = _locale.Tr("kitchen.pantry.header");
        _close.Text = _locale.Tr("menu.back");
        foreach (var itemId in _itemIds)
        {
            var name = _locale.Tr(DataCatalog.Item(itemId).NameKey);
            var backpackCount = _session.Inventory.Count(itemId);
            var pantryCount = _session.Kitchen.Count(itemId);
            _storeButtons[itemId].Text = _locale.Tr(
                "kitchen.pantry.store_action",
                name,
                backpackCount
            );
            _storeButtons[itemId].Visible = backpackCount > 0;
            _storeButtons[itemId].Disabled = !_session
                .CheckStoreKitchenIngredient(_target, itemId)
                .Succeeded;
            _takeButtons[itemId].Text = _locale.Tr(
                "kitchen.pantry.take_action",
                name,
                pantryCount
            );
            _takeButtons[itemId].Visible = pantryCount > 0;
            _takeButtons[itemId].Disabled = !_session
                .CheckTakeKitchenIngredient(_target, itemId)
                .Succeeded;
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
            PantryChanged?.Invoke();
        }
        RefreshText();
    }

    private static VBoxContainer ItemColumn()
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        column.AddThemeConstantOverride("separation", 2);
        return column;
    }

    private static ScrollContainer Scroll() => new()
    {
        CustomMinimumSize = new Vector2(258, 194),
        SizeFlagsVertical = SizeFlags.ExpandFill,
        HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
    };

    private static Button ItemButton(string itemId)
    {
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(246, 27);
        ThemeFactory.SetFontSize(button, 9);
        button.AutowrapMode = TextServer.AutowrapMode.Off;
        button.ClipText = true;
        if (HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            button.Icon = new AtlasTexture
            {
                Atlas = texture,
                Region = region,
                FilterClip = true
            };
            button.ExpandIcon = true;
        }
        return button;
    }
}

public sealed partial class CookedDishOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _description;
    private readonly Label _status;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _eatButtons = [];

    public CookedDishOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.015f, 0.02f, 0.08f, 0.84f)));
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = KitchenOverlay.Panel(506, 314, ThemeFactory.Mint);
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        panel.AddChild(column);
        var header = KitchenOverlay.Header(
            CottageKitchenArt.CookedDishIconTexture()
        );
        var headerText = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _description = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        headerText.AddChild(_title);
        headerText.AddChild(_description);
        header.AddChild(headerText);
        column.AddChild(header);
        foreach (var itemId in DataCatalog.CookedDishItemIds)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(KitchenOverlay.Icon(
                CottageKitchenArt.ItemIconTexture(itemId),
                new Vector2(42, 42)
            ));
            var label = ThemeFactory.Label(size: 10);
            label.Name = itemId;
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            var eat = ThemeFactory.Button("");
            eat.CustomMinimumSize = new Vector2(116, 28);
            eat.Pressed += () => Execute(itemId);
            row.AddChild(eat);
            _eatButtons[itemId] = eat;
            column.AddChild(row);
        }
        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_status);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 22);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);
        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? DishEaten;

    public void RefreshText()
    {
        _title.Text = _locale.Tr("cooking.meals.title");
        _description.Text = _locale.Tr("cooking.meals.description");
        _close.Text = _locale.Tr("menu.back");
        foreach (var itemId in DataCatalog.CookedDishItemIds)
        {
            var row = _eatButtons[itemId].GetParent();
            var label = row.GetNode<Label>(itemId);
            label.Text = _locale.Tr(
                "cooking.meal_entry",
                _locale.Tr(DataCatalog.Item(itemId).NameKey),
                _session.EffectiveDishEnergyRestore(itemId),
                _session.Inventory.Count(itemId)
            );
            _eatButtons[itemId].Text = _locale.Tr("cooking.eat_action");
            _eatButtons[itemId].Disabled = !_session
                .CheckEatCookedDish(itemId)
                .Succeeded;
        }
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void Execute(string itemId)
    {
        var result = _session.EatCookedDish(itemId);
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            DishEaten?.Invoke();
        }
        RefreshText();
    }
}
