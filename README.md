# Luminfield

[简体中文](README.zh-CN.md) | English

Luminfield is an original bilingual farming-life RPG vertical slice built with
Godot 4.7.1 .NET and C#. It uses no assets, characters, maps, dialogue, music,
or code from Stardew Valley or any other commercial game.

## Vertical slice

The playable loop is:

1. Talk to Mira and receive Starbud seeds.
2. Till at least three farm tiles.
3. Plant and water three Starbuds.
4. Sleep twice so watered crops can grow.
5. Harvest three mature Starbuds.
6. Return to Mira to complete the demo.

After the tutorial, the farm opens into a repeatable economy loop:

- Buy seeds for twelve original crops at the Twilight Market. The four new
  Gleamrise crops are season-limited; the original eight remain cross-season
  for save and tutorial compatibility.
- Buy Moonplum Saplings from the farm stall, plant them on clear homestead
  ground, harvest repeat Moonplums after they mature, then craft Glowcomb Hives
  that brew Starhoney when a mature fruit tree is nearby.
- Open the pause menu's Gleamrise Goals panel to follow the current season's
  fourteen-day objective route, track seasonal crop, processor, orchard, animal,
  and festival milestones, and claim atomic daily or season-closing rewards.
- Build the Starlight Coop with glow coins, Lumenwood, and Crystal Shards, then
  care for the first Starfeather Hen with Stargrain Feed and daily petting so
  she lays Starfeather Eggs that can become Glowcustard.
- Visit the Gleamrise Sowing Festival on Gleamrise day 7, complete the three
  sowing rite stages, earn Gleamrise Tokens, and exchange them for seasonal
  seeds, Starsoil Fertilizer, or a Moonplum Sapling.
- Sell Starbud, Moonroot, Cloudleaf, Glowpea, Emberbell, Prismcorn, Dewmelon,
  Duskbell, Dawnlace, Glimmerpod, Mistsong Mint, Comet Tuber, and more valuable
  artisan goods for glow coins.
- Run three fixed processing machines independently, then collect one ready
  product or claim every completed batch atomically.
- Reinvest the proceeds in more seeds to keep expanding the farm.

The original 48×32 farm now opens through its illuminated southern gate into a
192×128-cell exploration world. Seven regions are connected by walkable roads:
the home farm, Whispering Woods, Starfall Meadow, Lumen Village, Crystal Vale,
Moonwater Wetlands, and Starfall Ruins. The world is loaded in 32×32-cell
chunks; only the current 3×3 neighborhood remains active, so the camera can
travel across the full map without constructing every region at once.

The top-right minimap reveals chunks as the player enters them, keeps
undiscovered territory hidden, marks discovered landmarks, and stores the
exploration state in the regular save file.

## Lumen Village and eight core villagers

- Follow the southern homestead road east and the Crystal Road north to reach
  the first Lumen Village area. It contains eleven original exterior landmarks:
  the Moonlit Archive, Starweaver Tea House, Moonstone Workshop, Starlight
  Well, village gate, sign, lantern bench, glowflower cart, and the Twilight
  Emporium on a southeast lane, the Starlight Post on a northwest lane, plus
  Starfall Watch on a southwest lane.
- Liora, Tavi, Nemi, Sela, Elowen, Vessa, Orin, and Kael follow deterministic
  four-direction tile paths at each ten-minute clock tick. Day 7, Lanternrest,
  gives each of them a separate rest-day route. They avoid world/interior
  collision geometry, one another, the player, exterior doors, interior exits,
  and the village gate; cross-scene work shifts hand off at safe entrance cells.
- Schedules now select data-driven weather and season entries before the base
  route while keeping Lanternrest at the highest priority. Rain moves Nemi,
  Sela, and Orin into open interiors; stardust wind changes Tavi, Elowen, and
  Kael's work positions; all four seasons have a distinct villager route and
  bilingual dialogue. Restored `Weather.CurrentId` is the shared source for
  drawing, collision, preview, conversation, and character-event eligibility.
- The Moonlit Archive is open from 08:00 to 20:00. Its exterior door highlights
  as the actual target; inside, the central star-chart desk can be read. Liora
  works in the archive from 09:00 to 13:00 on weekdays.
- The Moonstone Workshop is the second enterable building and opens from 08:00
  to 19:00. Tavi works inside from 09:00 to 13:00 on ordinary days. Its
  moon-rune workbench now opens the first construction plan: spend 240 glow
  coins, 12 Lumenwood, and 4 Crystal shards atomically, then sleep twice to
  complete the first cottage upgrade.
- The Starweaver Tea House is the third enterable building and opens from 09:00
  to 21:00. Vessa works inside from 09:00 to 13:00 on ordinary days. Its
  starwoven tea counter is a read-only inspection point and does not add
  purchases, recipes, energy costs, or a new economy contract.
- The Twilight Emporium is the fourth enterable building and opens from 10:00
  to 18:00. Orin checks travel inventory inside from 10:00 to 13:00 on ordinary
  days; the shop closes for all of Lanternrest. Its manifest shelf opens a
  four-item seed inventory that rotates deterministically by week and season.
  Purchases recheck the interior, opening rule, stock, funds, and backpack
  capacity before changing coins or items. The farm stall remains independent.
- The Starlight Post is the fifth enterable building and opens from 07:00 to
  19:00. Nemi sorts village routes inside from 09:00 to 13:00 on ordinary days.
  Its route-sorting counter is read-only and adds no sending, receiving,
  delivery, fee, item, or economy contract.
- Starfall Watch is the sixth enterable building and opens from 06:00 to 20:00.
  Kael records patrol routes inside from 09:00 to 13:00 on ordinary days. Its
  seal route table is read-only and does not open ruins, issue tasks, provide
  equipment, charge fees, or grant items.
- Select the Hand, then face or stand next to a villager to talk. A first
  meeting uses an introduction, and the first conversation each day adds two
  relationship points.
- Move a non-tool item into the hotbar and select it to give one gift per
  villager per day. Loved, liked, neutral, and disliked reactions use per-NPC
  preferences plus item categories. Dialogue shows an original reaction icon,
  relationship tier, and current points.
- Player location, met villagers, relationship points, and daily talk/gift
  records use stable save IDs. Legacy saves remain compatible and unknown IDs
  are filtered.
- Liora now has the first complete two-stage friendship event chain. At 25
  relationship points, speaking to her in the Moonlit Archive reveals “The
  Faded Return Route.” At 60 points on a later day, “The Remembered Way Home”
  continues the story. Each stage has three bilingual pages and can complete
  only after its final page closes.
- Event completion uses the stable IDs `liora_faded_return_route` and
  `liora_remembered_way_home` plus completion days in the existing additive
  `schemaVersion: 1` save. Old saves receive an empty event list; unknown,
  duplicate, orphaned, and same-day out-of-order entries are filtered.
- Tavi now has the second complete event chain. At 25 relationship points,
  speaking to him with the Hand inside the Moonstone Workshop reveals “The
  Cracked Moon-Rune.” After that stage was completed on an earlier day, reaching
  60 points unlocks “The Mended Light.” The stable IDs are
  `tavi_cracked_moon_rune` and `tavi_mended_light`; both stages contain three
  bilingual pages and save their completion day only after the last page closes.
- Nemi now has the third complete event chain on her ordinary afternoon village
  route. At 25 points, “The Undeliverable Letter” begins; after it was completed
  on an earlier day, 60 points unlocks “The Route Written in the Star Chart.”
  The stable IDs are `nemi_undeliverable_letter` and `nemi_star_chart_route`.
- Kael now has the fourth complete event chain on his ordinary afternoon village
  route. At 25 points, “The Broken Blue Rune” begins; after it was completed on
  an earlier day, 60 points unlocks “The Safe Return Route.” The stable IDs are
  `kael_broken_blue_rune` and `kael_safe_return_route`.
- Sela now has the fifth complete event chain on her ordinary afternoon village
  route. At 25 points, “Tempered Starlight” begins; after it was completed on an
  earlier day, 60 points unlocks “The Shared Forge Rhythm.” The stable IDs are
  `sela_tempered_starlight` and `sela_shared_forge_rhythm`.
- Orin now has the sixth complete event chain, limited to his ordinary
  afternoon plaza schedule. At 25 points, “The Unpriced Waybill” begins; after
  it was completed on an earlier day, 60 points unlocks “The Shared Lantern
  Route.” The stable IDs are `orin_unpriced_waybill` and
  `orin_shared_lantern_route`.
- Character-event eligibility now resolves the NPC, location, relationship
  threshold, optional schedule dialogue, and prerequisite from each definition.
  All six chains normalize independently, so malformed ordering in one chain
  does not remove valid entries from another.
- The cottage construction state is additive under `schemaVersion: 1` and
  survives sleep and save/restore. Completion swaps to an original upgraded
  interior while retaining the shared bed and door routes, and exposes a
  read-only kitchen preparation area without enabling cooking.
- Phase B's seven required feature groups are complete. Additional character
  events remain optional content expansion rather than a core Phase B blocker.

## Starlight Mail and relationship rewards

- An original Starlight Mailbox now stands beside the homestead cottage. Select
  the Hand and face or stand next to it to open the mailbox. Unread mail uses a
  mint glow and full object outline; another tool shows the gold Hand
  requirement. Empty or fully read mail remains inspectable without consuming
  energy or items.
- Nemi's welcome letter arrives the morning after the player first meets her.
  Reaching Trusted Friend with Liora, Tavi, or Nemi schedules one unique reward
  letter for the following morning: 2 Crystal shards, 4 Lumenwood, or 3
  Starbud seeds respectively.
- The Starlight Post panel lists delivered letters, unread state, sender,
  delivery day, localized body, attachment, and claim result. Reading and
  claiming route through `GameSession`; a full backpack leaves the attachment
  safely in the mail.
- Delivered, read, and claimed states use stable mail IDs in an additive
  `Mail` projection under `schemaVersion: 1`. Old saves receive an empty
  mailbox, unknown IDs are filtered, and loading or sleeping cannot duplicate
  a delivered reward.

## Seven-day weather, shipping, and twelve crops

- Seven days form a week. Four stable 14-day seasons form a 56-day year:
  Gleamrise, Rainveil, Starharvest, and Longnight. The HUD shows the current
  season day, original weekday name, current weather, and next-day forecast.
- Clear, rain, and stardust wind are currently available. Rain automatically
  waters tilled soil, while rain and stardust wind have distinct world effects.
- A Star Shipping Chest now stands on the west side of the homestead. Select
  the Hand and approach it to queue crops, artisan goods, or gathered resources,
  or reclaim them before sleeping.
- Sleeping settles every queued item at its stable sell price, adds the earnings
  to the purse, and opens an itemized nightly summary with the new day and
  forecast.
- Gathered crystals regrow after two days and trees after seven days. The
  nightly summary reports newly regrown resource sites, while depletion dates
  and respawn cycles survive saving and loading.

| Crop | Watered nights | Seed price | Harvest value |
| --- | ---: | ---: | ---: |
| Starbud | 2 | ◈15 | ◈22 |
| Moonroot | 3 | ◈24 | ◈38 |
| Cloudleaf | 2 | ◈12 | ◈18 |
| Glowpea | 3 | ◈20 | ◈32 |
| Emberbell | 4 | ◈28 | ◈48 |
| Prismcorn | 5 | ◈36 | ◈68 |
| Dewmelon | 5 | ◈40 | ◈76 |
| Duskbell | 4 | ◈30 | ◈54 |
| Dawnlace (Gleamrise) | 4 | ◈26 | ◈46 |
| Glimmerpod (Gleamrise, regrows in 2 nights) | 5 | ◈42 | ◈34 |
| Mistsong Mint (Gleamrise) | 3 | ◈18 | ◈30 |
| Comet Tuber (Gleamrise) | 4 | ◈34 | ◈62 |

Rain can turn a maturing Dawnlace into Rainwoven Dawnlace, while stardust wind
can turn a maturing Glimmerpod into Starwind Glimmerpod. The result is derived
from weather, planting day, and cell, then saved so loading never rerolls it.

These systems complete all eight core phase-A gameplay increments in the
[gameplay expansion outline](docs/玩法扩展大纲.md). Crafting, the first
placeable facility, the daily commission board, and the first Starlight
Pedestal are complete. The first roads, fences, lights, and sprinklers are now
complete as well. Three crop-quality tiers and the first fertilizer are now
implemented. Phase B includes the first village, eight NPC schedules, all
six enterable buildings, a data-driven relationship and daily-gifting
entry point, relationship mail, and complete two-stage friendship event chains
for Liora, Tavi, Nemi, Kael, Sela, and Orin. Phase C has begun with twelve crops,
two resonance variants, three independent processing machines, and the first
0–5 farming skill with a permanent level-three specialization. It now also has
a fourteen-day Gleamrise Goals panel that resets each Gleamrise year, stores
stable progress counters and claimed reward IDs, and leaves unclaimed or
unfinished objectives available without permanent punishment.

## Tools and backpack

The eight-slot hotbar and the backpack are now separate:

- Slot 1 is always the Hand. Use it to harvest mature crops and inspect
  landmarks.
- The Stardust shovel tills farm soil and mines crystal clusters.
- The Mooncarved machete cuts harvestable trees.
- The Dewglass watering can carries 12 measures of water.
- The water bucket refills the watering can when aimed at a pond, stream, or
  wetland pool.
- The full backpack contains 24 slots. Press `B` or `Tab` to inspect it; the
  first eight slots are the active hotbar.

The target preview identifies the object in front of the player. Mint outlines
mean `E` can perform the shown action, gold explains which tool is required,
and rose indicates an energy, water, seed, or backpack-capacity blocker. Trees,
crystals, water, crops, NPCs, doors, stations, and the bed highlight their
actual object footprint instead of showing an unexplained ground square.

Trees yield Lumenwood and crystal clusters yield Crystal shards. Gathered
nodes temporarily disappear and become walkable; crystals return after two
days and trees after seven. Legacy permanently removed nodes begin a safe
respawn cycle on their load day, while the old `hoe` item still migrates to the
fixed tool order without dropping seeds or harvests.

## Crafting and Starwoven storage

- Press `C` to open Starweaving anywhere, or enter it from the top of the
  backpack.
- The panel now contains six recipes: Starsoil Fertilizer, Starwoven Chest,
  Moonstone Path, Starwood Fence, Starlight Torch, and Dewfall Sprinkler.
  Missing materials or a full backpack never consume any items.
- One Lumenwood plus one Crystal shard crafts two Starsoil Fertilizers.
- One Crystal shard crafts four paths; two Lumenwood craft four fences; one
  Lumenwood plus one shard craft two torches; four Lumenwood plus three shards
  craft one sprinkler.
- Paths, fences, and torches use clear homestead ground outside planting beds.
  Paths remain walkable, while fences and torches block movement. A sprinkler
  occupies an untilled planting-bed cell and waters the four orthogonal tilled
  cells before crops advance during sleep.
- A crafted chest moves into an available hotbar slot and becomes selected.
  Face a clear homestead cell and press `E` to place it. Farm plots, fixed
  facilities, obstacles, and existing chests report why placement is blocked.
- Return to slot 1, the Hand, then approach a chest and press `E` to open it.
  Every chest has 16 independent slots for seeds, produce, artisan goods,
  gathered materials, and spare chests.
- Chest positions and contents are part of the normal save. Older saves receive
  an empty storage list without losing existing progress.

## Crop quality and Starsoil Fertilizer

- All twelve crops now have Regular, Luminous, and Starlight quality tiers.
  Luminous produce is worth about 1.5× its regular value and Starlight about
  2.25×. Stable item IDs keep each tier separately stackable and sellable.
- With Starsoil selected, only an empty, tilled, unfertilized cell shows the
  mint `E · Apply Starsoil` action. Untilled, already fertilized, and planted
  cells explain the blocker without consuming fertilizer or energy.
- One application affects one crop. It guarantees at least Luminous quality,
  with a stable 20% Starlight result based on crop, cell, and planting day, so
  saving and loading cannot reroll the harvest.
- Harvest clears fertilizer for single-harvest crops. A regrowing Glimmerpod
  keeps its fertilizer and stable quality roll for the lifetime of that plant;
  the effect clears when the plant itself is removed. Tutorial progress,
  delivery commissions,
  processing, and Starlight offerings accept all three qualities as the same
  crop family and consume lower-value quality first.

## Processing machines and farming skill

- The Moonwell Infuser, Prism Preserve Vat, and Starweave Drying Loom are fixed
  farm entities with independent recipes, remaining nights, ready states, and
  save records. Cloudleaf can now become Cloudleaf Night Tea after two nights.
- Starfeather Eggs enter the Prism Preserve Vat as a two-egg artisan recipe and
  become Glowcustard after one night, using the same atomic machine state.
- The shared machine panel can focus one machine or collect every ready product
  at once. Batch collection first simulates the complete backpack result; if
  any product will not fit, neither inventory nor machine state changes.
- Legacy single-queue `ProcessorSave` data migrates to the Moonwell Infuser only
  when no valid modern machine record exists. Older legal chest and decoration
  cells remain legal and retain their contents.
- Successful tilling, planting, watering, and harvesting grant farming XP from
  one Core system. Levels run from 0 to 5. At level 3 the player permanently
  chooses Dewkeeper (watering costs one less energy, minimum one) or Resonance
  Scholar (successful harvest XP gains a 50% bonus).

## Daily and weekly commission board

- A Starlamp Commission Board stands between the cottage and greenhouse.
  Select the Hand, approach its mint outline, and press `E`; other tools explain
  that the Hand is required without consuming any resource.
- One deterministic offer rotates each day. Planting, gathering, and delivery
  templates are available now. Accepted work appears in the HUD, expires after
  sleeping if unfinished, and is replaced by the next day's offer.
- Planting records only successful target crops, gathering records only items
  actually granted to the backpack, and delivery removes items atomically only
  when the reward can be claimed.
- The day, stable definition ID, acceptance, progress, and claim state are
  saved. Older saves and unknown commission IDs safely receive the current
  day's offer without losing unrelated progress.
- The Weekly tab offers the three-stage “Starlit Route Restoration” chain:
  successfully plant 3 Starbud crops, gather 4 Lumenwood, then bring 3 Crystal
  shards. Only the active stage counts, and completed stages advance only after
  confirmation at the board.
- Weekly progress survives sleep inside the same seven-day week and refreshes
  at day 8, 15, and later week boundaries without changing the daily offer.
  Final confirmation atomically exchanges the 3 Crystal shards for 4 Moonstone
  Paths and awards 120 glow coins; insufficient items or backpack space changes
  neither inventory nor completion state.
- Week, stable commission and stage IDs, acceptance, progress, and claim state
  are additive `schemaVersion: 1` fields. Old, mismatched-week, and malformed
  saves safely receive the current week's first-stage offer.

## Woodland Watch Starlight

- The old watchlight in Whispering Woods is now a restorable Starlight
  Pedestal. Select the Hand, approach its real object outline, and press `E`;
  other tools only request the Hand and consume no energy or items.
- Its three constellation nodes accept partial offerings: any three different
  mature crops; six Lumenwood plus two Crystal shards; and one each of Starbud
  preserve, Moonroot tonic, and a Starwoven Chest.
- Every candidate item is capped by the node definition. An offer with no
  usable items removes nothing, partial progress persists, and the full
  restoration can activate only once.
- Full restoration changes the world sprite from dormant stone to flowing
  mint-and-blue starlight and permanently shortens Whispering Woods tree
  regrowth from seven days to four. Other trees and crystals are unchanged.
- Discovery, stable node IDs, per-item contributions, and the permanent reward
  remain in the existing `schemaVersion: 1` save. Older saves receive empty
  nodes, while unknown or overfilled data is normalized without unlocking the
  reward or clearing unrelated progress.

One in-game day lasts about four real minutes. Sleeping in the cottage can end
the day early.

## Controls

| Action | Keyboard | Controller |
| --- | --- | --- |
| Move | WASD / arrow keys | Left stick / D-pad |
| Use / interact | E / Space | A |
| Hotbar | 1–8 | Shoulder buttons |
| Backpack | B / Tab | Y |
| Crafting | C | X |
| Pause | Esc | Start |

## Developer playtest scenarios

Developer launch flags are registered in priority order in
[`PlaytestScenarioRegistry.cs`](src/Game/PlaytestScenarioRegistry.cs). The
registry uses exact, case-sensitive matching; when multiple known flags are
present, the first registered scenario wins. With no known playtest flag, the
game keeps the normal title-screen startup.

`Main.CreatePlaytestScenarioRegistry()` binds every scenario ID to its setup
method. Add a new ID and flag to the registry, then add its setup binding in
`Main`; focused tests lock the known flag catalog, uniqueness, priority, and
normal fallback behavior.

Use `--playtest-liora-event-one`, `--playtest-liora-event-two`,
`--playtest-tavi-event-one`, and `--playtest-tavi-event-two` to open the two
event stages for Liora or Tavi directly in the Moonlit Archive or Moonstone
Workshop. These flags preserve the existing registry priority and can be
combined with `--capture-playtest=<path>` for deterministic visual QA.

Use `--playtest-nemi-event-one` and `--playtest-nemi-event-two` to open Nemi's
two friendship stages on her ordinary 14:00 village route. Use
`--playtest-starlight-post-door`, `--playtest-starlight-post`, and
`--playtest-starlight-post-nemi` to inspect the fifth building's exterior,
read-only sorting counter, and Nemi's indoor work position. Use
`--playtest-starlight-post-wrong-tool` to verify the full counter outline uses
the warm-gold tool-mismatch state.

Use `--playtest-kael-event-one` and `--playtest-kael-event-two` to open Kael's
two friendship stages from his real ordinary-day 14:00 village projection. Use
`--playtest-starfall-watch-door`, `--playtest-starfall-watch`, and
`--playtest-starfall-watch-kael` to inspect the sixth building's exterior,
read-only seal route table, and Kael's indoor work position. Use
`--playtest-starfall-watch-wrong-tool` to verify the full table outline uses the
warm-gold tool-mismatch state.

Use `--playtest-sela-event-one` and `--playtest-sela-event-two` to open Sela's
two friendship stages from her real ordinary-day 14:00 village projection.

Use `--playtest-orin-event-one` and `--playtest-orin-event-two` to open Orin's
two friendship stages from his real ordinary-day 14:00 plaza projection.

Use `--playtest-weekly-commission-offer`,
`--playtest-weekly-commission-stage-ready`, and
`--playtest-weekly-commission-reward-ready` to inspect the Weekly tab's offer,
stage-confirmation, and atomic final-delivery states. Use
`--playtest-weekly-commission-map` to verify that weekly-only tracked work keeps
the closed board's active glow. The existing `--playtest-commission-offer`
remains the Daily-tab regression entry.

Use `--playtest-emporium-door`, `--playtest-emporium`, and
`--playtest-emporium-orin` to inspect the Twilight Emporium exterior entrance,
interior manifest shelf, and Orin interaction. They also support deterministic
capture.

Use `--playtest-emporium-rotation` to open the current week-and-season purchase
panel, and `--playtest-emporium-restday-door` to inspect Lanternrest closure.

Use `--playtest-cottage-upgrade-ready`,
`--playtest-cottage-upgrade-in-progress`, and
`--playtest-cottage-upgrade-completed` to inspect the construction offer,
two-night progress state, and upgraded cottage with its read-only kitchen area.

Use `--playtest-gleamrise-crops` to inspect the four seasonal crops, persistent
Glimmerpod regrowth, and both deterministic resonance harvests. Use
`--playtest-multi-processor` to inspect three machine states and atomic batch
collection. Use `--playtest-farming-specialization` to inspect the farming HUD
and permanent level-three specialization panel.

Use `--playtest-orchard-hives` to inspect a mature Moonplum tree, ready
Glowcomb Hive, orchard hotbar icons, object outlines, and deterministic
Starhoney collection state.

Use `--playtest-gleamrise-season` to open the pause-menu Gleamrise Goals panel
with prepared progress across the fourteen-day seasonal route.

Use `--playtest-starfeather-chickens` to start beside a built Starlight Coop
with the first Starfeather Hen, pending eggs, feed, eggs, and build materials
ready for visual QA and interaction checks.

Use `--playtest-gleamrise-festival` to inspect the day-seven festival gate,
dedicated festival scene, sowing rite panel, exchange stall, and deterministic
festival rewards.

Use `--playtest-village-rain-schedule` to open Sela's rainy-day workshop
dialogue, and `--playtest-village-rainveil-schedule` to inspect Vessa's first
Rainveil-day route and the season HUD. Both scenarios restore an explicit
current weather before resolving the NPC projection and support deterministic
capture.

Use `--playtest-npc-pathfinding` to open Lumen Village at 13:30 while multiple
villagers are between schedule anchors. Combine it with
`--capture-playtest=<path>` for deterministic movement QA.

## Local toolchain

The implementation is pinned to:

- Godot 4.7.1 .NET
- .NET SDK 10.0.302
- Project target framework `net8.0`

For this workspace the tools are installed outside the repository at
`/Users/edy/.codex/tools/luminfield/`.

On macOS, double-click
`/Users/edy/.codex/tools/luminfield/Luminfield Godot.app`, or run:

```bash
./scripts/open_editor_macos.command
```

The launcher injects the project-local `DOTNET_ROOT`. Opening the original
`Godot_mono.app` directly from Finder omits that environment and can produce a
`Failed to load .NET runtime / hostfxr` dialog.

```bash
export LUMINFIELD_TOOLS=/Users/edy/.codex/tools/luminfield
export DOTNET_ROOT="$LUMINFIELD_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

"$DOTNET_ROOT/dotnet" build
"$LUMINFIELD_TOOLS/godot/Godot_mono.app/Contents/MacOS/Godot" \
  --path /Users/edy/Desktop/personal/Luminfield --editor
```

## Validation

```bash
dotnet test tests/Luminfield.Tests/Luminfield.Tests.csproj
godot --headless --path . --editor --quit
godot --headless --path . --quit-after 180
./scripts/export_all.sh
```

The save file is written atomically to `user://saves/slot_1.json`. Corrupt saves
are preserved with a `.broken-<timestamp>` suffix.
Glow coins, the three independent processing jobs, artisan goods,
watering-can water,
world-resource depletion dates, weather, the pending shipping chest, placed
Starwoven Chests with their 16-slot contents, the daily and weekly commission
states, the Woodland Watch Starlight's partial offerings and permanent reward,
and the 24-slot backpack are stored in the existing `schemaVersion: 1` save.
Liora's, Tavi's, Nemi's, Kael's, Sela's, and Orin's character-event completion
IDs and dates use another additive projection in that same schema.
Season, season day, and year are derived from the existing absolute day; they do
not add a save field or change the `schemaVersion: 1` contract.
Fertilized farm cells, stable quality rolls, planting days, resolved resonance
produce, farming XP and specialization, Moonstone Paths, Starwood Fences,
Starlight Torches, and Dewfall Sprinklers use additive fields in that same
schema. Moonplum fruit trees and Glowcomb Hive production state also use
additive orchard fields; invalid, overlapping, or orphaned orchard entries are
filtered on load. Gleamrise goal counters and claimed objective IDs use another
additive projection; unknown goals or counters are filtered and a new Gleamrise
year receives a fresh route. The modern per-machine list safely migrates the
legacy processing queue. Older saves receive safe defaults and tool-ID
migration for the new additive fields.
Discovered world chunks are stored as stable chunk IDs; older saves begin with
the home chunk revealed.

Release outputs are created at:

- `builds/macos/Luminfield.zip` — universal ARM64 + x86_64 app, locally
  ad-hoc signed for testing.
- `builds/windows/Luminfield.exe` — Windows x86_64 plus its adjacent .NET data
  directory.
- `builds/linux/Luminfield.x86_64` — Linux x86_64 plus its adjacent .NET data
  directory.

The export script regenerates the macOS entitlement DER during ad-hoc signing,
which is required for local launch on current macOS versions. Developer ID
signing and notarization are intentionally outside this vertical slice.

Key visual acceptance captures are kept under `artifacts/screenshots/`.

## Change log

- 2026-08-20 10:59:20 CST — Added the fourteen-day Gleamrise Goals loop with
  stable progress counters, pause-menu UI, atomic rewards, next-year reset
  normalization, Star Chicken and festival milestone hooks, bilingual text,
  focused tests, and a deterministic playtest flag.
- 2026-08-20 11:20:28 CST — Added the first animal loop for Phase C: the
  Starlight Coop, Starfeather Hen feeding/petting, overnight egg production,
  egg collection, Stargrain Feed crafting, Glowcustard processing, stable save
  normalization, bilingual text, generated art, tests, and a deterministic
  playtest.
- 2026-08-20 11:26:37 CST — Added the Gleamrise Sowing Festival for Phase C:
  the day-seven festival gate, dedicated festival scene, three-stage sowing
  rite, token rewards, exchange stall, save normalization, bilingual UI,
  focused tests, and deterministic playtest entry.
- 2026-08-19 15:03:15 CST — Added Phase C orchard and hive play: Moonplum
  Saplings, mature fruit-tree harvest/regrowth, craftable Glowcomb Hives,
  nearby-tree Starhoney production, stable save normalization, bilingual
  previews/actions, original generated art, focused tests, and a deterministic
  playtest.
- 2026-08-19 10:13:02 CST — Began Phase C with four Gleamrise-only crops to
  reach twelve total plants, persistent Glimmerpod regrowth and two
  deterministic resonance harvests; added three independent fixed processing
  machines with atomic batch collection and legacy-save migration; and added
  the 0–5 farming skill with one permanent level-three specialization.
- 2026-08-13 11:28:47 CST — Completed the seven Phase B feature groups with
  Twilight Emporium opening hours, Lanternrest closure and deterministic stock
  rotation; the atomic two-night first cottage upgrade and original completed
  interior; and Orin's schedule-gated two-stage friendship chain.
- 2026-08-13 09:39:42 CST — Added the independent three-stage weekly “Starlit
  Route Restoration” commission with same-week persistence, atomic material
  reward settlement, Daily/Weekly board tabs, HUD tracking, and weekly-only
  closed-board active glow, plus Sela's independent two-stage afternoon
  friendship chain and deterministic playtests.
- 2026-08-12 17:41:50 CST — Added the sixth enterable building, Starfall Watch,
  its 06:00–20:00 door rules, read-only seal route table, Kael's ordinary
  09:00–13:00 indoor route, shared pathfinding and save restoration, original
  generated exterior/interior art, and Kael's independent two-stage afternoon
  friendship chain; Phase B's six-building interior target is complete.
- 2026-08-04 14:02:09 CST — Added the enterable Starlight Post, 07:00–19:00
  door rules, read-only route-sorting counter, Nemi's 09:00–13:00 indoor work
  route, furniture-aligned navigation and preview-state outlines, save
  restoration, original generated art, and Nemi's independent two-stage
  afternoon friendship event chain.
- 2026-08-04 12:02:43 CST — Added the four 14-day derived seasons, season HUD,
  explicit weather/season NPC schedule priorities, bilingual conditional routes
  for all eight villagers, restored-current-weather projection, deterministic
  playtests, and regression coverage without changing the save schema.
- 2026-08-04 12:10:00 CST — Replaced eight villagers' schedule-boundary
  teleports with deterministic ten-minute, four-direction tile movement,
  shared world/interior collision geometry, safe cross-scene entrance handoff,
  player/NPC reservations, fallback anchors, focused tests, and a movement
  playtest without adding save fields or scene-owned NPC state.
- 2026-08-03 13:46:36 CST — Added the enterable Twilight Emporium, 10:00–18:00
  door rules, read-only travel manifest, Orin's 10:00–13:00 indoor schedule,
  stable save location, bilingual interactions, focused playtests, and original
  generated exterior/interior without expanding the economy contract.
- 2026-08-03 13:34:26 CST — Added Tavi's two-stage friendship event, independent
  per-NPC event-chain normalization, data-driven eligibility, bilingual dialogue,
  focused playtest scenarios, tests, and macOS visual QA.
- 2026-07-31 16:34:11 CST — Added Liora's first two-stage friendship event,
  three bilingual pages per stage, final-page completion, ordered stable save
  state, focused playtest scenarios, tests, and macOS visual QA.
- 2026-07-31 16:32:37 CST — Added the enterable Starweaver Tea House, its
  09:00–21:00 door rules, read-only tea counter, Vessa's 09:00–13:00 indoor
  schedule, stable save location, bilingual interactions, and original
  generated interior.
- 2026-07-31 15:08:11 CST — Added the homestead Starlight Mailbox, first
  Starlight Post panel, Nemi's welcome letter, and three Trusted Friend reward
  letters with atomic attachment claims, compatible saves, and visual QA.
- 2026-07-31 15:00:13 CST — Added Sela, Elowen, Vessa, Orin, and Kael to reach
  eight core villagers, including ordinary-day/Lanternrest schedules,
  bilingual dialogue, gift preferences, relationships, and compatible saves.
- 2026-07-31 14:33:48 CST — Added the enterable Moonstone Workshop, its
  08:00–19:00 door rules, moon-rune workbench, Tavi's 09:00–13:00 indoor
  schedule, stable save location, bilingual interactions, and original
  generated interior.
- 2026-07-31 14:22:15 CST — Replaced the playtest launch condition chain with
  an ordered, test-covered scenario registry while preserving every existing
  flag, priority, and normal startup fallback.
