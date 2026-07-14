## ADDED Requirements

### Requirement: Inspection-only plant click
A normal click on a plant SHALL select that plant for inspection and SHALL NOT arm, place, move, return, or merge any plant.

#### Scenario: On-board plant is clicked
- **WHEN** the player clicks an on-board plant without an active explicit tool and without crossing the drag threshold
- **THEN** that plant becomes inspected, its information surface opens, and no plant changes flowerpot or nursery position

#### Scenario: Nursery plant is clicked
- **WHEN** the player clicks a nursery plant without starting a drag
- **THEN** that plant's information is shown and no pending click-to-place action is created

#### Scenario: Another occupied flowerpot is clicked
- **WHEN** one plant is already inspected and the player clicks a different on-board plant
- **THEN** inspection switches to the clicked plant and neither plant moves or merges

### Requirement: No click-to-relocate destination
Flowerpot and nursery-slot clicks SHALL NOT consume the inspected plant as a relocation source.

#### Scenario: Empty flowerpot is clicked after inspection
- **WHEN** an on-board or nursery plant is inspected and the player clicks an empty active flowerpot
- **THEN** no plant is placed or moved and the interface can direct the player to drag the plant instead

#### Scenario: Empty nursery slot is clicked after inspection
- **WHEN** an on-board plant is inspected and the player clicks an empty nursery slot
- **THEN** the plant remains on its flowerpot and no return-to-nursery operation occurs

### Requirement: Drag-only plant relocation
Plant placement, movement, return-to-nursery, and merging SHALL remain available through drag-and-drop and SHALL continue to use simulation legality checks.

#### Scenario: Nursery plant is dragged to a flowerpot
- **WHEN** the player drags a nursery plant across the activation threshold and releases it over a legal empty flowerpot
- **THEN** the plant is placed on that flowerpot

#### Scenario: On-board plant is dragged to another flowerpot
- **WHEN** the player drags an on-board plant to a legal empty or compatible occupied flowerpot
- **THEN** the simulation performs the corresponding move or merge and preserves existing cooldown and merge rules

#### Scenario: On-board plant is dragged to the nursery
- **WHEN** the player releases an on-board plant over a legal nursery slot
- **THEN** the plant returns to that nursery slot

#### Scenario: Drag ends on an illegal target
- **WHEN** a plant drag is released over an illegal destination
- **THEN** no plant position changes and the existing invalid/return feedback is shown

### Requirement: Selected attack-range presentation
An inspected on-board plant with positive effective attack range SHALL show a visible range overlay centered on its rendered position and projected from the same map distance used by combat targeting.

#### Scenario: Attacking plant is inspected
- **WHEN** the player clicks an on-board plant whose effective range is greater than zero
- **THEN** its information surface opens and a range overlay appears beneath battlefield entities at the plant's current position

#### Scenario: Inspected plant moves by drag
- **WHEN** an inspected plant is successfully dragged to another flowerpot
- **THEN** inspection remains on that plant and the range overlay recenters on its new position

#### Scenario: Projected range is checked
- **WHEN** deterministic points immediately inside and outside the simulation range are projected
- **THEN** the overlay boundary separates those points consistently with combat targeting

### Requirement: Zero-range and nursery inspection
Inspection SHALL show plant information without drawing a misleading battlefield attack range when the plant has no positive attack range or no on-board position.

#### Scenario: Support plant is inspected
- **WHEN** the player inspects an on-board support plant with zero attack range
- **THEN** its information surface identifies the zero/no attack range and no positive-radius range circle is drawn

#### Scenario: Nursery plant is inspected
- **WHEN** the inspected plant is in a nursery slot
- **THEN** its information surface is visible and no battlefield-centered range overlay is drawn

### Requirement: Inspection lifecycle
Inspection and its range overlay SHALL clear or update when the inspected plant is closed, removed, relocated off the board, or the game is restarted.

#### Scenario: Information surface is closed
- **WHEN** the player activates the information close control
- **THEN** the inspected plant, information surface, and range overlay are cleared

#### Scenario: Refresh removes an inspected nursery plant
- **WHEN** nursery refresh replaces the inspected plant
- **THEN** stale inspection is cleared without an error or orphaned overlay

#### Scenario: Game is restarted
- **WHEN** the run is reset
- **THEN** inspection, range presentation, and transient drag state are cleared

### Requirement: Click-versus-drag acceptance
The project SHALL verify inspection-only clicks and drag-only relocation through editor/runtime checks and a real WebGL interaction capture.

#### Scenario: WebGL interaction sequence
- **WHEN** acceptance clicks a plant, clicks a destination, and then drags the same plant to that destination
- **THEN** evidence shows information and range after the first click, no relocation after the destination click, and relocation only after the drag
