using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class CompendiumOverlay : FullScreenUi
{
    private static readonly ShaderMaterial ChromaMaterial =
        GeneratedArt.CreateChromaKeyMaterial();
    private static readonly ShaderMaterial UnknownSilhouetteMaterial =
        CreateUnknownSilhouetteMaterial();

    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly GridPosition _deskTarget;
    private readonly Label _title;
    private readonly Label _progress;
    private readonly GridContainer _grid;
    private readonly Label _entryTitle;
    private readonly Label _entryDetails;
    private readonly Label _rewardStatus;
    private readonly Label _status;
    private readonly Button _claim;
    private readonly Button _donateFish;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _categoryButtons =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _selectedEntryByCategory =
        new(StringComparer.Ordinal);
    private readonly List<Button> _entryButtons = [];
    private readonly List<TextureRect> _entryIcons = [];
    private readonly List<TextureRect> _detailIcons = [];
    private string _selectedCategoryId = CollectionCategoryIds.Crops;

    public CompendiumOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale,
        GridPosition deskTarget,
        string initialCategoryId = CollectionCategoryIds.Crops
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        _deskTarget = deskTarget;
        _selectedCategoryId = CompendiumCatalog.Categories.ContainsKey(
            initialCategoryId
        )
            ? initialCategoryId
            : CollectionCategoryIds.Crops;
        AddChild(Dim(new Color(0.02f, 0.03f, 0.1f, 0.78f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(568, 338)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#101a3afa"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 4);
        panel.AddChild(root);

        var header = new HBoxContainer();
        _title = ThemeFactory.Label(size: 18, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _progress = ThemeFactory.Label(size: 13, color: ThemeFactory.Gold);
        header.AddChild(_title);
        header.AddChild(_progress);
        root.AddChild(header);

        var categories = new GridContainer
        {
            Columns = 4,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        categories.AddThemeConstantOverride("separation", 6);
        root.AddChild(categories);
        foreach (var categoryId in CompendiumCatalog.CategoryIds)
        {
            var button = ThemeFactory.Button("");
            button.CustomMinimumSize = new Vector2(100, 22);
            button.ToggleMode = true;
            button.ClipText = true;
            button.Pressed += () => SelectCategory(categoryId);
            _categoryButtons[categoryId] = button;
            categories.AddChild(button);
        }

        root.AddChild(new HSeparator());
        var body = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 10);
        root.AddChild(body);

        _grid = new GridContainer
        {
            Columns = 5,
            CustomMinimumSize = new Vector2(256, 180)
        };
        _grid.AddThemeConstantOverride("h_separation", 4);
        _grid.AddThemeConstantOverride("v_separation", 4);
        body.AddChild(_grid);

        var maximumEntryCount = CompendiumCatalog.Categories.Values
            .Max(category => category.EntryIds.Count);
        for (var index = 0; index < maximumEntryCount; index++)
        {
            var button = ThemeFactory.Button("");
            var entryIcon = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore
            };
            button.AddChild(entryIcon);
            var entryIndex = index;
            button.Pressed += () => SelectEntry(entryIndex);
            button.FocusEntered += () => SelectEntry(entryIndex, false);
            _entryButtons.Add(button);
            _entryIcons.Add(entryIcon);
            _grid.AddChild(button);
        }

        var details = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(278, 180),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        details.AddThemeConstantOverride("separation", 3);
        body.AddChild(details);

        _entryTitle = ThemeFactory.Label(size: 15, color: ThemeFactory.Gold);
        details.AddChild(_entryTitle);

        var icons = new HBoxContainer();
        icons.AddThemeConstantOverride("separation", 4);
        details.AddChild(icons);
        for (var index = 0; index < 6; index++)
        {
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(index is 0 or 5 ? 36 : 28, 36),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest
            };
            _detailIcons.Add(icon);
            icons.AddChild(icon);
        }

        _entryDetails = ThemeFactory.Label(size: 10);
        _entryDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _entryDetails.CustomMinimumSize = new Vector2(278, 68);
        _entryDetails.SizeFlagsVertical = SizeFlags.ExpandFill;
        details.AddChild(_entryDetails);

        _rewardStatus = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _rewardStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(_rewardStatus);

        _status = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(_status);

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 8);
        root.AddChild(actions);
        _claim = ThemeFactory.Button("");
        _claim.CustomMinimumSize = new Vector2(224, 24);
        _claim.Pressed += ClaimContextualAction;
        actions.AddChild(_claim);
        _donateFish = ThemeFactory.Button("");
        _donateFish.CustomMinimumSize = new Vector2(126, 24);
        _donateFish.Pressed += () => FishingDonationRequested?.Invoke();
        actions.AddChild(_donateFish);
        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(150, 24);
        _close.Pressed += () => CloseRequested?.Invoke();
        actions.AddChild(_close);

        foreach (var categoryId in CompendiumCatalog.CategoryIds)
        {
            var firstKnown = CompendiumCatalog.EntriesForCategory(categoryId)
                .Select((entry, index) => (entry, index))
                .Where(pair => session.Collection.IsDiscovered(pair.entry.Id))
                .Select(pair => pair.index)
                .DefaultIfEmpty(0)
                .First();
            _selectedEntryByCategory[categoryId] = firstKnown;
        }

        session.Collection.Changed += Refresh;
        locale.LocaleChanged += Refresh;
        Refresh();
        _entryButtons[SelectedEntryIndex].CallDeferred(
            Control.MethodName.GrabFocus
        );
    }

    public event Action? CloseRequested;
    public event Action? RewardClaimed;
    public event Action? FishingDonationRequested;

    private IReadOnlyList<CompendiumEntryDefinition> CurrentEntries =>
        CompendiumCatalog.EntriesForCategory(_selectedCategoryId);

    private int SelectedEntryIndex
    {
        get => Math.Clamp(
            _selectedEntryByCategory.GetValueOrDefault(_selectedCategoryId),
            0,
            CurrentEntries.Count - 1
        );
        set => _selectedEntryByCategory[_selectedCategoryId] = Math.Clamp(
            value,
            0,
            CurrentEntries.Count - 1
        );
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputSetup.Pause))
        {
            CloseRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_page_up"))
        {
            SelectRelativeCategory(-1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_page_down"))
        {
            SelectRelativeCategory(1);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        _session.Collection.Changed -= Refresh;
        _locale.LocaleChanged -= Refresh;
    }

    private void SelectRelativeCategory(int offset)
    {
        var current = CompendiumCatalog.CategoryIds
            .Select((categoryId, index) => (categoryId, index))
            .Single(pair => pair.categoryId == _selectedCategoryId)
            .index;
        var next = (current + offset + CompendiumCatalog.CategoryIds.Count) %
            CompendiumCatalog.CategoryIds.Count;
        SelectCategory(CompendiumCatalog.CategoryIds[next]);
    }

    private void SelectCategory(string categoryId)
    {
        if (!CompendiumCatalog.Categories.ContainsKey(categoryId))
        {
            return;
        }

        _selectedCategoryId = categoryId;
        _status.Text = string.Empty;
        Refresh();
        _entryButtons[SelectedEntryIndex].GrabFocus();
    }

    private void SelectEntry(int index, bool grabFocus = true)
    {
        if (index >= CurrentEntries.Count)
        {
            return;
        }

        SelectedEntryIndex = index;
        RefreshEntryDetails();
        if (_selectedCategoryId == CollectionCategoryIds.Artifacts)
        {
            RefreshArtifactDonation();
        }
        if (grabFocus)
        {
            _entryButtons[SelectedEntryIndex].GrabFocus();
        }
    }

    private void Refresh()
    {
        var category = CompendiumCatalog.Category(_selectedCategoryId);
        var entries = CurrentEntries;
        var discovered = _session.Collection.DiscoveredCount(category.Id);
        _title.Text = _locale.Tr(category.TitleKey);
        _progress.Text = _locale.Tr(
            "collection.progress",
            discovered,
            entries.Count
        );
        _donateFish.Text = _locale.Tr("fishing.donation.title");
        _donateFish.Visible =
            _selectedCategoryId == CollectionCategoryIds.Fish;
        _close.Text = _locale.Tr("menu.back");

        foreach (var categoryId in CompendiumCatalog.CategoryIds)
        {
            var button = _categoryButtons[categoryId];
            button.Text = _locale.Tr(
                $"{CompendiumCatalog.Category(categoryId).NameKey}.tab"
            );
            button.TooltipText = _locale.Tr(
                CompendiumCatalog.Category(categoryId).NameKey
            );
            button.Disabled = false;
            button.ButtonPressed = categoryId == _selectedCategoryId;
        }

        var forageEntries = _selectedCategoryId == CollectionCategoryIds.Forage;
        var fishEntries = _selectedCategoryId == CollectionCategoryIds.Fish;
        var mineralEntries =
            _selectedCategoryId == CollectionCategoryIds.Minerals;
        var largeEntries = _selectedCategoryId is
            CollectionCategoryIds.Cooking or CollectionCategoryIds.Artisan or
            CollectionCategoryIds.Artifacts;
        _grid.Columns = fishEntries
            ? 6
            : forageEntries || mineralEntries
                ? 4
                : largeEntries
                    ? 2
                    : 5;
        _grid.AddThemeConstantOverride(
            "h_separation",
            largeEntries ? 6 : 4
        );
        _grid.AddThemeConstantOverride(
            "v_separation",
            largeEntries ? 6 : 4
        );
        for (var index = 0; index < _entryButtons.Count; index++)
        {
            var visible = index < entries.Count;
            var button = _entryButtons[index];
            var icon = _entryIcons[index];
            button.Visible = visible;
            if (!visible)
            {
                continue;
            }

            var entry = entries[index];
            var known = _session.Collection.IsDiscovered(entry.Id);
            var buttonSize = largeEntries
                ? 56
                : forageEntries || mineralEntries
                    ? 48
                    : fishEntries
                        ? 34
                        : 40;
            var iconSize = largeEntries
                ? 36
                : forageEntries || mineralEntries
                    ? 32
                    : fishEntries
                        ? 24
                        : 28;
            button.CustomMinimumSize = new Vector2(buttonSize, buttonSize);
            icon.Position = new Vector2(
                (buttonSize - iconSize) / 2f,
                (buttonSize - iconSize) / 2f
            );
            icon.Size = new Vector2(iconSize, iconSize);
            icon.Texture = EntryTexture(entry);
            icon.Material = known
                ? ChromaMaterial
                : UnknownSilhouetteMaterial;
            icon.Modulate = Colors.White;
            button.TooltipText = known
                ? _locale.Tr(entry.NameKey)
                : _locale.Tr("collection.entry.undiscovered");
        }

        RefreshReward(category, discovered);
        RefreshEntryDetails();
    }

    private void RefreshReward(
        CompendiumCategoryDefinition category,
        int discovered
    )
    {
        if (category.Id == CollectionCategoryIds.Artifacts)
        {
            RefreshArtifactDonation();
            return;
        }

        _claim.Disabled = false;

        var reward = CompendiumCatalog.RewardForCategory(category.Id);
        if (reward is null)
        {
            _claim.Visible = false;
            _rewardStatus.Text = string.Empty;
            return;
        }

        var claimed = _session.Collection.IsRewardClaimed(reward.Id);
        var ready = reward.RequiredEntryIds.All(
            _session.Collection.IsDiscovered
        );
        var rewardName = _locale.Tr(reward.NameKey);
        _claim.Visible = ready && !claimed;
        _claim.Text = _locale.Tr("collection.reward.claim_named", rewardName);
        _rewardStatus.Text = claimed
            ? _locale.Tr(
                "collection.reward.status.active",
                rewardName,
                _locale.Tr(reward.DescriptionKey)
            )
            : ready
                ? _locale.Tr(
                    "collection.reward.status.ready",
                    rewardName
                )
                : _locale.Tr(
                    "collection.reward.status.progress",
                    rewardName,
                    discovered,
                    reward.RequiredEntryIds.Count
                );
    }

    private void RefreshEntryDetails()
    {
        var category = CompendiumCatalog.Category(_selectedCategoryId);
        var entry = CurrentEntries[SelectedEntryIndex];
        if (!_session.Collection.IsDiscovered(entry.Id))
        {
            _entryTitle.Text = _locale.Tr("collection.entry.undiscovered");
            _entryDetails.Text = _locale.Tr(
                category.UndiscoveredDescriptionKey
            );
            SetUnknownDetailIcons(entry);
            return;
        }

        _entryTitle.Text = _locale.Tr(entry.NameKey);
        switch (entry.Kind)
        {
            case CompendiumEntryKind.Crop:
                RefreshCropDetails(entry);
                break;
            case CompendiumEntryKind.CookedDish:
                RefreshCookingDetails(entry);
                break;
            case CompendiumEntryKind.ArtisanGood:
                RefreshArtisanDetails(entry);
                break;
            case CompendiumEntryKind.Forage:
                RefreshForageDetails(entry);
                break;
            case CompendiumEntryKind.Fish:
                RefreshFishDetails(entry);
                break;
            case CompendiumEntryKind.Mineral:
                RefreshMineralDetails(entry);
                break;
            case CompendiumEntryKind.Artifact:
                RefreshArtifactDetails(entry);
                break;
            case CompendiumEntryKind.Enemy:
                RefreshEnemyDetails(entry);
                break;
        }
    }

    private void RefreshArtifactDetails(CompendiumEntryDefinition entry)
    {
        _entryDetails.Text = _locale.Tr(
            "collection.artifact.details",
            _session.Collection.IsDonated(entry.Id)
                ? _locale.Tr("collection.donation.state.donated")
                : _locale.Tr("collection.donation.state.recovered")
        );
        ConfigureDetailIconSizes(large: true);
        SetEntryIcon(_detailIcons[0], entry, false);
        for (var index = 1; index < _detailIcons.Count; index++)
        {
            ClearIcon(_detailIcons[index]);
        }
    }

    private void RefreshEnemyDetails(CompendiumEntryDefinition entry)
    {
        var enemy = StarfallRuinsTrialCatalog.Enemy(entry.Id);
        _entryDetails.Text = _locale.Tr(
            "collection.enemy.details",
            enemy.MaxHealth,
            enemy.Damage,
            enemy.MovementSpeedPixelsPerSecond
        );
        ConfigureDetailIconSizes(large: true);
        SetEntryIcon(_detailIcons[0], entry, false);
        for (var index = 1; index < _detailIcons.Count; index++)
        {
            ClearIcon(_detailIcons[index]);
        }
    }

    private void RefreshCropDetails(CompendiumEntryDefinition entry)
    {
        var crop = DataCatalog.Crop(entry.CropId);
        var seed = DataCatalog.Item(entry.SeedItemId);
        var produce = DataCatalog.Item(entry.ItemId);
        var seasonNames = crop.SeasonIds is { Count: > 0 }
            ? string.Join(
                _locale.Tr("collection.list.separator"),
                crop.SeasonIds.Select(seasonId => _locale.Tr(
                    $"calendar.season.{seasonId}"
                ))
            )
            : _locale.Tr("collection.season.all");
        var regrowth = crop.RegrowthNights > 0
            ? _locale.Tr("collection.regrowth.nights", crop.RegrowthNights)
            : _locale.Tr("collection.regrowth.none");
        _entryDetails.Text = _locale.Tr(
            "collection.crop.details",
            seasonNames,
            crop.MatureAfterWateredNights,
            regrowth,
            seed.BuyPrice,
            produce.SellPrice
        );
        SetCropDetailIcons(entry);
    }

    private void RefreshCookingDetails(CompendiumEntryDefinition entry)
    {
        var item = DataCatalog.Item(entry.ItemId);
        var recipe = DataCatalog.CookingRecipes.Values.Single(candidate =>
            candidate.OutputItemId == entry.ItemId
        );
        var ingredients = string.Join(
            _locale.Tr("collection.list.separator"),
            recipe.Ingredients.Select(ingredient => _locale.Tr(
                "collection.cooking.ingredient",
                _locale.Tr(DataCatalog.Item(ingredient.ItemId).NameKey),
                ingredient.Count
            ))
        );
        _entryDetails.Text = _locale.Tr(
            "collection.cooking.details",
            ingredients,
            _session.EffectiveDishEnergyRestore(entry.ItemId),
            item.SellPrice
        );
        SetCookingDetailIcons(entry, recipe);
    }

    private void RefreshArtisanDetails(CompendiumEntryDefinition entry)
    {
        var item = DataCatalog.Item(entry.ItemId);
        var currentPrice = _session.SalePrice(entry.ItemId);
        if (entry.ItemId == DataCatalog.StarhoneyId)
        {
            _entryDetails.Text = _locale.Tr(
                "collection.artisan.honey_details",
                item.SellPrice,
                currentPrice
            );
            SetArtisanDetailIcons(
                entry,
                DataCatalog.MoonplumId,
                DataCatalog.GlowcombHiveId
            );
            return;
        }

        var recipe = DataCatalog.ProcessorRecipes.Values.Single(candidate =>
            candidate.OutputItemId == entry.ItemId
        );
        var machines = string.Join(
            _locale.Tr("collection.list.separator"),
            ProcessorCatalog.Machines.Values
                .Where(machine => machine.RecipeIds.Contains(
                    recipe.Id,
                    StringComparer.Ordinal
                ))
                .Select(machine => _locale.Tr(machine.NameKey))
        );
        _entryDetails.Text = _locale.Tr(
            recipe.Nights == 1
                ? "collection.artisan.processor_details.single"
                : "collection.artisan.processor_details.multiple",
            _locale.Tr(DataCatalog.Item(recipe.InputItemId).NameKey),
            recipe.InputCount,
            machines,
            recipe.Nights,
            item.SellPrice,
            currentPrice
        );
        SetArtisanDetailIcons(entry, recipe.InputItemId);
    }

    private void RefreshForageDetails(CompendiumEntryDefinition entry)
    {
        var definition = ForageCatalog.ByItemId[entry.ItemId];
        var item = DataCatalog.Item(entry.ItemId);
        _entryDetails.Text = _locale.Tr(
            "collection.forage.details",
            _locale.Tr($"calendar.season.{definition.SeasonId}"),
            _locale.Tr(WorldDefinition.RegionNameKey(definition.Biome)),
            item.SellPrice
        );
        SetForageDetailIcons(entry);
    }

    private void RefreshFishDetails(CompendiumEntryDefinition entry)
    {
        var fish = DataCatalog.Fishes[entry.Id];
        var item = DataCatalog.Item(entry.ItemId);
        var seasons = fish.SeasonIds is { Count: > 0 }
            ? string.Join(
                _locale.Tr("collection.list.separator"),
                fish.SeasonIds.Select(seasonId =>
                    _locale.Tr($"calendar.season.{seasonId}")
                )
            )
            : _locale.Tr("fishing.condition.all_seasons");
        var weather = string.IsNullOrWhiteSpace(fish.WeatherId)
            ? _locale.Tr("fishing.condition.any_weather")
            : _locale.Tr(DataCatalog.Weather(fish.WeatherId).NameKey);
        _entryDetails.Text = _locale.Tr(
            "collection.fish.details",
            _locale.Tr(FishingWaterKey(fish.WaterKind)),
            seasons,
            $"{fish.StartMinute / 60:00}:{fish.StartMinute % 60:00}",
            $"{fish.EndMinute / 60:00}:{fish.EndMinute % 60:00}",
            weather,
            item.SellPrice
        );
        ConfigureDetailIconSizes(large: true);
        SetItemIcon(_detailIcons[0], entry.ItemId, false);
        for (var index = 1; index < _detailIcons.Count; index++)
        {
            ClearIcon(_detailIcons[index]);
        }
    }

    private void RefreshMineralDetails(CompendiumEntryDefinition entry)
    {
        var mineral = MiningCatalog.Mineral(entry.ItemId);
        var item = DataCatalog.Item(entry.ItemId);
        var tier = ToolProgressionCatalog.Tier(
            mineral.RequiredToolTierId
        );
        _entryDetails.Text = _locale.Tr(
            "collection.mineral.details",
            string.Join(
                _locale.Tr("collection.list.separator"),
                mineral.RoomNumbers
            ),
            _locale.Tr(tier.NameKey),
            mineral.EnergyCost,
            item.SellPrice
        );
        ConfigureDetailIconSizes(large: true);
        SetItemIcon(_detailIcons[0], entry.ItemId, false);
        _detailIcons[1].Texture = new AtlasTexture
        {
            Atlas = CrystalGrottoArt.Atlas,
            Region = CrystalGrottoArt.MineralVeinRegion(entry.ItemId),
            FilterClip = true
        };
        _detailIcons[1].Material = null;
        _detailIcons[1].Modulate = Colors.White;
        for (var index = 2; index < _detailIcons.Count; index++)
        {
            ClearIcon(_detailIcons[index]);
        }
    }

    private static string FishingWaterKey(FishingWaterKind waterKind) =>
        waterKind switch
        {
            FishingWaterKind.CrystalStream =>
                "fishing.water.crystal_stream",
            FishingWaterKind.MoonwaterWetlands =>
                "fishing.water.moonwater_wetlands",
            _ => "fishing.water.homestead_pond"
        };

    private void SetCropDetailIcons(CompendiumEntryDefinition entry)
    {
        ConfigureDetailIconSizes(large: false);
        SetItemIcon(_detailIcons[0], entry.SeedItemId, false);
        for (var stage = 0; stage < 4; stage++)
        {
            if (!CropArtCatalog.TryGrowthFrame(
                    entry.CropId,
                    stage,
                    out var texture,
                    out var region,
                    out var material
                ))
            {
                ClearIcon(_detailIcons[stage + 1]);
                continue;
            }

            _detailIcons[stage + 1].Texture = new AtlasTexture
            {
                Atlas = texture,
                Region = region,
                FilterClip = true
            };
            _detailIcons[stage + 1].Material = material;
            _detailIcons[stage + 1].Modulate = Colors.White;
        }
        SetItemIcon(_detailIcons[5], entry.ItemId, false);
    }

    private void SetCookingDetailIcons(
        CompendiumEntryDefinition entry,
        CookingRecipeDefinition recipe
    )
    {
        ConfigureDetailIconSizes(large: true);
        SetItemIcon(_detailIcons[0], entry.ItemId, false);
        for (var index = 1; index < _detailIcons.Count; index++)
        {
            if (index <= recipe.Ingredients.Count)
            {
                SetItemIcon(
                    _detailIcons[index],
                    recipe.Ingredients[index - 1].ItemId,
                    false
                );
            }
            else
            {
                ClearIcon(_detailIcons[index]);
            }
        }
    }

    private void SetArtisanDetailIcons(
        CompendiumEntryDefinition entry,
        params string[] supportingItemIds
    )
    {
        ConfigureDetailIconSizes(large: true);
        SetItemIcon(_detailIcons[0], entry.ItemId, false);
        for (var index = 1; index < _detailIcons.Count; index++)
        {
            if (index <= supportingItemIds.Length)
            {
                SetItemIcon(
                    _detailIcons[index],
                    supportingItemIds[index - 1],
                    false
                );
            }
            else
            {
                ClearIcon(_detailIcons[index]);
            }
        }
    }

    private void SetForageDetailIcons(CompendiumEntryDefinition entry)
    {
        ConfigureDetailIconSizes(large: true);
        SetItemIcon(_detailIcons[0], entry.ItemId, false);
        _detailIcons[1].Texture = new AtlasTexture
        {
            Atlas = SeasonalForageArt.Atlas,
            Region = SeasonalForageArt.WorldRegion(entry.ItemId),
            FilterClip = true
        };
        _detailIcons[1].Material = null;
        _detailIcons[1].Modulate = Colors.White;
        for (var index = 2; index < _detailIcons.Count; index++)
        {
            ClearIcon(_detailIcons[index]);
        }
    }

    private void SetUnknownDetailIcons(CompendiumEntryDefinition entry)
    {
        foreach (var icon in _detailIcons)
        {
            ClearIcon(icon);
        }

        var large = entry.Kind != CompendiumEntryKind.Crop;
        ConfigureDetailIconSizes(large);
        SetEntryIcon(
            large ? _detailIcons[0] : _detailIcons[5],
            entry,
            true
        );
    }

    private void ConfigureDetailIconSizes(bool large)
    {
        for (var index = 0; index < _detailIcons.Count; index++)
        {
            _detailIcons[index].CustomMinimumSize = large
                ? new Vector2(index == 0 ? 56 : 32, 56)
                : new Vector2(index is 0 or 5 ? 36 : 28, 36);
        }
    }

    private static void ClearIcon(TextureRect target)
    {
        target.Texture = null;
        target.Material = null;
        target.Modulate = Colors.White;
    }

    private static void SetItemIcon(
        TextureRect target,
        string itemId,
        bool silhouette
    )
    {
        target.Texture = ItemTexture(itemId);
        target.Material = silhouette
            ? UnknownSilhouetteMaterial
            : ChromaMaterial;
        target.Modulate = Colors.White;
    }

    private static void SetEntryIcon(
        TextureRect target,
        CompendiumEntryDefinition entry,
        bool silhouette
    )
    {
        target.Texture = EntryTexture(entry);
        target.Material = silhouette
            ? UnknownSilhouetteMaterial
            : ChromaMaterial;
        target.Modulate = Colors.White;
    }

    private static Texture2D EntryTexture(CompendiumEntryDefinition entry)
    {
        if (entry.Kind == CompendiumEntryKind.Enemy)
        {
            return DeepMineArt.EnemyIcon(entry.Id);
        }

        return ItemTexture(entry.ItemId);
    }

    private static Texture2D ItemTexture(string itemId)
    {
        if (!HotbarSlotContent.TryGetIconRegion(
                itemId,
                out var texture,
                out var region
            ))
        {
            throw new KeyNotFoundException(
                $"Missing compendium item art for '{itemId}'."
            );
        }

        return new AtlasTexture
        {
            Atlas = texture,
            Region = region,
            FilterClip = true
        };
    }

    private static ShaderMaterial CreateUnknownSilhouetteMaterial()
    {
        return new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;

                    void fragment() {
                        vec4 pixel = texture(TEXTURE, UV);
                        float other = max(pixel.g, pixel.b);
                        bool red_key = pixel.r > 0.45
                            && pixel.g < 0.32
                            && pixel.b < 0.32
                            && pixel.r > other * 2.0;
                        float alpha = red_key ? 0.0 : pixel.a;
                        COLOR = vec4(0.16, 0.22, 0.38, alpha * 0.86);
                    }
                    """
            }
        };
    }

    private void ClaimContextualAction()
    {
        if (_selectedCategoryId == CollectionCategoryIds.Artifacts)
        {
            DonateSelectedArtifact();
            return;
        }

        ClaimReward();
    }

    private void RefreshArtifactDonation()
    {
        var entry = CurrentEntries[SelectedEntryIndex];
        var known = _session.Collection.IsDiscovered(entry.Id);
        var donated = _session.Collection.IsDonated(entry.Id);
        var count = _session.Collection.DonatedCount(
            CollectionCategoryIds.Artifacts
        );
        _rewardStatus.Text = _locale.Tr(
            "collection.donation.progress",
            count,
            CurrentEntries.Count
        );
        _claim.Visible = known && !donated;
        if (!_claim.Visible)
        {
            return;
        }

        var check = _session.CheckDonateCollectionEntry(
            _deskTarget,
            entry.Id
        );
        _claim.Disabled = !check.Succeeded;
        _claim.Text = _locale.Tr(
            check.Succeeded
                ? "collection.donation.action"
                : "collection.donation.action_missing"
        );
    }

    private void DonateSelectedArtifact()
    {
        var entry = CurrentEntries[SelectedEntryIndex];
        var result = _session.DonateCollectionEntry(
            _deskTarget,
            entry.Id
        );
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            RewardClaimed?.Invoke();
        }
        Refresh();
    }

    private void ClaimReward()
    {
        var reward = CompendiumCatalog.RewardForCategory(
            _selectedCategoryId
        );
        if (reward is null)
        {
            return;
        }

        var result = _session.ClaimCollectionReward(
            _deskTarget,
            reward.Id
        );
        _status.Text = _locale.Tr(result.MessageKey);
        if (result.Succeeded)
        {
            RewardClaimed?.Invoke();
        }
        Refresh();
    }
}
