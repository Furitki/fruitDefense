# unified-combat-entities Specification

## Purpose
Define the shared serializable combat-entity boundary and deterministic effective-attribute contract used by plants, enemies, statuses, skills, and passives.

## Requirements
### Requirement: Common combat entity ownership
Plants and enemies SHALL expose stable entity identity, definition identity, faction, life state, runtime skills, runtime passives, and statuses through one serializable combat-entity contract while retaining their concrete gameplay fields.

#### Scenario: Plant and enemy entity lookup
- **WHEN** the simulation resolves a live plant or enemy by global entity ID
- **THEN** both are returned through the same combat-entity boundary with their correct faction and definition ID

#### Scenario: Globally unique identity
- **WHEN** a battle contains pots, plants, enemies, and projectiles
- **THEN** every runtime entity ID remains globally unique and snapshot validation rejects duplicates

### Requirement: Deterministic effective attributes
The simulation SHALL resolve supported combat attributes from immutable base values plus active status modifiers in deterministic sequence order, and SHALL NOT mutate authored definitions while calculating effective values.

#### Scenario: Positive plant modifier
- **WHEN** a plant receives a timed damage buff with a multiplicative modifier
- **THEN** its effective damage changes for the buff duration and returns to the same base value after expiry

#### Scenario: Enemy movement modifier
- **WHEN** an enemy receives the existing ice slow
- **THEN** movement uses the shared move-speed attribute result and retains current P0 slow behavior

### Requirement: Bounded entity capabilities
Finite effect executors MUST check the capability of their source and receiver so unsupported combinations do not introduce concrete-type casts outside the combat runtime boundary.

#### Scenario: Status on either faction
- **WHEN** a validated effect applies a status to a plant or enemy
- **THEN** the shared status owner accepts it and attribute queries observe its modifiers

#### Scenario: Unsupported projectile receiver
- **WHEN** a projectile effect resolves a receiver that is not a supported damage target
- **THEN** the executor completes deterministically without mutating unrelated state
