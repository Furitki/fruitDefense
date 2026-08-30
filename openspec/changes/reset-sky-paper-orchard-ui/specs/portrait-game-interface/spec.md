## MODIFIED Requirements

### Requirement: Portrait-first composition
The game SHALL render a dedicated portrait composition for the iPhone 17 reference viewport of 402 by 874 logical points and SHALL preserve the approved sky-paper information hierarchy, typography alignment, icon alignment, and component containment at 360×800, 375×812, 402×874, and 430×932 full and representative inset safe areas, rather than applying a uniform transform to a former layout or reference mockup.

#### Scenario: Initial portrait screen
- **WHEN** Battle opens at the iPhone 17 portrait aspect
- **THEN** the compact Header, single gameplay stage, independent phase/Wave row, ContextTray, NurseryTray, and RefreshAction appear in that top-to-bottom order without horizontal scrolling, overlap, clipped controls, misaligned repeated content, or stretched ornamental art

#### Scenario: Wider or taller portrait screen
- **WHEN** the game opens on another supported portrait viewport
- **THEN** the layout preserves the same information order and sky-paper visual hierarchy, uses available safe-area space, and keeps text, icons, indicators, nine-slice boundaries, and complete targets inside their authoritative rectangles

### Requirement: Full-width mobile control surface
The portrait build SHALL present one independent phase/Wave flow row immediately below the gameplay stage, followed by one full-width ContextTray that shows equipment and expansion tools by default and replaces them with a dismissible selected-plant detail when relevant, then persistent NurseryTray and RefreshAction controls; no phase/Wave action SHALL remain inside the battlefield projection and no hidden detail panel SHALL reserve a permanent empty band.

#### Scenario: No plant selected
- **WHEN** no plant is selected in the ready phase
- **THEN** the phase/Wave row shows the ready status and `开始波次` action, the ContextTray shows the four build tools, the nursery and refresh controls remain visible, and the lower composition contains no empty detail owner

#### Scenario: Plant selected
- **WHEN** the player selects a plant
- **THEN** the same ContextTray shows its finite title, attributes, and a close target of at least 44 logical points while the independent phase/Wave row, NurseryTray, and RefreshAction remain visible and operable

#### Scenario: Active wave
- **WHEN** the game phase is playing and no Wave command is available
- **THEN** the persistent phase/Wave row presents non-action phase or enemy-progress feedback in its authoritative owner and the lower tracks do not move

### Requirement: WebGL-safe Chinese text
All player-facing Chinese text SHALL render from packaged static font assets selected by semantic typography role and SHALL NOT depend on fonts installed on the host operating system, a legacy single-font fallback, or text baked into UI rasters.

#### Scenario: Browser without Chinese system fonts
- **WHEN** the WebGL build runs in a clean browser environment with no accessible system font APIs
- **THEN** the title, resource counts, labels, buttons, plant details, status messages, and modal text render with their assigned display/reading faces and remain visible and legible

#### Scenario: Role font is missing or incomplete
- **WHEN** a release typography role has no packaged font reference or its assigned font lacks a required finite-copy glyph
- **THEN** validation fails before the WebGL build is accepted rather than substituting another runtime or host font

### Requirement: Balanced route composition
Lobby, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, and a stable page-level visual center instead of concentrating all meaningful content in the upper half or preserving accidental empty bands; at 402×874 Battle SHALL target 38–43 percent of resolved safe-content height for its sole normal-state gameplay-stage anchor and SHALL devote the remaining stack to the compact Header, independent phase/Wave row, ContextTray, NurseryTray, RefreshAction, four-point gaps, and safe closeout.

#### Scenario: Battlefield is projected into its stage
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input, remains contained by the gameplay-stage frame, and keeps symmetric visual gutters within tolerance

#### Scenario: Battle has no selected detail
- **WHEN** the ready or active Battle state is rendered without a selected plant
- **THEN** the stage, phase/Wave row, context tools, nursery, and refresh action form one intentional top-to-bottom rhythm without an unused detail band, an in-stage Wave action, or a second enclosing lower-page frame

#### Scenario: Lobby or Settlement fills a tall portrait canvas
- **WHEN** the route is shown at a supported full or inset height
- **THEN** its content group is vertically intentional, repeated rows share the accepted paper-page rhythm, and lower empty space does not make the route appear unfinished or top-heavy

### Requirement: Orchard-paper Battle structural hierarchy
Battle SHALL render a sky-blue edge around a floating warm-paper Header and one
large warm-paper page shell, one inset soil-brown gameplay-stage frame, and an
independent phase/Wave row; it SHALL reserve heavy gameplay outlines for the
stage and blocking overlays and SHALL keep primary, secondary, quiet,
phase-status, and metric roles visually distinct without changing their command
or hit semantics.

#### Scenario: Ready Battle renders at the reference viewport
- **WHEN** Battle ready is captured at 402×874
- **THEN** the Header uses a title row above three raised resource capsules with
  two cream-rimmed yellow compact controls, the light page shell contains the
  inset battlefield and lower tracks, the battlefield is the only large
  component with a 3–5 capture-pixel gameplay outline band, the phase/Wave row
  contains sunlight phase emphasis plus the leaf-green primary command, and
  recipe cards, dashed nursery slots, and the bottom refresh action retain the
  reference anatomy

### Requirement: Reference-faithful Battle component anatomy
At the 402×874 Gate A viewport, Battle SHALL reproduce the supplied reference's
recognizable component construction and relative composition rather than merely
using similar colors on generic rectangles.

#### Scenario: Ready-state component comparison
- **WHEN** the new ready-state Battle capture is reviewed beside the supplied reference
- **THEN** it visibly contains the floating two-row Header, three individual
  metric capsules, two yellow rimmed compact buttons, one rounded paper page
  shell, one inset soil stage, paired phase/Wave blocks, recipe-style build
  cards, five dashed nursery slots, and one thick green bottom refresh action in
  the same top-to-bottom rhythm

#### Scenario: Generic recolor regression
- **WHEN** colors are related to the reference but the visible component rims,
  shadows, grouping, relative scale, or page nesting use the former generic flat anatomy
- **THEN** Gate A fails even if containment, commands, and automated slot checks pass

#### Scenario: Battle renders in an inset safe area
- **WHEN** the same state is rendered with representative top and bottom insets
- **THEN** Header, gameplay stage, phase/Wave row, ContextTray, NurseryTray, RefreshAction, text, ornament, and touch targets remain contained and preserve the same structural-weight order
