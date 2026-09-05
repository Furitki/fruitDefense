# runtime-ui-quality-standard Specification

## Purpose
Define the durable runtime UI quality, asset ownership, actual-size readability, and release-validation contract shared by every route.
## Requirements
### Requirement: Normative runtime UI quality standard
The project SHALL maintain one normative runtime UI standard covering typography roles, baseline and optical alignment, line policy, component containment, spacing, color semantics, contrast, state cues, icon canvases, nine-slice safety, illustration placement, and route hierarchy.

#### Scenario: Developer changes a shared component
- **WHEN** a developer changes runtime UI text, layout, color, icon, surface, or illustration composition
- **THEN** the change is evaluated against the same documented rules and numeric quality profile used by automated validation

### Requirement: Finite typography and bounds inspection
Every player-visible runtime UI copy/state SHALL have an explicit text role, alignment, line policy, and authoritative draw rectangle that can be validated with the packaged release font at all supported full and inset portrait geometries.

#### Scenario: Copy exceeds its assigned rectangle
- **WHEN** packaged-font measurement reports that a required line exceeds its width, height, or allowed line count
- **THEN** validation fails and the copy, style, or authoritative layout is corrected without runtime truncation, implicit font shrinking, or hidden overflow

#### Scenario: Text shares a component with an icon or indicator
- **WHEN** text, an icon, an indicator, or a state marker are drawn in one component
- **THEN** their explicit rectangles remain contained, preserve the required gap, and do not overlap in any supported full or inset viewport

### Requirement: Typography alignment and hierarchy
Runtime text SHALL use the packaged Noto Sans SC asset, semantic text roles, consistent baselines, and intentional horizontal/vertical alignment rather than ad-hoc offsets or default-skin behavior.

#### Scenario: Comparable labels and values are displayed
- **WHEN** metric, action, card, or status content appears in a repeated row or group
- **THEN** comparable labels, values, and icon centers align within the documented tolerance and preserve the intended primary/secondary reading order

### Requirement: Semantic color and rendered contrast
Runtime UI colors SHALL originate from semantic theme tokens, meet the documented contrast floor in their actual rendered states, and pair color with a non-color state cue where state meaning is important.

#### Scenario: Alpha, tint, or feedback emphasis changes a state
- **WHEN** a normal, pressed, selected, loading, disabled, success, warning, error, or modal state is rendered
- **THEN** the resulting foreground/background contrast still passes the applicable threshold and the state remains distinguishable without relying only on hue

### Requirement: Optical icon and resource consistency
Each common icon, marker, and indicator SHALL keep a stable import canvas and safe inset while satisfying the documented optical-box, centroid, baseline, and family-weight tolerances at its smallest runtime size.

#### Scenario: Resource set is validated
- **WHEN** a production ArtSet is imported or previewed
- **THEN** validation reports missing/duplicate slots, importer violations, alpha-edge contamination, optical outliers, unbound production files, review dependencies, and mixed-set ownership as failures

#### Scenario: Hub navigation icons are inspected at actual size
- **WHEN** `icon.hub-home`, `icon.hub-activity`, or `icon.hub-growth` is exported or validated
- **THEN** its 24- and 33-logical-point alpha silhouette contains exactly one dominant subject and only necessary semantic negative space, stays within the supporting finite perimeter-complexity threshold, remains distinct from the other two silhouettes, and retains the fixed house-doorway, calendar-star, or two-leaf-sprout semantic respectively without decorative props, scenery, soil clusters, or micro-texture

#### Scenario: A Hub destination becomes selected
- **WHEN** Home, Activity, or Growth owns the visible Hub page
- **THEN** selection is expressed by the raised paper tab, underline, and label without multiplying a selection tint into the domain-icon raster

### Requirement: Reference-driven route hierarchy
Bootstrap, Lobby, Battle, and Settlement SHALL apply the approved Sunny Orchard hierarchy through shared component anatomy, route-appropriate density, illustrations, and ornament restraint while keeping player copy and controls primary.

#### Scenario: Reference layout is adapted to a compact runtime rectangle
- **WHEN** a reference component is composed inside an existing compact runtime rectangle
- **THEN** its hierarchy and orchard identity remain recognizable without baking text, stretching ornaments, hiding controls, or changing hit geometry implicitly

### Requirement: Closed-loop defect audit
The polish change SHALL maintain a severity-ranked defect inventory and SHALL NOT declare completion while any blocking or high-severity typography, overflow, contrast, resource, alignment, seam, mixed-set, or input-drift defect remains open.

#### Scenario: A defect is fixed
- **WHEN** a blocking or high-severity issue is corrected
- **THEN** the inventory records its owner, affected state, before/after evidence, validating gate, and closed status

### Requirement: Raster-aware optical inspection
The quality system SHALL distinguish component rectangles from rendered visual mass and SHALL inspect combined group bounds, perceived alignment proxies, repeated baselines, asymmetric gutters, and occupied-content balance in addition to containment.

#### Scenario: Rectangles pass but the rendered group is off-center
- **WHEN** the icon rectangle and label rectangle are each valid but their combined rendered group exceeds the optical-centering tolerance
- **THEN** quality validation fails and the shared anatomy is corrected rather than declaring the component aligned

#### Scenario: Route content is technically contained but visibly unbalanced
- **WHEN** all components fit yet the occupied bounds or opposite gutters exceed the documented balance tolerance
- **THEN** the route remains a visual failure until its authoritative layout is rebalanced

### Requirement: Finite structural-weight hierarchy
The runtime UI quality system SHALL distinguish light structural surfaces from a gameplay-stage surface and blocking overlays, SHALL assign each semantic component one outline-weight role, and SHALL reject repeated enclosing borders that do not represent a distinct interaction or content boundary.

#### Scenario: Battle structural surfaces are validated
- **WHEN** the ready Battle hierarchy is inspected
- **THEN** exactly one normal-state large component uses `surface.gameplay-stage`, Header and persistent sections use the light standard family, slots/actions retain their own component boundaries, and no legacy outer panel encloses both the stage and controls

#### Scenario: Final outline bands are measured
- **WHEN** the 402×874 live raster is analyzed
- **THEN** the gameplay-stage outline measures 3–5 capture pixels, standard section outlines measure 1–2 capture pixels, and any unapproved second large outline of 3 capture pixels or more fails validation

### Requirement: Concept references remain non-production evidence
Generated full-page UI concepts SHALL remain review evidence and SHALL NOT be used as runtime screens, baked-copy surfaces, or direct layout geometry; every production raster SHALL have one owned text-free master, semantic ArtSet binding, deterministic export record, stable GUID, and validated importer metadata.

#### Scenario: Gameplay-stage art enters production
- **WHEN** the approved concept direction is implemented
- **THEN** a standalone text-free nine-slice gameplay-stage master is exported through the production pipeline and the full-page generated concept has no release dependency

#### Scenario: Activity reward art enters production
- **WHEN** the approved Activity concept direction is implemented
- **THEN** one standalone text-free `illustration.hub-activity-reward` master is exported through the production pipeline and the full-page generated concept has no release dependency

#### Scenario: Reference-led primary action art enters production
- **WHEN** Home and Activity require the approved primary-action treatment
- **THEN** the text-free `action.primary` master is restored in place from the user-selected hash-locked original rounded-square PNG, its stable semantic path and GUID are preserved, and neither the rejected capsule output nor any script-drawn substitute remains an export dependency

#### Scenario: Reference-led bottom navigation art enters production
- **WHEN** the approved Hub bottom navigation is implemented
- **THEN** separately hash-locked ImageGen, text-free `surface.hub-navigation-base` and `surface.hub-navigation-selected-tab` masters own exactly the refined continuous base and selected-tab chrome silhouettes, are exported through the production pipeline, and neither a generic raised-panel surface nor script-drawn primitive approximation is used as their visual substitute

#### Scenario: Production raster tooling is inspected
- **WHEN** a production UI-art exporter or validator is admitted to the release path
- **THEN** it contains no visible-pixel drawing or procedural authoring API and is restricted to fixed-master validation plus non-creative crop, selected-output alpha extraction, padding, alpha-safe resize, fringe cleanup, measurement, hashing, encoding, and metadata operations

#### Scenario: Production ArtSets are validated
- **WHEN** the production ArtSet is imported
- **THEN** all 62 required slots are present exactly once and every slot, including `surface.gameplay-stage`, the three Hub navigation icons, `illustration.hub-activity-reward`, `surface.hub-navigation-base`, and `surface.hub-navigation-selected-tab`, has approved geometry, source/runtime ownership, hashes, stable binding, and importer metadata

### Requirement: Shared plant drag connector and target frame
The runtime UI SHALL render the plant drag connector and target frame through one shared, allocation-free presentation helper using semantic theme colors, an approved production nine-slice frame resource, existing drag-state icons, and finite contained geometry. The target frame SHALL NOT be synthesized from primitive edge rectangles.

#### Scenario: Drag state is communicated without hue
- **WHEN** legal, illegal, merge, or swap feedback is visible
- **THEN** the target-sized frame is paired with the existing distinct legal, prohibition, merge, or swap icon so the result does not depend only on color

#### Scenario: Connector is rendered under portrait scaling
- **WHEN** the connector is drawn at a supported full or inset portrait safe area in ordinary WebGL
- **THEN** every dash remains finite, visible, clipped to the design-space presentation, and aligned with the same GUI transform as its source and destination

#### Scenario: Connector is rendered in a letterboxed PC window
- **WHEN** the portrait design is contained inside a wide or tall standalone-player viewport with fractional scale and a non-zero viewport offset
- **THEN** connector endpoints are projected once into device space, dash rotation does not reapply the letterbox offset, and the line remains aligned with its source and destination

#### Scenario: Target frame is rendered
- **WHEN** a legal, illegal, merge, or swap target is resolved
- **THEN** the authoritative target rectangle uses the approved transparent-center nine-slice UI resource with semantic tint and the distinct existing state icon rather than a procedurally drawn four-edge frame

#### Scenario: Reduced motion is requested
- **WHEN** reduced-motion presentation is active
- **THEN** the static connector, target frame, and semantic icon remain visible without travel, looping, or pulsing motion
