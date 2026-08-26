## Context

`BattleSnapshotV1` and `BattleSnapshotV2` both write schema version 3. V1 embeds V2 combat runtime, V2 reuses V1 entity DTOs, V2 export is built from V1, and V2 restore converts back to a V1 gameplay DTO before validation. The old raw JSON entry points deserialize directly with DTO initializers, so a missing header or collection can acquire a plausible default before validation.

`GameSimulation` has resolved-level, content-direct, map-direct, bundled-default, and GM construction paths. Only a resolved-level Standard battle has the catalog/composite identity needed by the current snapshot requirements, but the target instance does not retain the originating `CompiledLevelCatalog` identity. Meanwhile `OutcomeStateChecksum()` calls the legacy export path, which incorrectly couples checksum availability to persistence and would break GM multi-route determinism if export is narrowed.

This is an intentional persistence break. It must preserve deterministic Standard continuation, no-argument GM checksum behavior, and the state-external presentation-event boundary without retaining old readers or API aliases.

## Goals / Non-Goals

**Goals:**

- Support exactly one current v4 snapshot contract for catalog-resolved Standard sessions.
- Make the existing restore target prove immutable source equality before any candidate or live-state work.
- Presence-gate raw JSON independently of DTO defaults and validate all required state atomically.
- Include escaped-enemy state and remove duplicate status ownership and compatibility sentinels.
- Preserve a catalog-independent, no-argument deterministic checksum for Standard and GM modes.
- Delete all former DTO/API/conversion paths and keep one focused and one aggregate validation authority.

**Non-Goals:**

- Snapshot export or restore for GM stress, multi-route, content-direct, map-direct, bundled-default, or `ResolvedLevelDefinition`-only construction paths.
- Creating a simulation factory as part of restore or restoring into a newly created replacement target.
- Reading, translating, upgrading, or re-exporting V1, V2, V3, legacy single-map, or incomplete payloads.
- Persisting a running battle through P0 profile services.
- Changing gameplay balance, authored content, fixed-step rules, player-visible UI, scene flow, or presentation rendering.

## Decisions

### 1. Snapshot support belongs only to catalog-resolved Standard sessions

Add a catalog-backed Standard constructor/factory entry that takes a `CompiledLevelCatalog`, a `levelId`, and a seed, resolves the level internally, and captures an immutable `ResolvedBattleSourceIdentity`. The identity exposes ordinally comparable `catalogId`, `contentCatalogId`, `contentVersion`, the five-part `LevelCompositeIdentity`, the gameplay-map fingerprint, and `resolvedSourceDefinitionFingerprint`.

`resolvedSourceDefinitionFingerprint` is a SHA-256 hash over one canonical, ordinally ordered, invariant-culture projection of: catalog/content IDs and version; all five composite IDs; the map gameplay fingerprint; the full compiled battle-content definitions that can affect simulation (including sorted plants, enemies, equipment, abilities, statuses, projectiles, waves, and battle rules); the resolved ordered-wave sequence and every wave payload value; every resolved rule value and ordered milestone reward; and every resolved theme definition value. It deliberately uses the gameplay-map fingerprint rather than presentation-only map surfaces. Stable IDs/version remain independently serialized and validated, but they are not trusted as proof that their definitions are unchanged.

The existing target `GameSimulation` is restored in place; restore does not create a replacement simulation. `RestoreSnapshot(snapshot, catalog)` first requires `Mode == Standard`, a non-null captured source identity, a current snapshot source matching that identity, and a successful resolution of the snapshot `levelId` from the supplied catalog whose immutable source identity equals the target's. Snapshot, supplied resolution, and target must match both the explicit fields and `resolvedSourceDefinitionFingerprint`. Cross-catalog, cross-content-version, cross-level, cross-wave, cross-rules, cross-theme, gameplay-map, and same-ID/same-version-but-changed-definition mismatches all return `IncompatibleSource` before candidate construction.

GM, content/map-direct, bundled-default, and `ResolvedLevelDefinition`-only instances expose no fabricated source identity. Their snapshot APIs return `UnsupportedSessionSource`; their ordinary simulation and checksum behavior remains available.

Alternative considered: let restore construct a new simulation from the supplied catalog. Rejected because session ownership, event consumers, and host lifecycle belong to the existing target, and a factory would hide cross-target mistakes rather than reject them.

### 2. Use one current DTO/API and one audited result taxonomy

The serialized root is `BattleSnapshot`; supporting types are versionless. `BattleSnapshotSchema` is the only schema source and owns `Id = "fruit-defense.battle-snapshot"` and `Version = 4`. Export assigns both constants explicitly rather than relying on DTO field initializers.

The session boundary exposes only `ExportCurrentSessionSnapshot` and `RestoreCurrentSessionSnapshot`. `GameSimulation` exposes only `BattleSnapshotExportResult ExportSnapshot()` and `BattleSnapshotRestoreResult RestoreSnapshot(BattleSnapshot snapshot, CompiledLevelCatalog availableCatalog)`. Raw JSON is handled by a dedicated current-schema reader/serializer before the typed restore API, not by V1/V2 overloads.

`BattleSnapshotExportCode` contains `Success` and `UnsupportedSessionSource`. The restore taxonomy is consolidated to `Success`, `InvalidPayload`, `MissingRequiredField`, `UnsupportedSchema`, `UnsupportedSessionSource`, `SourceCatalogUnavailable`, `IncompatibleSource`, `UnknownDefinition`, `InvalidReference`, `InvalidNumericValue`, and `InvalidIdentity`. Obsolete V1/V2-specific, default-map, and overlapping catalog/map/level codes are deleted with their callers.

Alternative considered: rename V2 and retain forwarding overloads. Rejected because aliases and overlapping error codes would preserve two behavioral contracts.

### 3. Presence-gate raw JSON before DTO validation

`BattleSnapshot` required members do not use valid-looking schema, identity, collection, or nested runtime defaults. Export builds a complete object and explicitly assigns all required values.

The current JSON reader first tokenizes the JSON object/array structure into member paths and validates presence of `schemaId`, `schemaVersion`, every source/state scalar, every required root collection/object, and every required member of collection elements and nested combat/entity/Ability/status entries. Only after that gate passes may `JsonUtility` deserialize values for schema, type, range, identity, and reference validation. Missing and explicit null collections both fail; a field initializer can never satisfy the presence gate.

Raw fixtures live under `Assets/Editor/Tests/Fixtures/BattleSnapshot/` and cover former V1, V2, and V3 headers/shapes, missing `schemaId`, missing `schemaVersion`, each required root collection/object, and representative missing nested scalar/collection members. They are loaded explicitly by Editor tests and never placed in production `Resources`.

Alternative considered: infer shape from field presence after `JsonUtility` deserialization. Rejected because missing numeric fields can become valid zero values and former version-3 shapes overlap the current DTO.

### 4. Define one deterministic state owner for every value

The envelope includes `resolvedSourceDefinitionFingerprint` and `escapedEnemyCount`; escaped count restores into `GameState.EscapedEnemies`. `BattleSnapshotEntityRuntime.statuses` is the only serialized owner of slow/status runtime; `BattleSnapshotEnemy` has no status collection. `BattleSnapshotCombatRuntime` and all required runtime collections are mandatory and the old `present` sentinel is removed.

Supported Standard maps have one resolved primary route. `Zombie.RouteId` is derived from that resolved definition during restore and is not a snapshot field. This is not generalized to GM: GM is snapshot-unsupported and its multi-route `RouteId` remains part of the private deterministic checksum projection.

The complete envelope still contains logical step/random state, phase/wave/resource/life state, next entity/status/combat-event sequences, entities/references, projectiles, entity statuses, and complete Ability phase/cooldown/periodic/windup/recovery/burst/pending/root-event runtime. Presentation-only state is excluded.

### 5. Separate checksum projection from persistence

`OutcomeStateChecksum()` remains public and parameterless. It hashes a private catalog-independent deterministic projection built directly from authoritative simulation state. The projection supports every current construction mode and includes mode, map/gameplay identity needed for deterministic comparison, escaped-enemy count, GM zombie route IDs, all entity/runtime state, and deterministic sequences.

`ExportSnapshot()` may reuse private projection helpers, but `OutcomeStateChecksum()` never calls a public snapshot API and never requires a catalog or captured snapshot source. Snapshot support can therefore fail closed for GM/direct sessions without weakening deterministic validation.

Alternative considered: pass a catalog into checksum. Rejected because catalog identity is persistence metadata, not required for deterministic GM outcome comparison.

### 6. Validate source, build a candidate, then commit once

Restore order is fixed:

1. Reject unsupported target mode/source and null catalogs.
2. Validate current schema and complete presence-gated DTO input.
3. Resolve the supplied catalog and compare its immutable resolved source to both snapshot and existing target.
4. Validate required collections, enums, finite values, ranges, stable definitions, uniqueness, cross-references, `escapedEnemyCount`, and next entity/status/combat-event sequences.
5. Build an isolated candidate containing complete `GameState`, entity/combat/Ability runtime, random/clock state, and rebuilt indexes. Candidate collections share no mutable state with the target.
6. Commit state/random/indexes once, reset the frame accumulator, and reset the presentation-event stream to empty with initial sequence/drop counters. Restore emits no success event.

Any failure leaves live state, random state, indexes, accumulator, pending presentation-event contents and order, next event sequence, and dropped count byte-for-byte/logically unchanged.

Alternative considered: discard presentation events before validation. Rejected because failed restore is observationally a no-op and must not consume transient output.

### 7. Replace validation ownership directly

`BattleSnapshotSmoke` replaces both versioned smoke classes. `FruitDefense.Editor.ProjectSetup.SmokeValidate` is the single aggregate registration authority; `P0ValidationSuite` reaches it through its existing project-smoke call and removes its separate snapshot registration.

The focused suite covers supported phases and active effects, raw missing-field/former-schema rejection, cross-source restore, escaped-enemy continuation, derived Standard route behavior, unique entity-runtime status ownership, event-stream success reset and failure preservation, and mutation-free failures. Former-symbol absence is verified by repository audit, not a runtime test that reads source files.

## Risks / Trade-offs

- [Existing snapshots no longer load] → Return `UnsupportedSchema`; ship no reader, migration, reset fallback, or dual writer.
- [Catalog-backed construction changes production call sites] → Inventory first, add the source identity path additively, then cut production API/callers/tests over atomically and compile immediately.
- [A catalog reuses IDs/version for changed definitions] → Compare the canonical definition fingerprint across snapshot, supplied resolution, and target; test mutated rule, wave, theme, and compiled-content payloads with unchanged metadata.
- [Presence tokenization becomes a second serializer] → Limit it to structural member-path presence; `JsonUtility` remains the value deserializer and serializer.
- [Duplicate or omitted state changes deterministic continuation] → Enforce entity-runtime-only status ownership and add escaped-enemy/mid-effect branch tests before old suites are deleted.
- [Failed restore mutates event delivery metadata] → Snapshot event-stream observations before each failure test and assert pending values/order, next sequence, and dropped count exactly.
- [GM checksum regresses when snapshot is narrowed] → Migrate checksum to the private projection before the public API cutover and retain existing multi-route determinism coverage.

## Migration Plan

1. Inventory every API/caller/constructor/result/fixture/aggregate registration and capture a clean compile/smoke baseline.
2. Add v4 DTO/results, raw JSON presence reader, catalog-backed source identity, private deterministic projection, and candidate validation additively while old callers still compile.
3. In one atomic cutover, replace production/session APIs and all callers, replace focused tests/aggregate ownership, and delete old DTOs, APIs, conversions, fixtures, mappings, and obsolete result codes.
4. Compile immediately; do not leave an intermediate revision with removed APIs and unmigrated callers.
5. Run focused, deterministic, GM, aggregate, strict change/all-spec validation and former-symbol/compatibility audits, then synchronize the accepted current capabilities through the OpenSpec completion/archive workflow.

Rollback is a source-level revert of the complete cutover. It does not add a runtime compatibility path.

## Open Questions

None. Supported session scope, target/source matching, schema identity/version, API names, JSON presence policy, state ownership, checksum independence, event-stream behavior, and aggregate registration ownership are fixed.
