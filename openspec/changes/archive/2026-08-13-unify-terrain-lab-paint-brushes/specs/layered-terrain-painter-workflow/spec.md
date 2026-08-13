## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage for the native Overlay host, unified registered directional gallery, one-click painting activation, preserved original terrain registration, reciprocal resource projection, configured-contour context, Undo, and teardown, and runtime validation MUST preserve gameplay behavior and the `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** focused terrain resource-acceptance validation executes
- **THEN** native Overlay activation, target selection, two choices per registered resource, direct direction activation, original square registration, reverse fallback mapping, Undo grouping, and teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** aggregate validation compiles the player and exercises the accepted flow
- **THEN** no editor-only Overlay code enters the player UI and terrain/runtime behavior remains unchanged

## REMOVED Requirements

### Requirement: Two directed composition brushes with contextual pure output
**Reason**: Its target-relative A-on-B / B-on-A cards form the second selector that this change replaces, and the primary pure-only checkbox adds another ordinary mode outside the registered brush source of truth.

**Migration**: Use the two directional tiles emitted for each `TerrainBrushDefinition`; pure-base replacement remains available through explicit advanced authoring operations rather than the ordinary laboratory gallery.
