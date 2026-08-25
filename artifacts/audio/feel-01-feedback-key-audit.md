# FEEL-01 feedback failure-key audit

- Date: 2026-08-24 20:50 CST
- Checkout: `/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- Scope: `ImmediateFeedbackPresenter` failure classification only
- Excluded: free `BuildingSystem`, preset `ConstructionSystem`, localization, README, `Main`, `PixelAudio` and `PixelAudioProfile`

## Classification rule

`ImmediateFeedbackPresenter` now routes only explicit known keys into semantic outcomes:

- `ToolMismatchKeys` -> `ImmediateFeedbackOutcome.ToolMismatch` -> `PixelSound.ToolMismatch`
- `ResourceBlockedKeys` -> `ImmediateFeedbackOutcome.ResourceBlocked` -> `PixelSound.ResourceBlocked`
- unknown or ordinary failures -> `ImmediateFeedbackOutcome.Failure` -> `PixelSound.Error`

The old broad substring checks for values such as `insufficient`, `missing`, `capacity`, `wrong_tool`, `tool_required` and `needs_tool` were removed so that new unknown failures do not silently become resource or tool failures.

## Added tool-mismatch coverage

- `mining.requires_bronze_star_shovel`
- `target.need.bucket`
- `target.need.bucket_or_rod`
- `target.need.hand`
- `target.need.machete`
- `target.need.seed`
- `target.need.shovel_mine`
- `target.need.shovel_till`
- `target.need.watering_can`
- `target.need.weapon`

The existing `notice.needs_*`, `deep_mine.shovel_tier_low` and `combat.requires_weapon` mappings remain tool mismatch.

## Added resource or capacity coverage

- `animal.automation.feed_capacity`
- `animal.automation.no_feed_stored`
- `animal.feed.insufficient_fodder`
- `collection.donation.missing_item`
- `collection.reward.inventory_full`
- `cooking.backpack_full`
- `crafting.backpack_full`
- `festival.shop.backpack_full`
- `fishing.gear.bait_missing`
- `fishing.gear.materials_missing`
- `kitchen.pantry.full`
- `kitchen.pantry.none_stored`
- `mail.notice.backpack_full`
- `notice.no_chest_item`
- `notice.no_seed`
- `notice.water_full`
- `notice.watering_can_empty`
- `shop.not_enough_coins`
- `storage.chest_full`
- `storage.none_in_chest`
- `tool.upgrade.insufficient_materials`
- `village.gift.missing_item`

The existing `notice.no_energy`, `notice.needs_water`, inventory/backpack full, missing ingredients, bait, sapling and placeable-item mappings remain resource blocked.

## Representative ordinary failures kept generic

- `combat.attack.cooldown`
- `collection.reward.already_claimed`
- `fishing.crab_pot.occupied`
- `fishing.gear.level_locked`
- `mail.notice.already_claimed`
- `notice.not_water_source`
- `notice.nothing_to_interact`
- `processor.busy`
- `weekly_commission.not_ready`

Synthetic unknown keys containing broad resource/tool-looking words are also covered by tests and remain generic failure:

- `unknown.missing_supply`
- `unknown.insufficient_signal`
- `unknown.needs_tool_that_does_not_exist`

## Validation boundary

- Focused `ImmediateFeedbackPresenterTests`: 212/212 passed.
- Human sound-strength listening remains pending.
