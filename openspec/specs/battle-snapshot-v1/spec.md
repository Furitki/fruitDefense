# battle-snapshot-v1 Specification

## Purpose
TBD - created by archiving change add-battle-snapshot-v1. Update Purpose after archive.
## Requirements
### Requirement: Versioned snapshot envelope
`BattleSnapshotV1` SHALL identify schema, catalog, content, and map versions and SHALL contain every simulation value that can affect future battle outcomes.

#### Scenario: Export during active effects
- **WHEN** a snapshot is exported while projectiles, statuses, cooldowns, or burst shots are pending
- **THEN** those pending outcome-affecting values are present in the snapshot

#### Scenario: Presentation exclusion
- **WHEN** a snapshot is exported while selection, drag, modal, floating text, or temporary visual effects are active
- **THEN** those presentation-only values are absent

### Requirement: Exact content pinning
Restore MUST require the exact catalog ID and content version recorded by the snapshot and MUST NOT map the snapshot to newer balance data automatically.

#### Scenario: Matching catalog
- **WHEN** the exact compiled catalog is supplied
- **THEN** restore may continue to structural validation

#### Scenario: Missing pinned catalog
- **WHEN** the recorded content version cannot be supplied
- **THEN** restore returns `ContentUnavailable` and leaves the active simulation and snapshot unchanged

### Requirement: Atomic validated restore
Restore SHALL validate schema, IDs, references, ranges, finite numbers, and next-entity identity before replacing active simulation state.

#### Scenario: Invalid reference
- **WHEN** a projectile or status references a missing entity or definition
- **THEN** restore returns a structured validation failure and does not partially mutate the simulation

#### Scenario: Successful restore
- **WHEN** a valid V1 snapshot and matching catalog are supplied
- **THEN** logical step and random state are restored, runtime indexes are rebuilt, and the frame accumulator is zero

### Requirement: Deterministic round-trip continuation
Snapshot JSON round trips MUST preserve deterministic continuation across Ready, Playing, and BetweenWaves phases.

#### Scenario: Branch continuation
- **WHEN** one branch continues for N fixed steps and another exports, JSON-round-trips, restores, and continues for N fixed steps
- **THEN** both branches have the same outcome-state checksum

#### Scenario: Mid-effect continuation
- **WHEN** the branch point includes a projectile, burn, slow, ice count, or machine-gun burst
- **THEN** restored continuation matches the unsaved branch

### Requirement: Layer-aware gameplay map identity
Snapshot and deterministic continuation validation SHALL identify a battlefield's gameplay definition from its dimensions, compiled gameplay cells/collision channels, ordered routes, gameplay marker groups, gameplay markers, and gameplay-affecting references in canonical order, and SHALL exclude semantic visual surfaces, terrain palettes, sprites, and other presentation-only values from gameplay state identity without weakening exact catalog/content pinning.

#### Scenario: Presentation-only map change
- **WHEN** a matching supported catalog changes only a map's semantic surface layout or registered terrain palette while retaining identical gameplay topology and markers
- **THEN** the gameplay map fingerprint and deterministic outcome-state checksum are unchanged

#### Scenario: Gameplay topology changes
- **WHEN** a cell capability, collision channel, ordered route, spawn, goal, core, or initial-pot marker changes
- **THEN** the gameplay map fingerprint differs and restore cannot treat the changed gameplay map as the original definition

#### Scenario: Existing bundled snapshot is restored
- **WHEN** a supported pre-migration snapshot for a bundled map is restored against its exact migrated catalog/content version
- **THEN** the compatibility mapping resolves the same stable map identity and deterministic continuation matches the pre-migration outcome
