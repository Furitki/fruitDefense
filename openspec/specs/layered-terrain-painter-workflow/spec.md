# layered-terrain-painter-workflow Specification

## Purpose
Define the simplified Unity editor workflow for selecting, painting, validating, and undoing layered terrain composition.
## Requirements
### Requirement: Discoverable terrain painter entry and target selection
The editor SHALL provide one map-tool entry that activates a native terrain-resource acceptance Overlay in the active Scene view, SHALL select the currently selected valid layered target or the sole valid layered target in the open scene, and MUST require an explicit target choice when multiple unselected candidates make automatic selection ambiguous.

#### Scenario: Selected layered map opens in the painter
- **WHEN** an author opens terrain-resource acceptance while a valid layered target is selected
- **THEN** the active Scene view focuses, the native Overlay is displayed against that target, and no standalone painter window opens

#### Scenario: Several unselected maps are open
- **WHEN** the Overlay opens with multiple valid candidates and none is selected
- **THEN** painting remains inactive until the author explicitly chooses a target

### Requirement: Semantic material presentation
The Overlay SHALL obtain material display names and a thumbnail or swatch from authoring configuration, SHALL show the target's configured contour as read-only `方形` or `自然` context, SHALL identify itself as resource acceptance that does not create playable maps, and MUST NOT expose internal TileSet, mask, logical-output, or foreground/background vocabulary in its successful primary workflow.

#### Scenario: Grass and soil profile is configured
- **WHEN** the accepted square two-material target is selected
- **THEN** the primary controls identify grass and soil visually and textually, show `当前轮廓：方形`, and direct playable-map work to the canonical map editor

#### Scenario: Required presentation metadata is missing
- **WHEN** a material lacks a usable author-facing name or preview
- **THEN** the Overlay reports a configuration warning and directs a developer to the Inspector configuration instead of presenting the setup as author-ready

### Requirement: Contextual optional edge refinement
The painter SHALL expose no separate pure-base preset cards, SHALL let the selected ordinary terrain brush opt into a contextual `只绘制纯图` mode, SHALL resolve that pure visual from the selected brush's configured opaque endpoint rather than an unrelated global thumbnail, SHALL use the target's preconfigured contour without exposing a square/organic switch in the ordinary laboratory, SHALL let one contour-specific edge resource serve both pair directions through complemented reverse masks, and MUST disable unavailable combinations with an actionable reason and no cross-contour substitution.

#### Scenario: Pure-only mode is active
- **WHEN** an author selects an ordinary material or pair brush and enables `只绘制纯图`
- **THEN** that brush writes its own configured opaque foreground endpoint, clears landform and edge state in the touched cell, and does not substitute an unrelated material thumbnail or reverse-pair asset

#### Scenario: Full-composite trial brush enters pure-only mode
- **WHEN** a reviewed full-composite sixteen-mask brush provides opaque background at mask `0` and opaque foreground at mask `15`
- **THEN** the trial binding uses those exact endpoints for pure background and foreground previews and writes, so toggling pure-only mode does not switch to another texture family

#### Scenario: Primary brush chooser is shown
- **WHEN** the embedded terrain laboratory displays its ordinary brushes
- **THEN** it shows only A-on-B and B-on-A as square preview cards in one row, exposes pure output through the contextual option, and shows no duplicate landform-only or pure-material brush card

#### Scenario: Ordinary laboratory uses the configured contour
- **WHEN** the embedded terrain laboratory is shown for a target configured as square or organic
- **THEN** it keeps that target's configured contour and exposes no `方形` / `自然` option switch

#### Scenario: One square painted edge serves both directions
- **WHEN** either ordered direction of a material pair has the square painted edge and the painted edge is selected
- **THEN** both pair brushes use that pre-authored edge asset, with the reverse direction selecting the complemented mask and no image generation or source-pixel processing

#### Scenario: Registered production brushes are shown
- **WHEN** the canonical map editor or terrain laboratory resolves valid imported brush definitions
- **THEN** both authoring surfaces enumerate the same registry in stable order and select the exact semantic base, landform, contour, and edge combination without pair-specific hard-coded buttons

#### Scenario: Registered brush library is previewed
- **WHEN** the terrain laboratory contains two or more valid registered brush definitions
- **THEN** one scrollable preview gallery shows every definition at the same time as a real assembled composition card in stable registry order, labels its material pair and available direction count, and does not require applying one definition before the others can be discovered

#### Scenario: Registered brush card preserves artwork proportions
- **WHEN** a registered composition is drawn above its card footer
- **THEN** the assembled preview occupies a centered square rect and every mask sprite is uniformly scaled without horizontal or vertical stretching

#### Scenario: Production pair dependency is missing
- **WHEN** a shortcut lacks its base texture, foreground square landform, or exact directed refined edge
- **THEN** that shortcut is disabled and does not substitute another material pair or contour

#### Scenario: Laboratory selects a registered brush
- **WHEN** the author selects a registered brush on an empty terrain-laboratory target
- **THEN** the target uses the brush foreground as material A, background as material B, its registered endpoint bases, reusable foreground landform, and exact pair edge, then exposes only directions whose landform dependency exists

#### Scenario: Laboratory target already contains authored cells
- **WHEN** changing brush would reinterpret existing generic A/B marker cells
- **THEN** the laboratory keeps the complete registered preview gallery visible, refuses the switch with an actionable clear-canvas instruction, and preserves the existing cells

#### Scenario: Only organic refinement exists
- **WHEN** the requested square pair lacks its exact edge but the organic pair is registered
- **THEN** the painted edge is disabled with the missing-contour reason while the square base edge remains selectable

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
The terrain-resource acceptance Overlay SHALL show the active material, configured contour, and edge treatment in the Overlay and Scene view, SHALL support left-drag painting, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input when painting stops, the target becomes invalid, play mode begins, or the Overlay is hidden, closed, or destroyed.

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags a square-contour preset across multiple cells
- **THEN** each newly entered cell is painted once, connected-style constraints remain valid, and one Undo command restores the pre-gesture state

#### Scenario: Resource-acceptance Overlay closes during an active session
- **WHEN** the Overlay is hidden or closed, or loses its valid target
- **THEN** Scene input is released and subsequent clicks use normal Unity Scene behavior without changing terrain

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage for the native Overlay host, unified registered directional gallery, one-click painting activation, preserved original terrain registration, reciprocal resource projection, configured-contour context, Undo, and teardown, and runtime validation MUST preserve gameplay behavior and the `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** focused terrain resource-acceptance validation executes
- **THEN** native Overlay activation, target selection, two choices per registered resource, direct direction activation, original square registration, reverse fallback mapping, Undo grouping, and teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** aggregate validation compiles the player and exercises the accepted flow
- **THEN** no editor-only Overlay code enters the player UI and terrain/runtime behavior remains unchanged

### Requirement: Unified registered directional brush gallery
The terrain-resource acceptance Overlay SHALL use one registered directional brush gallery as its complete ordinary paint selector, SHALL expand each registered resource into exactly two reciprocal paint tiles in stable resource-and-direction order, SHALL arrange compact tiles in four columns per row, SHALL label and preview the exact direction of each tile, and MUST NOT show a separate resource selector followed by generic A-on-B / B-on-A cards.

#### Scenario: Registered resources are displayed
- **WHEN** the registry contains three valid terrain resources
- **THEN** the Overlay displays six directly paintable directional tiles across one full four-tile row and one partial row, with no duplicate ordinary direction selector

#### Scenario: Directional tile is selected
- **WHEN** an author selects either direction of a registered resource on the laboratory target
- **THEN** that click configures the resource when needed, selects the exact direction, exits pure-only mode, and activates Scene painting without another brush or start selection

#### Scenario: Direction changes within the active resource
- **WHEN** a target already contains cells authored with one registered resource and the author selects that resource's reciprocal tile
- **THEN** the direction changes without clearing or reinterpreting the existing cells

#### Scenario: Another resource is selected on a non-empty generic canvas
- **WHEN** the author selects a different registered resource after A/B marker cells already exist
- **THEN** the target switches immediately, preserves the logical marker cells, reprojects them through the selected resource, and activates the chosen direction without requiring a clear action

#### Scenario: Scene painting takes focus
- **WHEN** a selected brush activates Scene painting or the Scene view regains focus
- **THEN** the laboratory Overlay remains displayed in expanded panel form instead of collapsing or minimizing itself

#### Scenario: Recoverable authored content is inconsistent
- **WHEN** the target is structurally paintable but strict whole-canvas validation reports a correctable partial edge treatment
- **THEN** the target remains selectable and its directional brush can rebuild or repaint the existing logical cells while strict validation continues to report the inconsistency until repaired

### Requirement: Original terrain remains a registered paint choice
The original square grass/soil terrain shown by the initial laboratory target SHALL be represented by a normal laboratory brush definition, SHALL retain its existing endpoint textures, landform TileSets, refined edge TileSet, asset GUIDs, and native size, and SHALL contribute the same two reciprocal direct-paint tiles as later imported resources.

#### Scenario: Laboratory opens with all project brushes
- **WHEN** the original square resource and newer composite resources are installed
- **THEN** the gallery includes both original square grass-on-soil directions alongside both directions of every newer registered resource

#### Scenario: Original terrain is selected after another resource
- **WHEN** the author selects an original square directional tile after using another resource
- **THEN** the target is configured from the preserved original assets and painting uses the selected direction without a second choice

