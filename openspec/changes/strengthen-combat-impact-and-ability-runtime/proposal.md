## Why

FruitDefense already separates deterministic battle rules from transient presentation, but combat impact is weakened by 20 Hz render stepping, hard-coded cue mappings, visual timing that can trail authoritative damage, and automatic hit-stun that mixes feedback with gameplay. The current skill and passive definitions also duplicate activation/effect concepts, retain unused mechanism paths, and resolve content repeatedly in fixed-step hot loops.

## What Changes

- Add semantic combat presentation events for ability phases, resolved impacts, status changes, and defeats, with presentation-owned feedback profiles, budgets, interpolation, and battle-surface motion.
- Move cue-to-visual kind, duration, motion, and audio policy out of `GameSimulation`; the battle view resolves stable gameplay identities through a validated presentation catalog.
- Add render interpolation and event-driven attacker/target reactions without changing authoritative entity positions.
- **BREAKING** Replace separate skill and passive execution definitions with one finite, validated ability model covering cooldown, periodic, and combat-event activation, typed target selection, fixed-tick release timing, delivery, and payload effects.
- **BREAKING** Remove unused skill triggers, duplicate top-level effect fields, legacy presentation mirrors, and the old Skill/Passive execution paths after bundled content and tests migrate.
- **BREAKING** Stop applying the automatic movement-blocking `status.combat.hit-stun` on every direct hit; ordinary hit reaction becomes presentation-only. Explicit authored control statuses remain deterministic gameplay.
- Align authoritative effect timing with the visible release/impact point and make authored area radii govern area selection, including the durian attack.
- Cache compiled plant/equipment ability loadouts and remove recurring clone/sort/LINQ allocation from fixed-step execution.
- Synchronize the stable combat and content boundaries in the game-design overview and add aggregate Editor validation for the new contracts.

## Capabilities

### New Capabilities

- `combat-impact-feedback`: Semantic combat feedback, interpolation, presentation profiles, feedback budgets, and player-visible impact behavior for the five bundled plants and three equipment families.

### Modified Capabilities

- `composable-battle-skills`: Replace the separate skill execution shape with finite unified abilities, fixed-tick timelines, typed delivery/payload composition, correct area targeting, and cached loadout resolution.
- `deterministic-combat-passives`: Express deterministic combat reactions as event-activated abilities while retaining stable ordering, loop protection, cooldown continuation, and snapshot safety.
- `battle-presentation-event-boundary`: Replace simulation-owned visual mappings with semantic transient events and presentation-owned profiles while keeping one-way, bounded, snapshot-excluded delivery.
- `deterministic-battle-simulation`: Expose presentation interpolation state without increasing or mutating the 0.05-second authoritative step.
- `versioned-battle-content-catalog`: Replace the Skill/Passive catalog collections and references with one versioned Ability collection and stable `ability.*` identities.
- `unified-combat-entities`: Replace separate runtime Skill/Passive ownership with one serializable Ability runtime collection.
- `combat-buffs-and-modifiers`: Remove implicit direct-hit movement blocking from the current enemy-debuff contract while preserving explicitly authored controls.
- `battle-snapshot-v1`: Make the current resolved-level snapshot preserve delayed and event-activated Ability continuation and remove the obsolete pre-Ability legacy migration path.
- `level-selection-flow`: Keep exact composite level identity while removing the obsolete legacy single-map snapshot compatibility reader.
- `battlefield-map-layout`: Preserve one canonical projection and interaction geometry while allowing bounded transient presentation-only battlefield offsets.

## Impact

- Affects battle DTOs, content compilation/validation, ability/status/projectile runtime state, snapshots, deterministic checksums, `GameSimulation`, presentation buffering, `FruitDefenseGame`, bundled content, Editor smoke tests, and combat sections of `docs/design/game-design-overview.md`.
- The bundled battle content schema identity changes. No compatibility adapter or dual executor is retained for the replaced Skill/Passive definitions.
- No new package is required. The first implementation uses the existing immediate-mode sprite renderer and Unity-native audio capability; platform haptics remain outside the shared WebGL baseline.
- Required validation includes OpenSpec validation, combat/determinism/snapshot/presentation Editor smoke, aggregate project smoke, WebGL build, and real portrait WebGL capture of battle feedback.
