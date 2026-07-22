## Why

The production battle scene still renders the board as flat colored rectangles even though the project now has a validated `StoneFloor` Dual-Grid terrain set. Players cannot see the authored two-material transition effect in the actual Lobby → Battle → Settlement flow, so the generated terrain needs a deterministic runtime presentation path.

## What Changes

- Bind the generated `PixelGrassDualGridTileSet`, `StoneFloorDualGridTileSet`, and PixelGrass soil texture to the release Battle scene.
- Render a soil base, a clipped grass overlay derived from plantable cells, and a clipped stone-road overlay derived from route/entry/exit cells before core, pots, plants, enemies, and effects.
- Replace the procedural beige route fill with the authored StoneFloor layer while retaining entry and exit markers.
- Reuse the existing four-corner mask contract and the shared battlefield projection so all three bundled maps receive aligned grass and road transitions without adding authored per-map tile choices.
- Preserve route rendering, plantable-cell interaction feedback, hit targets, simulation, balance, snapshots, and release scene flow.
- Add editor smoke coverage for runtime asset binding, mask-to-map resolution, sprite availability, projection alignment, clipping bounds, and multi-level compatibility.

## Capabilities

### New Capabilities

- `battlefield-dual-grid-terrain-presentation`: Runtime binding and deterministic presentation of plantable grass, stone monster routes, and their soil base on the canonical battlefield grid.

### Modified Capabilities

None.

## Impact

- Runtime presentation: `Assets/Scripts/FruitDefenseGame.cs` and a focused terrain presentation utility under `Assets/Scripts/Tilemaps`.
- Runtime assets and scene wiring: the existing PixelGrass and StoneFloor generated TileSets, PixelGrass soil source texture, and `Assets/Scenes/Battle.unity`.
- Editor setup and validation: `Assets/Editor/ProjectSetup.cs` plus a dedicated smoke validator.
- No gameplay, level topology, persistence, reward, economy, platform-adapter, or scene-flow behavior changes.
