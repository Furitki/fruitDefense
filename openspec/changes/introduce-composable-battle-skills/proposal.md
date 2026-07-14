## Why

Plant attacks, projectiles, status effects, equipment combinations, and wave rewards are currently interleaved through enums and type-specific branches in `GameSimulation`. A finite data-driven skill runtime is needed so existing mechanics can be recombined for rapid content iteration without introducing an unbounded scripting system.

## What Changes

- Introduce explicit Trigger, Target, Effect, projectile-mode, status, and skill-runtime types backed by stable content IDs.
- Migrate the five current plants, three equipment types, projectile behaviors, status effects, and wave rewards to compiled catalog definitions.
- Express equipment as granted skills and tag-targeted modifiers instead of plant-specific installation branches.
- Keep new combinations data-only when they use existing mechanisms; require code only for a genuinely new Effect or projectile mode.
- Preserve current battle balance, drag/move/merge behavior, visual cues, and deterministic fixed-step behavior.
- Remove migrated plant/equipment/wave special-case branches after parity tests pass.

## Capabilities

### New Capabilities

- `composable-battle-skills`: Defines the supported skill composition model and parity requirements for current battle mechanics.

### Modified Capabilities

None.

## Impact

- Refactors core simulation attack, projectile, status, equipment, and reward execution.
- Consumes the compiled battle content catalog and deterministic simulation contracts.
- Keeps UI compatibility through stable IDs, `visualId`, and `cueId`; no battle UI redesign is included.
