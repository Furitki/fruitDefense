# local-profile-service-ports Specification

## Purpose
TBD - created by archiving change add-local-profile-and-service-ports. Update Purpose after archive.
## Requirements
### Requirement: Versioned non-economic profile
The P0 profile SHALL use `PlayerProfileEnvelopeV1` and SHALL contain only identity/revision metadata, settings, last selected level, and shell preferences.

#### Scenario: New player
- **WHEN** no valid local profile exists
- **THEN** the store returns a valid V1 default profile with a new profile ID and `orchard-01` as the default level

#### Scenario: Unsupported schema
- **WHEN** stored JSON declares an unsupported profile schema
- **THEN** loading returns a structured incompatibility result without interpreting it as V1

### Requirement: Narrow WebGL-compatible service ports
Profile and bundled-config operations MUST use coroutine/callback service ports and callers MUST NOT depend on files, PlayerPrefs, SDK types, threads, or a generic backend facade.

#### Scenario: Substitute backend
- **WHEN** the composition root replaces the local profile implementation with a future cloud implementation
- **THEN** Lobby and settings callers require no interface change

### Requirement: Recoverable local persistence
The local profile store SHALL validate complete JSON before promotion and SHALL recover from missing or corrupt primary data using backup or defaults.

#### Scenario: Atomic desktop save
- **WHEN** an Editor profile save succeeds
- **THEN** the new validated JSON replaces the primary atomically and the previous valid value is retained as backup

#### Scenario: Corrupt primary
- **WHEN** the primary profile cannot be parsed or validated
- **THEN** it is quarantined, the backup is attempted, and defaults are returned if no valid copy exists

#### Scenario: Web profile save
- **WHEN** WebGL saves the small profile
- **THEN** validated JSON is promoted through staging/primary/backup PlayerPrefs keys and `PlayerPrefs.Save` is invoked

### Requirement: Bundled runtime configuration
P0 SHALL provide immutable bundled configuration with schema version, config version, content channel, bundled content version, and feature flags.

#### Scenario: P0 offline start
- **WHEN** no network or production remote-config implementation is present
- **THEN** Bootstrap receives the bundled configuration and can enter Lobby without a network request

### Requirement: No automatic battle resume in P0
P0 profile services MUST NOT automatically persist or restore a running Battle, regardless of the current battle snapshot schema or session API.

#### Scenario: Profile save during battle
- **WHEN** settings or shell profile data is saved while Battle exists
- **THEN** no `BattleSnapshot` or current-session snapshot payload is written by the profile store

