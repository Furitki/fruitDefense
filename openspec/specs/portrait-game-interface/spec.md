# portrait-game-interface Specification

## Purpose
TBD - created by archiving change rebuild-portrait-webgl-layout. Update Purpose after archive.
## Requirements
### Requirement: Portrait-first composition
The game SHALL render a dedicated portrait composition for the iPhone 17 reference viewport of 402 by 874 logical points, rather than applying a uniform transform to the former landscape side panel.

#### Scenario: Initial portrait screen
- **WHEN** the game opens at the iPhone 17 portrait aspect
- **THEN** the status header, battlefield, build tray, and primary battle controls appear in a single top-to-bottom flow without horizontal scrolling, overlap, or clipped controls

#### Scenario: Wider or taller portrait screen
- **WHEN** the game opens on another portrait viewport
- **THEN** the layout preserves the same information order and uses available safe-area space without stretching the legacy landscape composition

### Requirement: Safe-area-aware presentation
The portrait interface SHALL keep interactive and player-readable content inside `Screen.safeArea` while allowing non-interactive backgrounds to extend to the physical screen edges.

#### Scenario: Device with top and bottom insets
- **WHEN** the runtime reports non-zero top or bottom safe-area insets
- **THEN** the status header and bottom controls remain fully visible and operable outside the obscured regions

### Requirement: Full-width mobile control surface
The portrait build SHALL present equipment, expansion, nursery results, refresh, and wave controls in a full-width mobile control surface below the battlefield. Secondary plant details SHALL appear only when relevant and SHALL not reserve a permanent empty column or panel.

#### Scenario: No plant selected
- **WHEN** no plant is selected
- **THEN** the control surface prioritizes build tools, nursery results, refresh, and wave actions without displaying a large empty details area

#### Scenario: Plant selected
- **WHEN** the player selects a plant
- **THEN** its details and contextual actions appear in a dismissible or collapsible surface without hiding the persistent primary battle action

### Requirement: WebGL-safe Chinese text
All player-facing Chinese text SHALL render from a font asset packaged with the build and SHALL NOT depend on fonts installed on the host operating system.

#### Scenario: Browser without Chinese system fonts
- **WHEN** the WebGL build runs in a clean browser environment with no accessible system font APIs
- **THEN** the title, resource counts, labels, buttons, plant details, status messages, and modal text remain visible and legible

### Requirement: Mobile readability and touch targets
Normal player-facing text SHALL be at least 15 logical points at the reference viewport, and primary interactive controls SHALL provide a touch target at least 44 logical points high or wide on their shortest interactive dimension.

#### Scenario: Reference viewport inspection
- **WHEN** the initial screen is captured at 402 by 874 logical points
- **THEN** required text is readable without zooming and primary controls meet the minimum touch-target size

### Requirement: Preserved battlefield interaction
The portrait layout SHALL preserve planting, moving, merging, weapon installation, expansion, nursery return, wave control, pause, and speed interactions without changing gameplay simulation rules.

#### Scenario: Drag interaction in portrait
- **WHEN** the player drags a plant, weapon, or pot on the portrait interface
- **THEN** the drag preview and legal target highlight align with the rendered source and destination controls

