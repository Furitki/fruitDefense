## Why

The current terrain pipeline can technically render a base-only visual cell, but authors do not have an explicit, guarded workflow for treating that cell as a clean square terrain tile alongside Dual-Grid landforms. A first production-shaped trial is needed now to compare the simpler cell-aligned look without creating fake sixteen-mask tiles or introducing seams and false internal borders.

## What Changes

- Add explicit grass-square and soil-square authoring presets that write a base surface and clear landform, contour, and pair-edge data.
- Keep pure square terrain on the existing opaque base layer and keep Dual-Grid terrain on the existing transparent landform/edge layers; do not add a parallel map schema or a sixteen-mask square-tile substitute.
- Reject a same-surface boundary where one side represents the surface as a pure base and the touching side represents it as a Dual-Grid landform, because the current mask resolver would create a false internal contour.
- Add a compact grass/soil trial layout and focused validation/preview evidence covering pure squares, Dual-Grid regions, and their supported separation.
- Preserve Battle interaction geometry, simulation, map identity, route rules, persistence, runtime UI semantics, and the current clean orchard visual standard.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `layered-terrain-brush-authoring`: Make base-only square presets explicit and define the guarded authoring rule for same-surface base/Dual-Grid representation boundaries.
- `battlefield-dual-grid-terrain-presentation`: Define how opaque square cells and transparent Dual-Grid layers coexist in one canonical map and how the trial is validated at runtime scale.

## Impact

- Affected systems: layered terrain editor tooling, canonical visual-cell validation, terrain-focused editor smoke, and terrain preview/evidence generation.
- Player-visible flow: Battle terrain presentation only; no Lobby, controls, actions, copy, or settlement behavior changes.
- Runtime rendering continues to use the current base-first, landform-second, edge-third order and current palette bindings.
- No new dependency, gameplay rule, saved-state migration, compatibility layer, or platform claim is introduced.
