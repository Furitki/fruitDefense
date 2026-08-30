## MODIFIED Requirements

### Requirement: Preserved map gameplay behavior
The map-unit migration SHALL preserve initial state, cardinal expansion behavior, representative plant reach, enemy route traversal timing, and wave outcomes within deterministic smoke-test tolerances. Route length SHALL be derived from the active map's ordered route nodes for traversal and sampling, but SHALL NOT control legacy combat-distance calibration.

#### Scenario: Initial game setup
- **WHEN** a deterministic new game is created after migration
- **THEN** it still has eight active flowerpots distributed through the same three semantic roadside groups and fewer active pots than plantable cells

#### Scenario: Cardinal expansion
- **WHEN** a player attempts to expand from an active flowerpot
- **THEN** an unused cardinally adjacent plantable cell is legal and a diagonal-only neighbor is illegal

#### Scenario: Representative combat geometry
- **WHEN** deterministic plant-versus-route scenarios are evaluated before and after the coordinate migration
- **THEN** route traversal duration and representative target-in-range outcomes remain within their recorded tolerances

#### Scenario: Route topology changes without changing cell pitch
- **WHEN** standard maps have equal cell pitch but different route lengths
- **THEN** the same authored plant range covers the same number of cells on each map while route traversal still uses each map's derived route length
