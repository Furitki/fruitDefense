## Context

The current map has no domain object of its own. `GameConfig` exposes four normalized route points, a hard-coded route length, an 8-by-6 planting-cell calculation, and a point conversion; `Pot` then stores both a cell and its calculated point. `GameSimulation` consumes those points for targeting and movement, while `FruitDefenseGame` independently turns them into IMGUI positions and sizes entities through `BoardScale`. At the 402-by-874 reference viewport, a pot is about 22.8 logical points wide. Doubling that size would exceed the current roughly 31-to-32 point cell pitch.

The map work must retain a deterministic, scene-independent simulation and support the existing editor smoke entry. It must also give later UI changes one projection API for drawing, hit testing, drag targets, and attack-range overlays.

## Goals / Non-Goals

**Goals:**

- Establish one canonical definition for the 8-by-6 planting grid, 48 cells, route, entry, exit, core, and initial-pot regions.
- Derive route length and sampling from the configured route instead of assuming three equal 76-unit segments.
- Give the simulation a stable topology API and the presentation a separate screen-projection API.
- Enlarge the portrait battlefield and render every on-board flowerpot at twice its current width and height without overlap.
- Preserve initial resources, eight initial flowerpots, cardinal expansion, wave traversal timing, targeting relationships, and combat balance within smoke-test tolerances.
- Use identical rectangles for rendering, hit testing, drag overlap, and acceptance geometry.

**Non-Goals:**

- Adding multiple playable maps or an in-game map editor.
- Migrating IMGUI to uGUI or UI Toolkit.
- Changing the number of planting cells, expansion costs, plant stats, waves, or rewards.
- Redesigning temporary art, combat effects, or save persistence.

## Decisions

### Add a pure runtime battlefield definition before adding Unity authoring assets

Introduce a scene-independent `BattlefieldMapDefinition` (or equivalently named immutable type) under `Assets/Scripts/Core`. It owns grid dimensions, plantable cells, ordered route nodes, entry/exit, core position, and named initial-pot groups. A default factory constructs the current single map.

This keeps deterministic tests simple and avoids making `GameSimulation` depend on `Resources` or a loaded `ScriptableObject`. A ScriptableObject adapter can be added later when multiple maps or Inspector authoring become requirements. Keeping the current static constants was rejected because it would preserve the coupling this change is intended to remove.

### Make canonical cells the source of flowerpot position

A flowerpot stores its canonical cell identity; its simulation point is derived through the active map definition. The redundant mutable `Pot.Point` value is removed or made read-only/derived. Initial placement and expansion query semantic map groups and topology rather than hard-coding `row == 0`, `column == 7`, and `row == 5` throughout the simulation.

This prevents cell and point state from drifting. Retaining both fields was rejected because every future map edit would need to update two representations atomically.

### Separate topology and metrics from portrait projection

The map layer has two consumers:

```text
BattlefieldMapDefinition
        |
        +--> BattlefieldTopology/RouteMetrics --> GameSimulation
        |
        +--> BattlefieldProjection -----------> FruitDefenseGame
```

Topology provides plantable-cell lookup, cardinal neighbors, derived cumulative route lengths, and `SampleRoute(progress)`. Projection is constructed from the current battlefield rectangle and provides `CellRect`, `PotRect`, `MapToScreen`, route geometry, and entity-size helpers. `FruitDefenseGame` must not recompute grid or route positions outside this projection.

The projection uses a nearly full-width safe-area board. It fits eight columns with a pot target of approximately 45.6 logical points at the reference viewport, preserves a non-negative gap between adjacent targets, and allocates reclaimed vertical space to the battlefield. Draw and hit-test paths receive the same `Rect` values.

### Migrate distance units without changing effective balance

The canonical map may use tile-oriented map units instead of the legacy 0-to-100 convention. Route metrics calculate the new length. Distance-based plant ranges, projectile speeds, hit radii, blast radii, and zombie speeds use one explicit legacy-to-map scale so representative traversal time and target coverage remain equivalent within defined tolerances.

This is preferable to visually moving cells while leaving projectile and target positions in an unrelated coordinate system. Maintaining two unrelated visual and simulation positions was rejected because projectiles, effects, and later range overlays would detach from their sources.

### Validate topology before visual acceptance

`ProjectSetup.SmokeValidate` verifies unique cells, valid initial groups, route continuity, derived route length, entry/exit sampling, cardinal adjacency, preserved initial counts, representative range coverage, and reference-viewport pot dimensions/non-overlap. WebGL acceptance then verifies safe-area containment, visible route continuity, enlarged pots, drag alignment, and the absence of clipping at 402 by 874.

## Risks / Trade-offs

- [Distance-unit migration subtly changes combat reach] -> Record representative pre-migration traversal and target-coverage expectations, centralize the scale, and assert them in editor smoke tests.
- [Eight doubled pot targets barely fit the portrait width] -> Use the nearly full safe-area width, calculate pitch and gap from the inner board width, and fail geometry validation rather than shrinking targets below the required size.
- [Route and cell definitions can describe an invalid map] -> Validate route node count, unique cells, bounds, named initial groups, and non-zero segment lengths at construction/smoke time.
- [A large refactor obscures presentation regressions] -> Migrate topology and projection first, keep gameplay data fixed, then resize the board and update WebGL evidence as a separate task group.
- [Future maps need Inspector authoring] -> Keep the definition serializable-friendly but defer ScriptableObject authoring until a second map is requested.

## Migration Plan

1. Capture representative legacy route duration, range coverage, initial pot distribution, and reference pot geometry in smoke assertions.
2. Add the default battlefield definition, topology, cumulative route metrics, and validation without changing the rendered layout.
3. Migrate `GameSimulation` and `Pot` to canonical cells and derived positions, applying the explicit distance-unit conversion.
4. Add the shared battlefield projection and migrate every draw, hit-test, drag, entity, projectile, effect, and feedback position to it.
5. Reallocate the portrait regions and enable the doubled flowerpot/tile geometry.
6. Run editor smoke, build WebGL, and capture initial, drag, active-wave, and dense-board states at the reference viewport.

Rollback restores the old map constants and projection together. There is no persisted runtime map data to migrate.

## Open Questions

None for this change. Multiple map assets and Inspector authoring remain explicit follow-up work.
