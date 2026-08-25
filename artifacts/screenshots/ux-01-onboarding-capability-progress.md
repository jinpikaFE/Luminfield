# UX-01 onboarding capability progress acceptance

- Worktree: `/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- Branch: `codex/base-01-playtest-bindings`
- Scope: first-day onboarding overlay only.
- Runtime entry: `--open-onboarding-plan --capture-playtest=res://artifacts/screenshots/ux-01-onboarding-capability-progress.png`
- Screenshot: `artifacts/screenshots/ux-01-onboarding-capability-progress.png`
- Renderer: Godot 4.7.1 .NET, Apple M3, Metal Compatibility.

## Result

- The overlay now shows all six opening capability areas at once.
- The progress summary is derived from `OnboardingNinetyMinuteCoverageContract`.
- It does not add save state, business state, localization keys, or dismissed-card state.
- Dismissing a card removes only the card from the carousel; the six capability summary remains based on the coverage contract.
- Visual review passed at the 1280x720 window that represents the 640x360 internal canvas.

## Validation

- `OnboardingPlanPresenterTests|OnboardingPlanSystemTests`: 18/18 passed.
- `git diff --check`: passed after this artifact was added.

## Not covered

- This is deterministic onboarding UI evidence, not a human 90-minute playtest.
- Physical controller and Windows/Linux runtime checks were not run in this increment.
