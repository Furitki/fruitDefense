## MODIFIED Requirements

### Requirement: Portrait-first composition
The game SHALL render a dedicated portrait composition for the iPhone 17 reference viewport of 402 by 874 logical points and SHALL preserve the approved information hierarchy, typography alignment, icon alignment, and component containment at 360×800, 375×812, 402×874, and 430×932 full and representative inset safe areas, rather than applying a uniform transform to a former layout or reference mockup.

#### Scenario: Initial portrait screen
- **WHEN** the game opens at the iPhone 17 portrait aspect
- **THEN** the status header, battlefield, build tray, and primary battle controls appear in a single top-to-bottom flow without horizontal scrolling, overlap, clipped controls, misaligned repeated content, or stretched ornamental art

#### Scenario: Wider or taller portrait screen
- **WHEN** the game opens on another supported portrait viewport
- **THEN** the layout preserves the same information order and visual hierarchy, uses available safe-area space, and keeps text, icons, indicators, and nine-slice boundaries inside their authoritative rectangles

### Requirement: Mobile readability and touch targets
Normal player-facing text SHALL be at least 15 logical points at the reference viewport, SHALL use an explicit semantic role and line policy, and SHALL fit its authoritative rectangle with the packaged font at every supported full and inset viewport. Primary interactive controls SHALL provide a touch target at least 44 logical points high or wide on their shortest interactive dimension.

#### Scenario: Reference viewport inspection
- **WHEN** the release UI inspection catalog is evaluated at 402 by 874 logical points
- **THEN** required text is readable without zooming, comparable baselines and icon groups meet the alignment tolerance, and primary controls meet the minimum touch-target size

#### Scenario: Narrow viewport or long finite copy
- **WHEN** a required Chinese label, status, metric, level description, loading message, or result copy is evaluated at the narrowest supported full or inset width
- **THEN** it fits its declared one-line or finite multi-line policy without clipping, ellipsis, implicit shrink-to-fit, overlap, or escape from the safe area

## ADDED Requirements

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
Lobby, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, and a stable page-level visual center instead of concentrating all meaningful content in the upper half or preserving accidental empty bands.

#### Scenario: Battlefield is projected into its panel
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input while its unused visual gutters are symmetric within tolerance or explicitly occupied by route information

#### Scenario: Lobby or Settlement fills a tall portrait canvas
- **WHEN** the route is shown at a supported full or inset height
- **THEN** its content group is vertically intentional, repeated rows share a rhythm, and lower empty space does not make the route appear unfinished or top-heavy
