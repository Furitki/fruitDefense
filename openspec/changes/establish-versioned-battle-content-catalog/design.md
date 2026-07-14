## Context

`GameConfig` currently owns plant/enemy stats, star multipliers, wave generation, and milestone rewards through enums, switches, formulas, and a wave-count matrix. `GameSimulation` consumes those methods directly. Future skill composition, deterministic snapshots, remote content delivery, and version-pinned saves need a stable content contract before the simulation can be migrated safely.

Unity `JsonUtility` is the required portable serializer, so dictionaries, interfaces, polymorphic payloads, and top-level arrays cannot be the serialized contract. The project has no test-framework package; validation must therefore be callable from both an Editor menu and a batch-mode static method.

## Goals / Non-Goals

**Goals:**

- Make all current battle-content categories authorable through a ScriptableObject and exportable as deterministic versioned JSON.
- Provide stable string IDs, explicit cross-references, structured validation results, and immutable runtime indexes.
- Include a bundled catalog equivalent to the current five plants, four enemies, three equipment items, fifteen waves, four star tiers, and reward rules.
- Preserve a narrow one-way bridge from current enums to IDs so later migration can be incremental.
- Give CI and developers independent export and validation entry points without loading a gameplay scene.

**Non-Goals:**

- Replacing `GameConfig` or changing `GameSimulation`/`FruitDefenseGame` behavior.
- Implementing effect execution, remote download, Addressables, save restoration, platform SDK integration, or runtime mod loading.
- Allowing arbitrary code, expressions, reflection, or polymorphic JSON in content.

## Decisions

### Use one JsonUtility-compatible aggregate DTO

`BattleContentCatalogDto` contains a serializable header and arrays for every content category. Definition DTOs contain fields and string references only; no dictionaries or Unity object references cross the JSON boundary. Arrays are sorted by stable ID during canonical export, while wave order and each wave spawn sequence remain ordered gameplay data.

Alternative considered: one file per definition. It would improve diff locality but introduces partial-version and atomicity problems before a release manifest exists. A single P0 catalog gives one validation and compatibility unit.

### Keep authoring and transport shapes deliberately close

`BattleContentCatalogAsset` is the ScriptableObject editing surface and owns a catalog DTO. The Editor exporter deep-copies via JSON, canonicalizes the copy, validates it, and writes only when valid. This prevents export sorting from mutating the authoring asset and avoids a second mapping layer before the content schema stabilizes.

Alternative considered: one ScriptableObject asset per item. That is more ergonomic at scale but creates asset-reference and GUID complexity that is unnecessary for the current small catalog. The aggregate can later be split without changing the exported DTO.

### Compile only after validation

`BattleContentCompiler.TryCompile` returns either a `CompiledBattleContentCatalog` or structured `ContentValidationIssue` records. The compiled object owns private dictionaries built with ordinal string comparison and exposes read-only dictionary interfaces plus typed `TryGet` methods. It never exposes mutable DTO arrays as its primary lookup API.

All definitions are deep-copied before indexing so post-compile edits to authoring/transport objects cannot mutate the compiled catalog. The future simulation dependency will accept only this compiled type.

### Stable IDs are semantic and enum-independent

IDs use lowercase dotted namespaces such as `plant.pea`, `enemy.normal`, and `equipment.gatling`. Validation enforces a conservative ASCII pattern and uniqueness within each category. References are string IDs. `LegacyBattleContentIds` maps each existing enum value explicitly to an ID; catalog loading never casts enum ordinals or derives IDs from enum names.

### Encode current formulas as explicit exported values

The bundled factory materializes all fifteen wave definitions with explicit spawn sequences, multipliers, intervals, and rewards. It also materializes star tiers and milestone rewards. This makes the JSON self-contained and ensures a remote catalog does not depend on the old formula implementation.

Distances remain legacy design-space values in this P0 schema and are named accordingly. The later simulation migration will convert them through the battlefield map exactly once, preserving current behavior.

### Deterministic export uses UTF-8 and normalized JSON

Canonical export sorts unordered definition arrays by ID, preserves meaningful ordered arrays, serializes with `JsonUtility.ToJson(..., true)`, normalizes line endings to LF, and writes UTF-8 without BOM. A second export of unchanged content MUST be byte-identical.

## Risks / Trade-offs

- [Aggregate ScriptableObject becomes unwieldy as content grows] → Keep JSON DTO stable and split only the authoring layer in a later change.
- [JsonUtility silently ignores unsupported shapes] → Restrict DTOs to fields, arrays, primitives, and serializable structs; round-trip the exported JSON in validation smoke tests.
- [Bundled values drift from legacy behavior before simulation migration] → Add parity checks for counts, stable mappings, wave sequences, star multipliers, and rewards; do not switch runtime consumers yet.
- [Read-only interfaces can still expose mutable DTO objects] → Deep-copy before compile and document definitions as immutable runtime records; callers receive no authoring asset or source arrays.
- [Another agent edits shared core enums] → Compatibility mapping is isolated to one file and uses exhaustive switches; no changes are made to enum declarations.

## Migration Plan

1. Add DTOs, validator/compiler, authoring asset type, and bundled-data factory alongside the existing rules.
2. Generate and commit the bundled ScriptableObject and canonical JSON through the Editor command.
3. Run compile, schema validation, round-trip, deterministic export, and legacy-parity smoke checks.
4. In a later OpenSpec change, inject the compiled catalog into `GameSimulation` while retaining enum-backed save compatibility.

Rollback removes the new content directory, generated catalog, and OpenSpec change; current runtime behavior is untouched.

## Open Questions

None for P0. Per-definition assets, localization keys, cryptographic manifests, and remote catalog activation remain explicit later changes.
