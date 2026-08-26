## MODIFIED Requirements

### Requirement: Versioned portable catalog schema
The system SHALL represent battle content in a `JsonUtility`-compatible catalog whose header includes non-empty `schemaVersion`, `catalogId`, `contentVersion`, and `minCodeVersion` values and whose definition collections cover plants, enemies, equipment, unified abilities, projectiles, statuses, waves, star tiers, and battle rules. Separate Skill and Passive collections SHALL NOT remain.

#### Scenario: Catalog JSON round trip
- **WHEN** a valid authored catalog is exported to JSON and deserialized with `JsonUtility`
- **THEN** its header, definition counts, canonical Ability IDs, references, wave order, and reward values remain equivalent

### Requirement: Stable identifiers and references
Every battle definition SHALL have an explicit lowercase dotted string ID independent of enum names and ordinal values, every cross-definition relation SHALL use a validated string reference, and migrated Ability definitions SHALL use canonical `ability.*` identities without aliases for former `skill.*` or `passive.*` IDs.

#### Scenario: Reference resolution
- **WHEN** a plant or equipment references an Ability and projectile delivery and a wave references enemy IDs
- **THEN** catalog validation succeeds only if every referenced definition exists in the appropriate collection

#### Scenario: Invalid identifier
- **WHEN** a definition uses an empty, duplicate, uppercase, whitespace-containing, malformed, or removed Skill/Passive ID
- **THEN** validation reports a structured error identifying the category, ID or index, field, and reason

### Requirement: Structured catalog validation
The validator SHALL report all detectable errors in one pass, including unsupported schema headers, invalid or duplicate IDs, invalid numeric ranges, missing required categories, unresolved Ability/delivery/payload references, unsupported activation/owner/target/delivery/effect/modifier kinds, duplicate star levels, invalid wave order, and incomplete required bundled content.

#### Scenario: Multiple invalid fields
- **WHEN** a catalog contains a duplicate plant ID, a missing Ability reference, an unsupported delivery kind, and a negative enemy health value
- **THEN** one validation run reports each independent problem instead of stopping at the first error
