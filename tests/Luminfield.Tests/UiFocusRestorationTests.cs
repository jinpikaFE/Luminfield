using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class UiFocusRestorationTests
{
    [Fact]
    public void PreviousFocusWinsWhenStillUsable()
    {
        var plan = UiFocusRestoration.ChoosePlan(
            Usable(),
            [Usable()]
        );

        Assert.Equal(
            UiFocusRestorationTargetKind.PreviousFocus,
            plan.TargetKind
        );
        Assert.Equal(-1, plan.FallbackButtonIndex);
    }

    [Theory]
    [MemberData(nameof(UnusablePreviousFocusCases))]
    public void InvalidPreviousFocusFallsBackToFirstUsableButton(
        UiFocusRestorationCandidate previousFocus
    )
    {
        var plan = UiFocusRestoration.ChoosePlan(
            previousFocus,
            [
                NotFocusable(),
                Usable()
            ]
        );

        Assert.Equal(
            UiFocusRestorationTargetKind.FallbackButton,
            plan.TargetKind
        );
        Assert.Equal(1, plan.FallbackButtonIndex);
    }

    [Fact]
    public void MissingFallbackLeavesFocusUntouched()
    {
        var plan = UiFocusRestoration.ChoosePlan(
            Released(),
            [
                Hidden(),
                Disabled(),
                NotFocusable()
            ]
        );

        Assert.Equal(UiFocusRestorationTargetKind.None, plan.TargetKind);
        Assert.Equal(-1, plan.FallbackButtonIndex);
    }

    [Fact]
    public void EmptyFallbackListIsSafe()
    {
        var plan = UiFocusRestoration.ChoosePlan(
            previousFocus: null,
            fallbackButtons: []
        );

        Assert.Equal(UiFocusRestorationTargetKind.None, plan.TargetKind);
    }

    [Theory]
    [MemberData(nameof(UnusablePreviousFocusCases))]
    public void CandidateReportsOnlyCompleteFocusStateAsUsable(
        UiFocusRestorationCandidate candidate
    )
    {
        Assert.False(candidate.CanReceiveFocus);
        Assert.True(Usable().CanReceiveFocus);
    }

    public static TheoryData<UiFocusRestorationCandidate>
        UnusablePreviousFocusCases => new()
    {
        Released(),
        Detached(),
        Hidden(),
        NotFocusable(),
        Disabled()
    };

    private static UiFocusRestorationCandidate Usable() =>
        new(
            IsInstanceValid: true,
            IsInsideTree: true,
            IsVisibleInTree: true,
            IsFocusable: true,
            IsEnabled: true
        );

    private static UiFocusRestorationCandidate Released() =>
        Usable() with { IsInstanceValid = false };

    private static UiFocusRestorationCandidate Detached() =>
        Usable() with { IsInsideTree = false };

    private static UiFocusRestorationCandidate Hidden() =>
        Usable() with { IsVisibleInTree = false };

    private static UiFocusRestorationCandidate NotFocusable() =>
        Usable() with { IsFocusable = false };

    private static UiFocusRestorationCandidate Disabled() =>
        Usable() with { IsEnabled = false };
}
