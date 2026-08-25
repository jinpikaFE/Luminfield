# NPC-B runtime QA ledger

- Date: 2026-08-25 (Asia/Shanghai)
- Repository baseline: `main@5696a6fb128e` with the current uncommitted
  STORY-01, NPC-A, and NPC-B workspace
- Runtime: Godot 4.7.1 .NET, OpenGL 4.1 Metal Compatibility, Apple M5
- Design viewport: 640×360
- Captures: 640×392, including the 32 px macOS debug-window title bar
- Locale coverage: Simplified Chinese and English

## Reproducible routes

Each route was launched from the repository root with the project-private
`DOTNET_ROOT` and the corresponding deterministic flag.

| Route | Expected observation | Evidence |
| --- | --- | --- |
| `--playtest-nemi-event-three` | Nemi 75-point event opens on her real delivery route | `artifacts/screenshots/npc-b-nemi-event-3-zh.png` |
| `--playtest-nemi-event-four` | Nemi 90-point event opens in the starlight post | `artifacts/screenshots/npc-b-nemi-event-4-zh.png` |
| `--playtest-kael-event-three` | Kael 75-point event opens in the starfall watch | `artifacts/screenshots/npc-b-kael-event-3-zh.png` |
| `--playtest-kael-event-four` | Kael 90-point event opens on his real plaza route | `artifacts/screenshots/npc-b-kael-event-4-zh.png` |
| `--playtest-sela-event-three` | Sela 75-point event opens on her real plaza route | `artifacts/screenshots/npc-b-sela-event-3-zh.png` |
| `--playtest-sela-event-four` | Sela 90-point event opens on her real workshop route | `artifacts/screenshots/npc-b-sela-event-4-zh.png` |
| `--playtest-halden-event-three` | Halden 75-point event opens on his real stocktake route | `artifacts/screenshots/npc-b-halden-event-3-zh.png` |
| `--playtest-halden-event-four` | Halden 90-point event opens on his real evening route | `artifacts/screenshots/npc-b-halden-event-4-zh.png` |
| `--playtest-npc-b-nemi-stardust-response` | Nemi's stardust-wind response opens in the post | `artifacts/screenshots/npc-b-nemi-stardust-response-zh.png` |
| `--playtest-npc-b-kael-longnight-response` | Kael's Longnight response opens in the watch | `artifacts/screenshots/npc-b-kael-longnight-response-zh.png` |
| `--playtest-npc-b-sela-starharvest-response` | Sela's Starharvest response opens in the workshop | `artifacts/screenshots/npc-b-sela-starharvest-response-zh.png` |
| `--playtest-npc-b-halden-stardust-response` | Halden's stardust-wind response opens on his real world route | `artifacts/screenshots/npc-b-halden-stardust-response-zh.png` |
| `--playtest-npc-b-group-event` | Chinese group page 1, Nemi speaking | `artifacts/screenshots/npc-b-group-page-1-zh.png` |
| `--playtest-npc-b-group-event-en` | English group page 1, Nemi speaking | `artifacts/screenshots/npc-b-group-page-1-en.png` |
| `--playtest-npc-b-group-event-page-2-en` | English group page 2, Kael speaking | `artifacts/screenshots/npc-b-group-page-2-en.png` |
| `--playtest-npc-b-group-event-page-3-en` | English group page 3, Sela speaking | `artifacts/screenshots/npc-b-group-page-3-en.png` |
| `--playtest-npc-b-group-event-page-4-en` | English group page 4, Halden speaking | `artifacts/screenshots/npc-b-group-page-4-en.png` |
| `--playtest-npc-b-group-event-page-5-en` | English group page 5, Nemi speaking | `artifacts/screenshots/npc-b-group-page-5-en.png` |
| `--playtest-npc-b-group-event-wrong-tool` | Shovel selected; the prompt requires an empty hand and no dialogue opens | `artifacts/screenshots/npc-b-group-wrong-tool-zh.png` |

## Contact sheets

- Personal events: `artifacts/screenshots/npc-b-personal-contact-sheet.png`
- Conditional responses: `artifacts/screenshots/npc-b-condition-contact-sheet.png`
- Group event and wrong-tool state: `artifacts/screenshots/npc-b-group-contact-sheet.png`

## Result

- No dialogue body, speaker name, relationship status, HUD, or continuation
  prompt was clipped in the retained frames.
- The eight personal events appeared at their real schedule projections.
- The four new conditional responses appeared in the intended weather or
  season projection without changing NPC identity.
- The four group participants were visibly distinct at the real world meeting
  area, and page speakers followed Nemi → Kael → Sela → Halden → Nemi.
- The wrong-tool route selected the shovel, displayed the empty-hand
  requirement, and did not open a dialogue.
- Dialogue-scale FPS and memory were not separately profiled; no performance
  claim is made from these captures.
