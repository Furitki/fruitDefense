## ADDED Requirements

### Requirement: Orthogonal planting grid
The battlefield SHALL derive every initial pot and expansion candidate from a shared integer-column and integer-row grid, and each cell MUST render at its grid-aligned center.

#### Scenario: Initial battlefield render
- **WHEN** a new game displays the battlefield
- **THEN** every active pot is centered on a unique orthogonal grid cell

### Requirement: Contiguous soil regions
Planting cells SHALL be visually grouped into contiguous rectangular or stepped soil regions around reserved road and orchard-core space rather than appearing as isolated scattered pots.

#### Scenario: Viewing the available planting area
- **WHEN** the battlefield is visible before any plant is placed
- **THEN** adjacent planting cells read as regular joined blocks with consistent size and spacing

### Requirement: Reserved cells remain unavailable
Road, entrance, exit, and orchard-core cells MUST NOT become active pots or legal expansion destinations.

#### Scenario: Inspecting reserved space
- **WHEN** the grid soil layer is rendered
- **THEN** no plantable soil cell overlaps the road gates or orchard core
