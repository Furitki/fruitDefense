## ADDED Requirements

### Requirement: Explicit pure-square terrain presets
The canonical battlefield map authoring workflow SHALL expose named pure-square presets for the registered grass and soil surfaces. Applying a pure-square preset MUST set the selected cell's base surface and clear its landform, contour, and pair-edge identifiers in one undoable edit, and MUST NOT create or select a synthetic sixteen-mask TileSet.

#### Scenario: Paint a grass square
- **WHEN** an author applies the grass-square preset to a visual cell
- **THEN** the cell stores grass as its base surface and stores empty landform, contour, and pair-edge identifiers

#### Scenario: Replace a layered cell with a soil square
- **WHEN** an author applies the soil-square preset to a cell that previously contained a Dual-Grid landform
- **THEN** the cell stores soil as its base surface and removes every optional layered-terrain identifier in the same undoable edit

#### Scenario: Preview a pure-square cell
- **WHEN** a base-only visual cell is previewed by the canonical map workflow
- **THEN** it is presented from the surface's opaque base texture without resolving a Dual-Grid mask

### Requirement: Single representation per touching surface
The battlefield visual-cell compiler SHALL reject a map when the same surface appears as base-only terrain on one cell and as a Dual-Grid landform on another cell that shares an edge or vertex. The compiler SHALL allow disconnected regions to use different representations and SHALL allow different base-only surfaces to touch.

#### Scenario: Reject edge contact across representations
- **WHEN** a grass base-only cell shares an edge with a cell whose landform surface is grass
- **THEN** compilation fails with a focused `surface.shared-representation-mix` diagnostic identifying both cells and the surface

#### Scenario: Reject diagonal contact across representations
- **WHEN** a soil base-only cell shares only a vertex with a cell whose landform surface is soil
- **THEN** compilation fails with a focused `surface.shared-representation-mix` diagnostic identifying both cells and the surface

#### Scenario: Allow disconnected use of both representations
- **WHEN** base-only grass and Dual-Grid grass regions do not share an edge or vertex
- **THEN** visual-cell compilation succeeds if all other terrain rules are satisfied

#### Scenario: Allow unlike square surfaces to touch
- **WHEN** a base-only grass cell touches a base-only soil cell
- **THEN** visual-cell compilation succeeds and preserves their intentional cell-aligned boundary
