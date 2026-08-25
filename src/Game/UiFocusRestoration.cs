using Godot;

namespace Luminfield.Game;

public enum UiFocusRestorationTargetKind
{
    None,
    PreviousFocus,
    FallbackButton
}

public readonly record struct UiFocusRestorationCandidate(
    bool IsInstanceValid,
    bool IsInsideTree,
    bool IsVisibleInTree,
    bool IsFocusable,
    bool IsEnabled
)
{
    public bool CanReceiveFocus =>
        IsInstanceValid &&
        IsInsideTree &&
        IsVisibleInTree &&
        IsFocusable &&
        IsEnabled;
}

public readonly record struct UiFocusRestorationPlan(
    UiFocusRestorationTargetKind TargetKind,
    int FallbackButtonIndex
)
{
    public static UiFocusRestorationPlan None =>
        new(UiFocusRestorationTargetKind.None, -1);

    public static UiFocusRestorationPlan PreviousFocus =>
        new(UiFocusRestorationTargetKind.PreviousFocus, -1);

    public static UiFocusRestorationPlan FallbackButton(int index) =>
        new(UiFocusRestorationTargetKind.FallbackButton, index);
}

public sealed class UiFocusRestoration
{
    private readonly WeakReference<Control>? _previousFocus;

    private UiFocusRestoration(Control? previousFocus)
    {
        if (previousFocus is not null)
        {
            _previousFocus = new WeakReference<Control>(previousFocus);
        }
    }

    public static UiFocusRestoration Capture(Viewport? viewport) =>
        new(viewport?.GuiGetFocusOwner());

    public static UiFocusRestoration Capture(Control? currentFocus) =>
        new(currentFocus);

    public static UiFocusRestorationPlan ChoosePlan(
        UiFocusRestorationCandidate? previousFocus,
        IEnumerable<UiFocusRestorationCandidate> fallbackButtons
    )
    {
        if (previousFocus is { CanReceiveFocus: true })
        {
            return UiFocusRestorationPlan.PreviousFocus;
        }

        var index = 0;
        foreach (var fallbackButton in fallbackButtons)
        {
            if (fallbackButton.CanReceiveFocus)
            {
                return UiFocusRestorationPlan.FallbackButton(index);
            }

            index++;
        }

        return UiFocusRestorationPlan.None;
    }

    public Control? ResolveTarget(Node? fallbackRoot)
    {
        var previous = PreviousFocus();
        if (CanReceiveFocus(previous))
        {
            return previous;
        }

        return FirstAvailableButton(fallbackRoot);
    }

    public bool RestoreDeferred(Node? fallbackRoot)
    {
        if (!IsLiveNode(fallbackRoot))
        {
            return false;
        }

        Callable.From(() =>
        {
            var target = ResolveTarget(fallbackRoot);
            if (target is not null && CanReceiveFocus(target))
            {
                target.GrabFocus();
            }
        }).CallDeferred();
        return true;
    }

    public static UiFocusRestorationCandidate Describe(Control? control)
    {
        var isLive = IsLiveNode(control);
        if (!isLive)
        {
            return new UiFocusRestorationCandidate(
                IsInstanceValid: false,
                IsInsideTree: false,
                IsVisibleInTree: false,
                IsFocusable: false,
                IsEnabled: false
            );
        }

        return new UiFocusRestorationCandidate(
            IsInstanceValid: true,
            IsInsideTree: control!.IsInsideTree(),
            IsVisibleInTree: control.IsVisibleInTree(),
            IsFocusable: control.FocusMode != Control.FocusModeEnum.None,
            IsEnabled: IsEnabled(control)
        );
    }

    public static bool CanReceiveFocus(Control? control) =>
        Describe(control).CanReceiveFocus;

    private Control? PreviousFocus()
    {
        if (_previousFocus is null ||
            !_previousFocus.TryGetTarget(out var focus))
        {
            return null;
        }

        return focus;
    }

    private static Button? FirstAvailableButton(Node? root)
    {
        if (!IsLiveNode(root))
        {
            return null;
        }

        return root!
            .FindChildren("*", "Button", true, false)
            .OfType<Button>()
            .FirstOrDefault(CanReceiveFocus);
    }

    private static bool IsEnabled(Control control)
    {
        if (control is BaseButton button)
        {
            return !button.Disabled;
        }

        return true;
    }

    private static bool IsLiveNode(GodotObject? node) =>
        node is not null && GodotObject.IsInstanceValid(node);
}
