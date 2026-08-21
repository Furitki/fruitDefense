## MODIFIED Requirements

### Requirement: Balanced route composition
Lobby, Battle, terminal, and Settlement SHALL use intentional occupied-content bounds, symmetric or explicitly justified gutters, a stable page-level visual center, and route-specific visual anchors instead of concentrating all meaningful content in the upper half or presenting every route as the same shallow panel stack.

#### Scenario: Battlefield is projected into its panel
- **WHEN** the battlefield grid is rendered and hit-tested
- **THEN** the grid uses the same projection for drawing and input while its unused visual gutters are symmetric within tolerance or explicitly occupied by route information

#### Scenario: Lobby fills a tall portrait canvas
- **WHEN** Lobby is shown at a supported full or inset height
- **THEN** the title, three repeated illustrated level cards, selection state, primary action, and route backdrop form one intentional vertical rhythm without unfinished lower empty space or background detail competing with copy

#### Scenario: Settlement fills a tall portrait canvas
- **WHEN** Settlement victory or defeat is shown at a supported full or inset height
- **THEN** the outcome banner and large result illustration form the hero region, the outcome glyphs fit inside the banner's significant-alpha envelope, the three borderless metric rows share one rhythm, and primary/secondary actions visibly close the composition

### Requirement: Information is not styled as interaction
Read-only route information SHALL default to borderless icon-and-copy composition inside an existing structural parent, while closed bordered surfaces SHALL be reserved for actionable, selectable, input, modal, or explicitly grouped status regions.

#### Scenario: Settlement metrics are rendered
- **WHEN** completed level, reached wave, and remaining lives are shown
- **THEN** the three rows use spacing and aligned icon/copy groups without drawing individual button-like or input-like borders

## ADDED Requirements

### Requirement: Readable battle header hierarchy
The portrait battle header SHALL present one title group, three micro resource groups, and two same-family control targets with readable separation, actual-size icon legibility, and at least 44 logical points of interaction area for controls.

#### Scenario: Battle opens at the reference viewport
- **WHEN** the ready battle state renders at 402×874
- **THEN** the sun, core, and wave icons render from their 18px micro slots, their values remain primary over supplemental labels, pause and speed controls share a consistent visible family, and no group overlaps or straddles the header surface

#### Scenario: Battle runs in an inset safe area
- **WHEN** the same header renders in a supported inset profile
- **THEN** its title, metrics, dividers, and controls preserve order and containment while the battlefield draw and hit projections remain identical to the updated authoritative board rectangle
