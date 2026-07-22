## Why

The release flow carries a `LevelId`, but every launch still constructs the same default battlefield and battle content, so the Lobby cannot offer meaningful level choice and retry, settlement, or snapshot restoration cannot prove which authored level was actually played. After the P0 tile-grid route contract is complete, the game needs one validated level catalog that resolves a stable level identity into its map, waves, rules, and presentation theme.

## What Changes

- Add immutable, semantically identified level definitions and a catalog that resolves `LevelId` to `mapId`, `waveSetId`, `ruleSetId`, and `themeId` without silently falling back to the default map.
- Author three selectable bundled levels: a U-shaped teaching route, an S-shaped coverage route, and a core-corridor route with boss pressure.
- Make the Lobby expose all three levels and launch the currently selected level through the existing `Bootstrap → Lobby → Battle → Settlement` flow.
- Preserve the selected level identity through battle initialization, snapshots, settlement, return-to-Lobby state, and retry; reject missing or mismatched catalog identities deterministically.
- Extend editor smoke validation and real portrait WebGL acceptance to cover catalog validity, level selection, distinct route topology, identity preservation, and retry behavior.
- Defer implementation until `refactor-battlefield-route-to-tile-grid` has supplied the canonical per-cell tile and ordered-route contract; do not duplicate or bypass that P0 framework.
- Keep long-term progression, currencies, rewards, unlock economy, and chapter-map structure out of scope.

## Capabilities

### New Capabilities

- `level-map-catalog`: Validated level/map/wave/rule/theme definitions, deterministic lookup, and the three bundled level configurations built on the P0 tile-grid route contract.
- `level-selection-flow`: Lobby selection and end-to-end preservation of the selected level identity through launch, snapshot, settlement, return, and retry.

### Modified Capabilities

None.

## Impact

- Adds level and map catalog contracts near the existing content and core map definitions; battle construction must receive resolved definitions instead of creating the default map internally.
- Extends bundled content so wave sets and rule sets can be addressed independently while retaining stable semantic IDs and deterministic validation.
- Updates Lobby layout/presentation, shell flow coordination, battle session construction, snapshot identity checks, settlement/retry handling, and their validation suites.
- Depends on `refactor-battlefield-route-to-tile-grid`; P1 implementation must start only after that change is implemented and validated.
- Requires `FruitDefense.Editor.ProjectSetup.SmokeValidate`, WebGL build validation, and real portrait canvas captures for the three selectable routes. It does not authorize Douyin or WeChat support.
