## MODIFIED Requirements

### Requirement: Four explicit composition presets
The primary painter SHALL expose pure A, pure B, A-on-B, and B-on-A as four named visual presets, SHALL map each preset to the existing validated base or ordered-pair operation without maintaining a second authoring state, and MUST render successful preset cards from the real active base, contour, and directed refined-edge assets rather than a single-material swatch.

#### Scenario: Pure material preset is painted
- **WHEN** the author paints the configured pure-grass preset over a composed cell
- **THEN** the cell contains the grass base and its landform and refined edge are cleared

#### Scenario: Grass-on-soil preset is displayed and painted
- **WHEN** the grass-on-soil preset has an exact refined edge for the active contour
- **THEN** its card shows a representative grass/soil/refined-edge composition and the gesture writes soil as the base, grass as the landform, and the exact directed refined edge for every affected cell

#### Scenario: Pair direction is reversed
- **WHEN** the author selects soil-on-grass instead of grass-on-soil
- **THEN** the card and mutation use the distinct reverse pair and never reuse the forward pair's edge binding

### Requirement: Direct Scene painting with bounded lifecycle and Undo
The painter SHALL show the active tool and a lightweight outline for the currently hovered Grid cell in the embedded Scene view, SHALL update and clear that outline as the pointer crosses cell, panel, and window boundaries without requiring a paint click, SHALL support left-drag painting, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input and restore temporary mouse-move settings when painting stops, the target becomes invalid, play mode begins, the laboratory closes, or scripts reload.

#### Scenario: Pointer moves across cells without painting
- **WHEN** an active author moves the pointer across several Scene cells without holding a mouse button
- **THEN** the outlined cell follows each newly resolved cell promptly without drawing a textured brush ghost or remaining on an earlier cell

#### Scenario: Pointer crosses a non-canvas boundary
- **WHEN** the pointer enters the embedded panel or leaves the Scene window
- **THEN** the previous cell outline is cleared and no terrain mutation occurs

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags across multiple cells outside the embedded panel
- **THEN** each newly entered cell is painted once and one Undo command restores the pre-gesture state of all affected cells

#### Scenario: Laboratory closes during an active session
- **WHEN** the laboratory closes or loses its valid target
- **THEN** Scene input, hover state, and temporary mouse-move settings are released and subsequent clicks use normal Unity Scene behavior without changing terrain

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage and editor visual evidence for refined-only presets, real composed card previews, responsive lightweight hover feedback, and the single-workspace workflow, and the ordinary release WebGL build MUST preserve the existing layered terrain output, gameplay behavior, and `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** the focused terrain painter validation is executed
- **THEN** target selection, semantic metadata, all four presets, required directed refinements, representative preview sources, hover change/clear behavior, advanced erasure, invalid-operation feedback, Undo grouping, and session teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** the existing layered sample is painted through the refined-only front end and the release build is validated
- **THEN** the runtime terrain composition and player flow match the accepted baseline with no editor-only painter code in the player UI

## REMOVED Requirements

### Requirement: Contextual optional edge refinement
**Reason**: The accepted refined edge is now the only supported landform-bearing result in the terrain laboratory, so exposing a bare-versus-refined authoring choice is misleading and creates content the primary workflow no longer wants.

**Migration**: Remove the edge-mode controls and session state, make exact directed refinement mandatory for pair and landform brushes, and keep low-level serialized compatibility only for reading or explicitly migrating older regions.
