# Verification

## Automated acceptance

- `openspec validate add-layered-terrain-brush-composition --strict`: passed.
- `openspec validate --all --strict`: 34 passed, 0 failed.
- `FruitDefense.Editor.ProjectSetup.SmokeValidate`: passed with
  `FRUIT_DEFENSE_LAYERED_MAP_OK`, `FRUIT_DEFENSE_LAYERED_TERRAIN_TILEMAP_OK`,
  `FRUIT_DEFENSE_BATTLEFIELD_DUAL_GRID_TERRAIN_OK maps=3`, and
  `FRUIT_DEFENSE_SMOKE_OK` in `Builds/Evidence/layered-terrain-smoke-final.log`.
- The authoring smoke checks all compatible horizontal and vertical RGBA sockets for the two
  generic landforms and the two AI edge directions. The accepted maximum boundary difference is
  zero. It also checks pure-base clearing, A on B, B on A, missing-direction refusal, Undo and
  unchanged incremental refresh.

## WebGL acceptance

- The non-release portrait demo built successfully to `Builds/LayeredTerrainWebGL`.
- The ordinary release built successfully to `Builds/WebGL` with Brotli fallback, high managed
  stripping, content version `23c565a32083`, and the unchanged scene order
  `Bootstrap -> Lobby -> Battle -> Settlement`.
- Real browser canvas evidence:
  - `Builds/Evidence/layered-terrain/webgl-layered-terrain-demo-final.png`
  - `Builds/Evidence/layered-terrain/webgl-release-battle-full.png`
  - `Builds/Evidence/layered-terrain/layered-terrain-seam-board.png`
- Visual inspection covered pure soil, pure grass, grass on soil, soil on grass, AI edge on/off,
  convex and concave contacts, isolated shapes, holes, diagonal masks 5/10, route/core markers,
  pots, controls, clipping and portrait safe-area readability.
- No asset, shader, script or rendering error appeared. Desktop WebGL reported only the expected
  unsupported orientation-lock message and Unity's existing persistent-data synchronization
  deprecation warning.

## Art provenance

The complete AI edit prompts and packaging boundary are retained in
`Assets/LayeredTerrain/GrassSoil/Sources/ai-edge-prompts.md`. AI owns the interior painted contact
ribbon; protected byte-identical outer socket samples own cross-tile continuity. There is no
runtime raster or edge-processing step.

## Documentation gate

This is a major design change. `docs/design/game-design-overview.md` was intentionally not edited;
design-document synchronization remains pending explicit user confirmation.
