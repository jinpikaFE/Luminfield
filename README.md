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

- Buy seeds for twenty original crops at the Twilight Market. Gleamrise,
  Rainveil, and Starharvest each have four seasonal crops; the original eight
  remain cross-season for save and tutorial compatibility.
- Buy Moonplum Saplings from the farm stall, plant them on clear homestead
  ground, harvest repeat Moonplums after they mature, then craft Glowcomb Hives
  that brew Starhoney when a mature fruit tree is nearby.
- Sell Starbud, Moonroot, Cloudleaf, Glowpea, Emberbell, Prismcorn, Dewmelon,
  Duskbell, Dawnlace, Glimmerpod, Mistsong Mint, Comet Tuber, and more valuable
  artisan goods for glow coins.
- Run three fixed processing machines independently, then collect one ready
  product or claim every completed batch atomically.
- Reinvest the proceeds in more seeds to keep expanding the farm.

## Phase G release candidate

The 1.0-polish milestone now closes the main story at the activated Sixfold
Star Gate once Farming, Gathering, Crystal Mining, Fishing, and Nightwatch all
reach level 5. The bilingual ending preserves the world and opens five-rank
postgame Stellar Resonance bonuses instead of ending the save.

Gathering is now a complete fifth skill with stable XP, two level-three
specializations, save support, and shared preview/action yields. The frozen
`release_balance_v1` audit covers sell margins, base drops, resource respawns,
skill caps, daily/weekly task cadence, festival rewards, and construction cost
bands.

Settings now persist separately from the world save and include fishing assist,
incoming damage and enemy counter speed, motion intensity, high-contrast and
red-green target palettes, 100/110/120% text, dialogue pace/auto-advance,
auto-run, target lock, hold-to-repeat tools, and keyboard remapping. Every
full-screen panel receives a controller focus fallback and the standard D-pad /
A / B navigation map.

The original 48×32 farm remains the cultivation core of a new 64×64 beginner
district, which now opens through its illuminated eastern arch into a 256×192-
cell exploration world. Central Lumen City occupies a 128×96 hub and connects
the homestead, Whispering Woods, Starfall Meadow, Crystal Vale, Moonwater
Wetlands, and Starfall Ruins through a wider civic road network. Public
services, the civic plaza, commerce, and late-game facilities each have a
separate district instead of competing for the same small center. The world is
loaded as an 8×6 grid of 32×32-cell chunks; only the current 3×3 neighborhood
remains active, so the camera can travel across the full map without
constructing every region at once. This one-time topology rebuild establishes a
new world-coordinate baseline and does not migrate old map positions.

The exploration world now follows the homestead's full-backdrop composition.
Four seasonal global masters, four 64×64 beginner-district masters, four
128×96 city masters, and eleven high-detail 64×64 sector masters are composed
offline into four continuous 4096×3072 seasonal backdrops. Runtime swaps one
complete backdrop instead of drawing tiled ground and isolated scenic stickers;
procedural trees, crystals, and forage remain only at interaction density. Core
still owns roads, water, collision, resources, NPCs, facilities, and save state.
The city layer is now assembled from four overlapping high-detail quadrants and
one dedicated civic-plaza refinement at its native 2048×1536 runtime size,
instead of upscaling a 1448×1086 single image.

The top-right minimap reveals chunks as the player enters them, keeps
undiscovered territory hidden, marks discovered landmarks, and stores the
exploration state in the regular save file.

Long-term content now adds regional event packs without changing the map or
reward economy. The woods/meadow, wetlands/crystal, and village/ruins packs
listen to the player's real region visits: environment echoes can repeat,
relationship narrative echoes are one-time scenes, and rare postgame echoes
unlock only after the main story. These events do not grant ordinary items,
coins, or festival currency; only rare postgame echoes award Stellar Resonance
XP.

The pause menu now includes a Festival Memories panel for reviewing each
festival's yearly result, the classic/seasonal/craft replay rule set, year-two
bonus scoring, and the annual memorial reward choice. Reward claims still go
through the same backpack-capacity-safe inventory path used by the live
festival stalls.

After the main story, the Stellar Resonance panel also tracks four postgame
objectives: annual festivals, rare regional echoes, postgame relationship
revisits, and Codex discoveries. Objective milestones record resonance XP
without resetting the save or duplicating the existing collection, relationship,
festival, or regional systems.

## Central Lumen City and sixteen villagers

- The central city now occupies a 128×96-cell district. The Moonlit Archive,
  Moonstone Workshop, and Starlight Post form the western services quarter;
  the civic pavilion anchors the center; the Starweaver Tea House, Twilight
  Emporium, and Starfall Watch form the eastern commerce quarter. The
  construction workbench, greenhouse, Starfeather Coop, Moonfleece Barn,
  city Starlight, and Sixfold Star Gate now have a dedicated southern facility
  band; all eleven existing exterior landmarks have been migrated to the new
  layout.
- All sixteen current villagers follow deterministic four-direction tile paths
  at each ten-minute clock tick. Day 7, Lanternrest, gives each of them a
  separate rest-day route. They avoid world/interior
  collision geometry, one another, the player, exterior doors, interior exits,
  and the village gate; cross-scene work shifts hand off at safe entrance cells.
  Every real one-cell step now interpolates over 0.48 seconds and shares the
  player's two-step sway, bob, horizontal lean, and animated shadow across the
  world, all six interiors, and all four festival scenes.
- Schedules now select data-driven weather and season entries before the base
  route while keeping Lanternrest at the highest priority. Rain moves Nemi,
  Sela, and Orin into open interiors; stardust wind changes Tavi, Elowen, and
  Kael's work positions; all four seasons have a distinct villager route and
  bilingual dialogue. Restored `Weather.CurrentId` is the shared source for
  drawing, collision, preview, conversation, and character-event eligibility.
- The Moonlit Archive is open from 08:00 to 20:00. Its exterior door highlights
  as the actual target; inside, the full research desk opens the Crop, Cooking,
  and Artisan Codices. Undiscovered entries remain silhouettes, while discovered
  crops show existing seed, growth, and harvest art, discovered dishes show
  meal art, ingredients, energy, and sale value, and discovered artisan goods
  show their real input, facility, processing time, and current sale value.
  Liora works in the archive from 09:00 to 13:00 on weekdays.
- The Moonstone Workshop is the second enterable building and opens from 08:00
  to 19:00. Tavi works inside from 09:00 to 13:00 on ordinary days. Its
  moon-rune workbench now opens the first construction plan: spend 240 glow
  coins, 12 Lumenwood, and 4 Crystal shards atomically, then sleep twice to
  complete the first cottage upgrade.
- The Starweaver Tea House is the third enterable building and opens from 09:00
  to 21:00. Vessa works inside from 09:00 to 13:00 on ordinary days. Its
  starwoven tea counter deterministically rotates Cloudleaf Focus Tea, Moonroot
  Calm Draught, and Starbud Sharing Tea. With the hand selected, one daily
  tasting atomically spends glow coins, adds the takeaway item to the backpack,
  restores 10–18 energy, and raises outdoor movement speed by 8% for 120 game
  minutes. From 13:00 to 18:00, at least two actually scheduled villagers can
  join one afternoon gathering per day for 3 relationship points each. The
  purchase, timed effect, gathering, and guest list persist within that day.
- The Twilight Emporium is the fourth enterable building and opens from 10:00
  to 18:00. Orin checks travel inventory inside from 10:00 to 13:00 on ordinary
  days; the shop closes for all of Lanternrest. Its manifest shelf opens a
  four-item seed inventory that rotates deterministically by week and season.
  Purchases recheck the interior, opening rule, stock, funds, and backpack
  capacity before changing coins or items. The farm stall remains independent.
- The Starlight Post is the fifth enterable building and opens from 07:00 to
  19:00. Nemi sorts village routes inside from 09:00 to 13:00 on ordinary days.
  Its sorting counter deterministically offers two limited routes per day, with
  one active route at a time and at most two completions. Recipients come from
  the 16 villagers' real schedule projection; adjacent delivery with the Hand
  atomically grants glow coins and 2 relationship points. Same-day progress is
  restorable, stale routes expire across days, and delivery state remains fully
  independent from Starlight Mail read and attachment state.
- Starfall Watch is the sixth enterable building and opens from 06:00 to 20:00.
  Kael records patrol routes inside from 09:00 to 13:00 on ordinary days. Its
  seal route table deterministically offers two patrols, one bounty, and one of
  three ruins preparations each day. Patrols require a real visit to the target
  region before returning for a reward, while bounties count only matching
  enemies actually defeated in the Starfall Ruins or Deep Mine; defeat fails an
  unfinished bounty. Patrol and bounty rewards atomically grant items, glow
  coins, and Kael relationship points, with no changes when capacity is
  insufficient. Preparations last only for the current day, and same-day state
  restores from saves before resetting across days.
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
- All sixteen current villagers now have a complete 25/60 two-stage friendship
  chain with three pages per stage. Elowen's “Tide Marks at the Well” and “A
  Waterline Read Together” use her real well/plaza schedules; Vessa's “Bitter
  Leaf, Warm Cup” and “The Path That Listens Back” use her real teahouse/world
  schedules. Every second stage requires its prerequisite on an earlier day,
  and an active event cannot be overwritten by another event.
- Yvara, Brial, Pavri, and Roven bring the village to sixteen. Each adds a
  stable ID, full-day/rest-day/season-or-weather route, explicit anchors and
  dialogue in all three implemented festivals, gift preferences, two more
  25/60 three-page events, and a one-time next-day reward letter. Their original
  four-direction sprites use a separate 4×4 atlas resolved strictly by NPC ID.
- Character-event eligibility now resolves the NPC, location, relationship
  threshold, optional schedule dialogue, and prerequisite from each definition.
  All sixteen chains normalize independently, so malformed ordering in one chain
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
- Halden, Mavea, Sivren, Dorrik, Kael, Sela, Elowen, Vessa, and Orin receive a
  one-time reward letter on the day after their second relationship event.
  All 16/16 current villagers therefore have a claimable relationship reward;
  insufficient backpack capacity leaves the attachment intact.
- The Starlight Post panel lists delivered letters, unread state, sender,
  delivery day, localized body, attachment, and claim result. Reading and
  claiming route through `GameSession`; a full backpack leaves the attachment
  safely in the mail.
- Delivered, read, and claimed states use stable mail IDs in an additive
  `Mail` projection under `schemaVersion: 1`. Old saves receive an empty
  mailbox, unknown IDs are filtered, and loading or sleeping cannot duplicate
  a delivered reward.

## Seven-day weather, shipping, and twenty crops

- Seven days form a week. Four stable 14-day seasons form a 56-day year:
  Gleamrise, Rainveil, Starharvest, and Longnight. The HUD shows the current
  season day, original weekday name, current weather, and next-day forecast.
- Clear, rain, stardust wind, and Longnight Snow are available. Rain
  automatically waters tilled soil. Longnight days 1, 5, 8, and 12 naturally
  snow, reducing outdoor movement by 15% without watering crops; cottages,
  the greenhouse, and village interiors keep normal speed and draw no snow.
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
| Ripplecap (Rainveil) | 2 | ◈16 | ◈30 |
| Tideglass Taro (Rainveil) | 4 | ◈38 | ◈72 |
| Lantern Reed (Rainveil, regrows in 2 nights) | 4 | ◈46 | ◈40 |
| Rainveil Lotus (Rainveil) | 5 | ◈52 | ◈105 |
| Auric Shoot (Starharvest) | 3 | ◈30 | ◈52 |
| Sunvault Gourd (Starharvest) | 4 | ◈46 | ◈86 |
| Crownstar Saffron (Starharvest) | 6 | ◈78 | ◈154 |
| Amberthread Cluster (Starharvest, regrows in 3 nights) | 5 | ◈64 | ◈52 |

Rain can turn a maturing Dawnlace into Rainwoven Dawnlace, while stardust wind
can turn a maturing Glimmerpod into Starwind Glimmerpod. The result is derived
from weather, planting day, and cell, then saved so loading never rerolls it.

These systems complete all eight core phase-A gameplay increments in the
[gameplay expansion outline](docs/玩法扩展大纲.md). Crafting, the first
placeable facility, the daily commission board, and the first three Starlight
Pedestals are complete. The first roads, fences, lights, and sprinklers are now
complete as well. Three crop-quality tiers and the first fertilizer are now
implemented. Phase B's village foundation now projects sixteen NPC schedules
across all six enterable buildings, with a data-driven relationship and
daily-gifting entry point, relationship mail, and complete two-stage friendship
event chains plus relationship rewards for all 16/16 villagers. Phase C delivered twelve crops,
two resonance variants, three independent processing machines, the first
0–5 farming skill with a permanent level-three specialization, Moonplum trees,
and Glowcomb Hives. Phase F now includes the four-crop Rainveil farming slice,
a date-derived Rainveil homestead skin, the four-crop Starharvest farming
slice, a date-derived Starharvest homestead skin, Rainveil, Starharvest, and
Longnight visual aspects across all six non-homestead world regions, the stable-ID Homestead
Workshop and Moondew Greenhouse, Longnight's frostbound-planting slice, and the
stable `longnight_snow` weather slice, plus the fully restorable Homestead
Harvest and Meadow Harmony Starlights and all four complete main festivals:
Starharvest Market, the Gleamrise Planting Festival, the Longnight Lantern
Feast, and Firefly Tide, plus complete relationship slices through the four-NPC
Yvara/Brial/Pavri/Roven expansion that reaches the planned 16-NPC minimum.
The twenty-third Phase-F slice adds the first complete compendium category:
all 20 crops are discovered only when produce actually enters player ownership,
persist through the additive schema-v1 Collection root, and can be reviewed at
the Moonlit Archive. Completing 20/20 unlocks a one-time Moonlit Almanac reward
that gives ordinary crop seeds a shared, rounded-up 10% coin-price discount in
both seed shops. The twenty-fourth slice generalizes the same collection and
archive UI across categories, completes the four-dish Cooking Codex, and grants
the Moonhearth Recipe Journal's shared +5 energy benefit. The twenty-fifth
slice completes the four-entry Artisan Codex for the existing preserves,
tonic, tea, and Starhoney. Its one-time Starlit Appraisal Ledger reward raises
direct-sale and shipping prices for those frozen entries by 10%, rounded up,
and stores historical settlement unit prices. The twenty-sixth slice adds eight
real seasonal forage items across the woods and meadow, Stardust Wind bonus
spawns, the 8/8 Forage Codex, and a Starpath Forager's Guide that marks only
today's uncollected nodes in explored minimap chunks. With the Fish, Mineral,
Artifact, and Enemy Codices below, the compendium is now 8/8 categories.
Meadow Harmony reads a
completed festival result without consuming festival currency; its other nodes
accept distinct flower and homestead-product families. Restoration permanently
extends Glowcomb Hive pollination range from four tiles to six.
The merged snapshot also adds a data-driven fourteen-day Gleamrise Goals panel.
Stable counters follow real seed purchases, planting, watering, harvesting,
fertilizing, processor work, orchard/hive output, animal care, and participation
in the existing day-4 Planting Festival. Claims are atomic, reset for each new
Gleamrise year, and persist claimed IDs without permanently punishing missed or
unfinished goals. This supplements the existing Phase-F systems; it does not
restore the older, superseded day-7 festival or single-chicken implementation.
The optional day-11 market has its own scene, all sixteen villagers, a three-item
showcase and auction, persistent Market Scrip, and a four-offer festival stall.
The optional Gleamrise day-4 festival adds its own sixteen-villager scene, a
three-family 12-plot temporary-seed challenge, annual score and award results,
persistent Bloom Tokens, and four atomic seed exchanges without consuming farm
inventory, energy, water, or skill progress.
The optional Longnight day-13 feast opens from 17:00 to 22:00 in an independent
sixteen-villager scene. Its Shared-Radiance Rite atomically consumes exactly two
different cooked dishes and one selected homestead gift, adds the complete
return gift, records the annual score/award/gift/rite result, and grants an
independent Lantern Knot balance. A four-offer stall spends only Lantern Knots;
the rite never mutates any regional Starlight.
Phase D now includes 24 stable fish, adjacent-water catches conditioned by
region, season, weather, and time, a line-control minigame, three rod tiers,
bait, bobbers, crab pots, levels 0–5 with two specializations, a 24/24 Fish
Codex, and the fourth Moonwater Resonance Starlight. Firefly Tide opens on
Rainveil day 12
from 18:00 to 23:00 in an independent wetland scene with all sixteen villagers,
four real facilities, and a Glowmark stall. Its activity atomically consumes
exactly three different Moonwater fish only on final launch, records the annual
score and award, and grants persistent Glowmarks; previews, wrong tools,
missing fish, repeat entries, and capacity failures change nothing. Main
festivals are now 4/4.
The Moonlit Archive now also exposes a 24-entry fish donation ledger from the
Fish Codex. With the Hand selected, a discovered fish currently held in the
backpack can be donated exactly once; successful donation removes one fish and
persists its stable ID. Undiscovered, missing, repeated, wrong-tool, and
outside-Archive attempts leave inventory and donation state unchanged.
Phase E retains the fixed five-room `crystal_grotto_survey`, four stable
minerals, and the fifth Crystal Vale Attunement Starlight, then continues from
its depth anchor into a deterministic twelve-room deep mine with four stable
anchors, six enemy families, three weapons, combat/dodge/drop/defeat recovery,
four shovel tiers, and independent level 0–5 Crystal Mining and Nightwatch
specializations. The survey also provides the 4/4 `codex_minerals` category and
a two-night Bronze-Star shovel upgrade. The basic shovel reaches the first two
ore families; an atomic 420-coin, 6 Lumenslate, and 3 Moonvein upgrade breaks the
seal and unlocks Prismheart and Stariron. Reaching room five records a stable
survey anchor. The Crystal Vale Starlight combines four-mineral contributions,
the shovel milestone, and that depth anchor, then opens the later Starfall Ruins
trial route. Phase E's generated-mine, combat, weapon, tool, and skill contracts
are therefore complete while keeping the fixed survey as its authored entrance.
The twenty-ninth slice adds the fixed three-room `starfall_ruins_trial`, six
enemy instances across three stable enemy families, the Moonsteel Shortblade,
real-time attacks and dodge invulnerability, trial health, and next-day defeat
recovery. Clearing rooms persists while partial-room damage safely resets.
Four unique artifacts can be recovered only after their rooms are cleared and
then donated atomically to the Moonlit Archive. First real recovery and first
real defeat complete the 4/4 Artifact Codex and 6/6 Enemy Codex. Their four
collection milestones restore the sixth Starfall Ruins Remembrance Starlight,
bringing Starlights to 6/6 and the compendium to 8/8 without automatically
building or activating a Star Gate. The expanded Enemy Codex now contains all
six stable enemy families.
The eleventh slice adds the Starfeather Coop and its first chicken: three-night
construction, juvenile-to-adult growth, clear-day grazing, rainy/Longnight
sheltering, daily feeding and petting, visible mood feedback, and atomic
collection/shipping of three egg qualities. Existing saves receive an additive
animal root while remaining on schema v1. The twelfth slice reuses that same
`AnimalSystem` for the waterside Moonfleece Barn and its first sheep: four-night
construction, juvenile growth, weather-aware grazing/feed, three fleece
qualities, and capacity-safe rack collection. The thirteenth slice adds the
third animal, Dewhorn, to that same barn: four fed or grazed nights to adulthood,
two-night Condensed Dewmilk production, three qualities, and a separate
capacity-safe milking station. Animal species are now 3/3 without changing the
schema-v1 save root. The fourteenth slice adds the Starwoven Husbandry Hub, a
sixth construction project that activates a real console in each animal building.
Each building independently stores 28 fodder and 12 stable-ID products; nightly
auto-feeding and collection are atomic, while petting, retrieval, and shipping
remain deliberate player actions.
The seventeenth slice completes the second cottage upgrade, activating the
Moonhearth kitchen and a 24-slot ingredient pantry. Four stable dishes plan
quality-aware ingredients across both containers atomically and restore energy
only after a finished dish reaches the backpack.
New outdoor planting is blocked through
Longnight days 1–14 while existing crops keep growing and the greenhouse remains
climate-controlled; the Twilight Emporium rotates eight greenhouse seeds across
two weeks. A valid same-day weather value from an old save remains authoritative,
while a fixed snow-day forecast is corrected without adding schema fields. The
Longnight world aspect now extends the same date-derived, in-place visual refresh
to all six non-homestead regions without changing resources, collision, weather,
or saves. After all six Starlights and the workshop, greenhouse, and second
cottage upgrade are complete, the eighth construction project builds the
Sixfold Star Gate over five nights for stable-ID materials and coins. The Hand
then attunes its dormant state and opens six persistent regional travel routes.
Construction, activation, last destination, travel count, target preview,
bilingual UI, save normalization, and the original 2×2 gate atlas are all wired.
With the Moonpearl Egg Press, complete Phase-D fishing, complete Phase-E deep
mine progression, and this final gate closure, the planned A–F milestone is
complete. Phase G's code, content, and local release candidate are now complete
as well; only real startup and input acceptance on Windows and Linux remain
open, and successful macOS exports do not substitute for those platform runs.

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

- All twenty crops now have Regular, Luminous, and Starlight quality tiers.
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

## Woodland Watch and Homestead Harvest Starlights

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
- A second independent Homestead Harvest Starlight now stands before the farm
  beds. Its nodes accept any four distinct crops, the three existing artisan
  goods, and any three of four homestead fixtures. Full restoration expands
  Dewfall Sprinklers from four cardinal tiles to all eight adjacent outdoor
  farm tiles without affecting the greenhouse or woodland reward.
- The schema-v1 root remains a compatibility mirror of woodland only; modern
  saves add a stable-ID pedestal portfolio so both discoveries, contributions,
  and permanent rewards round-trip without overwriting one another.

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
| Toggle tool target lock | R | Right stick |
| Open first-day tips | H | Pause menu |
| Open morning briefing | J | Pause menu |
| Choose route guidance | G | Pause menu |
| Open full objectives | O | Back |
| Collapse minimap | M | Left stick |
| Cycle minimap filter | N / right-click minimap | — |
| Pause | Esc | Start |

Open **Settings & Accessibility** from the title or pause menu to remap keyboard
actions and adjust assist options. Controller bindings remain available while
keyboard bindings are changed. The pause menu also exposes first-day tips, the
morning briefing, route guidance, Gleamrise goals, the fish collection, fishing
gear, stellar resonance, and settings through D-pad focus and A confirm; the
first-day tip entry is disabled once every card has been skipped. Closing any of
these pause-opened child panels returns focus to the pause-menu button that
opened it. If a child panel cannot actually open because its state is empty or no
longer valid, the pause menu is restored immediately instead of dropping
controller focus back into the world.

## Experience foundations

A new game opens six dismissible guidance cards for the current quest, weather,
shipping, processing, commissions, and first exploration. Press `H`, or choose
**First-day tips** from the pause menu, after closing the panel to revisit cards
that were not dismissed. The plan reads the existing `GameSession` and never
changes quests, inventory, coins, weather, or save state while building
guidance. Every card also presents an action, an entry location, and the current
result, moving between not-started, active, and complete states from the real
session projection. A six-capability overview remains visible above the active
card for quest, weather, shipping, processing, commission, and exploration;
its colors derive from the same coverage contract, so dismissing guidance does
not manufacture completion or hide overall progress.

The deterministic opening-flow audit now starts from `GameSession.NewGame()`
and continues through quest, weather, commission, farming, shipping,
processing, exploration, and save/restore checkpoints. It proves the 90-minute
coverage contract without claiming a human 90-minute play session. Collected
artisan goods remain complete through their existing compendium discovery
evidence instead of adding a second progression flag. Fixed-target hints for
Mira and the cottage entrance now also flow through
`GameSession.PreviewSelectedTarget`, so hand availability and wrong-tool cues
are no longer duplicated in the scene layer.

An Apple M3 / Metal GUI-driven proxy run now reaches day two from the title
screen through all six guidance cards, `H` reopen, `O` objective details,
`M`/`N` minimap controls, Mira dialogue, tilling, planting, watering, cottage
sleep, nightly settlement, the seven-card briefing, save-to-title, and same-day
continue. The trace is recorded in
`artifacts/screenshots/qa-01-runtime-route.md`. It is neither headless nor a
restored completed state, but it still is not a human 90-minute session. A
central input contract now registers Back, left-stick click, D-pad, and A/B;
guidance and morning overlays close through controller B / `ui_cancel` while a
physical controller remains pending.

The persistent HUD keeps a one-line objective summary; press `O`, controller
Back, or click the objective bar for the full list. The minimap can collapse and
filter all, landmark, or forage markers. Press `G`, or choose **Route guidance**
from the pause menu, to explicitly select a destination available from the
player's current region or clear the current route. The six real road contracts
produce 12 ephemeral direction options: the homestead and each outer region
offer one onward/return destination, while the village offers the homestead and
all five outer regions. The HUD then shows forward/recovery direction and
remaining distance, while the minimap marks the next guide or endpoint. The
game never guesses a destination. First-day tips, morning briefing, and route guidance are all
controller-reachable with D-pad and A. Ambient audio automatically selects one
of eight original procedural loops from homestead, village, wilderness,
weather, festival, and combat context with a 0.45-second crossfade, while 15
core effect profiles share the same synthesis entry point. Master, ambience,
and effects volume are separately
adjustable on the first settings screen and persist immediately.

`artifacts/audio/audio-01-preview/audio-01-acceptance-tour.wav` provides one
60.85-second deterministic technical audition of all eight ambient contexts,
their 0.45-second transitions, and the 15 core effects, including distinct
generic-failure, resource-blocked, and tool-mismatch cues. It remains a technical
artifact; subjective loudness, fatigue, and transition feel still need human
listening.
The automated audio-quality gate additionally measures clipping, RMS level,
crest factor, low/mid/high-band energy, 50 ms loop-edge windows, separation
between all eight ambient fingerprints, and 0.45-second crossfade continuity.
These waveform metrics reject clear technical regressions but cannot decide
musical taste or long-session fatigue.

After a nightly summary, or when continuing a save at 06:00, a read-only morning
briefing presents weather, mail, festivals, eligible character events,
commissions, and a region suggestion in a stable seven-card order. Press `J`, or
choose **Morning briefing** from the pause menu, to reopen it manually. The first
new-game morning keeps the six-card onboarding as
its only automatic panel so the two explanations do not stack. The last shown
morning is stored additively in the world save, preventing same-day automatic
repeats after restart. Legacy day-one saves reserve that first morning for
onboarding, while day-two and later legacy saves still open the briefing once.
The briefing starts with a compact read-only decision summary containing at
most three actionable Primary/Secondary cards in stable priority order, helping
the player choose a useful goal without creating a second objective state.
Rows that resolve to a real world region expose a Start navigation button. A
route is planned and the briefing closes only after that explicit press;
mail, daily/weekly commissions, festival entrances, character-event schedule
points or exterior doors, and undiscovered landmarks also resolve to an exact
world target. Same-region targets begin their final approach immediately;
cross-region targets follow existing roads and hand off after the exact final
regional endpoint. The briefing still never chooses a goal automatically.

Immediate feedback now shares one HUD treatment for tool use, watering,
harvest, pickup, processing, fishing, damage, dodge, and major rewards. Failure
reasons keep their existing localized messages; generic failure, resource
blockage, and wrong-tool cues now play separate procedural sounds while reduced
effects only remove motion. Failure classification uses an explicit message-key
catalog; unknown keys stay generic instead of being reclassified just because
they contain fragments such as `missing`, `capacity`, or `needs_tool`. Pulse or shake intensity respects the accessibility
settings. Use `--ui-feedback-gallery` with an
optional `--ui-feedback-gallery-domain=<domain>` argument to inspect all 72
standard/reduced feedback states or one eight-tile domain without changing the
world session. Use `--ui-pause-experience-preview` to capture the real pause
menu layout with both experience entries available. Fifty-seven non-interactive navigation
guides reuse the seasonal exploration prop atlas around road edges, region
thresholds, and landmark approaches. Six walkable route contracts connect the
homestead, village, and every outer region while limiting unguided gaps to 18
cells without changing collision, paths, resources, or save data.
`artifacts/screenshots/world-01-route-walk-audit.md` reconstructs each complete
four-direction route against real `WorldDefinition` passability; it proves the
six contracts are continuously walkable, but does not replace human wayfinding.
`WorldNavigationRouteProgressPresenter` also exposes the nearest path cell,
next guide, dominant direction, recovery direction, off-route distance, and
remaining steps as a read-only UI projection in both directions. Reverse options
derive from the same six road contracts and reverse their path, guide order,
endpoints, destination, directions, remaining steps, and arrival result. Route
selection is ephemeral UI state: it does not write save, collision, or exploration
state and never chooses a destination on behalf of the player.
`WorldNavigationJourneyPlanner` now builds read-only multi-segment regional
plans from those 12 direction options entirely in Core: same-region plans have
zero segments, village routes are one segment, and homestead/peripheral
cross-region plans stably route through the village in two segments. The
selection layer stores the journey and advances only when the player reaches the
exact current-segment endpoint; being nearby while off route does not count as
arrival. The HUD keeps the final destination, current leg, current-leg region,
direction, and recovery distance visible. Morning-briefing action rows can
append an ephemeral four-direction final-approach path to the exact target. It
uses `WorldDefinition` plus fixed homestead obstacles and arrives only on a
walkable adjacent cell. Interior character-event targets carry both the exterior
door and the indoor NPC target cell: reaching the door prompts the player to
enter, then the standalone route HUD continues the final approach indoors while
the minimap route marker stays world-only. The selection refreshes the final
approach from the player's current cell when they leave the cached path or when
the active walkability predicate blocks a cached cell; if no replacement path
exists, the projection reports no valid final path until walkability changes
again rather than drawing a step into a blocked cell. The plan writes no save,
collision, exploration, or objective state and never chooses a destination
automatically. Use
`--select-route-destination=<WorldBiome>` for deterministic journey QA.

## Developer playtest scenarios

Developer launch flags are registered in priority order in
[`PlaytestScenarioRegistry.cs`](src/Game/PlaytestScenarioRegistry.cs). The
registry uses exact, case-sensitive matching; when multiple known flags are
present, the first registered scenario wins. With no known playtest flag, the
game keeps the normal title-screen startup.

`Main.CreatePlaytestScenarioRegistry()` binds every scenario ID to its setup
method. The binding map lives in
[`Main.PlaytestBindings.cs`](src/Game/Main.PlaytestBindings.cs). Setup methods
are grouped into four `Main.Playtests.*.cs` partials for farm/facilities,
objectives/collections, activities/village, and world/foundation acceptance.
Add a new ID and flag to the registry, add its setup-method mapping to the
binding file, and place the setup method in the matching domain file. Focused
tests lock the known flag catalog, uniqueness, priority, and normal fallback.

Runtime scene construction and switching live in
[`Main.ScenePresentation.cs`](src/Game/Main.ScenePresentation.cs), including
HUD setup plus farm, interior, festival, and exploration presentation. Pause,
onboarding, and briefing orchestration lives in
[`Main.ExperienceIntegration.cs`](src/Game/Main.ExperienceIntegration.cs);
feedback/audio context and route guidance live in
[`Main.FeedbackIntegration.cs`](src/Game/Main.FeedbackIntegration.cs) and
[`Main.RouteGuidanceIntegration.cs`](src/Game/Main.RouteGuidanceIntegration.cs);
festival overlays live in
[`Main.FestivalIntegration.cs`](src/Game/Main.FestivalIntegration.cs); and shop,
processor, shipping, commission, mail, starlight, backpack, crafting, and
storage overlays live in
[`Main.PlayerServicesIntegration.cs`](src/Game/Main.PlayerServicesIntegration.cs).
Player-overlay close dispatch lives in
[`Main.OverlayInputIntegration.cs`](src/Game/Main.OverlayInputIntegration.cs).
The shared `Main.cs` still owns the top-level lifecycle and business input
dispatch without retaining those complete open/close flows.

`GameSession` remains the sole global business entry point. Festival interaction
is grouped in
[`GameSession.Festivals.cs`](src/Core/GameSession.Festivals.cs), while animal and
greenhouse rules live in
[`GameSession.AnimalsAndGreenhouse.cs`](src/Core/GameSession.AnimalsAndGreenhouse.cs).
These partials share the same state and events; they add no parallel session or
save fields.

Stable item, crop, fish, recipe, weather, commission, and starlight data lives
in [`DataCatalog.cs`](src/Core/DataCatalog.cs), with processor-machine data in
[`ProcessorCatalog.cs`](src/Core/ProcessorCatalog.cs).
[`Definitions.cs`](src/Core/Definitions.cs) now contains only the shared
definition types, so adding content does not require editing unrelated type
declarations.

[`GameLocaleBootstrap.cs`](src/Game/GameLocaleBootstrap.cs) owns the default
English-then-Simplified-Chinese resource load order and selects Simplified
Chinese for a new session. Resource paths no longer live inline in
`Main._Ready()`.

Use `--playtest-stellar-convergence` to open the completed five-skill main-story
ending and `--playtest-accessibility-settings` to inspect the full settings and
remapping panel. Both support deterministic visual capture through
`--capture-playtest=<path>`.

Use `--playtest-liora-event-one`, `--playtest-liora-event-two`,
`--playtest-tavi-event-one`, and `--playtest-tavi-event-two` to open the two
event stages for Liora or Tavi directly in the Moonlit Archive or Moonstone
Workshop. These flags preserve the existing registry priority and can be
combined with `--capture-playtest=<path>` for deterministic visual QA.

Use `--playtest-nemi-event-one` and `--playtest-nemi-event-two` to open Nemi's
two friendship stages on her ordinary 14:00 village route. Use
`--playtest-starlight-post-door`, `--playtest-starlight-post`, and
`--playtest-starlight-post-nemi` to inspect the fifth building's exterior,
sorting counter, and Nemi's indoor work position. Use
`--playtest-starlight-post-delivery` to open the deterministic two-route board
directly. Use
`--playtest-starlight-post-wrong-tool` to verify the full counter outline uses
the warm-gold tool-mismatch state.

Use `--playtest-kael-event-one` and `--playtest-kael-event-two` to open Kael's
two friendship stages from his real ordinary-day 14:00 village projection. Use
`--playtest-starfall-watch-door`, `--playtest-starfall-watch`, and
`--playtest-starfall-watch-kael` to inspect the sixth building's exterior,
seal route table, and Kael's indoor work position. Use
`--playtest-starfall-watch-board` to open today's patrol, bounty, and ruins
preparation board directly. Use `--playtest-starfall-watch-wrong-tool` to verify
the full table outline uses the warm-gold tool-mismatch state.

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
`--playtest-rainveil-crops` to inspect Rainveil day 1, four mature plants, four
sprouts, Lantern Reed regrowth data, seasonal seed icons, and the shared crop
harvest outline. Use `--playtest-starharvest-crops` to inspect Starharvest day
1, four mature plants, four sprouts, Amberthread regrowth data, stable seed
icons, and a harvest highlight covering the real crop silhouette. Use
`--playtest-multi-processor` to inspect three machine states and atomic batch
collection. Use `--playtest-farming-specialization` to inspect the farming HUD
and permanent level-three specialization panel.

Use `--playtest-orchard-hives` to inspect a mature Moonplum tree, ready
Glowcomb Hive, orchard hotbar icons, object outlines, and deterministic
Starhoney collection state.

Use `--playtest-gleamrise-season` to open the pause-menu Gleamrise Goals panel
with prepared progress across the fourteen-day route. Use
`--playtest-fishing-donation` to open the Moonlit Archive ledger with donated,
ready, missing-held-fish, and undiscovered states visible together.

Use `--playtest-homestead-workshop-ready`,
`--playtest-homestead-workshop-in-progress`, and
`--playtest-homestead-workshop-completed` to inspect the two-project panel,
the blocked in-progress homestead entity, and the completed workshop with its
true-object mint outline. With the Hand selected, the completed workshop opens
the same plans at home and can commission an unfinished cottage upgrade. The
workshop collision no longer extends into the visible two-tile road on its west
side, keeping that route continuously walkable between the garden and facility
lane. Static world collision also closes procedurally generated one-cell
pockets that have no cardinal entrance, so every walkable world cell belongs to
the main component reached from the farm gate. Navigation occupancy now reads
the actual layout geometry for every stable player location, including the
cottage, greenhouse, animal buildings, grotto, ruins trial, and festivals. The
village landmark collision now follows the scaled runtime sprites instead of
their former atlas-scale footprints: the southern stone road remains open, the
village gate keeps a walkable center opening, and only its visible pillars stop
movement.

Use `--playtest-greenhouse-ready`, `--playtest-greenhouse-in-progress`,
`--playtest-greenhouse-exterior-completed`, `--playtest-greenhouse-completed`,
and `--playtest-greenhouse-cistern` to inspect the three-project panel, the
Rainveil repair facade, the completed Starharvest entrance, Longnight indoor
cross-season crops and harvest outline, and the Moondew Cistern bucket-refill
outline. The greenhouse uses seeds already owned by the player and receives
neither outdoor rain watering nor weather resonance.

Use `--playtest-longnight-homestead` to inspect Longnight day 1, the opaque
frostbound homestead skin, and the magenta no-mutation planting block on real
soil. Use `--playtest-longnight-emporium` to inspect the first four greenhouse
seeds, the Longnight-only note, and the 640×360 shop layout. Use
`--playtest-longnight-snow-forecast`, `--playtest-longnight-snow`,
`--playtest-longnight-snow-indoor`, and `--playtest-longnight-snow-clear` to
inspect the prior-day forecast, outdoor snow with target prompts, the snow-free
interior regression, and the same-season clear day. Starfeather sheltering is
also implemented. Use `--playtest-longnight-feast-gate`,
`--playtest-longnight-feast`, `--playtest-longnight-feast-activity`,
`--playtest-longnight-feast-result`, `--playtest-longnight-feast-stall`,
`--playtest-longnight-feast-activity-en`, and
`--playtest-longnight-feast-wrong-tool` to inspect the real gate, independent
sixteen-villager scene, bilingual rite panel, completed item/ritual projection,
Lantern Knot stall, and gold Hand requirement.

Use `--playtest-starfeather-coop-ready`,
`--playtest-starfeather-coop-in-progress`, `--playtest-starfeather-coop-grazing`,
`--playtest-starfeather-coop-chick`, `--playtest-starfeather-coop-adult`, and
`--playtest-starfeather-coop-nest-blocked-en` to inspect the unbuilt/in-progress
facades, clear-day grazing, juvenile and adult interiors, Longnight sheltering,
and the English magenta nest block for a full backpack.

Use `--playtest-moonfleece-barn-ready`,
`--playtest-moonfleece-barn-in-progress`, `--playtest-moonfleece-barn-grazing`,
`--playtest-moonfleece-barn-juvenile`, and
`--playtest-moonfleece-barn-rack-blocked-en` to inspect the waterside unbuilt
and construction facades, clear-day grazing, the independent juvenile interior,
and the English magenta rack block for a full backpack. Use
`--playtest-dewhorn-grazing` and `--playtest-dewhorn-milking-blocked-en` to
inspect the Dewhorn's real-object grazing outline, two species sharing one barn,
and the English magenta milking-station block for a full backpack. Use
`--playtest-livestock-automation-console`, `--playtest-livestock-automation-panel`,
`--playtest-livestock-automation-panel-en`, and
`--playtest-livestock-automation-construction` to inspect the real console outline,
28/28 fodder, 12/12 mixed-quality products, bilingual 640×360 layout, and the
six-project construction panel. Phase F's three animals, barn, auto-feeding, and
auto-collection are complete; breeding and product processing remain later work.

Use `--playtest-meadow-starlight-dormant`,
`--playtest-meadow-starlight-restored`, `--playtest-meadow-starlight-panel`,
`--playtest-meadow-starlight-panel-en`, and `--playtest-meadow-pollination` to
inspect the real meadow pedestal in both states, bilingual three-node layout,
festival-derived progress, and a Glowcomb Hive using the six-tile reward range.

Use `--playtest-starharvest-market-gate`, `--playtest-starharvest-market`,
`--playtest-starharvest-market-showcase`, `--playtest-starharvest-market-result`,
`--playtest-starharvest-market-shop`, and
`--playtest-starharvest-market-showcase-en` to inspect the day-11 gate, the
independent sixteen-villager plaza, bilingual showcase layout, submitted exhibit
with exact item icons, and the four-offer Scrip stall. The festival closes at
18:00 and safely returns any remaining player to Lumen Village.

Use `--playtest-gleamrise-festival-gate`, `--playtest-gleamrise-festival`,
`--playtest-gleamrise-festival-challenge`,
`--playtest-gleamrise-festival-result`,
`--playtest-gleamrise-festival-exchange`, and
`--playtest-gleamrise-festival-challenge-en` to inspect the day-4 gate, the
independent sixteen-villager bloomfield, partial real-plot planting, frozen
30-point result, four-offer Bloom Token exchange, and bilingual 640×360
activity layout. Attempts persist on leaving and resolve at their deadline or
festival close without consuming ordinary farm resources.

Use `--playtest-village-rain-schedule` to open Sela's rainy-day workshop
dialogue, and `--playtest-village-rainveil-schedule` to inspect Vessa's first
Rainveil-day route and the season HUD. Both scenarios restore an explicit
current weather before resolving the NPC projection and support deterministic
capture.

Use `--playtest-npc-pathfinding` to open Lumen Village at 13:30 while multiple
villagers are between schedule anchors. Combine it with
`--capture-playtest=<path>` for deterministic movement QA.

Use `--playtest-elowen-event-one`, `--playtest-elowen-event-two`,
`--playtest-vessa-event-one`, and `--playtest-vessa-event-two` to open the new
relationship events from the villagers' real schedule projections. Use
`--playtest-vessa-event-wrong-tool` for the warm-gold mismatch outline on the
real character target, and `--playtest-relationship-mails-en` for the English
mail panel containing all five new event-gated letters and real attachments.

## Asset layout

Project assets are organized by source and runtime responsibility. Original
generated art lives below `assets/generated/` in dedicated activity, animal,
character, farming, feature, location, UI, world, or legacy subdirectories; the
generated root contains documentation only. Source atlases, runtime textures,
and their Godot import descriptions stay together. See `assets/README.md` and
`assets/generated/README.md` for the routing and migration rules.

## Local toolchain

The implementation is pinned to:

- Godot 4.7.1 .NET
- .NET SDK 10.0.302
- Project target framework `net8.0`

For this workspace the tools are installed outside the repository at
`$HOME/.codex/tools/luminfield/`.

On macOS, run:

```bash
./scripts/open_editor_macos.command
```

The launcher injects the project-local `DOTNET_ROOT`. Opening the original
`Godot_mono.app` directly from Finder omits that environment and can produce a
`Failed to load .NET runtime / hostfxr` dialog.

```bash
export LUMINFIELD_TOOLS="$HOME/.codex/tools/luminfield"
export DOTNET_ROOT="$LUMINFIELD_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

"$DOTNET_ROOT/dotnet" build
"$LUMINFIELD_TOOLS/godot/Godot_mono.app/Contents/MacOS/Godot" \
  --path "$(pwd)" --editor
```

## Validation

```bash
dotnet test tests/Luminfield.Tests/Luminfield.Tests.csproj
godot --headless --path . --editor --quit
godot --headless --path . --quit-after 180
./scripts/export_all.sh
./scripts/verify_phase_g_fast.sh
./scripts/verify_release_candidate.sh
```

Use `verify_phase_g_fast.sh` during implementation; it incrementally builds and
runs only Phase G, save, and scenario-registry coverage. The release-candidate script is
the final gate and intentionally runs the complete suite plus Godot startup and
export-structure checks.

The default policy is minimum sufficient validation: documentation and rule
changes receive content and diff checks; local code changes receive a build and
focused tests; scene, asset, localization, and UI checks are added only when
affected. Run the full regression only for a stable delivery candidate, save or
global-contract changes, cross-domain infrastructure changes, a focused failure
that indicates cross-module risk, or an explicit request. A passing full gate is
not invalidated by later README, rule, or Obsidian-only edits.

The save file is written atomically to `user://saves/slot_1.json`. Corrupt saves
are preserved with a `.broken-<timestamp>` suffix. The current contract is
`schemaVersion: 2`: schema-v1 saves migrate additively to Gathering and Stellar
Resonance defaults, and the latest three successful saves are retained as
`.bak.1`–`.bak.3`; a damaged primary save recovers from the newest valid backup.
Glow coins, the three independent processing jobs, artisan goods,
watering-can water,
world-resource depletion dates, weather, the pending shipping chest, placed
Starwoven Chests with their 16-slot contents, the daily and weekly commission
states, the Woodland Watch Starlight's partial offerings and permanent reward,
and the 24-slot backpack are stored in the migrated `schemaVersion: 2` save.
Liora's, Tavi's, Nemi's, Kael's, Sela's, and Orin's character-event completion
IDs and dates use another additive projection in that same schema.
Season, season day, and year are derived from the existing absolute day; they do
not add another save field to the migrated `schemaVersion: 2` contract.
Fertilized farm cells, stable quality rolls, planting days, resolved resonance
produce, farming XP and specialization, Moonstone Paths, Starwood Fences,
Starlight Torches, and Dewfall Sprinklers use additive fields in that same
schema. Moonplum fruit trees and Glowcomb Hive production state also use
additive orchard fields; invalid, overlapping, or orphaned orchard entries are
filtered on load. The modern per-machine list safely migrates the legacy processing
queue. Older saves receive safe defaults and tool-ID migration for the new
additive fields.
`ExperienceGuidance.LastMorningBriefingDay` is another additive field in the
same schema. It records only UI display history, not quest or business progress;
day-one legacy saves normalize to the onboarding-only policy, while later
legacy days remain eligible for their first briefing.
The Moondew Greenhouse's 24 cultivation cells use a separate additive
`Greenhouse.Tiles` root, so indoor and outdoor cells with the same coordinates
remain independent. Access is derived only from the completed
`homestead_greenhouse` project; old schema-v1 saves receive an empty greenhouse,
and an invalid unbuilt interior position safely returns to the homestead door.
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

- 2026-08-26 14:25:00 CST — Realigned all eleven village landmark collision
  footprints with their scaled runtime sprites, removed the transparent walls
  around the southern gate and six public buildings, and preserved the gate's
  visible pillars plus center passage. Added screenshot-time eastbound-road,
  gate-column, gate-opening, and former-hidden-cell regression coverage.

- 2026-08-26 10:21:43 CST — Narrowed the homestead workshop collision from an
  oversized hidden rectangle to its stable interaction anchor, restored the
  visible two-tile north-south passage beside the workshop, and closed 11
  procedurally isolated one-cell terrain pockets. Focused regression coverage
  now protects the facility corridor, full-world static connectivity, and
  navigation geometry coverage for all stable player locations.

- 2026-08-26 09:49:02 CST — Added three regional event packs, year-two festival
  replay rules and capacity-safe memorial rewards, plus four postgame Stellar
  Resonance objectives. Regional and milestone state now persists additively,
  the pause menu can review historic festival results, and bilingual text and
  focused compatibility tests cover the new content without resuming the
  deferred free-building system.

- 2026-08-25 12:11:03 CST — Closed the final `LOC-WATCH` acceptance gaps.
  The Seal Route Table target preview now uses the same opening-hours rule as
  its action, so a player still inside after 20:00 sees the blocked status
  instead of an executable prompt. The three-column board was compacted within
  the 640×360 safe canvas so its Back button is fully visible. The mismatched
  bounty case now runs through a real Starfall Ruins enemy defeat, and successful
  patrol/bounty tests assert exact coins, items, and Kael relationship rewards.
  Verification passes a zero-warning C# build, 10/10 LOC-WATCH checks, the
  51/51 Phase G fast gate, 2336/2336 localization parity, Godot import and
  180-frame startup, plus a renewed Apple M3 / Metal capture. Overall progress
  remains **74%** and the free `BuildingSystem` remains untouched.

- 2026-08-25 11:55:39 CST — Completed `LOC-WATCH`, turning Starfall Watch into
  a playable location. Its seal route table deterministically offers two
  completable patrols, one bounty, and one of three ruins preparations each
  day. Patrols connect to real world regions; bounties advance only from real
  matching-enemy defeats in the ruins or deep mine, fail on defeat, and reset
  across days. Both reward paths atomically grant items, glow coins, and Kael
  relationship points; insufficient capacity leaves the task, coins, items,
  and relationship unchanged. Seal Ward, Route Threads, and Field Ration apply
  daily incoming-damage, enemy-speed, and first-entry energy effects. Same-day
  persistence, invalid-save normalization, the bilingual board, and a
  deterministic playtest entry are included. Verification passes a zero-warning
  C# build, 10/10 LOC-WATCH checks, the 51/51 Phase G fast gate, 2336/2336
  localization-key parity, and Godot 4.7.1 import plus 180-frame startup. The
  Apple M3 / Metal capture is `artifacts/screenshots/loc-watch-board.png`.
  Overall progress is now **74%**; the free `BuildingSystem` remains
  `deferred-explicit-reopen` and untouched.

- 2026-08-25 11:12:05 CST — Completed `LOC-POST`, turning the Starlight Post
  into a playable location. Its sorting counter offers two deterministic daily
  routes, allows one active route at a time, and caps completion at two. Eight
  stable routes reuse the 16 villagers' real schedule projection; delivery to
  the correct recipient with the Hand atomically grants glow coins and 2
  relationship points. Wrong recipients, wrong tools, and invalid routes are
  side-effect free, while target preview and delivery share the same contract.
  Same-day progress restores, unfinished routes expire across days, and the
  delivery state neither copies nor mutates `MailSystem`. The bilingual route
  board, recipient-specific replies, deterministic playtest entry, and macOS
  640×360 visual capture are included. Verification passes a zero-warning C#
  build, 9/9 post-delivery/playtest checks, 6/6 localization checks, the 51/51
  Phase G fast gate, and Godot 4.7.1 import plus 180-frame startup. The Apple M3
  / Metal capture is `artifacts/screenshots/loc-post-delivery-board.png`.
  Overall progress is now **70%**; the free `BuildingSystem` remains
  `deferred-explicit-reopen` and untouched.

- 2026-08-25 10:23:53 CST — Completed `LOC-TEA`, turning the Starweaver Tea
  House into a playable location. The counter now rotates three deterministic
  teas; with the hand selected, one atomic tasting per day grants a takeaway
  item, restores 10–18 energy, and applies +8% outdoor movement for 120 game
  minutes. From 13:00 to 18:00, at least two actually scheduled villagers can
  join one daily afternoon gathering for 3 relationship points each. The
  purchase, effect, gathering, and guest list survive same-day saves and reset
  across days; invalid or wrong-day offers normalize safely. The shared target
  preview, bilingual menu, deterministic playtest entry, and 640×360 visual
  capture are included. Verification passes a zero-warning C# build, 7/7 tea
  house tests, the 51/51 Phase G fast gate, a Godot 4.7.1 editor import, and
  2213/2213 localization-key parity. Overall progress is now **66%**; the free
  `BuildingSystem` remains `deferred-explicit-reopen` and untouched.

- 2026-08-25 09:33:45 CST — Closed the pause-menu child-panel return and focus
  contract. First-day tips, the morning briefing, route guidance, Gleamrise
  goals, the fish collection, fishing gear, stellar resonance, and settings all
  enter through `OpenPauseChild(openChild, isChildOpen)`; if the target panel
  does not actually open because its state is empty or no longer valid, the pause
  menu is restored immediately. All matching close paths now restore the pause
  menu through `RestorePauseAfterChild()`, so B / Start closes only the child
  panel and returns to the original pause-menu button instead of closing pause or
  restoring world controls in the same input. Focused persistence coverage now
  exercises every accessibility setting, missing or corrupt files, and invalid
  value normalization. C# builds with zero warnings and the combined focus,
  input, Main integration, and accessibility checks pass 48/48; the Phase G
  fast gate passes 51/51 and `git diff --check` is clean. Overall progress is
  now **62%**; the free
  `BuildingSystem` remains `deferred-explicit-reopen` and untouched.

- 2026-08-24 23:23:56 CST — Made final-approach route guidance resilient to
  detours, temporary walkability changes, and indoor character-event targets.
  Character-event destinations can now carry an exterior door plus an indoor NPC
  target cell; the HUD prompts at the entrance and continues the final approach
  after location handoff, while the minimap route marker remains world-only.
  When the player leaves the cached final path, or when the current walkability
  predicate blocks a cached cell, the selection rebuilds a pure-Core
  adjacent-target path from the current player cell; if an obstruction has
  invalidated the cached path and rebuilding fails, the current projection
  reports no valid final path until walkability changes again.
  This remains ephemeral UI state and adds no save, collision, or objective
  state. Verification now passes WorldNavigation 61/61, morning navigation 9/9,
  Main integration architecture 9/9, Phase G fast 51/51, full C# 1127/1127,
  localization 2184/2184, Godot import, 180-frame headless startup, and
  `git diff --check`. No new
  macOS visual inspection was claimed, the candidate is conservatively counted
  as 60% of the independent content checklist, and the free `BuildingSystem`
  remains paused.

- 2026-08-24 23:08:02 CST — Closed morning navigation from a regional hint to
  a precise entrance/object approach. Mail, daily/weekly commissions, all four
  festival entrances, character-event schedule points or exterior doors, and
  landmark suggestions now carry stable exact targets. Same-region selections
  start the final approach directly; cross-region journeys hand off at the
  exact last road endpoint. A pure-Core four-direction approach path, adjacent
  arrival, fixed-obstacle avoidance, final-approach HUD, and dual minimap
  markers add no save or business state. Precise tests pass 47/47, expanded
  navigation/experience tests 159/159, Phase G fast 51/51, full C# 1118/1118,
  localization 2183/2183, and Godot 4.7.1 import. The process confirmed Apple
  M3 / Metal; macOS was locked, so this change does not claim a fresh visual
  inspection of the new HUD/minimap. The free `BuildingSystem` remains paused.

- 2026-08-24 22:30:29 CST — Added explicit Start navigation actions to
  actionable morning-briefing summaries. Mail, commissions, festivals,
  character events, and region suggestions resolve through real catalogs to a
  world region. Route selection now runs 0/1/2-leg journeys, advances at exact
  transfer endpoints, and keeps the final destination and leg number on the
  HUD; nearby off-route cells no longer count as arrival. No save, collision,
  exploration, or duplicate objective state was added. Focused tests pass
  67/67, Phase G fast 51/51, full C# 1111/1111, localization 2181/2181, and
  Godot import/180-frame startup. Apple M3 / Metal visuals cover the briefing
  action and two-leg off-route HUD. The free `BuildingSystem` remains paused.

- 2026-08-24 22:19:46 CST — Added the pure-Core
  `WorldNavigationJourneyPlanner`, which creates stable shortest multi-segment
  plans across all seven world regions from the 12 existing direction route
  options. Same-region plans have 0 segments, village routes have 1, and
  homestead/peripheral cross-region plans route through the village in 2. The
  route-selection layer can store and advance the active segment without writing
  save, collision, exploration, or scene state. New tests cover all 7×7 region
  pairs, continuity, existing-route references, invalid enum boundaries, and
  snapshot independence, and the morning-briefing navigation test now uses the
  current mail-save field names. Focused planner tests pass 8/8, the world
  navigation set passes 39/39, Phase G fast passes 51/51, and full C# passes
  1106/1106. The free `BuildingSystem` remains paused.

- 2026-08-24 22:08:01 CST — Derived 12 forward/return choices from the six real
  road contracts. Route guidance now offers only destinations available from
  the player's current world region, maps interiors back to their real region,
  and reverses path, guides, endpoints, destination, directions, remaining
  steps, and arrival without writing save, collision, or exploration state.
  Single-return panels now use a compact full-width layout. Player-overlay close
  dispatch also moved into its own `Main` partial with all 31 branches kept in
  order. Route/input focus passes 69/69, Phase G fast passes 51/51, full C#
  passes 1083/1083, localization matches 2176/2176, and Godot import/180-frame
  startup passes. Apple M3 / Metal visuals cover the six-destination village
  panel, one-destination woods return panel, and reverse-route HUD/minimap.
  The explicitly paused free `BuildingSystem` remains untouched.

- 2026-08-24 21:37:19 CST — Added player-selected six-route guidance through
  `G` and the pause menu, with HUD direction/distance feedback and a minimap
  marker for the next guide or endpoint. Off-route guidance uses a dedicated
  recovery direction, never auto-selects a destination, and writes no save,
  collision, or exploration state. Feedback/audio context also moved into its
  own `Main` partial to reduce experience-integration contention. WORLD/UI/BASE
  focus passes 73/73, Phase G fast passes 51/51, full C# passes 1058/1058,
  localization matches 2176/2176, Godot import/180-frame startup passes, and
  Apple M3 / Metal visuals cover both route selection and the active-route HUD.

- 2026-08-24 21:16:40 CST — Added a read-only six-capability overview to
  first-day guidance, moved feedback failures to an explicit message-key
  catalog, and added a UI-ready next-guide/direction/remaining-route projection.
  Festival and player-service overlays moved out of shared `Main.cs`, reducing
  it from 3105 to 2454 lines with architecture tests preventing regressions.
  Seven-module focus passes 431/431, Phase G fast passes 51/51, full C# passes
  1041/1041, localization remains 2154/2154, Godot import/main startup passes,
  and the Apple M3 / Metal capability-overview layout is visually accepted.

- 2026-08-24 20:54:31 CST — Added the read-only three-item morning decision
  summary, split clear ambience into homestead/village/wilderness soundscapes,
  and added complete four-direction audits for all six navigation routes.
  Pause and experience input orchestration now lives outside the shared
  `Main.cs` hotspot. Combined focus tests pass 293/293, Phase G fast passes
  51/51, full C# regression passes 987/987, localization remains 2154/2154,
  and Godot import/main-scene startup plus Apple M3 / Metal morning layout pass.

- 2026-08-24 20:40:32 CST — Added semantic feedback audio mapping so generic
  failure, resource-blocked, and tool-mismatch cues resolve to distinct
  procedural sounds while successful action sounds remain owned by their
  original actions. Focused feedback/audio tests pass 168/168 and 67/67, the
  preview audio package now exports 15 effect WAVs, Phase G fast remains 51/51,
  and full C# regression passes 969/969.

- 2026-08-24 20:21:20 CST — Added controller-reachable pause-menu entries for
  first-day tips and the morning briefing, restored focus to the originating
  pause button after tips/briefing/settings, and added a localized 72-state
  FEEL acceptance gallery. Apple M3 / Metal captures now cover all nine
  feedback domains plus the real pause layout; full C# regression passes
  884/884. The live focus route remains pending because macOS was locked.

- 2026-08-24 20:03:45 CST — Made first-day tips and the morning briefing
  reachable from the pause menu for controller focus/A navigation, with the
  first-day tip entry disabled after all cards are skipped and localized in both
  languages.

- 2026-08-24 19:34:47 CST — Added BASE candidate runtime and automatic quality
  evidence: an Apple M3 / Metal GUI run covered new game, first-day farming,
  sleep, the day-two briefing, and same-day resume; keyboard/controller bindings
  now share a central contract and guidance/briefing accept B to go back; audio
  gained clipping, loudness, band, loop, separation, and crossfade gates; Mira
  and cottage fixed-target hints now resolve through GameSession. Human
  90-minute play, listening, physical-controller, and free-building work remain
  explicitly incomplete.

- 2026-08-24 19:07:06 CST — Closed the next BASE experience acceptance slice:
  added the real-action opening-flow audit and empty-shipping-day restore fix,
  persisted morning-briefing display history with day-one legacy compatibility,
  added a 45.75-second deterministic audio audition, expanded FEEL coverage to
  a 72-case matrix, tightened 57 world guides to an 18-cell camera budget, and
  added 24 HUD hierarchy combinations with four-season and 100%/120% Metal
  captures. The deferred free-building scope remains untouched.

- 2026-08-24 18:27:34 CST — Added bilingual action, entry, and current-result
  guidance to all six opening cards; persisted separate master, ambience, and
  effects volumes with a 0.45-second ambient crossfade; and expanded navigation
  to 36 guides plus six walkable contracts spanning every outer region.

- 2026-08-24 17:46:29 CST — Added the seven-card morning briefing with automatic
  post-sleep and 06:00-continue entry plus a manual `J` shortcut; integrated
  shared immediate feedback for processing, combat damage/dodge, fishing, core
  farm actions, and major rewards; and registered 31 seasonal visual navigation
  guides without changing collision, pathing, resources, save data, or the
  deferred free-building scope.

- 2026-08-24 18:05:00 CST — Integrated six dismissible first-day guidance
  cards, full HUD objective details, minimap collapse/marker filtering, six
  24–40 second procedural ambient loops, and 13 action-effect profiles. Added
  explicit visual-capture flags and automatic audio context switching for
  location, weather, festivals, and combat.

- 2026-08-24 17:24:00 CST — Moved roughly 5,100 lines of playtest setup logic
  out of `Main.cs` into four domain partials for farm/facilities,
  objectives/collections, activities/village, and world/foundation acceptance,
  then centralized HUD and all scene presentation/switching in
  `Main.ScenePresentation.cs`. Roughly 2,700 lines of festival, animal, and
  greenhouse rules also moved into two `GameSession` domain partials, and the
  data and processor-machine catalogs moved out of the shared definition file.
  `GameLocaleBootstrap` now provides the bilingual resource-loading entry point.
  The byte-equivalent moves separate runtime entry, scene presentation,
  playtest support, domain rules, content catalogs, and locale assembly without
  changing behavior.

- 2026-08-24 16:32:20 CST — Moved all 243 developer playtest scenario bindings
  out of the oversized `Main.cs` into the dedicated
  `Main.PlaytestBindings.cs` partial. Registration order, setup methods, normal
  startup fallback, and gameplay behavior remain unchanged, giving concurrent
  content modules one playtest-binding integration point.

- 2026-08-24 14:00:21 CST — Separated the beginner processors, cleared the
  moon-lantern arch and widened cross-region road corridors, and moved the city
  starlight and star gate off the civic arteries. Rebuilt the city from four
  overlapping high-detail generations plus a dedicated plaza refinement at a
  native 2048×1536, and gave all sixteen NPCs shared smooth grid-step movement,
  two-step sway, bob, and dynamic shadows across world, interior, and festival
  scenes without adding save state.

- 2026-08-24 12:54:06 CST — Rebuilt exploration art around the homestead's
  complete-backdrop model. Twenty-three project source images now compose four
  continuous 4096×3072 seasonal world masters from global, beginner, city, and
  eleven high-detail sector generations. Runtime and minimap use the seasonal
  masters directly, tiled ground and scenic stickers are removed, procedural
  decoration density is reduced, and the rejected atlas approach is archived.

- 2026-08-24 10:49:33 CST — Rebuilt the world as a 256×192, 8×6-chunk map with
  a 64×64 beginner district and a 128×96 central city. Re-zoned public service,
  civic, commercial, and late-game facility districts; migrated entrances,
  festivals, NPC schedules, roads, collisions, minimap bounds, and deterministic
  playtests to the new coordinate baseline. Added original generated district
  ground and eight topology-specific scenic landmarks, archived the superseded
  scenic atlas, and cached static forage candidates so ten-year simulation
  remains within budget after expansion.

- 2026-08-21 17:49:21 CST — Corrected the merged southern world-gate atlas
  reference to the categorized farming asset. Added a fast source audit that
  fails when a literal runtime asset path points to a missing repository file.

- 2026-08-21 17:37:13 CST — Closed the final Phase G audit gaps. Controller B
  now closes open panels without opening pause in the world; empty remaps cannot
  erase defaults, disabling target lock releases facing, and backup recovery
  rebuilds the primary save. Added base-drop balance guards, populated postgame
  long-run coverage, and focused tests for applied bonuses. Corrected the 120%
  settings layout in an Apple M3 / Metal capture, passed the 50/50 fast gate and
  the single 605/605 final gate, then rebuilt all three platform candidates.

- 2026-08-21 17:00:17 CST — Changed validation to focused checks during
  development and one full gate for stable candidates. Documented full-suite
  triggers, invalidation boundaries, and the rule that documentation-only
  follow-ups do not repeat regression tests.

- 2026-08-21 16:45:46 CST — Completed the Phase G release-candidate increment:
  a persistent Gathering skill closes the five-skill catalog, the Sixfold Star
  Gate now resolves a bilingual main-story ending and five-rank postgame
  resonance, and `release_balance_v1` freezes economy/growth/task/festival/build
  guardrails. Added persistent accessibility and remapping settings, controller
  focus fallback, schema-v2 migration with three rotating backups and recovery,
  ten-year simulation coverage, deterministic convergence/settings captures,
  and a release-candidate verification script.

- 2026-08-21 15:32:48 CST — Reorganized all 146 generated PNG assets and their
  Godot import descriptions into 33 responsibility-based leaf directories for
  NPCs and player characters, animals, farming, activities, feature points,
  locations, UI, world visuals, and inactive legacy sources. All repository
  resource references and documented paths now follow the new layout; pixel
  content, stable atlas indices, and gameplay state remain unchanged.

- 2026-08-21 15:11:34 CST — Closed the complete A–F milestone. Phase C now has
  the Moonpearl Egg Press and Starfeather Cream; Phase D has line-control fishing,
  rod/tackle upgrades, crab pots, and fishing specializations; Phase E has a
  deterministic twelve-room deep mine, six enemies, three weapons, four shovel
  tiers, and dual adventure skills. Phase F now ends with an atomic five-night
  Sixfold Star Gate build, Hand attunement, six regional routes, additive save
  state, object preview, and bilingual UI. New generated atlases were processed
  into strict binary-alpha runtime assets and checked in Apple M3 / Metal
  captures; validation details are recorded in the completion handoff.

- 2026-08-21 13:13:10 CST — Completed the first central-city and world-space rebuild. The
  city is now a 64×64-cell four-way travel hub; all six enterable buildings,
  eleven existing landmarks, sixteen villager schedules, three festival return
  points, and legacy-save safe positions moved with it. Outer procedural prop
  density is about 15%, supplemented by 34 curated compositions, eight large
  original landmarks, and five deterministic scenic playtests. The 192×128
  world, stable IDs, schema v1, and Core state ownership remain unchanged.
  573/573 tests, C# build, Godot headless startup, hard asset checks,
  diff-check, and Apple M3 / Metal captures 374–379 passed. A complete freeform
  `BuildingSystem` is now explicitly future gameplay work; this rebuild does
  not add free construction.
- 2026-08-21 — Merged the paused Phase-F snapshot with the remote mainline.
  Added the fourteen-day Gleamrise Goals panel and the Moonlit Archive fish
  donation ledger, including additive schema-v1 persistence, atomic claims and
  donations, bilingual UI, focused tests, and deterministic playtest entries.
  Older Starfeather/sowing-festival art is retained only as historical source
  material; runtime animal and festival behavior continues to use the current
  three-species `AnimalSystem` and sixteen-villager day-4 planting challenge.
  The Star Gate, final Phase-F acceptance, full Phase-D fishing, and full
  Phase-E procedural mine/combat remain unfinished.
- 2026-08-21 — Archived the current paused Phase-F snapshot after the twenty-ninth
  slice and the Longnight six-region world aspect. The fixed three-room Starfall
  Ruins trial adds three enemy families, six instances, the Moonsteel Shortblade,
  attacks/dodge/health/defeat recovery, four artifacts and Archive donation, the
  4/4 Artifact and 3/3 Enemy Codices, and the sixth Remembrance Starlight. Existing
  captures 367–372 cover the trial, combat, collections, donation, and sixth-light
  states; capture 373 covers the Longnight world aspect. Current regression passed
  561/561 tests, 1759/1759 locale-key parity, C# build, Godot 4.7.1 .NET import and
  headless startup. Starlights are 6/6 and the compendium 8/8. The original 2×2
  Sixfold Star Gate atlas is only prepared art with no runtime integration, so the
  Star Gate, final Phase-F acceptance, full Phase-D fishing, and full Phase-E
  procedural mine/combat remain unfinished; later work should continue from this
  snapshot rather than treating Phase F as complete.
- 2026-08-21 — Completed Phase F's twenty-eighth slice: the fixed five-room
  Crystal Grotto survey and the fifth Crystal Vale Attunement Starlight. The
  slice adds four stable minerals, the two-night Bronze-Star shovel upgrade, a
  seal and room-five survey anchor, and the 4/4 `codex_minerals` category. Four
  ore contributions plus the tool and depth milestones restore the Starlight
  and open the later Starfall Ruins trial route; schema remains v1. Original
  interior, 4×3 ore/icon atlas, and 2×2 pedestal atlas passed hard asset checks;
  552/552 tests, 1670/1670 locale-key parity, Godot import/headless startup,
  diff-check, and Apple M5 / Metal captures 360–366 passed. Starlights are now
  5/6 and the compendium 6/8; this fixed survey is not complete Phase E, and the
  Ruins trial, final Starlight, Star Gate, and Phase F remain incomplete.
- 2026-08-21 — Unblocked Phase F's Phase-D content dependencies and completed
  its twenty-seventh slice. The working tree now contains 24 stable fish,
  region/season/weather/time-conditioned catches, a 24/24 Fish Codex, and the
  fourth Moonwater Resonance Starlight, followed by the fourth complete main
  festival, Firefly Tide. The optional Rainveil day-12 wetland scene opens
  18:00–23:00 with all sixteen villagers, atomically scores exactly three
  different wetland fish, records an annual award, grants persistent Glowmarks,
  and offers four capacity-safe exchanges. Original backdrop/prop art, 545/545
  tests, 1613/1613 locale-key parity, Godot import/headless startup, hard asset
  checks, diff-check, and Apple M5 / Metal captures 346–357 passed. Main
  festivals are 4/4, Starlights 4/6, and the compendium 5/8; the full fishing
  minigame, tackle, crab pots, two remaining Starlights, Star Gate, and Phase F
  are still incomplete.
- 2026-08-21 — Added Phase F's twenty-sixth slice: eight deterministic seasonal
  forage items and the 8/8 `codex_forage` category. Gleamrise, Rainveil,
  Starharvest, and Longnight each provide one woods and one meadow item; normal
  weather resolves two daily nodes while Stardust Wind resolves four. Hand-only
  adjacent pickup costs no energy, same-day saves preserve positions/collection,
  and every failure remains atomic. The one-time Starpath Forager's Guide reward
  marks only today's uncollected nodes in explored minimap chunks. An original
  1024×1024 4×4 chroma/runtime atlas, 519/519 tests, 1441/1441 localization
  parity, Godot import/headless startup, hard asset checks, diff-check, and Apple
  M5 Metal captures 340–345 passed. Saves remain schema v1; the codex is now 4/8
  categories and Phase F is not complete.
- 2026-08-21 — Added Phase F's twenty-fifth slice: `codex_artisan` records the
  four existing artisan goods only when a finished batch or Starhoney truly
  enters player ownership. The archive now has a 2×2 artisan grid with hidden
  silhouettes and catalog-derived production details. Completing 4/4 unlocks
  the one-time `codex_reward_starlit_appraisal_ledger`; direct sales and shipping
  settlements for those four frozen entries then pay 10% more, rounded up,
  through the same runtime price source. Shipping lines persist their settled
  unit price so later reward state changes cannot rewrite history. Schema v1
  category initialization backfills only the newly added artisan evidence once.
  Validation passed 501/501 tests, 1423/1423 locale-key parity, Godot import and
  headless startup, diff-check, and Apple M5 / Metal captures 327–331. The
  compendium is now 3/8 categories and Phase F remains incomplete.
- 2026-08-21 — Added Phase F's twenty-fourth slice: `codex_cooking` records the
  four existing cooked dishes only when a finished meal enters player
  ownership. The archive compendium is now category-driven and supports both a
  20-entry crop grid and a 4-entry cooking grid without exposing undiscovered
  names or recipes. Completing 4/4 unlocks a one-time
  `codex_reward_moonhearth_recipe_journal` claim; afterward those four dishes
  restore 5 additional energy through the same runtime value used by kitchen
  UI and eating. Schema v1 now records initialized category IDs so phase-23
  saves backfill cooking evidence once without reinitializing crops. This slice
  also fixes pantry-only cooking so a valid zero-backpack-removal plan commits
  atomically. Validation passed 497/497 tests, 1413/1413 locale-key parity,
  Godot import/headless startup, diff-check, and Apple M5 / Metal captures
  321–326. The compendium is now 2/8 categories and Phase F remains incomplete.
- 2026-08-21 — Added Phase F's twenty-third slice: the stable-ID `codex_crops`
  Crop Codex records all 20 existing crops when regular, quality, or resonance
  produce first enters player ownership; seeds and previews do not discover
  entries. The Moonlit Archive now opens a real 5×4 bilingual compendium with
  hidden silhouettes, existing seed/growth/harvest art, catalog-derived details,
  20/20 progress, and a one-time `codex_reward_moonlit_almanac` claim. The
  claimed reward uses one runtime price function to reduce ordinary crop-seed
  coin prices by 10%, rounded up, in both the farm stall and Twilight Emporium.
  Saves remain schema v1 with one-time evidence-based legacy backfill. Validation
  passed 492/492 tests, 1399/1399 locale-key parity, Godot import/headless
  startup, diff-check, and Apple M5 / Metal captures 315–320. This completes
  only the crop category and first major collection reward: the full compendium
  is 1/8 categories and Phase F remains incomplete.
- 2026-08-21 — Added Phase F's twenty-second slice: date-derived Rainveil and
  Starharvest world aspects across Whispering Woods, Starfall Meadow,
  Glimmering Village, Crystal Vale, Moonwater Wetlands, and Starfall Ruins.
  Existing loaded chunks refresh their ground palette and strict 4×4 prop atlas
  in place at day 14/15, 28/29, and 42/43 boundaries; resource positions,
  collision, depletion, drops, weather, movement, and schema-v1 saves are
  unchanged. Validation passed 482/482 tests, 1376/1376 locale-key parity,
  Godot import/headless startup, hard atlas checks, and Apple M5 / Metal
  captures 310–314 including rain/tree and stardust/crystal target outlines.
  This closes the two explicit world-aspect gaps, not the remaining fish,
  Firefly Tide, three Starlights, Stargate, full compendium, or Phase F.
- 2026-08-21 — Added Phase F's twenty-first slice, expanding the village from
  twelve to sixteen NPCs and reaching the planned 16–20 relationship-content
  minimum. Yvara, Brial, Pavri, and Roven each have stable IDs, full ordinary/
  Lanternrest/conditional schedules, catalogued gift preferences, unique
  anchors and dialogue in all three implemented festivals, two 25/60
  three-page relationship events, and a one-time reward letter delivered the
  day after the second event. A separate original 4×4 directional atlas is
  resolved by stable NPC ID; legacy twelve-NPC saves retain empty safe defaults
  without a schema migration. Validation passed 468/468 tests, 1376/1376
  locale-key parity, Godot import/headless startup, hard atlas checks, and Apple
  M5 / Metal captures 300–309. NPC item 5 now reaches its minimum at 16/16;
  Phase F remains incomplete because remaining seasonal content, the fourth main
  festival, three Starlights, the Stargate, and the compendium are still open.
- 2026-08-21 — Added Phase F's twentieth slice: two 25/60, three-page bilingual
  relationship events each for Elowen and Vessa, plus one-time next-day reward
  letters for Kael, Sela, Elowen, Vessa, and Orin after their second events.
  All 12/12 current villagers now have a complete two-stage event chain and at
  least one relationship reward letter. Preview and action share a real
  adjacency requirement; active events cannot be overwritten, and the catalogs
  validate increasing thresholds, distinct pages, and sender ownership.
  Validation passed 456/456 tests, 1278/1278 locale-key parity, Godot import and
  headless startup, diff-check, and Apple M5 / Metal captures 290–295. Saves
  remain schema v1; NPC count is still 12/16–20 and Phase F is not complete.
- 2026-08-21 — Added Phase F's nineteenth slice, expanding the village from
  eight to twelve NPCs. Halden, Mavea, Sivren, and Dorrik each have stable IDs,
  full-day/rest-day/conditional schedules, gift preferences, anchors and
  dialogue in all three implemented main festivals, two 25/60 relationship
  events, and a one-time reward mail delivered the day after the second event.
  NPC art now resolves three atlases by stable NPC ID and fails explicitly for
  unknown IDs or invalid rows instead of silently clamping to Kael. The four
  newcomers use an original 4×4 directional chroma/runtime atlas. A failed gift
  removal no longer marks an NPC as met. Validation passed 446/446 tests,
  1256/1256 locale-key parity, Godot import/headless startup, hard atlas checks,
  diff-check, and Apple M5 / Metal captures 280–286. Saves remain schema v1;
  NPCs are 12/16–20 and Phase F is not complete.
- 2026-08-20 — Added Phase F's eighteenth slice and third complete main
  festival, the Longnight Lantern Feast. The optional Longnight day-13 event
  opens 17:00–22:00 in an original independent scene with all eight current
  villagers. Its three-part Shared-Radiance Rite selects exactly two different
  cooked dishes and one real homestead gift, then atomically removes all inputs,
  adds the full return gift, writes one annual score/award/gift/rite result, and
  grants independent Lantern Knots. Four stall offers spend only that currency;
  the ritual never changes regional Starlight state. Original RGB backdrop and
  2×2 chroma/runtime prop art, seven deterministic playtests, and Apple M5 Metal
  bilingual/result/tool captures accompany 434/434 tests, 1160/1160 localization
  parity, Godot import/headless startup, hard asset checks, and diff-check. Saves
  remain schema v1; main festivals are 3/4 and Phase F is not complete.
- 2026-08-20 — Added Phase F's seventeenth slice, the Moonhearth Cottage
  Expansion (`cottage_second_upgrade`). It explicitly requires the first cottage
  upgrade and Homestead Workshop, atomically spends 960 coins, 32 lumenwood and
  14 crystal shards, and completes after four nights. The finished cottage
  activates a real kitchen and 24-slot ingredient pantry. Four stable recipes
  plan ingredients across pantry and backpack by quality, place outputs only in
  the backpack, restore energy when eaten, and leave both containers unchanged on
  every failure. Original upgraded-interior and 4×2 chroma/runtime kitchen art,
  seven deterministic playtests, and Apple M5 Metal bilingual/target/panel
  captures accompany 407/407 tests, 1105/1105 localization parity, Godot import
  and headless startup, hard asset checks, and diff-check. Saves remain schema v1;
  the second cottage upgrade is complete, while Phase F is not.
- 2026-08-20 — Added Phase F's sixteenth slice and third complete Starlight,
  Meadow Harmony. Its flower and meadow-bounty nodes accept distinct stable item
  families, while the festival-echo node derives completion from an existing
  Gleamrise Planting Festival or Starharvest Market yearly result without copying
  or consuming festival currency. Restoration extends Glowcomb Hive pollination
  from four to six Manhattan tiles while keeping the two-night, one-honey cycle.
  The original 1254×1254 2×2 atlas, five deterministic playtests, and Apple M5
  Metal bilingual/state captures accompany 399/399 tests, 1057/1057 localization
  parity, Godot import/headless startup, and hard asset checks. Saves remain
  schema v1; Starlights are 3/6 and Phase F itself is not complete.
- 2026-08-20 — Added Phase F's fifteenth slice and second complete main
  festival, the Gleamrise Planting Festival. The optional Gleamrise day-4 event
  has an independent scene, all eight existing villagers, a 12-plot temporary
  planting challenge with frozen harmony/time scoring, persistent annual
  attempts and results, three awards, separate Bloom Tokens, and four atomic
  seed exchanges. Original 1536×1024 background and 2×2 prop art, real-object
  previews, six deterministic playtests, and Apple M5 Metal bilingual/state
  captures accompany 388/388 tests, 1044/1044 localization parity, Godot
  import/headless startup, and hard asset checks. The Phase C sowing-festival
  item is complete; Phase F festivals are 2/4 and Phase F itself is not.
- 2026-08-20 — Added Phase F's fourteenth slice, the Starwoven Husbandry Hub.
  The sixth stable construction project explicitly requires the workshop, coop,
  and barn, then activates one real console per animal building after four nights.
  Each building owns an independent 28-fodder/12-product schema-v1 buffer; nightly
  auto-feed and auto-collection are whole-building atomic operations and never
  pet, grant passive XP, sell, or draw from the backpack. An original 2×2 atlas,
  four deterministic playtests, and Apple M5 Metal bilingual captures accompany
  370/370 tests, 985/985 localization parity, Godot import/headless startup, and
  hard asset checks. Phase F's animal item is complete; Phase F itself is not.
- 2026-08-20 — Added Phase F's thirteenth slice, Dewhorn and its Condensed
  Dewmilk station. The existing Moonfleece Barn now registers two catalog-fixed
  starters and assigns distinct indoor/pasture projections. Both species share
  fodder, petting, mood and night resolution, while fleece and milk collection
  remain separate atomic product-family transactions. An original 8×4
  juvenile/adult/product-ready atlas, three milk qualities, two deterministic
  playtests, and Apple M5 Metal bilingual captures complete the third species.
  Regression reached 360/360 plus Godot import/headless startup and hard asset
  checks. Saves remain schema v1; animals are 3/3, but barn automation and Phase F
  remain incomplete.
- 2026-08-20 — Added Phase F's twelfth slice, the Moonfleece Barn and first
  sheep. A fifth stable construction project requires the completed Starfeather
  Coop, while the shared `AnimalSystem` now drives catalog-defined starter
  instances, building spaces, pasture assignments, facility interactions, and
  night resolution. Original waterside three-state facade, independent interior,
  juvenile/short-fleece/full-fleece directional art, three fleece qualities, and
  capacity-safe rack collection complete the loop. Regression reached 354/354,
  plus Godot import/headless startup, hard asset checks, and five Apple M5 Metal
  bilingual/state captures. Saves remain schema v1, animals are 2/3, and Phase F
  remains incomplete.
- 2026-08-20 — Added Phase F's eleventh slice, the Starfeather Coop and first
  chicken. A fourth stable construction project opens an independent interior;
  original juvenile/adult four-direction art supports clear-day grazing and
  rainy/Longnight sheltering. `AnimalSystem` owns atomic feeding, petting,
  growth, mood, three egg qualities, and capacity-safe nest collection through
  an additive schema-v1 root. Legacy approach objects remain preserved without
  sealing the coop door, exhausted pasture keeps the chicken indoors, and the
  current save contract accepts only the single projected starter instance.
  Regression reached 346/346 plus headless Godot, hard asset checks, and six
  Apple M5 Metal bilingual/state captures. Animals are 1/3 and Phase F remains
  incomplete.
- 2026-08-20 — Added Phase F's tenth slice and first complete main festival,
  Starharvest Market. The optional day-11 event has an independent scene, all
  eight existing villagers, a three-family crop/artisan showcase with frozen
  score and auction rules, persistent annual results and Market Scrip, four
  atomic shop offers, true-object previews, and original backdrop/2×2 prop
  art. Full regression reached 334/334 plus headless Godot and six Apple M5
  Metal bilingual/state captures. Phase F festivals are 1/4 and Phase F remains
  incomplete.
- 2026-08-20 — Added Phase F's ninth slice and second complete Starlight,
  Homestead Harvest. The single-pedestal state is now a stable-ID portfolio
  with the woodland legacy root preserved; three atomic crop/artisan/building
  nodes, a true-object preview, an original 2×2 atlas, and permanent eight-tile
  outdoor sprinkler coverage form the full loop. Regression reached 313/313
  plus headless Godot and Apple M5 Metal bilingual/state/tool captures. Phase F
  now has 2/6 Starlights and remains incomplete.
- 2026-08-20 — Added Phase F's eighth slice, Longnight Snow Weather: a stable
  weather ID, frozen 14-day natural pattern, fixed-snow forecast correction,
  15% outdoor movement penalty, interior exceptions, HUD effect text, an
  original 4×2 snowflake/gust/icon atlas, and four deterministic playtests.
  Valid same-day legacy weather stays authoritative with no schema-v1 field
  changes. Full regression reached 306/306 plus headless Godot and four Apple
  M5 Metal captures; animals, festivals, and Phase F remain incomplete.
- 2026-08-20 — Added Phase F's seventh slice, Longnight Frostbound Planting:
  a date-derived Longnight homestead skin, shared preview/action rules that
  block new outdoor planting on days 43–56 without harming existing crops,
  climate-controlled greenhouse planting, and two intentional four-seed
  Emporium rotations. Added bilingual feedback, two deterministic playtests,
  boundary/atomicity tests, and Apple M5 Metal captures. Snowfall weather,
  animals, festivals, and Phase F as a whole remain incomplete.
- 2026-08-20 — Added Phase F's sixth slice: the Moondew Greenhouse restoration
  loop with a Homestead Workshop prerequisite, atomic commissioning,
  four-night progress, three facade states, a separate interior, 24
  cross-season planting cells, the Moondew Cistern, an additive schema-v1
  greenhouse farm root, bilingual previews, and Apple M5 Metal captures.
  Outdoor rain does not water indoor soil and weather resonance cannot trigger
  there; Longnight and Phase F as a whole remain incomplete.
- 2026-08-20 — Added Phase F's fifth slice by replacing the single-project
  construction state with a stable-ID portfolio and proving it with the
  Homestead Workshop: atomic payment, three-night progress, additive schema-1
  compatibility, a generated 2×2 state atlas, a true-object preview, a
  two-project panel, eight focused domain tests, and Apple M5 Metal captures.
  Phase F remains incomplete.
- 2026-08-20 — Added Phase F's fourth slice: a date-derived Starharvest
  homestead skin that preserves the registered farm layout, remains separate
  from weather, and adds no save state. Rainveil and Gleamrise visual
  regressions remain covered; this single-location skin is not a complete
  Starharvest environment.
- 2026-08-20 — Added Phase F's third slice with four Starharvest-only crops:
  Auric Shoot, Sunvault Gourd, Crownstar Saffron, and three-night-regrowing
  Amberthread Cluster. The catalog now has twenty crops and reuses seasonal
  stock, preview/atomic action, quality, shipping, and schema-v1 save rules;
  it adds an original registered 6×4 atlas, focused tests, and an Apple M5
  Metal capture without claiming Phase F complete.
- 2026-08-20 — Added Phase F's second slice: a date-derived Rainveil homestead
  skin that remains separate from weather and adds no save state. This single
  location skin does not represent a complete Rainveil environment.
- 2026-08-20 — Began Phase F with four Rainveil-only crops: Ripplecap,
  Tideglass Taro, regrowing Lantern Reed, and Rainveil Lotus. The increment
  reuses seasonal preview/action, quality, inventory, shipping, and save rules;
  adds seasonal Emporium rotation, an original registered 6×4 atlas, focused
  tests, and a macOS Metal playtest capture without claiming the whole season.
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
