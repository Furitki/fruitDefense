## MODIFIED Requirements

### Requirement: Balanced route composition
Lobby, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, a stable page-level visual center, and route-specific visual anchors instead of concentrating all meaningful content in the upper half, preserving accidental empty bands, or presenting every route as the same shallow panel stack; Battle SHALL use the gameplay stage as its sole normal-state heavy structural anchor and SHALL close its control stack near the lower safe-content edge.

#### Scenario: Battlefield is projected into its panel
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input, remains contained by the gameplay-stage frame, and keeps symmetric visual gutters within tolerance or explicitly occupies them with route information

#### Scenario: Battlefield is projected into its stage
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input, remains contained by the gameplay-stage frame, and keeps symmetric visual gutters within tolerance

#### Scenario: Battle has no selected detail
- **WHEN** the ready or active Battle state is rendered without a selected plant
- **THEN** the stage, context tools, nursery, and refresh action form one intentional top-to-bottom rhythm without an unused detail band or a second enclosing lower-page frame

#### Scenario: Lobby fills a tall portrait canvas
- **WHEN** Lobby is shown at a supported full or inset height
- **THEN** the title, three repeated illustrated level cards, selection state, primary action, and route backdrop form one intentional vertical rhythm without unfinished lower empty space or background detail competing with copy

#### Scenario: Settlement fills a tall portrait canvas
- **WHEN** Settlement victory or defeat is shown at a supported full or inset height
- **THEN** the large outlined outcome typography and its fitted banner form the primary hero signal, the result illustration supports it without competing, the outcome glyphs fit inside the banner's significant-alpha envelope with balanced breathing room, the three borderless metric rows share one rhythm, and primary/secondary actions visibly close the composition


#### Scenario: Lobby or Settlement fills a tall portrait canvas
- **WHEN** the route is shown at a supported full or inset height
- **THEN** its content group is vertically intentional, repeated rows share a rhythm, and lower empty space does not make the route appear unfinished or top-heavy

## ADDED Requirements

### Requirement: Information is not styled as interaction
Read-only route information SHALL default to borderless icon-and-copy composition inside an existing structural parent, while closed bordered surfaces SHALL be reserved for actionable, selectable, input, modal, or explicitly grouped status regions.

#### Scenario: Settlement metrics are rendered
- **WHEN** completed level, reached wave, and remaining lives are shown
- **THEN** the three rows use spacing and aligned icon/copy groups without drawing individual button-like or input-like borders

### Requirement: Readable battle header hierarchy
The portrait battle header SHALL present one title group, three micro resource groups, and two same-family control targets with readable separation, actual-size icon legibility, and at least 44 logical points of interaction area for controls.

#### Scenario: Battle opens at the reference viewport
- **WHEN** the ready battle state renders at 402×874
- **THEN** the sun, core, and wave icons render from their 18px micro slots, their values remain primary over supplemental labels, pause and speed controls share a consistent visible family, and no group overlaps or straddles the header surface

#### Scenario: Battle runs in an inset safe area
- **WHEN** the same header renders in a supported inset profile
- **THEN** its title, metrics, dividers, and controls preserve order and containment while the battlefield draw and hit projections remain identical to the updated authoritative board rectangle
