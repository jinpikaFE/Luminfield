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
    private PostDeliveryOverlay? _postDeliveryOverlay;
    private StarfallWatchOverlay? _starfallWatchOverlay;
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
    private JourneyRecapOverlay? _journeyRecapOverlay;
    private MainStoryEndingOverlay? _mainStoryEndingOverlay;
    private AccessibilitySettingsOverlay? _settingsOverlay;
    private FarmingSpecializationOverlay? _farmingSpecializationOverlay;
    private GleamriseSeasonOverlay? _gleamriseSeasonOverlay;
    private FestivalShowcaseOverlay? _festivalShowcaseOverlay;
    private FestivalShopOverlay? _festivalShopOverlay;
    private FestivalMemoriesOverlay? _festivalMemoriesOverlay;
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

        GameLocaleBootstrap.LoadDefault(
            _locale,
            Godot.FileAccess.GetFileAsString
        );

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
        _session.StarlightPedestalRestored += pedestalId =>
            Callable.From(() => TryShowRestoredStarlightStory(
                pedestalId
            )).CallDeferred();
        _locale.LocaleChanged += OnLocaleChanged;
        ShowTitle();

        var userArgs = OS.GetCmdlineUserArgs();
        if (RouteGuidanceStartup.SelectedRouteId(userArgs) is { } routeId)
        {
            _routeGuidanceSelection.Select(routeId);
        }
        var startupRouteDestination =
            RouteGuidanceStartup.SelectedDestination(userArgs);
        var playtestSetup = CreatePlaytestScenarioRegistry()
            .ResolveSetup(userArgs);
        _playtestMode = playtestSetup is not null;
        if (playtestSetup is not null)
        {
            Callable.From(playtestSetup).CallDeferred();
        }

        if (startupRouteDestination is { } destination)
        {
            Callable.From(
                () => StartRouteGuidanceJourney(destination)
            ).CallDeferred();
        }

        if (OnboardingPlanStartup.ShouldOpen(userArgs))
        {
            Callable.From(OpenOnboardingPlan).CallDeferred();
        }

        if (MorningBriefingStartup.ShouldOpen(userArgs))
        {
            Callable.From(OpenMorningBriefing).CallDeferred();
        }

        if (RouteGuidanceStartup.ShouldOpen(userArgs))
        {
            Callable.From(OpenRouteGuidance).CallDeferred();
        }

        if (ImmediateFeedbackAcceptanceGallery.ShouldOpen(userArgs))
        {
            Callable.From(
                () => OpenFeedbackAcceptanceGallery(userArgs)
            ).CallDeferred();
        }

        if (PauseOverlayExperiencePreviewStartup.ShouldOpen(userArgs))
        {
            Callable.From(OpenPauseExperiencePreview).CallDeferred();
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
        if (!_playing)
        {
            return;
        }

        UpdateAudioContext();
        if (IsInputBlocked)
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

        if (TryCloseExperienceOverlay(@event, overlayCancelPressed))
        {
            return;
        }

        if (TryClosePlayerOverlay(@event, overlayCancelPressed))
        {
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

        if (TryOpenExperienceOverlay(@event))
        {
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
            _postDeliveryOverlay is null &&
            _starfallWatchOverlay is null &&
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
        _postDeliveryOverlay is not null ||
        _starfallWatchOverlay is not null ||
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
        _journeyRecapOverlay is not null ||
        _mainStoryEndingOverlay is not null ||
        _settingsOverlay is not null ||
        _gleamriseSeasonOverlay is not null ||
        _farmingSpecializationOverlay is not null ||
        _festivalShowcaseOverlay is not null ||
        _festivalShopOverlay is not null ||
        _festivalMemoriesOverlay is not null ||
        _gleamrisePlantingOverlay is not null ||
        _gleamriseSeedExchangeOverlay is not null ||
        _longnightFeastOverlay is not null ||
        _longnightStallOverlay is not null ||
        _fireflyTideOverlay is not null ||
        _fireflyTideShopOverlay is not null ||
        _onboardingOverlay is not null ||
        _morningBriefingOverlay is not null ||
        _routeGuidanceOverlay is not null ||
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
        _postDeliveryOverlay is null &&
        _starfallWatchOverlay is null &&
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
        _journeyRecapOverlay is null &&
        _mainStoryEndingOverlay is null &&
        _settingsOverlay is null &&
        _gleamriseSeasonOverlay is null &&
        _farmingSpecializationOverlay is null &&
        _festivalShowcaseOverlay is null &&
        _festivalShopOverlay is null &&
        _festivalMemoriesOverlay is null &&
        _gleamrisePlantingOverlay is null &&
        _gleamriseSeedExchangeOverlay is null &&
        _longnightFeastOverlay is null &&
        _longnightStallOverlay is null &&
        _fireflyTideOverlay is null &&
        _fireflyTideShopOverlay is null &&
        _onboardingOverlay is null &&
        _morningBriefingOverlay is null &&
        _routeGuidanceOverlay is null &&
        _fadeTransition is null;

    private void ShowTitle(string? noticeKey = null)
    {
        _playing = false;
        _paused = false;
        ResetPauseChildFocusRestoration();
        _titleLanguageOverridden = false;
        _mailPlaytest = false;
        ClearWorld();
        ResetRouteGuidance();
        CloseOnboardingPlan();
        CloseMorningBriefing();
        FreeUi(_hud);
        _hud = null;
        FreeUi(_pauseOverlay);
        _pauseOverlay = null;
        FreeUi(_dialogueOverlay);
        _dialogueOverlay = null;
        _session.StarlightStory.CancelActive();
        _session.RegionalEvents.CancelActive();
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
        FreeUi(_journeyRecapOverlay);
        _journeyRecapOverlay = null;
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
        FreeUi(_festivalMemoriesOverlay);
        _festivalMemoriesOverlay = null;
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
        if (!_playtestMode)
        {
            Callable.From(OpenOnboardingPlan).CallDeferred();
        }
    }

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
        Callable.From(TryOpenMorningBriefingForCurrentDay).CallDeferred();
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
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
            ShowImmediateFeedback(
                ImmediateFeedbackDomain.Tool,
                anchorResult
            );
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
        ShowImmediateFeedback(
            FarmFeedbackDomain(result, selectedId),
            result
        );
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
        if (_session.ActivePostDeliveryRoute is not null)
        {
            DeliverPostToVillager(target);
            return;
        }

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
        if (conversation.StarlightStory is { } starlightStory)
        {
            ShowStarlightStory(starlightStory);
            return;
        }
        if (conversation.GroupCharacterEvent is { } groupCharacterEvent)
        {
            ShowGroupCharacterEvent(groupCharacterEvent);
            return;
        }

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

    private void ShowGroupCharacterEvent(
        GroupCharacterEventDialogue groupEvent
    )
    {
        var speakerKeys = groupEvent.Pages
            .Select(page => VillageCatalog.Npcs[
                page.SpeakerNpcId
            ].NameKey)
            .ToArray();
        ShowDialoguePages(
            speakerKeys[0],
            groupEvent.Pages.Select(page => page.DialogueKey).ToArray(),
            () =>
            {
                var completion = _session.CompleteGroupCharacterEvent(
                    groupEvent.EventId
                );
                if (!completion.Succeeded)
                {
                    _hud?.ShowNotice(completion.MessageKey);
                    return;
                }

                SaveNow(false);
            },
            speakerKeys: speakerKeys
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
            ShowRewardFeedback("collection.reward.claimed");
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
        OpenShop(ShopOverlayMode.StarweaverTeaHouse);
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
        OpenPostDeliveryBoard();
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
        OpenStarfallWatchBoard();
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
        string status = "",
        IReadOnlyList<IReadOnlyList<object>>? dialogueArguments = null,
        IReadOnlyList<string>? speakerKeys = null
    )
    {
        SetWorldControls(false);
        _dialogueOverlay = new DialogueOverlay(_theme, _locale);
        _dialogueOverlay.ShowDialoguePages(
            _locale.Tr(speakerKey),
            dialogueKeys.Select((key, index) =>
            {
                var arguments = dialogueArguments is not null &&
                    index < dialogueArguments.Count
                        ? dialogueArguments[index]
                            .Select(ResolveStarlightStoryArgument)
                            .ToArray()
                        : [];
                return _locale.Tr(key, arguments);
            }).ToList(),
            () =>
            {
                _dialogueOverlay = null;
                closed();
                if (CanRestoreWorldControls)
                {
                    TryShowCurrentRegionStory();
                }
                if (CanRestoreWorldControls)
                {
                    SetWorldControls(true);
                }
            },
            icon,
            status,
            speakerKeys?.Select(key => _locale.Tr(key)).ToArray()
        );
        _uiLayer.AddChild(_dialogueOverlay);
    }

    private void ShowStarlightStory(
        StarlightStoryDialogue story,
        Action? closed = null
    )
    {
        ShowDialoguePages(
            story.SpeakerKey,
            story.DialogueKeys,
            () =>
            {
                var result = _session.CompleteStarlightStoryBeat(
                    story.BeatId
                );
                if (!result.Succeeded)
                {
                    _hud?.ShowNotice(result.MessageKey);
                    return;
                }

                SaveNow(false);
                closed?.Invoke();
            },
            status: _locale.Tr(story.StatusKey),
            dialogueArguments: story.DialogueArguments
        );
    }

    private object ResolveStarlightStoryArgument(object argument) =>
        argument is StarlightStoryLocalizedListArgument list
            ? list.Keys.Count == 0
                ? _locale.Tr(list.EmptyKey)
                : string.Join(
                    _locale.Tr(list.SeparatorKey),
                    list.Keys.Select(key => _locale.Tr(key))
                )
            : argument;

    private void HandleWorldStep()
    {
        _audio.Play(PixelSound.Step);
        TryShowCurrentRegionStory();
    }

    private void HandleRegionEntered(string regionKey)
    {
        _hud?.ShowNotice(regionKey, 2.6);
        TryShowCurrentRegionStory();
    }

    private void TryShowCurrentRegionStory()
    {
        if (_dialogueOverlay is not null ||
            _farm is null ||
            _session.PlayerLocationId != PlayerLocationIds.World)
        {
            return;
        }

        var story = _session.BeginStarlightRegionResponse(
            WorldDefinition.GetBiome(_session.PlayerCell)
        );
        if (story is not null)
        {
            ShowStarlightStory(story);
            return;
        }

        var regionalEvent = _session.BeginRegionalEvent(
            WorldDefinition.GetBiome(_session.PlayerCell)
        );
        if (regionalEvent is not null)
        {
            ShowRegionalEvent(regionalEvent);
        }
    }

    private void ShowRegionalEvent(RegionalEventDialogue regionalEvent)
    {
        ShowDialoguePages(
            regionalEvent.SpeakerKey,
            regionalEvent.DialogueKeys,
            () =>
            {
                var result = _session.CompleteRegionalEvent(
                    regionalEvent.EventId
                );
                if (!result.Succeeded)
                {
                    _hud?.ShowNotice(result.MessageKey);
                    return;
                }

                SaveNow(false);
            },
            status: _locale.Tr(regionalEvent.StatusKey)
        );
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
        TryOpenMorningBriefingForCurrentDay();
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
        _audio.ApplySettings(_settings);
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
        if (RestorePauseAfterChild())
        {
            return;
        }
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
        _fishingCollectionOverlay.RewardClaimed += messageKey =>
        {
            ShowRewardFeedback(messageKey);
            SaveNow(false);
        };
        _uiLayer.AddChild(_fishingCollectionOverlay);
    }

    private void CloseFishingCollection()
    {
        FreeUi(_fishingCollectionOverlay);
        _fishingCollectionOverlay = null;
        if (RestorePauseAfterChild())
        {
            return;
        }
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
        ShowImmediateFeedback(ImmediateFeedbackDomain.Fishing, result);
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
        ShowImmediateFeedback(ImmediateFeedbackDomain.Fishing, result);
        _hud?.ShowNotice(result.MessageKey, 2.2);
        _audio.Play(result.Succeeded ? PixelSound.FishBite : PixelSound.Error);
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
        if (RestorePauseAfterChild())
        {
            return;
        }
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
        _deepMineOverlay.FeedbackRequested += (domain, result) =>
            ShowImmediateFeedback(domain, result);
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
        if (RestorePauseAfterChild())
        {
            return;
        }
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

        if (_journeyRecapOverlay is not null)
        {
            return;
        }

        var readiness = _session.CheckMainStoryCompletion();
        if (!readiness.Succeeded)
        {
            _starGateOverlay?.ShowNotice(readiness.MessageKey);
            return;
        }

        SetWorldControls(false);
        _journeyRecapOverlay = new JourneyRecapOverlay(
            _theme,
            _session,
            _locale
        );
        _journeyRecapOverlay.ConfirmRequested += ConfirmMainStoryFinale;
        _journeyRecapOverlay.CloseRequested += CloseJourneyRecap;
        _uiLayer.AddChild(_journeyRecapOverlay);
    }

    private void ConfirmMainStoryFinale()
    {
        var result = _session.CompleteMainStory();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            CloseJourneyRecap();
            return;
        }

        FreeUi(_journeyRecapOverlay);
        _journeyRecapOverlay = null;
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

    private void CloseJourneyRecap()
    {
        FreeUi(_journeyRecapOverlay);
        _journeyRecapOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void CloseMainStoryEnding()
    {
        FreeUi(_mainStoryEndingOverlay);
        _mainStoryEndingOverlay = null;
        ShowRewardFeedback("stellar.main_story.completed");
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
        _gleamriseSeasonOverlay.GoalClaimed += messageKey =>
        {
            ShowRewardFeedback(messageKey);
            SaveNow(false);
        };
        _uiLayer.AddChild(_gleamriseSeasonOverlay);
    }

    private void CloseGleamriseSeasonGoals()
    {
        FreeUi(_gleamriseSeasonOverlay);
        _gleamriseSeasonOverlay = null;
        if (RestorePauseAfterChild())
        {
            return;
        }
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
        _postDeliveryOverlay?.RefreshText();
        _starfallWatchOverlay?.RefreshText();
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
