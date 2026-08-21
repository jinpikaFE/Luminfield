using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public static class AccessibilityRuntime
{
    public static AccessibilitySettings Settings { get; private set; } = new();

    public static void Apply(AccessibilitySettings settings, Node? uiRoot = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        Settings = settings;
        ThemeFactory.SetTextScale(settings.TextScale, uiRoot);
    }
}
