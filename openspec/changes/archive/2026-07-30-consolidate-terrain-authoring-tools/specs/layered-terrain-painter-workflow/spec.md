## ADDED Requirements

### Requirement: Two directed composition brushes with contextual pure output
The resource-acceptance Overlay SHALL preserve exactly two ordinary composition brush cards, A-on-B and B-on-A using author-facing material names, SHALL keep `只绘制纯图` as a contextual checkbox for the selected brush, and MUST NOT add a second preset system or expose raw A/B vocabulary in the successful user-facing labels.

#### Scenario: Ordinary brush chooser is shown
- **WHEN** a configured terrain acceptance target is selected
- **THEN** the Overlay shows the two directed composition cards in one row and the existing contextual pure-only checkbox

#### Scenario: Pure-only mode is enabled
- **WHEN** the author checks `只绘制纯图` for a directed composition brush
- **THEN** painting writes that brush's configured opaque foreground endpoint and clears landform and edge state without presenting another brush card

#### Scenario: One edge resource serves both brush directions
- **WHEN** A-on-B has the configured contour-specific edge resource including its renderable mask-00 endpoint and B-on-A has no exact override
- **THEN** both cards remain available, A-on-B uses the authored mask, and B-on-A uses the same resource with the complemented mask

#### Scenario: Shared reverse brush paints a full interior
- **WHEN** the B-on-A source mask is full and complementation resolves it to mask 00
- **THEN** the renderer uses the shared resource's mask-00 tile so the B center remains filled

#### Scenario: Shared reverse brush has no source occupancy
- **WHEN** the B-on-A source mask is empty before complementation
- **THEN** the renderer emits no edge tile instead of turning that unrelated empty vertex into mask 15

#### Scenario: Legacy exact reverse asset exists
- **WHEN** B-on-A already has an exact configured directed edge asset
- **THEN** that exact resource remains the compatibility override and is not silently replaced by the shared fallback

## MODIFIED Requirements

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
The Overlay SHALL use the target's preconfigured contour without exposing a contour switch, SHALL resolve an exact configured edge first and otherwise reuse the opposite ordered edge from the same contour and style with a complemented mask, SHALL retain contextual pure-only output for bypassing landform composition, and MUST disable a pair only when neither exact nor shared same-contour resource is available.

#### Scenario: Pure base is active
- **WHEN** the contextual pure-only checkbox is enabled for a directed composition brush
- **THEN** painting writes the selected opaque endpoint and no edge style is written

#### Scenario: Exact AI-refined edge is available
- **WHEN** a pair card has an exact edge or a same-contour reverse binding that can be complemented
- **THEN** painting selects that pre-authored TileSet without invoking image generation or changing source assets

#### Scenario: Only reverse refinement exists
- **WHEN** the requested pair lacks an exact edge but the opposite order has the same contour and edge style with a renderable mask-00 endpoint
- **THEN** the pair remains available through the shared TileSet and complemented mask

#### Scenario: Only another contour exists
- **WHEN** neither direction of the requested material pair has the configured contour and edge style
- **THEN** that pair card is unavailable while contextual pure-only output remains available for its opaque endpoint

### Requirement: Direct Scene painting with bounded lifecycle and Undo
The resource-acceptance Overlay SHALL support standard Scene Overlay docking, floating, collapsing, and closing, SHALL show the active brush and configured contour, SHALL support left-drag painting, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input when painting stops, the target becomes invalid, play mode begins, or the Overlay is hidden, closed, or destroyed.

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags across multiple Scene cells
- **THEN** each newly entered cell is painted once and one Undo command restores the pre-gesture state of all affected cells

#### Scenario: Painter window closes during an active session
- **WHEN** the resource-acceptance Overlay is hidden, closed, or destroyed while painting is active
- **THEN** Scene input is released and subsequent clicks use normal Unity Scene behavior without changing terrain

#### Scenario: Author interacts with Overlay controls
- **WHEN** the author clicks, drags, or scrolls inside the native Overlay
- **THEN** those events operate the Overlay and do not paint the Scene cell beneath it

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage for the native Overlay host, preserved brush cards and pure-only checkbox, configured-contour context, exact-override/shared-fallback behavior, complemented-mask rendering, Undo, and teardown, and runtime validation MUST preserve gameplay behavior and the `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** focused terrain resource-acceptance validation executes
- **THEN** native Overlay activation, target selection, two-card geometry, pure-only behavior, contour context, exact-direction validation, Undo grouping, and teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** aggregate validation compiles the player and exercises the accepted flow
- **THEN** no editor-only Overlay code enters the player UI and terrain/runtime behavior remains unchanged

## REMOVED Requirements

### Requirement: Four explicit composition presets
**Reason**: The accepted ordinary interaction already uses two directed composition cards with a contextual pure-only checkbox, so the old four-card contract is obsolete and conflicts with the scoped consolidation.

**Migration**: Use `Two directed composition brushes with contextual pure output`; pure A and pure B are selected by checking `只绘制纯图` on the corresponding directed brush.
