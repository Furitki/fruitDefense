## ADDED Requirements

### Requirement: Configuration-resolved plant inspection
Plant inspection SHALL resolve the selected definition's display data, effective statistics, current and maximum configured tier, upgrade profile, and presentation identity without branches for individual plant IDs.

#### Scenario: Inspect two shared-visual variants
- **WHEN** the player inspects two plant definitions that share a presentation identity but have different base values or upgrade profiles
- **THEN** both render the shared visual while each detail surface shows its own configured name, tier limit, damage, attack timing, and range

## MODIFIED Requirements

### Requirement: Drag-only plant relocation
Plant placement, movement, return-to-nursery, swapping, and merging SHALL remain available through drag-and-drop and SHALL use simulation legality checks resolved from the active rule set and the dragged plant's upgrade profile.

#### Scenario: Nursery plant is dragged to a flowerpot
- **WHEN** the player drags a nursery plant across the activation threshold and releases it over a legal empty flowerpot
- **THEN** the plant is placed on that flowerpot

#### Scenario: On-board plant is dragged to another flowerpot
- **WHEN** the player drags an on-board plant to a legal empty or compatible occupied flowerpot
- **THEN** the simulation performs the corresponding move, swap, or merge and applies configured cooldown and tier-limit rules

#### Scenario: On-board plant is dragged to the nursery
- **WHEN** the player releases an on-board plant over a legal nursery slot
- **THEN** the plant returns to that nursery slot and applies the configured relocation cooldown when required

#### Scenario: Drag ends on an illegal target
- **WHEN** a plant drag is released over an illegal destination
- **THEN** no plant position changes and the existing invalid/return feedback is shown

### Requirement: Focused drag hint presentation
Drag targeting SHALL retain visual target highlights, but the floating text hint SHALL appear only for a legal plant merge and SHALL use the configured next tier in a compact content-sized frame.

#### Scenario: Plant can be planted, moved, swapped, returned, or cannot be dropped
- **WHEN** a dragged plant is over a non-merge target
- **THEN** the target highlight can communicate legality and no floating text hint is shown

#### Scenario: Plant can be merged
- **WHEN** a dragged plant is over an equal-tier plant with the same definition ID and its upgrade profile contains a next tier
- **THEN** a compact floating hint identifies that configured resulting tier
