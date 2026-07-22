## Why

Battle content is currently embedded in enum switches and generated wave formulas, which makes balancing changes code changes and gives future remote-content releases no stable schema or compatibility boundary. Establishing one validated, versioned catalog now lets later gameplay, save, and hot-update work depend on explicit IDs and reproducible bundled data without rewriting the current simulation in this change.

## What Changes

- Add ScriptableObject authoring definitions for plants, enemies, equipment, skills, projectiles, statuses, waves, star tiers, and battle rules.
- Add JsonUtility-compatible DTOs with the required `schemaVersion`, `catalogId`, `contentVersion`, and `minCodeVersion` catalog header.
- Add deterministic Editor export of the bundled authoring catalog to versioned JSON.
- Add structured validation for duplicate or malformed IDs, invalid numbers, missing references, and incomplete required content.
- Compile validated DTOs into read-only, ID-indexed runtime lookups.
- Ship a bundled catalog containing the current five plants, four enemies, three equipment items, fifteen waves, star-tier multipliers, and battle rewards.
- Add one-way compatibility mappings from current gameplay enums to stable content IDs; the catalog does not depend on enum ordinal values.
- Add an independent Editor validation/export entry and command-line smoke entry.
- Do not replace `GameSimulation`, `FruitDefenseGame`, current enum state, or current player-visible battle behavior in this change.

## Capabilities

### New Capabilities

- `versioned-battle-content-catalog`: Authoring, deterministic export, validation, bundled loading, compiled read-only lookup, and legacy enum-to-ID compatibility for battle content.

### Modified Capabilities

None.

## Impact

- Adds a new content boundary under `Assets/Scripts/Content`, Editor export and validation tooling, bundled catalog assets/data, and focused Editor tests.
- Creates runtime DTO and compiled-catalog APIs intended for a later `GameSimulation` constructor dependency; the current simulation remains unchanged.
- Adds no external Unity package, backend dependency, ProjectSettings change, build-scene change, or presentation change.
