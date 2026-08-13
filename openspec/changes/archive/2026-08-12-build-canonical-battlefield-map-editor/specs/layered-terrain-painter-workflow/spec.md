## MODIFIED Requirements

### Requirement: Discoverable terrain-material laboratory entry and target selection
The editor SHALL provide a clearly named terrain-material laboratory entry for the non-release layered terrain scene, SHALL select the currently selected valid laboratory target or the sole valid target in the open scene, MUST require an explicit target choice when selection is ambiguous, and MUST NOT present this entry as the official playable-map editor.

#### Scenario: Selected laboratory target opens
- **WHEN** an artist opens the terrain-material laboratory while a valid layered terrain target is selected
- **THEN** the laboratory displays material composition controls without claiming that the target owns gameplay topology, routes, markers, or catalog publication

#### Scenario: Official map authoring is requested
- **WHEN** a designer chooses the project map-authoring entry
- **THEN** the canonical battlefield map editor opens instead of the terrain mask board or its Scene painter

### Requirement: Authoring validation and runtime parity
The terrain-material laboratory SHALL provide automated coverage and visual evidence for material metadata, composition presets, directed edges, erasure, Undo, mask topology, and runtime terrain parity, but its scene, controls, or diagnostic board MUST NOT satisfy canonical map-editor readiness or playable-map publication acceptance.

#### Scenario: Laboratory acceptance runs
- **WHEN** focused terrain laboratory validation is executed
- **THEN** material and mask operations pass or fail independently of map identity, gameplay topology, routes, markers, and catalog publication

#### Scenario: Canonical map editor is accepted
- **WHEN** playable-map authoring acceptance is evaluated
- **THEN** only a bounded canonical asset that compiles, publishes, and runs through normal Battle qualifies; the laboratory scene is excluded as substitute evidence

