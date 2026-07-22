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

