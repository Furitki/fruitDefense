## MODIFIED Requirements

### Requirement: Canonical battlefield definition
The game SHALL define visual surfaces, gameplay cell capabilities/collision, a stable collection of named ordered routes, per-route spawn/goal markers, optional profile-specific core markers, and initial-flowerpot marker groups through one versioned canonical battlefield aggregate. It SHALL compile separate immutable gameplay and presentation views consumed by simulation and presentation without maintaining a second independently mutable position source, and SHALL expose an explicit validated primary route only to execution profiles that require one.

#### Scenario: Default map is loaded
- **WHEN** a new standard game is created from the migrated default map
- **THEN** the active map exposes the same current grid, plantable cells, continuous single primary route, spawn, goal, core, and semantic groups that place eight initial flowerpots

#### Scenario: GM map is loaded
- **WHEN** a GM stress battle is created
- **THEN** the active canonical map exposes eight named routes and eight paired spawn/goal marker sets through the same gameplay and presentation views without a second position source

#### Scenario: Flowerpot position is resolved
- **WHEN** a flowerpot marker or active flowerpot is queried by simulation or presentation
- **THEN** its position is derived from its canonical marker/cell and the shared projection instead of a second independently mutable point

#### Scenario: Visual surface is queried
- **WHEN** presentation resolves a map cell's ground surface
- **THEN** it reads the canonical visual layout while simulation legality continues to read the independent compiled gameplay cell

### Requirement: Derived route metrics
The game SHALL derive length and sampling metrics independently for every named route from that route's ordered nodes, SHALL resolve metrics by stable route ID, and SHALL NOT assume a fixed number or equal length of routes or segments.

#### Scenario: Route endpoint sampling
- **WHEN** any named route is sampled at zero and at its derived total length
- **THEN** the returned positions equal that route's configured entry and exit respectively

#### Scenario: Route crosses a corner
- **WHEN** progress advances across any route-node boundary
- **THEN** the sampled position remains continuous and follows the next configured segment on the same route

#### Scenario: Several routes are sampled
- **WHEN** equal progress is sampled on two named routes with different geometry
- **THEN** each result comes from its own immutable route metrics and neither lookup mutates or replaces the other route
