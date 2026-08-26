## MODIFIED Requirements

### Requirement: Ordered four-directional route topology
The system SHALL define enemy routes as stable named ordered lists of unique in-bounds cells whose consecutive cells are cardinal neighbors. Every executable route SHALL have matching typed spawn and goal markers at its first and last cells. The standard release execution profile SHALL accept exactly one primary route and SHALL require its goal relationship to the declared core marker; the GM multi-route profile SHALL accept exactly eight named vertical routes with independently paired spawn and goal markers and SHALL NOT require a damageable core.

#### Scenario: Default route order is inspected
- **WHEN** `orchard-01` route `route.main` is enumerated under the standard release profile
- **THEN** it runs east from `(0,0)` through `(7,0)`, south through `(7,6)`, and west through `(1,6)` without a duplicate, diagonal step, gap, or branch, with its spawn at the first cell and goal at the last cell

#### Scenario: GM routes are inspected
- **WHEN** the GM battlefield is enumerated under the GM multi-route profile
- **THEN** it contains exactly eight distinct route IDs, each route stays in its own column, and every route resolves its own matching spawn and goal endpoint

#### Scenario: Invalid route definition is validated
- **WHEN** a map contains a duplicate route ID, missing route cell, out-of-bounds cell, non-cardinal route step, duplicate cell within a route, mismatched spawn/goal endpoint, missing route reference, route count unsupported by its execution profile, non-adjacent standard goal and core, or non-plantable initial-pot marker
- **THEN** validation fails and identifies the invalid profile, route, marker, cell, or route index

## ADDED Requirements

### Requirement: Stable per-enemy route identity
Every live enemy SHALL own a non-empty route ID from its active compiled map, and movement, position lookup, targeting, projectiles, effects, feedback anchors, and presentation projection SHALL resolve that enemy through the identified route rather than a global primary route.

#### Scenario: Two lanes share progress
- **WHEN** two enemies on different route IDs have the same route progress
- **THEN** each enemy samples the corresponding route and their combat and presentation positions remain distinct

#### Scenario: Unknown route is assigned
- **WHEN** enemy creation or state validation receives a route ID absent from the compiled map
- **THEN** the command fails before the enemy becomes live and identifies the invalid route ID
