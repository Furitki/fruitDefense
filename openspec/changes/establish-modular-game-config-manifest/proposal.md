## Why

The project already has versioned battle-content DTOs, but production values are still regenerated from C# factories and several gameplay, selection, upgrade, and presentation decisions remain hard-coded in consumers. This prevents designers from adding variants such as two visually identical fruits with different combat and growth parameters without editing code.

## What Changes

- Add one versioned game-content manifest that selects the authoritative battle catalog, level catalog, and battle-presentation bindings for a build.
- Replace production C# content factories as the runtime source with validated ScriptableObject authoring and deterministic bundled export/load.
- Split fruit gameplay identity from reusable presentation identity so multiple fruit definitions can share visuals while retaining independent parameters and stable IDs.
- Add authored upgrade profiles and nursery draw profiles, including maximum tier, tier modifiers, weighted entries, first-refresh guarantees, producer limits, pot chance, and movement cooldown.
- Make merge eligibility, effective upgrade values, nursery generation, refresh cost display, and visual resolution consume the resolved configuration instead of fixed IDs or numeric constants.
- Add structured cross-catalog validation and fail startup/export on missing or invalid references; no fallback content path is retained.
- **BREAKING**: remove the requirement that the production catalog contain exactly the original five fruits and four global star tiers, and remove obsolete production factory/default-config paths after the asset-backed path is verified.

## Capabilities

### New Capabilities

- `modular-game-config-manifest`: Defines the authoritative manifest, modular authoring inputs, deterministic runtime bundle, version pinning, and cross-catalog validation.
- `configurable-content-variants`: Defines independent fruit variants, shared presentation identities, upgrade profiles, nursery profiles, and configuration-driven merge/selection behavior.

### Modified Capabilities

- `versioned-battle-content-catalog`: Changes the bundled catalog from an exact fixed roster/global-star baseline to an extensible validated roster authored from assets and loaded from deterministic bundled data.
- `plant-selection-inspection`: Requires selection and detail presentation to resolve variant display, effective statistics, upgrade limits, and visuals through configuration rather than fixed plant IDs or a global four-star assumption.

## Impact

- Content DTOs, compilers, validators, deterministic identity projection, and snapshot validation.
- App bootstrap and bundled level-catalog construction.
- Fruit merge/move commands, nursery generation, stat resolution, and refresh-cost presentation.
- Battle visual lookup and validation for plants, enemies, equipment, and projectiles.
- Editor content authoring/export tooling and smoke tests.
- No new package dependency and no remote hot-code or remote gameplay-content delivery is introduced.
