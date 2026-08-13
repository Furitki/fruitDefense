# Verification evidence

## Result

The change is accepted for the ordinary WebGL baseline. Square and organic contour identities coexist, the three bundled maps use explicit square contours, and gameplay fingerprints remain unchanged by presentation-only contour data.

This acceptance does not authorize Douyin or WeChat adapters.

## Deterministic art evidence

- Machine report: `Builds/Evidence/terrain-contours/square-contour-validation.json` (`result: pass`, validator `square-contour-soft-transition-v7`).
- Tile size: `256 x 256`; assembled atlas/board size: `1024 x 1024`; compatible sockets: `64/64` horizontal and `64/64` vertical with maximum RGBA difference `0`.
- Topology: isolated cell is a rounded square; strips, turns, holes, and diagonals are valid; masks `05` and `10` each retain two disconnected grass components while their painted outside-soil edge is one seamless component with no transparent center slit.
- Continuous imagegen-derived source SHA-256: `9594f3aef2c04de548246dd5ef8f5290e51e84a0d7db45bf8bdfbe7bceeba07a`.
- Shared light-tan base-soil SHA-256: `54de95aea887fb5e853d069f5d9f5186f8fa13a2ef985c3f1fb0c483b3c24019`.
- Topology guide SHA-256: `646e32b5929bb39e13ce3ffd2583a4990410302de5e878aa01ccb682dd194c15`.
- Grass landform SHA-256: `f15bac48b0a40c572e8bc1abef71b946092694e5b600f8a96fb949f914741fce`.
- Soil landform SHA-256: `0637485f0e79e54e7fdd57c526f5c9bbf7ab798634bf443491b40a608d5228a5`.
- Stone-road landform SHA-256: `e0309ea4e1251abc03c1680f6af300d220d1f3d3e34b5d776ed05ba4dd99f38d`.
- Painted grass-on-soil edge SHA-256: `36bfba5736295e7c2afa043b50ceed63f3ab6943c0a8b7129b0329922816feea`.
- The accepted painted edge keeps exact RGB for its grass lip, contact, exposed-soil body, and lower shadow from the retained continuous source. The seamless base texture is color-matched to the accepted light-tan edge palette: the measured maximum RGB mean delta is `1`, while the edge retains visible depth instead of flattening into the base. Per-column source-derived grass variation replaces the previous two fixed drip events. Deterministic contact and outer-shadow alpha ramps replace the former fully opaque cutoff; each non-empty mask retains at least `2,215` translucent pixels and `924` low-alpha outer-shadow pixels. Every non-empty mask has full boundary coverage and a 48-pixel median soil band.
- Visual boards: `square-contour-board.png`, `square-battle-scale-board.png`, `square-organic-coexistence-board.png`, and `organic-contour-board.png` under `Builds/Evidence/terrain-contours/`.

## Runtime and gameplay evidence

- The explicitly authorized production-brush pass imports A grass-soil and B stone-water byte-for-byte from `Builds/Evidence/dual-grid-stress-atlas-16x16-regression-20260730/` into `Assets/LayeredTerrain/CompositeBrushes/`. All `32` Runtime32 PNG hashes match their source files and both copied manifests are byte-identical to their source manifests.
- A is registered as the exact square refined `surface.grass` on `surface.soil` edge and uses Mask 15/00 as the grass/soil base endpoints. B is registered as the exact square refined `surface.stone-road` on `surface.water` edge and uses Mask 15/00 as the stone/water base endpoints. Water is a base-only surface; no water landform was synthesized.
- Source-manifest SHA-256 values are `4f0fa0b60c06386e8556e93f052e68da3115fff800c5f71d9f5f7e9aaa8b8d3e` for A and `854236c77619026dcf70b45f86ca7b70eee1ada1569fa820296c1c0b2871213d` for B. Their retained manifests continue to record `visualInspectionPerformed=false` and `seamSafetyClaimed=false`; installation does not upgrade either claim.
- `ProductionTerrainBrushSmoke` passed with `FRUIT_DEFENSE_PRODUCTION_TERRAIN_BRUSHES_OK`. It verifies all sixteen masks per family, deterministic Sprite import settings, base endpoint identity, exact palette direction, complemented reverse resolution, retained manifests, and both canonical-map editor shortcuts.
- Aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` passed after updating its deliberate missing-water fixture and current soil-base binding, with final markers `FRUIT_DEFENSE_PRODUCTION_TERRAIN_BRUSHES_OK`, `FRUIT_DEFENSE_BATTLEFIELD_DUAL_GRID_TERRAIN_OK`, and `FRUIT_DEFENSE_SMOKE_OK` in `Builds/Evidence/combined-workflow-trial-20260730-155939-derived-composite/evidence/unity-manual-review-pure-toggle.log`.
- The isolated `ProtectedHybrid` full-composite trial now binds mask `15` as its pure-grass endpoint and mask `0` as its pure-soil endpoint in both the terrain laboratory and trial Battle palette. Contextual pure-only preview and paint therefore remain in the selected brush's texture family instead of switching back to the production grass/soil thumbnails.
- The primary terrain-brush row now contains only the two directed composition brushes. The duplicate landform-only cards are removed from the ordinary chooser, while their low-level operations remain available for compatibility and tests.
- The ordinary terrain laboratory no longer exposes the `方形` / `自然` contour switch. Each target continues to use its preconfigured contour, while square/organic assets and low-level contour compatibility remain intact.
- Focused terrain-painter validation passed after the chooser cleanup with `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK` and `CODEX_TERRAIN_BRUSH_UI_DEDUP_OK`; the subsequent temporary-runner cleanup compiled successfully and restored the trial laboratory panel.
- Focused terrain-painter validation passed after the endpoint correction with `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK` and `CODEX_PROTECTED_TRIAL_PURE_ENDPOINTS_OK` in `Builds/Evidence/combined-workflow-trial-20260730-155939-derived-composite/evidence/unity-manual-review-pure-toggle.log`.
- The reusable brush-package pass makes pipeline-generated `BrushImport.json` the Unity registration contract. The generic importer creates or updates stable `TerrainBrushDefinition` assets, preserves the same asset GUID on repeated import, and supplies one shared registry to palette setup, canonical map authoring, and the terrain laboratory without A/B-specific source paths.
- The terrain laboratory now shows every registered definition simultaneously in one two-column, scrollable composition gallery. Each card assembles its real full-composite TileSet, identifies the pair, and mechanically reports grass-soil as dual-direction and stone-water as single-direction; a non-empty canvas still shows the complete library while refusing semantic reinterpretation.
- A clean Unity `6000.3.19f1` batch validation in an isolated temporary project passed the repeated-import identity check, `TerrainBrushRegistrySmoke`, and `LayeredTerrainPainterSmoke`. The retained log is `Builds/Evidence/terrain-brush-gallery-20260731/unity-focused-smoke.log` (SHA-256 `8727260e91a7548c74f416efe8a5058a5d351a888982fc70684e2aeb6b0e3f58`) with final markers at lines `1590`, `1976`, and `1988`.
- Pipeline unit coverage passed all `7` tests and strict OpenSpec validation passed all `40` current changes/specifications after the unified-gallery refinement. No WebGL build was run for this authoring-only pass, and the agent did not visually inspect the brush artwork.
- The clarity correction repackaged both accepted families under `Builds/Evidence/dual-grid-runtime64-20260731/`. All `16/16` Review256 masks for each family and both `Stress-All-1024.png` files are byte-identical to the accepted 32-pixel run; only descriptor-owned Runtime64 masks and their runtime atlas were added. The fixed 16×16 stress board continues to use Runtime32 and remains 1024×1024.
- Production grass-soil and stone-water now each contain `16` byte-identical Runtime64 masks at 64 PPU. Their obsolete production Runtime32 folders are absent, the persistent brush assets declare `runtimeTileSize: 64`, and repeated generic import retains the same definition GUID.
- Unified-gallery cards now center a square artwork rect above the footer instead of stretching a nominal 32-pixel sprite to unequal horizontal and vertical sizes. Focused layout coverage asserts equal width/height and centered bounds.
- Final clean Unity `6000.3.19f1` validation passed with `FRUIT_DEFENSE_TERRAIN_BRUSH_REGISTRY_OK`, `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK`, and `FRUIT_DEFENSE_TERRAIN_BRUSH_RUNTIME64_ONESHOT_OK` at lines `1795`, `2181`, and `2193` of `Builds/Evidence/terrain-brush-runtime64-20260731/unity-final-smoke.log` (SHA-256 `1b94f50feda706c8cd74d4af5664f9bd46e694fd3c657b6cb58dfa0b9f83c89f`). Pipeline unit coverage now passes `8` tests. No WebGL build or agent visual review was performed for this authoring-only correction.
- `FruitDefense.Editor.ProjectSetup.SmokeValidate` passed again after the diagonal-soil seam, shared-tan-soil, and soft-transition fixes with retained log `Logs/square-soil-soft-edge-project-smoke.log` and Editor markers `FRUIT_DEFENSE_LEVEL_MAP_CATALOG_OK`, `FRUIT_DEFENSE_MULTI_LEVEL_SIMULATION_OK`, and `FRUIT_DEFENSE_SMOKE_OK`.
- The P0 release suite passed with `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`; `Builds/Evidence/terrain-contours/final-validation.log` records the combined P0 and WebGL run as `PASS`.
- The bundled catalog remains the strict `orchard-01`, `orchard-02`, `orchard-03` sequence. Acceptance fixtures are loaded from `Assets/Editor/Tests/Fixtures/` and are absent from production `Resources`, the release scene list, and the playable catalog.
- Serialization migration, palette-key resolution, deterministic fixtures, and multi-level simulation all passed. Contour identity remains presentation-only and does not alter the gameplay fingerprint.
- Strict OpenSpec validation passed for this change and for all `40` current changes/specifications.

## WebGL and visual evidence

- Ordinary WebGL was rebuilt after production brush installation with marker `FRUIT_DEFENSE_WEB_BUILD_OK`, delivery version `f1da727a81db`, and total output `17,629,676` bytes. The four versioned payloads are `13,568,982`, `68,952`, `117,893`, and `3,854,046` bytes.
- Direct portrait Battle manifest: `Logs/visual-acceptance/20260724-011610/acceptance.json`; accepted at `402 x 874` with all HTTP, loading, identity, HUD, pause/restart, click, and drag checks passing.
- Full-flow manifest: `Logs/visual-acceptance/20260724-011701/flow-acceptance.json`; accepted for Lobby to Battle to Settlement, return to Lobby, and retry Battle.
- Real Battle screenshots show the migrated square-cell presentation. Editor/battle-scale boards additionally verify the hand-painted square edge, retained organic rendering, and square/organic coexistence without fallback.
- Visual review found a continuous non-mechanical grass lip, source-derived unequal hand-painted droops, no horizontal tile bands or vertical curtains, and no breakage in L-shapes, U-shapes, holes, or turns. The `05/10` regression case keeps diagonal grass islands separate while joining their outside soil without the former dark slit. The middle and surrounding soil use the edge's warm light-tan color family; the rim keeps authored depth but its former uniform opaque outer stroke now resolves as a restrained translucent falloff at real Battle scale.

## Remaining limitations

- Follow-up change `consolidate-terrain-authoring-tools` keeps the isolated square trial on one canonical `grass on soil` ProtectedHybrid edge family and now resolves its `soil on grass` brush from that same TileSet with the complemented 4-bit mask. Exact reverse bindings remain compatibility overrides; contour styles are still never substituted.
- The production full-composite pairs are limited to grass-on-soil and stone-on-water. Stone-on-water is available as a semantic shortcut, while water-on-stone remains unavailable as an authoring preset until a real square water landform TileSet exists.
- The agent performed no visual inspection of the newly installed A/B candidates by request. Their pipeline manifests therefore retain `visualInspectionPerformed=false` and `seamSafetyClaimed=false`; the deterministic, Unity, and WebGL results above are mechanical evidence rather than a new art-quality claim.
- Organic assets remain supported and validated, while the three bundled maps intentionally default to square contours.
- This is ordinary WebGL acceptance only. Douyin and WeChat conversion, simulator, package, and device gates remain separate and unavailable until their platform-specific evidence authorizes them.
