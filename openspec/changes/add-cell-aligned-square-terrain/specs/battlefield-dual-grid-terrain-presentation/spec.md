## ADDED Requirements

### Requirement: Cell-aligned square terrain coexists with Dual-Grid terrain
The battlefield terrain renderer SHALL present a base-only visual cell as one opaque, cell-aligned square using its bound base texture. Dual-Grid landforms SHALL continue to use their existing transparent mask, contour, and pair-edge layers, and the system MUST NOT generate sixteen mask variants for a pure-square surface.

#### Scenario: Render a pure-square cell
- **WHEN** a visual cell has a registered base surface and empty landform, contour, and pair-edge identifiers
- **THEN** the renderer draws exactly the opaque base layer for that gameplay cell

#### Scenario: Render separated square and Dual-Grid regions
- **WHEN** a valid map contains spatially separated base-only and Dual-Grid regions
- **THEN** each region uses its own existing rendering path without changing draw order or gameplay geometry

#### Scenario: Render two unlike square surfaces
- **WHEN** base-only grass and base-only soil cells share an edge
- **THEN** the renderer presents an intentional hard boundary aligned to the gameplay grid

### Requirement: Isolated square-terrain trial evidence
The project SHALL provide an isolated grass-and-soil square-terrain trial whose assets and palette do not replace release content. Its base textures MUST be opaque, use Repeat wrapping, remain free of visible seams in a repeated view, and follow the approved clean cartoon palette with broad hand-painted variation rather than fine noise.

#### Scenario: Generate the comparison board
- **WHEN** the square-terrain trial generator runs
- **THEN** it produces a comparison board at battlefield cell scale showing repeated pure grass and pure soil plus a spatially separated Dual-Grid reference

#### Scenario: Validate trial texture imports
- **WHEN** automated trial-art validation inspects the grass and soil base textures
- **THEN** each texture is opaque, imported with Repeat wrapping, and passes the configured opposite-edge continuity threshold

#### Scenario: Protect release content
- **WHEN** the trial assets and comparison board are generated
- **THEN** the release palette, playable map catalog, release scenes, and gameplay rules remain unchanged
