# battlefield-map-layout Specification

## Purpose
TBD - created by archiving change restructure-battlefield-map-and-tiles. Update Purpose after archive.
## Requirements
### Requirement: Canonical battlefield definition
The game SHALL define visual surfaces, gameplay cell capabilities/collision, named ordered routes, spawn/goal/core markers, and initial-flowerpot marker groups through one versioned canonical battlefield aggregate, and SHALL compile separate immutable gameplay and presentation views consumed by simulation and presentation without maintaining a second independently mutable position source.

#### Scenario: Default map is loaded
- **WHEN** a new game is created from the migrated default map
- **THEN** the active map exposes the same current grid, plantable cells, continuous primary route, spawn, goal, core, and semantic groups that place eight initial flowerpots

#### Scenario: Flowerpot position is resolved
- **WHEN** a flowerpot marker or active flowerpot is queried by simulation or presentation
- **THEN** its position is derived from its canonical marker/cell and the shared projection instead of a second independently mutable point

#### Scenario: Visual surface is queried
- **WHEN** presentation resolves a map cell's ground surface
- **THEN** it reads the canonical visual layout while simulation legality continues to read the independent compiled gameplay cell

### Requirement: Derived route metrics
The game SHALL derive route length and route sampling from the active map's ordered route nodes and SHALL NOT assume a fixed number or equal length of route segments.

#### Scenario: Route endpoint sampling
- **WHEN** route progress is sampled at zero and at the derived total length
- **THEN** the returned positions equal the configured entry and exit respectively

#### Scenario: Route crosses a corner
- **WHEN** progress advances across any route-node boundary
- **THEN** the sampled position remains continuous and follows the next configured segment

### Requirement: Shared battlefield projection
The portrait presentation SHALL derive visual surfaces, route geometry, gameplay-marker positions, cell rectangles, flowerpot rectangles, entities, effects, feedback, drag targets, and hit-test regions from one battlefield projection for the current board rectangle.

#### Scenario: Flowerpot is drawn and clicked
- **WHEN** an active flowerpot is rendered at a canonical marker/cell
- **THEN** its visual rectangle, click target, drag target, and drop highlight use the same projected bounds

#### Scenario: Projectile leaves a plant
- **WHEN** a plant creates a projectile or combat effect
- **THEN** the projected source aligns with the rendered plant and the projected target aligns with the rendered enemy

#### Scenario: Gameplay marker is presented
- **WHEN** a spawn, goal, core, item, or trigger marker has a visible presentation
- **THEN** its screen position is derived from its canonical cell through the same projection and remains aligned at every supported portrait safe area

### Requirement: Doubled flowerpot and tile presentation
At the 402-by-874 reference viewport, every on-board flowerpot SHALL render and interact at 200% of the previous width and 200% of the previous height, approximately 45.6 by 45.6 logical points, and SHALL remain at least 44 logical points on its shortest interactive dimension.

#### Scenario: Adjacent occupied cells
- **WHEN** flowerpots occupy horizontally or vertically adjacent cells
- **THEN** their rendered and interactive rectangles do not overlap and each remains individually targetable

#### Scenario: Full planting-grid inspection
- **WHEN** all 48 planting cells are displayed
- **THEN** every cell and flowerpot remains inside the battlefield with no clipping against the safe area

### Requirement: Enlarged portrait battlefield
The portrait layout SHALL remove obsolete reserved action-row space, use a nearly full safe-area width for the battlefield, and allocate additional vertical space to gameplay while keeping the build tray and required status surfaces operable.

#### Scenario: Reference portrait screen
- **WHEN** the game renders at 402 by 874 logical points
- **THEN** the battlefield is larger than the previous 386-by-320 region and all primary controls remain visible without scrolling

#### Scenario: Portrait safe-area insets
- **WHEN** the device reports non-zero safe-area insets
- **THEN** the battlefield projection fits within the usable area without shrinking flowerpot targets below the required minimum

### Requirement: Preserved map gameplay behavior
The map-unit migration SHALL preserve initial state, cardinal expansion behavior, representative plant reach, enemy route traversal timing, and wave outcomes within deterministic smoke-test tolerances.

#### Scenario: Initial game setup
- **WHEN** a deterministic new game is created after migration
- **THEN** it still has eight active flowerpots distributed through the same three semantic roadside groups and fewer active pots than plantable cells

#### Scenario: Cardinal expansion
- **WHEN** a player attempts to expand from an active flowerpot
- **THEN** an unused cardinally adjacent plantable cell is legal and a diagonal-only neighbor is illegal

#### Scenario: Representative combat geometry
- **WHEN** deterministic plant-versus-route scenarios are evaluated before and after the coordinate migration
- **THEN** route traversal duration and representative target-in-range outcomes remain within their recorded tolerances

### Requirement: Battlefield geometry acceptance
The project SHALL validate battlefield topology and geometry in editor smoke tests and SHALL capture the enlarged battlefield from a real WebGL canvas before publication.

#### Scenario: Invalid topology
- **WHEN** the map contains duplicate cells, a zero-length route segment, an invalid initial group, or an out-of-bounds required cell
- **THEN** validation fails with the invalid map condition identified

#### Scenario: WebGL reference capture
- **WHEN** portrait visual acceptance runs against a built player
- **THEN** evidence confirms visible route continuity, non-overlapping enlarged flowerpots, aligned drag targets, and safe-area containment
