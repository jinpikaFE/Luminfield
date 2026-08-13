using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private readonly GameSession _session = new();
    private readonly LocaleService _locale = new();
    private Theme _theme = null!;
    private SaveService _saveService = null!;
    private PixelAudio _audio = null!;
    private CanvasLayer _uiLayer = null!;
    private Node2D? _world;
    private FarmView? _farm;
    private CottageView? _cottage;
    private ArchiveView? _archive;
    private WorkshopView? _workshop;
    private TeaHouseView? _teaHouse;
    private TwilightEmporiumView? _twilightEmporium;
    private StarlightPostView? _starlightPost;
    private StarfallWatchView? _starfallWatch;
    private TitleMenu? _title;
    private HudView? _hud;
    private PauseOverlay? _pauseOverlay;
    private DialogueOverlay? _dialogueOverlay;
    private CompletionOverlay? _completionOverlay;
    private ShopOverlay? _shopOverlay;
    private ProcessorOverlay? _processorOverlay;
    private ShippingOverlay? _shippingOverlay;
    private CommissionBoardOverlay? _commissionOverlay;
    private StarlightMailOverlay? _mailOverlay;
    private StarlightPedestalOverlay? _starlightOverlay;
    private CraftingOverlay? _craftingOverlay;
    private StorageOverlay? _storageOverlay;
    private NightlySummaryOverlay? _nightlySummaryOverlay;
    private BackpackOverlay? _backpackOverlay;
    private FadeTransition? _fadeTransition;
    private bool _playing;
    private bool _paused;
    private bool _titleLanguageOverridden;
    private bool _mailPlaytest;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        InputSetup.EnsureActions();
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
        _saveService = new SaveService(
            ProjectSettings.GlobalizePath("user://saves/slot_1.json")
        );

        _audio = new PixelAudio();
        AddChild(_audio);
        _uiLayer = new CanvasLayer { Layer = 100 };
        AddChild(_uiLayer);

        _session.NewGame(_locale.CurrentLocale);
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
            EndDay();
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

        if (@event.IsActionPressed(InputSetup.Pause) && _shopOverlay is not null)
        {
            CloseShop();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) && _processorOverlay is not null)
        {
            CloseProcessor();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) && _shippingOverlay is not null)
        {
            CloseShipping();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) && _commissionOverlay is not null)
        {
            CloseCommissionBoard();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) &&
            _mailOverlay is not null)
        {
            CloseStarlightMail();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) &&
            _starlightOverlay is not null)
        {
            CloseStarlightPedestal();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((@event.IsActionPressed(InputSetup.Pause) ||
             @event.IsActionPressed(InputSetup.Crafting)) &&
            _craftingOverlay is not null)
        {
            CloseCrafting();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) && _storageOverlay is not null)
        {
            CloseStorage();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputSetup.Pause) && _nightlySummaryOverlay is not null)
        {
            CloseNightlySummary();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((@event.IsActionPressed(InputSetup.Pause) ||
             @event.IsActionPressed(InputSetup.Backpack)) &&
            _backpackOverlay is not null)
        {
            CloseBackpack();
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

        if (@event.IsActionPressed(InputSetup.Pause) &&
            _dialogueOverlay is null &&
            _completionOverlay is null &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _mailOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
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
        _mailOverlay is not null ||
        _starlightOverlay is not null ||
        _craftingOverlay is not null ||
        _storageOverlay is not null ||
        _nightlySummaryOverlay is not null ||
        _backpackOverlay is not null ||
        _fadeTransition is not null;

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
        FreeUi(_mailOverlay);
        _mailOverlay = null;
        FreeUi(_starlightOverlay);
        _starlightOverlay = null;
        FreeUi(_craftingOverlay);
        _craftingOverlay = null;
        FreeUi(_storageOverlay);
        _storageOverlay = null;
        FreeUi(_nightlySummaryOverlay);
        _nightlySummaryOverlay = null;
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
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
        _title.LanguageRequested += ToggleLanguage;
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
                [PlaytestScenarioId.Crops] = StartCropPlaytest,
                [PlaytestScenarioId.Economy] = StartEconomyPlaytest,
                [PlaytestScenarioId.Processor] = StartProcessorPlaytest,
                [PlaytestScenarioId.ArchiveGift] = StartArchiveGiftPlaytest,
                [PlaytestScenarioId.Archive] = StartArchivePlaytest,
                [PlaytestScenarioId.ArchiveDoor] = StartArchiveDoorPlaytest,
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
                [PlaytestScenarioId.NpcPathfinding] =
                    StartNpcPathfindingPlaytest,
                [PlaytestScenarioId.VillageRestdayEnglish] =
                    StartVillageRestdayEnglishPlaytest,
                [PlaytestScenarioId.VillageRainSchedule] =
                    StartVillageRainSchedulePlaytest,
                [PlaytestScenarioId.VillageRainveilSchedule] =
                    StartVillageRainveilSchedulePlaytest,
                [PlaytestScenarioId.Village] = StartVillagePlaytest,
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
                [PlaytestScenarioId.QualityCrafting] =
                    StartQualityCraftingPlaytest,
                [PlaytestScenarioId.QualityBackpackEnglish] =
                    StartQualityBackpackEnglishPlaytest,
                [PlaytestScenarioId.QualityBackpack] =
                    StartQualityBackpackPlaytest,
                [PlaytestScenarioId.Quality] = StartQualityPlaytest,
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
        Callable.From(OpenProcessor).CallDeferred();
    }

    private void StartCottagePlaytest()
    {
        StartNewGame();
        ShowCottage(true);
    }

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
        var columns = new[] { 12, 14, 16, 20, 22, 24, 27, 29 };
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

    private void StartNpcPathfindingPlaytest()
    {
        StartVillagePlaytestWorld(
            1,
            13 * 60 + 30,
            new GridPosition(97, 45)
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

    private void StartLioraEventOnePlaytest()
    {
        StartLioraEventPlaytest(2, 25, new CharacterEventSave());
    }

    private void StartLioraEventTwoPlaytest()
    {
        StartLioraEventPlaytest(
            3,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog
                                .LioraFadedReturnRouteId,
                        CompletedDay = 2
                    }
                ]
            }
        );
    }

    private void StartLioraEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    )
    {
        const int minuteOfDay = 10 * 60;
        var liora = VillageCatalog.CurrentNpc(
            VillageCatalog.LioraId,
            day,
            minuteOfDay
        );
        if (liora is null)
        {
            StartArchivePlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = relationshipPoints,
                    LastTalkDay = day
                }
            ]
        };
        save.CharacterEvents = characterEvents;
        _session.Restore(save);
        _session.Inventory.Select(0);

        _playing = true;
        EnsureHud();
        ShowArchive(false);
        Callable.From(
            () => TalkToVillager(liora.Position)
        ).CallDeferred();
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
        StartTaviEventPlaytest(2, 25, new CharacterEventSave());
    }

    private void StartTaviEventTwoPlaytest()
    {
        StartTaviEventPlaytest(
            3,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.TaviCrackedMoonRuneId,
                        CompletedDay = 2
                    }
                ]
            }
        );
    }

    private void StartTaviEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    )
    {
        const int minuteOfDay = 10 * 60;
        var tavi = VillageCatalog.CurrentNpc(
            VillageCatalog.TaviId,
            day,
            minuteOfDay
        );
        if (tavi is null)
        {
            StartWorkshopPlaytest();
            return;
        }

        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        save.Player.X = 20 * 16 + 8;
        save.Player.Y = 18 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.TaviId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.TaviId,
                    Points = relationshipPoints,
                    LastTalkDay = day
                }
            ]
        };
        save.CharacterEvents = characterEvents;
        _session.Restore(save);
        _session.Inventory.Select(0);

        _playing = true;
        EnsureHud();
        ShowWorkshop(false);
        Callable.From(
            () => TalkToVillager(tavi.Position)
        ).CallDeferred();
    }

    private void StartNemiEventOnePlaytest()
    {
        StartNemiEventPlaytest(15, 25, new CharacterEventSave());
    }

    private void StartNemiEventTwoPlaytest()
    {
        StartNemiEventPlaytest(
            17,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.NemiUndeliverableLetterId,
                        CompletedDay = 15
                    }
                ]
            }
        );
    }

    private void StartNemiEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    ) => StartWorldCharacterEventPlaytest(
        day,
        relationshipPoints,
        characterEvents,
        VillageCatalog.NemiId,
        "village.npc.nemi.route"
    );

    private void StartKaelEventOnePlaytest()
    {
        StartKaelEventPlaytest(15, 25, new CharacterEventSave());
    }

    private void StartKaelEventTwoPlaytest()
    {
        StartKaelEventPlaytest(
            17,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.KaelBrokenBlueRuneId,
                        CompletedDay = 15
                    }
                ]
            }
        );
    }

    private void StartKaelEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    ) => StartWorldCharacterEventPlaytest(
        day,
        relationshipPoints,
        characterEvents,
        VillageCatalog.KaelId,
        "village.npc.kael.plaza"
    );

    private void StartSelaEventOnePlaytest()
    {
        StartSelaEventPlaytest(15, 25, new CharacterEventSave());
    }

    private void StartSelaEventTwoPlaytest()
    {
        StartSelaEventPlaytest(
            17,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.SelaTemperedStarlightId,
                        CompletedDay = 15
                    }
                ]
            }
        );
    }

    private void StartSelaEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    ) => StartWorldCharacterEventPlaytest(
        day,
        relationshipPoints,
        characterEvents,
        VillageCatalog.SelaId,
        "village.npc.sela.plaza"
    );

    private void StartOrinEventOnePlaytest()
    {
        StartOrinEventPlaytest(15, 25, new CharacterEventSave());
    }

    private void StartOrinEventTwoPlaytest()
    {
        StartOrinEventPlaytest(
            17,
            60,
            new CharacterEventSave
            {
                Entries =
                [
                    new CharacterEventEntrySave
                    {
                        EventId =
                            CharacterEventCatalog.OrinUnpricedWaybillId,
                        CompletedDay = 15
                    }
                ]
            }
        );
    }

    private void StartOrinEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents
    ) => StartWorldCharacterEventPlaytest(
        day,
        relationshipPoints,
        characterEvents,
        VillageCatalog.OrinId,
        "village.npc.orin.plaza"
    );

    private void StartWorldCharacterEventPlaytest(
        int day,
        int relationshipPoints,
        CharacterEventSave characterEvents,
        string npcId,
        string expectedDialogueKey
    )
    {
        const int minuteOfDay = 14 * 60;
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = 96 * 16 + 8;
        save.Player.Y = 60 * 16 + 8;
        save.Village = new VillageSave
        {
            MetNpcIds = [npcId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = npcId,
                    Points = relationshipPoints,
                    LastTalkDay = day
                }
            ]
        };
        save.CharacterEvents = characterEvents;
        _session.Restore(save);
        _session.Inventory.Select(0);

        var villageNpcs = _session.Village.CurrentNpcs(
            day,
            minuteOfDay,
            PlayerLocationIds.World,
            _session.PlayerCell
        );
        var npc = villageNpcs.FirstOrDefault(state =>
            state.Definition.Id == npcId
        );
        if (npc is null || npc.DialogueKey != expectedDialogueKey)
        {
            StartVillagePlaytest();
            return;
        }

        var occupied = villageNpcs
            .Select(state => state.Position)
            .ToHashSet();
        GridPosition? approach = null;
        foreach (var candidate in new[]
                 {
                     new GridPosition(
                         npc.Position.X,
                         npc.Position.Y + 1
                     ),
                     new GridPosition(
                         npc.Position.X - 1,
                         npc.Position.Y
                     ),
                     new GridPosition(
                         npc.Position.X + 1,
                         npc.Position.Y
                     ),
                     new GridPosition(
                         npc.Position.X,
                         npc.Position.Y - 1
                     )
                 })
        {
            if (NpcNavigationMap.IsWalkableGeometry(
                    PlayerLocationIds.World,
                    candidate
                ) &&
                !NpcNavigationMap.IsCriticalEntranceCell(
                    PlayerLocationIds.World,
                    candidate
                ) &&
                !occupied.Contains(candidate))
            {
                approach = candidate;
                break;
            }
        }

        if (approach is null)
        {
            StartVillagePlaytest();
            return;
        }

        _session.SetPlayerLocation(
            approach.Value.X * 16 + 8,
            approach.Value.Y * 16 + 8,
            PlayerLocationIds.World
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(
            () => TalkToVillager(npc.Position)
        ).CallDeferred();
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
            _session.SetPlayerLocation(
                vessa.Position.X * 16 + 8,
                (vessa.Position.Y + 1) * 16 + 8,
                PlayerLocationIds.World
            );
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
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(
            97 * 16 + 8,
            63 * 16 + 8,
            false
        );
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
        if (_session.InsideCottage)
        {
            ShowCottage(false);
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
        else
        {
            ShowFarm(false);
        }
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
        bool fromStarfallWatch = false
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

        _farm = new FarmView(_session, _locale);
        _farm.UseRequested += UseFarmTarget;
        _farm.MiraRequested += TalkToMira;
        _farm.EnterCottageRequested += () => ShowCottage(true);
        _farm.EnterArchiveRequested += TryEnterMoonlitArchive;
        _farm.EnterWorkshopRequested += TryEnterMoonstoneWorkshop;
        _farm.EnterTeaHouseRequested += TryEnterStarweaverTeaHouse;
        _farm.EnterTwilightEmporiumRequested +=
            TryEnterTwilightEmporium;
        _farm.EnterStarlightPostRequested += TryEnterStarlightPost;
        _farm.EnterStarfallWatchRequested += TryEnterStarfallWatch;
        _farm.ShopRequested += OpenShop;
        _farm.ProcessorRequested += OpenProcessor;
        _farm.ShippingRequested += OpenShipping;
        _farm.CommissionRequested += OpenCommissionBoard;
        _farm.MailRequested += OpenStarlightMail;
        _farm.StarlightRequested += OpenStarlightPedestal;
        _farm.VillagerRequested += TalkToVillager;
        _farm.StorageRequested += OpenStorage;
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
        _cottage.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _cottage;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(fromFarm ? "notice.enter_cottage" : string.Empty);
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
        _archive.DeskRequested += InspectMoonlitArchiveDesk;
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
        _workshop.WorkbenchRequested += InspectMoonRuneWorkbench;
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
        var result = _session.UseSelected(target);
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.MessageKey))
        {
            _hud?.ShowNotice(result.MessageKey);
        }

        var selectedIsSeed =
            DataCatalog.Items.TryGetValue(selectedId, out var selectedItem) &&
            selectedItem.Kind == ItemKind.Seed;
        var sound = result.GrantedItemId is not null
            ? PixelSound.Harvest
            : selectedIsSeed
                ? PixelSound.Plant
                : selectedId switch
            {
                DataCatalog.ShovelId => PixelSound.Till,
                DataCatalog.MacheteId => PixelSound.Harvest,
                DataCatalog.WateringCanId => PixelSound.Water,
                DataCatalog.BucketId => PixelSound.Water,
                _ => PixelSound.Chime
            };
        _audio.Play(sound);
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

    private void InspectMoonlitArchiveDesk()
    {
        var result = _session.InspectMoonlitArchiveDesk();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "archive.desk.name",
            result.MessageKey,
            () => { },
            GeneratedArt.RelationshipIcon(
                RelationshipTier.NewAcquaintance
            )
        );
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

    private void InspectMoonRuneWorkbench()
    {
        var result = _session.InspectMoonRuneWorkbench();
        if (!result.Succeeded)
        {
            _hud?.ShowNotice(result.MessageKey);
            return;
        }

        _audio.Play(PixelSound.Chime);
        ShowDialogue(
            "workshop.workbench.name",
            result.MessageKey,
            () => { },
            GeneratedArt.RelationshipIcon(
                RelationshipTier.TrustedFriend
            )
        );
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
        if (!_paused &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _fadeTransition is null)
        {
            SetWorldControls(true);
        }
    }

    private void OpenProcessor()
    {
        if (_processorOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _processorOverlay = new ProcessorOverlay(_theme, _session, _locale);
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
        if (!_paused &&
            _shopOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _fadeTransition is null)
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _fadeTransition is null)
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _mailOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fadeTransition is null)
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fadeTransition is null)
        {
            SetWorldControls(true);
        }
    }

    private void OpenStarlightPedestal()
    {
        if (_starlightOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _session.Starlight.Discover();
        _starlightOverlay = new StarlightPedestalOverlay(
            _theme,
            _session,
            _locale
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fadeTransition is null)
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
        _uiLayer.AddChild(_backpackOverlay);
    }

    private void CloseBackpack()
    {
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _fadeTransition is null)
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _storageOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fadeTransition is null)
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
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _shippingOverlay is null &&
            _commissionOverlay is null &&
            _starlightOverlay is null &&
            _craftingOverlay is null &&
            _nightlySummaryOverlay is null &&
            _backpackOverlay is null &&
            _fadeTransition is null)
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
                if (_completionOverlay is null && !_paused)
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
        if (!_paused && _fadeTransition is null)
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
        _pauseOverlay.LanguageRequested += () =>
        {
            ToggleLanguage();
            _pauseOverlay?.RefreshText();
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
        SetWorldControls(true);
    }

    private void ShowCompletion()
    {
        SetWorldControls(false);
        _completionOverlay = new CompletionOverlay(_theme, _locale, _session);
        _completionOverlay.ContinueRequested += () =>
        {
            FreeUi(_completionOverlay);
            _completionOverlay = null;
            SetWorldControls(true);
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
        _craftingOverlay?.RefreshText();
        _storageOverlay?.RefreshText();
        _backpackOverlay?.RefreshText();
        _hud?.Refresh();
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
        _archive = null;
        _workshop = null;
        _teaHouse = null;
        _twilightEmporium = null;
        _starlightPost = null;
        _starfallWatch = null;
    }

    private static void FreeUi(CanvasItem? item)
    {
        if (item is not null && IsInstanceValid(item))
        {
            item.QueueFree();
        }
    }
}
