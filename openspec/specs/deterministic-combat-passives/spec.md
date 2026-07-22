# deterministic-combat-passives Specification

## Purpose
Define first-class catalog-authored combat passives with deterministic event dispatch, bounded recursion, and snapshot-safe runtime continuation.

## Requirements
### Requirement: First-class passive definitions
The versioned battle catalog SHALL define passives separately from skills using supported event triggers, owner-role filters, target selectors, priorities, cooldowns, and finite effects.

#### Scenario: Equipment on-hit passive
- **WHEN** an equipped plant deals damage with the existing ice or chili equipment
- **THEN** the catalog-granted passive applies the configured status to that event target without an equipment-specific simulation branch

#### Scenario: Enemy passive binding
- **WHEN** validated data binds a passive to an enemy definition
- **THEN** the runtime resolves and dispatches that passive through the same owner contract used for plants

### Requirement: Deterministic passive dispatch
Passive dispatch SHALL order owners by entity ID and passives by priority then stable ID, and every root combat event SHALL have a monotonic sequence.

#### Scenario: Multiple reactions
- **WHEN** multiple eligible passives observe one damage event
- **THEN** their effects execute in the documented deterministic order and repeated simulations produce the same outcome checksum

### Requirement: Passive loop protection
The dispatcher MUST prevent the same passive from reactivating for the same owner and root event and MUST enforce a finite activation budget.

#### Scenario: Recursive status reaction
- **WHEN** passive effects would otherwise create a cycle of status-applied events
- **THEN** each activation key executes at most once and the simulation completes or fails fast at the fixed budget instead of hanging

### Requirement: Passive runtime continuation
Passive cooldown and root-event sequence state that can affect future outcomes SHALL be included in the resolved-level snapshot and deterministic checksum.

#### Scenario: Restore pending passive cooldown
- **WHEN** a snapshot is exported while a passive cooldown is active and then restored
- **THEN** the restored battle fires the passive on the same future logic tick as an uninterrupted battle
