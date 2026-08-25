using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public enum InputBindingKind
{
    KeyboardKey,
    JoypadButton,
    JoypadAxis
}

public readonly record struct InputBindingContract(
    InputBindingKind Kind,
    Key KeyboardKey,
    JoyButton JoypadButton,
    JoyAxis JoypadAxis,
    float AxisValue
)
{
    public static InputBindingContract ForKey(Key key) =>
        new(InputBindingKind.KeyboardKey, key, default, default, 0f);

    public static InputBindingContract ForButton(JoyButton button) =>
        new(InputBindingKind.JoypadButton, default, button, default, 0f);

    public static InputBindingContract ForAxis(JoyAxis axis, float value) =>
        new(InputBindingKind.JoypadAxis, default, default, axis, value);

    public bool Matches(InputEvent @event)
    {
        return Kind switch
        {
            InputBindingKind.KeyboardKey => MatchesKey(@event),
            InputBindingKind.JoypadButton => MatchesButton(@event),
            InputBindingKind.JoypadAxis => MatchesAxis(@event),
            _ => false
        };
    }

    private bool MatchesKey(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return false;
        }

        return key.PhysicalKeycode == KeyboardKey || key.Keycode == KeyboardKey;
    }

    private bool MatchesButton(InputEvent @event)
    {
        return @event is InputEventJoypadButton { Pressed: true } button &&
            button.ButtonIndex == JoypadButton;
    }

    private bool MatchesAxis(InputEvent @event)
    {
        if (@event is not InputEventJoypadMotion motion ||
            motion.Axis != JoypadAxis)
        {
            return false;
        }

        if (AxisValue < 0)
        {
            return motion.AxisValue < 0;
        }

        return motion.AxisValue > 0;
    }
}

public readonly record struct InputActionContract(
    string Action,
    float Deadzone,
    IReadOnlyList<InputBindingContract> Bindings
);

public readonly record struct UiSurfaceNavigationContract(
    string SurfaceId,
    string InitialFocusPolicy,
    IReadOnlyList<string> NavigationActions,
    IReadOnlyList<string> CloseActions
);

public static class InputSetup
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string Interact = "interact";
    public const string Pause = "pause";
    public const string Backpack = "backpack";
    public const string Crafting = "crafting";
    public const string CombatDodge = "combat_dodge";
    public const string HotbarPrevious = "hotbar_previous";
    public const string HotbarNext = "hotbar_next";
    public const string TargetLock = "target_lock";
    public const string HudObjectiveDetails = "hud_objective_details";
    public const string HudMinimapToggle = "hud_minimap_toggle";
    public const string HudMinimapFilter = "hud_minimap_filter";
    public const string HudRouteGuidance = "hud_route_guidance";
    public const string OnboardingPlan = "onboarding_plan";
    public const string MorningBriefing = "morning_briefing";
    public const string UiAccept = "ui_accept";
    public const string UiCancel = "ui_cancel";
    public const string UiUp = "ui_up";
    public const string UiDown = "ui_down";
    public const string UiLeft = "ui_left";
    public const string UiRight = "ui_right";
    public const string FocusPolicyFirstEnabledButton = "first_enabled_button";
    public const string PauseSurface = "pause";
    public const string SettingsSurface = "settings";
    public const string GuidanceCardsSurface = "guidance_cards";
    public const string RouteGuidanceSurface = "route_guidance";

    public static IReadOnlyList<InputActionContract> DefaultActionContracts { get; } =
    [
        new(MoveLeft, 0.2f,
        [
            InputBindingContract.ForKey(Key.A),
            InputBindingContract.ForKey(Key.Left),
            InputBindingContract.ForAxis(JoyAxis.LeftX, -1f),
            InputBindingContract.ForButton(JoyButton.DpadLeft)
        ]),
        new(MoveRight, 0.2f,
        [
            InputBindingContract.ForKey(Key.D),
            InputBindingContract.ForKey(Key.Right),
            InputBindingContract.ForAxis(JoyAxis.LeftX, 1f),
            InputBindingContract.ForButton(JoyButton.DpadRight)
        ]),
        new(MoveUp, 0.2f,
        [
            InputBindingContract.ForKey(Key.W),
            InputBindingContract.ForKey(Key.Up),
            InputBindingContract.ForAxis(JoyAxis.LeftY, -1f),
            InputBindingContract.ForButton(JoyButton.DpadUp)
        ]),
        new(MoveDown, 0.2f,
        [
            InputBindingContract.ForKey(Key.S),
            InputBindingContract.ForKey(Key.Down),
            InputBindingContract.ForAxis(JoyAxis.LeftY, 1f),
            InputBindingContract.ForButton(JoyButton.DpadDown)
        ]),
        new(Interact, 0.5f,
        [
            InputBindingContract.ForKey(Key.E),
            InputBindingContract.ForKey(Key.Space),
            InputBindingContract.ForButton(JoyButton.A)
        ]),
        new(Pause, 0.5f,
        [
            InputBindingContract.ForKey(Key.Escape),
            InputBindingContract.ForButton(JoyButton.Start)
        ]),
        new(Backpack, 0.5f,
        [
            InputBindingContract.ForKey(Key.B),
            InputBindingContract.ForKey(Key.Tab),
            InputBindingContract.ForButton(JoyButton.Y)
        ]),
        new(Crafting, 0.5f,
        [
            InputBindingContract.ForKey(Key.C),
            InputBindingContract.ForButton(JoyButton.X)
        ]),
        new(CombatDodge, 0.5f,
        [
            InputBindingContract.ForKey(Key.Shift),
            InputBindingContract.ForButton(JoyButton.B)
        ]),
        new(HotbarPrevious, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.LeftShoulder)
        ]),
        new(HotbarNext, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.RightShoulder)
        ]),
        new(TargetLock, 0.5f,
        [
            InputBindingContract.ForKey(Key.R),
            InputBindingContract.ForButton(JoyButton.RightStick)
        ]),
        new(HudObjectiveDetails, 0.5f,
        [
            InputBindingContract.ForKey(Key.O),
            InputBindingContract.ForButton(JoyButton.Back)
        ]),
        new(HudMinimapToggle, 0.5f,
        [
            InputBindingContract.ForKey(Key.M),
            InputBindingContract.ForButton(JoyButton.LeftStick)
        ]),
        new(HudMinimapFilter, 0.5f,
        [
            InputBindingContract.ForKey(Key.N)
        ]),
        new(HudRouteGuidance, 0.5f,
        [
            InputBindingContract.ForKey(Key.G)
        ]),
        new(OnboardingPlan, 0.5f,
        [
            InputBindingContract.ForKey(Key.H)
        ]),
        new(MorningBriefing, 0.5f,
        [
            InputBindingContract.ForKey(Key.J)
        ]),
        new(UiAccept, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.A)
        ]),
        new(UiCancel, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.B)
        ]),
        new(UiUp, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.DpadUp),
            InputBindingContract.ForAxis(JoyAxis.LeftY, -1f)
        ]),
        new(UiDown, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.DpadDown),
            InputBindingContract.ForAxis(JoyAxis.LeftY, 1f)
        ]),
        new(UiLeft, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.DpadLeft),
            InputBindingContract.ForAxis(JoyAxis.LeftX, -1f)
        ]),
        new(UiRight, 0.5f,
        [
            InputBindingContract.ForButton(JoyButton.DpadRight),
            InputBindingContract.ForAxis(JoyAxis.LeftX, 1f)
        ])
    ];

    public static IReadOnlyList<string> FullScreenNavigationActions { get; } =
    [
        UiUp,
        UiDown,
        UiLeft,
        UiRight,
        UiAccept,
        UiCancel
    ];

    public static IReadOnlyList<UiSurfaceNavigationContract>
        FullScreenSurfaceNavigationContracts { get; } =
    [
        new(
            PauseSurface,
            FocusPolicyFirstEnabledButton,
            FullScreenNavigationActions,
            [UiCancel, Pause]
        ),
        new(
            SettingsSurface,
            FocusPolicyFirstEnabledButton,
            FullScreenNavigationActions,
            [UiCancel, Pause]
        ),
        new(
            GuidanceCardsSurface,
            FocusPolicyFirstEnabledButton,
            FullScreenNavigationActions,
            [UiCancel]
        ),
        new(
            RouteGuidanceSurface,
            FocusPolicyFirstEnabledButton,
            FullScreenNavigationActions,
            [UiCancel, Pause]
        )
    ];

    public static IReadOnlyList<string> RebindableActions { get; } =
    [
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Interact,
        Backpack,
        Crafting,
        CombatDodge,
        TargetLock
    ];

    public static void EnsureActions()
    {
        foreach (var contract in DefaultActionContracts)
        {
            Ensure(contract.Action, contract.Deadzone);
            foreach (var binding in contract.Bindings)
            {
                AddBinding(contract.Action, binding);
            }
        }
    }

    public static InputActionContract ContractFor(string action) =>
        DefaultActionContracts.Single(contract =>
            contract.Action == action
        );

    public static IReadOnlyList<InputBindingContract> BindingsFor(
        string action
    ) => ContractFor(action).Bindings;

    public static void ApplyKeyboardBindings(
        AccessibilitySettings settings
    )
    {
        foreach (var action in RebindableActions)
        {
            if (!settings.KeyboardBindings.TryGetValue(action, out var rawKey) ||
                !Enum.IsDefined((Key)rawKey) ||
                (Key)rawKey == Key.None)
            {
                continue;
            }

            foreach (var inputEvent in InputMap.ActionGetEvents(action))
            {
                if (inputEvent is InputEventKey)
                {
                    InputMap.ActionEraseEvent(action, inputEvent);
                }
            }
            AddKey(action, (Key)rawKey);
        }
    }

    private static void AddBinding(string action, InputBindingContract binding)
    {
        switch (binding.Kind)
        {
            case InputBindingKind.KeyboardKey:
                AddKey(action, binding.KeyboardKey);
                break;
            case InputBindingKind.JoypadButton:
                AddButton(action, binding.JoypadButton);
                break;
            case InputBindingKind.JoypadAxis:
                AddAxis(action, binding.JoypadAxis, binding.AxisValue);
                break;
        }
    }

    private static void Ensure(string action, float deadzone)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action, deadzone);
        }
    }

    private static void AddKey(string action, Key key)
    {
        if (key == Key.None)
        {
            return;
        }

        var input = new InputEventKey { PhysicalKeycode = key };
        if (!InputMap.ActionHasEvent(action, input))
        {
            InputMap.ActionAddEvent(action, input);
        }
    }

    private static void AddButton(string action, JoyButton button)
    {
        var input = new InputEventJoypadButton { ButtonIndex = button };
        if (!InputMap.ActionHasEvent(action, input))
        {
            InputMap.ActionAddEvent(action, input);
        }
    }

    private static void AddAxis(string action, JoyAxis axis, float value)
    {
        var input = new InputEventJoypadMotion
        {
            Axis = axis,
            AxisValue = value
        };
        if (!InputMap.ActionHasEvent(action, input))
        {
            InputMap.ActionAddEvent(action, input);
        }
    }
}
