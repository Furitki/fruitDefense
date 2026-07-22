## 1. Catalog Contract

- [x] 1.1 Add JsonUtility-compatible catalog header and Plant/Enemy/Equipment/Skill/Projectile/Status/Wave/StarTier/BattleRules DTOs with stable-ID constants.
- [x] 1.2 Add the ScriptableObject aggregate authoring asset and explicit one-way legacy enum-to-ID compatibility mapping.

## 2. Validation and Runtime Compilation

- [x] 2.1 Implement structured, exhaustive schema, ID, numeric, ordering, required-content, and cross-reference validation.
- [x] 2.2 Implement deep-copy compilation into ordinal, read-only typed indexes with invalid-catalog rejection.

## 3. Bundled Content and Export

- [x] 3.1 Materialize the current five plants, four enemies, three equipment items, skill/projectile/status definitions, fifteen waves, four star tiers, battle rules, and milestone rewards in the bundled authoring catalog.
- [x] 3.2 Implement deterministic Editor export to normalized UTF-8 JSON and commit the generated ScriptableObject and bundled JSON.
- [x] 3.3 Add Editor menu and batch-mode validation/export entries that do not load gameplay scenes.

## 4. Verification

- [x] 4.1 Add smoke coverage for JSON round-trip, deterministic bytes, structured invalid diagnostics, compiled lookups, bundled parity, and exhaustive legacy mappings.
- [x] 4.2 Run OpenSpec validation, Unity compilation, independent content validation, and the existing project smoke validation; record successful outputs.

## Validation Evidence

- OpenSpec strict validation: `Change 'establish-versioned-battle-content-catalog' is valid`.
- Unity content compile/export/validation: exit code `0`, `Battle content validation passed: catalog.bundled.orchard@1.0.0`.
- Deterministic export: repeated SHA-256 `1878BD12A7C109BCBCE440847C24BA836F8D61C2BD8C7FC265510829554C781F`.
- Existing project smoke: exit code `0`, `FRUIT_DEFENSE_SMOKE_OK`.
