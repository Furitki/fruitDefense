## 1. Core Map Contract (Core agent; exclusive ownership of `Assets/Scripts/Core/BattlefieldMap.cs`, `GameConfig.cs`, and map-identity code until this group is merged)

- [x] 1.1 Record the pre-change 23-unit route length, normal-enemy traversal duration, fifteen-wave content signature, numeric combat configuration, eight-pot initial count, and `orchard-01` snapshot identity for later parity assertions
- [x] 1.2 Add stable map identity, complete per-cell semantic roles, map-units-per-cell, the exact 8-by-7 `orchard-01` role matrix, its 20-cell ordered route, core cell, and three eight-pot initial groups while deriving compatibility collections from the canonical roles
- [x] 1.3 Validate complete role coverage, unique and in-bounds cells, entry/exit/core cardinal relationships, ordered four-directional route continuity, disjoint semantics, and plantable-only initial groups with cell/index diagnostics
- [x] 1.4 Derive oriented entry, exit, straight, and corner tile descriptors from ordered neighbor directions and reject impossible or inconsistent connection pairs
- [x] 1.5 Build route metrics from ordered cell centers using the `23 / 19` map-space cell pitch, prove all cumulative boundaries sample exact centers and total 23 units, and preserve current simulation-facing route and distance APIs
- [x] 1.6 Make the active definition's `MapId` authoritative for snapshot export/restore compatibility and include canonical roles and ordered route cells in custom-map identity checks without adding a map catalog or `LevelId` resolver

## 2. Projection and Runtime Presentation (Presentation agent; starts after Group 1 API freeze and exclusively owns `Assets/Scripts/Core/BattlefieldProjection.cs` and `Assets/Scripts/FruitDefenseGame.cs`)

- [x] 2.1 Refactor battlefield projection to fit and center an 8-by-7 grid of square tile rectangles, align map-space cell pitch to tile size, and expose unambiguous tile, route, core, pot-visual, and pot-hit rectangles
- [x] 2.2 Set one flowerpot visual ratio to 0.675, keep selection/drag/drop/expansion targets on the full tile, and migrate every pot drawing and interaction call site to the correct projection helper
- [x] 2.3 Replace multi-cell route line drawing with one tile-local procedural or atlas-backed render per derived descriptor, including entry, exit, both straights, and all four corners with consistent connector widths
- [x] 2.4 Render the core inside its semantic tile and migrate route markers, enemies, projectiles, combat effects, feedback, ranges, and diagnostics to the same projected route sampler without legacy fixed route/core geometry
- [x] 2.5 Reflow battlefield controls and update built-in acceptance states for the new plantable cells so route, core, full-tile targets, dense pot visuals, and controls remain disjoint
- [x] 2.6 Expose a pure viewport/safe-area layout calculation used by runtime drawing and editor validation, covering 360-by-800, 375-by-812, 402-by-874, and 430-by-932 without adding a second rendering path

## 3. Deterministic and Geometry Validation (Validation agent; starts after Groups 1 and 2 API freeze and exclusively owns `Assets/Editor/ProjectSetup.cs` plus dedicated acceptance helpers)

- [x] 3.1 Replace obsolete 48-cell/equal-pot assertions with exact `orchard-01` role counts, route order, invalid-topology diagnostics, all route descriptor orientations, and eight-pot plantability coverage
- [x] 3.2 Add deterministic assertions for exact cell-center sampling, continuous corner traversal, 23-unit total length, normal-enemy traversal parity, representative target coverage, fifteen-wave/content parity, and active map identity
- [x] 3.3 Validate every tile is square, centered, contained, and aligned with route samples; validate the core is tile-local and pot visuals are contained at the 0.675 ratio while full-tile hit/drag/drop targets remain independently addressable
- [x] 3.4 Run the pure layout matrix for 360-by-800, 375-by-812, 402-by-874, and 430-by-932 plus representative non-zero top and bottom safe-area insets, checking grid/control containment and absence of route, core, pot, or control overlap

## 4. Build and Live Acceptance (Coordinator only; starts after all implementation agents stop writing)

- [x] 4.1 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, fixing failures through the owning group rather than making overlapping cross-agent edits
- [x] 4.2 Build the WebGL player through `FruitDefense.Editor.WebBuild.Build` and confirm the accepted artifact still contains the current fifteen-wave battle flow with no map picker or additional playable map
- [x] 4.3 Inspect initial, active-wave, and dense-adjacent-pot states on the real WebGL canvas at all four required portrait viewports and record build, viewport, safe-area behavior, browser, route continuity, enemy alignment, pot legibility, hit alignment, and control containment
- [x] 4.4 Because the user reported possible in-app browser crashes, do not risk that surface; preserve editor/build results and complete the same canvas evidence through a stable external Chrome/CDP session
- [x] 4.5 Review the final diff for unchanged wave/stat/reward data, stable `orchard-01` identity, no `LevelId` binding or multi-map UI, and no edits outside the approved implementation and acceptance scope before marking the change complete
