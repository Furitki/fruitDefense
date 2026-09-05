## MODIFIED Requirements

### Requirement: Concept references remain non-production evidence
Generated full-page UI concepts SHALL remain review evidence and SHALL NOT be used as runtime screens, baked-copy surfaces, or direct layout geometry; every production raster SHALL have one owned text-free master, semantic ArtSet binding, deterministic export record, stable GUID, and validated importer metadata.

#### Scenario: Gameplay-stage art enters production
- **WHEN** the approved concept direction is implemented
- **THEN** a standalone text-free nine-slice gameplay-stage master is exported through the production pipeline and the full-page generated concept has no release dependency

#### Scenario: Activity reward art enters production
- **WHEN** the approved Activity concept direction is implemented
- **THEN** one standalone text-free `illustration.hub-activity-reward` master is exported through the production pipeline and the full-page generated concept has no release dependency

#### Scenario: Reference-led primary action art enters production
- **WHEN** the Home and Activity references require the active primary action surface to be corrected
- **THEN** the existing text-free `action.primary` master is restored in place from the user-selected hash-locked original rounded-square PNG, its stable semantic path and GUID are preserved, and the rejected capsule output plus every script-drawn substitute are no longer export dependencies

#### Scenario: Reference-led bottom navigation art enters production
- **WHEN** the approved Hub bottom navigation is implemented
- **THEN** separately hash-locked ImageGen, text-free `surface.hub-navigation-base` and `surface.hub-navigation-selected-tab` masters own the two refined chrome silhouettes, are exported through the production pipeline, and neither a generic raised-panel surface nor script-drawn primitive approximation is used as their visual substitute

#### Scenario: Production raster tooling is inspected
- **WHEN** a production UI-art exporter or validator is admitted to the release path
- **THEN** it contains no visible-pixel drawing or procedural authoring API and is restricted to fixed-master validation plus non-creative crop, selected-output alpha extraction, padding, alpha-safe resize, fringe cleanup, measurement, hashing, encoding, and metadata operations

#### Scenario: An approved Home reference is decomposed after user authorization
- **WHEN** a complete text-free Home illustration window, navigation silhouette, or domain icon is split from the approved page reference
- **THEN** the owned master records the reference hash and exact crop or isolation operation, excludes baked copy and unsupported data, and enters production only as an independent semantic component rather than as a page crop or runtime screenshot

#### Scenario: Production ArtSets are validated
- **WHEN** a production ArtSet is imported
- **THEN** all 62 required slots are present exactly once and every slot, including `surface.gameplay-stage`, the three Hub navigation icons, `illustration.hub-activity-reward`, `surface.hub-navigation-base`, and `surface.hub-navigation-selected-tab`, has approved geometry, source/runtime ownership, hashes, stable binding, and importer metadata
