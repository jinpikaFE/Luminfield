# NPC-C runtime QA ledger

- Date: 2026-08-25 (Asia/Shanghai)
- Source: `main@5696a6fb128ee44cadcb3bd9a944d226cb86156c` plus the current uncommitted STORY-01 / NPC-A / NPC-B / NPC-C worktree
- Runtime: Godot 4.7.1 stable mono, OpenGL 4.1 Metal Compatibility, Apple M5
- Build: `/Users/lipengpeng/.codex/tools/luminfield/dotnet/dotnet build Luminfield.sln --no-restore`, 0 warnings / 0 errors
- Design viewport: 640×360; retained macOS window captures are 640×392 including the 32 px title bar
- Capture route: launch the project-private Godot binary with one deterministic `--playtest-*` flag, wait about 1.8 seconds for the first stable frame, capture the Godot debug window, then terminate the process
- Log check: `--playtest-npc-c-group-event-en` reported Godot 4.7.1 / Apple M5 and an empty stderr stream; no Godot game process remained after capture

## Personal events

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-elowen-event-three` | `artifacts/screenshots/npc-c-elowen-event-3-zh.png` | Elowen at the real well route; Chinese page fits |
| `--playtest-elowen-event-four` | `artifacts/screenshots/npc-c-elowen-event-4-zh.png` | Elowen at the real starwell plaza route; Chinese page fits |
| `--playtest-mavea-event-three` | `artifacts/screenshots/npc-c-mavea-event-3-zh.png` | Mavea in the tea-house schedule projection; Chinese page fits |
| `--playtest-mavea-event-four` | `artifacts/screenshots/npc-c-mavea-event-4-zh.png` | Mavea in the evening tea-house projection; Chinese page fits |
| `--playtest-sivren-event-three` | `artifacts/screenshots/npc-c-sivren-event-3-zh.png` | Sivren in the Moonlit Archive; Chinese page fits |
| `--playtest-sivren-event-four` | `artifacts/screenshots/npc-c-sivren-event-4-zh.png` | Sivren on the real evening world route; Chinese page fits |
| `--playtest-dorrik-event-three` | `artifacts/screenshots/npc-c-dorrik-event-3-zh.png` | Dorrik in the Moonstone Workshop; Chinese page fits |
| `--playtest-dorrik-event-four` | `artifacts/screenshots/npc-c-dorrik-event-4-zh.png` | Dorrik on the real starwell plaza route; Chinese page fits |

Contact sheet: `artifacts/screenshots/npc-c-personal-contact-sheet.png`.

## Conditional responses

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-npc-c-elowen-rainveil-response` | `artifacts/screenshots/npc-c-elowen-rainveil-response-zh.png` | Rainveil response, world projection, no clipping |
| `--playtest-npc-c-mavea-rain-response` | `artifacts/screenshots/npc-c-mavea-rain-response-zh.png` | Rain response, tea-house projection, no clipping |
| `--playtest-npc-c-sivren-starharvest-response` | `artifacts/screenshots/npc-c-sivren-starharvest-response-zh.png` | Starharvest response, archive projection, no clipping |
| `--playtest-npc-c-dorrik-rainveil-response` | `artifacts/screenshots/npc-c-dorrik-rainveil-response-zh.png` | Rainveil response, plaza projection, no clipping |

Contact sheet: `artifacts/screenshots/npc-c-condition-contact-sheet.png`.

## Four-person group event

- Chinese first page: `artifacts/screenshots/npc-c-group-page-1-zh.png`
- English pages 1–5: `artifacts/screenshots/npc-c-group-page-1-en.png` through `npc-c-group-page-5-en.png`
- Wrong-tool preview: `artifacts/screenshots/npc-c-group-wrong-tool-zh.png`
- Contact sheet: `artifacts/screenshots/npc-c-group-contact-sheet.png`

The four distinct NPC actors are visible around the eastern starwell corner at 18:00. All five English pages retain the correct Dorrik → Elowen → Mavea → Sivren → Dorrik speaker order and fit the dialogue panel. The wrong-tool route keeps the dialogue closed and shows the warm-gold switch-to-hand outline on the real Dorrik actor.

## Result

- Implemented: eight personal-event pages, four condition routes, one five-page four-person group event, and deterministic runtime entries
- Static verified: focused NPC-C/A/B/village/localization regression and Phase G fast gate
- Dynamic verified: all 19 retained routes above on current macOS hardware
- Subjective review pending: final narrative taste remains a product judgment; no visual or layout defect was observed in this pass
