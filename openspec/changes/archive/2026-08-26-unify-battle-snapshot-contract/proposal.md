## Why

Snapshot persistence currently exposes V1 and V2 DTO/API families that both serialize schema version 3 and convert through each other, while raw JSON defaults can hide missing fields and the target simulation does not retain enough immutable catalog source identity to prove a safe restore. The contract must be narrowed and made explicit before snapshot code is decomposed: one current schema for catalog-resolved Standard sessions, with no legacy reader or unsupported GM/content-direct path masquerading as restorable.

## What Changes

- **BREAKING**: Replace `BattleSnapshotV1`/`BattleSnapshotV2` and all suffixed or adapter APIs with one `BattleSnapshot` envelope using schema ID `fruit-defense.battle-snapshot` and version 4. Session APIs become `ExportCurrentSessionSnapshot`/`RestoreCurrentSessionSnapshot`; `GameSimulation` keeps one `ExportSnapshot` and one `RestoreSnapshot`.
- Limit snapshot export/restore to Standard battles constructed from a `CompiledLevelCatalog` resolution. GM stress, content-direct, map-direct, bundled-default, and other non-catalog-resolved simulations return an explicit unsupported-session result; they are not inferred or converted.
- Make a snapshot-capable `GameSimulation` retain and expose immutable resolved source identity plus a canonical resolved-source definition fingerprint covering compiled battle content, ordered waves, rule values, theme definition, and gameplay map. Restore into an existing target MUST prove that the snapshot, supplied catalog resolution, and target share both stable IDs/versions and the definition fingerprint before candidate construction; cross-catalog, cross-level, cross-rules, or same-ID-but-changed-definition restore fails before mutation.
- **BREAKING**: Reject V1, V2, V3, missing-header, and incomplete payloads. Raw JSON import presence-gates the schema header and every required field/collection before DTO defaults or `JsonUtility` values can be treated as valid; no compatibility reader, migration, default-level mapping, or dual writer remains.
- Complete the deterministic envelope with `escapedEnemyCount`. In a supported single-route Standard session, an enemy route is derived from the resolved primary route rather than serialized; entity runtime is the sole owner of slow/status state, enemy DTOs have no duplicate statuses, and combat runtime has no `present` compatibility sentinel.
- Keep no-argument `OutcomeStateChecksum()` available to Standard and GM simulations through a private catalog-independent deterministic projection. Checksum calculation does not call the catalog-dependent public snapshot API.
- Restore atomically through source validation, full presence/value/reference validation, isolated candidate construction, and one commit. Success clears the presentation-event stream without emitting a restore-success event; failure preserves pending contents, order, next sequence, and dropped count exactly.
- Audit and remove obsolete snapshot result codes, constants, fixtures, DTOs, conversions, and smoke registrations. Replace V1/V2 smoke with one `BattleSnapshotSmoke`, raw JSON fixtures under `Assets/Editor/Tests/Fixtures/`, and one aggregate registration owned by `FruitDefense.Editor.ProjectSetup.SmokeValidate`.

## Capabilities

### New Capabilities

- `battle-snapshot`: Defines the single current v4 envelope and API for catalog-resolved Standard sessions, immutable source matching, complete deterministic state, strict raw JSON presence validation, atomic restore, and deterministic continuation.

### Modified Capabilities

- `battle-snapshot-v1`: Retires every version-labelled requirement and legacy mapping after its still-current guarantees move to `battle-snapshot`; former payloads and APIs remain unsupported.
- `level-selection-flow`: Requires current snapshot restore to match the existing target session's immutable catalog-resolved source and removes all legacy/default-level inference.
- `battle-presentation-event-boundary`: Replaces the stale `BattleSnapshotV1` reference with `BattleSnapshot` and defines exact event-stream behavior for successful and failed restore.
- `local-profile-service-ports`: Preserves the P0 rule that running battles are not persisted while removing the stale V1 type reference.

## Impact

- Core/session APIs and DTOs: `GameSimulation`, `GameSimulationSnapshot`, snapshot DTO/result files, `IBattleSessionHost`, `FruitDefenseGame`, GM host implementations, and every production caller.
- Construction/source boundaries: the release resolved-level path must provide its `CompiledLevelCatalog` source; direct and GM constructors keep simulation/checksum behavior but report snapshot unsupported.
- Validation: one `BattleSnapshotSmoke`, deterministic and GM checksum coverage, cross-source and same-ID-mutated-definition rejection, escaped-enemy/event-stream tests, raw former/missing-field JSON fixtures under Editor test fixtures, aggregate smoke, and repository-wide former-symbol auditing.
- Previously serialized payloads become unsupported. No player-visible UI, balance, authored content, release scene flow, profile persistence scope, or presentation rendering standard changes.
