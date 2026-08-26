## MODIFIED Requirements

### Requirement: Full-width mobile control surface
The portrait build SHALL present one full-width context tray below the battlefield that shows equipment and expansion tools by default and replaces them with a dismissible selected-plant detail when relevant, followed by persistent nursery results and refresh controls; the wave action SHALL remain available in the battlefield control strip, and no hidden detail panel SHALL reserve a permanent empty band.

#### Scenario: No plant selected
- **WHEN** no plant is selected
- **THEN** the context tray shows the four build tools, the nursery and refresh controls remain visible, and the lower composition contains no empty detail owner

#### Scenario: Plant selected
- **WHEN** the player selects a plant
- **THEN** the same context tray shows its finite title, attributes, and a close target of at least 44 logical points while the nursery, refresh, and persistent primary wave action remain visible and operable

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

## ADDED Requirements

### Requirement: Orchard-paper Battle structural hierarchy
Battle SHALL render light orchard-paper Header and section surfaces around one heavier gameplay-stage frame, reserve heavy outlines for the stage and blocking overlays, and keep primary, secondary, and quiet actions visually distinct without changing their command or hit semantics.

#### Scenario: Ready Battle renders at the reference viewport
- **WHEN** Battle ready is captured at 402×874
- **THEN** Header and control sections use the approved light surface family, the battlefield is the only normal-state large component with a 3–5 capture-pixel outline band, standard sections use 1–2 capture-pixel outlines, and no enclosing panel repeats the stage border around the lower control stack

#### Scenario: Battle renders in an inset safe area
- **WHEN** the same state is rendered with representative top and bottom insets
- **THEN** Header, gameplay stage, context tray, nursery, refresh action, text, and touch targets remain contained and preserve the same structural-weight order
