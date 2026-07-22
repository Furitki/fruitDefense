# level-map-catalog Specification

## Purpose
TBD - created by archiving change introduce-level-map-catalog. Update Purpose after archive.
## Requirements
### Requirement: Stable composite level definitions
The game SHALL define every playable level by a stable `levelId` that references exactly one stable `mapId`, `waveSetId`, `ruleSetId`, and `themeId`, and SHALL use those semantic IDs rather than display labels, enum positions, scene names, or Unity asset GUIDs as runtime identity.

#### Scenario: Resolve a supported level
- **WHEN** the catalog resolves a supported `levelId`
- **THEN** it returns that level's five stable identities and the concrete map, wave-set, rule-set, and theme definitions

#### Scenario: Reject an unknown level
- **WHEN** a launch or restore asks the catalog to resolve an unknown `levelId`
- **THEN** resolution fails with a structured unknown-level error and does not substitute the default map or another level

### Requirement: Catalog-wide reference validation
The compiled level catalog MUST reject duplicate identities, missing references, invalid P0 map topology, incompatible wave and rule counts, invalid wave or enemy references, and incomplete theme definitions before an affected level can be selected or launched.

#### Scenario: Compile a valid bundled catalog
- **WHEN** all level references resolve and every referenced definition passes its domain validation
- **THEN** compilation succeeds and exposes the complete ordered playable-level list

#### Scenario: Reject a dangling component reference
- **WHEN** a level references a missing map, wave set, rule set, or theme
- **THEN** compilation identifies the level and missing component and excludes the invalid catalog from runtime use

#### Scenario: Reject incompatible wave rules
- **WHEN** a wave set's ordered wave count or milestone bounds do not satisfy its referenced rule set
- **THEN** compilation fails with the incompatible wave-set and rule-set identities

### Requirement: P0 tile-grid map dependency
Every catalog map SHALL use the canonical cell semantics, ordered cardinal route cells, topology validation, projection, and route-tile derivation supplied by `refactor-battlefield-route-to-tile-grid`; this change MUST NOT introduce a parallel normalized polyline or stretched route-strip representation.

#### Scenario: Build a catalog map
- **WHEN** a bundled level map is constructed
- **THEN** every route position is an in-bounds route cell in the P0 map definition and its rendered tile type is derived from neighboring route cells

#### Scenario: Detect a disconnected authored route
- **WHEN** consecutive authored route cells are not cardinal neighbors or a required cell conflicts with another semantic cell
- **THEN** the P0 topology validator rejects the map before catalog compilation succeeds

### Requirement: Three bundled playable levels
The bundled catalog SHALL expose `orchard-01`, `orchard-02`, and `orchard-03` as three playable definitions with distinct stable map identities and complete wave, rule, and theme references.

#### Scenario: Inspect the bundled level list
- **WHEN** the bundled catalog is compiled
- **THEN** its playable-level order contains `orchard-01`, `orchard-02`, and `orchard-03` exactly once and every entry resolves completely

#### Scenario: Distinguish bundled map topology
- **WHEN** the three resolved maps are compared
- **THEN** their map IDs and ordered route-cell signatures are distinct

### Requirement: U-shaped teaching level
`orchard-01` SHALL provide a continuous U-shaped tile route with a teaching wave set and forgiving baseline rules that exercise planting, starting waves, movement, and merge preparation without requiring a new progression or reward system.

#### Scenario: Validate the teaching composition
- **WHEN** `orchard-01` is resolved and validated
- **THEN** its route has the entry, two U turns, exit, and core relationship required by the teaching map and its ordered teaching waves fit its rule-set bounds

### Requirement: S-shaped coverage level
`orchard-02` SHALL provide a continuous S-shaped route with alternating turns and a wave composition that surfaces coverage decisions through fast and armored enemies using existing combat definitions.

#### Scenario: Validate the coverage composition
- **WHEN** `orchard-02` is resolved and validated
- **THEN** its ordered route contains the required alternating S turns and its wave set references both fast and armored enemy definitions within its rule-set bounds

### Requirement: Core-corridor boss-pressure level
`orchard-03` SHALL provide a shorter core-corridor route whose ordered path terminates at an `Exit` cell cardinally adjacent to the declared core, and a pressure wave composition whose final wave includes an existing boss definition.

#### Scenario: Validate the pressure composition
- **WHEN** `orchard-03` is resolved and validated
- **THEN** its route is shorter than the teaching route, terminates at an `Exit` cell cardinally adjacent to its declared core, and its final configured wave includes a boss enemy

### Requirement: Session-specific waves and rules
The simulation SHALL obtain ordered waves and battle rules from the resolved level bundle and SHALL NOT construct wave identities from a global numeric naming convention or use one catalog-wide rule object for every level.

#### Scenario: Start two distinct levels
- **WHEN** sessions start for two levels with different wave-set or rule-set IDs
- **THEN** each simulation uses only the waves, wave count, milestones, and rule values referenced by its own resolved level

#### Scenario: Replay deterministically
- **WHEN** the same resolved level, content version, seed, and input sequence are simulated twice
- **THEN** both sessions produce the same gameplay state checksum and terminal result

### Requirement: Theme follows the resolved level
Battlefield presentation SHALL obtain its visual theme from the resolved `themeId` while simulation outcomes remain independent of theme-only values.

#### Scenario: Render a selected level
- **WHEN** a battle starts from a fully resolved level
- **THEN** the battlefield uses that level's theme and map while gameplay calculations use the resolved map, waves, and rules

#### Scenario: Change only the theme
- **WHEN** two deterministic simulations differ only in valid presentation theme
- **THEN** their gameplay state checksums and terminal results remain equal

### Requirement: Catalog scope excludes long-term economy
The initial level catalog MUST NOT define currencies, unlock costs, account progression, failure rewards, monetization, or chapter-map advancement as conditions for resolving or launching the three bundled levels.

#### Scenario: Launch any bundled level without progression state
- **WHEN** a valid local profile with no long-term economy fields selects any bundled level
- **THEN** catalog resolution and battle launch succeed without consulting an unlock, currency, or reward service

