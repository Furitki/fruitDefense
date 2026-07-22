## MODIFIED Requirements

### Requirement: Generic manual terrain wizard
The editor SHALL provide a pixel terrain wizard that creates or updates a uniquely named profile, accepts either one opaque manual source texture or separate opaque grass and soil source textures, and exposes optional outline width and bounded texture-guidance width for native-pixel transitions.

#### Scenario: One manual image is supplied
- **WHEN** an author selects single-source manual mode, assigns one opaque texture, chooses valid native-grid transition settings, and bakes
- **THEN** the same texture is used as the grass and effective soil source and one complete sixteen-mask TileSet is generated with the selected outline and guidance behavior

#### Scenario: Two manual images are supplied
- **WHEN** an author selects grass-and-soil manual mode, assigns two opaque textures, and chooses valid native-grid transition settings
- **THEN** the grass surface and soil rim use their respective source palettes without copying or overwriting either source file

#### Scenario: Connection-safe edge is selected
- **WHEN** an author chooses zero outline width and bakes a valid terrain
- **THEN** the wizard accepts the profile and the generated transparent boundary contains no baker-synthesized solid outline color

### Requirement: Wizard validation and selected-map application
The wizard SHALL validate a selected generated profile, including its bounded texture guidance and optional-outline behavior, and SHALL be able to apply its TileSet to the selected configured `DualGridTilemap` with Undo support, alignment, and rebuild.

#### Scenario: Generated profile is validated
- **WHEN** an author clicks Validate after a successful bake
- **THEN** source opacity, mask pixels, seams, topology, contour guidance, active palette, outline behavior, importer settings, evidence hashes, and all sixteen TileSet assignments pass for that profile

#### Scenario: TileSet is applied to a selected map
- **WHEN** a configured `DualGridTilemap` is selected and the author applies a baked profile
- **THEN** the component references that profile's TileSet, aligns its generated sibling, rebuilds output, and leaves the logical authoring cells unchanged
