## MODIFIED Requirements

### Requirement: Catalog-wide reference validation
The compiled level catalog MUST reject duplicate identities, missing references, invalid layered-map coverage, unknown surfaces/capabilities/collision channels, invalid route or marker references, unsupported current execution profiles, incompatible wave and rule counts, invalid wave or enemy references, incomplete theme definitions, and missing terrain palettes before an affected level can be selected or launched.

#### Scenario: Compile a valid bundled catalog
- **WHEN** all level references resolve, every layered map compiles, each gameplay marker/route reference is valid, and every theme palette is registered
- **THEN** compilation succeeds and exposes the complete ordered playable-level list with immutable gameplay and presentation map views

#### Scenario: Reject a dangling component reference
- **WHEN** a level references a missing map, wave set, rule set, theme, terrain palette, route, marker group, or marker target
- **THEN** compilation identifies the owning level/map and missing component and excludes the invalid catalog from runtime use

#### Scenario: Reject incompatible wave rules
- **WHEN** a wave set's ordered wave count or milestone bounds do not satisfy its referenced rule set
- **THEN** compilation fails with the incompatible wave-set and rule-set identities

### Requirement: P0 tile-grid map dependency
Every catalog map SHALL use the layered battlefield compiler, composable gameplay cells, stable named ordered cardinal routes, typed markers, topology validation, shared projection, and derived route-tile descriptors supplied by the canonical battlefield model; it MUST NOT introduce a parallel exclusive-role grid, normalized polyline, stretched route strip, marker transform hierarchy, or visual-art-derived gameplay representation.

#### Scenario: Build a catalog map
- **WHEN** a bundled level map is constructed
- **THEN** every route position is an in-bounds cell in a stable named route, spawn/goal/core semantics resolve through typed markers, and rendered route tiles are derived from neighboring ordered route cells and semantic surfaces

#### Scenario: Detect an invalid layered map
- **WHEN** route cells disconnect, required markers conflict, layer coverage is incomplete, a visual surface is unknown, or a gameplay marker violates its cell capabilities
- **THEN** layered-map validation rejects the map before catalog compilation succeeds

### Requirement: Theme follows the resolved level
Battlefield presentation SHALL obtain its color theme and stable terrain-palette identity from the resolved `themeId`, SHALL resolve semantic map surfaces through that palette, and SHALL keep simulation outcomes independent of theme, palette, surface, sprite, and other presentation-only values.

#### Scenario: Render a selected level
- **WHEN** a battle starts from a fully resolved level
- **THEN** the battlefield uses that level's theme, registered terrain palette, semantic map surface layout, and map projection while gameplay calculations use only the resolved gameplay map, waves, and rules

#### Scenario: Change only the theme or surface art
- **WHEN** two deterministic sessions differ only in a valid color theme, terrain palette, semantic visual surface, or sprite asset
- **THEN** their gameplay map fingerprints, gameplay state checksums, and terminal results remain equal

#### Scenario: Terrain palette is unavailable
- **WHEN** a level theme references an unknown or unregistered terrain-palette ID
- **THEN** catalog/release validation fails and the level does not silently render another level's palette
