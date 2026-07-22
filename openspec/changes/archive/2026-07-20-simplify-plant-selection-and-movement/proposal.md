## Why

Plant selection currently doubles as the first step of a click-to-place, click-to-move, or click-to-merge workflow, so a player who only wants to inspect a plant can accidentally change the formation. Selection should be observational, while spatial changes should use the already visible drag-and-drop interaction.

## What Changes

- Make a click on an on-board plant select it for inspection only.
- Show the selected plant's information surface and an accurate battlefield attack-range overlay; support plants with zero attack range show information without a misleading range circle.
- Make a click on a nursery plant show its information without creating a pending click-to-place action.
- Remove click-selected plant placement, movement, return-to-nursery, and merge behavior from flowerpot and nursery-slot clicks.
- Preserve drag-and-drop planting, movement, return-to-nursery, and merging, while keeping target highlights and limiting the floating text hint to a compact legal-merge cue.
- Preserve weapon and flowerpot tool interactions; this change only removes plant relocation caused by a prior plant click.
- Add smoke and WebGL interaction evidence proving that clicks inspect and drags relocate.

## Capabilities

### New Capabilities

- `plant-selection-inspection`: Inspection-only plant selection, attack-range presentation, information display, and drag-only plant relocation semantics.

### Modified Capabilities

None. The repository has no promoted baseline specifications for this behavior.

## Impact

- Selection, click handling, range rendering, information presentation, and drag targeting in `Assets/Scripts/FruitDefenseGame.cs`.
- Geometry supplied by `restructure-battlefield-map-and-tiles` is used to project the range overlay consistently on the resized board.
- Editor smoke and live WebGL acceptance gain explicit click-versus-drag scenarios.
- Plant stats, movement cooldowns, merge rules, combat balance, and persistence behavior do not change.
