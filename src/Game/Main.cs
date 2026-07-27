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
    private TitleMenu? _title;
    private HudView? _hud;
    private PauseOverlay? _pauseOverlay;
    private DialogueOverlay? _dialogueOverlay;
    private CompletionOverlay? _completionOverlay;
    private ShopOverlay? _shopOverlay;
    private ProcessorOverlay? _processorOverlay;
    private BackpackOverlay? _backpackOverlay;
    private FadeTransition? _fadeTransition;
    private bool _playing;
    private bool _paused;
    private bool _titleLanguageOverridden;

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
        if (userArgs.Contains("--playtest-door"))
        {
            Callable.From(StartDoorPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-cottage"))
        {
            Callable.From(StartCottagePlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-crops"))
        {
            Callable.From(StartCropPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-economy"))
        {
            Callable.From(StartEconomyPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-processor"))
        {
            Callable.From(StartProcessorPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-world"))
        {
            Callable.From(StartWorldPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-gate"))
        {
            Callable.From(StartGatePlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-backpack"))
        {
            Callable.From(StartBackpackPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-resource"))
        {
            Callable.From(StartResourcePlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-target"))
        {
            Callable.From(StartTargetPreviewPlaytest).CallDeferred();
        }
        else if (userArgs.Contains("--playtest-farm"))
        {
            CallDeferred(MethodName.StartNewGame);
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

        if (@event.IsActionPressed(InputSetup.Pause) &&
            _dialogueOverlay is null &&
            _completionOverlay is null &&
            _shopOverlay is null &&
            _processorOverlay is null &&
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
        _backpackOverlay is not null ||
        _fadeTransition is not null;

    private void ShowTitle(string? noticeKey = null)
    {
        _playing = false;
        _paused = false;
        _titleLanguageOverridden = false;
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
        _session.Farm.Restore(
        [
            CropState(12, 16, DataCatalog.StarbudId, 0),
            CropState(14, 16, DataCatalog.StarbudId, 1),
            CropState(16, 16, DataCatalog.StarbudId, 2),
            CropState(20, 16, DataCatalog.MoonrootId, 0),
            CropState(22, 16, DataCatalog.MoonrootId, 1),
            CropState(24, 16, DataCatalog.MoonrootId, 2),
            CropState(28, 16, DataCatalog.MoonrootId, 3),
        ]);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartEconomyPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 4);
        _session.Inventory.Add(DataCatalog.MoonrootId, 4);
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
        _session.Inventory.Add(DataCatalog.StarbudSeedId, 12);
        _session.Inventory.Add(DataCatalog.MoonrootSeedId, 7);
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

    private void ShowFarm(bool fromCottage)
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

        _farm = new FarmView(_session, _locale);
        _farm.UseRequested += UseFarmTarget;
        _farm.MiraRequested += TalkToMira;
        _farm.EnterCottageRequested += () => ShowCottage(true);
        _farm.ShopRequested += OpenShop;
        _farm.ProcessorRequested += OpenProcessor;
        _farm.RegionEntered += key => _hud?.ShowNotice(key, 2.6);
        _farm.StepRequested += () => _audio.Play(PixelSound.Step);
        _world = _farm;
        AddChild(_world);
        MoveChild(_world, 1);
        _hud?.ShowNotice(fromCottage ? "notice.leave_cottage" : string.Empty);
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

        var sound = result.GrantedItemId is not null
            ? PixelSound.Harvest
            : selectedId switch
            {
                DataCatalog.ShovelId => PixelSound.Till,
                DataCatalog.MacheteId => PixelSound.Harvest,
                DataCatalog.WateringCanId => PixelSound.Water,
                DataCatalog.BucketId => PixelSound.Water,
                DataCatalog.StarbudSeedId or DataCatalog.MoonrootSeedId => PixelSound.Plant,
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

        ShowDialogue(dialogueKey, () =>
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

    private void OpenShop()
    {
        if (_shopOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _shopOverlay = new ShopOverlay(_theme, _session, _locale);
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
        if (!_paused && _processorOverlay is null && _fadeTransition is null)
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
        if (!_paused && _shopOverlay is null && _fadeTransition is null)
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
        _uiLayer.AddChild(_backpackOverlay);
    }

    private void CloseBackpack()
    {
        FreeUi(_backpackOverlay);
        _backpackOverlay = null;
        if (!_paused &&
            _shopOverlay is null &&
            _processorOverlay is null &&
            _fadeTransition is null)
        {
            SetWorldControls(true);
        }
    }

    private void ShowDialogue(string dialogueKey, Action closed)
    {
        SetWorldControls(false);
        _dialogueOverlay = new DialogueOverlay(_theme, _locale);
        _dialogueOverlay.ShowDialogue(
            _locale.Tr("dialogue.mira.name"),
            _locale.Tr(dialogueKey),
            () =>
            {
                _dialogueOverlay = null;
                closed();
                if (_completionOverlay is null && !_paused)
                {
                    SetWorldControls(true);
                }
            }
        );
        _uiLayer.AddChild(_dialogueOverlay);
    }

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
                if (!_paused)
                {
                    SetWorldControls(true);
                }
            }
        );
        _uiLayer.AddChild(_fadeTransition);
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
    }

    private static void FreeUi(CanvasItem? item)
    {
        if (item is not null && IsInstanceValid(item))
        {
            item.QueueFree();
        }
    }
}
