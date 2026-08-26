## ADDED Requirements

### Requirement: Deterministic route-bound enemy execution
Enemy route identity SHALL be assigned by deterministic commands or wave spawning, SHALL remain stable for the enemy lifetime, and SHALL participate in deterministic movement, position-dependent combat, presentation event anchors, and gameplay-state checksums.

#### Scenario: Equivalent multi-route command streams
- **WHEN** two GM simulations with the same seed, fixed-step inputs, lane commands, enemy selections, plant placements, and batch sizes are advanced through equivalent render-frame partitions
- **THEN** they produce the same per-route enemy states, queue states, escaped count, presentation events, and deterministic gameplay-state checksum

#### Scenario: Lane assignment differs
- **WHEN** otherwise identical simulations spawn an enemy on different route IDs
- **THEN** their canonical positions and deterministic gameplay-state checksums differ

#### Scenario: Standard wave enemy is spawned
- **WHEN** a standard single-route wave creates an enemy
- **THEN** the enemy receives the map's validated primary route ID explicitly and preserves existing deterministic traversal and combat outcomes

### Requirement: Deterministic bounded GM queues
The GM simulation SHALL process manual per-lane FIFO spawn queues only on fixed logical steps, SHALL use a stable lane iteration order for all-lanes commands, and SHALL include pending queue contents, active count, and escaped count in its deterministic state checksum.

#### Scenario: Frame partitions differ
- **WHEN** equivalent GM command sequences are advanced using different render-frame partitions that consume the same number of logical steps
- **THEN** the same enemy types leave each lane queue on the same logical steps and the simulations produce the same checksum

#### Scenario: Capacity is partially available
- **WHEN** a deterministic command requests more enemies than the remaining 500-unit active-plus-pending capacity
- **THEN** the same stable prefix of requested lane queues is accepted on every equivalent simulation and the remainder is rejected
