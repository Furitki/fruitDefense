## MODIFIED Requirements

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
- **WHEN** the player clicks or begins dragging an available weapon or flowerpot from ContextTray
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
- **WHEN** the player activates RefreshAction with sufficient or insufficient sunlight
- **THEN** the existing simulation result is preserved and the complete action remains inside the safe-content edge

### Requirement: Contextual plant details
An inspected plant SHALL replace the tool anatomy inside ContextTray with a compact information anatomy, while no separate detail rectangle or hidden detail band SHALL be reserved when no plant is inspected.

#### Scenario: Plant is inspected
- **WHEN** the player clicks an on-board or nursery plant without starting a drag
- **THEN** ContextTray shows its identity, finite essential combat values, and a touch-sized close action while NurseryTray, RefreshAction, and WaveAction remain visible

#### Scenario: Inspection is closed
- **WHEN** the player closes the compact detail anatomy
- **THEN** ContextTray returns to the tool anatomy without changing any plant position or battle state

### Requirement: Battle control touch and acceptance coverage
Primary battle controls SHALL remain at least 44 logical points on their shortest interactive dimension, and the project SHALL validate the paper-page composition through Unity smoke and real WebGL portrait evidence.

#### Scenario: Geometry smoke
- **WHEN** editor validation checks the reference composition
- **THEN** the stage and lower sections are ordered, non-overlapping, touch-sized, aligned to the four-point rhythm, and contain no legacy enclosing BattleSurface

#### Scenario: WebGL portrait evidence
- **WHEN** the rebuilt WebGL player is captured at the 402-by-874 reference viewport
- **THEN** the evidence shows one heavy gameplay stage, mutually exclusive ContextTray modes, persistent NurseryTray and RefreshAction, finite text containment, and an 8-to-40-point lower closeout
