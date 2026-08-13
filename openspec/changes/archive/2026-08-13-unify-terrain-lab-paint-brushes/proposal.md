## Why

The terrain laboratory currently separates registered resource selection from the actual A-on-B / B-on-A paint choice, forcing authors through two selectors and allowing the original square grass/soil terrain shown by the initial laboratory target to disappear because it is target configuration rather than a registered brush resource.

## What Changes

- Replace the terrain laboratory's resource-card-then-direction workflow with one unified gallery of directly paintable directional brush tiles.
- Present the directional tiles as a compact four-column gallery so more registered brushes remain visible without enlarging the Overlay.
- Expand every registered terrain brush resource into its two reciprocal paint directions; selecting either tile configures the target and activates that exact direction in one action.
- Register the original square grass/soil laboratory terrain through the same brush-definition and registry path as newer composite resources so its artwork remains visible and selectable.
- Keep advanced erase/clear operations separate, while removing the duplicate ordinary A/B preset selector and its contextual second selection step.
- Keep the laboratory Overlay expanded while the tool is active so selecting a brush or returning focus to the Scene does not minimize the selector.
- Separate structural authoring readiness from strict whole-canvas consistency so recoverable legacy or partial edge data cannot lock the brush gallery.
- Preserve the existing layered terrain data model, Undo behavior, explicit Scene Overlay teardown, runtime rendering, gameplay rules, persistence, and release scene flow.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `layered-terrain-painter-workflow`: Registered resources become direct directional paint choices, with one resource contributing two reciprocal brush tiles and the original terrain family retained in the same gallery.
- `layered-terrain-brush-authoring`: Brush registration becomes the single source for laboratory paint choices and supports both imported composite resources and the original authored terrain family.

## Impact

- Affects the terrain laboratory Scene Overlay, terrain brush registry/application helpers, layered-terrain authoring validation, brush-definition validation/metadata, original layered-terrain asset setup, and focused editor smoke coverage under `Assets/Editor`.
- May add one production `TerrainBrushDefinition` asset for the original organic grass/soil family while preserving its existing textures, TileSets, GUIDs, and rendering assets.
- Does not change runtime gameplay, canonical map data, persistence, content catalogs, release scenes, or mini-game platform support.
