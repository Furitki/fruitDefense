## ADDED Requirements

### Requirement: Independent fruit variants with shared presentation
Every plant definition SHALL have its own stable gameplay ID, base statistics, abilities, tags, upgrade-profile reference, and presentation ID, and multiple plant definitions MAY reference the same validated presentation ID.

#### Scenario: Rapid pea variant
- **WHEN** a test catalog adds `plant.pea.rapid` with different damage, ability timing, and upgrade profile while sharing the standard pea presentation ID
- **THEN** compilation succeeds without changing simulation or renderer branches and both definitions resolve their own gameplay values with the same visual archetype

### Requirement: Profile-owned upgrade tiers
Each plant SHALL reference one upgrade profile containing contiguous ordered tiers and positive finite damage, attack-speed, and range multipliers, and merge legality SHALL use the referenced profile to determine whether a next tier exists.

#### Scenario: Variant-specific maximum tier
- **WHEN** two visually identical plant definitions reference upgrade profiles with different maximum tiers
- **THEN** each plant stops merging at its own configured maximum and resolves its own tier multipliers

#### Scenario: Different variants meet
- **WHEN** equal-tier plants with different definition IDs are dragged together
- **THEN** they do not merge solely because they share a presentation ID

### Requirement: Profile-owned deterministic nursery selection
Each active rule set SHALL reference one nursery profile containing weighted plant entries, pot chance, first-refresh guarantee, and per-refresh tag cap, and nursery generation SHALL consume that profile using the battle deterministic random source.

#### Scenario: Variant excluded from one pool
- **WHEN** a valid plant variant exists in the battle catalog but has no entry in the active nursery profile
- **THEN** that variant never appears in refresh results for that rule set

#### Scenario: Fixed-seed nursery replay
- **WHEN** two battles use the same seed, content version, level, and nursery profile
- **THEN** their refresh costs, plant variants, pot results, and slot ordering are identical

### Requirement: Configured interaction tuning
The active rule set SHALL own relocation cooldown and refresh-cost parameters, and both simulation legality and player-facing cost presentation SHALL consume the same resolved values.

#### Scenario: Relocation cooldown changes
- **WHEN** a rule set configures a relocation cooldown different from the bundled baseline
- **THEN** all board-to-board and board-to-nursery moves use that value without a hard-coded two-second path

#### Scenario: Refresh cost is displayed
- **WHEN** the current refresh count changes
- **THEN** the displayed cost equals the simulation cost resolved from the active rule set

