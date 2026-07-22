## ADDED Requirements

### Requirement: Enlarged dominant battlefield
The portrait battle screen SHALL make the battlefield the dominant surface below the header and SHALL enlarge its reference map region beyond the previous 394-by-398 logical-point region without clipping route, grid, core, pots, plants, enemies, or wave controls.

#### Scenario: Reference portrait layout
- **WHEN** the battle screen renders at 402 by 874 logical points
- **THEN** the battlefield spans the available logical width, is taller than the previous battlefield, and all projected map content remains contained

#### Scenario: Safe-area portrait layout
- **WHEN** the same composition is scaled into a supported portrait safe area
- **THEN** the header and complete battle surface remain visible and operable inside the safe area

### Requirement: Embedded tool tray
The three weapon controls and flowerpot control SHALL render inside the battle surface, and each control's draw, click, and drag-source bounds SHALL come from the same embedded tool-tray geometry.

#### Scenario: Tool is selected or dragged
- **WHEN** the player clicks or begins dragging an available weapon or flowerpot from the embedded tray
- **THEN** the existing selection or drag behavior starts from the visible control without coordinate drift

### Requirement: Embedded refresh tray
The five nursery slots and refresh action SHALL render inside the battle surface, SHALL preserve nursery drag/drop behavior and refresh cost behavior, and SHALL use shared draw and hit-test rectangles.

#### Scenario: Nursery plant is moved
- **WHEN** the player drags a nursery plant to the map or returns an on-board plant to a nursery slot
- **THEN** source, destination, highlight, and drop resolution align with the embedded nursery controls

#### Scenario: Nursery is refreshed
- **WHEN** the player activates the embedded refresh action with sufficient or insufficient sunlight
- **THEN** the existing simulation result is preserved and the control remains inside the battle surface

### Requirement: No persistent bottom guidance or status panel
The default battle screen SHALL NOT reserve or render the former generic guidance block or the standalone bottom operation-status panel.

#### Scenario: No plant is inspected
- **WHEN** the battle screen is idle with no inspected plant
- **THEN** no generic instruction paragraph, operation-hint heading, or persistent bottom status copy is visible

### Requirement: Contextual plant details
An inspected plant SHALL retain a compact information card inside the battle surface, while no detail card content SHALL be reserved when no plant is inspected.

#### Scenario: Plant is inspected
- **WHEN** the player clicks an on-board or nursery plant without starting a drag
- **THEN** a compact card shows its identity and essential combat values with a touch-sized close action

#### Scenario: Inspection is closed
- **WHEN** the player closes the compact card
- **THEN** the detail content disappears without changing any plant position or battle state

### Requirement: Embedded control touch and acceptance coverage
Primary embedded controls SHALL remain at least 44 logical points on their shortest interactive dimension, and the project SHALL validate the new composition through Unity smoke and real WebGL portrait evidence.

#### Scenario: Geometry smoke
- **WHEN** editor validation checks the reference composition
- **THEN** the enlarged map and embedded subregions are contained, ordered, non-overlapping, and touch-sized

#### Scenario: WebGL portrait evidence
- **WHEN** the rebuilt WebGL player is captured at the 402-by-874 reference viewport
- **THEN** the evidence shows the enlarged map, embedded tool and refresh trays, and absence of persistent bottom guidance/status text

### Requirement: Frameless near-cell-size flowerpot and fruit icons
Map flowerpots, map fruits, occupied nursery fruits, nursery flowerpot rewards, and the flowerpot tool SHALL render without persistent opaque square backplates, and their atlas art SHALL nearly fill the associated logical cell without changing its click or drag bounds.

#### Scenario: Idle map entities
- **WHEN** active flowerpots and fruits render without selection or drag feedback
- **THEN** their transparent atlas art is visible at no less than 0.85 of the map tile width and no opaque rectangular underlay or persistent border is drawn beneath it

#### Scenario: Nursery and flowerpot tool icons
- **WHEN** a nursery fruit, nursery flowerpot reward, or flowerpot tool icon renders
- **THEN** the icon uses only a small inset from its logical cell and the cell remains the unchanged interaction target

#### Scenario: Entity interaction feedback
- **WHEN** a flowerpot or fruit is selected, targeted, dragged, or returned
- **THEN** the relevant state is indicated with a transient outline while the persistent backplate remains absent

### Requirement: Per-plant flowerpot height configuration
Every plant definition SHALL expose an independent non-negative flowerpot visual-height offset, and the map renderer SHALL apply that value generically without plant-kind-specific height branches.

#### Scenario: Zero height offset
- **WHEN** a plant definition configures its flowerpot visual-height offset as `0`
- **THEN** the plant art center remains unchanged relative to the shared map visual rectangle

#### Scenario: Unit height offset
- **WHEN** a plant definition configures its flowerpot visual-height offset as `1`
- **THEN** the plant art center moves upward by exactly one logical point

#### Scenario: Independently tuned bundled plants
- **WHEN** the bundled plant catalog is compiled and rendered on map flowerpots
- **THEN** each plant uses its own configured offset while nursery icons and drag ghosts remain centered
