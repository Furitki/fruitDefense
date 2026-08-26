## REMOVED Requirements

### Requirement: Versioned snapshot envelope
**Reason**: The version-labelled V1 contract is replaced by the single current v4 `battle-snapshot` capability for catalog-resolved Standard sessions; former V1/V2 DTO families both claiming version 3 are not independent supported contracts.

**Migration**: Supported session callers MUST use `BattleSnapshot`, `ExportCurrentSessionSnapshot`/`RestoreCurrentSessionSnapshot`, and the sole `GameSimulation.ExportSnapshot`/`RestoreSnapshot` APIs. No compatibility reader or adapter is provided, and V1, V2, and V3 payloads are rejected.

### Requirement: Exact content pinning
**Reason**: Content pinning is replaced by stricter three-way immutable source comparison among snapshot, supplied catalog resolution, and existing target, including a canonical definition fingerprint that does not trust reused IDs/version strings.

**Migration**: Callers MUST construct a snapshot-capable Standard target from `CompiledLevelCatalog` and supply a catalog whose IDs, versions, compiled content, ordered waves, rules, theme, and gameplay map match the captured definition fingerprint. No default-map, cross-catalog, content-direct, or same-ID-changed-definition fallback remains.

### Requirement: Atomic validated restore
**Reason**: Atomic restore is replaced by current source validation, complete JSON presence/value validation, isolated candidate construction, one commit, and exact presentation-event behavior.

**Migration**: Tests and callers MUST use the current result taxonomy and target. No V1 overload or translation layer remains, and any former payload is rejected before target mutation.

### Requirement: Deterministic round-trip continuation
**Reason**: Deterministic continuation moves to the single current Standard-session envelope, while GM/direct checksum behavior is separated into a catalog-independent projection rather than legacy snapshot export.

**Migration**: Supported persistence tests MUST round-trip `BattleSnapshot`; former JSON shapes are raw rejection fixtures only and are never upgraded or deserialized into legacy DTOs.

### Requirement: Layer-aware gameplay map identity
**Reason**: Gameplay-map identity moves into immutable resolved source matching, and the legacy bundled-map compatibility scenario conflicts with current-only restore.

**Migration**: Current snapshots MUST match the target and supplied catalog gameplay-map fingerprint. `ResolveMapIdentity` remains as stable map functionality, but no legacy snapshot/default-map mapping or reader remains.
