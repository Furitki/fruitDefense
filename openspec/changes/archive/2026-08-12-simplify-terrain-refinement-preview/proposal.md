## Why

The embedded terrain laboratory still exposes an implementation-level choice between a bare contour and an accepted refined edge, while its pair-preset thumbnails show only one material and do not resemble the composed result. Its Scene hover cell also updates only when Unity happens to repaint, so the authoring cursor can lag behind or remain on an old cell.

## What Changes

- Remove the base-edge versus AI-refined-edge choice from the terrain laboratory and make every landform-bearing brush require and apply the exact directed refined edge asset.
- Replace pair-preset swatches with representative composites drawn from the real base tile, active contour TileSet, and directed refined-edge TileSet; pure presets continue to preview their real base material.
- Keep Scene feedback lightweight: show only the active cell outline and label, request mouse-move events while painting, repaint the current Scene view when the hovered cell changes, and clear stale hover state at panel/window/session boundaries.
- Migrate editor fixtures and accepted laboratory content away from optional bare-edge authoring without changing gameplay, persistence, runtime terrain resolution, release scenes, or player-visible UI.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `layered-terrain-painter-workflow`: Landform-bearing presets become refined-only, pair cards preview the real composition, and the lightweight Scene cell indicator must follow pointer movement without stale state.
- `dual-grid-tilemap-authoring`: The developer painter no longer exposes an optional second-pass edge toggle; supported ordered pairs require their accepted directed refinement.

## Impact

The change is editor-only and primarily affects `LayeredTerrainPainterWindow`, `LayeredTerrainPaintSession`, preview helpers around `LayeredTerrainTilemap`/`DualGridTileSet`, editor smoke coverage, and terrain-laboratory evidence. Existing low-level terrain serialization remains compatible, but ordinary authoring no longer creates unrefined pair regions. The runtime `Bootstrap → Lobby → Battle → Settlement` flow and ordinary WebGL output are unchanged.
