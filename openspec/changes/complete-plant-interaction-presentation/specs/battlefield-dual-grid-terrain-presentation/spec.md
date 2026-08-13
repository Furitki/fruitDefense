## ADDED Requirements

### Requirement: Persistent planting-grid preview
The runtime battle SHALL draw a subtle projection-aligned outline for every plantable cell at rest and SHALL strengthen legal, illegal, occupied, and hover feedback when flowerpot placement is active. The preview SHALL remain above terrain and below pots, plants, enemies, projectiles, and transient feedback.

#### Scenario: Battlefield is idle
- **WHEN** no flowerpot tool is selected and no flowerpot drag is active
- **THEN** every plantable cell has a quiet outline with no placement fill or marker

#### Scenario: Flowerpot placement is active
- **WHEN** the player selects or drags the flowerpot tool
- **THEN** the same projected cells add readable legal, illegal, occupied, and current-target feedback without changing their hit rectangles

#### Scenario: Portrait safe area changes
- **WHEN** the board projection is recomputed for a supported portrait safe area
- **THEN** the planting-grid outlines remain aligned with the canonical cell and flowerpot interaction rectangles
