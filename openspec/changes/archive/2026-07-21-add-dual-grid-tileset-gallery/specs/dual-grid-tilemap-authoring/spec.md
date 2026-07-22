## MODIFIED Requirements

### Requirement: Explicit Dual-Grid tile-set configuration
The system SHALL expose a serializable tile-set asset with one stable lookup slot for each mask from `0` through `15`, SHALL require configured visual tiles for masks `1` through `15`, SHALL allow mask `0` to remain transparent, and SHALL provide a cached Inspector gallery that discovers every project TileSet and previews valid whole-layer choices without replacing the explicit object field.

#### Scenario: Valid overlay tile set
- **WHEN** masks `1` through `15` contain `TileBase` assets and mask `0` is empty
- **THEN** tile-set validation succeeds and empty visual cells remain clear

#### Scenario: Required mask is missing
- **WHEN** any mask from `1` through `15` has no configured tile
- **THEN** validation fails with the missing numeric mask identified

#### Scenario: TileSet gallery is displayed
- **WHEN** an author inspects a `DualGridTilemap`
- **THEN** every project `DualGridTileSet` is listed once in deterministic asset-path order with a name, representative transition preview, validity state, and selected state

#### Scenario: Project TileSets change
- **WHEN** a TileSet is added, removed, renamed, moved, or regenerated while the Inspector is available
- **THEN** the cached gallery is invalidated and can refresh without a serialized registry or persistent preview asset

#### Scenario: Valid gallery card is selected
- **WHEN** an author clicks a valid non-selected TileSet card
- **THEN** the current component adopts that TileSet for the whole layer, generated visuals rebuild immediately, the scene becomes dirty, and the operation is undoable without changing manual paint mode

#### Scenario: Invalid gallery card is present
- **WHEN** discovery finds a TileSet missing a required mask
- **THEN** the card remains identifiable with its validation reason but cannot be selected through the gallery

### Requirement: Developer demo and acceptance validation
The project SHALL provide an idempotently generated Dual-Grid demo scene with procedural placeholder art and SHALL validate mask resolution, configuration, gallery discovery and selection, alignment, full rebuild, and local refresh from the required editor smoke entry.

#### Scenario: Demo setup is run repeatedly
- **WHEN** the demo setup command is run more than once
- **THEN** it updates the same demo assets and scene without creating duplicate assets or adding the demo to release build settings

#### Scenario: Project smoke validation runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** all sixteen masks, tile-set validation, deterministic gallery discovery, preview readiness, gallery assignment and rebuild, source/output safety, half-cell alignment, rebuild behavior, and incremental updates are verified

#### Scenario: Release flow is inspected
- **WHEN** release scene order is checked after the demo is created
- **THEN** it remains `Bootstrap → Lobby → Battle → Settlement` and excludes `DualGridDemo`
