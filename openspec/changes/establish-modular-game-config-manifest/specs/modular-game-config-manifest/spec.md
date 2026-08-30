## ADDED Requirements

### Requirement: Versioned authoritative game-content manifest
The system SHALL load one bundled versioned game-content manifest that pins the battle catalog resource, battle catalog identity and version, level catalog identity, presentation catalog identity, and default nursery profile before normal route initialization completes.

#### Scenario: Valid bundled manifest
- **WHEN** Bootstrap loads the committed manifest and its referenced battle catalog
- **THEN** all pinned identities match, the battle and level catalogs compile, and normal Lobby initialization continues

#### Scenario: Manifest identity mismatch
- **WHEN** the manifest pins an ID or content version that differs from the referenced bundled data
- **THEN** startup fails with a structured blocking configuration error and does not substitute factory or default content

### Requirement: Asset-authored deterministic bundle
The system SHALL use typed ScriptableObject authoring selected by the manifest and SHALL export canonical UTF-8 manifest and battle JSON without using a production C# factory as the source of authored values.

#### Scenario: Repeated complete export
- **WHEN** unchanged manifest-selected authoring assets are exported twice
- **THEN** both manifest outputs and both battle catalog outputs are byte-identical

#### Scenario: Invalid module blocks export
- **WHEN** any selected module or cross-module reference is invalid
- **THEN** export reports all detectable structured issues and preserves the last valid runtime bundle

### Requirement: Immutable resolved configuration
The runtime SHALL compile manifest-selected content into read-only indexes before Battle and SHALL freeze the manifest, content version, level composition, upgrade profiles, and nursery profile for the lifetime of a battle.

#### Scenario: Authoring changes during a battle
- **WHEN** source assets or bundled files change after a battle has started
- **THEN** the current battle continues with the same resolved values and deterministic identity

### Requirement: Runtime and gameplay configuration separation
Deployment channel and feature policy SHALL remain in runtime configuration, while plants, upgrades, nursery selection, levels, and battle presentation bindings SHALL be owned by the game-content manifest and its selected catalogs.

#### Scenario: Runtime feature flag changes
- **WHEN** a deployment feature flag changes without a content-version change
- **THEN** no gameplay definition, upgrade curve, nursery probability, or battle presentation binding changes

