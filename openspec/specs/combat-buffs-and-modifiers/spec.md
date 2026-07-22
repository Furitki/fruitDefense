# combat-buffs-and-modifiers Specification

## Purpose
Define the shared deterministic status model for combat buffs, debuffs, counters, controls, attribute modifiers, periodic effects, stacking, expiry, and removal.

## Requirements
### Requirement: General buff and debuff instances
Buffs, debuffs, counters, and control states SHALL use one source-attributed status instance model that can be owned by any supported combat entity.

#### Scenario: Positive plant buff
- **WHEN** a passive applies a positive status to its plant owner
- **THEN** the plant owns the instance with its definition ID, source ID, remaining ticks, stack count, magnitude, sequence, and tick progress

#### Scenario: Existing enemy debuffs
- **WHEN** ice slow, freeze, hit stun, or chili burn is applied to an enemy
- **THEN** the shared status model reproduces the existing duration and combat outcome

### Requirement: Deterministic stacking and expiry
Status definitions SHALL select a validated stacking mode, maximum stack count, and duration, and the runtime SHALL replace, refresh, add, or proc instances using stable sequence rules.

#### Scenario: Burn cap
- **WHEN** more than three chili burns are applied to one enemy
- **THEN** only the three newest independent instances remain in deterministic sequence order

#### Scenario: Additive buff stacks
- **WHEN** a stackable positive buff reaches its configured maximum
- **THEN** later applications refresh or replace according to its definition without exceeding that maximum

### Requirement: Effective attribute modifiers
Status definitions SHALL support a finite set of validated flat, additive, and multiplicative combat-attribute modifiers.

#### Scenario: Ordered modifier calculation
- **WHEN** several statuses modify the same attribute
- **THEN** the resolver applies them by status sequence and authored modifier order and returns the same finite result across runs

### Requirement: Fixed-step periodic effects
Periodic statuses MUST honor their compiled tick interval and preserve tick progress across snapshots.

#### Scenario: Chili burn tick
- **WHEN** chili burn remains active for its configured duration
- **THEN** damage is applied only on configured fixed-step intervals and totals the current P0 burn result

#### Scenario: Restore between ticks
- **WHEN** a status snapshot is restored with partial tick progress
- **THEN** its next periodic effect occurs after the same remaining number of fixed steps as the uninterrupted branch

### Requirement: Status classification and removal
Every status SHALL declare Buff, Debuff, or Neutral polarity and stable tags, and the runtime SHALL expose deterministic removal by definition, polarity, or tag for future finite effects.

#### Scenario: Remove debuffs only
- **WHEN** the runtime removes statuses using the Debuff polarity filter
- **THEN** matching instances are removed in sequence order while Buff and Neutral instances remain
