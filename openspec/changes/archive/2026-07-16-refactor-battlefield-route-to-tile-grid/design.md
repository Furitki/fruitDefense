## Context

`BattlefieldMapDefinition` currently exposes an 8-by-6 collection in which all 48 cells are plantable, while the enemy route is a separate four-node polyline outside that grid. `BattlefieldProjection` converts the polyline to several screen points and forces `CellRect` and `PotRect` to the same doubled size. `FruitDefenseGame` then draws each route segment as one thick stretched rectangle. This lets the route, grid, enemy centerline, core, and pot geometry describe different spatial structures, and the mismatch becomes visible when the portrait canvas or safe area changes.

The P0 runtime has one bundled map, fifteen waves, deterministic simulation and snapshot support, an immediate-mode presentation, and a required real WebGL acceptance surface. This change must replace the spatial contract without adding multi-level selection or retuning combat content. The map keeps the stable identity `orchard-01` so a later change can bind `BattleLaunchRequest.LevelId` to a map catalog.

## Goals / Non-Goals

**Goals:**

- Make every coordinate in the battlefield grid resolve to one explicit semantic role.
- Represent the route as a unique ordered list of four-directionally adjacent grid cells and derive route artwork from those connections.
- Make route rendering, route sampling, enemy movement, projectiles, effects, and hit geometry share one tile projection.
- Keep the 8-column portrait board legible by moving `orchard-01` to an 8-by-7 total grid with 20 route cells, 35 plantable cells, and one core cell.
- Preserve eight initial flowerpots, the current fifteen waves, current numeric combat configuration, and legacy-equivalent route traversal duration.
- Fit square tiles inside the usable board and render flowerpots at 65% to 70% of a tile while retaining a full-tile interaction target.
- Validate the layout at multiple portrait sizes and representative safe-area insets, then inspect it on a real WebGL canvas.

**Non-Goals:**

- Adding a level picker, multiple playable map definitions, branching paths, teleporters, or an in-game map editor.
- Binding arbitrary `LevelId` values to maps in this change.
- Changing wave composition, plant/enemy/equipment stats, rewards, battle rules, or the fifteen-wave progression.
- Replacing IMGUI, importing a platform SDK, changing platform adapters, or redesigning the temporary art style.
- Defining a durable migration for snapshots produced by an older build of the same P0 map; no cross-build disk-save promise exists yet.
- Automatically updating the stable game-design overview; that remains subject to the project's design synchronization gate.

## Decisions

### Use one role matrix and one ordered route-cell list

`BattlefieldMapDefinition` will own a stable `MapId`, grid dimensions, a semantic role for every in-bounds cell, the ordered route cells, the core cell, and semantic initial-pot groups. Entry and exit are specialized route roles. A role lookup is the source for plantability; `PlantableCells`, where retained for compatibility, is derived rather than independently authored.

The default `orchard-01` layout is:

```text
row 0: E  R  R  R  R  R  R  R
row 1: P  P  P  P  P  P  P  R
row 2: P  P  P  P  P  P  P  R
row 3: P  P  P  P  P  P  P  R
row 4: P  P  P  P  P  P  P  R
row 5: P  P  P  P  P  P  P  R
row 6: C  X  R  R  R  R  R  R
```

`E` is entry `(0,0)`, `X` is exit `(1,6)`, `C` is core `(0,6)`, `R` is route, and `P` is plantable. The ordered route runs from `(0,0)` east through `(7,0)`, south through `(7,6)`, then west through `(1,6)`. This produces 20 route cells and 19 center-to-center segments while leaving 35 plantable cells. P0 uses no blocked cell, but blocked is a valid role for later definitions. The eight initial pots remain in three roadside groups on plantable cells: north-roadside `(1,1)`, `(4,1)`, `(6,1)`; east-roadside `(6,2)`, `(6,4)`; and south-roadside `(1,5)`, `(4,5)`, `(6,5)`.

Keeping the former 48 planting cells by surrounding them with a 9- or 10-column route grid was rejected because it makes full-cell touch targets too narrow at the smallest portrait baseline. Keeping route nodes outside the grid was rejected because it preserves two competing topologies.

### Validate role and path invariants at the map boundary

Construction or topology validation will require every in-bounds coordinate to have exactly one role; exactly one entry, exit, and core; unique route cells; entry and exit equal to the first and last ordered route cells; Manhattan distance one between every route pair; no route/plantable/core overlap; a cardinally adjacent exit and core; and initial-pot groups containing only unique plantable cells. Diagnostics will identify the offending cell or route index.

Branching and crossing connectivity are not inferred from neighboring route-looking cells. Only the previous and next cells in the ordered list form the route. This keeps simulation order deterministic and prevents an adjacent return leg from accidentally becoming a junction.

### Derive a route tile descriptor instead of authoring artwork IDs

Topology will derive a descriptor for every ordered route cell. Internal cells resolve from previous/current/next directions to horizontal, vertical, or one of four corners. The first and last cells resolve to oriented entry and exit descriptors. Impossible direction pairs fail validation. Presentation selects a procedural tile or a single atlas sub-sprite from this descriptor and draws it entirely inside that cell's `TileRect`.

Storing a manually selected sprite per route cell was rejected because topology and art could disagree. Drawing a multi-cell line or stretching one image over a segment is explicitly removed because it recreates the aspect-ratio failure.

### Sample the route through cell centers while retaining legacy-equivalent distance

`BattlefieldRouteMetrics` will be built from `CellToMap` positions of the ordered route cells. At every cumulative segment boundary, `Sample` returns that route cell's exact map-space center; between boundaries it interpolates linearly to the next center. Simulation and presentation continue to consume the same sampler.

To avoid a balance retune, `orchard-01` uses a uniform map-space cell pitch of `23 / 19` units. Its 19 equal route segments therefore retain the current 23 map-unit route length and the existing `23 / 228` legacy-distance conversion. Current zombie traversal time, range constants, projectile speed constants, and wave values remain numerically unchanged. A unit cell pitch with a shorter total route was rejected because it would silently change traversal duration or require broad stat changes.

### Fit a centered square tile grid, then derive specialized rectangles

`BattlefieldProjection` will calculate `TileSize = min(contentWidth / gridWidth, contentHeight / gridHeight)` and center the complete grid in the available map content rectangle. All `TileRect` values are square and contained by the content rectangle. Map-to-screen distance uses `TileSize / MapUnitsPerCell`, keeping sampled enemy centers aligned with tile centers.

The projection exposes separate rectangles with explicit purposes:

- `TileRect` / `PotHitRect`: the full tile used for selection, drag origin, drop target, and expansion highlighting.
- `PotVisualRect`: a centered square at a single constant ratio in the inclusive 0.65 to 0.70 range; P0 uses 0.675.
- `RouteTileRect`: the tile rectangle itself; connectors and art stay inside it.
- `CoreRect`: a tile-local contained visual rectangle derived from the core cell instead of the legacy multi-cell fixed size.

Draw and hit paths may have different sizes, but both must come from the same projection instance. Reusing `PotRect` ambiguously for both purposes was rejected because it caused the current full-screen pot density.

### Keep map identity explicit without introducing a catalog

`BattlefieldMapDefinition.MapId` becomes the authoritative runtime identity. The bundled default returns `orchard-01`, snapshot export records the active definition's identity, and custom-map compatibility hashing includes the new role and route-cell data. `BattleLaunchRequest.LevelId` remains flow metadata during this P0; default battle initialization still receives the one bundled map.

A map registry and `LevelId -> MapId` resolution were rejected for this change because there is only one accepted map and no level-selection experience yet.

### Use layered acceptance with a browser fallback

Editor smoke owns deterministic topology, tile descriptor, route sampling, combat parity, and projection-matrix assertions. Real WebGL acceptance then checks the rendered canvas at 360-by-800, 375-by-812, 402-by-874, and 430-by-932, including representative top and bottom safe-area insets and dense-pot/active-wave states.

The in-app browser is not a release dependency. If it crashes or disconnects, acceptance will stop retrying that surface and continue in the user's existing Chrome or another stable external Chromium session against the same local WebGL build. Evidence records the viewport, safe-area case, browser, and build artifact so an automation crash is not mistaken for a gameplay failure.

## Risks / Trade-offs

- [Changing from 48 planting cells to 35 alters expansion capacity] -> Treat the new count as an intentional P0 map-layout change, preserve the eight initial pots and current expansion rules, and validate that dense-board play remains operable.
- [Map identity stays `orchard-01` while its topology changes] -> Preserve identity for future level binding, reject structurally invalid restored cell references, and make no cross-build snapshot compatibility claim in P0.
- [Per-cell corners show seams] -> Use one shared tile size and connection width, integer-friendly screen rectangles where practical, and WebGL captures at both smallest and largest viewports.
- [Route length or target coverage drifts during coordinate migration] -> Retain the 23-unit route through the explicit cell pitch and assert traversal time plus representative range outcomes before visual acceptance.
- [Hit targets and visuals drift after being separated] -> Derive both from the same tile center and assert containment, ratio, drag alignment, and full-tile interaction bounds.
- [The in-app browser crashes during acceptance] -> Bound the attempt, preserve editor/build results, switch to external Chrome for canvas evidence, and report the browser failure separately.

## Migration Plan

1. Record current deterministic traversal duration, fifteen-wave content signature, combat constants, initial-pot count, and `orchard-01` snapshot identity.
2. Introduce the role matrix, exact 8-by-7 default layout, ordered route cells, validation, descriptors, map identity, and centerline route metrics behind current core APIs.
3. Migrate projection helpers and IMGUI drawing to square tile rectangles, per-cell route rendering, contained core geometry, and separate pot visual/hit rectangles.
4. Replace route-segment drawing and all ambiguous pot rectangles, then extend editor validation across topology, simulation, and viewport/safe-area matrices.
5. Run compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, build with `FruitDefense.Editor.WebBuild.Build`, and capture the required WebGL states using the stable browser path.

Rollback restores the previous map definition, projection, and route drawing together. No content catalog, platform adapter, or persisted save migration is deployed by this change.

## Open Questions

None for P0. Additional map layouts, authored tile art, a map catalog, and `LevelId` binding remain follow-up changes after `orchard-01` passes the tile-grid acceptance gate.
