## ADDED Requirements

### Requirement: Normalized validated player progression
The current player profile SHALL persist non-negative item balances, unique owned outgame growth-equipment ranks, legal growth-equipment slot assignments, cultivation node ranks, and unique activity receipts using stable outgame-content identities.

#### Scenario: Persisted progression is loaded
- **WHEN** the profile contains only known identities, legal ranks and slots, satisfied prerequisites, and non-negative quantities
- **THEN** validation succeeds and the hub receives an immutable view projection of that revision

#### Scenario: Duplicate or invalid progression is loaded
- **WHEN** the profile contains a duplicate item, negative quantity, unknown growth-equipment, illegal slot, out-of-range rank, or invalid cultivation prerequisite
- **THEN** validation returns a structured error and no partial progression state becomes interactive

### Requirement: Equipment commands are transactional
Outgame growth-equipment equip and upgrade commands SHALL validate ownership, slot compatibility, next-rank existence, and complete costs before committing one atomic profile revision.

#### Scenario: Owned equipment is equipped
- **WHEN** the player equips an owned definition into a compatible slot
- **THEN** the new loadout replaces that slot assignment once and all other inventory and progression data remain unchanged

#### Scenario: Equipment is upgraded
- **WHEN** the next rank exists and all required item quantities are available
- **THEN** all costs are debited and the equipment rank increments exactly once in the same committed revision

#### Scenario: Equipment cost is insufficient
- **WHEN** any required quantity is missing
- **THEN** no item is debited, no rank changes, and Growth shows the exact insufficient requirement

### Requirement: Cultivation commands enforce prerequisites and costs
Cultivation upgrades SHALL validate the node's prerequisite ranks, next-rank existence, and complete item costs before committing one atomic profile revision.

#### Scenario: Cultivation prerequisite is met
- **WHEN** the required nodes and costs are satisfied
- **THEN** the requested node increments exactly one rank and its costs are debited in one revision

#### Scenario: Cultivation node is locked
- **WHEN** a prerequisite rank is absent
- **THEN** the node remains unchanged and Growth exposes the prerequisite as a non-interactive locked reason

### Requirement: Growth actions expose complete finite states
Equipment slots, equipment ranks, and cultivation nodes SHALL render owned, selected, equipped, upgradeable, insufficient, locked, maximum, loading, success, and error states where applicable without relying only on color.

#### Scenario: Maximum rank is displayed
- **WHEN** equipment or a cultivation node has no configured next rank
- **THEN** its action area shows a completed maximum state and cannot submit an upgrade command

### Requirement: Presenters cannot mutate authoritative progression
Hub presenters SHALL receive read-only projections and SHALL invoke explicit progression commands; they MUST NOT edit the player profile, item balances, ranks, receipts, or loadout collections directly.

#### Scenario: Save completes successfully
- **WHEN** a progression command persists a new revision
- **THEN** the progression service publishes that committed projection once and all visible balances and growth views refresh from it
