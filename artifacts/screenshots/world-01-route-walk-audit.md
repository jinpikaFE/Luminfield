# WORLD-01 route walk audit

- Date: 2026-08-24
- Worktree: `/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- Branch: `codex/base-01-playtest-bindings`
- Base HEAD: `5696a6fb128ee44cadcb3bd9a944d226cb86156c`
- Source: `WorldNavigationRouteAuditor.AuditAll()`
- Scope: read-only route verification from `WorldNavigationGuideCatalog`
  and `WorldDefinition`; no collision, save, resource, Main, localization,
  BuildingSystem, or ConstructionSystem changes.

## What this proves

- Every route contract has one continuous four-direction walkable path from
  its start cell to its end cell.
- Every cell in each computed path is in bounds and not blocked according to
  `WorldDefinition`.
- Every visible guide stop used by a route is also a `WorldDefinition.IsPath`
  cell and is not blocked.
- Every guide-to-guide interval stays within the current 18-tile camera
  discovery budget.

## What this does not prove

- This automatic audit is not a human wayfinding or getting-lost test.
- It does not replace a real controller walk, a new-player route read, or a
  visual judgement of whether every non-guide walkable cell looks like a road
  in the generated background.
- It does not validate Windows/Linux runtime behavior.

## Route summary

| Route | Region | Start | End | Path length | Turn points | Max guide gap | Guides |
| --- | --- | --- | --- | ---: | --- | ---: | ---: |
| `route_home_to_lumen_village` | Home -> LumenVillage | `(19,30)` | `(128,80)` | 159 | `(19,48)`, `(62,48)`, `(62,51)`, `(68,51)`, `(68,58)`, `(72,58)`, `(72,80)` | 18 | 13 |
| `route_lumen_village_to_whispering_woods` | LumenVillage -> WhisperingWoods | `(128,80)` | `(42,168)` | 174 | `(42,80)` | 18 | 13 |
| `route_lumen_village_to_starfall_meadow` | LumenVillage -> StarfallMeadow | `(128,80)` | `(136,24)` | 64 | `(128,25)`, `(136,25)` | 17 | 6 |
| `route_lumen_village_to_crystal_vale` | LumenVillage -> CrystalVale | `(128,80)` | `(84,154)` | 126 | `(128,142)`, `(80,142)`, `(84,142)` | 18 | 10 |
| `route_lumen_village_to_moonwater_wetlands` | LumenVillage -> MoonwaterWetlands | `(128,80)` | `(226,48)` | 138 | `(172,80)`, `(172,79)`, `(176,79)`, `(176,80)`, `(227,80)`, `(227,69)`, `(226,69)`, `(226,68)`, `(224,68)`, `(224,56)`, `(226,56)` | 18 | 11 |
| `route_lumen_village_to_starfall_ruins` | LumenVillage -> StarfallRuins | `(128,80)` | `(170,159)` | 123 | `(128,154)`, `(136,154)`, `(136,158)`, `(159,158)`, `(159,159)`, `(160,159)`, `(160,160)`, `(170,160)` | 18 | 9 |

## Validation

- `WorldNavigationGuideCatalogTests|WorldNavigationRouteAuditorTests`: 12/12
  passing after adding the route auditor.
- The first strict visual-road-only pass exposed that the route contract relies
  on walkable ground between some visual guide stops; the final audit therefore
  treats continuous traversal as real passability while keeping guide stops on
  visual road cells.
