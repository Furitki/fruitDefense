## ADDED Requirements

### Requirement: Manifest-pinned outgame content
The authoritative game-content manifest SHALL pin one versioned outgame catalog identity and resource together with the battle and level content required by the same release.

#### Scenario: Bootstrap loads matching content
- **WHEN** the manifest, outgame catalog, battle catalog, and level catalog have matching pinned identities and versions
- **THEN** all catalogs compile before Lobby becomes interactive

#### Scenario: Outgame identity is missing or mismatched
- **WHEN** the manifest omits the outgame catalog or a pinned identity differs from the loaded data
- **THEN** Bootstrap reports a structured blocking configuration error and does not construct fallback activity or growth content

### Requirement: Typed stable outgame definitions
The outgame catalog SHALL define stable lowercase dotted IDs and typed records for items, activities, outgame growth-equipment, cultivation nodes, costs, reward grants, and growth policies, and every reference SHALL resolve to the correct definition category. Outgame growth-equipment SHALL remain distinct from battle equipment installed on fruits.

#### Scenario: Catalog contains a complete starter loop
- **WHEN** the bundled catalog is validated
- **THEN** its starter activity rewards, starter growth-equipment, growth material, first growth-equipment rank, first cultivation node, and growth-policy references all resolve

#### Scenario: Definition contains an invalid reference
- **WHEN** a growth-equipment cost references an unknown item or a level references an unknown growth policy
- **THEN** validation reports the exact definition, field, and missing identity and compilation produces no runtime indexes

### Requirement: Finite validated growth contribution schema
Growth-equipment and cultivation definitions SHALL express contributions only through the supported growth domains, attribute IDs, operation kinds, finite values, bounded ranks, and validated policy caps.

#### Scenario: Arbitrary growth attribute is authored
- **WHEN** a contribution names an unsupported attribute or operation
- **THEN** export fails without creating a runtime script, reflective lookup, or ignored contribution

### Requirement: Deterministic authoring export and compilation
Outgame authoring assets SHALL export canonical UTF-8 JSON and compile deep-copied ordinal-ID-indexed read-only lookups without runtime dependency on authoring ScriptableObjects.

#### Scenario: Unchanged outgame content is exported twice
- **WHEN** the same manifest-selected authoring assets are exported repeatedly
- **THEN** the outgame JSON bytes and computed content fingerprint are identical

#### Scenario: Invalid outgame content is exported
- **WHEN** any local or cross-catalog validation issue exists
- **THEN** export reports all detectable issues and preserves the last valid bundled output
