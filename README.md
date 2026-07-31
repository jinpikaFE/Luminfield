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

- Buy seeds for eight original crops at the Twilight Market.
- Sell Starbud, Moonroot, Cloudleaf, Glowpea, Emberbell, Prismcorn, Dewmelon,
  Duskbell, and more valuable artisan goods for glow coins.
- Load two crops into the Moonwell Infuser, sleep one night, then collect
  Starbud preserve or Moonroot tonic.
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
  the first Lumen Village area. It contains eight original exterior landmarks:
  the Moonlit Archive, Starweaver Tea House, Moonstone Workshop, Starlight
  Well, village gate, sign, lantern bench, and glowflower cart.
- Liora, Tavi, Nemi, Sela, Elowen, Vessa, Orin, and Kael change position with
  the game clock. Day 7, Lanternrest, gives each of them a separate rest-day
  route. Their schedule cells remain distinct, walkable, and clear of the
  village gate and archive entrance.
- The Moonlit Archive is open from 08:00 to 20:00. Its exterior door highlights
  as the actual target; inside, the central star-chart desk can be read. Liora
  works in the archive from 09:00 to 13:00 on weekdays.
- The Moonstone Workshop is the second enterable building and opens from 08:00
  to 19:00. Tavi works inside from 09:00 to 13:00 on ordinary days. Its
  moon-rune workbench explains the currently available inspection and upkeep
  services and marks tool upgrades as future work without changing the economy.
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
- Character events, tile-level walking paths, weather/season schedule
  conditions, and four more building interiors remain planned work.

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

## Seven-day weather, shipping, and eight crops

- Seven days form a week. The HUD shows the day, an original weekday name,
  current weather, and the next-day forecast.
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

These systems complete all eight core phase-A gameplay increments in the
[gameplay expansion outline](docs/玩法扩展大纲.md). Crafting, the first
placeable facility, the daily commission board, and the first Starlight
Pedestal are complete. The first roads, fences, lights, and sprinklers are now
complete as well. Three crop-quality tiers and the first fertilizer are now
implemented. Phase B now includes the first village, eight NPC schedules, the
first two enterable buildings, and a data-driven relationship and daily-gifting
entry point.

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

- All eight crops now have Regular, Luminous, and Starlight quality tiers.
  Luminous produce is worth about 1.5× its regular value and Starlight about
  2.25×. Stable item IDs keep each tier separately stackable and sellable.
- With Starsoil selected, only an empty, tilled, unfertilized cell shows the
  mint `E · Apply Starsoil` action. Untilled, already fertilized, and planted
  cells explain the blocker without consuming fertilizer or energy.
- One application affects one crop. It guarantees at least Luminous quality,
  with a stable 20% Starlight result based on crop, cell, and planting day, so
  saving and loading cannot reroll the harvest.
- Harvest clears the fertilizer. Tutorial progress, delivery commissions,
  processing, and Starlight offerings accept all three qualities as the same
  crop family and consume lower-value quality first.

## Daily commission board

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
Glow coins, the active processing job, artisan goods, watering-can water,
world-resource depletion dates, weather, the pending shipping chest, placed
Starwoven Chests with their 16-slot contents, the daily commission state, the
Woodland Watch Starlight's partial offerings and permanent reward, and the
24-slot backpack are stored in the existing `schemaVersion: 1` save.
Fertilized farm cells, stable quality rolls, Moonstone Paths, Starwood Fences,
Starlight Torches, and Dewfall Sprinklers use additive fields in that same
schema. Older saves receive safe defaults and tool-ID migration for the new
additive fields.
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
