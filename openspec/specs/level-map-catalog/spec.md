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

### Requirement: P0 tile-grid map dependency
Every catalog map SHALL use the layered battlefield compiler, composable gameplay cells, stable named ordered cardinal routes, typed markers, topology validation, shared projection, and derived route-tile descriptors supplied by the canonical battlefield model; it MUST NOT introduce a parallel exclusive-role grid, normalized polyline, stretched route strip, marker transform hierarchy, or visual-art-derived gameplay representation.

#### Scenario: Build a catalog map
- **WHEN** a bundled level map is constructed
- **THEN** every route position is an in-bounds cell in a stable named route, spawn/goal/core semantics resolve through typed markers, and rendered route tiles are derived from neighboring ordered route cells and semantic surfaces

#### Scenario: Detect an invalid layered map
- **WHEN** route cells disconnect, required markers conflict, layer coverage is incomplete, a visual surface is unknown, or a gameplay marker violates its cell capabilities
- **THEN** layered-map validation rejects the map before catalog compilation succeeds

### Requirement: Three bundled playable levels
The bundled catalog SHALL expose `orchard-01`, `orchard-02`, and `orchard-03` as three playable definitions with distinct stable map identities and complete wave, rule, and theme references.

#### Scenario: Inspect the bundled level list
- **WHEN** the bundled catalog is compiled
- **THEN** its playable-level order contains `orchard-01`, `orchard-02`, and `orchard-03` exactly once and every entry resolves completely

#### Scenario: Distinguish bundled map topology
- **WHEN** the three resolved maps are compared
- **THEN** their map IDs and ordered route-cell signatures are distinct

### Requirement: U-shaped teaching level
`orchard-01` SHALL provide a continuous U-shaped tile route with a teaching wave set and forgiving baseline rules that exercise planting, starting waves, movement, and merge preparation without requiring a new progression or reward system. Its presentation SHALL resolve `palette.orchard-01.square-grid` and SHALL describe every map cell as exactly one base-only square surface: 35 plantable interior cells use the approved production grass tile and the 21-cell U-shaped route/core frame uses the approved production soil tile, with no landform, contour, or edge overlay identities.

#### Scenario: Validate the teaching composition
- **WHEN** `orchard-01` is resolved and validated
- **THEN** its route has the entry, two U turns, exit, and core relationship required by the teaching map, its ordered teaching waves fit its rule-set bounds, its theme resolves `palette.orchard-01.square-grid`, and its 8×7 visual map contains exactly 35 base-only grass cells and 21 base-only soil cells

#### Scenario: Preserve teaching-level gameplay
- **WHEN** the square presentation is applied to `orchard-01`
- **THEN** its stable level and map identities, gameplay capabilities, route topology, markers, projection, hit-test geometry, deterministic simulation, persistence, and release scene flow remain unchanged

#### Scenario: Keep later bundled levels isolated
- **WHEN** all three bundled levels are resolved after the first-level square presentation is applied
- **THEN** only `orchard-01` references `palette.orchard-01.square-grid`, while `orchard-02` and `orchard-03` continue to reference `palette.orchard.default` and retain their layered terrain compositions

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
Battlefield presentation SHALL obtain its visual theme and layered terrain palette from the resolved `themeId`, while base surfaces, landforms, ordered edge styles, and all other theme-only values remain independent of simulation outcomes.

#### Scenario: Render a selected level
- **WHEN** a battle starts from a fully resolved level
- **THEN** the battlefield uses that level's theme, layered terrain composition, and map while gameplay uses only the resolved gameplay view, waves, and rules

#### Scenario: Change only layered presentation
- **WHEN** two deterministic simulations differ only in valid base surfaces, landforms, pair edges, palette assets, or other theme values
- **THEN** their gameplay state checksums and terminal results remain equal

### Requirement: Catalog scope excludes long-term economy
The initial level catalog MUST NOT define currencies, unlock costs, account progression, failure rewards, monetization, or chapter-map advancement as conditions for resolving or launching the three bundled levels.

#### Scenario: Launch any bundled level without progression state
- **WHEN** a valid local profile with no long-term economy fields selects any bundled level
- **THEN** catalog resolution and battle launch succeed without consulting an unlock, currency, or reward service

### Requirement: Deterministic editor-authored level publication
The editor SHALL use one explicit publication manifest as the only authority for the published authored-map set, SHALL fully rebuild one deterministic generated runtime catalog from its ordered entries, SHALL combine each map with exactly one explicitly selected existing template level under a stable unique level ID, and MUST leave the last valid generated catalog unchanged when publication fails. The generated resource MUST NOT be edited manually, used as rebuild input, or treated as an independent authoring source.

#### Scenario: Publish a valid authored map
- **WHEN** an authored map, level ID, and template references pass canonical and catalog validation
- **THEN** the generated catalog contains a normalized deep copy ordered by manifest order and stable ID, atomically inherits the template's wave-set, rule-set, and theme, and normal catalog compilation resolves the new complete level without a C# edit

#### Scenario: Rebuild the publication set
- **WHEN** the manifest publishes A then adds B, removes A, or rebuilds after the generated resource is deleted
- **THEN** each full rebuild exactly reflects the current manifest, preserves unrelated entries, removes cancelled entries, is idempotent, and produces content-equivalent output from identical inputs

#### Scenario: Reject duplicate published identity
- **WHEN** an authored map or level duplicates a bundled or published stable ID
- **THEN** publication reports every conflict, does not replace either existing definition, and does not modify the last valid generated catalog

#### Scenario: Reject invalid template reference
- **WHEN** a published entry names an unknown template level or that template's wave set, rule set, theme, or terrain palette cannot resolve
- **THEN** publication fails with the owning map, template level, and missing stable reference and the level cannot be launched

#### Scenario: Reject incompatible terrain palette
- **WHEN** a map uses a semantic surface or exact directed edge that the template theme's real `BattlefieldTerrainPalette` does not bind, or the release Battle palette registry omits that palette
- **THEN** publication fails with the map ID, coordinate, surface pair, edge style, and palette ID and leaves the last valid generated catalog unchanged

### Requirement: Published maps use the normal runtime path
Every published authored level SHALL be appended to the normal level catalog and SHALL use the existing layered compiler, deterministic simulation, level identity selection, `BattlefieldProjection`, theme/palette resolution, Battle scene, and settlement flow without a demo-only runtime fallback.

#### Scenario: Launch a published authored level
- **WHEN** the editor or Lobby selects a valid published authored level ID
- **THEN** the game reloads the generated resource, resolves its map and atomically inherited template waves, rules, and theme through the normal compiled catalog and AppFlow, reports the expected levelId/mapId, and never substitutes a bundled default map

#### Scenario: Draft changes after publication
- **WHEN** an author modifies a draft asset without rebuilding the publication manifest
- **THEN** the currently generated catalog and any active Battle remain unchanged until a later successful full publication rebuild

#### Scenario: Published resource is absent
- **WHEN** no generated authored-map catalog is present
- **THEN** the three bundled levels compile and run unchanged in their existing stable order

### Requirement: Production ownership of first-level square terrain
The release Battle SHALL bind `orchard-01` only to production-owned normalized texture and palette assets, SHALL register every terrain palette referenced by the bundled catalog, and MUST NOT depend on trial scenes, prompt records, provenance records, review masters, or trial palettes.

#### Scenario: Inspect release dependencies
- **WHEN** the Battle scene and first-level palette dependencies are validated
- **THEN** both required production palettes are registered, the first-level grass and soil bindings resolve to normalized 64×64 production textures, and no release dependency path is under `Assets/LayeredTerrain/Trials/`

#### Scenario: Build ordinary WebGL
- **WHEN** the normal WebGL build launches `Bootstrap → Lobby → Battle` for `orchard-01` at the portrait acceptance viewport
- **THEN** the real Battle canvas shows the approved 7×5 grass field inside the soil U-frame with continuous square-cell coverage, readable gameplay content, and no missing-palette diagnostic
