using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private readonly GameSession _session = new();
    private readonly LocaleService _locale = new();
    private Theme _theme = null!;
    private SaveService _saveService = null!;
    private AccessibilitySettingsService _settingsService = null!;
    private AccessibilitySettings _settings = null!;
    private PixelAudio _audio = null!;
    private CanvasLayer _uiLayer = null!;
    private Node2D? _world;
    private FarmView? _farm;
    private CottageView? _cottage;
    private GreenhouseView? _greenhouse;
    private StarfeatherCoopView? _starfeatherCoop;
    private MoonfleeceBarnView? _moonfleeceBarn;
    private ArchiveView? _archive;
    private WorkshopView? _workshop;
    private TeaHouseView? _teaHouse;
    private TwilightEmporiumView? _twilightEmporium;
    private StarlightPostView? _starlightPost;
    private StarfallWatchView? _starfallWatch;
    private StarharvestMarketView? _starharvestMarket;
    private GleamrisePlantingFestivalView? _gleamrisePlantingFestival;
    private LongnightLanternFeastView? _longnightLanternFeast;
    private FireflyTideView? _fireflyTide;
    private CrystalGrottoView? _crystalGrotto;
    private StarfallRuinsTrialView? _starfallRuinsTrial;
    private TitleMenu? _title;
    private HudView? _hud;
    private PauseOverlay? _pauseOverlay;
    private DialogueOverlay? _dialogueOverlay;
    private CompletionOverlay? _completionOverlay;
    private ShopOverlay? _shopOverlay;
    private ProcessorOverlay? _processorOverlay;
    private ShippingOverlay? _shippingOverlay;
    private CommissionBoardOverlay? _commissionOverlay;
    private ConstructionOverlay? _constructionOverlay;
    private StarlightMailOverlay? _mailOverlay;
    private StarlightPedestalOverlay? _starlightOverlay;
    private CraftingOverlay? _craftingOverlay;
    private KitchenOverlay? _kitchenOverlay;
    private IngredientPantryOverlay? _ingredientPantryOverlay;
    private CookedDishOverlay? _cookedDishOverlay;
    private StorageOverlay? _storageOverlay;
    private NightlySummaryOverlay? _nightlySummaryOverlay;
    private BackpackOverlay? _backpackOverlay;
    private FishingCollectionOverlay? _fishingCollectionOverlay;
    private FishingDonationOverlay? _fishingDonationOverlay;
    private FishingMinigameOverlay? _fishingMinigameOverlay;
    private FishingGearOverlay? _fishingGearOverlay;
    private DeepMineOverlay? _deepMineOverlay;
    private StarGateOverlay? _starGateOverlay;
    private StellarResonanceOverlay? _stellarResonanceOverlay;
    private MainStoryEndingOverlay? _mainStoryEndingOverlay;
    private AccessibilitySettingsOverlay? _settingsOverlay;
    private FarmingSpecializationOverlay? _farmingSpecializationOverlay;
    private GleamriseSeasonOverlay? _gleamriseSeasonOverlay;
    private FestivalShowcaseOverlay? _festivalShowcaseOverlay;
    private FestivalShopOverlay? _festivalShopOverlay;
    private GleamrisePlantingOverlay? _gleamrisePlantingOverlay;
    private GleamriseSeedExchangeOverlay? _gleamriseSeedExchangeOverlay;
    private LongnightLanternFeastOverlay? _longnightFeastOverlay;
    private LongnightLanternStallOverlay? _longnightStallOverlay;
    private FireflyTideOverlay? _fireflyTideOverlay;
    private FireflyTideShopOverlay? _fireflyTideShopOverlay;
    private LivestockAutomationOverlay? _livestockAutomationOverlay;
    private CompendiumOverlay? _compendiumOverlay;
    private ToolUpgradeOverlay? _toolUpgradeOverlay;
    private FadeTransition? _fadeTransition;
    private bool _playing;
    private bool _paused;
    private bool _titleLanguageOverridden;
    private bool _mailPlaytest;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        InputSetup.EnsureActions();
        _settingsService = new AccessibilitySettingsService(
            ProjectSettings.GlobalizePath("user://settings.json")
        );
        _settings = _settingsService.Load();
        InputSetup.ApplyKeyboardBindings(_settings);
        AccessibilityRuntime.Apply(_settings);
        ConfigureSessionAccessibility();
        DisplayServer.WindowSetMinSize(new Vector2I(960, 540));

        _locale.LoadJson(
            LocaleService.English,
            Godot.FileAccess.GetFileAsString("res://localization/en.json")
        );
        _locale.LoadJson(
            LocaleService.SimplifiedChinese,
            Godot.FileAccess.GetFileAsString("res://localization/zh_CN.json")
        );
        _locale.SetLocale(LocaleService.SimplifiedChinese);

        _theme = ThemeFactory.CreateTheme();
        foreach (var itemId in HotbarSlotContent.MissingStableItemIconIds())
        {
            GD.PushError($"Stable item '{itemId}' has no runtime icon.");
        }
        _saveService = new SaveService(
            ProjectSettings.GlobalizePath("user://saves/slot_1.json")
        );

        _audio = new PixelAudio();
        AddChild(_audio);
        _uiLayer = new CanvasLayer { Layer = 100 };
        AddChild(_uiLayer);

        _session.NewGame(_locale.CurrentLocale);
        _session.CollectionEntryDiscovered += OnCollectionEntryDiscovered;
        _locale.LocaleChanged += OnLocaleChanged;
        ShowTitle();

        var userArgs = OS.GetCmdlineUserArgs();
        var playtestSetup = CreatePlaytestScenarioRegistry()
            .ResolveSetup(userArgs);
        if (playtestSetup is not null)
        {
            Callable.From(playtestSetup).CallDeferred();
        }

        var captureArgument = userArgs.FirstOrDefault(value =>
            value.StartsWith(
                "--capture-playtest=",
                StringComparison.Ordinal
            )
        );
        if (!string.IsNullOrWhiteSpace(captureArgument))
        {
            var capturePath = captureArgument[
                "--capture-playtest=".Length..
            ];
            Callable.From(
                () => CapturePlaytest(capturePath)
            ).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        if (!_playing || IsInputBlocked)
        {
            return;
        }

        _session.Clock.AdvanceRealTime(delta);
        if (_session.Clock.EndOfDayReached && _fadeTransition is null)
        {
            if (_session.InsideStarfallRuinsTrial)
            {
                ResolveStarfallTrialDefeat(forcedByClosingTime: true);
            }
            else
            {
                EndDay();
            }
        }
    }

    private async void CapturePlaytest(string resourcePath)
    {
        for (var frame = 0; frame < 12; frame++)
        {
            await ToSignal(
                GetTree(),
                SceneTree.SignalName.ProcessFrame
            );
        }

        var image = GetViewport().GetTexture().GetImage();
        var targetPath = ProjectSettings.GlobalizePath(resourcePath);
        var error = image.SavePng(targetPath);
        if (error != Error.Ok)
        {
            GD.PushError(
                $"Could not capture playtest image: {targetPath}"
            );
        }
        else
        {
            GD.Print($"Captured playtest image: {targetPath}");
        }

        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playing)
        {
            return;
        }

        var pausePressed = @event.IsActionPressed(InputSetup.Pause);
        var uiCancelPressed = @event.IsActionPressed(InputSetup.UiCancel);
        var overlayCancelPressed = pausePressed || uiCancelPressed;

        if (overlayCancelPressed &&
            _settingsOverlay is not null)
        {
            CloseSettings();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _fishingMinigameOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _fishingGearOverlay is not null)
        {
            CloseFishingGear();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _deepMineOverlay is not null)
        {
            CloseDeepMine();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _starGateOverlay is not null)
        {
            CloseStarGate();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _stellarResonanceOverlay is not null)
        {
            CloseStellarResonance();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _mainStoryEndingOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _compendiumOverlay is not null)
        {
            CloseCropCodex();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _festivalShowcaseOverlay is not null)
        {
            CloseFestivalShowcase();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _festivalShopOverlay is not null)
        {
            CloseFestivalShop();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _gleamrisePlantingOverlay is not null)
        {
            CloseGleamrisePlanting();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _gleamriseSeedExchangeOverlay is not null)
        {
            CloseGleamriseSeedExchange();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _shopOverlay is not null)
        {
            CloseShop();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _processorOverlay is not null)
        {
            CloseProcessor();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _shippingOverlay is not null)
        {
            CloseShipping();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _commissionOverlay is not null)
        {
            CloseCommissionBoard();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _constructionOverlay is not null)
        {
            CloseConstructionPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _livestockAutomationOverlay is not null)
        {
            CloseLivestockAutomation();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _mailOverlay is not null)
        {
            CloseStarlightMail();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _starlightOverlay is not null)
        {
            CloseStarlightPedestal();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _kitchenOverlay is not null)
        {
            CloseKitchen();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _ingredientPantryOverlay is not null)
        {
            CloseIngredientPantry();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _cookedDishOverlay is not null)
        {
            CloseCookedDishes();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((overlayCancelPressed ||
             @event.IsActionPressed(InputSetup.Crafting)) &&
            _craftingOverlay is not null)
        {
            CloseCrafting();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _farmingSpecializationOverlay is not null)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _storageOverlay is not null)
        {
            CloseStorage();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed && _nightlySummaryOverlay is not null)
        {
            CloseNightlySummary();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((overlayCancelPressed ||
             @event.IsActionPressed(InputSetup.Backpack)) &&
            _backpackOverlay is not null)
        {
            CloseBackpack();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _fishingCollectionOverlay is not null)
        {
            CloseFishingCollection();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _fishingDonationOverlay is not null)
        {
            CloseFishingDonation();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (overlayCancelPressed &&
            _gleamriseSeasonOverlay is not null)
        {
            CloseGleamriseSeasonGoals();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Backpack) && !IsInputBlocked)
        {
            OpenBackpack();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Crafting) && !IsInputBlocked)
        {
            OpenCrafting();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((pausePressed || (_paused && uiCancelPressed)) &&
            _dialogueOverlay is null &&
            _completionOverlay is null &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _constructionOverlay is null &&
            _livestockAutomationOverlay is null &&
            _mailOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _kitchenOverlay is null &&
            _ingredientPantryOverlay is null &&
            _cookedDishOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fishingCollectionOverlay is null &&
            _fishingDonationOverlay is null &&
            _gleamriseSeasonOverlay is null &&
            _farmingSpecializationOverlay is null &&
            _fadeTransition is null)
        {
            if (_paused)
            {
                ClosePause();
            }
            else
            {
                OpenPause();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (IsInputBlocked)
        {
            return;
        }

        if (@event.IsActionPressed(InputSetup.HotbarPrevious))
        {
            _session.Inventory.SelectRelative(-1);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.HotbarNext))
        {
            _session.Inventory.SelectRelative(1);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        var physical = key.PhysicalKeycode;
        if (physical is >= Key.Key1 and <= Key.Key8)
        {
            _session.Inventory.Select((int)(physical - Key.Key1));
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest)
        {
            return;
        }

        if (_playing)
        {
            SaveNow(false);
        }
        GetTree().Quit();
    }

    private bool IsInputBlocked =>
        _paused ||
        _dialogueOverlay is not null ||
        _completionOverlay is not null ||
        _shopOverlay is not null ||
        _processorOverlay is not null ||
        _shippingOverlay is not null ||
        _commissionOverlay is not null ||
        _constructionOverlay is not null ||
        _livestockAutomationOverlay is not null ||
        _compendiumOverlay is not null ||
        _toolUpgradeOverlay is not null ||
        _mailOverlay is not null ||
        _starlightOverlay is not null ||
        _craftingOverlay is not null ||
        _kitchenOverlay is not null ||
        _ingredientPantryOverlay is not null ||
        _cookedDishOverlay is not null ||
        _storageOverlay is not null ||
        _nightlySummaryOverlay is not null ||
        _backpackOverlay is not null ||
        _fishingCollectionOverlay is not null ||
        _fishingDonationOverlay is not null ||
        _fishingMinigameOverlay is not null ||
        _fishingGearOverlay is not null ||
        _deepMineOverlay is not null ||
        _starGateOverlay is not null ||
        _stellarResonanceOverlay is not null ||
        _mainStoryEndingOverlay is not null ||
        _settingsOverlay is not null ||
        _gleamriseSeasonOverlay is not null ||
        _farmingSpecializationOverlay is not null ||
        _festivalShowcaseOverlay is not null ||
        _festivalShopOverlay is not null ||
        _gleamrisePlantingOverlay is not null ||
        _gleamriseSeedExchangeOverlay is not null ||
        _longnightFeastOverlay is not null ||
        _longnightStallOverlay is not null ||
        _fireflyTideOverlay is not null ||
        _fireflyTideShopOverlay is not null ||
        _fadeTransition is not null;

    private bool CanRestoreWorldControls =>
        !_paused &&
        _dialogueOverlay is null &&
        _completionOverlay is null &&
        _shopOverlay is null &&
        _processorOverlay is null &&
        _shippingOverlay is null &&
        _commissionOverlay is null &&
        _constructionOverlay is null &&
        _livestockAutomationOverlay is null &&
        _compendiumOverlay is null &&
        _toolUpgradeOverlay is null &&
        _mailOverlay is null &&
        _starlightOverlay is null &&
        _craftingOverlay is null &&
        _kitchenOverlay is null &&
        _ingredientPantryOverlay is null &&
        _cookedDishOverlay is null &&
        _storageOverlay is null &&
        _nightlySummaryOverlay is null &&
        _backpackOverlay is null &&
        _fishingCollectionOverlay is null &&
        _fishingDonationOverlay is null &&
        _fishingMinigameOverlay is null &&
        _fishingGearOverlay is null &&
        _deepMineOverlay is null &&
        _starGateOverlay is null &&
        _stellarResonanceOverlay is null &&
        _mainStoryEndingOverlay is null &&
        _settingsOverlay is null &&
        _gleamriseSeasonOverlay is null &&
        _farmingSpecializationOverlay is null &&
        _festivalShowcaseOverlay is null &&
        _festivalShopOverlay is null &&
        _gleamrisePlantingOverlay is null &&
        _gleamriseSeedExchangeOverlay is null &&
        _longnightFeastOverlay is null &&
        _longnightStallOverlay is null &&
        _fireflyTideOverlay is null &&
        _fireflyTideShopOverlay is null &&
        _fadeTransition is null;

    private void ShowTitle(string? noticeKey = null)
    {
        _playing = false;
        _paused = false;
        _titleLanguageOverridden = false;
        _mailPlaytest = false;
        ClearWorld();
        FreeUi(_hud);
        _hud = null;
        FreeUi(_pauseOverlay);
        _pauseOverlay = null;
        FreeUi(_dialogueOverlay);
        _dialogueOverlay = null;
        FreeUi(_completionOverlay);
        _completionOverlay = null;
        FreeUi(_shopOverlay);
        _shopOverlay = null;
        FreeUi(_processorOverlay);
        _processorOverlay = null;
        FreeUi(_shippingOverlay);
        _shippingOverlay = null;
        FreeUi(_commissionOverlay);
        _commissionOverlay = null;
        FreeUi(_constructionOverlay);
        _constructionOverlay = null;
        FreeUi(_livestockAutomationOverlay);
        _livestockAutomationOverlay = null;
        FreeUi(_compendiumOverlay);
        _compendiumOverlay = null;
        FreeUi(_mailOverlay);
        _mailOverlay = null;
        FreeUi(_starlightOverlay);
        _starlightOverlay = null;
        FreeUi(_craftingOverlay);
        _craftingOverlay = null;
        FreeUi(_kitchenOverlay);
        _kitchenOverlay = null;
        FreeUi(_ingredientPantryOverlay);
        _ingredientPantryOverlay = null;
        FreeUi(_cookedDishOverlay);
        _cookedDishOverlay = null;
        FreeUi(_storageOverlay);
        _storageOverlay = null;
        FreeUi(_nightlySummaryOverlay);
        _nightlySummaryOverlay = null;
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
        FreeUi(_fishingCollectionOverlay);
        _fishingCollectionOverlay = null;
        FreeUi(_fishingDonationOverlay);
        _fishingDonationOverlay = null;
        FreeUi(_starGateOverlay);
        _starGateOverlay = null;
        FreeUi(_stellarResonanceOverlay);
        _stellarResonanceOverlay = null;
        FreeUi(_mainStoryEndingOverlay);
        _mainStoryEndingOverlay = null;
        FreeUi(_settingsOverlay);
        _settingsOverlay = null;
        FreeUi(_gleamriseSeasonOverlay);
        _gleamriseSeasonOverlay = null;
        FreeUi(_farmingSpecializationOverlay);
        _farmingSpecializationOverlay = null;
        FreeUi(_festivalShowcaseOverlay);
        _festivalShowcaseOverlay = null;
        FreeUi(_festivalShopOverlay);
        _festivalShopOverlay = null;
        FreeUi(_gleamrisePlantingOverlay);
        _gleamrisePlantingOverlay = null;
        FreeUi(_gleamriseSeedExchangeOverlay);
        _gleamriseSeedExchangeOverlay = null;
        FreeUi(_fadeTransition);
        _fadeTransition = null;
        FreeUi(_title);

        _title = new TitleMenu(
            _theme,
            _locale,
            _saveService,
            noticeKey is null ? null : _locale.Tr(noticeKey)
        );
        _uiLayer.AddChild(_title);
        _title.NewGameRequested += StartNewGame;
        _title.ContinueRequested += ContinueGame;
        _title.LanguageRequested += OpenSettings;
        _title.QuitRequested += () => GetTree().Quit();
    }

    private void StartNewGame()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private PlaytestScenarioRegistry CreatePlaytestScenarioRegistry() =>
        new(
            new Dictionary<PlaytestScenarioId, Action>
            {
                [PlaytestScenarioId.Door] = StartDoorPlaytest,
                [PlaytestScenarioId.Cottage] = StartCottagePlaytest,
                [PlaytestScenarioId.CottageUpgradeReady] =
                    StartCottageUpgradeReadyPlaytest,
                [PlaytestScenarioId.CottageUpgradeInProgress] =
                    StartCottageUpgradeInProgressPlaytest,
                [PlaytestScenarioId.CottageUpgradeCompleted] =
                    StartCottageUpgradeCompletedPlaytest,
                [PlaytestScenarioId.CottageSecondUpgradeReady] =
                    StartCottageSecondUpgradeReadyPlaytest,
                [PlaytestScenarioId.CottageSecondUpgradeInProgress] =
                    StartCottageSecondUpgradeInProgressPlaytest,
                [PlaytestScenarioId.CottageKitchen] =
                    StartCottageKitchenPlaytest,
                [PlaytestScenarioId.CottageKitchenPanel] =
                    StartCottageKitchenPanelPlaytest,
                [PlaytestScenarioId.CottagePantry] =
                    StartCottagePantryPlaytest,
                [PlaytestScenarioId.CottagePantryPanel] =
                    StartCottagePantryPanelPlaytest,
                [PlaytestScenarioId.CottageMealsEnglish] =
                    StartCottageMealsEnglishPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopReady] =
                    StartHomesteadWorkshopReadyPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopInProgress] =
                    StartHomesteadWorkshopInProgressPlaytest,
                [PlaytestScenarioId.HomesteadWorkshopCompleted] =
                    StartHomesteadWorkshopCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseReady] =
                    StartGreenhouseReadyPlaytest,
                [PlaytestScenarioId.GreenhouseInProgress] =
                    StartGreenhouseInProgressPlaytest,
                [PlaytestScenarioId.GreenhouseExteriorCompleted] =
                    StartGreenhouseExteriorCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseCompleted] =
                    StartGreenhouseCompletedPlaytest,
                [PlaytestScenarioId.GreenhouseCistern] =
                    StartGreenhouseCisternPlaytest,
                [PlaytestScenarioId.StarfeatherCoopReady] =
                    StartStarfeatherCoopReadyPlaytest,
                [PlaytestScenarioId.StarfeatherCoopInProgress] =
                    StartStarfeatherCoopInProgressPlaytest,
                [PlaytestScenarioId.StarfeatherCoopGrazing] =
                    StartStarfeatherCoopGrazingPlaytest,
                [PlaytestScenarioId.StarfeatherCoopChick] =
                    StartStarfeatherCoopChickPlaytest,
                [PlaytestScenarioId.StarfeatherCoopAdult] =
                    StartStarfeatherCoopAdultPlaytest,
                [PlaytestScenarioId.StarfeatherCoopNestBlockedEnglish] =
                    StartStarfeatherCoopNestBlockedEnglishPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnReady] =
                    StartMoonfleeceBarnReadyPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnInProgress] =
                    StartMoonfleeceBarnInProgressPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnGrazing] =
                    StartMoonfleeceBarnGrazingPlaytest,
                [PlaytestScenarioId.MoonfleeceBarnJuvenile] =
                    StartMoonfleeceBarnJuvenilePlaytest,
                [PlaytestScenarioId.MoonfleeceBarnRackBlockedEnglish] =
                    StartMoonfleeceBarnRackBlockedEnglishPlaytest,
                [PlaytestScenarioId.DewhornGrazing] =
                    StartDewhornGrazingPlaytest,
                [PlaytestScenarioId.DewhornMilkingBlockedEnglish] =
                    StartDewhornMilkingBlockedEnglishPlaytest,
                [PlaytestScenarioId.LivestockAutomationConsole] =
                    StartLivestockAutomationConsolePlaytest,
                [PlaytestScenarioId.LivestockAutomationPanel] =
                    StartLivestockAutomationPanelPlaytest,
                [PlaytestScenarioId.LivestockAutomationPanelEnglish] =
                    StartLivestockAutomationPanelEnglishPlaytest,
                [PlaytestScenarioId.LivestockAutomationConstruction] =
                    StartLivestockAutomationConstructionPlaytest,
                [PlaytestScenarioId.Crops] = StartCropPlaytest,
                [PlaytestScenarioId.GleamriseCrops] =
                    StartGleamriseCropPlaytest,
                [PlaytestScenarioId.GleamriseSeason] =
                    StartGleamriseSeasonPlaytest,
                [PlaytestScenarioId.RainveilCrops] =
                    StartRainveilCropPlaytest,
                [PlaytestScenarioId.StarharvestCrops] =
                    StartStarharvestCropPlaytest,
                [PlaytestScenarioId.StarharvestMarketGate] =
                    StartStarharvestMarketGatePlaytest,
                [PlaytestScenarioId.StarharvestMarket] =
                    StartStarharvestMarketPlaytest,
                [PlaytestScenarioId.StarharvestMarketShowcase] =
                    StartStarharvestMarketShowcasePlaytest,
                [PlaytestScenarioId.StarharvestMarketResult] =
                    StartStarharvestMarketResultPlaytest,
                [PlaytestScenarioId.StarharvestMarketShop] =
                    StartStarharvestMarketShopPlaytest,
                [PlaytestScenarioId.StarharvestMarketShowcaseEnglish] =
                    StartStarharvestMarketShowcaseEnglishPlaytest,
                [PlaytestScenarioId.GleamriseFestivalGate] =
                    StartGleamriseFestivalGatePlaytest,
                [PlaytestScenarioId.GleamriseFestival] =
                    StartGleamriseFestivalPlaytest,
                [PlaytestScenarioId.GleamriseFestivalChallenge] =
                    StartGleamriseFestivalChallengePlaytest,
                [PlaytestScenarioId.GleamriseFestivalResult] =
                    StartGleamriseFestivalResultPlaytest,
                [PlaytestScenarioId.GleamriseFestivalExchange] =
                    StartGleamriseFestivalExchangePlaytest,
                [PlaytestScenarioId.GleamriseFestivalChallengeEnglish] =
                    StartGleamriseFestivalChallengeEnglishPlaytest,
                [PlaytestScenarioId.LongnightFeastGate] =
                    StartLongnightFeastGatePlaytest,
                [PlaytestScenarioId.LongnightFeast] =
                    StartLongnightFeastPlaytest,
                [PlaytestScenarioId.LongnightFeastActivity] =
                    StartLongnightFeastActivityPlaytest,
                [PlaytestScenarioId.LongnightFeastResult] =
                    StartLongnightFeastResultPlaytest,
                [PlaytestScenarioId.LongnightFeastStall] =
                    StartLongnightFeastStallPlaytest,
                [PlaytestScenarioId.LongnightFeastActivityEnglish] =
                    StartLongnightFeastActivityEnglishPlaytest,
                [PlaytestScenarioId.LongnightFeastWrongTool] =
                    StartLongnightFeastWrongToolPlaytest,
                [PlaytestScenarioId.FireflyTideGate] =
                    StartFireflyTideGatePlaytest,
                [PlaytestScenarioId.FireflyTide] =
                    StartFireflyTidePlaytest,
                [PlaytestScenarioId.FireflyTideActivity] =
                    StartFireflyTideActivityPlaytest,
                [PlaytestScenarioId.FireflyTideResult] =
                    StartFireflyTideResultPlaytest,
                [PlaytestScenarioId.FireflyTideShop] =
                    StartFireflyTideShopPlaytest,
                [PlaytestScenarioId.FireflyTideActivityEnglish] =
                    StartFireflyTideActivityEnglishPlaytest,
                [PlaytestScenarioId.FireflyTideWrongTool] =
                    StartFireflyTideWrongToolPlaytest,
                [PlaytestScenarioId.LongnightHomestead] =
                    StartLongnightHomesteadPlaytest,
                [PlaytestScenarioId.LongnightEmporium] =
                    StartLongnightEmporiumPlaytest,
                [PlaytestScenarioId.LongnightSnowForecast] =
                    StartLongnightSnowForecastPlaytest,
                [PlaytestScenarioId.LongnightSnow] =
                    StartLongnightSnowPlaytest,
                [PlaytestScenarioId.LongnightSnowIndoor] =
                    StartLongnightSnowIndoorPlaytest,
                [PlaytestScenarioId.LongnightSnowClear] =
                    StartLongnightSnowClearPlaytest,
                [PlaytestScenarioId.Economy] = StartEconomyPlaytest,
                [PlaytestScenarioId.Processor] = StartProcessorPlaytest,
                [PlaytestScenarioId.MultiProcessorBatch] =
                    StartMultiProcessorBatchPlaytest,
                [PlaytestScenarioId.MoonpearlEggPress] =
                    StartMoonpearlEggPressPlaytest,
                [PlaytestScenarioId.ArchiveGift] = StartArchiveGiftPlaytest,
                [PlaytestScenarioId.Archive] = StartArchivePlaytest,
                [PlaytestScenarioId.ArchiveDoor] = StartArchiveDoorPlaytest,
                [PlaytestScenarioId.CropCodexDesk] =
                    StartCropCodexDeskPlaytest,
                [PlaytestScenarioId.CropCodexPartial] =
                    StartCropCodexPartialPlaytest,
                [PlaytestScenarioId.CropCodexRewardReady] =
                    StartCropCodexRewardReadyPlaytest,
                [PlaytestScenarioId.CropCodexRewardClaimedEnglish] =
                    StartCropCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.CropCodexWrongTool] =
                    StartCropCodexWrongToolPlaytest,
                [PlaytestScenarioId.CropCodexDiscountShop] =
                    StartCropCodexDiscountShopPlaytest,
                [PlaytestScenarioId.CookingCodexUnknown] =
                    StartCookingCodexUnknownPlaytest,
                [PlaytestScenarioId.CookingCodexPartial] =
                    StartCookingCodexPartialPlaytest,
                [PlaytestScenarioId.CookingCodexRewardReady] =
                    StartCookingCodexRewardReadyPlaytest,
                [PlaytestScenarioId.CookingCodexRewardClaimedEnglish] =
                    StartCookingCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.CookingCodexRewardMealsEnglish] =
                    StartCookingCodexRewardMealsEnglishPlaytest,
                [PlaytestScenarioId.ArtisanCodexUnknown] =
                    StartArtisanCodexUnknownPlaytest,
                [PlaytestScenarioId.ArtisanCodexPartial] =
                    StartArtisanCodexPartialPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardReady] =
                    StartArtisanCodexRewardReadyPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardClaimedEnglish] =
                    StartArtisanCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.ArtisanCodexRewardShippingEnglish] =
                    StartArtisanCodexRewardShippingEnglishPlaytest,
                [PlaytestScenarioId.SeasonalForage] =
                    StartSeasonalForagePlaytest,
                [PlaytestScenarioId.SeasonalForageWrongTool] =
                    StartSeasonalForageWrongToolPlaytest,
                [PlaytestScenarioId.SeasonalForageStardustMap] =
                    StartSeasonalForageStardustMapPlaytest,
                [PlaytestScenarioId.ForageCodexPartial] =
                    StartForageCodexPartialPlaytest,
                [PlaytestScenarioId.ForageCodexRewardReady] =
                    StartForageCodexRewardReadyPlaytest,
                [PlaytestScenarioId.ForageCodexRewardClaimedEnglish] =
                    StartForageCodexRewardClaimedEnglishPlaytest,
                [PlaytestScenarioId.Fishing] = StartFishingPlaytest,
                [PlaytestScenarioId.FishingMinigame] =
                    StartFishingMinigamePlaytest,
                [PlaytestScenarioId.FishingGear] =
                    StartFishingGearPlaytest,
                [PlaytestScenarioId.FishingCollection] =
                    StartFishingCollectionPlaytest,
                [PlaytestScenarioId.FishingDonation] =
                    StartFishingDonationPlaytest,
                [PlaytestScenarioId.FishCodexPartial] =
                    StartFishCodexPartialPlaytest,
                [PlaytestScenarioId.FishCodexCompleteEnglish] =
                    StartFishCodexCompleteEnglishPlaytest,
                [PlaytestScenarioId.CrystalGrottoEntry] =
                    StartCrystalGrottoEntryPlaytest,
                [PlaytestScenarioId.CrystalGrottoBasic] =
                    StartCrystalGrottoBasicPlaytest,
                [PlaytestScenarioId.CrystalGrottoUpgrade] =
                    StartCrystalGrottoUpgradePlaytest,
                [PlaytestScenarioId.CrystalGrottoDeep] =
                    StartCrystalGrottoDeepPlaytest,
                [PlaytestScenarioId.DeepMine] = StartDeepMinePlaytest,
                [PlaytestScenarioId.MineralCodexCompleteEnglish] =
                    StartMineralCodexCompleteEnglishPlaytest,
                [PlaytestScenarioId.CrystalValeStarlightPanel] =
                    StartCrystalValeStarlightPanelPlaytest,
                [PlaytestScenarioId.CrystalValeStarlightRestored] =
                    StartCrystalValeStarlightRestoredPlaytest,
                [PlaytestScenarioId.StarfallRuinsEntry] =
                    StartStarfallRuinsEntryPlaytest,
                [PlaytestScenarioId.StarfallRuinsCombat] =
                    StartStarfallRuinsCombatPlaytest,
                [PlaytestScenarioId.StarfallRuinsArtifacts] =
                    StartStarfallRuinsArtifactsPlaytest,
                [PlaytestScenarioId.ArtifactCodexDonationEnglish] =
                    StartArtifactCodexDonationEnglishPlaytest,
                [PlaytestScenarioId.StarfallRuinsStarlightPanel] =
                    StartStarfallRuinsStarlightPanelPlaytest,
                [PlaytestScenarioId.StarfallRuinsStarlightRestored] =
                    StartStarfallRuinsStarlightRestoredPlaytest,
                [PlaytestScenarioId.SixfoldStarGate] =
                    StartSixfoldStarGatePlaytest,
                [PlaytestScenarioId.SixfoldStarGatePanel] =
                    StartSixfoldStarGatePanelPlaytest,
                [PlaytestScenarioId.StellarConvergence] =
                    StartStellarConvergencePlaytest,
                [PlaytestScenarioId.AccessibilitySettings] =
                    StartAccessibilitySettingsPlaytest,
                [PlaytestScenarioId.LioraEventOne] =
                    StartLioraEventOnePlaytest,
                [PlaytestScenarioId.LioraEventTwo] =
                    StartLioraEventTwoPlaytest,
                [PlaytestScenarioId.TaviEventOne] =
                    StartTaviEventOnePlaytest,
                [PlaytestScenarioId.TaviEventTwo] =
                    StartTaviEventTwoPlaytest,
                [PlaytestScenarioId.NemiEventOne] =
                    StartNemiEventOnePlaytest,
                [PlaytestScenarioId.NemiEventTwo] =
                    StartNemiEventTwoPlaytest,
                [PlaytestScenarioId.KaelEventOne] =
                    StartKaelEventOnePlaytest,
                [PlaytestScenarioId.KaelEventTwo] =
                    StartKaelEventTwoPlaytest,
                [PlaytestScenarioId.SelaEventOne] =
                    StartSelaEventOnePlaytest,
                [PlaytestScenarioId.SelaEventTwo] =
                    StartSelaEventTwoPlaytest,
                [PlaytestScenarioId.OrinEventOne] =
                    StartOrinEventOnePlaytest,
                [PlaytestScenarioId.OrinEventTwo] =
                    StartOrinEventTwoPlaytest,
                [PlaytestScenarioId.ElowenEventOne] =
                    StartElowenEventOnePlaytest,
                [PlaytestScenarioId.ElowenEventTwo] =
                    StartElowenEventTwoPlaytest,
                [PlaytestScenarioId.VessaEventOne] =
                    StartVessaEventOnePlaytest,
                [PlaytestScenarioId.VessaEventTwo] =
                    StartVessaEventTwoPlaytest,
                [PlaytestScenarioId.VessaEventWrongTool] =
                    StartVessaEventWrongToolPlaytest,
                [PlaytestScenarioId.RelationshipMailsEnglish] =
                    StartRelationshipMailsEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3] =
                    StartVillageExpansionWave3Playtest,
                [PlaytestScenarioId.VillageExpansionWave3Indoor] =
                    StartVillageExpansionWave3IndoorPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3DialogueEnglish] =
                    StartVillageExpansionWave3DialogueEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWave3WrongTool] =
                    StartVillageExpansionWave3WrongToolPlaytest,
                [PlaytestScenarioId.YvaraEventOne] =
                    StartYvaraEventOnePlaytest,
                [PlaytestScenarioId.YvaraEventTwo] =
                    StartYvaraEventTwoPlaytest,
                [PlaytestScenarioId.Wave3RelationshipMailsEnglish] =
                    StartWave3RelationshipMailsEnglishPlaytest,
                [PlaytestScenarioId.WorkshopTavi] =
                    StartWorkshopTaviPlaytest,
                [PlaytestScenarioId.Workshop] = StartWorkshopPlaytest,
                [PlaytestScenarioId.WorkshopDoor] =
                    StartWorkshopDoorPlaytest,
                [PlaytestScenarioId.TeaHouseVessa] =
                    StartTeaHouseVessaPlaytest,
                [PlaytestScenarioId.TeaHouse] =
                    StartTeaHousePlaytest,
                [PlaytestScenarioId.TeaHouseDoor] =
                    StartTeaHouseDoorPlaytest,
                [PlaytestScenarioId.EmporiumOrin] =
                    StartEmporiumOrinPlaytest,
                [PlaytestScenarioId.Emporium] =
                    StartEmporiumPlaytest,
                [PlaytestScenarioId.EmporiumDoor] =
                    StartEmporiumDoorPlaytest,
                [PlaytestScenarioId.EmporiumRotation] =
                    StartEmporiumRotationPlaytest,
                [PlaytestScenarioId.EmporiumRestdayDoor] =
                    StartEmporiumRestdayDoorPlaytest,
                [PlaytestScenarioId.StarlightPostNemi] =
                    StartStarlightPostNemiPlaytest,
                [PlaytestScenarioId.StarlightPost] =
                    StartStarlightPostPlaytest,
                [PlaytestScenarioId.StarlightPostWrongTool] =
                    StartStarlightPostWrongToolPlaytest,
                [PlaytestScenarioId.StarlightPostDoor] =
                    StartStarlightPostDoorPlaytest,
                [PlaytestScenarioId.StarfallWatchKael] =
                    StartStarfallWatchKaelPlaytest,
                [PlaytestScenarioId.StarfallWatch] =
                    StartStarfallWatchPlaytest,
                [PlaytestScenarioId.StarfallWatchWrongTool] =
                    StartStarfallWatchWrongToolPlaytest,
                [PlaytestScenarioId.StarfallWatchDoor] =
                    StartStarfallWatchDoorPlaytest,
                [PlaytestScenarioId.VillageDialogue] =
                    StartVillageDialoguePlaytest,
                [PlaytestScenarioId.SelaDialogue] =
                    StartSelaDialoguePlaytest,
                [PlaytestScenarioId.VillageExpansion] =
                    StartVillageExpansionPlaytest,
                [PlaytestScenarioId.VillageExpansionArchive] =
                    StartVillageExpansionArchivePlaytest,
                [PlaytestScenarioId.VillageExpansionDialogueEnglish] =
                    StartVillageExpansionDialogueEnglishPlaytest,
                [PlaytestScenarioId.VillageExpansionWrongTool] =
                    StartVillageExpansionWrongToolPlaytest,
                [PlaytestScenarioId.NpcPathfinding] =
                    StartNpcPathfindingPlaytest,
                [PlaytestScenarioId.VillageRestdayEnglish] =
                    StartVillageRestdayEnglishPlaytest,
                [PlaytestScenarioId.VillageRainSchedule] =
                    StartVillageRainSchedulePlaytest,
                [PlaytestScenarioId.VillageRainveilSchedule] =
                    StartVillageRainveilSchedulePlaytest,
                [PlaytestScenarioId.Village] = StartVillagePlaytest,
                [PlaytestScenarioId.WorldAspectBoundary] =
                    StartWorldAspectBoundaryPlaytest,
                [PlaytestScenarioId.RainveilWorldAspect] =
                    StartRainveilWorldAspectPlaytest,
                [PlaytestScenarioId.StarharvestWorldAspect] =
                    StartStarharvestWorldAspectPlaytest,
                [PlaytestScenarioId.LongnightWorldAspect] =
                    StartLongnightWorldAspectPlaytest,
                [PlaytestScenarioId.RainveilWorldTreeRain] =
                    StartRainveilWorldTreeRainPlaytest,
                [PlaytestScenarioId.StarharvestWorldCrystalStardust] =
                    StartStarharvestWorldCrystalStardustPlaytest,
                [PlaytestScenarioId.WorldBeginnerArch] =
                    StartWorldBeginnerArchPlaytest,
                [PlaytestScenarioId.WorldWoodsGrove] =
                    StartWorldWoodsGrovePlaytest,
                [PlaytestScenarioId.WorldMeadowCircle] =
                    StartWorldMeadowCirclePlaytest,
                [PlaytestScenarioId.WorldCrystalRidge] =
                    StartWorldCrystalRidgePlaytest,
                [PlaytestScenarioId.WorldWetlandIslet] =
                    StartWorldWetlandIsletPlaytest,
                [PlaytestScenarioId.WorldRuinsColonnade] =
                    StartWorldRuinsColonnadePlaytest,
                [PlaytestScenarioId.WorldFacilitiesGateway] =
                    StartWorldFacilitiesGatewayPlaytest,
                [PlaytestScenarioId.World] = StartWorldPlaytest,
                [PlaytestScenarioId.Gate] = StartGatePlaytest,
                [PlaytestScenarioId.Backpack] = StartBackpackPlaytest,
                [PlaytestScenarioId.Resource] = StartResourcePlaytest,
                [PlaytestScenarioId.Target] = StartTargetPreviewPlaytest,
                [PlaytestScenarioId.PhaseA] = StartPhaseAPlaytest,
                [PlaytestScenarioId.PhaseASummary] =
                    StartPhaseASummaryPlaytest,
                [PlaytestScenarioId.PhaseARain] =
                    StartPhaseARainPlaytest,
                [PlaytestScenarioId.ResourceRespawn] =
                    StartResourceRespawnPlaytest,
                [PlaytestScenarioId.Crafting] = StartCraftingPlaytest,
                [PlaytestScenarioId.Placeables] =
                    StartFarmPlaceablesPlaytest,
                [PlaytestScenarioId.ChestPlacement] =
                    StartChestPlacementPlaytest,
                [PlaytestScenarioId.Storage] = StartStoragePlaytest,
                [PlaytestScenarioId.CommissionOffer] =
                    StartCommissionOfferPlaytest,
                [PlaytestScenarioId.CommissionReady] =
                    StartCommissionReadyPlaytest,
                [PlaytestScenarioId.CommissionReadyEnglish] =
                    StartCommissionReadyEnglishPlaytest,
                [PlaytestScenarioId.CommissionMap] =
                    StartCommissionMapPlaytest,
                [PlaytestScenarioId.WeeklyCommissionOffer] =
                    StartWeeklyCommissionOfferPlaytest,
                [PlaytestScenarioId.WeeklyCommissionStageReady] =
                    StartWeeklyCommissionStageReadyPlaytest,
                [PlaytestScenarioId.WeeklyCommissionRewardReady] =
                    StartWeeklyCommissionRewardReadyPlaytest,
                [PlaytestScenarioId.WeeklyCommissionMap] =
                    StartWeeklyCommissionMapPlaytest,
                [PlaytestScenarioId.MailboxUnread] =
                    StartMailboxUnreadPlaytest,
                [PlaytestScenarioId.MailPanel] =
                    StartMailPanelPlaytest,
                [PlaytestScenarioId.MailReward] =
                    StartMailRewardPlaytest,
                [PlaytestScenarioId.StarlightMap] =
                    StartStarlightMapPlaytest,
                [PlaytestScenarioId.StarlightMapRestored] =
                    StartStarlightRestoredMapPlaytest,
                [PlaytestScenarioId.StarlightPanel] =
                    StartStarlightPanelPlaytest,
                [PlaytestScenarioId.StarlightRestored] =
                    StartStarlightRestoredPlaytest,
                [PlaytestScenarioId.StarlightRestoredEnglish] =
                    StartStarlightRestoredEnglishPlaytest,
                [PlaytestScenarioId.HomesteadStarlightDormant] =
                    StartHomesteadStarlightDormantPlaytest,
                [PlaytestScenarioId.HomesteadStarlightWrongTool] =
                    StartHomesteadStarlightWrongToolPlaytest,
                [PlaytestScenarioId.HomesteadStarlightRestored] =
                    StartHomesteadStarlightRestoredPlaytest,
                [PlaytestScenarioId.HomesteadStarlightPanel] =
                    StartHomesteadStarlightPanelPlaytest,
                [PlaytestScenarioId.HomesteadStarlightPanelEnglish] =
                    StartHomesteadStarlightPanelEnglishPlaytest,
                [PlaytestScenarioId.MeadowStarlightDormant] =
                    StartMeadowStarlightDormantPlaytest,
                [PlaytestScenarioId.MeadowStarlightRestored] =
                    StartMeadowStarlightRestoredPlaytest,
                [PlaytestScenarioId.MeadowStarlightPanel] =
                    StartMeadowStarlightPanelPlaytest,
                [PlaytestScenarioId.MeadowStarlightPanelEnglish] =
                    StartMeadowStarlightPanelEnglishPlaytest,
                [PlaytestScenarioId.MeadowPollination] =
                    StartMeadowPollinationPlaytest,
                [PlaytestScenarioId.MoonwaterStarlightPanel] =
                    StartMoonwaterStarlightPanelPlaytest,
                [PlaytestScenarioId.QualityCrafting] =
                    StartQualityCraftingPlaytest,
                [PlaytestScenarioId.QualityBackpackEnglish] =
                    StartQualityBackpackEnglishPlaytest,
                [PlaytestScenarioId.QualityBackpack] =
                    StartQualityBackpackPlaytest,
                [PlaytestScenarioId.Quality] = StartQualityPlaytest,
                [PlaytestScenarioId.OrchardHives] =
                    StartOrchardHivesPlaytest,
                [PlaytestScenarioId.FarmingSpecialization] =
                    StartFarmingSpecializationPlaytest,
                [PlaytestScenarioId.Farm] = StartNewGame
            }
        );

    private void StartProcessorPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 4);
        _session.Inventory.Add(DataCatalog.MoonrootId, 4);
        _session.SetPlayerState(
            FarmView.ProcessorCell.X * 16 + 8,
            (FarmView.ProcessorCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(
            () => OpenProcessor(ProcessorCatalog.MainMachineId)
        ).CallDeferred();
    }

    private void StartMultiProcessorBatchPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 2);
        _session.Inventory.Add(DataCatalog.MoonrootId, 2);
        _session.Inventory.Add(DataCatalog.CloudleafId, 3);
        _session.StartProcessing(
            ProcessorCatalog.MoonwellInfuserId,
            DataCatalog.MoonrootTonicRecipeId
        );
        _session.StartProcessing(
            ProcessorCatalog.PrismPreserveVatId,
            DataCatalog.StarbudPreserveRecipeId
        );
        _session.StartProcessing(
            ProcessorCatalog.StarweaveDryingLoomId,
            DataCatalog.CloudleafTeaRecipeId
        );
        _session.EndDay();
        _session.EndDay();
        var focus = ProcessorCatalog.Machine(
            ProcessorCatalog.PrismPreserveVatId
        ).Position;
        _session.SetPlayerState(
            focus.X * 16 + 8,
            (focus.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(
            () => OpenProcessor(ProcessorCatalog.PrismPreserveVatId)
        ).CallDeferred();
    }

    private void StartMoonpearlEggPressPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarfeatherEggId, 2);
        _session.StartProcessing(
            ProcessorCatalog.MoonpearlEggPressId,
            DataCatalog.StarfeatherCreamRecipeId
        );
        var focus = ProcessorCatalog.Machine(
            ProcessorCatalog.MoonpearlEggPressId
        ).Position;
        _session.SetPlayerState(
            focus.X * 16 + 8,
            (focus.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCottagePlaytest()
    {
        StartNewGame();
        ShowCottage(true);
    }

    private void StartCottageUpgradeReadyPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        Callable.From(OpenConstructionPanel).CallDeferred();
    }

    private void StartCottageUpgradeInProgressPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        _ = _session.StartCottageFirstUpgrade();
        Callable.From(OpenConstructionPanel).CallDeferred();
    }

    private void StartCottageUpgradeCompletedPlaytest()
    {
        PrepareCottageConstructionPlaytest();
        _ = _session.StartCottageFirstUpgrade();
        _session.EndDay();
        _session.EndDay();
        _session.SetPlayerLocation(
            27 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        ShowCottage(false);
    }

    private void StartCottageSecondUpgradeReadyPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest();
        _session.SetPlayerLocation(
            20 * 16 + 8,
            11 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.CottageSecondUpgradeId
            )
        ).CallDeferred();
    }

    private void StartCottageSecondUpgradeInProgressPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                RemainingNights = 3
            }
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
    }

    private void StartCottageKitchenPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
    }

    private void StartCottageKitchenPanelPlaytest()
    {
        StartCottageKitchenPlaytest();
        Callable.From(
            () => OpenKitchen(new GridPosition(29, 10))
        ).CallDeferred();
    }

    private void StartCottagePantryPanelPlaytest()
    {
        StartCottagePantryPlaytest();
        _session.Inventory.Select(0);
        Callable.From(
            () => OpenIngredientPantry(new GridPosition(34, 10))
        ).CallDeferred();
    }

    private void StartCottagePantryPlaytest()
    {
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        _session.SetPlayerLocation(
            34 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        _session.Inventory.Select(1);
        ShowCottage(false);
    }

    private void StartCottageMealsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
        Callable.From(OpenCookedDishes).CallDeferred();
    }

    private void PrepareCottageSecondUpgradePlaytest(
        ConstructionProjectSave? secondUpgrade = null
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        foreach (var (itemId, count) in new (string, int)[]
                 {
                     (DataCatalog.LumenwoodId, 32),
                     (DataCatalog.CrystalShardId, 14),
                     (DataCatalog.RipplecapId, 2),
                     (DataCatalog.MistsongMintId, 2),
                     (DataCatalog.DewhornMilkId, 2),
                     (DataCatalog.SunvaultGourdId, 2),
                     (DataCatalog.CometTuberId, 2),
                     (DataCatalog.MoonplumId, 2),
                     (DataCatalog.LanternReedId, 2),
                     (DataCatalog.MoonrootId, 2),
                     (DataCatalog.TideglassTaroId, 2),
                     (DataCatalog.MoonmistStewId, 1),
                     (DataCatalog.SunvaultHashId, 1),
                     (DataCatalog.StarhoneyCustardId, 1),
                     (DataCatalog.LanternrootBrothId, 1)
                 })
        {
            _session.Inventory.Add(itemId, count);
        }

        var projects = new List<ConstructionProjectSave>
        {
            new()
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                Completed = true
            },
            new()
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            }
        };
        if (secondUpgrade is not null)
        {
            projects.Add(secondUpgrade);
        }

        var save = _session.Capture();
        save.Day = 43;
        save.Coins = 960;
        save.Construction = new ConstructionSave { Projects = projects };
        save.Kitchen = new KitchenSave
        {
            PantryItems =
            [
                new InventorySlot
                {
                    ItemId = DataCatalog.StarhoneyId,
                    Count = 2
                },
                new InventorySlot
                {
                    ItemId = DataCatalog.StarfeatherEggId,
                    Count = 2
                }
            ]
        };
        save.Player.LocationId = PlayerLocationIds.Cottage;
        save.Player.X = 29 * 16 + 8;
        save.Player.Y = 9 * 16 + 8;
        save.Player.Energy = 35;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtCottageKitchen()
    {
        _session.SetPlayerLocation(
            29 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
    }

    private static ConstructionProjectSave
        CompletedCottageSecondUpgradeProject() => new()
        {
            ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
            Completed = true
        };

    private void PrepareCottageConstructionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 12);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 4);
        var save = _session.Capture();
        save.Coins = 240;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
    }

    private void StartHomesteadWorkshopReadyPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest();
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.HomesteadWorkshopProjectId
            )
        ).CallDeferred();
    }

    private void StartHomesteadWorkshopInProgressPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest(15);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        PositionAtHomesteadWorkbench();
        ShowFarm(false);
    }

    private void StartHomesteadWorkshopCompletedPlaytest()
    {
        PrepareHomesteadWorkshopPlaytest(29);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
        _session.EndDay();
        _session.EndDay();
        _session.EndDay();
        PositionAtHomesteadWorkbench();
        ShowFarm(false);
    }

    private void PrepareHomesteadWorkshopPlaytest(int day = 1)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 20);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 8);
        var save = _session.Capture();
        save.Day = day;
        save.Coins = 480;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtHomesteadWorkbench()
    {
        _session.SetPlayerLocation(
            FarmLayout.HomesteadWorkbenchCell.X * 16 + 8,
            (FarmLayout.HomesteadWorkbenchCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private void StartGreenhouseReadyPlaytest()
    {
        PrepareGreenhousePlaytest();
        ShowWorkshop(false);
        Callable.From(
            () => OpenConstructionOverlay(
                ConstructionCatalog.HomesteadGreenhouseProjectId
            )
        ).CallDeferred();
    }

    private void StartGreenhouseInProgressPlaytest()
    {
        PrepareGreenhousePlaytest(15);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        PositionAtGreenhouseDoor();
        ShowFarm(false);
    }

    private void StartGreenhouseCompletedPlaytest()
    {
        CompleteGreenhousePlaytestConstruction();
        RestoreGreenhouseShowcaseCrops();
        _session.SetPlayerLocation(
            12 * 16 + 8,
            6 * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        ShowGreenhouse(false);
    }

    private void StartGreenhouseExteriorCompletedPlaytest()
    {
        CompleteGreenhousePlaytestConstruction(29);
        PositionAtGreenhouseDoor();
        ShowFarm(false);
    }

    private void StartGreenhouseCisternPlaytest()
    {
        CompleteGreenhousePlaytestConstruction();
        RestoreGreenhouseShowcaseCrops();
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.Greenhouse;
        save.Player.X = GreenhouseLayout.CisternCell.X * 16 + 8;
        save.Player.Y = (GreenhouseLayout.CisternCell.Y - 1) * 16 + 8;
        save.Player.SelectedSlot = 4;
        save.Player.WateringCanWater = 6;
        _session.Restore(save);
        ShowGreenhouse(false);
    }

    private void CompleteGreenhousePlaytestConstruction(int day = 43)
    {
        PrepareGreenhousePlaytest(day);
        _ = _session.StartConstruction(
            ConstructionCatalog.HomesteadGreenhouseProjectId
        );
        for (var night = 0; night < 4; night++)
        {
            _session.EndDay();
        }
    }

    private void RestoreGreenhouseShowcaseCrops()
    {
        _session.GreenhouseFarm.Restore(
        [
            MatureCropState(12, 7, DataCatalog.DawnlaceId),
            CropState(13, 7, DataCatalog.RipplecapId, 1),
            MatureCropState(25, 7, DataCatalog.TideglassTaroId),
            CropState(26, 7, DataCatalog.LanternReedId, 2),
            MatureCropState(11, 12, DataCatalog.AuricShootId),
            CropState(12, 12, DataCatalog.SunvaultGourdId, 2),
            MatureCropState(26, 12, DataCatalog.CrownstarSaffronId),
            CropState(27, 12, DataCatalog.AmberthreadClusterId, 2)
        ]);
    }

    private void PrepareGreenhousePlaytest(int day = 1)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 28);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 12);
        var save = _session.Capture();
        save.Day = day;
        save.Coins = 720;
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadWorkshopProjectId,
                Completed = true
            }
        ];
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private void PositionAtGreenhouseDoor()
    {
        _session.SetPlayerLocation(
            FarmLayout.GreenhouseReturnCell.X * 16 + 8,
            FarmLayout.GreenhouseReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
    }

    private void StartStarfeatherCoopReadyPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 1,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            project: null,
            animal: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopInProgressPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 15,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            project: new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadStarfeatherCoopProjectId,
                RemainingNights = 2
            },
            animal: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopGrazingPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 2, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.StarfeatherCoopReturnCell
        );
        ShowFarm(false);
    }

    private void StartStarfeatherCoopChickPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 16,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 0, mood: 2),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void StartStarfeatherCoopAdultPlaytest()
    {
        PrepareStarfeatherCoopPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(ageNights: 2, mood: 5),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void StartStarfeatherCoopNestBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareStarfeatherCoopPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            project: CompletedStarfeatherCoopProject(),
            animal: StarfeatherChickenPlaytestState(
                ageNights: 2,
                mood: 4,
                pendingProductItemId: DataCatalog.StarfeatherEggLuminousId
            ),
            locationId: PlayerLocationIds.StarfeatherCoop,
            playerCell: new GridPosition(32, 11)
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.StarfeatherEggLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowStarfeatherCoop(false);
    }

    private void PrepareStarfeatherCoopPlaytest(
        int day,
        int minuteOfDay,
        string weatherId,
        ConstructionProjectSave? project,
        AnimalEntrySave? animal,
        string locationId,
        GridPosition playerCell
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Construction = new ConstructionSave
        {
            Projects = project is null ? [] : [project]
        };
        save.Animals = new AnimalSave
        {
            Animals = animal is null ? [] : [animal]
        };
        save.Player.LocationId = locationId;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static ConstructionProjectSave CompletedStarfeatherCoopProject() =>
        new()
        {
            ProjectId = ConstructionCatalog
                .HomesteadStarfeatherCoopProjectId,
            Completed = true
        };

    private static AnimalEntrySave StarfeatherChickenPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterStarfeatherChickenId,
        SpeciesId = AnimalCatalog.StarfeatherChickenId,
        BuildingId = AnimalCatalog.StarfeatherCoopId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private void StartMoonfleeceBarnReadyPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 1,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: null,
            sheep: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnInProgressPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 15,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            barnProject: new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog
                    .HomesteadMoonfleeceBarnProjectId,
                RemainingNights = 2
            },
            sheep: null,
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnGrazingPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: FarmLayout.MoonfleeceBarnReturnCell
        );
        ShowFarm(false);
    }

    private void StartMoonfleeceBarnJuvenilePlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 16,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.RainWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 0, mood: 2),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(19, 13)
        );
        _session.Inventory.Add(DataCatalog.MeadowFodderId, 4);
        _session.Inventory.PromoteToHotbar(DataCatalog.MeadowFodderId);
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartMoonfleeceBarnRackBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMoonfleeceBarnPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(
                ageNights: 3,
                mood: 5,
                pendingProductItemId: DataCatalog.MoonfleeceLuminousId
            ),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(31, 14)
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.MoonfleeceLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartDewhornGrazingPlaytest()
    {
        PrepareMoonfleeceBarnPlaytest(
            day: 29,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.ClearWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.World,
            playerCell: new GridPosition(40, 17),
            dewhorn: DewhornPlaytestState(ageNights: 4, mood: 4)
        );
        ShowFarm(false);
    }

    private void StartDewhornMilkingBlockedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMoonfleeceBarnPlaytest(
            day: 43,
            minuteOfDay: 10 * 60,
            weatherId: DataCatalog.LongnightSnowWeatherId,
            barnProject: CompletedMoonfleeceBarnProject(),
            sheep: MoonfleeceSheepPlaytestState(ageNights: 3, mood: 4),
            locationId: PlayerLocationIds.MoonfleeceBarn,
            playerCell: new GridPosition(31, 18),
            dewhorn: DewhornPlaytestState(
                ageNights: 4,
                mood: 5,
                pendingProductItemId: DataCatalog.DewhornMilkLuminousId
            )
        );
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId =>
                         itemId != DataCatalog.DewhornMilkLuminousId
                     )
                     .Distinct(StringComparer.Ordinal)
                     .Take(19))
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.Inventory.Select(0);
        ShowMoonfleeceBarn(false);
    }

    private void StartLivestockAutomationConsolePlaytest()
    {
        PrepareLivestockAutomationPlaytest(
            AnimalCatalog.StarfeatherCoopId,
            new AnimalBuildingAutomationSave
            {
                BuildingId = AnimalCatalog.StarfeatherCoopId,
                StoredFeed = 28,
                StoredProducts =
                [
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.StarfeatherEggLuminousId,
                        Count = 4
                    }
                ]
            }
        );
        ShowStarfeatherCoop(false);
    }

    private void StartLivestockAutomationPanelPlaytest()
    {
        PrepareLivestockAutomationPlaytest(
            AnimalCatalog.MoonfleeceBarnId,
            new AnimalBuildingAutomationSave
            {
                BuildingId = AnimalCatalog.MoonfleeceBarnId,
                StoredFeed = 28,
                StoredProducts =
                [
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.MoonfleeceLuminousId,
                        Count = 6
                    },
                    new ShippingEntrySave
                    {
                        ItemId = DataCatalog.DewhornMilkStarlightId,
                        Count = 6
                    }
                ],
                LastResolvedDay = 43,
                LastAutoFedCount = 2,
                LastAutoCollectedCount = 2,
                LastFeedStatusId = AnimalAutomationStatusIds.Succeeded,
                LastCollectionStatusId =
                    AnimalAutomationStatusIds.Succeeded
            }
        );
        ShowMoonfleeceBarn(false);
        Callable.From(() => OpenLivestockAutomation(
            AnimalCatalog.MoonfleeceBarnId,
            MoonfleeceBarnLayout.AutomationStationCell
        )).CallDeferred();
    }

    private void StartLivestockAutomationPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartLivestockAutomationPanelPlaytest();
    }

    private void StartLivestockAutomationConstructionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 24);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 16);
        var save = _session.Capture();
        save.Coins = 900;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 11 * 16 + 8;
        save.Construction = new ConstructionSave
        {
            Projects = LivestockAutomationPrerequisiteProjects()
        };
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        Callable.From(() => OpenConstructionOverlay(
            ConstructionCatalog.HomesteadLivestockAutomationProjectId
        )).CallDeferred();
    }

    private void PrepareLivestockAutomationPlaytest(
        string buildingId,
        AnimalBuildingAutomationSave automation
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var spatial = AnimalBuildingSpatialCatalog.Definitions.Single(
            definition => definition.BuildingId == buildingId
        );
        var save = _session.Capture();
        save.Day = 43;
        save.MinuteOfDay = 10 * 60;
        save.Weather = new WeatherSave
        {
            Day = 43,
            CurrentId = DataCatalog.LongnightSnowWeatherId,
            ForecastId = DataCatalog.LongnightSnowWeatherId
        };
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                .. LivestockAutomationPrerequisiteProjects(),
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadLivestockAutomationProjectId,
                    Completed = true
                }
            ]
        };
        save.Animals = new AnimalSave
        {
            Animals =
            [
                new AnimalEntrySave
                {
                    InstanceId = AnimalCatalog
                        .StarterStarfeatherChickenId,
                    SpeciesId = AnimalCatalog.StarfeatherChickenId,
                    BuildingId = AnimalCatalog.StarfeatherCoopId,
                    AgeNights = 2,
                    Mood = 4
                },
                MoonfleeceSheepPlaytestState(3, 4),
                DewhornPlaytestState(4, 4)
            ],
            Automation = [automation]
        };
        save.Player.LocationId = spatial.LocationId;
        save.Player.X = spatial.AutomationStationCell.X * 16 + 8;
        save.Player.Y = (spatial.AutomationStationCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static List<ConstructionProjectSave>
        LivestockAutomationPrerequisiteProjects() =>
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            },
            CompletedStarfeatherCoopProject(),
            CompletedMoonfleeceBarnProject()
        ];

    private void PrepareMoonfleeceBarnPlaytest(
        int day,
        int minuteOfDay,
        string weatherId,
        ConstructionProjectSave? barnProject,
        AnimalEntrySave? sheep,
        string locationId,
        GridPosition playerCell,
        AnimalEntrySave? dewhorn = null
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var projects = new List<ConstructionProjectSave>
        {
            CompletedStarfeatherCoopProject()
        };
        if (barnProject is not null)
        {
            projects.Add(barnProject);
        }

        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Construction = new ConstructionSave { Projects = projects };
        save.Animals = new AnimalSave { Animals = [] };
        if (sheep is not null)
        {
            save.Animals.Animals.Add(sheep);
        }
        if (dewhorn is not null)
        {
            save.Animals.Animals.Add(dewhorn);
        }
        save.Player.LocationId = locationId;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
    }

    private static ConstructionProjectSave CompletedMoonfleeceBarnProject() =>
        new()
        {
            ProjectId = ConstructionCatalog.HomesteadMoonfleeceBarnProjectId,
            Completed = true
        };

    private static AnimalEntrySave MoonfleeceSheepPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterMoonfleeceSheepId,
        SpeciesId = AnimalCatalog.MoonfleeceSheepId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private static AnimalEntrySave DewhornPlaytestState(
        int ageNights,
        int mood,
        string pendingProductItemId = ""
    ) => new()
    {
        InstanceId = AnimalCatalog.StarterDewhornId,
        SpeciesId = AnimalCatalog.DewhornId,
        BuildingId = AnimalCatalog.MoonfleeceBarnId,
        AgeNights = ageNights,
        Mood = mood,
        PendingProductItemId = pendingProductItemId
    };

    private static FarmTileState MatureCropState(
        int x,
        int y,
        string cropId
    ) => CropState(
        x,
        y,
        cropId,
        DataCatalog.Crop(cropId).MatureAfterWateredNights
    );

    private void StartDoorPlaytest()
    {
        StartNewGame();
        _session.SetPlayerState(
            FarmView.CottageDoorCell.X * 16 + 8,
            (FarmView.CottageDoorCell.Y + 1) * 16 + 8,
            false
        );
        ShowFarm(false);
    }

    private void StartCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var columns = Enumerable.Range(0, FarmSystem.MapWidth)
            .Where(x => FarmSystem.IsPlantingBed(new GridPosition(x, 16)))
            .Take(DataCatalog.CropIds.Count)
            .ToArray();
        if (columns.Length != DataCatalog.CropIds.Count)
        {
            throw new InvalidOperationException(
                "The crop playtest requires one planting-bed column per crop."
            );
        }
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.CropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.CropIds[index]);
            crops.Add(CropState(columns[index], 16, crop.Id, crop.MatureAfterWateredNights));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }
        _session.Farm.Restore(crops);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGleamriseCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var cropIds = new[]
        {
            DataCatalog.DawnlaceId,
            DataCatalog.GlimmerpodId,
            DataCatalog.MistsongMintId,
            DataCatalog.CometTuberId
        };
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < cropIds.Length; index++)
        {
            var crop = DataCatalog.Crop(cropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGleamriseSeasonPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterBuyGleamriseSeed,
            4
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantGleamriseCrop,
            3
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterWaterGleamriseCrop,
            3
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterFertilizeGleamriseSoil
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestGleamriseCrop,
            2
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterStartProcessor
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterCollectProcessor,
            2
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterPlantMoonplumTree
        );
        _session.RecordGleamriseSeasonMilestone(
            GleamriseSeasonGoalSystem.CounterHarvestMoonplum
        );
        var save = _session.Capture();
        save.Day = CalendarSystem.DaysPerSeason;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = WeatherSystem.WeatherForDay(save.Day),
            ForecastId = WeatherSystem.WeatherForDay(save.Day + 1)
        };
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenGleamriseSeasonGoals).CallDeferred();
    }

    private void StartRainveilCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(15, 8 * 60);
        _session.Weather.AdvanceToDay(15);
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.RainveilCropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.RainveilCropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        foreach (var seedId in DataCatalog.RainveilSeedItemIds)
        {
            _session.Inventory.Add(seedId, 2);
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            columns[2] * 16 + 8,
            15 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartStarharvestCropPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(29, 8 * 60);
        _session.Weather.Restore(
            new WeatherSave
            {
                Day = 29,
                CurrentId = DataCatalog.ClearWeatherId,
                ForecastId = DataCatalog.ClearWeatherId
            },
            29
        );
        var columns = new[] { 12, 16, 22, 29 };
        var crops = new List<FarmTileState>();
        for (var index = 0; index < DataCatalog.StarharvestCropIds.Count; index++)
        {
            var crop = DataCatalog.Crop(DataCatalog.StarharvestCropIds[index]);
            crops.Add(CropState(
                columns[index],
                16,
                crop.Id,
                crop.MatureAfterWateredNights
            ));
            crops.Add(CropState(columns[index], 20, crop.Id, 0));
        }

        _session.Farm.Restore(crops);
        foreach (var seedId in DataCatalog.StarharvestSeedItemIds)
        {
            _session.Inventory.Add(seedId, 2);
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            columns[3] * 16 + 8,
            15 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartStarharvestMarketGatePlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.WorldReturnCell.X * 16 + 8,
            StarharvestMarketLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartStarharvestMarketPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
            StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
    }

    private void StartStarharvestMarketShowcasePlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        AddStarharvestMarketExamples();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ExhibitCell.X * 16 + 8,
            (StarharvestMarketLayout.ExhibitCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
        Callable.From(OpenFestivalShowcase).CallDeferred();
    }

    private void StartStarharvestMarketResultPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        AddStarharvestMarketExamples();
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ExhibitCell.X * 16 + 8,
            (StarharvestMarketLayout.ExhibitCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        _ = _session.SubmitFestivalExhibit(
        [
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId
        ]);
        ShowStarharvestMarket(false);
    }

    private void StartStarharvestMarketShopPlaytest()
    {
        PrepareStarharvestMarketPlaytest();
        _session.Festival.Restore(new FestivalSave { Scrip = 12 });
        _session.SetPlayerLocation(
            StarharvestMarketLayout.ShopCell.X * 16 + 8,
            (StarharvestMarketLayout.ShopCell.Y + 1) * 16 + 8,
            PlayerLocationIds.StarharvestMarket
        );
        ShowStarharvestMarket(false);
        Callable.From(OpenFestivalShop).CallDeferred();
    }

    private void StartStarharvestMarketShowcaseEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartStarharvestMarketShowcasePlaytest();
    }

    private void PrepareStarharvestMarketPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(39, 10 * 60);
        _session.Weather.AdvanceToDay(39);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddStarharvestMarketExamples()
    {
        foreach (var itemId in new[]
        {
            DataCatalog.AuricShootId,
            DataCatalog.SunvaultGourdId,
            DataCatalog.CrownstarSaffronId,
            DataCatalog.AuricShootStarlightId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartGleamriseFestivalGatePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.WorldReturnCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartGleamriseFestivalPlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.SetPlayerLocation(
            GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
            GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
    }

    private void StartGleamriseFestivalChallengePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(8);
        var plot = GleamrisePlantingFestivalLayout.PlotCells[8];
        _session.SetPlayerLocation(
            (plot.X - 1) * 16 + 8,
            plot.Y * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
    }

    private void StartGleamriseFestivalResultPlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(12);
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamrisePlanting).CallDeferred();
    }

    private void StartGleamriseFestivalExchangePlaytest()
    {
        PrepareGleamriseFestivalPlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.GleamriseBloomTokenId,
                    Balance = 10
                }
            ]
        });
        var exchange = GleamrisePlantingFestivalLayout.SeedExchangeCell;
        _session.SetPlayerLocation(
            exchange.X * 16 + 8,
            (exchange.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamriseSeedExchange).CallDeferred();
    }

    private void StartGleamriseFestivalChallengeEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareGleamriseFestivalPlaytest();
        PrepareGleamriseChallenge(5);
        var table = GleamrisePlantingFestivalLayout.ActivityTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.GleamrisePlantingFestival
        );
        ShowGleamrisePlantingFestival(false);
        Callable.From(OpenGleamrisePlanting).CallDeferred();
    }

    private void PrepareGleamriseFestivalPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(4, 9 * 60);
        _session.Weather.AdvanceToDay(4);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void PrepareGleamriseChallenge(int plantedCount)
    {
        var selected = new[]
        {
            DataCatalog.DawnlaceSeedId,
            DataCatalog.GlimmerpodSeedId,
            DataCatalog.MistsongMintSeedId
        };
        _ = _session.Festival.StartPlantingChallenge(
            CalendarSystem.YearNumber(_session.Clock.Day),
            _session.Clock.MinuteOfDay,
            selected
        );
        for (var index = 0; index < plantedCount; index++)
        {
            var seedId = selected[index % selected.Length];
            _ = _session.Festival.SelectPlantingSeed(1, seedId);
            _ = _session.Festival.PlantPlot(
                1,
                _session.Clock.MinuteOfDay + Math.Min(index * 5, 60),
                GleamrisePlantingFestivalLayout.PlotIds[index]
            );
        }

        _session.Clock.Reset(
            4,
            9 * 60 + Math.Min(Math.Max(0, plantedCount - 1) * 5, 60)
        );
    }

    private void StartLongnightFeastGatePlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.SetPlayerLocation(
            LongnightLanternFeastLayout.WorldReturnCell.X * 16 + 8,
            LongnightLanternFeastLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartLongnightFeastPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.SetPlayerLocation(
            LongnightLanternFeastLayout.SafeArrivalCell.X * 16 + 8,
            LongnightLanternFeastLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
    }

    private void StartLongnightFeastActivityPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        AddLongnightFeastExamples();
        var table = LongnightLanternFeastLayout.SharedTableCell;
        _session.SetPlayerLocation(
            table.X * 16 + 8,
            (table.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
        Callable.From(() => OpenLongnightFeast(table)).CallDeferred();
    }

    private void StartLongnightFeastResultPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        AddLongnightFeastExamples();
        var ritual = LongnightLanternFeastLayout.RitualCell;
        _session.SetPlayerLocation(
            ritual.X * 16 + 8,
            (ritual.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        _ = _session.CompleteLongnightFeast(
            ritual,
            [DataCatalog.MoonmistStewId, DataCatalog.StarhoneyCustardId],
            FestivalCatalog.LongnightCloudleafTeaExchangeId
        );
        ShowLongnightLanternFeast(false);
    }

    private void StartLongnightFeastStallPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.LongnightLanternKnotId,
                    Balance = 10
                }
            ]
        });
        var stall = LongnightLanternFeastLayout.StallCell;
        _session.SetPlayerLocation(
            stall.X * 16 + 8,
            (stall.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        ShowLongnightLanternFeast(false);
        Callable.From(OpenLongnightStall).CallDeferred();
    }

    private void StartLongnightFeastActivityEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartLongnightFeastActivityPlaytest();
    }

    private void StartLongnightFeastWrongToolPlaytest()
    {
        PrepareLongnightFeastPlaytest();
        var ritual = LongnightLanternFeastLayout.RitualCell;
        _session.SetPlayerLocation(
            ritual.X * 16 + 8,
            (ritual.Y + 1) * 16 + 8,
            PlayerLocationIds.LongnightLanternFeast
        );
        _session.Inventory.Select(1);
        ShowLongnightLanternFeast(false);
    }

    private void PrepareLongnightFeastPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(55, 17 * 60);
        _session.Weather.AdvanceToDay(55);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddLongnightFeastExamples()
    {
        foreach (var itemId in new[]
        {
            DataCatalog.MoonmistStewId,
            DataCatalog.SunvaultHashId,
            DataCatalog.StarhoneyCustardId,
            DataCatalog.LanternrootBrothId,
            DataCatalog.StarbudPreserveId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.StarhoneyId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartFireflyTideGatePlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.SetPlayerLocation(
            FireflyTideLayout.WorldReturnCell.X * 16 + 8,
            FireflyTideLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        ShowFarm(false);
    }

    private void StartFireflyTidePlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.SetPlayerLocation(
            FireflyTideLayout.SafeArrivalCell.X * 16 + 8,
            FireflyTideLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
    }

    private void StartFireflyTideActivityPlaytest()
    {
        PrepareFireflyTidePlaytest();
        AddFireflyTideExamples();
        var launch = FireflyTideLayout.LanternLaunchCell;
        _session.SetPlayerLocation(
            launch.X * 16 + 8,
            (launch.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
        Callable.From(() => OpenFireflyTideActivity(launch)).CallDeferred();
    }

    private void StartFireflyTideResultPlaytest()
    {
        PrepareFireflyTidePlaytest();
        AddFireflyTideExamples();
        var altar = FireflyTideLayout.TideAltarCell;
        _session.SetPlayerLocation(
            altar.X * 16 + 8,
            (altar.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        _ = _session.CompleteFireflyTide(
            altar,
            [
                DataCatalog.MooncapGobyId,
                DataCatalog.RainveilLampreyId,
                DataCatalog.StardustRayId
            ]
        );
        ShowFireflyTide(false);
        Callable.From(() => OpenFireflyTideActivity(altar)).CallDeferred();
    }

    private void StartFireflyTideShopPlaytest()
    {
        PrepareFireflyTidePlaytest();
        _session.Festival.Restore(new FestivalSave
        {
            CurrencyBalances =
            [
                new FestivalCurrencySave
                {
                    CurrencyId = FestivalCatalog.FireflyGlowmarkId,
                    Balance = 10
                }
            ]
        });
        var shop = FireflyTideLayout.ShopCell;
        _session.SetPlayerLocation(
            shop.X * 16 + 8,
            (shop.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        ShowFireflyTide(false);
        Callable.From(OpenFireflyTideShop).CallDeferred();
    }

    private void StartFireflyTideActivityEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartFireflyTideActivityPlaytest();
    }

    private void StartFireflyTideWrongToolPlaytest()
    {
        PrepareFireflyTidePlaytest();
        var altar = FireflyTideLayout.TideAltarCell;
        _session.SetPlayerLocation(
            altar.X * 16 + 8,
            (altar.Y + 1) * 16 + 8,
            PlayerLocationIds.FireflyTide
        );
        _session.Inventory.Select(1);
        ShowFireflyTide(false);
    }

    private void PrepareFireflyTidePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(26, 18 * 60);
        _session.Weather.AdvanceToDay(26);
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
    }

    private void AddFireflyTideExamples()
    {
        foreach (var itemId in FestivalCatalog.FireflyTideFishIds)
        {
            _session.Inventory.Add(itemId, 1);
        }
    }

    private void StartLongnightHomesteadPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        const int day = 43;
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 8 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);

        var target = new GridPosition(22, 16);
        _ = _session.Farm.TryTill(target, GameSession.MaxEnergy);
        _session.Inventory.Add(DataCatalog.CloudleafSeedId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.CloudleafSeedId);
        _session.SetPlayerState(
            target.X * 16 + 8,
            (target.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartLongnightEmporiumPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        const int day = 43;
        _session.Clock.Reset(day, 10 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
        Callable.From(InspectTravelManifest).CallDeferred();
    }

    private void StartLongnightSnowForecastPlaytest() =>
        StartLongnightWeatherFarmPlaytest(42);

    private void StartLongnightSnowPlaytest() =>
        StartLongnightWeatherFarmPlaytest(43);

    private void StartLongnightSnowClearPlaytest() =>
        StartLongnightWeatherFarmPlaytest(44);

    private void StartLongnightSnowIndoorPlaytest()
    {
        CompleteGreenhousePlaytestConstruction(39);
        RestoreGreenhouseShowcaseCrops();
        _session.SetPlayerLocation(
            12 * 16 + 8,
            6 * 16 + 8,
            PlayerLocationIds.Greenhouse
        );
        ShowGreenhouse(false);
    }

    private void StartLongnightWeatherFarmPlaytest(int day)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, 8 * 60);
        _session.Weather.AdvanceToDay(day);
        var target = new GridPosition(22, 16);
        _ = _session.Farm.TryTill(target, GameSession.MaxEnergy);
        _session.Inventory.Add(DataCatalog.CloudleafSeedId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.CloudleafSeedId);
        _session.SetPlayerState(
            target.X * 16 + 8,
            (target.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCraftingPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.LumenwoodId, 20);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 20);
        OpenCrafting();
    }

    private void StartQualityCraftingPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 5);
        OpenCrafting();
    }

    private void StartQualityBackpackPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.StarsoilFertilizerId, 4);
        _session.Inventory.Add(DataCatalog.StarbudId, 2);
        _session.Inventory.Add(DataCatalog.StarbudLuminousId, 2);
        _session.Inventory.Add(DataCatalog.StarbudStarlightId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootLuminousId, 2);
        _session.Inventory.Add(DataCatalog.MoonrootStarlightId, 1);
        Callable.From(OpenBackpack).CallDeferred();
    }

    private void StartQualityBackpackEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartQualityBackpackPlaytest();
    }

    private void StartQualityPlaytest()
    {
        StartNewGame();
        _session.Farm.Restore(
        [
            new FarmTileState
            {
                X = 12,
                Y = 16,
                Tilled = true
            },
            new FarmTileState
            {
                X = 14,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId
            },
            new FarmTileState
            {
                X = 16,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId,
                CropId = DataCatalog.StarbudId,
                WateredNights = 2,
                QualityRoll = 55
            },
            new FarmTileState
            {
                X = 20,
                Y = 16,
                Tilled = true,
                FertilizerId = DataCatalog.StarsoilFertilizerId,
                CropId = DataCatalog.MoonrootId,
                WateredNights = 3,
                QualityRoll = 5
            },
            new FarmTileState
            {
                X = 22,
                Y = 16,
                Tilled = true,
                CropId = DataCatalog.CloudleafId,
                WateredNights = 2,
                QualityRoll = 80
            }
        ]);
        _session.Inventory.Add(DataCatalog.StarsoilFertilizerId, 3);
        _session.Inventory.Add(DataCatalog.StarbudLuminousId, 2);
        _session.Inventory.Add(DataCatalog.StarbudStarlightId, 1);
        _session.Inventory.PromoteToHotbar(
            DataCatalog.StarsoilFertilizerId
        );
        _session.SetPlayerState(12 * 16 + 8, 15 * 16 + 8, false);
        ShowFarm(false);
    }

    private void StartFarmingSpecializationPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.FarmingSkill.Experience = FarmingSkillCatalog.Levels[
            FarmingSkillSystem.SpecializationUnlockLevel
        ].RequiredExperience;
        _session.Restore(save);
        _session.SetPlayerState(20 * 16 + 8, 14 * 16 + 8, false);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
    }

    private void StartOrchardHivesPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);

        var treeCell = new GridPosition(23, 13);
        var hiveCell = new GridPosition(27, 13);
        var save = _session.Capture();
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = treeCell.X,
                Y = treeCell.Y,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                PendingHoney = 1
            }
        ];
        _session.Restore(save);

        _session.Inventory.Add(DataCatalog.MoonplumSaplingId, 2);
        _session.Inventory.Add(DataCatalog.MoonplumId, 3);
        _session.Inventory.Add(DataCatalog.StarhoneyId, 2);
        _session.Inventory.Add(DataCatalog.GlowcombHiveId, 1);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 8);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        _session.Inventory.PromoteToHotbar(DataCatalog.GlowcombHiveId);
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            hiveCell.X * 16 + 8,
            (hiveCell.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartFarmPlaceablesPlaytest()
    {
        StartNewGame();
        PlacePlaytestObjects(
            DataCatalog.MoonstonePathId,
            Enumerable.Range(20, 4).Select(x => new GridPosition(x, 13))
        );
        PlacePlaytestObjects(
            DataCatalog.StarwoodFenceId,
            Enumerable.Range(25, 4).Select(x => new GridPosition(x, 13))
        );
        PlacePlaytestObjects(
            DataCatalog.StarlightTorchId,
            [
                new GridPosition(19, 13),
                new GridPosition(30, 13)
            ]
        );
        PlacePlaytestObjects(
            DataCatalog.DewfallSprinklerId,
            [
                new GridPosition(15, 16),
                new GridPosition(21, 16),
                new GridPosition(28, 16)
            ]
        );
        foreach (var target in new[]
                 {
                     new GridPosition(15, 15),
                     new GridPosition(16, 16),
                     new GridPosition(15, 17),
                     new GridPosition(14, 16)
                 })
        {
            _session.Farm.TryTill(target, GameSession.MaxEnergy);
            _session.Farm.TryPlant(target, DataCatalog.StarbudId);
        }
        _session.Inventory.Add(DataCatalog.StarlightTorchId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarlightTorchId);
        _session.SetPlayerState(31 * 16 + 8, 12 * 16 + 8, false);
        ShowFarm(false);
    }

    private void PlacePlaytestObjects(
        string itemId,
        IEnumerable<GridPosition> positions
    )
    {
        var cells = positions.ToList();
        _session.Inventory.Add(itemId, cells.Count);
        _session.Inventory.PromoteToHotbar(itemId);
        foreach (var position in cells)
        {
            _session.UseSelected(position);
        }
    }

    private void StartChestPlacementPlaytest()
    {
        StartNewGame();
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        _session.SetPlayerState(25 * 16 + 8, 12 * 16 + 8, false);
        ShowFarm(false);
    }

    private void StartStoragePlaytest()
    {
        StartNewGame();
        var chest = new GridPosition(25, 13);
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.Inventory.PromoteToHotbar(DataCatalog.StarwovenChestId);
        _session.UseSelected(chest);
        _session.Inventory.Add(DataCatalog.StarbudSeedId, 5);
        _session.Inventory.Add(DataCatalog.CloudleafId, 3);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 4);
        _session.StoreInChest(chest, DataCatalog.StarbudSeedId);
        _session.StoreInChest(chest, DataCatalog.CloudleafId);
        _session.Inventory.Select(0);
        _session.SetPlayerState(25 * 16 + 8, 14 * 16 + 8, false);
        ShowFarm(false);
        OpenStorage(chest);
    }

    private void StartCommissionOfferPlaytest()
    {
        StartCommissionPlaytest();
    }

    private void StartCommissionReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.EndDay();
        _session.AcceptDailyCommission();
        _session.Commission.RecordGather(DataCatalog.LumenwoodId, 3);
        StartCommissionPlaytestWorld();
    }

    private void StartCommissionReadyEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCommissionReadyPlaytest();
    }

    private void StartCommissionMapPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.EndDay();
        _session.AcceptDailyCommission();
        _session.Commission.RecordGather(DataCatalog.LumenwoodId, 2);
        StartCommissionPlaytestWorld(false);
    }

    private void StartCommissionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        StartCommissionPlaytestWorld();
    }

    private void StartCommissionPlaytestWorld(bool openBoard = true)
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.CommissionBoardCell.X * 16 + 8,
            (FarmView.CommissionBoardCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openBoard)
        {
            Callable.From(OpenCommissionBoard).CallDeferred();
        }
    }

    private void StartWeeklyCommissionOfferPlaytest()
    {
        PrepareWeeklyCommissionPlaytest();
    }

    private void StartWeeklyCommissionStageReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionRewardReadyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.WeeklyCommission.RecordPlant(DataCatalog.StarbudId);
        _session.AdvanceWeeklyCommissionStage();
        _session.WeeklyCommission.RecordGather(
            DataCatalog.LumenwoodId,
            4
        );
        _session.AdvanceWeeklyCommissionStage();
        _session.Inventory.Add(DataCatalog.CrystalShardId, 3);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionMapPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.AcceptWeeklyCommission();
        StartCommissionPlaytestWorld(false);
    }

    private void PrepareWeeklyCommissionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        StartWeeklyCommissionPlaytestWorld();
    }

    private void StartWeeklyCommissionPlaytestWorld()
    {
        StartCommissionPlaytestWorld(false);
        Callable.From(OpenWeeklyCommissionBoard).CallDeferred();
    }

    private void StartMailboxUnreadPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 2
                }
            ]
        );
        StartMailPlaytestWorld(false);
    }

    private void StartMailPanelPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 4
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.LioraTrustedId,
                    DeliveredDay = 3
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.TaviTrustedId,
                    DeliveredDay = 3,
                    IsRead = true,
                    AttachmentClaimed = true
                }
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private void StartMailRewardPlaytest()
    {
        PrepareMailPlaytest(
            [
                new MailEntrySave
                {
                    MailId = MailCatalog.LioraTrustedId,
                    DeliveredDay = 5
                },
                new MailEntrySave
                {
                    MailId = MailCatalog.NemiWelcomeId,
                    DeliveredDay = 2,
                    IsRead = true
                }
            ]
        );
        StartMailPlaytestWorld(true, true);
    }

    private void PrepareMailPlaytest(IReadOnlyList<MailEntrySave> entries)
    {
        FreeUi(_title);
        _title = null;
        _mailPlaytest = true;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = entries.Max(entry => entry.DeliveredDay);
        save.Mail = new MailSave
        {
            Entries = entries.ToList()
        };
        _session.Restore(save);
    }

    private void StartMailPlaytestWorld(
        bool openPanel,
        bool claimAttachment = false
    )
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.StarlightMailboxCell.X * 16 + 8,
            (FarmView.StarlightMailboxCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() =>
            {
                OpenStarlightMail();
                if (claimAttachment)
                {
                    _mailOverlay?.PressClaimForPlaytest();
                }
            }).CallDeferred();
        }
    }

    private void StartStarlightMapPlaytest()
    {
        StartStarlightPlaytestWorld(false);
    }

    private void StartStarlightPanelPlaytest()
    {
        PrepareStarlightPlaytest();
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        _session.Inventory.Add(DataCatalog.LumenwoodId, 3);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        StartStarlightPlaytestWorld();
    }

    private void StartStarlightRestoredPlaytest()
    {
        PrepareRestoredStarlightPlaytest();
        StartStarlightPlaytestWorld();
    }

    private void StartStarlightRestoredMapPlaytest()
    {
        PrepareRestoredStarlightPlaytest();
        StartStarlightPlaytestWorld(false);
    }

    private void PrepareRestoredStarlightPlaytest()
    {
        PrepareStarlightPlaytest();
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootId, 1);
        _session.Inventory.Add(DataCatalog.CloudleafId, 1);
        _session.Inventory.Add(DataCatalog.LumenwoodId, 6);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 2);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.Inventory.Add(DataCatalog.MoonrootTonicId, 1);
        _session.Inventory.Add(DataCatalog.StarwovenChestId, 1);
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandHarvestNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandMaterialsNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.WoodlandCraftNodeId
        );
    }

    private void StartStarlightRestoredEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartStarlightRestoredPlaytest();
    }

    private void StartHomesteadStarlightDormantPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: false);
        StartHomesteadStarlightPlaytestWorld(openPanel: false);
    }

    private void StartHomesteadStarlightWrongToolPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: false);
        StartHomesteadStarlightPlaytestWorld(
            openPanel: false,
            selectedSlot: 1
        );
    }

    private void StartHomesteadStarlightRestoredPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: true);
        StartHomesteadStarlightPlaytestWorld(openPanel: false);
    }

    private void StartHomesteadStarlightPanelPlaytest()
    {
        PrepareHomesteadStarlightPlaytest(restored: true);
        StartHomesteadStarlightPlaytestWorld(openPanel: true);
    }

    private void StartHomesteadStarlightPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartHomesteadStarlightPanelPlaytest();
    }

    private void PrepareHomesteadStarlightPlaytest(bool restored)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Starlight.Discover(DataCatalog.HomesteadStarlightId);
        if (!restored)
        {
            return;
        }

        var crops = DataCatalog.CropIds.Take(4).ToArray();
        foreach (var cropId in crops)
        {
            _session.Inventory.Add(cropId, 1);
        }
        foreach (var itemId in new[]
        {
            DataCatalog.StarbudPreserveId,
            DataCatalog.MoonrootTonicId,
            DataCatalog.CloudleafTeaId,
            DataCatalog.MoonstonePathId,
            DataCatalog.StarwoodFenceId,
            DataCatalog.StarlightTorchId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadHarvestNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadArtisanNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.HomesteadStarlightId,
            DataCatalog.HomesteadBuildingNodeId
        );
    }

    private void StartHomesteadStarlightPlaytestWorld(
        bool openPanel,
        int selectedSlot = 0
    )
    {
        _session.Inventory.Select(selectedSlot);
        _session.SetPlayerState(
            FarmView.HomesteadStarlightCell.X * 16 + 8,
            (FarmView.HomesteadStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() => OpenStarlightPedestal(
                DataCatalog.HomesteadStarlightId
            )).CallDeferred();
        }
    }

    private void StartMeadowStarlightDormantPlaytest()
    {
        PrepareMeadowStarlightPlaytest();
        StartMeadowStarlightPlaytestWorld(openPanel: false);
    }

    private void StartMeadowStarlightRestoredPlaytest()
    {
        PrepareMeadowStarlightPlaytest(complete: true);
        StartMeadowStarlightPlaytestWorld(openPanel: false);
    }

    private void StartMeadowStarlightPanelPlaytest()
    {
        PrepareMeadowStarlightPlaytest(partial: true);
        StartMeadowStarlightPlaytestWorld(openPanel: true);
    }

    private void StartMeadowStarlightPanelEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartMeadowStarlightPanelPlaytest();
    }

    private void PrepareMeadowStarlightPlaytest(
        bool complete = false,
        bool partial = false
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Festival.Restore(new FestivalSave
        {
            Results =
            [
                new FestivalYearResultSave
                {
                    FestivalId =
                        FestivalCatalog.StarharvestMarketFestivalId,
                    Year = 1,
                    ItemIds =
                    [
                        DataCatalog.AuricShootId,
                        DataCatalog.SunvaultGourdId,
                        DataCatalog.CrownstarSaffronId
                    ],
                    Score = 30,
                    AwardId = FestivalCatalog.GoldenCrownAwardId
                }
            ]
        });
        _session.Starlight.Discover(DataCatalog.MeadowStarlightId);
        if (!complete && !partial)
        {
            return;
        }

        var blooms = complete
            ? new[]
            {
                DataCatalog.DawnlaceId,
                DataCatalog.EmberbellId,
                DataCatalog.DuskbellId
            }
            : new[]
            {
                DataCatalog.DawnlaceId,
                DataCatalog.EmberbellId
            };
        foreach (var itemId in blooms)
        {
            _session.Inventory.Add(itemId, 1);
        }

        var bounty = complete
            ? new[]
            {
                DataCatalog.StarhoneyId,
                DataCatalog.StarfeatherEggId,
                DataCatalog.MoonfleeceId,
                DataCatalog.DewhornMilkId
            }
            : new[]
            {
                DataCatalog.StarhoneyId,
                DataCatalog.StarfeatherEggId
            };
        foreach (var itemId in bounty)
        {
            _session.Inventory.Add(itemId, 1);
        }

        _session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBloomsNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.MeadowStarlightId,
            DataCatalog.MeadowBountyNodeId
        );
        _session.Starlight.RefreshRewardUnlocks(new StarlightProgressContext(
            new HashSet<string>(
                [FestivalCatalog.StarharvestMarketFestivalId],
                StringComparer.Ordinal
            )
        ));
    }

    private void StartMeadowStarlightPlaytestWorld(bool openPanel)
    {
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.MeadowStarlightCell.X * 16 + 8,
            (FarmView.MeadowStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(() => OpenStarlightPedestal(
                DataCatalog.MeadowStarlightId
            )).CallDeferred();
        }
    }

    private void StartMeadowPollinationPlaytest()
    {
        PrepareMeadowStarlightPlaytest(complete: true);
        var hiveCell = new GridPosition(27, 13);
        var treeCell = new GridPosition(21, 13);
        var save = _session.Capture();
        save.Orchard.FruitTrees =
        [
            new FruitTreeSave
            {
                X = treeCell.X,
                Y = treeCell.Y,
                TreeId = DataCatalog.MoonplumTreeId,
                AgeNights = DataCatalog.FruitTree(
                    DataCatalog.MoonplumTreeId
                ).MatureAfterNights,
                FruitReady = true
            }
        ];
        save.FarmObjects.Objects =
        [
            new PlacedFarmObjectSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ItemId = DataCatalog.GlowcombHiveId
            }
        ];
        save.Orchard.Beehives =
        [
            new BeehiveSave
            {
                X = hiveCell.X,
                Y = hiveCell.Y,
                ProgressNights = 1
            }
        ];
        _session.Restore(save);
        _session.Inventory.Select(0);
        _session.SetPlayerState(
            hiveCell.X * 16 + 8,
            (hiveCell.Y - 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void PrepareStarlightPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Starlight.Discover();
    }

    private void StartStarlightPlaytestWorld(bool openPedestal = true)
    {
        if (_title is not null)
        {
            PrepareStarlightPlaytest();
        }

        _session.Inventory.Select(0);
        _session.SetPlayerState(
            FarmView.WoodlandStarlightCell.X * 16 + 8,
            (FarmView.WoodlandStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openPedestal)
        {
            Callable.From(OpenStarlightPedestal).CallDeferred();
        }
    }

    private void StartEconomyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        foreach (var cropId in DataCatalog.CropIds)
        {
            _session.Inventory.Add(cropId, 4);
        }
        _session.SetPlayerState(
            FarmView.ShopCell.X * 16 + 8,
            (FarmView.ShopCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShop).CallDeferred();
    }

    private void StartVillagePlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(97, 45)
        );
    }

    private void StartVillageDialoguePlaytest()
    {
        StartArchivePlaytest(true, false);
    }

    private void StartSelaDialoguePlaytest()
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var sela = VillageCatalog.CurrentNpc(
            VillageCatalog.SelaId,
            day,
            minuteOfDay
        );
        if (sela is null)
        {
            StartVillagePlaytest();
            return;
        }

        StartVillagePlaytestWorld(
            day,
            minuteOfDay,
            new GridPosition(sela.Position.X, sela.Position.Y + 1)
        );
        sela = PlacePlayerAdjacentForPlaytest(
            _session.Village.CurrentNpcs(
                    day,
                    minuteOfDay,
                    sela.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == VillageCatalog.SelaId
                )
        );
        Callable.From(
            () => TalkToVillager(sela.Position)
        ).CallDeferred();
    }

    private void StartVillageExpansionPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            14 * 60,
            new GridPosition(97, 55)
        );
    }

    private void StartVillageExpansionArchivePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(1, 12 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
    }

    private void StartVillageExpansionDialogueEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillageExpansionFocusPlaytest(openDialogue: true,
            wrongTool: false);
    }

    private void StartVillageExpansionWrongToolPlaytest() =>
        StartVillageExpansionFocusPlaytest(
            openDialogue: false,
            wrongTool: true
        );

    private void StartVillageExpansionFocusPlaytest(
        bool openDialogue,
        bool wrongTool
    )
    {
        const int day = 1;
        const int minuteOfDay = 17 * 60;
        StartVillagePlaytestWorld(
            day,
            minuteOfDay,
            new GridPosition(97, 55)
        );
        var dorrik = _session.Village.CurrentNpcs(
                day,
                minuteOfDay,
                PlayerLocationIds.World,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.DorrikId
            );
        if (dorrik is null)
        {
            return;
        }
        dorrik = PlacePlayerAdjacentForPlaytest(dorrik);

        _session.Inventory.Select(wrongTool ? 1 : 0);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(dorrik.Position)
            ).CallDeferred();
        }
    }

    private void StartNpcPathfindingPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            13 * 60 + 30,
            new GridPosition(104, 61)
        );
    }

    private void StartArchivePlaytest()
    {
        StartArchivePlaytest(false, false);
    }

    private void StartArchiveGiftPlaytest()
    {
        StartArchivePlaytest(true, true);
    }

    private void StartArchiveDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.MoonlitArchiveDoorCell.X,
                VillageCatalog.MoonlitArchiveDoorCell.Y + 1
            )
        );
    }

    private void StartCropCodexDeskPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: false,
            wrongTool: false
        );

    private void StartCropCodexPartialPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: true,
            wrongTool: false
        );

    private void StartCropCodexRewardReadyPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: CompendiumCatalog.CropEntries.Count,
            rewardClaimed: false,
            openPanel: true,
            wrongTool: false
        );

    private void StartCropCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCropCodexPlaytest(
            discoveredCount: CompendiumCatalog.CropEntries.Count,
            rewardClaimed: true,
            openPanel: true,
            wrongTool: false
        );
    }

    private void StartCropCodexWrongToolPlaytest() =>
        StartCropCodexPlaytest(
            discoveredCount: 7,
            rewardClaimed: false,
            openPanel: false,
            wrongTool: true
        );

    private void StartCropCodexDiscountShopPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Coins = 500;
        save.Collection = CompletedCropCodexSave(rewardClaimed: true);
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmView.ShopCell.X * 16 + 8;
        save.Player.Y = (FarmView.ShopCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShop).CallDeferred();
    }

    private void StartCropCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed,
        bool openPanel,
        bool wrongTool
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.CropEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.CropEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.MoonlitAlmanac]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = wrongTool ? 1 : 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        if (openPanel)
        {
            Callable.From(() => OpenCropCodex(
                VillageCatalog.MoonlitArchiveDeskCell
            )).CallDeferred();
        }
    }

    private static CollectionSave CompletedCropCodexSave(
        bool rewardClaimed
    ) => new()
    {
        Initialized = true,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
        DiscoveredEntryIds = CompendiumCatalog.CropEntries
            .Select(entry => entry.Id)
            .ToList(),
        ClaimedRewardIds = rewardClaimed
            ? [CollectionRewardIds.MoonlitAlmanac]
            : []
    };

    private void StartCookingCodexUnknownPlaytest() =>
        StartCookingCodexPlaytest(0, rewardClaimed: false);

    private void StartCookingCodexPartialPlaytest() =>
        StartCookingCodexPlaytest(2, rewardClaimed: false);

    private void StartCookingCodexRewardReadyPlaytest() =>
        StartCookingCodexPlaytest(
            CompendiumCatalog.CookingEntries.Count,
            rewardClaimed: false
        );

    private void StartCookingCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartCookingCodexPlaytest(
            CompendiumCatalog.CookingEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartCookingCodexRewardMealsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareCottageSecondUpgradePlaytest(
            CompletedCottageSecondUpgradeProject()
        );
        foreach (var entry in CompendiumCatalog.CookingEntries)
        {
            _session.Collection.RecordObtainedItem(entry.ItemId);
        }
        _ = _session.Collection.ClaimReward(
            CollectionRewardIds.MoonhearthRecipeJournal
        );
        PositionAtCottageKitchen();
        ShowCottage(false);
        Callable.From(OpenCookedDishes).CallDeferred();
    }

    private void StartCookingCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.CookingEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.CookingEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.MoonhearthRecipeJournal]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Cooking
        )).CallDeferred();
    }

    private void StartArtisanCodexUnknownPlaytest() =>
        StartArtisanCodexPlaytest(0, rewardClaimed: false);

    private void StartArtisanCodexPartialPlaytest() =>
        StartArtisanCodexPlaytest(2, rewardClaimed: false);

    private void StartArtisanCodexRewardReadyPlaytest() =>
        StartArtisanCodexPlaytest(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: false
        );

    private void StartArtisanCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartArtisanCodexPlaytest(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartArtisanCodexRewardShippingEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = ArtisanCodexSave(
            CompendiumCatalog.ArtisanEntries.Count,
            rewardClaimed: true
        );
        save.Shipping.Pending = CompendiumCatalog.ArtisanEntries
            .Select(entry => new ShippingEntrySave
            {
                ItemId = entry.ItemId,
                Count = 1
            })
            .ToList();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmView.ShippingCell.X * 16 + 8;
        save.Player.Y = (FarmView.ShippingCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShipping).CallDeferred();
    }

    private void StartArtisanCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = ArtisanCodexSave(
            discoveredCount,
            rewardClaimed
        );
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Artisan
        )).CallDeferred();
    }

    private static CollectionSave ArtisanCodexSave(
        int discoveredCount,
        bool rewardClaimed
    ) => new()
    {
        Initialized = true,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
        DiscoveredEntryIds = CompendiumCatalog.ArtisanEntries
            .Take(Math.Clamp(
                discoveredCount,
                0,
                CompendiumCatalog.ArtisanEntries.Count
            ))
            .Select(entry => entry.Id)
            .ToList(),
        ClaimedRewardIds = rewardClaimed
            ? [CollectionRewardIds.StarlitAppraisalLedger]
            : []
    };

    private void StartSeasonalForagePlaytest() =>
        StartSeasonalForagePlaytest(
            day: 1,
            weatherId: DataCatalog.ClearWeatherId,
            wrongTool: false,
            mapUnlocked: false
        );

    private void StartSeasonalForageWrongToolPlaytest() =>
        StartSeasonalForagePlaytest(
            day: 15,
            weatherId: DataCatalog.RainWeatherId,
            wrongTool: true,
            mapUnlocked: false
        );

    private void StartSeasonalForageStardustMapPlaytest() =>
        StartSeasonalForagePlaytest(
            day: 29,
            weatherId: DataCatalog.StardustWindWeatherId,
            wrongTool: false,
            mapUnlocked: true
        );

    private void StartSeasonalForagePlaytest(
        int day,
        string weatherId,
        bool wrongTool,
        bool mapUnlocked
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var spawns = ForageSystem.Generate(day, weatherId);
        var focus = spawns[0];
        var approach = new[]
        {
            new GridPosition(focus.Cell.X, focus.Cell.Y - 1),
            new GridPosition(focus.Cell.X + 1, focus.Cell.Y),
            new GridPosition(focus.Cell.X, focus.Cell.Y + 1),
            new GridPosition(focus.Cell.X - 1, focus.Cell.Y)
        }.First(cell => !WorldDefinition.IsBlocked(cell));
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = weatherId
        };
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = mapUnlocked
                ? CompendiumCatalog.ForageEntries
                    .Select(entry => entry.Id)
                    .ToList()
                : [],
            ClaimedRewardIds = mapUnlocked
                ? [CollectionRewardIds.StarpathForagersGuide]
                : []
        };
        save.Exploration.DiscoveredChunks = spawns
            .Select(spawn => WorldDefinition.ChunkId(
                WorldDefinition.GetChunk(spawn.Cell)
            ))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = approach.X * 16 + 8;
        save.Player.Y = approach.Y * 16 + 8;
        save.Player.SelectedSlot = wrongTool ? 1 : 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartForageCodexPartialPlaytest() =>
        StartForageCodexPlaytest(4, rewardClaimed: false);

    private void StartForageCodexRewardReadyPlaytest() =>
        StartForageCodexPlaytest(
            CompendiumCatalog.ForageEntries.Count,
            rewardClaimed: false
        );

    private void StartForageCodexRewardClaimedEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartForageCodexPlaytest(
            CompendiumCatalog.ForageEntries.Count,
            rewardClaimed: true
        );
    }

    private void StartForageCodexPlaytest(
        int discoveredCount,
        bool rewardClaimed
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 10 * 60;
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.ForageEntries
                .Take(Math.Clamp(
                    discoveredCount,
                    0,
                    CompendiumCatalog.ForageEntries.Count
                ))
                .Select(entry => entry.Id)
                .ToList(),
            ClaimedRewardIds = rewardClaimed
                ? [CollectionRewardIds.StarpathForagersGuide]
                : []
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Forage
        )).CallDeferred();
    }

    private void StartFishingPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(38 * 16 + 8, 20 * 16 + 8, false);
        _session.Inventory.Select(5);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartFishingMinigamePlaytest()
    {
        StartFishingPlaytest();
        Callable.From(() => OpenFishingMinigame(new GridPosition(38, 21)))
            .CallDeferred();
    }

    private void StartFishingGearPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Coins = 1800;
        save.Fishing.Experience = 150;
        save.Fishing.Level = 3;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenFishingGear).CallDeferred();
    }

    private void StartFishingCollectionPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Fishing.CaughtFishIds = DataCatalog.FishItemIds
            .Take(8)
            .ToList();
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenFishingCollection).CallDeferred();
    }

    private void StartFishingDonationPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.Player.SelectedSlot = 0;
        var previewFishIds = DataCatalog.FishItemIds.Take(3).ToArray();
        save.Fishing.CaughtFishIds = previewFishIds.ToList();
        save.Fishing.DonatedFishIds = [previewFishIds[0]];
        _session.Restore(save);
        _session.Inventory.Add(previewFishIds[1], 1);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(OpenFishingDonation).CallDeferred();
    }

    private void StartFishCodexPartialPlaytest() =>
        StartFishCodexPlaytest(12);

    private void StartFishCodexCompleteEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartFishCodexPlaytest(CompendiumCatalog.FishEntries.Count);
    }

    private void StartFishCodexPlaytest(int discoveredCount)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var discovered = CompendiumCatalog.FishEntries
            .Take(Math.Clamp(
                discoveredCount,
                0,
                CompendiumCatalog.FishEntries.Count
            ))
            .Select(entry => entry.Id)
            .ToList();
        var save = _session.Capture();
        save.Day = 15;
        save.MinuteOfDay = 10 * 60;
        save.Fishing.CaughtFishIds = discovered.ToList();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = discovered
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Fish
        )).CallDeferred();
    }

    private void StartCrystalGrottoEntryPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Select(0);
        _session.SetPlayerLocation(
            CrystalGrottoSurveyLayout.WorldReturnCell.X * 16 + 8,
            CrystalGrottoSurveyLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartCrystalGrottoBasicPlaytest()
    {
        PrepareCrystalGrottoPlaytest(
            new GridPosition(23, 15),
            selectedSlot: 1
        );
    }

    private void StartCrystalGrottoUpgradePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Coins = 600;
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = 17 * 16 + 8;
        save.Player.Y = 15 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _session.Inventory.Add(DataCatalog.LumenSlateOreId, 6);
        _session.Inventory.Add(DataCatalog.MoonveinOreId, 3);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
        Callable.From(() => OpenToolUpgrade(
            CrystalGrottoSurveyLayout.UpgradeBenchCell
        )).CallDeferred();
    }

    private void StartCrystalGrottoDeepPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 7 * 16 + 8;
        save.Player.SelectedSlot = 1;
        save.Mining = new MiningSave { DeepestRoomReached = 4 };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
    }

    private void StartDeepMinePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerLocation(
            CrystalGrottoSurveyLayout.DepthAnchorCell.X * 16 + 8,
            (CrystalGrottoSurveyLayout.DepthAnchorCell.Y + 1) * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        _session.DeepMine.Start(_session.Clock.Day, _session.Inventory);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
        Callable.From(OpenDeepMine).CallDeferred();
    }

    private void StartMineralCodexCompleteEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.MineralEntries
                .Select(entry => entry.Id)
                .ToList()
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Minerals
        )).CallDeferred();
    }

    private void StartCrystalValeStarlightPanelPlaytest()
    {
        PrepareCrystalValeStarlightPlaytest(restored: false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.CrystalValeStarlightId
        )).CallDeferred();
    }

    private void StartCrystalValeStarlightRestoredPlaytest() =>
        PrepareCrystalValeStarlightPlaytest(restored: true);

    private void StartStarfallRuinsEntryPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8;
        save.Player.Y = StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        RestoreWorldPlaytest(save);
    }

    private void StartStarfallRuinsCombatPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.StarfallRuinsTrial;
        save.Player.X = 6 * 16 + 8;
        save.Player.Y = 15 * 16 + 8;
        save.StarfallRuinsTrial.WeaponClaimed = true;
        RestoreStarfallRuinsPlaytest(save);
        _session.Inventory.Add(DataCatalog.MoonsteelShortbladeId, 1);
        _session.Inventory.PromoteToHotbar(
            DataCatalog.MoonsteelShortbladeId
        );
    }

    private void StartStarfallRuinsArtifactsPlaytest()
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        save.Player.LocationId = PlayerLocationIds.StarfallRuinsTrial;
        save.Player.X = 22 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.StarfallRuinsTrial.WeaponClaimed = true;
        save.StarfallRuinsTrial.ClearedRoomIds =
            StarfallRuinsTrialCatalog.Rooms
                .Select(room => room.Id)
                .ToList();
        RestoreStarfallRuinsPlaytest(save);
        _session.Inventory.Add(DataCatalog.MoonsteelShortbladeId, 1);
        _session.Inventory.Select(0);
    }

    private void StartArtifactCodexDonationEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var artifactIds = CompendiumCatalog.ArtifactEntries
            .Select(entry => entry.Id)
            .ToList();
        var save = _session.Capture();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = artifactIds,
            DonatedEntryIds = artifactIds.Skip(2).ToList()
        };
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 12 * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        foreach (var artifactId in artifactIds.Take(2))
        {
            _session.Inventory.Add(artifactId, 1);
        }
        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(() => OpenCompendium(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionCategoryIds.Artifacts
        )).CallDeferred();
    }

    private void StartStarfallRuinsStarlightPanelPlaytest()
    {
        PrepareStarfallRuinsStarlightPlaytest(restored: false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.StarfallRuinsStarlightId
        )).CallDeferred();
    }

    private void StartStarfallRuinsStarlightRestoredPlaytest() =>
        PrepareStarfallRuinsStarlightPlaytest(restored: true);

    private void StartSixfoldStarGatePlaytest() =>
        PrepareSixfoldStarGatePlaytest(openPanel: false);

    private void StartSixfoldStarGatePanelPlaytest() =>
        PrepareSixfoldStarGatePlaytest(openPanel: true);

    private void StartStellarConvergencePlaytest()
    {
        PrepareSixfoldStarGatePlaytest(openPanel: false);
        var save = _session.Capture();
        save.FarmingSkill.Experience = FarmingSkillCatalog.Levels[^1]
            .RequiredExperience;
        save.GatheringSkill.Experience = GatheringSkillCatalog
            .LevelThresholds[^1];
        save.Fishing.Experience = FishingProgressionCatalog
            .LevelThresholds[^1];
        save.Mining.CrystalMiningSkill.Experience = AdventureSkillCatalog
            .LevelThresholds[^1];
        save.Mining.NightwatchSkill.Experience = AdventureSkillCatalog
            .LevelThresholds[^1];
        _session.Restore(save);
        var result = _session.CompleteMainStory();
        if (!result.Succeeded)
        {
            GD.PushError($"Could not prepare convergence playtest: {result.MessageKey}");
            return;
        }
        SetWorldControls(false);
        _mainStoryEndingOverlay = new MainStoryEndingOverlay(
            _theme,
            _session,
            _locale
        );
        _mainStoryEndingOverlay.ContinueRequested += CloseMainStoryEnding;
        _uiLayer.AddChild(_mainStoryEndingOverlay);
    }

    private void StartAccessibilitySettingsPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _settings.FontScalePercent = 120;
        AccessibilityRuntime.Apply(_settings, GetTree().Root);
        _theme = ThemeFactory.CreateTheme();
        _session.NewGame(_locale.CurrentLocale);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenSettings).CallDeferred();
    }

    private void PrepareSixfoldStarGatePlaytest(bool openPanel)
    {
        PrepareStarfallRuinsStarlightPlaytest(restored: true);
        var save = _session.Capture();
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadWorkshopProjectId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadGreenhouseProjectId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                    Completed = true
                },
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .SixfoldStarGateProjectId,
                    Completed = true
                }
            ]
        };
        save.StarGate = new StarGateSave { Activated = true };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmLayout.StarGateCell.X * 16 + 8;
        save.Player.Y = (FarmLayout.StarGateCell.Y - 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        ShowFarm(false);
        if (openPanel)
        {
            Callable.From(OpenStarGate).CallDeferred();
        }
    }

    private GameSaveV1 PrepareStarfallRuinsPlaytestSave()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 29;
        save.MinuteOfDay = 18 * 60;
        save.Mining = new MiningSave
        {
            DeepestRoomReached = CrystalGrottoSurveyLayout.RoomCount
        };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        save.Festival.Results =
        [
            new FestivalYearResultSave
            {
                FestivalId =
                    FestivalCatalog.GleamrisePlantingFestivalId,
                Year = 1,
                Score = 1
            }
        ];
        save.Starlight = new StarlightSave
        {
            Pedestals = DataCatalog.StarlightPedestals.Values
                .Where(definition => definition.Id !=
                    DataCatalog.StarfallRuinsStarlightId)
                .Select(CompletedPedestalSave)
                .ToList()
        };
        return save;
    }

    private void PrepareStarfallRuinsStarlightPlaytest(bool restored)
    {
        var save = PrepareStarfallRuinsPlaytestSave();
        var artifactIds = CompendiumCatalog.ArtifactEntries
            .Select(entry => entry.Id)
            .ToList();
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = artifactIds
                .Concat(CompendiumCatalog.EnemyEntries.Select(entry => entry.Id))
                .ToList(),
            DonatedEntryIds = artifactIds.Take(3).ToList()
        };
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.KaelId, VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.KaelId,
                    Points = 60
                },
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 60
                }
            ]
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = WorldDefinition.StarfallRuinsStarlightCell.X * 16 + 8;
        save.Player.Y =
            (WorldDefinition.StarfallRuinsStarlightCell.Y + 1) * 16 + 8;
        save.Player.SelectedSlot = 0;
        RestoreWorldPlaytest(save);
        _session.Starlight.Discover(DataCatalog.StarfallRuinsStarlightId);
        if (restored)
        {
            _session.ActivateStarlightPedestal(
                DataCatalog.StarfallRuinsStarlightId,
                WorldDefinition.StarfallRuinsStarlightCell
            );
        }
    }

    private static StarlightPedestalSave CompletedPedestalSave(
        StarlightPedestalDefinition definition
    ) => new()
    {
        PedestalId = definition.Id,
        Discovered = true,
        RewardUnlocked = true,
        Nodes = definition.Nodes
            .Where(node => node.SourceKind ==
                StarlightNodeSourceKind.Inventory)
            .Select(node =>
            {
                var remaining = node.RequiredCount;
                var contributions = new List<StarlightContributionSave>();
                foreach (var option in node.Options)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }
                    var count = Math.Min(remaining, option.MaximumCount);
                    contributions.Add(new StarlightContributionSave
                    {
                        ItemId = option.ItemId,
                        Count = count
                    });
                    remaining -= count;
                }
                return new StarlightNodeSave
                {
                    NodeId = node.Id,
                    Contributions = contributions
                };
            })
            .ToList()
    };

    private void RestoreWorldPlaytest(GameSaveV1 save)
    {
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void RestoreStarfallRuinsPlaytest(GameSaveV1 save)
    {
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowStarfallRuinsTrial(false);
    }

    private void PrepareCrystalGrottoPlaytest(
        GridPosition playerCell,
        int selectedSlot
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerLocation(
            playerCell.X * 16 + 8,
            playerCell.Y * 16 + 8,
            PlayerLocationIds.CrystalGrottoSurvey
        );
        _session.Inventory.Select(selectedSlot);
        _playing = true;
        EnsureHud();
        ShowCrystalGrotto(false);
    }

    private void PrepareCrystalValeStarlightPlaytest(bool restored)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Mining = new MiningSave
        {
            DeepestRoomReached = CrystalGrottoSurveyLayout.RoomCount
        };
        save.ToolProgression = CompletedBronzeStarShovelSave();
        _session.Restore(save);
        _session.Starlight.Discover(DataCatalog.CrystalValeStarlightId);
        foreach (var itemId in MiningCatalog.Minerals.Select(
                     mineral => mineral.ItemId
                 ))
        {
            _session.Inventory.Add(itemId, 1);
        }

        if (restored)
        {
            _session.ContributeToStarlightNode(
                DataCatalog.CrystalValeStarlightId,
                DataCatalog.CrystalValeMineralChorusNodeId
            );
        }

        _session.Inventory.Select(0);
        _session.SetPlayerLocation(
            WorldDefinition.CrystalWellCell.X * 16 + 8,
            (WorldDefinition.CrystalWellCell.Y + 1) * 16 + 8,
            PlayerLocationIds.World
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private static ToolProgressionSave CompletedBronzeStarShovelSave() =>
        new()
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };

    private void StartMoonwaterStarlightPanelPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 15;
        save.MinuteOfDay = 18 * 60;
        save.Weather = new WeatherSave
        {
            Day = 15,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);
        _session.Starlight.Discover(DataCatalog.MoonwaterStarlightId);
        foreach (var itemId in new[]
        {
            DataCatalog.MoonwaterMinnowId,
            DataCatalog.MarshveilKilliId,
            DataCatalog.RainveilLampreyId
        })
        {
            _session.Inventory.Add(itemId, 1);
        }
        _session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterLocalFishNodeId
        );
        _session.ContributeToStarlightNode(
            DataCatalog.MoonwaterStarlightId,
            DataCatalog.MoonwaterWeatherFishNodeId
        );
        _session.SetPlayerState(
            WorldDefinition.MoonwaterStarlightCell.X * 16 + 8,
            (WorldDefinition.MoonwaterStarlightCell.Y + 1) * 16 + 8,
            false
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(() => OpenStarlightPedestal(
            DataCatalog.MoonwaterStarlightId
        )).CallDeferred();
    }

    private void StartLioraEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.LioraFadedReturnRouteId
        );
    }

    private void StartLioraEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.LioraRememberedWayHomeId
        );
    }

    private void StartArchivePlaytest(
        bool openDialogue,
        bool giveGift
    )
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            day,
            minuteOfDay
        );
        if (liora is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            17 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        liora = PlacePlayerAdjacentForPlaytest(liora);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.MoonrootId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.MoonrootId
            );
        }

        _playing = true;
        EnsureHud();
        ShowArchive(false);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(liora.Position)
            ).CallDeferred();
        }
    }

    private void StartWorkshopDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.MoonstoneWorkshopDoorCell.X,
                VillageCatalog.MoonstoneWorkshopDoorCell.Y + 1
            )
        );
    }

    private void StartTaviEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.TaviCrackedMoonRuneId
        );
    }

    private void StartTaviEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.TaviMendedLightId
        );
    }

    private void StartNemiEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NemiUndeliverableLetterId
        );
    }

    private void StartNemiEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.NemiStarChartRouteId
        );
    }

    private void StartKaelEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.KaelBrokenBlueRuneId
        );
    }

    private void StartKaelEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.KaelSafeReturnRouteId
        );
    }

    private void StartSelaEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.SelaTemperedStarlightId
        );
    }

    private void StartSelaEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.SelaSharedForgeRhythmId
        );
    }

    private void StartOrinEventOnePlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.OrinUnpricedWaybillId
        );
    }

    private void StartOrinEventTwoPlaytest()
    {
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.OrinSharedLanternRouteId
        );
    }

    private void StartElowenEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.ElowenTideMarksAtTheWellId
        );

    private void StartElowenEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.ElowenWaterlineReadTogetherId
        );

    private void StartVessaEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaBitterLeafWarmCupId
        );

    private void StartVessaEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaPathThatListensBackId
        );

    private void StartVessaEventWrongToolPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.VessaBitterLeafWarmCupId,
            wrongTool: true
        );

    private void StartRelationshipMailsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMailPlaytest(
            [
                RelationshipMail(MailCatalog.KaelKindredId),
                RelationshipMail(MailCatalog.SelaKindredId),
                RelationshipMail(MailCatalog.ElowenKindredId),
                RelationshipMail(MailCatalog.VessaKindredId),
                RelationshipMail(MailCatalog.OrinKindredId)
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private void StartVillageExpansionWave3Playtest()
    {
        StartVillagePlaytestWorld(
            1,
            14 * 60,
            new GridPosition(97, 55)
        );
    }

    private void StartVillageExpansionWave3IndoorPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(1, 11 * 60);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        _session.Inventory.Select(0);
        var yvara = _session.Village.CurrentNpcs(
                1,
                11 * 60,
                PlayerLocationIds.TwilightEmporium,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.YvaraId
            );
        if (yvara is not null)
        {
            PlacePlayerAdjacentForPlaytest(yvara);
        }
        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
    }

    private void StartVillageExpansionWave3DialogueEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillageExpansionWave3FocusPlaytest(
            openDialogue: true,
            wrongTool: false
        );
    }

    private void StartVillageExpansionWave3WrongToolPlaytest() =>
        StartVillageExpansionWave3FocusPlaytest(
            openDialogue: false,
            wrongTool: true
        );

    private void StartVillageExpansionWave3FocusPlaytest(
        bool openDialogue,
        bool wrongTool
    )
    {
        const int day = 1;
        const int minuteOfDay = 14 * 60;
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerState(
            97 * 16 + 8,
            55 * 16 + 8,
            false
        );
        var yvara = _session.Village.CurrentNpcs(
                day,
                minuteOfDay,
                PlayerLocationIds.World,
                _session.PlayerCell
            )
            .FirstOrDefault(state =>
                state.Definition.Id == VillageCatalog.YvaraId
            );
        if (yvara is null)
        {
            StartVillagePlaytest();
            return;
        }
        yvara = PlacePlayerAdjacentForPlaytest(yvara);
        _session.Inventory.Select(wrongTool ? 1 : 0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (openDialogue)
        {
            Callable.From(
                () => TalkToVillager(yvara.Position)
            ).CallDeferred();
        }
    }

    private void StartYvaraEventOnePlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.YvaraSeedsBeyondTheCalendarId
        );

    private void StartYvaraEventTwoPlaytest() =>
        StartCatalogCharacterEventPlaytest(
            CharacterEventCatalog.YvaraASeasonCarriedGentlyId
        );

    private void StartWave3RelationshipMailsEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        PrepareMailPlaytest(
            [
                RelationshipMail(MailCatalog.YvaraKindredId),
                RelationshipMail(MailCatalog.BrialKindredId),
                RelationshipMail(MailCatalog.PavriKindredId),
                RelationshipMail(MailCatalog.RovenKindredId)
            ]
        );
        StartMailPlaytestWorld(true);
    }

    private static MailEntrySave RelationshipMail(string mailId) => new()
    {
        MailId = mailId,
        DeliveredDay = 3
    };

    private void StartCatalogCharacterEventPlaytest(
        string eventId,
        bool wrongTool = false
    )
    {
        var definition = CharacterEventCatalog.ById[eventId];
        var trigger = FindCharacterEventTrigger(definition);
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = trigger.Day;
        save.MinuteOfDay = trigger.Minute;
        save.Player.LocationId = definition.RequiredLocationId;
        save.Player.X = 8;
        save.Player.Y = 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [definition.NpcId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = definition.NpcId,
                    Points = definition.RequiredRelationshipPoints,
                    LastTalkDay = trigger.Day
                }
            ]
        };
        if (definition.RequiredPreviousEventId is not null)
        {
            save.CharacterEvents = new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId = definition.RequiredPreviousEventId,
                        CompletedDay = trigger.Day - 1
                    }
                ]
            };
        }
        _session.Restore(save);
        _session.Inventory.Select(wrongTool ? 1 : 0);

        var npc = _session.Village.CurrentNpcs(
                trigger.Day,
                trigger.Minute,
                definition.RequiredLocationId
            )
            .FirstOrDefault(state =>
                state.Definition.Id == definition.NpcId
            );
        if (npc is null)
        {
            StartVillagePlaytest();
            return;
        }
        npc = PlacePlayerAdjacentForPlaytest(npc);

        _playing = true;
        EnsureHud();
        ShowCharacterEventLocation(definition.RequiredLocationId);
        if (!wrongTool)
        {
            var target = npc.Position;
            Callable.From(() => TalkToVillager(target)).CallDeferred();
        }
    }

    private VillageNpcState PlacePlayerAdjacentForPlaytest(
        VillageNpcState npc
    )
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var current = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == npc.Definition.Id
                );
            var occupied = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Where(state =>
                    state.Definition.Id != npc.Definition.Id
                )
                .Select(state => state.Position)
                .ToHashSet();
            var approach = new[]
                {
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y + 1
                    ),
                    new GridPosition(
                        current.Position.X - 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X + 1,
                        current.Position.Y
                    ),
                    new GridPosition(
                        current.Position.X,
                        current.Position.Y - 1
                    )
                }
                .First(candidate =>
                    NpcNavigationMap.IsWalkableGeometry(
                        npc.LocationId,
                        candidate
                    ) &&
                    !NpcNavigationMap.IsCriticalEntranceCell(
                        npc.LocationId,
                        candidate
                    ) &&
                    !occupied.Contains(candidate)
                );
            _session.SetPlayerLocation(
                approach.X * 16 + 8,
                approach.Y * 16 + 8,
                npc.LocationId
            );
            var projected = _session.Village.CurrentNpcs(
                    _session.Clock.Day,
                    _session.Clock.MinuteOfDay,
                    npc.LocationId,
                    _session.PlayerCell
                )
                .Single(state =>
                    state.Definition.Id == npc.Definition.Id
                );
            if (Math.Abs(_session.PlayerCell.X - projected.Position.X) +
                Math.Abs(_session.PlayerCell.Y - projected.Position.Y) == 1)
            {
                return projected;
            }
        }

        throw new InvalidOperationException(
            $"Could not place player adjacent to {npc.Definition.Id}."
        );
    }

    private void ShowCharacterEventLocation(string locationId)
    {
        switch (locationId)
        {
            case PlayerLocationIds.World:
                ShowFarm(false);
                break;
            case PlayerLocationIds.MoonlitArchive:
                ShowArchive(false);
                break;
            case PlayerLocationIds.MoonstoneWorkshop:
                ShowWorkshop(false);
                break;
            case PlayerLocationIds.StarweaverTeaHouse:
                ShowTeaHouse(false);
                break;
            case PlayerLocationIds.TwilightEmporium:
                ShowTwilightEmporium(false);
                break;
            case PlayerLocationIds.StarlightPost:
                ShowStarlightPost(false);
                break;
            case PlayerLocationIds.StarfallWatch:
                ShowStarfallWatch(false);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported character event location: {locationId}."
                );
        }
    }

    private static (int Day, int Minute) FindCharacterEventTrigger(
        CharacterEventDefinition definition
    )
    {
        var firstDay = definition.RequiredPreviousEventId is null ? 1 : 2;
        var npc = VillageCatalog.Npcs[definition.NpcId];
        for (var day = firstDay;
             day <= CalendarSystem.DaysPerYear;
             day++)
        {
            var weatherId = WeatherSystem.WeatherForDay(day);
            for (var minute = GameClock.StartMinute;
                 minute < GameClock.EndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var entry = NpcScheduleSystem.SelectEntry(
                    npc,
                    day,
                    minute,
                    weatherId
                );
                if (entry?.LocationId == definition.RequiredLocationId &&
                    entry.DialogueKey == definition.RequiredNpcDialogueKey &&
                    minute >= entry.StartMinute + 60)
                {
                    return (day, minute);
                }
            }
        }

        throw new InvalidOperationException(
            $"No schedule trigger exists for {definition.Id}."
        );
    }

    private void StartWorkshopPlaytest()
    {
        StartWorkshopPlaytest(false);
    }

    private void StartWorkshopTaviPlaytest()
    {
        StartWorkshopPlaytest(true);
    }

    private void StartWorkshopPlaytest(bool giveGift)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var tavi = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            day,
            minuteOfDay
        );
        if (tavi is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            18 * 16 + 8,
            PlayerLocationIds.MoonstoneWorkshop
        );
        tavi = PlacePlayerAdjacentForPlaytest(tavi);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.LumenwoodId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.LumenwoodId
            );
        }

        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        if (giveGift)
        {
            Callable.From(
                () => TalkToVillager(tavi.Position)
            ).CallDeferred();
        }
    }

    private void StartTeaHouseDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.StarweaverTeaHouseDoorCell.X,
                VillageCatalog.StarweaverTeaHouseDoorCell.Y + 1
            )
        );
    }

    private void StartTeaHousePlaytest()
    {
        StartTeaHousePlaytest(false);
    }

    private void StartTeaHouseVessaPlaytest()
    {
        StartTeaHousePlaytest(true);
    }

    private void StartTeaHousePlaytest(bool giveGift)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var vessa = VillageCatalog.CurrentNpc(
            VillageCatalog.VessaId,
            day,
            minuteOfDay
        );
        if (vessa is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            10 * 16 + 8,
            PlayerLocationIds.StarweaverTeaHouse
        );
        vessa = PlacePlayerAdjacentForPlaytest(vessa);
        _session.Inventory.Select(0);
        if (giveGift)
        {
            _session.Inventory.Add(DataCatalog.CloudleafId, 2);
            _session.Inventory.PromoteToHotbar(
                DataCatalog.CloudleafId
            );
        }

        _playing = true;
        EnsureHud();
        ShowTeaHouse(false);
        if (giveGift)
        {
            Callable.From(
                () => TalkToVillager(vessa.Position)
            ).CallDeferred();
        }
    }

    private void StartEmporiumDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            10 * 60,
            new GridPosition(
                VillageCatalog.TwilightEmporiumDoorCell.X,
                VillageCatalog.TwilightEmporiumDoorCell.Y + 1
            )
        );
    }

    private void StartEmporiumRestdayDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            CalendarSystem.DaysPerWeek,
            10 * 60,
            new GridPosition(
                VillageCatalog.TwilightEmporiumDoorCell.X,
                VillageCatalog.TwilightEmporiumDoorCell.Y + 1
            )
        );
    }

    private void StartEmporiumRotationPlaytest()
    {
        StartEmporiumPlaytest(false);
        Callable.From(InspectTravelManifest).CallDeferred();
    }

    private void StartEmporiumPlaytest()
    {
        StartEmporiumPlaytest(false);
    }

    private void StartEmporiumOrinPlaytest()
    {
        StartEmporiumPlaytest(true);
    }

    private void StartEmporiumPlaytest(bool openOrinDialogue)
    {
        const int day = 1;
        const int minuteOfDay = 10 * 60;
        var orin = VillageCatalog.CurrentNpc(
            VillageCatalog.OrinId,
            day,
            minuteOfDay
        );
        if (orin is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.TwilightEmporium
        );
        orin = PlacePlayerAdjacentForPlaytest(orin);
        _session.Inventory.Select(0);

        _playing = true;
        EnsureHud();
        ShowTwilightEmporium(false);
        if (openOrinDialogue)
        {
            Callable.From(
                () => TalkToVillager(orin.Position)
            ).CallDeferred();
        }
    }

    private void StartStarlightPostDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            9 * 60,
            new GridPosition(
                VillageCatalog.StarlightPostDoorCell.X,
                VillageCatalog.StarlightPostDoorCell.Y + 1
            )
        );
    }

    private void StartStarlightPostPlaytest()
    {
        StartStarlightPostPlaytest(false, 0);
    }

    private void StartStarlightPostWrongToolPlaytest()
    {
        StartStarlightPostPlaytest(false, 1);
    }

    private void StartStarlightPostNemiPlaytest()
    {
        StartStarlightPostPlaytest(true, 0);
    }

    private void StartStarlightPostPlaytest(
        bool openNemiDialogue,
        int selectedSlot
    )
    {
        const int day = 1;
        const int minuteOfDay = 12 * 60;
        var nemi = VillageCatalog.CurrentNpc(
            VillageCatalog.NemiId,
            day,
            minuteOfDay
        );
        if (nemi is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            14 * 16 + 8,
            PlayerLocationIds.StarlightPost
        );
        nemi = PlacePlayerAdjacentForPlaytest(nemi);
        _session.Inventory.Select(selectedSlot);

        _playing = true;
        EnsureHud();
        ShowStarlightPost(false);
        if (openNemiDialogue)
        {
            Callable.From(
                () => TalkToVillager(nemi.Position)
            ).CallDeferred();
        }
    }

    private void StartStarfallWatchDoorPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            9 * 60,
            new GridPosition(
                VillageCatalog.StarfallWatchDoorCell.X,
                VillageCatalog.StarfallWatchDoorCell.Y + 1
            )
        );
    }

    private void StartStarfallWatchPlaytest()
    {
        StartStarfallWatchPlaytest(false, 0);
    }

    private void StartStarfallWatchWrongToolPlaytest()
    {
        StartStarfallWatchPlaytest(false, 1);
    }

    private void StartStarfallWatchKaelPlaytest()
    {
        StartStarfallWatchPlaytest(true, 0);
    }

    private void StartStarfallWatchPlaytest(
        bool openKaelDialogue,
        int selectedSlot
    )
    {
        const int day = 1;
        const int minuteOfDay = 12 * 60;
        var kael = VillageCatalog.CurrentNpc(
            VillageCatalog.KaelId,
            day,
            minuteOfDay
        );
        if (kael is null)
        {
            StartVillagePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerLocation(
            20 * 16 + 8,
            14 * 16 + 8,
            PlayerLocationIds.StarfallWatch
        );
        kael = PlacePlayerAdjacentForPlaytest(kael);
        _session.Inventory.Select(selectedSlot);

        _playing = true;
        EnsureHud();
        ShowStarfallWatch(false);
        if (openKaelDialogue)
        {
            Callable.From(
                () => TalkToVillager(kael.Position)
            ).CallDeferred();
        }
    }

    private void StartVillageRestdayEnglishPlaytest()
    {
        _locale.SetLocale(LocaleService.English);
        StartVillagePlaytestWorld(
            CalendarSystem.DaysPerWeek,
            14 * 60,
            new GridPosition(97, 50)
        );
    }

    private void StartVillageRainSchedulePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 1;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 27 * 16 + 8;
        save.Player.Y = 13 * 16 + 8;
        save.Village.MetNpcIds = [VillageCatalog.SelaId];
        _session.Restore(save);
        var sela = _session.Village.CurrentNpcs(
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.MoonstoneWorkshop
            )
            .SingleOrDefault(npc =>
                npc.Definition.Id == VillageCatalog.SelaId
            );

        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        if (sela is not null)
        {
            sela = PlacePlayerAdjacentForPlaytest(sela);
            Callable.From(
                () => TalkToVillager(sela.Position)
            ).CallDeferred();
        }
    }

    private void StartVillageRainveilSchedulePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = CalendarSystem.DaysPerSeason + 1;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = save.Day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.RainWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Village.MetNpcIds = [VillageCatalog.VessaId];
        _session.Restore(save);
        var vessa = _session.Village.CurrentNpcs(
                _session.Clock.Day,
                _session.Clock.MinuteOfDay,
                PlayerLocationIds.World
            )
            .SingleOrDefault(npc =>
                npc.Definition.Id == VillageCatalog.VessaId
            );
        if (vessa is not null)
        {
            vessa = PlacePlayerAdjacentForPlaytest(vessa);
        }

        _playing = true;
        EnsureHud();
        ShowFarm(false);
        if (vessa is not null)
        {
            Callable.From(
                () => TalkToVillager(vessa.Position)
            ).CallDeferred();
        }
    }

    private void StartVillagePlaytestWorld(
        int day,
        int minuteOfDay,
        GridPosition playerCell
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Clock.Reset(day, minuteOfDay);
        _session.SetPlayerState(
            playerCell.X * 16 + 8,
            playerCell.Y * 16 + 8,
            false
        );
        _session.Inventory.Select(0);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartWorldPlaytest()
    {
        StartWorldScenicPlaytest(VillageCatalog.VillageCenterCell);
    }

    private void StartWorldBeginnerArchPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(52, 52));

    private void StartWorldWoodsGrovePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(52, 116));

    private void StartWorldMeadowCirclePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(136, 17));

    private void StartWorldCrystalRidgePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(92, 178));

    private void StartWorldWetlandIsletPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(214, 60));

    private void StartWorldRuinsColonnadePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(184, 182));

    private void StartWorldFacilitiesGatewayPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(128, 110));

    private void StartWorldScenicPlaytest(GridPosition playerCell)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var safeCell = WorldDefinition.NearestWalkableCell(playerCell);
        _session.SetPlayerState(
            safeCell.X * 16 + 8,
            safeCell.Y * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartWorldAspectBoundaryPlaytest() =>
        StartWorldAspectPlaytest(14);

    private void StartRainveilWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(15);

    private void StartStarharvestWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(29);

    private void StartLongnightWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(43);

    private void StartRainveilWorldTreeRainPlaytest() =>
        StartWorldAspectResourcePlaytest(
            15,
            DataCatalog.RainWeatherId,
            WorldResourceKind.Tree,
            2
        );

    private void StartStarharvestWorldCrystalStardustPlaytest() =>
        StartWorldAspectResourcePlaytest(
            29,
            DataCatalog.StardustWindWeatherId,
            WorldResourceKind.Crystal,
            2
        );

    private void StartWorldAspectPlaytest(int day)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var playerCell = new GridPosition(70, 64);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartWorldAspectResourcePlaytest(
        int day,
        string weatherId,
        WorldResourceKind resourceKind,
        int selectedSlot
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var resource = FindResourceWithNorthernApproach(resourceKind);
        var playerCell = new GridPosition(resource.X, resource.Y - 1);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = weatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = selectedSlot;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGatePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(
            19 * 16 + 8,
            30 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartBackpackPlaytest()
    {
        StartNewGame();
        foreach (var seedId in DataCatalog.SeedItemIds)
        {
            _session.Inventory.Add(seedId, 7);
        }
        foreach (var cropId in DataCatalog.CropIds)
        {
            _session.Inventory.Add(cropId, 3);
        }
        _session.Inventory.Add(DataCatalog.LumenwoodId, 8);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 3);
        Callable.From(OpenBackpack).CallDeferred();
    }

    private void StartResourcePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var resource = FindResourceWithNorthernApproach(WorldResourceKind.Tree);
        _session.SetPlayerState(
            resource.X * 16 + 8,
            (resource.Y - 1) * 16 + 8,
            false
        );
        _session.Inventory.Select(2);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartTargetPreviewPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(12 * 16 + 8, 15 * 16 + 8, false);
        _session.Inventory.Select(1);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartPhaseAPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 5);
        _session.Inventory.Add(DataCatalog.MoonrootId, 3);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.SetPlayerState(
            FarmView.ShippingCell.X * 16 + 8,
            (FarmView.ShippingCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShipping).CallDeferred();
    }

    private void StartPhaseASummaryPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 3);
        _session.Inventory.Add(DataCatalog.MoonrootId, 2);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.QueueForShipping(DataCatalog.MoonrootId);
        _session.QueueForShipping(DataCatalog.StarbudPreserveId);
        _session.EndDay();
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(ShowNightlySummary).CallDeferred();
    }

    private void StartPhaseARainPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 2;
        save.Weather = new WeatherSave
        {
            Day = 2,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.SetPlayerState(
            FarmView.ShippingCell.X * 16 + 8,
            (FarmView.ShippingCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartResourceRespawnPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var crystal = FindResourceWithNorthernApproach(WorldResourceKind.Crystal);
        _session.Inventory.Select(1);
        _session.UseSelected(crystal);
        _session.EndDay();
        _session.EndDay();
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(ShowNightlySummary).CallDeferred();
    }

    private static GridPosition FindResourceWithNorthernApproach(
        WorldResourceKind resourceKind
    )
    {
        for (var y = FarmSystem.MapHeight + 1; y < WorldDefinition.Height - 1; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var resource = new GridPosition(x, y);
                var approach = new GridPosition(x, y - 1);
                if (WorldDefinition.ResourceAt(resource) == resourceKind &&
                    !WorldDefinition.IsBlocked(approach))
                {
                    return resource;
                }
            }
        }

        throw new InvalidOperationException(
            $"No approachable world resource found for {resourceKind}."
        );
    }

    private static FarmTileState CropState(int x, int y, string cropId, int wateredNights) =>
        new()
        {
            X = x,
            Y = y,
            Tilled = true,
            CropId = cropId,
            WateredNights = wateredNights
        };

    private void ContinueGame()
    {
        var result = _saveService.Load();
        if (result.Status != SaveLoadStatus.Loaded || result.Save is null)
        {
            var notice = result.Status is SaveLoadStatus.Corrupt or SaveLoadStatus.Unsupported
                ? "menu.corrupt_save"
                : "menu.no_save";
            ShowTitle(notice);
            return;
        }

        FreeUi(_title);
        _title = null;
        if (_titleLanguageOverridden)
        {
            result.Save.Locale = _locale.CurrentLocale;
            _saveService.Save(result.Save);
        }
        else
        {
            _locale.SetLocale(result.Save.Locale);
        }
        _session.Restore(result.Save);
        _playing = true;
        EnsureHud();
        if (!string.IsNullOrWhiteSpace(result.PreservedPath))
        {
            _hud?.ShowNotice("notice.save_recovered", 3.2);
        }
        if (_session.InsideCottage)
        {
            ShowCottage(false);
        }
        else if (_session.InsideGreenhouse)
        {
            ShowGreenhouse(false);
        }
        else if (_session.InsideStarfeatherCoop)
        {
            ShowStarfeatherCoop(false);
        }
        else if (_session.InsideMoonfleeceBarn)
        {
            ShowMoonfleeceBarn(false);
        }
        else if (_session.InsideArchive)
        {
            ShowArchive(false);
        }
        else if (_session.InsideWorkshop)
        {
            ShowWorkshop(false);
        }
        else if (_session.InsideTeaHouse)
        {
            ShowTeaHouse(false);
        }
        else if (_session.InsideTwilightEmporium)
        {
            ShowTwilightEmporium(false);
        }
        else if (_session.InsideStarlightPost)
        {
            ShowStarlightPost(false);
        }
        else if (_session.InsideStarfallWatch)
        {
            ShowStarfallWatch(false);
        }
        else if (_session.InsideStarharvestMarket)
        {
            ShowStarharvestMarket(false);
        }
        else if (_session.InsideGleamrisePlantingFestival)
        {
            ShowGleamrisePlantingFestival(false);
        }
        else if (_session.InsideLongnightLanternFeast)
        {
            ShowLongnightLanternFeast(false);
        }
        else if (_session.InsideFireflyTide)
        {
            ShowFireflyTide(false);
        }
        else if (_session.InsideCrystalGrottoSurvey)
        {
            ShowCrystalGrotto(false);
        }
        else if (_session.InsideStarfallRuinsTrial)
        {
            ShowStarfallRuinsTrial(false);
        }
        else
        {
            ShowFarm(false);
        }
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
    }

    private void EnsureHud()
    {
        if (_hud is not null)
        {
            return;
        }

        _hud = new HudView(_theme, _session, _locale);
        _uiLayer.AddChild(_hud);
    }

    private void ShowFarm(
        bool fromCottage,
        bool fromArchive = false,
        bool fromWorkshop = false,
        bool fromTeaHouse = false,
        bool fromTwilightEmporium = false,
        bool fromStarlightPost = false,
        bool fromStarfallWatch = false,
        bool fromGreenhouse = false,
        bool fromStarfeatherCoop = false,
        bool fromMoonfleeceBarn = false,
        bool fromStarharvestMarket = false,
        bool fromGleamrisePlantingFestival = false,
        bool fromLongnightLanternFeast = false,
        bool fromFireflyTide = false,
        bool fromCrystalGrotto = false,
        bool fromStarfallRuinsTrial = false
    )
    {
        ClearWorld();
        if (fromCottage)
        {
            _session.SetPlayerState(
                FarmView.CottageDoorCell.X * 16 + 8,
                (FarmView.CottageDoorCell.Y + 1) * 16 + 8,
                false
            );
        }
        else if (fromGreenhouse)
        {
            _session.SetPlayerLocation(
                FarmLayout.GreenhouseReturnCell.X * 16 + 8,
                FarmLayout.GreenhouseReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfeatherCoop)
        {
            _session.SetPlayerLocation(
                FarmLayout.StarfeatherCoopReturnCell.X * 16 + 8,
                FarmLayout.StarfeatherCoopReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromMoonfleeceBarn)
        {
            _session.SetPlayerLocation(
                FarmLayout.MoonfleeceBarnReturnCell.X * 16 + 8,
                FarmLayout.MoonfleeceBarnReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromArchive)
        {
            _session.SetPlayerLocation(
                VillageCatalog.MoonlitArchiveDoorCell.X * 16 + 8,
                (VillageCatalog.MoonlitArchiveDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromWorkshop)
        {
            _session.SetPlayerLocation(
                VillageCatalog.MoonstoneWorkshopDoorCell.X * 16 + 8,
                (VillageCatalog.MoonstoneWorkshopDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromTeaHouse)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarweaverTeaHouseDoorCell.X * 16 + 8,
                (VillageCatalog.StarweaverTeaHouseDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromTwilightEmporium)
        {
            _session.SetPlayerLocation(
                VillageCatalog.TwilightEmporiumDoorCell.X * 16 + 8,
                (VillageCatalog.TwilightEmporiumDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarlightPost)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarlightPostDoorCell.X * 16 + 8,
                (VillageCatalog.StarlightPostDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfallWatch)
        {
            _session.SetPlayerLocation(
                VillageCatalog.StarfallWatchDoorCell.X * 16 + 8,
                (VillageCatalog.StarfallWatchDoorCell.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarharvestMarket)
        {
            _session.SetPlayerLocation(
                StarharvestMarketLayout.WorldReturnCell.X * 16 + 8,
                StarharvestMarketLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromGleamrisePlantingFestival)
        {
            _session.SetPlayerLocation(
                GleamrisePlantingFestivalLayout.WorldReturnCell.X * 16 + 8,
                GleamrisePlantingFestivalLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromLongnightLanternFeast)
        {
            _session.SetPlayerLocation(
                LongnightLanternFeastLayout.WorldReturnCell.X * 16 + 8,
                LongnightLanternFeastLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromFireflyTide)
        {
            _session.SetPlayerLocation(
                FireflyTideLayout.WorldReturnCell.X * 16 + 8,
                FireflyTideLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromCrystalGrotto)
        {
            _session.SetPlayerLocation(
                CrystalGrottoSurveyLayout.WorldReturnCell.X * 16 + 8,
                CrystalGrottoSurveyLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }
        else if (fromStarfallRuinsTrial)
        {
            _session.SetPlayerLocation(
                StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8,
                StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8,
                PlayerLocationIds.World
            );
        }

        _farm = new FarmView(_session, _locale);
        _farm.UseRequested += UseFarmTarget;
        _farm.MiraRequested += TalkToMira;
        _farm.EnterCottageRequested += () => ShowCottage(true);
        _farm.EnterGreenhouseRequested += TryEnterGreenhouse;
        _farm.EnterStarfeatherCoopRequested += TryEnterStarfeatherCoop;
        _farm.EnterMoonfleeceBarnRequested += TryEnterMoonfleeceBarn;
        _farm.EnterArchiveRequested += TryEnterMoonlitArchive;
        _farm.EnterWorkshopRequested += TryEnterMoonstoneWorkshop;
        _farm.EnterTeaHouseRequested += TryEnterStarweaverTeaHouse;
        _farm.EnterTwilightEmporiumRequested +=
            TryEnterTwilightEmporium;
        _farm.EnterStarlightPostRequested += TryEnterStarlightPost;
        _farm.EnterStarfallWatchRequested += TryEnterStarfallWatch;
        _farm.EnterStarharvestMarketRequested +=
            TryEnterStarharvestMarket;
        _farm.EnterGleamrisePlantingFestivalRequested +=
            TryEnterGleamrisePlantingFestival;
        _farm.EnterLongnightLanternFeastRequested +=
            TryEnterLongnightLanternFeast;
        _farm.EnterFireflyTideRequested += TryEnterFireflyTide;
        _farm.EnterCrystalGrottoRequested += TryEnterCrystalGrotto;
        _farm.EnterStarfallRuinsRequested += TryEnterStarfallRuinsTrial;
        _farm.ShopRequested += OpenShop;
        _farm.ProcessorRequested += OpenProcessor;
        _farm.ShippingRequested += OpenShipping;
        _farm.CommissionRequested += OpenCommissionBoard;
        _farm.MailRequested += OpenStarlightMail;
        _farm.StarlightRequested += OpenStarlightPedestal;
        _farm.VillagerRequested += TalkToVillager;
        _farm.StorageRequested += OpenStorage;
        _farm.HomesteadWorkbenchRequested +=
            OpenHomesteadConstructionPanel;
        _farm.NoticeRequested += key => _hud?.ShowNotice(key);
        _farm.RegionEntered += key => _hud?.ShowNotice(key, 2.6);
        _farm.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _farm;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromCottage)
        {
            _hud?.ShowNotice("notice.leave_cottage");
        }
        else if (fromArchive)
        {
            _hud?.ShowNotice("notice.leave_archive");
        }
        else if (fromWorkshop)
        {
            _hud?.ShowNotice("notice.leave_workshop");
        }
        else if (fromTeaHouse)
        {
            _hud?.ShowNotice("notice.leave_tea_house");
        }
        else if (fromTwilightEmporium)
        {
            _hud?.ShowNotice("notice.leave_emporium");
        }
        else if (fromStarlightPost)
        {
            _hud?.ShowNotice("notice.leave_starlight_post");
        }
        else if (fromStarfallWatch)
        {
            _hud?.ShowNotice("notice.leave_starfall_watch");
        }
        else if (fromGreenhouse)
        {
            _hud?.ShowNotice("notice.leave_greenhouse");
        }
        else if (fromStarfeatherCoop)
        {
            _hud?.ShowNotice("notice.leave_starfeather_coop");
        }
        else if (fromMoonfleeceBarn)
        {
            _hud?.ShowNotice("notice.leave_moonfleece_barn");
        }
        else if (fromStarharvestMarket)
        {
            _hud?.ShowNotice("notice.leave_starharvest_market");
        }
        else if (fromLongnightLanternFeast)
        {
            _hud?.ShowNotice("notice.leave_longnight_feast");
        }
        else if (fromFireflyTide)
        {
            _hud?.ShowNotice("notice.leave_firefly_tide");
        }
        else if (fromCrystalGrotto)
        {
            _hud?.ShowNotice("mining.survey.exit");
        }
        else if (fromStarfallRuinsTrial)
        {
            _hud?.ShowNotice("ruins.trial.exit");
        }
    }

    private void ShowCottage(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerState(20 * 16 + 8, 17 * 16 + 8, true);
        }

        _cottage = new CottageView(_session, _locale);
        _cottage.SleepRequested += EndDay;
        _cottage.ExitRequested += () => ShowFarm(true);
        _cottage.KitchenReserveRequested += InspectKitchenReserve;
        _cottage.KitchenRequested += OpenKitchen;
        _cottage.IngredientPantryRequested += OpenIngredientPantry;
        _cottage.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _cottage;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(fromFarm ? "notice.enter_cottage" : string.Empty);
    }

    private void ShowGreenhouse(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                GreenhouseLayout.SafeArrivalCell.X * 16 + 8,
                GreenhouseLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.Greenhouse
            );
        }

        _greenhouse = new GreenhouseView(_session, _locale);
        _greenhouse.UseRequested += UseFarmTarget;
        _greenhouse.ExitRequested += () =>
            ShowFarm(false, fromGreenhouse: true);
        _greenhouse.NoticeRequested += key => _hud?.ShowNotice(key);
        _greenhouse.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _greenhouse;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_greenhouse" : string.Empty
        );
    }

    private void ShowCrystalGrotto(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                CrystalGrottoSurveyLayout.SafeArrivalCell.X * 16 + 8,
                CrystalGrottoSurveyLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.CrystalGrottoSurvey
            );
        }

        _crystalGrotto = new CrystalGrottoView(_session, _locale);
        _crystalGrotto.UseRequested += UseFarmTarget;
        _crystalGrotto.UpgradeRequested += OpenToolUpgrade;
        _crystalGrotto.ExitRequested += () => ShowFarm(
            false,
            fromCrystalGrotto: true
        );
        _crystalGrotto.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _crystalGrotto.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _crystalGrotto;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("mining.survey.enter");
        }
    }

    private void ShowStarfallRuinsTrial(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8,
                StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarfallRuinsTrial
            );
        }

        _starfallRuinsTrial = new StarfallRuinsTrialView(
            _session,
            _locale
        );
        _starfallRuinsTrial.ExitRequested += () =>
        {
            SaveNow(false);
            ShowFarm(false, fromStarfallRuinsTrial: true);
        };
        _starfallRuinsTrial.DefeatRequested += () =>
            ResolveStarfallTrialDefeat(forcedByClosingTime: false);
        _starfallRuinsTrial.ProgressChanged += () => SaveNow(false);
        _starfallRuinsTrial.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starfallRuinsTrial.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starfallRuinsTrial;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("ruins.trial.enter");
        }
    }

    private void ShowStarfeatherCoop(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                StarfeatherCoopLayout.SafeArrivalCell.X * 16 + 8,
                StarfeatherCoopLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarfeatherCoop
            );
        }

        _starfeatherCoop = new StarfeatherCoopView(_session, _locale);
        _starfeatherCoop.UseRequested += UseFarmTarget;
        _starfeatherCoop.ExitRequested += () => ShowFarm(
            false,
            fromStarfeatherCoop: true
        );
        _starfeatherCoop.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starfeatherCoop.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starfeatherCoop;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_starfeather_coop" : string.Empty
        );
    }

    private void ShowMoonfleeceBarn(bool fromFarm)
    {
        ClearWorld();
        if (fromFarm)
        {
            _session.SetPlayerLocation(
                MoonfleeceBarnLayout.SafeArrivalCell.X * 16 + 8,
                MoonfleeceBarnLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.MoonfleeceBarn
            );
        }

        _moonfleeceBarn = new MoonfleeceBarnView(_session, _locale);
        _moonfleeceBarn.UseRequested += UseFarmTarget;
        _moonfleeceBarn.ExitRequested += () => ShowFarm(
            false,
            fromMoonfleeceBarn: true
        );
        _moonfleeceBarn.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _moonfleeceBarn.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _moonfleeceBarn;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(
            fromFarm ? "notice.enter_moonfleece_barn" : string.Empty
        );
    }

    private void ShowStarharvestMarket(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                StarharvestMarketLayout.SafeArrivalCell.X * 16 + 8,
                StarharvestMarketLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.StarharvestMarket
            );
        }

        _starharvestMarket = new StarharvestMarketView(
            _session,
            _locale
        );
        _starharvestMarket.ExitRequested += () => ShowFarm(
            false,
            fromStarharvestMarket: true
        );
        _starharvestMarket.ClosedRequested += () =>
        {
            CloseFestivalShowcase();
            CloseFestivalShop();
            ShowFarm(false);
            _hud?.ShowNotice("festival.starharvest.closed");
        };
        _starharvestMarket.ShowcaseRequested += OpenFestivalShowcase;
        _starharvestMarket.ShopRequested += OpenFestivalShop;
        _starharvestMarket.VillagerRequested += TalkToVillager;
        _starharvestMarket.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _starharvestMarket.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _starharvestMarket;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starharvest_market");
        }
    }

    private void ShowGleamrisePlantingFestival(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                GleamrisePlantingFestivalLayout.SafeArrivalCell.X * 16 + 8,
                GleamrisePlantingFestivalLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.GleamrisePlantingFestival
            );
        }

        _gleamrisePlantingFestival = new GleamrisePlantingFestivalView(
            _session,
            _locale
        );
        _gleamrisePlantingFestival.ExitRequested += () => ShowFarm(
            false,
            fromGleamrisePlantingFestival: true
        );
        _gleamrisePlantingFestival.ClosedRequested += () =>
        {
            CloseGleamrisePlanting();
            CloseGleamriseSeedExchange();
            ShowFarm(false);
            _hud?.ShowNotice("festival.gleamrise.closed");
        };
        _gleamrisePlantingFestival.ActivityRequested +=
            OpenGleamrisePlanting;
        _gleamrisePlantingFestival.ExchangeRequested +=
            OpenGleamriseSeedExchange;
        _gleamrisePlantingFestival.VillagerRequested += TalkToVillager;
        _gleamrisePlantingFestival.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _gleamrisePlantingFestival.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _gleamrisePlantingFestival;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_gleamrise_festival");
        }
    }

    private void ShowLongnightLanternFeast(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                LongnightLanternFeastLayout.SafeArrivalCell.X * 16 + 8,
                LongnightLanternFeastLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.LongnightLanternFeast
            );
        }

        _longnightLanternFeast = new LongnightLanternFeastView(
            _session,
            _locale
        );
        _longnightLanternFeast.ExitRequested += () => ShowFarm(
            false,
            fromLongnightLanternFeast: true
        );
        _longnightLanternFeast.ClosedRequested += () =>
        {
            CloseLongnightFeast();
            CloseLongnightStall();
            ShowFarm(false);
            _hud?.ShowNotice("festival.longnight.closed");
        };
        _longnightLanternFeast.ActivityRequested += OpenLongnightFeast;
        _longnightLanternFeast.StallRequested += OpenLongnightStall;
        _longnightLanternFeast.VillagerRequested += TalkToVillager;
        _longnightLanternFeast.NoticeRequested += key =>
            _hud?.ShowNotice(key);
        _longnightLanternFeast.StepRequested += () =>
            _audio.Play(PixelSound.Step);
        _world = _longnightLanternFeast;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_longnight_feast");
        }
    }

    private void ShowFireflyTide(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                FireflyTideLayout.SafeArrivalCell.X * 16 + 8,
                FireflyTideLayout.SafeArrivalCell.Y * 16 + 8,
                PlayerLocationIds.FireflyTide
            );
        }

        _fireflyTide = new FireflyTideView(_session, _locale);
        _fireflyTide.ExitRequested += () => ShowFarm(
            false,
            fromFireflyTide: true
        );
        _fireflyTide.ClosedRequested += () =>
        {
            CloseFireflyTideActivity();
            CloseFireflyTideShop();
            ShowFarm(false);
            _hud?.ShowNotice("festival.firefly.closed");
        };
        _fireflyTide.ActivityRequested += OpenFireflyTideActivity;
        _fireflyTide.ShopRequested += OpenFireflyTideShop;
        _fireflyTide.VillagerRequested += TalkToVillager;
        _fireflyTide.NoticeRequested += key => _hud?.ShowNotice(key);
        _fireflyTide.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _fireflyTide;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_firefly_tide");
        }
    }

    private void ShowArchive(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                17 * 16 + 8,
                PlayerLocationIds.MoonlitArchive
            );
        }

        _archive = new ArchiveView(_session, _locale);
        _archive.ExitRequested += TryLeaveMoonlitArchive;
        _archive.DeskRequested += OpenCropCodex;
        _archive.VillagerRequested += TalkToVillager;
        _archive.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _archive;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_archive");
        }
    }

    private void ShowWorkshop(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.MoonstoneWorkshop
            );
        }

        _workshop = new WorkshopView(_session, _locale);
        _workshop.ExitRequested += TryLeaveMoonstoneWorkshop;
        _workshop.WorkbenchRequested += OpenConstructionPanel;
        _workshop.VillagerRequested += TalkToVillager;
        _workshop.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _workshop;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_workshop");
        }
    }

    private void ShowTeaHouse(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarweaverTeaHouse
            );
        }

        _teaHouse = new TeaHouseView(_session, _locale);
        _teaHouse.ExitRequested += TryLeaveStarweaverTeaHouse;
        _teaHouse.TeaCounterRequested += InspectStarwovenTeaCounter;
        _teaHouse.VillagerRequested += TalkToVillager;
        _teaHouse.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _teaHouse;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_tea_house");
        }
    }

    private void ShowTwilightEmporium(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.TwilightEmporium
            );
        }

        _twilightEmporium = new TwilightEmporiumView(
            _session,
            _locale
        );
        _twilightEmporium.ExitRequested += TryLeaveTwilightEmporium;
        _twilightEmporium.ManifestRequested += InspectTravelManifest;
        _twilightEmporium.VillagerRequested += TalkToVillager;
        _twilightEmporium.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _twilightEmporium;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_emporium");
        }
    }

    private void ShowStarlightPost(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                20 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarlightPost
            );
        }

        _starlightPost = new StarlightPostView(
            _session,
            _locale
        );
        _starlightPost.ExitRequested += TryLeaveStarlightPost;
        _starlightPost.SortingCounterRequested +=
            InspectRouteSortingCounter;
        _starlightPost.VillagerRequested += TalkToVillager;
        _starlightPost.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _starlightPost;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starlight_post");
        }
    }

    private void ShowStarfallWatch(bool fromWorld)
    {
        ClearWorld();
        if (fromWorld)
        {
            _session.SetPlayerLocation(
                19 * 16 + 8,
                18 * 16 + 8,
                PlayerLocationIds.StarfallWatch
            );
        }

        _starfallWatch = new StarfallWatchView(
            _session,
            _locale
        );
        _starfallWatch.ExitRequested += TryLeaveStarfallWatch;
        _starfallWatch.SealRouteTableRequested +=
            InspectSealRouteTable;
        _starfallWatch.VillagerRequested += TalkToVillager;
        _starfallWatch.StepRequested +=
            () => _audio.Play(PixelSound.Step);
        _world = _starfallWatch;
        AddChild(_world);
        MoveChild(_world, 1);
        if (fromWorld)
        {
            _hud?.ShowNotice("notice.enter_starfall_watch");
        }
    }

    private void UseFarmTarget(GridPosition target)
    {
        var selectedId = _session.Inventory.Selected.ItemId;
        if (selectedId == DataCatalog.FishingRodId &&
            WorldDefinition.IsWaterSource(target))
        {
            OpenFishingMinigame(target);
            return;
        }

        if (target == CrystalGrottoSurveyLayout.DepthAnchorCell &&
            _session.InsideCrystalGrottoSurvey)
        {
            var anchorResult = _session.UseSelected(target);
            if (!anchorResult.Succeeded)
            {
                _hud?.ShowNotice(anchorResult.MessageKey);
                return;
            }

            _hud?.ShowNotice(anchorResult.MessageKey);
            if (_session.DeepMine.Active)
            {
                OpenDeepMine();
            }
            return;
        }

        var result = _session.UseSelected(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        if (result.MessageKey == "star_gate.travel_opened")
        {
            OpenStarGate();
            return;
        }

        if (result.MessageKey == "star_gate.activated")
        {
            SaveNow(false);
        }

        if (result.MessageKey == "animal.automation.panel.opened" &&
            _session.CurrentAnimalBuildingId is { } buildingId &&
            AnimalBuildingSpatialCatalog.TryByBuildingId(
                buildingId,
                out var spatial
            ))
        {
            OpenLivestockAutomation(
                buildingId,
                spatial.AutomationStationCell
            );
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.MessageKey))
        {
            _hud?.ShowNotice(result.MessageKey);
        }

        _audio.Play(ResolveFarmActionSound(result, selectedId));
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
    }

    private static PixelSound ResolveFarmActionSound(
        ActionResult result,
        string selectedId
    )
    {
        if (result.GrantedItemId is not null)
        {
            return PixelSound.Harvest;
        }

        if (DataCatalog.Items.TryGetValue(selectedId, out var selectedItem) &&
            selectedItem.Kind == ItemKind.Seed)
        {
            return PixelSound.Plant;
        }

        return selectedId switch
        {
            DataCatalog.ShovelId => PixelSound.Till,
            DataCatalog.MacheteId => PixelSound.Harvest,
            DataCatalog.WateringCanId => PixelSound.Water,
            DataCatalog.BucketId => PixelSound.Water,
            DataCatalog.FishingRodId => PixelSound.Water,
            _ => PixelSound.Chime
        };
    }

    private void TryOpenFarmingSpecialization()
    {
        if (!_playing ||
            !_session.FarmingSkill.CanChooseSpecialization ||
            _farmingSpecializationOverlay is not null ||
            !CanRestoreWorldControls)
        {
            return;
        }

        SetWorldControls(false);
        _farmingSpecializationOverlay = new FarmingSpecializationOverlay(
            _theme,
            _locale
        );
        _farmingSpecializationOverlay.SelectionRequested +=
            ChooseFarmingSpecialization;
        _uiLayer.AddChild(_farmingSpecializationOverlay);
    }

    private void ChooseFarmingSpecialization(string specializationId)
    {
        var result = _session.ChooseFarmingSpecialization(specializationId);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        SaveNow(false);
        _audio.Play(PixelSound.Chime);
        FreeUi(_farmingSpecializationOverlay);
        _farmingSpecializationOverlay = null;
        _hud?.ShowNotice(result.MessageKey);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TalkToMira()
    {
        var stage = _session.Quest.Stage;
        var dialogueKey = stage switch
        {
            QuestStage.TalkToMira => "dialogue.mira.offer",
            QuestStage.Till or QuestStage.Plant => "dialogue.mira.planting",
            QuestStage.Water or QuestStage.Grow => "dialogue.mira.growing",
            QuestStage.Harvest => "dialogue.mira.harvest",
            QuestStage.ReturnToMira => "dialogue.mira.return",
            QuestStage.Complete => "dialogue.mira.complete",
            _ => "dialogue.mira.planting"
        };

        ShowDialogue("dialogue.mira.name", dialogueKey, () =>
        {
            if (stage == QuestStage.TalkToMira)
            {
                _session.InteractWithMira();
                _audio.Play(PixelSound.Chime);
                _hud?.ShowNotice("notice.seeds_received");
            }
            else if (stage == QuestStage.ReturnToMira)
            {
                _session.InteractWithMira();
                _audio.Play(PixelSound.Chime);
                SaveNow(false);
                ShowCompletion();
            }
        });
    }

    private void TalkToVillager(GridPosition target)
    {
        var conversation = _session.InteractWithVillager(
            target,
            out var result
        );
        if (!result.Succeeded || conversation is null)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        var icon = conversation.GiftReaction is { } giftReaction
            ? GeneratedArt.GiftReactionIcon(giftReaction)
            : GeneratedArt.RelationshipIcon(
                conversation.RelationshipTier
            );
        var relationshipStatus = string.Format(
            _locale.Tr("village.relationship.progress"),
            _locale.Tr(RelationshipTierKey(
                conversation.RelationshipTier
            )),
            conversation.RelationshipPoints,
            VillageSystem.MaximumRelationshipPoints
        );
        var characterEvent = conversation.CharacterEvent;
        var dialogueKeys = characterEvent is not null
            ? characterEvent.DialogueKeys
            : [conversation.DialogueKey];
        ShowDialoguePages(
            conversation.NameKey,
            dialogueKeys,
            () =>
            {
                if (characterEvent is not null)
                {
                    var completion = _session.CompleteCharacterEvent(
                        characterEvent.EventId
                    );
                    if (!completion.Succeeded)
                    {
                        _hud?.ShowNotice(completion.MessageKey);
                        return;
                    }
                }

                SaveNow(false);
            },
            icon,
            relationshipStatus
        );
    }

    private void TryEnterMoonlitArchive()
    {
        var result = _session.TryEnterMoonlitArchive();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowArchive(true);
    }

    private void TryEnterGreenhouse()
    {
        var result = _session.TryEnterGreenhouse(
            FarmLayout.GreenhouseDoorCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowGreenhouse(true);
    }

    private void TryEnterCrystalGrotto()
    {
        var result = _session.TryEnterCrystalGrottoSurvey(
            CrystalGrottoSurveyLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowCrystalGrotto(true);
    }

    private void TryEnterStarfallRuinsTrial()
    {
        var result = _session.TryEnterStarfallRuinsTrial(
            StarfallRuinsTrialLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarfallRuinsTrial(true);
    }

    private void TryEnterStarfeatherCoop()
    {
        var result = _session.TryEnterStarfeatherCoop(
            FarmLayout.StarfeatherCoopDoorCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarfeatherCoop(true);
    }

    private void TryEnterMoonfleeceBarn()
    {
        var result = _session.TryEnterAnimalBuilding(
            AnimalCatalog.MoonfleeceBarnId,
            FarmLayout.MoonfleeceBarnDoorCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowMoonfleeceBarn(true);
    }

    private void TryEnterStarharvestMarket()
    {
        var result = _session.TryEnterStarharvestMarket(
            StarharvestMarketLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarharvestMarket(true);
    }

    private void TryEnterGleamrisePlantingFestival()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.GleamrisePlantingFestivalId,
            GleamrisePlantingFestivalLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowGleamrisePlantingFestival(true);
    }

    private void TryEnterLongnightLanternFeast()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            LongnightLanternFeastLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowLongnightLanternFeast(true);
    }

    private void TryEnterFireflyTide()
    {
        var result = _session.TryEnterFestival(
            FestivalCatalog.FireflyTideFestivalId,
            FireflyTideLayout.WorldEntryCell
        );
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowFireflyTide(true);
    }

    private void OpenFestivalShowcase()
    {
        if (_festivalShowcaseOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _festivalShowcaseOverlay = new FestivalShowcaseOverlay(
            _theme,
            _session,
            _locale
        );
        _festivalShowcaseOverlay.CloseRequested +=
            CloseFestivalShowcase;
        _festivalShowcaseOverlay.SubmissionCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_festivalShowcaseOverlay);
    }

    private void CloseFestivalShowcase()
    {
        FreeUi(_festivalShowcaseOverlay);
        _festivalShowcaseOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFestivalShop()
    {
        if (_festivalShopOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _festivalShopOverlay = new FestivalShopOverlay(
            _theme,
            _session,
            _locale
        );
        _festivalShopOverlay.CloseRequested += CloseFestivalShop;
        _festivalShopOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_festivalShopOverlay);
    }

    private void CloseFestivalShop()
    {
        FreeUi(_festivalShopOverlay);
        _festivalShopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenGleamrisePlanting()
    {
        if (_gleamrisePlantingOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _gleamrisePlantingOverlay = new GleamrisePlantingOverlay(
            _theme,
            _session,
            _locale
        );
        _gleamrisePlantingOverlay.CloseRequested +=
            CloseGleamrisePlanting;
        _uiLayer.AddChild(_gleamrisePlantingOverlay);
    }

    private void CloseGleamrisePlanting()
    {
        FreeUi(_gleamrisePlantingOverlay);
        _gleamrisePlantingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenGleamriseSeedExchange()
    {
        if (_gleamriseSeedExchangeOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _gleamriseSeedExchangeOverlay = new GleamriseSeedExchangeOverlay(
            _theme,
            _session,
            _locale
        );
        _gleamriseSeedExchangeOverlay.CloseRequested +=
            CloseGleamriseSeedExchange;
        _uiLayer.AddChild(_gleamriseSeedExchangeOverlay);
    }

    private void CloseGleamriseSeedExchange()
    {
        FreeUi(_gleamriseSeedExchangeOverlay);
        _gleamriseSeedExchangeOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenLongnightFeast(GridPosition sourceCell)
    {
        if (_longnightFeastOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _longnightFeastOverlay = new LongnightLanternFeastOverlay(
            _theme,
            _session,
            _locale,
            sourceCell
        );
        _longnightFeastOverlay.CloseRequested += CloseLongnightFeast;
        _longnightFeastOverlay.ParticipationCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_longnightFeastOverlay);
    }

    private void CloseLongnightFeast()
    {
        FreeUi(_longnightFeastOverlay);
        _longnightFeastOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenLongnightStall()
    {
        if (_longnightStallOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _longnightStallOverlay = new LongnightLanternStallOverlay(
            _theme,
            _session,
            _locale,
            LongnightLanternFeastLayout.StallCell
        );
        _longnightStallOverlay.CloseRequested += CloseLongnightStall;
        _longnightStallOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_longnightStallOverlay);
    }

    private void CloseLongnightStall()
    {
        FreeUi(_longnightStallOverlay);
        _longnightStallOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFireflyTideActivity(GridPosition sourceCell)
    {
        if (_fireflyTideOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _fireflyTideOverlay = new FireflyTideOverlay(
            _theme,
            _session,
            _locale,
            sourceCell
        );
        _fireflyTideOverlay.CloseRequested += CloseFireflyTideActivity;
        _fireflyTideOverlay.ParticipationCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fireflyTideOverlay);
    }

    private void CloseFireflyTideActivity()
    {
        FreeUi(_fireflyTideOverlay);
        _fireflyTideOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFireflyTideShop()
    {
        if (_fireflyTideShopOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _fireflyTideShopOverlay = new FireflyTideShopOverlay(
            _theme,
            _session,
            _locale,
            FireflyTideLayout.ShopCell
        );
        _fireflyTideShopOverlay.CloseRequested += CloseFireflyTideShop;
        _fireflyTideShopOverlay.PurchaseCompleted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fireflyTideShopOverlay);
    }

    private void CloseFireflyTideShop()
    {
        FreeUi(_fireflyTideShopOverlay);
        _fireflyTideShopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TryLeaveMoonlitArchive()
    {
        var result = _session.TryExitMoonlitArchive();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromArchive: true);
    }

    private void OpenCropCodex(GridPosition target)
    {
        OpenCompendium(target, CollectionCategoryIds.Crops);
    }

    private void OpenCompendium(
        GridPosition target,
        string initialCategoryId
    )
    {
        if (_compendiumOverlay is not null)
        {
            return;
        }

        var result = _session.OpenMoonlitArchiveCompendium(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _compendiumOverlay = new CompendiumOverlay(
            _theme,
            _session,
            _locale,
            target,
            initialCategoryId
        );
        _compendiumOverlay.CloseRequested += CloseCropCodex;
        _compendiumOverlay.RewardClaimed += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _compendiumOverlay.FishingDonationRequested += () =>
        {
            CloseCropCodex();
            OpenFishingDonation();
        };
        _uiLayer.AddChild(_compendiumOverlay);
    }

    private void CloseCropCodex()
    {
        FreeUi(_compendiumOverlay);
        _compendiumOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TryEnterMoonstoneWorkshop()
    {
        var result = _session.TryEnterMoonstoneWorkshop();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowWorkshop(true);
    }

    private void TryLeaveMoonstoneWorkshop()
    {
        var result = _session.TryExitMoonstoneWorkshop();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromWorkshop: true);
    }

    private void OpenConstructionPanel()
    {
        var result = _session.OpenConstructionPanel();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        OpenConstructionOverlay();
    }

    private void OpenHomesteadConstructionPanel(GridPosition target)
    {
        var result = _session.OpenHomesteadWorkbench(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        OpenConstructionOverlay(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );
    }

    private void OpenConstructionOverlay(string? initialProjectId = null)
    {
        if (_constructionOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _constructionOverlay = new ConstructionOverlay(
            _theme,
            _session,
            _locale,
            initialProjectId
        );
        _constructionOverlay.CloseRequested += CloseConstructionPanel;
        _constructionOverlay.ConstructionChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_constructionOverlay);
    }

    private void CloseConstructionPanel()
    {
        FreeUi(_constructionOverlay);
        _constructionOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenToolUpgrade(GridPosition target)
    {
        if (_toolUpgradeOverlay is not null)
        {
            return;
        }

        var check = _session.OpenCrystalGrottoUpgradeBench(target);
        if (!check.Succeeded)
        {
            _hud?.ShowNotice(check.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _toolUpgradeOverlay = new ToolUpgradeOverlay(
            _theme,
            _session,
            _locale,
            target
        );
        _toolUpgradeOverlay.CloseRequested += CloseToolUpgrade;
        _toolUpgradeOverlay.UpgradeStarted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_toolUpgradeOverlay);
    }

    private void CloseToolUpgrade()
    {
        FreeUi(_toolUpgradeOverlay);
        _toolUpgradeOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenLivestockAutomation(
        string buildingId,
        GridPosition target
    )
    {
        if (_livestockAutomationOverlay is not null)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _livestockAutomationOverlay = new LivestockAutomationOverlay(
            _theme,
            _session,
            _locale,
            buildingId,
            target
        );
        _livestockAutomationOverlay.CloseRequested +=
            CloseLivestockAutomation;
        _livestockAutomationOverlay.AutomationChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_livestockAutomationOverlay);
    }

    private void CloseLivestockAutomation()
    {
        FreeUi(_livestockAutomationOverlay);
        _livestockAutomationOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void InspectKitchenReserve(GridPosition target)
    {
        var result = _session.InspectKitchenReserve(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "construction.kitchen_reserve.name",
            result.MessageKey,
            () => { }
        );
    }

    private void OpenKitchen(GridPosition target)
    {
        if (_kitchenOverlay is not null)
        {
            return;
        }

        var result = _session.OpenKitchenStation(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _kitchenOverlay = new KitchenOverlay(
            _theme,
            _session,
            _locale,
            target
        );
        _kitchenOverlay.CloseRequested += CloseKitchen;
        _kitchenOverlay.Cooked += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_kitchenOverlay);
    }

    private void CloseKitchen()
    {
        FreeUi(_kitchenOverlay);
        _kitchenOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenIngredientPantry(GridPosition target)
    {
        if (_ingredientPantryOverlay is not null)
        {
            return;
        }

        var result = _session.OpenIngredientPantry(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _ingredientPantryOverlay = new IngredientPantryOverlay(
            _theme,
            _session,
            _locale,
            target
        );
        _ingredientPantryOverlay.CloseRequested += CloseIngredientPantry;
        _ingredientPantryOverlay.PantryChanged += () => SaveNow(false);
        _uiLayer.AddChild(_ingredientPantryOverlay);
    }

    private void CloseIngredientPantry()
    {
        FreeUi(_ingredientPantryOverlay);
        _ingredientPantryOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenCookedDishes()
    {
        if (_cookedDishOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _cookedDishOverlay = new CookedDishOverlay(
            _theme,
            _session,
            _locale
        );
        _cookedDishOverlay.CloseRequested += CloseCookedDishes;
        _cookedDishOverlay.DishEaten += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_cookedDishOverlay);
    }

    private void CloseCookedDishes()
    {
        FreeUi(_cookedDishOverlay);
        _cookedDishOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TryEnterStarweaverTeaHouse()
    {
        var result = _session.TryEnterStarweaverTeaHouse();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowTeaHouse(true);
    }

    private void TryLeaveStarweaverTeaHouse()
    {
        var result = _session.TryExitStarweaverTeaHouse();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromTeaHouse: true);
    }

    private void InspectStarwovenTeaCounter()
    {
        var result = _session.InspectStarwovenTeaCounter();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "tea_house.counter.name",
            result.MessageKey,
            () => { },
            GeneratedArt.RelationshipIcon(
                RelationshipTier.NewAcquaintance
            )
        );
    }

    private void TryEnterTwilightEmporium()
    {
        var result = _session.TryEnterTwilightEmporium();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowTwilightEmporium(true);
    }

    private void TryLeaveTwilightEmporium()
    {
        var result = _session.TryExitTwilightEmporium();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromTwilightEmporium: true);
    }

    private void InspectTravelManifest()
    {
        var result = _session.InspectTravelManifest();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        OpenShop(ShopOverlayMode.TwilightEmporium);
    }

    private void TryEnterStarlightPost()
    {
        var result = _session.TryEnterStarlightPost();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarlightPost(true);
    }

    private void TryLeaveStarlightPost()
    {
        var result = _session.TryExitStarlightPost();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromStarlightPost: true);
    }

    private void InspectRouteSortingCounter()
    {
        var result = _session.InspectRouteSortingCounter();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "starlight_post.counter.name",
            result.MessageKey,
            () => { },
            GeneratedArt.RelationshipIcon(
                RelationshipTier.NewAcquaintance
            )
        );
    }

    private void TryEnterStarfallWatch()
    {
        var result = _session.TryEnterStarfallWatch();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowStarfallWatch(true);
    }

    private void TryLeaveStarfallWatch()
    {
        var result = _session.TryExitStarfallWatch();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        ShowFarm(false, fromStarfallWatch: true);
    }

    private void InspectSealRouteTable()
    {
        var result = _session.InspectSealRouteTable();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "starfall_watch.table.name",
            result.MessageKey,
            () => { },
            GeneratedArt.RelationshipIcon(
                RelationshipTier.NewAcquaintance
            )
        );
    }

    private void OpenShop()
    {
        OpenShop(ShopOverlayMode.FarmStall);
    }

    private void OpenShop(ShopOverlayMode mode)
    {
        if (_shopOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _shopOverlay = new ShopOverlay(
            _theme,
            _session,
            _locale,
            mode
        );
        _shopOverlay.CloseRequested += CloseShop;
        _shopOverlay.TransactionSucceeded += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_shopOverlay);
    }

    private void CloseShop()
    {
        FreeUi(_shopOverlay);
        _shopOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenProcessor(string machineId)
    {
        if (_processorOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _processorOverlay = new ProcessorOverlay(
            _theme,
            _session,
            _locale,
            machineId
        );
        _processorOverlay.CloseRequested += CloseProcessor;
        _processorOverlay.ProcessingSucceeded += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_processorOverlay);
    }

    private void CloseProcessor()
    {
        FreeUi(_processorOverlay);
        _processorOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenShipping()
    {
        if (_shippingOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _shippingOverlay = new ShippingOverlay(_theme, _session, _locale);
        _shippingOverlay.CloseRequested += CloseShipping;
        _shippingOverlay.ShippingChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_shippingOverlay);
    }

    private void CloseShipping()
    {
        FreeUi(_shippingOverlay);
        _shippingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenCommissionBoard()
    {
        OpenCommissionBoard(CommissionBoardPage.Daily);
    }

    private void OpenWeeklyCommissionBoard()
    {
        OpenCommissionBoard(CommissionBoardPage.Weekly);
    }

    private void OpenCommissionBoard(CommissionBoardPage initialPage)
    {
        if (_commissionOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _farm?.SetCommissionBoardOpen(true);
        _commissionOverlay = new CommissionBoardOverlay(
            _theme,
            _session,
            _locale,
            initialPage
        );
        _commissionOverlay.CloseRequested += CloseCommissionBoard;
        _commissionOverlay.CommissionChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_commissionOverlay);
    }

    private void CloseCommissionBoard()
    {
        FreeUi(_commissionOverlay);
        _commissionOverlay = null;
        _farm?.SetCommissionBoardOpen(false);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarlightMail()
    {
        if (_mailOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _mailOverlay = new StarlightMailOverlay(
            _theme,
            _session,
            _locale
        );
        _mailOverlay.CloseRequested += CloseStarlightMail;
        _mailOverlay.MailChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            if (!_mailPlaytest)
            {
                SaveNow(false);
            }
        };
        _uiLayer.AddChild(_mailOverlay);
    }

    private void CloseStarlightMail()
    {
        FreeUi(_mailOverlay);
        _mailOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarlightPedestal() => OpenStarlightPedestal(
        DataCatalog.WoodlandStarlightId
    );

    private void OpenStarlightPedestal(string pedestalId)
    {
        if (_starlightOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _session.Starlight.Discover(pedestalId);
        _starlightOverlay = new StarlightPedestalOverlay(
            _theme,
            _session,
            _locale,
            pedestalId
        );
        _starlightOverlay.CloseRequested += CloseStarlightPedestal;
        _starlightOverlay.StarlightChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_starlightOverlay);
    }

    private void CloseStarlightPedestal()
    {
        FreeUi(_starlightOverlay);
        _starlightOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenBackpack()
    {
        if (_backpackOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _backpackOverlay = new BackpackOverlay(_theme, _session, _locale);
        _backpackOverlay.CloseRequested += CloseBackpack;
        _backpackOverlay.CraftingRequested += () =>
        {
            CloseBackpack();
            OpenCrafting();
        };
        _backpackOverlay.MealsRequested += () =>
        {
            CloseBackpack();
            OpenCookedDishes();
        };
        _uiLayer.AddChild(_backpackOverlay);
    }

    private void CloseBackpack()
    {
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenCrafting()
    {
        if (_craftingOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _craftingOverlay = new CraftingOverlay(_theme, _session, _locale);
        _craftingOverlay.CloseRequested += CloseCrafting;
        _craftingOverlay.Crafted += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_craftingOverlay);
    }

    private void CloseCrafting()
    {
        FreeUi(_craftingOverlay);
        _craftingOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStorage(GridPosition position)
    {
        if (_storageOverlay is not null ||
            _session.Storage.ChestAt(position) is null)
        {
            return;
        }

        SetWorldControls(false);
        _farm?.SetStorageChestOpen(position);
        _storageOverlay = new StorageOverlay(_theme, _session, _locale, position);
        _storageOverlay.CloseRequested += CloseStorage;
        _storageOverlay.StorageChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_storageOverlay);
    }

    private void CloseStorage()
    {
        FreeUi(_storageOverlay);
        _storageOverlay = null;
        _farm?.SetStorageChestOpen(null);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void ShowDialogue(
        string speakerKey,
        string dialogueKey,
        Action closed,
        Texture2D? icon = null,
        string status = ""
    ) => ShowDialoguePages(
        speakerKey,
        [dialogueKey],
        closed,
        icon,
        status
    );

    private void ShowDialoguePages(
        string speakerKey,
        IReadOnlyList<string> dialogueKeys,
        Action closed,
        Texture2D? icon = null,
        string status = ""
    )
    {
        SetWorldControls(false);
        _dialogueOverlay = new DialogueOverlay(_theme, _locale);
        _dialogueOverlay.ShowDialoguePages(
            _locale.Tr(speakerKey),
            dialogueKeys.Select(key => _locale.Tr(key)).ToList(),
            () =>
            {
                _dialogueOverlay = null;
                closed();
                if (CanRestoreWorldControls)
                {
                    SetWorldControls(true);
                }
            },
            icon,
            status
        );
        _uiLayer.AddChild(_dialogueOverlay);
    }

    private static string RelationshipTierKey(
        RelationshipTier tier
    ) => tier switch
    {
        RelationshipTier.TrustedFriend =>
            "village.relationship.trusted_friend",
        RelationshipTier.KindredLight =>
            "village.relationship.kindred_light",
        _ => "village.relationship.new_acquaintance"
    };

    private void EndDay()
    {
        if (_fadeTransition is not null)
        {
            return;
        }

        SetWorldControls(false);
        _hud?.ShowNotice("notice.day_end", 1.3);
        _audio.Play(PixelSound.Sleep);
        _fadeTransition = new FadeTransition(
            () =>
            {
                _session.EndDay();
                SaveNow(false);
                _hud?.Refresh();
            },
            () =>
            {
                _fadeTransition = null;
                ShowNightlySummary();
            }
        );
        _uiLayer.AddChild(_fadeTransition);
    }

    private void ResolveStarfallTrialDefeat(bool forcedByClosingTime)
    {
        if (_fadeTransition is not null)
        {
            return;
        }

        SetWorldControls(false);
        _audio.Play(PixelSound.Sleep);
        _fadeTransition = new FadeTransition(
            () =>
            {
                var result = _session.ResolveStarfallTrialDefeat(
                    forcedByClosingTime
                );
                if (!result.Succeeded)
                {
                    _hud?.ShowNotice(result.MessageKey);
                    return;
                }
                SaveNow(false);
                _hud?.Refresh();
            },
            () =>
            {
                _fadeTransition = null;
                ShowCottage(false);
                _hud?.ShowNotice("combat.defeat.resolved", 2.6);
            }
        );
        _uiLayer.AddChild(_fadeTransition);
    }

    private void ShowNightlySummary()
    {
        FreeUi(_nightlySummaryOverlay);
        _nightlySummaryOverlay = new NightlySummaryOverlay(
            _theme,
            _session,
            _locale
        );
        _nightlySummaryOverlay.ContinueRequested += CloseNightlySummary;
        _uiLayer.AddChild(_nightlySummaryOverlay);
    }

    private void CloseNightlySummary()
    {
        FreeUi(_nightlySummaryOverlay);
        _nightlySummaryOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenPause()
    {
        if (_paused)
        {
            return;
        }

        _paused = true;
        SetWorldControls(false);
        _pauseOverlay = new PauseOverlay(_theme, _locale);
        _pauseOverlay.ResumeRequested += ClosePause;
        _pauseOverlay.GleamriseGoalsRequested += () =>
        {
            ClosePause();
            OpenGleamriseSeasonGoals();
        };
        _pauseOverlay.FishingCollectionRequested += () =>
        {
            ClosePause();
            OpenFishingCollection();
        };
        _pauseOverlay.FishingGearRequested += () =>
        {
            ClosePause();
            OpenFishingGear();
        };
        _pauseOverlay.StellarResonanceRequested += () =>
        {
            ClosePause();
            OpenStellarResonance();
        };
        _pauseOverlay.LanguageRequested += () =>
        {
            ClosePause();
            OpenSettings();
        };
        _pauseOverlay.SaveQuitRequested += () =>
        {
            SaveNow(false);
            ShowTitle("notice.saved");
        };
        _uiLayer.AddChild(_pauseOverlay);
    }

    private void ClosePause()
    {
        if (!_paused)
        {
            return;
        }

        _paused = false;
        FreeUi(_pauseOverlay);
        _pauseOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenSettings()
    {
        if (_settingsOverlay is not null)
        {
            return;
        }

        if (_playing)
        {
            SetWorldControls(false);
        }
        _settingsOverlay = new AccessibilitySettingsOverlay(
            _theme,
            _settings,
            _locale
        );
        _settingsOverlay.CloseRequested += CloseSettings;
        _settingsOverlay.LanguageRequested += () =>
        {
            ToggleLanguage();
            _settingsOverlay?.RefreshText();
        };
        _settingsOverlay.SettingsChanged += ApplySettings;
        _settingsOverlay.BindingChanged += (_, _) =>
            InputSetup.ApplyKeyboardBindings(_settings);
        _uiLayer.AddChild(_settingsOverlay);
    }

    private void ApplySettings()
    {
        _settingsService.Save(_settings);
        AccessibilityRuntime.Apply(_settings, _uiLayer);
        ConfigureSessionAccessibility();
    }

    private void ConfigureSessionAccessibility() =>
        _session.ConfigureAccessibility(
            _settings.FishingCatchZoneBonus,
            _settings.IncomingDamageMultiplier,
            _settings.EnemySpeedMultiplier
        );

    private void CloseSettings()
    {
        ApplySettings();
        FreeUi(_settingsOverlay);
        _settingsOverlay = null;
        _title?.RefreshText();
        if (_playing && CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFishingCollection()
    {
        if (_fishingCollectionOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _fishingCollectionOverlay = new FishingCollectionOverlay(
            _theme,
            _session,
            _locale
        );
        _fishingCollectionOverlay.CloseRequested += CloseFishingCollection;
        _uiLayer.AddChild(_fishingCollectionOverlay);
    }

    private void CloseFishingCollection()
    {
        FreeUi(_fishingCollectionOverlay);
        _fishingCollectionOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFishingMinigame(GridPosition target)
    {
        if (_fishingMinigameOverlay is not null)
        {
            return;
        }

        var result = _session.BeginFishingChallenge(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        SetWorldControls(false);
        _audio.Play(PixelSound.Water);
        _fishingMinigameOverlay = new FishingMinigameOverlay(
            _theme,
            _session,
            _locale
        );
        _fishingMinigameOverlay.Finished += ResolveFishingMinigame;
        _uiLayer.AddChild(_fishingMinigameOverlay);
    }

    private void ResolveFishingMinigame()
    {
        var result = _session.ResolveFishingChallenge();
        FreeUi(_fishingMinigameOverlay);
        _fishingMinigameOverlay = null;
        _hud?.ShowNotice(result.MessageKey, 2.2);
        _audio.Play(result.Succeeded ? PixelSound.Harvest : PixelSound.Step);
        SaveNow(false);

        if (_session.FishingProgression.CanChooseSpecialization)
        {
            Callable.From(OpenFishingGear).CallDeferred();
            return;
        }

        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFishingGear()
    {
        if (_fishingGearOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _fishingGearOverlay = new FishingGearOverlay(
            _theme,
            _session,
            _locale
        );
        _fishingGearOverlay.CloseRequested += CloseFishingGear;
        _fishingGearOverlay.GearChanged += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fishingGearOverlay);
    }

    private void CloseFishingGear()
    {
        FreeUi(_fishingGearOverlay);
        _fishingGearOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenDeepMine()
    {
        if (_deepMineOverlay is not null || !_session.DeepMine.Active)
        {
            return;
        }

        SetWorldControls(false);
        _deepMineOverlay = new DeepMineOverlay(
            _theme,
            _session,
            _locale
        );
        _deepMineOverlay.CloseRequested += CloseDeepMine;
        _deepMineOverlay.ProgressChanged += () => SaveNow(false);
        _deepMineOverlay.DefeatRequested += ResolveDeepMineDefeat;
        _uiLayer.AddChild(_deepMineOverlay);
    }

    private void CloseDeepMine()
    {
        _session.DeepMine.Leave();
        FreeUi(_deepMineOverlay);
        _deepMineOverlay = null;
        SaveNow(false);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarGate()
    {
        if (_starGateOverlay is not null || !_session.StarGate.Activated)
        {
            return;
        }

        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _starGateOverlay = new StarGateOverlay(
            _theme,
            _session,
            _locale
        );
        _starGateOverlay.TravelRequested += TravelStarGate;
        _starGateOverlay.ConvergenceRequested += BeginMainStoryFinale;
        _starGateOverlay.CloseRequested += CloseStarGate;
        _uiLayer.AddChild(_starGateOverlay);
    }

    private void CloseStarGate()
    {
        FreeUi(_starGateOverlay);
        _starGateOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void TravelStarGate(string destinationId)
    {
        var result = _session.TravelStarGate(destinationId);
        if (!result.Succeeded)
        {
            _starGateOverlay?.ShowNotice(result.MessageKey);
            return;
        }

        FreeUi(_starGateOverlay);
        _starGateOverlay = null;
        SaveNow(false);
        _audio.Play(PixelSound.Chime);
        ShowFarm(false);
        _hud?.ShowNotice(result.MessageKey);
    }

    private void OpenStellarResonance()
    {
        if (_stellarResonanceOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _stellarResonanceOverlay = new StellarResonanceOverlay(
            _theme,
            _session,
            _locale
        );
        _stellarResonanceOverlay.CloseRequested += CloseStellarResonance;
        _uiLayer.AddChild(_stellarResonanceOverlay);
    }

    private void CloseStellarResonance()
    {
        FreeUi(_stellarResonanceOverlay);
        _stellarResonanceOverlay = null;
        SaveNow(false);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void BeginMainStoryFinale()
    {
        if (_session.StellarResonance.MainStoryCompleted)
        {
            CloseStarGate();
            OpenStellarResonance();
            return;
        }

        var result = _session.CompleteMainStory();
        if (!result.Succeeded)
        {
            _starGateOverlay?.ShowNotice(result.MessageKey);
            return;
        }

        FreeUi(_starGateOverlay);
        _starGateOverlay = null;
        SaveNow(false);
        _audio.Play(PixelSound.Chime);
        SetWorldControls(false);
        _mainStoryEndingOverlay = new MainStoryEndingOverlay(
            _theme,
            _session,
            _locale
        );
        _mainStoryEndingOverlay.ContinueRequested += CloseMainStoryEnding;
        _uiLayer.AddChild(_mainStoryEndingOverlay);
    }

    private void CloseMainStoryEnding()
    {
        FreeUi(_mainStoryEndingOverlay);
        _mainStoryEndingOverlay = null;
        _hud?.ShowNotice("stellar.main_story.completed", 3.2);
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void ResolveDeepMineDefeat()
    {
        FreeUi(_deepMineOverlay);
        _deepMineOverlay = null;
        var result = _session.ResolveDeepMineDefeat();
        SaveNow(false);
        ShowCrystalGrotto(false);
        _hud?.ShowNotice(result.MessageKey, 2.6);
    }

    private void OpenFishingDonation()
    {
        if (_fishingDonationOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _fishingDonationOverlay = new FishingDonationOverlay(
            _theme,
            _session,
            _locale
        );
        _fishingDonationOverlay.CloseRequested += CloseFishingDonation;
        _uiLayer.AddChild(_fishingDonationOverlay);
    }

    private void CloseFishingDonation()
    {
        FreeUi(_fishingDonationOverlay);
        _fishingDonationOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenGleamriseSeasonGoals()
    {
        if (_gleamriseSeasonOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _gleamriseSeasonOverlay = new GleamriseSeasonOverlay(
            _theme,
            _session,
            _locale
        );
        _gleamriseSeasonOverlay.CloseRequested += CloseGleamriseSeasonGoals;
        _gleamriseSeasonOverlay.GoalClaimed += () =>
        {
            _audio.Play(PixelSound.Chime);
            SaveNow(false);
        };
        _uiLayer.AddChild(_gleamriseSeasonOverlay);
    }

    private void CloseGleamriseSeasonGoals()
    {
        FreeUi(_gleamriseSeasonOverlay);
        _gleamriseSeasonOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void ShowCompletion()
    {
        SetWorldControls(false);
        _completionOverlay = new CompletionOverlay(_theme, _locale, _session);
        _completionOverlay.ContinueRequested += () =>
        {
            FreeUi(_completionOverlay);
            _completionOverlay = null;
            if (CanRestoreWorldControls)
            {
                SetWorldControls(true);
            }
        };
        _completionOverlay.MenuRequested += () =>
        {
            SaveNow(false);
            ShowTitle();
        };
        _uiLayer.AddChild(_completionOverlay);
    }

    private void ToggleLanguage()
    {
        _locale.Toggle();
        _session.SetLocale(_locale.CurrentLocale);
        if (_playing)
        {
            SaveNow(false);
        }
        else
        {
            _titleLanguageOverridden = true;
        }
        _title?.RefreshText();
        _hud?.Refresh();
    }

    private void OnLocaleChanged()
    {
        _title?.RefreshText();
        _pauseOverlay?.RefreshText();
        _shopOverlay?.RefreshText();
        _processorOverlay?.RefreshText();
        _shippingOverlay?.RefreshText();
        _commissionOverlay?.RefreshText();
        _mailOverlay?.RefreshText();
        _starlightOverlay?.RefreshText();
        _starGateOverlay?.RefreshText();
        _craftingOverlay?.RefreshText();
        _kitchenOverlay?.RefreshText();
        _ingredientPantryOverlay?.RefreshText();
        _cookedDishOverlay?.RefreshText();
        _storageOverlay?.RefreshText();
        _backpackOverlay?.RefreshText();
        _farmingSpecializationOverlay?.RefreshText();
        _hud?.Refresh();
    }

    private void OnCollectionEntryDiscovered(string entryId)
    {
        if (!CompendiumCatalog.Entries.TryGetValue(entryId, out var entry))
        {
            return;
        }

        var noticeKey = CompendiumCatalog.Category(
            entry.CategoryId
        ).DiscoveryNoticeKey;
        _hud?.ShowNoticeFormatted(
            noticeKey,
            2.6,
            _locale.Tr(entry.NameKey)
        );
    }

    private void SaveNow(bool showNotice)
    {
        _session.SetLocale(_locale.CurrentLocale);
        _saveService.Save(_session.Capture());
        if (showNotice)
        {
            _hud?.ShowNotice("notice.saved");
        }
    }

    private void SetWorldControls(bool enabled)
    {
        if (_farm is not null)
        {
            _farm.ControlsEnabled = enabled;
        }

        if (_cottage is not null)
        {
            _cottage.ControlsEnabled = enabled;
        }

        if (_greenhouse is not null)
        {
            _greenhouse.ControlsEnabled = enabled;
        }

        if (_starfeatherCoop is not null)
        {
            _starfeatherCoop.ControlsEnabled = enabled;
        }

        if (_moonfleeceBarn is not null)
        {
            _moonfleeceBarn.ControlsEnabled = enabled;
        }

        if (_archive is not null)
        {
            _archive.ControlsEnabled = enabled;
        }

        if (_workshop is not null)
        {
            _workshop.ControlsEnabled = enabled;
        }

        if (_teaHouse is not null)
        {
            _teaHouse.ControlsEnabled = enabled;
        }

        if (_twilightEmporium is not null)
        {
            _twilightEmporium.ControlsEnabled = enabled;
        }

        if (_starlightPost is not null)
        {
            _starlightPost.ControlsEnabled = enabled;
        }

        if (_starfallWatch is not null)
        {
            _starfallWatch.ControlsEnabled = enabled;
        }

        if (_starharvestMarket is not null)
        {
            _starharvestMarket.ControlsEnabled = enabled;
        }

        if (_gleamrisePlantingFestival is not null)
        {
            _gleamrisePlantingFestival.ControlsEnabled = enabled;
        }

        if (_longnightLanternFeast is not null)
        {
            _longnightLanternFeast.ControlsEnabled = enabled;
        }

        if (_fireflyTide is not null)
        {
            _fireflyTide.ControlsEnabled = enabled;
        }

        if (_crystalGrotto is not null)
        {
            _crystalGrotto.ControlsEnabled = enabled;
        }

        if (_starfallRuinsTrial is not null)
        {
            _starfallRuinsTrial.ControlsEnabled = enabled;
        }
    }

    private void ClearWorld()
    {
        if (_world is not null && IsInstanceValid(_world))
        {
            _world.QueueFree();
        }
        _world = null;
        _farm = null;
        _cottage = null;
        _greenhouse = null;
        _starfeatherCoop = null;
        _moonfleeceBarn = null;
        _archive = null;
        _workshop = null;
        _teaHouse = null;
        _twilightEmporium = null;
        _starlightPost = null;
        _starfallWatch = null;
        _starharvestMarket = null;
        _gleamrisePlantingFestival = null;
        _longnightLanternFeast = null;
        _fireflyTide = null;
        _crystalGrotto = null;
        _starfallRuinsTrial = null;
    }

    private static void FreeUi(CanvasItem? item)
    {
        if (item is not null && IsInstanceValid(item))
        {
            item.QueueFree();
        }
    }
}
