## MODIFIED Requirements

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
