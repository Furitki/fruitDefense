## 1. Baseline and Map Definition

- [x] 1.1 Record deterministic legacy route duration, representative target coverage, initial flowerpot distribution, and 402-by-874 pot/cell geometry as migration assertions
- [x] 1.2 Add the immutable default battlefield definition with an 8-by-6 grid, 48 unique plantable cells, ordered route nodes, entry, exit, core, and named initial-pot groups
- [x] 1.3 Add topology validation for bounds, uniqueness, non-zero route segments, semantic groups, and cardinal neighbors
- [x] 1.4 Implement cumulative route metrics and arbitrary-segment route sampling from the active definition

## 2. Simulation Coordinate Migration

- [x] 2.1 Make canonical flowerpot cells the source of simulation position and remove or encapsulate the redundant mutable point field
- [x] 2.2 Migrate initial flowerpot placement and expansion checks to the battlefield topology and semantic groups
- [x] 2.3 Centralize legacy-to-map distance conversion for plant ranges, zombie/projectile speeds, hit radii, blast radii, and other distance-based combat constants
- [x] 2.4 Migrate targeting, enemy movement, projectiles, effects, and feedback to active-map positions and derived route length
- [x] 2.5 Extend deterministic smoke coverage to prove preserved initial state, cardinal expansion, traversal timing, representative reach, and combat outcomes

## 3. Shared Projection and Enlarged Board

- [x] 3.1 Implement a battlefield projection that derives route geometry, cell/pot rectangles, entity positions, distance extents, and map-to-screen conversion from one board rectangle
- [x] 3.2 Migrate battlefield drawing, click targets, drag sources, drop targets, highlights, enemies, projectiles, effects, core, and feedback to the shared projection
- [x] 3.3 Reflow the portrait regions to a nearly full-width and taller battlefield while keeping the build tray, contextual details, status surface, and safe area operable
- [x] 3.4 Set reference flowerpot visual and interactive rectangles to 200% of the previous width and height, at least 44 logical points, with no adjacent overlap or clipping
- [x] 3.5 Extend `ValidatePortraitLayout` to verify the enlarged board, 48 projected cells, doubled targets, shared hit bounds, and non-overlap at the reference viewport

## 4. Build and Runtime Acceptance

- [x] 4.1 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate` with all new topology, migration, and portrait-geometry assertions passing
- [x] 4.2 Build the WebGL player through `FruitDefense.Editor.WebBuild.Build` and update acceptance coordinates/helpers for the enlarged battlefield
- [x] 4.3 Capture and review initial, adjacent-pot, drag-target, active-wave, and dense-board WebGL states at 402 by 874, proving route continuity, alignment, safe-area containment, and non-overlapping doubled flowerpots
