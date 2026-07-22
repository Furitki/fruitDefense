## ADDED Requirements

### Requirement: Deterministic four-corner mask resolution
The system SHALL resolve each visual Dual-Grid cell from the four logical cells around the corresponding grid vertex using north-west `1`, north-east `2`, south-east `4`, and south-west `8` bits.

#### Scenario: All four logical cells are occupied
- **WHEN** all four logical cells surrounding a visual vertex contain tiles
- **THEN** the resolved visual mask is `15`

#### Scenario: One logical corner is occupied
- **WHEN** only the logical cell north-west of a visual vertex contains a tile
- **THEN** the resolved visual mask is `1`

#### Scenario: Every mask is addressable
- **WHEN** the sixteen possible occupied/empty corner combinations are evaluated
- **THEN** each combination resolves to one unique mask from `0` through `15`

### Requirement: Explicit Dual-Grid tile-set configuration
The system SHALL expose a serializable tile-set asset with one stable lookup slot for each mask from `0` through `15`, SHALL require configured visual tiles for masks `1` through `15`, and SHALL allow mask `0` to remain transparent.

#### Scenario: Valid overlay tile set
- **WHEN** masks `1` through `15` contain `TileBase` assets and mask `0` is empty
- **THEN** tile-set validation succeeds and empty visual cells remain clear

#### Scenario: Required mask is missing
- **WHEN** any mask from `1` through `15` has no configured tile
- **THEN** validation fails with the missing numeric mask identified

### Requirement: Logical Tilemap generates an aligned visual Tilemap
The system SHALL generate a visual Tilemap from occupied cells in a distinct logical source Tilemap, SHALL place visual tile centers on logical-grid vertices through a negative half-cell alignment, and SHALL treat the generated output as component-owned data.

#### Scenario: Bounded source pattern is rebuilt
- **WHEN** a logical source pattern is fully rebuilt
- **THEN** the output covers the source vertex bounds, uses the configured tile for every non-zero resolved mask, and contains no stale generated tiles

#### Scenario: Source and output are the same Tilemap
- **WHEN** a component is configured with the same Tilemap as source and output
- **THEN** configuration validation fails without clearing or overwriting source data

#### Scenario: Empty source is rebuilt
- **WHEN** the logical source contains no tiles and a full rebuild runs
- **THEN** the generated output contains no tiles

### Requirement: Editor and runtime refresh
The system SHALL refresh generated visuals after logical painting in edit mode and SHALL expose deterministic full-rebuild, local-cell refresh, and logical-cell mutation APIs for runtime callers.

#### Scenario: One logical cell changes
- **WHEN** a runtime caller adds or removes one logical tile through the component API
- **THEN** the four visual vertices touching that logical cell are refreshed and unrelated generated cells remain unchanged

#### Scenario: Tile Palette painting changes the source
- **WHEN** an author changes the logical Tilemap in edit mode while automatic refresh is enabled
- **THEN** the component detects the changed source signature and rebuilds the visual Tilemap

#### Scenario: Automatic refresh is disabled
- **WHEN** the logical Tilemap changes while automatic refresh is disabled
- **THEN** no automatic output mutation occurs and an explicit rebuild still produces the current result

### Requirement: Reusable binary terrain layers
The system SHALL support ground, road, wall, and other binary terrain categories by allowing independent logical/output Tilemap pairs to use different tile-set assets without changing the mask algorithm.

#### Scenario: Two terrain layers share a Grid
- **WHEN** road and wall components use distinct source/output pairs under the same Grid
- **THEN** each output is generated only from its own logical source and configured tile set

### Requirement: Developer demo and acceptance validation
The project SHALL provide an idempotently generated Dual-Grid demo scene with procedural placeholder art and SHALL validate mask resolution, configuration, alignment, full rebuild, and local refresh from the required editor smoke entry.

#### Scenario: Demo setup is run repeatedly
- **WHEN** the demo setup command is run more than once
- **THEN** it updates the same demo assets and scene without creating duplicate assets or adding the demo to release build settings

#### Scenario: Project smoke validation runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** all sixteen masks, tile-set validation, source/output safety, half-cell alignment, rebuild behavior, and incremental updates are verified

#### Scenario: Release flow is inspected
- **WHEN** release scene order is checked after the demo is created
- **THEN** it remains `Bootstrap → Lobby → Battle → Settlement` and excludes `DualGridDemo`

### Requirement: Reproducible high-quality terrain-art baking
The project SHALL provide an editor-only terrain bake profile that records source grass and soil textures, output resolution, supersampling scale, edge-antialias width, exposed-soil width, deterministic seed, irregularity, and grass-tuft settings, and SHALL regenerate all sixteen mask sprites and their Tile assets without hand-editing generated output.

#### Scenario: Profile is baked repeatedly
- **WHEN** an author bakes the same terrain profile more than once without changing its inputs or parameters
- **THEN** the generated mask topology, edge pixels, Tile assets, and TileSet slot assignments remain deterministic

#### Scenario: Terrain edge is rasterized
- **WHEN** a non-full mask is baked
- **THEN** its silhouette is derived from a pixel-distance field, rasterized with at least four-times supersampling, and retains a narrow antialiased outer edge independently from the wider soil-to-grass material transition

#### Scenario: Opposite corners are baked
- **WHEN** mask `5` or mask `10` is baked
- **THEN** the two occupied corner regions remain visually disconnected at the tile center

#### Scenario: Compatible mask edges are compared
- **WHEN** every horizontally and vertically compatible mask pair is inspected
- **THEN** their shared alpha edges are pixel-identical and the bake emits machine-readable evidence

### Requirement: Layered terrain demo and manual art acceptance
The developer demo SHALL present generated grass as a transparent Dual-Grid overlay above a corresponding soil base and SHALL provide a Scene-view paint mode that updates generated visuals while the author drags.

#### Scenario: Demo terrain is rendered
- **WHEN** the connected terrain sample is viewed
- **THEN** transparent space beneath and around the grass edge reveals the soil base instead of the camera clear color

#### Scenario: Author manually paints the demo
- **WHEN** manual paint mode is enabled and the author left-drags or Shift-left-drags over the logical grid
- **THEN** logical grass cells are respectively added or removed and the affected generated vertices refresh immediately
