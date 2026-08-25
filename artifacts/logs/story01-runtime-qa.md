# STORY-01 runtime QA ledger

- Date: 2026-08-24 (Asia/Shanghai)
- Source: `main@5696a6fb128ee44cadcb3bd9a944d226cb86156c` plus the current uncommitted STORY-01 workspace
- Runtime: Godot 4.7.1 stable mono, OpenGL 4.1 Metal Compatibility, Apple M5
- Design viewport: 640×360; retained captures are 1280×720 nearest-neighbor output
- Capture route: launch the project-private Godot binary with one deterministic `--playtest-story01-*` flag, wait for the stable dialogue frame, capture the Godot window, then terminate the process
- Locale coverage: Simplified Chinese and English

## Six-light world responses

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-story01-woodland-discovery` | `artifacts/screenshots/story01-woodland-discovery-zh.png` | Real woodland pedestal discovery; Chinese three-page dialogue fits |
| `--playtest-story01-woodland-restoration` | `artifacts/screenshots/story01-woodland-restoration-zh.png` | Restored woodland pedestal and its local response visual are visible |
| `--playtest-story01-woodland-response` | `artifacts/screenshots/story01-woodland-response-zh.png` | Next-day woodland response appears at the real world anchor |
| `--playtest-story01-homestead-response` | `artifacts/screenshots/story01-homestead-response-zh.png` | Homestead dew-light response appears at its real anchor |
| `--playtest-story01-meadow-response` | `artifacts/screenshots/story01-meadow-response-zh.png` | Meadow pollen response appears at its real anchor |
| `--playtest-story01-moonwater-response` | `artifacts/screenshots/story01-moonwater-response-zh.png` | Moonwater ripple and stele response appear at the real anchor |
| `--playtest-story01-crystal-vale-response` | `artifacts/screenshots/story01-crystal-vale-response-zh.png` | Crystal-vale ruin-door response appears at the real anchor |
| `--playtest-story01-starfall-ruins-response` | `artifacts/screenshots/story01-starfall-ruins-response-zh.png` | Six-light gate-base response appears at the real anchor |

## Liora revisits and journey recap

| Route | Evidence | Observation |
| --- | --- | --- |
| `--playtest-story01-woodland-revisit-en` | `artifacts/screenshots/story01-woodland-revisit-en.png` | Liora revisit opens through her real Moonlit Archive projection; English page fits |
| `--playtest-story01-final-revisit-en` | `artifacts/screenshots/story01-final-revisit-en.png` | Final revisit page 1 lists the restored lights from current state |
| `--playtest-story01-final-revisit-page-2-en` | `artifacts/screenshots/story01-final-revisit-page-2-en.png` | Page 2 lists current relationships and companions without placeholders |
| `--playtest-story01-final-revisit-page-3-en` | `artifacts/screenshots/story01-final-revisit-page-3-en.png` | Page 3 reports current explored chunks and regions without clipping |
| Final-revisit scenario in Simplified Chinese | `artifacts/screenshots/story01-journey-recap-zh.png` | Chinese journey recap renders the same live light, relationship, and exploration projection |

## Result

- Implemented: six independent four-beat light chains, six real world-anchor responses, six Liora revisits, and a live journey recap.
- Static verified: the final Phase G fast gate built with 0 warnings / 0 errors and passed 290/290 tests; the unchanged implementation state had already passed 848/848 full tests.
- Dynamic verified: all thirteen retained frames above were rendered from the current STORY-01 implementation on macOS; no dialogue, HUD, placeholder, or response visual defect was observed.
- Boundary verified: the story layer reads existing starlight, village relationship, exploration, character-event, gate, fishing, construction, and finale state without creating replacement state for those systems.
- Performance boundary: dialogue-scale FPS and memory were not separately profiled; no performance claim is made from these captures.
