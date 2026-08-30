## ADDED Requirements

### Requirement: Single deterministic sky-paper production treatment
The release SHALL contain one complete sky-paper-orchard treatment using the existing finite 56-slot semantic ArtSet contract, text-free owned masters, deterministic runtime exports, stable destination GUIDs, validated importer metadata, and one active theme/ArtSet revision. Production action surfaces SHALL use pixels selected from project-owned ImageGen output and SHALL NOT be procedurally painted by the exporter. The release SHALL NOT ship the supplied full-page reference, a selectable legacy treatment, inherited slots, placeholders, or runtime fallback art.

#### Scenario: Reset art is exported
- **WHEN** the reviewed UI masters are exported for release
- **THEN** every required semantic slot resolves exactly once, source/runtime hashes and optical metadata match the generated files, existing destination GUIDs remain stable, and no generated Chinese copy is present in a production raster

#### Scenario: Image-generated action material is exported twice
- **WHEN** the same reviewed ImageGen component-sheet bytes are processed twice without source changes
- **THEN** extraction and export produce byte-identical text-free action masters and runtime PNGs, and the tool performs only crop, exterior-alpha cleanup, transparent padding, resize, measurement, hashing, and export rather than procedural material drawing

#### Scenario: Procedural action fallback is inspected
- **WHEN** the production exporter and release dependency graph are audited
- **THEN** no procedural recipe can author or replace an action rim, face, outline, highlight, shadow, texture, or decoration when the reviewed ImageGen source is missing or invalid

#### Scenario: Release dependencies are inspected
- **WHEN** the release scenes and WebGL payload are audited
- **THEN** they depend on exactly one active theme and complete ArtSet revision and contain no legacy theme selector, fallback drawing path, full-page concept image, or unowned UI texture

## MODIFIED Requirements

### Requirement: Typography alignment and hierarchy
Runtime text SHALL use packaged static Chinese font assets referenced explicitly by semantic typography role, consistent baselines, and intentional horizontal/vertical alignment rather than a single global font, synthesized default-skin styling, ad-hoc offsets, or host-font behavior. Display, screen-title, section-title, and control-label roles SHALL use the approved rounded high-weight face; body, metric, and supplemental roles SHALL use the approved reading face unless the release theme explicitly assigns another packaged role face.

#### Scenario: Comparable labels and values are displayed
- **WHEN** metric, action, card, or status content appears in a repeated row or group
- **THEN** comparable labels, values, and icon centers align within the documented tolerance and preserve the intended primary/secondary reading order

#### Scenario: Typography roles are validated
- **WHEN** the release theme, font imports, and finite-copy catalog are inspected
- **THEN** every typography role resolves to a packaged licensed Chinese font with required glyph coverage, the font used for measurement is the font used for rendering, and no role falls back to the host or legacy single-font field

### Requirement: Reference-driven route hierarchy
Bootstrap, Lobby, Battle, and Settlement SHALL apply the approved sky-paper-orchard hierarchy through shared component anatomy, route-appropriate density, sky-blue edge background, warm paper surfaces, one soil-brown gameplay-stage anchor, leaf-green primary actions, sunlight-yellow phase/selection emphasis, rounded typography, and restrained fruit/leaf ornament while keeping player copy and controls primary.

#### Scenario: Reference layout is adapted to a compact runtime rectangle
- **WHEN** a reference component is composed inside an authoritative compact runtime rectangle
- **THEN** its hierarchy and orchard identity remain recognizable without baking text, stretching ornaments, hiding controls, copying concept-image geometry, or changing hit geometry implicitly

#### Scenario: Release flow uses one visual language
- **WHEN** Bootstrap, Lobby, Battle, and Settlement are captured from the same build
- **THEN** all routes resolve the same theme roles, typography bindings, action anatomy, ArtSet revision, and paper/sky hierarchy without route-local fonts, texture paths, or color families

### Requirement: Component-anatomy fidelity is a blocking quality gate
Reference fidelity SHALL be assessed from the final composed Battle canvas at
the component-anatomy level, including nested surfaces, rims, highlight/shadow
construction, repeated-card rhythm, relative scale, and page grouping. Similar
palette alone SHALL NOT satisfy the quality standard.

#### Scenario: Complete semantic ArtSet remains visually unrelated
- **WHEN** all 56 slots are complete and valid but the final Battle page still
  uses generic flat buttons or a different grouping from the approved reference
- **THEN** the visual defect remains blocking and the change cannot be declared complete

#### Scenario: Production surfaces are rebuilt
- **WHEN** the reference-faithful component kit is exported
- **THEN** its text-free nine-slice masters independently encode the approved
  cream rim, rounded face, shallow highlight, soil outline, and short bottom
  shadow using selected project-owned ImageGen pixels without cropping or
  shipping the supplied reference image
