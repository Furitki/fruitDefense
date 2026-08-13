## MODIFIED Requirements

### Requirement: Drag-only plant relocation
Plant placement, movement, return-to-nursery, swapping, and merging SHALL remain available through drag-and-drop and SHALL continue to use simulation legality checks. A compatible same-kind, same-star occupied destination SHALL merge; any other occupied plant destination SHALL swap the two plant locations directly when both moved plants satisfy active-wave cooldown rules.

#### Scenario: Nursery plant is dragged to a flowerpot
- **WHEN** the player drags a nursery plant across the activation threshold and releases it over a legal empty flowerpot
- **THEN** the plant is placed on that flowerpot

#### Scenario: On-board plant is dragged to an empty flowerpot
- **WHEN** the player drags an on-board plant to a legal empty flowerpot
- **THEN** the simulation moves the plant and preserves the existing board-move cooldown rule

#### Scenario: Compatible plants are dragged together
- **WHEN** a plant is dragged onto a same-kind, same-star plant below the maximum star level
- **THEN** the simulation performs the existing merge rather than a swap

#### Scenario: Different plants are dragged together
- **WHEN** a plant is dragged onto an occupied destination that does not satisfy the merge predicate and both plants pass movement legality
- **THEN** the two plants exchange their flowerpot or nursery locations directly without changing identity, star level, or equipment

#### Scenario: Swap target is cooling down
- **WHEN** an active-wave swap would relocate either a dragged plant or target plant whose movement cooldown is positive
- **THEN** the swap is illegal and neither plant changes position

#### Scenario: Drag ends on an illegal target
- **WHEN** a plant drag is released over an illegal destination
- **THEN** no plant position changes and the existing invalid/return feedback is shown

## ADDED Requirements

### Requirement: High-resolution attack-range presentation
The selected attack-range overlay SHALL use a raster source of at least 1024 by 1024 pixels, bilinear filtering, and the existing simulation-derived battlefield projection so its edge remains crisp at the 1206-pixel portrait acceptance width.

#### Scenario: Attacking plant is inspected at portrait reference size
- **WHEN** a positive-range on-board plant is inspected on the 1206-by-2622 WebGL canvas
- **THEN** the range overlay has a smooth readable boundary without visible 128-pixel source blockiness

#### Scenario: Range geometry is compared with targeting
- **WHEN** points immediately inside and outside the effective simulation range are projected
- **THEN** increasing texture resolution does not change the overlay center or boundary geometry
