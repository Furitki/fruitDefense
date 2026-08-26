## MODIFIED Requirements

### Requirement: General buff and debuff instances
Buffs, debuffs, counters, and explicitly authored gameplay controls SHALL use one source-attributed status instance model that can be owned by any supported combat entity. Ordinary direct-hit presentation reaction SHALL NOT create a status or block movement.

#### Scenario: Positive plant buff
- **WHEN** an event-activated Ability applies a positive status to its plant owner
- **THEN** the plant owns the instance with its definition ID, source ID, remaining ticks, stack count, magnitude, sequence, and tick progress

#### Scenario: Existing enemy debuffs
- **WHEN** ice slow, freeze, or chili burn is applied to an enemy
- **THEN** the shared status model reproduces the authored duration and combat outcome

#### Scenario: Ordinary direct hit
- **WHEN** damage resolves without an explicit gameplay-control payload
- **THEN** no hit-stun status is created and enemy movement is affected only by its existing authored statuses
