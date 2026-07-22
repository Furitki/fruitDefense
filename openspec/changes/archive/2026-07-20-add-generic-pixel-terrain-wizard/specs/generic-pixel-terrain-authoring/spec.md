## ADDED Requirements

### Requirement: Generic manual terrain wizard
The editor SHALL provide a pixel terrain wizard that creates or updates a uniquely named profile and accepts either one opaque manual source texture or separate opaque grass and soil source textures.

#### Scenario: One manual image is supplied
- **WHEN** an author selects single-source manual mode, assigns one opaque texture, chooses valid native-grid settings, and bakes
- **THEN** the same texture is used as the grass and effective soil source and one complete sixteen-mask TileSet is generated

#### Scenario: Two manual images are supplied
- **WHEN** an author selects grass-and-soil manual mode and assigns two opaque textures
- **THEN** the grass surface and soil rim use their respective source palettes without copying or overwriting either source file

### Requirement: Imagegen-only AI source handoff
The wizard SHALL support AI-assisted source creation by writing a machine-readable imagegen request with exact target paths and a copyable Codex instruction, and editor code SHALL NOT draw, synthesize, download, or substitute source bitmap art.

#### Scenario: AI source request is prepared
- **WHEN** an author selects AI mode and provides valid prompts and a terrain id
- **THEN** the wizard writes a request that names the `imagegen` skill, forbids script drawing, records one or two prompts, and identifies each required PNG asset path

#### Scenario: Requested AI files are absent
- **WHEN** an author attempts to bake before imagegen has produced every requested source PNG
- **THEN** baking remains unavailable and the missing target paths are shown without creating fallback images

#### Scenario: Imagegen outputs arrive
- **WHEN** imagegen-produced files are saved at all requested paths and the author refreshes the wizard
- **THEN** Unity imports those files, assigns them to the profile, and enables the same deterministic baker used by manual mode

### Requirement: Independent terrain ownership and evidence
Each wizard-created terrain SHALL own a distinct profile, generated folder, TileSet, native atlas, and JSON validation report derived from its terrain id, while preserving the existing PixelGrass paths.

#### Scenario: Two terrain profiles are baked
- **WHEN** two profiles use different terrain ids and output roots
- **THEN** their sixteen PNGs, Tile assets, TileSets, atlases, and reports coexist without overwriting one another

#### Scenario: PixelGrass is regenerated
- **WHEN** the existing PixelGrass profile is baked after generalization
- **THEN** its existing source, profile, output, atlas, and JSON paths remain compatible and its source PNGs are not overwritten

### Requirement: Wizard validation and selected-map application
The wizard SHALL validate a selected generated profile and SHALL be able to apply its TileSet to the selected configured `DualGridTilemap` with Undo support, alignment, and rebuild.

#### Scenario: Generated profile is validated
- **WHEN** an author clicks Validate after a successful bake
- **THEN** source opacity, mask pixels, seams, topology, importer settings, evidence hashes, and all sixteen TileSet assignments pass for that profile

#### Scenario: TileSet is applied to a selected map
- **WHEN** a configured `DualGridTilemap` is selected and the author applies a baked profile
- **THEN** the component references that profile's TileSet, aligns its generated sibling, rebuilds output, and leaves the logical authoring cells unchanged

### Requirement: Aggregate multi-profile smoke validation
The required project editor smoke SHALL discover and validate every pixel terrain profile rather than only the PixelGrass sample, without changing release scenes or runtime flow.

#### Scenario: Project smoke runs with multiple profiles
- **WHEN** multiple valid baked pixel terrain profiles exist and `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** each profile's derived assets and independent evidence pass before the aggregate smoke success marker is emitted

#### Scenario: An unfinished profile exists
- **WHEN** a discovered pixel terrain profile is invalid or has missing generated assets
- **THEN** aggregate smoke fails with the offending profile asset path identified
