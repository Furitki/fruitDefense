## Why

The current battle view hides the planting grid at rest, renders equipped plants with the unchanged base plant plus a tiny badge, can expose a temporary vector projectile trail, and scales a low-resolution attack-range texture. These gaps make pot layout, equipment evolution, attack feedback, and direct formation editing harder to read than the underlying rules intend.

## What Changes

- Restore a restrained, always-visible preview grid over plantable map cells while preserving stronger legal/illegal feedback during pot placement.
- Render an equipped plant through the existing full-size equipment evolution resource instead of leaving the base plant as the dominant form with a tiny badge.
- Remove the temporary vector attack trail and keep transient attacks on the existing authored projectile/effect atlas path.
- Replace the fixed 128-by-128 range texture with a viewport-appropriate high-resolution overlay while preserving the simulation-derived center and radius.
- Allow dragging an on-board plant onto an incompatible occupied on-board pot to swap the two plants directly; compatible same-kind, same-star drops continue to merge.
- Preserve combat balance, content counts, persistence format, cooldown rules, map topology, hit rectangles, and platform adapters.

## Capabilities

### New Capabilities
- `plant-combat-resource-presentation`: Covers full-size equipment evolution resources and resource-backed transient plant attack effects.

### Modified Capabilities
- `battlefield-dual-grid-terrain-presentation`: Restore a subtle plantable-cell preview grid at rest and keep active placement feedback visually stronger.
- `plant-selection-inspection`: Add occupied-pot plant swapping and require a crisp attack-range overlay at supported portrait resolutions.

## Impact

- Runtime presentation and input: `Assets/Scripts/FruitDefenseGame.cs`.
- Plant drop legality and relocation: `Assets/Scripts/Core/GameSimulation.cs` and `Assets/Scripts/Core/GameModel.cs`.
- Editor smoke coverage under `Assets/Editor/Tests/` and the aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` suite.
- Ordinary WebGL portrait acceptance for idle grid, equipped forms, attack effects, range clarity, and plant swapping.
- No new package dependency, persistence migration, scene-flow change, or mini-game platform claim.
