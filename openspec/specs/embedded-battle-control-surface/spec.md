# embedded-battle-control-surface Specification

## Purpose
TBD - created by archiving change embed-battle-trays-and-expand-map. Update Purpose after archive.
## Requirements
### Requirement: Enlarged dominant battlefield
The portrait battle screen SHALL make one gameplay stage the dominant surface below the header, SHALL not wrap the stage and lower controls in another enclosing panel, and SHALL contain route, grid, core, pots, plants, enemies, wave controls, and their shared projection within the stage.

#### Scenario: Reference portrait layout
- **WHEN** the battle screen renders at 402 by 874 logical points
- **THEN** the gameplay stage shares Header's full-width track, remains the only persistent heavy structural frame, and contains all projected map content

#### Scenario: Safe-area portrait layout
- **WHEN** the same composition is scaled into a supported portrait safe area
- **THEN** Header, gameplay stage, ContextTray, NurseryTray, RefreshAction, and their complete hit targets remain visible and operable inside the safe area

### Requirement: Contextual tool tray
The three weapon controls and flowerpot control SHALL render in the single ContextTray below the gameplay stage when no plant detail is selected, and each control's draw, click, and drag-source bounds SHALL come from that authoritative tray geometry.

#### Scenario: Tool is selected or dragged
- **WHEN** the player clicks or begins dragging an available weapon or flowerpot from the embedded tray
- **THEN** the existing selection or drag behavior starts from the visible control without coordinate drift

#### Scenario: Plant detail owns the context tray
- **WHEN** a plant is selected for inspection
- **THEN** the detail anatomy replaces the tool anatomy in the same ContextTray rectangle and the two anatomies are not drawn together

### Requirement: Persistent nursery and refresh controls
The five nursery slots and RefreshAction SHALL render as persistent light sections below ContextTray, SHALL preserve nursery drag/drop and refresh-cost behavior, and SHALL use shared draw and hit-test rectangles without an enclosing lower-page panel.

#### Scenario: Nursery plant is moved
- **WHEN** the player drags a nursery plant to the map or returns an on-board plant to a nursery slot
- **THEN** source, destination, highlight, and drop resolution align with the persistent NurseryTray controls

#### Scenario: Nursery is refreshed
- **WHEN** the player activates the embedded refresh action with sufficient or insufficient sunlight
- **THEN** the existing simulation result is preserved and the complete action remains inside the safe-content edge

### Requirement: No persistent bottom guidance or status panel
The default battle screen SHALL NOT reserve or render the former generic guidance block or the standalone bottom operation-status panel.

#### Scenario: No plant is inspected
- **WHEN** the battle screen is idle with no inspected plant
- **THEN** no generic instruction paragraph, operation-hint heading, or persistent bottom status copy is visible

### Requirement: Contextual plant details
An inspected plant SHALL replace the tool anatomy inside ContextTray with a compact information anatomy, while no separate detail rectangle or hidden detail band SHALL be reserved when no plant is inspected.

#### Scenario: Plant is inspected
- **WHEN** the player clicks an on-board or nursery plant without starting a drag
- **THEN** ContextTray shows its identity, finite essential combat values, and a touch-sized close action while NurseryTray, RefreshAction, and WaveAction remain visible

#### Scenario: Inspection is closed
- **WHEN** the player closes the compact card
- **THEN** ContextTray returns to the tool anatomy without changing any plant position or battle state

### Requirement: Battle control touch and acceptance coverage
Primary battle controls SHALL remain at least 44 logical points on their shortest interactive dimension, and the project SHALL validate the paper-page composition through Unity smoke and real WebGL portrait evidence.

#### Scenario: Geometry smoke
- **WHEN** editor validation checks the reference composition
- **THEN** the stage and lower sections are ordered, non-overlapping, touch-sized, aligned to the four-point rhythm, and contain no legacy enclosing BattleSurface

#### Scenario: WebGL portrait evidence
- **WHEN** the rebuilt WebGL player is captured at the 402-by-874 reference viewport
- **THEN** the evidence shows one heavy gameplay stage, mutually exclusive ContextTray modes, persistent NurseryTray and RefreshAction, finite text containment, and an 8-to-40-point lower closeout

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

