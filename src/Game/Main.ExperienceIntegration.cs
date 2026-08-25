using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private OnboardingPlanOverlay? _onboardingOverlay;
    private MorningBriefingOverlay? _morningBriefingOverlay;
    private ImmediateFeedbackAcceptanceGalleryOverlay?
        _feedbackAcceptanceGalleryOverlay;
    private UiFocusRestoration? _pauseChildFocusRestoration;
    private bool _pauseSuspendedForChild;
    private bool _playtestMode;

    private void OpenPauseChild(
        Action openChild,
        Func<bool> isChildOpen
    )
    {
        if (_pauseOverlay is null ||
            !_paused ||
            _pauseSuspendedForChild)
        {
            return;
        }

        _pauseChildFocusRestoration = UiFocusRestoration.Capture(
            GetViewport()
        );
        _pauseSuspendedForChild = true;
        _pauseOverlay.Visible = false;
        openChild();
        if (!isChildOpen())
        {
            RestorePauseAfterChild();
        }
    }

    private bool RestorePauseAfterChild()
    {
        if (!_pauseSuspendedForChild)
        {
            return false;
        }

        if (!_paused || _pauseOverlay is null)
        {
            ResetPauseChildFocusRestoration();
            return false;
        }

        _pauseSuspendedForChild = false;
        _pauseOverlay.Visible = true;
        _pauseChildFocusRestoration?.RestoreDeferred(_pauseOverlay);
        _pauseChildFocusRestoration = null;
        return true;
    }

    private void ResetPauseChildFocusRestoration()
    {
        _pauseSuspendedForChild = false;
        _pauseChildFocusRestoration = null;
    }

    private bool TryCloseExperienceOverlay(
        InputEvent @event,
        bool overlayCancelPressed
    )
    {
        var morningBriefingPressed = @event.IsActionPressed(
            InputSetup.MorningBriefing
        );
        if ((overlayCancelPressed || morningBriefingPressed) &&
            _morningBriefingOverlay is not null)
        {
            CloseMorningBriefing();
            GetViewport().SetInputAsHandled();
            return true;
        }

        var onboardingPressed = @event.IsActionPressed(
            InputSetup.OnboardingPlan
        );
        if ((overlayCancelPressed || onboardingPressed) &&
            _onboardingOverlay is not null)
        {
            CloseOnboardingPlan();
            GetViewport().SetInputAsHandled();
            return true;
        }

        var routeGuidancePressed = @event.IsActionPressed(
            InputSetup.HudRouteGuidance
        );
        if ((overlayCancelPressed || routeGuidancePressed) &&
            _routeGuidanceOverlay is not null)
        {
            CloseRouteGuidance();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private bool TryOpenExperienceOverlay(InputEvent @event)
    {
        if (IsInputBlocked)
        {
            return false;
        }

        if (@event.IsActionPressed(InputSetup.OnboardingPlan))
        {
            OpenOnboardingPlan();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputSetup.MorningBriefing))
        {
            OpenMorningBriefing();
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputSetup.HudRouteGuidance))
        {
            OpenRouteGuidance();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private void OpenPause()
    {
        if (_paused)
        {
            return;
        }

        _paused = true;
        SetWorldControls(false);
        var onboardingPlanAvailable = !OnboardingPlanSystem.Create(
            _session,
            _settings.DismissedOnboardingCardIds
        ).IsEmpty;
        _pauseOverlay = new PauseOverlay(
            _theme,
            _locale,
            onboardingPlanAvailable
        );
        _pauseOverlay.ResumeRequested += ClosePause;
        _pauseOverlay.OnboardingPlanRequested += () =>
            OpenPauseChild(
                OpenOnboardingPlan,
                () => _onboardingOverlay is not null
            );
        _pauseOverlay.MorningBriefingRequested += () =>
            OpenPauseChild(
                OpenMorningBriefing,
                () => _morningBriefingOverlay is not null
            );
        _pauseOverlay.RouteGuidanceRequested += () =>
            OpenPauseChild(
                OpenRouteGuidance,
                () => _routeGuidanceOverlay is not null
            );
        _pauseOverlay.GleamriseGoalsRequested += () =>
            OpenPauseChild(
                OpenGleamriseSeasonGoals,
                () => _gleamriseSeasonOverlay is not null
            );
        _pauseOverlay.FishingCollectionRequested += () =>
            OpenPauseChild(
                OpenFishingCollection,
                () => _fishingCollectionOverlay is not null
            );
        _pauseOverlay.FishingGearRequested += () =>
            OpenPauseChild(
                OpenFishingGear,
                () => _fishingGearOverlay is not null
            );
        _pauseOverlay.StellarResonanceRequested += () =>
            OpenPauseChild(
                OpenStellarResonance,
                () => _stellarResonanceOverlay is not null
            );
        _pauseOverlay.LanguageRequested += () =>
            OpenPauseChild(
                OpenSettings,
                () => _settingsOverlay is not null
            );
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

        ResetPauseChildFocusRestoration();
        _paused = false;
        FreeUi(_pauseOverlay);
        _pauseOverlay = null;
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OpenFeedbackAcceptanceGallery(
        IEnumerable<string> arguments
    )
    {
        if (_feedbackAcceptanceGalleryOverlay is not null)
        {
            return;
        }

        _feedbackAcceptanceGalleryOverlay =
            new ImmediateFeedbackAcceptanceGalleryOverlay(
                _theme,
                _locale,
                _settings,
                ImmediateFeedbackAcceptanceGallery.SelectedDomain(arguments)
            );
        _feedbackAcceptanceGalleryOverlay.CloseRequested +=
            CloseFeedbackAcceptanceGallery;
        _uiLayer.AddChild(_feedbackAcceptanceGalleryOverlay);
    }

    private void CloseFeedbackAcceptanceGallery()
    {
        if (_feedbackAcceptanceGalleryOverlay is null)
        {
            return;
        }

        _feedbackAcceptanceGalleryOverlay.CloseRequested -=
            CloseFeedbackAcceptanceGallery;
        FreeUi(_feedbackAcceptanceGalleryOverlay);
        _feedbackAcceptanceGalleryOverlay = null;
    }

    private void OpenPauseExperiencePreview()
    {
        if (_pauseOverlay is not null)
        {
            return;
        }

        _pauseOverlay = new PauseOverlay(
            _theme,
            _locale,
            onboardingPlanAvailable: true
        );
        _uiLayer.AddChild(_pauseOverlay);
    }

    private void OpenOnboardingPlan()
    {
        if (_onboardingOverlay is not null)
        {
            return;
        }

        var plan = OnboardingPlanSystem.Create(
            _session,
            _settings.DismissedOnboardingCardIds
        );
        if (plan.IsEmpty)
        {
            return;
        }

        SetWorldControls(false);
        _onboardingOverlay = new OnboardingPlanOverlay(
            _theme,
            plan,
            OnboardingPlanSystem.CreateNinetyMinuteCoverageContract(_session),
            _locale
        );
        _onboardingOverlay.CardDismissed += OnOnboardingCardDismissed;
        _onboardingOverlay.CloseRequested += CloseOnboardingPlan;
        _uiLayer.AddChild(_onboardingOverlay);
    }

    private void CloseOnboardingPlan()
    {
        if (_onboardingOverlay is null)
        {
            return;
        }

        _onboardingOverlay.CardDismissed -= OnOnboardingCardDismissed;
        _onboardingOverlay.CloseRequested -= CloseOnboardingPlan;
        FreeUi(_onboardingOverlay);
        _onboardingOverlay = null;
        if (RestorePauseAfterChild())
        {
            return;
        }
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OnOnboardingCardDismissed(string cardId)
    {
        if (_settings.DismissOnboardingCard(cardId))
        {
            _settingsService.Save(_settings);
        }
    }

    private void TryOpenMorningBriefingForCurrentDay()
    {
        if (_session.ExperienceGuidance.WasMorningBriefingShown(
                _session.Clock.Day
            ) ||
            _session.Clock.MinuteOfDay != GameClock.StartMinute)
        {
            return;
        }

        OpenMorningBriefing();
    }

    private void OpenMorningBriefing()
    {
        if (_morningBriefingOverlay is not null)
        {
            return;
        }

        SetWorldControls(false);
        _morningBriefingOverlay = new MorningBriefingOverlay(
            _theme,
            _session,
            _locale
        );
        _morningBriefingOverlay.NavigationRequested +=
            OnMorningBriefingNavigationRequested;
        _morningBriefingOverlay.CloseRequested += CloseMorningBriefing;
        _uiLayer.AddChild(_morningBriefingOverlay);
        if (_playing && _session.ExperienceGuidance.MarkMorningBriefingShown(
                _session.Clock.Day
            ))
        {
            SaveNow(false);
        }
    }

    private void CloseMorningBriefing()
    {
        if (_morningBriefingOverlay is null)
        {
            return;
        }

        _morningBriefingOverlay.NavigationRequested -=
            OnMorningBriefingNavigationRequested;
        _morningBriefingOverlay.CloseRequested -= CloseMorningBriefing;
        FreeUi(_morningBriefingOverlay);
        _morningBriefingOverlay = null;
        if (RestorePauseAfterChild())
        {
            return;
        }
        Callable.From(TryOpenFarmingSpecialization).CallDeferred();
        if (CanRestoreWorldControls)
        {
            SetWorldControls(true);
        }
    }

    private void OnMorningBriefingNavigationRequested(
        WorldNavigationDestination destination
    )
    {
        StartRouteGuidanceJourney(destination);
        CloseMorningBriefing();
    }
}

public static class OnboardingPlanStartup
{
    public const string OpenFlag = "--open-onboarding-plan";

    public static bool ShouldOpen(IEnumerable<string> arguments) =>
        arguments.Contains(OpenFlag, StringComparer.Ordinal);
}

public static class MorningBriefingStartup
{
    public const string OpenFlag = "--open-morning-briefing";

    public static bool ShouldOpen(IEnumerable<string> arguments) =>
        arguments.Contains(OpenFlag, StringComparer.Ordinal);
}
