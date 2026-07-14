## ADDED Requirements

### Requirement: Versioned portable catalog schema
The system SHALL represent battle content in a `JsonUtility`-compatible catalog whose header includes non-empty `schemaVersion`, `catalogId`, `contentVersion`, and `minCodeVersion` values and whose definition collections cover plants, enemies, equipment, skills, projectiles, statuses, waves, star tiers, and battle rules.

#### Scenario: Catalog JSON round trip
- **WHEN** a valid authored catalog is exported to JSON and deserialized with `JsonUtility`
- **THEN** its header, definition counts, IDs, references, wave order, and reward values remain equivalent

### Requirement: Stable identifiers and references
Every battle definition SHALL have an explicit lowercase dotted string ID independent of enum names and ordinal values, and every cross-definition relation SHALL use a validated string reference.

#### Scenario: Reference resolution
- **WHEN** a plant references a skill and projectile and a wave references enemy IDs
- **THEN** catalog validation succeeds only if every referenced definition exists in the appropriate collection

#### Scenario: Invalid identifier
- **WHEN** a definition uses an empty, duplicate, uppercase, whitespace-containing, or malformed ID
- **THEN** validation reports a structured error identifying the category, ID or index, field, and reason

### Requirement: ScriptableObject authoring and deterministic export
The system SHALL provide a ScriptableObject authoring source and an Editor exporter that validates a deep-copied catalog, canonicalizes unordered definition collections, and writes UTF-8 JSON with normalized line endings without mutating the authoring asset.

#### Scenario: Repeated export
- **WHEN** unchanged authored content is exported twice
- **THEN** both exported files are byte-identical

#### Scenario: Rejected export
- **WHEN** authored content contains validation errors
- **THEN** the exporter fails with structured diagnostics and does not replace the last valid bundled JSON

### Requirement: Structured catalog validation
The validator SHALL report all detectable errors in one pass, including unsupported schema headers, invalid or duplicate IDs, invalid numeric ranges, missing required categories, unresolved references, duplicate star levels, invalid wave order, and incomplete required bundled content.

#### Scenario: Multiple invalid fields
- **WHEN** a catalog contains a duplicate plant ID, a missing skill reference, and a negative enemy health value
- **THEN** one validation run reports each independent problem instead of stopping at the first error

### Requirement: Read-only compiled runtime indexes
The compiler SHALL accept only a valid catalog and SHALL produce deep-copied, ordinal-ID-indexed, read-only lookups for each content category without exposing authoring assets as runtime dependencies.

#### Scenario: Compile valid catalog
- **WHEN** the bundled DTO passes validation
- **THEN** callers can resolve every definition by stable ID through typed read-only lookups

#### Scenario: Reject invalid catalog
- **WHEN** a DTO fails validation
- **THEN** compilation returns no compiled catalog and returns the complete structured validation result

### Requirement: Bundled current-content parity
The bundled catalog SHALL include exactly five current plants, four current enemies, three current equipment items, fifteen ordered waves, four star tiers, current refresh and battle rules, and current wave and milestone rewards.

#### Scenario: Bundled catalog smoke validation
- **WHEN** the independent content smoke entry validates and compiles the bundled catalog
- **THEN** all required counts, IDs, ordered wave sequences, multipliers, intervals, and rewards match the current configuration baseline

### Requirement: One-way legacy enum compatibility
The system SHALL explicitly map every current `PlantKind`, non-`None` `WeaponKind`, and `ZombieKind` value to a stable content ID without deriving IDs from enum names or ordinal casts.

#### Scenario: Exhaustive legacy mapping
- **WHEN** the compatibility smoke test enumerates all current enum values
- **THEN** each supported value resolves to the expected stable ID and each resolved ID exists in the compiled bundled catalog

### Requirement: Independent validation surfaces
The system SHALL expose content export and validation through Editor menu commands and batch-mode static entry points that do not load or execute a gameplay scene.

#### Scenario: Batch validation succeeds
- **WHEN** Unity runs the content validation static method in batch mode against the committed bundled data
- **THEN** the process exits successfully only after schema, round-trip, determinism, compilation, and parity checks pass
