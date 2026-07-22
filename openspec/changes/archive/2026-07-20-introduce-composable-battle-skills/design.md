## Context

The compiled catalog and fixed-step simulation provide stable content IDs and deterministic time/randomness. Current combat still selects behavior through plant, equipment, projectile, and wave-specific branches. The migration must preserve every current mechanic while making combinations reusable and bounded.

## Goals / Non-Goals

**Goals:**

- Execute current attacks, projectiles, statuses, equipment, and rewards through compiled definitions.
- Make combinations of existing mechanisms data-only and deterministic.
- Keep definitions independent from Unity presentation assets and localized copy.

**Non-Goals:**

- Arbitrary scripting, reflection dispatch, nested effect graphs, behavior trees, or ECS.
- New balance, mechanics, plants, equipment, rewards, or UI.
- Network or Addressables loading inside the simulation.

## Decisions

1. Supported triggers are `CooldownReady`, `Periodic`, `AfterDamageDealt`, and `WaveFirstSpawned`.
2. Supported targets are `Self`, `EventTarget`, `FrontmostEnemyInRange`, `AllEnemiesInRadius`, `AllEnemies`, and `LineFromCaster`.
3. Supported effects are `Damage`, `LaunchProjectile`, `GrantResource`, `ApplyStatus`, and `EmitCue`. Each has an explicit executor; unknown values fail catalog validation.
4. Projectile modes are limited to `Tracking`, `TimedArc`, and `LinearReturn`, covering pea, watermelon, and banana behavior.
5. Status instances carry definition ID, source entity, remaining fixed ticks, stack count, and magnitude. Slow/stun/freeze refresh duration, ice triggers freeze on the fifth hit, and burn retains at most three independent source instances.
6. Equipment grants skills and modifies skills selected by validated tags. A modifier matching zero or ambiguously many skills is a catalog error unless its definition explicitly allows multiple matches.
7. Simulation emits `cueId` only. Presentation maps cues and `visualId` to current art and effects.

## Risks / Trade-offs

- **[Parity regression during branch removal]** -> Migrate one plant family at a time and keep old/new comparison fixtures until all parity checks pass.
- **[A generic system grows without bounds]** -> Reject unknown mechanism enums and require a reviewed executor for every new mechanism kind.
- **[Floating timers reduce replay stability]** -> Store new skill/status timing in fixed ticks and convert authored seconds during catalog compilation.
- **[Tag modifiers silently do nothing]** -> Validate target tags and match cardinality before Battle starts.

## Migration Plan

1. Add the runtime types and executor registry without changing current behavior.
2. Migrate plant base skills, then projectile modes, statuses, equipment combinations, and wave rewards.
3. Compare current acceptance fixtures and state checksums after each family.
4. Remove migrated enum/switch branches only after all parity and data-only extension tests pass.
