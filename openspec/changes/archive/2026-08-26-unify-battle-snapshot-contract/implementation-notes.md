## Additive Foundation Inventory

Recorded before implementation on 2026-08-26 against Unity `6000.3.19f1`.

### Snapshot symbols and callers

- DTO/schema ownership: `Assets/Scripts/Core/BattleSnapshotV1.cs` and `BattleSnapshotV2.cs`.
- Export/restore, V1↔V2 conversion, candidate validation, commit, and checksum coupling: `Assets/Scripts/Core/GameSimulationSnapshot.cs`.
- Production session boundary: `IBattleSessionHost.ExportCurrentSessionSnapshotV2` / `RestoreCurrentSessionSnapshotV2`; implementations in `FruitDefenseGame` and `Development/GmStress/GmStressBattlePresenter`.
- Other production/editor callers: `ProjectSetup`, `BattlePresentationBoundarySmoke`, `ComposableBattleAbilitiesSmoke`, `BattleSnapshotV1Smoke`, `BattleSnapshotV2Smoke`, and GM session smoke.
- Old result taxonomy is owned by `BattleSnapshotV1.cs`; the additive foundation must retain its existing members until the atomic cutover while adding current-only failure members.
- Stable `ResolveMapIdentity` is general map behavior and is not a legacy snapshot mapping target.

### Construction classification

- Catalog-resolved Standard: no existing constructor retains the originating `CompiledLevelCatalog`; the additive foundation introduces the only snapshot-capable path.
- Resolved-only Standard: `GameSimulation(ResolvedLevelDefinition, seed)` appears in multi-level and V2 snapshot coverage and remains snapshot-unsupported because it cannot prove catalog source.
- Bundled-default/content/map-direct Standard: seed-only, `CompiledBattleContentCatalog`, and explicit-map constructors are widely used by project/editor smokes and remain simulation/checksum capable but snapshot-unsupported.
- GM stress: `GameSimulation(content, seed, map, BattleSimulationMode.GmStress)` is used by the GM runtime and multi-route determinism coverage; snapshot remains unsupported and no-argument checksum remains required.

### Aggregate registration ownership

- `P0ValidationSuite.Run` directly calls `BattleSnapshotV1Smoke.Run` and then `ProjectSetup.SmokeValidate`.
- `ProjectSetup.ValidateP1LevelCatalogPath` directly calls `BattleSnapshotV2Smoke.Run`.
- The atomic cutover will make `ProjectSetup.SmokeValidate` the sole `BattleSnapshotSmoke` aggregate owner; no registration changes are made in the additive foundation.

## Pre-cutover Baseline

- Command: Unity batchmode `FruitDefense.Editor.P0ValidationSuite.Run`.
  - Exit code: `0`.
  - Evidence markers: `FRUIT_DEFENSE_SMOKE_OK`, `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
  - Covers script compilation, deterministic smoke, V1/V2 snapshot smokes, and aggregate ProjectSetup validation on the shared working tree.
- Command: Unity batchmode `FruitDefense.Editor.GmMultiLaneStressBattleSmoke.Run`.
  - Exit code: `0`.
  - Evidence markers: `FRUIT_DEFENSE_GM_MULTI_ROUTE_DETERMINISM_OK`, `FRUIT_DEFENSE_GM_MULTI_LANE_STRESS_BATTLE_OK`.
- Raw transient logs were written under ignored `Temp/` and are not delivery artifacts.

## Authorized Foundation Boundary

Tasks 1.1–2.9 may add current v4 DTO/serialization/source/projection/candidate files and minimal Core integration. Session contracts, production presenters, versioned smokes, aggregate registration, current specs, UI, and runners remain untouched until the 3.x atomic cutover.

## Additive Foundation Result

Completed tasks 2.1–2.9 without performing the public API/caller cutover.

- `BattleSnapshot.cs` (166 lines): current v4 schema and versionless envelope/DTOs with no valid-looking header, identity, or collection defaults.
- `BattleSnapshotResults.cs` (98 lines): versionless export/restore results and the temporary superset taxonomy required to keep old callers compiling until task 3.1 deletes obsolete members.
- `ResolvedBattleSourceIdentity.cs` (268 lines): immutable source identity plus invariant, ordinal SHA-256 definition fingerprint over compiled content, resolved waves, rules/milestones, theme, and gameplay-map identity.
- `BattleSnapshotJson.cs` (478 lines): one structural tokenizer/presence gate followed by `JsonUtility` value deserialization; no legacy reader or second value serializer.
- `GameSimulationCurrentSnapshotSource.cs` (111 lines): pre-candidate three-way comparison of serialized, supplied-catalog-resolved, and target source identities/fingerprints.
- `GameSimulationCurrentSnapshotExport.cs` (189 lines): explicit v4 export projection with escaped count, entity-runtime-only statuses, and no route or compatibility sentinel.
- `GameSimulationCurrentSnapshotRestore.cs` (381 lines): isolated deterministic candidate construction, Standard primary-route derivation, projectile/reference validation, and one success commit.
- `GameSimulationCurrentSnapshotRuntime.cs` (151 lines): exact Ability/status sidecar coverage and next sequence validation on the isolated candidate.
- `GameSimulationDeterministicProjection.cs` (288 lines): private no-argument outcome projection. It preserves the established Standard checksum bytes while conditionally including escaped count and GM route identity, and never calls a public snapshot API.
- `Fixtures/BattleSnapshot/`: 13 raw JSON rejection fixtures plus paired Unity `.meta` files. The set covers former versions 1–3, both header members, each required root collection/object, and representative nested scalar/collection omissions; it is outside `Resources`.

The first checksum projection draft changed the layered-map migration fixture hash. No fixture was edited. The projection was corrected to retain the established canonical bytes, then the historical fixture and GM route-sensitive tests were rerun successfully.

### Foundation verification

- Legacy V1 smoke after schema isolation: exit `0`, marker `FRUIT_DEFENSE_BATTLE_SNAPSHOT_CURRENT_OK`.
- Legacy V2 smoke after schema isolation and again after the final split: exit `0`, marker `FRUIT_DEFENSE_BATTLE_SNAPSHOT_V2_OK`.
- `BattlefieldLayeredMapSmoke.ValidateMigrationFixtures`: exit `0`; all three pre-migration checksum fixtures remain unchanged.
- `GmMultiLaneStressBattleSmoke.Run`: exit `0`; markers include `FRUIT_DEFENSE_GM_MULTI_ROUTE_DETERMINISM_OK` and `FRUIT_DEFENSE_GM_MULTI_LANE_STRESS_BATTLE_OK`.
- `P0ValidationSuite.Run`: completed with `FRUIT_DEFENSE_SMOKE_OK` and `FRUIT_DEFENSE_P0_RELEASE_GATE_OK` after the foundation changes.

The current v4 schema is isolated from the legacy writer: `BattleSnapshotSchema` exists only in `BattleSnapshot.cs`, while the temporary V1 writer truthfully emits `BattleSnapshotV1Schema.Version = 3`. Versionless restore results no longer live in the V1 source file, so the atomic deletion in task 3.1 will not remove current result ownership.

## Atomic Cutover and Post-review Result

Tasks 3.1–5.4 replaced the temporary dual-family state with one current contract.

- Production and test callers now use only `GameSimulation.ExportSnapshot` / `RestoreSnapshot` and `IBattleSessionHost.ExportCurrentSessionSnapshot` / `RestoreCurrentSessionSnapshot`.
- `ProjectSetup.SmokeValidate` is the sole aggregate registration for `BattleSnapshotSmoke`; `P0ValidationSuite` reaches it only through `ProjectSetup`.
- V1/V2 DTO, schema, smoke, conversion, suffixed API, typed fixture, mapping, obsolete result branch, and matching `.meta` ownership were deleted. No forwarding alias, adapter, compatibility reader, or runtime source-reading deletion smoke was added.
- Catalog-resolved Standard snapshots validate the serialized source, the supplied catalog resolution, and the immutable target source before candidate construction. The definition fingerprint covers compiled content, selected ordered-wave payload, rules/milestones, theme, and gameplay map, so equal IDs/version cannot conceal changed definitions.
- Restore additionally validates phase/wave state against the exact resolved ordered wave, treats Ability pending source/target as either zero or a live combat entity, and keeps all failures mutation-free, including the complete presentation-event queue and sequence/drop accounting.
- Export/checksum canonicalize disappeared Ability entity references to zero. Standard zombie route remains derived from the resolved primary route rather than serialized, while the private checksum projection remains route-sensitive for GM.

### Private outcome projection v4

Post-review removed the last duplicated enemy-status projection and the compatibility `present` sentinel from the private checksum shape. `GameSimulationDeterministicProjection` now writes an explicit private `projectionVersion = 4`; entity runtime is the sole status owner. This is an intentional canonical checksum version change, not a gameplay-map migration and not a compatibility-preserving rewrite.

Before changing the historical fixture, `BattlefieldLayeredMapSmoke.ValidateMigrationFixtures` was run once against the old expected hashes. Its expected/actual JSON showed the independent map identity and deterministic counters unchanged and only `outcomeStateChecksum` different. The captured transitions were:

| Level | Previous checksum | Projection v4 checksum |
|---|---|---|
| `orchard-01` | `a292c7b611b4ee74b1e47d13a0e9e253e1d79db4cf27ab58355d4c99a9df7734` | `e08028b775b29422d968e73d5762f555d7b38e8d2a7f4ec83cb675b3eadbb9c4` |
| `orchard-02` | `c8d502226355270d98cefd4ca56591cae55ad4aae5f1f71813dea963f44719d2` | `00991d228f54e6911503393c123976e255dd4daf37c26283a2adc93cd303157f` |
| `orchard-03` | `7a77fc1d3443790fe568d6be2f65e802d1ab340dc61977f3887dd78541002be1` | `627fab9a2565efc2595f0cc4ae59b673c4dd66f86b96a5119056f208684dc431` |

The fixture was then minimally advanced from schema 1 to schema 2 by adding `outcomeProjectionVersion: 4` and replacing only those three hashes. The nearby smoke constants/comments make this sole-owner projection transition explicit; no fallback accepts the previous checksum representation.

### Focused verification after review fixes

- Current snapshot compile and `BattleSnapshotSmoke.Run`: exit `0`, marker `FRUIT_DEFENSE_BATTLE_SNAPSHOT_OK`.
- `DeterministicSimulationSmoke.Run`: exit `0`.
- `BattlefieldLayeredMapSmoke.Validate`: exit `0`, marker `FRUIT_DEFENSE_LAYERED_MAP_OK fixtures=3`.
- `GmMultiLaneStressBattleSmoke.Run`: exit `0`, including `FRUIT_DEFENSE_GM_MULTI_ROUTE_DETERMINISM_OK` and `FRUIT_DEFENSE_GM_MULTI_LANE_STRESS_BATTLE_OK`.
- The focused snapshot coverage includes exact resolved-wave validation, same-ID/version mutated rule/wave/theme/content rejection, gameplay-cell fingerprint rejection, Standard and GM escape counts, current-only raw JSON presence rejection, live pending-reference validation/canonicalization, atomic failure event preservation, and success event-stream reset.

### Removal audit scope

The post-cutover audit is limited to git-tracked production/test source and resources. It reports zero former V1/V2 DTO/schema/API/conversion/mapping symbols outside the deliberately malformed raw JSON fixture bodies and historical OpenSpec evidence. Ignored Unity `Library`, `Logs`, and transient `Temp` response/build caches are not repository evidence. Current-spec synchronization and the final repeated audit remain tasks 6.5 and 6.6.

### Snapshot-stage aggregate verification

- `openspec validate unify-battle-snapshot-contract --strict`: exit `0`, change valid.
- `FruitDefense.Editor.ProjectSetup.SmokeValidate`: exit `0`; markers include `FRUIT_DEFENSE_LAYERED_MAP_OK fixtures=3`, `FRUIT_DEFENSE_BATTLE_SNAPSHOT_OK`, and `FRUIT_DEFENSE_SMOKE_OK`. Transient log: `Temp/unify-battle-snapshot-project-aggregate-retry-3.log`.
- `FruitDefense.Editor.P0ValidationSuite.Run`: clean retry exit `0`; markers include `FRUIT_DEFENSE_LAYERED_MAP_OK fixtures=3`, `FRUIT_DEFENSE_BATTLE_SNAPSHOT_OK`, `FRUIT_DEFENSE_SMOKE_OK`, and `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`. Transient log: `Temp/unify-battle-snapshot-p0-aggregate-retry.log`.
- The first P0 launch immediately after the ProjectSetup Unity process encountered transient Windows mapped-file error 1224 while the terrain generator rewrote `Mask-08.png`. No source or asset was changed to hide it; Unity was allowed to exit and the exact same P0 command passed on the clean retry. Failure log: `Temp/unify-battle-snapshot-p0-aggregate.log`.
- Repository registration audit finds exactly one `BattleSnapshotSmoke.Run` call, in `ProjectSetup`; `P0ValidationSuite` reaches it through `ProjectSetup.SmokeValidate` and does not register it directly.

Tasks 6.4–6.6 remain deliberately open: all-change strict validation, accepted current-spec synchronization, and the post-synchronization final audit are outside this snapshot-stage stop point.
