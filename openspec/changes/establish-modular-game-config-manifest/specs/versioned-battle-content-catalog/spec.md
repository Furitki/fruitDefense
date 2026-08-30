## MODIFIED Requirements

### Requirement: Versioned portable catalog schema
The system SHALL represent battle content in a `JsonUtility`-compatible catalog whose header includes non-empty `schemaVersion`, `catalogId`, `contentVersion`, and `minCodeVersion` values and whose definition collections cover plants, enemies, equipment, abilities, projectiles, statuses, waves, upgrade profiles, nursery profiles, and battle rules.

#### Scenario: Catalog JSON round trip
- **WHEN** a valid authored catalog is exported to JSON and deserialized with `JsonUtility`
- **THEN** its header, definition counts, IDs, references, wave order, upgrade tiers, nursery entries, and reward values remain equivalent

### Requirement: ScriptableObject authoring and deterministic export
The system SHALL provide manifest-selected ScriptableObject authoring and an Editor exporter that validates deep-copied modular content, canonicalizes unordered definition collections, and writes UTF-8 JSON with normalized line endings without mutating authored values or regenerating them from a production C# factory.

#### Scenario: Repeated export
- **WHEN** unchanged authored content is exported twice
- **THEN** both exported files are byte-identical

#### Scenario: Rejected export
- **WHEN** authored content contains validation errors
- **THEN** the exporter fails with structured diagnostics and does not replace the last valid bundled JSON

### Requirement: Read-only compiled runtime indexes
The compiler SHALL accept only a valid catalog and SHALL produce deep-copied, ordinal-ID-indexed, read-only lookups for every content category, including upgrade and nursery profiles, without exposing authoring assets as simulation dependencies.

#### Scenario: Compile valid catalog
- **WHEN** the bundled DTO passes validation
- **THEN** callers can resolve every definition, plant upgrade tier, and nursery profile by stable ID through typed read-only lookups

#### Scenario: Reject invalid catalog
- **WHEN** a DTO fails validation
- **THEN** compilation returns no compiled catalog and returns the complete structured validation result

### Requirement: Bundled current-content parity
The bundled catalog SHALL retain the original five plant IDs, four enemy IDs, three equipment IDs, fifteen ordered waves, current four-tier baseline upgrade profile, current refresh and battle rules, and current wave and milestone rewards, while permitting additional validated definitions and profiles that do not alter the active bundled nursery profile.

#### Scenario: Bundled catalog smoke validation
- **WHEN** the independent content smoke validates and compiles the bundled catalog
- **THEN** all required baseline IDs, ordered wave sequences, active profile multipliers, intervals, and rewards match the current configuration baseline without requiring exact total definition counts

