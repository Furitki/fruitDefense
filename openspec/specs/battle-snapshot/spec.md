# battle-snapshot Specification

## Purpose
TBD - created by archiving change unify-battle-snapshot-contract. Update Purpose after archive.
## Requirements
### Requirement: Catalog-resolved Standard session scope
The runtime SHALL support battle snapshot export and restore only for a Standard `GameSimulation` constructed from a level resolved by a `CompiledLevelCatalog` and carrying immutable resolved source identity. The session boundary SHALL expose only `ExportCurrentSessionSnapshot` and `RestoreCurrentSessionSnapshot`, and `GameSimulation` SHALL expose only one `ExportSnapshot` and one `RestoreSnapshot` contract using `BattleSnapshot`.

#### Scenario: Export a supported Standard session
- **WHEN** a Standard battle was constructed from a successful `CompiledLevelCatalog` level resolution and exports its current session snapshot
- **THEN** export succeeds through `BattleSnapshot` and records the captured immutable source identity

#### Scenario: Unsupported construction path requests a snapshot
- **WHEN** a GM stress, content-direct, map-direct, bundled-default, or other non-catalog-resolved simulation calls export or restore
- **THEN** the operation returns `UnsupportedSessionSource` without fabricating a level identity or mutating simulation or presentation state

### Requirement: Single current v4 schema and complete JSON presence
`BattleSnapshotSchema` SHALL be the only snapshot schema source with ID `fruit-defense.battle-snapshot` and version 4, and export MUST assign both values explicitly. Raw JSON import MUST prove the presence of the schema header and every required root and nested field, object, and collection before DTO defaults or deserialized values are validated, and no former DTO, suffixed API, compatibility reader, conversion path, or dual writer SHALL remain.

#### Scenario: Complete current JSON is imported
- **WHEN** raw JSON explicitly contains the current schema ID/version and every required source, state, entity, and runtime member
- **THEN** presence validation succeeds and value/source/candidate validation may continue

#### Scenario: Header or required member is absent
- **WHEN** raw JSON omits `schemaId`, `schemaVersion`, any required scalar, runtime object, root collection, or required nested member
- **THEN** import returns `MissingRequiredField` at that member path before DTO defaults can make the payload appear valid

#### Scenario: Former schema is supplied
- **WHEN** raw JSON declares or matches a V1, V2, or V3 payload shape or any schema identity/version other than the current constants
- **THEN** import returns `UnsupportedSchema` without translating, mapping, or re-exporting the payload

### Requirement: Existing target and supplied catalog share one immutable source
A snapshot-capable `GameSimulation` SHALL retain and expose immutable resolved source identity containing exact catalog ID, content catalog ID/version, five-part level identity, gameplay-map fingerprint, and a canonical `resolvedSourceDefinitionFingerprint`. The fingerprint MUST cover complete simulation-affecting compiled battle-content definitions, resolved ordered-wave order and payloads, resolved rule values and milestone rewards, resolved theme definition values, and gameplay-map identity. `RestoreSnapshot(snapshot, catalog)` MUST resolve the snapshot level from the supplied catalog and prove that the resolved source, serialized source, and existing target source match in both explicit identity and definition fingerprint before candidate construction.

#### Scenario: Restore to an existing same-source target
- **WHEN** the current snapshot, supplied catalog resolution, and target simulation have identical catalog/content, level, map, wave, rule, theme, gameplay-map identities, and resolved-source definition fingerprints
- **THEN** restore may continue to structural and deterministic-state validation on that existing target

#### Scenario: Cross-source restore is attempted
- **WHEN** the supplied catalog or target differs in catalog, content version, level, map, wave set, rule set, theme, or gameplay-map fingerprint
- **THEN** restore returns `IncompatibleSource` with the mismatched path before candidate construction and preserves the target exactly

#### Scenario: Stable IDs hide a changed definition
- **WHEN** catalog/content versions and every component ID match but the supplied catalog changes a rule value, ordered-wave payload, theme value, or compiled battle-content definition
- **THEN** its resolved-source definition fingerprint differs and restore rejects atomically before candidate construction

### Requirement: Complete deterministic state has one owner
`BattleSnapshot` SHALL contain all supported-session state that can affect future outcomes, including `escapedEnemyCount`, logical clock/random state, phase/wave/resources/lives, next identity sequences, entities/references, projectiles, and complete entity/combat/Ability runtime. `BattleSnapshotEntityRuntime` SHALL be the only owner of slow/status runtime, `BattleSnapshotEnemy` SHALL NOT duplicate statuses, and required combat runtime SHALL NOT use a compatibility `present` sentinel.

#### Scenario: Export during active runtime
- **WHEN** export occurs with statuses, slow, projectile delivery, cooldown, periodic progress, windup/recovery, burst shots, pending event context, or root-event sequencing active
- **THEN** each outcome-affecting value appears exactly once under its authoritative snapshot owner

#### Scenario: Enemy has escaped
- **WHEN** one or more enemies have reached the goal before export
- **THEN** `escapedEnemyCount` is serialized, restored, and included in deterministic continuation and outcome checksums

#### Scenario: Standard enemy route is restored
- **WHEN** active enemies are restored into a supported single-route Standard session
- **THEN** each `Zombie.RouteId` is derived from the target's resolved primary route and no enemy route field is read from the snapshot

#### Scenario: Presentation state is active
- **WHEN** export occurs while selection, drag, modal, interpolation, reaction, battlefield motion, floating text, audio, or presentation events are active
- **THEN** those presentation-only values are absent from the snapshot and deterministic outcome projection

### Requirement: Atomic candidate restore preserves or resets the event stream exactly
Restore SHALL validate schema, presence, source, required collections, definitions, references, enums, finite values, ranges, uniqueness, and next sequences into an isolated complete candidate before one live-state commit. Success SHALL rebuild indexes, restore deterministic state, reset the frame accumulator, clear the presentation-event stream to its initial empty state, and emit no restore-success event; failure SHALL preserve every authoritative and presentation-event observation exactly.

#### Scenario: Candidate commits successfully
- **WHEN** a complete current snapshot, supplied catalog, and target source pass every validation stage
- **THEN** restore commits once, clears pending presentation events and their dropped/sequence history, emits no new event, and resumes with a zero frame accumulator

#### Scenario: Restore fails after events are pending
- **WHEN** any restore validation fails while the target event stream contains retained and dropped events
- **THEN** active state, indexes, random state, accumulator, pending event contents/order, next event sequence, and dropped count remain unchanged

### Requirement: Outcome checksum is catalog-independent and mode-complete
`OutcomeStateChecksum()` SHALL remain parameterless and SHALL hash a private catalog-independent deterministic projection rather than calling a catalog-dependent public snapshot API. The projection MUST support Standard and GM simulations and include GM route identity and escaped-enemy state where they affect future outcomes.

#### Scenario: GM checksum is calculated
- **WHEN** a multi-route GM simulation created without a `CompiledLevelCatalog` calculates its outcome checksum
- **THEN** checksum succeeds without snapshot export and distinguishes otherwise equal enemies assigned to different GM routes

#### Scenario: Presentation queue changes
- **WHEN** pending presentation events are drained, discarded, or dropped without authoritative battle-state changes
- **THEN** `OutcomeStateChecksum()` remains unchanged

### Requirement: Deterministic Standard round-trip continuation
Current snapshot JSON round trips MUST preserve deterministic continuation across Ready, Playing, and BetweenWaves for supported catalog-resolved Standard sessions.

#### Scenario: Standard branch continuation
- **WHEN** one supported branch continues for N fixed steps and another exports, JSON-round-trips, restores into a same-source target, and continues for N fixed steps
- **THEN** both branches produce the same outcome-state checksum

#### Scenario: Mid-effect and post-escape continuation
- **WHEN** the branch point includes projectile/status/Ability runtime or an enemy that escaped before export
- **THEN** the restored branch matches the unsaved branch after the same fixed steps, including escaped-enemy count
