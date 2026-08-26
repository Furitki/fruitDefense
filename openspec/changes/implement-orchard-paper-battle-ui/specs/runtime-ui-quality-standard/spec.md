## ADDED Requirements

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

#### Scenario: Production ArtSets are validated
- **WHEN** either production ArtSet is imported
- **THEN** all 56 required slots are present exactly once and `surface.gameplay-stage` has the approved geometry, slice border, safe inset, optical bounds, source/runtime ownership, hashes, and stable binding
