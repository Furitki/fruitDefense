## Why

Selecting a `DualGridTileSet` through a single object field is slow and gives authors no visual indication of how each terrain looks. The project already contains multiple reusable tile sets, so the `DualGridTilemap` Inspector should discover them automatically and make the active layer style selectable like a compact brush gallery.

## What Changes

- Add an editor-only TileSet gallery beneath the existing `DualGridTilemap` Tile Set field.
- Discover all project `DualGridTileSet` assets automatically, sort them deterministically, and refresh the cached list when project assets change.
- Render a representative pixel-preserving transition preview and name for every discovered TileSet, including selected and invalid states.
- Let an author select a valid card with one click, record Undo, assign it to the current component, rebuild the generated layer immediately, and keep the existing manual terrain paint mode available.
- Retain the object field as an explicit fallback and keep TileSet selection scoped to the whole Dual-Grid layer.
- Add editor smoke coverage for discovery, preview readiness, deterministic ordering, assignment, and rebuild behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `dual-grid-tilemap-authoring`: TileSet configuration gains an automatically discovered visual gallery and one-click whole-layer selection workflow in the component Inspector.

## Impact

- Primarily affects `Assets/Editor/DualGridTilemapEditor.cs` and `Assets/Editor/DualGridTilemapSmoke.cs`.
- Uses existing Unity editor APIs and current `DualGridTileSet` assets; no package or runtime dependency is added.
- Does not add per-cell terrain identities, change mask resolution, alter runtime rendering, modify saved gameplay data, or change the `Bootstrap → Lobby → Battle → Settlement` release flow.
