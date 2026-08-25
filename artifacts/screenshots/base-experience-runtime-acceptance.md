# BASE / UI / FEEL / AUDIO final-source runtime acceptance

- Date: 2026-08-24 21:16 CST
- Checkout: `/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- Branch: `codex/base-01-playtest-bindings`
- Base HEAD: `5696a6fb128ee44cadcb3bd9a944d226cb86156c`
- Runtime: Godot 4.7.1 .NET, OpenGL 4.1 Metal Compatibility, Apple M3
- Capture size: 1280×720, representing the 640×360 internal canvas at 2× nearest-neighbor scale

## Pause-menu experience access

`ui-01-pause-experience-preview.png` renders the real `PauseOverlay` with the new first-day tips and morning briefing entries. The first focus ring is visible on Resume, the two experience entries are visible without scrolling, and the remaining menu continues through the vertical scroll container instead of overflowing the 640×360 canvas.

The runtime wiring keeps the pause menu alive but hidden when it opens first-day tips, morning briefing, or settings. Closing the child overlay restores the saved pause-button focus through `UiFocusRestoration`; released, hidden, disabled, detached, or non-focusable controls fall back to the first available button.

## Immediate-feedback matrix

Each image below contains one full eight-tile domain matrix: four outcomes × standard/reduced motion. The matrix initially used four columns, and Metal inspection exposed right-edge clipping. The final two-column/four-row layout was recaptured only after all eight tiles fit the canvas at once.

- `feel-01-feedback-tool.png`
- `feel-01-feedback-watering.png`
- `feel-01-feedback-harvest.png`
- `feel-01-feedback-pickup.png`
- `feel-01-feedback-processor.png`
- `feel-01-feedback-fishing.png`
- `feel-01-feedback-damage.png`
- `feel-01-feedback-dodge.png`
- `feel-01-feedback-reward.png`

The images prove localized copy, outcome colors, border treatment, standard pulse/shake values, and zero pulse/shake in reduced-effects mode for all 72 deterministic combinations. They do not prove subjective timing, sound strength, or physical-controller behavior.

## Semantic feedback audio

`ImmediateFeedbackAudio.SoundFor` now maps the same cue outcome used by the HUD to sound:

- success remains silent here except damage and dodge, so tool, water, harvest, pickup, fishing and reward successes continue using their original action sounds without a duplicate success tone;
- generic failure plays `PixelSound.Error`;
- resource-blocked cues play `PixelSound.ResourceBlocked`;
- tool-mismatch cues play `PixelSound.ToolMismatch`;
- reduced-effects cues keep the same semantic sound as standard cues.

The generated audio preview now includes `effect-resource-blocked.wav` and `effect-tool-mismatch.wav`, and `audio-01-acceptance-tour.wav` includes the three negative feedback cues in the same audition segment.

## Morning decision summary

`loop-01-morning-decision-summary.png` renders the real seven-card briefing with
a compact read-only summary above the scroll area. It selects no more than three
actionable Primary/Secondary cards in stable priority order, skips cards without
an action, and reuses existing localized card titles/actions. It does not save a
second objective or mutate the session. The Apple M3 / Metal capture keeps the
summary, card scroll area, and bottom close button inside the 640×360 canvas.

## First-day capability overview

`ux-01-onboarding-capability-progress.png` renders the real first-day guidance
panel with all six quest, weather, shipping, processing, commission, and
exploration capabilities visible above the active card. Each marker derives
from the existing not-started/in-progress/complete coverage contract. Dismissing
a guidance card changes only the rotating card list; it does not remove the
capability overview or manufacture completion. The Apple M3 / Metal capture
keeps the overview, action/location/result copy, and all three buttons inside
the 640×360 canvas.

## Regional ambience and route audit

Clear-weather ambience now resolves separately for homestead, village, and
wilderness regions; village interiors retain the village family and the crystal
survey retains the wilderness family. Weather, festival, and combat continue to
take precedence. Together with the existing contexts this produces eight
procedural loops. The regenerated 60.85-second audition and technical report
cover all eight loops, all seven crossfade boundaries, and 15 effect sounds.

`world-01-route-walk-audit.md` reconstructs all six route contracts one cell at
a time against real `WorldDefinition` passability. Every route is continuous;
the longest is 174 steps and the largest guide interval is 18 cells. This does
not replace a human wayfinding or getting-lost test. The read-only
`WorldNavigationRouteProgressPresenter` now exposes the nearest path cell, next
guide or endpoint, dominant four-way direction, off-route distance, and
remaining steps for every route. It is intentionally not auto-selected in the
normal HUD yet because the player has not chosen a destination.

Pause, onboarding, briefing, feedback, and their input routing now live in
`Main.ExperienceIntegration.cs`; an architecture contract prevents these method
declarations from drifting back into the shared `Main.cs` hotspot. Festival
overlays now live in `Main.FestivalIntegration.cs`, while shop, processor,
shipping, commission, mail, starlight, backpack, crafting, and storage overlays
live in `Main.PlayerServicesIntegration.cs`. The shared `Main.cs` fell from
3,105 to 2,454 lines without adding a second session or save service.

`artifacts/audio/feel-01-feedback-key-audit.md` records the explicit known-key
catalog used to distinguish tool mismatch, resource/capacity blockage, and
ordinary failure. Broad substring fallbacks were removed, so unknown future
keys remain ordinary failures until deliberately classified.

## Validation boundary

- C# build: 0 warnings, 0 errors.
- Current AUDIO/FEEL profile, waveform, resolver, and mapping tests: 253/253.
- Audio preview export smoke: 1/1, producing 15 effect WAVs.
- Current BASE/UX/UI/LOOP/FEEL/AUDIO/WORLD combined focus: 431/431.
- Phase G fast gate: 51/51.
- Full C# regression: 1041/1041, 0 failed, 0 skipped.
- English and Simplified Chinese localization: 2154/2154 identical keys.
- Godot import and 180-frame headless main-scene startup: passed.
- Live final-source window launch reached Apple M3 / Metal, but macOS was locked and Computer Use could not execute the pause→child→back route. This remains an explicit GUI interaction gap and is not replaced by the deterministic captures.
- Human first-90-minute, seven-day, full-season and main-story play, physical controller, human audio listening, human full-world route recognition, and Windows/Linux runtime acceptance remain pending.
- Free `BuildingSystem` remains `deferred-explicit-reopen` and was not touched.
