## MODIFIED Requirements

### Requirement: Common combat entity ownership
Plants and enemies SHALL expose stable entity identity, definition identity, faction, life state, unified runtime abilities, and statuses through one serializable combat-entity contract while retaining their concrete gameplay fields. Separate runtime Skill and Passive collections SHALL NOT remain.

#### Scenario: Plant and enemy entity lookup
- **WHEN** the simulation resolves a live plant or enemy by global entity ID
- **THEN** both are returned through the same combat-entity boundary with their correct faction, definition ID, and ordered Ability runtimes

#### Scenario: Globally unique identity
- **WHEN** a battle contains pots, plants, enemies, and projectiles
- **THEN** every runtime entity ID remains globally unique and snapshot validation rejects duplicates
