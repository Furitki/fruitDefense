# Consumed P0 API baseline

P1 starts from the completed and strictly validated `refactor-battlefield-route-to-tile-grid` change. The retained release-gate log is `Logs/p0-tile-route-gate-after-acceptance.log`, and the accepted WebGL build version is `caa73cbe3cce`.

## Canonical map contract

- `BattlefieldMapDefinition` is the only gameplay map model. It owns `MapId`, `Width`, `Height`, `MapUnitsPerCell`, complete `CellRoles`, `RouteCells`, `EntryCell`, `ExitCell`, `CoreCell`, and initial pot groups.
- `BattlefieldCellRole` is the canonical per-cell semantic source. Compatibility collections are derived from that source rather than authored independently.
- `BattlefieldTopology.ValidateCanonical(...)` is the authoring boundary for complete coverage, bounds, uniqueness, disjoint roles, cardinal route continuity, entry/exit/core relationships, and plantable initial groups.
- `BattlefieldRouteTileDescriptor`, `BattlefieldRouteTileKind`, and `BattlefieldRouteConnections` are derived from ordered route neighbors. P1 stores route cells, never tile-art choices.
- `BattlefieldRouteMetrics` converts the ordered route to exact cumulative map-space distance and remains the simulation movement/targeting sampler.

## Projection and presentation contract

- `BattlefieldProjection` consumes a `BattlefieldMapDefinition` and exposes the projected grid, route samples, `TileRect`, `RouteTileRect`, `CoreRect`, `PotVisualRect`, and `PotHitRect`.
- `BattlefieldProjection.CalculateViewportLayout(...)` is the single safe-area-aware logical-layout path for battle drawing and validation.
- Runtime route rendering iterates `RouteTileDescriptors` and draws only inside each route cell. Core and pots use their semantic cell rectangles; full-tile pot hit targets remain separate from the 0.675 visual rectangle.

## P1 boundary

P1 may construct additional validated `BattlefieldMapDefinition` instances and pass the resolved instance into simulation and presentation. It must not add a normalized polyline, stretched route strip, second coordinate system, manually authored route-tile atlas choice, or legacy fixed core geometry.
