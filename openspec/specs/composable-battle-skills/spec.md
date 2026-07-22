# composable-battle-skills Specification

## Purpose
TBD - created by archiving change introduce-composable-battle-skills. Update Purpose after archive.
## Requirements
### Requirement: Finite skill composition
The simulation SHALL execute skills as validated combinations of the supported Trigger, Target, and Effect kinds and SHALL reject unknown mechanism kinds before Battle starts.

#### Scenario: Existing mechanism combination
- **WHEN** a test plant combines an existing trigger, target, projectile, and status definition
- **THEN** it runs without adding or changing simulation executor code

#### Scenario: Unknown effect
- **WHEN** a catalog contains an Effect kind with no registered executor
- **THEN** catalog compilation fails with a structured validation error

### Requirement: Current projectile behaviors
The skill runtime MUST reproduce Tracking, TimedArc, and LinearReturn projectile behavior using fixed-step timing.

#### Scenario: Tracking pea
- **WHEN** a pea projectile loses its target before impact
- **THEN** it deterministically retargets according to the current frontmost-target rules

#### Scenario: Timed watermelon arc
- **WHEN** a watermelon launches
- **THEN** it resolves its stored impact point after the current 0.4-second arc and applies the current area damage

#### Scenario: Banana return
- **WHEN** a banana completes its outward and return paths
- **THEN** each eligible target is hit at most once per path

### Requirement: General status instances
Slow, stun, freeze, ice-count, and burn behavior SHALL use general status definitions and instances rather than plant- or equipment-specific state fields.

#### Scenario: Fifth ice hit
- **WHEN** an enemy receives its fifth valid ice-count hit
- **THEN** the count clears and the configured freeze status is applied

#### Scenario: Burn cap
- **WHEN** more than three burn applications would remain active on one enemy
- **THEN** no more than three independent burn instances are retained according to the deterministic replacement rule

### Requirement: Equipment grants and modifiers
Equipment SHALL grant skills and apply tag-selected skill modifiers, and catalog validation MUST prevent silent zero-match or ambiguous modifier behavior.

#### Scenario: Machine-gun equipment
- **WHEN** machine-gun equipment modifies a `ranged.projectile` skill
- **THEN** the skill emits four shots separated by 0.2 seconds

#### Scenario: Producer equipment combinations
- **WHEN** ice or pepper equipment is installed on a producer-tagged plant
- **THEN** the catalog-granted producer combination reproduces the current opening slow or resource bonus without plant-ID branches

### Requirement: Current battle parity
All five plants, three equipment types, fifteen waves, milestone rewards, and existing drag/move/merge interactions MUST retain current behavior within one fixed logic step.

#### Scenario: Existing acceptance suite
- **WHEN** Unity smoke and the 13-state WebGL battle acceptance run after migration
- **THEN** all existing assertions and visual interactions pass

