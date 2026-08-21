using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class AccessibilitySettingsOverlay : FullScreenUi
{
    private readonly AccessibilitySettings _settings;
    private readonly LocaleService _locale;
    private readonly VBoxContainer _rows;
    private readonly Label _title;
    private readonly Label _hint;
    private readonly Button _language;
    private readonly Button _fishing;
    private readonly Button _damage;
    private readonly Button _enemySpeed;
    private readonly Button _shake;
    private readonly Button _targetCues;
    private readonly Button _fontScale;
    private readonly Button _dialoguePace;
    private readonly Button _autoAdvance;
    private readonly Button _autoRun;
    private readonly Button _targetLock;
    private readonly Button _holdRepeat;
    private readonly Button _close;
    private readonly Dictionary<string, Button> _bindingButtons =
        new(StringComparer.Ordinal);
    private string? _pendingBindingAction;

    public AccessibilitySettingsOverlay(
        Theme theme,
        AccessibilitySettings settings,
        LocaleService locale
    ) : base(theme)
    {
        _settings = settings;
        _locale = locale;
        AddChild(Dim(new Color(0.01f, 0.02f, 0.08f, 0.9f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(580, 344)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fc"),
                ThemeFactory.Mint,
                2,
                8
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_title);
        _hint = ThemeFactory.Label(size: 9, color: ThemeFactory.Gold);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_hint);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(548, 230),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        column.AddChild(scroll);
        _rows = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _rows.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_rows);

        _language = AddRow("settings.language");
        _fishing = AddRow("settings.fishing_assist");
        _damage = AddRow("settings.incoming_damage");
        _enemySpeed = AddRow("settings.enemy_speed");
        _shake = AddRow("settings.screen_shake");
        _targetCues = AddRow("settings.target_cues");
        _fontScale = AddRow("settings.font_scale");
        _dialoguePace = AddRow("settings.dialogue_pace");
        _autoAdvance = AddRow("settings.auto_advance");
        _autoRun = AddRow("settings.auto_run");
        _targetLock = AddRow("settings.target_lock");
        _holdRepeat = AddRow("settings.hold_repeat");

        var controlsHeader = ThemeFactory.Label(
            locale.Tr("settings.controls"),
            15,
            ThemeFactory.Gold
        );
        _rows.AddChild(controlsHeader);
        foreach (var action in InputSetup.RebindableActions)
        {
            _bindingButtons[action] = AddRow($"settings.action.{action}");
        }

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(190, 27);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(_close);

        _language.Pressed += () => LanguageRequested?.Invoke();
        _fishing.Pressed += () =>
        {
            _settings.FishingAssist = Next(_settings.FishingAssist);
            Commit();
        };
        _damage.Pressed += () =>
        {
            _settings.IncomingDamagePercent = NextPercent(
                _settings.IncomingDamagePercent,
                [100, 75, 50]
            );
            Commit();
        };
        _enemySpeed.Pressed += () =>
        {
            _settings.EnemySpeedPercent = NextPercent(
                _settings.EnemySpeedPercent,
                [100, 75, 50]
            );
            Commit();
        };
        _shake.Pressed += () =>
        {
            _settings.ScreenShakePercent = NextPercent(
                _settings.ScreenShakePercent,
                [100, 50, 0]
            );
            Commit();
        };
        _targetCues.Pressed += () =>
        {
            _settings.TargetCues = Next(_settings.TargetCues);
            Commit();
        };
        _fontScale.Pressed += () =>
        {
            _settings.FontScalePercent = NextPercent(
                _settings.FontScalePercent,
                [100, 110, 120]
            );
            Commit();
        };
        _dialoguePace.Pressed += () =>
        {
            _settings.DialoguePace = Next(_settings.DialoguePace);
            Commit();
        };
        _autoAdvance.Pressed += () => Toggle(
            () => _settings.DialogueAutoAdvance,
            value => _settings.DialogueAutoAdvance = value
        );
        _autoRun.Pressed += () => Toggle(
            () => _settings.AutoRun,
            value => _settings.AutoRun = value
        );
        _targetLock.Pressed += () => Toggle(
            () => _settings.TargetLock,
            value => _settings.TargetLock = value
        );
        _holdRepeat.Pressed += () => Toggle(
            () => _settings.HoldToRepeatTools,
            value => _settings.HoldToRepeatTools = value
        );
        foreach (var binding in _bindingButtons)
        {
            var action = binding.Key;
            binding.Value.Pressed += () => BeginBinding(action);
        }
        _close.Pressed += () => CloseRequested?.Invoke();
        locale.LocaleChanged += RefreshText;
        RefreshText();
        _language.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? LanguageRequested;
    public event Action? SettingsChanged;
    public event Action<string, Key>? BindingChanged;

    public override void _ExitTree()
    {
        _locale.LocaleChanged -= RefreshText;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_pendingBindingAction is not null &&
            @event.IsActionPressed(InputSetup.UiCancel))
        {
            _pendingBindingAction = null;
            RefreshText();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_pendingBindingAction is null &&
            (@event.IsActionPressed(InputSetup.Pause) ||
             @event.IsActionPressed(InputSetup.UiCancel)))
        {
            CloseRequested?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_pendingBindingAction is null ||
            @event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        var capturedKey = key.PhysicalKeycode != Key.None
            ? key.PhysicalKeycode
            : key.Keycode;
        if (capturedKey == Key.None)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (capturedKey == Key.Escape)
        {
            _pendingBindingAction = null;
            RefreshText();
            GetViewport().SetInputAsHandled();
            return;
        }

        _settings.KeyboardBindings[_pendingBindingAction] =
            (long)capturedKey;
        BindingChanged?.Invoke(_pendingBindingAction, capturedKey);
        _pendingBindingAction = null;
        Commit();
        GetViewport().SetInputAsHandled();
    }

    public void RefreshText()
    {
        _title.Text = _locale.Tr("settings.title");
        _hint.Text = _pendingBindingAction is null
            ? _locale.Tr("settings.hint")
            : _locale.Tr("settings.binding_wait");
        RefreshRowLabels();
        _language.Text = _locale.Tr($"settings.locale.{_locale.CurrentLocale}");
        _fishing.Text = EnumText("settings.fishing", _settings.FishingAssist);
        _damage.Text = _locale.Tr(
            "settings.percent",
            _settings.IncomingDamagePercent
        );
        _enemySpeed.Text = _locale.Tr(
            "settings.percent",
            _settings.EnemySpeedPercent
        );
        _shake.Text = _locale.Tr(
            "settings.percent",
            _settings.ScreenShakePercent
        );
        _targetCues.Text = EnumText("settings.cues", _settings.TargetCues);
        _fontScale.Text = _locale.Tr(
            "settings.percent",
            _settings.FontScalePercent
        );
        _dialoguePace.Text = EnumText(
            "settings.pace",
            _settings.DialoguePace
        );
        _autoAdvance.Text = OnOff(_settings.DialogueAutoAdvance);
        _autoRun.Text = OnOff(_settings.AutoRun);
        _targetLock.Text = OnOff(_settings.TargetLock);
        _holdRepeat.Text = OnOff(_settings.HoldToRepeatTools);
        foreach (var binding in _bindingButtons)
        {
            var key = CurrentKey(binding.Key);
            binding.Value.Text = OS.GetKeycodeString(key);
        }
        _close.Text = _locale.Tr("menu.back");
    }

    private Button AddRow(string labelKey)
    {
        var row = new HBoxContainer();
        row.SetMeta("settings_label_key", labelKey);
        row.AddThemeConstantOverride("separation", 8);
        var label = ThemeFactory.Label(size: 11);
        label.Name = "Label";
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var button = ThemeFactory.Button("");
        button.CustomMinimumSize = new Vector2(212, 25);
        row.AddChild(label);
        row.AddChild(button);
        _rows.AddChild(row);
        return button;
    }

    private void RefreshRowLabels()
    {
        foreach (var child in _rows.GetChildren())
        {
            if (child is not HBoxContainer row ||
                !row.HasMeta("settings_label_key"))
            {
                continue;
            }
            var label = row.GetNode<Label>("Label");
            label.Text = _locale.Tr(
                row.GetMeta("settings_label_key").AsString()
            );
        }
    }

    private void Commit()
    {
        _settings.Normalize();
        SettingsChanged?.Invoke();
        RefreshText();
    }

    private void Toggle(Func<bool> get, Action<bool> set)
    {
        set(!get());
        Commit();
    }

    private void BeginBinding(string action)
    {
        _pendingBindingAction = action;
        RefreshText();
    }

    private Key CurrentKey(string action)
    {
        if (_settings.KeyboardBindings.TryGetValue(action, out var raw))
        {
            return (Key)raw;
        }

        return InputMap.ActionGetEvents(action)
            .OfType<InputEventKey>()
            .Select(input => input.PhysicalKeycode)
            .FirstOrDefault();
    }

    private string OnOff(bool enabled) =>
        _locale.Tr(enabled ? "settings.on" : "settings.off");

    private string EnumText<T>(string prefix, T value) where T : struct, Enum =>
        _locale.Tr($"{prefix}.{value.ToString().ToLowerInvariant()}");

    private static int NextPercent(int current, IReadOnlyList<int> values)
    {
        var index = Array.IndexOf(values.ToArray(), current);
        return values[(index + 1 + values.Count) % values.Count];
    }

    private static T Next<T>(T current) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var index = Array.IndexOf(values, current);
        return values[(index + 1 + values.Length) % values.Length];
    }
}
