## MODIFIED Requirements

### Requirement: First-class passive definitions
The versioned battle catalog SHALL express reactive combat behavior as unified Abilities with combat-event activation, supported owner-role filters, target selectors, priorities, cooldowns, deliveries, and finite payload effects. A separate Passive definition and executor SHALL NOT remain after migration.

#### Scenario: Equipment on-hit reaction
- **WHEN** an equipped plant deals damage with the existing ice or chili equipment
- **THEN** the catalog-granted event-activated ability applies the configured status to that event target without an equipment-specific simulation branch

#### Scenario: Enemy reactive ability binding
- **WHEN** validated data binds an event-activated ability to an enemy definition
- **THEN** the runtime resolves and dispatches it through the same Ability and owner contract used for plants

### Requirement: Deterministic passive dispatch
Combat-event Ability dispatch SHALL order owners by entity ID and abilities by priority then stable ID, and every root combat event SHALL have a monotonic sequence.

#### Scenario: Multiple reactions
- **WHEN** multiple eligible event-activated abilities observe one damage event
- **THEN** their deliveries execute in the documented deterministic order and repeated simulations produce the same outcome checksum

### Requirement: Passive loop protection
The combat-event Ability dispatcher MUST prevent the same ability from reactivating for the same owner and root event and MUST enforce a finite activation budget.

#### Scenario: Recursive status reaction
- **WHEN** ability payloads would otherwise create a cycle of status-applied events
- **THEN** each activation key executes at most once and the simulation completes or fails fast at the fixed budget instead of hanging

### Requirement: Passive runtime continuation
Event-activated Ability cooldown and root-event sequence state that can affect future outcomes SHALL be included in the current resolved-level snapshot and deterministic checksum.

#### Scenario: Restore pending reactive cooldown
- **WHEN** a snapshot is exported while an event-activated ability cooldown is active and then restored
- **THEN** the restored battle fires the ability on the same future logic tick as an uninterrupted battle
