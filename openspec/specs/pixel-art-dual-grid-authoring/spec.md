# pixel-art-dual-grid-authoring Specification

## Purpose
TBD - created by archiving change add-pixel-art-dual-grid-pipeline. Update Purpose after archive.
## Requirements
### Requirement: Native-grid pixel terrain profile
The project SHALL provide an editor-only pixel terrain profile with opaque grass and soil pixel textures, an even native tile size, optional integer outline width, integer soil-rim and texture-guidance widths, an opaque edge color, deterministic seed, terrain id, and owned output folder, and SHALL reject profiles whose active edge bands cannot preserve an interior pixel region.

#### Scenario: Valid pixel profile is selected
- **WHEN** an author configures opaque pixel sources, an even tile size of at least eight pixels, zero outline width, and soil-rim and texture-guidance widths within their supported bounds
- **THEN** profile validation succeeds without requiring supersampling, antialiasing, or a solid outer edge color in generated pixels

#### Scenario: Valid outlined pixel profile is selected
- **WHEN** an author configures a positive outline width and active edge bands that leave an interior pixel region
- **THEN** profile validation succeeds and retains explicit solid-outline composition

#### Scenario: Invalid pixel dimensions are selected
- **WHEN** the tile size is odd, texture guidance exceeds its supported bound, or the active outline and soil rim consume the available interior
- **THEN** profile validation fails with the offending pixel constraint identified

### Requirement: Deterministic discrete sixteen-mask generation
The pixel baker SHALL generate all sixteen Dual-Grid masks directly on the final integer pixel grid from a normalized corner field with bounded deterministic source-texture guidance, SHALL use no antialiasing or interpolated texture sampling, and SHALL produce identical output for repeated bakes with the same profile and sources.

#### Scenario: Textured pixel profile is baked
- **WHEN** a non-flat foreground source is baked with positive texture guidance
- **THEN** the generated transition pixels differ from the unguided corner field within the configured displacement bound and do not use fixed quarter-circle silhouettes

#### Scenario: Flat or unguided pixel profile is baked
- **WHEN** the foreground source has no usable luminance range or texture guidance is zero
- **THEN** the baker deterministically falls back to the non-circular unguided corner field

#### Scenario: Pixel profile is baked repeatedly
- **WHEN** the same profile and source files are baked more than once
- **THEN** every output mask has the configured dimensions and the combined pixel hash remains unchanged

#### Scenario: Full and empty masks are baked
- **WHEN** masks `0` and `15` are generated
- **THEN** mask `0` is fully transparent and mask `15` is fully opaque grass terrain

### Requirement: Stable side-socket continuity
Each generated tile side SHALL be determined only by the two corner bits that terminate that side, and compatible horizontal and vertical mask pairs SHALL have pixel-identical RGBA border sequences.

#### Scenario: Compatible horizontal masks are compared
- **WHEN** a left mask's north-east and south-east bits equal a right mask's north-west and south-west bits
- **THEN** the left mask's right border exactly equals the right mask's left border

#### Scenario: Compatible vertical masks are compared
- **WHEN** an upper mask's south-west and south-east bits equal a lower mask's north-west and north-east bits
- **THEN** the upper mask's bottom border exactly equals the lower mask's top border

### Requirement: Pixel-safe topology and palette
Generated masks SHALL use binary alpha, SHALL contain only colors from the opaque source palettes plus the configured edge color when a positive outline is active, SHALL emit no synthesized edge color for a zero-width outline, and SHALL keep opposite-corner masks `5` and `10` disconnected through the tile center.

#### Scenario: Output pixels are inspected
- **WHEN** every generated RGBA pixel for a zero-outline profile is enumerated
- **THEN** alpha is either `0` or `255` and every opaque color belongs to an effective source palette

#### Scenario: Outlined output pixels are inspected
- **WHEN** every generated RGBA pixel for a positive-outline profile is enumerated
- **THEN** alpha is either `0` or `255` and every opaque color belongs to an effective source palette or equals the configured edge color

#### Scenario: Opposite corners are inspected
- **WHEN** masks `5` and `10` are generated at an even tile size
- **THEN** their central two-by-two pixel block is transparent and each mask contains exactly two disconnected occupied components

### Requirement: Pixel-safe Unity assets
The baker SHALL import each generated PNG as a single Full Rect Sprite with Point filtering, disabled mipmaps, Clamp wrapping, uncompressed texture data, and pixels per unit equal to the native tile size, and SHALL assign all sixteen generated Tile assets to a reusable `DualGridTileSet`.

#### Scenario: Generated importers are inspected
- **WHEN** a generated mask texture is imported
- **THEN** its importer preserves exact native pixels without filtering, mipmaps, compression, tight-mesh trimming, or unit rescaling

#### Scenario: Generated TileSet is inspected
- **WHEN** the bake completes
- **THEN** every mask slot from `0` through `15` references the corresponding generated Tile asset

### Requirement: Reproducible sample and acceptance evidence
The project SHALL provide imagegen-produced sample pixel source textures, idempotently maintained sample profiles, native-resolution sixteen-mask atlases, machine-readable validation evidence including contour-guidance and outline behavior, and a public validator reachable from the required Dual-Grid/project editor smoke entry, and the editor baker SHALL NOT synthesize replacement source art.

#### Scenario: Default sample is generated repeatedly
- **WHEN** the PixelGrass and StoneFloor sample profiles are baked more than once
- **THEN** the baker reuses their imagegen source/profile paths, refreshes only owned derived outputs, never overwrites source art, and reports successful deterministic texture-guided validation

#### Scenario: Sample source art is missing
- **WHEN** an imagegen-produced grass or soil source file is absent
- **THEN** the editor command fails with the missing authoring input identified instead of drawing fallback art

#### Scenario: Project smoke validation runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes after sample generation
- **THEN** pixel seams, alpha, active palette, opposite-corner topology, bounded contour guidance, outline behavior, determinism, importer settings, and TileSet assignments are validated without changing release scene order

