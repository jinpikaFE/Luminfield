# NPC-D runtime QA ledger

- Date: 2026-08-25 (Asia/Shanghai)
- Source: `main@5696a6fb128ee44cadcb3bd9a944d226cb86156c` plus the current uncommitted STORY-01 / NPC-A / NPC-B / NPC-C / NPC-D workspace
- Runtime: Godot 4.7.1 stable mono, OpenGL 4.1 Metal Compatibility, Apple M5
- Build: `/Users/lipengpeng/.codex/tools/luminfield/dotnet/dotnet build Luminfield.sln --no-restore`, 0 warnings / 0 errors
- Design viewport: 640×360; retained macOS window captures are 640×392 including the 32 px title bar
- Capture route: launch the project-private Godot binary with one deterministic `--playtest-*` flag, wait about 2.2 seconds for the first stable frame, capture the Godot debug window, then terminate the process
- Log check: the retained `--playtest-npc-d-group-event-page-2-en` rerun reported Godot 4.7.1 / Apple M5 and an empty stderr stream; no Godot game process remained after capture

## Personal events

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-yvara-event-three` | `artifacts/screenshots/npc-d-yvara-event-3-zh.png` | Yvara's 75-point event opens in the real Twilight Emporium projection; Chinese page fits |
| `--playtest-yvara-event-four` | `artifacts/screenshots/npc-d-yvara-event-4-zh.png` | Yvara's 90-point event opens on the real plaza route; Chinese page fits |
| `--playtest-brial-event-three` | `artifacts/screenshots/npc-d-brial-event-3-zh.png` | Brial's 75-point event opens in the existing tea-house NPC projection; Chinese page fits |
| `--playtest-brial-event-four` | `artifacts/screenshots/npc-d-brial-event-4-zh.png` | Brial's 90-point event opens on the real plaza route; Chinese page fits |
| `--playtest-pavri-event-three` | `artifacts/screenshots/npc-d-pavri-event-3-zh.png` | Pavri's 75-point event opens on the real plaza route; Chinese page fits |
| `--playtest-pavri-event-four` | `artifacts/screenshots/npc-d-pavri-event-4-zh.png` | Pavri's 90-point event opens in the Moonstone Workshop projection; Chinese page fits |
| `--playtest-roven-event-three` | `artifacts/screenshots/npc-d-roven-event-3-zh.png` | Roven's 75-point event opens on the real plaza route; Chinese page fits |
| `--playtest-roven-event-four` | `artifacts/screenshots/npc-d-roven-event-4-zh.png` | Roven's 90-point event opens in the existing starlight-post NPC projection; Chinese page fits |

Contact sheet: `artifacts/screenshots/npc-d-personal-contact-sheet.png`.

## Conditional responses

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-npc-d-yvara-rain-response` | `artifacts/screenshots/npc-d-yvara-rain-response-zh.png` | Light-rain response at 11:00 in the emporium projection; no clipping |
| `--playtest-npc-d-brial-longnight-response` | `artifacts/screenshots/npc-d-brial-longnight-response-zh.png` | Longnight response at 10:00 in the tea-house projection; no clipping |
| `--playtest-npc-d-pavri-rainveil-response` | `artifacts/screenshots/npc-d-pavri-rainveil-response-zh.png` | Rainveil response at 10:00 in the workshop projection; no clipping |
| `--playtest-npc-d-roven-rain-response` | `artifacts/screenshots/npc-d-roven-rain-response-zh.png` | Light-rain response at 14:00 on the real world route; no clipping |

Contact sheet: `artifacts/screenshots/npc-d-condition-contact-sheet.png`.

## Four-person group event

- Chinese first page: `artifacts/screenshots/npc-d-group-page-1-zh.png`
- English pages 1–5: `artifacts/screenshots/npc-d-group-page-1-en.png` through `npc-d-group-page-5-en.png`
- Wrong-tool preview: `artifacts/screenshots/npc-d-group-wrong-tool-zh.png`
- Contact sheet: `artifacts/screenshots/npc-d-group-contact-sheet.png`

The four distinct NPC actors are visible around the real east-plaza bench at 08:00. All five English pages retain the correct Roven → Yvara → Brial → Pavri → Roven speaker order and fit the dialogue panel. The wrong-tool route keeps the dialogue closed and shows the warm-gold switch-to-hand outline on the real Roven actor.

## Result

- Implemented: eight personal-event pages, four condition routes, one five-page four-person group event, and nineteen deterministic runtime entries
- Static verified: NPC-D and cross-NPC village/relationship/save/localization regression plus the Phase G fast gate
- Dynamic verified: all nineteen retained routes above on current macOS hardware
- Boundary verified: tea house, starlight post, and starfall watch are used only as existing NPC schedule projections; no LOC-TEA, LOC-POST, or LOC-WATCH gameplay state was added
- Subjective review pending: final narrative taste remains a product judgment; no visual or layout defect was observed in this pass
- Performance boundary: dialogue-scale FPS and memory were not separately profiled; no performance claim is made from these captures
