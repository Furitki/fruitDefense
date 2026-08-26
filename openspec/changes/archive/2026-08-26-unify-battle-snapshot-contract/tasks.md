## 1. Inventory Callers and Establish the Baseline

- [x] 1.1 Inventory every production/test reference to V1/V2 snapshot DTOs, schema constants, JSON/export/restore APIs, conversion helpers, restore/export result codes, legacy mapping, and serialized field names before editing implementation.
- [x] 1.2 Inventory every `GameSimulation` construction path and classify catalog-resolved Standard, `ResolvedLevelDefinition`-only, bundled-default, content/map-direct, and GM sessions so only the first category becomes snapshot-capable.
- [x] 1.3 Inventory snapshot smoke registrations and designate `FruitDefense.Editor.ProjectSetup.SmokeValidate` as the sole aggregate owner; record the direct V1 registration in `P0ValidationSuite` and V2 registration in `ProjectSetup` for atomic replacement.
- [x] 1.4 Run and record the pre-cutover Unity compile, V1/V2 snapshot smokes, deterministic/GM smokes, and aggregate project smoke so later failures can be attributed to this change.

## 2. Add the v4 Foundation Without Breaking Existing Callers

- [x] 2.1 Add `BattleSnapshotSchema` with ID `fruit-defense.battle-snapshot` and version 4 plus versionless `BattleSnapshot` and supporting DTOs; make export assign the header constants explicitly instead of relying on DTO defaults.
- [x] 2.2 Add versionless export/restore results and the consolidated error taxonomy, including explicit `UnsupportedSessionSource`, `MissingRequiredField`, `SourceCatalogUnavailable`, and `IncompatibleSource`, while keeping old callers compiling until cutover.
- [x] 2.3 Add `ResolvedBattleSourceIdentity` and a catalog-backed Standard construction path that captures immutable catalog/content/composite/map identity plus `resolvedSourceDefinitionFingerprint`, computed canonically from complete compiled battle content, ordered-wave payloads, rule values/milestones, theme values, and gameplay-map fingerprint, and expose it read-only on `GameSimulation`.
- [x] 2.4 Add source comparison that resolves the supplied restore catalog and proves snapshot source, resolved source, and existing target source match in explicit identity and definition fingerprint before candidate construction; return explicit unsupported results for GM/direct/non-catalog targets.
- [x] 2.5 Add a private catalog-independent deterministic projection and migrate no-argument `OutcomeStateChecksum()` to hash it without calling any public snapshot API, retaining Standard and GM route-sensitive behavior.
- [x] 2.6 Add current export projection for `escapedEnemyCount`, required deterministic state, and single owner `BattleSnapshotEntityRuntime.statuses`; omit enemy status duplication and the `combatRuntime.present` sentinel.
- [x] 2.7 Add current candidate validation/restore internals that derive Standard `Zombie.RouteId` from the resolved primary route, restore escaped count, validate all identities/references/ranges/sequences, and commit only after a complete isolated candidate exists.
- [x] 2.8 Add the raw current JSON structural presence gate that checks header and every required root/nested field and collection before `JsonUtility` value deserialization, without adding a second value serializer or compatibility reader.
- [x] 2.9 Add raw rejection fixtures under `Assets/Editor/Tests/Fixtures/BattleSnapshot/` for V1, V2, V3, missing `schemaId`, missing `schemaVersion`, missing required root collections/objects, and representative missing nested fields/collections, with preserved Unity `.meta` files.

## 3. Perform One Atomic API and Caller Cutover

- [x] 3.1 In one atomic implementation slice, replace session APIs with `ExportCurrentSessionSnapshot`/`RestoreCurrentSessionSnapshot`, replace `GameSimulation` with its sole `ExportSnapshot`/`RestoreSnapshot`, migrate every production and test caller, replace V1/V2 smokes with `BattleSnapshotSmoke`, move aggregate registration solely to `ProjectSetup.SmokeValidate`, and delete former DTO/API/conversion/typed-fixture/mapping/result symbols, duplicate enemy statuses, and `combatRuntime.present` without aliases or adapters.
- [x] 3.2 Immediately compile all runtime and Editor assemblies after task 3.1 and fix every cutover error before starting behavioral test expansion.
- [x] 3.3 Run an immediate caller audit proving production and tests use only the current APIs, GM hosts return explicit unsupported results, `P0ValidationSuite` does not directly register snapshot smoke, and stable non-snapshot `ResolveMapIdentity` remains intact.

## 4. Prove Supported State, Source, and JSON Behavior

- [x] 4.1 In `BattleSnapshotSmoke`, cover Ready, Playing, and BetweenWaves JSON branch continuation for catalog-resolved Standard targets after identical fixed steps.
- [x] 4.2 Cover active projectile, slow/status, cooldown, periodic progress, windup/recovery, burst, pending event context, and root-event continuation with entity runtime as the sole serialized status owner.
- [x] 4.3 Exercise every raw JSON fixture through the presence gate and assert V1/V2/V3, missing header, missing required collections, and missing nested fields fail before DTO defaults or candidate construction.
- [x] 4.4 Cover same-source restore plus cross-catalog, cross-content-version, cross-level, cross-map, cross-wave, cross-rule, cross-theme, and gameplay-map mismatch; also build catalogs whose IDs/version are unchanged while rule, ordered-wave, theme, or compiled-content payload changes, and assert every definition-fingerprint mismatch rejects before mutation with its source path.
- [x] 4.5 Drive an enemy across the goal before export, then round-trip and continue both branches; assert `escapedEnemyCount`, outcome checksum, phase/lives progression, and subsequent deterministic results match.
- [x] 4.6 Assert restored Standard enemies derive `Zombie.RouteId` from the resolved primary route and that the snapshot DTO contains no enemy route field, while existing GM multi-route checksum tests still distinguish route assignments.
- [x] 4.7 Assert successful restore empties the presentation event stream, resets sequence/drop history, and emits no success event.
- [x] 4.8 For schema, presence, unsupported-source, cross-source, numeric, identity, definition, and reference failures, assert active checksum/state/indexes/random/accumulator plus pending event contents/order/next sequence/dropped count are unchanged.
- [x] 4.9 Assert GM, content-direct, map-direct, bundled-default, and `ResolvedLevelDefinition`-only simulations return `UnsupportedSessionSource` for export/restore while no-argument checksum and existing simulation behavior continue to work.

## 5. Audit the Atomic Removal

- [x] 5.1 Verify the atomic cutover deleted V1/V2 DTO/schema source files and matching `.meta` files, versioned smoke classes, conversion helpers, suffixed methods, adapters, legacy snapshot mapping/default-map constants, `combatRuntime.present`, duplicate enemy statuses, and typed legacy fixtures.
- [x] 5.2 Verify obsolete/overlapping snapshot enum values and error branches are absent and every current failure maps to the consolidated export/restore taxonomy.
- [x] 5.3 Run a repository-wide former-symbol audit for `BattleSnapshotV1`, `BattleSnapshotV2`, `BattleSnapshotV2Schema`, suffixed export/restore methods, conversion helpers, numeric-suffixed supporting DTOs, compatibility readers/writers, and legacy snapshot mapping; require zero references outside raw JSON fixture contents and historical OpenSpec evidence.
- [x] 5.4 Verify old-symbol absence through repository tooling only; remove any runtime smoke that reads source files to prove deletion, and verify no raw fixture lives in production `Resources` or playable catalogs.

## 6. Run Aggregate Gates and Synchronize Current Specs

- [x] 6.1 Run `openspec validate unify-battle-snapshot-contract --strict` and resolve every artifact or delta-spec failure.
- [x] 6.2 Run focused `BattleSnapshotSmoke`, deterministic simulation/Ability smokes, and GM multi-route determinism smokes with recorded evidence for current round trips, escape replay, source rejection, GM checksum, and event-stream preservation.
- [x] 6.3 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` as the single aggregate snapshot registration and then `P0ValidationSuite`; prove the focused snapshot suite is not registered twice.
- [x] 6.4 Run `openspec validate --all --strict` and resolve all cross-change/current-capability validation failures.
- [ ] 6.5 Synchronize `battle-snapshot`, retired `battle-snapshot-v1`, `level-selection-flow`, `battle-presentation-event-boundary`, and `local-profile-service-ports` into current specs through the accepted OpenSpec completion/archive workflow, with no manual compatibility wording retained.
- [ ] 6.6 Repeat the full former-symbol/compatibility audit after current-spec synchronization, review the working tree for generated/disposable or unrelated changes, and hand off exact validation commands and results.
