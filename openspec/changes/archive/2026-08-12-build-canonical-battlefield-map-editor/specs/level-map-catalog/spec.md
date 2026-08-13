## ADDED Requirements

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
