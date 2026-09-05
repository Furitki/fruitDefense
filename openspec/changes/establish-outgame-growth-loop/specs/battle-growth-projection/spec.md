## ADDED Requirements

### Requirement: Every gameplay selects a growth permission policy
Each playable level SHALL reference one validated growth policy defining the permitted growth domains, source filters, attribute IDs, and caps for that gameplay.

#### Scenario: Two levels allow different growth
- **WHEN** the same profile is projected for two levels with different policies
- **THEN** each result applies and suppresses sources according to its own policy without changing the profile

### Requirement: Deterministic growth projection and preview
The system SHALL resolve equipped outgame growth-equipment and purchased cultivation in stable identity order into one deterministic applied/suppressed projection, and Home SHALL display the exact projection used by Start.

#### Scenario: Player changes the selected level
- **WHEN** Home selects a level with a different growth policy
- **THEN** the visible applied attributes and suppressed-source reasons update from the shared resolver before Start is enabled

#### Scenario: Same inputs are resolved twice
- **WHEN** catalog identity, profile revision, selected level, and policy are unchanged
- **THEN** ordered source records, aggregate modifiers, and canonical fingerprint are identical

### Requirement: Launch carries an immutable growth snapshot
Every standard `BattleLaunchRequest` SHALL contain a required deep-copied growth snapshot with profile revision, policy identity, ordered source records, aggregate modifiers, content identity, and canonical fingerprint.

#### Scenario: Valid Start is activated
- **WHEN** Home has a valid selected level and growth preview
- **THEN** the launched Battle receives exactly the previewed snapshot and does not read the live player profile during initialization

#### Scenario: Snapshot does not match the resolved level
- **WHEN** the snapshot policy, content identity, values, or fingerprint differs from the selected resolved level and canonical projection
- **THEN** Battle initialization fails before simulation construction with a structured mismatch error

### Requirement: Growth is a launch baseline before transient statuses
Applied growth SHALL modify supported authored battle attributes through the deterministic effective-attribute pipeline before transient buffs and debuffs, and SHALL NOT be represented as an expiring runtime status.

#### Scenario: Growth-equipment and a timed buff affect one attribute
- **WHEN** launch growth-equipment and a combat status both modify the same supported attribute
- **THEN** the resolver applies authored base, launch baseline, permanent battle rules, and transient status operations in the documented stable order

#### Scenario: Profile changes after launch
- **WHEN** the persisted loadout or ranks change after a session has initialized
- **THEN** the running simulation and its deterministic identity remain unchanged

### Requirement: Retry and restore preserve growth identity
Settlement Retry SHALL reuse the completed session's growth snapshot with a new session ID and seed, and snapshot restore SHALL reject any missing or mismatched launch-growth policy or fingerprint.

#### Scenario: Player retries from Settlement
- **WHEN** Retry is activated without an intervening Growth edit
- **THEN** the new battle uses the same level, content version, and growth fingerprint with a new session ID and seed

#### Scenario: Battle snapshot omits launch growth
- **WHEN** restore receives a snapshot without the current growth identity or with a different fingerprint
- **THEN** restore rejects it before candidate construction and leaves the live session unchanged
