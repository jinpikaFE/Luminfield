using Godot;

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
    public const string HotbarPrevious = "hotbar_previous";
    public const string HotbarNext = "hotbar_next";

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
        AddAction(HotbarPrevious, 0.5f, JoyButton.LeftShoulder);
        AddAction(HotbarNext, 0.5f, JoyButton.RightShoulder);
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
