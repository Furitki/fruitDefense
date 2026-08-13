## MODIFIED Requirements

### Requirement: Catalog-wide reference validation
The compiled level catalog MUST reject duplicate identities, missing references, invalid gameplay topology, incompatible wave and rule counts, invalid content references, incomplete themes, incomplete visual-cell coverage, unknown base or landform materials, and unavailable ordered pair-edge styles before an affected level can be selected or launched.

#### Scenario: Compile a valid bundled catalog
- **WHEN** all level, gameplay, theme, layered terrain, and content references resolve and pass domain validation
- **THEN** compilation succeeds and exposes the complete ordered playable-level list

#### Scenario: Reject a dangling component reference
- **WHEN** a level references a missing map, wave set, rule set, theme, terrain palette, material, or ordered edge binding
- **THEN** compilation identifies the owning level and missing reference and excludes the invalid catalog from runtime use

#### Scenario: Reject incompatible wave rules
- **WHEN** a wave set's ordered wave count or milestone bounds do not satisfy its referenced rule set
- **THEN** compilation fails with the incompatible wave-set and rule-set identities

#### Scenario: Reject incomplete visual coverage
- **WHEN** a map has a missing base cell, an unknown landform, an edge without a landform, or a requested pair style not registered for its exact direction
- **THEN** compilation fails with the map, cell, surface, and edge identities needed to repair the authoring data

### Requirement: Theme follows the resolved level
Battlefield presentation SHALL obtain its visual theme and layered terrain palette from the resolved `themeId`, while base surfaces, landforms, ordered edge styles, and all other theme-only values remain independent of simulation outcomes.

#### Scenario: Render a selected level
- **WHEN** a battle starts from a fully resolved level
- **THEN** the battlefield uses that level's theme, layered terrain composition, and map while gameplay uses only the resolved gameplay view, waves, and rules

#### Scenario: Change only layered presentation
- **WHEN** two deterministic simulations differ only in valid base surfaces, landforms, pair edges, palette assets, or other theme values
- **THEN** their gameplay state checksums and terminal results remain equal

