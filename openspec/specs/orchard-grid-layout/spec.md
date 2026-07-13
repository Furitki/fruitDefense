# orchard-grid-layout Specification

## Purpose
TBD - created by archiving change redesign-grid-and-drag-feedback. Update Purpose after archive.
## Requirements
### Requirement: Orthogonal planting grid
The battlefield SHALL derive every initial pot and expansion candidate from a shared integer-column and integer-row grid, and each cell MUST render at its grid-aligned center.

#### Scenario: Initial battlefield render
- **WHEN** a new game displays the battlefield
- **THEN** every active pot is centered on a unique orthogonal grid cell

### Requirement: Contiguous soil regions
Planting cells SHALL be visually grouped into contiguous rectangular or stepped soil regions around reserved road and orchard-destination space, every soil cell MUST render as a strict square without rounded corners, and adjacent cells MUST retain consistent size and spacing.

#### Scenario: Viewing the available planting area
- **WHEN** the battlefield is visible before any plant is placed
- **THEN** adjacent planting cells read as regular joined blocks of equal square cells

### Requirement: Reserved cells remain unavailable
Road, entrance, and orchard-destination cells MUST NOT become active pots or legal expansion destinations.

#### Scenario: Inspecting reserved space
- **WHEN** the grid soil layer is rendered
- **THEN** no plantable soil cell overlaps the road entrance or square orchard destination

### Requirement: Orchard is the path destination
The battlefield SHALL render one square orchard destination centered at the final zombie path point and MUST NOT render a separate exit marker elsewhere.

#### Scenario: Zombie route presentation
- **WHEN** the battlefield and zombie route are visible
- **THEN** the road terminates visually at the square orchard destination
