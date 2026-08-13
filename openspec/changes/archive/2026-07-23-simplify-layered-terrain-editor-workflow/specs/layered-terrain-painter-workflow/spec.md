## ADDED Requirements

### Requirement: Discoverable terrain painter entry and target selection
The editor SHALL provide one map-tool entry that opens the layered terrain painter, SHALL select the currently selected valid layered map or the sole valid layered map in the open scene, and MUST require an explicit target choice when multiple unselected candidates make automatic selection ambiguous.

#### Scenario: Selected layered map opens in the painter
- **WHEN** an author opens the terrain painter while a valid layered map is selected
- **THEN** the painter targets that map and displays its configuration status without requiring Project-window asset selection

#### Scenario: Several unselected maps are open
- **WHEN** the painter opens with multiple valid candidates and none is selected
- **THEN** painting remains inactive until the author explicitly chooses a target

### Requirement: Semantic material presentation
The painter SHALL obtain material display names and a thumbnail or swatch from authoring configuration and MUST NOT expose internal `A/B`, TileSet, mask, logical-output, or foreground/background vocabulary in its successful primary workflow.

#### Scenario: Grass and soil profile is configured
- **WHEN** the accepted two-material profile is displayed
- **THEN** the primary controls identify grass and soil visually and textually without requiring the author to inspect the underlying assets

#### Scenario: Required presentation metadata is missing
- **WHEN** a material lacks a usable author-facing name or preview
- **THEN** the painter reports a configuration warning and directs a developer to the collapsed configuration surface instead of presenting the setup as author-ready

### Requirement: Four explicit composition presets
The primary painter SHALL expose pure A, pure B, A-on-B, and B-on-A as four named visual presets and SHALL map each preset to the existing validated base or ordered-pair operation without maintaining a second authoring state.

#### Scenario: Pure material preset is painted
- **WHEN** the author paints the configured pure-grass preset over a composed cell
- **THEN** the cell contains the grass base and its landform and refined edge are cleared

#### Scenario: Grass-on-soil preset is painted
- **WHEN** the author paints the grass-on-soil preset
- **THEN** the gesture writes soil as the base and grass as the landform for every affected cell

#### Scenario: Pair direction is reversed
- **WHEN** the author selects soil-on-grass instead of grass-on-soil
- **THEN** the painter invokes the distinct reverse pair and never reuses the forward pair's edge binding

### Requirement: Contextual optional edge refinement
The painter SHALL hide edge controls for pure-base presets, SHALL offer base edge and AI-refined edge for landform-bearing tools, and MUST disable unavailable directed refinements with an actionable reason and no silent substitution.

#### Scenario: Pure base is active
- **WHEN** the active preset contains no landform
- **THEN** no edge-style decision is shown or written

#### Scenario: Exact AI-refined edge is available
- **WHEN** a pair preset has the exact registered directed edge and AI-refined edge is selected
- **THEN** painting selects that pre-authored edge asset without invoking image generation or pixel processing

#### Scenario: Only reverse refinement exists
- **WHEN** the requested pair lacks its exact edge but the reverse pair is registered
- **THEN** AI-refined edge is disabled with the missing-direction reason while base edge remains selectable

### Requirement: Explicit advanced layer and erasure operations
The painter SHALL keep landform-only painting, landform erasure, whole-cell clearing, and raw developer configuration outside the primary preset surface, and SHALL make the destructive scope of each erase operation explicit.

#### Scenario: Landform is erased
- **WHEN** the author selects erase-landform and paints a composed cell
- **THEN** the landform and edge are removed while the existing base remains

#### Scenario: Whole cell is cleared
- **WHEN** the author selects clear-cell and paints a composed cell
- **THEN** the base, landform, and edge are all removed

#### Scenario: Landform-only painting reaches an empty cell
- **WHEN** the author tries to paint only a landform where no base exists
- **THEN** the cell remains unchanged and the painter instructs the author to paint a base first

### Requirement: Direct Scene painting with bounded lifecycle and Undo
The painter SHALL show the active tool in the painter and Scene view, SHALL support left-drag painting, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input when painting stops, the target becomes invalid, play mode begins, or the window closes.

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags across multiple cells
- **THEN** each newly entered cell is painted once and one Undo command restores the pre-gesture state of all affected cells

#### Scenario: Painter window closes during an active session
- **WHEN** the painter window closes or loses its valid target
- **THEN** Scene input is released and subsequent clicks use normal Unity Scene behavior without changing terrain

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage and editor visual evidence for the simplified workflow, and the ordinary release WebGL build MUST preserve the existing layered terrain output, gameplay behavior, and `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** the focused terrain painter validation is executed
- **THEN** target selection, semantic metadata, all four presets, contextual edges, advanced erasure, invalid-operation feedback, Undo grouping, and session teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** the existing layered sample is painted through the new front end and the release build is validated
- **THEN** the runtime terrain composition and player flow match the accepted baseline with no editor-only painter code in the player UI

