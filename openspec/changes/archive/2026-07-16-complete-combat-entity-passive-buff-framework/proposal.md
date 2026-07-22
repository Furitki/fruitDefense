## Why

The current deterministic battle runtime has reusable skills and enemy status instances, but ownership is split across concrete `Plant` and `Zombie` fields: only plants own skills and only enemies receive statuses. This prevents passive abilities, positive buffs, enemy abilities, summons, and future combatants from sharing one validated runtime contract without adding more type-specific branches.

## What Changes

- Introduce a finite combat-entity contract that exposes stable identity, faction, definition identity, life state, skills, attributes, and status ownership without adopting ECS or reflection dispatch.
- Introduce first-class passive definitions compiled from the versioned battle catalog and executed through a bounded trigger/event pipeline with deterministic ordering and re-entry protection.
- Generalize status instances into buffs and debuffs that can target any supported combat entity, modify named attributes, tick at configured intervals, stack deterministically, expire, and preserve source attribution.
- Migrate the existing ice slow/freeze/count, chili burn, hit stun, equipment on-hit behavior, and producer opening behavior onto the shared passive/status contracts while preserving current P0 outcomes.
- Extend battle snapshots, catalog validation, outcome checksums, and smoke fixtures so pending passives, modifiers, and entity-owned statuses continue deterministically after restore.
- Keep presentation one-way: simulation emits cue/visual IDs, while combat rules remain independent from Unity objects and localized copy.

## Capabilities

### New Capabilities

- `unified-combat-entities`: Defines the common identity, faction, attribute, skill, and status ownership contract for battle participants.
- `deterministic-combat-passives`: Defines authored passive bindings, supported trigger events, deterministic dispatch, filtering, and loop protection.
- `combat-buffs-and-modifiers`: Defines general buffs/debuffs, attribute modifiers, periodic effects, stacking, expiry, dispel classification, and snapshot continuation.

### Modified Capabilities

- None. The current repo-level specs do not yet own the composable-skill requirements; this change adds bounded extension contracts while retaining their established behavior.

## Impact

- Affects battle content DTOs/compiler/validator, core entity and simulation state, equipment skill resolution, snapshot DTOs/restoration/checksums, and editor validation suites.
- Existing content IDs and P0 player-visible behavior remain compatible; no arbitrary scripts, reflection, ECS package, network dependency, or presentation-owned combat rule is introduced.
- The stable game-design overview's combat-composition and content-production sections are affected conceptually, but synchronization is held until the user explicitly confirms it.
