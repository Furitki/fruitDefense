## ADDED Requirements

### Requirement: Canonical three-layer battlefield definition
Every battlefield map SHALL define one complete semantic visual-surface layout, one complete gameplay cell-capability/collision layout, stable named ordered routes, and a typed marker layer under one versioned map identity. Simulation MUST NOT infer gameplay rules from visual surfaces, and presentation MUST NOT mutate gameplay topology or marker state.

#### Scenario: Visual and gameplay meanings differ
- **WHEN** a grass-surface cell does not carry the plantable capability
- **THEN** the presenter renders grass, placement remains illegal, and the map passes validation when no marker requires plantability at that cell

#### Scenario: Layer coverage is incomplete
- **WHEN** either the visual-surface layout or gameplay-cell layout does not contain exactly `GridWidth × GridHeight` entries
- **THEN** map compilation fails with the missing or excess layer and cell index identified

### Requirement: Composable gameplay cell capabilities and collision channels
Gameplay cells SHALL compile a finite validated set of stable capability and collision-channel identifiers into deterministic runtime masks, SHALL permit compatible capabilities on the same cell, and SHALL expose gameplay queries without Unity physics, NavMesh, or presentation asset dependencies.

#### Scenario: Cell supports several capabilities
- **WHEN** one authored cell is enemy-traversable and item-spawn-compatible but not plantable
- **THEN** all declared capabilities compile independently and a planting query remains false

#### Scenario: Unknown gameplay capability
- **WHEN** a map declares a capability or collision-channel identifier that the compiler does not support
- **THEN** compilation fails with the map, cell, and unknown identifier reported

### Requirement: Stable typed gameplay markers
Every gameplay marker SHALL have a stable unique marker ID, a finite marker kind, an in-bounds cell, and all kind-specific typed references required by that kind. Multiple compatible markers MAY occupy one cell, while invalid multiplicity, missing references, incompatible combinations, and capability violations MUST be rejected before a level is selectable.

#### Scenario: Enemy spawn references a route
- **WHEN** an `EnemySpawn` marker references a valid route and its configured start cell
- **THEN** compilation indexes the marker by ID and route without converting it into an exclusive cell role

#### Scenario: Initial flowerpot candidate is invalid
- **WHEN** an `InitialPotCandidate` marker belongs to a valid group but its cell is not plantable
- **THEN** compilation fails with the marker ID, group ID, and cell

#### Scenario: Compatible markers share a cell
- **WHEN** an item-spawn marker and a trigger marker occupy the same compatible cell
- **THEN** both markers remain independently addressable by stable ID

### Requirement: Bounded current execution profile
The layered schema SHALL support stable collections of named routes and markers, while the current battle execution profile MUST require exactly one primary enemy route, one matching enemy-spawn marker at its first cell, one matching route-goal marker at its last cell, and one core marker satisfying the existing goal-to-core relationship.

#### Scenario: Current bundled map compiles
- **WHEN** a migrated bundled map supplies `route.main` with matching spawn, goal, and core markers
- **THEN** compilation exposes the same entry, exit, core, route sampling, and life-loss behavior as the current map

#### Scenario: Additional active route is authored
- **WHEN** a map selects more than one route for current enemy execution without a future multi-route capability
- **THEN** compilation rejects the unsupported execution profile instead of silently choosing a route

### Requirement: Deterministic legacy-compatible compilation
The compiler SHALL derive read-only compatibility views for plantable cells, route cells/nodes/descriptors, entry, exit, core, and initial-flowerpot groups from the layered source, and the three bundled maps MUST preserve their existing gameplay geometry and deterministic results through migration.

#### Scenario: Bundled maps are migrated
- **WHEN** `orchard-01`, `orchard-02`, and `orchard-03` compile from layered definitions
- **THEN** their grid sizes, ordered routes, core positions, initial-pot candidates/counts, route lengths, projections, and deterministic battle outcomes match the recorded pre-migration fixtures

#### Scenario: Legacy and layered sources disagree
- **WHEN** a parity fixture compiles legacy and layered definitions with different gameplay topology or markers
- **THEN** validation fails with the first differing canonical field rather than accepting two runtime truths

### Requirement: Layer-specific deterministic identity
The system SHALL compute gameplay map identity from dimensions, gameplay cells, collision channels, ordered routes, gameplay marker groups, gameplay markers, and gameplay-affecting references in canonical order, and SHALL exclude semantic visual surfaces, terrain palettes, sprites, and other presentation-only values from simulation identity.

#### Scenario: Only visual surface changes
- **WHEN** two valid maps have identical gameplay topology and markers but different surface layouts or terrain palettes
- **THEN** their gameplay map fingerprints and deterministic simulation outcomes are equal

#### Scenario: Gameplay marker changes
- **WHEN** a spawn, goal, core, initial-pot marker, route, capability, or collision channel changes
- **THEN** the gameplay map fingerprint changes deterministically
