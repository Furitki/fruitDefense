## ADDED Requirements

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
