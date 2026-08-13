## MODIFIED Requirements

### Requirement: Discoverable terrain painter entry and target selection
The editor SHALL provide one map-tool entry that activates an embedded layered-terrain laboratory panel inside the active Scene view without opening a separate painter window, SHALL select the currently selected valid layered map or the sole valid layered map in the open scene, and MUST require an explicit target choice when multiple unselected candidates make automatic selection ambiguous.

#### Scenario: Selected layered map opens in the Scene workspace
- **WHEN** an author opens the terrain laboratory while a valid layered map is selected
- **THEN** the active Scene view contains the painter controls, targets that map, and displays its configuration status without requiring Project-window asset selection or another editor window

#### Scenario: Several unselected maps are open
- **WHEN** the painter opens with multiple valid candidates and none is selected
- **THEN** the embedded panel displays an explicit target picker and painting remains inactive until the author chooses a target

#### Scenario: Existing launch surface invokes the painter
- **WHEN** the map Inspector or an existing editor caller invokes the former painter-window open API
- **THEN** the Scene-embedded laboratory is activated and no standalone painter window is created

### Requirement: Direct Scene painting with bounded lifecycle and Undo
The painter SHALL show the active tool in the embedded Scene panel and over the affected Scene cell, SHALL support left-drag painting outside the panel rectangle, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input when painting stops, the target becomes invalid, play mode begins, the laboratory closes, or scripts reload.

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags across multiple cells outside the embedded panel
- **THEN** each newly entered cell is painted once and one Undo command restores the pre-gesture state of all affected cells

#### Scenario: Author interacts with the embedded panel
- **WHEN** the author clicks or drags inside the panel rectangle
- **THEN** the controls may change laboratory state but no terrain cell is painted by the same pointer event

#### Scenario: Laboratory panel collapses during an active session
- **WHEN** the author collapses the embedded panel
- **THEN** the paint session, target, and active brush remain available while the Scene canvas gains space

#### Scenario: Laboratory closes during an active session
- **WHEN** the author explicitly closes the laboratory or its target becomes invalid
- **THEN** Scene input is released and subsequent clicks use normal Unity Scene behavior without changing terrain

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage and editor visual evidence for the single-workspace flow, and the ordinary release WebGL build MUST preserve the existing layered terrain output, gameplay behavior, and `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** the focused terrain painter validation is executed
- **THEN** target selection, embedded hosting, compatibility launch behavior, panel hit isolation, collapse/close lifecycle, semantic metadata, all four presets, contextual edges, advanced erasure, invalid-operation feedback, Undo grouping, and session teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** the existing layered sample is painted through the embedded front end and the release build is validated
- **THEN** the runtime terrain composition and player flow match the accepted baseline with no editor-only painter code in the player UI
