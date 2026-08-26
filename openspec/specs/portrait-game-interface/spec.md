# portrait-game-interface Specification

## Purpose
TBD - created by archiving change rebuild-portrait-webgl-layout. Update Purpose after archive.
## Requirements
### Requirement: Portrait-first composition
The game SHALL render a dedicated portrait composition for the iPhone 17 reference viewport of 402 by 874 logical points and SHALL preserve the approved information hierarchy, typography alignment, icon alignment, and component containment at 360×800, 375×812, 402×874, and 430×932 full and representative inset safe areas, rather than applying a uniform transform to a former layout or reference mockup.

#### Scenario: Initial portrait screen
- **WHEN** the game opens at the iPhone 17 portrait aspect
- **THEN** the status header, battlefield, build tray, and primary battle controls appear in a single top-to-bottom flow without horizontal scrolling, overlap, clipped controls, misaligned repeated content, or stretched ornamental art

#### Scenario: Wider or taller portrait screen
- **WHEN** the game opens on another supported portrait viewport
- **THEN** the layout preserves the same information order and visual hierarchy, uses available safe-area space, and keeps text, icons, indicators, and nine-slice boundaries inside their authoritative rectangles

### Requirement: Safe-area-aware presentation
The portrait interface SHALL keep interactive and player-readable content inside `Screen.safeArea` while allowing non-interactive backgrounds to extend to the physical screen edges.

#### Scenario: Device with top and bottom insets
- **WHEN** the runtime reports non-zero top or bottom safe-area insets
- **THEN** the status header and bottom controls remain fully visible and operable outside the obscured regions

### Requirement: Full-width mobile control surface
The portrait build SHALL present one full-width context tray below the battlefield that shows equipment and expansion tools by default and replaces them with a dismissible selected-plant detail when relevant, followed by persistent nursery results and refresh controls; the wave action SHALL remain available in the battlefield control strip, and no hidden detail panel SHALL reserve a permanent empty band.

#### Scenario: No plant selected
- **WHEN** no plant is selected
- **THEN** the context tray shows the four build tools, the nursery and refresh controls remain visible, and the lower composition contains no empty detail owner

#### Scenario: Plant selected
- **WHEN** the player selects a plant
- **THEN** the same context tray shows its finite title, attributes, and a close target of at least 44 logical points while the nursery, refresh, and persistent primary wave action remain visible and operable

### Requirement: WebGL-safe Chinese text
All player-facing Chinese text SHALL render from a font asset packaged with the build and SHALL NOT depend on fonts installed on the host operating system.

#### Scenario: Browser without Chinese system fonts
- **WHEN** the WebGL build runs in a clean browser environment with no accessible system font APIs
- **THEN** the title, resource counts, labels, buttons, plant details, status messages, and modal text remain visible and legible

### Requirement: Mobile readability and touch targets
Normal player-facing text SHALL be at least 15 logical points at the reference viewport, SHALL use an explicit semantic role and line policy, and SHALL fit its authoritative rectangle with the packaged font at every supported full and inset viewport. Primary interactive controls SHALL provide a touch target at least 44 logical points high or wide on their shortest interactive dimension.

#### Scenario: Reference viewport inspection
- **WHEN** the release UI inspection catalog is evaluated at 402 by 874 logical points
- **THEN** required text is readable without zooming, comparable baselines and icon groups meet the alignment tolerance, and primary controls meet the minimum touch-target size

#### Scenario: Narrow viewport or long finite copy
- **WHEN** a required Chinese label, status, metric, level description, loading message, or result copy is evaluated at the narrowest supported full or inset width
- **THEN** it fits its declared one-line or finite multi-line policy without clipping, ellipsis, implicit shrink-to-fit, overlap, or escape from the safe area

### Requirement: Preserved battlefield interaction
The portrait layout SHALL preserve planting, moving, merging, weapon installation, expansion, nursery return, wave control, pause, and speed interactions without changing gameplay simulation rules.

#### Scenario: Drag interaction in portrait
- **WHEN** the player drags a plant, weapon, or pot on the portrait interface
- **THEN** the drag preview and legal target highlight align with the rendered source and destination controls

### Requirement: Complete portrait canvas in desktop hosts
The ordinary-WebGL host SHALL uniformly fit the complete portrait canvas within the available desktop or embedded-browser viewport and SHALL preserve its aspect ratio, centered letterboxing, and input mapping without scroll-dependent access to the top or bottom of the game.

#### Scenario: Short desktop review window
- **WHEN** the 402 by 874 game is opened in a 1280 by 720 or another viewport shorter than the reference canvas
- **THEN** the full title, top safe area, primary content, bottom controls, and bottom safe area are simultaneously visible with no negative canvas origin, page scrollbar dependency, non-uniform stretch, or pointer-coordinate drift

#### Scenario: Tall or wide desktop window
- **WHEN** the game is opened in a larger wide desktop viewport
- **THEN** the portrait canvas remains uniformly scaled, centered, and fully visible rather than growing beyond either available axis

### Requirement: Visually centered component groups
Actions, metrics, status rows, and result rows that combine icons, indicators, labels, or values SHALL align the rendered group as one composition rather than centering text independently from a separately anchored icon.

#### Scenario: Action contains an icon and Chinese label
- **WHEN** a primary, secondary, quiet, danger, loading, or disabled action is rendered
- **THEN** the combined icon-and-label group is optically centered within the action, maintains its semantic gap, and does not become left-heavy or right-heavy at any supported scale

#### Scenario: Repeated metric rows are rendered
- **WHEN** multiple comparable metrics appear in a header or result card
- **THEN** icon ink, label baseline, value baseline, and group insets align consistently and no icon straddles the component border

### Requirement: Balanced route composition
Lobby, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, and a stable page-level visual center instead of concentrating all meaningful content in the upper half or preserving accidental empty bands; Battle SHALL use the gameplay stage as its sole normal-state heavy structural anchor and SHALL close its control stack near the lower safe-content edge.

#### Scenario: Battlefield is projected into its stage
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input, remains contained by the gameplay-stage frame, and keeps symmetric visual gutters within tolerance

#### Scenario: Battle has no selected detail
- **WHEN** the ready or active Battle state is rendered without a selected plant
- **THEN** the stage, context tools, nursery, and refresh action form one intentional top-to-bottom rhythm without an unused detail band or a second enclosing lower-page frame

#### Scenario: Lobby or Settlement fills a tall portrait canvas
- **WHEN** the route is shown at a supported full or inset height
- **THEN** its content group is vertically intentional, repeated rows share a rhythm, and lower empty space does not make the route appear unfinished or top-heavy

### Requirement: Orchard-paper Battle structural hierarchy
Battle SHALL render light orchard-paper Header and section surfaces around one heavier gameplay-stage frame, reserve heavy outlines for the stage and blocking overlays, and keep primary, secondary, and quiet actions visually distinct without changing their command or hit semantics.

#### Scenario: Ready Battle renders at the reference viewport
- **WHEN** Battle ready is captured at 402×874
- **THEN** Header and control sections use the approved light surface family, the battlefield is the only normal-state large component with a 3–5 capture-pixel outline band, standard sections use 1–2 capture-pixel outlines, and no enclosing panel repeats the stage border around the lower control stack

#### Scenario: Battle renders in an inset safe area
- **WHEN** the same state is rendered with representative top and bottom insets
- **THEN** Header, gameplay stage, context tray, nursery, refresh action, text, and touch targets remain contained and preserve the same structural-weight order
