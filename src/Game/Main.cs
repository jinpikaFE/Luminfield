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

        if (OS.GetCmdlineUserArgs().Contains("--playtest-farm"))
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

        if (@event.IsActionPressed(InputSetup.Pause) &&
            _dialogueOverlay is null &&
            _completionOverlay is null &&
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

        _farm = new FarmView(_session);
        _farm.UseRequested += UseFarmTarget;
        _farm.MiraRequested += TalkToMira;
        _farm.EnterCottageRequested += () => ShowCottage(true);
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

        _cottage = new CottageView(_session);
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

        var sound = result.GrantedItemId is not null
            ? PixelSound.Harvest
            : selectedId switch
            {
                DataCatalog.HoeId => PixelSound.Till,
                DataCatalog.WateringCanId => PixelSound.Water,
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
