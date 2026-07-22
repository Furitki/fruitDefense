## Why

Building terrain transitions by hand currently requires authors to select and maintain many edge and corner tiles. A reusable Dual-Grid layer can derive those transitions from a small logical occupancy map, so maps are faster to paint and less likely to contain disconnected or mismatched tiles.

## What Changes

- Add a reusable Dual-Grid Tilemap component that samples four logical cells into a deterministic 4-bit mask and writes the corresponding visual tile.
- Add a 16-slot terrain tile-set asset, with validation and explicit corner-bit conventions, so ground, road, and wall layers can each use the same renderer with different art.
- Refresh generated tiles automatically while painting in the Unity Editor and expose full-rebuild and local-cell refresh APIs for runtime changes.
- Add an editor-created demo scene and procedural placeholder tiles that exercise edges, inner/outer corners, holes, and incremental repainting without requiring final art.
- Add editor smoke validation for all masks, bounds, offset alignment, full rebuilds, and incremental updates.
- Add a reproducible final-art bake profile that combines seamless terrain materials with pixel-distance edges, four-times supersampling, deterministic grass tufts, topology-safe diagonal states, and automated seam evidence.
- Extend the developer demo with an underlying soil layer and a ready-to-use Scene-view paint/erase workflow for manual art acceptance.
- Keep the release scene flow, gameplay topology, persistence, combat balance, and current battlefield presentation unchanged; the demo is a developer-facing authoring surface until a later map-migration change adopts it.

## Capabilities

### New Capabilities

- `dual-grid-tilemap-authoring`: Defines logical-to-visual mask resolution, tile-set configuration, editor/runtime refresh behavior, demo authoring, and validation.

### Modified Capabilities

None.

## Impact

- Adds runtime code under `Assets/Scripts/Tilemaps` and editor tooling/tests under `Assets/Editor`.
- Adds generated demo assets and `Assets/Scenes/DualGridDemo.unity`; release build scene order remains `Bootstrap → Lobby → Battle → Settlement`.
- Adds editor-only terrain bake configuration and generated grass/soil test art; generated output remains replaceable and excluded from release-scene adoption.
- Uses the existing Unity Tilemap module only; no package, gameplay-save, platform-adapter, or content-catalog changes are required.
