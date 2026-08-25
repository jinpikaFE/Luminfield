# NPC-A runtime QA ledger

- Date: 2026-08-25 (Asia/Shanghai)
- Repository baseline: `main@5696a6fb128e` with the current uncommitted
  STORY-01 and NPC-A workspace
- Runtime: Godot 4.7.1 .NET, OpenGL 4.1 Metal Compatibility, Apple M5
- Design viewport: 640×360
- Captures: 640×392, including the 32 px macOS debug-window title bar
- Locale coverage: Simplified Chinese and English

## Reproducible routes

Each route was launched from the repository root with the project-private
`DOTNET_ROOT` and the corresponding deterministic flag.

| Route | Expected observation | Evidence |
| --- | --- | --- |
| `--playtest-liora-event-three` | Liora 75-point event opens in the archive | `artifacts/screenshots/npc-a-liora-event-3-zh.png` |
| `--playtest-liora-event-four` | Liora 90-point event opens in the archive | `artifacts/screenshots/npc-a-liora-event-4-zh.png` |
| `--playtest-tavi-event-three` | Tavi 75-point event opens in the workshop | `artifacts/screenshots/npc-a-tavi-event-3-zh.png` |
| `--playtest-tavi-event-four` | Tavi 90-point event opens at his real route | `artifacts/screenshots/npc-a-tavi-event-4-zh.png` |
| `--playtest-vessa-event-three` | Vessa 75-point event opens in the world | `artifacts/screenshots/npc-a-vessa-event-3-zh.png` |
| `--playtest-vessa-event-four` | Vessa 90-point event opens in the tea house | `artifacts/screenshots/npc-a-vessa-event-4-zh.png` |
| `--playtest-orin-event-three` | Orin 75-point event opens in the emporium | `artifacts/screenshots/npc-a-orin-event-3-zh.png` |
| `--playtest-orin-event-four` | Orin 90-point event opens in the emporium | `artifacts/screenshots/npc-a-orin-event-4-zh.png` |
| `--playtest-npc-a-liora-rain-response` | Rain response opens in the archive | `artifacts/screenshots/npc-a-liora-rain-response-zh.png` |
| `--playtest-npc-a-tavi-longnight-response` | Longnight response opens in the workshop | `artifacts/screenshots/npc-a-tavi-longnight-response-zh.png` |
| `--playtest-npc-a-vessa-stardust-response` | Stardust-wind response opens in the tea house | `artifacts/screenshots/npc-a-vessa-stardust-response-zh.png` |
| `--playtest-npc-a-orin-longnight-snow-response` | Longnight-snow response opens in the emporium | `artifacts/screenshots/npc-a-orin-longnight-snow-response-zh.png` |
| `--playtest-npc-a-group-event` | Chinese group page 1, Liora speaking | `artifacts/screenshots/npc-a-group-page-1-zh.png` |
| `--playtest-npc-a-group-event-en` | English group page 1, Liora speaking | `artifacts/screenshots/npc-a-group-page-1-en.png` |
| English page advance 2 | Tavi speaking | `artifacts/screenshots/npc-a-group-page-2-en.png` |
| English page advance 3 | Vessa speaking | `artifacts/screenshots/npc-a-group-page-3-en.png` |
| English page advance 4 | Orin speaking | `artifacts/screenshots/npc-a-group-page-4-en.png` |
| English page advance 5 | Liora speaking | `artifacts/screenshots/npc-a-group-page-5-en.png` |
| `--playtest-npc-a-group-event-wrong-tool` | Shovel selected; target prompt says hand is required and no dialogue opens | `artifacts/screenshots/npc-a-group-wrong-tool-zh.png` |

## Result

- No dialogue body, speaker name, relationship status, HUD, or continuation
  prompt was clipped in the retained frames.
- Group page speakers changed in the required order:
  Liora → Tavi → Vessa → Orin → Liora.
- The four participants were visibly distinct around the real world plaza.
- Wrong-tool state did not open or complete the group event.
- Dialogue-scale FPS and memory were not separately profiled; no performance
  claim is made from these captures.
