## MODIFIED Requirements

### Requirement: Canonical three-layer battlefield definition
Every battlefield map SHALL define one complete semantic visual-surface layout whose optional landforms carry explicit presentation-only contour style, one complete gameplay cell-capability/collision layout, stable named ordered routes, and a typed marker layer under one versioned map identity. Simulation MUST NOT infer gameplay rules from visual surfaces or contour styles, and presentation MUST NOT mutate gameplay topology or marker state.

#### Scenario: Visual and gameplay meanings differ
- **WHEN** a square-contour grass-surface cell does not carry the plantable capability
- **THEN** the presenter renders square grass, placement remains illegal, and the map passes validation when no marker requires plantability at that cell

#### Scenario: Layer coverage is incomplete
- **WHEN** either the visual-surface layout or gameplay-cell layout does not contain exactly `GridWidth × GridHeight` entries
- **THEN** map compilation fails with the missing or excess layer and cell index identified

#### Scenario: Landform contour is missing
- **WHEN** a canonical visual cell declares a landform without a supported contour style after migration
- **THEN** map compilation fails with the cell, landform, and missing contour identity identified

### Requirement: Layer-specific deterministic identity
The system SHALL compute gameplay map identity from dimensions, gameplay cells, collision channels, ordered routes, gameplay marker groups, gameplay markers, and gameplay-affecting references in canonical order, and SHALL exclude semantic visual surfaces, contour styles, edge styles, terrain palettes, sprites, and other presentation-only values from simulation identity.

#### Scenario: Only visual surface or contour changes
- **WHEN** two valid maps have identical gameplay topology and markers but different surface layouts, contour styles, edge styles, or terrain palettes
- **THEN** their gameplay map fingerprints and deterministic simulation outcomes are equal

#### Scenario: Gameplay marker changes
- **WHEN** a spawn, goal, core, initial-pot marker, route, capability, or collision channel changes
- **THEN** the gameplay map fingerprint changes deterministically

