## MODIFIED Requirements

### Requirement: Canonical battlefield cell semantics
The system SHALL define exactly one gameplay-cell record for every in-bounds battlefield cell, SHALL compile a finite set of composable plantability, traversal, placement, and collision capabilities for that record, and SHALL derive required gameplay cell collections from those capabilities. Entry, exit, core, spawn, goal, and other semantic locations MUST be represented by routes or typed markers rather than mutually exclusive cell roles, and gameplay semantics MUST NOT be inferred from visual terrain.

#### Scenario: Default orchard map is loaded
- **WHEN** the bundled `orchard-01` battlefield is compiled
- **THEN** it has an 8-by-7 gameplay grid containing 20 cells in `route.main`, 35 plantable cells, one core marker, one enemy-spawn marker, one route-goal marker, and no gameplay-blocked cells

#### Scenario: Initial flowerpots are resolved
- **WHEN** a new `orchard-01` battle initializes
- **THEN** its typed roadside marker groups place eight initial flowerpots only on cells carrying the plantable capability

#### Scenario: Cell capabilities are queried
- **WHEN** simulation queries any coordinate inside the battlefield dimensions
- **THEN** it receives the same compiled capability/collision record, may observe several compatible capabilities, and does not infer gameplay legality from a visual surface, route image, or marker icon

### Requirement: Ordered four-directional route topology
The system SHALL define enemy routes as stable named ordered lists of unique in-bounds cells whose consecutive cells are cardinal neighbors. The current execution profile SHALL derive entry and exit from the primary route's first and last cells, SHALL require matching typed spawn and goal markers, and SHALL require the goal relationship to the declared core marker without encoding those points as exclusive cell roles.

#### Scenario: Default route order is inspected
- **WHEN** `orchard-01` route `route.main` is enumerated
- **THEN** it runs east from `(0,0)` through `(7,0)`, south through `(7,6)`, and west through `(1,6)` without a duplicate, diagonal step, gap, or branch, with its spawn at the first cell and goal at the last cell

#### Scenario: Invalid route definition is validated
- **WHEN** a map contains a duplicate route ID, missing route cell, out-of-bounds cell, non-cardinal route step, duplicate cell, mismatched spawn/goal endpoint, missing route reference, unsupported active route count, non-adjacent goal and core, or non-plantable initial-pot marker
- **THEN** validation fails and identifies the invalid route, marker, cell, or route index
