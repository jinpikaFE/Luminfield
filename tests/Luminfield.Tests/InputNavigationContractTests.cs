using Godot;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class InputNavigationContractTests
{
    [Theory]
    [InlineData(InputSetup.HudObjectiveDetails, Key.O)]
    [InlineData(InputSetup.HudMinimapToggle, Key.M)]
    [InlineData(InputSetup.HudMinimapFilter, Key.N)]
    [InlineData(InputSetup.HudRouteGuidance, Key.G)]
    public void HudKeyboardBindingsExposeExplicitKeys(
        string action,
        Key key
    )
    {
        var binding = BindingFor(
            action,
            InputBindingKind.KeyboardKey,
            candidate => candidate.KeyboardKey == key
        );

        Assert.Equal(key, binding.KeyboardKey);
    }

    [Fact]
    public void HudControllerButtonsExposeBackAndLeftStick()
    {
        var details = BindingFor(
            InputSetup.HudObjectiveDetails,
            InputBindingKind.JoypadButton,
            candidate => candidate.JoypadButton == JoyButton.Back
        );
        var minimap = BindingFor(
            InputSetup.HudMinimapToggle,
            InputBindingKind.JoypadButton,
            candidate => candidate.JoypadButton == JoyButton.LeftStick
        );

        Assert.Equal(JoyButton.Back, details.JoypadButton);
        Assert.Equal(JoyButton.LeftStick, minimap.JoypadButton);
        Assert.DoesNotContain(
            InputSetup.BindingsFor(InputSetup.HudMinimapFilter),
            candidate => candidate.Kind == InputBindingKind.JoypadButton
        );
        Assert.DoesNotContain(
            InputSetup.BindingsFor(InputSetup.HudRouteGuidance),
            candidate => candidate.Kind != InputBindingKind.KeyboardKey
        );
    }

    [Theory]
    [InlineData(InputSetup.UiAccept, JoyButton.A)]
    [InlineData(InputSetup.UiCancel, JoyButton.B)]
    [InlineData(InputSetup.UiUp, JoyButton.DpadUp)]
    [InlineData(InputSetup.UiDown, JoyButton.DpadDown)]
    [InlineData(InputSetup.UiLeft, JoyButton.DpadLeft)]
    [InlineData(InputSetup.UiRight, JoyButton.DpadRight)]
    public void UiNavigationButtonsExposeExplicitJoypadButtons(
        string action,
        JoyButton button
    )
    {
        var binding = BindingFor(
            action,
            InputBindingKind.JoypadButton,
            candidate => candidate.JoypadButton == button
        );

        Assert.Equal(button, binding.JoypadButton);
    }

    [Theory]
    [InlineData(InputSetup.UiUp, JoyAxis.LeftY, -1f)]
    [InlineData(InputSetup.UiDown, JoyAxis.LeftY, 1f)]
    [InlineData(InputSetup.UiLeft, JoyAxis.LeftX, -1f)]
    [InlineData(InputSetup.UiRight, JoyAxis.LeftX, 1f)]
    public void UiNavigationAxesExposeExplicitJoypadMotionContracts(
        string action,
        JoyAxis axis,
        float value
    )
    {
        var binding = BindingFor(
            action,
            InputBindingKind.JoypadAxis,
            candidate => candidate.JoypadAxis == axis &&
                Math.Sign(candidate.AxisValue) == Math.Sign(value)
        );

        Assert.Equal(axis, binding.JoypadAxis);
        Assert.Equal(Math.Sign(value), Math.Sign(binding.AxisValue));
    }

    [Fact]
    public void ObjectiveDetailsContractKeepsOpenCloseAndFocusReachable()
    {
        Assert.Equal(
            InputSetup.HudObjectiveDetails,
            HudInputNavigationContract.ObjectiveDetailsOpenAction
        );
        Assert.Equal(
            "menu.back",
            HudInputNavigationContract.ObjectiveDetailsFocusedControlKey
        );
        Assert.Contains(
            InputSetup.HudObjectiveDetails,
            HudInputNavigationContract.ObjectiveDetailsCloseActions
        );
        Assert.Contains(
            InputSetup.UiCancel,
            HudInputNavigationContract.ObjectiveDetailsCloseActions
        );
        Assert.Contains(
            InputSetup.Pause,
            HudInputNavigationContract.ObjectiveDetailsCloseActions
        );
    }

    [Fact]
    public void SettingsAndGuidanceContractsUseFirstButtonFocus()
    {
        Assert.Equal(
            "settings.language",
            AccessibilitySettingsOverlay.InitialFocusLabelKey
        );

        var settings = SurfaceContract(InputSetup.SettingsSurface);
        Assert.Equal(
            InputSetup.FocusPolicyFirstEnabledButton,
            settings.InitialFocusPolicy
        );
        Assert.Contains(InputSetup.UiCancel, settings.CloseActions);
        Assert.Contains(InputSetup.Pause, settings.CloseActions);
        AssertNavigationActions(settings);

        var guidance = SurfaceContract(InputSetup.GuidanceCardsSurface);
        Assert.Equal(
            InputSetup.FocusPolicyFirstEnabledButton,
            guidance.InitialFocusPolicy
        );
        Assert.Contains(InputSetup.UiCancel, guidance.CloseActions);
        AssertNavigationActions(guidance);
    }

    [Fact]
    public void PauseMenuContractMakesExperiencePanelsControllerReachable()
    {
        var pause = SurfaceContract(InputSetup.PauseSurface);
        Assert.Equal(
            InputSetup.FocusPolicyFirstEnabledButton,
            pause.InitialFocusPolicy
        );
        Assert.Contains(InputSetup.UiCancel, pause.CloseActions);
        Assert.Contains(InputSetup.Pause, pause.CloseActions);
        AssertNavigationActions(pause);

        Assert.Equal(
            new[]
            {
                PauseOverlay.OnboardingPlanMenuKey,
                PauseOverlay.OnboardingPlanSkippedMenuKey,
                PauseOverlay.MorningBriefingMenuKey,
                PauseOverlay.RouteGuidanceMenuKey
            },
            PauseOverlay.ExperienceMenuLabelKeys
        );
    }

    [Fact]
    public void RouteGuidanceUsesStandardControllerNavigationAndCancel()
    {
        var routeGuidance = SurfaceContract(
            InputSetup.RouteGuidanceSurface
        );

        Assert.Equal(
            InputSetup.FocusPolicyFirstEnabledButton,
            routeGuidance.InitialFocusPolicy
        );
        Assert.Contains(InputSetup.UiCancel, routeGuidance.CloseActions);
        Assert.Contains(InputSetup.Pause, routeGuidance.CloseActions);
        AssertNavigationActions(routeGuidance);
    }

    [Fact]
    public void PauseExperiencePreviewUsesOneExactOptInFlag()
    {
        Assert.False(PauseOverlayExperiencePreviewStartup.ShouldOpen([]));
        Assert.True(PauseOverlayExperiencePreviewStartup.ShouldOpen(
            [PauseOverlayExperiencePreviewStartup.OpenFlag]
        ));
        Assert.False(PauseOverlayExperiencePreviewStartup.ShouldOpen(
            ["--ui-pause-experience-preview-extra"]
        ));
    }

    private static void AssertNavigationActions(
        UiSurfaceNavigationContract contract
    )
    {
        Assert.Contains(InputSetup.UiUp, contract.NavigationActions);
        Assert.Contains(InputSetup.UiDown, contract.NavigationActions);
        Assert.Contains(InputSetup.UiLeft, contract.NavigationActions);
        Assert.Contains(InputSetup.UiRight, contract.NavigationActions);
        Assert.Contains(InputSetup.UiAccept, contract.NavigationActions);
        Assert.Contains(InputSetup.UiCancel, contract.NavigationActions);
    }

    private static UiSurfaceNavigationContract SurfaceContract(
        string surfaceId
    ) => Assert.Single(
        InputSetup.FullScreenSurfaceNavigationContracts,
        contract => contract.SurfaceId == surfaceId
    );

    private static InputBindingContract BindingFor(
        string action,
        InputBindingKind kind,
        Func<InputBindingContract, bool> predicate
    )
    {
        return Assert.Single(
            InputSetup.BindingsFor(action),
            candidate => candidate.Kind == kind && predicate(candidate)
        );
    }

}
