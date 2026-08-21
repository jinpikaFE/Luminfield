using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

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
    public const string UiAccept = "ui_accept";
    public const string UiCancel = "ui_cancel";
    public const string UiUp = "ui_up";
    public const string UiDown = "ui_down";
    public const string UiLeft = "ui_left";
    public const string UiRight = "ui_right";

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
        AddAction(MoveLeft, 0.2f,
            Key.A, Key.Left,
            JoyAxis.LeftX, -1,
            JoyButton.DpadLeft
        );
        AddAction(MoveRight, 0.2f,
            Key.D, Key.Right,
            JoyAxis.LeftX, 1,
            JoyButton.DpadRight
        );
        AddAction(MoveUp, 0.2f,
            Key.W, Key.Up,
            JoyAxis.LeftY, -1,
            JoyButton.DpadUp
        );
        AddAction(MoveDown, 0.2f,
            Key.S, Key.Down,
            JoyAxis.LeftY, 1,
            JoyButton.DpadDown
        );
        AddAction(Interact, 0.5f, Key.E, Key.Space, JoyButton.A);
        AddAction(Pause, 0.5f, Key.Escape, JoyButton.Start);
        AddAction(Backpack, 0.5f, Key.B, Key.Tab, JoyButton.Y);
        AddAction(Crafting, 0.5f, Key.C, JoyButton.X);
        AddAction(CombatDodge, 0.5f, Key.Shift, JoyButton.B);
        AddAction(HotbarPrevious, 0.5f, JoyButton.LeftShoulder);
        AddAction(HotbarNext, 0.5f, JoyButton.RightShoulder);
        AddAction(TargetLock, 0.5f, Key.R, JoyButton.RightStick);
        EnsureUiNavigation();
    }

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

    private static void EnsureUiNavigation()
    {
        Ensure(UiAccept, 0.5f);
        Ensure(UiCancel, 0.5f);
        Ensure(UiUp, 0.5f);
        Ensure(UiDown, 0.5f);
        Ensure(UiLeft, 0.5f);
        Ensure(UiRight, 0.5f);
        AddButton(UiAccept, JoyButton.A);
        AddButton(UiCancel, JoyButton.B);
        AddButton(UiUp, JoyButton.DpadUp);
        AddButton(UiDown, JoyButton.DpadDown);
        AddButton(UiLeft, JoyButton.DpadLeft);
        AddButton(UiRight, JoyButton.DpadRight);
        AddAxis(UiUp, JoyAxis.LeftY, -1);
        AddAxis(UiDown, JoyAxis.LeftY, 1);
        AddAxis(UiLeft, JoyAxis.LeftX, -1);
        AddAxis(UiRight, JoyAxis.LeftX, 1);
    }

    private static void AddAction(
        string action,
        float deadzone,
        Key key1,
        Key key2,
        JoyAxis axis,
        float axisValue,
        JoyButton button
    )
    {
        Ensure(action, deadzone);
        AddKey(action, key1);
        AddKey(action, key2);
        AddAxis(action, axis, axisValue);
        AddButton(action, button);
    }

    private static void AddAction(
        string action,
        float deadzone,
        Key key1,
        Key key2,
        JoyButton button
    )
    {
        Ensure(action, deadzone);
        AddKey(action, key1);
        AddKey(action, key2);
        AddButton(action, button);
    }

    private static void AddAction(string action, float deadzone, Key key, JoyButton button)
    {
        Ensure(action, deadzone);
        AddKey(action, key);
        AddButton(action, button);
    }

    private static void AddAction(string action, float deadzone, JoyButton button)
    {
        Ensure(action, deadzone);
        AddButton(action, button);
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
